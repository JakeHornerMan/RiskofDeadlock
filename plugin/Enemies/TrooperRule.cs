#if OWN_FILES
using System;
using System.Linq;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadlockOfRain.Enemies
{
    /// <summary>The lobby's "Risk of Deadlock" box with its "Troopers" checkbox (creator, 2026-11-11: "underneath the Karma on the right side can we have a orange box in
    /// the same style with a check box for Troopers this will enable and disable troopers in a players game"): a rule category of
    /// RoR2's own rule catalogue, added straight after Karma, with one rule whose two choices are a ticked and an empty box. The lobby
    /// votes it and the run's rule book carries it to every player, like the artifacts. Off: TrooperDirector leaves the Troopers out of
    /// that run's spawn pools. Only made when the Troopers are built at all (config Enemies / Troopers).</summary>
    [HarmonyPatch(typeof(RuleCatalog), "AddCategory", new[] { typeof(string), typeof(string), typeof(Color), typeof(string), typeof(string),
        typeof(Func<bool>), typeof(RuleCatalog.RuleCategoryType), typeof(RuleCategoryDef.GridFormattingOverride) })]
    static class TrooperRule
    {
        public const string Header = "DLR_RULE_HEADER_TROOPERS", RuleName = "Deadlock.Troopers";
        public static readonly Color Orange = new Color32(236, 132, 32, 255);
        public static RuleDef Def;
        /// <summary>The Enemy test scene tick (enemyTestScene.json; off by default).</summary>
        public static RuleDef TestDef;
        public const string TestRuleName = "Deadlock.EnemyTestScene";
        static bool adding;

        // the catalogue adds its categories in order; ours goes in just before Items, so it sits under Karma
        static void Prefix(string displayToken)
        {
            if (adding || Def != null || displayToken != "RULE_HEADER_ITEMS") return;
            if (Enemies.Enabled == null || !Enemies.Enabled.Value) return;
            adding = true;
            try { Add(); }
            catch (Exception e) { Log.Warn($"Enemies: the lobby's Troopers box could not be added: {e.Message}"); }
            finally { adding = false; }
        }

        static void Add()
        {
            var tokens = Survivors.Tokens;
            // the box is Risk of Deadlock's; the Troopers are a ticked item in it (creator, 2026-11-11: "the title should be Risk fo
            // Deadlock and troopers should be in the box witha check box that will turn them on and off")
            tokens[Header] = "Risk of Deadlock";
            tokens["DLR_RULE_HEADER_TROOPERS_SUBTITLE"] = "Risk of Deadlock's extras for this run";
            tokens["DLR_RULE_HEADER_TROOPERS_EDIT"] = "Tick or untick the Troopers for this run";
            tokens["DLR_RULE_TROOPERS_NAME"] = "Troopers";
            tokens["DLR_RULE_TROOPERS_ON_DESC"] = "<style=cIsHealing>On:</style> Melee, Ranged and Medic Troopers spawn with the other monsters, from stage 1.";
            tokens["DLR_RULE_TROOPERS_OFF_DESC"] = "<style=cDeath>Off:</style> no Troopers spawn in this run.";
            AccessTools.Method(typeof(RuleCatalog), "AddCategory", new[] { typeof(string), typeof(string), typeof(Color), typeof(string), typeof(string),
                typeof(Func<bool>), typeof(RuleCatalog.RuleCategoryType), typeof(RuleCategoryDef.GridFormattingOverride) })
                .Invoke(null, new object[] { Header, "DLR_RULE_HEADER_TROOPERS_SUBTITLE", Orange, "", "DLR_RULE_HEADER_TROOPERS_EDIT",
                    (Func<bool>)(() => false), RuleCatalog.RuleCategoryType.VoteResultGrid, null });
            var def = new RuleDef(RuleName, "DLR_RULE_TROOPERS_NAME") { forceLobbyDisplay = true };
            var on = def.AddChoice("On", true);
            on.tooltipNameToken = "DLR_RULE_TROOPERS_NAME"; on.tooltipBodyToken = "DLR_RULE_TROOPERS_ON_DESC";
            on.tooltipNameColor = Orange; on.sprite = Box(true); on.onlyShowInGameBrowserIfNonDefault = true;
            var off = def.AddChoice("Off", false);
            off.tooltipNameToken = "DLR_RULE_TROOPERS_NAME"; off.tooltipBodyToken = "DLR_RULE_TROOPERS_OFF_DESC";
            off.tooltipNameColor = Orange; off.sprite = Box(false); off.onlyShowInGameBrowserIfNonDefault = true;
            def.defaultChoiceIndex = 0;
            AccessTools.Method(typeof(RuleCatalog), "AddRule").Invoke(null, new object[] { def });
            Def = def;
            // the Enemy test scene (creator, 2026-11-13: "call it enemy test scene you access it through our custom risk of deadlock
            // section on the character selection screen"): off by default
            var ts = EnemyTestSceneRow.All.FirstOrDefault();
            if (ts != null)
            {
                tokens["DLR_RULE_ENEMYTEST_NAME"] = ts.Name;
                tokens["DLR_RULE_ENEMYTEST_ON_DESC"] = $"<style=cIsHealing>On:</style> the run starts on a test stage where one of each Trooper appears every {ts.Interval:0.#} seconds (at most {ts.MaxAlive} at once); the stage spawns no other monsters.";
                tokens["DLR_RULE_ENEMYTEST_OFF_DESC"] = "<style=cDeath>Off:</style> a normal run.";
                var test = new RuleDef(TestRuleName, "DLR_RULE_ENEMYTEST_NAME") { forceLobbyDisplay = true };
                var tOff = test.AddChoice("Off", false);
                tOff.tooltipNameToken = "DLR_RULE_ENEMYTEST_NAME"; tOff.tooltipBodyToken = "DLR_RULE_ENEMYTEST_OFF_DESC";
                tOff.tooltipNameColor = Orange; tOff.sprite = Box(false); tOff.onlyShowInGameBrowserIfNonDefault = true;
                var tOn = test.AddChoice("On", true);
                tOn.tooltipNameToken = "DLR_RULE_ENEMYTEST_NAME"; tOn.tooltipBodyToken = "DLR_RULE_ENEMYTEST_ON_DESC";
                tOn.tooltipNameColor = Orange; tOn.sprite = Box(true); tOn.onlyShowInGameBrowserIfNonDefault = true;
                test.defaultChoiceIndex = 0;
                AccessTools.Method(typeof(RuleCatalog), "AddRule").Invoke(null, new object[] { test });
                TestDef = test;
            }
            Log.Info("Enemies: the lobby has a Risk of Deadlock box: Troopers (on by default), Enemy test scene (off by default)");
        }

        /// <summary>The lobby ticked the Enemy test scene for this run (or the lobby that is open has it ticked).</summary>
        public static bool TestSceneOn()
        {
            if (TestDef == null) return false;
            var book = Run.instance && Run.instance.ruleBook != null ? Run.instance.ruleBook : PreGameController.instance ? PreGameController.instance.readOnlyRuleBook : null;
            return book != null && book.GetRuleChoice(TestDef)?.extraData is bool b && b;
        }

        /// <summary>This run's choice: true unless the lobby switched the Troopers off (and true outside a run, or without the rule).</summary>
        public static bool OnThisRun()
        {
            if (Def == null || !Run.instance || Run.instance.ruleBook == null) return true;
            return !(Run.instance.ruleBook.GetRuleChoice(Def)?.extraData is bool b) || b;
        }

        /// <summary>The choice's icon in the lobby gets the word "Troopers" beside it, in the lobby's own font (the grid shows icons
        /// only).</summary>
        [HarmonyPatch(typeof(RuleChoiceController), nameof(RuleChoiceController.SetChoice))]
        static class Label
        {
            const string Name = "DeadlockTroopersLabel";

            static void Postfix(RuleChoiceController __instance, RuleChoiceDef newChoiceDef)
            {
                try
                {
                    string label = newChoiceDef == null ? null : Def != null && newChoiceDef.ruleDef == Def ? "Troopers"
                        : TestDef != null && newChoiceDef.ruleDef == TestDef ? (EnemyTestSceneRow.All.FirstOrDefault()?.Name ?? "Enemy test scene") : null;
                    bool ours = label != null;
                    var old = __instance.transform.Find(Name);
                    if (!ours) { if (old) old.gameObject.SetActive(false); return; }
                    if (old) { old.gameObject.SetActive(true); var ot = old.GetComponent<TMP_Text>(); if (ot) ot.text = label; return; }
                    var cat = __instance.GetComponentInParent<RuleCategoryController>();
                    TMP_Text src = cat && cat.categoryHeaderLanguageController ? cat.categoryHeaderLanguageController.GetComponent<TMP_Text>() : null;
                    if (!src) src = Object.FindObjectOfType<TextMeshProUGUI>();
                    var go = new GameObject(Name, typeof(RectTransform));
                    var rt = (RectTransform)go.transform;
                    rt.SetParent(__instance.transform, false);
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(10f, 0f);
                    rt.sizeDelta = new Vector2(220f, 40f);
                    var le = go.AddComponent<UnityEngine.UI.LayoutElement>(); le.ignoreLayout = true;
                    var text = go.AddComponent<HGTextMeshProUGUI>();
                    if (src) { text.font = src.font; text.fontSharedMaterial = src.fontSharedMaterial; }
                    text.text = label;
                    text.fontSize = src ? Mathf.Max(18f, src.fontSize * 0.85f) : 24f;
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.color = Color.white;
                    text.raycastTarget = false;
                    text.enableWordWrapping = false;
                }
                catch (Exception e) { Log.Warn($"Enemies: Troopers label: {e.Message}"); }
            }
        }

        /// <summary>The choice's icon, drawn here: a square box in the Troopers' orange, ticked when on.</summary>
        internal static Sprite BoxSprite(bool ticked) => Box(ticked);

        static Sprite Box(bool ticked)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = ticked ? "texDeadlockRuleTroopersOn" : "texDeadlockRuleTroopersOff", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            Color32 edge = Orange, fill = new Color32(40, 24, 10, 230), tick = new Color32(255, 240, 220, 255), clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int m = 14, w = 12;
                    bool inside = x >= m && x < n - m && y >= m && y < n - m;
                    bool border = inside && (x < m + w || x >= n - m - w || y < m + w || y >= n - m - w);
                    var c = !inside ? clear : border ? edge : fill;
                    if (ticked && inside && !border && OnTick(x, y, n)) c = tick;
                    px[y * n + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        // a tick mark: two thick strokes, short down-right then long up-right (texture y runs upward)
        static bool OnTick(int x, int y, int n)
        {
            var p = new Vector2(x, y) / n;
            return Near(p, new Vector2(0.27f, 0.52f), new Vector2(0.43f, 0.33f)) || Near(p, new Vector2(0.43f, 0.33f), new Vector2(0.75f, 0.72f));
        }

        static bool Near(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude < 0.065f;
        }
    }
}
#endif
