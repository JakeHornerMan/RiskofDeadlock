using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using EntityStates;
using KinematicCharacterController;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ---------------- Viscous: Deep Sea Interpreter (M1) ----------------
    /// <summary>The gun fires goo blobs (DlrGoo) instead of bullets: from the muzzle when nothing is between it and the camera's aim
    /// origin, toward the point under the crosshair, so the blobs meet the aim at range (then drop under their gravity).</summary>
    [Kind("gun_goo")]
    public sealed class GunGoo : GunState
    {
        protected override bool Shoot(Ray aim, bool left)
        {
            var target = aim.origin + aim.direction * row.Range;
            if (Physics.Raycast(aim, out var hit, row.Range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore)) target = hit.point;
            var from = MuzzlePos(left);
            if (Physics.Linecast(aim.origin, from, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) from = aim.origin;
            var dir = target - from;
            if (dir.sqrMagnitude < 1f) dir = aim.direction;
            if (row.SpreadDeg > 0f) dir = Quaternion.AngleAxis(Random.Range(0f, 360f), dir) * Quaternion.AngleAxis(Random.Range(0f, row.SpreadDeg), Vector3.Cross(dir, Vector3.up).normalized) * dir;
            DlrGoo.Launch(characterBody, row, from, dir.normalized, Server);
            return true;
        }
    }

    // ---------------- Viscous: Splatter (M2) ----------------
    /// <summary>Splatter, step 1 (lob_ammo): Right Click takes out a ball of goo. The owner sees the throw's path and a goo dome the
    /// size of the splash it would make with the rounds left (DlrLob.Predict); Left Click throws (SplatterThrow), Right Click again
    /// puts it away. No cooldown: the throw costs ammoCost rounds of the gun's magazine. An empty magazine (or one reloading) can't
    /// throw: the reload starts instead.</summary>
    [Kind("lob_ammo")]
    public sealed class ThrowSplatter : DlrState
    {
        GameObject marker;
        readonly List<Vector3> path = new List<Vector3>();
        float scale = 1f;

        public override void OnEnter()
        {
            base.OnEnter();
            characterBody.SetAimTimer(2f);
            if (!isAuthority) return;
            var gun = Survivors.RowFor(skillLocator ? skillLocator.primary : null);
            if (rt != null && rt.Ammo < 0 && gun != null) rt.Ammo = (int)gun.ClipSize;
            if (rt != null && !rt.CanFire)
            {
                if (!rt.Reloading && gun != null) { rt.BeginReload(gun, attackSpeedStat); if (anim) anim.Gesture("reload", 1f); Relay.Reload(characterBody); }
                outer.SetNextStateToMain();
                return;
            }
            scale = SplatterThrow.Scale(row, rt);
            marker = Vfx.Play(row.Vfx, "aim", transform.position, row.Radius * scale);
        }

        public override void Update()
        {
            base.Update();
            if (!marker) return;
            var landing = DlrLob.Predict(characterBody, row, MuzzlePos(), GetAimRay().direction, path);
            Vfx.SetArc(marker, path, landing);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!isAuthority) return;
            characterBody.SetAimTimer(2f);
            // test mode and the DPS bench have no player to click: they throw shortly after the dome is up
            bool bot = MeltyTest.Active || DpsBench.Active;
            if (inputBank && inputBank.skill1.justPressed || bot && fixedAge >= 0.8f)
                outer.SetNextState(new SplatterThrow { RowOverride = row });
            else if (!bot && inputBank && inputBank.skill2.justPressed && fixedAge > 0.1f)
                outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            Vfx.Stop(marker, 0.1f);
            base.OnExit();
        }

        // the gun can't take the Left Click that throws
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    /// <summary>Splatter, step 2: the throw. The owner spends up to ammoCost rounds (all it has left, if fewer) and the splash is that
    /// fraction of its radius; the rounds travel with the state, so every machine throws the same ball. An empty magazine reloads.</summary>
    public sealed class SplatterThrow : DlrState
    {
        bool thrown, botAim; int rounds = -1;

        /// <summary>The throw direction (same yaw as aim) whose landing is nearest the feet of the enemy closest to the aim line.</summary>
        Vector3 FloorAim(Vector3 aim)
        {
            var from = MuzzlePos();
            CharacterBody best = null; float bestScore = float.MaxValue;
            foreach (var hb in Fx.Enemies(characterBody, from, row.Range * 0.5f))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                var to = b.corePosition - from;
                float along = Vector3.Dot(to, aim);
                if (along <= 0f) continue;
                float off = (to - aim * along).magnitude;
                if (off < bestScore) { bestScore = off; best = b; }
            }
            if (!best) return aim;
            var flat = best.footPosition - from; flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return aim;
            var yaw = flat.normalized;
            Vector3 bestDir = aim; float bestD = float.MaxValue;
            for (float pitch = -70f; pitch <= 45f; pitch += 2.5f)
            {
                var dir = Vector3.RotateTowards(yaw, pitch < 0 ? Vector3.down : Vector3.up, Mathf.Abs(pitch) * Mathf.Deg2Rad, 0f);
                float d = Vector3.Distance(DlrLob.Predict(characterBody, row, from, dir, null), best.footPosition);
                if (d < bestD) { bestD = d; bestDir = dir; }
            }
            return bestDir;
        }

        /// <summary>The splash's size for the rounds left: 1 with ammoCost or more, rounds / ammoCost below.</summary>
        public static float Scale(SkillRow row, HeroRuntime rt)
        {
            if (row.AmmoCost <= 0 || rt == null || rt.Ammo < 0) return 1f;
            return Mathf.Clamp01(Mathf.Min(rt.Rounds, row.AmmoCost) / row.AmmoCost);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority && rt != null)
            {
                rounds = Mathf.Max(1, Mathf.Min(rt.Rounds, (int)row.AmmoCost));
                rt.Spend(rounds);
            }
            // test mode and the bench aim at an enemy's chest, which the ball now flies past: they lob it onto the floor at its feet
            // as a player would (the aim travels with the state)
            if (isAuthority && (MeltyTest.Active || DpsBench.Active)) { OwnerAim = FloorAim(GetAimRay().direction); botAim = true; }
            dur = row.Duration / attackSpeedStat;
            if (anim) anim.Gesture("secondary");
            Sound("cast");
            characterBody.SetAimTimer(2f);
        }

        float Fraction => rounds < 0 || row.AmmoCost <= 0 ? 1f : Mathf.Clamp01(rounds / row.AmmoCost);

        void Throw()
        {
            thrown = true;
            DlrLob.Launch(characterBody, row, MuzzlePos(), botAim ? OwnerAim : ThrowDir, Server, Fraction);
            // the magazine is empty now: the reload starts on its own
            var gun = Survivors.RowFor(skillLocator ? skillLocator.primary : null);
            if (isAuthority && rt != null && rt.Ammo <= 0 && !rt.Reloading && gun != null)
            {
                rt.BeginReload(gun, attackSpeedStat);
                if (anim) anim.Gesture("reload", 1f);
                Relay.Reload(characterBody);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!thrown && fixedAge >= dur * 0.3f) Throw();
            if (fixedAge >= dur && isAuthority) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            // cut short before the throw point: still throw, the rounds are already spent
            if (!thrown && row != null) Throw();
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(rounds);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            rounds = reader.ReadInt32();
        }
    }

    // ---------------- Viscous: The Cube (Shift) ----------------
    /// <summary>Viscous seals himself in a goo cube for duration: frozen in place (no animation), invulnerable (vanilla HiddenInvincibility), no moves, no steering,
    /// sliding on with the momentum he had (low friction; walls stop him). RoR2's Mired Urn attachment tethers the chainCount nearest
    /// enemies within range, draining damagePct per second each and healing him for it; each tethered enemy gets one stack of the
    /// debuff (Gooed) a second. Then the cube bursts: splashPct to everything within radius, Gooed to buffStacks, a push of knockback
    /// m/s outward, and the row's buff (Cube Rush: faster) on him. No early exit. The cooldown starts at the burst.</summary>
    [Kind("goo_cube")]
    public sealed class GooCube : DlrState
    {
        Vector3 slide, lastPos; GameObject cube, tethers, urn; int loop; float nextGoo = 1f; bool burst, locked;
        readonly List<Vector3> tetherTo = new List<Vector3>();

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            // trapped in the goo: the pose holds exactly as it is, nothing moves until the burst
            Freeze(true);
            float size = CubeSize(characterBody);
            cube = Vfx.Play(row.Vfx, "loop", characterBody.footPosition, size, transform);
            tethers = Vfx.Play(row.Vfx, "tether", characterBody.corePosition, 1f);
            if (characterMotor) { var v = characterMotor.velocity; slide = new Vector3(v.x, 0f, v.z); }
            lastPos = transform.position;
            if (isAuthority) { LockSkills(true); locked = true; }
            if (!Server) return;
            characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, row.Duration + 0.2f);
            // the drain and heal: RoR2's own Mired Urn attachment, enemies only
            var p = Fx.Prefab(row.Effect);
            if (!p) return;
            urn = Object.Instantiate(p, transform.position, Quaternion.identity);
            var c = urn.GetComponent<SiphonNearbyController>();
            if (c)
            {
                c.Networkradius = row.Range;
                c.NetworkmaxTargets = (int)row.ChainCount;
                c.damagePerSecondCoefficient = Coef;
                c.tickRate = 1f / Mathf.Max(0.05f, row.Interval);
                c.allowSiphonOutOfCombat = true;
                c.ignoreTeamMasks = false;
            }
            urn.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(gameObject);
            Object.Destroy(urn, row.Duration);
        }

        /// <summary>The cube around a body: a little taller than its capsule (any survivor fits, though only Viscous has the move).</summary>
        public static float CubeSize(CharacterBody b)
        {
            var cap = b ? b.GetComponent<CapsuleCollider>() : null;
            return Mathf.Max(2.2f, (cap ? cap.height : 2f) * 1.25f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterMotor.moveDirection = Vector3.zero;
                characterBody.isSprinting = false;
                // slide on with little friction; a wall (it barely moved although it slides) stops it
                var moved = transform.position - lastPos; moved.y = 0f;
                if (fixedAge > 0.1f && slide.sqrMagnitude > 1f && moved.magnitude < slide.magnitude * Time.fixedDeltaTime * 0.2f) slide = Vector3.zero;
                slide *= Mathf.Exp(-0.25f * Time.fixedDeltaTime);
                var v = characterMotor.velocity;
                characterMotor.velocity = new Vector3(slide.x, v.y, slide.z);
            }
            lastPos = transform.position;
            // the tethers, on every machine: the nearest enemies the urn drains
            tetherTo.Clear();
            foreach (var hb in Fx.Enemies(characterBody, characterBody.corePosition, row.Range, (int)row.ChainCount))
                tetherTo.Add(hb.healthComponent.body ? hb.healthComponent.body.corePosition : hb.transform.position);
            Vfx.SetTethers(tethers, characterBody.corePosition, tetherTo);
            if (Server && fixedAge >= nextGoo)
            {
                nextGoo += 1f;
                foreach (var hb in Fx.Enemies(characterBody, characterBody.corePosition, row.Range, (int)row.ChainCount))
                    if (hb.healthComponent.body && row.Debuff != "none") Buffs.AddStacks(characterBody, hb.healthComponent.body, row.Debuff, 1);
            }
            if (isAuthority && fixedAge >= row.Duration) outer.SetNextStateToMain();
        }

        void Burst()
        {
            burst = true;
            var at = characterBody.corePosition;
            Vfx.Stop(cube, 0.1f); cube = null;
            Vfx.Stop(tethers, 0.1f); tethers = null;
            Vfx.Play(row.Vfx, "hit", characterBody.footPosition, row.Radius);
            Sound("hit");
            Sound("end");
            // a goo puddle like Splatter's where he breaks out (extra = the skill row whose puddle it is, reach m wide), on every machine
            var puddle = row.Extra != "none" ? Sheet.Skill(row.Extra) : null;
            if (puddle != null && row.Reach > 0) DlrPuddle.Plant(characterBody, puddle, characterBody.footPosition, row.Reach, Server);
            if (!Server) return;
            characterBody.ClearTimedBuffs(RoR2Content.Buffs.HiddenInvincibility);
            if (urn) Object.Destroy(urn);
            Fx.Blast(characterBody, at, row.Radius, row.SplashPct / 100f, row.ProcCoefficient);
            foreach (var hb in Fx.Enemies(characterBody, at, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                if (row.Debuff != "none") Buffs.AddStacks(characterBody, b, row.Debuff, Mathf.Max(1, (int)row.BuffStacks));
                if (row.Knockback > 0 && !b.isChampion) Fx.KnockBack(characterBody, hb.healthComponent, b.corePosition - at, row.Knockback);
            }
            var exit = Sheet.Buff(row.Buff);
            if (exit != null) { Buffs.Clear(characterBody, row.Buff); Buffs.AddTimed(characterBody, row.Buff, exit.Duration); }
        }

        /// <summary>Holds the hero's pose (Deadlock clips, or a vanilla body's Animator) or lets it go.</summary>
        void Freeze(bool on)
        {
            if (anim) { anim.Freeze(on); return; }
            var a = GetModelAnimator();
            if (a) a.speed = on ? 0f : 1f;
        }

        public override void OnExit()
        {
            if (row != null && !burst) Burst();
            Freeze(false);
            DeadlockAudio.Stop(loop);
            if (locked) LockSkills(false);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    // ---------------- Viscous: Puddle Punch (R) ----------------
    /// <summary>Puddle Punch, step 1 (placed_punch): R shows a ring on the ground under the crosshair, up to range away (twice as wide
    /// over a goo puddle); Left Click places the punch (PunchStrike), R or Right Click puts it away and gives the charge back.</summary>
    [Kind("placed_punch")]
    public sealed class PlacePunch : DlrState
    {
        GameObject ring; int loop; float shown = -1f;

        public override void OnEnter()
        {
            base.OnEnter();
            characterBody.SetAimTimer(2f);
            if (!isAuthority) return;
            ring = Vfx.Play(row.Vfx, "aim", AimPoint(characterBody, row.Range), row.Radius);
            loop = Fx.Loop(row.Id, transform.position);
        }

        /// <summary>The ground under the crosshair (or under the enemy there), at most range away (flat distance).</summary>
        public static Vector3 AimPoint(CharacterBody body, float range)
        {
            var aim = body.inputBank.GetAimRay();
            var point = aim.origin + aim.direction * range;
            var hits = Physics.SphereCastAll(aim, 0.3f, range * 2f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            var team = body.teamComponent.teamIndex;
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == team)) continue;
                point = hb ? hb.healthComponent.body.footPosition : h.point;
                break;
            }
            var from = body.footPosition;
            var flat = point - from; flat.y = 0;
            if (flat.magnitude > range) point = from + flat.normalized * range + Vector3.up * (point.y - from.y);
            if (DlrPuddle.Floor(point + Vector3.up * 2f, out var floor)) point = floor;
            return point;
        }

        public static float RadiusAt(SkillRow row, Vector3 point) => row.Radius * (row.PuddleMult > 0 && DlrPuddle.On(point) ? row.PuddleMult : 1f);

        public override void Update()
        {
            base.Update();
            if (!ring) return;
            var p = AimPoint(characterBody, row.Range);
            ring.transform.position = p;
            float r = RadiusAt(row, p);
            if (r != shown) { shown = r; ring.transform.localScale = Vector3.one * r / Mathf.Max(0.1f, row.Radius); }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!isAuthority) return;
            characterBody.SetAimTimer(2f);
            // test mode and the DPS bench have no player to click: the punch lands under the crosshair shortly after the ring is up
            bool bot = MeltyTest.Active || DpsBench.Active;
            if (inputBank && inputBank.skill1.justPressed || bot && fixedAge >= 0.5f)
                outer.SetNextState(new PunchStrike { RowOverride = row, Point = AimPoint(characterBody, row.Range) });
            else if (!bot && inputBank && (inputBank.skill2.justPressed || inputBank.skill4.justPressed) && fixedAge > 0.15f)
            {
                RefundStock();
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            Vfx.Stop(ring, 0.1f);
            DeadlockAudio.Stop(loop);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    /// <summary>Puddle Punch, step 2: the punch at the placed point (DlrPunch on every machine), its radius doubled when the owner
    /// saw a puddle there; point and radius travel with the state.</summary>
    public sealed class PunchStrike : DlrState
    {
        public Vector3 Point; float radius;

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority) radius = PlacePunch.RadiusAt(row, Point);
            if (anim) anim.Gesture("special");
            characterBody.SetAimTimer(1f);
            DlrPunch.Plant(characterBody, row, Point, radius > 0 ? radius : row.Radius, Server);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= 0.3f) outer.SetNextStateToMain();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(Point);
            writer.Write(radius);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            Point = reader.ReadVector3();
            radius = reader.ReadSingle();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Viscous: Goo Ball (Q, the ult equipment) ----------------
    /// <summary>Whoever fires it becomes a huge ball of goo for duration (any survivor): the ball (Deadlock's sphere, see-through) wraps
    /// the carrier, who stays visible inside walking where it rolls. The row's buff (Goo Ball) gives the top speed (speed m/s), little
    /// grip (it gathers speed and turns slowly: momentum), a high double jump and armor; steering works in the air too, it rolls
    /// faster downhill, bounces off walls and gives a little bounce on landing. Smashing into an enemy (once per interval each):
    /// damageMinPct to damagePct by speed, a stun, a launch away and up (bosses x bossMult), Gooed to buffStacks; each smash costs
    /// 1 - moveSpeedMult of the speed. Landing after a jump slams everything within radius for splashPct. No other moves; Q again
    /// ends it. Runs on the Weapon machine, so the carrier's own main state keeps walking, jumping and animating it.</summary>
    [Kind("ult_gooball")]
    public sealed class UltGooBall : UltState
    {
        GameObject ball; Transform spin; int loop; float radius, airControl, wallUntil, nextBounce;
        bool hadNoFall, locked, hopped, airborne;
        Vector3 lastPos, seenVel;
        readonly Dictionary<HealthComponent, float> nextHit = new Dictionary<HealthComponent, float>();
        readonly Dictionary<HealthComponent, float> nextSlow = new Dictionary<HealthComponent, float>();

        /// <summary>The ball's radius for a carrier: at least half the row's range (its diameter), and big enough to wrap the body.</summary>
        public static float RadiusFor(CharacterBody b, SkillRow row)
        {
            var cap = b ? b.GetComponent<CapsuleCollider>() : null;
            float h = cap ? cap.height : 2f, w = cap ? cap.radius * 2f : 1f;
            return Mathf.Max(row.Range * 0.5f, Mathf.Max(h, w) * 0.62f);
        }

        Vector3 Centre => characterBody.footPosition + Vector3.up * radius;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            radius = RadiusFor(characterBody, row);
            if (anim) anim.Override("ult_start", 0.6f);
            ball = Vfx.Play(row.Vfx, "loop", Centre, radius);
            spin = ball ? ball.transform.Find("spin") : null;
            Vfx.Play(row.Vfx, "cast", Centre, radius);
            hadNoFall = (characterBody.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0;
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            lastPos = transform.position;
            if (Server) Buffs.AddTimed(characterBody, row.Buff, row.Duration + 0.5f);
            if (isAuthority && characterMotor)
            {
                airControl = characterMotor.airControl;
                characterMotor.airControl = 1f;
                characterMotor.onHitGroundAuthority += OnLand;
                characterMotor.onMovementHit += OnWall;
                LockSkills(true); locked = true;
            }
        }

        bool Bot => MeltyTest.Active || DpsBench.Active;

        public override void Update()
        {
            base.Update();
            if (!ball) return;
            // the ball sits on the ground around the carrier and rolls the way it moves
            ball.transform.position = Centre;
            var v = isAuthority && characterMotor ? characterMotor.velocity : seenVel;
            var flat = new Vector3(v.x, 0f, v.z);
            if (spin && flat.sqrMagnitude > 0.01f)
                spin.Rotate(Vector3.Cross(Vector3.up, flat.normalized), flat.magnitude / Mathf.Max(0.5f, radius) * Mathf.Rad2Deg * Time.deltaTime, Space.World);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            float dt = Time.fixedDeltaTime;
            seenVel = Vector3.Lerp(seenVel, (transform.position - lastPos) / dt, 0.5f);
            lastPos = transform.position;
            if (isAuthority && characterMotor)
            {
                characterBody.isSprinting = false;
                if (Bot)
                {
                    // test mode and the bench steer at the crosshair (the targets are in front)
                    var aim = GetAimRay().direction; aim.y = 0;
                    if (inputBank) inputBank.moveVector = aim.normalized;
                    characterMotor.moveDirection = aim.normalized;
                }
                // downhill it rolls faster: gravity along the slope it stands on
                if (characterMotor.isGrounded && Physics.Raycast(characterBody.footPosition + Vector3.up * 0.5f, Vector3.down, out var g, 1.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                {
                    var along = Vector3.ProjectOnPlane(Physics.gravity, g.normal); along.y = 0f;
                    characterMotor.velocity += along * dt;
                }
                // each smash costs some speed (the owner judges what it rolls into)
                foreach (var hb in Fx.Enemies(characterBody, Centre, radius + 0.6f))
                {
                    if (nextSlow.TryGetValue(hb.healthComponent, out var t) && t > fixedAge) continue;
                    nextSlow[hb.healthComponent] = fixedAge + row.Interval;
                    characterMotor.velocity = new Vector3(characterMotor.velocity.x * row.MoveSpeedMult, characterMotor.velocity.y, characterMotor.velocity.z * row.MoveSpeedMult);
                }
                if (fixedAge >= row.Duration || !Bot && fixedAge > 0.5f && inputBank && inputBank.activateEquipment.justPressed)
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }
            if (Server) ServerHits();
        }

        /// <summary>The server's smashes and slam, from how the body really moves (a joined player's ball is moved on their PC).</summary>
        void ServerHits()
        {
            var v = isAuthority && characterMotor ? characterMotor.velocity : seenVel;
            var flat = new Vector3(v.x, 0f, v.z);
            float speed = flat.magnitude;
            foreach (var hb in Fx.Enemies(characterBody, Centre, radius + 0.6f))
            {
                var hc = hb.healthComponent;
                if (nextHit.TryGetValue(hc, out var t) && t > fixedAge) continue;
                nextHit[hc] = fixedAge + row.Interval;
                var b = hc.body;
                var at = b ? b.corePosition : hb.transform.position;
                float k = Mathf.Clamp01(speed / Mathf.Max(1f, row.Speed));
                Fx.Hit(characterBody, hb, Mathf.Lerp(row.DamageMinPct, row.DamagePct, k) / 100f, row.ProcCoefficient, at);
                Fx.Stun(hc, row.StunDuration);
                if (b && row.Debuff != "none") Buffs.AddStacks(characterBody, b, row.Debuff, Mathf.Max(1, (int)row.BuffStacks));
                if (b && row.Knockback > 0)
                {
                    var away = speed > 1f ? flat / speed : (at - Centre).normalized;
                    away.y = 0f;
                    float push = row.Knockback * Mathf.Max(0.3f, k) * (b.isChampion ? row.BossMult : 1f);
                    float mass = b.characterMotor ? b.characterMotor.mass : b.rigidbody ? b.rigidbody.mass : 100f;
                    if (b.characterMotor) b.characterMotor.Motor.ForceUnground();
                    hc.TakeDamageForce((away.normalized + Vector3.up * 0.6f).normalized * push * mass, true, true);
                }
                Relay.Vfx(row.Vfx, "hit", at, radius);
                Relay.Sound(row.Id, "hit", at);
            }
            // a jump (rising faster than a bounce can throw it) then a landing: the slam
            bool grounded = Physics.Raycast(characterBody.footPosition + Vector3.up * 0.3f, Vector3.down, 0.8f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
            if (v.y > characterBody.jumpPower * 0.7f) hopped = true;
            if (!grounded) airborne = true;
            if (hopped && airborne && grounded && v.y <= 1f)
            {
                hopped = airborne = false;
                Slam();
            }
            else if (grounded && v.y <= 1f) airborne = false;
        }

        void Slam()
        {
            var at = characterBody.footPosition;
            Relay.Vfx(row.Vfx, "slam", at, row.Radius);
            Relay.Sound(row.Id + "/bounce", "hit", at);
            Fx.Blast(characterBody, at + Vector3.up * 0.5f, row.Radius, row.SplashPct / 100f, row.ProcCoefficient);
            foreach (var hb in Fx.Enemies(characterBody, at, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                if (row.Debuff != "none") Buffs.AddStacks(characterBody, b, row.Debuff, Mathf.Max(1, (int)row.BuffStacks));
                if (!b.isChampion) Fx.KnockBack(characterBody, hb.healthComponent, b.corePosition - at, row.Knockback * 0.5f);
            }
            MeltyTest.Report("gooball-slam", true, characterBody.baseNameToken);
        }

        /// <summary>The owner's landing: a falling ball bounces back up a little (less every time, gone below a few m/s).</summary>
        void OnLand(ref CharacterMotor.HitGroundInfo info)
        {
            if (!characterMotor || fixedAge < 0.2f || Time.time < nextBounce) return;
            float fall = -info.velocity.y;
            if (fall < 8f) return;
            nextBounce = Time.time + 0.1f;
            characterMotor.Motor.ForceUnground();
            var v = characterMotor.velocity;
            characterMotor.velocity = new Vector3(v.x, fall * 0.4f, v.z);
            Vfx.Play(row.Vfx, "bounce", characterBody.footPosition, radius);
            Fx.Sound(row.Id + "/bounce", "cast", characterBody.footPosition);
        }

        /// <summary>The owner rolled into a wall: the ball bounces off it (horizontal speed reflected, a little lost).</summary>
        void OnWall(ref CharacterMotor.MovementHitInfo info)
        {
            if (!characterMotor || Mathf.Abs(info.hitNormal.y) > 0.4f || Time.time < wallUntil) return;
            var v = characterMotor.velocity;
            var flat = new Vector3(v.x, 0f, v.z);
            var n = new Vector3(info.hitNormal.x, 0f, info.hitNormal.z);
            if (flat.magnitude < 4f || n.sqrMagnitude < 0.01f || Vector3.Dot(flat, n) >= 0f) return;
            wallUntil = Time.time + 0.15f;
            var r = Vector3.Reflect(flat, n.normalized) * 0.8f;
            characterMotor.velocity = new Vector3(r.x, v.y, r.z);
            Vfx.Play(row.Vfx, "bounce", info.hitPoint, radius * 0.6f);
            Fx.Sound(row.Id + "/bounce", "cast", info.hitPoint);
        }

        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            Sound("end");
            Vfx.Stop(ball, 0.2f);
            if (characterBody) Vfx.Play(row.Vfx, "end", Centre, radius);
            if (anim) anim.StopOverride();
            if (characterBody && !hadNoFall && !Survivors.NoFallDamage(characterBody)) characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            if (isAuthority && characterMotor)
            {
                characterMotor.airControl = airControl;
                characterMotor.onHitGroundAuthority -= OnLand;
                characterMotor.onMovementHit -= OnWall;
            }
            if (locked) LockSkills(false);
            if (Server) Buffs.Clear(characterBody, row.Buff);
            base.OnExit();
        }
    }
}
