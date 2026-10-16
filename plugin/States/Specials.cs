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
            target = FindTarget();
            if (!target)
            {
                // nothing under the crosshair in range: no blink, and R stays ready
                if (isAuthority && activatorSkillSlot) activatorSkillSlot.AddOneStock();
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

        HurtBox FindTarget()
        {
            var aim = GetAimRay();
            var search = new BullseyeSearch
            {
                teamMaskFilter = TeamMask.GetEnemyTeams(teamComponent.teamIndex),
                filterByLoS = true,
                searchOrigin = aim.origin,
                searchDirection = aim.direction,
                maxDistanceFilter = row.Range,
                maxAngleFilter = 15f,
                sortMode = BullseyeSearch.SortMode.Angle,
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(gameObject);
            return search.GetResults().FirstOrDefault(h => h && h.healthComponent && h.healthComponent.alive && h.healthComponent.body);
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
                if (Server && victim)
                {
                    Fx.Hit(characterBody, target, Coef, row.ProcCoefficient, at);
                    // each cut adds buffStacks of Haze's Fixation (its 20-stack burst can fire from here)
                    for (int i = 0; i < Mathf.Max(1, (int)row.BuffStacks); i++) Buffs.Stack(characterBody, victim, row.Buff, false);
                }
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Abrams: Haymaker (Deadlock heavy melee) ----------------
    [Kind("heavy_melee")]
    public sealed class HeavyMelee : DlrState
    {
        Vector3 dir; bool punched; float windup, lungeEnd; GameObject glow;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            dir = GetAimRay().direction; dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            if (characterDirection) characterDirection.forward = dir;
            windup = row.Interval / attackSpeedStat;
            lungeEnd = windup + row.Range / Mathf.Max(1f, row.Speed);
            if (anim) anim.Gesture("special_start", anim.HasClip("special_start") ? 1.5f : 1f);
            var loc = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>() : null;
            var hand = loc ? loc.FindChild("HandL") : null;
            glow = Vfx.Play(row.Vfx, "cast", hand ? hand.position : characterBody.corePosition, 1f, hand ? hand : transform);
            characterBody.SetAimTimer(2f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterMotor.moveDirection = Vector3.zero;
                if (!punched && fixedAge >= windup && fixedAge < lungeEnd) characterMotor.rootMotion += dir * row.Speed * Time.fixedDeltaTime;
                if (characterDirection) characterDirection.forward = dir;
            }
            if (!punched && fixedAge >= windup)
            {
                // the fist lands on the first enemy in reach, or at the end of the lunge
                var front = characterBody.corePosition + dir * (characterBody.radius + 1f);
                bool contact = Fx.Enemies(characterBody, front, row.Radius * 0.6f, 1).Count > 0;
                if (contact || fixedAge >= lungeEnd) Punch(front);
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }

        void Punch(Vector3 front)
        {
            punched = true;
            Vfx.Stop(glow, 0.2f);
            if (anim) anim.Gesture("special", 1.4f);
            var at = front + dir * row.Radius * 0.4f;
            Fx.Sound(row.Id, "hit", at);
            Fx.Impact(row.Effect, at);
            Vfx.Play(row.Vfx, "hit", at, row.Radius, null, at + dir);
            bool wave = row.Extra == "shockwave";
            if (wave) Vfx.Play(row.Vfx, "wave", at, row.Range, null, at + dir, row.SpreadDeg);
            if (!Server) return;
            var punchedBodies = new HashSet<CharacterBody>();
            foreach (var hb in Fx.Enemies(characterBody, at, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                // only what is in front of him: a punch, not a stomp
                var to = b.corePosition - characterBody.corePosition; to.y = 0;
                if (to.sqrMagnitude > 1f && Vector3.Dot(to.normalized, dir) < 0.2f) continue;
                punchedBodies.Add(b);
                Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition);
                Fx.KnockBack(characterBody, hb.healthComponent, dir, row.Knockback);
            }
            if (!wave) return;
            // the shockwave: every other enemy in a cone in front (length range, half-angle spreadDeg) takes the same hit
            float cos = Mathf.Cos(row.SpreadDeg * Mathf.Deg2Rad);
            foreach (var hb in Fx.Enemies(characterBody, at, row.Range))
            {
                var b = hb.healthComponent.body;
                if (!b || punchedBodies.Contains(b)) continue;
                var to = b.corePosition - at; to.y = 0;
                if (to.sqrMagnitude > 0.25f && Vector3.Dot(to.normalized, dir) < cos) continue;
                punchedBodies.Add(b);
                Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition);
                Fx.KnockBack(characterBody, hb.healthComponent, dir, row.Knockback);
            }
        }

        public override void OnExit()
        {
            Vfx.Stop(glow, 0.2f);
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
            if (hit.Count > 1) Fx.Sound(surge.Id, "hit", victim.corePosition);
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
            if (Server)
            {
                var aura = gameObject.GetComponent<PulseAura>() ?? gameObject.AddComponent<PulseAura>();
                aura.Begin(characterBody, row);
            }
            TimedLoop.Run(gameObject, row, row.Duration, Vfx.Play(row.Vfx, "loop", characterBody.footPosition, row.Radius, transform));
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.2f && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;
    }

    /// <summary>Server side of Torment Pulse: every interval for duration, everything within radius takes damagePct and
    /// buffStacks of the buff (Afterburn build-up, whose ignition is the usual burn).</summary>
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
            foreach (var hb in Fx.Enemies(owner, owner.corePosition, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, b.corePosition);
                bool fired = false;
                for (int k = 0; k < Mathf.Max(1, (int)row.BuffStacks); k++) fired |= Buffs.Stack(owner, b, row.Buff, false);
                Vfx.Play(hitVfx, fired ? "trigger" : "hit", b.corePosition, 1f, b.transform);
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
