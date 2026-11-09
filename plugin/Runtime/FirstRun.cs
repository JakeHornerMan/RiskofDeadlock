using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using RoR2.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadlockOfRain
{
    /// <summary>The start of the game in every build: the loading screen (heroes being built), the title-screen problem box.
    /// Reading Deadlock on a Thunderstore install lives in FirstRunRead.cs, which the DLL build (OWN_FILES) leaves out.</summary>
    public static partial class FirstRun
    {
        public static string Problem;   // shown on the title screen when the heroes could not be loaded
        public static string ProblemHeader = "Risk of Deadlock: no Deadlock heroes";

        // ---------------------------------------------------------------- on screen while it works (FirstRunOverlay)

        internal static readonly List<string> Lines = new List<string>();
#if OWN_FILES
        internal static string Status = "getting your heroes ready (built into the mod)";
#else
        internal static string Status = "Looking for Deadlock...";
#endif
        internal static float DoneAt = -1f;

        /// <summary>Logs a line and keeps it for the on-screen box (the last few are shown).</summary>
        static void Say(string s)
        {
            Log.Info(s);
            lock (Lines) { Lines.Add(s.Trim()); if (Lines.Count > 60) Lines.RemoveAt(0); }
        }

        // ---------------------------------------------------------------- building the heroes (every start, Melty too)

        /// <summary>The hero Survivors.Build is on (null outside it) and how many are done: the loading screen names each one, so
        /// a slow PC never looks frozen (creator, 2026-10-23, after a player closed the game 41 s in, before the title screen).</summary>
        internal static string Building;
        internal static int BuildDone, BuildTotal;
        internal static float BuildStarted = -1f;
        static float heroStarted;
        internal static bool Missing;   // a hero couldn't be read: the box stays longer and says where to look

        public static float HeroSeconds => Time.realtimeSinceStartup - heroStarted;

        public static void BuildBegin(int total)
        {
            BuildTotal = total;
            BuildDone = 0;
            BuildStarted = Time.realtimeSinceStartup;
        }

        public static void BuildHero(string name)
        {
            Building = name;
            heroStarted = Time.realtimeSinceStartup;
            Status = $"building {name} ({BuildDone + 1} of {BuildTotal})";
            if (MeltyTest.Active && BuildDone == 2) MeltyTest.Shot("loading-building");   // drawn on the frame Survivors.Build yields next
        }

        /// <summary>One line per hero in the box (why = null: built); the BepInEx log already has its own line for each.</summary>
        public static void BuildHeroDone(string name, string why)
        {
            BuildDone++;
            if (why != null) Missing = true;
            lock (Lines) Lines.Add(why == null ? $"{name}: ready" : $"{name}: NOT AVAILABLE ({why})");
        }

        /// <summary>End of our content loading: the box says how it went and fades (12 s on screen with a hero missing, 4 s otherwise).</summary>
        public static void BuildEnd()
        {
            Building = null;
            int ready = Survivors.Bodies.Count, all = Survivors.Released.Length;
            if (BuildStarted >= 0f) Log.Info($"Deadlock heroes built: {ready} of {all} in {Time.realtimeSinceStartup - BuildStarted:F1} s");
            if (ready < all) { Missing = true; Say(BuiltWith.Versions().Replace("\n", "  ")); }
            Status = ready == 0 ? "no Deadlock heroes this time" : ready == all ? $"all {all} Deadlock heroes ready" : $"{ready} of {all} Deadlock heroes ready";
            DoneAt = Time.realtimeSinceStartup + (Missing ? 4f : -4f);
        }

        // ---------------------------------------------------------------- the title-screen message

        public static void Init()
        {
            SceneManager.activeSceneChanged += (_, next) =>
            {
                if (Problem != null && next.name == "title") Plugin.Instance.StartCoroutine(ShowProblem());
            };
        }

        static IEnumerator ShowProblem()
        {
            yield return new WaitForSecondsRealtime(1f);
            if (Problem == null) yield break;
            Survivors.Tokens["DLR_FIRSTRUN_HEADER"] = ProblemHeader;
            Survivors.Tokens["DLR_FIRSTRUN_TEXT"] = Problem;
            Survivors.Tokens["DLR_FIRSTRUN_OK"] = "OK";
            Problem = null;   // once per start
            var box = SimpleDialogBox.Create();
            box.headerToken = new SimpleDialogBox.TokenParamsPair("DLR_FIRSTRUN_HEADER");
            box.descriptionToken = new SimpleDialogBox.TokenParamsPair("DLR_FIRSTRUN_TEXT");
            box.AddCancelButton("DLR_FIRSTRUN_OK");
        }
    }

    /// <summary>The loading screen on every start: a banner across the screen ("Risk of Deadlock: building your Deadlock heroes",
    /// the hero being built, the time, "the game is not stuck") and a box at the bottom left with each hero as it gets ready and,
    /// on a Thunderstore first start, the extractor's latest lines (the same as BepInEx/LogOutput.log). IMGUI, because it has to
    /// draw while RoR2's own UI is still loading. Fades out after our content is built (12 s on screen when a hero is missing).</summary>
    public sealed class FirstRunOverlay : MonoBehaviour
    {
        static FirstRunOverlay instance;
        GUIStyle box, band, title, text, bigTitle, bigLine, smallLine;
        float shownAt;
        bool shotDone;
        /// <summary>The heroes the banner has named while building them (test check loading-screen).</summary>
        internal static readonly HashSet<string> Named = new HashSet<string>();

        public static void Show()
        {
            if (instance) return;
            var go = new GameObject("DeadlockOfRainFirstRun");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<FirstRunOverlay>();
            instance.shownAt = Time.realtimeSinceStartup;
        }

        static Texture2D Fill(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        void Update()
        {
            var _ = FirstRun.Progress;   // keeps the status and lines current even before content loading polls it
            if (FirstRun.DoneAt >= 0f && Time.realtimeSinceStartup > FirstRun.DoneAt + 8f) Destroy(gameObject);
        }

        void OnGUI()
        {
            if (FirstRun.Choosing) return;   // the hero picker has the screen
            bool reading = FirstRun.Started && FirstRun.BuildStarted < 0f && FirstRun.DoneAt < 0f;
            // a Thunderstore start whose cache is current ends the extractor in about a second: no reading phase on screen
            if (reading && (FirstRun.Quick || Time.realtimeSinceStartup - shownAt < 3f)) return;
            if (box == null)
            {
                box = new GUIStyle(GUI.skin.box) { normal = { background = Fill(new Color(0.06f, 0.05f, 0.04f, 0.88f)) }, padding = new RectOffset(14, 14, 10, 10) };
                band = new GUIStyle(GUI.skin.box) { normal = { background = Fill(new Color(0.03f, 0.025f, 0.02f, 0.82f)) } };
                title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.96f, 0.78f, 0.42f) }, wordWrap = true };
                text = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.9f, 0.88f, 0.84f) }, wordWrap = false, clipping = TextClipping.Clip };
                bigTitle = new GUIStyle(title) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
                bigLine = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                smallLine = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.78f, 0.76f, 0.72f) } };
            }
            bool done = FirstRun.DoneAt >= 0f;
            float a = done ? Mathf.Clamp01(1f - (Time.realtimeSinceStartup - FirstRun.DoneAt - 6f) / 2f) : 1f;
            GUI.color = new Color(1, 1, 1, a);
            float t = (done ? FirstRun.DoneAt : Time.realtimeSinceStartup) - shownAt;
            string clock = $"{Mathf.FloorToInt(t / 60f)}:{Mathf.FloorToInt(t % 60f):00}";

            // the banner: what is happening, in words nobody can miss
            string headline, detail;
            if (done)
            {
                headline = char.ToUpper(FirstRun.Status[0]) + FirstRun.Status.Substring(1);
                detail = FirstRun.Missing ? "A hero that couldn't be read keeps a red ! tile in character select; hover it for why." : "Pick one in character select.";
            }
