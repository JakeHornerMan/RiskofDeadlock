using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadlockOfRain
{
    /// <summary>Animated Deadlock models used as effects (effects props with clips, extractor 1.6.0+: Rat King's rat and his
    /// banner's sewer): the skeleton, one skinned renderer per material with the game's own textures, and its clips on a
    /// legacy Animation, the same way the heroes are built.</summary>
    public static class PropModels
    {
        public sealed class Built
        {
            public GameObject Root;
            public Animation Anim;
            public SkinnedMeshRenderer[] Renderers;
        }

        /// <summary>A new copy of the prop in Deadlock's size (metres), or null when it is not in the cache.</summary>
        public static Built Build(string propId)
        {
            var asset = DeadlockCache.PropAsset(propId);
            if (asset == null) return null;
            var root = new GameObject("DeadlockProp_" + propId);
            if (asset.Bones == null || asset.Bones.Length == 0)
            {
                // a still model (the grenade): plain meshes with the game's own textures
                for (int s = 0; s < asset.Submeshes.Count; s++)
                {
                    var def = asset.Materials[Mathf.Clamp(asset.Submeshes[s].Key, 0, asset.Materials.Length - 1)];
                    var idx = asset.Submeshes[s].Value;
                    var m = new Mesh { name = propId + "_" + s, indexFormat = asset.Positions.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    m.vertices = asset.Positions; m.normals = asset.Normals; m.uv = asset.Uvs;
                    m.SetTriangles(idx, 0, true);
                    var go = new GameObject("mesh_" + def.Name);
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = m;
                    go.AddComponent<MeshRenderer>().sharedMaterial = ModelBuilder.MakeMaterial(asset, def);
                }
                return new Built { Root = root, Renderers = new SkinnedMeshRenderer[0] };
            }
            var armature = new GameObject("Armature").transform;
            armature.SetParent(root.transform, false);
            var bones = new Transform[asset.Bones.Length];
            for (int i = 0; i < bones.Length; i++) bones[i] = new GameObject(asset.Bones[i].Name).transform;
            for (int i = 0; i < bones.Length; i++)
            {
                var b = asset.Bones[i];
                bones[i].SetParent(b.Parent >= 0 ? bones[b.Parent] : armature, false);
                bones[i].localPosition = b.Pos; bones[i].localRotation = b.Rot; bones[i].localScale = b.Scale;
            }
            var parts = asset.Parts;
            var smrs = new List<SkinnedMeshRenderer>();
            for (int s = 0; s < parts.Length; s++)
            {
                var def = asset.Materials[Mathf.Clamp(asset.Submeshes[s].Key, 0, asset.Materials.Length - 1)];
                var go = new GameObject("mesh_" + def.Name);
                go.transform.SetParent(root.transform, false);
                var smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = parts[s]; smr.bones = bones; smr.rootBone = armature;
                smr.sharedMaterial = ModelBuilder.MakeMaterial(asset, def);
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = ShadowCastingMode.On;
                smrs.Add(smr);
            }
            var anim = root.AddComponent<Animation>();
            anim.playAutomatically = false;
            anim.cullingType = AnimationCullingType.BasedOnRenderers;
            foreach (var kv in DeadlockAnimator.BuildClips("prop:" + propId, asset, bones, root.transform)) anim.AddClip(kv.Value, kv.Key);
            return new Built { Root = root, Anim = anim, Renderers = smrs.ToArray() };
        }
    }

    /// <summary>One crowd rat: a plain mesh that swaps between baked frames of the rat's clips (no skeleton, no collider,
    /// not a character: nothing can hit or target it). Its owner (a ring, a swarm, a burst) moves it every frame with
    /// Run (along the ground) or Pose (anywhere: leaping, clinging); the crowd animates it.</summary>
    public sealed class Rat
    {
        public GameObject Go;
        public MeshFilter Filter;
        public MeshRenderer Renderer;
        public int Clip;          // 0 run, 1 leap
        public float Phase, Rate = 1f, Size = 1f, Speed;
        public bool Hidden;
        // the ground under it, looked up every few frames
        public float GroundY;
        public Vector3 GroundNormal = Vector3.up;
        public bool OnGround;
        public int RayFrame;
        public Vector3 Pos, Facing = Vector3.forward;

        /// <summary>Runs on the ground at xz (y is looked up below from refY), facing dir, at speed m/s (the run clip's pace
        /// follows it, so feet don't slide). False (and hidden) where there is no ground or it drops more than maxDrop below refY.</summary>
        public bool Run(Vector3 at, Vector3 dir, float speed, float refY, float maxDrop = 6f)
        {
            if (Time.frameCount >= RayFrame)
            {
                RayFrame = Time.frameCount + 3 + (int)(Random.value * 2f);
                OnGround = Physics.Raycast(new Vector3(at.x, refY + 2.5f, at.z), Vector3.down, out var hit, 2.5f + maxDrop, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
                if (OnGround) { GroundY = hit.point.y; GroundNormal = hit.normal; }
            }
            at.y = OnGround ? GroundY : refY;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f) Facing = dir.normalized;
            Pos = at; Speed = speed; Clip = 0;
            Rate = Mathf.Max(0.3f, speed / RatCrowd.StrideSpeed);
            Show(OnGround);
            if (!Hidden)
            {
                var fwd = Vector3.ProjectOnPlane(Facing, GroundNormal);
                Go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(fwd.sqrMagnitude > 1e-4f ? fwd : Facing, GroundNormal));
            }
            return OnGround;
        }

        /// <summary>Anywhere, any way up (leaping, clinging to an enemy, falling), playing clip at rate.</summary>
        public void Pose(Vector3 at, Quaternion rot, int clip, float rate)
        {
            Pos = at; Clip = clip; Rate = rate;
            Show(true);
            Go.transform.SetPositionAndRotation(at, rot);
        }

        public void Show(bool on)
        {
            Hidden = !on;
            if (Renderer.enabled != on) Renderer.enabled = on;
        }
    }

    /// <summary>The rats of Rat King's kit (Royal Pestments' ring, the rats bursting out of Rule, Ratannia!'s slam, Rat Swarm's
    /// fifty): one pool per stage, made once and reused. Deadlock's rat (effects ratking_rat) is posed once per frame of its
    /// clips and baked into plain meshes, so a rat costs one mesh renderer: 300 at once (four Rat Kings) stay cheap.
    /// Rats off screen don't animate. Every machine moves and draws its own rats; only the server's deal damage.</summary>
    public sealed class RatCrowd : MonoBehaviour
    {
        /// <summary>Ground speed (m/s) at which the run clip plays at its own pace.</summary>
        public const float StrideSpeed = 5f;
        /// <summary>The rats' size against Deadlock's (times the heroes' shared scale): a little bigger, so they read from RoR2's camera.</summary>
        const float RatScale = 1.7f;
        const int FramesPerClip = 12;

        static RatCrowd instance;
        static Mesh[][] frames;
        static float[] lengths = { 0.5f, 0.6f };
        static Material material;
        static bool baked, failed;
        readonly List<Rat> free = new List<Rat>();
        readonly List<Rat> live = new List<Rat>();

        static RatCrowd Instance
        {
            get
            {
                if (instance) return instance;
                instance = new GameObject("DeadlockRatCrowd").AddComponent<RatCrowd>();
                return instance;
            }
        }

        /// <summary>The rat's run (rat_pack_run_01) and leap (rat_release), FramesPerClip frames each, baked once per game.
        /// Without the rat in the cache, a small dark capsule stands in.</summary>
        static void Bake()
        {
            if (baked) return;
            baked = true;
            var row = Sheet.Effect("ratking_rat");
            var clips = row != null ? System.Array.FindAll(row.Clips, c => c != "none") : new string[0];
            var built = PropModels.Build("ratking_rat");
            if (built == null || built.Renderers.Length == 0 || clips.Length == 0)
            {
                failed = true;
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var mesh = Object.Instantiate(cap.GetComponent<MeshFilter>().sharedMesh);
                var v = mesh.vertices;
                for (int i = 0; i < v.Length; i++) v[i] = new Vector3(v[i].x * 0.15f, v[i].y * 0.12f + 0.12f, v[i].z * 0.35f);
                mesh.vertices = v; mesh.RecalculateBounds();
                material = new Material(ModelBuilder.Template) { color = new Color(0.25f, 0.2f, 0.18f) };
                Object.Destroy(cap);
                frames = new[] { new[] { mesh }, new[] { mesh } };
                return;
            }
            var smr = built.Renderers[0];
            material = smr.sharedMaterial;
            // the run and the leap (the third listed clip, rat_release); the second run is spare
            var use = new[] { clips[0], clips[clips.Length - 1] };
            frames = new Mesh[use.Length][];
            for (int c = 0; c < use.Length; c++)
            {
                var st = built.Anim[use[c]];
                var clip = st ? st.clip : null;
                if (clip && clip.length > 0.05f) lengths[c] = clip.length;
                frames[c] = new Mesh[FramesPerClip];
                for (int f = 0; f < FramesPerClip; f++)
                {
                    if (clip) clip.SampleAnimation(built.Root, clip.length * f / FramesPerClip);
                    var m = new Mesh { name = $"rat_{use[c]}_{f}" };
                    smr.BakeMesh(m);
                    m.RecalculateBounds();
                    m.UploadMeshData(true);
                    frames[c][f] = m;
                }
            }
            Object.Destroy(built.Root);
            Log.Info($"rat crowd: baked {use.Length} clips x {FramesPerClip} frames, {frames[0][0].vertexCount} verts each");
        }

        /// <summary>n rats from the pool (made as needed), hidden until their owner places them.</summary>
        public static List<Rat> Take(int n)
        {
            Bake();
            var c = Instance;
            var list = new List<Rat>(n);
            for (int i = 0; i < n; i++)
            {
                Rat r;
                if (c.free.Count > 0) { r = c.free[c.free.Count - 1]; c.free.RemoveAt(c.free.Count - 1); }
                else r = c.Make();
                // no two rats in step: a random start in the clip, a little faster or slower, a little bigger or smaller
                r.Phase = Random.value; r.Size = Random.Range(0.9f, 1.1f);
                r.Go.transform.localScale = Vector3.one * RatScale * ModelBuilder.GlobalScale * r.Size;
                r.RayFrame = 0; r.Show(false);
                r.Go.SetActive(true);
                c.live.Add(r);
                list.Add(r);
            }
            return list;
        }

        /// <summary>Rats back to the pool.</summary>
        public static void Give(IEnumerable<Rat> rats)
        {
            if (!instance) return;
            foreach (var r in rats)
            {
                if (r == null || !r.Go) continue;
                r.Show(false); r.Go.SetActive(false);
                instance.live.Remove(r);
                instance.free.Add(r);
            }
        }

        public static int Live => instance ? instance.live.Count : 0;
        public static bool UsingStandIn => failed;

        Rat Make()
        {
            var go = new GameObject("DeadlockRat");
            go.transform.SetParent(transform, false);
            var r = new Rat { Go = go, Filter = go.AddComponent<MeshFilter>(), Renderer = go.AddComponent<MeshRenderer>() };
            r.Filter.sharedMesh = frames[0][0];
            r.Renderer.sharedMaterial = material;
            r.Renderer.shadowCastingMode = ShadowCastingMode.Off;
            r.Renderer.receiveShadows = true;
            return r;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var r in live)
            {
                if (r.Hidden || !r.Go) continue;
                // ±10 % playback per rat (its size: small rats scurry faster); Phase counts plays of the clip
                r.Phase += dt * r.Rate * (2f - r.Size) / lengths[Mathf.Clamp(r.Clip, 0, lengths.Length - 1)];
                if (!r.Renderer.isVisible) continue;
                var clip = frames[Mathf.Clamp(r.Clip, 0, frames.Length - 1)];
                int f = r.Clip == 1 ? Mathf.Min(clip.Length - 1, (int)(r.Phase * clip.Length)) : (int)(Mathf.Repeat(r.Phase, 1f) * clip.Length) % clip.Length;
                if (r.Filter.sharedMesh != clip[f]) r.Filter.sharedMesh = clip[f];
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
