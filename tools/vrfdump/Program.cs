// vrfdump: dev-only Deadlock lookups on the extractor's own ValveResourceFormat (20.1), for when the VRF CLI is too old for
// the installed Deadlock (build 6795 broke CLI 20.0). Reads the VPK only; never writes to Deadlock. Not shipped.
//   dotnet build tools/vrfdump -c Release -o build/vrfdump
//   build/vrfdump/vrfdump.exe <pak01_dir.vpk> <mode> <path> [out] [materials]
//   list   <text>          every VPK file whose path contains text (list "" = all: tools/dev/vpk_files.txt)
//   glb    <x.vmdl> <out>  the model with every clip and mesh (anim_names.json, bones, heights); any 5th argument adds materials
//   text   <x.vdata|x.vsndevts> <out>   decompiled to text (heroes.vdata, a hero's sound events)
//   wav    <x.vsnd> <out>  one sound decoded to wav (then: python tools/sound_env.py out.wav)
//   events x <out>         every sound event name (tools/dev/sound_events.txt)
//   png    <x.vtex> <out>  one texture as a png (to look at an icon before picking it; never copied into the repo)
// Run it with stdin from /dev/null in a bash loop (it otherwise reads the loop's input), and strip carriage returns from list files.
using SteamDatabase.ValvePak;
using ValveResourceFormat;
using ValveResourceFormat.IO;
using ValveResourceFormat.ResourceTypes;
using System.Text;

var vpk = args[0]; var mode = args[1]; var path = args[2]; var outp = args.Length > 3 ? args[3] : null;
using var package = new Package();
package.OptimizeEntriesForBinarySearch(StringComparison.OrdinalIgnoreCase);
package.Read(vpk);
using var loader = new GameFileLoader(package, vpk);
switch (mode)
{
    case "list":
        foreach (var kv in package.Entries) foreach (var e in kv.Value) { var f = e.GetFullPath(); if (f.Contains(path, StringComparison.OrdinalIgnoreCase)) Console.WriteLine(f); }
        break;
    case "events":
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var e in package.Entries["vsndevts_c"]) { var f = e.GetFullPath(); if (!f.StartsWith("soundevents/")) continue;
            try { var r = loader.LoadFileCompiled(f[..^2]); ValveKeyValue.KVObject root = r?.DataBlock switch { BinaryKV3 kv => kv.Data.Root, KeyValuesOrNTRO kn => kn.Data, _ => null };
                if (root != null) foreach (var c in root.Children) names.Add(c.Key); } catch { } }
        File.WriteAllLines(outp, names);
        break;
    }
    case "glb":
    {
        var res = loader.LoadFileCompiled(path);
        var ex = new GltfModelExporter(loader) { ExportAnimations = true, ExportMaterials = args.Length > 4, AdaptTextures = true, SatelliteImages = false, ComposeAdditiveAnimations = true, ProgressReporter = new Progress<string>(_ => { }) };
        ex.Export(res, outp, CancellationToken.None);
        break;
    }
    case "text":
    {
        var res = loader.LoadFileCompiled(path);
        string txt = res.DataBlock switch {
            BinaryKV3 kv => kv.ToString(),
            _ => res.DataBlock.ToString() };
        File.WriteAllText(outp, txt);
        break;
    }
    case "png":
    {
        var res = loader.LoadFileCompiled(path);
        using var bmp = ((Texture)res.DataBlock).GenerateBitmap();
        File.WriteAllBytes(outp, TextureExtract.ToPngImage(bmp));
        break;
    }
    case "wav":
    {
        var res = loader.LoadFileCompiled(path);
        var snd = (Sound)res.DataBlock;
        using var st = snd.GetSoundStream();
        if (snd.SoundType == Sound.AudioFileType.MP3)
        {
            var mpeg = new NLayer.MpegFile(st); int ch = mpeg.Channels, rate = mpeg.SampleRate;
            var s = new List<short>(); var buf = new float[4096]; int n;
            while ((n = mpeg.ReadSamples(buf, 0, buf.Length)) > 0) for (int i = 0; i < n; i++) s.Add((short)Math.Clamp(buf[i]*32767f,-32768f,32767f));
            using var fs = File.Create(outp); using var w = new BinaryWriter(fs); int db = s.Count*2;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36+db); w.Write(Encoding.ASCII.GetBytes("WAVE")); w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(rate); w.Write(rate*ch*2); w.Write((short)(ch*2)); w.Write((short)16); w.Write(Encoding.ASCII.GetBytes("data")); w.Write(db); foreach (var x in s) w.Write(x);
        }
        else { using var fs = File.Create(outp); st.CopyTo(fs); }
        break;
    }
}
