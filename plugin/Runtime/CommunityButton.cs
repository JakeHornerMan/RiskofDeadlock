using UnityEngine;
using RoR2.UI.MainMenu;

namespace DeadlockOfRain
{
    /// <summary>The "Join the Risk of Deadlock Discord" button on RoR2's title menu (creator, 2026-10-24). Shown only while the
    /// title menu itself is up (not settings, lobby or the loading screen). The invite never expires and gives everyone who joins
    /// through it the "Risk of Deadlock Player" role, which sees #general, #promote-yourself and the Risk of Deadlock section.
    /// IMGUI, like the loading overlay, so it needs nothing from RoR2's own UI prefabs.</summary>
    public sealed class CommunityButton : MonoBehaviour
    {
        public const string Invite = "https://discord.gg/sNByetmcqc";

        GUIStyle button, sub;

        public static void Init()
        {
            var go = new GameObject("DeadlockOfRainCommunity");
            DontDestroyOnLoad(go);
            go.AddComponent<CommunityButton>();
        }

        static Texture2D Fill(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        void OnGUI()
        {
            var menu = MainMenuController.instance;
            if (!menu || !menu.titleMenuScreen || menu.currentMenuScreen != menu.titleMenuScreen || menu.desiredMenuScreen != menu.titleMenuScreen) return;
            if (MeltyTest.Active) return;
            if (button == null)
            {
                // Discord blurple, with the loading overlay's gold for the second line
                button = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false,
                    normal = { background = Fill(new Color(0.345f, 0.396f, 0.949f, 0.95f)), textColor = Color.white },
                    hover = { background = Fill(new Color(0.42f, 0.47f, 1f, 1f)), textColor = Color.white },
                    active = { background = Fill(new Color(0.28f, 0.32f, 0.8f, 1f)), textColor = Color.white },
                };
                sub = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.96f, 0.78f, 0.42f) } };
            }
            float k = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2f);
            button.fontSize = Mathf.RoundToInt(30 * k);
            sub.fontSize = Mathf.RoundToInt(17 * k);
            float w = 520f * k, h = 84f * k;
            var r = new Rect(Screen.width - w - 40f * k, 40f * k, w, h);
            if (GUI.Button(r, "JOIN THE RISK OF DEADLOCK DISCORD", button)) Application.OpenURL(Invite);
            GUI.Label(new Rect(r.x, r.yMax + 4f * k, w, 28f * k), "FAQ, hero requests and news from the mod's maker", sub);
        }
    }
}
