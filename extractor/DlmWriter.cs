// Converts a hero glTF (exported by ValveResourceFormat from the player's Deadlock install) into DLM, a small binary
// format the Risk of Rain 2 plugin reads without any glTF/JSON dependency. Everything is converted to Unity space here:
// glTF is right-handed, Unity left-handed, so X is mirrored (positions/normals x -> -x, quaternions (x,y,z,w) -> (x,-y,-z,w),
// matrices S*M*S with S = diag(-1,1,1,1), triangle winding reversed) and V is flipped (v -> 1-v).
//
// DLM layout (little endian, strings are BinaryWriter length-prefixed UTF-8):
//   "DLM2" (1.6.1; "DLM1" before: the same without the trailing block)
//   int boneCount; per bone: string name, int parent (-1 = model root), float3 localPos, float4 localRot(xyzw), float3 localScale,
//                            float[16] bindpose (Unity row-major m00 m01 m02 m03 m10 ...)
//   int vertexCount; float3 pos[]; float3 normal[]; float2 uv[]; per vertex: int4 bone, float4 weight
//   int submeshCount; per submesh: int material, int indexCount, int index[]
//   int materialCount; per material: string name, string albedoFile, string normalFile, int alphaMode (0 opaque,1 mask,2 blend), float cutoff, float4 color
//   int clipCount; per clip: string name, float length, float fps, int frames,
//                  int trackCount; per track: int bone, int posFrames (0|1|frames), float3 pos[posFrames], int rotFrames (1|frames), float4 rot[rotFrames]
//   DLM2 trailing block (Deadlock material settings the glTF leaves out, read from each .vmat):
//   int hasColors; if 1: float4 color[vertexCount] (the mesh's vertex colours, COLOR_0, white where a mesh has none)
//   per material: float3 emission (g_vSelfIllumTint1 x g_flSelfIllumScale1 when F_SELF_ILLUM, else 0), int vertexColor (F_VERTEX_COLOR)
//   The material colour above already carries g_vColorTint1, and F_ALPHA_TEST turns alphaMode to 1 (mask).
using System.Numerics;
using SharpGLTF.Schema2;
using SkiaSharp;
using ValveResourceFormat.IO;

namespace DeadlockExtract;

/// <summary>The Deadlock material settings DLM2 carries beyond glTF's (null = the material wasn't found).</summary>
public sealed record VmatInfo(Vector3 Tint, bool AlphaTest, bool VertexColor, Vector3 Emission);

public static class DlmWriter
{
    const float Fps = 30f;

    static Vector3 P(Vector3 v) => new(-v.X, v.Y, v.Z);
    static Quaternion Q(Quaternion q) => new(q.X, -q.Y, -q.Z, q.W);

    // Unity row-major (mRC) of S*M*S, from a System.Numerics (row-vector) matrix.
    static float[] UnityMatrix(Matrix4x4 m)
    {
        // numerics M(r+1)(c+1) applies as v*M; the column-vector matrix is its transpose: U[r,c] = N[c,r]
        float[,] n = {
            { m.M11, m.M12, m.M13, m.M14 },
            { m.M21, m.M22, m.M23, m.M24 },
            { m.M31, m.M32, m.M33, m.M34 },
            { m.M41, m.M42, m.M43, m.M44 } };
        var o = new float[16];
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
            {
                float s = ((r == 0) ^ (c == 0)) ? -1f : 1f;
                o[r * 4 + c] = n[c, r] * s;
            }
        return o;
    }

    static Matrix4x4 World(Node n)
    {
        var m = n.LocalMatrix;
        for (var p = n.VisualParent; p != null; p = p.VisualParent) m *= p.LocalMatrix;
        return m;
    }

