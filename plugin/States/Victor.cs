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
    // Pain-powered bruiser from the creator's spec (redesigned the same day): Pain stacks from hits taken speed up everything (buff
    // pain, attackSpeedBaseAdd), Left Click spins up Memento Mori, Right Click is Jumpstart (hurt himself for Pain and speed), Shift
    // spends 3 Pain on an aimed bolt, R toggles the Aura of Suffering, Q is Shocking Reanimation (any carrier).

    /// <summary>Server side of Victor's kit that isn't a state: Pain from hits taken (heroes.passiveBuff / hitsPerStack), spending
    /// the oldest stacks, and the Ukulele rolls of a Shocking Reanimation charge.</summary>
    public static class VictorPain
    {
        static readonly Dictionary<CharacterBody, int> hits = new Dictionary<CharacterBody, int>();

        public static void Init()
        {
            GlobalEventManager.onServerDamageDealt += OnDamage;
            RoR2Application.onFixedUpdate += Decay;
        }

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

        /// <summary>Bodies holding Pain and when their next stack drops (server).</summary>
        static readonly Dictionary<CharacterBody, float> decayAt = new Dictionary<CharacterBody, float>();

        /// <summary>n stacks of a decaying stacking buff (Pain), up to its cap. The stacks are plain (not timed): one drops every
        /// buff duration (10 s) while any are left (creator, 2026-11-05: "1 of each stack will dissolve every 10 second ... the stack
        /// goes from 9 to 8 meaning the buff is only 8x for the next 10 seconds"). The clock starts when he goes from 0 to 1 and is
        /// not reset by new stacks. Server only.</summary>
        public static void Add(CharacterBody b, string id, int n)
        {
            var row = Sheet.Buff(id); var d = Buffs.Get(id);
            if (row == null || !d || !b || !NetworkServer.active) return;
            int max = Mathf.Max(1, (int)row.MaxStacks);
            if (b.GetBuffCount(d) == 0) decayAt[b] = Time.fixedTime + row.Duration;
            for (int i = 0; i < n && b.GetBuffCount(d) < max; i++) b.AddBuff(d);
            if (!decayAt.ContainsKey(b)) decayAt[b] = Time.fixedTime + row.Duration;
            if (MeltyTest.Active) MeltyTest.Report("pain-stacks", true, b.baseNameToken);
        }

        /// <summary>Takes away n stacks (Pain Battery spends 1). Server only.</summary>
        public static void RemoveOldest(CharacterBody b, string id, int n)
        {
            var d = Buffs.Get(id);
            if (!b || !d || !NetworkServer.active) return;
            for (int k = 0; k < n && b.HasBuff(d); k++) b.RemoveBuff(d);
        }

        /// <summary>Victor hurting himself (Jumpstart, the aura): attacker = himself, so it never counts as a hit for Pain; goes
        /// through armor and blocks. Jumpstart never kills him (NonLethal); the aura's drain can (creator, 2026-11-09: "Keep, can kill").
        /// Server only.</summary>
        public static void HurtSelf(CharacterBody b, float amount, bool lethal = false)
        {
            var hc = b ? b.healthComponent : null;
            if (!hc || !hc.alive || amount <= 0f || !NetworkServer.active) return;
            hc.TakeDamage(new DamageInfo
            {
                attacker = b.gameObject, inflictor = b.gameObject, damage = amount, position = b.corePosition,
                damageType = (lethal ? DamageType.Generic : DamageType.NonLethal) | DamageType.BypassArmor | DamageType.BypassBlock, procCoefficient = 0f, crit = false,
                damageColorIndex = DamageColorIndex.Bleed,
            });
        }

        /// <summary>The decay clock: one stack of each Pain holder's buff every buff duration. Server, every fixed update.</summary>
        static void Decay()
        {
            if (!NetworkServer.active || decayAt.Count == 0) return;
            float now = Time.fixedTime;
            foreach (var b in decayAt.Keys.ToList())
            {
                var hero = b ? Sheet.Hero(Survivors.HeroOf(b)) : null;
                var d = hero != null ? Buffs.Get(hero.PassiveBuff) : null;
                var row = hero != null ? Sheet.Buff(hero.PassiveBuff) : null;
                if (!b || !d || row == null || b.GetBuffCount(d) == 0) { decayAt.Remove(b); continue; }
                if (now < decayAt[b]) continue;
                b.RemoveBuff(d);
                decayAt[b] = now + row.Duration;
            }
        }
    }

    // ---------------------------------------------------------------- M2: Jumpstart
    /// <summary>Right Click: Victor jolts himself with the defibrillator. selfDamageFlat health (non-lethal, his own damage so it is
    /// never a hit for Pain), buffStacks Pain, and the buff (+30 % move speed, refreshed rather than stacked). The jolt clip plays
    /// for duration seconds on the upper body while he keeps walking; it is on the Weapon machine, so it pauses Memento Mori for
    /// that long and the held trigger picks the gun up again still spun up (gun hangTime is longer than the jolt).
    /// The strike holds him for minDuration (creator, 2026-11-10: "the lightning does not strike victor ... make sure it always
    /// follows victor ... put in a white flash on his skin and pause his movement and animation for 0.5 second"): his skin flashes
    /// white (mat_flash_white overlay), the jolt pose freezes, RoR2's Entangle root (server) and the owner's velocity held at 0, so
    /// the bolt always lands on him.</summary>
    [Kind("jumpstart")]
    public sealed class Jumpstart : DlrState
    {
        bool paused, released;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            WhiteFlash(characterBody, row.MinDuration);
            if (Server && row.MinDuration > 0f) characterBody.AddTimedBuff(RoR2Content.Buffs.Entangle, row.MinDuration);
            if (isAuthority && characterMotor) characterMotor.velocity = Vector3.zero;
            Sound("cast");
            if (anim) anim.Gesture("secondary", Mathf.Max(1f, anim.ClipLength("secondary") / Mathf.Max(0.1f, row.Duration)));
            Vfx.Play(row.Vfx, "cast", VictorAura.ArmPoint(characterBody), 1f, null, characterBody.corePosition);
            // a bolt of green lightning comes down on him (creator, 2026-11-09: "have a strike of green lightning come down!"), and
            // as it lands a radius blast round him (creator, 2026-11-10: "a 5m radius aoe damage ... exactly when the lightning
            // strikes victor ... creative and green"): the nova on every machine, the hit and an arc to each enemy from the server
            Vfx.Play(row.Vfx, "strike", characterBody.footPosition, 1f, null, characterBody.corePosition);
            if (row.Radius > 0f) Vfx.Play(row.Vfx, "nova", characterBody.footPosition, row.Radius);
            if (!Server) return;
            if (row.DamagePct > 0f && row.Radius > 0f)
                foreach (var hb in Fx.Enemies(characterBody, characterBody.corePosition, row.Radius))
                {
                    var vb = hb.healthComponent.body;
                    var p = vb ? vb.corePosition : hb.transform.position;
                    Fx.Hit(characterBody, hb, row.DamagePct / 100f, row.ProcCoefficient, p);
                    Relay.Vfx(row.Vfx, "zap", p, 1f, null, characterBody.corePosition, 0f);
                }
            VictorPain.HurtSelf(characterBody, row.SelfDamageFlat);
            Buffs.Clear(characterBody, row.Buff);
            Buffs.AddTimed(characterBody, row.Buff, Sheet.Buff(row.Buff)?.Duration ?? 5f);
            var hero = Sheet.Hero(Survivors.HeroOf(characterBody));
            if (hero != null && hero.PassiveBuff != "none") VictorPain.Add(characterBody, hero.PassiveBuff, (int)Mathf.Max(1, row.BuffStacks));
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row != null && !released)
            {
                if (fixedAge < row.MinDuration)
                {
                    // the jolt pose shows for a moment, then he is held in it
                    if (!paused && anim && fixedAge >= 0.06f) { anim.enabled = false; paused = true; }
                    if (isAuthority && characterMotor) characterMotor.velocity = Vector3.zero;
                }
                else Release();
            }
            if (isAuthority && (row == null || fixedAge >= row.Duration)) outer.SetNextStateToMain();
        }

        void Release()
        {
            released = true;
            if (paused && anim) anim.enabled = true;
            paused = false;
        }

        public override void OnExit() { Release(); base.OnExit(); }

        /// <summary>A white flash over the whole model for seconds, fading out (every machine).</summary>
        static void WhiteFlash(CharacterBody b, float seconds)
        {
            var model = b && b.modelLocator && b.modelLocator.modelTransform ? b.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            if (!model || seconds <= 0f) return;
            var o = TemporaryOverlayManager.AddOverlay(model.gameObject);
            o.duration = seconds;
            o.animateShaderAlpha = true;
            o.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            o.destroyComponentOnEnd = true;
            o.originalMaterial = Vfx.Mat("mat_flash_white", Color.white);
            o.AddToCharacterModel(model);
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------------------------------------------------------- M1: Memento Mori
    /// <summary>Hold Left Click: the gun spins up over duration seconds, its rate rising evenly from 1/interval to 1/minDuration
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
    /// <summary>Spends buffStacks of the buff (1 Pain) on an instant bolt along the aim, up to range: the first enemy or surface it
    /// meets, and everything within radius of that point, takes damagePct (falling off from falloffStart to falloffMinPct % at
    /// falloffEnd), is knocked back away from the impact and gets the debuff (a 40% slow). With too few stacks nothing happens and
    /// the charge stays. Never hurts Victor.</summary>
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
            Vfx.Play(row.Vfx, target ? "impact" : "hit", end, row.Radius);
            MeltyTest.Report("pain-battery", true, characterBody.baseNameToken);
            if (!Server) return;
            VictorPain.RemoveOldest(characterBody, row.Buff, (int)row.BuffStacks);
            // falloff: full damage to falloffStart, down to falloffMinPct % at falloffEnd (= range, so it never travels for ever)
            float coef = Coef * DlrGoo.Falloff(row, Vector3.Distance(aim.origin, end));
            var struck = new HashSet<HealthComponent>();
            if (target && target.healthComponent && target.healthComponent.alive)
            {
                struck.Add(target.healthComponent);
                Fx.Hit(characterBody, target, coef, row.ProcCoefficient, end);
                Buffs.Apply(characterBody, target.healthComponent.body, row.Debuff);
                Push(target.healthComponent, end - aim.direction);
            }
            foreach (var hb in Fx.Enemies(characterBody, end, row.Radius))
            {
                if (!struck.Add(hb.healthComponent)) continue;
                Fx.Hit(characterBody, hb, coef, row.ProcCoefficient, hb.healthComponent.body ? hb.healthComponent.body.corePosition : end);
                if (hb.healthComponent.body) Buffs.Apply(characterBody, hb.healthComponent.body, row.Debuff);
                Push(hb.healthComponent, end);
            }
        }

        /// <summary>Knocks a struck enemy away from the impact point (bosses not).</summary>
        void Push(HealthComponent hc, Vector3 from)
        {
            var b = hc ? hc.body : null;
            if (row.Knockback <= 0f || !b || b.isChampion || !hc.alive) return;
            var dir = b.corePosition - from; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = characterBody.transform.forward;
            Fx.KnockBack(characterBody, hc, dir.normalized + Vector3.up * 0.35f, row.Knockback);
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
    /// component, so it stays on while Victor shoots and jolts himself.</summary>
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

    /// <summary>On Victor's body, every machine: the barbed-wire arm crackling once Pain Battery is ready, and the aura while its
    /// buff is on (the field, its loop sound, its radius following Pain). On the server, while on: every interval each enemy within
    /// radius + reach x Pain gets the debuff (victor_suffering: +33 % damage taken from him) and, if damagePct is above 0, a tick
    /// ramping from damageMinPct to damagePct a second over duration with healFraction of it healed (both 0 since 2026-11-09:
    /// the creator's "amplify zone only"); selfDrainPct of his max health plus selfDrainRegenMult x his own health regen (items and level included; creator, 2026-11-10: 2x at first, then "it says victor should take 4x the regen rate when aura is active!") a second, which can kill him; +buffStacks Pain every
    /// buffEvery seconds; off at stopBelowPct health only if that is above 0 (0 now: it stays on until R).</summary>
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

        /// <summary>Pain Battery's cost (the wire crackles from it).</summary>
        int Ready
        {
            get
            {
                if (ready > 0) return ready;
                var hero = Survivors.HeroOf(body);
                var bat = hero != null ? Sheet.SkillsOf(hero).FirstOrDefault(r => r.Kind == "pain_battery") : null;
                return ready = bat != null ? Mathf.Max(1, (int)bat.BuffStacks) : 3;
            }
        }
        int ready;

        void SelfHurt(float fraction, bool lethal = false)
        {
            if (body && body.healthComponent) VictorPain.HurtSelf(body, body.healthComponent.fullCombinedHealth * fraction, lethal);
        }

        void Update()
        {
            if (!body) return;
            int pain = Buffs.Count(body, "pain");
            // the barbed wire crackles from 3 Pain: Pain Battery is ready
            if (pain >= Ready && !wire && Arm()) wire = Vfx.Play("victor_wire", "loop", arm.position, 1f, arm);
            else if (pain < Ready && wire) { Vfx.Stop(wire, 0.3f); wire = null; }
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
            // off at stopBelowPct health, if set (0 since 2026-11-09: it never switches itself off, and the drain can kill him)
            if (row.StopBelowPct > 0f && hc.combinedHealthFraction <= row.StopBelowPct / 100f)
            {
                Buffs.Clear(body, Buff);
                Relay.Sound(row.Id, "end", body.corePosition);
                return;
            }
            if (now >= nextDrain)
            {
                nextDrain = now + 1f;
                float drain = hc.fullCombinedHealth * row.SelfDrainPct / 100f + row.SelfDrainRegenMult * Mathf.Max(0f, body.regen);
                if (drain > 0f) VictorPain.HurtSelf(body, drain, row.StopBelowPct <= 0f);
            }
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
                // inside the field: +33 % damage taken from him (creator, 2026-11-09: "every enemy within in pain aura circle take a
                // flat 33% extra damage"); the debuff lasts a moment past each tick, so it ends soon after they leave
                if (vb && row.Debuff != "none") Buffs.Apply(body, vb, row.Debuff);
                if (coef > 0f)
                {
                    float before = victim.combinedHealth;
                    Fx.Hit(body, hb, coef, row.TickProc, vb ? vb.corePosition : hb.transform.position);
                    healed += Mathf.Max(0f, before - victim.combinedHealth) * row.HealFraction;
                }
                // a flying enemy (the strikes only run along the floor) gets one bolt from Victor every tick that hurts it (creator,
                // 2026-11-05: "if there is a flying enemy in the aoe circle shoot 1 lightning bolt to them every time they are damaged");
                // others a thin red thread to Victor now and then (every player sees both)
                if (vb && vb.isFlying) Relay.Vfx(row.Vfx, "zap", vb.corePosition, 1f, null, body.corePosition + Vector3.up * 0.5f);
                else if (vb && Random.value < 0.35f) Relay.Vfx(row.Vfx, row.HealFraction > 0f ? "hit" : "mark", vb.corePosition, 1f, null, body.corePosition);
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
            // Victor rides his zipline clip up (ult_loop); anyone else stands in their idle (creator, 2026-11-10: "it stay on what
            // it was last on make sure it is idle for all other characters!"): a Deadlock hero loops its idle clip over the whole
            // body; a RoR2 survivor is put in its Body layer's Idle with the gesture layers emptied, then held there (its own
            // controller would otherwise switch to falling as it rises)
            if (anim && anim.HasClip("ult_loop")) anim.Override("ult_loop", row.MinDuration, true);
            else if (anim && anim.HasClip("idle")) anim.Override("idle", row.MinDuration, true);
            else { vanilla = GetModelAnimator(); if (vanilla) { Idle(vanilla); vanilla.speed = 0f; frozen = true; } }
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

        /// <summary>A RoR2 survivor's animator straight into its idle: Body layer Idle, the gesture layers on their empty states.</summary>
        static void Idle(Animator a)
        {
            for (int i = 0; i < a.layerCount; i++)
            {
                var name = a.GetLayerName(i);
                foreach (var st in name == "Body" ? new[] { "Idle", "IdleIn" } : new[] { "Empty", "BufferEmpty" })
                {
                    int h = Animator.StringToHash(st);
                    if (!a.HasState(i, h)) continue;
                    a.Play(h, i, 0f);
                    break;
                }
            }
            a.Update(0f);
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
