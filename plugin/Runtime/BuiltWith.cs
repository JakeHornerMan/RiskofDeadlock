using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>The Risk of Rain 2 and Deadlock versions this release was built and tested with (built-with.txt beside the DLL,
    /// written by tools/package.py from the creator's own installs), next to the ones on this PC. Every problem message names
    /// both pairs, so a player (and we, from player_logs) can see at once whether a version difference is behind it.</summary>
    public static class BuiltWith
    {
        /// <summary>Steam build ids of Risk of Rain 2 (app 632360) and Deadlock's ClientVersion; "?" when unknown.</summary>
        public static string Ror2 = "?", Deadlock = "?", Date = "?";
        public static string Ror2Here = "?";

        public static void Read()
        {
            var file = Path.Combine(Plugin.PluginDir ?? "", "built-with.txt");
            try
            {
                if (File.Exists(file))
                    foreach (var line in File.ReadAllLines(file))
                    {
                        var kv = line.Split(new[] { '=' }, 2);
                        if (kv.Length != 2) continue;
                        var v = kv[1].Trim();
                        switch (kv[0].Trim())
                        {
                            case "ror2": Ror2 = v; break;
                            case "deadlock": Deadlock = v; break;
                            case "date": Date = v; break;
                        }
                    }
            }
            catch (Exception e) { Log.Warn("built-with.txt: " + e.Message); }
            Ror2Here = SteamBuild(632360);
            Log.Info($"Built with Risk of Rain 2 build {Ror2} and Deadlock build {Deadlock} ({Date}); this PC: Risk of Rain 2 build {Ror2Here} (version {Application.version})");
        }

        /// <summary>This PC's Steam build id of an app, from the appmanifest next to the game's own steamapps/common folder.</summary>
        static string SteamBuild(int appId)
        {
            try
            {
                // .../steamapps/common/Risk of Rain 2/Risk of Rain 2_Data -> .../steamapps
                var steamapps = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)));
                var acf = Path.Combine(steamapps, $"appmanifest_{appId}.acf");
                if (!File.Exists(acf)) return "?";
                var m = Regex.Match(File.ReadAllText(acf), "\"buildid\"\\s+\"(\\d+)\"");
                return m.Success ? m.Groups[1].Value : "?";
            }
            catch { return "?"; }
        }

        /// <summary>Two lines for a problem message: what the mod was built with, what this PC has, and a hint when the
        /// Deadlock versions differ.</summary>
        public static string Versions()
        {
            var here = DeadlockCache.DeadlockBuild;
            var s = $"Built with: Risk of Rain 2 build {Ror2}, Deadlock build {Deadlock}.\nThis PC: Risk of Rain 2 build {Ror2Here}, Deadlock build {here}.";
            if (int.TryParse(here, out var a) && int.TryParse(Deadlock, out var b) && a != b)
                s += a < b
                    ? "\nYour Deadlock is older than the one this mod was built with: start Deadlock once in Steam so it updates, then restart."
                    : "\nYour Deadlock is newer than the one this mod was built with: a mod update will catch up.";
            return s;
        }
    }
}
