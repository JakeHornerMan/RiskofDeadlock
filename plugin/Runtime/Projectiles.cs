using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    // Co-op: each of these flies (or sits) on every machine, launched by the skill state that runs everywhere; only the
    // server's copy (damage = true) deals damage, stuns and stacks. The other copies are the same flight, drawn and heard.

    /// <summary>Simulated projectile for Sleep Dagger (hit once: damage + sleep) and Lightning Ball
    /// (flies through enemies, bounces off walls and floors, and shocks everything around it until its lifetime ends).</summary>
    public sealed class DlrProjectile : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 dir; float travelled, age, nextTick; bool damage;
        GameObject ghost, vfx; int loop;
        // extra "pierce": the enemies this projectile has already gone through
        HashSet<HealthComponent> pierced;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, bool damage)
        {
            var go = new GameObject("DlrProjectile_" + row.Id);
            go.transform.position = from;
            var p = go.AddComponent<DlrProjectile>();
            p.owner = owner; p.row = row; p.dir = dir.normalized; p.damage = damage;
            p.ghost = Fx.Ghost(row.Effect, from, Quaternion.LookRotation(dir));
            if (p.ghost) p.ghost.transform.SetParent(go.transform, true);
            if (row.Kind == "projectile_orb") p.loop = Fx.Loop(row.Id, from);
            p.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform);
        }

        void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            float dt = Time.fixedDeltaTime;
            age += dt;
            bool orb = row.Kind == "projectile_orb";
            {
                float step = row.Speed * dt;
                var pos = transform.position;
                // the orb flies through enemies (it shocks them on the way) and only stops on the world
                int mask = orb ? (int)LayerIndex.world.mask : LayerIndex.world.mask | LayerIndex.entityPrecise.mask;
                if (Physics.SphereCast(pos, Mathf.Max(0.3f, orb ? 1.0f : row.Radius * 0.5f), dir, out var hit, step, mask, QueryTriggerInteraction.Ignore))
                {
                    var hb = hit.collider.GetComponent<HurtBox>();
                    bool enemy = hb && hb.healthComponent && hb.healthComponent.body && hb.healthComponent.body.teamComponent.teamIndex != owner.teamComponent.teamIndex;
                    if (hb && !enemy) { transform.position += dir * step; }
                    else if (enemy && !orb && row.Extra == "pierce")
                    {
                        // a piercing projectile hits each enemy once and flies on through it
                        if (pierced == null) pierced = new HashSet<HealthComponent>();
                        if (pierced.Add(hb.healthComponent)) HitEnemy(hb, hit.point);
                        transform.position += dir * step;
                    }
                    else if (orb)
                    {
                        // the orb bounces off the world: stop where the sphere touched, fly on along the reflection
                        transform.position = pos + dir * hit.distance + hit.normal * 0.05f;
                        dir = Vector3.Reflect(dir, hit.normal).normalized;
                    }
                    else
                    {
                        transform.position = hit.point;
                        if (enemy) HitEnemy(hb, hit.point);
                        Destroy(gameObject);
                        return;
                    }
                }
                else transform.position += dir * step;
                travelled += step;
                if (!orb && travelled > row.Range) { Destroy(gameObject); return; }
            }
            if (orb)
            {
                if (age >= nextTick)
                {
                    nextTick = age + row.Interval;
                    var enemies = Fx.Enemies(owner, transform.position, row.Radius);
                    foreach (var hb in enemies)
                    {
                        var v = hb.healthComponent.body;
                        if (damage)
                        {
                            Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, hb.transform.position);
                            // every shock builds the row's stacking debuff (Seven's Static)
                            if (row.Buff != "none" && v) Buffs.Stack(owner, v, row.Buff, false);
                        }
                        Vfx.Play(row.Vfx, "hit", transform.position, row.Radius, null, v ? v.corePosition : hb.transform.position);
                    }
                    if (enemies.Count > 0 && Mathf.Repeat(age, 0.5f) < row.Interval) Fx.Sound(row.Id, "hit", transform.position);
                }
                if (age >= row.Duration || travelled > row.Range)
                {
                    Fx.Sound(row.Id, "end", transform.position);
                    Fx.Impact("static_blast", transform.position, 2f);
                    Vfx.Play(row.Vfx, "end", transform.position, row.Radius);
                    Destroy(gameObject);
                }
            }
        }

        void HitEnemy(HurtBox hb, Vector3 at)
        {
            if (damage)
            {
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, at);
                Fx.Stun(hb.healthComponent, row.StunDuration);
            }
            Fx.Impact("stun", at);
            Fx.Sound(row.Id, "hit", at);
        }

        void OnDestroy()
        {
            DeadlockAudio.Stop(loop);
            Vfx.Stop(vfx);
        }
    }

    /// <summary>Static Charge: planted on an enemy (follows it) or a spot; discharges after the delay.</summary>
    public sealed class DlrDelayed : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Transform follow; float age; int loop; GameObject vfx; bool damage;
        public static void Plant(CharacterBody owner, SkillRow row, Vector3 at, Transform follow, bool damage)
        {
            var go = new GameObject("DlrDelayed_" + row.Id);
            go.transform.position = at;
            var d = go.AddComponent<DlrDelayed>();
            d.owner = owner; d.row = row; d.follow = follow; d.damage = damage;
            d.loop = Fx.Loop(row.Id, at);
            Fx.Impact("stun", at);
            d.vfx = Vfx.Play(row.Vfx, "cast", at, row.Radius, follow ? follow : go.transform);
        }
        void FixedUpdate()
        {
            if (follow) transform.position = follow.position;
            age += Time.fixedDeltaTime;
            if (age < row.Duration) return;
            if (owner)
            {
                Fx.Impact(row.Effect, transform.position, row.Radius);
                Vfx.Play(row.Vfx, "hit", transform.position, row.Radius);
                Fx.Sound(row.Id, "hit", transform.position);
                if (damage)
                {
                    Fx.Blast(owner, transform.position, row.Radius, row.DamagePct / 100f, row.ProcCoefficient);
                    foreach (var hb in Fx.Enemies(owner, transform.position, row.Radius)) Fx.Stun(hb.healthComponent, row.StunDuration);
                }
            }
            Destroy(gameObject);
        }
        void OnDestroy() { DeadlockAudio.Stop(loop); Vfx.Stop(vfx, 0.2f); }
    }

    /// <summary>Napalm: a flask thrown along the aim that falls under gravity and bursts on the first enemy or surface it
    /// touches: damage and the row's debuff to every enemy in the splash.</summary>
    public sealed class DlrLob : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 velocity; float age; GameObject vfx; bool damage;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, bool damage)
        {
            var go = new GameObject("DlrLob_" + row.Id);
            go.transform.position = from;
            var l = go.AddComponent<DlrLob>();
            l.owner = owner; l.row = row; l.velocity = dir.normalized * row.Speed; l.damage = damage;
            l.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform);
        }

        void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            var pos = transform.position;
            if (Step(owner, row, ref pos, ref velocity, ref age, Time.fixedDeltaTime)) Burst(pos);
            else transform.position = pos;
        }

        /// <summary>One tick of the flight: gravity, then the first enemy or surface on the way (it flies through its own team).
        /// True = it bursts at pos. Shared by the flask and the aim marker, so the marker shows exactly where it lands.</summary>
        static bool Step(CharacterBody owner, SkillRow row, ref Vector3 pos, ref Vector3 velocity, ref float age, float dt)
        {
            age += dt;
            velocity += Physics.gravity * dt;
            var step = velocity * dt;
            var hits = Physics.SphereCastAll(pos, 0.4f, step.normalized, step.magnitude, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == owner.teamComponent.teamIndex)) continue;
                if (h.distance > 0) pos = h.point;
                return true;
            }
            pos += step;
            return age * row.Speed > row.Range;
        }

        /// <summary>Where a flask thrown now would burst; points gets the path (every few ticks) for the aim marker.</summary>
        public static Vector3 Predict(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, List<Vector3> points)
        {
            var pos = from; var vel = dir.normalized * row.Speed; float age = 0f; int n = 0;
            points?.Clear(); points?.Add(pos);
            while (!Step(owner, row, ref pos, ref vel, ref age, Time.fixedDeltaTime))
                if (++n % 3 == 0) points?.Add(pos);
            points?.Add(pos);
            return pos;
        }

        void Burst(Vector3 at)
        {
            Fx.Impact(row.Effect, at, row.Radius);
            Vfx.Play(row.Vfx, "hit", at, row.Radius);
            Fx.Sound(row.Id, "hit", at);
            if (damage)
            {
                foreach (var hb in Fx.Enemies(owner, at, row.Radius))
                {
                    Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, hb.transform.position);
                    if (row.Buff != "none" && hb.healthComponent.body) Buffs.Apply(owner, hb.healthComponent.body, row.Buff);
                }
            }
            Destroy(gameObject);
        }

        void OnDestroy() => Vfx.Stop(vfx, 0.6f);
    }

    /// <summary>Thunderhead: a storm planted on the ground for duration. Every interval lightning strikes up to chainCount
    /// enemies inside radius; the extra object (RoR2's Warbanner ward) gives allies inside its buff. The strikes are picked
    /// at random by the server, so every player sees them through the relay; the clouds and sound are local.</summary>
    public sealed class DlrZone : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; float age, next; int loop; GameObject vfx, ward; bool damage;
        static readonly System.Random rng = new System.Random();

        public static void Plant(CharacterBody owner, SkillRow row, Vector3 at, bool damage)
        {
            var go = new GameObject("DlrZone_" + row.Id);
            go.transform.position = at;
            var z = go.AddComponent<DlrZone>();
            z.owner = owner; z.row = row; z.damage = damage;
            z.loop = Fx.Loop(row.Id, at);
            z.vfx = Vfx.Play(row.Vfx, "loop", at, row.Radius);
            var p = damage ? Fx.Prefab(row.Extra) : null;
            if (p)
            {
                z.ward = Object.Instantiate(p, at, Quaternion.identity);
                var tf = z.ward.GetComponent<TeamFilter>();
                if (tf) tf.teamIndex = owner.teamComponent.teamIndex;
                var bw = z.ward.GetComponent<BuffWard>();
                if (bw) { bw.radius = row.Radius; bw.expires = true; bw.expireDuration = row.Duration; }
                UnityEngine.Networking.NetworkServer.Spawn(z.ward);
            }
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (!owner || age >= row.Duration) { Destroy(gameObject); return; }
            if (!damage || age < next) return;
            next = age + row.Interval;
            var enemies = Fx.Enemies(owner, transform.position, row.Radius);
            if (enemies.Count == 0) return;
            for (int i = 0; i < row.ChainCount && enemies.Count > 0; i++)
            {
                int k = rng.Next(enemies.Count);
                var hb = enemies[k]; enemies.RemoveAt(k);
                var at = hb.healthComponent.body ? hb.healthComponent.body.corePosition : hb.transform.position;
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, at);
                // RoR2's own strike effect once per tick (it is bright; one per enemy washed the screen out)
                if (i == 0) Relay.Impact(row.Effect, at);
                Relay.Vfx(row.Vfx, "hit", transform.position, row.Radius, null, at);
            }
            Relay.Sound(row.Id, "hit", transform.position);
        }

        void OnDestroy()
        {
            DeadlockAudio.Stop(loop);
            if (row != null) Fx.Sound(row.Id, "end", transform.position);
            Vfx.Stop(vfx, 2.5f);
            if (ward) Destroy(ward);
        }
    }

    /// <summary>Flame Dash: one pocket of fire on the ground (server only). For chainCount seconds, every interval each enemy
    /// inside radius gets buffStacks of the hero's gun buff (Afterburn build-up), so overlapping pockets stack and 12 ignite.
    /// The flames are drawn by the dash state on every machine.</summary>
    public sealed class DlrFirePocket : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; string buff; float age, next;

        public static void Plant(CharacterBody owner, SkillRow row, string buff, Vector3 at)
        {
            if (!owner || row == null || buff == "none" || !UnityEngine.Networking.NetworkServer.active) return;
            var go = new GameObject("DlrFirePocket_" + row.Id);
            go.transform.position = at;
            var p = go.AddComponent<DlrFirePocket>();
            p.owner = owner; p.row = row; p.buff = buff;
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (!owner || age >= row.ChainCount) { Destroy(gameObject); return; }
            if (age < next) return;
            next = age + row.Interval;
            foreach (var hb in Fx.Enemies(owner, transform.position, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                for (int k = 0; k < Mathf.Max(1, (int)row.BuffStacks); k++) Buffs.Stack(owner, b, buff, false);
            }
        }
    }
    /// <summary>Sand Blast: a front of sand racing along the ground from the hero at speed out to range, in Deadlock's cone
    /// (radius wide at the start, spreadDeg either side). Every enemy the front passes inside the cone is hit once: damagePct
    /// and the row's buff (Sanded: no attacks; bosses bossMult as long). A wall in front of the centre stops it.</summary>
    public sealed class DlrSandWave : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 origin, dir; float dist; bool damage; GameObject vfx;
        readonly HashSet<HealthComponent> hit = new HashSet<HealthComponent>();

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, bool damage)
        {
            var go = new GameObject("DlrSandWave_" + row.Id);
            go.transform.position = from;
            var w = go.AddComponent<DlrSandWave>();
            w.owner = owner; w.row = row; w.origin = from; w.dir = dir.normalized; w.damage = damage;
            w.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, null, from + dir);
        }

        float HalfWidth(float d) => row.Radius * 0.5f + d * Mathf.Tan(row.SpreadDeg * Mathf.Deg2Rad);

        void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            float step = row.Speed * Time.fixedDeltaTime;
            // the centre line runs into a wall: the wave ends there
            var p = origin + dir * dist + Vector3.up * 1f;
            bool blocked = Physics.Raycast(p, dir, out var wall, step, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
            float next = Mathf.Min(row.Range, blocked ? dist + wall.distance : dist + step);
            // the front hugs the ground (slopes, steps)
            var front = origin + dir * next;
            if (Physics.Raycast(front + Vector3.up * 4f, Vector3.down, out var g, 10f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) front = g.point;
            transform.position = front;
            Vfx.SetWave(vfx, front, dir, HalfWidth(next));
            foreach (var hb in Fx.Enemies(owner, origin + dir * (next * 0.5f), next * 0.5f + HalfWidth(next) + 2f))
            {
                if (hit.Contains(hb.healthComponent)) continue;
                var b = hb.healthComponent.body;
                var at = b ? b.corePosition : hb.transform.position;
                var rel = at - origin; rel.y = 0;
                float along = Vector3.Dot(rel, dir);
                if (along < -1f || along > next + 1f) continue;
                float side = (rel - dir * along).magnitude;
                if (side > HalfWidth(Mathf.Max(0f, along)) + (b ? b.radius : 0.5f)) continue;
                hit.Add(hb.healthComponent);
                // the hit, and sand circling the Sanded enemy for as long as the debuff lasts
                Vfx.Play(row.Vfx, "hit", at, 1f, b ? b.transform : null);
                Fx.Sound(row.Id, "hit", at);
                if (!damage) continue;
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, at);
                if (row.Buff != "none" && b) Buffs.Apply(owner, b, row.Buff, row.BossMult);
                // the sand shoves what it hits back along the cone (bosses stand firm)
                if (row.Knockback > 0 && b && !b.isChampion) Fx.KnockBack(owner, hb.healthComponent, dir, row.Knockback);
            }
            dist = next;
            if (blocked || dist >= row.Range) Destroy(gameObject);
        }

        void OnDestroy() => Vfx.Stop(vfx, 0.8f);
    }

    /// <summary>Tag Team, on the server: Krill, thrown at a first target, lands on an enemy every interval for duration:
    /// damagePct and a stun to it, splashPct to everything else within radius. Each next target is the nearest enemy within
    /// reach he hasn't landed on yet (any enemy once they all have, so he can stay on a lone boss). With no enemy in reach
    /// for minDuration, or at duration, he leaps back to whoever threw him. Kills by his landings give the thrower the row's
    /// debuff (the trophies: buffStacks, x bossMult for a boss). His flights and landings reach every player through the relay;
    /// the thrower's Krill stays hidden while the row's buff (krill_away) is on.</summary>
    public sealed class DlrHopper : MonoBehaviour
    {
        const float Flight = 0.3f;
        CharacterBody owner; SkillRow row; HurtBox target; Vector3 pos;
        float age, nextHop, landAt = -1f, idle, returnAt = -1f;
        readonly HashSet<HealthComponent> landed = new HashSet<HealthComponent>();

        public static void Throw(CharacterBody owner, SkillRow row, Vector3 from, HurtBox first, Vector3 aim)
        {
            if (!owner || row == null || !UnityEngine.Networking.NetworkServer.active) return;
            var go = new GameObject("DlrHopper_" + row.Id);
            go.transform.position = from;
            var h = go.AddComponent<DlrHopper>();
            h.owner = owner; h.row = row; h.pos = from;
            Buffs.Clear(owner, row.Buff);
            Buffs.AddTimed(owner, row.Buff, row.Duration + 2f);
            // no target (a joined player's aim: the server can't see it): look for one a little ahead of the thrower
            h.target = first ? first : h.Nearest(from + aim.normalized * 6f, row.Reach * 0.5f);
            if (h.target) h.HopTo(h.target);
            GlobalEventManager.onCharacterDeathGlobal += h.OnDeath;
        }

        HurtBox Nearest(Vector3 from, float within)
        {
            HurtBox any = null;
            foreach (var hb in Fx.Enemies(owner, from, within))
            {
                if (!hb.healthComponent.body) continue;
                if (!landed.Contains(hb.healthComponent)) return hb;
                if (!any) any = hb;
            }
            return any;
        }

        Vector3 Point(HurtBox hb) => hb && hb.healthComponent && hb.healthComponent.body ? hb.healthComponent.body.corePosition : hb ? hb.transform.position : pos;

        void HopTo(HurtBox hb)
        {
            target = hb;
            var to = Point(hb);
            // the flight: Krill in an arc from where he is to the next enemy (moment hop: at = landing, to = take-off)
            Relay.Vfx(row.Vfx, "hop", to, Flight, null, pos);
            landAt = age + Flight;
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (!owner || !owner.healthComponent || !owner.healthComponent.alive) { Destroy(gameObject); return; }
            if (returnAt >= 0f)
            {
                if (age >= returnAt) Destroy(gameObject);
                return;
            }
            if (landAt >= 0f && age >= landAt) Land();
            if (landAt >= 0f) return;
            if (age >= row.Duration) { Return(); return; }
            if (age < nextHop) return;
            var next = Nearest(pos, row.Reach);
            if (next) { idle = 0f; HopTo(next); }
            else if ((idle += Time.fixedDeltaTime) >= row.MinDuration) Return();
        }

        void Land()
        {
            landAt = -1f;
            nextHop = age + Mathf.Max(0.05f, row.Interval - Flight);
            if (!target || !target.healthComponent || !target.healthComponent.alive) return;
            pos = Point(target);
            landed.Add(target.healthComponent);
            Fx.Hit(owner, target, row.DamagePct / 100f, row.ProcCoefficient, pos, default, DamageColorIndex.Default, gameObject);
            Fx.Stun(target.healthComponent, row.StunDuration);
            foreach (var hb in Fx.Enemies(owner, pos, row.Radius))
                if (hb.healthComponent != target.healthComponent) Fx.Hit(owner, hb, row.SplashPct / 100f, row.ProcCoefficient, pos, default, DamageColorIndex.Default, gameObject);
            Relay.Vfx(row.Vfx, "hit", pos, row.Radius);
            Relay.Sound(row.Id, "hit", pos);
            MeltyTest.Report("krill-lands", true, owner.baseNameToken);
        }

        void Return()
        {
            var to = owner.corePosition;
            Relay.Vfx(row.Vfx, "hop", to, Flight, null, pos);
            Relay.Vfx(row.Vfx, "end", to);
            Relay.Sound(row.Id, "end", to);
            returnAt = age + Flight;
        }

        void OnDeath(DamageReport r)
        {
            if (r == null || !owner || r.damageInfo == null || r.damageInfo.inflictor != gameObject || row.Debuff == "none") return;
            int stacks = Mathf.Max(1, (int)row.BuffStacks) * (r.victimBody && r.victimBody.isChampion ? Mathf.Max(1, (int)row.BossMult) : 1);
            var b = Sheet.Buff(row.Debuff);
            for (int i = 0; i < stacks; i++) Buffs.AddTimed(owner, row.Debuff, 0f, b != null ? (int)b.MaxStacks : 0);
        }

        void OnDestroy()
        {
            GlobalEventManager.onCharacterDeathGlobal -= OnDeath;
            // Krill is back (or gone with a dead thrower): Mo shows him and shoots again
            if (owner && row != null) Buffs.Clear(owner, row.Buff);
        }
    }
}
