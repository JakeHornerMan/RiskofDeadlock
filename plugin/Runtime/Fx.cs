using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DeadlockOfRain
{
    /// <summary>Effects from design/effects.json (RoR2's own effect prefabs, by Addressables key; our own built visuals are in
    /// Vfx) and combat helpers.</summary>
    public static class Fx
    {
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        public static GameObject Prefab(string effectId)
        {
            var row = Sheet.Effect(effectId);
            // materials are loaded by Vfx.Mat, custom visuals are built by Vfx
            if (row == null || row.Addressable == "none" || row.Kind == "material") return null;
            if (prefabs.TryGetValue(row.Addressable, out var p)) return p;
            try { p = Addressables.LoadAssetAsync<GameObject>(row.Addressable).WaitForCompletion(); }
            catch (System.Exception e) { Log.Warn($"effect {effectId}: {e.Message}"); }
            prefabs[row.Addressable] = p;
            return p;
        }

        public static void Preload()
        {
            foreach (var e in Sheets.EffectRow.All) Prefab(e.Id);
            Vfx.Preload();
        }

        public static void Impact(string effectId, Vector3 at, float radius = 1f, Vector3? normal = null)
        {
            var p = Prefab(effectId);
            if (!p) return;
            var row = Sheet.Effect(effectId);
            if (p.GetComponent<EffectComponent>())
            {
                EffectManager.SpawnEffect(p, new EffectData
                {
                    origin = at,
                    scale = row.ScaleWithRadius ? radius : 1f,
                    rotation = normal.HasValue ? Util.QuaternionSafeLookRotation(normal.Value) : Quaternion.identity,
                }, false);
            }
            else
            {
                var go = Object.Instantiate(p, at, Quaternion.identity);
                if (row.ScaleWithRadius) go.transform.localScale *= radius;
                Object.Destroy(go, 3f);
            }
        }

        public static GameObject Ghost(string effectId, Vector3 at, Quaternion rot)
        {
            var p = Prefab(effectId);
            if (!p) return null;
            var go = Object.Instantiate(p, at, rot);
            foreach (var g in go.GetComponentsInChildren<ProjectileGhostController>(true)) Object.Destroy(g);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            return go;
        }

        public static Transform Muzzle(CharacterBody body, bool left = false)
        {
            var ml = body ? body.modelLocator : null;
            var loc = ml && ml.modelTransform ? ml.modelTransform.GetComponent<ChildLocator>() : null;
            var t = loc ? loc.FindChild(left ? "MuzzleLeft" : "MuzzleRight") : null;
            return t ? t : (body ? body.aimOriginTransform : null);
        }

        public static void Sound(string skillId, string moment, Vector3? at = null)
        {
            var row = Sheet.Sound(skillId);
            if (row == null) return;
            string ev = moment == "cast" ? row.Cast : moment == "loop" ? row.Loop : moment == "hit" ? row.Hit : moment == "end" ? row.End : row.Reload;
            DeadlockAudio.Play(ev, at);
        }

        public static int Loop(string skillId, Vector3? at = null)
        {
            var row = Sheet.Sound(skillId);
            return row == null ? 0 : DeadlockAudio.Play(row.Loop, at, true);
        }

        /// <summary>Enemies (alive, other team) within radius, closest first.</summary>
        public static List<HurtBox> Enemies(CharacterBody owner, Vector3 center, float radius, int max = 64)
        {
            var search = new SphereSearch { origin = center, radius = radius, mask = LayerIndex.entityPrecise.mask, queryTriggerInteraction = QueryTriggerInteraction.UseGlobal };
            var list = new List<HurtBox>();
            search.RefreshCandidates();
            var team = TeamMask.GetEnemyTeams(owner ? owner.teamComponent.teamIndex : TeamIndex.Player);
            search.FilterCandidatesByHurtBoxTeam(team).FilterCandidatesByDistinctHurtBoxEntities().OrderCandidatesByDistance().GetHurtBoxes(list);
            list.RemoveAll(h => !h || !h.healthComponent || !h.healthComponent.alive);
            if (list.Count > max) list.RemoveRange(max, list.Count - max);
            return list;
        }

        public static void Stun(HealthComponent hc, float duration)
        {
            if (!hc || duration <= 0) return;
            var s = hc.GetComponent<SetStateOnHurt>();
            if (s && s.canBeStunned) s.SetStun(duration);
        }

        /// <summary>Server-side direct damage to one hurtbox (used by ticks, chains and custom projectiles).</summary>
        public static void Hit(CharacterBody owner, HurtBox target, float coefficient, float proc, Vector3 at, DamageTypeCombo type = default, DamageColorIndex color = DamageColorIndex.Default)
        {
            if (!owner || !target || !target.healthComponent || !UnityEngine.Networking.NetworkServer.active) return;
            var info = new DamageInfo
            {
                attacker = owner.gameObject,
                inflictor = owner.gameObject,
                damage = owner.damage * coefficient,
                crit = owner.RollCrit(),
                position = at,
                procCoefficient = proc,
                damageType = type,
                damageColorIndex = color,
                force = Vector3.zero,
                procChainMask = default,
                inflictedHurtbox = target,
            };
            info.ModifyDamageInfo(target.damageModifier);
            target.healthComponent.TakeDamage(info);
            GlobalEventManager.instance.OnHitEnemy(info, target.healthComponent.gameObject);
            GlobalEventManager.instance.OnHitAll(info, target.healthComponent.gameObject);
        }

        /// <summary>RoR2 BlastAttack around a point.</summary>
        public static BlastAttack.Result Blast(CharacterBody owner, Vector3 at, float radius, float coefficient, float proc, DamageTypeCombo type = default)
        {
            if (!owner || !UnityEngine.Networking.NetworkServer.active) return default;
            return new BlastAttack
            {
                attacker = owner.gameObject,
                inflictor = owner.gameObject,
                teamIndex = owner.teamComponent.teamIndex,
                position = at,
                radius = radius,
                baseDamage = owner.damage * coefficient,
                crit = owner.RollCrit(),
                procCoefficient = proc,
                damageType = type,
                falloffModel = BlastAttack.FalloffModel.None,
                baseForce = 400f,
                attackerFiltering = AttackerFiltering.NeverHitSelf,
            }.Fire();
        }
    }
}
