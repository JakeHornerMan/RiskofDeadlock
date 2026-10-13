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
        bool stuck; GameObject ghost; int loop;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir)
        {
            var go = new GameObject("DlrProjectile_" + row.Id);
            go.transform.position = from;
            var p = go.AddComponent<DlrProjectile>();
            p.owner = owner; p.row = row; p.dir = dir.normalized;
            p.ghost = Fx.Ghost(row.Effect, from, Quaternion.LookRotation(dir));
            if (p.ghost) p.ghost.transform.SetParent(go.transform, true);
            if (row.Kind == "projectile_orb") p.loop = Fx.Loop(row.Id, from);
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
                    foreach (var hb in enemies) Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, hb.transform.position);
                    if (enemies.Count > 0 && Mathf.Repeat(age, 0.5f) < row.Interval) Fx.Sound(row.Id, "hit", transform.position);
                }
                if (age >= row.Duration || travelled > row.Range)
                {
                    Fx.Sound(row.Id, "end", transform.position);
                    Fx.Impact("static_blast", transform.position, 2f);
                    Destroy(gameObject);
                }
            }
        }

        void OnDestroy()
        {
            DeadlockAudio.Stop(loop);
        }
    }

    /// <summary>Static Charge: planted on an enemy (follows it) or a spot; discharges after the delay.</summary>
    public sealed class DlrDelayed : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Transform follow; float age; int loop;
        public static void Plant(CharacterBody owner, SkillRow row, Vector3 at, Transform follow)
        {
            var go = new GameObject("DlrDelayed_" + row.Id);
            go.transform.position = at;
            var d = go.AddComponent<DlrDelayed>();
            d.owner = owner; d.row = row; d.follow = follow;
            d.loop = Fx.Loop(row.Id, at);
            Fx.Impact("stun", at);
        }
        void FixedUpdate()
        {
            if (follow) transform.position = follow.position;
            age += Time.fixedDeltaTime;
            if (age < row.Duration) return;
            if (owner)
            {
                Fx.Impact(row.Effect, transform.position, row.Radius);
                Fx.Sound(row.Id, "hit", transform.position);
                Fx.Blast(owner, transform.position, row.Radius, row.DamagePct / 100f, row.ProcCoefficient);
                foreach (var hb in Fx.Enemies(owner, transform.position, row.Radius)) Fx.Stun(hb.healthComponent, row.StunDuration);
            }
            Destroy(gameObject);
        }
        void OnDestroy() => DeadlockAudio.Stop(loop);
    }
}
