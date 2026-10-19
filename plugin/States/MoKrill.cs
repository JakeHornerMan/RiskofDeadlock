using System.Collections.Generic;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ---------------- Mo & Krill: Burrow (Shift) ----------------
    // Mo digs in: he sinks into the ground, then the model is hidden (he can still be hit, as in Deadlock) and the row's
    // buff makes him faster and armoured with no gun; a dirt mound follows him. Each hit he takes costs 1 m/s for 1.5 s. He
    // bursts out at duration, on Shift or Jump after minDuration, or when he walks off a ledge (BurrowPop, a state of its
    // own so every player sees it); Q underground (Dust Devil, Equipment.Fire) bursts straight into the tornado.
    [Kind("burrow")]
    public sealed class Burrow : DlrState
    {
        /// <summary>When the model starts sinking (after the dig-in clip's first moment) and how long it takes to go under.</summary>
        public const float SinkDelay = 0.1f, SinkTime = 0.45f;

        GameObject mound; int loop; bool hidden; float lastHealth, airborne;
        readonly List<float> hitsTaken = new List<float>();
        CharacterModel model;
        Transform modelBase; Vector3 baseLocal;

        /// <summary>How far down the model goes to be out of sight: his capsule's height and a little more.</summary>
        public static float Depth(CharacterBody b)
        {
            var cap = b ? b.GetComponent<CapsuleCollider>() : null;
            return (cap ? cap.height : 2.5f) + 0.5f;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            Vfx.Play(row.Vfx, "cast", characterBody.footPosition, row.Radius);
            if (anim) anim.Gesture("utility");
            if (Server) Buffs.AddTimed(characterBody, row.Buff, row.Duration + 1f);
            model = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            modelBase = characterBody.modelLocator ? characterBody.modelLocator.modelBaseTransform : null;
            if (modelBase) baseLocal = modelBase.localPosition;
            lastHealth = healthComponent ? healthComponent.health : 0f;
        }

        public override void Update()
        {
            base.Update();
            // digging in: the model sinks into the ground (easing in), then goes out of sight
            if (hidden || !modelBase) return;
            float t = Mathf.Clamp01((age - SinkDelay) / SinkTime);
            modelBase.localPosition = baseLocal + Vector3.down * Depth(characterBody) * t * t;
        }

        void Hide()
        {
            if (hidden) return;
            hidden = true;
            if (model) model.invisibilityCount++;
            if (modelBase) modelBase.localPosition = baseLocal;
            Vfx.Play(row.Vfx, "under", characterBody.footPosition, row.Radius);
            loop = Fx.Loop(row.Id, transform.position);
            mound = Vfx.Play(row.Vfx, "loop", characterBody.footPosition, row.Radius, transform);
        }

        void Show()
        {
            if (modelBase) modelBase.localPosition = baseLocal;
            if (!hidden) return;
            hidden = false;
            if (model) model.invisibilityCount--;
            DeadlockAudio.Stop(loop); loop = 0;
            Vfx.Stop(mound, 0.8f); mound = null;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!hidden && fixedAge >= SinkDelay + SinkTime) Hide();
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

            bool early = fixedAge >= row.MinDuration && inputBank && (inputBank.skill3.justPressed || inputBank.jump.justPressed);
            if (fixedAge >= row.Duration || early || airborne > 0.15f && fixedAge > SinkDelay + SinkTime || bot && fixedAge >= 1.2f)
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

    /// <summary>Burrow, part 2: Mo bursts up out of the ground in a spin (the model rises from below): once he breaks the
    /// surface, burst hits of damagePct every interval within radius, each enemy slowed (the row's debuff) and, on the first
    /// hit, knocked up (bosses only if bossMult > 0).</summary>
    public sealed class BurrowPop : DlrState
    {
        const float RiseTime = 0.3f;
        int hits; float next = RiseTime * 0.5f;
        Transform modelBase; Vector3 baseLocal;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("hit");
            Vfx.Play(row.Vfx, "hit", characterBody.footPosition, row.Radius);
            if (anim) anim.Override("special", RiseTime * 0.5f + row.Interval * row.Burst + 0.2f);
            modelBase = characterBody.modelLocator ? characterBody.modelLocator.modelBaseTransform : null;
            if (modelBase) { baseLocal = modelBase.localPosition; Rise(); }
        }

        void Rise()
        {
            float t = Mathf.Clamp01(age / RiseTime), e = 1f - (1f - t) * (1f - t);
            modelBase.localPosition = baseLocal + Vector3.down * Burrow.Depth(characterBody) * (1f - e);
        }

        public override void Update()
        {
            base.Update();
            if (modelBase) Rise();
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
            if (isAuthority && fixedAge >= RiseTime * 0.5f + row.Interval * row.Burst + 0.1f) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (modelBase) modelBase.localPosition = baseLocal;
            Sound("end");
            if (anim) anim.StopOverride();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Mo & Krill: Sand Blast (M2) ----------------
    /// <summary>Mo winds up for minDuration while the cone shows on the ground, then throws a wave of sand along it
    /// (DlrSandWave) in Deadlock's cone: radius wide at Mo, widening by spreadDeg either side, out to range at speed. The
    /// direction is fixed at the start (sent with the state) so the cone shown is the cone hit. On the Weapon machine, so it
    /// works while Mo is burrowed.</summary>
    [Kind("cone_wave")]
    public sealed class ConeWave : DlrState
    {
        Vector3 dir; bool thrown; GameObject marker, gather;

        public override void OnEnter()
        {
            base.OnEnter();
            if (anim) anim.Gesture("secondary");
            characterBody.SetAimTimer(2f);
            dir = ThrowDir; dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = characterDirection ? characterDirection.forward : transform.forward;
            dir.Normalize();
            var foot = characterBody.footPosition;
            marker = Vfx.Play(row.Vfx, "telegraph", foot, row.Radius, null, foot + dir * row.Range, row.SpreadDeg, row.MinDuration);
            gather = Vfx.Play(row.Vfx, "gather", characterBody.corePosition + dir * characterBody.radius, row.Radius, transform);
            if (row.MinDuration <= 0f) Throw();
        }

        void Throw()
        {
            thrown = true;
            Sound("cast");
            Vfx.Stop(gather, 0.5f);
            var from = characterBody.corePosition + dir * characterBody.radius;
            Vfx.Play(row.Vfx, "cast", from, row.Radius, null, characterBody.corePosition + dir * 5f);
            // every machine runs the wave; only the server's deals damage
            DlrSandWave.Launch(characterBody, row, characterBody.footPosition, dir, Server);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!thrown && fixedAge >= row.MinDuration) Throw();
            if (isAuthority && fixedAge >= row.Duration) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            // cut short (a stun) before the throw: the cone goes with it
            if (!thrown) Vfx.Stop(marker, 0.2f);
            Vfx.Stop(gather, 0.3f);
            base.OnExit();
        }
    }

    // ---------------- Mo & Krill: Dust Devil (Q, the ult equipment) ----------------
    /// <summary>The carrier spins as a sand tornado for duration: it keeps walking and can jump (no sprint), everything within
    /// range is pulled toward it (bosses at bossMult), everything within radius takes damagePct every interval. Mo plays
    /// Deadlock's tornado clip; any other carrier (no special_loop clip) has its whole model turned two times a second.</summary>
    [Kind("ult_devil")]
    public sealed class UltDevil : UltState
    {
        const float SpinDegPerSec = 720f;
        /// <summary>Started from Burrow (Q underground): Mo bursts out of the ground straight into the tornado.</summary>
        public bool FromBurrow;
        GameObject fx; int loop; float nextTick, yaw; bool clip;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            if (FromBurrow) Vfx.Play(Sheet.SkillOf(row.Hero, "utility").Vfx, "hit", characterBody.footPosition, row.Radius);
            Vfx.Play(row.Vfx, "cast", characterBody.footPosition, row.Radius);
            fx = Vfx.Play(row.Vfx, "loop", characterBody.footPosition, row.Radius, transform, null, row.Range);
            clip = anim && anim.HasClip("special_loop");
            if (clip) anim.Override("special_loop", row.Duration, true);
            if (characterDirection) { characterDirection.moveVector = Vector3.zero; yaw = characterDirection.yaw; }
            if (Server) Buffs.AddTimed(characterBody, row.Buff, row.Duration + 0.5f);
        }

        public override void Update()
        {
            base.Update();
            // a carrier without the tornado clip turns on the spot, model only: the camera and the aim stay put
            if (clip || !characterDirection) return;
            yaw = Mathf.Repeat(yaw + SpinDegPerSec * Time.deltaTime, 360f);
            characterDirection.yaw = yaw;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterBody.isSprinting = false;
                characterMotor.moveDirection = inputBank ? inputBank.moveVector : Vector3.zero;
                // the carrier can jump while it spins (every jump it has: double jumps, Feathers)
                if (inputBank && inputBank.jump.justPressed && characterMotor.jumpCount < characterBody.maxJumpCount)
                {
                    GenericCharacterMain.ApplyJumpVelocity(characterMotor, characterBody, 1f, 1f);
                    characterMotor.jumpCount++;
                }
                var face = GetAimRay().direction; face.y = 0;
                if (clip && characterDirection && face.sqrMagnitude > 0.01f) characterDirection.forward = face.normalized;
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
            if (isAuthority && fixedAge >= row.Duration) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Sound("end");
            Vfx.Stop(fx, 0.8f);
            Vfx.Play(row.Vfx, "end", characterBody.footPosition, row.Radius);
            if (anim) anim.StopOverride();
            // a spun carrier faces where it aims again
            if (!clip && characterDirection && inputBank) characterDirection.forward = inputBank.aimDirection;
            if (Server) Buffs.Clear(characterBody, row.Buff);
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(FromBurrow);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            FromBurrow = reader.ReadBoolean();
        }
    }

    // ---------------- Mo & Krill: Tag Team (R) ----------------
    /// <summary>Throws Krill at the enemy under the crosshair (range); the server's DlrHopper does the hopping. No enemy
    /// there: nothing is thrown and the charge comes back. Krill is hidden while the row's buff is on, and Mo is in a
    /// Frenzy: twice as fast, and the M1 punches.</summary>
    [Kind("krill_hop")]
    public sealed class KrillHop : DlrState
    {
        bool thrown;

        public override void OnEnter()
        {
            base.OnEnter();
            var target = LockOn(row.Range);
            if (!target) { RefundStock(); return; }
            thrown = true;
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
            if (isAuthority && (!thrown || fixedAge >= 0.5f)) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (thrown && anim) anim.StopOverride();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}
