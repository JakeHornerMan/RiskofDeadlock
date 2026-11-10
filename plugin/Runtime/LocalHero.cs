using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DeadlockOfRain
{
    /// <summary>The creator's own hero files (tools/build_local_hero.sh, their r2modman build; creator, 2026-11-01: "a mod that will
    /// run mo and krill only ... without the need of deadlock installed"). Since 1.5.13 they sit as plain files in a heroes/ folder
    /// beside the DLL, the way popular RoR2 survivor mods ship their AssetBundles and SoundBanks (Enforcer, Paladin: Info.Location ->
    /// GetDirectoryName -> LoadFromFile), and are read from there: nothing is unpacked and nothing goes to AppData (a reviewer, 2026-11-10:
    /// "why are the assets being extracted and placed into app data? Why not just include them in the mod folder itself alongside the
    /// DLL?"). The folder is a whole cache (ready.json, heroes/, sounds/, images/, props/), opened instead of the Deadlock cache.
    /// Only in the OWN_FILES build; Deadlock is never looked for and only those heroes load.</summary>
    public static class LocalHero
    {
        /// <summary>The folder the hero files are read from (set in the DLL build even when they are missing), or null in the normal build.</summary>
        public static string Dir;
        /// <summary>The heroes in its ready.json; Survivors.Released keeps only these.</summary>
        public static HashSet<string> Heroes;
        /// <summary>Heroes left out of this build but shown greyed out as "coming soon" (ready.json "comingSoon", from
        /// build_local_hero.sh --soon; creator, 2026-11-09: "dll build with no victor or paige grey out these characters").</summary>
        public static HashSet<string> ComingSoon;

        /// <summary>Finds the hero files beside the DLL. Returns true when they are in use; otherwise the title screen says why.</summary>
        public static bool Init()
        {
            // the mod's folder/heroes; if a mod manager ever lifts a folder's files up beside the DLL, the folder itself
            var candidates = new[] { Path.Combine(Plugin.PluginDir, "heroes"), Plugin.PluginDir };
            var dir = candidates.FirstOrDefault(d => File.Exists(Path.Combine(d, "ready.json")));
            ClearOldUnpack();
            // the DLL build never falls back to the Deadlock cache in AppData: with the files missing it loads no hero and says why
            Heroes = new HashSet<string>(); ComingSoon = new HashSet<string>();
            Dir = dir ?? candidates[0];
            if (dir == null)
            {
                Log.Error($"Own hero files: no heroes\\ready.json in {Plugin.PluginDir}");
                FirstRun.ProblemHeader = "Risk of Deadlock: the hero files are missing";
                FirstRun.Problem = "The mod's hero files are missing from\n" + Path.Combine(Plugin.PluginDir, "heroes")
                    + "\n\nReinstall the mod (in r2modman: uninstall it, then import the zip again), then restart Risk of Rain 2.";
                return false;
            }
            try
            {
                var ready = File.ReadAllText(Path.Combine(dir, "ready.json"));
                Heroes = Ids(ready, "heroes");
                ComingSoon = Ids(ready, "comingSoon");
            }
            catch (Exception e)
            {
                Log.Error("Own hero files: ready.json could not be read: " + e);
                return false;
            }
            Log.Info($"Playing {string.Join(", ", Heroes)} from your own files in {dir}; Deadlock is not read");
            return true;
        }

        static HashSet<string> Ids(string json, string key)
        {
            var list = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            return new HashSet<string>(Regex.Matches(list.Success ? list.Groups[1].Value : "", "\"([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value));
        }

        /// <summary>Builds before 1.5.13 unpacked their files to %LOCALAPPDATA%\DeadlockOfRain\embedded (about 600 MB): removed once.</summary>
        static void ClearOldUnpack()
        {
            var old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "embedded");
            try
            {
                if (!Directory.Exists(old)) return;
                Directory.Delete(old, true);
                Log.Info($"Removed {old}: older builds unpacked their hero files there; they are read from the mod's folder now");
            }
            catch (Exception e) { Log.Warn($"Could not remove {old}: {e.Message}"); }
        }
    }
}
