using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadlockOfRain
{
    /// <summary>One bat of Mina's kit: a plain mesh that flips between baked wing poses (no skeleton, no collider, not a
    /// character: nothing can hit or target it). Its owner (a <see cref="BatSwarm"/>) places it every frame; the flock flaps it.</summary>
    public sealed class Bat
    {
        public GameObject Go;
        public MeshFilter Filter;
        public MeshRenderer Renderer;
        public float Phase, Rate = 1f, Size = 1f;
        public bool Hidden, Angry;

        public void Pose(Vector3 at, Quaternion rot)
        {
            Show(true);
            Go.transform.SetPositionAndRotation(at, rot);
        }

        public void Show(bool on)
        {
            Hidden = !on;
            if (Renderer.enabled != on) Renderer.enabled = on;
        }
    }

    /// <summary>The bats of Mina's kit (Sanguine Retreat's swarm, Nox Nostra's swirl, Mist Form's furious flock, the umbrella's
    /// few): one pool per game, made once and reused. Deadlock's bat (effects mina_bat_prop) flaps with morph targets rather than
    /// bones; extractor 1.9.0 writes each wing pose as its own model, and here they are ordered from wings up to wings down and
    /// blended into FramesPerFlap meshes, so a bat costs one mesh renderer and a flap is a mesh swap. Without the bat in the cache
    /// (an older cache) a simple two-winged shape stands in. Bats off screen don't animate. Every machine draws its own bats.</summary>
    public sealed class BatFlock : MonoBehaviour
    {
        /// <summary>Seconds per wingbeat at Rate 1.</summary>
        const float FlapTime = 0.28f;
        const int FramesPerFlap = 14;
        /// <summary>A bat's wingspan in the world (m) at Size 1: Deadlock's are drawn about this big by its particles.</summary>
        const float Wingspan = 0.42f;
        const string PropId = "mina_bat_prop";

        static BatFlock instance;
        static Mesh[] frames;
        static float meshSpan = 1f;
        static Material dark, angry, lit;
        static bool baked, standIn;
        readonly List<Bat> free = new List<Bat>();
        readonly List<Bat> live = new List<Bat>();

        static BatFlock Instance
        {
            get
            {
                if (instance) return instance;
                instance = new GameObject("DeadlockBatFlock").AddComponent<BatFlock>();
                return instance;
            }
        }

        public static bool UsingStandIn { get { Bake(); return standIn; } }
        public static int Live => instance ? instance.live.Count : 0;

        static void Bake()
        {
            if (baked) return;
            baked = true;
            // the bats are drawn in the game's own material in Mina's colours (Deadlock's bat texture is only a flat swatch):
            // near-black with a faint red glow; Mist Form's furious ones darker red, glowing hard
            dark = BatMaterial("matDeadlockBat", new Color(0.16f, 0.03f, 0.05f), new Color(0.7f, 0.04f, 0.1f), 1.2f);
            angry = BatMaterial("matDeadlockBatAngry", new Color(0.4f, 0.03f, 0.06f), new Color(1f, 0.1f, 0.14f), 3f);
            // Nox Nostra's stream (2026-11-02): dark bats with a stronger red glow, so they still stand out on a dark floor
            lit = BatMaterial("matDeadlockBatLit", new Color(0.2f, 0.03f, 0.05f), new Color(0.95f, 0.08f, 0.12f), 2f);

            var asset = DeadlockCache.PropAsset(PropId);
            var keys = new List<Vector3[]>();
            if (asset != null && asset.Positions != null && asset.Positions.Length > 0)
            {
                keys.Add(asset.Positions);
                for (int k = 0; k < 8; k++)
                {
                    var p = DeadlockCache.PropPose(PropId, k);
                    if (p == null) break;
                    if (p.Length == asset.Positions.Length && Differs(p, asset.Positions)) keys.Add(p);
                }
            }
            if (keys.Count == 0) { BakeStandIn(); return; }
            // wings up first, wings down last (the highest wingtip of each pose), then back up: one wingbeat
            keys.Sort((a, b) => TipHeight(b).CompareTo(TipHeight(a)));
            var cycle = new List<Vector3[]>(keys);
            for (int i = keys.Count - 2; i > 0; i--) cycle.Add(keys[i]);
            var idx = new List<int>();
            foreach (var sm in asset.Submeshes) idx.AddRange(sm.Value);
            var b0 = Bounds(keys[0]);
            meshSpan = Mathf.Max(0.05f, Mathf.Max(Bounds(asset.Positions).size.x, b0.size.x));
            frames = new Mesh[FramesPerFlap];
            for (int f = 0; f < FramesPerFlap; f++)
            {
                // the downstroke takes less time than the upstroke, as a real bat's does
                float t = (float)f / FramesPerFlap;
                float s = t < 0.45f ? t / 0.45f * 0.5f : 0.5f + (t - 0.45f) / 0.55f * 0.5f;
                float x = s * cycle.Count;
                int a = (int)x % cycle.Count, b = (a + 1) % cycle.Count;
                float w = x - Mathf.Floor(x);
                var v = new Vector3[cycle[a].Length];
                for (int i = 0; i < v.Length; i++) v[i] = Vector3.LerpUnclamped(cycle[a][i], cycle[b][i], w * w * (3f - 2f * w));
                var m = new Mesh { name = "bat_" + f };
                m.vertices = v; m.uv = asset.Uvs;
                m.SetTriangles(idx, 0, true);
                m.RecalculateNormals(); m.RecalculateBounds();
                m.UploadMeshData(true);
                frames[f] = m;
            }
            Log.Info($"bat flock: {keys.Count} wing poses, {FramesPerFlap} frames, {asset.Positions.Length} verts, span {meshSpan:F2} m");
        }

        /// <summary>The game's own material, cleaned of Commando's textures: a plain colour, a glow over the whole bat, both sides of
        /// the wings drawn.</summary>
        static Material BatMaterial(string name, Color c, Color glow, float power)
        {
            var m = new Material(ModelBuilder.Template) { name = name };
            foreach (var k in m.shaderKeywords) m.DisableKeyword(k);
            m.mainTexture = Texture2D.whiteTexture;
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_NormalTex")) m.SetTexture("_NormalTex", null);
            if (m.HasProperty("_FresnelMask")) m.SetTexture("_FresnelMask", null);
            if (m.HasProperty("_EmTex")) m.SetTexture("_EmTex", Texture2D.whiteTexture);
            if (m.HasProperty("_EmColor")) m.SetColor("_EmColor", glow);
            if (m.HasProperty("_EmPower")) m.SetFloat("_EmPower", power);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            if (m.HasProperty("_LimbRemovalOn")) m.SetFloat("_LimbRemovalOn", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
            return m;
        }

        static bool Differs(Vector3[] a, Vector3[] b)
        {
            float most = 0f;
            for (int i = 0; i < a.Length; i++) most = Mathf.Max(most, (a[i] - b[i]).sqrMagnitude);
            return most > 0.01f * 0.01f;
        }

        static float TipHeight(Vector3[] v) { float y = float.MinValue; foreach (var p in v) y = Mathf.Max(y, p.y); return y; }

        static Bounds Bounds(Vector3[] v)
        {
            var b = new Bounds(v[0], Vector3.zero);
            foreach (var p in v) b.Encapsulate(p);
            return b;
        }

        /// <summary>No bat in the cache: a small body and two wings that beat.</summary>
        static void BakeStandIn()
        {
            standIn = true;
            meshSpan = 1f;
            frames = new Mesh[FramesPerFlap];
            for (int f = 0; f < FramesPerFlap; f++)
            {
                float ang = Mathf.Sin(f / (float)FramesPerFlap * Mathf.PI * 2f) * 55f;
                var up = Quaternion.AngleAxis(ang, Vector3.forward); var upL = Quaternion.AngleAxis(-ang, Vector3.forward);
                var v = new[]
                {
                    new Vector3(0f, 0f, 0.25f), new Vector3(0f, 0.06f, -0.15f), new Vector3(0f, -0.04f, -0.1f),
                    Vector3.zero, up * new Vector3(0.5f, 0f, 0.05f), up * new Vector3(0.45f, 0f, -0.25f),
                    Vector3.zero, upL * new Vector3(-0.5f, 0f, 0.05f), upL * new Vector3(-0.45f, 0f, -0.25f),
                };
                var m = new Mesh { name = "bat_standin_" + f, vertices = v, triangles = new[] { 0, 1, 2, 0, 2, 1, 3, 4, 5, 3, 5, 4, 6, 7, 8, 6, 8, 7 } };
                m.RecalculateNormals(); m.RecalculateBounds();
                frames[f] = m;
            }
        }

        /// <summary>n bats from the pool (made as needed), hidden until their owner places them; glowing: the dark bat with a stronger
        /// red glow (Nox Nostra's stream).</summary>
        public static List<Bat> Take(int n, bool furious = false, float size = 1f, bool glowing = false)
        {
            Bake();
            var c = Instance;
            var list = new List<Bat>(n);
            for (int i = 0; i < n; i++)
            {
                Bat b;
                if (c.free.Count > 0) { b = c.free[c.free.Count - 1]; c.free.RemoveAt(c.free.Count - 1); }
                else b = c.Make();
                // no two bats in step: a random start in the wingbeat, a little faster or slower, a little bigger or smaller
                b.Phase = Random.value; b.Size = size * Random.Range(0.8f, 1.15f); b.Rate = Random.Range(0.85f, 1.2f) * (furious ? 1.6f : 1f);
                b.Angry = furious;
                b.Renderer.sharedMaterial = furious ? angry : glowing ? lit : dark;
                b.Go.transform.localScale = Vector3.one * Wingspan / meshSpan * b.Size;
                b.Show(false);
                b.Go.SetActive(true);
                c.live.Add(b);
                list.Add(b);
            }
            return list;
        }

        /// <summary>Bats back to the pool.</summary>
        public static void Give(IEnumerable<Bat> bats)
        {
            if (!instance) return;
            foreach (var b in bats)
            {
                if (b == null || !b.Go) continue;
                b.Show(false); b.Go.SetActive(false);
                instance.live.Remove(b);
                instance.free.Add(b);
            }
        }

        Bat Make()
        {
            var go = new GameObject("DeadlockBat");
            go.transform.SetParent(transform, false);
            var b = new Bat { Go = go, Filter = go.AddComponent<MeshFilter>(), Renderer = go.AddComponent<MeshRenderer>() };
            b.Filter.sharedMesh = frames[0];
            b.Renderer.shadowCastingMode = ShadowCastingMode.Off;
            b.Renderer.receiveShadows = false;
            return b;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var b in live)
            {
                if (b.Hidden || !b.Go) continue;
                b.Phase += dt * b.Rate / FlapTime;
                if (!b.Renderer.isVisible) continue;
                int f = (int)(Mathf.Repeat(b.Phase, 1f) * frames.Length) % frames.Length;
                if (b.Filter.sharedMesh != frames[f]) b.Filter.sharedMesh = frames[f];
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }

    /// <summary>A flock of bats around a point or a body: each bat circles on its own orbit (its own radius, height, speed and
    /// wobble) and chases its place on it, so a moving centre leaves the flock streaming behind it. Modes on top: Stream (fly from
    /// here to a point), Swirl (a wide, low circle over the ground), Burst (scatter outwards and go), Lunge (a few dart at given
    /// targets and snap back). Purely visual, on every machine; Stop scatters the bats and hands them back to the pool.</summary>
    public sealed class BatSwarm : MonoBehaviour
    {
        sealed class Slot
        {
            public Bat Bat;
            public float Angle, Spin, Radius, Height, Bob, BobSpeed, Lag;
            public Vector3 Pos, Prev, Burst;
            public bool Out;
            public Transform Lunge; public float LungeUntil;
        }

        readonly List<Slot> slots = new List<Slot>();
        /// <summary>The body or point the flock circles; Center when Follow is null.</summary>
        public Transform Follow;
        public Vector3 Center;
        public Vector3 Offset;
        /// <summary>The orbit's size (m), its vertical spread, how fast the bats go round (turns/s) and how tightly they keep
        /// their places (1/s: high = a tight churning flock, low = a loose drifting one).</summary>
        public float Radius = 1f, Spread = 0.6f, Turns = 0.6f, Tightness = 6f;
        /// <summary>Seconds after which the flock scatters by itself (negative: when its owner says).</summary>
        public float AutoStop = -1f;
        float stopAt = -1f, age;

        public static BatSwarm Make(int count, Vector3 at, Transform follow, float radius, bool furious = false, float size = 1f)
        {
            var go = new GameObject("DlrBatSwarm");
            go.transform.position = at;
            var s = go.AddComponent<BatSwarm>();
            s.Follow = follow; s.Center = at; s.Radius = radius;
            foreach (var b in BatFlock.Take(count, furious, size))
            {
                var sl = new Slot
                {
                    Bat = b, Angle = Random.value * 360f, Spin = Random.Range(0.6f, 1.4f) * (Random.value < 0.5f ? -1f : 1f),
                    Radius = Random.Range(0.35f, 1f), Height = Random.Range(-1f, 1f), Bob = Random.value * 6f,
                    BobSpeed = Random.Range(2f, 5f), Lag = Random.Range(0.6f, 1.4f),
                };
                sl.Pos = at + Random.insideUnitSphere * radius * 0.3f;
                s.slots.Add(sl);
            }
            return s;
        }

        Vector3 Here => (Follow ? Follow.position : Center) + Offset;

        /// <summary>Every bat flies out along its own direction at speed for life seconds, then the swarm goes.</summary>
        public void Burst(float speed, float life)
        {
            foreach (var sl in slots)
            {
                var d = sl.Pos - Here; d.y *= 0.4f;
                if (d.sqrMagnitude < 0.01f) d = Random.onUnitSphere;
                sl.Burst = (d.normalized + Random.insideUnitSphere * 0.35f + Vector3.up * 0.15f).normalized * speed * Random.Range(0.7f, 1.2f);
                sl.Out = true;
            }
            stopAt = age + life;
        }

        /// <summary>The flock leaves: a soft scatter, then the bats go back to the pool.</summary>
        public void Stop(float scatter = 6f) { if (stopAt < 0f) Burst(scatter, 0.6f); }

        /// <summary>A few bats dart at target for a moment, then return to the flock (Mist Form's lunges).</summary>
        public void Lunge(Transform target, int bats, float time)
        {
            if (!target) return;
            int n = 0;
            foreach (var sl in slots)
            {
                if (sl.Out || sl.Lunge || Random.value < 0.4f) continue;
                sl.Lunge = target; sl.LungeUntil = age + time * Random.Range(0.7f, 1.1f);
                if (++n >= bats) break;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (AutoStop > 0f && age >= AutoStop) { AutoStop = -1f; Stop(); }
            var c = Here;
            foreach (var sl in slots)
            {
                if (sl.Bat == null || !sl.Bat.Go) continue;
                Vector3 want;
                if (sl.Out) { sl.Pos += sl.Burst * dt; sl.Burst *= Mathf.Exp(-1.2f * dt); want = sl.Pos; }
                else if (sl.Lunge && age < sl.LungeUntil)
                {
                    var hb = sl.Lunge.position + Random.insideUnitSphere * 0.3f;
                    want = hb;
                    sl.Pos = Vector3.Lerp(sl.Pos, want, 1f - Mathf.Exp(-14f * dt));
                }
                else
                {
                    sl.Lunge = null;
                    sl.Angle += sl.Spin * Turns * 360f * dt;
                    sl.Bob += sl.BobSpeed * dt;
                    var ring = Quaternion.Euler(0f, sl.Angle, 0f) * Vector3.forward * Radius * sl.Radius;
                    want = c + ring + Vector3.up * (sl.Height * Spread + Mathf.Sin(sl.Bob) * Spread * 0.35f);
                    sl.Pos = Vector3.Lerp(sl.Pos, want, 1f - Mathf.Exp(-Tightness * sl.Lag * dt));
                }
                var v = (sl.Pos - sl.Prev) / Mathf.Max(dt, 1e-4f);
                var face = v.sqrMagnitude > 0.04f ? v.normalized : (want - sl.Pos).sqrMagnitude > 1e-4f ? (want - sl.Pos).normalized : sl.Bat.Go.transform.forward;
                // banked into the turn a little, wings level otherwise
                var rot = Quaternion.LookRotation(face, Vector3.up + Vector3.Cross(face, Vector3.up) * 0.3f * Mathf.Sign(sl.Spin));
                sl.Bat.Pose(sl.Pos, rot);
                sl.Prev = sl.Pos;
            }
            if (stopAt >= 0f && age >= stopAt) Destroy(gameObject);
        }

        void OnDestroy()
        {
            var bats = new List<Bat>();
            foreach (var sl in slots) if (sl.Bat != null) bats.Add(sl.Bat);
            BatFlock.Give(bats);
            slots.Clear();
        }
    }
}
