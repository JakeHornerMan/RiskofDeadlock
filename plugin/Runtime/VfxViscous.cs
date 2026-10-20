using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadlockOfRain
{
    // Viscous' goo (effects rows viscous_*), in the creator's Ghibli cel look (v0.8.0b, from their MagicCastingTopDown GooMesh shader
    // and its ghibli-goo-material palette, the No-Face sludge as reference): a flat base tone that snaps to a flat sheen band where
    // the light hits, a bright rim band at the silhouette, a dark deep tone underneath, a slow ooze and pulse, and heavy drips that
    // hang, stretch and fall. RoR2 can't load that Unity 6 shader, so CelGoo does the same banding per vertex every frame into vertex
    // colours drawn by one unlit alpha-blended material. Shells (the Cube, Goo Ball) are clear in the middle and solid at the rim, so
    // whoever is inside shows. The cube and fist are Deadlock's own models (effects kind prop); Goo Ball uses a smooth sphere.
    public static partial class Vfx
    {
        // Viscous' shakes are a tenth of the other heroes' (creator, 2026-10-20: "like 10% of what it is")
        // the palette (ghibli-goo-material from #4FB83A, the base lifted a little so it reads on RoR2's bright stages)
        static readonly Color gooDeep = new Color32(0x0C, 0x21, 0x08, 255), gooBase = new Color32(0x2A, 0x6E, 0x1E, 255),
            gooSheen = new Color32(0x62, 0xD8, 0x7F, 255), gooRim = new Color32(0x73, 0xFF, 0x96, 255);

        static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
        /// <summary>The sheen band's tone: the palette's sheen pulled toward the base, so it stays a saturated green on RoR2's bright screen.</summary>
        static readonly Color sheenDeep = Color.Lerp(new Color32(0x62, 0xD8, 0x7F, 255), new Color32(0x2A, 0x6E, 0x1E, 255), 0.3f);

        static Material celMat;
        /// <summary>The one material every CelGoo mesh uses: unlit, alpha-blended, coloured by its vertex colours.</summary>
        static Material CelMat()
        {
            if (!celMat) celMat = new Material(Shader.Find("Sprites/Default")) { name = "matDlrCelGoo", color = Color.white };
            return celMat;
        }

        static Texture2D dropTex;
        /// <summary>A goo droplet for particles: a round flat tone with a painted highlight dot and a darker lower edge.</summary>
        static Material DropMat()
        {
            string key = "gooDrop";
            if (tinted.TryGetValue(key, out var m) && m) return m;
            if (!dropTex)
            {
                const int n = 64;
                dropTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "texDlrGooDrop" };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f, r = Mathf.Sqrt(u * u + v * v);
                        float a = Mathf.Clamp01((1f - r) * 18f);
                        float shade = v < -0.45f + 0.3f * u * u ? 0.6f : 1f;
                        float hi = Mathf.Sqrt((u + 0.32f) * (u + 0.32f) + (v - 0.35f) * (v - 0.35f)) < 0.2f ? 1.6f : 1f;
                        float c = Mathf.Min(1f, shade * hi * 0.8f);
                        dropTex.SetPixel(x, y, new Color(c, c, c, a));
                    }
                dropTex.Apply();
            }
            m = new Material(Shader.Find("Sprites/Default")) { name = "matDlrGooDrop", mainTexture = dropTex, color = Color.white };
            tinted[key] = m;
            return m;
        }

        /// <summary>Goo droplet particles (Particles with the droplet material; colour from the particle).</summary>
        static ParticleSystem Drops(GameObject parent, Color c, float rate = 0f, int count = 0, float life = 0.6f, float speed = 1f, float size = 0.2f,
            float shapeRadius = 0f, bool upward = false, float gravity = 1.5f)
        {
            var ps = Particles(parent, "mat_glow", c, rate: rate, count: count, life: life, speed: speed, size: size, shapeRadius: shapeRadius, world: true, upward: upward, gravity: gravity);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = DropMat();
            return ps;
        }

        static void DropBurst(Vector3 at, Color c, int count, float life, float speed, float size, float shapeRadius = 0.2f, float gravity = 1.6f)
        {
            var root = Root(at, life + 0.3f, null);
            Drops(root, c, count: count, life: life, speed: speed, size: size, shapeRadius: shapeRadius, upward: true, gravity: gravity);
        }

        static Mesh sphereMesh;
        static Mesh SphereMesh()
        {
            if (sphereMesh) return sphereMesh;
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = g.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(g);
            return sphereMesh;
        }

        /// <summary>A CelGoo mesh size metres across at its widest, centred on parent. shell = see-through (clear middle, solid rim).</summary>
        static CelGoo Goo(Transform parent, Mesh mesh, float size, bool shell = false, float ooze = 0.05f)
        {
            if (!mesh) mesh = SphereMesh();
            var go = new GameObject("goo");
            go.transform.SetParent(parent, false);
            var b = mesh.bounds;
            // size 0: the mesh as it is (already in metres around the parent, like a draped puddle)
            float k = size <= 0f ? 1f : size / Mathf.Max(0.01f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            var inner = new GameObject("mesh");
            inner.transform.SetParent(go.transform, false);
            inner.transform.localScale = Vector3.one * k;
            inner.transform.localPosition = size <= 0f ? Vector3.zero : -b.center * k;
            var mr = inner.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CelMat();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var cel = inner.AddComponent<CelGoo>();
            cel.Source = mesh; cel.Shell = shell; cel.Ooze = ooze;
            return cel;
        }

        /// <summary>A hero part made of goo (heroes.gooMaterials, Viscous' head): mesh already in bone's space, drawn there as thick
        /// see-through cel goo with a gentle ooze and pulse, so it wobbles on him like Deadlock's.</summary>
        public static bool GooPart(Transform bone, Mesh mesh, bool glass = false)
        {
            if (!bone || !mesh) return false;
            // the mesh stays for the game: the body prefab and every copy of it share it. Glass is a still shell (clear middle,
            // glossy rim); the goo inside it wobbles
            var cel = Goo(bone, mesh, 0f, glass, glass ? 0f : 0.012f);
            if (glass) { cel.Pulse = 0f; cel.Glass = true; }
            else { cel.Gel = true; cel.Pulse = 0.015f; cel.Bright = true; }
            return true;
        }

        /// <summary>Dev only (the kit-page studio, DpsBench): the goo shades itself for a camera here instead of the game's.</summary>
        internal static Vector3? StudioView;

        static CelGoo GooBall(Transform parent, float size, bool shell = false, float ooze = 0.05f) => Goo(parent, SphereMesh(), size, shell, ooze);

        /// <summary>A disc of radius metres laid over the ground around centre (a ray down for every vertex), in metres around centre:
        /// a puddle that follows slopes, steps and rocks instead of floating flat over them. Enemies don't hold it up (world only).</summary>
        static Mesh DrapeMesh(Vector3 centre, float radius, int rings = 10, int segs = 40)
        {
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            Vector3 Ground(Vector3 at, out Vector3 n)
            {
                var from = new Vector3(at.x, centre.y + Mathf.Max(3f, radius * 0.6f), at.z);
                if (Physics.Raycast(from, Vector3.down, out var g, Mathf.Max(3f, radius * 0.6f) * 2.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                {
                    // the top of a boulder, a wall or a ledge well above or below the landing isn't the floor it lies on: a downward
                    // normal there, which hides that part of the disc (CelGoo Flat and FloorDisc show only faces that look up)
                    n = Mathf.Abs(g.point.y - centre.y) > Mathf.Max(1.5f, radius * 0.25f) || g.normal.y < 0.45f ? Vector3.down : g.normal;
                    return g.point + g.normal * 0.05f - centre;
                }
                n = Vector3.up; return new Vector3(at.x, centre.y + 0.05f, at.z) - centre;
            }
            verts.Add(Ground(centre, out var n0)); norms.Add(n0);
            for (int r = 1; r <= rings; r++)
                for (int a = 0; a < segs; a++)
                {
                    float ang = a * Mathf.PI * 2f / segs, d = radius * r / rings;
                    verts.Add(Ground(centre + new Vector3(Mathf.Cos(ang) * d, 0f, Mathf.Sin(ang) * d), out var n)); norms.Add(n);
                }
            for (int a = 0; a < segs; a++) { tris.Add(0); tris.Add(1 + (a + 1) % segs); tris.Add(1 + a); }
            for (int r = 1; r < rings; r++)
                for (int a = 0; a < segs; a++)
                {
                    int i0 = 1 + (r - 1) * segs + a, i1 = 1 + (r - 1) * segs + (a + 1) % segs, o0 = i0 + segs, o1 = i1 + segs;
                    tris.Add(i0); tris.Add(i1); tris.Add(o0);
                    tris.Add(i1); tris.Add(o1); tris.Add(o0);
                }
            var m = new Mesh { name = "mshDlrGooDrape" };
            m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0); m.RecalculateBounds();
            return m;
        }

        static readonly Queue<GameObject> stains = new Queue<GameObject>();
        const int MaxStains = 30;

        /// <summary>A glossy goo splat where a hit landed, flat against the surface (normal), stuck to what it hit (follow), fading over
        /// life seconds. At most MaxStains at once: the oldest goes first.</summary>
        static void Stain(Vector3 at, Vector3 normal, float size, Transform follow = null, float life = 2f)
        {
            while (stains.Count >= MaxStains) { var old = stains.Dequeue(); if (old) Object.Destroy(old); }
            var g = new GameObject("DlrGooStain");
            g.transform.position = at;
            g.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.up);
            var cel = GooBall(g.transform, size, false, 0.04f);
            cel.transform.parent.localScale = new Vector3(1f, 0.16f, 1f);
            if (follow) g.transform.SetParent(follow, true);
            var f = g.AddComponent<StainFade>(); f.Life = life;
            stains.Enqueue(g);
        }

        static GameObject Dome(Transform parent, float radius, Color c)
        {
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(dome.GetComponent<Collider>());
            dome.transform.SetParent(parent, false);
            dome.transform.localScale = Vector3.one * radius * 2f;
            dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", c);
            return dome;
        }

        /// <summary>Real blobs of goo thrown out of a splash: little glossy spheres that arc, fall and shrink away.</summary>
        static void Globs(Vector3 at, int count, float speed, float size)
        {
            var root = Root(at, 2f, null);
            for (int i = 0; i < count; i++)
            {
                var holder = new GameObject("glob");
                holder.transform.SetParent(root.transform, false);
                GooBall(holder.transform, size * Random.Range(0.6f, 1.2f), false, 0.08f);
                var d = holder.AddComponent<Glob>();
                var dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * 1.4f + 0.3f;
                d.Velocity = dir.normalized * speed * Random.Range(0.5f, 1f);
            }
        }

        /// <summary>A big wet splash: globs and droplets flung up and out, a dark wet ring of drops along the ground, a soft light.</summary>
        static void Splash(Vector3 at, float radius, int globs)
        {
            Globs(at + Vector3.up * 0.3f, Mathf.Min(globs, 14), Mathf.Clamp(radius * 1.2f, 4f, 14f), Mathf.Clamp(radius * 0.08f, 0.25f, 0.8f));
            DropBurst(at, gooBase, globs, 0.9f, radius * 1.2f, Mathf.Clamp(radius * 0.06f, 0.2f, 0.6f), radius * 0.2f);
            DropBurst(at, gooSheen, globs / 2, 0.7f, radius * 0.9f, 0.18f, radius * 0.2f);
            Flash(at + Vector3.up, gooSheen, radius * 1.5f, 0.25f, 2.5f);
        }

        // ------------------------------------------------------------------ M1: Deep Sea Interpreter
        static GameObject ViscousGoo(string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the blob in flight (DlrGoo turns the root along its path): a glossy cel blob, stretched and wobbling, dripping
                    var root = Root(at, 0f, follow);
                    var blob = GooBall(root.transform, Mathf.Max(0.25f, radius * 1.3f), false, 0.1f);
                    blob.transform.parent.gameObject.AddComponent<Wobble>().Base = new Vector3(0.85f, 0.85f, 1.5f);
                    Drops(root, gooBase, rate: 14, life: 0.4f, speed: 0.1f, size: 0.12f, gravity: 1.2f);
                    return root;
                }
                case "hit":
                {
                    var normal = to.HasValue ? to.Value - at : Vector3.up;
                    DropBurst(at, gooBase, 7, 0.45f, 3f, 0.14f, 0.1f);
                    Stain(at, normal, 0.6f, follow);
                    return null;
                }
                case "muzzle":
                    DropBurst(at, gooSheen, 3, 0.15f, 2f, 0.1f, 0.05f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ M2: Splatter
        static GameObject ViscousSplatter(string moment, Vector3 at, float radius, Transform follow, float life)
        {
            switch (moment)
            {
                case "aim":
                {
                    // where it lands, the size of the splash it would make now (smaller with fewer rounds): a bright green disc laid
                    // over the floor itself (it never paints enemies or sits on top of them), a glowing edge, and the throw's path
                    var root = Root(at, 0f, null);
                    root.AddComponent<FloorDisc>().Radius = radius;
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.3f;
                    Particles(edge, "mat_glow", gooRim, rate: 90, life: 0.45f, speed: 0.8f, size: 0.55f, shapeRadius: radius, world: true, ring: true);
                    var arc = Line(root.transform, gooRim, 0.12f, Vector3.zero, Vector3.zero);
                    arc.name = "arc"; arc.useWorldSpace = true; arc.sharedMaterial = Solid(gooRim);
                    arc.widthCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.15f, 1f), new Keyframe(1, 1f));
                    return root;
                }
                case "loop":
                {
                    // the goo ball in flight: a glossy wobbling ball, hanging drips
                    var root = Root(at, 0f, follow);
                    var ball = GooBall(root.transform, 1f, false, 0.1f);
                    ball.transform.parent.gameObject.AddComponent<Wobble>().Base = Vector3.one;
                    root.AddComponent<Dripper>().Radius = 0.4f;
                    return root;
                }
                case "hit":
                    Splash(at, radius, 18);
                    return null;
                case "puddle":
                {
                    // the puddle for life seconds: a thick glossy pool of goo with an oozing edge, blobs rising and popping on it,
                    // its reach outlined on the terrain; it sinks away over its last second
                    // the pool is draped over the ground itself (slopes, steps, rocks), so it always lies on the floor
                    var root = Root(at, life + 1.5f, null);
                    var pool = new GameObject("pool"); pool.transform.SetParent(root.transform, false);
                    var drape = DrapeMesh(at, radius);
                    var cel = Goo(pool.transform, drape, 0f, false, 0.03f);
                    cel.Pulse = 0.01f; cel.Flat = true;
                    root.AddComponent<DestroyMesh>().Mesh = drape;
                    var bubbles = root.AddComponent<Bubbler>(); bubbles.Radius = radius * 0.8f; bubbles.Rate = Mathf.Max(2f, radius * 0.6f);
                    var sink = root.AddComponent<Sink>(); sink.After = Mathf.Max(0.5f, life - 1f); sink.Target = pool.transform;
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Shift: The Cube
        static GameObject ViscousCube(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // Deadlock's cube as a solid block of goo with Viscous stuck inside (a dim frozen shape through thick green),
                    // jiggling, oozing, bubbles rising through it, dripping
                    var root = Root(at, 0f, follow);
                    var feet = new GameObject("feet"); feet.transform.SetParent(root.transform, false);
                    if (follow) feet.transform.localPosition = at - follow.position;
                    var cube = Goo(feet.transform, DeadlockCache.Prop("viscous_cube_prop") ?? CubeMesh(), radius, false, 0.03f);
                    cube.Gel = true;
                    cube.transform.parent.localPosition = Vector3.up * radius * 0.45f;
                    cube.transform.parent.gameObject.AddComponent<Jiggle>();
                    var inside = new GameObject("bubbles"); inside.transform.SetParent(feet.transform, false);
                    Drops(inside, gooSheen, rate: 10f, life: 1.6f, speed: radius * 0.5f, size: 0.16f, shapeRadius: radius * 0.35f, upward: true, gravity: -0.05f);
                    var drip = feet.AddComponent<Dripper>(); drip.Radius = radius * 0.45f; drip.Height = radius * 0.2f; drip.Rate = 4f;
                    Light(feet, gooSheen, radius * 2.5f, 1.2f, Vector3.up * radius * 0.5f);
                    return root;
                }
                case "tether":
                {
                    var root = Root(at, 0f, null);
                    root.AddComponent<TetherSet>();
                    return root;
                }
                case "hit":
                    // the cube bursts: goo flung all the way out to the burst's reach
                    Splash(at + Vector3.up * 0.8f, radius, 30);
                    Shake(at, radius * 2f, 0.2f, 0.3f);
                    Stain(at, Vector3.up, Mathf.Min(radius * 0.35f, 5f), null, 3f);
                    return null;
            }
            return null;
        }

        /// <summary>A goo fist size metres tall above its wrist (origin at the wrist, knuckles up, +z toward the viewer): a forearm going
        /// down into the ground, the clenched hand, four knuckles on top, curled fingers on the front and the thumb across them.</summary>
        static void Fist(Transform parent, float size)
        {
            void Piece(Vector3 pos, Vector3 scale, float ooze = 0.04f, bool wobble = false)
            {
                var g = new GameObject("piece"); g.transform.SetParent(parent, false);
                g.transform.localPosition = pos * size;
                GooBall(g.transform, 1f, false, ooze);
                g.transform.localScale = scale * size;
                if (wobble) g.AddComponent<Wobble>().Base = scale * size;
            }
            Piece(new Vector3(0f, -0.75f, 0f), new Vector3(0.42f, 1.7f, 0.42f));               // forearm, into the ground
            Piece(new Vector3(0f, 0.42f, 0f), new Vector3(0.82f, 0.72f, 0.66f), 0.05f, true);  // the clenched hand
            for (int i = 0; i < 4; i++)
            {
                float x = -0.3f + i * 0.2f;
                Piece(new Vector3(x, 0.82f - Mathf.Abs(x) * 0.25f, 0.08f), Vector3.one * 0.3f, 0.03f);        // knuckles
                Piece(new Vector3(x, 0.55f - Mathf.Abs(x) * 0.2f, 0.33f), new Vector3(0.19f, 0.34f, 0.24f), 0.03f); // curled fingers
            }
            Piece(new Vector3(0.38f, 0.38f, 0.3f), new Vector3(0.22f, 0.22f, 0.5f), 0.03f);    // thumb across the fingers
        }

        static Mesh cubeMesh;
        static Mesh CubeMesh()
        {
            if (cubeMesh) return cubeMesh;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = g.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(g);
            return cubeMesh;
        }

        /// <summary>Points the cube's tethers (moment "tether") from the hero to the enemies it drains: sagging goo strands pulsing toward him.</summary>
        public static void SetTethers(GameObject tethers, Vector3 from, List<Vector3> to)
        {
            if (!tethers) return;
            var t = tethers.GetComponent<TetherSet>();
            if (t) t.Set(from, to);
        }

        // ------------------------------------------------------------------ R: Puddle Punch
        static GameObject ViscousPunch(string moment, Vector3 at, float radius)
        {
            switch (moment)
            {
                case "aim":
                {
                    // the placement ring (owner only; the state moves it and scales it up over a puddle)
                    var root = Root(at, 0f, null);
                    Dome(root.transform, radius, gooRim);
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.3f;
                    Particles(edge, "mat_glow", gooRim, rate: 70, life: 0.4f, speed: 0.6f, size: 0.45f, shapeRadius: radius, world: true, ring: true);
                    var beam = Line(root.transform, gooRim, 0.25f, Vector3.up * 0.2f, Vector3.up * 6f);
                    beam.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
                    return root;
                }
                case "telegraph":
                {
                    // the ground bubbles where the fist will come up
                    var root = Root(at, 0f, null);
                    Dome(root.transform, radius, gooBase);
                    var bubbles = root.AddComponent<Bubbler>(); bubbles.Radius = radius * 0.7f; bubbles.Rate = radius * 8f;
                    return root;
                }
                case "hit":
                {
                    // a giant goo fist on its forearm punches up out of the ground (knuckles up, facing the camera), overshoots,
                    // squeezes a moment and sinks back; goo splashes everywhere. Built from cel goo pieces: Deadlock's fist model is a
                    // small lump that doesn't read as a fist from RoR2's camera (creator, 2026-10-20: "an actual fist").
                    var root = Root(at, 1.7f, null);
                    var cam = Camera.main;
                    if (cam) { var f = cam.transform.position - at; f.y = 0f; if (f.sqrMagnitude > 0.01f) root.transform.rotation = Quaternion.LookRotation(f.normalized); }
                    float size = Mathf.Max(2.5f, radius * 0.9f);
                    var rise = new GameObject("rise"); rise.transform.SetParent(root.transform, false);
                    Fist(rise.transform, size);
                    rise.AddComponent<Riser>().Size = size;
                    Splash(at, radius, 22);
                    Shake(at, radius * 3f, 0.25f, 0.3f);
                    Stain(at, Vector3.up, radius * 0.6f, null, 3f);
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Q: Goo Ball
        static GameObject ViscousBall(string moment, Vector3 at, float radius)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the ball (radius = the ball's): a see-through goo shell (clear middle, glossy rim and sheen band) so the carrier
                    // shows inside walking; "spin" is turned by the state as it rolls; goo hangs and drips off it
                    var root = Root(at, 0f, null);
                    var spin = new GameObject("spin"); spin.transform.SetParent(root.transform, false);
                    GooBall(spin.transform, radius * 2f, true, 0.035f).Pulse = 0.02f;
                    var drip = root.AddComponent<Dripper>(); drip.Radius = radius * 0.8f; drip.Height = -radius * 0.6f; drip.Rate = 5f;
                    Light(root, gooSheen, radius * 2.5f, 1f, Vector3.zero);
                    return root;
                }
                case "cast":
                    Splash(at, radius * 1.5f, 16);
                    return null;
                case "hit":
                    Globs(at, 5, 7f, 0.35f);
                    DropBurst(at, gooBase, 12, 0.6f, 7f, 0.22f, 0.3f);
                    return null;
                case "slam":
                    Splash(at, radius, 26);
                    Shake(at, radius * 3f, 0.3f, 0.35f);
                    Stain(at, Vector3.up, radius * 0.5f, null, 3f);
                    return null;
                case "bounce":
                    DropBurst(at, gooBase, 6, 0.5f, 4f, 0.2f, radius * 0.4f);
                    Stain(at, Vector3.up, radius * 0.7f);
                    return null;
                case "end":
                    // the ball pops
                    Globs(at, 12, radius * 4f, 0.45f);
                    DropBurst(at, gooBase, 24, 0.8f, radius * 3f, 0.3f, radius * 0.6f);
                    Flash(at, gooSheen, radius * 3f, 0.3f, 2.5f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Gooed (the passive's debuff on enemies)
        static GameObject ViscousGooed(string moment, Transform follow)
        {
            var body = follow ? follow.GetComponent<CharacterBody>() : null;
            if (moment == "hit" && body && !body.GetComponent<GooedFx>()) body.gameObject.AddComponent<GooedFx>();
            return null;
        }

        /// <summary>A Gooed enemy while it has the stacks (read from the synced buff, so every player sees it): a few green drops falling
        /// off it, more with each stack. (Its stacks show as the buff icon over its health bar.)</summary>
        sealed class GooedFx : MonoBehaviour
        {
            CharacterBody body; GameObject root; ParticleSystem drips; float idle; string buff;

            void Start()
            {
                body = GetComponent<CharacterBody>();
                foreach (var r in BuffRow.All) if (r.Vfx == "viscous_gooed") { buff = r.Id; break; }
                if (!body || buff == null) { Destroy(this); return; }
                float r0 = Mathf.Max(0.5f, body.radius);
                root = Root(body.corePosition, 0f, body.coreTransform ? body.coreTransform : transform);
                drips = Drops(root, gooSheen, rate: 0f, life: 0.7f, speed: 0.2f, size: 0.12f, shapeRadius: r0 * 0.5f, gravity: 1.6f);
            }

            void Update()
            {
                if (!body || !body.healthComponent || !body.healthComponent.alive) { Done(); return; }
                int stacks = Buffs.Count(body, buff);
                var e = drips.emission; e.rateOverTime = stacks * 3f;
                if (stacks <= 0) { idle += Time.deltaTime; if (idle > 0.5f) Done(); } else idle = 0f;
            }

            void Done() => Destroy(this);
            void OnDestroy() { if (root) Stop(root); }
        }

        // ------------------------------------------------------------------ the cel goo

        /// <summary>The Ghibli cel shading, per vertex, every frame: a light from the camera's upper left; where half-Lambert passes the
        /// threshold the surface takes the flat sheen tone, else the flat base tone (the deep tone on its underside); a hard rim band at the
        /// silhouette. A slow ooze and pulse move the vertices along their normals. Back faces are hidden for solid goo; a shell is
        /// clear in the middle (alphaShell) and solid at the rim, its far side faint, so what is inside shows.</summary>
        sealed class CelGoo : MonoBehaviour
        {
            public Mesh Source; public bool Shell, Flat, Gel, Glass, Bright; public float Ooze = 0.05f, Pulse = 0.04f;
            // the light sits mostly above, so the sheen is a crescent on top (not the whole lit half) with a painted glint in it
            const float Threshold = 0.86f, RimBand = 0.7f, Glint = 0.985f, ShellAlpha = 0.16f;
            Mesh mesh; Vector3[] v0, n0, verts; Color32[] cols; float seed, maxR = 1f, maxH = 1f;

            void Start()
            {
                seed = Random.value * 20f;
                if (!Source) { enabled = false; return; }
                mesh = Instantiate(Source);
                mesh.MarkDynamic();
                v0 = Source.vertices; n0 = Source.normals;
                if (n0 == null || n0.Length != v0.Length) { mesh.RecalculateNormals(); n0 = mesh.normals; }
                verts = new Vector3[v0.Length]; cols = new Color32[v0.Length];
                maxR = 0.001f; foreach (var p in v0) maxR = Mathf.Max(maxR, new Vector2(p.x, p.z).magnitude);
                var lb = Source.bounds; maxH = Mathf.Max(0.01f, lb.size.y * Mathf.Abs(transform.lossyScale.y));
                var mf = gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                LateUpdate();
            }

            void LateUpdate()
            {
                if (mesh == null) return;
                var cam = Camera.main;
                var camT = cam ? cam.transform : null;
                var camPos = StudioView ?? (camT ? camT.position : transform.position + Vector3.back * 10f);
                var fwd = StudioView.HasValue ? (transform.position - camPos).normalized : camT ? camT.forward : Vector3.zero;
                var rgt = StudioView.HasValue ? Vector3.Cross(Vector3.up, fwd).normalized : camT ? camT.right : Vector3.zero;
                var light = (-rgt * 0.35f - fwd * 0.25f + Vector3.up).normalized;
                float t = Time.time;
                var tr = transform;
                float pulse = 1f + Pulse * Mathf.Sin(t * 1.2f * Mathf.PI * 2f * 0.5f + seed);
                var scale = tr.lossyScale; float avg = Mathf.Max(0.001f, (Mathf.Abs(scale.x) + Mathf.Abs(scale.y) + Mathf.Abs(scale.z)) / 3f);
                for (int i = 0; i < v0.Length; i++)
                {
                    var p = v0[i]; var n = n0[i];
                    // ooze: a slow travelling wave along the normal, in object metres
                    float o = Mathf.Sin(p.x * 2.5f * avg + t * 0.8f * 3f + seed) + Mathf.Sin(p.y * 2.1f * avg - t * 0.8f * 2.2f) + Mathf.Sin(p.z * 2.9f * avg + t * 0.8f * 2.6f + seed * 0.5f);
                    verts[i] = p * pulse + n * (o * Ooze / 3f / avg);
                    var wn = tr.TransformDirection(n).normalized;
                    var wp = tr.TransformPoint(verts[i]);
                    var view = (camPos - wp).normalized;
                    float facing = Vector3.Dot(wn, view);
                    float half = Vector3.Dot(wn, light) * 0.5f + 0.5f;
                    float fres = 1f - Mathf.Abs(facing);
                    Color c;
                    if (Flat)
                    {
                        // a pool: the flat base, a lit lip round its edge and slow sheen patches drifting over it
                        float r = new Vector2(p.x, p.z).magnitude / maxR;
                        float patch = Mathf.Sin(p.x / maxR * 7f + t * 0.6f + seed) * Mathf.Sin(p.z / maxR * 6f - t * 0.5f);
                        c = r > 0.92f ? sheenDeep : patch > 0.86f ? sheenDeep : r < 0.35f ? Color.Lerp(gooDeep, gooBase, 0.5f) : gooBase;
                        cols[i] = new Color(c.r, c.g, c.b, wn.y < 0.3f ? 0f : 0.95f);
                        continue;
                    }
                    // Bright (the goo head): lit a step up, so it glows green inside its helmet like Deadlock's
                    c = half > Threshold ? (Bright ? gooSheen : sheenDeep) : wn.y < -0.35f ? (Bright ? gooBase : gooDeep) : (Bright ? sheenDeep : gooBase);
                    if (Vector3.Dot(wn, (light + view).normalized) > Glint && facing > 0f) c = Color.Lerp(gooRim, Color.white, 0.5f);
                    else if (fres > RimBand && facing > 0f) c = Color.Lerp(c, gooRim, 0.55f);
                    float a;
                    if (Gel)
                    {
                        // a block of goo with someone stuck in it: thick green all through (denser and darker low down), so what is
                        // inside shows as a dim shape; the glossy rim and sheen stay solid
                        float low = Mathf.Clamp01(0.5f - (wp.y - tr.position.y) / Mathf.Max(0.5f, maxH) * 0.5f);
                        if (half <= Threshold && fres <= RimBand) c = Color.Lerp(gooBase, gooDeep, low * 0.6f);
                        a = facing < 0f ? 0.3f + 0.2f * low : Mathf.Lerp(0.5f + 0.2f * low, 0.95f, Mathf.Pow(fres, 1.6f)) + (half > Threshold ? 0.1f : 0f);
                    }
                    else if (Glass)
                    {
                        // a clear helmet: almost invisible face-on, a pale glossy rim and the glint
                        c = Color.Lerp(Color.white, gooRim, 0.4f);
                        a = facing < 0f ? 0f : Mathf.Lerp(0.04f, 0.5f, Mathf.Pow(fres, 2.2f)) + (Vector3.Dot(wn, (light + view).normalized) > Glint ? 0.5f : 0f);
                    }
                    else if (Shell) a = facing < 0f ? 0.08f : Mathf.Lerp(ShellAlpha, 0.95f, Mathf.Pow(fres, 1.6f)) + (half > Threshold ? 0.12f : 0f);
                    else a = facing < -0.02f ? 0f : 1f;
                    cols[i] = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
                }
                mesh.vertices = verts;
                mesh.colors32 = cols;
                mesh.RecalculateBounds();
            }

            void OnDestroy() { if (mesh) Destroy(mesh); }
        }

        /// <summary>Splatter's landing preview: a see-through green disc over the floor (DrapeMesh) with a solid bright edge, laid again
        /// whenever the marker moves.</summary>
        sealed class FloorDisc : MonoBehaviour
        {
            public float Radius = 5f;
            Mesh mesh; MeshFilter mf; Vector3 laid = new Vector3(float.MaxValue, 0f, 0f);

            void Start()
            {
                var g = new GameObject("floorDisc");
                g.transform.SetParent(transform, false);
                mf = g.AddComponent<MeshFilter>();
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = CelMat(); mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            }

            void LateUpdate()
            {
                if (!mf || (transform.position - laid).sqrMagnitude < 0.09f) return;
                laid = transform.position;
                if (mesh) Destroy(mesh);
                mesh = DrapeMesh(laid, Radius, 8, 48);
                var v = mesh.vertices; var nrm = mesh.normals; var cols = new Color32[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    float r = new Vector2(v[i].x, v[i].z).magnitude / Mathf.Max(0.1f, Radius);
                    cols[i] = nrm[i].y < 0f ? A(gooRim, 0f) : r > 0.93f ? A(gooRim, 0.95f) : A(Color.Lerp(gooBase, gooRim, r * 0.6f), 0.22f + 0.2f * r);
                }
                mesh.colors32 = cols;
                mf.sharedMesh = mesh;
            }

            void OnDestroy() { if (mesh) Destroy(mesh); }
        }

        /// <summary>Frees a mesh built for one effect when the effect goes.</summary>
        sealed class DestroyMesh : MonoBehaviour
        {
            public Mesh Mesh;
            void OnDestroy() { if (Mesh) Destroy(Mesh); }
        }

        /// <summary>A blob's wobble: its scale breathes on each axis out of step, so it looks soft.</summary>
        sealed class Wobble : MonoBehaviour
        {
            public Vector3 Base = Vector3.one;
            float seed;
            void Awake() => seed = Random.value * 10f;
            void Update()
            {
                float t = Time.time * 9f + seed;
                transform.localScale = new Vector3(Base.x * (1f + 0.08f * Mathf.Sin(t)), Base.y * (1f + 0.08f * Mathf.Sin(t * 1.3f + 1f)), Base.z * (1f + 0.06f * Mathf.Sin(t * 0.7f + 2f)));
            }
        }

        /// <summary>The cube jiggles: a slow wobble, with a bigger shake now and then that dies down.</summary>
        sealed class Jiggle : MonoBehaviour
        {
            Vector3 base_; float kick, nextKick;
            void Start() => base_ = transform.localScale;
            void Update()
            {
                if (Time.time >= nextKick) { nextKick = Time.time + Random.Range(0.4f, 0.9f); kick = 0.06f; }
                kick = Mathf.MoveTowards(kick, 0.015f, Time.deltaTime * 0.1f);
                float t = Time.time * 12f;
                transform.localScale = new Vector3(base_.x * (1f + kick * Mathf.Sin(t)), base_.y * (1f - kick * Mathf.Sin(t)), base_.z * (1f + kick * Mathf.Sin(t + 1.5f)));
            }
        }

        /// <summary>Puddle Punch's fist: up out of the ground fast, a moment at the top, then back down, squashing as it lands.</summary>
        sealed class Riser : MonoBehaviour
        {
            public float Size = 2f;
            float age; Vector3 scale;
            void Start() { scale = transform.localScale; transform.localPosition = Vector3.down * Size * 1.6f; }
            void Update()
            {
                // all the way out in 0.12 s with an overshoot, settles, holds and squeezes, then sinks back into the ground
                age += Time.deltaTime;
                float y;
                if (age < 0.12f) y = Mathf.Lerp(-Size * 1.6f, Size * 0.18f, age / 0.12f);
                else if (age < 0.25f) y = Mathf.Lerp(Size * 0.18f, 0f, (age - 0.12f) / 0.13f);
                else if (age < 0.7f) y = 0f;
                else y = Mathf.Lerp(0f, -Size * 1.8f, (age - 0.7f) / 0.75f);
                transform.localPosition = Vector3.up * y;
                float squash = age < 0.12f ? 1.2f : age < 0.25f ? 1f - 0.1f * Mathf.Sin((age - 0.12f) / 0.13f * Mathf.PI) : 1f;
                transform.localScale = new Vector3(scale.x / squash, scale.y * squash, scale.z / squash);
            }
        }

        /// <summary>A goo stain fades: full for most of its life, then it shrinks away into the surface.</summary>
        sealed class StainFade : MonoBehaviour
        {
            public float Life = 2f;
            float age; Vector3 scale;
            void Start() => scale = transform.localScale;
            void Update()
            {
                age += Time.deltaTime;
                float k = Mathf.Clamp01((Life - age) / 0.6f);
                transform.localScale = scale * k;
                if (age >= Life) Destroy(gameObject);
            }
        }

        /// <summary>A puddle goes: after After seconds Target draws in toward its middle over a second.</summary>
        sealed class Sink : MonoBehaviour
        {
            public float After = 5f; public Transform Target;
            float age; Vector3 scale;
            void Start() { if (Target) scale = Target.localScale; }
            void Update()
            {
                age += Time.deltaTime;
                if (!Target || age < After) return;
                float k = Mathf.Clamp01(1f - (age - After));
                // it draws in toward its middle (the pool keeps lying on the ground as it goes)
                Target.localScale = new Vector3(scale.x * k, scale.y, scale.z * k);
                if (k <= 0f) { foreach (var b in GetComponents<Bubbler>()) b.enabled = false; }
            }
        }

        /// <summary>A thrown glob of goo: flies, falls, stretches along its fall and shrinks away.</summary>
        sealed class Glob : MonoBehaviour
        {
            public Vector3 Velocity;
            float age, life;
            void Start() => life = Random.Range(0.6f, 1.1f);
            void Update()
            {
                float dt = Time.deltaTime;
                age += dt;
                Velocity += Physics.gravity * 0.6f * dt;
                transform.position += Velocity * dt;
                float k = Mathf.Clamp01(1f - age / life);
                float stretch = 1f + Mathf.Min(1.2f, Velocity.magnitude * 0.06f);
                transform.rotation = Quaternion.FromToRotation(Vector3.up, Velocity.sqrMagnitude > 0.01f ? Velocity.normalized : Vector3.up);
                transform.localScale = new Vector3(k, k * stretch, k);
                if (age >= life) Destroy(gameObject);
            }
        }

        /// <summary>Heavy drips hanging off goo: a drop swells and stretches down from the underside for a moment, then lets go and
        /// falls (the No-Face drool). Radius = how far from the centre they form, Height = where (local y), Rate = drips a second.</summary>
        sealed class Dripper : MonoBehaviour
        {
            public float Radius = 0.5f, Height = 0f, Rate = 3f;
            float next;
            void Update()
            {
                if (Time.time < next) return;
                next = Time.time + Random.Range(0.5f, 1.5f) / Mathf.Max(0.1f, Rate);
                var off = Random.insideUnitCircle * Radius;
                var at = transform.position + new Vector3(off.x, Height, off.y);
                var holder = new GameObject("drip");
                holder.transform.position = at;
                GooBall(holder.transform, Random.Range(0.12f, 0.22f) * Mathf.Max(1f, Radius * 0.6f), false, 0.06f);
                holder.AddComponent<Drip>();
            }
        }

        sealed class Drip : MonoBehaviour
        {
            float age, hang; Vector3 velocity; Vector3 start;
            void Start() { hang = Random.Range(0.25f, 0.5f); start = transform.position; }
            void Update()
            {
                float dt = Time.deltaTime;
                age += dt;
                if (age < hang)
                {
                    // swelling and stretching down before it lets go
                    float k = age / hang;
                    transform.localScale = new Vector3(0.6f + 0.4f * k, 0.6f + 1.6f * k, 0.6f + 0.4f * k);
                    transform.position = start + Vector3.down * 0.15f * k;
                    return;
                }
                velocity += Physics.gravity * 0.7f * dt;
                transform.position += velocity * dt;
                float f = Mathf.Clamp01(1f - (age - hang) / 0.6f);
                transform.localScale = new Vector3(f, f * 1.8f, f);
                if (f <= 0f) Destroy(gameObject);
            }
        }

        /// <summary>Goo bubbles rising out of a puddle (or the punch's ground), swelling, then popping into droplets.</summary>
        sealed class Bubbler : MonoBehaviour
        {
            public float Radius = 2f, Rate = 4f;
            float next;
            void Update()
            {
                if (Time.time < next) return;
                next = Time.time + Random.Range(0.5f, 1.5f) / Mathf.Max(0.1f, Rate);
                var off = Random.insideUnitCircle * Radius;
                var holder = new GameObject("bubble");
                holder.transform.position = transform.position + new Vector3(off.x, 0.05f, off.y);
                GooBall(holder.transform, Random.Range(0.3f, 0.7f), false, 0.05f);
                holder.AddComponent<Bubble>();
            }
        }

        sealed class Bubble : MonoBehaviour
        {
            float age, life;
            void Start() { life = Random.Range(0.5f, 0.9f); transform.localScale = Vector3.one * 0.1f; }
            void Update()
            {
                age += Time.deltaTime;
                float k = Mathf.Clamp01(age / life);
                transform.localScale = new Vector3(1f, 0.55f, 1f) * Mathf.Lerp(0.1f, 1f, k);
                if (age < life) return;
                DropBurst(transform.position + Vector3.up * 0.2f, gooBase, 4, 0.4f, 2f, 0.1f, 0.1f);
                Destroy(gameObject);
            }
        }

        /// <summary>The Cube's tethers: one sagging goo strand per drained enemy, a bulge running along each toward the hero.</summary>
        sealed class TetherSet : MonoBehaviour
        {
            readonly List<LineRenderer> lines = new List<LineRenderer>();
            Vector3 from; readonly List<Vector3> to = new List<Vector3>();
            const int Points = 12;

            public void Set(Vector3 a, List<Vector3> b) { from = a; to.Clear(); to.AddRange(b); }

            LineRenderer Make()
            {
                var go = new GameObject("tether"); go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.sharedMaterial = Solid(Color.white);
                lr.startColor = lr.endColor = gooBase;
                lr.positionCount = Points;
                lr.widthMultiplier = 0.16f;
                lr.numCapVertices = 3;
                // the strand's own band: base along it, the sheen down the middle (two lines would double the draw: colour does it)
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(gooDeep, 0f), new GradientColorKey(gooBase, 0.3f), new GradientColorKey(gooSheen, 0.85f), new GradientColorKey(gooBase, 1f) },
                          new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                lr.colorGradient = g;
                lines.Add(lr);
                return lr;
            }

            void LateUpdate()
            {
                while (lines.Count < to.Count) Make();
                for (int i = 0; i < lines.Count; i++)
                {
                    var lr = lines[i];
                    lr.enabled = i < to.Count;
                    if (!lr.enabled) continue;
                    var b = to[i];
                    float sag = Mathf.Min(2f, Vector3.Distance(from, b) * 0.12f);
                    for (int p = 0; p < Points; p++)
                    {
                        float t = (float)p / (Points - 1);
                        lr.SetPosition(p, Vector3.Lerp(b, from, t) + Vector3.down * sag * 4f * t * (1f - t));
                    }
                    // the drain: a thicker bulge running from the enemy to Viscous
                    float pulse = Mathf.Lerp(0.12f, 0.88f, Mathf.Repeat(Time.time * 1.6f + i * 0.3f, 1f));
                    lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(pulse - 0.1f, 0.6f), new Keyframe(pulse, 1.9f), new Keyframe(pulse + 0.1f, 0.6f), new Keyframe(1f, 0.9f));
                }
            }
        }
    }
}
