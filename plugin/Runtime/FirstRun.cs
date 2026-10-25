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
        static float progress, nextRead, started, stepStarted, lastActivity;
        static bool stalled;
        /// <summary>No new extractor line or step for this long: it is hung (the slowest hero takes well under a minute since 1.8.0), so
        /// the loading screen stops waiting instead of sitting there forever (a player waited over an hour, 2026-10-24).</summary>
        const float StallSeconds = 600f;
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
        /// <summary>This start reads from Deadlock (Thunderstore): the loading screen waits for the picker and the extractor.</summary>
        public static bool Started { get; private set; }
        /// <summary>The hero picker is up (HeroPicker): nothing is read until the player presses Go.</summary>
        public static bool Choosing { get; private set; }
        /// <summary>The heroes this start reads or keeps (--heroes): every read one plus the ones ticked in the picker.</summary>
        public static HashSet<string> Wanted;
        static string exePath, deadlockDir, cache;

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
            if (!File.Exists(exe))
            {
                // extract/ without the exe: a Thunderstore install whose exe was removed, nearly always by antivirus (Melty has no extract/)
                if (!Directory.Exists(ExtractDir)) return false;
                Say($"DeadlockExtract.exe is missing from {ExtractDir}");
                Status = "DeadlockExtract.exe is missing (antivirus?)";
                ProblemHeader = "Deadlock of Rain: DeadlockExtract.exe is missing";
                if (!File.Exists(Path.Combine(cacheDir, "ready.json")))
                    Problem = "The mod's Deadlock reader, DeadlockExtract.exe, is missing from\n" + ExtractDir + "\n\nThis is nearly always your antivirus removing it. "
                        + "Restore it from the antivirus quarantine (or allow it), or reinstall the mod, then restart Risk of Rain 2. The Deadlock folder setting can't help with this.\n\n" + BuiltWith.Versions();
                return false;
            }
            FirstRunOverlay.Show();
            var deadlock = FindDeadlock(configuredFolder);
            if (deadlock == null)
            {
                Problem = File.Exists(Path.Combine(cacheDir, "ready.json"))
                    ? null   // an older extract is still there: play with it
                    : "Deadlock was not found. Install Deadlock (free on Steam), then restart Risk of Rain 2.\n\nIf it is installed somewhere unusual, set \"Deadlock folder\" in the mod's config (BepInEx/config/melty.deadlockofrain.cfg, or r2modman's Config editor) to the Deadlock folder, for example\nD:\\SteamLibrary\\steamapps\\common\\Deadlock";
                Say("Deadlock install not found; no extraction");
                Status = "Deadlock not found: the game starts without the Deadlock heroes";
                return false;
            }
            exePath = exe; deadlockDir = deadlock; cache = cacheDir;
            Started = true;
            // the heroes already read: a hero of this release that isn't brings up the picker (creator, 2026-10-25); with every
            // one read, the extractor still runs with all of them (about a second, or a re-read after a Deadlock update)
            var installed = ReadyIds(cacheDir, "heroes");
            var all = new List<string>();
            foreach (var h in Survivors.Released) all.Add(h.Id);
            if (!MeltyTest.Active && all.Exists(id => !installed.Contains(id)))
            {
                Choosing = true;
                Status = "Choose your Deadlock heroes";
                Say($"Heroes read from Deadlock already: {(installed.Count > 0 ? string.Join(", ", installed) : "none")}; showing the hero picker");
                HeroPicker.Show(Survivors.Released, installed);
                return true;
            }
            return Launch(all);
        }

        /// <summary>The picker's Go: read these heroes (and keep the ones already read).</summary>
        public static void Chosen(List<string> ids)
        {
            Choosing = false;
            Launch(ids);
        }

        /// <summary>The ids in ready.json's list (e.g. "heroes"): what an earlier start already read.</summary>
        static HashSet<string> ReadyIds(string cacheDir, string key)
        {
            var ids = new HashSet<string>();
            try
            {
                var f = Path.Combine(cacheDir, "ready.json");
                if (!File.Exists(f)) return ids;
                var m = Regex.Match(File.ReadAllText(f), "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
                if (m.Success) foreach (Match id in Regex.Matches(m.Groups[1].Value, "\"([^\"]+)\"")) ids.Add(id.Groups[1].Value);
            }
            catch { }
            return ids;
        }

        static bool Launch(List<string> ids)
        {
            Wanted = new HashSet<string>(ids);
            if (ids.Count == 0) { Say("No heroes chosen: the game starts without the Deadlock heroes"); return true; }
            var cacheDir = cache;
            Directory.CreateDirectory(cacheDir);
            progressFile = Path.Combine(cacheDir, "progress.txt");
            logFile = Path.Combine(cacheDir, "extract.log");
            try { File.Delete(progressFile); File.Delete(logFile); } catch { }
            var args = $"--deadlock \"{deadlockDir}\" --out \"{cacheDir}\" --manifest \"{Path.Combine(ExtractDir, "deadlock-manifest.json")}\" --heroes {string.Join(",", ids)}";
            Say($"Reading {string.Join(", ", ids)} from Deadlock at {deadlockDir} into {cacheDir} (about 2 minutes for every hero on the first start or after a Deadlock update, about a second otherwise)");
            Log.Info($"  {exePath} {args}");   // log only: too long for the box
            started = stepStarted = lastActivity = Time.realtimeSinceStartup;
            var exe = exePath;
            try
            {
                proc = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = ExtractDir });
                return proc != null;
            }
            catch (Exception e)
            {
                Problem = "The Deadlock reader could not start (" + e.Message + "). See BepInEx/LogOutput.log.";
                Status = "The Deadlock reader could not start";
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
                                lastActivity = Time.realtimeSinceStartup;
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
                    lastActivity = Time.realtimeSinceStartup;
                    foreach (var line in text.Substring(0, end).Split('\n'))
                        if (line.Trim().Length > 0) Say("[DeadlockExtract] " + line.TrimEnd('\r'));
                }
            }
            catch { }
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

        /// <summary>Polled while the loading screen waits: true (once) when the extractor has been silent for StallSeconds; it is
        /// then stopped and Finish reports it.</summary>
        public static bool StopIfStalled()
        {
            if (!Running || stalled || Time.realtimeSinceStartup - lastActivity < StallSeconds) return false;
            stalled = true;
            Say($"DeadlockExtract printed nothing for {StallSeconds / 60f:F0} minutes; stopping it");
            try { proc.Kill(); proc.WaitForExit(5000); } catch (Exception e) { Log.Warn("Could not stop DeadlockExtract: " + e.Message); }
            return true;
        }

        /// <summary>Called once the process has exited: records why, when it failed.</summary>
        public static void Finish(string cacheDir)
        {
            if (proc == null) return;
            nextRead = 0f;
            var _ = Progress;   // relay the last log lines
            int code = proc.HasExited ? proc.ExitCode : -1;
            Say($"DeadlockExtract finished with exit code {code} after {Time.realtimeSinceStartup - started:F0} s");
            Status = code == 0 ? $"Deadlock read ({Time.realtimeSinceStartup - started:F0} s)" : "Reading Deadlock failed: see BepInEx/LogOutput.log";
            if (stalled)
            {
                Status = "Reading Deadlock stopped responding: see BepInEx/LogOutput.log";
                ProblemHeader = "Deadlock of Rain: reading Deadlock got stuck";
                Problem = $"Reading the heroes from Deadlock printed nothing for {StallSeconds / 60f:F0} minutes, so it was stopped and the game started without them. "
                    + "Restart Risk of Rain 2 to try again. If it happens again, an antivirus scanning DeadlockExtract.exe is the usual cause: allow it. The details are in "
                    + Path.Combine(cacheDir, "extract.log") + ".\n\n" + BuiltWith.Versions();
            }
            else if (code != 0)
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
                var root = DeadlockRoot(dir);
                if (root != null)
                {
                    Say($"  found Deadlock: {root}");
                    return root;
                }
                Say($"  not here: {dir}");
            }
            Say($"Deadlock not found in any of {tried.Count} places; set \"Deadlock folder\" in the config if it is installed somewhere else");
            return null;
        }

        /// <summary>The Deadlock folder (the one holding game/citadel/pak01_dir.vpk) for a typed path: also accepts its game or
        /// game/citadel folder, or pak01_dir.vpk itself, and drops a trailing slash (a trailing \ inside the quoted
        /// --deadlock "...\" argument escapes the quote and broke the extractor's arguments). Null when it isn't one.</summary>
        static string DeadlockRoot(string dir)
        {
            try
            {
                var d = dir.Trim().Trim('"').Trim().Replace('/', '\\').TrimEnd('\\');
                if (d.Length == 2 && d[1] == ':') d += "\\";   // a bare drive
                for (int i = 0; i < 4 && !string.IsNullOrEmpty(d); i++, d = Path.GetDirectoryName(d))
                    if (File.Exists(Path.Combine(d, "game", "citadel", "pak01_dir.vpk")))
                        return d.EndsWith("\\") ? d + "." : d;   // "D:\." : never a \ before the argument's closing quote
            }
            catch { }
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

    /// <summary>The loading screen on every start: a banner across the screen ("Deadlock of Rain: building your Deadlock heroes",
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
            else if (reading)
            {
                headline = "Reading the heroes from your own Deadlock install";
                detail = $"{FirstRun.Progress * 100f:F0} %   ·   {clock}   ·   first start or after a Deadlock update: a few minutes. The game is not stuck.";
            }
            else
            {
                headline = FirstRun.Building != null ? $"Building your Deadlock heroes: {FirstRun.Building}  ({FirstRun.BuildDone + 1} of {FirstRun.BuildTotal})" : "Getting your Deadlock heroes ready";
                detail = $"{clock}   ·   models, animations and sounds from your own Deadlock install. The game is not stuck.";
            }
            float k = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2f);
            bigTitle.fontSize = Mathf.RoundToInt(46 * k);
            bigLine.fontSize = Mathf.RoundToInt(26 * k);
            smallLine.fontSize = Mathf.RoundToInt(18 * k);
            var b = new Rect(0f, Screen.height * 0.12f, Screen.width, 170f * k);
            GUI.Box(b, GUIContent.none, band);
            GUI.Label(new Rect(0f, b.y + 14f * k, Screen.width, 60f * k), "DEADLOCK OF RAIN", bigTitle);
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
            GUILayout.Label("Deadlock of Rain: " + FirstRun.Status, title);
            if (reading) GUILayout.Label($"{FirstRun.Progress * 100f:F0} %   ·   {clock}   ·   first start only: it is not stuck (Abrams alone takes a few minutes)", text);
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
