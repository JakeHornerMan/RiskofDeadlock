// DeadlockExtract: reads the four heroes' models, animations, icons and sounds from the PLAYER'S OWN Deadlock install
// and writes a local cache that the Risk of Rain 2 plugin loads. Nothing from Deadlock ships with the mod.
//
//   DeadlockExtract.exe --deadlock <Deadlock folder> --out <cache folder> [--manifest manifest.json] [--force]
//
// Melty runs this once before the first start (recipe.setup) and again when Deadlock updates.
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
    public const string Version = "1.1.0";
    static StreamWriter? log;

    public static int Main(string[] args)
    {
        string? deadlock = null, outDir = null, manifestPath = null;
        bool force = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--deadlock": deadlock = args[++i]; break;
                case "--out": outDir = args[++i]; break;
                case "--manifest": manifestPath = args[++i]; break;
                case "--force": force = true; break;
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
            return Run(deadlock, outDir, manifestPath, force);
        }
        catch (Exception e)
        {
            Log("FAILED: " + e);
            Console.Error.WriteLine("DeadlockExtract failed: " + e.Message);
            return 1;
        }
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
        foreach (var hero in manifest.Heroes)
            ExportHero(loader, hero, Path.Combine(heroesDir, hero.Id));

        var imagesDir = Path.Combine(outDir, "images");
        Directory.CreateDirectory(imagesDir);
        int images = 0;
        foreach (var img in manifest.Images)
            if (ExportImage(loader, img, Path.Combine(imagesDir, ImageFileName(img)))) images++;
        Log($"Images: {images}/{manifest.Images.Count}");

        int sounds = ExportSounds(package, loader, manifest.SoundEvents, manifest.VoiceGroups, Path.Combine(outDir, "sounds"), out int voiceGroups);
        Log($"Sound events: {sounds}/{manifest.SoundEvents.Count}, voice groups: {voiceGroups}/{manifest.VoiceGroups.Count}");

        var readyJson = JsonSerializer.Serialize(new
        {
            stamp,
            deadlockBuild = build,
            extractor = Version,
            heroes = manifest.Heroes.Select(h => h.Id).ToArray(),
            images,
            soundEvents = sounds,
            voiceGroups,
            time = DateTime.UtcNow.ToString("o"),
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(readyPath, readyJson);
        Log("Done: " + readyPath);
        return 0;
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
        var stats = DlmWriter.Write(model, hero, dir, Log);
        Log($"  {hero.Id}: {stats}");
        Directory.Delete(tmp, true);
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
            var files = Resolve(events, name, "vsnd_files", 0);
            float volume = ResolveFloat(events, name, "volume", 0) ?? 1f;
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
}

public sealed class HeroEntry
{
    public string Id { get; set; } = "";
    public string Vmdl { get; set; } = "";
    public List<string> Meshes { get; set; } = new();
    public List<string> Clips { get; set; } = new();
}
