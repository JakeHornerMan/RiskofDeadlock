using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ---------------- Billy: the bat (M2) ----------------
    /// <summary>One swing of Billy's 3-hit bat combo (burst swings, the last splashPct and heavier). One press is one swing,
    /// lasting duration (Deadlock's whole melee clip at its own pace), and the cooldown is the same, so the next press can follow
    /// as it ends; holding the key does not repeat it (creator, 2026-10-22). On the
    /// ground it hits what is in the arc where he aims; in the air it locks onto the nearest enemy within lockRange and snaps onto
    /// it (up to reach metres, moved exactly like a dash, arriving before the bat lands, then hanging there), deals airMult x, and each hit stalls his fall
    /// and the enemy's (if it is in the air) for hangTime more, so he can juggle; it never launches anyone. Every swing that connects puts refillPct % of Biquette's magazine back. Runs on the Weapon machine with the gun,
    /// so the two never act at once; the gun waits minDuration after the last swing.</summary>
    [Kind("bat_combo")]
    public sealed class BatCombo : DlrState
    {
        int swing; bool air, heavy, struck;
        /// <summary>Swings started on this machine's own heroes (the test counts them).</summary>
        public static int Swings;
        float swingTime, hitAt, stopUntil;
        // how far the air snap may still carry him (the row's reach)
        float snapLeft, snapSpeed, snapTime;
        bool swungClip;
        GameObject snapFx;
        Vector3 dir;

        void SwingClip()
        {
            if (swungClip) return;
            swungClip = true;
            float left = Mathf.Max(0.1f, swingTime - fixedAge);
            if (anim)
            {
                if (snapTime > 0f) anim.StopOverride();
                anim.Gesture(air ? "melee_air" : swing == 0 ? "melee" : heavy ? "melee_heavy" : "melee_alt", row.Duration / left);
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            if (isAuthority && rt != null)
            {
                if (Time.time - rt.LastSwingEnd > row.Interval) rt.Combo = 0;
                swing = rt.Combo;
                air = BillyAir.InAir(characterBody);
                LockTarget = air ? BillyAir.Nearest(characterBody, row.LockRange) : null;
            }
            int burst = (int)Mathf.Max(1f, row.Burst);
            swing %= burst;
            heavy = swing == burst - 1;
            swingTime = row.Duration / attackSpeedStat;
            hitAt = swingTime * 0.4f;
            snapLeft = row.Reach;
            dir = AimDir();
            Face();
            if (isAuthority) Swings++;
            // in the air with a lock: a dodge onto the enemy first, at the G.O.A.T. dodge's speed, and the bat lands as he arrives
            var t0 = Locked;
            var hero = Sheet.Hero(Survivors.HeroOf(characterBody));
            snapSpeed = hero != null && hero.DodgeTime > 0f ? hero.DodgeDistance / hero.DodgeTime : row.Reach / Mathf.Max(0.05f, hitAt);
            if (air && t0)
            {
                float gap = Mathf.Min(row.Reach, Mathf.Max(0f, Vector3.Distance(t0.corePosition, characterBody.corePosition) - characterBody.radius - t0.radius - 0.6f));
                snapTime = gap / Mathf.Max(1f, snapSpeed);
                if (snapTime > 0.05f)
                {
                    hitAt = Mathf.Max(hitAt, snapTime + swingTime * 0.15f);
                    if (anim) anim.Override("dash_air", snapTime + 0.05f);
                    var gun = Sheet.SkillOf(hero != null ? hero.Id : row.Hero, "primary");
                    if (gun != null) Fx.Sound(gun.Id + "/dodge", "cast", characterBody.corePosition);
                    snapFx = Vfx.Play("billy_dodge", "cast", characterBody.corePosition, 1f, transform);
                    if (snapFx) snapFx.transform.rotation = Quaternion.LookRotation(dir);
                }
                else snapTime = 0f;
            }
            // the clip runs its whole length over the swing (faster only with attack speed); after a dodge-in it starts on
            // arrival, quick enough to land with the hit
            if (snapTime <= 0f) SwingClip();
            Sound("cast");
            if (heavy) Sound("end");
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition + dir * 1.3f, heavy ? 1.5f : 1f, null, characterBody.corePosition + dir * 3f, swing * 70f + (air ? 30f : 0f));
            characterBody.SetAimTimer(1f);
        }

        // the owner's choice of swing and air travels with the state, so every machine plays the same swing
        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(swing); writer.Write(air); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); swing = reader.ReadInt32(); air = reader.ReadBoolean(); }

        CharacterBody Locked => LockTarget && LockTarget.healthComponent && LockTarget.healthComponent.alive ? LockTarget.healthComponent.body : null;

        bool frozen;
        void HitStop()
        {
            bool stop = Time.time < stopUntil;
            if (!anim || stop == frozen) return;
            frozen = stop;
            anim.Freeze(stop);
        }

        /// <summary>Where the swing goes: at the locked enemy in the air, where he aims on the ground (level).</summary>
        Vector3 AimDir()
        {
            var t = Locked;
            if (air && t) return (t.corePosition - characterBody.corePosition).normalized;
            var d = ThrowDir;
            if (!air) { d.y = 0; d.Normalize(); }
            return d.sqrMagnitude > 0.01f ? d : transform.forward;
        }

        void Face()
        {
            var flat = dir; flat.y = 0;
            if (flat.sqrMagnitude > 0.01f && characterDirection) characterDirection.forward = flat.normalized;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            var t = Locked;
            if (air && t && !struck)
            {
                dir = (t.corePosition - characterBody.corePosition).normalized;
                Face();
                // the dodge-in: moved straight onto the locked enemy at the dodge's speed (exactly, as dashes move: no gravity or
                // air control in the way), then held there until the bat lands
                if (isAuthority && characterMotor)
                {
                    characterMotor.velocity = Vector3.zero;
                    characterMotor.moveDirection = Vector3.zero;
                    float gap = Vector3.Distance(t.corePosition, characterBody.corePosition) - characterBody.radius - t.radius - 0.6f;
                    snapLeft = Mathf.Min(snapLeft, Mathf.Max(0f, gap));
                    if (snapLeft > 0.01f)
                    {
                        float step = Mathf.Min(snapLeft, snapSpeed * Time.fixedDeltaTime);
                        characterMotor.rootMotion += dir * step;
                        snapLeft -= step;
                    }
                }
                if (snapFx) snapFx.transform.rotation = Quaternion.LookRotation(dir);
            }
            // arrived: the trails go and the swing plays
            if (snapTime > 0f && !swungClip && fixedAge >= snapTime) { Vfx.Stop(snapFx, 0.3f); snapFx = null; }
            if (!swungClip && fixedAge >= Mathf.Max(0f, snapTime)) SwingClip();
            if (!struck && fixedAge >= hitAt) Strike();
            // hit-stop: his pose holds a moment on contact (this machine only; timing is unchanged)
            HitStop();
            if (fixedAge >= swingTime && isAuthority) outer.SetNextStateToMain();
        }

        void Strike()
        {
            struck = true;
            if (isAuthority && rt != null) rt.Combo = swing + 1;
            var center = characterBody.corePosition;
            float cos = Mathf.Cos(row.SpreadDeg * Mathf.Deg2Rad);
            var hits = new List<HurtBox>();
            foreach (var hb in Fx.Enemies(characterBody, center, row.Radius + 3f))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                var to = b.corePosition - center;
                if (!air) to.y = 0f;
                if (to.magnitude - b.radius > row.Radius) continue;
                if (to.sqrMagnitude > 1f && Vector3.Dot(to.normalized, dir) < cos) continue;
                hits.Add(hb);
            }
            // the locked enemy always takes the swing if it is in reach
            var t = Locked;
            if (air && t && !hits.Contains(LockTarget) && Vector3.Distance(t.corePosition, center) <= row.Radius + t.radius + characterBody.radius + 1f) hits.Add(LockTarget);
            if (heavy && !air) Vfx.Play(row.Vfx, "end", characterBody.footPosition + dir * 2f, 1f);
            if (hits.Count == 0) return;
            float coef = (heavy ? row.SplashPct : row.DamagePct) / 100f * (air ? row.AirMult : 1f);
            foreach (var hb in hits)
            {
                var b = hb.healthComponent.body;
                var at = b ? b.corePosition : hb.transform.position;
                Vfx.Play(row.Vfx, "hit", at, heavy || air ? 1.5f : 1f, null, at + dir);
                if (!Server) continue;
                Fx.Hit(characterBody, hb, coef, row.ProcCoefficient, at);
                // up there every hit stalls an enemy in the air for hangTime more, as long as the hero's own hang: a floating
                // one floats longer, one falling or flying is held where it is (bosses excepted)
                var lift = b ? b.GetComponent<DlrLift>() : null;
                if (air && lift) lift.Extend(row.HangTime, row.FloatTime);
                else if (air && b && !b.isChampion && (!b.characterMotor || !b.characterMotor.isGrounded)) DlrLift.Float(b, 0f, Time.fixedDeltaTime, row.HangTime);
            }
            Fx.Sound(row.Id, "hit", hits[0].transform.position);
            stopUntil = Time.time + (heavy ? 0.08f : 0.05f);
            if (isAuthority)
            {
                if (air && rt != null) rt.HangFor(row.HangTime, row.FloatTime);
                var gun = Survivors.RowFor(skillLocator ? skillLocator.primary : null);
                if (rt != null && row.RefillPct > 0f && gun != null) { rt.Refill(gun, row.RefillPct); Sound("reload"); }
            }
            MeltyTest.Report(air ? "bat-air-hit" : "bat-hit", true, characterBody.baseNameToken);
        }

        public override void OnExit()
        {
            if (anim && frozen) anim.Freeze(false);
            Vfx.Stop(snapFx, 0.3f);
            if (rt != null && row != null)
            {
                rt.LastSwingEnd = Time.time;
                rt.GunBlockedUntil = Time.time + row.MinDuration;
            }
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }

    // ---------------- Billy: Bashdown (Shift) ----------------
    /// <summary>Locks onto the nearest enemy within lockRange. On the ground: a minDuration hop, then damagePct, a knockback and a
    /// stunDuration stun on it (bosses: bossMult of the stun, no knockback) and splashPct to every other enemy within reach (the
    /// shockwave). In the air: he dives at speed m/s; on contact damagePct x airMult to it and damagePct to every enemy within
    /// radius of it, all driven into the floor (floats end) and stunned duration seconds; he drops with them. No target: on the
    /// ground splashPct within reach in front, in the air straight down and damagePct within radius of where he lands.</summary>
    [Kind("bashdown")]
    public sealed class Bashdown : DlrState
    {
        bool air, smashed;
        float smashAt, stopUntil;
        Vector3 dir;
        GameObject trail;
        int loop;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            if (isAuthority)
            {
                air = BillyAir.InAir(characterBody);
                LockTarget = BillyAir.Nearest(characterBody, row.LockRange);
            }
            var t = Locked;
            dir = t ? (t.corePosition - characterBody.corePosition).normalized : air ? Vector3.down : Flat(ThrowDir);
            Face();
            Sound("cast");
            characterBody.SetAimTimer(1.5f);
            if (air)
            {
                characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                // the slam pose holds through the dive and the fall until he lands (StopOverride on exit)
                if (anim) { anim.Override("slam", 0f); slamPose = true; }
                loop = Fx.Loop(row.Id, transform.position);
                trail = Vfx.Play(row.Vfx, "loop", characterBody.corePosition, 1f, transform);
            }
            else
            {
                if (anim) anim.Override("utility", row.MinDuration + 0.35f);
                Vfx.Play(row.Vfx, "cast", characterBody.footPosition, 1f, transform);
                // the hop as he raises the bat (Deadlock's channel)
                if (isAuthority && characterMotor) { characterMotor.Motor.ForceUnground(); characterMotor.velocity = Vector3.up * 6f; }
            }
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(air); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); air = reader.ReadBoolean(); }

        CharacterBody Locked => LockTarget && LockTarget.healthComponent && LockTarget.healthComponent.alive ? LockTarget.healthComponent.body : null;

        bool frozen;
        void HitStop()
        {
            bool stop = Time.time < stopUntil;
            if (!anim || stop == frozen) return;
            frozen = stop;
            anim.Freeze(stop);
        }

        static Vector3 Flat(Vector3 d) { d.y = 0; return d.sqrMagnitude > 0.001f ? d.normalized : Vector3.forward; }

        void Face()
        {
            var flat = dir; flat.y = 0;
            if (flat.sqrMagnitude > 0.01f && characterDirection) characterDirection.forward = flat.normalized;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            HitStop();
            var t = Locked;
            if (!air)
            {
                if (isAuthority && characterMotor) characterMotor.moveDirection = Vector3.zero;
                if (t) { dir = (t.corePosition - characterBody.corePosition).normalized; Face(); }
                if (!smashed && fixedAge >= row.MinDuration) GroundSmash();
                // after the smash he is driven back down out of the hop, in the slam pose, and the move ends on the floor
                if (smashed) DriveDown();
                return;
            }
            if (!smashed)
            {
                if (t) dir = (t.corePosition - characterBody.corePosition).normalized;
                Face();
                if (isAuthority && characterMotor) { characterMotor.moveDirection = Vector3.zero; characterMotor.velocity = dir * row.Speed; }
                bool contact = t && Vector3.Distance(t.corePosition, characterBody.corePosition) <= characterBody.radius + t.radius + 1.2f;
                bool landed = characterMotor && characterMotor.isGrounded && fixedAge > 0.05f;
                if (contact || landed || fixedAge >= 1.2f) AirSmash();
            }
            // he drops with them
            else DriveDown();
        }

        /// <summary>After the smash: Billy stays in the slam pose, driven straight down, until he is on the floor (creator,
        /// 2026-10-22: "always ... until he hit the floor"); only a fall with no floor in sight lets go after a while.</summary>
        void DriveDown()
        {
            if (!slamPose && anim) { slamPose = true; anim.Override("slam", 0f); }
            if (isAuthority && characterMotor) { characterMotor.moveDirection = Vector3.zero; characterMotor.velocity = Vector3.down * row.Speed; }
            bool down = characterMotor && characterMotor.isGrounded && fixedAge - smashAt > 0.05f;
            if ((down || fixedAge - smashAt > 6f) && isAuthority) outer.SetNextStateToMain();
        }
        bool slamPose;

        void GroundSmash()
        {
            smashed = true;
            smashAt = fixedAge;
            stopUntil = Time.time + 0.1f;
            var t = Locked;
            var front = characterBody.footPosition + Flat(dir) * row.Reach * 0.6f;
            var at = t ? t.footPosition : front;
            Fx.Sound(row.Id, "hit", at);
            Vfx.Play(row.Vfx, "hit", at, row.Reach);
            Vfx.Play(row.Vfx, "wave", characterBody.footPosition, row.Reach);
            if (!Server) return;
            if (t)
            {
                Fx.Hit(characterBody, LockTarget, Coef, row.ProcCoefficient, t.corePosition);
                Fx.Stun(t.healthComponent, row.StunDuration * (t.isChampion ? row.BossMult : 1f));
                if (!t.isChampion) Fx.KnockBack(characterBody, t.healthComponent, dir, row.Knockback);
            }
            // the shockwave: every other enemy within reach of him (or, with no target, in front of him)
            foreach (var hb in Fx.Enemies(characterBody, t ? characterBody.footPosition : front, row.Reach))
            {
                var b = hb.healthComponent.body;
                if (!b || b == t) continue;
                Fx.Hit(characterBody, hb, row.SplashPct / 100f, row.ProcCoefficient, b.corePosition);
            }
        }

        void AirSmash()
        {
            smashed = true;
            smashAt = fixedAge;
            stopUntil = Time.time + 0.1f;
            Vfx.Stop(trail, 0.3f);
            DeadlockAudio.Stop(loop);
            var t = Locked;
            var center = t ? t.corePosition : characterBody.footPosition;
            var ground = center;
            if (Physics.Raycast(center + Vector3.up, Vector3.down, out var g, 60f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) ground = g.point;
            Fx.Sound(row.Id, "hit", ground);
            Vfx.Play(row.Vfx, "hit", ground, row.Radius);
            // chains run from the slammed enemy to every other enemy in the air it drags down, breaking as they land
            var dragged = new List<CharacterBody>();
            foreach (var hb in Fx.Enemies(characterBody, center, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b || b == t || dragged.Contains(b)) continue;
                if (b.characterMotor && b.characterMotor.isGrounded) continue;
                dragged.Add(b);
            }
            ChainUp(t, dragged, 0.5f);
            if (!Server) return;
            float stun = row.Duration + 0.4f;
            if (t)
            {
                Fx.Hit(characterBody, LockTarget, Coef * row.AirMult, row.ProcCoefficient, t.corePosition);
                BillyAir.SlamDown(t.healthComponent, row.Speed);
                Fx.Stun(t.healthComponent, stun);
            }
            foreach (var hb in Fx.Enemies(characterBody, center, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b || b == t) continue;
                Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition);
                BillyAir.SlamDown(hb.healthComponent, row.Speed);
                Fx.Stun(hb.healthComponent, stun);
            }
        }

        /// <summary>Chains (Chain Gang's) from the slammed enemy (Billy with none) onto every enemy it drags down, for life seconds;
        /// each chain deals chainPct % of the move's damagePct (server). Drawn on every machine.</summary>
        void ChainUp(CharacterBody from, List<CharacterBody> bodies, float life)
        {
            if (row.ChainPct <= 0f || bodies.Count == 0) return;
            Vfx.Chains(characterBody, bodies, 0.06f, life, from ? from.coreTransform : null);
            if (!Server) return;
            foreach (var b in bodies)
                if (b && b.mainHurtBox && b.healthComponent && b.healthComponent.alive)
                    Fx.Hit(characterBody, b.mainHurtBox, Coef * row.ChainPct / 100f, row.ProcCoefficient, b.corePosition);
        }

        public override void OnExit()
        {
            if (row != null && air && !smashed && Server) AirSmash();
            Vfx.Stop(trail, 0.3f);
            DeadlockAudio.Stop(loop);
            if (anim) { if (frozen) anim.Freeze(false); anim.StopOverride(); }
            if (!Survivors.NoFallDamage(characterBody)) characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Billy: Rising Ram (R) ----------------
    /// <summary>Ground only (pressed in the air nothing happens and the charge is kept). A duration-second wind-up he can steer, less and
    /// less toward its end; then a range-metre charge at speed m/s that stops at a wall, or at the first enemy: then
    /// <see cref="RamRise"/> launches everyone around into the float and him with them, and hitRefund of the cooldown comes back.
    /// The owner decides the hit, so the server and every player act on the same one.</summary>
    [Kind("rising_ram")]
    public sealed class RisingRam : DlrState
    {
        bool refused, charging;
        Vector3 dir, lastPos;
        float stuck;
        GameObject fx;
        int loop;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            if (isAuthority && characterMotor && !characterMotor.isGrounded) { refused = true; RefundStock(); return; }
            dir = ThrowDir; dir.y = 0; dir = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
            if (characterDirection) characterDirection.forward = dir;
            Sound("cast");
            if (anim) anim.Override("special_start", row.Duration);
            fx = Vfx.Play(row.Vfx, "cast", characterBody.footPosition, 1f, transform);
            lastPos = transform.position;
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(refused); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); refused = reader.ReadBoolean(); }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null || refused) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (fixedAge < row.Duration)
            {
                // the wind-up: head down, hoof scraping; steering fades out toward the charge
                if (isAuthority && characterMotor)
                {
                    characterMotor.moveDirection = Vector3.zero;
                    characterMotor.velocity = new Vector3(0f, characterMotor.velocity.y, 0f);
                    var aim = GetAimRay().direction; aim.y = 0;
                    if (aim.sqrMagnitude > 0.01f) dir = Vector3.RotateTowards(dir, aim.normalized, 6f * (1f - fixedAge / row.Duration) * Time.fixedDeltaTime, 0f);
                    if (characterDirection) characterDirection.forward = dir;
                }
                return;
            }
            if (!charging)
            {
                charging = true;
                Vfx.Stop(fx, 0.3f);
                if (anim) anim.Override("special_loop", row.Range / row.Speed, true);
                loop = Fx.Loop(row.Id, transform.position);
                fx = Vfx.Play(row.Vfx, "loop", characterBody.footPosition, 1f, transform);
                lastPos = transform.position;
            }
            if (isAuthority && characterMotor)
            {
                characterMotor.moveDirection = Vector3.zero;
                characterMotor.rootMotion += dir * row.Speed * Time.fixedDeltaTime;
                if (characterDirection) characterDirection.forward = dir;
            }
            dir = MotionDir(dir);
            if (fx && dir.sqrMagnitude > 0.01f) fx.transform.rotation = Quaternion.LookRotation(dir);
            if (!isAuthority) return;
            // the first enemy in front of the horns
            foreach (var hb in Fx.Enemies(characterBody, characterBody.corePosition + dir * (characterBody.radius + 0.8f), 1.6f))
            {
                if (!hb.healthComponent.body) continue;
                if (activatorSkillSlot) activatorSkillSlot.RunRecharge(row.Cooldown * row.HitRefund);
                if (rt != null) rt.RefreshDodges();
                outer.SetNextState(new RamRise { RowOverride = row, Target = hb });
                return;
            }
            float moved = Vector3.Distance(transform.position, lastPos);
            lastPos = transform.position;
            if (fixedAge - row.Duration > 0.1f && moved < row.Speed * Time.fixedDeltaTime * 0.15f) stuck += Time.fixedDeltaTime; else stuck = 0f;
            if (stuck > 0.08f || fixedAge >= row.Duration + row.Range / row.Speed) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Vfx.Stop(fx, 0.4f);
            if (anim) anim.StopOverride();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    /// <summary>Rising Ram connected: damagePct to the rammed enemy, every non-boss enemy standing on the ground within radius of it rises launchHeight
    /// metres in minDuration seconds into a floatTime float (<see cref="DlrLift"/>), and Billy rises the same, then hangs a moment so
    /// the air combo can start.</summary>
    public sealed class RamRise : DlrState
    {
        public HurtBox Target;
        bool rising;

        public override void OnEnter()
        {
            if (Target) LockTarget = Target;
            base.OnEnter();
            if (row == null) return;
            var t = LockTarget && LockTarget.healthComponent ? LockTarget.healthComponent.body : null;
            var at = t ? t.corePosition : characterBody.corePosition + transform.forward * 2f;
            var ground = at;
            if (Physics.Raycast(at + Vector3.up, Vector3.down, out var g, 8f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) ground = g.point;
            Fx.Sound(row.Id, "hit", at);
            Vfx.Play(row.Vfx, "hit", ground, row.Radius);
            if (anim) anim.Override("special", 0.6f);
            MeltyTest.Report("ram-hit", true, characterBody.baseNameToken);
            // everyone launched: bosses take the damage but stay put (bossMult 0); only enemies standing on the ground go up
            // (flyers and enemies already in the air just take the hit; creator, 2026-10-22)
            var launched = new List<CharacterBody>();
            foreach (var hb in Fx.Enemies(characterBody, at, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b || b.isChampion && row.BossMult <= 0f) continue;
                if (!b.characterMotor || !b.characterMotor.isGrounded) continue;
                if (!launched.Contains(b)) launched.Add(b);
            }
            // chains snap onto them and haul them up, breaking at the top (chainPct % of the ram's hit each)
            if (row.ChainPct > 0f && launched.Count > 0) Vfx.Chains(characterBody, launched, 0.06f, row.MinDuration + 0.05f);
            if (Server)
            {
                if (t) Fx.Hit(characterBody, LockTarget, Coef, row.ProcCoefficient, at);
                foreach (var b in launched)
                {
                    DlrLift.Float(b, row.LaunchHeight, row.MinDuration, row.FloatTime);
                    if (row.ChainPct > 0f && b.mainHurtBox) Fx.Hit(characterBody, b.mainHurtBox, Coef * row.ChainPct / 100f, row.ProcCoefficient, b.corePosition);
                }
            }
            if (isAuthority && characterMotor)
            {
                characterMotor.Motor.ForceUnground();
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                rising = true;
                if (rt != null) { rt.ComboUntil = Time.time + row.FloatTime + 0.5f; rt.RefreshDodges(); }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (isAuthority && characterMotor)
            {
                characterMotor.moveDirection = Vector3.zero;
                characterMotor.velocity = Vector3.up * row.LaunchHeight / Mathf.Max(0.05f, row.MinDuration);
            }
            if (fixedAge >= row.MinDuration && isAuthority) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (rising && characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
                characterMotor.velocity = Vector3.zero;
                // a moment's hang at the top, so the bat can start before he drops
                var bat = Sheet.SkillOf(Survivors.HeroOf(characterBody), "secondary");
                if (rt != null && bat != null) rt.HangFor(bat.HangTime, bat.FloatTime);
            }
            if (anim) anim.StopOverride();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    // ---------------- Billy: G.O.A.T. (the passive's air dodge) ----------------
    /// <summary>Deadlock's air dash: dodgeDistance metres in dodgeTime along Dir (the held keys, or the aim with none), goring every
    /// enemy he passes through once for dodgePct %. Started by <see cref="BillyAir.TryDodge"/> on the owner; no invulnerability.</summary>
    public sealed class BillyDodge : BaseState
    {
        public Vector3 Dir;
        HeroRow hero;
        string snd;
        float time, speed;
        bool floating;
        GameObject fx;
        DeadlockAnimator anim;
        readonly HashSet<HealthComponent> gored = new HashSet<HealthComponent>();

        public override void OnEnter()
        {
            base.OnEnter();
            hero = Sheet.Hero(Survivors.HeroOf(characterBody));
            if (hero == null || hero.AirDodges <= 0 || Dir.sqrMagnitude < 0.01f) return;
            var gun = Sheet.SkillOf(hero.Id, "primary");
            snd = gun != null ? gun.Id + "/dodge" : null;
            time = hero.DodgeTime;
            speed = hero.DodgeDistance / Mathf.Max(0.05f, time);
            var rt = characterBody.GetComponent<HeroRuntime>();
            if (isAuthority && rt) rt.UseDodge();
            anim = DeadlockAnimator.Of(characterBody);
            if (anim) anim.Override("dash_air", time + 0.2f);
            Fx.Sound(snd, "cast", characterBody.corePosition);
            fx = Vfx.Play("billy_dodge", "cast", characterBody.corePosition, 1f, transform);
            if (fx) fx.transform.rotation = Quaternion.LookRotation(Dir);
            if (characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
            MeltyTest.Report("air-dodge", true, characterBody.baseNameToken);
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(Dir); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); Dir = reader.ReadVector3(); }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (hero == null || time <= 0f) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (isAuthority && characterMotor)
            {
                characterMotor.moveDirection = Vector3.zero;
                characterMotor.velocity = Dir * speed;
                var flat = Dir; flat.y = 0;
                if (flat.sqrMagnitude > 0.01f && characterDirection) characterDirection.forward = flat.normalized;
            }
            // the horns: each enemy along the path once (server)
            if (NetworkServer.active)
                foreach (var hb in Fx.Enemies(characterBody, characterBody.corePosition, characterBody.radius + 1.2f))
                {
                    if (!gored.Add(hb.healthComponent)) continue;
                    var at = hb.healthComponent.body ? hb.healthComponent.body.corePosition : hb.transform.position;
                    Fx.Hit(characterBody, hb, hero.DodgePct / 100f, 1f, at);
                    // a flinch
                    Fx.Stun(hb.healthComponent, 0.3f);
                    Relay.Sound(snd, "hit", at);
                    Relay.Vfx("billy_dodge", "hit", at);
                }
            if (fixedAge >= time && isAuthority) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (floating && characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
                if (isAuthority) characterMotor.velocity *= 0.3f;
            }
            Vfx.Stop(fx, 0.4f);
            if (anim) anim.StopOverride();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Billy: Chain Gang (Q ult equipment) ----------------
    /// <summary>Chains to up to chainCount enemies within radius that the carrier can see (interval the throw), hauls them
    /// launchHeight metres up by minDuration, holds them until duration, then slams them down (speed m/s) for damagePct and a
    /// stunDuration stun; bosses are slammed where they stand. The carrier is rooted throughout. Works on any body.</summary>
    [Kind("ult_chains")]
    public sealed class UltChains : UltState
    {
        readonly List<CharacterBody> victims = new List<CharacterBody>();
        bool hauled, slammed;
        GameObject chains;
        int loop;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            Sound("cast");
            if (anim) anim.Override("ult_start", row.Interval);
            var from = characterBody.corePosition;
            foreach (var hb in Fx.Enemies(characterBody, from, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b || victims.Contains(b)) continue;
                if (Physics.Linecast(from, b.corePosition, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                victims.Add(b);
                if (victims.Count >= row.ChainCount) break;
            }
            chains = Vfx.Chains(characterBody, victims, row.Interval, row.Duration + 0.2f);
            loop = Fx.Loop(row.Id, transform.position);
            characterBody.SetAimTimer(row.Duration + 0.5f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            // rooted from the throw to the slam
            if (isAuthority && characterMotor) { characterMotor.moveDirection = Vector3.zero; characterMotor.velocity = Vector3.zero; }
            if (!hauled && fixedAge >= row.Interval)
            {
                hauled = true;
                if (anim) anim.Override("ult_loop", row.Duration - row.Interval, true);
                Fx.Sound(row.Id, "hit", transform.position);
                if (Server)
                    foreach (var v in victims)
                    {
                        if (!v || !v.healthComponent || !v.healthComponent.alive) continue;
                        // bosses are chained where they stand
                        if (v.isChampion) { Fx.Stun(v.healthComponent, row.Duration); continue; }
                        DlrLift.Haul(v, row.LaunchHeight, row.MinDuration - row.Interval, row.Duration - row.MinDuration);
                    }
            }
            if (!slammed && fixedAge >= row.Duration) Slam();
            if (slammed && fixedAge >= row.Duration + 0.35f && isAuthority) outer.SetNextStateToMain();
        }

        void Slam()
        {
            slammed = true;
            if (anim) anim.Override("ult_end", 0.35f);
            Sound("end");
            Vfx.Stop(chains, 0.1f);
            DeadlockAudio.Stop(loop);
            foreach (var v in victims)
            {
                if (!v || !v.healthComponent || !v.healthComponent.alive) continue;
                var ground = v.footPosition;
                if (Physics.Raycast(v.corePosition, Vector3.down, out var g, 40f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) ground = g.point;
                Vfx.Play(row.Vfx, "hit", ground, Mathf.Max(2f, v.radius * 2f));
                if (!Server) continue;
                Fx.Hit(characterBody, v.mainHurtBox, Coef, row.ProcCoefficient, v.corePosition);
                BillyAir.SlamDown(v.healthComponent, row.Speed);
                Fx.Stun(v.healthComponent, row.StunDuration);
            }
            if (victims.Count > 0) Fx.Sound(row.Id, "hit", transform.position);
        }

        public override void OnExit()
        {
            if (row != null && !slammed && Server) Slam();
            Vfx.Stop(chains, 0.1f);
            DeadlockAudio.Stop(loop);
            if (anim) anim.StopOverride();
            base.OnExit();
        }
    }
}
