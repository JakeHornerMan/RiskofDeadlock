#if OWN_FILES
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.Enemies
{
    /// <summary>The Enemy test scene (enemyTestScene.json; creator, 2026-11-13: "set up a test level where we spawn 1 of each of these
    /// trooper every 5 seconds. call it enemy test scene you access it through our custom risk of deadlock section on the character
    /// selection screen"). Ticked in the lobby's Risk of Deadlock box (TrooperRule.TestDef): the run starts on the row's scene, the
    /// stage's own monster spawning is off, and every interval seconds one of each enemy appears near a player and goes for them,
    /// never more than maxAlive at once. Server only; the enemies are ordinary networked monsters, so every player sees them.</summary>
    public static class EnemyTestScene
    {
        public static int Waves, Spawned;

        public static void Init()
        {
            Stage.onServerStageBegin += OnStage;
        }

        static EnemyTestSceneRow Row => EnemyTestSceneRow.All.FirstOrDefault();

        /// <summary>The run's first stage becomes the test scene (before it loads: Run.Start picks it, then changes scene).</summary>
        [HarmonyPatch(typeof(Run), nameof(Run.PickNextStageScene))]
        static class FirstStage
        {
            static void Postfix(Run __instance)
            {
                try
                {
                    if (!NetworkServer.active || Stage.instance || __instance.stageClearCount > 0 || !TrooperRule.TestSceneOn()) return;
                    var scene = Row != null ? SceneCatalog.FindSceneDef(Row.Scene) : null;
                    if (!scene) { Log.Warn($"Enemy test scene: no scene {Row?.Scene}"); return; }
                    __instance.nextStageScene = scene;
                    Log.Info($"Enemy test scene: the run starts on {scene.cachedName}");
                }
                catch (System.Exception e) { Log.Warn($"Enemy test scene: {e.Message}"); }
            }
        }

        static void OnStage(Stage stage)
        {
            if (!NetworkServer.active || !TrooperRule.TestSceneOn() || Row == null) return;
            // the stage's own monster spawning off; the teleporter's boss director is left alone
            foreach (var d in Object.FindObjectsOfType<CombatDirector>())
                if (!d.GetComponentInParent<TeleporterInteraction>()) d.enabled = false;
            var go = new GameObject("DeadlockEnemyTestScene");
            go.AddComponent<Spawner>();
            Log.Info($"Enemy test scene: {stage.sceneDef?.cachedName}: one of each of {string.Join(", ", Row.Enemies)} every {Row.Interval} s, at most {Row.MaxAlive}");
        }

        /// <summary>Server: a wave every interval, around a random living player.</summary>
        sealed class Spawner : MonoBehaviour
        {
            float next;

            void Start() { next = Time.time + 2f; }

            void Update()
            {
                var row = Row;
                if (row == null || !NetworkServer.active || Time.time < next) return;
                next = Time.time + row.Interval;
                var players = PlayerCharacterMasterController.instances.Select(p => p && p.master ? p.master.GetBody() : null).Where(b => b && b.healthComponent && b.healthComponent.alive).ToList();
                if (players.Count == 0) return;
                int alive = TeamComponent.GetTeamMembers(TeamIndex.Monster).Count(tc => tc && tc.body && tc.body.healthComponent && tc.body.healthComponent.alive && EnemyTag.Of(tc.body) != null);
                int made = 0;
                foreach (var id in row.Enemies)
                {
                    if (alive + made >= row.MaxAlive) break;
                    if (!Enemies.Masters.TryGetValue(id, out var prefab)) continue;
                    var target = players[Random.Range(0, players.Count)];
                    var dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                    var at = Ground(target.footPosition + dir * row.Distance);
                    var m = new MasterSummon { masterPrefab = prefab, position = at, rotation = Quaternion.LookRotation(-dir), teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform();
                    if (!m) continue;
                    // as the combat director does: the player is its target from the start
                    foreach (var ai in m.GetComponents<BaseAI>()) { ai.currentEnemy.gameObject = target.gameObject; ai.enemyAttention = ai.enemyAttentionDuration; }
                    made++;
                }
                if (made > 0) { Waves++; Spawned += made; }
            }

            static Vector3 Ground(Vector3 near)
            {
                var g = SceneInfo.instance ? SceneInfo.instance.groundNodes : null;
                if (g)
                {
                    var n = g.FindClosestNode(near, HullClassification.Human);
                    if (g.GetNodePosition(n, out var p)) return p;
                }
                return near;
            }
        }
    }
}
#endif
