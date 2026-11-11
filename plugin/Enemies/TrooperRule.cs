#if OWN_FILES
using System;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain.Enemies
{
    /// <summary>The lobby's "Troopers" box (creator, 2026-11-11: "underneath the Karma on the right side can we have a orange box in
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
            tokens[Header] = "Troopers";
            tokens["DLR_RULE_HEADER_TROOPERS_SUBTITLE"] = "Deadlock's lane Troopers among the monsters";
            tokens["DLR_RULE_HEADER_TROOPERS_EDIT"] = "Switch the Troopers on or off for this run";
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
            Log.Info("Enemies: the lobby has a Troopers box (on by default)");
        }

        /// <summary>This run's choice: true unless the lobby switched the Troopers off (and true outside a run, or without the rule).</summary>
        public static bool OnThisRun()
        {
            if (Def == null || !Run.instance || Run.instance.ruleBook == null) return true;
            return !(Run.instance.ruleBook.GetRuleChoice(Def)?.extraData is bool b) || b;
        }

        /// <summary>The choice's icon, drawn here: a square box in the Troopers' orange, ticked when on.</summary>
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
