using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    // Viscous' goo (design/skills.json viscous_*). Like the other projectiles, each of these flies (or sits) on every machine and
    // only the server's copy (damage = true) deals damage and stacks Gooed.

    /// <summary>Viscous' gun (gun_goo): a blob of goo that flies at speed and falls under its own gravity, so it drops over distance.
    /// It bursts on the first enemy or surface: damagePct, less past falloffStart metres travelled (falloffMinPct at falloffEnd),
    /// and buffStacks of the row's buff (Gooed). The owner launches one, the relayed shot one on every other machine.</summary>
    public sealed class DlrGoo : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 velocity, start; float travelled; bool damage; GameObject vfx;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, bool damage)
        {
            if (!owner || row == null || dir.sqrMagnitude < 1e-6f) return;
            var go = new GameObject("DlrGoo_" + row.Id);
            go.transform.position = from;
            go.transform.rotation = Quaternion.LookRotation(dir);
            var g = go.AddComponent<DlrGoo>();
            g.owner = owner; g.row = row; g.velocity = dir.normalized * row.Speed; g.start = from; g.damage = damage;
            g.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform);
        }

        /// <summary>The damage factor after flying this far: 1 up to falloffStart, falloffMinPct % at falloffEnd and beyond.</summary>
        public static float Falloff(SkillRow row, float distance)
        {
            if (row.FalloffEnd <= row.FalloffStart) return 1f;
            float t = Mathf.Clamp01((distance - row.FalloffStart) / (row.FalloffEnd - row.FalloffStart));
            return Mathf.Lerp(1f, row.FalloffMinPct / 100f, t);
        }

        void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            float dt = Time.fixedDeltaTime;
            velocity += Vector3.down * row.Gravity * dt;
            var step = velocity * dt;
            var pos = transform.position;
            var hits = Physics.SphereCastAll(pos, Mathf.Max(0.1f, row.Radius), step.normalized, step.magnitude, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                // it flies through its own team
                if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == owner.teamComponent.teamIndex)) continue;
                bool inside = h.distance <= 0f;
                Splat(inside ? pos : h.point, inside ? -step.normalized : h.normal, hb);
                return;
            }
            transform.position = pos + step;
            if (step.sqrMagnitude > 1e-8f) transform.rotation = Quaternion.LookRotation(step);
            if (vfx) vfx.transform.rotation = transform.rotation;
            travelled += step.magnitude;
            if (travelled > row.Range) Destroy(gameObject);
        }

        void Splat(Vector3 at, Vector3 normal, HurtBox hb)
        {
            // the splat sticks to what it hit (an enemy: on its body; the world: a goo stain for a while)
            Vfx.Play(row.Vfx, "hit", at, row.Radius, hb ? hb.transform : null, at + normal);
            Fx.Sound(row.Id, "hit", at);
            if (damage && hb)
            {
                Fx.Hit(owner, hb, row.DamagePct / 100f * Falloff(row, Vector3.Distance(start, at)), row.ProcCoefficient, at);
                var b = hb.healthComponent.body;
                if (b && row.Buff != "none") Buffs.AddStacks(owner, b, row.Buff, Mathf.Max(1, (int)row.BuffStacks));
            }
            Destroy(gameObject);
        }

        void OnDestroy() => Vfx.Stop(vfx, 0.3f);
    }

    /// <summary>Splatter's goo puddle (lob_ammo): radius wide for chainCount seconds where the goo ball landed. Every interval the
    /// server adds one stack of the row's buff (Gooed) to each enemy inside and gives allies inside the row's debuff (Slick:
    /// faster). Every machine keeps its own list (Live), so Puddle Punch can ask whether a point is on goo.</summary>
    public sealed class DlrPuddle : MonoBehaviour
    {
        public static readonly List<DlrPuddle> Live = new List<DlrPuddle>();
        CharacterBody owner; SkillRow row; float radius, age, next; bool damage; GameObject vfx;

        /// <summary>How far above or below a puddle something can stand and still be on it.</summary>
        const float Reach = 3f;

        public static void Plant(CharacterBody owner, SkillRow row, Vector3 at, float radius, bool damage)
        {
            // on the ground under the splash (a hit on an enemy's chest still makes the puddle at its feet)
            if (Physics.Raycast(at + Vector3.up * 1f, Vector3.down, out var g, 8f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) at = g.point;
            var go = new GameObject("DlrPuddle_" + row.Id);
            go.transform.position = at;
            var p = go.AddComponent<DlrPuddle>();
            p.owner = owner; p.row = row; p.radius = radius; p.damage = damage;
            p.vfx = Vfx.Play(row.Vfx, "puddle", at, radius, life: row.ChainCount);
            Live.Add(p);
        }

        bool Inside(Vector3 at)
        {
            var d = at - transform.position;
            return Mathf.Abs(d.y) < Reach && new Vector2(d.x, d.z).magnitude <= radius;
        }

        /// <summary>A live puddle under this point (Puddle Punch is twice as wide there).</summary>
        public static bool On(Vector3 at)
        {
            foreach (var p in Live) if (p && p.Inside(at)) return true;
            return false;
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (!owner || age >= row.ChainCount) { Destroy(gameObject); return; }
            if (!damage || age < next) return;
            next = age + Mathf.Max(0.1f, row.Interval);
            foreach (var hb in Fx.Enemies(owner, transform.position, radius + Reach))
            {
                var b = hb.healthComponent.body;
                if (b && Inside(b.footPosition) && row.Buff != "none") Buffs.AddStacks(owner, b, row.Buff, 1);
            }
            if (row.Debuff == "none") return;
            var slick = Sheet.Buff(row.Debuff);
            foreach (var tc in TeamComponent.GetTeamMembers(owner.teamComponent.teamIndex))
            {
                var b = tc ? tc.body : null;
                if (!b || !Inside(b.footPosition)) continue;
                Buffs.Clear(b, row.Debuff);
                Buffs.AddTimed(b, row.Debuff, slick != null ? slick.Duration : row.Interval + 0.1f);
            }
        }

        void OnDestroy()
        {
            Live.Remove(this);
            Vfx.Stop(vfx, 1f);
        }
    }

    /// <summary>Puddle Punch where it was placed (placed_punch): the ground bubbles for duration, then a fist of goo punches up:
    /// damagePct to every enemy in radius (already doubled on a puddle by the state), buffStacks of the row's buff, and enemies
    /// knocked up at knockback m/s (bosses x bossMult). Every body of the hero's team standing in it that this machine moves (its
    /// owner's) is bounced launchHeight metres up and spared the fall. Planted on every machine; the server's deals the damage.</summary>
    public sealed class DlrPunch : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; float radius, age; bool damage, struck; GameObject telegraph;

        public static void Plant(CharacterBody owner, SkillRow row, Vector3 at, float radius, bool damage)
        {
            var go = new GameObject("DlrPunch_" + row.Id);
            go.transform.position = at;
            var p = go.AddComponent<DlrPunch>();
            p.owner = owner; p.row = row; p.radius = radius; p.damage = damage;
            p.telegraph = Vfx.Play(row.Vfx, "telegraph", at, radius);
            // the portal's gurgle and the cast both peak about 0.4 s in, as the fist comes up
            Fx.Sound(row.Id, "cast", at);
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (!struck && age >= row.Duration) Strike();
            if (age >= row.Duration + 1f) Destroy(gameObject);
        }

        void Strike()
        {
            struck = true;
            var at = transform.position;
            Vfx.Stop(telegraph, 0.3f);
            Vfx.Play(row.Vfx, "hit", at, radius);
            Fx.Sound(row.Id, "hit", at);
            if (!owner) return;
            if (damage)
            {
                Fx.Blast(owner, at + Vector3.up * 0.5f, radius, row.DamagePct / 100f, row.ProcCoefficient);
                foreach (var hb in Fx.Enemies(owner, at, radius + 1f))
                {
                    var b = hb.healthComponent.body;
                    if (!b) continue;
                    if (row.Buff != "none") Buffs.AddStacks(owner, b, row.Buff, Mathf.Max(1, (int)row.BuffStacks));
                    float k = b.isChampion ? row.BossMult : 1f;
                    if (row.Knockback <= 0 || k <= 0) continue;
                    float mass = b.characterMotor ? b.characterMotor.mass : b.rigidbody ? b.rigidbody.mass : 100f;
                    if (b.characterMotor) b.characterMotor.Motor.ForceUnground();
                    hb.healthComponent.TakeDamageForce(Vector3.up * row.Knockback * k * mass, true, true);
                }
            }
            if (row.LaunchHeight <= 0) return;
            // a bounce pad for the hero's own team: straight up to launchHeight, keeping the run they had
            float up = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * row.LaunchHeight);
            foreach (var tc in TeamComponent.GetTeamMembers(owner.teamComponent.teamIndex))
            {
                var b = tc ? tc.body : null;
                if (!b || !b.characterMotor) continue;
                var d = b.footPosition - at;
                if (Mathf.Abs(d.y) > 3f || new Vector2(d.x, d.z).magnitude > radius) continue;
                SoftLanding.Give(b);
                if (!b.hasEffectiveAuthority) continue;
                b.characterMotor.Motor.ForceUnground();
                var v = b.characterMotor.velocity;
                b.characterMotor.velocity = new Vector3(v.x, up, v.z);
                Fx.Sound(row.Id + "/launch", "cast", b.footPosition);
                MeltyTest.Report("punch-bounce", true, b.baseNameToken);
            }
        }

        void OnDestroy() => Vfx.Stop(telegraph, 0.2f);
    }

    /// <summary>No fall damage on the next landing (Puddle Punch's bounce): the body ignores fall damage until it has left the ground
    /// and landed again (or 8 s), then gets back whatever it had (Viscous never takes any).</summary>
    public sealed class SoftLanding : MonoBehaviour
    {
        CharacterBody body; bool had, left; float age;

        public static void Give(CharacterBody b)
        {
            if (!b) return;
            var s = b.GetComponent<SoftLanding>();
            if (s) { s.age = 0f; s.left = false; return; }
            b.gameObject.AddComponent<SoftLanding>();
        }

        void Awake()
        {
            body = GetComponent<CharacterBody>();
            had = body && (body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0;
            if (body) body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            bool grounded = body && body.characterMotor && body.characterMotor.isGrounded;
            if (!grounded) left = true;
            if (!body || left && grounded && age > 0.3f || age > 8f) Destroy(this);
        }

        void OnDestroy()
        {
            if (body && !had && !Survivors.NoFallDamage(body)) body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
        }
    }
}
