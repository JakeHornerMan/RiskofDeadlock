using System.Collections.Generic;
using System.Linq;
using RoR2.UI;
using RoR2.UI.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadlockOfRain
{
    /// <summary>The "Join the Discord" button on RoR2's title menu (creator, 2026-10-24: "use the discord log and put the
    /// writing in the same style as risk of rain 2"). A copy of the title menu's own Singleplayer button (same frame, font,
    /// hover and click sound) in Discord blurple, in the bottom-right corner of the title screen (creator: "put it in the bottom
    /// right!"; the top right holds RoR2's profile box and event banner), as wide as a menu button and about twice as tall, with Discord's logo (`Assets/discord.png`, drawn by `tools/discord_logo.py`) on the left. It lives in the
    /// title screen, so it goes away with it. The invite never expires and gives everyone who joins through it the
    /// "Risk of Deadlock Player" role (doc 03 §9). Hidden in test mode.
    /// Above it, the same kind of button in the hero pages' brass opens the balance overview, the hero sheets' hub (every hero's
    /// damage next to its Risk of Rain match, with links to each kit sheet; creator, 2026-10-24: "a button ... just above the
    /// discord button that bring them to the characters sheet artifacts the link should open up the damage comparisons").</summary>
    public sealed class CommunityButton : MonoBehaviour
    {
        public const string Invite = "https://discord.gg/sNByetmcqc";
        /// <summary>The balance overview (docs/06 lists every page's link).</summary>
        public const string Pages = "https://claude.ai/artifact/5zx1qdr3LnXL2vhzJiebo3";
        static readonly Color Blurple = new Color(0.345f, 0.396f, 0.949f);
        static readonly Color Brass = new Color(0.788f, 0.639f, 0.357f);
        const float HeightScale = 2.2f, Margin = 48f, Gap = 16f;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static void Init()
        {
            Survivors.Tokens["DLR_DISCORD_JOIN"] = "Join the Risk of Deadlock Discord";
            Survivors.Tokens["DLR_DISCORD_HOVER"] = "The Risk of Deadlock community: FAQ, hero requests and news from the mod's maker.";
            Survivors.Tokens["DLR_PAGES_OPEN"] = "Deadlock Hero Info";
            Survivors.Tokens["DLR_PAGES_HOVER"] = "Every hero's abilities and numbers, and their damage next to their Risk of Rain 2 match (opens in your browser).";
            var go = new GameObject("DeadlockOfRainCommunity");
            DontDestroyOnLoad(go);
            go.AddComponent<CommunityButton>();
        }

        void LateUpdate()
        {
            var menu = MainMenuController.instance;
            if (!menu || !menu.titleMenuScreen || MeltyTest.Active) return;
            var screen = menu.titleMenuScreen.transform;
            if (screen.Find("DeadlockOfRainDiscord") && screen.Find("DeadlockOfRainPages")) return;
            var template = screen.GetComponentsInChildren<HGButton>(true).FirstOrDefault(b => b.GetComponentInChildren<LanguageTextMeshController>(true)
                && !b.name.StartsWith("DeadlockOfRain"));
            if (!template) return;
            // the Discord button in the corner, the hero pages' button one button-height plus a gap above it
            if (!screen.Find("DeadlockOfRainDiscord")) Build(screen, template, "DeadlockOfRainDiscord", "DLR_DISCORD_JOIN", "DLR_DISCORD_HOVER", Invite, Blurple, 0, Embedded("discord.png"));
            if (!screen.Find("DeadlockOfRainPages")) Build(screen, template, "DeadlockOfRainPages", "DLR_PAGES_OPEN", "DLR_PAGES_HOVER", Pages, Brass, 1, Embedded("heroes.png"));
        }

        static void Build(Transform screen, HGButton template, string name, string token, string hover, string url, Color colour, int row, Sprite icon)
        {
            var tr = (RectTransform)template.transform;
            var go = Instantiate(template.gameObject, screen, false);
            go.name = name;
            // the "new" badge: copied, it threw on quit (ViewableTag.OnDisable)
            foreach (var tag in go.GetComponentsInChildren<ViewableTag>(true)) DestroyImmediate(tag);
            go.SetActive(true);
            (go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>()).ignoreLayout = true;
            foreach (var lg in go.GetComponentsInChildren<LayoutGroup>(true)) lg.enabled = false;
            foreach (var f in go.GetComponentsInChildren<ContentSizeFitter>(true)) f.enabled = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            float w = tr.rect.width, h = tr.rect.height * HeightScale;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-Margin, Margin + row * (h + Gap));
            rt.localScale = tr.lossyScale.x > 0f && screen.lossyScale.x > 0f ? tr.lossyScale / screen.lossyScale.x : Vector3.one;

            var button = go.GetComponent<HGButton>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Application.OpenURL(url));
            button.interactable = true;
            button.hoverToken = hover;
            if (button.targetGraphic) button.targetGraphic.color = colour;   // HGButton keeps this as its resting colour
            if (button.hoverHighlightGameObject) button.hoverHighlightGameObject.SetActive(false);

            var label = go.GetComponentInChildren<LanguageTextMeshController>(true);
            label.token = token;
            // the text the label writes to: RoR2 keeps the controller on the button and the text on its ButtonText child (1.0.0
            // hid every text not on the controller's own object, so both buttons were blank)
            var text = label.GetComponent<TMP_Text>() ?? go.transform.Find("ButtonText")?.GetComponent<TMP_Text>() ?? go.GetComponentInChildren<TMP_Text>(true);
            // the image left, the text in the rest of the button: never on top of each other
            bool heroes = icon && icon.rect.width / icon.rect.height > 1.5f;   // the heads grid is drawn taller than the logo
            float logoH = h * (heroes ? 0.82f : 0.62f), logoW = icon ? logoH * icon.rect.width / icon.rect.height : 0f, left = h * (heroes ? 0.09f : 0.22f);
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp != text) { tmp.gameObject.SetActive(false); continue; }   // "NEW" tags and the like
                tmp.gameObject.SetActive(true);
                // up to two lines, shrinking to fit beside the image
                float size = tmp.fontSize * 1.6f;
                tmp.enableWordWrapping = true;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMax = size;
                tmp.fontSizeMin = size * 0.5f;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = Color.white;
                var lr = tmp.rectTransform;
                lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.pivot = new Vector2(0.5f, 0.5f);
                lr.offsetMin = new Vector2(icon ? left + logoW + left * 0.8f : left, 0f); lr.offsetMax = new Vector2(-8f, 0f);
                if (colour == Brass) tmp.color = new Color(0.051f, 0.067f, 0.071f);   // dark ink on brass, as the pages' keys
            }

            if (!icon) { Log.Info($"{name} added to the title screen, row {row} above the bottom-right corner ({w:F0}x{h:F0})"); return; }
            var logoGo = new GameObject(name + "Image", typeof(RectTransform), typeof(Image));
            var ir = (RectTransform)logoGo.transform;
            ir.SetParent(rt, false);
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0f, 0.5f);
            ir.sizeDelta = new Vector2(logoW, logoH);
            ir.anchoredPosition = new Vector2(left, 0f);
            var img = logoGo.GetComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;

            Log.Info($"{name} added to the title screen, row {row} above the bottom-right corner (copied from {template.name}, {w:F0}x{h:F0}, text '{text?.text}' at {text?.fontSize:F0} pt)");
        }

        /// <summary>An image embedded in the DLL (plugin/Assets: discord.png from tools/discord_logo.py, heroes.png from tools/hero_strip.py).</summary>
        static Sprite Embedded(string file)
        {
            if (sprites.TryGetValue(file, out var sp) && sp) return sp;
            using (var s = typeof(CommunityButton).Assembly.GetManifestResourceStream("DeadlockOfRain." + file))
            {
                var bytes = new byte[s.Length];
                s.Read(bytes, 0, bytes.Length);
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(bytes);
                return sprites[file] = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
            }
        }
    }
}
