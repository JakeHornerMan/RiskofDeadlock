#if OWN_FILES
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Enemies;
using DeadlockOfRain.Sheets;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>The enemies' test mode (test-mode file "enemies": "melee,ranged,medic"; ENEMIES= run_test.sh; docs/11): the creator's
    /// Trooper test plan. The player (can't be hurt) is the target and is moved by the test; each Trooper runs its own AI; what the
    /// Troopers do is noted by their states (EnemyNote) and checked against the plan.</summary>
    public sealed partial class MeltyTest
    {
        static List<string> TestEnemies;
        static CharacterBody enemyTarget;
        struct Note { public float T; public string What; public string Who; public float Dist; }
        static readonly List<Note> notes = new List<Note>();

        /// <summary>An enemy did something (swing, hit, shot); kept with its distance to the test's target.</summary>
        public static void EnemyNote(string what, CharacterBody who)
        {
            if (!Active || !who) return;
            float d = enemyTarget ? Vector3.Distance(Flat(who.footPosition), Flat(enemyTarget.footPosition)) : -1f;
            notes.Add(new Note { T = Time.time, What = what, Who = EnemyTag.Of(who), Dist = d });
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static void ReadEnemies(string text)
        {
            var m = System.Text.RegularExpressions.Regex.Match(text, "\"enemies\"\\s*:\\s*\"([^\"]*)\"");
            if (m.Success) TestEnemies = m.Groups[1].Value.Split(',').Select(s => s.Trim()).Where(s => s != "").ToList();
            var sp = System.Text.RegularExpressions.Regex.Match(text, "\"spawn\"\\s*:\\s*\"([a-z0-9:,]+)\"");
            if (sp.Success) PlaySpawn = sp.Groups[1].Value;
        }

        static Vector3 Ground(Vector3 near)
        {
            // the spot itself, dropped onto the ground (2026-11-11: snapping to the nearest node stacked the Medic's group on one spot)
            if (Physics.Raycast(near + Vector3.up * 15f, Vector3.down, out var hit, 40f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.2f;
            var g = SceneInfo.instance ? SceneInfo.instance.groundNodes : null;
            if (g)
            {
                var n = g.FindClosestNode(near, HullClassification.Human);
                if (g.GetNodePosition(n, out var p)) return p;
            }
            return near;
        }

        static void Put(CharacterBody b, Vector3 at)
        {
            TeleportHelper.TeleportBody(b, at + Vector3.up * 0.5f, false);
            if (b.characterMotor) b.characterMotor.velocity = Vector3.zero;
        }

        static CharacterBody SpawnEnemy(string id, Vector3 at, bool ai)
        {
            if (!Enemies.Enemies.Masters.TryGetValue(id, out var prefab)) return null;
            var m = new MasterSummon { masterPrefab = prefab, position = Ground(at), rotation = Quaternion.identity, teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform();
            if (!m) return null;
            if (!ai) foreach (var a in m.GetComponents<BaseAI>()) a.enabled = false;
            // as the combat director does for its spawns: the player is its target from the start (a monster's AI otherwise sees only
            // what is within 90 degrees in front of it, and the test spawns it facing any way)
            else if (enemyTarget)
                foreach (var a in m.GetComponents<BaseAI>()) { a.currentEnemy.gameObject = enemyTarget.gameObject; a.enemyAttention = a.enemyAttentionDuration; }
            var b = m.GetBody();
            if (b) targets.Add(b);
            return b;
        }

        static CharacterBody SpawnVanilla(string master, Vector3 at)
        {
            var prefab = MasterCatalog.FindMasterPrefab(master);
            var m = prefab ? new MasterSummon { masterPrefab = prefab, position = Ground(at), rotation = Quaternion.identity, teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform() : null;
            if (!m) return null;
            foreach (var a in m.GetComponents<BaseAI>()) a.enabled = false;
            var b = m.GetBody();
            if (b) targets.Add(b);
            return b;
        }

        static void Clear()
        {
            foreach (var t in targets.ToList()) if (t && t.master) t.master.TrueKill();
            targets.Clear();
        }

        /// <summary>Play mode's "spawn" (e.g. "medic:5,melee:2"): those Troopers, AI on, 15-20 m in front of the player, a few
        /// seconds into the stage.</summary>
        static string PlaySpawn;

        static IEnumerator PlaySpawns(LocalUser user)
        {
            CharacterBody b = null;
            while (!(b = user != null ? user.cachedBody : null)) yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(4f);
            enemyTarget = b;
            var fwd = b.inputBank ? b.inputBank.aimDirection : b.transform.forward; fwd.y = 0; fwd.Normalize();
            var side = Vector3.Cross(Vector3.up, fwd);
            int n = 0;
            foreach (var part in PlaySpawn.Split(','))
            {
                var kv = part.Split(':');
                int count = kv.Length > 1 && int.TryParse(kv[1], out var c) ? Mathf.Clamp(c, 1, 20) : 1;
                for (int i = 0; i < count; i++, n++)
                {
                    var e = SpawnEnemy("trooper_" + kv[0], b.footPosition + fwd * (15f + 2.5f * (n / 5)) + side * (((n % 5) - 2) * 3f), true);
                    targets.Remove(e);
                }
            }
            Log.Info($"Play mode: spawned {n} Troopers ({PlaySpawn}) in front of the player");
        }

        /// <summary>One line on what the enemy's AI is doing (target, driver, state, distance): for reading a failed check.</summary>
        static void LogAi(CharacterBody e, CharacterBody p)
        {
            var ai = e && e.master ? e.master.GetComponent<BaseAI>() : null;
            var esm = e ? EntityStateMachine.FindByCustomName(e.gameObject, "Body") : null;
            Log.Info($"enemy ai {(e ? e.name : "none")}: ai {(ai ? (ai.enabled ? "on" : "off") : "none")}, target {(ai && ai.currentEnemy != null && ai.currentEnemy.gameObject ? ai.currentEnemy.gameObject.name : "none")}, " +
                $"driver {(ai && ai.skillDriverEvaluation.dominantSkillDriver ? ai.skillDriverEvaluation.dominantSkillDriver.customName : "none")}, state {(esm ? esm.state?.GetType().Name : "?")}, " +
                $"move {(e && e.inputBank ? e.inputBank.moveVector.ToString("F2") : "?")}, dist {Dist(e, p):F1}, player at {(p ? p.footPosition.ToString("F1") : "?")} vis {(p ? p.GetVisibilityLevel(TeamIndex.Monster).ToString() : "?")}; " +
                $"body at {(e ? e.footPosition.ToString("F1") : "?")} speed {(e ? e.moveSpeed : 0f):F1}, motor {(e && e.characterMotor ? $"{(e.characterMotor.enabled ? "on" : "off")} grounded {e.characterMotor.isGrounded} vel {e.characterMotor.velocity:F1} dir {e.characterMotor.moveDirection:F2} kcm {(e.characterMotor.Motor ? e.characterMotor.Motor.enabled.ToString() : "none")}" : "none")}" +
                $", anim [{EnemyLook.Of(e)?.Animator?.Describe() ?? "none"}] look {(EnemyLook.Of(e) ? EnemyLook.Of(e).Shown : -1)} model {(e && e.modelLocator && e.modelLocator.modelTransform ? e.modelLocator.modelTransform.name + "/" + string.Join(",", e.modelLocator.modelTransform.GetComponents<Component>().Select(c => c.GetType().Name)) : "none")}" +
                $", capsule {(e && e.GetComponent<CapsuleCollider>() ? $"h {e.GetComponent<CapsuleCollider>().height:F2} r {e.GetComponent<CapsuleCollider>().radius:F2} c {e.GetComponent<CapsuleCollider>().center.y:F2}" : "none")}");
        }

        /// <summary>The enemy is in one of its attack states.</summary>
        static bool Swinging(CharacterBody e)
        {
            var esm = e ? EntityStateMachine.FindByCustomName(e.gameObject, "Weapon") : null;
            var body = e ? EntityStateMachine.FindByCustomName(e.gameObject, "Body") : null;
            return esm && esm.state is EnemyAttack || body && body.state is EnemyAttack;
        }

        static float Dist(CharacterBody a, CharacterBody b) => a && b ? Vector3.Distance(Flat(a.footPosition), Flat(b.footPosition)) : -1f;

        /// <summary>The player walks: the test moves its body each physics step (root motion at its move speed). Its controller stays
        /// on: with no enabled PlayerCharacterMasterController the stage counts every player gone and ends the run (Stage.FixedUpdate).</summary>
        static Vector3 walk;
        void FixedUpdate()
        {
            if (walk != Vector3.zero && enemyTarget && enemyTarget.characterMotor)
                enemyTarget.characterMotor.rootMotion += walk * enemyTarget.moveSpeed * Time.fixedDeltaTime;
        }

        IEnumerator EnemyRun(LocalUser user)
        {
            var body = user.cachedBody;
            enemyTarget = body;
            body.healthComponent.godMode = true;
            QuietStage();
            arenaForward = body.inputBank.aimDirection; arenaForward.y = 0; arenaForward.Normalize();
            arena = FindArena(body.footPosition, ref arenaForward);
            Put(body, arena);
            var side = Vector3.Cross(Vector3.up, arenaForward);
            int passed = 0, total = 0;
            void Check(string name, bool ok, string why) { total++; if (ok) passed++; Write(name, ok, why); }

            // content: every enemy built with both looks; spawn cards; the stage's pool reached; the family; the Medic's gate
            var built = (TestEnemies ?? new List<string>()).Where(e => e != "stages" && e != "lineup" && e != "testscene").Select(e => "trooper_" + e).ToList();
            var missing = built.Where(e => !Enemies.Enemies.Bodies.ContainsKey(e)).ToList();
            Check("enemies-built", missing.Count == 0, missing.Count == 0 ? $"{Enemies.Enemies.Bodies.Count} built" : "missing " + string.Join(", ", missing));
            var looks = Enemies.Enemies.Bodies.Values.Select(b => b.GetComponent<ModelLocator>().modelTransform.GetComponent<EnemyLook>()).ToList();
            Check("enemy-looks", looks.All(l => l && l.Looks.Length == 2), string.Join(", ", looks.Select(l => l ? l.Looks.Length.ToString() : "none")) + " looks");
            var sel = ClassicStageInfo.instance ? ClassicStageInfo.instance.monsterSelection : null;
            int inPool = 0;
            if (sel != null) for (int i = 0; i < sel.Count; i++) if (Enemies.Enemies.Cards.Values.Contains(sel.GetChoice(i).value?.spawnCard)) inPool++;
            if (TrooperDirector.StageAllowed) Check("enemy-spawn-pool", TrooperDirector.StagesTouched > 0 && inPool == Enemies.Enemies.Cards.Count,
                $"R2API pools touched {TrooperDirector.StagesTouched}; {inPool}/{Enemies.Enemies.Cards.Count} Trooper cards in this stage's monster list; costs " +
                string.Join(", ", Enemies.Enemies.Cards.Select(kv => $"{kv.Key} {kv.Value.directorCreditCost}")));
            var choice = TrooperRule.Def != null && Run.instance ? Run.instance.ruleBook.GetRuleChoice(TrooperRule.Def) : null;
            Check("troopers-lobby-rule", TrooperRule.Def != null && TrooperRule.OnThisRun() && TrooperRule.Def.category != null && TrooperRule.Def.category.displayToken == TrooperRule.Header
                && RuleCatalog.allCategoryDefs.IndexOf(TrooperRule.Def.category) == RuleCatalog.allCategoryDefs.FindIndex(c => c.displayToken == "RULE_HEADER_KARMA") + 1,
                TrooperRule.Def == null ? "no Troopers rule" : $"Troopers box after Karma, this run: {choice?.localName ?? "?"}");
            Check("enemy-family", TrooperDirector.Family("troopers") != null, "Trooper family event made");
            Check("medic-spawn-gate", !MedicSpawnGate.AlliesNear(arena + Vector3.up * 500f), "a spot with no monsters near turns the Medic down");

            if (TestEnemies.Contains("testscene")) { yield return TestSceneRun(Check); walk = Vector3.zero; Log.Info($"Melty test finished: enemies {passed + lobbyPassed}/{total + lobbyTotal} checks passed"); yield break; }
            if (TestEnemies.Contains("stages")) { yield return StagesRun(user, Check); walk = Vector3.zero; Log.Info($"Melty test finished: enemies {passed + lobbyPassed}/{total + lobbyTotal} checks passed"); yield break; }
            if (TestEnemies.Contains("lineup")) yield return LineupTest(body, side);
            if (TestEnemies.Contains("melee")) yield return MeleeTest(body, side, Check);
            if (TestEnemies.Contains("ranged")) yield return RangedTest(body, Check);
            if (TestEnemies.Contains("medic")) yield return MedicTest(body, side, Check);
            if (TestEnemies.Contains("melee") || TestEnemies.Contains("ranged") || TestEnemies.Contains("medic")) yield return DeathTest(body, side, Check);
            if (TestEnemies.Contains("melee")) yield return SoulOrbTest(body, Check);

            walk = Vector3.zero;
            Clear();
            Log.Info($"Melty test finished: enemies {passed}/{total} checks passed");
        }

        // ---- the Trooper stage list (ENEMIES=stages; the creator's "Feature: Trooper stage selection" tests) ----
        static string stagesBefore;
        static int lobbyPassed, lobbyTotal;
        static void LobbyCheck(string name, bool ok, string why) { lobbyTotal++; if (ok) lobbyPassed++; Write(name, ok, why); }

        /// <summary>In the lobby: the dropdown's rows, the default, opening it, greying it with the Troopers box unticked (the list
        /// kept), Reset to default. Leaves a list for the run: the Lemurian's minus Titanic Plains, plus Sky Meadow.</summary>
        internal static IEnumerator LobbyStages()
        {
            float t0 = Time.unscaledTime;
            while ((!TrooperStages.Ready || !TrooperStagesUi.Instance) && Time.unscaledTime - t0 < 40f) yield return null;
            var ui = TrooperStagesUi.Instance;
            stagesBefore = TrooperStages.Selected.Value;
            var vis = TrooperStages.Visible.ToList();
            var groups = vis.Select(s => s.Group).Distinct().ToList();
            LobbyCheck("stages-list", TrooperStages.Ready && vis.Count > 0 && vis.All(s => s.Owned) && groups.All(g => g.StartsWith("Stage ") || g == "Final" || g == "Hidden Realms" || g == "Simulacrum"),
                $"{vis.Count} of {TrooperStages.All.Count} stages shown (owned), groups {string.Join(", ", groups)}: " + string.Join(", ", vis.Select(s => $"{s.Group}/{s.Name}")));
            TrooperStages.Reset();
            var lem = TrooperStages.LemurianKeys;
            LobbyCheck("stages-default", TrooperStages.IsDefault && TrooperStages.Current().SetEquals(lem) && lem.Contains("golemplains") && lem.Contains("blackbeach") && !lem.Contains("skymeadow"),
                "default = the Lemurian stages: " + string.Join(", ", lem.OrderBy(x => x)));
            if (!ui) { LobbyCheck("stages-ui-open", false, "no dropdown in the lobby"); yield break; }
            ui.Open(true);
            yield return new WaitForSeconds(1f);
            Shot("lobby_stages_open");
            LobbyCheck("stages-ui-open", ui.PanelOpen && ui.RowCount == vis.Count, $"open {ui.PanelOpen}, {ui.RowCount} rows for {vis.Count} stages");
            // untick the Troopers box (the host's vote): greyed and closed, the list kept
            TrooperStages.Set(lem.Where(k => k != "golemplains").Concat(new[] { "skymeadow" }));
            var chosen = TrooperStages.Selected.Value;
            var voter = PreGameRuleVoteController.FindForUser(LocalUserManager.GetFirstLocalUser()?.currentNetworkUser);
            if (voter && TrooperRule.Def != null) voter.SetVote(TrooperRule.Def.globalIndex, 1);
            t0 = Time.unscaledTime;
            while (ui.Interactable && Time.unscaledTime - t0 < 5f) yield return null;
            yield return new WaitForSeconds(0.5f);
            Shot("lobby_stages_greyed");
            bool greyed = !ui.Interactable && !ui.PanelOpen;
            ui.Open(true);
            bool stayedShut = !ui.PanelOpen;
            if (voter && TrooperRule.Def != null) voter.SetVote(TrooperRule.Def.globalIndex, 0);
            t0 = Time.unscaledTime;
            while (!ui.Interactable && Time.unscaledTime - t0 < 5f) yield return null;
            LobbyCheck("stages-ui-greyed", greyed && stayedShut && ui.Interactable && TrooperStages.Selected.Value == chosen,
                $"greyed {greyed}, would not open {stayedShut}, back on {ui.Interactable}, list kept {TrooperStages.Selected.Value == chosen}");
            TrooperStages.Set(new[] { "golemplains" });
            TrooperStages.Reset();
            bool reset = TrooperStages.Current().SetEquals(lem);
            TrooperStages.Set(lem.Where(k => k != "golemplains").Concat(new[] { "skymeadow" }));
            LobbyCheck("stages-reset", reset, "Reset to default gives the Lemurian list back");
        }

        /// <summary>The lobby: the Enemy test scene ticked by the host's vote, with its label beside its box.</summary>
        internal static IEnumerator LobbyTestScene()
        {
            float t0 = Time.unscaledTime;
            while (TrooperRule.TestDef == null && Time.unscaledTime - t0 < 20f) yield return null;
            var voter = PreGameRuleVoteController.FindForUser(LocalUserManager.GetFirstLocalUser()?.currentNetworkUser);
            if (voter && TrooperRule.TestDef != null) voter.SetVote(TrooperRule.TestDef.globalIndex, 1);
            t0 = Time.unscaledTime;
            while (!TrooperRule.TestSceneOn() && Time.unscaledTime - t0 < 5f) yield return null;
            yield return new WaitForSeconds(0.5f);
            Shot("lobby_enemy_test_scene");
            bool label = Object.FindObjectsOfType<TMPro.TMP_Text>().Any(x => x.text == (EnemyTestSceneRow.All.FirstOrDefault()?.Name ?? "?"));
            LobbyCheck("enemytest-lobby", TrooperRule.TestDef != null && TrooperRule.TestSceneOn() && label,
                $"Enemy test scene in the Risk of Deadlock box {TrooperRule.TestDef != null}, ticked {TrooperRule.TestSceneOn()}, labelled {label}");
        }

        /// <summary>The run on the test scene: its stage, no stage spawning, waves of one of each every interval, the cap.</summary>
        IEnumerator TestSceneRun(System.Action<string, bool, string> Check)
        {
            var row = EnemyTestSceneRow.All.FirstOrDefault();
            var scene = SceneCatalog.mostRecentSceneDef ? SceneCatalog.mostRecentSceneDef.cachedName : "?";
            bool directorsOff = !Object.FindObjectsOfType<CombatDirector>().Any(d => d.enabled && !d.GetComponentInParent<TeleporterInteraction>());
            Check("enemytest-stage", row != null && scene == row.Scene && directorsOff, $"stage {scene} (want {row?.Scene}), the stage's own spawning off {directorsOff}");
            int waves0 = EnemyTestScene.Waves;
            float t0 = Time.time, wait = row != null ? row.Interval * 2f + 1.5f : 12f;
            int most = 0;
            while (Time.time - t0 < wait)
            {
                most = Mathf.Max(most, TeamComponent.GetTeamMembers(TeamIndex.Monster).Count(tc => tc && tc.body && tc.body.healthComponent && tc.body.healthComponent.alive && EnemyTag.Of(tc.body) != null));
                yield return new WaitForSeconds(0.25f);
            }
            Shot("enemy_test_scene");
            var ids = TeamComponent.GetTeamMembers(TeamIndex.Monster).Where(tc => tc && tc.body && tc.body.healthComponent && tc.body.healthComponent.alive).Select(tc => EnemyTag.Of(tc.body)).Where(x => x != null).ToList();
            int waves = EnemyTestScene.Waves - waves0;
            bool each = row != null && row.Enemies.All(e => ids.Contains(e));
            Check("enemytest-waves", row != null && waves >= 2 && each && most <= row.MaxAlive,
                $"{waves} waves in {wait:F0} s (every {row?.Interval} s), alive now: {string.Join(", ", ids.GroupBy(x => x).Select(g => $"{g.Key} x{g.Count()}"))}; most at once {most} (cap {row?.MaxAlive})");
        }

        static IEnumerator GoTo(LocalUser user, string scene)
        {
            var old = user.cachedBody;
            UnityEngine.Networking.NetworkManager.singleton.ServerChangeScene(scene);
            float t0 = Time.unscaledTime;
            while ((UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != scene || !user.cachedBody || user.cachedBody == old) && Time.unscaledTime - t0 < 90f) yield return null;
            yield return new WaitForSeconds(2f);
            QuietStage();
        }

        static int TrooperCards()
        {
            var sel = ClassicStageInfo.instance ? ClassicStageInfo.instance.monsterSelection : null;
            int n = 0;
            if (sel != null) for (int i = 0; i < sel.Count; i++) if (Enemies.Enemies.Cards.Values.Contains(sel.GetChoice(i).value?.spawnCard)) n++;
            return n;
        }

        /// <summary>In the run: the list the lobby left (no Titanic Plains, Sky Meadow added) decides each stage as it loads.</summary>
        IEnumerator StagesRun(LocalUser user, System.Action<string, bool, string> Check)
        {
            var here = SceneCatalog.mostRecentSceneDef;
            Check("stages-untick", !TrooperDirector.StageAllowed && TrooperCards() == 0,
                $"{here?.cachedName}: allowed {TrooperDirector.StageAllowed}, {TrooperCards()} Trooper cards in its monster list");
            yield return GoTo(user, "blackbeach");
            Check("stages-untick-other", TrooperDirector.StageAllowed && TrooperCards() == 3, $"blackbeach: allowed {TrooperDirector.StageAllowed}, {TrooperCards()} Trooper cards");
            yield return GoTo(user, "skymeadow");
            Check("stages-tick", TrooperDirector.StageAllowed && TrooperCards() == 3, $"skymeadow (not a Lemurian stage): allowed {TrooperDirector.StageAllowed}, {TrooperCards()} Trooper cards");
            // nothing ticked: from the next stage on, none anywhere (the stage running keeps what it had)
            TrooperStages.Set(new string[0]);
            bool kept = TrooperDirector.StageAllowed;
            yield return GoTo(user, "blackbeach");
            Check("stages-none", kept && !TrooperDirector.StageAllowed && TrooperCards() == 0 && TrooperStages.Current().Count == 0,
                $"the running stage kept its Troopers {kept}; blackbeach after: allowed {TrooperDirector.StageAllowed}, {TrooperCards()} cards");
            TrooperStages.Selected.Value = stagesBefore ?? TrooperStages.Default;
            Log.Info($"Melty test: stage list put back to '{TrooperStages.Selected.Value}'");
        }

        /// <summary>Every enemy in each of its looks, standing still in a row facing the camera, for comparing the materials with
        /// Deadlock (creator, 2026-11-11). Two shots: the whole row, then each pair closer.</summary>
        IEnumerator LineupTest(CharacterBody p, Vector3 side)
        {
            Put(p, arena);
            var made = new List<CharacterBody>();
            var rows = EnemyRow.All.Where(r => Enemies.Enemies.Masters.ContainsKey(r.Id)).ToList();
            int n = rows.Sum(r => r.Models.Length), i = 0;
            foreach (var r in rows)
                for (int k = 0; k < r.Models.Length; k++, i++)
                {
                    var e = SpawnEnemy(r.Id, arena + arenaForward * 13f + side * ((i - (n - 1) * 0.5f) * 3.2f), false);
                    if (!e) continue;
                    made.Add(e);
                    yield return null;
                    var lk = EnemyLook.Of(e);
                    if (lk) lk.Pick(k);
                    if (e.characterDirection) { e.characterDirection.forward = -arenaForward; e.characterDirection.moveVector = Vector3.zero; }
                }
            // the camera looks the way the player faces: at the row
            if (p.inputBank) p.inputBank.aimDirection = arenaForward;
            yield return new WaitForSeconds(3f);
            Shot("trooper_lineup");
            for (int r = 0; r < rows.Count; r++)
            {
                int first = rows.Take(r).Sum(x => x.Models.Length);
                var mid = arena + arenaForward * 13f + side * (((first + (rows[r].Models.Length - 1) * 0.5f) - (n - 1) * 0.5f) * 3.2f);
                Put(p, Ground(mid - arenaForward * 7f));
                yield return new WaitForSeconds(1.5f);
                Shot("trooper_lineup_" + rows[r].Id);
            }
            Log.Info("enemy lineup: " + string.Join(", ", made.Select(e => $"{e.name} look {EnemyLook.Of(e)?.Shown}")));
            Clear();
            yield return new WaitForSeconds(1f);
        }

        /// <summary>Each Trooper killed (AI off): no bone flies (no rigidbody that moves, no bone more than 0.3 m from its pose at
        /// death, measured within the model: the corpse itself may settle), the model fades out (corpseFade to 0) and is gone within 3 s (creator, 2026-11-11: "just fade out").</summary>
        IEnumerator DeathTest(CharacterBody p, Vector3 side, System.Action<string, bool, string> Check)
        {
            Put(p, arena);
            var rows = EnemyRow.All.Where(r => Enemies.Enemies.Masters.ContainsKey(r.Id)).ToList();
            var bodies = rows.Select((r, i) => SpawnEnemy(r.Id, arena + arenaForward * 8f + side * ((i - (rows.Count - 1) * 0.5f) * 3.5f), false)).Where(b => b).ToList();
            yield return new WaitForSeconds(2f);
            var watch = new List<(string id, Transform model, Transform[] bones, Vector3[] start)>();
            foreach (var b in bodies)
            {
                var model = b.modelLocator ? b.modelLocator.modelTransform : null;
                var bones = model ? model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.gameObject.activeInHierarchy).SelectMany(r => r.bones).Where(x => x).Distinct().ToArray() : new Transform[0];
                watch.Add((EnemyTag.Of(b), model, bones, null));
                b.healthComponent.Suicide();
            }
            yield return new WaitForSeconds(0.05f);
            // bone positions in the model's own space: the whole corpse may settle as the body lands; a bone may not move on its own
            for (int i = 0; i < watch.Count; i++) { var w = watch[i]; watch[i] = (w.id, w.model, w.bones, w.bones.Select(x => x && w.model ? w.model.InverseTransformPoint(x.position) : Vector3.zero).ToArray()); }
            float moved = 0f, tipped = 0f, sankAt = -1f; bool boneBodies = false;
            float t0 = Time.time;
            bool shot1 = false, shot2 = false, shot3 = false;
            var startY = watch.Select(w => w.model ? w.model.position.y : 0f).ToArray();
            while (Time.time - t0 < 6f && watch.Any(w => w.model))
            {
                for (int i = 0; i < watch.Count; i++)
                {
                    var w = watch[i];
                    if (!w.model) continue;
                    for (int k = 0; k < w.bones.Length; k++) if (w.bones[k]) moved = Mathf.Max(moved, Vector3.Distance(w.model.InverseTransformPoint(w.bones[k].position), w.start[k]) * w.model.lossyScale.x);
                    // only the corpse's own body may be physical: no rigidbody on any bone
                    if (w.bones.Any(b => b && b.GetComponent<Rigidbody>())) boneBodies = true;
                    tipped = Mathf.Max(tipped, Vector3.Angle(w.model.up, Vector3.up));
                    var f = w.model.GetComponent<EnemyFade>();
                    if (f && f.Sinking && w.model.position.y < startY[i] - 0.5f && sankAt < 0f) sankAt = Time.time - t0;
                }
                if (!shot1 && Time.time - t0 > 0.6f) { shot1 = true; Shot("trooper_death_fallen"); }
                if (!shot2 && sankAt > 0f) { shot2 = true; Shot("trooper_death_sinking"); }
                yield return null;
            }
            float goneAt = Time.time - t0;
            bool gone = watch.All(w => !w.model);
            Check("trooper-death-fade", !boneBodies && moved <= 0.3f && tipped >= 45f && sankAt > 0f && gone && goneAt <= 6f,
                $"{watch.Count} killed: fell over {tipped:F0} degrees, sank through the floor at {sankAt:F1} s, gone at {goneAt:F1} s; bones moved on their own at most {moved:F2} m, rigidbodies on bones {boneBodies}");
            foreach (var b in bodies) targets.Remove(b);
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>A Trooper's death: smoke in its look's colour; a soul orb rises from it; breaking it pays its breaker the row's gold
        /// exactly; one left alone goes after its lifetime and pays nothing (creator, 2026-11-11).</summary>
        IEnumerator SoulOrbTest(CharacterBody p, System.Action<string, bool, string> Check)
        {
            var row = SoulOrb.Row;
            if (row == null || !SoulOrb.Body) { Check("trooper-soul-orb", false, "no soul orb built"); yield break; }
            Put(p, arena);
            yield return new WaitForSeconds(0.5f);
            var master = p.master;
            // 1. broken: smoke, an orb above the body, +gold
            var t1 = SpawnEnemy("trooper_melee", arena + arenaForward * 7f, false);
            yield return new WaitForSeconds(1.5f);
            var look = EnemyLook.Of(t1);
            var want = Sheet.Color(row.SmokeColors[Mathf.Clamp(look ? look.Shown : 0, 0, row.SmokeColors.Length - 1)]);
            int smoke0 = DeathSmoke.Played, spawned0 = SoulOrb.Spawned;
            var deathAt = t1.footPosition;
            targets.Remove(t1);
            t1.healthComponent.Suicide();
            yield return new WaitForSeconds(0.4f);
            Shot("trooper_death_smoke");
            Check("trooper-death-smoke", DeathSmoke.Played > smoke0 && Mathf.Abs(DeathSmoke.LastColor.r - want.r) + Mathf.Abs(DeathSmoke.LastColor.g - want.g) + Mathf.Abs(DeathSmoke.LastColor.b - want.b) < 0.02f,
                $"smoke {DeathSmoke.Played - smoke0}, colour {ColorUtility.ToHtmlStringRGB(DeathSmoke.LastColor)} for look {(look ? look.Shown : -1)} (want {ColorUtility.ToHtmlStringRGB(want)})");
            yield return new WaitForSeconds(1.2f);
            var orb = SoulOrb.Live.LastOrDefault(o => o);
            Shot("trooper_soul_orb");
            // a close look: the player 3 m from the orb, facing it
            if (orb) { Put(p, Ground(orb.transform.position - arenaForward * 3f)); yield return new WaitForSeconds(0.4f); Shot("trooper_soul_orb_close"); }
            bool up = orb && orb.transform.position.y > deathAt.y + SoulOrb.Row.Rise;
            bool neutral = orb && orb.teamComponent && orb.teamComponent.teamIndex == TeamIndex.Neutral;
            uint money0 = master ? master.money : 0;
            if (orb) orb.healthComponent.TakeDamage(new DamageInfo { damage = 50f, attacker = p.gameObject, inflictor = p.gameObject, position = orb.corePosition, procCoefficient = 0f, damageColorIndex = DamageColorIndex.Default });
            yield return new WaitForSeconds(0.6f);
            uint gained = master ? master.money - money0 : 0;
            Check("trooper-soul-orb", SoulOrb.Spawned > spawned0 && up && neutral && gained == (uint)Mathf.RoundToInt(row.Gold) && !orb,
                $"orb spawned {SoulOrb.Spawned - spawned0}, above the body {up}, Neutral {neutral}; shot: +{gained} money (want {row.Gold}), orb gone {!orb}");
            // 2. left alone: gone after its lifetime, no money
            var t2 = SpawnEnemy("trooper_melee", arena + arenaForward * 7f, false);
            yield return new WaitForSeconds(1.5f);
            targets.Remove(t2);
            int missed0 = SoulOrb.Missed;
            t2.healthComponent.Suicide();
            yield return new WaitForSeconds(0.8f);
            var orb2 = SoulOrb.Live.LastOrDefault(o => o);
            uint money1 = master ? master.money : 0;
            float t0 = Time.time;
            while (orb2 && Time.time - t0 < row.Lifetime + 2f) yield return null;
            float lasted = Time.time - t0 + 0.8f;
            Check("trooper-soul-orb-miss", orb2 == null && SoulOrb.Missed > missed0 && master && master.money == money1 && Mathf.Abs(lasted - row.Lifetime - 0.6f) < 1f,
                $"unbroken orb gone after {lasted:F1} s (lifetime {row.Lifetime} + 0.6 s fade), missed {SoulOrb.Missed - missed0}, money unchanged {(master ? master.money == money1 : false)}");
        }

        IEnumerator MeleeTest(CharacterBody p, Vector3 side, System.Action<string, bool, string> Check)
        {
            Put(p, arena);
            notes.Clear();
            var t = SpawnEnemy("trooper_melee", arena + arenaForward * 20f, true);
            if (!t) { Check("trooper-melee", false, "could not spawn"); yield break; }
            yield return new WaitForSeconds(2f);
            var lk = EnemyLook.Of(t);
            Check("enemy-look-spawned", lk && lk.Shown >= 0 && lk.Animator && lk.Animator.Has(Sheet.Enemy("trooper_melee").Idle),
                lk ? $"look {lk.Shown}, animator {(lk.Animator ? lk.Animator.Describe() : "none")}; looks {(lk.Looks == null ? "null" : lk.Looks.Length.ToString())} " +
                $"[{(lk.Looks == null ? "" : string.Join(", ", lk.Looks.Select(l => l == null ? "null" : $"{l.gameObject.name + (l.gameObject.activeSelf ? " on" : " off")} anim {(l.Animator ? "yes" : "no")}")))}] " +
                $"enabled {lk.enabled} active {lk.isActiveAndEnabled} same as tag {(t.GetComponent<EnemyTag>()?.Look == lk)}" : "no EnemyLook");
            Shot("trooper_melee_spawn");
            // the target walks a steady path across the arena, turning every 3 s
            float start = Time.time, closest = 99f;
            for (float s = 0f; s < 12f; s += Time.fixedDeltaTime)
            {
                walk = (((int)(s / 3f)) % 2 == 0 ? side : -side) * 0.6f;
                closest = Mathf.Min(closest, Dist(p, t));
                if (Mathf.Repeat(s, 1f) < Time.fixedDeltaTime) LogAi(t, p);
                yield return new WaitForFixedUpdate();
            }
            walk = Vector3.zero;
            var swings = notes.Where(n => n.What == "swingstart" && n.T >= start).ToList();
            Check("trooper-melee-chase", closest <= 3.5f, $"closest {closest:F1} m to the walking target");
            Check("trooper-melee-swing", swings.Count > 0 && swings.All(n => n.Dist <= 4f), $"{swings.Count} swings, at {string.Join(", ", swings.Select(n => n.Dist.ToString("F1")))} m");
            // the target stops: the Trooper stays beside it and keeps hitting
            float hold = Time.time; var at = Flat(t.footPosition); float wander = 0f; bool swingShot = false;
            for (float s = 0f; s < 8f; s += 0.1f)
            {
                if (s > 2f) wander = Mathf.Max(wander, Dist(p, t));
                if (Mathf.Repeat(s + 0.01f, 0.5f) < 0.1f) LogAi(t, p);
                if (!swingShot && Swinging(t)) { swingShot = true; yield return new WaitForSeconds(0.35f); LogAi(t, p); Shot("trooper_melee_swinging"); }
                yield return new WaitForSeconds(0.1f);
            }
            Shot("trooper_melee_hold");
            int hits = notes.Count(n => n.What == "hit" && n.T >= hold);
            Check("trooper-melee-hold", hits >= 3 && wander <= 4f, $"{hits} hits on the standing target in 8 s; farthest it stood {wander:F1} m");
            Clear();
            yield return new WaitForSeconds(1f);
        }

        IEnumerator RangedTest(CharacterBody p, System.Action<string, bool, string> Check)
        {
            Put(p, arena);
            notes.Clear();
            var t = SpawnEnemy("trooper_ranged", arena + arenaForward * 50f, true);
            if (!t) { Check("trooper-ranged", false, "could not spawn"); yield break; }
            float start = Time.time; float firstShot = -1f, nearest = 99f;
            while (Time.time - start < 25f)
            {
                var shot = notes.FirstOrDefault(n => n.What == "shot");
                if (shot.What != null && firstShot < 0f) firstShot = shot.Dist;
                if (firstShot >= 0f && Time.time - start > 8f) break;
                yield return new WaitForSeconds(0.2f);
            }
            // after the first shot: it holds its ground (no closer than the band)
            bool shotShot = false;
            for (float s = 0f; s < 5f; s += 0.2f)
            {
                nearest = Mathf.Min(nearest, Dist(p, t));
                if (!shotShot && Swinging(t)) { shotShot = true; yield return new WaitForSeconds(0.3f); LogAi(t, p); Shot("trooper_ranged_shooting"); }
                else if (Mathf.Repeat(s + 0.01f, 1f) < 0.2f) LogAi(t, p);
                yield return new WaitForSeconds(0.2f);
            }
            Shot("trooper_ranged_band");
            var shots = notes.Where(n => n.What == "shot").ToList();
            Check("trooper-ranged-approach", firstShot >= 14f && firstShot <= 31f && nearest >= 13f, $"first shot at {firstShot:F1} m, nearest after {nearest:F1} m");
            Check("trooper-ranged-no-oor", shots.Count > 0 && shots.All(n => n.Dist >= 6.5f && n.Dist <= 31f), $"{shots.Count} shots, at {string.Join(", ", shots.Select(n => n.Dist.ToString("F0")))} m");
            // the target goes past it and out the other side, beyond 30 m: it moves to catch up
            var from = Flat(t.footPosition);
            Put(p, Ground(t.footPosition + arenaForward * 40f));
            float moved = 0f;
            for (float s = 0f; s < 8f; s += 0.2f) { moved = Mathf.Max(moved, Vector3.Distance(from, Flat(t.footPosition))); yield return new WaitForSeconds(0.2f); }
            Check("trooper-ranged-follow", moved > 3f, $"moved {moved:F1} m after the target left the band ({Dist(p, t):F1} m now)");
            // back into the band: it stops and shoots, no extra steps
            var dir = Flat(p.footPosition - t.footPosition).normalized;
            Put(p, Ground(t.footPosition + dir * 22f));
            yield return new WaitForSeconds(1.5f);
            float back = Time.time; var stand = Flat(t.footPosition); float drift = 0f;
            for (float s = 0f; s < 5f; s += 0.2f) { drift = Mathf.Max(drift, Vector3.Distance(stand, Flat(t.footPosition))); yield return new WaitForSeconds(0.2f); }
            int again = notes.Count(n => n.What == "shot" && n.T >= back - 1.5f);
            Check("trooper-ranged-resume", again > 0 && drift <= 2f, $"{again} shots once back in range, moved {drift:F1} m meanwhile (at {Dist(p, t):F1} m)");
            Clear();
            yield return new WaitForSeconds(1f);
        }

        IEnumerator MedicTest(CharacterBody p, Vector3 side, System.Action<string, bool, string> Check)
        {
            Put(p, arena);
            notes.Clear();
            var g = arena + arenaForward * 12f;
            var group = new List<CharacterBody>
            {
                SpawnVanilla("BeetleMaster", g + side * 3f), SpawnVanilla("LemurianMaster", g - side * 3f),
                SpawnVanilla("BeetleMaster", g + arenaForward * 3f), SpawnEnemy("trooper_melee", g - arenaForward * 2f, false),
            }.Where(b => b).ToList();
            var medic = SpawnEnemy("trooper_medic", g + arenaForward * 6f, true);
            if (!medic || group.Count == 0) { Check("trooper-medic", false, "could not spawn the group"); yield break; }
            var pulse = medic.GetComponent<MedicPulse>();
            yield return new WaitForSeconds(2f);
            // the group is hurt steadily; each must climb back
            var healedBy = new Dictionary<CharacterBody, float>();
            HealthComponent.onCharacterHealServer += OnHeal;
            void OnHeal(HealthComponent hc, float amount, ProcChainMask _) { if (hc && hc.body) healedBy[hc.body] = (healedBy.TryGetValue(hc.body, out var a) ? a : 0f) + amount; }
            int pulses0 = pulse ? pulse.Pulses : 0, rings0 = MedicFx.RingsShown;
            for (float s = 0f; s < 8f; s += 0.5f)
            {
                foreach (var b in group.Append(medic))
                    if (b && b.healthComponent.alive && b.healthComponent.combinedHealthFraction > 0.4f)
                        b.healthComponent.TakeDamage(new DamageInfo { damage = b.healthComponent.fullHealth * 0.05f, position = b.corePosition, procCoefficient = 0f, damageColorIndex = DamageColorIndex.Default });
                if (Mathf.Abs(s - 4f) < 0.01f) Shot("trooper_medic_heal");
                if (Mathf.Repeat(s, 1f) < 0.01f && medic)
                    Log.Info("medic group: " + string.Join("; ", group.Select(g2 => g2 ? $"{g2.name.Replace("(Clone)", "")} {Vector3.Distance(g2.corePosition, medic.corePosition):F1} m hp {g2.healthComponent.health:F0}/{g2.healthComponent.fullHealth:F0} alive {g2.healthComponent.alive} team {g2.teamComponent.teamIndex}" : "gone")) + $"; medic at {medic.footPosition:F1}");
                yield return new WaitForSeconds(0.5f);
            }
            HealthComponent.onCharacterHealServer -= OnHeal;
            var unhealed = group.Where(b => b && (!healedBy.TryGetValue(b, out var a) || a <= 0f)).Select(b => b.name).ToList();
            float medicHealed = 0f;
            // the Medic's own regen counts as healing too: only a heal of its pulse size would be the pulse
            if (healedBy.TryGetValue(medic, out var mh)) medicHealed = mh;
            // with the Mending buff, Mending's own rules: the nearest hurt allies, up to its target cap
            var mend = pulse && pulse.Affix ? Object.FindObjectsOfType<HealNearbyController>().FirstOrDefault(h => h.GetComponent<NetworkedBodyAttachment>()?.attachedBody == medic) : null;
            int need = mend ? Mathf.Min(group.Count, Mathf.Max(1, mend.maxTargets)) : group.Count;
            Check("trooper-medic-heal", group.Count - unhealed.Count >= need && medicHealed < medic.healthComponent.fullHealth * 0.04f,
                $"healed {group.Count - unhealed.Count}/{group.Count}, needed {need} ({string.Join(", ", group.Select(b => $"{b.name.Replace("(Clone)", "")} +{(healedBy.TryGetValue(b, out var a) ? a : 0f):F0}"))}); the Medic itself +{medicHealed:F0}" + (unhealed.Count > 0 ? "; not healed: " + string.Join(", ", unhealed) : ""));
            int pulses = (pulse ? pulse.Pulses : 0) - pulses0, rings = MedicFx.RingsShown - rings0;
            if (pulse && pulse.Affix)
            {
                // the Mending elite's own healer attachment on the Medic, tethered to the hurt group (its green beams)
                var hnc = Object.FindObjectsOfType<HealNearbyController>().FirstOrDefault(h => h.GetComponent<NetworkedBodyAttachment>()?.attachedBody == medic);
                bool tethered = hnc && (bool)HarmonyLib.AccessTools.Field(typeof(HealNearbyController), "isTetheredToAtLeastOneObject").GetValue(hnc);
                Check("trooper-medic-fx", medic.HasBuff(pulse.Affix) && hnc && tethered,
                    $"Mending buff {medic.HasBuff(pulse.Affix)}, healer attachment {(hnc ? $"yes (radius {hnc.radius:F0} m, up to {hnc.maxTargets} at a time, {hnc.damagePerSecondCoefficient:F2} x damage {medic.damage:F0} a second)" : "no")}, tethered {tethered}, {pulses} seconds with someone to heal");
            }
            else
                Check("trooper-medic-fx", pulses >= 6 && rings >= pulses - 1 && MedicFx.BeamsShown > 0, $"{pulses} pulses in 8 s, {rings} rings and {MedicFx.BeamsShown} beams shown here");
            // nobody left to heal: it fights like a Ranged Trooper, 15-30 m away, shooting
            foreach (var b in group) if (b && b.master) { targets.Remove(b); b.master.TrueKill(); }
            notes.Clear();
            float d = 0f;
            for (float s = 0f; s < 14f; s += 0.2f) { d = Dist(p, medic); yield return new WaitForSeconds(0.2f); }
            Shot("trooper_medic_alone");
            int shots = notes.Count(n => n.What == "shot");
            Check("trooper-medic-alone", shots > 0 && d >= 9f && d <= 32f, $"{shots} shots with nobody to heal, standing {d:F1} m from the target");
            // its kill reward: a healing orb
            seen.Remove("medic-orb");
            medic.healthComponent.TakeDamage(new DamageInfo { damage = medic.healthComponent.fullCombinedHealth * 10f, attacker = p.gameObject, position = medic.corePosition, procCoefficient = 0f });
            yield return new WaitForSeconds(1f);
            Check("trooper-medic-orb", seen.Contains("medic-orb"), "a healing orb dropped where it died");
            Clear();
        }
    }
}
#endif
