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
            if (!thrown && fixedAge >= dur * 0.4f) Throw();
            if (fixedAge >= dur && isAuthority) outer.SetNextStateToMain();
        }

        void Throw()
        {
            thrown = true;
            // from the aim origin, not the hand: a hand inside a wall or the floor (aiming down, hugging cover) made the
            // dagger hit the world on its first frame and vanish unseen
            var aim = GetAimRay();
            // every machine flies its own dagger from the aim it sees; only the server's deals damage
            DlrProjectile.Launch(characterBody, row, aim.origin, ThrowDir, Server);
        }

        public override void OnExit()
        {
            // cut short before the throw (another skill, a stun): the dagger still flies, the cooldown is already spent
            if (!thrown && row != null) Throw();
            base.OnExit();
        }
    }

    // ---------------- buff on self: Smoke Bomb ----------------
    [Kind("self_buff")]
    public sealed class SelfBuff : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("utility");
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

    // ---------------- Abrams: Siphon Life (Mired Urn) ----------------
    [Kind("mired_urn")]
    public sealed class MiredUrn : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("secondary");
            // the drain's loop sound for every player; the urn itself is RoR2's networked attachment, spawned by the server
            TimedLoop.Run(gameObject, row, row.Duration, null);
            if (!Server) return;
            Buffs.AddTimed(characterBody, row.Buff, row.Duration);
            // RoR2's own Mired Urn attachment: tar tethers to the nearest enemies, drains them and heals Abrams for all of it
            var p = Fx.Prefab(row.Effect);
            if (!p) return;
            var go = Object.Instantiate(p, transform.position, Quaternion.identity);
            var urn = go.GetComponent<SiphonNearbyController>();
            if (urn)
            {
                urn.Networkradius = row.Radius;
                urn.NetworkmaxTargets = (int)row.ChainCount;
                urn.damagePerSecondCoefficient = Coef;
                urn.tickRate = 1f / Mathf.Max(0.05f, row.Interval);
                urn.allowSiphonOutOfCombat = true;
            }
            go.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(gameObject);
            Object.Destroy(go, row.Duration);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.3f && isAuthority) outer.SetNextStateToMain();
        }
    }

    /// <summary>Ends a skill's loop sound (and plays its end sound) when the object it belongs to goes.</summary>
    public sealed class StopSoundOnDestroy : MonoBehaviour
    {
        public int Loop; public string EndOf;
        void OnDestroy()
        {
            DeadlockAudio.Stop(Loop);
            if (EndOf != null) Fx.Sound(EndOf, "end", transform.position);
        }
    }

    // ---------------- Abrams: Shoulder Charge ----------------
    [Kind("dash_charge")]
    public sealed class DashCharge : DlrState
    {
        Vector3 dir;
        readonly HashSet<HealthComponent> hit = new HashSet<HealthComponent>();
        Vector3 lastPos;
        float stuckTime;
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            dir = ThrowDir; dir.y = 0; dir.Normalize();
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
            // the owner steers; the server and the other players follow the way the body really moves
            dir = MotionDir(dir);
            if (Server)
            {
                foreach (var hb in Fx.Enemies(characterBody, transform.position + dir * 1.2f, row.Radius))
                {
                    if (!hit.Add(hb.healthComponent)) continue;
                    Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, hb.transform.position);
                    Fx.Stun(hb.healthComponent, row.StunDuration);
                    // knocked to the side of his path, out of his way
                    Fx.KnockAside(characterBody, hb.healthComponent, dir, row.Knockback);
                    Relay.Impact(row.Effect, hb.transform.position);
                    Relay.Sound(row.Id, "hit", hb.transform.position);
                }
            }
            // wall (the owner judges it): we stopped moving although we push forward; the charge ends there
            float moved = Vector3.Distance(transform.position, lastPos);
            lastPos = transform.position;
            if (fixedAge > 0.25f && moved < row.Speed * Time.fixedDeltaTime * 0.15f) stuckTime += Time.fixedDeltaTime; else stuckTime = 0;
            if (stuckTime > 0.08f && isAuthority)
            {
                outer.SetNextStateToMain();
                return;
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            // ended early (a wall, a stun): the thud, on every player's machine
            if (row != null && fixedAge < row.Duration - 0.05f)
            {
                Sound("end");
                Fx.Impact(row.Effect, transform.position + dir);
            }
            if (anim) anim.StopOverride();
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
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
                DlrProjectile.Launch(characterBody, row, MuzzlePos(), ThrowDir, Server);
            }
            if (fixedAge >= dur && isAuthority) outer.SetNextStateToMain();
        }
    }

    // ---------------- Seven: Static Charge ----------------
    [Kind("delayed_blast")]
    public sealed class DelayedBlast : DlrState
    {
        // picked by the owner from its aim and sent with the state, so every machine plants it in the same place
        Vector3 point; GameObject stuckTo;

        public override void OnEnter()
        {
            base.OnEnter();
            if (anim) anim.Gesture("utility");
            Sound("cast");
            characterBody.SetAimTimer(2f);
            if (isAuthority)
            {
                // first world or enemy hit along the aim; Seven's own hurtboxes (and allies') are skipped
                var aim = GetAimRay();
                point = aim.origin + aim.direction * row.Range;
                var hits = Physics.SphereCastAll(aim, 0.5f, row.Range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    var hb = hit.collider.GetComponent<HurtBox>();
                    if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == teamComponent.teamIndex)) continue;
                    point = hit.distance > 0 ? hit.point : hit.collider.ClosestPoint(aim.origin);
                    if (hb) stuckTo = hb.healthComponent.body.gameObject;
                    break;
                }
            }
            Transform follow = null;
            var sb = stuckTo ? stuckTo.GetComponent<CharacterBody>() : null;
            if (sb) follow = sb.coreTransform ? sb.coreTransform : sb.transform;
            DlrDelayed.Plant(characterBody, row, point, follow, Server);
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(point);
            writer.Write(stuckTo);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            point = reader.ReadVector3();
            stuckTo = reader.ReadGameObject();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.25f && isAuthority) outer.SetNextStateToMain();
        }
    }

    // ---------------- Infernus: Napalm ----------------
    [Kind("lob")]
    public sealed class ThrowLob : DlrState
    {
        bool thrown;
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
            if (!thrown && fixedAge >= dur * 0.3f)
            {
                thrown = true;
                DlrLob.Launch(characterBody, row, MuzzlePos(), ThrowDir, Server);
            }
            if (fixedAge >= dur && isAuthority) outer.SetNextStateToMain();
        }
    }

    // ---------------- Infernus: Flame Dash ----------------
    [Kind("dash_trail")]
    public sealed class DashTrail : DlrState
    {
        DamageTrail trail; int loop; GameObject flames; Vector3 lastPatch, heading = Vector3.forward; float nextPatch;
        readonly HashSet<HealthComponent> shoved = new HashSet<HealthComponent>();
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            flames = Vfx.Play(row.Vfx, "loop", characterBody.footPosition, 1f, transform);
            lastPatch = characterBody.footPosition + Vector3.up * 100f;
            if (anim) anim.Override("utility_loop", row.Duration, true);
            if (Server)
            {
                Buffs.AddTimed(characterBody, row.Buff, row.Duration);
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
                        trail.pointLifetime = row.ChainCount; // how long the flames stay on the ground (chainCount column)
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
            // burning patches of ground along the path, lasting as long as the trail does (chainCount seconds)
            if (fixedAge >= nextPatch && Vector3.Distance(characterBody.footPosition, lastPatch) > 1.5f)
            {
                nextPatch = fixedAge + 0.12f;
                lastPatch = characterBody.footPosition;
                Vfx.Play(row.Vfx, "hit", lastPatch, row.ChainCount);
            }
            // enemies Infernus runs into are knocked aside to clear his path, once each
            var face = characterDirection ? characterDirection.forward : transform.forward; face.y = 0;
            heading = MotionDir(face.sqrMagnitude > 0.01f ? face.normalized : heading);
            if (Server && row.Knockback > 0)
            {
                var fwd = heading;
                foreach (var hb in Fx.Enemies(characterBody, transform.position + fwd * 1.2f, characterBody.radius + 1.5f))
                {
                    if (!shoved.Add(hb.healthComponent)) continue;
                    // scorched on the way past (one tick of the trail), since the shove throws them out of the flames
                    Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, hb.transform.position);
                    Fx.KnockAside(characterBody, hb.healthComponent, fwd, row.Knockback);
                    Relay.Sound(row.Id, "hit", hb.transform.position);
                }
            }
            // a tap dashes for minDuration; holding Shift keeps going up to duration
            bool held = inputBank && inputBank.skill3.down;
            if (isAuthority && (fixedAge >= row.Duration || fixedAge >= row.MinDuration && !held)) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            if (Server) Buffs.Clear(characterBody, row.Buff);
            if (trail) { trail.active = false; Object.Destroy(trail.gameObject, row.ChainCount + 0.5f); }
            Vfx.Stop(flames, 0.6f);
            DeadlockAudio.Stop(loop);
            Sound("end");
            if (anim) anim.StopOverride();
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}
