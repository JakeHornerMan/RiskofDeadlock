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
        float next; int loop; float startDelay; GameObject vfx;
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
            if (loop == 0)
            {
                loop = Fx.Loop(row.Id, transform.position);
                if (anim) anim.Override("ult_loop", row.Duration, true);
                vfx = Vfx.Play(row.Vfx, "loop", transform.position, row.Radius, transform);
            }
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
                    Vfx.Play(row.Vfx, "cast", from);
                    Vfx.Play(row.Vfx, "hit", target.transform.position);
                    Fx.Sound(row.Id, "hit", transform.position);
                }
            }
            if (fixedAge >= row.Duration + startDelay && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Sound("end");
            Vfx.Stop(vfx, 0.5f);
            Vfx.Play(row.Vfx, "end", transform.position);
            if (anim) anim.StopOverride();
            base.OnExit();
        }
    }

    // ---------------- Abrams: Seismic Impact ----------------
    [Kind("ult_leap")]
    public sealed class UltLeap : UltState
    {
        // rise for interval seconds, hover while a reticle on the ground follows the aim (at least minDuration, at most
        // duration), then dive at speed onto the reticle and slam
        enum Phase { Rise, Hover, Dive, Done }
        Phase phase; float phaseStart; int loop; Vector3 target; GameObject reticle, trail;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            Vfx.Play(row.Vfx, "cast", characterBody.footPosition, row.Radius);
            if (anim) anim.Override("ult_start", row.Interval);
            if (characterMotor) { characterMotor.Motor.ForceUnground(); characterMotor.velocity = Vector3.up * row.Speed * 0.6f; }
            loop = Fx.Loop(row.Id, transform.position);
            target = characterBody.footPosition;
        }

        /// <summary>Where the slam lands: the enemy or ground under the crosshair, at most range away (flat distance).</summary>
        Vector3 AimPoint()
        {
            var aim = GetAimRay();
            var point = aim.origin + aim.direction * row.Range;
            var hits = Physics.SphereCastAll(aim, 0.5f, row.Range * 2f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == teamComponent.teamIndex)) continue;
                point = hb ? hb.healthComponent.body.footPosition : h.point;
                break;
            }
            var flat = point - transform.position; flat.y = 0;
            if (flat.magnitude > row.Range) point = transform.position + flat.normalized * row.Range;
            if (Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out var ground, 300f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) point = ground.point;
            return point;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!characterMotor) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (isAuthority) characterMotor.moveDirection = Vector3.zero;
            float t = fixedAge - phaseStart;
            switch (phase)
            {
                case Phase.Rise:
                    if (isAuthority) characterMotor.velocity = Vector3.up * (row.Speed * 0.6f * Mathf.Clamp01(1f - t / row.Interval) + 2f);
                    if (t >= row.Interval)
                    {
                        phase = Phase.Hover; phaseStart = fixedAge;
                        if (anim) anim.Override("ult_loop", row.Duration, true);
                        target = AimPoint();
                        reticle = Vfx.Play(row.Vfx, "aim", target, row.Radius);
                    }
                    break;
                case Phase.Hover:
                {
                    // hang in the air (a slow bob) while the reticle follows the crosshair; Abrams can still be hit
                    if (isAuthority) characterMotor.velocity = Vector3.up * Mathf.Sin(t * 3f) * 0.6f;
                    target = AimPoint();
                    if (reticle) reticle.transform.position = target;
                    bool pressed = inputBank && (inputBank.skill4.justPressed || inputBank.skill1.justPressed);
                    // test mode has no player to press: it slams shortly after the reticle is up
                    bool test = MeltyTest.Active && t >= row.MinDuration + 0.5f;
                    if (t >= row.Duration || t >= row.MinDuration && (pressed || test))
                    {
                        phase = Phase.Dive; phaseStart = fixedAge;
                        Fx.Sound(row.Id, "hit", transform.position);
                        if (anim) anim.Override("ult_end", 0.6f);
                        trail = Vfx.Play(row.Vfx, "loop", transform.position, row.Radius, transform);
                    }
                    break;
                }
                case Phase.Dive:
                    if (isAuthority) characterMotor.velocity = (target - transform.position).normalized * row.Speed;
                    if (reticle) reticle.transform.position = target;
                    if (characterMotor.isGrounded && t > 0.1f || Vector3.Distance(transform.position, target) < 1.5f || t > 3f)
                    {
                        Slam();
                        if (isAuthority) outer.SetNextStateToMain();
                    }
                    break;
            }
        }

        void Slam()
        {
            phase = Phase.Done;
            var at = characterBody.footPosition;
            Fx.Impact(row.Effect, at, row.Radius);
            Vfx.Play(row.Vfx, "hit", at, row.Radius);
            Fx.Sound(row.Id, "end", at);
            if (Server)
            {
                Fx.Blast(characterBody, at, row.Radius, Coef, row.ProcCoefficient);
                foreach (var hb in Fx.Enemies(characterBody, at, row.Radius)) Fx.Stun(hb.healthComponent, row.StunDuration);
            }
            if (characterMotor) characterMotor.velocity = Vector3.zero;
        }

        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Vfx.Stop(reticle, 0.1f);
            Vfx.Stop(trail, 0.4f);
            characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            if (phase != Phase.Done && Server) Slam();
            if (anim) anim.StopOverride();
            base.OnExit();
        }
    }

    // ---------------- Seven: Thunderhead ----------------
    [Kind("ult_zone")]
    public sealed class UltZone : UltState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Override("ult_start", 0.6f);
            if (Server)
            {
                Buffs.AddTimed(characterBody, row.Buff, row.Duration);
                DlrZone.Plant(characterBody, row, characterBody.footPosition);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.6f && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            if (anim) anim.StopOverride();
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
                    // each kill by the blast heals healFraction of max health (deaths are reported while the blast lands)
                    int kills = 0;
                    System.Action<DamageReport> onDeath = r => { if (r != null && r.attackerBody == characterBody) kills++; };
                    GlobalEventManager.onCharacterDeathGlobal += onDeath;
                    try { Fx.Blast(characterBody, transform.position, row.Radius, Coef, row.ProcCoefficient); }
                    finally { GlobalEventManager.onCharacterDeathGlobal -= onDeath; }
                    foreach (var hb in Fx.Enemies(characterBody, transform.position, row.Radius)) Fx.Stun(hb.healthComponent, row.StunDuration);
                    if (kills > 0 && row.HealFraction > 0) healthComponent.HealFraction(row.HealFraction * kills, default);
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
