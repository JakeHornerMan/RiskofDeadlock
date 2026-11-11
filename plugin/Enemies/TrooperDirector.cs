#if OWN_FILES
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using R2API;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain.Enemies
{
    /// <summary>Puts the enemies into every stage's monster pool through R2API.Director (creator, 2026-11-11: "Add R2API"):
    /// Basic Monsters on every stage from stage 1 (Commencement, the Void stages and Simulacrum's waves use the same pools), weight =
    /// the vanilla card's they copy x weightMult, and an enemy family event (family column) announced in chat. Only touched when
    /// R2API.Director is loaded (Plugin's soft dependency): without it the enemies are built but never spawn, and the log says so.</summary>
    public static class TrooperDirector
    {
        public const string DirectorGuid = "com.bepis.r2api.director";
        static readonly Dictionary<string, FamilyDirectorCardCategorySelection> families = new Dictionary<string, FamilyDirectorCardCategorySelection>();
        public static int StagesTouched;

        public static void Init()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(DirectorGuid))
            {
                Log.Warn("Enemies: R2API.Director is not installed, so the Troopers never spawn (install R2API_Director in r2modman)");
                return;
            }
            Hook();
        }

        // its own method, so R2API's types are only touched when R2API is there
        static void Hook()
        {
            DirectorAPI.MonsterActions += OnMonsters;
            Log.Info("Enemies: R2API.Director found; the Troopers join every stage's monsters");
        }

        static string VanillaName(string key) => key.Substring(key.LastIndexOf('/') + 1).Replace(".asset", "").ToLowerInvariant();

        static void OnMonsters(DccsPool pool, List<DirectorAPI.DirectorCardHolder> mix, DirectorAPI.StageInfo stage)
        {
            if (!Enemies.Enabled.Value || Enemies.Cards.Count == 0 || !pool) return;
            // the lobby's Troopers box (TrooperRule) set to off for this run
            if (!TrooperRule.OnThisRun()) { Log.Info("Enemies: Troopers switched off in the lobby for this run"); return; }
            StagesTouched++;
            var dccsList = new List<DirectorCardCategorySelection>();
            DirectorAPI.Helpers.ForEachPoolEntryInDccsPool(pool, e => { if (e != null && e.dccs) dccsList.Add(e.dccs); });
            foreach (var row in EnemyRow.All)
            {
                if (!Enemies.Cards.TryGetValue(row.Id, out var csc)) continue;
                // the copied monster's weight on this stage (1 where it isn't in the pool)
                string vanilla = VanillaName(row.CostFrom);
                int weight = 1;
                foreach (var d in dccsList)
                    foreach (var c in d.categories)
                        foreach (var card in c.cards ?? new DirectorCard[0])
                            if (card?.spawnCard && card.spawnCard.name.ToLowerInvariant() == vanilla) weight = Mathf.Max(weight, card.selectionWeight);
                var holder = new DirectorAPI.DirectorCardHolder
                {
                    Card = new DirectorCard
                    {
                        spawnCard = csc,
                        selectionWeight = Mathf.Max(1, Mathf.RoundToInt(weight * row.WeightMult)),
                        spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                        minimumStageCompletions = 0,
                        preventOverhead = false,
                    },
                    MonsterCategory = DirectorAPI.MonsterCategory.BasicMonsters,
                };
                DirectorAPI.Helpers.AddNewMonster(pool, mix, holder, false);
            }
            AddFamilies(pool);
        }

        /// <summary>One family event per family name (Troopers), offered on stages that have family events, like the Beetle family.</summary>
        static void AddFamilies(DccsPool pool)
        {
            foreach (var g in EnemyRow.All.Where(r => r.Family != "none" && Enemies.Cards.ContainsKey(r.Id)).GroupBy(r => r.Family))
            {
                if (!families.TryGetValue(g.Key, out var fam) || !fam)
                {
                    fam = ScriptableObject.CreateInstance<FamilyDirectorCardCategorySelection>();
                    ((ScriptableObject)fam).name = "dccsDeadlock" + g.Key + "Family";
                    var token = Sheet.Token("family", g.Key);
                    Survivors.Tokens[token] = "<style=cWorldEvent>You hear the march of candles...</style>";
                    AccessTools.Field(typeof(FamilyDirectorCardCategorySelection), "selectionChatString").SetValue(fam, token);
                    AccessTools.Field(typeof(FamilyDirectorCardCategorySelection), "minimumStageCompletion").SetValue(fam, 1);
                    int cat = fam.AddCategory("Basic Monsters", 1f);
                    foreach (var r in g)
                        fam.AddCard(cat, new DirectorCard { spawnCard = Enemies.Cards[r.Id], selectionWeight = Mathf.Max(1, Mathf.RoundToInt(3 * r.WeightMult)), spawnDistance = DirectorCore.MonsterSpawnDistance.Standard });
                    families[g.Key] = fam;
                }
                foreach (var c in pool.poolCategories)
                {
                    if (c.name != DirectorAPI.Helpers.MonsterPoolCategories.Family) continue;
                    var entries = c.includedIfConditionsMet;
                    if (entries == null || entries.Any(e => e.dccs == fam)) continue;
                    float w = entries.Length > 0 ? entries.Average(e => e.weight) : 1f;
                    c.includedIfConditionsMet = entries.Concat(new[] { new DccsPool.ConditionalPoolEntry { dccs = fam, weight = w, requiredExpansions = new RoR2.ExpansionManagement.ExpansionDef[0] } }).ToArray();
                }
            }
        }

        public static FamilyDirectorCardCategorySelection Family(string name) => families.TryGetValue(name, out var f) ? f : null;
    }

    /// <summary>The Medic only joins a fight (creator's spec): its card is turned down when no other living monster stands within
    /// 30 m of where it would spawn, and the director picks again.</summary>
    [HarmonyPatch(typeof(CombatDirector), "AttemptSpawnOnTarget")]
    static class MedicSpawnGate
    {
        public const float Range = 30f;
        public static int Refused;
        static readonly AccessTools.FieldRef<CombatDirector, DirectorCard> current = AccessTools.FieldRefAccess<CombatDirector, DirectorCard>("currentMonsterCard");

        static bool Prefix(CombatDirector __instance, Transform spawnTarget, ref bool __result)
        {
            var card = current(__instance);
            if (card == null || !card.spawnCard) return true;
            // the lobby switched the Troopers off: no Trooper card spawns (a pool or family event kept from an earlier run included)
            if (!TrooperRule.OnThisRun() && Enemies.Cards.ContainsValue(card.spawnCard as CharacterSpawnCard))
            {
                current(__instance) = null; __result = false; return false;
            }
            if (!Enemies.Cards.TryGetValue("trooper_medic", out var medic) || card.spawnCard != medic) return true;
            if (AlliesNear(spawnTarget ? spawnTarget.position : __instance.transform.position)) return true;
            current(__instance) = null;
            Refused++;
            __result = false;
            return false;
        }

        public static bool AlliesNear(Vector3 at)
        {
            foreach (var tc in TeamComponent.GetTeamMembers(TeamIndex.Monster))
            {
                var b = tc ? tc.body : null;
                if (b && b.healthComponent && b.healthComponent.alive && !Enemies.IsMedic(b) && (b.corePosition - at).sqrMagnitude < Range * Range) return true;
            }
            return false;
        }
    }
}
#endif
