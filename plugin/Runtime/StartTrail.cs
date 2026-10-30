using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;
using Path = System.IO.Path;

namespace DeadlockOfRain
{
    /// <summary>The start-up trail (1.2.0): every step of a start, with seconds since the mod loaded, in the log and in
    /// %LOCALAPPDATA%\DeadlockOfRain\start-trail.txt (written at once, last start only). Melty's "closed right after starting"
    /// reports on 1.1.0 ended with no error and no Unity shutdown lines, so the logs couldn't say why; now they do:
    /// how far the start got, whether Steam started it and owns the copy, and whether the game was asked to close
    /// (a "closing" line, with what asked) or was ended from outside (no closing line: the next start says so).
    /// Nothing here may stop RoR2 from starting: every hook catches its own errors.</summary>
    public sealed class StartTrail : MonoBehaviour
    {
        static string file;
        static readonly Stopwatch clock = new Stopwatch();
        static string step = "mod start";
        static string quitBy;
        static bool closed, loadingSeen, titleSeen, steamDropped;

        public static void Init()
        {
            try
            {
                clock.Start();
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain");
                Directory.CreateDirectory(dir);
                file = Path.Combine(dir, "start-trail.txt");
                if (File.Exists(file))
                {
                    var last = File.ReadAllLines(file).Where(l => l.Trim() != "").ToArray();
                    if (last.Length > 0 && !last.Any(l => l.Contains(" closing")))
                        Log.Warn($"start trail: the last start ended without closing, after \"{last[last.Length - 1]}\" (ended from outside or crashed)");
                }
                File.WriteAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Risk of Deadlock {Plugin.Version}\n");
                Step("mod start");
                SceneManager.activeSceneChanged += (a, b) =>
                {
                    if (loadingSeen || b.name != "loadingbasic") return;
                    loadingSeen = true;
                    Step("RoR2 loading screen");
                };
                RoR2Application.onLoadFinished += () => { Step("game loaded"); Steam(); };
                Application.wantsToQuit += () => { Closing(quitBy ?? "the window was closed or the game was asked to quit"); return true; };
                Application.quitting += () => Closing(quitBy ?? "Unity quit");
                AppDomain.CurrentDomain.ProcessExit += (s, e) => Closing("process exit");
                var go = new GameObject("DeadlockOfRain.StartTrail");
                DontDestroyOnLoad(go);
                go.AddComponent<StartTrail>();
            }
            catch (Exception e) { Log.Warn("start trail: " + e.Message); }
        }

        /// <summary>One step: the log and the trail file, with seconds since the mod loaded.</summary>
        public static void Step(string what)
        {
            step = what;
            var line = $"{clock.Elapsed.TotalSeconds:F1} s  {what}";
            Log.Info("start trail: " + line);
            try { if (file != null) File.AppendAllText(file, line + "\n"); } catch { }
        }

        static void Closing(string why)
        {
            if (closed) return;
            closed = true;
            Step($"closing ({why}), last step: {step}");
        }

        /// <summary>How the game met Steam: started by Steam (Steam sets SteamAppId/SteamGameId), inside a Steam library
        /// (an appmanifest beside it), Steamworks up. RoR2 quits or restarts itself through Steam when these fail.</summary>
        static void Steam()
        {
            try
            {
                bool bySteam = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SteamAppId"))
                               || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SteamGameId"));
                bool library = BuiltWith.Ror2Here != "?";
                bool up = SteamworksClientManager.instance != null && !steamDropped;
                Step($"Steam: started by Steam {(bySteam ? "yes" : "no")}, game in a Steam library {(library ? "yes" : "no")}, Steamworks {(up ? "up" : "not up")}");
                if (!library)
                    Log.Warn($"Risk of Rain 2 is not inside a Steam library ({Path.GetDirectoryName(Application.dataPath)}): Steam may close or restart it. Install it through Steam.");
            }
            catch (Exception e) { Log.Warn("start trail Steam: " + e.Message); }
        }

        void Update()
        {
            if (titleSeen) { enabled = false; return; }
            try
            {
                if (!RoR2Application.loadFinished || !FindObjectOfType<RoR2.UI.MainMenu.MainMenuController>()) return;
                titleSeen = true;
                Step("title screen");
            }
            catch { enabled = false; }
        }

        /// <summary>RoR2 drops its Steam connection: at quit, or during the start when Steam won't run it (restart through
        /// Steam, not owned, Steam not running). The caller tells which.</summary>
        [HarmonyPatch(typeof(SteamworksClientManager), nameof(SteamworksClientManager.Dispose))]
        static class SteamClosing
        {
            static void Prefix()
            {
                try
                {
                    var caller = new StackTrace().GetFrames()?.Skip(2).Select(f => f.GetMethod())
                        .FirstOrDefault(m => m != null && m.DeclaringType != null && !m.DeclaringType.FullName.StartsWith("DeadlockOfRain"));
                    steamDropped = true;
                    Step($"RoR2 closes its Steam connection (from {caller?.DeclaringType?.Name}.{caller?.Name})");
                }
                catch { }
            }
        }

        /// <summary>The quit button and the "quit" console command.</summary>
        [HarmonyPatch(typeof(RoR2Application), "CCQuit")]
        static class QuitCommand
        {
            static void Prefix() => quitBy = "RoR2's quit command";
        }
    }
}
