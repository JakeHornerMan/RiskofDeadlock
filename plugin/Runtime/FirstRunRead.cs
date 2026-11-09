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
    /// build has no extract/ folder here (Melty's recipe runs the extractor before the game starts) and skips all of this.
    /// The DLL build (tools/build_local_hero.sh sets OWN_FILES) compiles none of it: its heroes are inside the DLL
    /// (creator, 2026-11-09: "make sure no extractor code or reference is included").</summary>
    public static partial class FirstRun
    {
#if OWN_FILES
        public static bool Running => false;
        internal static bool Quick => false;
        public static bool Started => false;
        public static bool Choosing => false;
        public static HashSet<string> Wanted => null;
        public static float Progress => 1f;
        public static bool StopIfStalled() => false;
        public static void Finish(string cacheDir) { }
#else
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
                ProblemHeader = "Risk of Deadlock: DeadlockExtract.exe is missing";
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
                ProblemHeader = "Risk of Deadlock: reading Deadlock got stuck";
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

#endif
    }
}
