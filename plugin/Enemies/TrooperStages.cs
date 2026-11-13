#if OWN_FILES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace DeadlockOfRain.Enemies
{
    /// <summary>Which stages the Troopers may spawn on (creator, 2026-11-11, "Feature: Trooper stage selection"): every stage that
    /// has a monster pool in the game's own data (the addressable pools RoR2/&lt;pack&gt;/&lt;scene&gt;/dp*Monsters.asset, matched to
    /// SceneCatalog), grouped Stage 1-5 / Final / Hidden Realms / Simulacrum; the default is every stage whose pool has the vanilla
    /// Lemurian (the Ranged Trooper's costFrom card), read from those pools. The choice is the config entry Enemies / TrooperStages
    /// ("default", or the stages' base scene names comma-separated), read on the host. Stages of a DLC the players don't own are
    /// never listed.</summary>
    public static class TrooperStages
    {
        public sealed class Stage
        {
            public string Key;              // the base scene name (golemplains); variants that share it are one row
            public SceneDef Scene;
            public string Group;
            public int GroupOrder;
            public bool Lemurian;
            public string Name
            {
                get
                {
                    if (!Scene) return Key;
                    // Simulacrum stages all read "The Simulacrum": named after the stage they copy
                    if (Group == "Simulacrum") { var b = BaseOf(Scene); if (b) return "Simulacrum: " + Language.GetString(b.nameToken); }
                    return Language.GetString(Scene.nameToken);
                }
            }
            public bool Owned
            {
                get
                {
                    var exp = Scene ? Scene.requiredExpansion : null;
                    if (!exp || !exp.requiredEntitlement) return true;
                    var t = RoR2.EntitlementManagement.EntitlementManager.localUserEntitlementTracker;
                    return t == null || t.AnyUserHasEntitlement(exp.requiredEntitlement);
                }
            }
        }

        public const string Default = "default";
        public static ConfigEntry<string> Selected;
        public static readonly List<Stage> All = new List<Stage>();
        public static bool Ready;
        /// <summary>Raised when the selection changes (the lobby dropdown redraws).</summary>
        public static event Action Changed;

        public static void Init(ConfigFile config)
        {
            Selected = config.Bind("Enemies", "TrooperStages", Default,
                "Stages the Troopers may spawn on: \"default\" (every stage the Lemurian spawns on, read from the game), or base scene names " +
                "separated by commas (golemplains,blackbeach,...); empty = none. Set in the lobby's Risk of Deadlock box. The host's choice counts.");
            RoR2Application.onLoad += () => Plugin.Instance.StartCoroutine(Scan());
        }

        /// <summary>The stages shown in the lobby: owned ones only, in group order.</summary>
        public static IEnumerable<Stage> Visible => All.Where(s => s.Owned).OrderBy(s => s.GroupOrder).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase);

        public static HashSet<string> LemurianKeys => new HashSet<string>(All.Where(s => s.Lemurian).Select(s => s.Key), StringComparer.OrdinalIgnoreCase);

        public static HashSet<string> Current()
        {
            var v = Selected != null ? Selected.Value.Trim() : Default;
            if (v.Equals(Default, StringComparison.OrdinalIgnoreCase)) return LemurianKeys;
            return new HashSet<string>(v.Split(',').Select(x => x.Trim()).Where(x => x != ""), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsDefault => Selected == null || Selected.Value.Trim().Equals(Default, StringComparison.OrdinalIgnoreCase);

        public static void Set(IEnumerable<string> keys)
        {
            Selected.Value = string.Join(",", keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
            Changed?.Invoke();
        }

        public static void Toggle(string key, bool on)
        {
            var cur = Current();
            if (on) cur.Add(key); else cur.Remove(key);
            Set(cur);
        }

        public static void Reset() { Selected.Value = Default; Changed?.Invoke(); }

        /// <summary>The key a scene is listed under (its base scene name).</summary>
        public static string KeyOf(SceneDef s) => s ? (string.IsNullOrEmpty(s.baseSceneName) ? s.cachedName : s.baseSceneName) : null;

        /// <summary>Whether the Troopers may join this scene's monsters. A scene with no row (no monster pool of its own) and the time
        /// before the stage list is read count as allowed.</summary>
        public static bool Allowed(SceneDef scene)
        {
            var key = KeyOf(scene);
            if (key == null || !Ready || !All.Any(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase))) return true;
            return Current().Contains(key);
        }

        static readonly Regex PoolKey = new Regex(@"^RoR2/[^/]+/(?:[^/]+/)*?([^/]+)/dp[^/]*Monsters\.asset$", RegexOptions.IgnoreCase);

        static IEnumerator Scan()
        {
            if (Enemies.Enabled == null || !Enemies.Enabled.Value) yield break;
            // the game's monster pools, by the folder they sit in (the scene's base name)
            var pools = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var loc in Addressables.ResourceLocators)
                foreach (var k in loc.Keys)
                {
                    if (!(k is string s)) continue;
                    var m = PoolKey.Match(s);
                    if (!m.Success) continue;
                    var folder = m.Groups[1].Value;
                    if (folder.StartsWith("_") || folder.Equals("testingscene", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!pools.TryGetValue(folder, out var list)) pools[folder] = list = new List<string>();
                    if (!list.Contains(s)) list.Add(s);
                }
            // the Lemurian card: by object, or by the address GUID the newer pools point with
            var lemRow = EnemyRow.All.FirstOrDefault(r => r.Id == "trooper_ranged");
            string lemAddr = lemRow != null ? lemRow.CostFrom : "RoR2/Base/Lemurian/cscLemurian.asset";
            string lemName = lemAddr.Substring(lemAddr.LastIndexOf('/') + 1).Replace(".asset", "");
            var lemGuid = GuidOf(lemAddr);
            var found = new List<Stage>();
            foreach (var kv in pools)
            {
                var scenes = SceneCatalog.allSceneDefs.Where(d => d && (KeyOf(d).Equals(kv.Key, StringComparison.OrdinalIgnoreCase) || d.cachedName.Equals(kv.Key, StringComparison.OrdinalIgnoreCase))).ToList();
                if (scenes.Count == 0) continue;
                var scene = scenes.FirstOrDefault(d => d.cachedName.Equals(KeyOf(d), StringComparison.OrdinalIgnoreCase)) ?? scenes[0];
                bool lem = false;
                foreach (var key in kv.Value)
                {
                    var h = Addressables.LoadAssetAsync<DccsPool>(key);
                    while (!h.IsDone) yield return null;
                    if (h.Result && HasCard(h.Result, lemName, lemGuid)) lem = true;
                    // read once and let go (the stage loads its own pool when it plays)
                    Addressables.Release(h);
                }
                var st = new Stage { Key = KeyOf(scene), Scene = scene, Lemurian = lem };
                (st.Group, st.GroupOrder) = GroupOf(scene);
                if (!found.Any(f => f.Key.Equals(st.Key, StringComparison.OrdinalIgnoreCase))) found.Add(st);
            }
            All.Clear(); All.AddRange(found);
            Ready = true;
            Log.Info($"Enemies: {All.Count} stages with monster pools; the Lemurian's (default): {string.Join(", ", All.Where(s => s.Lemurian).Select(s => $"{s.Name} [{s.Key}, {s.Group}]"))}");
            Log.Info($"Enemies: other stages: {string.Join(", ", All.Where(s => !s.Lemurian).Select(s => $"{s.Name} [{s.Key}, {s.Group}]"))}");
            // the creator's base-game list (spec cross-check): what the game data says differently
            var spec = new[] { "blackbeach", "golemplains", "lakes", "goolake", "foggyswamp", "frozenwall", "dampcave", "rootjungle", "artifactworld", "goldshores", "arena" };
            var lemKeys = LemurianKeys;
            var missing = spec.Where(k => All.Any(s => s.Key.Equals(k, StringComparison.OrdinalIgnoreCase)) && !lemKeys.Contains(k)).ToList();
            var extra = All.Where(s => s.Lemurian && s.Scene && !s.Scene.requiredExpansion && !spec.Contains(s.Key, StringComparer.OrdinalIgnoreCase)).Select(s => s.Key).ToList();
            Log.Info($"Enemies: the Lemurian list against the spec's base-game list: not Lemurian in the game data: {(missing.Count > 0 ? string.Join(", ", missing) : "none")}; " +
                     $"Lemurian base-game stages not in the spec: {(extra.Count > 0 ? string.Join(", ", extra) : "none")}");
            Changed?.Invoke();
        }

        static bool HasCard(DccsPool pool, string name, string guid)
        {
            foreach (var c in pool.poolCategories ?? new DccsPool.Category[0])
            {
                var entries = (c.alwaysIncluded ?? new DccsPool.PoolEntry[0])
                    .Concat(c.includedIfConditionsMet ?? new DccsPool.ConditionalPoolEntry[0])
                    .Concat(c.includedIfNoConditionsMet ?? new DccsPool.PoolEntry[0]);
                foreach (var e in entries)
                    foreach (var cat in e?.dccs ? e.dccs.categories : new DirectorCardCategorySelection.Category[0])
                        foreach (var card in cat.cards ?? new DirectorCard[0])
                        {
                            if (card == null) continue;
                            if (card.spawnCard && card.spawnCard.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
                            if (guid != null && card.spawnCardReference != null && string.Equals(card.spawnCardReference.AssetGUID, guid, StringComparison.OrdinalIgnoreCase)) return true;
                        }
            }
            return false;
        }

        /// <summary>The 32-character GUID key Addressables also knows an address by (null if not found).</summary>
        static string GuidOf(string address)
        {
            try
            {
                foreach (var loc in Addressables.ResourceLocators)
                {
                    if (!loc.Locate(address, typeof(SpawnCard), out IList<IResourceLocation> locs) || locs.Count == 0) continue;
                    var id = locs[0].InternalId;
                    foreach (var k in loc.Keys)
                        if (k is string s && s.Length == 32 && s.All(Uri.IsHexDigit) && loc.Locate(s, typeof(SpawnCard), out IList<IResourceLocation> l2) && l2.Count > 0 && l2[0].InternalId == id)
                            return s;
                }
            }
            catch (Exception e) { Log.Warn($"Enemies: no GUID for {address}: {e.Message}"); }
            return null;
        }

        // Simulacrum scenes are it<stage>; hidden realms are the ones the game names "Hidden Realm: ..."; stageOrder 1-5 the loop's
        // stages; the rest (Commencement, Prime Meridian, Solutional Haunt...) the finals (2026-11-11: stageOrder alone put Void
        // Fields under Final and the Simulacrum there too)
        static (string, int) GroupOf(SceneDef d)
        {
            if (IsSimulacrum(d)) return ("Simulacrum", 9);
            if (d.sceneType == SceneType.Intermission || Language.GetString(d.nameToken, "en").StartsWith("Hidden Realm", StringComparison.OrdinalIgnoreCase)) return ("Hidden Realms", 8);
            if (d.stageOrder >= 1 && d.stageOrder <= 5) return ("Stage " + d.stageOrder, d.stageOrder);
            return ("Final", 7);
        }

        static bool IsSimulacrum(SceneDef d) => d.cachedName.StartsWith("it", StringComparison.OrdinalIgnoreCase) && BaseOf(d);

        /// <summary>The stage a Simulacrum scene copies (itdampcave: dampcavesimple), or null.</summary>
        static SceneDef BaseOf(SceneDef d)
        {
            var k = d.cachedName.Substring(2);
            return SceneCatalog.FindSceneDef(k) ?? SceneCatalog.allSceneDefs.FirstOrDefault(s => s && s != d && s.cachedName.StartsWith(k, StringComparison.OrdinalIgnoreCase));
        }
    }
}
#endif
