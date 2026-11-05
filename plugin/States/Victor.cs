using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using HarmonyLib;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ================================================================== Victor (2026-11-05, the creator's own DLL build only)
    // Pain-powered bruiser from the creator's spec: Pain stacks from hits taken speed up everything (buff pain, attackSpeedBaseAdd),
    // Left Click punches, Right Click spins up Memento Mori (a gun on M2), Shift spends 3 Pain on an aimed bolt, R toggles the
    // Aura of Suffering, Q is Shocking Reanimation (any carrier).

    /// <summary>Server side of Victor's kit that isn't a state: Pain from hits taken (heroes.passiveBuff / hitsPerStack), spending
    /// the oldest stacks, and the Ukulele rolls of a Shocking Reanimation charge.</summary>
    public static class VictorPain
    {
        static readonly Dictionary<CharacterBody, int> hits = new Dictionary<CharacterBody, int>();
        static readonly AccessTools.FieldRef<CharacterBody, List<CharacterBody.TimedBuff>> timed =
            AccessTools.FieldRefAccess<CharacterBody, List<CharacterBody.TimedBuff>>("timedBuffs");

        public static void Init() => GlobalEventManager.onServerDamageDealt += OnDamage;

        static void OnDamage(DamageReport r)
        {
            if (r == null || r.damageInfo == null) return;
            var victim = r.victimBody; var attacker = r.attackerBody;
            if (victim && attacker && attacker != victim && r.damageDealt > 0f && attacker.teamComponent.teamIndex != victim.teamComponent.teamIndex)
            {
                // every enemy hit counts; his own aura (attacker = himself) and falls (no attacker) don't
                var hero = Sheet.Hero(Survivors.HeroOf(victim));
                if (hero != null && hero.PassiveBuff != "none" && hero.HitsPerStack > 0)
                {
                    if (hits.Count > 64) foreach (var k in hits.Keys.Where(b => !b).ToList()) hits.Remove(k);
                    int n = (hits.TryGetValue(victim, out var c) ? c : 0) + 1;
                    if (n >= (int)hero.HitsPerStack) { n = 0; Add(victim, hero.PassiveBuff, 1); }
                    hits[victim] = n;
                }
            }
            if (attacker && victim && attacker != victim) ReanimationCharge.OnHit(r);
        }

        /// <summary>n stacks of a timed stacking buff (Pain), each on its own timer; at the cap the oldest makes room. Server only.</summary>
        public static void Add(CharacterBody b, string id, int n)
        {
            var row = Sheet.Buff(id); var d = Buffs.Get(id);
            if (row == null || !d || !b || !NetworkServer.active) return;
            int max = Mathf.Max(1, (int)row.MaxStacks);
            for (int i = 0; i < n; i++)
            {
                if (b.GetBuffCount(d) >= max) RemoveOldest(b, id, 1);
                b.AddTimedBuff(d, row.Duration);
            }
            if (MeltyTest.Active) MeltyTest.Report("pain-stacks", true, b.baseNameToken);
        }

        /// <summary>Takes away the n stacks closest to running out (Pain Battery spends the 3 oldest). Server only.</summary>
        public static void RemoveOldest(CharacterBody b, string id, int n)
        {
            var d = Buffs.Get(id);
            if (!b || !d || !NetworkServer.active) return;
            var list = timed(b);
            for (int k = 0; k < n; k++)
            {
                int at = -1; float least = float.MaxValue;
                for (int i = 0; i < list.Count; i++) if (list[i].buffIndex == d.buffIndex && list[i].timer < least) { least = list[i].timer; at = i; }
                if (at >= 0) list.RemoveAt(at);
                if (b.HasBuff(d)) b.RemoveBuff(d);
            }
        }
    }

    // ---------------------------------------------------------------- M1: Heavy melee
    /// <summary>Hold Left Click: a wind-up (minDuration), then a heavy punch, alternating arms, hitting everything within reach in a
    /// 2 x spreadDeg arc in front for damagePct; one every interval (both scaled by attack speed, so Pain speeds them up). A short
    /// hit-stop and camera kick on a hit; from 5 Pain sparks on every hit. Server deals the damage along the aim the owner sent.</summary>
    [Kind("victor_punch")]
    public sealed class VictorPunch : DlrState
    {
        const float HitStop = 0.06f;
        float swingStart, windup, cycle, stopUntil; bool struck, alt;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            Swing();
        }

        void Swing()
        {
            swingStart = fixedAge;
            windup = row.MinDuration / attackSpeedStat;
            cycle = row.Interval / attackSpeedStat;
            struck = false;
            if (rt != null) { rt.AltMuzzle = !rt.AltMuzzle; alt = rt.AltMuzzle; }
            if (anim) anim.Gesture(alt ? "melee_alt" : "melee", Mathf.Max(1f, attackSpeedStat));
            characterBody.SetAimTimer(2f);
            if (isAuthority) OwnerAim = GetAimRay().direction;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (anim && stopUntil > 0f && fixedAge >= stopUntil) { anim.enabled = true; stopUntil = 0f; }
            float t = fixedAge - swingStart;
            if (!struck && t >= windup) { struck = true; Strike(); }
            if (t < cycle) return;
            // held: the next punch at once (test mode holds it through WantsFire)
            bool held = inputBank && inputBank.skill1.down || MeltyTest.WantsFire(characterBody);
            if (held && isAuthority) Swing();
            else if (isAuthority) outer.SetNextStateToMain();
        }

        void Strike()
        {
            var dir = ThrowDir; dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = characterDirection ? characterDirection.forward : transform.forward;
            dir.Normalize();
            if (characterDirection) characterDirection.forward = dir;
            var core = characterBody.corePosition;
            Fx.Sound(row.Id, "cast", core);
            Vfx.Play(row.Vfx, "cast", core + dir * 1.2f, 1f, null, core + dir * (row.Reach + 1f), alt ? -1f : 1f);
            float cos = Mathf.Cos(row.SpreadDeg * Mathf.Deg2Rad);
            bool hit = false, sparks = Buffs.Count(characterBody, "pain") >= 5;
            foreach (var hb in Fx.Enemies(characterBody, core, row.Reach + characterBody.radius + 2f))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                var to = b.corePosition - core; float flat = new Vector2(to.x, to.z).magnitude;
                if (flat - b.radius > row.Reach + characterBody.radius) continue;
                var tf = to; tf.y = 0;
                if (tf.sqrMagnitude > 0.25f && Vector3.Dot(tf.normalized, dir) < cos) continue;
                hit = true;
                Fx.Sound(row.Id, "hit", b.corePosition);
                Vfx.Play(row.Vfx, sparks ? "spark" : "hit", b.corePosition, 1f);
                if (!Server) continue;
                Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition, new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Primary });
                if (!b.isChampion) Fx.KnockBack(characterBody, hb.healthComponent, dir, row.Knockback);
            }
            if (!hit) return;
            MeltyTest.Report("punch", true, characterBody.baseNameToken);
            // the heavy feel: a short freeze and a kick of the camera
            if (anim) { anim.enabled = false; stopUntil = fixedAge + HitStop; }
            if (isAuthority) AddRecoil(-1.2f, -1.6f, -0.5f, 0.5f);
        }

        public override void OnExit()
        {
            if (anim) anim.enabled = true;
            base.OnExit();
        }

        // mid-swing nothing cuts in (the gun waits for the punch to end)
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------------------------------------------------------- M2: Memento Mori
    /// <summary>Hold Right Click: the gun spins up over duration seconds, its rate rising evenly from 1/interval to 1/minDuration
    /// shots a second and its spread from a third of spreadDeg to all of it. Let go and press again within hangTime and it is still
    /// spun up. Everything else (magazine, reload, falloff, tracers) is the shared gun.</summary>
    [Kind("gun_spin")]
    public sealed class GunSpin : GunState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            if (rt == null || row == null) return;
            if (Time.time - rt.SpinReleasedAt > row.HangTime) { rt.Spin = 0f; Sound("tick"); }
        }

        public override void FixedUpdate()
        {
            if (rt != null && row != null && row.Duration > 0f) rt.Spin = Mathf.Min(1f, rt.Spin + Time.fixedDeltaTime * attackSpeedStat / row.Duration);
            base.FixedUpdate();
        }

        protected override float ShotInterval()
        {
            float s = rt != null ? rt.Spin : 1f;
            float rate = Mathf.Lerp(1f / Mathf.Max(0.01f, row.Interval), 1f / Mathf.Max(0.01f, row.MinDuration), s);
            return 1f / rate;
        }

        protected override float Spread() => Mathf.Lerp(row.SpreadDeg / 3f, row.SpreadDeg, rt != null ? rt.Spin : 1f);

        public override void OnExit()
        {
            if (rt != null) rt.SpinReleasedAt = Time.time;
            base.OnExit();
        }
    }

    // ---------------------------------------------------------------- Shift: Pain Battery
    /// <summary>Spends buffStacks of the buff (3 Pain) on an instant bolt along the aim, up to range: the first enemy or surface it
    /// meets, and everything within radius of that point, takes damagePct and the debuff (a 40% slow). With fewer stacks nothing
    /// happens and the charge stays. Never hurts Victor.</summary>
    [Kind("pain_battery")]
    public sealed class PainBattery : DlrState
    {
        bool fired;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            if (Buffs.Count(characterBody, row.Buff) < (int)row.BuffStacks) { RefundStock(); return; }
            fired = true;
            if (anim) anim.Gesture("utility", 1.4f);
            characterBody.SetAimTimer(1.5f);
            var aim = new Ray(isAuthority ? GetAimRay().origin : (inputBank ? inputBank.aimOrigin : characterBody.corePosition), ThrowDir);
            var end = aim.origin + aim.direction * row.Range;
            HurtBox target = null;
            var hits = Physics.RaycastAll(aim, row.Range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (hb && (!hb.healthComponent || hb.healthComponent == healthComponent || hb.healthComponent.body && hb.healthComponent.body.teamComponent.teamIndex == teamComponent.teamIndex)) continue;
                end = h.point; target = hb;
                break;
            }
            var from = VictorAura.ArmPoint(characterBody);
            Sound("cast");
            Vfx.Play(row.Vfx, "cast", from, row.Radius, null, end);
            Fx.Sound(row.Id, "hit", end);
            Vfx.Play(row.Vfx, "hit", end, row.Radius);
            MeltyTest.Report("pain-battery", true, characterBody.baseNameToken);
            if (!Server) return;
            VictorPain.RemoveOldest(characterBody, row.Buff, (int)row.BuffStacks);
            var struck = new HashSet<HealthComponent>();
            if (target && target.healthComponent && target.healthComponent.alive)
            {
                struck.Add(target.healthComponent);
                Fx.Hit(characterBody, target, Coef, row.ProcCoefficient, end);
                Buffs.Apply(characterBody, target.healthComponent.body, row.Debuff);
            }
            foreach (var hb in Fx.Enemies(characterBody, end, row.Radius))
            {
                if (!struck.Add(hb.healthComponent)) continue;
                Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, hb.healthComponent.body ? hb.healthComponent.body.corePosition : end);
                if (hb.healthComponent.body) Buffs.Apply(characterBody, hb.healthComponent.body, row.Debuff);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && (!fired || fixedAge >= 0.3f)) outer.SetNextStateToMain();
        }
    }

    // ---------------------------------------------------------------- R: Aura of Suffering (toggle)
    /// <summary>R switches the aura on (its buff, kept by the server) or off. Switching on gives the charge straight back so R can
    /// switch it off at once; switching off lets the cooldown (5 s) run. The aura itself is <see cref="VictorAura"/>, on its own
    /// component, so it stays on while Victor punches and shoots.</summary>
    [Kind("aura_toggle")]
    public sealed class AuraToggle : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            bool on = Buffs.Count(characterBody, VictorAura.Buff) > 0;
            if (!on) RefundStock();
            Sound(on ? "end" : "cast");
            if (anim) anim.Gesture(on ? "special" : "special_start", 1.2f);
            if (!on) Vfx.Play(row.Vfx, "cast", characterBody.footPosition, row.Radius);
            if (Server) VictorAura.Switch(characterBody, row, !on);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.2f && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;
    }

    /// <summary>On Victor's body, every machine: the barbed-wire arm crackling from 3 Pain, and the aura while its buff is on (the
    /// field, its loop sound, its radius following Pain). On the server, while on: every interval each enemy within radius +
    /// reach x Pain takes a tick ramping from damageMinPct to damagePct a second over duration, and Victor heals healFraction of what
    /// it dealt; selfDrainPct of his max health a second; +buffStacks Pain every buffEvery seconds; off at stopBelowPct health.</summary>
    public sealed class VictorAura : MonoBehaviour
    {
        public const string Buff = "victor_aura";
        CharacterBody body; SkillRow row;
        GameObject field, wire; int loop; Transform arm;
        float onSince, nextTick, nextDrain, nextPain; bool wasOn;

        void Awake() => body = GetComponent<CharacterBody>();

        void Start()
        {
            var hero = Survivors.HeroOf(body);
            row = hero != null ? Sheet.SkillsOf(hero).FirstOrDefault(r => r.Kind == "aura_toggle") : null;
        }

        /// <summary>Where the barbed-wire left arm is (the hand bone), for the bolt and the crackle.</summary>
        public static Vector3 ArmPoint(CharacterBody b)
        {
            var a = b ? b.GetComponent<VictorAura>() : null;
            var t = a ? a.Arm() : null;
            return t ? t.position : b ? b.corePosition : Vector3.zero;
        }

        Transform Arm()
        {
            if (arm) return arm;
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            if (model) arm = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "hand_L");
            return arm;
        }

        /// <summary>Server: the aura on (the jolt, 1 Pain) or off.</summary>
        public static void Switch(CharacterBody b, SkillRow r, bool on)
        {
            if (!NetworkServer.active || !b) return;
            var a = b.GetComponent<VictorAura>() ?? b.gameObject.AddComponent<VictorAura>();
            a.row = r;
            if (!on) { Buffs.Clear(b, Buff); return; }
            Buffs.AddTimed(b, Buff, 0f, 1);
            a.onSince = Time.fixedTime; a.nextTick = Time.fixedTime; a.nextDrain = Time.fixedTime + 1f; a.nextPain = Time.fixedTime + r.BuffEvery;
            a.SelfHurt(r.SelfDamagePct / 100f);
            VictorPain.Add(b, r.Buff, (int)Mathf.Max(1, r.BuffStacks));
        }

        /// <summary>Server: the aura off (test mode turns it off after its check; death ends it with the body).</summary>
        public static void Off(CharacterBody b) { if (b) Buffs.Clear(b, Buff); }

        float Radius => row == null ? 0f : row.Radius + row.Reach * Buffs.Count(body, row.Buff);

        void SelfHurt(float fraction)
        {
            var hc = body.healthComponent;
            if (!hc || !hc.alive || fraction <= 0f) return;
            // his own damage (attacker = himself): never counted as a hit for Pain, never kills him
            hc.TakeDamage(new DamageInfo
            {
                attacker = body.gameObject, inflictor = body.gameObject, damage = hc.fullCombinedHealth * fraction, position = body.corePosition,
                damageType = DamageType.NonLethal | DamageType.BypassArmor | DamageType.BypassBlock, procCoefficient = 0f, crit = false,
                damageColorIndex = DamageColorIndex.Bleed,
            });
        }

        void Update()
        {
            if (!body) return;
            int pain = Buffs.Count(body, "pain");
            // the barbed wire crackles from 3 Pain: Pain Battery is ready
            if (pain >= 3 && !wire && Arm()) wire = Vfx.Play("victor_wire", "loop", arm.position, 1f, arm);
            else if (pain < 3 && wire) { Vfx.Stop(wire, 0.3f); wire = null; }
            bool on = row != null && Buffs.Count(body, Buff) > 0;
            if (on && !field)
            {
                field = Vfx.Play(row.Vfx, "loop", body.footPosition, row.Radius, body.transform);
                loop = Fx.Loop(row.Id, body.footPosition);
            }
            else if (!on && field) { Vfx.Stop(field, 0.5f); field = null; DeadlockAudio.Stop(loop); loop = 0; }
            if (field) field.transform.localScale = Vector3.one * (Radius / Mathf.Max(0.1f, row.Radius));
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active || row == null || !body || !body.healthComponent) return;
            bool on = Buffs.Count(body, Buff) > 0;
            if (on && !wasOn && onSince <= 0f) onSince = Time.fixedTime;
            wasOn = on;
            if (!on) { onSince = 0f; return; }
            var hc = body.healthComponent;
            if (!hc.alive) return;
            float now = Time.fixedTime;
            // never kills him: off at stopBelowPct health
            if (hc.combinedHealthFraction <= row.StopBelowPct / 100f)
            {
                Buffs.Clear(body, Buff);
                Relay.Sound(row.Id, "end", body.corePosition);
                return;
            }
            if (now >= nextDrain) { nextDrain = now + 1f; SelfHurt(row.SelfDrainPct / 100f); }
            if (row.BuffEvery > 0f && now >= nextPain) { nextPain = now + row.BuffEvery; VictorPain.Add(body, row.Buff, (int)Mathf.Max(1, row.BuffStacks)); }
            if (now < nextTick) return;
            nextTick = now + row.Interval;
            float ramp = row.Duration > 0f ? Mathf.Clamp01((now - onSince) / row.Duration) : 1f;
            float coef = Mathf.Lerp(row.DamageMinPct, row.DamagePct, ramp) / 100f * row.Interval;
            float healed = 0f;
            foreach (var hb in Fx.Enemies(body, body.corePosition, Radius))
            {
                var victim = hb.healthComponent;
                var vb = victim.body;
                float before = victim.combinedHealth;
                Fx.Hit(body, hb, coef, row.TickProc, vb ? vb.corePosition : hb.transform.position);
                healed += Mathf.Max(0f, before - victim.combinedHealth) * row.HealFraction;
                // a thin red thread from the enemy to Victor, now and then (every player sees it)
                if (vb && Random.value < 0.35f) Relay.Vfx(row.Vfx, "hit", vb.corePosition, 1f, null, body.corePosition);
            }
            if (healed > 0f) hc.Heal(healed, default);
        }

        void OnDestroy() { Vfx.Stop(field, 0.2f); Vfx.Stop(wire, 0.2f); DeadlockAudio.Stop(loop); }
    }

    // ---------------------------------------------------------------- Q: Shocking Reanimation (any carrier)
    /// <summary>Rise launchHeight metres over minDuration seconds with gravity off and untouchable (Victor plays his zipline clip,
    /// anyone else is frozen in their pose); at the peak lightning strikes the carrier (no damage) and a ball of lightning stuns every
    /// enemy within radius for stunDuration; then they drop with no fall damage and carry the buff for duration seconds
    /// (<see cref="ReanimationCharge"/>: buffStacks Ukuleles and Tesla Coils). Victor also fills to the Pain cap.</summary>
    [Kind("ult_reanimation")]
    public sealed class UltReanimation : UltState
    {
        bool struck, floating, frozen; float dropStart; Animator vanilla;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            if (Server) characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, row.MinDuration + 0.3f);
            if (characterMotor)
            {
                characterMotor.Motor.ForceUnground();
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
            // Victor rides his zipline clip up; anyone else is frozen in their pose
            if (anim && anim.HasClip("ult_loop")) anim.Override("ult_loop", row.MinDuration, true);
            else if (anim) { anim.enabled = false; frozen = true; }
            else { vanilla = GetModelAnimator(); if (vanilla) { vanilla.speed = 0f; frozen = true; } }
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, 1f, transform);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!struck)
            {
                if (isAuthority && characterMotor)
                {
                    characterMotor.moveDirection = Vector3.zero;
                    characterMotor.velocity = Vector3.up * row.LaunchHeight / Mathf.Max(0.1f, row.MinDuration);
                }
                if (fixedAge >= row.MinDuration) Strike();
                return;
            }
            // the drop: straight down until he lands (at most 4 s)
            if (isAuthority && (characterMotor && characterMotor.isGrounded && fixedAge - dropStart > 0.1f || fixedAge - dropStart > 4f)) outer.SetNextStateToMain();
        }

        void Strike()
        {
            struck = true; dropStart = fixedAge;
            Unfreeze();
            Land();
            if (anim && anim.HasClip("ult_start")) anim.Override("ult_start", 0.8f);
            var at = characterBody.corePosition;
            Fx.Sound(row.Id, "hit", at);
            Vfx.Play(row.Vfx, "hit", at, row.Radius);
            MeltyTest.Report("reanimation-strike", true, characterBody.baseNameToken);
            // the 20 s charge: visuals on every machine, the Tesla pulses on the server (Ukulele: VictorPain's hook reads the buff)
            ReanimationCharge.Start(characterBody, row);
            if (!Server) return;
            Buffs.Clear(characterBody, row.Buff);
            Buffs.AddTimed(characterBody, row.Buff, row.Duration);
            foreach (var hb in Fx.Enemies(characterBody, at, row.Radius)) Fx.Stun(hb.healthComponent, row.StunDuration);
            // Victor: straight to full Pain, each stack on a fresh timer
            var hero = Sheet.Hero(Survivors.HeroOf(characterBody));
            if (hero != null && hero.PassiveBuff != "none")
            {
                var pain = Sheet.Buff(hero.PassiveBuff);
                if (pain != null) VictorPain.Add(characterBody, hero.PassiveBuff, (int)pain.MaxStacks);
            }
        }

        void Unfreeze()
        {
            if (!frozen) return;
            frozen = false;
            if (anim) anim.enabled = true;
            if (vanilla) vanilla.speed = 1f;
        }

        void Land()
        {
            if (!floating || !characterMotor) return;
            floating = false;
            var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
        }

        public override void OnExit()
        {
            if (!struck && Server) Strike();
            Unfreeze();
            Land();
            if (anim) anim.StopOverride();
            if (!Survivors.NoFallDamage(characterBody)) characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            base.OnExit();
        }
    }

    /// <summary>A Shocking Reanimation charge on its carrier, for the buff's length: a crackle around them (every machine) and, on
    /// the server, every interval a Tesla bolt (damagePct, procCoefficient) at up to chainCount enemies within range through walls;
    /// and (<see cref="OnHit"/>) each hit they deal rolls 25% x its proc coefficient for an Ukulele chain (chainPct total damage,
    /// chainCount targets within seekRange, tickProc). Copied in code: no real items, so nothing can be scrapped, printed or stolen.
    /// The carrier's own Ukuleles and Tesla Coils keep working on their own on top.</summary>
    public sealed class ReanimationCharge : MonoBehaviour
    {
        CharacterBody body; SkillRow row; float until, next; GameObject fx;

        public static void Start(CharacterBody b, SkillRow r)
        {
            if (!b) return;
            var c = b.GetComponent<ReanimationCharge>() ?? b.gameObject.AddComponent<ReanimationCharge>();
            c.body = b; c.row = r; c.until = Time.time + r.Duration; c.next = Time.time + 0.2f;
            if (!c.fx) c.fx = Vfx.Play(r.Vfx, "loop", b.corePosition, 1f, b.transform);
            c.enabled = true;
        }

        void Update()
        {
            if (!body || row == null || Time.time >= until || !body.healthComponent || !body.healthComponent.alive)
            {
                Vfx.Stop(fx, 0.5f); fx = null; enabled = false; return;
            }
            if (!NetworkServer.active || Time.time < next) return;
            next = Time.time + row.Interval;
            if (Buffs.Count(body, row.Buff) <= 0) return;
            int n = 0;
            foreach (var hb in Fx.Enemies(body, body.corePosition, row.Range, (int)row.ChainCount))
            {
                var orb = new LightningOrb
                {
                    origin = body.corePosition, damageValue = body.damage * row.DamagePct / 100f, isCrit = body.RollCrit(),
                    bouncesRemaining = 0, teamIndex = body.teamComponent.teamIndex, attacker = body.gameObject, procCoefficient = row.ProcCoefficient,
                    lightningType = LightningOrb.LightningType.Tesla, damageColorIndex = DamageColorIndex.Item, range = row.Range, target = hb,
                };
                orb.procChainMask.AddProc(ProcType.ChainLightning);
                OrbManager.instance.AddOrb(orb);
                n++;
            }
            if (n > 0) MeltyTest.Report("reanimation-tesla", true, body.baseNameToken);
        }

        /// <summary>Server: an Ukulele roll for a hit dealt by a charged carrier.</summary>
        public static void OnHit(DamageReport r)
        {
            var a = r.attackerBody; var info = r.damageInfo;
            if (!a || info.procCoefficient <= 0f || info.procChainMask.HasProc(ProcType.ChainLightning)) return;
            if (Buffs.Count(a, "reanimation_charged") <= 0) return;
            var row = SkillRow.All.FirstOrDefault(s => s.Kind == "ult_reanimation");
            if (row == null || !Util.CheckRoll(25f * info.procCoefficient, a.master)) return;
            var orb = new LightningOrb
            {
                origin = info.position, damageValue = Util.OnHitProcDamage(info.damage, a.damage, row.ChainPct / 100f), isCrit = info.crit,
                bouncesRemaining = Mathf.Max(0, (int)row.ChainCount - 1), teamIndex = a.teamComponent.teamIndex, attacker = a.gameObject,
                bouncedObjects = new List<HealthComponent> { r.victim }, procChainMask = info.procChainMask, procCoefficient = row.TickProc,
                lightningType = LightningOrb.LightningType.Ukulele, damageColorIndex = DamageColorIndex.Item, range = row.SeekRange,
            };
            orb.procChainMask.AddProc(ProcType.ChainLightning);
            orb.damageType.damageTypeExtended = DamageTypeExtended.Electrical;
            var next = orb.PickNextTarget(info.position);
            if (!next) return;
            orb.target = next;
            OrbManager.instance.AddOrb(orb);
        }
    }
}
