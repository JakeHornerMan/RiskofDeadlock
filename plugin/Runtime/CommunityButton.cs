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
    /// "Risk of Deadlock Player" role (doc 03 §9). Hidden in test mode.</summary>
    public sealed class CommunityButton : MonoBehaviour
    {
        public const string Invite = "https://discord.gg/sNByetmcqc";
        static readonly Color Blurple = new Color(0.345f, 0.396f, 0.949f);
        const float HeightScale = 2.2f, Margin = 48f;
        static Sprite logo;

        public static void Init()
        {
            Survivors.Tokens["DLR_DISCORD_JOIN"] = "Join the Discord";
            Survivors.Tokens["DLR_DISCORD_HOVER"] = "The Risk of Deadlock community: FAQ, hero requests and news from the mod's maker.";
            var go = new GameObject("DeadlockOfRainCommunity");
            DontDestroyOnLoad(go);
            go.AddComponent<CommunityButton>();
        }

        void LateUpdate()
        {
            var menu = MainMenuController.instance;
            if (!menu || !menu.titleMenuScreen || MeltyTest.Active) return;
            var screen = menu.titleMenuScreen.transform;
            if (screen.Find("DeadlockOfRainDiscord")) return;
            var template = screen.GetComponentsInChildren<HGButton>(true).FirstOrDefault(b => b.GetComponentInChildren<LanguageTextMeshController>(true));
            if (template) Build(screen, template);
        }

        static void Build(Transform screen, HGButton template)
        {
            var tr = (RectTransform)template.transform;
            var go = Instantiate(template.gameObject, screen, false);
            go.name = "DeadlockOfRainDiscord";
            go.SetActive(true);
            (go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>()).ignoreLayout = true;
            foreach (var lg in go.GetComponentsInChildren<LayoutGroup>(true)) lg.enabled = false;
            foreach (var f in go.GetComponentsInChildren<ContentSizeFitter>(true)) f.enabled = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            float w = tr.rect.width, h = tr.rect.height * HeightScale;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-Margin, Margin);
            rt.localScale = tr.lossyScale.x > 0f && screen.lossyScale.x > 0f ? tr.lossyScale / screen.lossyScale.x : Vector3.one;

            var button = go.GetComponent<HGButton>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Application.OpenURL(Invite));
            button.interactable = true;
            button.hoverToken = "DLR_DISCORD_HOVER";
            if (button.targetGraphic) button.targetGraphic.color = Blurple;   // HGButton keeps this as its resting colour
            if (button.hoverHighlightGameObject) button.hoverHighlightGameObject.SetActive(false);

            var label = go.GetComponentInChildren<LanguageTextMeshController>(true);
            label.token = "DLR_DISCORD_JOIN";
            float logoH = h * 0.62f, logoW = logoH * 256f / 197f, left = h * 0.22f;
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.gameObject != label.gameObject) { tmp.gameObject.SetActive(false); continue; }   // "NEW" tags and the like
                tmp.enableAutoSizing = false;
                tmp.fontSize = tmp.fontSize * 1.6f;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = Color.white;
                var lr = tmp.rectTransform;
                lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.pivot = new Vector2(0.5f, 0.5f);
                lr.offsetMin = new Vector2(left + logoW + left * 0.8f, 0f); lr.offsetMax = new Vector2(-8f, 0f);
            }

            var icon = new GameObject("DiscordLogo", typeof(RectTransform), typeof(Image));
            var ir = (RectTransform)icon.transform;
            ir.SetParent(rt, false);
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0f, 0.5f);
            ir.sizeDelta = new Vector2(logoW, logoH);
            ir.anchoredPosition = new Vector2(left, 0f);
            var img = icon.GetComponent<Image>();
            img.sprite = Logo();
            img.preserveAspect = true;
            img.raycastTarget = false;

            Log.Info($"Discord button added to the bottom right of the title screen (copied from {template.name}, {w:F0}x{h:F0})");
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                Log.Info($"  discord button part: {t.name} [{string.Join(", ", t.GetComponents<Component>().Select(c => c.GetType().Name))}]");
        }

        static Sprite Logo()
        {
            if (logo) return logo;
            using (var s = typeof(CommunityButton).Assembly.GetManifestResourceStream("DeadlockOfRain.discord.png"))
            {
                var bytes = new byte[s.Length];
                s.Read(bytes, 0, bytes.Length);
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(bytes);
                logo = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
            }
            return logo;
        }
    }
}
