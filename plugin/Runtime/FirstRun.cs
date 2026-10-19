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
    /// <summary>Thunderstore install: DeadlockExtract.exe ships beside the DLL (plugins/extract/). The plugin finds the player's
    /// own Deadlock install and runs it once; the loading screen waits for it (Survivors.cs LoadStaticContentAsync). The
    /// extractor itself skips the work when ready.json's stamp is current, so later starts cost about a second. The Melty
    /// build has no extract/ folder here (Melty's recipe runs the extractor before the game starts) and skips all of this.</summary>
    public static class FirstRun
    {
        const int DeadlockAppId = 1422450;
        static Process proc;
        static string progressFile, logFile;
        static float progress, nextRead, started, stepStarted;
        static int lastDone = -1;
        static long logPos;
        public static string Problem;   // shown on the title screen when the heroes could not be loaded
        public static string ProblemHeader = "Deadlock of Rain: no Deadlock heroes";

        public static bool Running => proc != null && !proc.HasExited;

        /// <summary>The extractor ended within 3 s without an error: the cache was current (every normal start), so the
        /// on-screen box stays hidden.</summary>
        internal static bool Quick
        {
            get
            {
                if (proc == null || !proc.HasExited) return false;
                if (endedAt < 0f) endedAt = Time.realtimeSinceStartup;
                return endedAt - started < 3f && proc.ExitCode == 0;
            }
        }
        static float endedAt = -1f;
        public static bool Started => proc != null;

        public static string ExtractDir => Path.Combine(Plugin.PluginDir, "extract");

        // ---------------------------------------------------------------- on screen while it works (FirstRunOverlay)

        internal static readonly List<string> Lines = new List<string>();
        internal static string Status = "Looking for Deadlock...";
        internal static float DoneAt = -1f;

        /// <summary>Logs a line and keeps it for the on-screen box (the last few are shown).</summary>
        static void Say(string s)
        {
            Log.Info(s);
            lock (Lines) { Lines.Add(s.Trim()); if (Lines.Count > 60) Lines.RemoveAt(0); }
        }

        /// <summary>Starts the extractor when this is a Thunderstore install. Returns true when it was started.</summary>
        public static bool Start(string cacheDir, string configuredFolder)
        {
            var exe = Path.Combine(ExtractDir, "DeadlockExtract.exe");
            if (!File.Exists(exe)) return false;
            FirstRunOverlay.Show();
            var deadlock = FindDeadlock(configuredFolder);
            if (deadlock == null)
            {
                Problem = File.Exists(Path.Combine(cacheDir, "ready.json"))
                    ? null   // an older extract is still there: play with it
                    : "Deadlock was not found. Install Deadlock (free on Steam), then restart Risk of Rain 2.\n\nIf it is installed somewhere unusual, set \"Deadlock folder\" in the mod's config.";
                Say("Deadlock install not found; no extraction");
                Status = "Deadlock not found: the game starts without the Deadlock heroes";
                DoneAt = Time.realtimeSinceStartup;
                return false;
            }
            Directory.CreateDirectory(cacheDir);
            progressFile = Path.Combine(cacheDir, "progress.txt");
            logFile = Path.Combine(cacheDir, "extract.log");
            try { File.Delete(progressFile); File.Delete(logFile); } catch { }
            var args = $"--deadlock \"{deadlock}\" --out \"{cacheDir}\" --manifest \"{Path.Combine(ExtractDir, "deadlock-manifest.json")}\"";
            Say($"Reading the heroes from Deadlock at {deadlock} into {cacheDir} (a few minutes on the first start or after a Deadlock update, about a second otherwise)");
            Log.Info($"  {exe} {args}");   // log only: too long for the box
            started = stepStarted = Time.realtimeSinceStartup;
            try
            {
                proc = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = ExtractDir });
                return proc != null;
            }
            catch (Exception e)
            {
                Problem = "The Deadlock reader could not start (" + e.Message + "). See BepInEx/LogOutput.log.";
                Status = "The Deadlock reader could not start";
                DoneAt = Time.realtimeSinceStartup;
                Log.Error("DeadlockExtract did not start: " + e);
                return false;
            }
        }

        /// <summary>0..1 from the extractor's progress.txt ("done total label"). Within a step the bar keeps creeping
        /// towards the next one (a hero can take minutes: Abrams about 4), so the loading screen never looks frozen.
        /// Each poll also copies the extractor's new extract.log lines into the BepInEx log.</summary>
        public static float Progress
        {
            get
            {
                if (Time.realtimeSinceStartup < nextRead || progressFile == null) return progress;
                nextRead = Time.realtimeSinceStartup + 0.5f;
                RelayLog();
                try
                {
                    if (File.Exists(progressFile))
                    {
                        var p = File.ReadAllText(progressFile).Split(new[] { ' ' }, 3);
                        if (p.Length >= 2 && int.TryParse(p[0], out var done) && int.TryParse(p[1], out var total) && total > 0)
                        {
                            if (done != lastDone)
                            {
                                lastDone = done;
                                stepStarted = Time.realtimeSinceStartup;
                                if (done < total) Say($"Reading Deadlock: step {done + 1}/{total}, {(p.Length > 2 ? p[2].Trim() : "")} ({Time.realtimeSinceStartup - started:F0} s so far)");
                                Status = done < total ? $"Reading the heroes from Deadlock: step {done + 1} of {total} ({(p.Length > 2 ? p[2].Trim() : "")})" : "Done reading Deadlock";
                            }
                            float creep = 0.9f * (1f - Mathf.Exp(-(Time.realtimeSinceStartup - stepStarted) / 60f));
                            progress = Mathf.Max(progress, Mathf.Clamp01((done + (done < total ? creep : 0f)) / total));
                        }
                    }
                }
                catch { }
                return progress;
            }
        }

        static void RelayLog()
        {
            try
            {
                if (logFile == null || !File.Exists(logFile)) return;
                using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (fs.Length < logPos) logPos = 0;
                    fs.Seek(logPos, SeekOrigin.Begin);
                    var text = new StreamReader(fs).ReadToEnd();
                    int end = text.LastIndexOf('\n');
                    if (end < 0) return;   // wait for a whole line
                    logPos += System.Text.Encoding.UTF8.GetByteCount(text.Substring(0, end + 1));
                    foreach (var line in text.Substring(0, end).Split('\n'))
                        if (line.Trim().Length > 0) Say("[DeadlockExtract] " + line.TrimEnd('\r'));
                }
            }
            catch { }
        }

        /// <summary>Melty build (Melty's setup read Deadlock before the game started): when a hero was skipped, the loading-
        /// screen box still shows which heroes are ready and which are not, then fades like the Thunderstore one.</summary>
        public static void ShowSummary()
        {
            if (Started || DeadlockCache.Failed.Count == 0) return;
            foreach (var hero in Survivors.Released)
                Say(DeadlockCache.Failed.TryGetValue(hero.Id, out var why) ? $"{hero.DisplayName}: NOT AVAILABLE ({why})" : $"{hero.DisplayName}: ready");
            Say(BuiltWith.Versions().Replace("\n", "  "));
            Status = $"{Survivors.Released.Length - DeadlockCache.Failed.Count} of {Survivors.Released.Length} heroes ready";
            progress = 1f;
            DoneAt = Time.realtimeSinceStartup + 4f;   // 12 s on screen in all
            FirstRunOverlay.Show(true);
        }

        /// <summary>Called once the process has exited: records why, when it failed.</summary>
        public static void Finish(string cacheDir)
        {
            if (proc == null) return;
            nextRead = 0f;
            var _ = Progress;   // relay the last log lines
            int code = proc.ExitCode;
            Say($"DeadlockExtract finished with exit code {code} after {Time.realtimeSinceStartup - started:F0} s");
            Status = code == 0 ? $"Deadlock heroes ready ({Time.realtimeSinceStartup - started:F0} s)" : "Reading Deadlock failed: see BepInEx/LogOutput.log";
            DoneAt = Time.realtimeSinceStartup;
            if (code != 0)
                Problem = File.Exists(Path.Combine(cacheDir, "ready.json"))
                    ? null
                    : "Reading the heroes from Deadlock failed. Restart to try again; if it keeps failing, the details are in " + Path.Combine(cacheDir, "extract.log") + ".\n\n" + BuiltWith.Versions();
        }

        // ---------------------------------------------------------------- finding Deadlock

        static string FindDeadlock(string configured)
        {
            Say("Looking for Deadlock (the folder with game/citadel/pak01_dir.vpk)...");
            var tried = new List<string>();
            foreach (var dir in Candidates(configured))
            {
                if (string.IsNullOrEmpty(dir) || tried.Exists(t => Same(t, dir))) continue;
                tried.Add(dir);
                if (File.Exists(Path.Combine(dir, "game", "citadel", "pak01_dir.vpk")))
                {
                    Say($"  found Deadlock: {dir}");
                    return dir;
                }
                Say($"  not here: {dir}");
            }
            Say($"Deadlock not found in any of {tried.Count} places; set \"Deadlock folder\" in the config if it is installed somewhere else");
            return null;
        }

        static IEnumerable<string> Candidates(string configured)
        {
            if (!string.IsNullOrWhiteSpace(configured)) yield return configured.Trim().Trim('"');
            yield return Environment.GetEnvironmentVariable("DEADLOCKOFRAIN_DEADLOCK");
            var libraries = new List<string>();
            // Risk of Rain 2's own library (…/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data)
            var steamapps = Up(Application.dataPath, 3);
            if (steamapps != null) libraries.Add(Path.GetDirectoryName(steamapps));
            var steam = SteamPath();
            Say(steam != null ? $"  Steam (registry): {steam}" : "  Steam not in the registry");
            if (steam != null) libraries.Add(steam);
            libraries.Add(@"C:\Program Files (x86)\Steam");
            // Windows paths ignore case: the registry gives "c:\program files (x86)\steam", the vdf "C:\Program Files (x86)\Steam"
            libraries = Distinct(libraries);
            foreach (var lib in new List<string>(libraries))
            {
                var more = Distinct(new List<string>(LibraryFolders(Path.Combine(lib, "steamapps", "libraryfolders.vdf")))).FindAll(m => !libraries.Exists(l => Same(l, m)));
                if (more.Count > 0) Say($"  Steam libraries listed in {lib}: {string.Join("; ", more)}");
                libraries.AddRange(more);
            }
            foreach (var lib in libraries)
                yield return Path.Combine(lib, "steamapps", "common", "Deadlock");
        }

        static bool Same(string a, string b) =>
            string.Equals(a.Replace('/', '\\').TrimEnd('\\'), b.Replace('/', '\\').TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

        static List<string> Distinct(List<string> paths)
        {
            var result = new List<string>();
            foreach (var p in paths) if (!string.IsNullOrEmpty(p) && !result.Exists(r => Same(r, p))) result.Add(p);
            return result;
        }

        static string Up(string path, int n)
        {
            try { for (int i = 0; i < n && path != null; i++) path = Path.GetDirectoryName(path); return path; }
            catch { return null; }
        }

        static IEnumerable<string> LibraryFolders(string vdf)
        {
            var found = new List<string>();
            try
            {
                if (File.Exists(vdf))
                    foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                        found.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
            }
            catch { }
            return found;
        }

        /// <summary>Steam's folder from the registry (reg.exe: the plugin targets netstandard2.1, which has no Registry API).</summary>
        static string SteamPath()
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo("reg.exe", @"query HKCU\Software\Valve\Steam /v SteamPath")
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true });
                var text = p.StandardOutput.ReadToEnd();
                p.WaitForExit(3000);
                var m = Regex.Match(text, @"SteamPath\s+REG_SZ\s+(.+)");
                return m.Success ? m.Groups[1].Value.Trim().Replace('/', '\\') : null;
            }
            catch { return null; }
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

    /// <summary>The first-run box on the loading screen: what the mod is doing to find and read Deadlock, the elapsed time
    /// and the extractor's latest lines (the same lines as BepInEx/LogOutput.log). IMGUI, because it has to draw while
    /// RoR2's own UI is still loading. Fades out 8 s after the work ends.</summary>
    public sealed class FirstRunOverlay : MonoBehaviour
    {
        static FirstRunOverlay instance;
        GUIStyle box, title, text;
        float shownAt;
        bool summary;   // the Melty summary of a start with a skipped hero (FirstRun.ShowSummary), not the extractor at work

        public static void Show(bool summary = false)
        {
            if (instance) return;
            var go = new GameObject("DeadlockOfRainFirstRun");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<FirstRunOverlay>();
            instance.shownAt = Time.realtimeSinceStartup;
            instance.summary = summary;
        }

        void Update()
        {
            var _ = FirstRun.Progress;   // keeps the status and lines current even before content loading polls it
            if (FirstRun.DoneAt >= 0f && Time.realtimeSinceStartup > FirstRun.DoneAt + 8f) Destroy(gameObject);
        }

        void OnGUI()
        {
            // normal starts (the cache is current) finish in about a second: show nothing unless it takes longer or failed
            if (!summary && (FirstRun.Quick || FirstRun.DoneAt < 0f && Time.realtimeSinceStartup - shownAt < 3f)) return;
            if (box == null)
            {
                var bg = new Texture2D(1, 1); bg.SetPixel(0, 0, new Color(0.06f, 0.05f, 0.04f, 0.88f)); bg.Apply();
                box = new GUIStyle(GUI.skin.box) { normal = { background = bg }, padding = new RectOffset(14, 14, 10, 10) };
                title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.96f, 0.78f, 0.42f) }, wordWrap = true };
                text = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.9f, 0.88f, 0.84f) }, wordWrap = false, clipping = TextClipping.Clip };
            }
            float a = FirstRun.DoneAt >= 0f ? Mathf.Clamp01(1f - (Time.realtimeSinceStartup - FirstRun.DoneAt - 6f) / 2f) : 1f;
            GUI.color = new Color(1, 1, 1, a);
            float w = Mathf.Min(900f, Screen.width - 40f), h = 250f;
            var r = new Rect(20f, Screen.height - h - 20f, w, h);
            GUI.Box(r, GUIContent.none, box);
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 10, r.width - 28, r.height - 20));
            GUILayout.Label("Deadlock of Rain: " + FirstRun.Status, title);
            float t = (FirstRun.DoneAt >= 0f ? FirstRun.DoneAt : Time.realtimeSinceStartup) - shownAt;
            if (summary) GUILayout.Label("A hero that couldn't be read keeps a red ! tile in character select; hover it for why.", text);
            else GUILayout.Label($"{FirstRun.Progress * 100f:F0} %   ·   {Mathf.FloorToInt(t / 60f)}:{Mathf.FloorToInt(t % 60f):00}   ·   first start only: it is not stuck (Abrams alone takes a few minutes)", text);
            GUILayout.Space(6);
            lock (FirstRun.Lines)
                for (int i = Mathf.Max(0, FirstRun.Lines.Count - (summary ? 8 : 6)); i < FirstRun.Lines.Count; i++)
                    GUILayout.Label(FirstRun.Lines[i], text);
            GUILayout.EndArea();
            GUI.color = Color.white;
        }
    }
}
