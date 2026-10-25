using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>The Thunderstore hero picker (creator, 2026-10-25: "a popup ... select which characters they want ... a clear visual
    /// for what is already installed ... a running time that increase for each character selected ... the user will press go").
    /// Shown on the loading screen before anything is read, only while some hero of this release isn't read from Deadlock yet
    /// (FirstRun.Start). Read heroes are ticked and marked Installed; the player ticks the new ones, the total grows with each
    /// (heroes.json readSeconds, plus sharedReadSeconds for the icons and sounds when anything new is read), and Go hands the
    /// list to the extractor (--heroes). Enter also presses Go. IMGUI, because RoR2's own UI isn't loaded yet.</summary>
    public sealed class HeroPicker : MonoBehaviour
    {
        sealed class Choice { public HeroRow Hero; public bool Installed, Ticked; }
        static HeroPicker instance;
        readonly List<Choice> choices = new List<Choice>();
        GUIStyle dim, panel, title, sub, note, row, tag, time, total, button, go, hit;
        Texture2D boxEdge, boxOn, boxInstalled;
        Vector2 scroll;

        public static void Show(IEnumerable<HeroRow> heroes, ICollection<string> installed)
        {
            if (instance) return;
            var obj = new GameObject("DeadlockOfRainHeroPicker");
            DontDestroyOnLoad(obj);
            instance = obj.AddComponent<HeroPicker>();
            foreach (var h in heroes)
                instance.choices.Add(new Choice { Hero = h, Installed = installed.Contains(h.Id), Ticked = true });
            Log.Info("Hero picker: " + string.Join(", ", instance.choices.Select(c => c.Hero.DisplayName + (c.Installed ? " (installed)" : ""))));
        }

        float Seconds() =>
            choices.Where(c => c.Ticked && !c.Installed).Sum(c => c.Hero.ReadSeconds) is float s && s > 0f ? s + SheetInfo.SharedReadSeconds : 0f;

        static string Clock(float s) => $"{Mathf.FloorToInt(s / 60f)}:{Mathf.CeilToInt(s % 60f):00}";

        void Update()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        void Go(string how)
        {
            Log.Info("Hero picker: Go pressed (" + how + ")");
            var ids = choices.Where(c => c.Ticked).Select(c => c.Hero.Id).ToList();
            Log.Info($"Hero picker: Go with {string.Join(", ", ids)} (new: {string.Join(", ", choices.Where(c => c.Ticked && !c.Installed).Select(c => c.Hero.Id))}, about {Clock(Seconds())})");
            FirstRun.Chosen(ids);
            Destroy(gameObject);
        }

        static Texture2D Fill(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        void OnGUI()
        {
            float k = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2f);
            if (dim == null)
            {
                var brass = new Color(0.96f, 0.78f, 0.42f);
                dim = new GUIStyle { normal = { background = Fill(new Color(0f, 0f, 0f, 0.72f)) } };
                panel = new GUIStyle(GUI.skin.box) { normal = { background = Fill(new Color(0.06f, 0.05f, 0.04f, 0.97f)) } };
                title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = brass } };
                sub = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.85f, 0.83f, 0.8f) } };
                note = new GUIStyle(sub) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.62f, 0.3f) } };
                row = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
                hit = new GUIStyle { hover = { background = Fill(new Color(1f, 1f, 1f, 0.06f)) } };   // the whole row is clickable
                boxEdge = Fill(new Color(0.55f, 0.5f, 0.42f));
                boxOn = Fill(brass);
                boxInstalled = Fill(new Color(0.45f, 0.85f, 0.45f));
                tag = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.45f, 0.85f, 0.45f) } };
                time = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.78f, 0.76f, 0.72f) } };
                total = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
                button = new GUIStyle(GUI.skin.button);
                go = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, normal = { background = Fill(new Color(0.79f, 0.64f, 0.36f)), textColor = new Color(0.05f, 0.07f, 0.07f) },
                    hover = { background = Fill(new Color(0.9f, 0.75f, 0.45f)), textColor = Color.black }, active = { background = Fill(brass), textColor = Color.black } };
            }
            title.fontSize = Mathf.RoundToInt(40 * k);
            sub.fontSize = note.fontSize = Mathf.RoundToInt(18 * k);
            row.fontSize = tag.fontSize = time.fontSize = Mathf.RoundToInt(22 * k);
            total.fontSize = Mathf.RoundToInt(24 * k);
            button.fontSize = Mathf.RoundToInt(18 * k);
            go.fontSize = Mathf.RoundToInt(28 * k);

            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)) { e.Use(); Go("Enter key"); return; }

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, dim);
            float lineH = 44f * k, w = Mathf.Min(760f * k, Screen.width - 40f);
            float listH = Mathf.Min(choices.Count * lineH, Screen.height * 0.5f);
            float h = 210f * k + listH + 190f * k;
            var r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(r, GUIContent.none, panel);
            float x = r.x + 30f * k, iw = r.width - 60f * k, y = r.y + 18f * k;

            GUI.Label(new Rect(r.x, y, r.width, 50f * k), "DEADLOCK OF RAIN", title); y += 52f * k;
            GUI.Label(new Rect(x, y, iw, 70f * k), "Choose the heroes to read from your own Deadlock install. Each is read once; the ones you skip stay on this list for next time.", sub);
            y += 70f * k;
            // co-op needs the same heroes on every PC (the mod-list entry names them; Net.cs), so say it before anyone skips one
            GUI.Label(new Rect(x, y, iw, 50f * k), "Playing online? Every player needs the same heroes installed: install all of them to be able to join any game.", note);
            y += 60f * k;

            // the list: installed heroes stay ticked and can't be unticked; each new one shows its time
            var view = new Rect(0, 0, iw - 20f * k, choices.Count * lineH);
            scroll = GUI.BeginScrollView(new Rect(x, y, iw, listH), scroll, view);
            for (int i = 0; i < choices.Count; i++)
            {
                var c = choices[i];
                var lr = new Rect(0, i * lineH, view.width, lineH);
                // a big tick box (IMGUI's own toggle is a few pixels): an edge, and a filled square when ticked
                if (!c.Installed && GUI.Button(lr, GUIContent.none, hit)) { c.Ticked = !c.Ticked; Log.Info($"Hero picker: {c.Hero.DisplayName} {(c.Ticked ? "ticked" : "unticked")}"); }
                float bs = 26f * k, by = lr.y + (lineH - bs) / 2f;
                GUI.DrawTexture(new Rect(lr.x + 4f * k, by, bs, bs), boxEdge);
                GUI.DrawTexture(new Rect(lr.x + 4f * k + 3f, by + 3f, bs - 6f, bs - 6f), Texture2D.blackTexture);
                if (c.Ticked || c.Installed) GUI.DrawTexture(new Rect(lr.x + 4f * k + 6f, by + 6f, bs - 12f, bs - 12f), c.Installed ? boxInstalled : boxOn);
                GUI.Label(new Rect(lr.x + bs + 18f * k, lr.y, lr.width * 0.6f, lineH), c.Hero.DisplayName, row);
                if (c.Installed) GUI.Label(new Rect(lr.x, lr.y, lr.width, lineH), "Installed  ✓", tag);
                else GUI.Label(new Rect(lr.x, lr.y, lr.width, lineH), c.Ticked ? $"about {Clock(c.Hero.ReadSeconds)}" : "skipped", time);
            }
            GUI.EndScrollView();
            y += listH + 14f * k;

            int fresh = choices.Count(c => c.Ticked && !c.Installed);
            GUI.Label(new Rect(x, y, iw, 36f * k), fresh == 0 ? "Nothing new to read: Go starts the game" : $"{fresh} new hero{(fresh == 1 ? "" : "es")} to read: about {Clock(Seconds())}", total);
            y += 46f * k;

            float bw = 150f * k, bh = 40f * k;
            if (GUI.Button(new Rect(x, y + 10f * k, bw, bh), "Select all", button)) foreach (var c in choices) c.Ticked = true;
            if (GUI.Button(new Rect(x + bw + 12f * k, y + 10f * k, bw, bh), "Select none", button)) foreach (var c in choices) c.Ticked = c.Installed;
            if (GUI.Button(new Rect(r.xMax - 30f * k - 220f * k, y, 220f * k, 60f * k), "GO", go)) Go("button");
        }
    }
}
