using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ---------------- Mina: Rake ----------------
    /// <summary>Mina sweeps her umbrella through a 180 deg arc (spreadDeg either side of her aim) after minDuration: everything
    /// within radius takes damagePct, she heals healFraction health (a flat amount) per enemy hit, and a monster (not a boss)
    /// left under damageMinPct % health dies outright. Works in the air. Damage, heal and execute on the server; the swing,
    /// the crescent, the sparks and the motes flying back to her on every machine.</summary>
    [Kind("rake_arc")]
    public sealed class RakeArc : DlrState
    {
        bool swung;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            // Deadlock's Rake clip (0.93 s) fitted to the move
            if (anim) anim.Gesture("secondary", 1.4f);
            var dir = ThrowDir;
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, row.Radius, characterBody.transform, characterBody.corePosition + dir);
            // the umbrella arm flares the skirt a little as she turns into the swing
            if (anim && anim.Springs) anim.Springs.Kick(Vector3.Cross(Vector3.up, dir) * 3f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!swung && fixedAge >= row.MinDuration) Swing();
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }

        void Swing()
        {
            swung = true;
            var core = characterBody.corePosition;
            var dir = ThrowDir.normalized;
            Vfx.Play(row.Vfx, "swing", core, row.Radius, characterBody.transform, core + dir);
            var hits = new List<HurtBox>();
            foreach (var hb in Fx.Enemies(characterBody, core, row.Radius + 1f))
            {
                var b = hb.healthComponent.body;
                var at = b ? b.corePosition : hb.transform.position;
                var to = at - core;
                if (to.magnitude > row.Radius + (b ? b.radius : 0.5f)) continue;
                if (Vector3.Angle(dir, to) > row.SpreadDeg) continue;
                hits.Add(hb);
            }
            bool executed = false;
            foreach (var hb in hits)
            {
                var hc = hb.healthComponent;
                var at = hc.body ? hc.body.corePosition : hb.transform.position;
                Vfx.Play(row.Vfx, "hit", at, 1f, null, core);
                if (!Server) continue;
                Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, at, new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Secondary });
                // Deadlock's execute on non-heroes: a monster left this low dies outright (never a boss)
                if (hc.alive && hc.body && !hc.body.isChampion && hc.combinedHealthFraction < row.DamageMinPct / 100f)
                {
                    hc.Suicide(characterBody.gameObject, characterBody.gameObject);
                    executed = true;
                    Relay.Vfx(row.Vfx, "execute", at, 1f, null, core);
                }
            }
            if (hits.Count > 0)
            {
                Sound("hit");
                if (Server && healthComponent) healthComponent.Heal(row.HealFraction * hits.Count, default);
                MeltyTest.Report("rake-hits", true, characterBody.baseNameToken);
            }
            if (executed) Relay.Sound(row.Id, "end", core);
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Mina: Sanguine Retreat ----------------
    /// <summary>Mina bursts into a swarm of bats and flies to the point she looks at, up to range metres, untouchable on the way
    /// (vanilla HiddenInvincibility and Cloak, so monsters lose her), over duration seconds; walls stop the flight just short; she
    /// reforms launchHeight metres above the ground there, ready to float. For interval seconds after the first, Shift again
    /// recasts it (chainCount times); the cooldown starts when that window ends (<see cref="MinaRecast"/>). The owner picks the
    /// point and it travels with the state. Used during Nox Nostra's flight, the flight carries on afterwards (NoxLeft).</summary>
    [Kind("bat_blink")]
    public sealed class BatBlink : DlrState
    {
        /// <summary>A recast (no new recast window after it).</summary>
        public bool Recast;
        /// <summary>Seconds of Nox Nostra's flight left when this started from it (0: none), and whether its swarm was planted.</summary>
        public float NoxLeft;
        public bool NoxPlanted;
        Vector3 to, from;
        bool hidden, floating, ended;
        int loop;
        GameObject swarm, marker;
        CharacterModel model;

        public override void OnEnter()
        {
            base.OnEnter();
            from = characterBody.corePosition;
            if (isAuthority) to = Target();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            model = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            if (model) { model.invisibilityCount++; hidden = true; }
            if (Server)
            {
                characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, row.Duration + 0.15f);
                characterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, row.Duration + 0.5f);
            }
            if (characterMotor)
            {
                characterMotor.Motor.ForceUnground();
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
            Vfx.Play(row.Vfx, "cast", from, 1f, characterBody.transform);
            swarm = Vfx.Play(row.Vfx, "loop", from, Mathf.Max(0.6f, characterBody.radius), characterBody.coreTransform ? characterBody.coreTransform : transform);
            if (isAuthority) marker = Vfx.Play(row.Vfx, "aim", to, 1f);
            MeltyTest.Report("bat-blink", true, characterBody.baseNameToken);
        }

        /// <summary>Where she reforms: along the aim, up to range metres, short of any wall, at least launchHeight above the ground.</summary>
        Vector3 Target()
        {
            var aim = GetAimRay();
            var dir = aim.direction.normalized;
            float dist = row.Range;
            var start = characterBody.corePosition;
            if (Physics.SphereCast(start, 0.5f, dir, out var hit, row.Range, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0f, hit.distance - 0.8f);
            var p = start + dir * dist;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var g, row.LaunchHeight + 0.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                p.y = g.point.y + row.LaunchHeight;
            return p;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterMotor.moveDirection = Vector3.zero;
                float left = Mathf.Max(Time.fixedDeltaTime, row.Duration - fixedAge);
                var want = (to - characterBody.corePosition) / left;
                if (want.magnitude > row.Range / row.Duration * 2.5f) want = want.normalized * row.Range / row.Duration * 2.5f;
                characterMotor.velocity = want;
                var flat = want; flat.y = 0;
                if (characterDirection && flat.sqrMagnitude > 0.1f) characterDirection.forward = flat.normalized;
            }
            if (marker && isAuthority) marker.transform.position = to;
            if (fixedAge >= row.Duration && isAuthority)
            {
                if (NoxLeft > 0f)
                {
                    // back to Nox Nostra's flight for what was left of it
                    var nox = Sheet.SkillsOf(Survivors.HeroOf(characterBody)).FirstOrDefault(r => r.Kind == "nox_flight");
                    if (nox != null) { outer.SetNextState(new NoxFlight { RowOverride = nox, Left = NoxLeft, Planted = NoxPlanted }); return; }
                }
                outer.SetNextStateToMain();
            }
        }

        void End()
        {
            if (ended) return;
            ended = true;
            DeadlockAudio.Stop(loop);
            Sound("end");
            if (hidden && model) model.invisibilityCount--;
            hidden = false;
            if (floating && characterMotor)
            {
                floating = false;
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
            }
            // she reforms with a little lift, ready to open the umbrella
            if (isAuthority && characterMotor) characterMotor.velocity = Vector3.up * 2f;
            Vfx.Play(row.Vfx, "end", characterBody.corePosition, 1f, characterBody.transform);
            var bats = swarm ? swarm.GetComponentInChildren<BatSwarm>() : null;
            if (bats) bats.Stop(5f);
            Vfx.Stop(swarm, 0.6f); swarm = null;
            Vfx.Stop(marker, 0.1f); marker = null;
            if (Server) characterBody.ClearTimedBuffs(RoR2Content.Buffs.HiddenInvincibility);
            if (anim && anim.Springs) anim.Springs.Reset();
        }

        public override void OnExit()
        {
            End();
            // the recast window (the first cast only): Shift again within interval seconds; the cooldown waits for it
            if (isAuthority && !Recast && row.ChainCount > 0 && row.Interval > 0f)
            {
                var rc = characterBody.GetComponent<MinaRecast>() ?? characterBody.gameObject.AddComponent<MinaRecast>();
                rc.Begin(row, row.Interval, (int)row.ChainCount);
            }
            else if (isAuthority && Recast)
            {
                var rc = characterBody.GetComponent<MinaRecast>();
                if (rc) rc.Used();
            }
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(to); writer.Write(Recast); writer.Write(NoxLeft); writer.Write(NoxPlanted);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            to = reader.ReadVector3(); Recast = reader.ReadBoolean(); NoxLeft = reader.ReadSingle(); NoxPlanted = reader.ReadBoolean();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    /// <summary>Sanguine Retreat's recast window, on the owner: for its time, Shift again flies again (the key's own cooldown is
    /// held at its start meanwhile, so the 8 s begin when the window ends); then the window closes with Deadlock's last blink sound.</summary>
    public sealed class MinaRecast : MonoBehaviour
    {
        SkillRow row;
        float until;
        int left;
        CharacterBody body;

        public bool Open => row != null && Time.fixedTime < until && left > 0;

        public void Begin(SkillRow r, float window, int recasts)
        {
            body = GetComponent<CharacterBody>();
            row = r; until = Time.fixedTime + window; left = recasts;
        }

        /// <summary>A recast was used: one fewer left (none: the window closes and the cooldown starts).</summary>
        public void Used() { if (--left <= 0) until = 0f; }

        void FixedUpdate()
        {
            if (!body || row == null || !body.hasEffectiveAuthority) return;
            var gs = body.skillLocator ? body.skillLocator.utility : null;
            if (!Open)
            {
                Fx.Sound(row.Id, "reload", body.corePosition);
                row = null;
                return;
            }
            if (gs) gs.rechargeStopwatch = 0f;
            var esm = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
            bool pressed = body.inputBank && body.inputBank.skill3.justPressed;
            // test mode and the bench have no player to press again: they recast after a moment
            if ((MeltyTest.Active || DpsBench.Active) && Time.fixedTime > until - row.Interval + 0.4f) pressed = true;
            if (pressed && esm && esm.state is GenericCharacterMain)
            {
                esm.SetNextState(new BatBlink { RowOverride = row, Recast = true });
                MeltyTest.Report("bat-blink-recast", true, body.baseNameToken);
            }
        }
    }

    // ---------------- Mina: Nox Nostra ----------------
    /// <summary>Mina rises launchHeight metres and flies for duration with gravity off: the move keys at speed m/s, Jump climbs and
    /// Sprint sinks. A circle under the crosshair (up to range metres away, on the ground) shows where her bats will land; Left
    /// Click plants them (<see cref="NoxPlant"/>), and after that New Money fires again. Unplanted when the flight ends, the swarm
    /// drops on the circle by itself; then her umbrella opens and she drifts down. Rake works in the flight (another machine);
    /// Shift is caught here and becomes a Sanguine Retreat after which the flight carries on.</summary>
    [Kind("nox_flight")]
    public sealed class NoxFlight : DlrState
    {
        /// <summary>Resuming after a Sanguine Retreat: the flight time left and whether the swarm is already down.</summary>
        public float Left;
        public bool Planted;
        const float RiseTime = 0.6f;
        float length;
        bool ended, floating, toBlink, resumed;
        int loop;
        GameObject reticle, hint, lift;
        Vector3 target;

        public override void OnEnter()
        {
            base.OnEnter();
            resumed = Left > 0f;
            length = resumed ? Left : row.Duration;
            if (!resumed)
            {
                Sound("cast");
                if (anim) anim.Override("special_start", RiseTime);
                Vfx.Play(row.Vfx, "cast", characterBody.footPosition, 1f, characterBody.transform);
            }
            loop = Fx.Loop(row.Id, transform.position);
            if (anim) anim.Hold("special_loop");
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            if (characterMotor)
            {
                characterMotor.Motor.ForceUnground();
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
            // a few bats hold her up under the umbrella the whole flight
            lift = Vfx.Play(row.Vfx, "loop", characterBody.corePosition, 1f, characterBody.transform);
            if (isAuthority)
            {
                target = AimPoint();
                if (!Planted) reticle = Vfx.Play(row.Vfx, "aim", target, row.Radius);
                hint = FlightHint.Show(characterBody, length);
            }
        }

        /// <summary>The ground under the crosshair, at most range metres away (flat), or below the end of the aim.</summary>
        Vector3 AimPoint()
        {
            var aim = GetAimRay();
            var point = aim.origin + aim.direction * row.Range;
            if (Physics.Raycast(aim, out var hit, row.Range * 1.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) point = hit.point;
            var flat = point - transform.position; flat.y = 0;
            if (flat.magnitude > row.Range) point = transform.position + flat.normalized * row.Range;
            if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out var ground, 300f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) point = ground.point;
            return point;
        }

        bool Bot => MeltyTest.Active || DpsBench.Active;

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterBody.isSprinting = false;
                characterMotor.moveDirection = Vector3.zero;
                float vy;
                if (!resumed && fixedAge < RiseTime) vy = row.LaunchHeight / RiseTime;
                else
                {
                    // Jump (Space) climbs, Sprint (the key itself: Ctrl or Shift as the player bound it) sinks
                    bool sink = inputBank && inputBank.rawSprintInput.down;
                    int dir = (inputBank && inputBank.jump.down ? 1 : 0) - (sink ? 1 : 0);
                    vy = dir * row.Speed;
                }
                var move = inputBank ? inputBank.moveVector : Vector3.zero;
                characterMotor.velocity = new Vector3(move.x * row.Speed, vy, move.z * row.Speed);
                var aim = GetAimRay().direction; aim.y = 0f;
                if (characterDirection) characterDirection.moveVector = aim.sqrMagnitude > 0.09f ? aim.normalized : characterDirection.forward;

                target = AimPoint();
                // the bench's bot centres the circle on its pack, as a player puts it on a pack (aiming at the nearest enemy's chest
                // from up here put the circle behind the pack, or on whatever enemy floated nearest)
                if (DpsBench.PackCentre is Vector3 pc) target = pc;
                if (reticle) reticle.transform.position = target;
                if (!Planted)
                {
                    // the gun waits until the swarm is down: until then Left Click plants
                    if (rt != null) rt.GunBlockedUntil = Time.time + 0.15f;
                    bool click = inputBank && inputBank.skill1.justPressed && fixedAge >= row.MinDuration;
                    // test mode plants after a moment; the bench's bot plants as soon as she is up (a player plants before flying off)
                    if (click || MeltyTest.Active && fixedAge >= 1.2f || DpsBench.Active && (resumed || fixedAge >= RiseTime)) Plant();
                }
                // Shift: a Sanguine Retreat, and the flight carries on after it
                var util = skillLocator ? skillLocator.utility : null;
                var urow = Survivors.RowFor(util);
                // (test mode blinks once by itself, to check the flight carries on after it; the bench presses keys like a player)
                bool shift = inputBank && inputBank.skill3.justPressed || MeltyTest.Active && fixedAge >= 2.5f && fixedAge < 2.6f;
                if (shift && (resumed || fixedAge >= RiseTime) && urow != null && urow.Kind == "bat_blink" && util.stock > 0 && fixedAge < length - 0.3f)
                {
                    util.DeductStock(1);
                    toBlink = true;
                    outer.SetNextState(new BatBlink { RowOverride = urow, NoxLeft = Mathf.Max(0.3f, length - fixedAge), NoxPlanted = Planted });
                    return;
                }
                if (fixedAge >= length)
                {
                    if (!Planted) Plant();
                    outer.SetNextStateToMain();
                }
            }
        }

        void Plant()
        {
            if (Planted) return;
            Planted = true;
            Vfx.Stop(reticle, 0.1f); reticle = null;
            var weapon = EntityStateMachine.FindByCustomName(gameObject, "Weapon");
            if (weapon) weapon.SetNextState(new NoxPlant { RowOverride = row, Point = target });
        }

        void End()
        {
            if (ended) return;
            ended = true;
            DeadlockAudio.Stop(loop);
            Vfx.Stop(reticle, 0.1f); reticle = null;
            if (hint) Object.Destroy(hint);
            var bats = lift ? lift.GetComponentInChildren<BatSwarm>() : null;
            if (bats) bats.Stop(4f);
            Vfx.Stop(lift, 0.5f); lift = null;
            if (floating && characterMotor)
            {
                floating = false;
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
            }
        }

        public override void OnExit()
        {
            End();
            if (anim) { anim.StopOverride(); if (!toBlink) anim.Hold(null); }
            if (!toBlink)
            {
                Sound("end");
                // the flight is over: her umbrella opens and she drifts down (no fall damage until she lands)
                var mr = characterBody.GetComponent<MinaRuntime>();
                if (mr && isAuthority) mr.AutoFloat = true;
                if (!Survivors.NoFallDamage(characterBody) && characterBody.GetComponent<SoftLanding>() == null) characterBody.gameObject.AddComponent<SoftLanding>();
            }
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(Left); writer.Write(Planted);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            Left = reader.ReadSingle(); Planted = reader.ReadBoolean();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    /// <summary>Nox Nostra's swarm planted at Point (the owner's circle, sent with the state): on every machine the bats pour down
    /// into it (<see cref="NoxSwarm"/>), on the server they bite. A moment on the Weapon machine, then back to the gun.</summary>
    public sealed class NoxPlant : DlrState
    {
        public Vector3 Point;

        public override void OnEnter()
        {
            base.OnEnter();
            Fx.Sound(row.Id, "reload", characterBody.corePosition);
            var swarm = NoxSwarm.Make(characterBody, row, Point);
            MeltyTest.Report("nox-planted", true, characterBody.baseNameToken);
            if (MeltyTest.Active || DpsBench.Active)
                Log.Info($"nox nostra: planted {Vector3.Distance(characterBody.footPosition, Point):F1} m from her ({characterBody.footPosition.y - Point.y:F1} m above it), {swarm.Count} enemies inside");
            if (MeltyTest.Active || DpsBench.Active)
                Log.Info("nox nostra: enemies near the circle (flat m, height m): " + string.Join(" ", Fx.Enemies(characterBody, Point, 25f).Select(h =>
                {
                    var b = h.healthComponent.body; var f = (b ? b.footPosition : h.transform.position) - Point;
                    return $"{new Vector2(f.x, f.z).magnitude:F1}/{f.y:F1}";
                })));
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= 0.1f) outer.SetNextStateToMain();
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(Point); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); Point = reader.ReadVector3(); }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }

    /// <summary>Nox Nostra's swarm on the ground: dotTime seconds, a circle of radius, reach metres tall (fliers low over it are
    /// caught too). Every interval everything inside takes damagePct (server). Every machine draws the bats swirling low over it
    /// and diving at what is inside, and plays the bites.</summary>
    public sealed class NoxSwarm : MonoBehaviour
    {
        CharacterBody owner;
        SkillRow row;
        float age, next, nextBite;
        GameObject fx;
        BatSwarm bats;
        int loop;

        public static NoxSwarm Make(CharacterBody owner, SkillRow row, Vector3 at)
        {
            var go = new GameObject("DlrNoxSwarm");
            go.transform.position = at;
            var s = go.AddComponent<NoxSwarm>();
            s.owner = owner; s.row = row;
            s.fx = Vfx.Play(row.Vfx, "swarm", at, row.Radius, null, null, 0f, row.DotTime);
            s.bats = s.fx ? s.fx.GetComponentInChildren<BatSwarm>() : null;
            s.loop = DeadlockAudio.Play(Sheet.Sound(row.Id)?.Loop, at, true, Sheet.Sound(row.Id)?.Gain ?? 1f);
            return s;
        }

        int bites, ticks;
        /// <summary>Enemies inside the circle now (the bench and test log it).</summary>
        public int Count => Inside().Count;

        List<HurtBox> Inside()
        {
            var p = transform.position;
            var list = new List<HurtBox>();
            foreach (var hb in Fx.Enemies(owner, p + Vector3.up * row.Reach * 0.5f, row.Radius + row.Reach))
            {
                var b = hb.healthComponent.body;
                var at = b ? b.footPosition : hb.transform.position;
                var flat = at - p; flat.y = 0;
                if (flat.magnitude > row.Radius + (b ? b.radius * 0.5f : 0f)) continue;
                if (at.y < p.y - 2f || at.y > p.y + row.Reach) continue;
                list.Add(hb);
            }
            return list;
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (!owner || age >= row.DotTime) { Destroy(gameObject); return; }
            if (age >= next)
            {
                next += row.Interval;
                var inside = Inside();
                ticks++; bites += inside.Count;
                if (NetworkServer.active)
                    foreach (var hb in inside)
                        Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, hb.transform.position,
                            new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Special });
                // a few bats dive at what is inside, with Deadlock's bite
                if (inside.Count > 0 && age >= nextBite)
                {
                    nextBite = age + 0.5f;
                    Fx.Sound(row.Id, "hit", transform.position);
                    foreach (var hb in inside.Take(4))
                    {
                        if (bats) bats.Lunge(hb.transform, 2, 0.4f);
                        Vfx.Play(row.Vfx, "hit", hb.transform.position);
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (MeltyTest.Active || DpsBench.Active) Log.Info($"nox nostra: swarm done, {ticks} ticks, {bites} bites ({(ticks > 0 ? bites / (float)ticks : 0f):F1} enemies a tick)");
            DeadlockAudio.Stop(loop);
            if (bats) bats.Stop(5f);
            Vfx.Stop(fx, 0.6f);
        }
    }

    // ---------------- Mina's item: Mist Form (Q, any survivor) ----------------
    /// <summary>Whoever fires it becomes a flock of furious bats for duration: the body hidden, untouchable (HiddenInvincibility and
    /// Cloak), gravity off, flying at speed m/s with the move keys, Jump up and Sprint down; no moves (every skill locked). Every enemy
    /// the flock passes through (the body's radius plus reach) takes damagePct, again after interval. Q again (after minDuration)
    /// reforms early. Reforming: splashPct to everything within radius with a push of knockback m/s, the body back, and no fall
    /// damage until it lands (Mina's umbrella opens). The flock's size follows the body (MUL-T makes a big cloud). Any survivor.</summary>
    [Kind("ult_mist")]
    public sealed class MistForm : UltState
    {
        bool hidden, floating, ended, locked;
        int loop;
        float nextLunge;
        GameObject flock, hint;
        CharacterModel model;
        readonly Dictionary<HealthComponent, float> nextHit = new Dictionary<HealthComponent, float>();

        float Size => Mathf.Max(1.4f, characterBody.radius * 2.6f);

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            model = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            if (model) { model.invisibilityCount++; hidden = true; }
            if (Server)
            {
                characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, row.Duration + 0.3f);
                characterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, row.Duration + 0.8f);
            }
            if (characterMotor)
            {
                characterMotor.Motor.ForceUnground();
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, Size);
            flock = Vfx.Play(row.Vfx, "loop", characterBody.corePosition, Size, characterBody.coreTransform ? characterBody.coreTransform : transform);
            if (isAuthority)
            {
                LockSkills(true); locked = true;
                hint = FlightHint.Show(characterBody, row.Duration);
            }
        }

        bool Bot => MeltyTest.Active || DpsBench.Active;

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterBody.isSprinting = false;
                characterMotor.moveDirection = Vector3.zero;
                Vector3 v;
                if (Bot)
                {
                    // no player to fly it: straight through the nearest enemy, again and again
                    var near = Fx.Enemies(characterBody, characterBody.corePosition, 40f, 1).FirstOrDefault();
                    var to = near ? near.transform.position - characterBody.corePosition : Vector3.zero;
                    v = to.sqrMagnitude > 0.5f ? to.normalized * row.Speed : GetAimRay().direction * row.Speed;
                }
                else
                {
                    var move = inputBank ? inputBank.moveVector : Vector3.zero;
                    bool sink = inputBank && inputBank.rawSprintInput.down;
                    int dir = (inputBank && inputBank.jump.down ? 1 : 0) - (sink ? 1 : 0);
                    v = new Vector3(move.x * row.Speed, dir * row.Speed, move.z * row.Speed);
                }
                characterMotor.velocity = v;
                var aim = GetAimRay().direction; aim.y = 0f;
                if (characterDirection) characterDirection.moveVector = aim.sqrMagnitude > 0.09f ? aim.normalized : characterDirection.forward;
                // (the bench presses Q over and over: for it, the flight runs its full time)
                bool again = !DpsBench.Active && inputBank && inputBank.activateEquipment.justPressed && fixedAge >= row.MinDuration;
                if (fixedAge >= row.Duration || again) { outer.SetNextStateToMain(); return; }
            }

            var core = characterBody.corePosition;
            // passing through: every enemy the flock touches, once per interval each (server), the bites and sparks everywhere
            if (Server)
            {
                float now = fixedAge;
                foreach (var hb in Fx.Enemies(characterBody, core, characterBody.radius + row.Reach + Size * 0.5f))
                {
                    var hc = hb.healthComponent;
                    if (nextHit.TryGetValue(hc, out var t) && now < t) continue;
                    nextHit[hc] = now + row.Interval;
                    var at = hc.body ? hc.body.corePosition : hb.transform.position;
                    Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, at, new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Equipment });
                    Relay.Vfx(row.Vfx, "hit", at, Size, hc.gameObject);
                    Relay.Sound(row.Id, "hit", at);
                }
            }
            // for show: bats keep lunging at anything within a few metres and snapping back
            if (fixedAge >= nextLunge)
            {
                nextLunge = fixedAge + 0.3f;
                var bats = flock ? flock.GetComponentInChildren<BatSwarm>() : null;
                if (bats) foreach (var hb in Fx.Enemies(characterBody, core, 6f + Size, 3)) bats.Lunge(hb.transform, 3, 0.35f);
            }
        }

        void Reform()
        {
            if (ended) return;
            ended = true;
            DeadlockAudio.Stop(loop);
            if (hint) Object.Destroy(hint);
            var at = characterBody.corePosition;
            var bats = flock ? flock.GetComponentInChildren<BatSwarm>() : null;
            if (bats) bats.Burst(row.Radius * 1.4f, 0.9f);
            Vfx.Stop(flock, 1f); flock = null;
            Vfx.Play(row.Vfx, "end", at, row.Radius);
            Sound("end"); Sound("reload");
            if (Server)
            {
                Fx.Blast(characterBody, at, row.Radius, row.SplashPct / 100f, row.ProcCoefficient,
                    new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Equipment });
                foreach (var hb in Fx.Enemies(characterBody, at, row.Radius)) Fx.KnockBack(characterBody, hb.healthComponent, hb.transform.position - at, row.Knockback * (hb.healthComponent.body && hb.healthComponent.body.isChampion ? 0.25f : 1f));
                characterBody.ClearTimedBuffs(RoR2Content.Buffs.HiddenInvincibility);
            }
            if (hidden && model) model.invisibilityCount--;
            hidden = false;
            if (floating && characterMotor)
            {
                floating = false;
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
            }
            if (locked) { LockSkills(false); locked = false; }
        }

        public override void OnExit()
        {
            Reform();
            // reformed in the air: no fall damage from the drop, for anyone; Mina's umbrella opens
            if (!Survivors.NoFallDamage(characterBody) && characterBody.GetComponent<SoftLanding>() == null) characterBody.gameObject.AddComponent<SoftLanding>();
            var mr = characterBody.GetComponent<MinaRuntime>();
            if (mr && isAuthority && characterMotor && !characterMotor.isGrounded) mr.AutoFloat = true;
            base.OnExit();
        }
    }
}
