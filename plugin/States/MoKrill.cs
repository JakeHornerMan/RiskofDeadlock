using System.Collections.Generic;
using EntityStates;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain.States
{
    // ---------------- Mo & Krill: Burrow (Shift) ----------------
    // Mo digs in: the model is hidden (he can still be hit, as in Deadlock) and the row's buff makes him faster and
    // armoured with no gun; a dirt mound follows him. Each hit he takes costs 1 m/s for 1.5 s. He pops out at duration, on
    // Shift or Jump after minDuration, or when he walks off a ledge (BurrowPop, a state of its own so every player sees it);
    // R underground bursts straight into Dust Devil.
    [Kind("burrow")]
    public sealed class Burrow : DlrState
    {
        GameObject mound; int loop; bool hidden; float lastHealth, airborne;
        readonly List<float> hitsTaken = new List<float>();
        CharacterModel model;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            Vfx.Play(row.Vfx, "cast", characterBody.footPosition, row.Radius);
            if (anim) anim.Gesture("utility");
            if (Server) Buffs.AddTimed(characterBody, row.Buff, row.Duration + 1f);
            model = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            lastHealth = healthComponent ? healthComponent.health : 0f;
        }

        void Hide()
        {
            if (hidden) return;
            hidden = true;
            if (model) model.invisibilityCount++;
            loop = Fx.Loop(row.Id, transform.position);
            mound = Vfx.Play(row.Vfx, "loop", characterBody.footPosition, row.Radius, transform);
        }

        void Show()
        {
            if (!hidden) return;
            hidden = false;
            if (model) model.invisibilityCount--;
            DeadlockAudio.Stop(loop); loop = 0;
            Vfx.Stop(mound, 0.8f); mound = null;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            // the dig-in clip shows for a moment, then he is under the ground
            if (!hidden && fixedAge >= 0.25f) Hide();
            if (!isAuthority || !characterMotor) return;
            bool bot = MeltyTest.Active || DpsBench.Active;
            // sprint adds nothing underground: the buff already doubles his speed
            characterBody.isSprinting = false;
            var mv = inputBank ? inputBank.moveVector : Vector3.zero;
            // test mode and the bench have no player to steer: dig toward the aim
            if (bot) { mv = GetAimRay().direction; mv.y = 0; mv.Normalize(); }
            // each hit taken costs 1 m/s (of the buffed speed) for 1.5 s, like Deadlock's damage slowing him underground
            if (healthComponent && healthComponent.health < lastHealth - 0.5f) hitsTaken.Add(fixedAge);
            lastHealth = healthComponent ? healthComponent.health : 0f;
            hitsTaken.RemoveAll(t => fixedAge - t > 1.5f);
            float speed = Mathf.Max(1f, moveSpeedStat);
            characterMotor.moveDirection = mv * Mathf.Clamp01((speed - hitsTaken.Count) / speed);
            if (characterDirection && mv.sqrMagnitude > 0.01f) characterDirection.forward = mv;
            airborne = characterMotor.isGrounded ? 0f : airborne + Time.fixedDeltaTime;

            // R underground: straight into Dust Devil (the pop-out becomes the tornado)
            var r = skillLocator ? skillLocator.special : null;
            var rRow = Survivors.RowFor(r);
            if (!bot && fixedAge > 0.25f && inputBank && inputBank.skill4.justPressed && r && rRow != null && rRow.Kind == "dust_devil" && r.stock > 0)
            {
                r.DeductStock(1);
                outer.SetNextState(new DustDevil { activatorSkillSlot = r, FromBurrow = true });
                return;
            }
            bool early = fixedAge >= row.MinDuration && inputBank && (inputBank.skill3.justPressed || inputBank.jump.justPressed);
            if (fixedAge >= row.Duration || early || airborne > 0.15f && fixedAge > 0.3f || bot && fixedAge >= 1.2f)
                outer.SetNextState(new BurrowPop { RowOverride = row });
        }

        public override void OnExit()
        {
            Show();
            if (Server) Buffs.Clear(characterBody, row.Buff);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    /// <summary>Burrow, part 2: Mo bursts out of the ground in a spin: burst hits of damagePct every interval within radius,
    /// each enemy slowed (the row's debuff) and, on the first hit, knocked up (bosses only if bossMult > 0).</summary>
    public sealed class BurrowPop : DlrState
    {
        int hits; float next;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("hit");
            Vfx.Play(row.Vfx, "hit", characterBody.footPosition, row.Radius);
            if (anim) anim.Override("special", row.Interval * row.Burst + 0.2f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (hits < row.Burst && fixedAge >= next)
            {
                hits++;
                next = fixedAge + row.Interval;
                if (Server)
                    foreach (var hb in Fx.Enemies(characterBody, characterBody.corePosition, row.Radius))
                    {
                        var b = hb.healthComponent.body;
                        if (!b) continue;
                        Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition);
                        if (row.Debuff != "none") Buffs.Apply(characterBody, b, row.Debuff);
                        if (hits == 1 && row.Knockback > 0 && (!b.isChampion || row.BossMult > 0))
                        {
                            float mass = b.characterMotor ? b.characterMotor.mass : b.rigidbody ? b.rigidbody.mass : 100f;
                            hb.healthComponent.TakeDamageForce(Vector3.up * row.Knockback * (b.isChampion ? row.BossMult : 1f) * mass, true, true);
                        }
                    }
            }
            if (isAuthority && fixedAge >= row.Interval * row.Burst + 0.1f) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            Sound("end");
            if (anim) anim.StopOverride();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Mo & Krill: Sand Blast (M2) ----------------
    /// <summary>A wave of sand along the ground (DlrSandWave) in Deadlock's cone: radius wide at Mo, widening by spreadDeg
    /// either side, out to range at speed. On the Weapon machine, so it works while Mo is burrowed or spinning.</summary>
    [Kind("cone_wave")]
    public sealed class ConeWave : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("secondary");
            characterBody.SetAimTimer(2f);
            var dir = ThrowDir; dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = characterDirection ? characterDirection.forward : transform.forward;
            dir.Normalize();
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition + dir * characterBody.radius, row.Radius, null, characterBody.corePosition + dir * 5f);
            // every machine runs the wave; only the server's deals damage
            DlrSandWave.Launch(characterBody, row, characterBody.footPosition, dir, Server);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= row.Duration) outer.SetNextStateToMain();
        }
    }

    // ---------------- Mo & Krill: Dust Devil (R) ----------------
    /// <summary>Mo spins as a sand tornado for duration: he keeps walking (no sprint, no jump), everything within range is
    /// pulled toward him (bosses at bossMult), everything within radius takes damagePct every interval. R again stops it;
    /// the cooldown starts when it ends (cooldownOnEnd).</summary>
    [Kind("dust_devil")]
    public sealed class DustDevil : DlrState
    {
        /// <summary>Started from Burrow (R underground): the pop-out turns straight into the tornado.</summary>
        public bool FromBurrow;
        GameObject fx; int loop; float nextTick;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            if (FromBurrow) Vfx.Play(Sheet.SkillOf(row.Hero, "utility").Vfx, "hit", characterBody.footPosition, row.Radius);
            Vfx.Play(row.Vfx, "cast", characterBody.footPosition, row.Radius);
            fx = Vfx.Play(row.Vfx, "loop", characterBody.footPosition, row.Radius, transform, null, row.Range);
            if (anim) anim.Override("special_loop", row.Duration, true);
            if (Server) Buffs.AddTimed(characterBody, row.Buff, row.Duration + 0.5f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterBody.isSprinting = false;
                characterMotor.moveDirection = inputBank ? inputBank.moveVector : Vector3.zero;
                var face = GetAimRay().direction; face.y = 0;
                if (characterDirection && face.sqrMagnitude > 0.01f) characterDirection.forward = face.normalized;
            }
            if (Server) Fx.Pull(characterBody, characterBody.corePosition, row.Radius * 0.5f, row.Range, row.Speed, row.BossMult);
            if (fixedAge >= nextTick)
            {
                nextTick = fixedAge + row.Interval;
                int shown = 0;
                foreach (var hb in Fx.Enemies(characterBody, characterBody.corePosition, row.Radius))
                {
                    var b = hb.healthComponent.body;
                    var at = b ? b.corePosition : hb.transform.position;
                    if (shown++ < 6) Vfx.Play(row.Vfx, "hit", at);
                    if (Server) Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, at);
                }
            }
            if (!isAuthority) return;
            bool bot = MeltyTest.Active || DpsBench.Active;
            bool stop = !bot && fixedAge > 0.3f && inputBank && inputBank.skill4.justPressed;
            if (stop || fixedAge >= row.Duration) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Sound("end");
            Vfx.Stop(fx, 0.8f);
            Vfx.Play(row.Vfx, "end", characterBody.footPosition, row.Radius);
            if (anim) anim.StopOverride();
            if (Server) Buffs.Clear(characterBody, row.Buff);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Mo & Krill: Tag Team (Q, the ult equipment) ----------------
    /// <summary>Throws Krill at the enemy under the crosshair (range); the server's DlrHopper does the hopping. Any carrier:
    /// on Mo his Krill is hidden while the row's buff is on (and Mo punches), on anyone else a Krill appears.</summary>
    [Kind("ult_krill")]
    public sealed class UltKrill : UltState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            var target = LockOn(row.Range);
            Sound("cast");
            if (anim) anim.Override("ult_start", 0.5f);
            characterBody.SetAimTimer(2f);
            var from = MuzzlePos();
            Vfx.Play(row.Vfx, "cast", from);
            if (Server) DlrHopper.Throw(characterBody, row, from, target, ThrowDir);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= 0.5f) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (anim) anim.StopOverride();
            base.OnExit();
        }
    }
}
