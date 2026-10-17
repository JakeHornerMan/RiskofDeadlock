using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // The R moves (RoR2's special slot, normal cooldowns). The heroes' Deadlock ultimates are their Q equipment (Ultimates.cs).

    // ---------------- Haze: Phantom Strike (Deadlock item) ----------------
    [Kind("phantom_strike")]
    public sealed class PhantomStrike : DlrState
    {
        HurtBox target; int cuts; float nextCut; bool started;

        public override void OnEnter()
        {
            base.OnEnter();
            target = LockOn(row.Range);
            if (!target)
            {
                // nothing under the crosshair in range: no blink, and R stays ready
                RefundStock();
                return;
            }
            started = true;
            Sound("cast");
            var victim = target.healthComponent.body;
            var from = characterBody.footPosition;
            var toTarget = victim.footPosition - from; toTarget.y = 0;
            if (toTarget.sqrMagnitude < 0.01f) toTarget = characterDirection ? characterDirection.forward : Vector3.forward;
            toTarget.Normalize();
            // just behind the target, facing it
            var behind = victim.footPosition + toTarget * (victim.radius + 1.2f);
            if (Physics.Raycast(behind + Vector3.up * 3f, Vector3.down, out var g, 8f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) behind = g.point;
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, 1f, null, behind + Vector3.up * characterBody.radius * 2f);
            if (isAuthority)
            {
                TeleportHelper.TeleportBody(characterBody, behind, false);
                if (characterMotor) characterMotor.velocity = Vector3.zero;
            }
            if (characterDirection) characterDirection.forward = -toTarget;
            if (anim) anim.Gesture("special_start");
            nextCut = 0.1f;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!started) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (isAuthority && characterMotor) { characterMotor.velocity = Vector3.zero; characterMotor.moveDirection = Vector3.zero; }
            if (cuts < row.Burst && fixedAge >= nextCut)
            {
                cuts++;
                nextCut = fixedAge + row.Interval / attackSpeedStat;
                var victim = target && target.healthComponent && target.healthComponent.alive ? target.healthComponent.body : null;
                if (anim) anim.Gesture("special", 1.6f);
                var at = victim ? victim.corePosition : transform.position + (characterDirection ? characterDirection.forward : transform.forward) * 1.5f;
                Vfx.Play(row.Vfx, "hit", at, cuts);
                Fx.Sound(row.Id, "hit", at);
                if (victim)
                {
                    // each cut sweeps every enemy within radius of the target, not just the target (every player draws
                    // the cuts; the server deals the damage)
                    var swept = Fx.Enemies(characterBody, at, row.Radius);
                    if (!swept.Exists(h => h.healthComponent == target.healthComponent)) swept.Insert(0, target);
                    foreach (var hb in swept)
                    {
                        var b = hb.healthComponent.body;
                        if (!b) continue;
                        if (hb.healthComponent != target.healthComponent) Vfx.Play(row.Vfx, "hit", b.corePosition, cuts);
                        if (!Server) continue;
                        Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition);
                        // each cut adds buffStacks of Haze's Fixation (its 20-stack burst can fire from here)
                        for (int i = 0; i < Mathf.Max(1, (int)row.BuffStacks); i++) Buffs.Stack(characterBody, b, row.Buff, false);
                    }
                }
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Abrams: Haymaker (Deadlock heavy melee) ----------------
    // Locks onto the enemy under the crosshair within range (no target: no punch, and R stays ready), winds up (interval),
    // then flies straight at it at speed m/s, even up to one in the air, and punches along that line, so the fist never
    // lands in the floor. On contact a short hit-stop freezes him, then he recovers for duration seconds.
    [Kind("heavy_melee")]
    public sealed class HeavyMelee : DlrState
    {
        const float HitStop = 0.08f;
        HurtBox target; Vector3 dir, aimPoint; bool started, punched, floating; float windup, rushEnd, punchAt; GameObject glow;

        public override void OnEnter()
        {
            base.OnEnter();
            target = LockOn(row.Range);
            if (!target)
            {
                RefundStock();
                return;
            }
            started = true;
            Sound("cast");
            aimPoint = target.healthComponent.body.corePosition;
            dir = (aimPoint - characterBody.corePosition).normalized;
            Face();
            windup = row.Interval / attackSpeedStat;
            // time to cover the whole lock range, with slack for a target that moves
            rushEnd = windup + 1.5f * row.Range / Mathf.Max(1f, row.Speed);
            if (anim) anim.Gesture("special_start", anim.HasClip("special_start") ? 1.5f : 1f);
            var loc = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>() : null;
            var hand = loc ? loc.FindChild("HandL") : null;
            glow = Vfx.Play(row.Vfx, "cast", hand ? hand.position : characterBody.corePosition, 1f, hand ? hand : transform);
            characterBody.SetAimTimer(2f);
            // no gravity while he winds up and flies at the target
            if (characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
        }

        void Face()
        {
            var flat = dir; flat.y = 0;
            if (flat.sqrMagnitude > 0.01f && characterDirection) characterDirection.forward = flat.normalized;
        }

        void Land()
        {
            if (!floating || !characterMotor) return;
            floating = false;
            var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!started) { if (isAuthority) outer.SetNextStateToMain(); return; }
            var victim = target && target.healthComponent && target.healthComponent.alive ? target.healthComponent.body : null;
            if (!punched)
            {
                if (victim) aimPoint = victim.corePosition;
                var to = aimPoint - characterBody.corePosition;
                if (to.sqrMagnitude > 0.01f) dir = to.normalized;
                Face();
                if (isAuthority && characterMotor)
                {
                    characterMotor.moveDirection = Vector3.zero;
                    if (fixedAge < windup) characterMotor.velocity = Vector3.zero;
                    else
                    {
                        if (characterMotor.isGrounded && dir.y > 0.05f) characterMotor.Motor.ForceUnground();
                        characterMotor.velocity = dir * row.Speed;
                    }
                }
                float reach = characterBody.radius + (victim ? victim.radius : 0f) + 1f;
                if (fixedAge >= windup && (to.magnitude <= reach || fixedAge >= rushEnd)) Punch();
            }
            else
            {
                bool stopped = fixedAge < punchAt + HitStop;
                if (isAuthority && characterMotor)
                {
                    characterMotor.moveDirection = Vector3.zero;
                    if (stopped) characterMotor.velocity = Vector3.zero;
                }
                if (anim) anim.enabled = !stopped;
                if (!stopped) Land();
                if (fixedAge >= punchAt + HitStop + row.Duration && isAuthority) outer.SetNextStateToMain();
            }
        }

        void Punch()
        {
            punched = true;
            punchAt = fixedAge;
            Vfx.Stop(glow, 0.2f);
            if (anim) anim.Gesture("special", 1.4f);
            // the fist lands along the line to the target (up, down or level)
            var at = characterBody.corePosition + dir * (characterBody.radius + 1f + row.Radius * 0.4f);
            Fx.Sound(row.Id, "hit", at);
            Fx.Impact(row.Effect, at, row.Radius, -dir);
            Vfx.Play(row.Vfx, "hit", at, row.Radius, null, at + dir);
            bool wave = row.Extra == "shockwave";
            // the shockwave cone is twice the fist's radius long (range is the lock-on reach)
            float waveLength = row.Radius * 2f;
            if (wave) Vfx.Play(row.Vfx, "wave", at, waveLength, null, at + dir, row.SpreadDeg);
            if (!Server) return;
            var punchedBodies = new HashSet<CharacterBody>();
            void Smash(HurtBox hb)
            {
                var b = hb.healthComponent.body;
                if (!b || !punchedBodies.Add(b)) return;
                Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition);
                Fx.Stun(hb.healthComponent, row.StunDuration);
                Fx.KnockBack(characterBody, hb.healthComponent, dir, row.Knockback);
            }
            // the locked target always takes the fist if it is still in reach
            if (target && target.healthComponent && target.healthComponent.alive && target.healthComponent.body
                && Vector3.Distance(target.healthComponent.body.corePosition, at) <= row.Radius + target.healthComponent.body.radius + 1f) Smash(target);
            foreach (var hb in Fx.Enemies(characterBody, at, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                // only what is in front of him: a punch, not a stomp
                var to = b.corePosition - characterBody.corePosition;
                if (to.sqrMagnitude > 1f && Vector3.Dot(to.normalized, dir) < 0.2f) continue;
                Smash(hb);
            }
            if (!wave) return;
            // the shockwave: every other enemy in a cone along the punch (half-angle spreadDeg) takes the same hit
            float cos = Mathf.Cos(row.SpreadDeg * Mathf.Deg2Rad);
            foreach (var hb in Fx.Enemies(characterBody, at, waveLength))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                var to = b.corePosition - at;
                if (to.sqrMagnitude > 0.25f && Vector3.Dot(to.normalized, dir) < cos) continue;
                Smash(hb);
            }
        }

        public override void OnExit()
        {
            Vfx.Stop(glow, 0.2f);
            Land();
            if (anim) anim.enabled = true;
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Seven: Power Surge (his Deadlock ability, built like RoR2's Unstable Tesla Coil) ----------------
    [Kind("power_surge")]
    public sealed class PowerSurgeCast : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("special");
            if (Server)
            {
                Buffs.Clear(characterBody, row.Buff);
                Buffs.AddTimed(characterBody, row.Buff, row.Duration);
            }
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, 1f, transform);
            TimedLoop.Run(gameObject, row, row.Duration, Vfx.Play(row.Vfx, "loop", characterBody.corePosition, 1f, characterBody.coreTransform ? characterBody.coreTransform : transform));
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.25f && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;

        /// <summary>The surge row whose buff is on the body right now (null when no surge runs).</summary>
        public static SkillRow Active(CharacterBody body)
        {
            if (!body) return null;
            foreach (var r in SkillRow.All) if (r.Kind == "power_surge" && Buffs.Count(body, r.Buff) > 0) return r;
            return null;
        }

        /// <summary>One burst's arcs: RoR2 Tesla lightning from the target to up to chainCount more enemies, each within radius
        /// of the last; each arc deals damagePct and adds buffStacks of the gun's stacking buff (Static).</summary>
        public static void Arc(CharacterBody owner, CharacterBody victim, SkillRow surge, string stackBuff)
        {
            if (!NetworkServer.active || !owner || !victim || surge == null || !OrbManager.instance) return;
            var hit = new HashSet<HealthComponent> { victim.healthComponent };
            var from = victim;
            for (int i = 0; i < surge.ChainCount; i++)
            {
                var next = Fx.Enemies(owner, from.corePosition, surge.Radius).FirstOrDefault(h => !hit.Contains(h.healthComponent));
                if (!next) break;
                hit.Add(next.healthComponent);
                OrbManager.instance.AddOrb(new LightningOrb
                {
                    lightningType = LightningOrb.LightningType.Tesla,
                    origin = from.corePosition,
                    target = next,
                    attacker = owner.gameObject,
                    inflictor = owner.gameObject,
                    teamIndex = owner.teamComponent.teamIndex,
                    damageValue = owner.damage * surge.DamagePct / 100f,
                    isCrit = owner.RollCrit(),
                    procCoefficient = surge.ProcCoefficient,
                    bouncesRemaining = 0,
                    damageColorIndex = DamageColorIndex.Default,
                });
                var nb = next.healthComponent.body;
                if (nb && stackBuff != "none")
                    for (int k = 0; k < Mathf.Max(1, (int)surge.BuffStacks); k++) Buffs.Stack(owner, nb, stackBuff, false);
                if (nb) from = nb;
            }
            // server only: every player hears it through the relay (the arcs themselves are RoR2 orbs, networked by RoR2)
            if (hit.Count > 1) Relay.Sound(surge.Id, "hit", victim.corePosition);
            MeltyTest.Report("surge-arcs", true, owner.baseNameToken);
        }
    }

    // ---------------- Infernus: Torment Pulse (Deadlock item) ----------------
    [Kind("aura_pulse")]
    public sealed class AuraPulse : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("special");
            // on every machine: the rings and sound everywhere, the damage on the server
            var aura = gameObject.GetComponent<PulseAura>() ?? gameObject.AddComponent<PulseAura>();
            aura.Begin(characterBody, row);
            TimedLoop.Run(gameObject, row, row.Duration, Vfx.Play(row.Vfx, "loop", characterBody.footPosition, row.Radius, transform));
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.2f && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;
    }

    /// <summary>Torment Pulse, on every machine: every interval for duration a ring and its sound; on the server everything
    /// within radius takes damagePct and buffStacks of the buff (Afterburn build-up, whose ignition is the usual burn).</summary>
    public sealed class PulseAura : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; float until, next; string hitVfx;

        public void Begin(CharacterBody body, SkillRow r)
        {
            owner = body; row = r;
            until = Time.fixedTime + r.Duration;
            next = Time.fixedTime;
            // the hero's gun draws the build-up (embers, ignition) for the same buff
            var hero = Survivors.HeroOf(body);
            var gun = hero != null ? Sheet.SkillOf(hero, "primary") : null;
            hitVfx = gun != null && gun.Buff == r.Buff ? gun.Vfx : "none";
            enabled = true;
        }

        void FixedUpdate()
        {
            if (!owner || row == null || Time.fixedTime >= until || !owner.healthComponent || !owner.healthComponent.alive) { enabled = false; return; }
            if (Time.fixedTime < next) return;
            next = Time.fixedTime + row.Interval;
            var at = owner.footPosition;
            Vfx.Play(row.Vfx, "hit", at, row.Radius);
            Fx.Sound(row.Id, "hit", at);
            if (!NetworkServer.active) return;
            foreach (var hb in Fx.Enemies(owner, owner.corePosition, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, b.corePosition);
                bool fired = false;
                for (int k = 0; k < Mathf.Max(1, (int)row.BuffStacks); k++) fired |= Buffs.Stack(owner, b, row.Buff, false);
                // the build-up depends on the stacks, which only the server knows: every player sees it through the relay
                Relay.Vfx(hitVfx, fired ? "trigger" : "hit", b.corePosition, 1f, b.gameObject);
            }
        }
    }

    /// <summary>Keeps a skill's loop sound and visual going on the body for a while after its (short) state has ended.</summary>
    public sealed class TimedLoop : MonoBehaviour
    {
        GameObject vfx; int sound; string skill; float until;

        public static void Run(GameObject on, SkillRow row, float duration, GameObject vfx)
        {
            foreach (var old in on.GetComponents<TimedLoop>()) if (old.skill == row.Id) old.Finish();
            var t = on.AddComponent<TimedLoop>();
            t.vfx = vfx; t.skill = row.Id; t.until = Time.time + duration;
            t.sound = Fx.Loop(row.Id, on.transform.position);
        }

        void Update() { if (Time.time >= until) Finish(); }

        void Finish()
        {
            if (!this) return;
            DeadlockAudio.Stop(sound); sound = 0;
            Vfx.Stop(vfx, 1f); vfx = null;
            Fx.Sound(skill, "end", transform.position);
            Destroy(this);
        }

        void OnDestroy() { DeadlockAudio.Stop(sound); Vfx.Stop(vfx, 1f); }
    }
}
