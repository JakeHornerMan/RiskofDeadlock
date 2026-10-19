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
        static string progressFile;
        static float progress, nextRead;
        public static string Problem;   // shown on the title screen when the heroes could not be loaded

        public static bool Running => proc != null && !proc.HasExited;
        public static bool Started => proc != null;

        public static string ExtractDir => Path.Combine(Plugin.PluginDir, "extract");

        /// <summary>Starts the extractor when this is a Thunderstore install. Returns true when it was started.</summary>
        public static bool Start(string cacheDir, string configuredFolder)
        {
            var exe = Path.Combine(ExtractDir, "DeadlockExtract.exe");
            if (!File.Exists(exe)) return false;
            var deadlock = FindDeadlock(configuredFolder);
            if (deadlock == null)
            {
                Problem = File.Exists(Path.Combine(cacheDir, "ready.json"))
                    ? null   // an older extract is still there: play with it
                    : "Deadlock was not found. Install Deadlock (free on Steam), then restart Risk of Rain 2.\n\nIf it is installed somewhere unusual, set \"Deadlock folder\" in the mod's config.";
                Log.Warn("Deadlock install not found; no extraction");
                return false;
            }
            Directory.CreateDirectory(cacheDir);
            progressFile = Path.Combine(cacheDir, "progress.txt");
            try { File.Delete(progressFile); } catch { }
            var args = $"--deadlock \"{deadlock}\" --out \"{cacheDir}\" --manifest \"{Path.Combine(ExtractDir, "deadlock-manifest.json")}\"";
            Log.Info($"Reading the heroes from Deadlock at {deadlock}: {exe} {args}");
            try
            {
                proc = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = ExtractDir });
                return proc != null;
            }
            catch (Exception e)
            {
                Problem = "The Deadlock reader could not start (" + e.Message + "). See BepInEx/LogOutput.log.";
                Log.Error("DeadlockExtract did not start: " + e);
                return false;
            }
        }

        /// <summary>0..1 from the extractor's progress.txt ("done total label").</summary>
        public static float Progress
        {
            get
            {
                if (Time.realtimeSinceStartup < nextRead || progressFile == null) return progress;
                nextRead = Time.realtimeSinceStartup + 0.5f;
                try
                {
                    if (File.Exists(progressFile))
                    {
                        var p = File.ReadAllText(progressFile).Split(new[] { ' ' }, 3);
                        if (p.Length >= 2 && int.TryParse(p[0], out var done) && int.TryParse(p[1], out var total) && total > 0)
                            progress = Mathf.Clamp01((float)done / total);
                    }
                }
                catch { }
                return progress;
            }
        }

        /// <summary>Called once the process has exited: records why, when it failed.</summary>
        public static void Finish(string cacheDir)
        {
            if (proc == null) return;
            int code = proc.ExitCode;
            Log.Info($"DeadlockExtract finished with exit code {code}");
            if (code != 0)
                Problem = File.Exists(Path.Combine(cacheDir, "ready.json"))
                    ? null
                    : "Reading the heroes from Deadlock failed. Restart to try again; if it keeps failing, the details are in " + Path.Combine(cacheDir, "extract.log") + ".";
        }

        // ---------------------------------------------------------------- finding Deadlock

        static string FindDeadlock(string configured)
        {
            var tried = new List<string>();
            foreach (var dir in Candidates(configured))
            {
                if (string.IsNullOrEmpty(dir) || tried.Contains(dir)) continue;
                tried.Add(dir);
                if (File.Exists(Path.Combine(dir, "game", "citadel", "pak01_dir.vpk"))) return dir;
            }
            Log.Info("Deadlock not in: " + string.Join("; ", tried));
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
            if (steam != null) libraries.Add(steam);
            libraries.Add(@"C:\Program Files (x86)\Steam");
            foreach (var lib in new List<string>(libraries))
                libraries.AddRange(LibraryFolders(Path.Combine(lib, "steamapps", "libraryfolders.vdf")));
            foreach (var lib in libraries)
                yield return Path.Combine(lib, "steamapps", "common", "Deadlock");
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
            Survivors.Tokens["DLR_FIRSTRUN_HEADER"] = "Deadlock of Rain: no Deadlock heroes";
            Survivors.Tokens["DLR_FIRSTRUN_TEXT"] = Problem;
            Survivors.Tokens["DLR_FIRSTRUN_OK"] = "OK";
            Problem = null;   // once per start
            var box = SimpleDialogBox.Create();
            box.headerToken = new SimpleDialogBox.TokenParamsPair("DLR_FIRSTRUN_HEADER");
            box.descriptionToken = new SimpleDialogBox.TokenParamsPair("DLR_FIRSTRUN_TEXT");
            box.AddCancelButton("DLR_FIRSTRUN_OK");
        }
    }
}
