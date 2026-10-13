using System.Collections.Generic;
using EntityStates;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain.States
{
    /// <summary>Base of the ultimates (R): entering empties the charge meter, and ult damage never charges it.</summary>
    public abstract class UltState : DlrState
    {
        protected UltController ult;
        public override void OnEnter()
        {
            base.OnEnter();
            ult = UltController.Of(characterBody);
            // pressing R spent the one stock the full meter gave; the meter starts over
            if (ult) { ult.Consume(); ult.UltDamageInFlight = true; }
            MeltyTest.Report("ult-fires", true, characterBody.baseNameToken);
        }
        public override void OnExit()
        {
            if (ult) ult.UltDamageInFlight = false;
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    // ---------------- Haze: Bullet Dance ----------------
    [Kind("ult_dance")]
    public sealed class UltDance : UltState
    {
        float next; int loop; float startDelay;
        static readonly System.Random rng = new System.Random();
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            startDelay = 0.4f;
            if (anim) { anim.Override("ult_start", startDelay); }
            if (Server) Buffs.AddTimed(characterBody, row.Buff, row.Duration + startDelay);
            SlowWhileActive(row.MoveSpeedMult);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge < startDelay) return;
            if (loop == 0) { loop = Fx.Loop(row.Id, transform.position); if (anim) anim.Override("ult_loop", row.Duration, true); }
            if (fixedAge >= next)
            {
                next = fixedAge + row.Interval / attackSpeedStat;
                var enemies = Fx.Enemies(characterBody, transform.position, row.Radius);
                if (enemies.Count > 0)
                {
                    var target = enemies[rng.Next(enemies.Count)];
                    bool left = rng.Next(2) == 0;
                    var from = MuzzlePos(left);
                    if (isAuthority)
                    {
                        new BulletAttack
                        {
                            owner = gameObject, weapon = gameObject,
                            origin = from, aimVector = (target.transform.position - from).normalized,
                            damage = damageStat * Coef, procCoefficient = row.ProcCoefficient,
                            tracerEffectPrefab = Fx.Prefab(row.Effect), muzzleName = left ? "MuzzleLeft" : "MuzzleRight",
                            isCrit = RollCrit(), radius = 0.4f, smartCollision = true, maxDistance = row.Range * 1.5f,
                            force = 50f, bulletCount = 1, falloffModel = BulletAttack.FalloffModel.None,
                        }.Fire();
                    }
                    Fx.Sound(row.Id, "hit", transform.position);
                }
            }
            if (fixedAge >= row.Duration + startDelay && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Sound("end");
            if (anim) anim.StopOverride();
            base.OnExit();
        }
    }

    // ---------------- Abrams: Seismic Impact ----------------
    [Kind("ult_leap")]
    public sealed class UltLeap : UltState
    {
        Vector3 target; int phase; float phaseStart; int loop;
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            // land on the enemy you aim at, or on the ground you aim at (own and allied hurtboxes are skipped)
            var aim = GetAimRay();
            target = aim.origin + aim.direction * row.Range;
            var hits = Physics.SphereCastAll(aim, 0.5f, row.Range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == teamComponent.teamIndex)) continue;
                target = hb ? hb.healthComponent.body.footPosition : h.point;
                break;
            }
            if (Physics.Raycast(target + Vector3.up * 2f, Vector3.down, out var ground, 200f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) target = ground.point;
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            if (anim) anim.Override("ult_start", row.Duration * 0.6f);
            if (characterMotor) { characterMotor.Motor.ForceUnground(); characterMotor.velocity = Vector3.up * row.Speed * 0.6f; }
            loop = Fx.Loop(row.Id, transform.position);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!characterMotor) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (isAuthority) characterMotor.moveDirection = Vector3.zero;
            if (phase == 0 && fixedAge >= row.Duration * 0.6f) { phase = 1; phaseStart = fixedAge; if (anim) anim.Override("ult_loop", 0.25f, true); }
            if (phase == 0 && isAuthority)
            {
                var flat = target - transform.position; flat.y = 0;
                characterMotor.velocity = new Vector3(flat.x, 0, flat.z) / Mathf.Max(0.3f, row.Duration) + Vector3.up * Mathf.Max(characterMotor.velocity.y, 2f);
            }
            if (phase == 1)
            {
                if (isAuthority) characterMotor.velocity = Vector3.zero;
                if (fixedAge - phaseStart > 0.25f) { phase = 2; Fx.Sound(row.Id, "hit", transform.position); if (anim) anim.Override("ult_end", 0.6f); }
            }
            if (phase == 2)
            {
                if (isAuthority)
                {
                    var to = target - transform.position;
                    characterMotor.velocity = to.normalized * row.Speed;
                }
                if (characterMotor.isGrounded || Vector3.Distance(transform.position, target) < 1.5f || fixedAge > row.Duration + 3f)
                {
                    Slam();
                    if (isAuthority) outer.SetNextStateToMain();
                }
            }
        }
        void Slam()
        {
            phase = 3;
            Fx.Impact(row.Effect, transform.position, row.Radius);
            Fx.Sound(row.Id, "end", transform.position);
            if (Server)
            {
                Fx.Blast(characterBody, transform.position, row.Radius, Coef, row.ProcCoefficient);
                foreach (var hb in Fx.Enemies(characterBody, transform.position, row.Radius)) Fx.Stun(hb.healthComponent, row.StunDuration);
            }
            if (characterMotor) characterMotor.velocity = Vector3.zero;
        }
        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            if (phase != 3 && Server) Slam();
            if (anim) anim.StopOverride();
            base.OnExit();
        }
    }

    // ---------------- Seven: Storm Cloud ----------------
    [Kind("ult_storm")]
    public sealed class UltStorm : UltState
    {
        float next; int loop; float hoverY;
        static readonly System.Random rng = new System.Random();
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            if (anim) anim.Override("ult_start", 0.8f);
            if (Server) Buffs.AddTimed(characterBody, row.Buff, row.Duration);
            if (characterMotor) { characterMotor.Motor.ForceUnground(); characterMotor.velocity = Vector3.up * 14f; }
            hoverY = transform.position.y + 6f;
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge > 0.8f && anim && fixedAge - Time.fixedDeltaTime <= 0.8f) anim.Override("ult_loop", row.Duration, true);
            if (isAuthority && characterMotor)
            {
                var mv = inputBank ? inputBank.moveVector : Vector3.zero;
                var v = mv * moveSpeedStat * row.MoveSpeedMult;
                float dy = hoverY - transform.position.y;
                v.y = fixedAge < 0.5f ? Mathf.Max(characterMotor.velocity.y, 4f) : Mathf.Clamp(dy * 3f, -4f, 4f);
                characterMotor.moveDirection = Vector3.zero;
                characterMotor.velocity = v;
            }
            float radius = Mathf.Lerp(row.Range, row.Radius, Mathf.Clamp01(fixedAge / row.Speed));
            if (fixedAge >= next)
            {
                next = fixedAge + row.Interval;
                if (Server)
                {
                    var center = transform.position; center.y -= 6f;
                    var enemies = Fx.Enemies(characterBody, center, radius);
                    foreach (var hb in enemies) Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, hb.transform.position, default, DamageColorIndex.Default);
                    for (int i = 0; i < Mathf.Min(3, enemies.Count); i++) Fx.Impact(row.Effect, enemies[rng.Next(enemies.Count)].transform.position);
                    if (enemies.Count > 0) Fx.Sound(row.Id, "hit", center);
                }
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Sound("end");
            if (anim) { anim.Override("ult_end", 0.4f); }
            characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            base.OnExit();
        }
    }

    // ---------------- Infernus: Concussive Combustion ----------------
    [Kind("ult_selfbomb")]
    public sealed class UltSelfBomb : UltState
    {
        bool exploded; int loop; float nextPulse;
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            SlowWhileActive(row.MoveSpeedMult);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= nextPulse && !exploded)
            {
                nextPulse = fixedAge + 0.5f;
                Fx.Impact("napalm", transform.position + Vector3.up);
            }
            if (!exploded && fixedAge >= row.Duration)
            {
                exploded = true;
                if (anim) anim.Override("ult_end", 0.8f);
                Fx.Impact(row.Effect, transform.position, row.Radius);
                Sound("end");
                if (Server)
                {
                    Fx.Blast(characterBody, transform.position, row.Radius, Coef, row.ProcCoefficient);
                    foreach (var hb in Fx.Enemies(characterBody, transform.position, row.Radius)) Fx.Stun(hb.healthComponent, row.StunDuration);
                }
            }
            if (fixedAge >= row.Duration + 0.6f && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            base.OnExit();
        }
    }
}
