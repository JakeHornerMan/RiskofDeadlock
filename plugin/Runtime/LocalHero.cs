using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DeadlockOfRain
{
    /// <summary>The creator's own hero files built into the DLL (tools/build_local_hero.sh, their PC only; creator, 2026-11-01:
    /// "a mod that will run mo and krill only ... without the need of deadlock installed"). Resources named localhero/... are a
    /// whole cache (ready.json, heroes/, sounds/, images/); they are written out once per DLL build, because the model, texture and
    /// sound readers read files, and opened instead of the Deadlock cache. Deadlock is never looked for; only those heroes load.</summary>
    public static class LocalHero
    {
        const string Prefix = "localhero/";
        /// <summary>The unpacked folder, or null when this is the normal build (no localhero resources).</summary>
        public static string Dir;
        /// <summary>The heroes in the embedded ready.json; Survivors.Released keeps only these.</summary>
        public static HashSet<string> Heroes;

        /// <summary>Unpacks the embedded files when the DLL carries them. Returns true when they are in use.</summary>
        public static bool Init()
        {
            var asm = typeof(LocalHero).Assembly;
            var names = asm.GetManifestResourceNames().Where(n => n.StartsWith(Prefix, StringComparison.Ordinal)).ToList();
            if (names.Count == 0) return false;
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "embedded");
            var stampFile = Path.Combine(dir, "dll-build.txt");
            // a new build changes the module id (deterministic build): only then are the files written out again
            var stamp = asm.ManifestModule.ModuleVersionId.ToString();
            try
            {
                if (!File.Exists(stampFile) || File.ReadAllText(stampFile).Trim() != stamp)
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                    foreach (var n in names)
                    {
                        var file = Path.Combine(dir, n.Substring(Prefix.Length).Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(file));
                        using (var src = asm.GetManifestResourceStream(n))
                        using (var dst = File.Create(file))
                            src.CopyTo(dst);
                    }
                    File.WriteAllText(stampFile, stamp);
                    Log.Info($"Own hero files: {names.Count} files written out of the DLL to {dir}");
                }
                var ready = File.ReadAllText(Path.Combine(dir, "ready.json"));
                var list = Regex.Match(ready, "\"heroes\"\\s*:\\s*\\[([^\\]]*)\\]");
                Heroes = new HashSet<string>(Regex.Matches(list.Groups[1].Value, "\"([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value));
            }
            catch (Exception e)
            {
                Log.Error("Own hero files could not be written out of the DLL: " + e);
                return false;
            }
            Dir = dir;
            Log.Info($"Playing {string.Join(", ", Heroes)} from your own files (built into this DLL); Deadlock is not read");
            return true;
        }
    }
}