#if OWN_FILES
            else
            {
                // the DLL build: every file is inside the mod, Deadlock isn't needed
                headline = FirstRun.Building != null ? $"Building your heroes: {FirstRun.Building}  ({FirstRun.BuildDone + 1} of {FirstRun.BuildTotal})" : "Getting your heroes ready";
                detail = $"{clock}   ·   models, animations and sounds built into the mod, no Deadlock install needed. The game is not stuck.";
            }
#else
            else if (reading)
            {
                headline = "Reading the heroes from your own Deadlock install";
                detail = $"{FirstRun.Progress * 100f:F0} %   ·   {clock}   ·   first start or after a Deadlock update: about 2 minutes for every hero. The game is not stuck.";
            }
            else
            {
                headline = FirstRun.Building != null ? $"Building your Deadlock heroes: {FirstRun.Building}  ({FirstRun.BuildDone + 1} of {FirstRun.BuildTotal})" : "Getting your Deadlock heroes ready";
                detail = $"{clock}   ·   models, animations and sounds from your own Deadlock install. The game is not stuck.";
            }
#endif
            float k = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2f);
            bigTitle.fontSize = Mathf.RoundToInt(46 * k);
            bigLine.fontSize = Mathf.RoundToInt(26 * k);
            smallLine.fontSize = Mathf.RoundToInt(18 * k);
            var b = new Rect(0f, Screen.height * 0.12f, Screen.width, 170f * k);
            GUI.Box(b, GUIContent.none, band);
            GUI.Label(new Rect(0f, b.y + 14f * k, Screen.width, 60f * k), "RISK OF DEADLOCK", bigTitle);
            GUI.Label(new Rect(0f, b.y + 76f * k, Screen.width, 40f * k), headline, bigLine);
            GUI.Label(new Rect(0f, b.y + 118f * k, Screen.width, 32f * k), detail, smallLine);
            if (Event.current.type == EventType.Repaint)
            {
                if (!done && !reading && FirstRun.Building != null) Named.Add(FirstRun.Building);
                // test mode: the result as drawn (captured at the end of this frame, after this repaint)
                if (done && !shotDone && MeltyTest.Active) { shotDone = true; MeltyTest.Shot("loading-done"); }
            }

            // the box: each hero as it gets ready (or why not), and the extractor's own lines on a Thunderstore first start;
            // it grows with the screen so nine hero lines and the versions line always fit
            title.fontSize = Mathf.RoundToInt(20 * k);
            text.fontSize = Mathf.RoundToInt(14 * k);
            float w = Mathf.Min(1100f * k, Screen.width - 40f), h = (64f + 21f * 10) * k + 20f;
            var r = new Rect(20f, Screen.height - h - 20f, w, h);
            GUI.Box(r, GUIContent.none, box);
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 10, r.width - 28, r.height - 20));
            GUILayout.Label("Risk of Deadlock: " + FirstRun.Status, title);
            if (reading) GUILayout.Label($"{FirstRun.Progress * 100f:F0} %   ·   {clock}   ·   first start only: it is not stuck", text);
            else GUILayout.Label(FirstRun.BuildTotal > 0 ? $"{FirstRun.BuildDone} of {FirstRun.BuildTotal} heroes done   ·   {clock}" : clock, text);
            GUILayout.Space(6);
            lock (FirstRun.Lines)
                for (int i = Mathf.Max(0, FirstRun.Lines.Count - 10); i < FirstRun.Lines.Count; i++)
                    GUILayout.Label(FirstRun.Lines[i], text);
            GUILayout.EndArea();
            GUI.color = Color.white;
        }
    }
}
