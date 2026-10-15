using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Server-side simulated projectile for Sleep Dagger (hit once: damage + sleep) and Lightning Ball
    /// (stops on the first enemy and shocks everything around it until its lifetime ends).</summary>
    public sealed class DlrProjectile : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 dir; float travelled, age, nextTick;
        bool stuck; GameObject ghost, vfx; int loop;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir)
        {
            var go = new GameObject("DlrProjectile_" + row.Id);
            go.transform.position = from;
            var p = go.AddComponent<DlrProjectile>();
            p.owner = owner; p.row = row; p.dir = dir.normalized;
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
            if (!stuck)
            {
                float step = row.Speed * dt;
                var pos = transform.position;
                if (Physics.SphereCast(pos, Mathf.Max(0.3f, orb ? 1.0f : row.Radius * 0.5f), dir, out var hit, step, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                {
                    var hb = hit.collider.GetComponent<HurtBox>();
                    bool enemy = hb && hb.healthComponent && hb.healthComponent.body && hb.healthComponent.body.teamComponent.teamIndex != owner.teamComponent.teamIndex;
                    if (hb && !enemy) { transform.position += dir * step; }
                    else
                    {
                        transform.position = hit.point;
                        if (!orb)
                        {
                            if (enemy)
                            {
                                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, hit.point);
                                Fx.Stun(hb.healthComponent, row.StunDuration);
                                Fx.Impact("stun", hit.point);
                                Fx.Sound(row.Id, "hit", hit.point);
                            }
                            Destroy(gameObject);
                            return;
                        }
                        stuck = true;
                    }
                }
                else transform.position += dir * step;
                travelled += step;
                if (!orb && travelled > row.Range) { Destroy(gameObject); return; }
                if (orb && !stuck && Fx.Enemies(owner, transform.position, 1.5f).Count > 0) stuck = true;
            }
            if (orb)
            {
                if (age >= nextTick)
                {
                    nextTick = age + row.Interval;
                    var enemies = Fx.Enemies(owner, transform.position, row.Radius);
                    foreach (var hb in enemies)
                    {
                        Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, hb.transform.position);
                        // every shock builds the row's stacking debuff (Seven's Static)
                        var v = hb.healthComponent.body;
                        if (row.Buff != "none" && v) Buffs.Stack(owner, v, row.Buff, false);
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

        void OnDestroy()
        {
            DeadlockAudio.Stop(loop);
            Vfx.Stop(vfx);
        }
    }

    /// <summary>Static Charge: planted on an enemy (follows it) or a spot; discharges after the delay.</summary>
    public sealed class DlrDelayed : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Transform follow; float age; int loop; GameObject vfx;
        public static void Plant(CharacterBody owner, SkillRow row, Vector3 at, Transform follow)
        {
            var go = new GameObject("DlrDelayed_" + row.Id);
            go.transform.position = at;
            var d = go.AddComponent<DlrDelayed>();
            d.owner = owner; d.row = row; d.follow = follow;
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
                Fx.Blast(owner, transform.position, row.Radius, row.DamagePct / 100f, row.ProcCoefficient);
                foreach (var hb in Fx.Enemies(owner, transform.position, row.Radius)) Fx.Stun(hb.healthComponent, row.StunDuration);
            }
            Destroy(gameObject);
        }
        void OnDestroy() { DeadlockAudio.Stop(loop); Vfx.Stop(vfx, 0.2f); }
    }

    /// <summary>Napalm: a flask thrown along the aim that falls under gravity and bursts on the first enemy or surface it
    /// touches: damage and the row's debuff to every enemy in the splash.</summary>
    public sealed class DlrLob : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 velocity; float age; GameObject vfx;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir)
        {
            var go = new GameObject("DlrLob_" + row.Id);
            go.transform.position = from;
            var l = go.AddComponent<DlrLob>();
            l.owner = owner; l.row = row; l.velocity = dir.normalized * row.Speed;
            l.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform);
        }

        void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            float dt = Time.fixedDeltaTime;
            age += dt;
            velocity += Physics.gravity * dt;
            var step = velocity * dt;
            var pos = transform.position;
            var hits = Physics.SphereCastAll(pos, 0.4f, step.normalized, step.magnitude, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                // fly through Infernus and his allies
                if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == owner.teamComponent.teamIndex)) continue;
                Burst(h.distance > 0 ? h.point : pos);
                return;
            }
            transform.position = pos + step;
            if (age * row.Speed > row.Range) Burst(transform.position);
        }

        void Burst(Vector3 at)
        {
            Fx.Impact(row.Effect, at, row.Radius);
            Vfx.Play(row.Vfx, "hit", at, row.Radius);
            Fx.Sound(row.Id, "hit", at);
            foreach (var hb in Fx.Enemies(owner, at, row.Radius))
            {
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, hb.transform.position);
                if (row.Buff != "none" && hb.healthComponent.body) Buffs.Apply(owner, hb.healthComponent.body, row.Buff);
            }
            Destroy(gameObject);
        }

        void OnDestroy() => Vfx.Stop(vfx, 0.6f);
    }

    /// <summary>Thunderhead: a storm planted on the ground for duration. Every interval lightning strikes up to chainCount
    /// enemies inside radius; the extra object (RoR2's Warbanner ward) gives allies inside its buff.</summary>
    public sealed class DlrZone : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; float age, next; int loop; GameObject vfx, ward;
        static readonly System.Random rng = new System.Random();

        public static void Plant(CharacterBody owner, SkillRow row, Vector3 at)
        {
            var go = new GameObject("DlrZone_" + row.Id);
            go.transform.position = at;
            var z = go.AddComponent<DlrZone>();
            z.owner = owner; z.row = row;
            z.loop = Fx.Loop(row.Id, at);
            z.vfx = Vfx.Play(row.Vfx, "loop", at, row.Radius);
            var p = Fx.Prefab(row.Extra);
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
            if (age < next) return;
            next = age + row.Interval;
            var enemies = Fx.Enemies(owner, transform.position, row.Radius);
            if (enemies.Count == 0) return;
            // the storm's damage is the ult's: it must not charge the ult meter again
            var ult = UltController.Of(owner);
            if (ult) ult.UltDamageInFlight = true;
            try
            {
                for (int i = 0; i < row.ChainCount && enemies.Count > 0; i++)
                {
                    int k = rng.Next(enemies.Count);
                    var hb = enemies[k]; enemies.RemoveAt(k);
                    var at = hb.healthComponent.body ? hb.healthComponent.body.corePosition : hb.transform.position;
                    Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, at);
                    Fx.Impact(row.Effect, at);
                    Vfx.Play(row.Vfx, "hit", transform.position, row.Radius, null, at);
                }
            }
            finally { if (ult) ult.UltDamageInFlight = false; }
            Fx.Sound(row.Id, "hit", transform.position);
        }

        void OnDestroy()
        {
            DeadlockAudio.Stop(loop);
            if (row != null) Fx.Sound(row.Id, "end", transform.position);
            Vfx.Stop(vfx, 2.5f);
            if (ward) Destroy(ward);
        }
    }
}
