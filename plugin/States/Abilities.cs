using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ---------------- Haze: Sleep Dagger ----------------
    [Kind("projectile")]
    public sealed class ThrowProjectile : DlrState
    {
        bool thrown;
        public override void OnEnter()
        {
            base.OnEnter();
            dur = row.Duration / attackSpeedStat;
            if (anim) anim.Gesture("secondary", 1f);
            Sound("cast");
            characterBody.SetAimTimer(2f);
            SlowWhileActive(row.MoveSpeedMult < 1 ? row.MoveSpeedMult : 1f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!thrown && fixedAge >= dur * 0.4f)
            {
                thrown = true;
                if (Server) DlrProjectile.Launch(characterBody, row, MuzzlePos(), GetAimRay().direction);
            }
            if (fixedAge >= dur && isAuthority) outer.SetNextStateToMain();
        }
    }

    // ---------------- buffs on self: Smoke Bomb, Fixation, Power Surge, Afterburn ----------------
    [Kind("self_buff")]
    public sealed class SelfBuff : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (row.Hero == "seven") { if (anim) anim.Gesture("special"); }
            else if (row.Slot == "utility") { if (anim) anim.Gesture("utility"); }
            else if (anim) anim.Gesture("special");
            if (Server)
            {
                Buffs.Clear(characterBody, row.Buff);
                Buffs.AddTimed(characterBody, row.Buff, row.Duration);
                if (row.Buff == "smoke") characterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, row.Duration);
            }
            if (row.Effect != "none") Fx.Impact(row.Effect, transform.position);
            if (row.Buff == "smoke" && rt != null) rt.SmokeUntil = Time.time + row.Duration;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.2f && isAuthority) outer.SetNextStateToMain();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;
    }

    // ---------------- Abrams: Siphon Life ----------------
    [Kind("aura_drain")]
    public sealed class AuraDrain : DlrState
    {
        float nextTick; int loop;
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            if (anim) anim.Gesture("secondary");
            if (Server) Buffs.AddTimed(characterBody, row.Buff, row.Duration);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= nextTick)
            {
                nextTick += row.Interval;
                if (Server)
                {
                    float healed = 0;
                    foreach (var hb in Fx.Enemies(characterBody, transform.position, row.Radius))
                    {
                        Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, hb.transform.position);
                        healed += damageStat * Coef * row.HealFraction;
                        Fx.Impact(row.Effect, hb.transform.position);
                    }
                    if (healed > 0) healthComponent.Heal(healed, default);
                }
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Sound("end");
            base.OnExit();
        }
    }

    // ---------------- Abrams: Shoulder Charge ----------------
    [Kind("dash_charge")]
    public sealed class DashCharge : DlrState
    {
        Vector3 dir;
        readonly HashSet<HealthComponent> hit = new HashSet<HealthComponent>();
        readonly List<CharacterBody> carried = new List<CharacterBody>();
        Vector3 lastPos;
        float stuckTime;
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            dir = GetAimRay().direction; dir.y = 0; dir.Normalize();
            if (characterDirection) characterDirection.forward = dir;
            if (anim) anim.Override("utility_loop", row.Duration, true);
            lastPos = transform.position;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                var input = inputBank ? inputBank.moveVector : Vector3.zero;
                if (input.sqrMagnitude > 0.1f) dir = Vector3.RotateTowards(dir, new Vector3(input.x, 0, input.z).normalized, 2.4f * Time.fixedDeltaTime, 0f);
                characterMotor.moveDirection = Vector3.zero;
                characterMotor.rootMotion += dir * row.Speed * Time.fixedDeltaTime;
                if (characterDirection) characterDirection.forward = dir;
            }
            if (Server)
            {
                foreach (var hb in Fx.Enemies(characterBody, transform.position + dir * 1.2f, row.Radius))
                {
                    if (!hit.Add(hb.healthComponent)) continue;
                    Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, hb.transform.position);
                    Fx.Impact(row.Effect, hb.transform.position);
                    Fx.Sound(row.Id, "hit", hb.transform.position);
                    if (hb.healthComponent.body) carried.Add(hb.healthComponent.body);
                }
                foreach (var c in carried)
                    if (c && c.characterMotor) c.characterMotor.rootMotion += dir * row.Speed * Time.fixedDeltaTime;
                    else if (c && c.rigidbody) c.rigidbody.MovePosition(c.transform.position + dir * row.Speed * Time.fixedDeltaTime);
            }
            // wall slam: we stopped moving although we push forward
            float moved = Vector3.Distance(transform.position, lastPos);
            lastPos = transform.position;
            if (fixedAge > 0.25f && moved < row.Speed * Time.fixedDeltaTime * 0.15f) stuckTime += Time.fixedDeltaTime; else stuckTime = 0;
            if (stuckTime > 0.08f)
            {
                if (Server) foreach (var c in carried) if (c) Fx.Stun(c.healthComponent, row.StunDuration);
                Sound("end");
                Fx.Impact(row.Effect, transform.position + dir);
                if (isAuthority) outer.SetNextStateToMain();
                return;
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            if (anim) anim.StopOverride();
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    // ---------------- Abrams: Infernal Resilience ----------------
    [Kind("self_heal")]
    public sealed class SelfHeal : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            Fx.Impact(row.Effect, transform.position);
            if (Server)
            {
                float taken = 0;
                if (Buffs.DamageTaken.TryGetValue(characterBody, out var list))
                    foreach (var kv in list) if (Time.time - kv.Key <= row.Interval) taken += kv.Value;
                Buffs.AddTimed(characterBody, row.Buff, row.Duration);
                if (rt != null) rt.QueueHeal(taken * row.HealFraction, row.Duration);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.15f && isAuthority) outer.SetNextStateToMain();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;
    }

    // ---------------- Seven: Lightning Ball ----------------
    [Kind("projectile_orb")]
    public sealed class ThrowOrb : DlrState
    {
        bool thrown;
        public override void OnEnter()
        {
            base.OnEnter();
            dur = 0.35f / attackSpeedStat;
            if (anim) anim.Gesture("secondary");
            Sound("cast");
            characterBody.SetAimTimer(2f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!thrown && fixedAge >= dur * 0.5f)
            {
                thrown = true;
                if (Server) DlrProjectile.Launch(characterBody, row, MuzzlePos(), GetAimRay().direction);
            }
            if (fixedAge >= dur && isAuthority) outer.SetNextStateToMain();
        }
    }

    // ---------------- Seven: Static Charge ----------------
    [Kind("delayed_blast")]
    public sealed class DelayedBlast : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            if (anim) anim.Gesture("utility");
            Sound("cast");
            characterBody.SetAimTimer(2f);
            if (Server)
            {
                var aim = GetAimRay();
                Transform follow = null; Vector3 point = aim.origin + aim.direction * row.Range;
                if (Physics.SphereCast(aim, 0.5f, out var hit, row.Range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                {
                    point = hit.point;
                    var hb = hit.collider.GetComponent<HurtBox>();
                    if (hb && hb.healthComponent && hb.healthComponent.body && hb.healthComponent.body.teamComponent.teamIndex != teamComponent.teamIndex)
                        follow = hb.healthComponent.body.coreTransform ? hb.healthComponent.body.coreTransform : hb.healthComponent.transform;
                }
                DlrDelayed.Plant(characterBody, row, point, follow);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.25f && isAuthority) outer.SetNextStateToMain();
        }
    }

    // ---------------- Infernus: Catalyst ----------------
    [Kind("cone")]
    public sealed class Cone : DlrState
    {
        bool fired;
        public override void OnEnter()
        {
            base.OnEnter();
            dur = row.Duration / attackSpeedStat;
            if (anim) anim.Gesture("secondary");
            Sound("cast");
            characterBody.SetAimTimer(2f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!fired && fixedAge >= dur * 0.3f)
            {
                fired = true;
                var aim = GetAimRay();
                for (int i = 1; i <= 4; i++) Fx.Impact(row.Effect, aim.origin + aim.direction * row.Range * i / 4.5f);
                if (Server)
                {
                    var cat = Sheet.Buff(row.Buff);
                    foreach (var hb in Fx.Enemies(characterBody, aim.origin, row.Range))
                    {
                        var to = hb.transform.position - aim.origin;
                        if (Vector3.Angle(aim.direction, to) > row.SpreadDeg + Mathf.Rad2Deg * Mathf.Atan2(row.Radius, Mathf.Max(1f, to.magnitude))) continue;
                        Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, hb.transform.position);
                        var v = hb.healthComponent.body;
                        if (v)
                        {
                            Buffs.Clear(v, row.Buff);
                            Buffs.AddTimed(v, row.Buff, cat.Duration);
                            Buffs.CatalystOwner[v] = characterBody;
                        }
                        Fx.Sound(row.Id, "hit", hb.transform.position);
                    }
                }
            }
            if (fixedAge >= dur && isAuthority) outer.SetNextStateToMain();
        }
    }

    // ---------------- Infernus: Flame Dash ----------------
    [Kind("dash_trail")]
    public sealed class DashTrail : DlrState
    {
        DamageTrail trail; int loop;
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            if (anim) anim.Override("utility_loop", row.Duration, true);
            if (Server)
            {
                var p = Fx.Prefab(row.Effect);
                if (p)
                {
                    var go = Object.Instantiate(p, transform.position, Quaternion.identity);
                    trail = go.GetComponent<DamageTrail>();
                    if (trail)
                    {
                        trail.owner = gameObject;
                        trail.damagePerSecond = damageStat * Coef / row.Interval;
                        trail.radius = row.Radius * 0.5f;
                        trail.pointLifetime = row.ChainCount; // GroundFlameDuration = 4 s (chainCount column)
                    }
                }
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor && inputBank)
            {
                var mv = inputBank.moveVector; if (mv.sqrMagnitude < 0.01f) { mv = GetAimRay().direction; mv.y = 0; }
                mv.Normalize();
                characterMotor.moveDirection = mv;
                characterMotor.rootMotion += mv * moveSpeedStat * (row.MoveSpeedMult - 1f) * Time.fixedDeltaTime;
                if (characterDirection) characterDirection.forward = mv;
            }
            if (trail) trail.transform.position = transform.position;
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            if (trail) { trail.active = false; Object.Destroy(trail.gameObject, row.ChainCount + 0.5f); }
            DeadlockAudio.Stop(loop);
            Sound("end");
            if (anim) anim.StopOverride();
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}
