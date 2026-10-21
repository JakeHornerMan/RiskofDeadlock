using System.Collections.Generic;
using EntityStates;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain.States
{
    // ---------------- Rat King: Scrap Grenade ----------------
    static class ScrapAim
    {
        /// <summary>Where the grenade leaves from (the gun, or the aim origin when the gun is behind a wall or in the floor) and
        /// the direction from there to the point under the crosshair, so it flies where the arc says even though the gun is
        /// off to one side of the camera.</summary>
        public static Vector3 From(CharacterBody body, Vector3 muzzle)
        {
            var origin = body.inputBank ? body.inputBank.aimOrigin : body.corePosition;
            return Physics.Linecast(origin, muzzle, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? origin : muzzle;
        }

        public static Vector3 Dir(CharacterBody body, Ray aim, Vector3 from)
        {
            var to = aim.GetPoint(200f);
            var hits = Physics.RaycastAll(aim, 200f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (hb && hb.healthComponent && hb.healthComponent.body && hb.healthComponent.body.teamComponent.teamIndex == body.teamComponent.teamIndex) continue;
                to = h.point; break;
            }
            var d = to - from;
            return d.sqrMagnitude > 0.01f ? d.normalized : aim.direction;
        }
    }

    [Kind("lob_scrap")]
    public sealed class ThrowScrap : DlrState
    {
        // Scrap Grenade, step 1: Right Click readies it; the owner sees the arc to the first impact and a ring the size of the
        // first blast (DlrLob.Predict: the same speed and gravity the grenade flies with), and any click, Left or Right, throws
        // it (ScrapThrow). Test mode and the DPS bench throw shortly after it is up.
        GameObject marker;
        int readyLoop;
        readonly List<Vector3> path = new List<Vector3>();
        public override void OnEnter()
        {
            base.OnEnter();
            characterBody.SetAimTimer(2f);
            Fx.Sound(row.Id + "/ready", "cast", transform.position);
            if (isAuthority)
            {
                marker = Vfx.Play(row.Vfx, "aim", transform.position, row.Radius);
                readyLoop = Fx.Loop(row.Id + "/ready", transform.position);
            }
        }
        public override void Update()
        {
            base.Update();
            if (!marker) return;
            var from = ScrapAim.From(characterBody, MuzzlePos());
            var landing = DlrLob.Predict(characterBody, row, from, ScrapAim.Dir(characterBody, GetAimRay(), from), path);
            Vfx.SetArc(marker, path, landing);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!isAuthority) return;
            characterBody.SetAimTimer(2f);
            bool bot = MeltyTest.Active || DpsBench.Active;
            bool click = inputBank && fixedAge > 0.1f && (inputBank.skill1.justPressed || inputBank.skill2.justPressed);
            if (click || bot && fixedAge >= 0.8f) outer.SetNextState(new ScrapThrow { RowOverride = row });
        }
        public override void OnExit()
        {
            Vfx.Stop(marker, 0.1f);
            DeadlockAudio.Stop(readyLoop);
            base.OnExit();
        }
        // the gun can't take the Left Click that throws
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    /// <summary>Scrap Grenade, step 2: the throw (no kind of its own; started by ThrowScrap with the row).</summary>
    public sealed class ScrapThrow : DlrState
    {
        bool thrown;
        public override void OnEnter()
        {
            base.OnEnter();
            dur = row.Duration / attackSpeedStat;
            // the owner aims it at the crosshair point; the direction travels with the state (OwnerAim) to everyone else
            if (isAuthority) OwnerAim = ScrapAim.Dir(characterBody, GetAimRay(), ScrapAim.From(characterBody, MuzzlePos()));
            if (anim) anim.Gesture("secondary", 1.4f);
            Sound("cast");
            characterBody.SetAimTimer(2f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!thrown && fixedAge >= dur * 0.3f) Throw();
            if (fixedAge >= dur && isAuthority) outer.SetNextStateToMain();
        }
        void Throw()
        {
            thrown = true;
            var from = ScrapAim.From(characterBody, MuzzlePos());
            DlrScrap.Launch(characterBody, row, from, OwnerAim != Vector3.zero ? OwnerAim : ThrowDir, Server);
        }
        public override void OnExit()
        {
            // cut short before the throw point: still throw, so the cooldown is never spent for nothing
            if (!thrown && row != null) Throw();
            base.OnExit();
        }
    }

    // ---------------- Rat King: Royal Pestments ----------------
    [Kind("rat_ring")]
    public sealed class RatRingCast : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("utility");
            // on every machine: the rats and sound everywhere, the bites on the server (RatRing)
            RatRing.Begin(characterBody, row);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.2f && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;
    }

    // ---------------- Rat King: Rule, Ratannia! ----------------
    [Kind("banner_leap")]
    public sealed class BannerLeap : DlrState
    {
        // raise the banner (minDuration), leap in a fixed arc toward the crosshair (at most range away; a little steering in the
        // air), and slam it down on landing (or after duration): damagePct within radius, splashPct and a stunDuration stun
        // within reach (bosses x bossMult), only enemies the banner can see. Works from the air too.
        enum Phase { WindUp, Leap, Done }
        Phase phase;
        float leapAt;
        int loop;
        GameObject trail;
        Vector3 target, flatVel;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            if (anim) { anim.ShowProps(true); anim.Override("special_start", row.MinDuration + 0.1f); }
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition + Vector3.up, row.Radius, transform);
            if (isAuthority) target = AimPoint();
        }

        /// <summary>The ground or enemy under the crosshair, at most range away (flat distance).</summary>
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
            if (flat.magnitude > row.Range) point = transform.position + flat.normalized * row.Range + Vector3.up * (point.y - transform.position.y);
            if (Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out var ground, 60f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) point = ground.point;
            return point;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!characterMotor) { if (isAuthority) outer.SetNextStateToMain(); return; }
            switch (phase)
            {
                case Phase.WindUp:
                    // the banner goes up; in the air he hangs for the moment it takes
                    if (isAuthority) { characterMotor.moveDirection = Vector3.zero; var v = characterMotor.velocity; v.x *= 0.5f; v.z *= 0.5f; v.y = Mathf.Max(v.y, 0f); characterMotor.velocity = v; }
                    if (fixedAge >= row.MinDuration)
                    {
                        phase = Phase.Leap; leapAt = fixedAge;
                        Fx.Sound(row.Id + "/jump", "cast", transform.position);
                        loop = Fx.Loop(row.Id, transform.position);
                        if (anim) anim.Override("special_loop", row.Duration, true);
                        trail = Vfx.Play(row.Vfx, "loop", transform.position, row.Radius, transform);
                        if (isAuthority) Launch();
                    }
                    break;
                case Phase.Leap:
                {
                    float t = fixedAge - leapAt;
                    if (isAuthority)
                    {
                        // the arc keeps its own forward speed (RoR2's motor would brake it in the air), plus a little steering
                        var steer = inputBank ? inputBank.moveVector : Vector3.zero; steer.y = 0;
                        flatVel += steer * 10f * Time.fixedDeltaTime;
                        characterMotor.velocity = new Vector3(flatVel.x, characterMotor.velocity.y, flatVel.z);
                        characterMotor.moveDirection = Vector3.zero;
                    }
                    if (characterMotor.isGrounded && t > 0.15f || t >= row.Duration)
                    {
                        Slam();
                        if (isAuthority) outer.SetNextStateToMain();
                    }
                    break;
                }
            }
        }

        /// <summary>The leap: a fixed arc that lands on the target, flying at about speed m/s forward (a short hop still takes
        /// 0.45 s, a long one at most duration).</summary>
        void Launch()
        {
            var from = transform.position;
            var flat = target - from; flat.y = 0;
            float g = -Physics.gravity.y * characterMotor.gravityScale * characterMotor.gravityMultiplier;
            float time = Mathf.Clamp(flat.magnitude / Mathf.Max(1f, row.Speed), 0.45f, row.Duration * 0.85f);
            float dy = target.y - from.y;
            var v = flat / time;
            v.y = dy / time + 0.5f * g * time;
            characterMotor.Motor.ForceUnground();
            characterMotor.velocity = v;
            flatVel = new Vector3(v.x, 0f, v.z);
        }

        void Slam()
        {
            phase = Phase.Done;
            var at = characterBody.footPosition;
            var fwd = characterDirection ? characterDirection.forward : transform.forward;
            if (anim) anim.Override("special", 0.7f);
            Vfx.Play(row.Vfx, "hit", at, row.Reach);
            Sound("hit");
            RatBurst.Play(at, row.Reach, 12);
            RatBanner.Plant(row, at, fwd);
            if (characterMotor) characterMotor.velocity = Vector3.zero;
            MeltyTest.Report("banner-slam", true, characterBody.baseNameToken);
            if (!Server) return;
            // like Deadlock: only what the banner can see from its top
            var top = at + Vector3.up * 1.5f;
            foreach (var hb in Fx.Enemies(characterBody, at, row.Reach))
            {
                var b = hb.healthComponent.body;
                var core = b ? b.corePosition : hb.transform.position;
                if (Physics.Linecast(top, core, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                var flat = core - at; flat.y = 0;
                bool inner = flat.magnitude <= row.Radius + (b ? b.radius : 0f);
                Fx.Hit(characterBody, hb, (inner ? row.DamagePct : row.SplashPct) / 100f, row.ProcCoefficient, core);
                if (b) Fx.Stun(hb.healthComponent, row.StunDuration * (b.isChampion ? row.BossMult : 1f));
            }
        }

        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Vfx.Stop(trail, 0.4f);
            if (phase != Phase.Done) Slam();
            if (!Survivors.NoFallDamage(characterBody)) characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            if (anim) { anim.StopOverride(); anim.ShowProps(false); }
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Rat King: Rat Swarm (Q equipment) ----------------
    [Kind("ult_swarm")]
    public sealed class UltSwarm : UltState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            Vfx.Play(row.Vfx, "cast", characterBody.footPosition, 2f);
            if (anim && anim.HasClip("ult_start")) anim.Gesture("ult_start");
            // on every machine, around whoever fired it: the rats everywhere, their bites on the server
            RatSwarm.Begin(characterBody, row);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.3f && isAuthority) outer.SetNextStateToMain();
        }
    }
}
