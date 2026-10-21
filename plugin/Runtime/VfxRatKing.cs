using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Rat King's visuals (effects rows ratking_*), built like the others from RoR2's materials, with Deadlock's own
    /// grenade and banner models (props) where Deadlock has one. Colours: his crown's gold, the gun's brass, sewer grime.</summary>
    public static partial class Vfx
    {
        static readonly Color ratGold = new Color(0.79f, 0.64f, 0.15f), ratBrass = new Color(1f, 0.78f, 0.38f),
            ratGrime = new Color(0.36f, 0.31f, 0.24f), ratSewer = new Color(0.45f, 0.62f, 0.2f), ratBlood = new Color(0.75f, 0.12f, 0.1f),
            ratSmoke = new Color(0.42f, 0.4f, 0.37f, 0.8f);

        static GameObject RatKing(string id, string moment, Vector3 at, float radius, Transform follow)
        {
            switch (id)
            {
                case "ratking_gun": return RatKingGun(moment, at, follow);
                case "ratking_scrap": return RatKingScrap(moment, at, radius, follow);
                case "ratking_ring": return RatKingRing(moment, at, radius);
                case "ratking_banner": return RatKingBanner(moment, at, radius, follow);
                case "ratking_swarm": return RatKingSwarm(moment, at);
            }
            return null;
        }

        // ------------------------------------------------------------------ Sovereign's Decree
        static GameObject RatKingGun(string moment, Vector3 at, Transform follow)
        {
            switch (moment)
            {
                case "muzzle":
                    // a brass flash and a puff of grey smoke off the barrel, sparks forward
                    Burst(at, "mat_glow", ratBrass, 2, 0.08f, 0f, 0.7f);
                    Burst(at, "mat_smoke", ratSmoke, 3, 0.5f, 1.2f, 0.45f, shapeRadius: 0.1f);
                    Burst(at, "mat_spark", ratBrass, 5, 0.15f, 9f, 0.05f);
                    return null;
                case "hit":
                    Burst(at, "mat_spark", ratBrass, 4, 0.2f, 5f, 0.05f, gravity: 1.5f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Scrap Grenade
        static GameObject RatKingScrap(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "aim":
                {
                    // the arc line from his hand to the first impact, and a ring the size of the first blast where it lands
                    var root = Root(at, 0f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", ratGold);
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.3f;
                    Particles(edge, "mat_spark", ratBrass, rate: 50, life: 0.35f, speed: 1.5f, size: 0.06f, shapeRadius: radius, world: true, ring: true);
                    var arc = Line(root.transform, ratBrass, 0.1f, Vector3.zero, Vector3.zero);
                    arc.name = "arc"; arc.useWorldSpace = true;
                    arc.widthCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.15f, 1f), new Keyframe(1, 1f));
                    return root;
                }
                case "loop":
                {
                    // Deadlock's grenade tumbling through the air, a hot glow and sparks trailing off it
                    var root = Root(at, 0f, follow);
                    var g = PropModels.Build("ratking_grenade_prop");
                    if (g != null)
                    {
                        g.Root.transform.SetParent(root.transform, false);
                        g.Root.transform.localScale = Vector3.one * ModelBuilder.GlobalScale;
                        var tumble = new GameObject("tumble"); tumble.transform.SetParent(root.transform, false);
                        g.Root.transform.SetParent(tumble.transform, false);
                        tumble.AddComponent<Tumble>().DegPerSec = 420f;
                    }
                    else
                    {
                        var ball = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Object.Destroy(ball.GetComponent<Collider>());
                        ball.transform.SetParent(root.transform, false);
                        ball.transform.localScale = Vector3.one * 0.45f;
                        ball.GetComponent<MeshRenderer>().sharedMaterial = new Material(ModelBuilder.Template) { color = ratGrime };
                        ball.AddComponent<Tumble>().DegPerSec = 420f;
                    }
                    Particles(root, "mat_spark", ratBrass, rate: 40, life: 0.3f, speed: 2f, size: 0.05f, world: true);
                    Particles(root, "mat_glow", new Color(1f, 0.55f, 0.2f), rate: 12, life: 0.15f, speed: 0f, size: 0.6f);
                    Light(root, new Color(1f, 0.6f, 0.25f), 4f, 1.5f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "sit":
                {
                    // the last landing: it sits glowing hotter and rattling harder until the big blast
                    var root = Root(at, 1.5f, follow);
                    var l = Light(root, new Color(1f, 0.45f, 0.15f), radius * 0.8f, 1f, Vector3.up * 0.5f);
                    var grow = l.gameObject.AddComponent<GrowLight>(); grow.To = 9f; grow.Over = 0.8f;
                    Particles(root, "mat_spark", new Color(1f, 0.6f, 0.2f), rate: 90, life: 0.35f, speed: 4f, size: 0.06f, world: true, upward: true, gravity: 1.5f);
                    Particles(root, "mat_glow", new Color(1f, 0.4f, 0.1f), rate: 20, life: 0.2f, speed: 0f, size: 1.4f);
                    if (follow) { var t = follow.GetComponentInChildren<Tumble>(); if (t) t.Rattle = true; }
                    return root;
                }
                case "hit":
                    ScrapBlast(at, radius, false);
                    return null;
                case "end":
                    ScrapBlast(at, radius, true);
                    return null;
            }
            return null;
        }

        /// <summary>One blast of the Scrap Grenade: bolts, gears and sheet metal flying out and skittering on the ground, sparks,
        /// dust, a smoke puff, a scorch mark and a shake by distance (the final one bigger and deeper).</summary>
        static void ScrapBlast(Vector3 at, float radius, bool big)
        {
            float k = big ? 1.6f : 1f;
            Burst(at, "mat_glow", new Color(1f, 0.6f, 0.25f), 3, 0.18f, 0f, radius * 0.7f);
            Burst(at, "mat_fire_puff", new Color(1f, 0.55f, 0.2f), (int)(10 * k), 0.4f, radius * 1.2f, 1.2f * k, shapeRadius: 0.4f, upward: true);
            Burst(at, "mat_spark", ratBrass, (int)(45 * k), 0.9f, radius * 2.2f, 0.09f, shapeRadius: 0.3f, upward: true, gravity: 2f);
            Burst(at, "mat_smoke", ratSmoke, (int)(10 * k), 1.6f, radius * 0.5f, 2.2f * k, shapeRadius: radius * 0.3f, upward: true);
            Burst(at, "mat_smoke", new Color(0.55f, 0.48f, 0.38f, 0.7f), (int)(10 * k), 1.0f, radius * 1.1f, 1.4f * k, shapeRadius: 0.5f, flat: true);
            Ring(at + Vector3.up * 0.2f, ratBrass, 0.5f, radius, 0.3f, big ? 0.9f : 0.5f);
            Flash(at + Vector3.up, new Color(1f, 0.6f, 0.25f), radius * 3f, big ? 0.45f : 0.3f, big ? 9f : 6f);
            Scorch(at, radius * 0.45f, big ? 5f : 3.5f);
            Scrap(at, big ? 14 : 8, radius);
            Shake(at, radius * 4f, big ? 3.5f : 1.6f, big ? 0.55f : 0.3f);
        }

        /// <summary>A dark burn on the ground that fades.</summary>
        static void Scorch(Vector3 at, float radius, float life)
        {
            if (!Physics.Raycast(at + Vector3.up, Vector3.down, out var g, 3f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return;
            var root = Root(g.point + g.normal * 0.03f, life, null);
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(disc.GetComponent<Collider>());
            disc.transform.SetParent(root.transform, false);
            disc.transform.rotation = Quaternion.FromToRotation(Vector3.up, g.normal);
            disc.transform.localScale = new Vector3(radius * 2f, 0.005f, radius * 2f);
            var mr = disc.GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Solid(new Color(0.06f, 0.05f, 0.04f, 0.55f)));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            disc.AddComponent<FadeMaterial>().Life = life;
        }

        /// <summary>Bits of scrap (bolts, gears, sheet metal: small boxes and flat plates) thrown out of a blast; they bounce
        /// and skitter on the ground (debris layer: they hit the world only), then vanish.</summary>
        static void Scrap(Vector3 at, int count, float radius)
        {
            var metal = new[] { new Color(0.45f, 0.43f, 0.4f), new Color(0.6f, 0.5f, 0.3f), new Color(0.3f, 0.28f, 0.26f) };
            for (int i = 0; i < count; i++)
            {
                var piece = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                piece.layer = LayerIndex.debris.intVal;
                piece.transform.position = at + Vector3.up * 0.4f + Random.insideUnitSphere * 0.3f;
                piece.transform.rotation = Random.rotation;
                // a plate, a bolt or a gear-ish disc
                piece.transform.localScale = i % 3 == 1 ? new Vector3(0.35f, 0.03f, 0.25f) : i % 3 == 0 ? new Vector3(0.18f, 0.03f, 0.18f) : new Vector3(0.07f, 0.07f, 0.2f);
                var mr = piece.GetComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(ModelBuilder.Template) { color = metal[i % metal.Length] };
                var rb = piece.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                var dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) + 0.4f;
                rb.velocity = dir.normalized * Random.Range(0.6f, 1.2f) * radius * 1.4f;
                rb.angularVelocity = Random.insideUnitSphere * 20f;
                Object.Destroy(piece, Random.Range(2f, 3f));
            }
        }

        // ------------------------------------------------------------------ Royal Pestments
        static GameObject RatKingRing(string moment, Vector3 at, float radius)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the ring's edge: a faint area dome where it meets the ground and dust kicked up along it (the rats themselves
                    // are the crowd's)
                    var root = Root(at, 0f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", new Color(0.62f, 0.5f, 0.3f));
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.2f;
                    Particles(edge, "mat_smoke", new Color(0.55f, 0.47f, 0.36f, 0.45f), rate: 30, life: 0.8f, speed: 0.6f, size: 0.7f, shapeRadius: radius, world: true, ring: true);
                    return root;
                }
                case "hit":
                    // a rat's nip on an enemy
                    Burst(at, "mat_spark", ratBlood, 5, 0.25f, 3f, 0.06f, gravity: 1f);
                    return null;
                case "end":
                    Burst(at, "mat_smoke", new Color(0.55f, 0.47f, 0.36f, 0.6f), 16, 1f, radius * 0.8f, 1f, shapeRadius: radius * 0.6f, flat: true);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Rule, Ratannia!
        static GameObject RatKingBanner(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                    // the banner raised: a gold glint above him
                    Burst(at + Vector3.up * 1.5f, "mat_glow", ratGold, 3, 0.35f, 0f, 1.4f);
                    Burst(at + Vector3.up * 1.5f, "mat_spark", ratBrass, 14, 0.4f, 5f, 0.07f);
                    return null;
                case "loop":
                {
                    // the leap: a thin gold streak behind him (thin: the camera sits right behind it)
                    var root = Root(at, 0f, follow);
                    root.transform.localPosition = Vector3.up;
                    Trail(root, ratGold, 0.35f, 0.3f);
                    Particles(root, "mat_spark", ratBrass, rate: 40, life: 0.4f, speed: 1f, size: 0.06f, shapeRadius: 0.5f, world: true);
                    return root;
                }
                case "hit":
                {
                    // the slam: dust racing out over the outer ring, cracks in the ground, a gold flash and a heavy shake
                    Ring(at + Vector3.up * 0.3f, ratGold, 0.5f, radius, 0.45f, 1.2f);
                    Ring(at + Vector3.up * 0.3f, ratBrass, 0.5f, radius * 0.3f, 0.3f, 0.7f);
                    Burst(at, "mat_smoke", new Color(0.55f, 0.48f, 0.38f, 0.75f), 36, 1.5f, radius * 0.9f, 2.6f, shapeRadius: 1.5f, flat: true);
                    Burst(at, "mat_spark", ratBrass, 50, 1f, 16f, 0.12f, gravity: 2.5f, upward: true);
                    Burst(at, "mat_glow", ratGold, 4, 0.35f, 0f, radius * 0.5f);
                    Cracks(at, radius * 0.45f, 9);
                    Flash(at + Vector3.up * 2f, ratGold, radius * 2f, 0.5f, 8f);
                    Shake(at, radius * 3f, 3f, 0.6f);
                    // the 14 m ring of the stun, briefly, as a dome on the ground
                    var mark = Root(at, 0.9f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(mark.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", ratGold);
                    return null;
                }
                case "banner":
                {
                    // on the planted banner: a gold light at its top and sewer slime dripping around its foot
                    var root = Root(at, 0f, follow);
                    Light(root, ratGold, 9f, 2.5f, Vector3.up * 5f).gameObject.AddComponent<Flicker>();
                    Particles(root, "mat_glow", ratGold, rate: 4, life: 1f, speed: 0.2f, size: 0.8f, shapeRadius: 0.3f);
                    Particles(root, "mat_spark", ratSewer, rate: 14, life: 1.2f, speed: 0.8f, size: 0.12f, shapeRadius: 2.5f, world: true, flat: true, gravity: 0.5f);
                    return root;
                }
            }
            return null;
        }

        /// <summary>Dark cracks racing out over the ground from a slam.</summary>
        static void Cracks(Vector3 at, float length, int count)
        {
            var root = Root(at, 3f, null);
            for (int i = 0; i < count; i++)
            {
                var dir = Quaternion.Euler(0, i * 360f / count + Random.Range(-12f, 12f), 0) * Vector3.forward;
                var pts = new List<Vector3>();
                var p = at;
                int seg = 5;
                for (int s = 0; s <= seg; s++)
                {
                    var q = at + dir * length * Random.Range(0.6f, 1f) * s / seg + Vector3.Cross(Vector3.up, dir) * Random.Range(-0.3f, 0.3f) * (s > 0 ? 1 : 0);
                    if (Physics.Raycast(q + Vector3.up * 2f, Vector3.down, out var g, 4f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) q = g.point + Vector3.up * 0.05f;
                    pts.Add(q);
                }
                var go = new GameObject("crack"); go.transform.SetParent(root.transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.sharedMaterial = Solid(new Color(0.08f, 0.06f, 0.05f, 0.85f));
                lr.widthCurve = new AnimationCurve(new Keyframe(0, 0.35f), new Keyframe(1, 0.04f));
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());
                go.AddComponent<FadeLine>().Life = 3f;
            }
        }

        // ------------------------------------------------------------------ Rat Swarm
        static GameObject RatKingSwarm(string moment, Vector3 at)
        {
            switch (moment)
            {
                case "cast":
                    // the rats pour out: a puff of dark sewer dust at the carrier's feet
                    Burst(at, "mat_smoke", new Color(0.25f, 0.22f, 0.18f, 0.7f), 14, 0.9f, 3f, 1.2f, shapeRadius: 1f, flat: true);
                    Burst(at, "mat_spark", ratSewer, 18, 0.6f, 4f, 0.08f, upward: true, gravity: 1f);
                    return null;
                case "hit":
                    // one bite
                    Burst(at, "mat_spark", ratBlood, 3, 0.2f, 2.5f, 0.05f, gravity: 1f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ small behaviours

        /// <summary>The grenade tumbling in flight; rattling (a fast jitter) once it sits for the final blast.</summary>
        sealed class Tumble : MonoBehaviour
        {
            public float DegPerSec;
            public bool Rattle;
            Vector3 axis;
            void Awake() => axis = Random.onUnitSphere;
            void Update()
            {
                if (Rattle)
                {
                    transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(-12f, 12f), Random.Range(-12f, 12f));
                    transform.localPosition = Random.insideUnitSphere * 0.05f;
                }
                else transform.Rotate(axis, DegPerSec * Time.deltaTime, Space.Self);
            }
        }

        sealed class FadeMaterial : MonoBehaviour
        {
            public float Life = 3f;
            float age; Material m; Color c;
            void Awake() { var r = GetComponent<Renderer>(); m = r ? r.material : null; if (m) c = m.color; }
            void Update() { age += Time.deltaTime; if (m) { var k = c; k.a = c.a * Mathf.Clamp01(1f - age / Life); m.color = k; } }
        }
    }
}
