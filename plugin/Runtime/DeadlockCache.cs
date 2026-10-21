using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Reads what DeadlockExtract wrote from the player's own Deadlock install (models, icons, sounds).</summary>
    public static class DeadlockCache
    {
        public static string Dir;
        public static bool Ready;
        public static string DeadlockBuild = "?";
        public static readonly Dictionary<string, HeroAsset> Heroes = new Dictionary<string, HeroAsset>();
        /// <summary>Heroes the extractor could not read from this Deadlock build (ready.json "failed", extractor 1.3.0+),
        /// with its error. The other heroes still load; these are skipped and named in a title-screen message.</summary>
        public static readonly Dictionary<string, string> Failed = new Dictionary<string, string>();
        static readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static string DefaultDir()
        {
            var env = Environment.GetEnvironmentVariable("DEADLOCKOFRAIN_CACHE");
            if (!string.IsNullOrEmpty(env)) return env;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "cache");
        }

        public static bool Open(string dir)
        {
            Dir = dir;
            var ready = Path.Combine(dir, "ready.json");
            if (!File.Exists(ready))
            {
                Log.Error($"Deadlock content is missing ({ready}). It is read from your own Deadlock install (Melty does it before the first start; the Thunderstore build on the loading screen).");
                return false;
            }
            var text = File.ReadAllText(ready);
            var i = text.IndexOf("\"deadlockBuild\"", StringComparison.Ordinal);
            if (i >= 0)
            {
                var a = text.IndexOf('"', text.IndexOf(':', i) + 1);
                var b = text.IndexOf('"', a + 1);
                if (a > 0 && b > a) DeadlockBuild = text.Substring(a + 1, b - a - 1);
            }
            ReadFailed(text);
            Ready = true;
            return true;
        }

        static void ReadFailed(string text)
        {
            Failed.Clear();
            var i = text.IndexOf("\"failed\"", StringComparison.Ordinal);
            if (i < 0) return;
            var block = text.Substring(i);   // only the failed list has id/error pairs
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(block,
                         "\"id\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"error\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
                Failed[m.Groups[1].Value] = System.Text.RegularExpressions.Regex.Unescape(m.Groups[2].Value);
            AnnounceFailed();
        }

        /// <summary>Test mode ("break": "infernus"): treat a hero as unreadable although its cache is fine, so the skip path
        /// (red ! tile, title-screen message, loading summary) is tested on every release.</summary>
        public static void MarkFailed(string id, string why)
        {
            Failed[id] = why;
            FirstRun.Problem = null;
            AnnounceFailed();
            FirstRun.ShowSummary();
        }

        static void AnnounceFailed()
        {
            if (Failed.Count == 0) return;
            var names = new List<string>();
            foreach (var kv in Failed)
            {
                Log.Error($"{kv.Key}: Deadlock build {DeadlockBuild} could not be read for this hero: {kv.Value}");
                var row = Array.Find(Sheets.HeroRow.All, h => h.Id == kv.Key);
                names.Add(row != null ? row.DisplayName : kv.Key);
            }
            if (FirstRun.Problem == null)
            {
                FirstRun.ProblemHeader = "Deadlock of Rain: " + string.Join(", ", names) + " not available";
                FirstRun.Problem = string.Join(", ", names) + " couldn't"
                    + $" be read from this Deadlock version (build {DeadlockBuild}); the other heroes are ready to play. "
                    + (names.Count == 1 ? "Its" : "Their") + " portrait shows a red ! in character select.\n\n" + BuiltWith.Versions()
                    + "\n\nDetails: BepInEx/LogOutput.log.";
            }
        }

        public static HeroAsset LoadHero(string id)
        {
            if (Heroes.TryGetValue(id, out var h)) return h;
            var dir = Path.Combine(Dir, "heroes", id);
            var file = Path.Combine(dir, id + ".dlm");
            if (!File.Exists(file)) { Log.Error("missing hero cache " + file); return null; }
            h = HeroAsset.Read(file, dir);
            Heroes[id] = h;
            return h;
        }

        static readonly Dictionary<string, Mesh> props = new Dictionary<string, Mesh>();

        /// <summary>An effect model (effects kind prop: Viscous' cube, ball, fist) as one plain mesh in metres, or null when the
        /// extractor could not read it (ready.json failedProps) or the cache predates 1.5.0; the effect then draws a simple shape.</summary>
        public static Mesh Prop(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "none" || Dir == null) return null;
            if (props.TryGetValue(id, out var m)) return m;
            var dir = Path.Combine(Dir, "props", id);
            var file = Path.Combine(dir, id + ".dlm");
            try
            {
                if (File.Exists(file))
                {
                    var a = HeroAsset.Read(file, dir);
                    var idx = new List<int>();
                    foreach (var sm in a.Submeshes) idx.AddRange(sm.Value);
                    m = new Mesh { name = "mshDeadlock_" + id };
                    m.indexFormat = a.Positions.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                    m.vertices = a.Positions; m.normals = a.Normals; m.uv = a.Uvs;
                    m.SetTriangles(idx, 0, true);
                    m.RecalculateBounds();
                    // kept readable: the cel goo re-colours and oozes its vertices every frame (a no-longer-readable mesh gives it none)
                    m.UploadMeshData(false);
                }
                else Log.Warn($"effect model {id} not in the cache (extractor 1.5.0+ reads it); a simple shape stands in");
            }
            catch (Exception e) { Log.Warn($"effect model {id}: {e.Message}"); m = null; }
            props[id] = m;
            return m;
        }

        static readonly Dictionary<string, HeroAsset> propAssets = new Dictionary<string, HeroAsset>();

        /// <summary>An effect model read whole: skeleton, clips and textures (extractor 1.6.0+: Rat King's rat and banner, drawn by
        /// PropModels), or null when it isn't in the cache; the effect then draws without it.</summary>
        public static HeroAsset PropAsset(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "none" || Dir == null) return null;
            if (propAssets.TryGetValue(id, out var a)) return a;
            var dir = Path.Combine(Dir, "props", id);
            var file = Path.Combine(dir, id + ".dlm");
            try
            {
                if (File.Exists(file)) a = HeroAsset.Read(file, dir);
                else Log.Warn($"effect model {id} not in the cache (extractor 1.6.0+ reads it)");
            }
            catch (Exception e) { Log.Warn($"effect model {id}: {e.Message}"); a = null; }
            propAssets[id] = a;
            return a;
        }

        public static string ImageFile(string vtex) =>
            Path.Combine(Dir, "images", vtex.Replace('\\', '/').Replace(".vtex", "").Replace('/', '_') + ".png");

        public static Texture2D Image(string vtex)
        {
            if (string.IsNullOrEmpty(vtex) || vtex == "none") return null;
            if (images.TryGetValue(vtex, out var t)) return t;
            var f = ImageFile(vtex);
            if (File.Exists(f))
            {
                t = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                t.LoadImage(File.ReadAllBytes(f));
                t.wrapMode = TextureWrapMode.Clamp;
                t.name = Path.GetFileNameWithoutExtension(f);
            }
            else Log.Warn("missing image " + f);
            images[vtex] = t;
            return t;
        }

        public static Sprite Sprite(string vtex)
        {
            if (string.IsNullOrEmpty(vtex) || vtex == "none") return null;
            if (sprites.TryGetValue(vtex, out var s)) return s;
            var t = Image(vtex);
            s = t ? UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f) : null;
            sprites[vtex] = s;
            return s;
        }
    }

    public sealed class BoneDef
    {
        public string Name;
        public int Parent;
        public Vector3 Pos;
        public Quaternion Rot;
        public Vector3 Scale;
        public Matrix4x4 Bindpose;
    }

    public sealed class MaterialDef
    {
        public string Name, Albedo, Normal;
        public int AlphaMode;
        public float Cutoff;
        public Color Color;
        // DLM2 (extractor 1.6.1): Deadlock's self-illumination and whether the material takes the mesh's vertex colours
        public Color Emission = Color.black;
        public bool VertexColor;
    }

    public sealed class ClipDef
    {
        public string Name;
        public float Length, Fps;
        public int Frames;
        public List<TrackDef> Tracks = new List<TrackDef>();
    }

    public sealed class TrackDef
    {
        public int Bone;
        public Vector3[] Pos;      // length 0 (rest), 1 (constant) or Frames
        public Quaternion[] Rot;   // length 1 or Frames
    }

    /// <summary>One hero read from DLM (see extractor/DlmWriter.cs for the layout), already in Unity space.</summary>
    public sealed class HeroAsset
    {
        public string Dir;
        public BoneDef[] Bones;
        public Vector3[] Positions, Normals;
        public Vector2[] Uvs;
        public Color[] Colors;     // vertex colours (DLM2, only when a material uses them), else null
        public BoneWeight[] Weights;
        public List<KeyValuePair<int, int[]>> Submeshes = new List<KeyValuePair<int, int[]>>();
        public MaterialDef[] Materials;
        public Dictionary<string, ClipDef> Clips = new Dictionary<string, ClipDef>(StringComparer.OrdinalIgnoreCase);
        Mesh mesh;
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        static Vector3 V3(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        static Quaternion Q(BinaryReader r) => new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        public static HeroAsset Read(string file, string dir)
        {
            var h = new HeroAsset { Dir = dir };
            using (var r = new BinaryReader(File.OpenRead(file), Encoding.UTF8))
            {
                var magic = new string(r.ReadChars(4));
                if (magic != "DLM1" && magic != "DLM2") throw new InvalidDataException("not a DLM file: " + file);
                int nb = r.ReadInt32();
                h.Bones = new BoneDef[nb];
                for (int i = 0; i < nb; i++)
                {
                    var b = new BoneDef { Name = r.ReadString(), Parent = r.ReadInt32(), Pos = V3(r), Rot = Q(r), Scale = V3(r) };
                    var m = new Matrix4x4();
                    for (int k = 0; k < 16; k++) m[k / 4, k % 4] = r.ReadSingle();
                    b.Bindpose = m;
                    h.Bones[i] = b;
                }
                int nv = r.ReadInt32();
                h.Positions = new Vector3[nv]; h.Normals = new Vector3[nv]; h.Uvs = new Vector2[nv]; h.Weights = new BoneWeight[nv];
                for (int i = 0; i < nv; i++) h.Positions[i] = V3(r);
                for (int i = 0; i < nv; i++) h.Normals[i] = V3(r);
                for (int i = 0; i < nv; i++) h.Uvs[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < nv; i++)
                {
                    h.Weights[i] = new BoneWeight
                    {
                        boneIndex0 = r.ReadInt32(), boneIndex1 = r.ReadInt32(), boneIndex2 = r.ReadInt32(), boneIndex3 = r.ReadInt32(),
                        weight0 = r.ReadSingle(), weight1 = r.ReadSingle(), weight2 = r.ReadSingle(), weight3 = r.ReadSingle(),
                    };
                }
                int ns = r.ReadInt32();
                for (int s = 0; s < ns; s++)
                {
                    int mat = r.ReadInt32(); int n = r.ReadInt32();
                    var idx = new int[n];
                    for (int k = 0; k < n; k++) idx[k] = r.ReadInt32();
                    h.Submeshes.Add(new KeyValuePair<int, int[]>(mat, idx));
                }
                int nm = r.ReadInt32();
                h.Materials = new MaterialDef[nm];
                for (int m = 0; m < nm; m++)
                    h.Materials[m] = new MaterialDef
                    {
                        Name = r.ReadString(), Albedo = r.ReadString(), Normal = r.ReadString(), AlphaMode = r.ReadInt32(), Cutoff = r.ReadSingle(),
                        Color = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                    };
                int nc = r.ReadInt32();
                for (int c = 0; c < nc; c++)
                {
                    var clip = new ClipDef { Name = r.ReadString(), Length = r.ReadSingle(), Fps = r.ReadSingle(), Frames = r.ReadInt32() };
                    int nt = r.ReadInt32();
                    for (int t = 0; t < nt; t++)
                    {
                        var tr = new TrackDef { Bone = r.ReadInt32() };
                        int np = r.ReadInt32(); tr.Pos = new Vector3[np];
                        for (int k = 0; k < np; k++) tr.Pos[k] = V3(r);
                        int nr = r.ReadInt32(); tr.Rot = new Quaternion[nr];
                        for (int k = 0; k < nr; k++) tr.Rot[k] = Q(r);
                        clip.Tracks.Add(tr);
                    }
                    h.Clips[clip.Name] = clip;
                }
                if (magic == "DLM2")
                {
                    if (r.ReadInt32() == 1)
                    {
                        h.Colors = new Color[nv];
                        for (int i = 0; i < nv; i++) h.Colors[i] = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    }
                    foreach (var m in h.Materials)
                    {
                        m.Emission = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), 1f);
                        m.VertexColor = r.ReadInt32() == 1;
                    }
                }
            }
            return h;
        }

        Mesh[] parts;

        /// <summary>One mesh per material (RoR2's CharacterModel drives one material per renderer), each holding only the
        /// vertices its triangles use. All parts share the skeleton and bind poses.</summary>
        public Mesh[] Parts
        {
            get
            {
                if (parts != null) return parts;
                var bp = new Matrix4x4[Bones.Length];
                for (int i = 0; i < bp.Length; i++) bp[i] = Bones[i].Bindpose;
                parts = new Mesh[Submeshes.Count];
                for (int s = 0; s < Submeshes.Count; s++)
                {
                    var src = Submeshes[s].Value;
                    var map = new Dictionary<int, int>();
                    var idx = new int[src.Length];
                    var pos = new List<Vector3>(); var nrm = new List<Vector3>(); var uv = new List<Vector2>(); var bw = new List<BoneWeight>();
                    for (int k = 0; k < src.Length; k++)
                    {
                        if (!map.TryGetValue(src[k], out var ni))
                        {
                            ni = pos.Count; map[src[k]] = ni;
                            pos.Add(Positions[src[k]]); nrm.Add(Normals[src[k]]); uv.Add(Uvs[src[k]]); bw.Add(Weights[src[k]]);
                        }
                        idx[k] = ni;
                    }
                    var mesh = new Mesh { name = Path.GetFileName(Dir) + "_part" + s };
                    mesh.indexFormat = pos.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                    mesh.SetVertices(pos); mesh.SetNormals(nrm); mesh.SetUVs(0, uv);
                    var cols = new Color32[pos.Count];
                    var mat = Submeshes[s].Key;
                    bool vc = Colors != null && mat >= 0 && mat < Materials.Length && Materials[mat].VertexColor;
                    if (vc) foreach (var kv in map) cols[kv.Value] = Colors[kv.Key];
                    else for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(255, 255, 255, 255);
                    mesh.colors32 = cols;
                    mesh.boneWeights = bw.ToArray();
                    mesh.bindposes = bp;
                    mesh.SetTriangles(idx, 0, true);
                    mesh.RecalculateTangents();
                    mesh.UploadMeshData(false);
                    parts[s] = mesh;
                }
                return parts;
            }
        }

        public Texture2D Texture(string file, bool linear)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (textures.TryGetValue(file, out var t)) return t;
            var path = Path.Combine(Dir, file);
            if (File.Exists(path))
            {
                t = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
                t.LoadImage(File.ReadAllBytes(path));
                t.name = Path.GetFileName(Dir) + "_" + file;
                if (t.width % 4 == 0 && t.height % 4 == 0) t.Compress(true);
                t.Apply(true, true);
            }
            textures[file] = t;
            return t;
        }
    }
}
