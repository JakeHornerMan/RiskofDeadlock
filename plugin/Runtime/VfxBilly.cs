using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Billy's visuals (v0.11.0): stone and punk pink, as the creator chose. Grey-brown rock chunks that fly and bounce,
    /// rock spikes punching up out of the ground, cracks, craters and dust, with sparks, rings and rims in his Deadlock magenta.
    /// Chain Gang strings Deadlock's own chain links (the effects prop) from his hand to every chained enemy.</summary>
    public static partial class Vfx
    {
        static readonly Color billyPink = new Color(0.95f, 0.32f, 0.72f), billyHot = new Color(1f, 0.75f, 0.9f),
                              billyMagenta = new Color(0.64f, 0.24f, 0.53f), billyDust = new Color(0.62f, 0.56f, 0.48f, 0.65f),
                              billyGrit = new Color(0.72f, 0.66f, 0.58f);
        static readonly Color[] rockColors = { new Color(0.42f, 0.39f, 0.35f), new Color(0.55f, 0.5f, 0.44f), new Color(0.33f, 0.3f, 0.27f), new Color(0.48f, 0.42f, 0.36f) };
        static readonly Dictionary<Color, Material> rockMats = new Dictionary<Color, Material>();

        static Material RockMat(Color c)
        {
            if (!rockMats.TryGetValue(c, out var m) || !m) rockMats[c] = m = new Material(ModelBuilder.Template) { color = c };
            return m;
        }

        static GameObject Billy(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle)
        {
            switch (id)
            {
                case "billy_gun": return BillyGun(moment, at, follow);
                case "billy_bat": return BillyBat(moment, at, radius, to, angle);
                case "billy_bashdown": return BillyBashdown(moment, at, radius, follow);
                case "billy_ram": return BillyRam(moment, at, radius, follow);
                case "billy_float": return BillyFloat(moment, at, radius, follow);
                case "billy_dodge": return BillyDodge(moment, at, follow);
                case "billy_chains": return BillyChainSlam(moment, at, radius);
            }
            return null;
        }

        // ------------------------------------------------------------------ Biquette
        static GameObject BillyGun(string moment, Vector3 at, Transform follow)
        {
            switch (moment)
            {
                case "muzzle":
                    Burst(at, "mat_spark", billyPink, 4, 0.12f, 6f, 0.05f);
                    Burst(at, "mat_smoke", new Color(0.6f, 0.58f, 0.56f, 0.35f), 1, 0.4f, 0.6f, 0.4f);
                    return null;
                case "hit":
                    // a chip of stone and a little dust where the bullet lands
                    Burst(at, "mat_spark", billyGrit, 4, 0.3f, 4f, 0.05f, gravity: 1.5f);
                    Burst(at, "mat_smoke", billyDust, 1, 0.5f, 0.4f, 0.35f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ the bat
        static GameObject BillyBat(string moment, Vector3 at, float radius, Vector3? to, float angle)
        {
            switch (moment)
            {
                case "cast":
                    // the swing's arc, rolled a different way each swing; the heavy overhead wider
                    Slash(at, billyPink, 1.5f * radius, angle);
                    Slash(at, billyHot, 1.1f * radius, angle + 8f);
                    return null;
                case "hit":
                {
                    var dir = to.HasValue ? (to.Value - at).normalized : Vector3.up;
                    Burst(at, "mat_glow", billyPink, 2, 0.15f, 0f, 1.2f * radius);
                    Burst(at, "mat_spark", billyGrit, (int)(14 * radius), 0.5f, 9f, 0.08f, shapeRadius: 0.3f, gravity: 2.5f);
                    Burst(at, "mat_spark", billyPink, (int)(8 * radius), 0.25f, 7f, 0.06f, shapeRadius: 0.2f);
                    Burst(at, "mat_smoke", billyDust, 3, 0.6f, 1.5f, 0.8f * radius, shapeRadius: 0.3f);
                    Rocks(at + dir * 0.3f, (int)(3 * radius), 0.6f, 0.12f);
                    Flash(at, billyPink, 5f * radius, 0.15f, 4f);
                    Shake(at, 8f, 0.6f * radius, 0.12f);
                    return null;
                }
                case "end":
                    // the overhead's small crack where the bat comes down in front of him
                    if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var g, 5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    {
                        Cracks(g.point, 1.6f, 4);
                        Burst(g.point, "mat_smoke", billyDust, 5, 0.7f, 2f, 0.9f, shapeRadius: 0.4f, flat: true);
                        Rocks(g.point, 3, 0.5f, 0.1f);
                    }
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Bashdown
        static GameObject BillyBashdown(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                    // the hop: dust off the ground and grit falling off the raised bat
                    Burst(at, "mat_smoke", billyDust, 6, 0.7f, 2.5f, 1f, shapeRadius: 0.6f, flat: true);
                    Burst(at + Vector3.up * 2f, "mat_spark", billyPink, 10, 0.35f, 3f, 0.06f);
                    return null;
                case "loop":
                {
                    // the dive: a thin pink streak and grit pouring off behind him
                    var root = Root(at, 0f, follow);
                    var streak = new GameObject("streak"); streak.transform.SetParent(root.transform, false); streak.transform.localPosition = Vector3.up;
                    Trail(streak, billyPink, 0.35f, 0.25f);
                    Particles(root, "mat_spark", billyGrit, rate: 60, life: 0.4f, speed: 2f, size: 0.07f, shapeRadius: 0.5f, world: true, gravity: 1f);
                    return root;
                }
                case "hit":
                    Smash(at, radius, radius >= 6f);
                    // Deadlock's nail from the sigil, planted in the crater a moment
                    Nail(at);
                    return null;
                case "wave":
                    // the ground shockwave racing out: a ring of dust and stones
                    Ring(at + Vector3.up * 0.2f, billyPink, 0.5f, radius, 0.3f, 0.5f);
                    var edge = Root(at + Vector3.up * 0.2f, 1.2f, null);
                    Particles(edge, "mat_smoke", billyDust, count: 18, life: 0.9f, speed: radius * 1.6f, size: 0.9f, shapeRadius: 0.5f, world: true, ring: true);
                    Rocks(at, 6, radius * 0.5f, 0.12f);
                    return null;
            }
            return null;
        }

        /// <summary>A heavy impact into the ground: crater, rock spikes punching up, boulder chunks, cracks, a pink shock ring, a
        /// dust ring and a shake (big: the air slam over its 10 m).</summary>
        static void Smash(Vector3 at, float radius, bool big)
        {
            float k = big ? 1.6f : 1f;
            Burst(at + Vector3.up * 0.5f, "mat_glow", billyHot, 3, 0.2f, 0f, 2.5f * k);
            Flash(at + Vector3.up, billyPink, radius * 2.5f, 0.4f, 7f);
            Ring(at + Vector3.up * 0.25f, billyPink, 0.5f, radius, 0.4f, big ? 1f : 0.6f);
            Ring(at + Vector3.up * 0.25f, billyHot, 0.3f, radius * 0.4f, 0.25f, 0.5f);
            Burst(at, "mat_smoke", billyDust, (int)(22 * k), 1.4f, radius * 1.1f, 2f * k, shapeRadius: 1f, flat: true);
            Burst(at, "mat_smoke", billyDust, (int)(8 * k), 1.2f, 3f, 1.6f * k, shapeRadius: 0.8f, upward: true);
            Burst(at, "mat_spark", billyGrit, (int)(40 * k), 0.9f, 14f, 0.1f, shapeRadius: 0.5f, upward: true, gravity: 2.5f);
            Burst(at, "mat_spark", billyPink, (int)(20 * k), 0.4f, 10f, 0.07f, shapeRadius: 0.4f, upward: true);
            Scorch(at, 1.4f * k, 4f);
            Cracks(at, radius * 0.55f, big ? 10 : 7);
            Spikes(at, 1.6f * k, big ? 9 : 6, 1.1f * k);
            Rocks(at, big ? 14 : 8, radius * 0.6f, 0.22f);
            Shake(at, radius * 3f, big ? 2.2f : 1.4f, big ? 0.45f : 0.3f);
        }

        static void Nail(Vector3 at)
        {
            var built = PropModels.Build("billy_nail_prop");
            if (built == null) return;
            var root = Root(at, 1.4f, null);
            built.Root.transform.SetParent(root.transform, false);
            built.Root.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
            built.Root.AddComponent<NailSink>().Delay = 0.9f;
        }

        // ------------------------------------------------------------------ Rising Ram
        static GameObject BillyRam(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // the wind-up: dirt kicked up by his hoof and a pink glow on the horns
                    var root = Root(at, 0f, follow);
                    var horns = new GameObject("horns"); horns.transform.SetParent(root.transform, false); horns.transform.localPosition = Vector3.up * 2.1f;
                    Particles(horns, "mat_glow", billyPink, rate: 14, life: 0.25f, speed: 0f, size: 0.9f, shapeRadius: 0.2f);
                    Light(horns, billyPink, 4f, 2.5f, Vector3.zero);
                    Particles(root, "mat_smoke", billyDust, rate: 18, life: 0.6f, speed: 1.5f, size: 0.6f, shapeRadius: 0.4f, world: true, upward: true);
                    Particles(root, "mat_spark", billyGrit, rate: 40, life: 0.5f, speed: 3f, size: 0.06f, shapeRadius: 0.3f, world: true, upward: true, gravity: 2f);
                    return root;
                }
                case "loop":
                {
                    // the charge: thin pink horn trails (the camera sits right behind them) and grit sprayed off the ground
                    var root = Root(at, 0f, follow);
                    foreach (var x in new[] { -0.35f, 0.35f })
                    {
                        var horn = new GameObject("horn"); horn.transform.SetParent(root.transform, false); horn.transform.localPosition = new Vector3(x, 2f, 0.4f);
                        Trail(horn, billyPink, 0.18f, 0.2f);
                    }
                    Particles(root, "mat_smoke", billyDust, rate: 30, life: 0.7f, speed: 1.2f, size: 0.7f, shapeRadius: 0.5f, world: true);
                    Particles(root, "mat_spark", billyGrit, rate: 70, life: 0.45f, speed: 4f, size: 0.07f, shapeRadius: 0.5f, world: true, upward: true, gravity: 2f);
                    return root;
                }
                case "hit":
                {
                    // the impact: a white-pink flash, boulders bursting up, cracks racing out and the launch ring on the ground
                    Burst(at + Vector3.up * 1.2f, "mat_glow", billyHot, 3, 0.2f, 0f, 3f);
                    Flash(at + Vector3.up * 1.5f, billyPink, radius * 2.5f, 0.45f, 8f);
                    Ring(at + Vector3.up * 0.25f, billyPink, 0.5f, radius, 0.45f, 1f);
                    Burst(at, "mat_smoke", billyDust, 26, 1.5f, radius * 1.1f, 2.2f, shapeRadius: 1.2f, flat: true);
                    Burst(at, "mat_spark", billyGrit, 45, 1f, 15f, 0.1f, shapeRadius: 0.6f, upward: true, gravity: 2.5f);
                    Burst(at + Vector3.up, "mat_spark", billyPink, 24, 0.4f, 12f, 0.07f, shapeRadius: 0.3f);
                    Cracks(at, radius, 10);
                    Rocks(at, 12, radius * 0.7f, 0.26f);
                    Shake(at, radius * 3f, 2f, 0.4f);
                    var mark = Root(at, 0.9f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(mark.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", billyPink);
                    return null;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ a floating enemy
        static GameObject BillyFloat(string moment, Vector3 at, float seconds, Transform follow)
        {
            if (moment != "loop") return null;
            // grit and pebbles trickling off it and a faint pink glow for as long as it floats
            var root = Root(at, Mathf.Max(0.5f, seconds) + 0.3f, follow);
            Particles(root, "mat_spark", billyGrit, rate: 30, life: 0.8f, speed: 0.5f, size: 0.07f, shapeRadius: 0.6f, world: true, gravity: 2f);
            Particles(root, "mat_smoke", billyDust, rate: 5, life: 0.9f, speed: 0.3f, size: 0.6f, shapeRadius: 0.5f, world: true, gravity: 0.3f);
            Particles(root, "mat_glow", billyMagenta, rate: 5, life: 0.4f, speed: 0f, size: 1.6f, shapeRadius: 0.2f);
            root.AddComponent<StopAfter>().After = Mathf.Max(0.3f, seconds);
            return root;
        }

        // ------------------------------------------------------------------ G.O.A.T.
        static GameObject BillyDodge(string moment, Vector3 at, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    Burst(at, "mat_smoke", billyDust, 6, 0.6f, 2.5f, 0.8f, shapeRadius: 0.4f);
                    Burst(at, "mat_spark", billyPink, 10, 0.25f, 6f, 0.06f);
                    var root = Root(at, 0f, follow);
                    foreach (var x in new[] { -0.3f, 0.3f })
                    {
                        var horn = new GameObject("horn"); horn.transform.SetParent(root.transform, false); horn.transform.localPosition = new Vector3(x, 1f, 0.3f);
                        Trail(horn, billyPink, 0.15f, 0.25f);
                    }
                    return root;
                }
                case "hit":
                    Burst(at, "mat_spark", billyGrit, 12, 0.45f, 8f, 0.07f, gravity: 2f);
                    Burst(at, "mat_glow", billyPink, 2, 0.15f, 0f, 1.2f);
                    Rocks(at, 3, 0.5f, 0.1f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Chain Gang
        static GameObject BillyChainSlam(string moment, Vector3 at, float radius)
        {
            if (moment != "hit") return null;
            // under every chained enemy: a dust column, a crater ring and chunks
            Burst(at, "mat_smoke", billyDust, 14, 1.3f, 3f, 1.6f, shapeRadius: 0.8f, flat: true);
            Burst(at, "mat_smoke", billyDust, 6, 1f, 4f, 1.2f, shapeRadius: 0.4f, upward: true);
            Burst(at, "mat_spark", billyGrit, 20, 0.8f, 10f, 0.08f, shapeRadius: 0.4f, upward: true, gravity: 2.5f);
            Ring(at + Vector3.up * 0.2f, billyPink, 0.3f, radius * 1.5f, 0.3f, 0.5f);
            Scorch(at, radius * 0.5f, 3f);
            Cracks(at, radius, 5);
            Rocks(at, 5, radius, 0.18f);
            Shake(at, 25f, 1.2f, 0.3f);
            return null;
        }

        /// <summary>Chains from the carrier's hand (its muzzle, or its chest), or from another point (from: Bashdown's slammed enemy),
        /// to each victim: Deadlock's chain links (billy_chain_prop) tiled along the line in big links over a bright pink glow line,
        /// a pink glow on each chained enemy, reaching out over throwTime; a dark line when the prop can't be read. Every machine
        /// draws its own.</summary>
        public static GameObject Chains(CharacterBody owner, List<CharacterBody> victims, float throwTime, float life, Transform from = null)
        {
            if (!owner || victims == null || victims.Count == 0) return null;
            var root = Root(owner.corePosition, life, null);
            var fx = root.AddComponent<ChainFx>();
            var hand = Fx.Muzzle(owner);
            fx.From = from ? from : hand ? hand : owner.coreTransform;
            fx.Owner = owner;
            fx.Throw = Mathf.Max(0.05f, throwTime);
            fx.Victims = new List<CharacterBody>(victims);
            fx.Template = ChainTemplate(out fx.LinkLength, out fx.LinkAxis);
            foreach (var v in victims)
            {
                if (!v) continue;
                Burst(v.corePosition, "mat_spark", billyPink, 14, 0.35f, 6f, 0.09f);
                // the bite: a pink glow on the chained enemy for as long as the chain holds
                var bite = Root(v.corePosition, life, v.coreTransform);
                Particles(bite, "mat_glow", billyPink, rate: 10, life: 0.25f, speed: 0f, size: 1.6f + v.radius, shapeRadius: 0.2f);
            }
            if (fx.From) Burst(fx.From.position, "mat_glow", billyPink, 2, 0.25f, 0f, 1.8f);
            return root;
        }

        static GameObject chainTemplate;
        static float chainLength; static Vector3 chainAxis;
        static GameObject ChainTemplate(out float length, out Vector3 axis)
        {
            if (!chainTemplate)
            {
                var built = PropModels.Build("billy_chain_prop");
                if (built != null)
                {
                    chainTemplate = built.Root;
                    chainTemplate.SetActive(false);
                    Object.DontDestroyOnLoad(chainTemplate);
                    var b = new Bounds();
                    bool first = true;
                    foreach (var mf in chainTemplate.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (!mf.sharedMesh) continue;
                        if (first) { b = mf.sharedMesh.bounds; first = false; } else b.Encapsulate(mf.sharedMesh.bounds);
                    }
                    var s = b.size;
                    chainAxis = s.x >= s.y && s.x >= s.z ? Vector3.right : s.y >= s.z ? Vector3.up : Vector3.forward;
                    chainLength = Mathf.Max(0.05f, Vector3.Dot(s, chainAxis));
                }
            }
            length = chainLength; axis = chainAxis;
            return chainTemplate;
        }

        sealed class ChainFx : MonoBehaviour
        {
            public Transform From; public CharacterBody Owner;
            public List<CharacterBody> Victims;
            public GameObject Template; public float LinkLength; public Vector3 LinkAxis;
            public float Throw;
            float age;
            readonly List<List<GameObject>> links = new List<List<GameObject>>();
            readonly List<LineRenderer> lines = new List<LineRenderer>();
            const int MaxLinks = 40;
            const float BigLink = 0.55f;

            void Start()
            {
                for (int i = 0; i < Victims.Count; i++)
                {
                    links.Add(new List<GameObject>());
                    // a thin dark line under the links, the whole chain when the prop is missing
                    // a bright pink line under the links, so each chain reads at a glance (the whole chain when the prop is missing)
                    var lr = Line(transform, Template ? billyPink : new Color(0.25f, 0.24f, 0.24f), Template ? 0.16f : 0.2f, Vector3.zero, Vector3.zero, "mat_tracer");
                    if (!Template) lr.sharedMaterial = Solid(new Color(0.22f, 0.21f, 0.2f, 1f));
                    lr.useWorldSpace = true;
                    lines.Add(lr);
                }
            }

            void LateUpdate()
            {
                age += Time.deltaTime;
                var a = From ? From.position : Owner ? Owner.corePosition : transform.position;
                float reach = Mathf.Clamp01(age / Throw);
                for (int i = 0; i < Victims.Count; i++)
                {
                    var v = Victims[i];
                    var lr = lines[i];
                    if (!v) { lr.enabled = false; foreach (var l in links[i]) if (l) l.SetActive(false); continue; }
                    var b = Vector3.Lerp(a, v.corePosition, reach);
                    lr.SetPosition(0, a); lr.SetPosition(1, b);
                    if (!Template) continue;
                    var d = b - a; float dist = d.magnitude;
                    if (dist < 0.05f) continue;
                    // big links: about 55 cm each (Deadlock's are 17 cm, too thin to see from behind the hero)
                    float seg = BigLink, scale = seg / LinkLength;
                    int n = Mathf.Min(MaxLinks, Mathf.CeilToInt(dist / seg));
                    var list = links[i];
                    while (list.Count < n) { var l = Instantiate(Template, transform); l.SetActive(true); list.Add(l); }
                    var dir = d / dist;
                    for (int k = 0; k < list.Count; k++)
                    {
                        var l = list[k];
                        bool on = k < n;
                        l.SetActive(on);
                        if (!on) continue;
                        l.transform.position = a + dir * (k + 0.5f) * dist / n;
                        l.transform.rotation = Quaternion.AngleAxis(k % 2 == 0 ? 0f : 90f, dir) * Quaternion.FromToRotation(LinkAxis, dir);
                        l.transform.localScale = Vector3.one * scale * Mathf.Min(1f, dist / n / seg);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ stones

        /// <summary>Rock chunks thrown out of an impact: squashed grey-brown boxes on the debris layer (they hit the world only),
        /// tumbling, bouncing and shrinking away.</summary>
        static void Rocks(Vector3 at, int count, float spread, float size)
        {
            for (int i = 0; i < count; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.layer = LayerIndex.debris.intVal;
                piece.transform.position = at + Vector3.up * 0.4f + Random.insideUnitSphere * 0.3f;
                piece.transform.rotation = Random.rotation;
                float s = size * Random.Range(0.6f, 1.5f);
                piece.transform.localScale = new Vector3(s * Random.Range(0.8f, 1.4f), s * Random.Range(0.5f, 1f), s * Random.Range(0.8f, 1.3f));
                var mr = piece.GetComponent<MeshRenderer>();
                mr.sharedMaterial = RockMat(rockColors[i % rockColors.Length]);
                var rb = piece.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                var dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) + 0.6f;
                rb.velocity = dir.normalized * Random.Range(4f, 9f) * Mathf.Max(0.6f, spread * 0.25f + 0.5f);
                rb.angularVelocity = Random.insideUnitSphere * 15f;
                float life = Random.Range(1.8f, 2.8f);
                piece.AddComponent<RockFade>().Over = life;
                Object.Destroy(piece, life);
            }
        }

        /// <summary>A ring of rock spikes punching up out of the ground around a slam, standing a moment and sinking back.</summary>
        static void Spikes(Vector3 at, float ringRadius, int count, float height)
        {
            var root = Root(at, 1.6f, null);
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.Range(-0.2f, 0.2f)) * Mathf.PI * 2f / count;
                var off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ringRadius * Random.Range(0.8f, 1.15f);
                var p = at + off;
                if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var g, 4f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) p = g.point;
                var spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(spike.GetComponent<Collider>());
                spike.transform.SetParent(root.transform, true);
                spike.transform.position = p;
                // leaning out from the centre, twisted a little
                spike.transform.rotation = Quaternion.AngleAxis(Random.Range(15f, 35f), Vector3.Cross(Vector3.up, off.normalized)) * Quaternion.Euler(0f, Random.Range(0f, 90f), 0f);
                spike.GetComponent<MeshRenderer>().sharedMaterial = RockMat(rockColors[i % rockColors.Length]);
                var r = spike.AddComponent<SpikeRise>();
                r.Size = new Vector3(0.45f, height * Random.Range(0.8f, 1.3f), 0.45f) * Random.Range(0.8f, 1.2f);
            }
        }

        sealed class SpikeRise : MonoBehaviour
        {
            public Vector3 Size;
            float age;
            const float Up = 0.1f, Stand = 0.8f, Down = 0.5f;
            void Update()
            {
                age += Time.deltaTime;
                float k = age < Up ? age / Up : age < Up + Stand ? 1f : 1f - (age - Up - Stand) / Down;
                k = Mathf.Clamp01(k);
                // grows from its foot: the cube's pivot is its centre, so lift it by half its height
                if (basePos == null) basePos = transform.position;
                transform.localScale = new Vector3(Size.x, Size.y * k, Size.z);
                transform.position = basePos.Value + transform.up * Size.y * k * 0.5f;
            }
            Vector3? basePos;
        }

        /// <summary>A rock chunk shrinking away over its life, keeping its own shape.</summary>
        sealed class RockFade : MonoBehaviour
        {
            public float Over = 2f;
            float age; Vector3 size;
            void Start() => size = transform.localScale;
            void Update() { age += Time.deltaTime; float k = Mathf.Clamp01(age / Over); transform.localScale = size * (k < 0.6f ? 1f : Mathf.Lerp(1f, 0.05f, (k - 0.6f) / 0.4f)); }
        }

        /// <summary>A prop (Bashdown's nail) that stays put, then sinks into the ground.</summary>
        sealed class NailSink : MonoBehaviour
        {
            public float Delay = 1f;
            float age;
            void Update()
            {
                age += Time.deltaTime;
                if (age > Delay) transform.position += Vector3.down * Time.deltaTime * 2f;
            }
        }
    }
}
