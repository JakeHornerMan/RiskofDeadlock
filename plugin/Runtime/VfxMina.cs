using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Mina's visuals (creator, 2026-10-25: "the bat and red vfx just like deadlock"): built like the others from RoR2's
    /// glow, spark, smoke and tracer materials tinted crimson and blood red, with Deadlock's own bat model (flapping, through
    /// <see cref="BatFlock"/>) and her Deadlock textures read from the player's install (effects kind texture): the Love Bites
    /// mark (heart and fangs) popping over enemies, Sanguine Retreat's marker where she reforms and under Nox Nostra's circle, and
    /// Rake's cone on the ground. Bats everywhere: a stream of them is Sanguine Retreat, a blood-red cone of them pouring out of her is Nox Nostra, a
    /// dense furious flock with glowing eyes is Mist Form, which blasts out in a ring of bats when it reforms.</summary>
    public static partial class Vfx
    {
        static readonly Color minaCrimson = new Color(0.88f, 0.07f, 0.2f), minaBlood = new Color(0.55f, 0.02f, 0.07f),
                              minaRose = new Color(1f, 0.32f, 0.42f), minaDark = new Color(0.22f, 0.02f, 0.05f, 0.9f),
                              minaSmoke = new Color(0.45f, 0.06f, 0.1f, 0.85f), minaGlow = new Color(1f, 0.12f, 0.2f), minaRing = new Color(0.5f, 0.02f, 0.06f, 0.7f);

        static GameObject Mina(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float life)
        {
            switch (id)
            {
                case "mina_gun": return MinaGun(moment, at, follow, to);
                case "mina_bites": return MinaBites(moment, at, follow, to);
                case "mina_umbrella": return MinaUmbrella(moment, at, follow);
                case "mina_rake": return MinaRake(moment, at, radius, follow, to);
                case "mina_bats": return MinaBats(moment, at, radius, follow);
                case "mina_nox": return MinaNox(moment, at, radius, follow, to, life);
                case "mina_mist": return MinaMist(moment, at, radius, follow);
            }
            return null;
        }

        /// <summary>A bat swarm hung under a visual's root (so finding it and stopping the root take it too).</summary>
        static BatSwarm Swarm(GameObject root, int count, Vector3 at, Transform follow, float radius, bool furious = false, float size = 1f)
        {
            var s = BatSwarm.Make(count, at, follow, radius, furious, size);
            s.transform.SetParent(root.transform, true);
            return s;
        }

        /// <summary>A few glowing motes arcing from a point into a body (Rake's heal, a Love Bites reward).</summary>
        static void Motes(Vector3 from, Vector3 to, Color c, int count, float time)
        {
            for (int i = 0; i < count; i++)
            {
                var root = Root(from, time + 0.3f, null);
                Particles(root, "mat_glow", c, rate: 70, life: 0.18f, speed: 0f, size: 0.2f, world: true);
                var m = root.AddComponent<ArcMover>();
                m.From = from + Random.insideUnitSphere * 0.4f; m.To = to + Random.insideUnitSphere * 0.2f; m.Time = time * Random.Range(0.8f, 1.2f); m.Height = Random.Range(0.6f, 1.6f);
            }
        }

        // ------------------------------------------------------------------ New Money

        static GameObject MinaGun(string moment, Vector3 at, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "muzzle":
                {
                    // a bright crimson flash at the pistol's muzzle with a quick light (about 0.05 s)
                    var root = Root(at, 0.25f, follow);
                    Particles(root, "mat_glow", minaRose, count: 3, life: 0.06f, speed: 0f, size: 0.55f);
                    Particles(root, "mat_spark", minaCrimson, count: 6, life: 0.12f, speed: 6f, size: 0.05f, shapeRadius: 0.03f, stretch: 3f);
                    var l = Light(root, minaGlow, 5f, 6f, Vector3.zero);
                    var f = l.gameObject.AddComponent<FadeLight>(); f.Life = 0.06f; f.From = 6f;
                    return root;
                }
                case "hit":
                {
                    // on an enemy: a crimson spark burst and a puff of red
                    Burst(at, "mat_spark", minaCrimson, 10, 0.25f, 7f, 0.07f, 0.05f);
                    Burst(at, "mat_glow", minaRose, 2, 0.12f, 0f, 0.6f);
                    Burst(at, "mat_smoke", minaSmoke, 2, 0.4f, 0.8f, 0.35f, 0.1f);
                    return null;
                }
                case "impact":
                {
                    // on the world: sparks, a dust puff and a bullet mark that fades
                    var normal = to.HasValue ? (to.Value - at).normalized : Vector3.up;
                    Burst(at, "mat_spark", new Color(1f, 0.75f, 0.55f), 7, 0.2f, 6f, 0.05f, 0.03f);
                    Burst(at + normal * 0.05f, "mat_dust", new Color(0.6f, 0.55f, 0.5f, 0.7f), 2, 0.5f, 0.6f, 0.3f, 0.05f);
                    BulletMark(at, normal);
                    return null;
                }
            }
            return null;
        }

        static Material markMat;
        /// <summary>A small dark scorch where a bullet hit the world, fading over a few seconds.</summary>
        static void BulletMark(Vector3 at, Vector3 normal)
        {
            if (!markMat)
            {
                var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16f;
                        tex.SetPixel(x, y, new Color(0.05f, 0.03f, 0.03f, Mathf.Clamp01(1f - d * d) * 0.85f));
                    }
                tex.Apply();
                markMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, renderQueue = 2990 };
            }
            var root = Root(at + normal * 0.02f, 4f, null);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(root.transform, false);
            q.transform.rotation = Quaternion.LookRotation(-normal);
            q.transform.localScale = Vector3.one * 0.18f;
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = markMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var p = q.AddComponent<QuadPulse>(); p.From = 0.18f; p.To = 0.2f; p.Life = 4f; p.Tint = Color.white;
        }

        // ------------------------------------------------------------------ Love Bites

        static Material biteMat;
        /// <summary>Deadlock's Love Bites mark is a mask (the heart in red, the fangs in yellow and magenta, the jaw in blue): it is
        /// painted here as Deadlock shows it, a crimson heart with white fangs on a dark jaw. Null without the texture.</summary>
        static Material BiteMat()
        {
            if (biteMat) return biteMat;
            var row = Sheet.Effect("mina_bite_tex");
            var src = row != null ? DeadlockCache.Image(row.Addressable) : null;
            if (!src) return null;
            Color[] px;
            try { px = src.GetPixels(); }
            catch { return null; }
            var outPx = new Color[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                float a = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                Color col;
                if (c.g > 0.5f && c.r > 0.5f || c.r > 0.5f && c.b > 0.5f) col = new Color(1f, 0.96f, 0.94f);      // fangs
                else if (c.b > 0.5f) col = new Color(0.3f, 0.02f, 0.06f);                                           // the jaw
                else if (c.g > 0.5f) col = new Color(0.6f, 0.04f, 0.1f);                                            // the edge
                else col = new Color(0.9f, 0.08f, 0.2f);                                                            // the heart
                col.a = a;
                outPx[i] = col;
            }
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true);
            tex.SetPixels(outPx); tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            biteMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, renderQueue = 3100 };
            return biteMat;
        }

        static GameObject MinaBites(string moment, Vector3 at, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "trigger":
                {
                    // the bite mark pops over the enemy, and red motes fly from it to Mina
                    var root = Root(at + Vector3.up * 1.2f, 0.9f, null);
                    var mat = BiteMat();
                    if (mat)
                    {
                        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        Object.Destroy(q.GetComponent<Collider>());
                        q.transform.SetParent(root.transform, false);
                        q.AddComponent<FaceCamera>();
                        var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        var p = q.AddComponent<QuadPulse>(); p.From = 0.5f; p.To = 1.1f; p.Life = 0.8f; p.Tint = Color.white;
                    }
                    Particles(root, "mat_glow", minaCrimson, count: 4, life: 0.25f, speed: 0f, size: 0.9f);
                    if (to.HasValue) Motes(at, to.Value, minaRose, 3, 0.4f);
                    return root;
                }
                case "jump":
                {
                    // a jump back: a ring of red at her feet and a few bats fluttering up
                    Ring(at + Vector3.up * 0.1f, minaRing, 0.3f, 1.6f, 0.35f, 0.08f);
                    var root = Root(at, 1.2f, null);
                    var s = Swarm(root, 4, at + Vector3.up * 0.5f, null, 0.5f, false, 0.6f);
                    s.Burst(4f, 0.9f);
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ the umbrella

        static GameObject MinaUmbrella(string moment, Vector3 at, Transform follow)
        {
            if (moment != "loop") return null;
            // a soft crimson glow under the open umbrella, dark petals and motes drifting off its rim
            var root = Root(at, 0f, follow);
            Light(root, minaGlow, 3.5f, 1.4f, Vector3.down * 0.3f);
            Particles(root, "mat_glow", minaCrimson, rate: 6, life: 0.6f, speed: 0f, size: 0.8f);
            Particles(root, "mat_glow", minaRose, rate: 14, life: 1.1f, speed: 0.3f, size: 0.08f, shapeRadius: 0.6f, ring: true, world: true, gravity: 0.05f);
            Particles(root, "mat_smoke", minaDark, rate: 5, life: 1.2f, speed: 0.2f, size: 0.18f, shapeRadius: 0.65f, ring: true, world: true, gravity: 0.08f);
            return root;
        }

        // ------------------------------------------------------------------ Rake

        static GameObject MinaRake(string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            var dir = to.HasValue ? to.Value - at : Vector3.forward;
            var flat = dir; flat.y = 0; flat = flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward;
            switch (moment)
            {
                case "cast":
                {
                    // the area the swing covers, as a flat half-disc at her own height (creator, 2026-10-26: the floor cone looked
                    // wrong in the air): just above the floor when she stands on it, at her waist when she is in the air.
                    // Deadlock's Rake cone, a bright crimson edge at its full reach and a glow inside it
                    var plane = at - Vector3.up * 0.3f;
                    if (Physics.Raycast(at, Vector3.down, out var g, 1.6f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) plane = g.point + Vector3.up * 0.08f;
                    var root = Root(plane, 0.6f, null);
                    root.transform.rotation = Quaternion.LookRotation(flat);
                    var tint = new Color(minaCrimson.r, minaCrimson.g, minaCrimson.b, 0.6f);
                    var q = SymbolQuad(root, "mina_cone_tex", tint, radius * 2f, false);
                    if (q)
                    {
                        // the cone's tip at her, pointing along the swing
                        q.transform.localPosition = Vector3.forward * radius * 0.5f;
                        var p = q.AddComponent<QuadPulse>(); p.From = radius * 1.6f; p.To = radius * 2f; p.Life = 0.55f; p.Tint = tint;
                    }
                    AddArc(root, new Color(minaRose.r, minaRose.g, minaRose.b, 0.85f), radius, 0.14f, 0.12f);
                    Particles(root, "mat_glow", new Color(minaCrimson.r, minaCrimson.g, minaCrimson.b, 0.5f), count: 6, life: 0.4f, speed: 0f, size: radius * 0.5f, shapeRadius: radius * 0.4f, flat: true);
                    return root;
                }
                case "swing":
                {
                    // a crimson crescent trailing the umbrella's tip across the 180 deg arc, a wider faint one out to the reach
                    var root = Root(at, 0.6f, null);
                    root.transform.rotation = Quaternion.LookRotation(flat);
                    AddArc(root, minaCrimson, 2.4f, 0.35f, 0.16f);
                    AddArc(root, new Color(minaRose.r, minaRose.g, minaRose.b, 0.5f), 3.2f, 0.18f, 0.2f);
                    AddArc(root, new Color(minaBlood.r, minaBlood.g, minaBlood.b, 0.35f), radius * 0.8f, 0.12f, 0.26f);
                    Particles(root, "mat_spark", minaCrimson, count: 24, life: 0.3f, speed: 8f, size: 0.06f, shapeRadius: 1.5f, stretch: 2f);
                    var fl = Light(root, minaGlow, 6f, 2.5f, Vector3.forward * 1.5f).gameObject.AddComponent<FadeLight>(); fl.Life = 0.3f; fl.From = 2.5f;
                    return root;
                }
                case "hit":
                {
                    // a crescent slash cutting across the enemy, the swing's own crimson arcs in small (creator, 2026-10-26: "a slash
                    // vfx on them just like on the character"), blood-red sparks, and red motes flying back into Mina for the heal
                    var from = to.HasValue ? to.Value : at - Vector3.forward;
                    var away = at - from; away.y = 0; away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
                    var slash = Root(at - away * 1.1f, 0.6f, null);
                    slash.transform.rotation = Quaternion.LookRotation(away) * Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f));
                    AddArc(slash, minaCrimson, 1.1f, 0.3f, 0.1f);
                    AddArc(slash, new Color(minaRose.r, minaRose.g, minaRose.b, 0.6f), 1.4f, 0.14f, 0.13f);
                    var fl = Light(slash, minaGlow, 4f, 2.5f, away * 1.1f).gameObject.AddComponent<FadeLight>(); fl.Life = 0.2f; fl.From = 2.5f;
                    Burst(at, "mat_spark", minaCrimson, 14, 0.3f, 8f, 0.07f, 0.1f);
                    Burst(at, "mat_glow", minaBlood, 3, 0.2f, 0f, 1.0f);
                    if (to.HasValue) Motes(at, to.Value, minaRose, 3, 0.35f);
                    return null;
                }
                case "execute":
                {
                    Burst(at, "mat_spark", minaCrimson, 30, 0.45f, 11f, 0.09f, 0.2f);
                    Burst(at, "mat_smoke", minaSmoke, 6, 0.7f, 2f, 0.6f, 0.3f);
                    Flash(at, minaGlow, 6f, 0.25f, 5f);
                    if (to.HasValue) Motes(at, to.Value, minaRose, 6, 0.45f);
                    return null;
                }
            }
            return null;
        }

        /// <summary>A crescent of radius r swept from one side to the other over sweep seconds (180 deg in front of the root), then fading.</summary>
        static void AddArc(GameObject root, Color c, float r, float width, float sweep)
        {
            var go = new GameObject("arc");
            go.transform.SetParent(root.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mat("mat_tracer", c);
            lr.useWorldSpace = false;
            lr.positionCount = 0;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.7f, width), new Keyframe(1f, width * 0.5f));
            lr.startColor = c; lr.endColor = c;
            var a = go.AddComponent<ArcSweep>(); a.Radius = r; a.Sweep = sweep; a.Line = lr; a.Tint = c;
        }

        sealed class ArcSweep : MonoBehaviour
        {
            public float Radius, Sweep; public LineRenderer Line; public Color Tint;
            float age;
            void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / Mathf.Max(0.01f, Sweep));
                int n = Mathf.Max(2, (int)(24 * t));
                Line.positionCount = n;
                // from her right to her left, a little rising as it goes (the umbrella swung up through the arc)
                for (int i = 0; i < n; i++)
                {
                    float a = Mathf.Lerp(90f, 90f - 180f * t, (float)i / (n - 1));
                    var p = Quaternion.Euler(0f, a, 0f) * Vector3.forward * Radius;
                    Line.SetPosition(i, p + Vector3.up * (0.25f * i / n - 0.1f));
                }
                float fade = Mathf.Clamp01(1f - (age - Sweep) / 0.3f);
                var c = new Color(Tint.r, Tint.g, Tint.b, Tint.a * fade);
                Line.startColor = c; Line.endColor = c;
            }
        }

        // ------------------------------------------------------------------ Sanguine Retreat

        static GameObject MinaBats(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // she bursts apart: a red flash, smoke and bats flying off in every direction
                    Flash(at, minaGlow, 7f, 0.3f, 5f);
                    Burst(at, "mat_smoke", minaSmoke, 8, 0.6f, 2.5f, 0.7f, 0.4f);
                    Burst(at, "mat_glow", minaCrimson, 4, 0.2f, 0f, 1.6f);
                    Ring(at, minaRing, 0.4f, 2.4f, 0.3f, 0.07f, true);
                    var root = Root(at, 1.3f, null);
                    Swarm(root, 8, at, null, 0.6f).Burst(7f, 1f);
                    return root;
                }
                case "loop":
                {
                    // the swarm she is now: bats churning tight round where she flies, a dark-red streak behind them
                    var root = Root(at, 0f, follow);
                    var s = Swarm(root, 18, at, follow, Mathf.Max(0.7f, radius), false, 0.85f);
                    s.Spread = 0.5f; s.Turns = 1.2f; s.Tightness = 9f;
                    Particles(root, "mat_smoke", minaSmoke, rate: 40, life: 0.45f, speed: 0.3f, size: 0.5f, shapeRadius: 0.5f, world: true);
                    Particles(root, "mat_glow", minaCrimson, rate: 30, life: 0.25f, speed: 0f, size: 0.35f, shapeRadius: 0.5f, world: true);
                    Light(root, minaGlow, 5f, 2f, Vector3.zero);
                    var trail = new GameObject("trail"); trail.transform.SetParent(root.transform, false);
                    Trail(trail, minaCrimson, 0.45f, 0.3f);
                    return root;
                }
                case "aim":
                {
                    // Deadlock's marker where she will reform
                    var root = Root(at, 0f, null);
                    SymbolQuad(root, "mina_marker_tex", new Color(minaRose.r, minaRose.g, minaRose.b, 0.85f), 1.1f, true);
                    Particles(root, "mat_glow", minaCrimson, rate: 10, life: 0.4f, speed: 0f, size: 0.6f);
                    return root;
                }
                case "end":
                {
                    // the bats pull back together into her
                    var root = Root(at, 1.2f, follow);
                    Ring(at, minaRing, 2.2f, 0.3f, 0.25f, 0.06f, true);
                    Burst(at, "mat_smoke", minaSmoke, 6, 0.5f, 1.5f, 0.6f, 0.5f);
                    Particles(root, "mat_glow", minaRose, count: 3, life: 0.2f, speed: 0f, size: 1.2f);
                    Flash(at, minaGlow, 5f, 0.25f, 3f);
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Nox Nostra

        static GameObject MinaNox(string moment, Vector3 at, float radius, Transform follow, Vector3? to, float life)
        {
            switch (moment)
            {
                case "cast":
                {
                    // bats burst up round her and lift her into the air: a blood-red flash, a shockwave on the ground and a shake
                    var root = Root(at, 1.6f, null);
                    var s = Swarm(root, 24, at, follow, 1.4f, true, 0.8f);
                    s.Offset = Vector3.up * 1.5f; s.Turns = 1.2f;
                    s.Burst(7f, 1.4f);
                    Flash(at + Vector3.up, minaGlow, 12f, 0.4f, 8f);
                    Ring(at + Vector3.up * 0.1f, minaRing, 0.5f, 6f, 0.5f, 0.3f);
                    Ring(at + Vector3.up, minaRing, 0.4f, 3.5f, 0.4f, 0.15f, true);
                    Burst(at, "mat_smoke", minaSmoke, 14, 0.9f, 2.5f, 1.1f, 0.8f);
                    Burst(at, "mat_spark", minaCrimson, 30, 0.5f, 9f, 0.08f, 0.5f, upward: true);
                    Shake(at, 20f, 1.5f, 0.3f);
                    return root;
                }
                case "loop":
                {
                    // a few bats circle under her the whole time
                    var root = Root(at, 0f, follow);
                    var s = Swarm(root, 8, at, follow, 1.1f, false, 0.7f);
                    s.Offset = Vector3.down * 0.4f; s.Turns = 0.7f; s.Spread = 0.4f;
                    Light(root, minaGlow, 4f, 1f, Vector3.zero);
                    return root;
                }
                case "aim":
                {
                    // the circle where the bats land (the owner's): Deadlock's marker in the middle, a dark-red glowing edge and a faint dome
                    var root = Root(at, 0f, null);
                    var mark = SymbolQuad(root, "mina_marker_tex", new Color(minaCrimson.r, minaCrimson.g, minaCrimson.b, 0.8f), 2.2f, false);
                    if (mark) mark.transform.localPosition = Vector3.up * 0.1f;
                    var edge = Particles(root, "mat_glow", minaCrimson, rate: 9f * radius, life: 0.5f, speed: 0f, size: 0.6f, shapeRadius: radius, ring: true);
                    var sh = edge.shape; sh.radiusThickness = 0f;
                    Particles(root, "mat_smoke", minaDark, rate: 3f * radius, life: 0.9f, speed: 0.2f, size: 0.5f, shapeRadius: radius, ring: true, upward: false);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = new Vector3(radius * 2f, radius * 0.5f, radius * 2f);
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", new Color(minaBlood.r, minaBlood.g, minaBlood.b, 0.5f));
                    return root;
                }
                case "stream":
                {
                    // the bats pouring out of her down a cone onto the circle (NoxStreamFx; life is the cone's width at her)
                    var root = Root(at, 0f, null);
                    root.AddComponent<NoxStreamFx>().Build(at, to ?? at + Vector3.down * 5f, radius, life > 0f ? life : 1f);
                    return root;
                }
                case "hit":
                    // what the stream catches bursts blood red
                    Burst(at, "mat_spark", minaCrimson, 16, 0.35f, 9f, 0.08f, 0.2f);
                    Burst(at, "mat_glow", minaBlood, 3, 0.25f, 0f, 1.3f);
                    Burst(at, "mat_smoke", minaSmoke, 3, 0.5f, 1.2f, 0.6f, 0.2f);
                    return null;
                case "end":
                {
                    // the stream's last bats slam into the ground: a big red flash, two shockwaves, a pillar of smoke and a shake
                    Flash(at + Vector3.up, minaGlow, radius * 2f, 0.5f, 8f);
                    Ring(at + Vector3.up * 0.1f, minaRing, 0.5f, radius * 1.4f, 0.6f, 0.45f);
                    Ring(at + Vector3.up * 1.5f, minaRing, 0.5f, radius, 0.45f, 0.3f, true);
                    Burst(at, "mat_smoke", minaSmoke, 20, 1.1f, radius * 0.4f, 2f, radius * 0.5f, upward: true);
                    Burst(at, "mat_spark", minaCrimson, 60, 0.7f, 14f, 0.1f, radius * 0.4f, upward: true, gravity: 1f);
                    Shake(at, radius * 3f, 2.5f, 0.35f);
                    var root = Root(at, 1.6f, null);
                    Swarm(root, 30, at + Vector3.up, null, radius * 0.4f, true).Burst(radius * 1.2f, 1.2f);
                    return root;
                }
            }
            return null;
        }

        /// <summary>Nox Nostra's stream (creator, 2026-10-26: bats "streaming out of mina toward the target location" in a cone,
        /// "really imact ful", blood red): furious red bats pour out of her down a cone that widens from Near at her to Far (the
        /// circle's radius) on the ground, spiralling as they go, round a crimson beam, sparks and a churn of blood-red mist; where
        /// they land the ground glows, smokes and throws sparks, and every bite pulses a shockwave (Pulse). The state moves both
        /// ends every frame (Aim); Release scatters the bats as it ends. Every machine draws its own.</summary>
        public sealed class NoxStreamFx : MonoBehaviour
        {
            /// <summary>Bats down the stream, and their speed (m/s).</summary>
            const int Bats = 70;
            const float Speed = 24f;

            sealed class Flier { public Bat Bat; public float T, Angle, R, Rate, Spin, Wobble; public Vector3 Out; }
            readonly List<Flier> fliers = new List<Flier>();
            readonly List<KeyValuePair<Transform, float>> mist = new List<KeyValuePair<Transform, float>>();
            float near = 1f, far = 10f, pulse;
            Vector3 from, to;
            bool released;
            LineRenderer core, glow;
            Transform mouth, impact, sparkT;
            ParticleSystem sparks;

            public void Build(Vector3 a, Vector3 b, float radius, float nearWidth)
            {
                from = a; to = b; far = Mathf.Max(1f, radius); near = Mathf.Max(0.2f, nearWidth);
                // the beam: a bright crimson core, and a wide dark-red glow round it that fans out to the circle
                core = Beam(minaRose, 0.35f, 1.6f);
                glow = Beam(new Color(minaBlood.r, minaBlood.g, minaBlood.b, 0.55f), near * 1.2f, far * 0.9f);
                // blood-red mist and glow churning along the cone
                foreach (var f in new[] { 0.15f, 0.35f, 0.55f, 0.75f, 0.92f })
                {
                    var go = new GameObject("mist"); go.transform.SetParent(transform, false);
                    float w = Mathf.Lerp(near, far, f);
                    Particles(go, "mat_smoke", minaSmoke, rate: 10f + 25f * f, life: 0.6f, speed: 0.6f, size: 0.6f + w * 0.18f, shapeRadius: w * 0.8f, world: true);
                    Particles(go, "mat_glow", minaCrimson, rate: 10f, life: 0.3f, speed: 0f, size: 0.5f + w * 0.05f, shapeRadius: w * 0.7f, world: true);
                    mist.Add(new KeyValuePair<Transform, float>(go.transform, f));
                }
                // sparks shooting out of her down the cone
                var sg = new GameObject("sparks"); sg.transform.SetParent(transform, false); sparkT = sg.transform;
                sparks = Particles(sg, "mat_spark", minaCrimson, rate: 90f, life: 0.5f, speed: Speed * 1.3f, size: 0.08f, world: true, stretch: 4f);
                var sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.radius = near * 0.5f;
                // her end: a hot glow under the umbrella
                var mg = new GameObject("mouth"); mg.transform.SetParent(transform, false); mouth = mg.transform;
                Light(mg, minaGlow, 6f, 3f, Vector3.zero);
                Particles(mg, "mat_glow", minaRose, rate: 30f, life: 0.2f, speed: 0f, size: 0.9f, shapeRadius: 0.4f);
                // the ground end: a glowing edge, smoke rolling out, sparks thrown up, a pool of red light, Deadlock's marker
                var ig = new GameObject("impact"); ig.transform.SetParent(transform, false); impact = ig.transform;
                Particles(ig, "mat_glow", minaCrimson, rate: 9f * far, life: 0.5f, speed: 0f, size: 0.8f, shapeRadius: far, ring: true);
                Particles(ig, "mat_smoke", minaSmoke, rate: 5f * far, life: 1.2f, speed: 1.2f, size: 1.4f, shapeRadius: far * 0.9f, flat: true, world: true);
                Particles(ig, "mat_spark", minaCrimson, rate: 60f, life: 0.6f, speed: 9f, size: 0.07f, shapeRadius: far * 0.6f, upward: true, world: true, gravity: 1f, stretch: 2f);
                Particles(ig, "mat_glow", minaBlood, rate: 6f, life: 0.8f, speed: 0f, size: far * 0.6f, shapeRadius: far * 0.3f);
                var mark = SymbolQuad(ig, "mina_marker_tex", new Color(minaCrimson.r, minaCrimson.g, minaCrimson.b, 0.7f), far * 1.1f, false);
                if (mark) mark.transform.localPosition = Vector3.up * 0.1f;
                Light(ig, minaGlow, far * 1.6f, 3f, Vector3.up * 1.5f).gameObject.AddComponent<Flicker>();
                // the first bats hit: a shockwave, a flash and a shake
                Ring(b + Vector3.up * 0.1f, minaRing, 0.5f, far * 1.2f, 0.5f, 0.4f);
                Flash(b + Vector3.up, minaGlow, far * 2f, 0.4f, 8f);
                Shake(b, far * 3f, 2f, 0.35f);
                foreach (var bat in BatFlock.Take(Bats, true, 0.9f))
                    fliers.Add(new Flier
                    {
                        Bat = bat, T = Random.value, Angle = Random.value * 360f, R = Mathf.Sqrt(Random.value), Rate = Random.Range(0.8f, 1.25f),
                        Spin = Random.Range(90f, 260f) * (Random.value < 0.5f ? -1f : 1f), Wobble = Random.value * 6f,
                    });
                Place();
            }

            LineRenderer Beam(Color c, float w0, float w1)
            {
                var go = new GameObject("beam"); go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.sharedMaterial = Mat("mat_tracer", c);
                lr.startColor = lr.endColor = c;
                lr.positionCount = 2;
                lr.widthCurve = new AnimationCurve(new Keyframe(0f, w0), new Keyframe(1f, w1));
                return lr;
            }

            /// <summary>Both ends, every frame: under her umbrella and the ground under the crosshair.</summary>
            public void Aim(Vector3 a, Vector3 b) { from = a; to = b; }

            /// <summary>A bite: a shockwave, sparks and smoke where the bats land, the beam swells, a small shake.</summary>
            public void Pulse()
            {
                if (released) return;
                pulse = 1f;
                Ring(to + Vector3.up * 0.1f, minaRing, far * 0.3f, far * 1.15f, 0.4f, 0.3f);
                Burst(to, "mat_spark", minaCrimson, 30, 0.5f, 12f, 0.09f, far * 0.5f, upward: true);
                Burst(to, "mat_smoke", minaSmoke, 6, 0.8f, 3f, 1.6f, far * 0.6f, flat: true);
                Flash(to + Vector3.up, minaGlow, far * 1.5f, 0.2f, 6f);
                Shake(to, far * 2.5f, 0.8f, 0.15f);
            }

            /// <summary>It ends: the bats scatter from the stream and go back to the pool when the visual goes.</summary>
            public void Release()
            {
                if (released) return;
                released = true;
                foreach (var fl in fliers)
                    fl.Out = (Random.onUnitSphere + Vector3.up * 0.4f).normalized * Random.Range(6f, 14f);
            }

            void Place()
            {
                var axis = to - from;
                float len = Mathf.Max(0.5f, axis.magnitude);
                var dir = axis / len;
                float swell = 1f + pulse * 0.6f;
                if (core) { core.SetPosition(0, from); core.SetPosition(1, to); core.widthMultiplier = swell; }
                if (glow) { glow.SetPosition(0, from); glow.SetPosition(1, to); glow.widthMultiplier = swell; }
                foreach (var m in mist) m.Key.position = from + axis * m.Value;
                if (mouth) mouth.position = from;
                if (impact) impact.position = to;
                if (sparkT)
                {
                    sparkT.position = from;
                    sparkT.rotation = Quaternion.LookRotation(dir);
                    var sh = sparks.shape; sh.angle = Mathf.Clamp(Mathf.Atan2(far - near, len) * Mathf.Rad2Deg, 2f, 60f);
                    var main = sparks.main; main.startLifetime = len / (Speed * 1.3f);
                }
            }

            void Update()
            {
                float dt = Time.deltaTime;
                pulse = Mathf.Max(0f, pulse - dt * 4f);
                if (!released) Place();
                var axis = to - from;
                float len = Mathf.Max(0.5f, axis.magnitude);
                var dir = axis / len;
                var side = Vector3.Cross(dir, Vector3.up);
                if (side.sqrMagnitude < 1e-3f) side = Vector3.right;
                side.Normalize();
                var up = Vector3.Cross(side, dir);
                foreach (var fl in fliers)
                {
                    if (fl.Bat == null || !fl.Bat.Go) continue;
                    var at = fl.Bat.Go.transform.position;
                    if (released)
                    {
                        fl.Out *= Mathf.Exp(-1.5f * dt);
                        if (fl.Out.sqrMagnitude > 0.01f) fl.Bat.Pose(at + fl.Out * dt, Quaternion.LookRotation(fl.Out.normalized));
                        continue;
                    }
                    // down the cone, spiralling round it and wider the further they go; back out of her once they land
                    fl.T += dt * Speed * fl.Rate / len;
                    bool wrapped = fl.T >= 1f;
                    if (wrapped) { fl.T -= 1f; fl.R = Mathf.Sqrt(Random.value); }
                    fl.Angle += fl.Spin * dt; fl.Wobble += dt * 5f;
                    float w = Mathf.Lerp(near, far * 0.9f, fl.T) * fl.R;
                    float rad = fl.Angle * Mathf.Deg2Rad;
                    var want = from + axis * fl.T + (Mathf.Cos(rad) * side + Mathf.Sin(rad) * up) * w + up * Mathf.Sin(fl.Wobble) * 0.25f;
                    var face = !wrapped && (want - at).sqrMagnitude > 1e-4f ? (want - at).normalized : dir;
                    fl.Bat.Pose(want, Quaternion.LookRotation(face, Vector3.up));
                }
            }

            void OnDestroy()
            {
                var list = new List<Bat>();
                foreach (var fl in fliers) if (fl.Bat != null) list.Add(fl.Bat);
                BatFlock.Give(list);
                fliers.Clear();
            }
        }

        // ------------------------------------------------------------------ Mist Form

        static GameObject MinaMist(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // she bursts apart: a blinding red flash, two shockwaves, a column of blood-red smoke, bats exploding out and a hard shake
                    Flash(at, minaGlow, 16f, 0.5f, 10f);
                    Ring(at, minaRing, 0.5f, 6f, 0.45f, 0.3f);
                    Ring(at, minaRing, 0.5f, 4.5f, 0.4f, 0.2f, true);
                    Burst(at, "mat_smoke", minaSmoke, 24, 1f, 4f, 1.4f, 0.6f, upward: true);
                    Burst(at, "mat_spark", minaCrimson, 50, 0.6f, 14f, 0.1f, 0.4f);
                    Burst(at, "mat_glow", minaBlood, 4, 0.35f, 0f, 3f);
                    Shake(at, 25f, 3f, 0.4f);
                    var root = Root(at, 1.4f, null);
                    Swarm(root, 30, at, null, 0.8f, true).Burst(12f, 1.1f);
                    return root;
                }
                case "loop":
                {
                    // a big furious flock churning round the flier: blood-red bats with glowing eyes beating fast, a looser outer swirl,
                    // blood-red mist and embers streaming off it, a wide red streak behind and a flickering glow. radius is the body's
                    // size: a big survivor makes a bigger cloud
                    var root = Root(at, 0f, follow);
                    int n = Mathf.Clamp(Mathf.RoundToInt(40 * radius), 40, 90);
                    var s = Swarm(root, n, at, follow, radius, true, 0.75f);
                    s.Spread = radius * 0.6f; s.Turns = 1.6f; s.Tightness = 11f;
                    var outer = Swarm(root, n / 2, at, follow, radius * 1.9f, true, 0.6f);
                    outer.Spread = radius * 0.9f; outer.Turns = 0.9f; outer.Tightness = 5f;
                    Particles(root, "mat_smoke", minaSmoke, rate: 60, life: 0.7f, speed: 0.5f, size: 0.8f * radius, shapeRadius: radius * 0.8f, world: true);
                    Particles(root, "mat_glow", minaGlow, rate: 35, life: 0.25f, speed: 0f, size: 0.5f, shapeRadius: radius * 0.8f);
                    Particles(root, "mat_spark", minaCrimson, rate: 50, life: 0.5f, speed: 2f, size: 0.06f, shapeRadius: radius * 0.9f, world: true, stretch: 2f);
                    Light(root, minaGlow, 8f * radius, 4f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    var trail = new GameObject("trail"); trail.transform.SetParent(root.transform, false);
                    Trail(trail, minaCrimson, 0.9f * radius, 0.5f);
                    var trail2 = new GameObject("trail2"); trail2.transform.SetParent(root.transform, false);
                    Trail(trail2, new Color(minaBlood.r, minaBlood.g, minaBlood.b, 0.6f), 2f * radius, 0.35f);
                    return root;
                }
                case "hit":
                {
                    // a burst of blood-red sparks and mist with a flash, and a few bats cling to the enemy for a moment
                    Burst(at, "mat_spark", minaCrimson, 28, 0.4f, 11f, 0.09f, 0.2f);
                    Burst(at, "mat_glow", minaBlood, 4, 0.25f, 0f, 1.8f);
                    Burst(at, "mat_smoke", minaSmoke, 4, 0.6f, 1.5f, 0.8f, 0.2f);
                    Flash(at, minaGlow, 5f, 0.15f, 4f);
                    var root = Root(at, 1.6f, follow);
                    var s = Swarm(root, 5, at, follow, 0.6f, true, 0.7f);
                    s.Tightness = 14f; s.Turns = 2f; s.AutoStop = 0.7f;
                    return root;
                }
                case "end":
                {
                    // the flock spirals in, then blasts out across the whole burst: a huge flash, three shockwaves (one on the ground),
                    // a pillar of blood-red smoke, a storm of sparks, a ring of bats flung out and a hard shake
                    Flash(at, minaGlow, radius * 1.5f, 0.6f, 12f);
                    Ring(at, minaRing, 0.5f, radius, 0.45f, 0.45f);
                    Ring(at, minaRing, 0.5f, radius * 0.7f, 0.35f, 0.3f, true);
                    Ring(at, minaRing, 0.5f, radius * 1.3f, 0.7f, 0.25f);
                    if (Physics.Raycast(at, Vector3.down, out var g, 30f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                        Ring(g.point + Vector3.up * 0.1f, minaRing, 1f, radius * 1.2f, 0.6f, 0.5f);
                    Burst(at, "mat_smoke", minaSmoke, 40, 1.2f, radius * 0.7f, 2f, 1.5f);
                    Burst(at, "mat_smoke", minaDark, 16, 1.6f, radius * 0.3f, 2.4f, 1f, upward: true);
                    Burst(at, "mat_spark", minaCrimson, 90, 0.8f, radius * 1.2f, 0.14f, 0.6f);
                    Burst(at, "mat_glow", minaBlood, 6, 0.4f, 0f, radius * 0.5f);
                    Shake(at, radius * 2.5f, 5f, 0.5f);
                    var root = Root(at, 1.6f, null);
                    Swarm(root, 80, at, null, 1f, true).Burst(radius * 1.4f, 1.3f);
                    return root;
                }
            }
            return null;
        }
    }
}
