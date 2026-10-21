// DeadlockExtract: reads the heroes' models, animations, icons and sounds from the PLAYER'S OWN Deadlock install
// and writes a local cache that the Risk of Rain 2 plugin loads. Nothing from Deadlock ships with the mod.
//
//   DeadlockExtract.exe --deadlock <Deadlock folder> --out <cache folder> [--manifest manifest.json] [--force]
//   DeadlockExtract.exe --deadlock <Deadlock folder> --list-models     (each hero key's model, from scripts/heroes.vdata)
//
// Melty runs this once before the first start (recipe.setup) and again when Deadlock updates; the Thunderstore build's
// plugin runs it on every start from BepInEx/plugins/.../extract/ (it returns at once when ready.json is current).
// While working it writes progress.txt ("<done> <total> <step>") for the plugin's loading screen.
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SharpGLTF.Schema2;
using SkiaSharp;
using SteamDatabase.ValvePak;
using ValveResourceFormat;
using ValveResourceFormat.IO;
using ValveResourceFormat.ResourceTypes;

namespace DeadlockExtract;

public static class Program
{
    public const string Version = "1.6.1";
    static StreamWriter? log;

    public static int Main(string[] args)
    {
        string? deadlock = null, outDir = null, manifestPath = null;
        bool force = false, listModels = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--deadlock": deadlock = args[++i]; break;
                case "--out": outDir = args[++i]; break;
                case "--manifest": manifestPath = args[++i]; break;
                case "--force": force = true; break;
                case "--list-models": listModels = true; break;
            }
        }
        deadlock ??= Environment.GetEnvironmentVariable("DEADLOCKOFRAIN_DEADLOCK");
        outDir ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "cache");
        manifestPath ??= Path.Combine(AppContext.BaseDirectory, "manifest.json");
        Directory.CreateDirectory(outDir);
        log = new StreamWriter(Path.Combine(outDir, "extract.log"), false, new UTF8Encoding(false)) { AutoFlush = true };
        try
        {
            if (deadlock == null) throw new Exception("No Deadlock folder given (--deadlock).");
            if (listModels) return ListModels(deadlock);
            return Run(deadlock, outDir, manifestPath, force);
        }
        catch (Exception e)
        {
            Log("FAILED: " + e);
            Console.Error.WriteLine("DeadlockExtract failed: " + e.Message);
            return 1;
        }
    }

    static void Progress(string outDir, int done, int total, string step)
    {
        try { File.WriteAllText(Path.Combine(outDir, "progress.txt"), $"{done} {total} {step}"); } catch { }
    }

    static void Log(string s)
    {
        Console.WriteLine(s);
        log?.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + s);
    }

    static string FindVpk(string deadlock)
    {
        foreach (var c in new[] { Path.Combine(deadlock, "game", "citadel", "pak01_dir.vpk"), Path.Combine(deadlock, "citadel", "pak01_dir.vpk"), Path.Combine(deadlock, "pak01_dir.vpk") })
            if (File.Exists(c)) return c;
        throw new FileNotFoundException("pak01_dir.vpk not found under " + deadlock + " (is this the Deadlock folder?)");
    }

    static int Run(string deadlock, string outDir, string manifestPath, bool force)
    {
        var vpkPath = FindVpk(deadlock);
        var steamInf = Path.Combine(Path.GetDirectoryName(vpkPath)!, "steam.inf");
        string build = File.Exists(steamInf)
            ? (File.ReadAllLines(steamInf).FirstOrDefault(l => l.StartsWith("ClientVersion="))?.Split('=')[1] ?? "unknown")
            : "unknown";
        var manifestText = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<Manifest>(manifestText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var stamp = $"{build}|{Version}|{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifestText)))[..16]}";
        var readyPath = Path.Combine(outDir, "ready.json");
        if (!force && File.Exists(readyPath))
        {
            try
            {
                var ready = JsonDocument.Parse(File.ReadAllText(readyPath));
                if (ready.RootElement.GetProperty("stamp").GetString() == stamp)
                {
                    Log($"Cache is current for Deadlock build {build}; nothing to do.");
                    return 0;
                }
            }
            catch { }
            File.Delete(readyPath);
        }
        Log($"DeadlockExtract {Version}: Deadlock build {build} at {deadlock}");

        using var package = new Package();
        package.OptimizeEntriesForBinarySearch(StringComparison.OrdinalIgnoreCase);
        package.Read(vpkPath);
        using var loader = new GameFileLoader(package, vpkPath);

        var heroesDir = Path.Combine(outDir, "heroes");
        Directory.CreateDirectory(heroesDir);
        int total = manifest.Heroes.Count + 3, step = 0;
        // Each hero is read on its own (1.3.0): a Deadlock update that breaks one model must not cost the player the others.
        // A hero is written to <id>.tmp and swapped in only when it finished; a failed one has no folder at all, so the plugin
        // never loads an old copy that no longer matches, and ready.json lists it under "failed".
        var read = new List<string>();
        var failed = new List<object>();
        var modelPaths = new Dictionary<string, string>();
        var heroModels = HeroModels(loader);
        foreach (var hero in manifest.Heroes)
        {
            Progress(outDir, step, total, hero.Id);
            Log($"[{++step}/{total}] Reading {hero.Id}...");
            var heroDir = Path.Combine(heroesDir, hero.Id);
            var work = heroDir + ".tmp";
            var t0 = DateTime.UtcNow;
            try
            {
                modelPaths[hero.Id] = hero.Vmdl = FindModel(package, hero, heroModels);
                ExportHero(loader, hero, work);
                if (Directory.Exists(heroDir)) Directory.Delete(heroDir, true);
                Directory.Move(work, heroDir);
                read.Add(hero.Id);
                Log($"{hero.Id} ready ({(DateTime.UtcNow - t0).TotalSeconds:F0} s)");
            }
            catch (Exception e)
            {
                Log($"HERO FAILED {hero.Id}: {e.Message}");
                Log($"{hero.Id} SKIPPED (the other heroes still load)");
                Log(e.ToString());
                TryDeleteDir(work);
                TryDeleteDir(heroDir);
                failed.Add(new { id = hero.Id, error = e.Message });
            }
        }
        if (read.Count == 0 && manifest.Heroes.Count > 0)
            throw new Exception($"none of the {manifest.Heroes.Count} heroes could be read from Deadlock build {build} (see the HERO FAILED lines above)");
        // the Deadlock models our effects draw as plain meshes (1.5.0: Viscous' cube, ball and fist). Each is read on its own and
        // never costs the player a hero: a failed one is listed under "failedProps" and the plugin draws a simple shape instead
        Progress(outDir, step, total, "props");
        Log($"[{++step}/{total}] Reading effect models...");
        var props = new List<string>();
        var failedProps = new List<object>();
        var propsDir = Path.Combine(outDir, "props");
        TryDeleteDir(propsDir);
        foreach (var prop in manifest.Props)
        {
            var work = Path.Combine(propsDir, prop.Id);
            try
            {
                ExportProp(loader, prop, work);
                props.Add(prop.Id);
            }
            catch (Exception e)
            {
                Log($"  prop {prop.Id} ({prop.Vmdl}) SKIPPED: {e.Message}");
                TryDeleteDir(work);
                failedProps.Add(new { id = prop.Id, error = e.Message });
            }
        }
        Log($"Effect models: {props.Count}/{manifest.Props.Count}");

        Progress(outDir, step, total, "images");
        Log($"[{++step}/{total}] Reading icons...");

        var imagesDir = Path.Combine(outDir, "images");
        int images = 0;
        // icons and sounds never cost the player the heroes already read: a failure here only leaves them out
        try
        {
            Directory.CreateDirectory(imagesDir);
            foreach (var img in manifest.Images)
                if (ExportImage(loader, img, Path.Combine(imagesDir, ImageFileName(img)))) images++;
        }
        catch (Exception e) { Log("IMAGES FAILED: " + e); }
        Log($"Images: {images}/{manifest.Images.Count}");

        Progress(outDir, step, total, "sounds");
        Log($"[{++step}/{total}] Reading sounds...");
        int sounds = 0, voiceGroups = 0;
        try { sounds = ExportSounds(package, loader, manifest.SoundEvents, manifest.VoiceGroups, Path.Combine(outDir, "sounds"), out voiceGroups); }
        catch (Exception e) { Log("SOUNDS FAILED: " + e); }
        Log($"Sound events: {sounds}/{manifest.SoundEvents.Count}, voice groups: {voiceGroups}/{manifest.VoiceGroups.Count}");

        var readyJson = JsonSerializer.Serialize(new
        {
            stamp,
            deadlockBuild = build,
            extractor = Version,
            heroes = read.ToArray(),
            failed,
            modelPaths,
            props = props.ToArray(),
            failedProps,
            images,
            soundEvents = sounds,
            voiceGroups,
            time = DateTime.UtcNow.ToString("o"),
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(readyPath, readyJson);
        Progress(outDir, total, total, "done");
        Log($"Done: {read.Count}/{manifest.Heroes.Count} heroes" + (failed.Count > 0 ? " (failed: " + string.Join(", ", manifest.Heroes.Select(h => h.Id).Except(read)) + ")" : "") + ": " + readyPath);
        Log("Ready: " + string.Join(", ", read) + (failed.Count > 0 ? " | Skipped: " + string.Join(", ", manifest.Heroes.Select(h => h.Id).Except(read)) : ""));
        return 0;
    }

    /// <summary>Each hero key's model as Deadlock itself names it (scripts/heroes.vdata, m_strModelName). A Deadlock build can
    /// move a model between heroes_staging/ and heroes_wip/ (a player's build 5977 had no heroes_wip/inferno), so this comes
    /// first; an unreadable vdata only costs the lookup, and the manifest's fixed paths are used.</summary>
    static Dictionary<string, string> HeroModels(GameFileLoader loader)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var r = loader.LoadFileCompiled("scripts/heroes.vdata");
            ValveKeyValue.KVObject? root = r?.DataBlock switch
            {
                BinaryKV3 kv => kv.Data.Root,
                KeyValuesOrNTRO kn => kn.Data,
                _ => null,
            };
            if (root == null) { Log("  heroes.vdata not readable; using the manifest's model paths"); return map; }
            foreach (var child in root.Children)
                if (child.Value != null && child.Value.TryGetValue("m_strModelName", out var m) && m != null && !m.IsNull)
                {
                    var path = Str(m).Replace('\\', '/');
                    if (path.EndsWith(".vmdl", StringComparison.OrdinalIgnoreCase)) map[child.Key] = path;
                }
            Log($"  heroes.vdata: {map.Count} hero models");
        }
        catch (Exception e) { Log($"  heroes.vdata not readable ({e.Message}); using the manifest's model paths"); }
        return map;
    }

    /// <summary>The model to read for a hero: Deadlock's own path for its key when it is the same model file (the clip and
    /// mesh names belong to it), then the manifest's, then the manifest's under the other of heroes_staging/ and heroes_wip/,
    /// and last Deadlock's path even when its file differs (unreleased heroes point at the stand-in gen_man.vmdl, so that one
    /// never comes first). The first one in this Deadlock install wins.</summary>
    static string FindModel(Package package, HeroEntry hero, Dictionary<string, string> heroModels)
    {
        var tries = new List<string>();
        heroModels.TryGetValue(hero.Key ?? "", out var own);
        bool sameFile = own != null && string.Equals(Path.GetFileName(own), Path.GetFileName(hero.Vmdl), StringComparison.OrdinalIgnoreCase);
        if (sameFile) tries.Add(own!);
        tries.Add(hero.Vmdl);
        if (hero.Vmdl.Contains("/heroes_wip/")) tries.Add(hero.Vmdl.Replace("/heroes_wip/", "/heroes_staging/"));
        else if (hero.Vmdl.Contains("/heroes_staging/")) tries.Add(hero.Vmdl.Replace("/heroes_staging/", "/heroes_wip/"));
        if (own != null && !sameFile) tries.Add(own);
        tries = tries.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var t in tries)
            if (package.FindEntry(t + "_c") != null)
            {
                Log($"  {hero.Id}: model {t}" + (string.Equals(t, hero.Vmdl, StringComparison.OrdinalIgnoreCase) ? "" : $" (the manifest says {hero.Vmdl})"));
                return t;
            }
        throw new Exception("model not found: " + string.Join(" or ", tries));
    }

    static int ListModels(string deadlock)
    {
        var vpkPath = FindVpk(deadlock);
        using var package = new Package();
        package.OptimizeEntriesForBinarySearch(StringComparison.OrdinalIgnoreCase);
        package.Read(vpkPath);
        using var loader = new GameFileLoader(package, vpkPath);
        foreach (var kv in HeroModels(loader).OrderBy(k => k.Key)) Console.WriteLine($"{kv.Key}\t{kv.Value}");
        return 0;
    }

    static void TryDeleteDir(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
    }

    public static string ImageFileName(string vtexPath) =>
        vtexPath.Replace('\\', '/').Replace(".vtex", "").Replace('/', '_') + ".png";

    // ---------------- textures ----------------

    static bool ExportImage(GameFileLoader loader, string path, string outFile)
    {
        try
        {
            var res = loader.LoadFileCompiled(path);
            if (res?.DataBlock is not ValveResourceFormat.ResourceTypes.Texture tex) { Log("  missing image " + path); return false; }
            using var bmp = tex.GenerateBitmap();
            using var scaled = Downscale(bmp, 512);
            File.WriteAllBytes(outFile, TextureExtract.ToPngImage(scaled));
            return true;
        }
        catch (Exception e)
        {
            Log($"  image {path} failed: {e.Message}");
            return false;
        }
    }

    static SKBitmap Downscale(SKBitmap src, int max)
    {
        if (src.Width <= max && src.Height <= max) return src.Copy();
        float k = (float)max / Math.Max(src.Width, src.Height);
        var info = new SKImageInfo(Math.Max(1, (int)(src.Width * k)), Math.Max(1, (int)(src.Height * k)), SKColorType.Rgba8888, SKAlphaType.Unpremul);
        return src.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
    }

    // ---------------- models ----------------

    static void ExportHero(GameFileLoader loader, HeroEntry hero, string dir)
    {
        Log($"Hero {hero.Id}: {hero.Vmdl}");
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, "_gltf");
        Directory.CreateDirectory(tmp);

        var res = loader.LoadFileCompiled(hero.Vmdl) ?? throw new Exception("model not found: " + hero.Vmdl);
        var exporter = new GltfModelExporter(loader)
        {
            ExportAnimations = true,
            ExportMaterials = true,
            AdaptTextures = true,
            SatelliteImages = false,
            // Deadlock's shoot/land clips are additive layers; compose them over the bind pose so every clip is a full pose
            ComposeAdditiveAnimations = true,
            ProgressReporter = new Progress<string>(_ => { }),
        };
        foreach (var c in hero.Clips) exporter.AnimationFilter.Add(c);
        foreach (var m in hero.Meshes) exporter.MeshFilter.Add(m);
        var glb = Path.Combine(tmp, hero.Id + ".glb");
        exporter.Export(res, glb, CancellationToken.None);
        if (!File.Exists(glb)) throw new Exception("glTF export produced nothing for " + hero.Id);

        var model = ModelRoot.Load(glb);
        // heroes keep their look as approved (whole-material tints/glows would change them; Deadlock's glow is masked): props only
        var stats = DlmWriter.Write(model, hero, dir, Log);
        Log($"  {hero.Id}: {stats}");
        Directory.Delete(tmp, true);
    }

    /// <summary>One effect model (a prop): its meshes and materials to DLM; since 1.6.0 also its skeleton and the clips the
    /// manifest names (Rat King's rats run, his banner's sewer rises), and only the meshes it names (none listed = all).</summary>
    static void ExportProp(GameFileLoader loader, PropEntry prop, string dir)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, "_gltf");
        Directory.CreateDirectory(tmp);
        var res = loader.LoadFileCompiled(prop.Vmdl) ?? throw new Exception("model not found: " + prop.Vmdl);
        var exporter = new GltfModelExporter(loader)
        {
            ExportAnimations = prop.Clips.Count > 0,
            ExportMaterials = true,
            AdaptTextures = true,
            SatelliteImages = false,
            ComposeAdditiveAnimations = true,
            ProgressReporter = new Progress<string>(_ => { }),
        };
        foreach (var c in prop.Clips) exporter.AnimationFilter.Add(c);
        foreach (var m in prop.Meshes) exporter.MeshFilter.Add(m);
        var glb = Path.Combine(tmp, prop.Id + ".glb");
        exporter.Export(res, glb, CancellationToken.None);
        if (!File.Exists(glb)) throw new Exception("glTF export produced nothing");
        var model = ModelRoot.Load(glb);
        var stats = DlmWriter.Write(model, new HeroEntry { Id = prop.Id, Vmdl = prop.Vmdl, Meshes = prop.Meshes, Clips = prop.Clips }, dir, Log, n => Vmat(loader, n, prop.Vmdl));
        Log($"  prop {prop.Id}: {stats}");
        Directory.Delete(tmp, true);
    }

    // every .vmat in the VPK by its file name (built once), for the material settings glTF leaves out (DLM2)
    static Dictionary<string, List<string>>? vmats;
    static readonly Dictionary<string, VmatInfo?> vmatInfos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The Deadlock settings of the material named name (glTF names a material after its .vmat file), preferring the
    /// one in the model's own folder: g_vColorTint1, F_ALPHA_TEST, F_VERTEX_COLOR, F_SELF_ILLUM with its tint and scale.</summary>
    static VmatInfo? Vmat(GameFileLoader loader, string name, string vmdl)
    {
        if (vmats == null)
        {
            vmats = new(StringComparer.OrdinalIgnoreCase);
            if (loader.CurrentPackage?.Entries.TryGetValue("vmat_c", out var list) == true)
                foreach (var e in list)
                {
                    var f = e.GetFullPath(); f = f[..^2];
                    var key = Path.GetFileNameWithoutExtension(f);
                    if (!vmats.TryGetValue(key, out var paths)) vmats[key] = paths = new List<string>();
                    paths.Add(f);
                }
        }
        var folder = Path.GetDirectoryName(vmdl)?.Replace('\\', '/') ?? "";
        if (vmatInfos.TryGetValue(folder + "|" + name, out var cached)) return cached;
        VmatInfo? info = null;
        try
        {
            if (vmats.TryGetValue(name, out var paths))
            {
                var path = paths.FirstOrDefault(p => p.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) ?? paths[0];
                if (loader.LoadFileCompiled(path)?.DataBlock is ValveResourceFormat.ResourceTypes.Material m)
                {
                    long I(string k) => m.IntParams.TryGetValue(k, out var v) ? v : 0;
                    float F(string k, float d) => m.FloatParams.TryGetValue(k, out var v) ? v : d;
                    Vector4 V(string k) => m.VectorParams.TryGetValue(k, out var v) ? v : Vector4.One;
                    var tint = m.VectorParams.ContainsKey("g_vColorTint1") ? V("g_vColorTint1") : V("g_vColorTint");
                    var em = Vector3.Zero;
                    if (I("F_SELF_ILLUM") != 0)
                    {
                        var st = V("g_vSelfIllumTint1");
                        em = new Vector3(st.X, st.Y, st.Z) * F("g_flSelfIllumScale1", 1f);
                    }
                    info = new VmatInfo(new Vector3(tint.X, tint.Y, tint.Z), I("F_ALPHA_TEST") != 0, I("F_VERTEX_COLOR") != 0, em);
                }
            }
        }
        catch (Exception e) { Log($"  material {name}: {e.Message}"); }
        vmatInfos[folder + "|" + name] = info;
        return info;
    }

    // ---------------- sounds ----------------

    static int ExportSounds(Package package, GameFileLoader loader, List<string> wanted, List<string> voiceGroups, string dir, out int voiceOk)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        var events = new Dictionary<string, ValveKeyValue.KVObject>(StringComparer.OrdinalIgnoreCase);
        if (package.Entries.TryGetValue("vsndevts_c", out var entries))
        {
            foreach (var entry in entries)
            {
                var full = entry.GetFullPath();
                if (!full.StartsWith("soundevents/", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    var r = loader.LoadFileCompiled(full.Substring(0, full.Length - 2));
                    ValveKeyValue.KVObject? root = r?.DataBlock switch
                    {
                        BinaryKV3 kv => kv.Data.Root,
                        KeyValuesOrNTRO kn => kn.Data,
                        _ => null,
                    };
                    if (root != null)
                        foreach (var child in root.Children) events[child.Key] = child.Value;
                }
                catch (Exception e) { Log($"  soundevents {full}: {e.Message}"); }
            }
        }
        Log($"  {events.Count} sound events indexed");

        var index = new Dictionary<string, object>();
        int ok = 0;
        foreach (var name in wanted)
        {
          try
          {
            var files = Resolve(events, name, "vsnd_files", 0);
            // Deadlock's "volume" is in decibels (0 = as recorded, -1 a touch quieter, +2 louder); the plugin mixes with a
            // linear gain, so convert (before 1.2.0 the dB number was used as the gain, and every event at 0 dB or below
            // played at the mixer's 20 % floor)
            float volume = MathF.Pow(10f, (ResolveFloat(events, name, "volume", 0) ?? 0f) / 20f);
            if (files.Count == 0) { Log($"  sound event {name}: no vsnd files"); continue; }
            var outFiles = new List<string>();
            foreach (var f in files.Take(4))
            {
                var wav = DecodeSound(loader, f);
                if (wav == null) continue;
                var fn = SafeName(name) + "_" + outFiles.Count + ".wav";
                File.WriteAllBytes(Path.Combine(dir, fn), wav);
                outFiles.Add(fn);
            }
            if (outFiles.Count == 0) { Log($"  sound event {name}: none decoded"); continue; }
            index[name] = new { volume = Math.Clamp(volume, 0f, 4f), files = outFiles };
            ok++;
          }
          catch (Exception e) { Log($"  sound event {name}: {e.Message}"); }
        }
        voiceOk = ExportVoice(package, loader, voiceGroups, dir, index);
        File.WriteAllText(Path.Combine(dir, "sounds.json"), JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true }));
        return ok;
    }

    /// <summary>Voice lines are loose files, not sound events: every numbered take of a group, sounds/vo/&lt;group&gt;_NN.vsnd,
    /// becomes one event "vo:&lt;group&gt;" in sounds.json (the plugin picks a take at random).</summary>
    static int ExportVoice(Package package, GameFileLoader loader, List<string> groups, string dir, Dictionary<string, object> index)
    {
        if (groups.Count == 0 || !package.Entries.TryGetValue("vsnd_c", out var entries)) return 0;
        var paths = entries.Select(e => e.GetFullPath()).ToList();
        int ok = 0;
        foreach (var g in groups)
        {
            var take = new System.Text.RegularExpressions.Regex("^sounds/vo/" + System.Text.RegularExpressions.Regex.Escape(g) + "_[0-9]+\\.vsnd_c$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var outFiles = new List<string>();
            foreach (var full in paths.Where(p => take.IsMatch(p)).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).Take(24))
            {
                var wav = DecodeSound(loader, full.Substring(0, full.Length - 2));
                if (wav == null) continue;
                var fn = "vo_" + SafeName(g) + "_" + outFiles.Count + ".wav";
                File.WriteAllBytes(Path.Combine(dir, fn), wav);
                outFiles.Add(fn);
            }
            if (outFiles.Count == 0) { Log($"  voice group {g}: no takes"); continue; }
            index["vo:" + g] = new { volume = 1f, files = outFiles };
            ok++;
        }
        return ok;
    }

    static string SafeName(string s) => new string(s.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

    static List<string> Resolve(Dictionary<string, ValveKeyValue.KVObject> events, string name, string key, int depth)
    {
        var result = new List<string>();
        if (depth > 8 || !events.TryGetValue(name, out var ev)) return result;
        if (ev.TryGetValue(key, out var v) && v != null && !v.IsNull)
        {
            if (v.IsArray)
            {
                for (int i = 0; i < v.Count; i++) result.Add(Str(v[i]));
            }
            else
            {
                var s = Str(v);
                if (!string.IsNullOrEmpty(s)) result.Add(s);
            }
            result.RemoveAll(string.IsNullOrEmpty);
            if (result.Count > 0) return result;
        }
        if (ev.TryGetValue("base", out var b) && b != null && !b.IsNull) return Resolve(events, Str(b), key, depth + 1);
        return result;
    }

    static float? ResolveFloat(Dictionary<string, ValveKeyValue.KVObject> events, string name, string key, int depth)
    {
        if (depth > 8 || !events.TryGetValue(name, out var ev)) return null;
        if (ev.TryGetValue(key, out var v) && v != null && !v.IsNull && float.TryParse(Str(v), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f)) return f;
        return ev.TryGetValue("base", out var b) && b != null && !b.IsNull ? ResolveFloat(events, Str(b), key, depth + 1) : null;
    }

    static string Str(ValveKeyValue.KVObject o) => Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture) ?? "";

    static byte[]? DecodeSound(GameFileLoader loader, string vsnd)
    {
        try
        {
            var path = vsnd.EndsWith(".vsnd") ? vsnd : vsnd + ".vsnd";
            var res = loader.LoadFileCompiled(path);
            if (res?.DataBlock is not Sound snd) return null;
            using var stream = snd.GetSoundStream();
            if (snd.SoundType == Sound.AudioFileType.MP3)
                return Mp3ToWav(stream);
            if (snd.SoundType == Sound.AudioFileType.WAV)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            return null;
        }
        catch (Exception e)
        {
            Log($"  sound {vsnd}: {e.Message}");
            return null;
        }
    }

    static byte[] Mp3ToWav(Stream mp3)
    {
        var mpeg = new NLayer.MpegFile(mp3);
        int channels = mpeg.Channels, rate = mpeg.SampleRate;
        var samples = new List<short>();
        var buf = new float[4096];
        int n;
        while ((n = mpeg.ReadSamples(buf, 0, buf.Length)) > 0)
            for (int i = 0; i < n; i++)
                samples.Add((short)Math.Clamp(buf[i] * 32767f, -32768f, 32767f));
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        int dataBytes = samples.Count * 2;
        w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + dataBytes); w.Write(Encoding.ASCII.GetBytes("WAVE"));
        w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)channels);
        w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data")); w.Write(dataBytes);
        foreach (var s in samples) w.Write(s);
        w.Flush();
        return ms.ToArray();
    }
}

public sealed class Manifest
{
    public List<HeroEntry> Heroes { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public List<string> SoundEvents { get; set; } = new();
    public List<string> VoiceGroups { get; set; } = new();
    public List<PropEntry> Props { get; set; } = new();
}

public sealed class PropEntry
{
    public string Id { get; set; } = "";
    public string Vmdl { get; set; } = "";
    public List<string> Meshes { get; set; } = new();   // 1.6.0: only these meshes (empty = every mesh)
    public List<string> Clips { get; set; } = new();    // 1.6.0: these clips, with the skeleton they move
}

public sealed class HeroEntry
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";   // Deadlock's hero key (hero_inferno), for the heroes.vdata model lookup
    public string Vmdl { get; set; } = "";
    public List<string> Meshes { get; set; } = new();
    public List<string> Clips { get; set; } = new();
    public List<string> Split { get; set; } = new();    // 1.6.0: meshes kept as their own submesh (Rat King's ammo belt)
}
