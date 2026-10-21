using HarmonyLib;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>The magazine as "rounds/clip" just above the M1 icon of the skill bar, for every hero whose gun has a clipSize
    /// (just information: grey while a reload is under way); in a goo puddle Viscous's puddle rounds go in front ("20+5/20"). Rounds are kept by the owner (HeroRuntime.Ammo), so it shows only
    /// for the body this machine moves and is hidden for vanilla survivors and while spectating.</summary>
    [HarmonyPatch(typeof(SkillIcon), "LateUpdate")]
    static class AmmoCounter
    {
        static void Postfix(SkillIcon __instance)
        {
            if (__instance.targetSkillSlot != SkillSlot.Primary) return;
            var label = __instance.GetComponent<AmmoLabel>();
            if (!label) label = __instance.gameObject.AddComponent<AmmoLabel>();
            label.Show(__instance);
        }
    }

    sealed class AmmoLabel : MonoBehaviour
    {
        static readonly Color Full = Color.white, Reload = new Color(0.6f, 0.6f, 0.6f);
        /// <summary>The last text shown and whose it was (test mode checks it).</summary>
        public static string Shown; public static CharacterBody ShownFor;
        TextMeshProUGUI text;
        int shownAmmo = int.MinValue, shownClip = -1, shownExtra = -1; bool shownReload;

        public void Show(SkillIcon icon)
        {
            var gs = icon.targetSkill;
            var body = gs ? gs.characterBody : null;
            var gun = Survivors.RowFor(gs);
            var rt = body ? body.GetComponent<HeroRuntime>() : null;
            bool on = gun != null && gun.ClipSize > 0 && rt && body.hasEffectiveAuthority;
            if (!on) { if (text) text.gameObject.SetActive(false); return; }
            if (!text) Build(icon);
            if (!text) return;
            int clip = (int)gun.ClipSize, ammo = rt.Ammo < 0 ? clip : rt.Ammo, extra = rt.Extra;
            if (!text.gameObject.activeSelf) text.gameObject.SetActive(true);
            if (ammo == shownAmmo && clip == shownClip && extra == shownExtra && rt.Reloading == shownReload) return;
            shownAmmo = ammo; shownClip = clip; shownExtra = extra; shownReload = rt.Reloading;
            // a goo puddle's rounds (Viscous) in front, in goo green: spent first, gone when he steps out
            string front = extra > 0 ? $"{extra}+" : "";
            text.SetText(extra > 0 ? $"<color=#8CFF5A>{front}</color>{ammo}/{clip}" : $"{ammo}/{clip}");
            Shown = $"{front}{ammo}/{clip}"; ShownFor = body;
            text.color = rt.Reloading ? Reload : Full;
        }

        void Build(SkillIcon icon)
        {
            var go = new GameObject("DlrAmmo", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(icon.transform, false);
            // centred on the icon's top edge, sitting just above it
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 4f);
            rect.sizeDelta = new Vector2(160f, 28f);
            text = go.AddComponent<HGTextMeshProUGUI>();
            // the skill bar's own font (its stock number), so it reads like the rest of the HUD
            var like = icon.stockText ? icon.stockText : icon.cooldownText;
            if (like) { text.font = like.font; text.fontSharedMaterial = like.fontSharedMaterial; }
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Bottom;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
        }
    }
}
