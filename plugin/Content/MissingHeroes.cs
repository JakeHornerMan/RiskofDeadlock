using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>A released hero the extractor could not read from this player's Deadlock keeps a locked tile in character
    /// select: a red "!" where the portrait would be, and the reason and both version pairs on hover. The tile has an
    /// unlockable nobody ever gets, so it can never be picked, started or joined with; the bodies and defs live apart from
    /// Survivors.Bodies/Defs, so the bench, test mode, co-op and the mod list never see them.</summary>
    public static class MissingHeroes
    {
        public static readonly Dictionary<string, GameObject> Bodies = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, SurvivorDef> Defs = new Dictionary<string, SurvivorDef>();
        public static readonly Dictionary<string, UnlockableDef> Unlockables = new Dictionary<string, UnlockableDef>();
        static readonly HashSet<SurvivorDef> placeholders = new HashSet<SurvivorDef>();
        static Texture2D icon;

        public static bool Is(SurvivorDef def) => def && placeholders.Contains(def);

        public static void Add(GameObject commando, Transform holder, HeroRow hero, string why)
        {
            var code = Survivors.CodeName(hero);
            var body = UnityEngine.Object.Instantiate(commando, holder);
            body.name = "Deadlock" + code + "MissingBody";
            var ni = body.GetComponent<NetworkIdentity>();
            if (ni)
            {
                var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("DeadlockOfRain." + body.name));
                AccessTools.Field(typeof(NetworkIdentity), "m_AssetId").SetValue(ni, NetworkHash128.Parse(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()));
            }
            var cb = body.GetComponent<CharacterBody>();
            cb.baseNameToken = "DLR_MISSING_NAME_" + hero.Id.ToUpperInvariant();
            cb.subtitleNameToken = cb.baseNameToken;
            cb.portraitIcon = Icon();
            Survivors.Tokens[cb.baseNameToken] = hero.DisplayName;

            var text = $"{hero.DisplayName} couldn't be read from your Deadlock ({why}). The other heroes are ready to play.\n\n{BuiltWith.Versions()}";
            var unlock = ScriptableObject.CreateInstance<UnlockableDef>();
            unlock.cachedName = "DeadlockOfRain.Missing." + hero.Id;
            unlock.nameToken = cb.baseNameToken;
            unlock.hidden = true;
            unlock.getHowToUnlockString = () => text;
            unlock.getUnlockedString = () => text;

            var display = new GameObject("Deadlock" + code + "MissingDisplay");
            display.transform.SetParent(holder, false);
            var sd = ScriptableObject.CreateInstance<SurvivorDef>();
            ((ScriptableObject)sd).name = "Deadlock" + code + "Missing";
            sd.cachedName = "Deadlock" + code + "Missing";
            sd.bodyPrefab = body;
            sd.displayPrefab = display;
            sd.displayNameToken = cb.baseNameToken;
            sd.descriptionToken = cb.baseNameToken;
            sd.primaryColor = Color.red;
            sd.desiredSortPosition = hero.SortPosition;
            sd.unlockableDef = unlock;

            Bodies[hero.Id] = body;
            Defs[hero.Id] = sd;
            Unlockables[hero.Id] = unlock;
            placeholders.Add(sd);
            Log.Warn($"{hero.DisplayName}: shown as a locked red ! tile in character select ({why})");
        }

        /// <summary>A red "!" in a dark red disc, drawn here (no Deadlock art).</summary>
        static Texture2D Icon()
        {
            if (icon) return icon;
            const int n = 128;
            icon = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "DeadlockOfRainMissing", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            var disc = new Color32(70, 8, 8, 255);
            var mark = new Color32(235, 40, 40, 255);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - n / 2f + 0.5f, dy = y - n / 2f + 0.5f;
                    bool inDisc = dx * dx + dy * dy <= (n / 2f - 2f) * (n / 2f - 2f);
                    // texture rows go up: the bar above, the dot below
                    bool bar = Mathf.Abs(dx) <= 9f && y >= 46 && y <= 108;
                    bool dot = dx * dx + (y - 28f) * (y - 28f) <= 110f;
                    px[y * n + x] = !inDisc ? new Color32(0, 0, 0, 0) : bar || dot ? mark : disc;
                }
            icon.SetPixels32(px);
            icon.Apply();
            return icon;
        }

        /// <summary>Nobody has the placeholder unlocked, whatever the profile says.</summary>
        [HarmonyPatch(typeof(UserProfile), nameof(UserProfile.HasSurvivorUnlocked))]
        static class LockPatch
        {
            static bool Prefix(SurvivorIndex survivorIndex, ref bool __result)
            {
                if (!Is(SurvivorCatalog.GetSurvivorDef(survivorIndex))) return true;
                __result = false;
                return false;
            }
        }

        /// <summary>Locked tiles are drawn black ("UNIDENTIFIED"); ours keeps the red "!" and names the hero.</summary>
        [HarmonyPatch(typeof(SurvivorIconController), "Rebuild")]
        static class IconPatch
        {
            static void Postfix(SurvivorIconController __instance)
            {
                var def = __instance.survivorDef;
                if (!Is(def)) return;
                if (__instance.survivorIcon) __instance.survivorIcon.color = Color.white;
                var title = Language.GetString(def.displayNameToken) + ": not available";
                var body = def.unlockableDef ? def.unlockableDef.getHowToUnlockString() : "";
                var red = new Color(0.9f, 0.2f, 0.2f, 1f);
                if (__instance.tooltipProvider)
                {
                    __instance.tooltipProvider.enabled = true;
                    __instance.tooltipProvider.overrideTitleText = title;
                    __instance.tooltipProvider.titleColor = red;
                    __instance.tooltipProvider.overrideBodyText = body;
                }
                var button = __instance.GetComponent<HGButton>();
                if (button)
                {
                    red.a = 0.2f;
                    button.hoverToken = Language.GetStringFormatted("LOGBOOK_HOVER_DESCRIPTION_FORMAT", title, body, ColorUtility.ToHtmlStringRGBA(red));
                }
            }
        }
    }
}