    public static string Write(ModelRoot model, HeroEntry hero, string dir, Action<string> log, Func<string, VmatInfo?>? vmat = null)
    {
        // ---- skeleton: union of the joints of every skin used by an included mesh
        var meshNodes = model.LogicalNodes.Where(n => n.Mesh != null).ToList();
        var boneNodes = new List<Node>();
        var boneIndex = new Dictionary<Node, int>();
        foreach (var mn in meshNodes)
        {
            if (mn.Skin == null) continue;
            for (int j = 0; j < mn.Skin.JointsCount; j++)
            {
                var (joint, _) = mn.Skin.GetJoint(j);
                if (!boneIndex.ContainsKey(joint)) { boneIndex[joint] = boneNodes.Count; boneNodes.Add(joint); }
            }
        }
        // make sure parents of joints are bones too (so the hierarchy is closed)
        for (int i = 0; i < boneNodes.Count; i++)
        {
            var p = boneNodes[i].VisualParent;
            if (p != null && !boneIndex.ContainsKey(p) && p.VisualParent != null && model.LogicalSkins.Any(s => Enumerable.Range(0, s.JointsCount).Any(k => s.GetJoint(k).Joint == p)))
            { boneIndex[p] = boneNodes.Count; boneNodes.Add(p); }
        }
        var bindWorld = new Matrix4x4[boneNodes.Count];
        var haveBind = new bool[boneNodes.Count];
        foreach (var mn in meshNodes)
        {
            if (mn.Skin == null) continue;
            for (int j = 0; j < mn.Skin.JointsCount; j++)
            {
                var (joint, ibm) = mn.Skin.GetJoint(j);
                int bi = boneIndex[joint];
                if (!haveBind[bi]) { Matrix4x4.Invert(ibm, out var wm); bindWorld[bi] = wm; haveBind[bi] = true; }
            }
        }

        using var fs = File.Create(Path.Combine(dir, hero.Id + ".dlm"));
        using var w = new BinaryWriter(fs);
        w.Write("DLM2".ToCharArray());
        w.Write(boneNodes.Count);
        for (int i = 0; i < boneNodes.Count; i++)
        {
            var n = boneNodes[i];
            int parent = n.VisualParent != null && boneIndex.TryGetValue(n.VisualParent, out var pi) ? pi : -1;
            // a root bone carries the transform of any non-bone ancestors (VRF's model node is identity, but be safe)
            var local = parent >= 0 ? n.LocalMatrix : World(n);
            Matrix4x4.Decompose(local, out var s, out var r, out var t);
            w.Write(n.Name ?? ("bone" + i));
            w.Write(parent);
            WriteV3(w, P(t)); WriteQ(w, Q(r)); WriteV3(w, s);
            Matrix4x4 bw = haveBind[i] ? bindWorld[i] : World(n);
            Matrix4x4.Invert(bw, out var ibm);
            foreach (var f in UnityMatrix(ibm)) w.Write(f);
        }

        // ---- mesh: merge every primitive of every included mesh node
        var pos = new List<Vector3>(); var nrm = new List<Vector3>(); var uv = new List<Vector2>();
        var bones = new List<(int, int, int, int)>(); var weights = new List<Vector4>(); var colors = new List<Vector4>();
        var submeshes = new List<(int mat, List<int> idx)>();
        // a material slot is a glTF material, or (1.6.0) a material on one of the hero's Split meshes, kept as its own submesh
        // and named "<material>@<mesh>" so the plugin can tell it apart (Rat King's ammo belt: one mesh per round count)
        var materials = new List<(Material? mat, string? tag)>();
        foreach (var mn in meshNodes)
        {
            var meshShort = (mn.Mesh.Name ?? "").Split('.').Last();
            string? tag = hero.Split.Contains(meshShort, StringComparer.OrdinalIgnoreCase) ? meshShort : null;
            var skinMap = new int[mn.Skin?.JointsCount ?? 0];
            for (int j = 0; j < skinMap.Length; j++) skinMap[j] = boneIndex[mn.Skin!.GetJoint(j).Joint];
            int nodeBone = 0;
            var sharedBase = new Dictionary<int, int>();
            foreach (var prim in mn.Mesh.Primitives)
            {
                if (prim.DrawPrimitiveType != PrimitiveType.TRIANGLES) continue;
                var pa = prim.GetVertexAccessor("POSITION")?.AsVector3Array();
                if (pa == null) continue;
                var na = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var ta = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                var ja = prim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
                var wa = prim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
                var ca = prim.GetVertexAccessor("COLOR_0")?.AsColorArray(1f);
                // primitives of one VRF mesh share one vertex buffer: add it once
                int posAccessor = prim.GetVertexAccessor("POSITION").LogicalIndex;
                if (!sharedBase.TryGetValue(posAccessor, out int baseV))
                {
                baseV = pos.Count;
                sharedBase[posAccessor] = baseV;
                for (int v = 0; v < pa.Count; v++)
                {
                    pos.Add(P(pa[v]));
                    nrm.Add(na != null ? P(na[v]) : Vector3.UnitY);
                    var t = ta != null ? ta[v] : Vector2.Zero;
                    uv.Add(new Vector2(t.X, 1f - t.Y));
                    colors.Add(ca != null ? ca[v] : Vector4.One);
                    if (ja != null && wa != null && skinMap.Length > 0)
                    {
                        var jj = ja[v]; var ww = wa[v];
                        bones.Add((skinMap[(int)jj.X], skinMap[(int)jj.Y], skinMap[(int)jj.Z], skinMap[(int)jj.W]));
                        weights.Add(ww);
                    }
                    else { bones.Add((nodeBone, 0, 0, 0)); weights.Add(new Vector4(1, 0, 0, 0)); }
                }
                }
                var idx = prim.GetIndices();
                var list = new List<int>(idx.Count);
                for (int k = 0; k + 2 < idx.Count; k += 3)
                {   // reverse winding for the X mirror
                    list.Add(baseV + (int)idx[k]); list.Add(baseV + (int)idx[k + 2]); list.Add(baseV + (int)idx[k + 1]);
                }
                var mat = prim.Material;
                int mi = mat == null && tag == null ? -1 : materials.IndexOf((mat, tag));
                if ((mat != null || tag != null) && mi < 0) { mi = materials.Count; materials.Add((mat, tag)); }
                var existing = submeshes.FindIndex(sm => sm.mat == mi);
                if (existing >= 0) submeshes[existing].idx.AddRange(list); else submeshes.Add((mi, list));
            }
        }
        w.Write(pos.Count);
        foreach (var v in pos) WriteV3(w, v);
        foreach (var v in nrm) WriteV3(w, v);
        foreach (var v in uv) { w.Write(v.X); w.Write(v.Y); }
        for (int i = 0; i < pos.Count; i++)
        {
            var (a, b, c, d) = bones[i]; var wt = weights[i];
            float sum = wt.X + wt.Y + wt.Z + wt.W; if (sum <= 0) { wt = new Vector4(1, 0, 0, 0); sum = 1; }
            w.Write(a); w.Write(b); w.Write(c); w.Write(d);
            w.Write(wt.X / sum); w.Write(wt.Y / sum); w.Write(wt.Z / sum); w.Write(wt.W / sum);
        }
        w.Write(submeshes.Count);
        foreach (var (mat, idx) in submeshes)
        {
            w.Write(Math.Max(0, mat)); w.Write(idx.Count);
            foreach (var i in idx) w.Write(i);
        }
        if (materials.Count == 0) materials.Add((null, null));
        w.Write(materials.Count);
        // a split mesh's slot shares its material's textures: each real material's images are saved once
        var saved = new Dictionary<Material, (string albedo, string normal)>();
        var infos = new List<VmatInfo?>();
        for (int m = 0; m < materials.Count; m++)
        {
            var (mat, tag) = materials[m];
            string name = (mat?.Name ?? "default") + (tag != null ? "@" + tag : "");
            string albedo, normal;
            if (mat != null && saved.TryGetValue(mat, out var files)) (albedo, normal) = files;
            else
            {
                albedo = SaveChannel(mat, "BaseColor", dir, $"m{m}_albedo.png", 2048, log);
                normal = SaveChannel(mat, "Normal", dir, $"m{m}_normal.png", 1024, log);
                if (mat != null) saved[mat] = (albedo, normal);
            }
            int alpha = mat == null ? 0 : mat.Alpha switch { AlphaMode.MASK => 1, AlphaMode.BLEND => 2, _ => 0 };
            var info = mat?.Name != null && vmat != null ? vmat(mat.Name) : null;
            if (info != null && info.AlphaTest && alpha == 0) alpha = 1;
            w.Write(name); w.Write(albedo); w.Write(normal); w.Write(alpha);
            w.Write(mat?.AlphaCutoff ?? 0.5f);
            var col = mat?.FindChannel("BaseColor")?.Color ?? Vector4.One;
            if (info != null) col = new Vector4(col.X * info.Tint.X, col.Y * info.Tint.Y, col.Z * info.Tint.Z, col.W);
            infos.Add(info);
            w.Write(col.X); w.Write(col.Y); w.Write(col.Z); w.Write(col.W);
        }

        // ---- animations, sampled at 30 fps in each bone's local space
        var anims = model.LogicalAnimations.Where(a => hero.Clips.Contains(Short(a.Name), StringComparer.OrdinalIgnoreCase))
                                          .GroupBy(a => Short(a.Name), StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        w.Write(anims.Count);
        int totalTracks = 0;
        foreach (var anim in anims)
        {
            // 1.8.0: Duration read once; SharpGLTF works it out from every channel's keys on each read, and it was read per
            // frame per bone (Abrams, 499 bones: 9.5 min, Rat King 9, Wraith 7)
            float duration = anim.Duration;
            float length = Math.Max(duration, 1f / Fps);
            int frames = Math.Max(1, (int)Math.Round(length * Fps) + 1);
            w.Write(Short(anim.Name)); w.Write(length); w.Write(Fps); w.Write(frames);
            var tracks = new List<(int bone, Vector3[] p, Quaternion[] r)>();
            // the clip's channels indexed once instead of a Find*Channel scan per bone
            var tChannels = new Dictionary<Node, AnimationChannel>();
            var rChannels = new Dictionary<Node, AnimationChannel>();
            foreach (var ch in anim.Channels)
            {
                if (ch.TargetNode == null) continue;
                if (ch.TargetNodePath == PropertyPath.translation) tChannels.TryAdd(ch.TargetNode, ch);
                else if (ch.TargetNodePath == PropertyPath.rotation) rChannels.TryAdd(ch.TargetNode, ch);
            }
            for (int bi = 0; bi < boneNodes.Count; bi++)
            {
                var node = boneNodes[bi];
                var ps = new Vector3[frames]; var rs = new Quaternion[frames];
                bool moved = false;
                Matrix4x4.Decompose(node.LocalMatrix, out _, out var r0, out var t0);
                var tch = tChannels.GetValueOrDefault(node)?.GetTranslationSampler()?.CreateCurveSampler(true);
                var rch = rChannels.GetValueOrDefault(node)?.GetRotationSampler()?.CreateCurveSampler(true);
                if (tch == null && rch == null) continue;
                for (int f = 0; f < frames; f++)
                {
                    float time = Math.Min(f / Fps, duration);
                    ps[f] = tch != null ? tch.GetPoint(time) : t0;
                    rs[f] = Quaternion.Normalize(rch != null ? rch.GetPoint(time) : r0);
                    if (Vector3.DistanceSquared(ps[f], t0) > 1e-8f || Math.Abs(Quaternion.Dot(rs[f], r0)) < 0.999999f) moved = true;
                }
                if (!moved) continue;
                tracks.Add((bi, ps, rs));
            }
            w.Write(tracks.Count);
            totalTracks += tracks.Count;
            foreach (var (bone, ps, rs) in tracks)
            {
                w.Write(bone);
                bool posConst = ps.All(p => Vector3.DistanceSquared(p, ps[0]) < 1e-8f);
                w.Write(posConst ? 1 : frames);
                for (int f = 0; f < (posConst ? 1 : frames); f++) WriteV3(w, P(ps[f]));
                bool rotConst = rs.All(q => Math.Abs(Quaternion.Dot(q, rs[0])) > 0.999999f);
                w.Write(rotConst ? 1 : frames);
                for (int f = 0; f < (rotConst ? 1 : frames); f++) WriteQ(w, Q(rs[f]));
            }
        }
        // ---- DLM2: vertex colours (only when a material uses them) and each material's glow and vertex-colour flag
        bool anyVc = infos.Any(i => i != null && i.VertexColor);
        w.Write(anyVc ? 1 : 0);
        if (anyVc) foreach (var c in colors) { w.Write(c.X); w.Write(c.Y); w.Write(c.Z); w.Write(c.W); }
        foreach (var i in infos)
        {
            var e = i?.Emission ?? Vector3.Zero;
            w.Write(e.X); w.Write(e.Y); w.Write(e.Z); w.Write(i != null && i.VertexColor ? 1 : 0);
        }
        int tinted = infos.Count(i => i != null && (i.VertexColor || i.Emission != Vector3.Zero || i.Tint != Vector3.One || i.AlphaTest));
        if (tinted > 0) log($"  {hero.Id}: {tinted} material(s) with Deadlock tint/glow/vertex colour/cut-out" + (anyVc ? ", vertex colours kept" : ""));

        var missing = hero.Clips.Where(c => !anims.Any(a => string.Equals(Short(a.Name), c, StringComparison.OrdinalIgnoreCase))).ToList();
        if (missing.Count > 0) log($"  {hero.Id}: clips not found: {string.Join(", ", missing)}");

        float minY = pos.Count > 0 ? pos.Min(p => p.Y) : 0, maxY = pos.Count > 0 ? pos.Max(p => p.Y) : 0;
        return $"{boneNodes.Count} bones, {pos.Count} verts, {submeshes.Count} submeshes, {materials.Count} materials, {anims.Count}/{hero.Clips.Count} clips ({totalTracks} tracks), height {maxY - minY:F3} m";
    }

    static string Short(string name) => (name ?? "").Replace('\\', '/').Split('/').Last();

    static string SaveChannel(Material? mat, string channel, string dir, string file, int max, Action<string> log)
    {
        try
        {
            var img = mat?.FindChannel(channel)?.Texture?.PrimaryImage;
            if (img == null) return "";
            var bytes = img.Content.Content.ToArray();
            using var bmp = SKBitmap.Decode(bytes);
            if (bmp == null) return "";
            SKBitmap outBmp = bmp;
            if (bmp.Width > max || bmp.Height > max)
            {
                float k = (float)max / Math.Max(bmp.Width, bmp.Height);
                outBmp = bmp.Resize(new SKImageInfo((int)(bmp.Width * k), (int)(bmp.Height * k), SKColorType.Rgba8888, SKAlphaType.Unpremul), new SKSamplingOptions(SKCubicResampler.Mitchell));
            }
            File.WriteAllBytes(Path.Combine(dir, file), TextureExtract.ToPngImage(outBmp));
            if (!ReferenceEquals(outBmp, bmp)) outBmp.Dispose();
            return file;
        }
        catch (Exception e)
        {
            log($"  texture {mat?.Name}/{channel}: {e.Message}");
            return "";
        }
    }

    static void WriteV3(BinaryWriter w, Vector3 v) { w.Write(v.X); w.Write(v.Y); w.Write(v.Z); }
    static void WriteQ(BinaryWriter w, Quaternion q) { w.Write(q.X); w.Write(q.Y); w.Write(q.Z); w.Write(q.W); }
}
