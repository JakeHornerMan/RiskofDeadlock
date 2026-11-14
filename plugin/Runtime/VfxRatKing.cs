using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Rat King's visuals (effects rows ratking_*), rebuilt 2026-11 from Deadlock's recipes
    /// (docs/VfxRebuild/ratkingVfxRebuild.md), with Deadlock's own grenade and banner models (props). Deadlock's Rat King is warm
    /// amber and cream over grey scrap and sewer dust: orange sparks cooling to red, cream rays off the banner.</summary>
    public static partial class Vfx
    {
        static readonly Color ratGold = new Color(0.79f, 0.64f, 0.15f), ratBrass = new Color(1f, 0.78f, 0.38f),
            ratGrime = new Color(0.36f, 0.31f, 0.24f), ratSewer = new Color(0.45f, 0.62f, 0.2f), ratBlood = new Color(0.75f, 0.12f, 0.1f),
            ratSmoke = new Color(0.42f, 0.4f, 0.37f, 0.8f);
        // particles/abilities/ratking: amber #F4A142 / #D5852A / #FFAB52, hot #FFB340, sparks #F9BF89 > #F89760 > #C7412A, banner rays and
        // ring #FDD8A0 > #765028, grenade ground #FFF0DC / #FF902F / #A75B3A, shards #D5D5D5 / #7D99FF, dust #8D7F6E / #51493D, bite #F84909
        static readonly Color ratAmber = Hex("F4A142"), ratDeepAmber = Hex("D5852A"), ratHot = Hex("FFB340"), ratSparkA = Hex("F9BF89"), ratSparkB = Hex("C7412A"),
            ratCream = Hex("FDD8A0"), ratDust = Hex("8D7F6E", 0.7f), ratDustDark = Hex("51493D", 0.4f), ratBite = Hex("F84909");

        static void RatSparks(Vector3 at, int count, float speed, float life = 0.7f, float angle = 70f)
        {
            Puff(at, new P { Mat = Mat("mat_spark", ratHot), Count = count, LifeMin = life * 0.35f, LifeMax = life, SpeedMin = speed * 0.3f, SpeedMax = speed, SizeMin = 0.05f, SizeMax = 0.1f,
                             A = ratSparkA, B = ratSparkB, Shape = Shape.Up, Angle = angle, Radius = 0.2f, Stretch = 3f, Gravity = 2f, Max = count + 2 });
        }


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
                    // ratking_muzzle_flash: a ragged amber flash, a rusty shock ring, cream embers, grey smoke on some shots
                    if (!Every("ratking_mz", 0.07f)) return null;
                    QuadAt(at, Glow("fx_muzzle", ratHot), 0.6f, 1.6f, 0.09f, Face.Camera, 0f, 0f, 0.25f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_ring_burst", Hex("BC6B2D")), 0.3f, 1.5f, 0.12f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    if (Rich)
                    {
                        Puff(at, new P { Mat = Glow("fx_fleks", Hex("F6E2B8")), Count = 4, LifeMin = 0.3f, LifeMax = 0.9f, SpeedMin = 2f, SpeedMax = 7f, SizeMin = 0.05f, SizeMax = 0.1f,
                                         A = Hex("F6E2B8"), B = Hex("F48014"), Gravity = 0.8f, Drag = 2f, TilesX = 8, TilesY = 8, SpinMin = -6f, SpinMax = 6f, Max = 5 });
                        if (Every("ratking_mz_smoke", 0.2f))
                            Puff(at, new P { Mat = Soft("fx_cloud", Color.white), Count = 2, LifeMin = 0.4f, LifeMax = 0.7f, SpeedMin = 0.5f, SpeedMax = 1.5f, SizeMin = 0.4f, SizeMax = 0.6f,
                                             A = Hex("9C9C9C", 0.4f), B = Hex("606060", 0.15f), Size0 = 0.5f, Size1 = 1.6f, Drag = 2f, Max = 3 });
                    }
                    return null;
                case "hit":
                    if (!Every("ratking_hit", 0.06f)) return null;
                    QuadAt(at, Glow("fx_star", Hex("FFE8C8")), 0.3f, 0.9f, 0.1f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    RatSparks(at, 5, 6f, 0.35f);
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
                    Quad(edge, Glow("fx_ring_soft", ratDeepAmber), radius * 2.15f, radius * 2.15f, 1f, Face.Ground, 30f, loop: true);
                    Emit(edge, new P { Mat = Mat("mat_spark", ratHot), Rate = 18, LifeMin = 0.3f, LifeMax = 0.4f, SpeedMin = 1f, SpeedMax = 1.8f, SizeMin = 0.05f, SizeMax = 0.07f,
                                       A = ratSparkA, B = ratSparkB, Shape = Shape.Ring, Radius = radius, Max = 10 }, "sparks");
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
                    // ratking_scrap_grenade_clock_projectile: an orange ribbon cooling to brown, sparks shaken off it, a hot glow
                    Ribbon(root, Glow("fx_beam", Hex("FF993F")), Hex("FF993F"), Hex("3D2013"), 0.35f, 0.4f);
                    Emit(root, new P { Mat = Mat("mat_spark", ratHot), Rate = 26, LifeMin = 0.3f, LifeMax = 0.7f, SpeedMin = 1f, SpeedMax = 2.5f, SizeMin = 0.04f, SizeMax = 0.07f,
                                       A = Hex("FFBB74"), B = Hex("F23E0D"), Stretch = 2f, Gravity = 1f, Max = 16 }, "sparks");
                    Quad(root, Glow("fx_glow", new Color(1f, 0.55f, 0.2f, 0.8f)), 0.8f, 0.8f, 1f, Face.Camera, loop: true);
                    Light(root, new Color(1f, 0.6f, 0.25f), 4f, 1.5f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "sit":
                {
                    // the last landing: it sits glowing hotter and rattling harder until the big blast
                    var root = Root(at, 1.5f, follow);
                    var l = Light(root, new Color(1f, 0.45f, 0.15f), radius * 0.8f, 1f, Vector3.up * 0.5f);
                    var grow = l.gameObject.AddComponent<GrowLight>(); grow.To = 9f; grow.Over = 0.8f;
                    Emit(root, new P { Mat = Mat("mat_spark", ratHot), Rate = 50, LifeMin = 0.25f, LifeMax = 0.4f, SpeedMin = 2f, SpeedMax = 4.5f, SizeMin = 0.05f, SizeMax = 0.07f,
                                       A = ratSparkA, B = ratSparkB, Shape = Shape.Up, Angle = 50f, Stretch = 2.5f, Gravity = 1.5f, Max = 22 }, "sparks");
                    Quad(root, Glow("fx_glow", new Color(1f, 0.4f, 0.1f, 0.8f)), 1.6f, 1.6f, 1f, Face.Camera, loop: true);
                    Quad(root, Glow("fx_ring_spiral", new Color(1f, 0.56f, 0.18f, 0.7f)), 2.4f, 2.4f, 1f, Face.Ground, -400f, offset: Vector3.up * 0.1f, loop: true);
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

        /// <summary>One blast of the Scrap Grenade (ratking_scrap_grenade_bounce / _explode): a hot flash, rings of light racing over
        /// the ground, sparks cooling from cream to red, shards of metal, scrap skittering away, sewer dust, a scorch, a shake.</summary>
        static void ScrapBlast(Vector3 at, float radius, bool big)
        {
            float k = big ? 1.6f : 1f, fl = Mathf.Min(radius, 8f);
            var g = Ground(at);
            QuadAt(at + Vector3.up * 0.5f, Glow("fx_glow", new Color(1f, 0.6f, 0.25f, 0.8f)), fl * 0.4f, fl * (big ? 1.5f : 1.1f), 0.2f, Face.Camera, 0f, 0f, 0.2f);
            if (big) QuadAt(at + Vector3.up * 0.5f, Glow("fx_star", Hex("FFF0DC", 0.8f)), fl * 0.3f, fl * 1.1f, 0.16f, Face.Camera, 0f, 0f, 0.2f, Random.Range(0f, 360f));
            QuadAt(g, Glow("fx_ring", Hex("FF902F")), 0.6f, radius * 2f, big ? 0.4f : 0.3f, Face.Ground, 0f, 0.04f, 0.3f);
            QuadAt(g, Glow("fx_ring_burst", Hex("FFF0DC", 0.8f)), 0.5f, radius * 1.3f, 0.25f, Face.Ground, 0f, 0.04f, 0.3f, Random.Range(0f, 360f));
            QuadAt(g, Soft("fx_puff", Hex("121212", 0.7f)), radius * 0.7f, radius * 0.95f, big ? 5f : 3.5f, Face.Ground, 0f, 0.03f, 0.7f, Random.Range(0f, 360f));
            Burst(at, "mat_fire_puff", new Color(1f, 0.55f, 0.2f), N((int)(6 * k)), 0.4f, radius * 1.2f, 1.2f * k, shapeRadius: 0.4f, upward: true);
            RatSparks(at + Vector3.up * 0.2f, (int)(26 * k), radius * 2.2f, 0.9f);
            Puff(at + Vector3.up * 0.3f, new P { Mat = Soft("fx_fleks", Color.white), Count = (int)(10 * k), LifeMin = 0.5f, LifeMax = 1f, SpeedMin = radius * 0.6f, SpeedMax = radius * 1.8f,
                                                 SizeMin = 0.1f, SizeMax = 0.26f, A = Hex("D5D5D5"), B = Hex("7D99FF"), Shape = Shape.Up, Angle = 60f, Gravity = 2.4f, FadeIn = 0.02f,
                                                 FadeOutFrom = 0.8f, TilesX = 8, TilesY = 8, SpinMin = -9f, SpinMax = 9f, Max = (int)(10 * k) + 2 });
            DustPuffs(at + Vector3.up * 0.3f, (int)(7 * k), 2f * k, radius * 0.9f, 1.5f, Hex("AC9371", 0.7f), Hex("535353", 0.4f), true, 0.6f);
            if (big) Flash(at + Vector3.up, new Color(1f, 0.6f, 0.25f), radius * 3f, 0.45f, 7f);
            else Glint(at + Vector3.up, new Color(1f, 0.6f, 0.25f), radius * 3f, 0.3f, 5f);
            Scrap(at, big ? 10 : 6, radius);
            Shake(at, radius * 4f, big ? 3.5f : 1.6f, big ? 0.55f : 0.3f);
        }

        /// <summary>A dark burn on the ground that fades (one soft blotch laid on the slope).</summary>
        static void Scorch(Vector3 at, float radius, float life)
        {
            QuadAt(Ground(at), Soft("fx_puff", new Color(0.06f, 0.05f, 0.04f, 0.6f)), radius * 1.8f, radius * 2.2f, life, Face.Ground, 0f, 0.03f, 0.7f, Random.Range(0f, 360f));
        }

        /// <summary>Bits of scrap (bolts, gears, sheet metal: small boxes and flat plates) thrown out of a blast; they bounce
        /// and skitter on the ground (debris layer: they hit the world only), then vanish.</summary>
        static Material[] scrapMats;
        static void Scrap(Vector3 at, int count, float radius)
        {
            if (scrapMats == null)
            {
                var metal = new[] { new Color(0.45f, 0.43f, 0.4f), new Color(0.6f, 0.5f, 0.3f), new Color(0.3f, 0.28f, 0.26f) };
                scrapMats = new Material[metal.Length];
                for (int m = 0; m < metal.Length; m++) scrapMats[m] = new Material(ModelBuilder.Template) { color = metal[m] };
            }
            for (int i = 0; i < count; i++)
            {
                var piece = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                piece.layer = LayerIndex.debris.intVal;
                piece.transform.position = at + Vector3.up * 0.4f + Random.insideUnitSphere * 0.3f;
                piece.transform.rotation = Random.rotation;
                // a plate, a bolt or a gear-ish disc
                piece.transform.localScale = i % 3 == 1 ? new Vector3(0.35f, 0.03f, 0.25f) : i % 3 == 0 ? new Vector3(0.18f, 0.03f, 0.18f) : new Vector3(0.07f, 0.07f, 0.2f);
                var mr = piece.GetComponent<MeshRenderer>();
                mr.sharedMaterial = scrapMats[i % scrapMats.Length];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
                    // the ring's edge (ratking_ratswarm_aoe / _debuff_dust): a faint area dome where it meets the ground, an amber ring
                    // turning on it and sewer dust kicked up along it (the rats themselves are the crowd's)
                    var root = Root(at, 0f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", new Color(0.62f, 0.5f, 0.3f));
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.2f;
                    Quad(edge, Glow("fx_ring_soft", new Color(ratDeepAmber.r, ratDeepAmber.g, ratDeepAmber.b, 0.5f)), radius * 2.1f, radius * 2.1f, 1f, Face.Ground, 25f, loop: true);
                    Emit(edge, new P { Mat = Soft("fx_cloud", Color.white), Rate = 10, LifeMin = 0.8f, LifeMax = 1.2f, SpeedMin = 0.3f, SpeedMax = 0.8f, SizeMin = 0.8f, SizeMax = 1.3f,
                                       A = ratDust, B = ratDustDark, Shape = Shape.Ring, Radius = radius, Size0 = 0.5f, Size1 = 1.4f, FadeIn = 0.15f, FadeOutFrom = 0.4f,
                                       SpinMin = -0.5f, SpinMax = 0.5f, Max = 14 }, "dust");
                    return root;
                }
                case "hit":
                    // a rat's nip on an enemy (ratking_nibble_debuff): a small orange-red flash and a few drops
                    if (!Every("ratking_nip", 0.05f)) return null;
                    QuadAt(at, Glow("fx_star", ratBite), 0.2f, 0.8f, 0.1f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Burst(at, "mat_spark", ratBlood, 4, 0.25f, 3f, 0.06f, gravity: 1f);
                    return null;
                case "end":
                    DustPuffs(at + Vector3.up * 0.2f, 8, 1.4f, radius * 0.7f, 1f, ratDust, ratDustDark, true, radius * 0.6f);
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
                    // the banner raised (ratking_ratshield_start): a cream star and an amber burst ring above him, sparks
                    QuadAt(at + Vector3.up * 1.5f, Glow("fx_star", ratCream), 0.5f, 2.4f, 0.3f, Face.Camera, 0f, 0f, 0.3f);
                    QuadAt(at + Vector3.up * 1.5f, Glow("fx_ring_burst", ratDeepAmber), 0.5f, 3f, 0.3f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    RatSparks(at + Vector3.up * 1.5f, 10, 5f, 0.5f, 85f);
                    return null;
                case "loop":
                {
                    // the leap: a thin amber ribbon behind him (thin: the camera sits right behind it)
                    var root = Root(at, 0f, follow);
                    root.transform.localPosition = Vector3.up;
                    Ribbon(root, Glow("fx_beam", ratAmber), ratCream, Hex("765028"), 0.35f, 0.3f);
                    Emit(root, new P { Mat = Mat("mat_spark", ratHot), Rate = 24, LifeMin = 0.3f, LifeMax = 0.45f, SpeedMin = 0.5f, SpeedMax = 1.5f, SizeMin = 0.05f, SizeMax = 0.07f,
                                       A = ratSparkA, B = ratSparkB, Radius = 0.5f, Max = 12 }, "sparks");
                    return root;
                }
                case "hit":
                {
                    // the slam (ratking_anim_flag_plant): cream and amber rings racing out to the stun's reach, rays of light standing
                    // round the banner, sewer dust rolling out, sparks, cracks in the ground, a flash and a heavy shake
                    var g = Ground(at);
                    float d = radius * 2f, fl = Mathf.Min(radius, 9f);
                    QuadAt(g, Glow("fx_ring", ratCream), 1f, d * 1.02f, 0.45f, Face.Ground, 0f, 0.03f, 0.3f);
                    QuadAt(g, Glow("fx_ring_thick", ratDeepAmber), 0.8f, d * 0.6f, 0.4f, Face.Ground, 0f, 0.03f, 0.3f, 0f, 0.04f);
                    QuadAt(g, Glow("fx_ring_burst", ratAmber), 0.6f, d * 0.35f, 0.3f, Face.Ground, 0f, 0.03f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(at + Vector3.up * 1.5f, Glow("fx_glow", new Color(ratCream.r, ratCream.g, ratCream.b, 0.75f)), fl * 0.4f, fl * 1.3f, 0.3f, Face.Camera, 0f, 0f, 0.2f);
                    for (int i = 0; i < (Rich ? 6 : 3); i++)
                    {
                        var o = Quaternion.Euler(0, i * 60f + Random.Range(-15f, 15f), 0) * Vector3.forward * Random.Range(0.8f, 2.2f);
                        Beam(at + o + Vector3.up * 0.2f, at + o * 1.6f + Vector3.up * Random.Range(5f, 8f), Glow("fx_beam", ratAmber), Hex("FFC98A", 0.8f), 0.26f, Random.Range(0.4f, 0.6f));
                    }
                    DustPuffs(at + Vector3.up * 0.3f, 16, 2.6f, radius * 0.9f, 1.6f, ratDust, ratDustDark, true, 1.5f);
                    RatSparks(at + Vector3.up * 0.3f, N(30), 16f, 1f, 60f);
                    Cracks(at, radius * 0.45f, 9);
                    Flash(at + Vector3.up * 2f, ratAmber, radius * 2f, 0.5f, 6f);
                    Shake(at, radius * 3f, 3f, 0.6f);
                    // the 14 m ring of the stun, briefly, as a dome on the ground
                    var mark = Root(at, 0.9f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(mark.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", ratAmber);
                    return null;
                }
                case "banner":
                {
                    // on the planted banner (ratking_ratshield_ambient_rays): an amber light and a soft glow at its top, slow rays of
                    // light rising off it, sewer slime dripping around its foot
                    var root = Root(at, 0f, follow);
                    Light(root, ratAmber, 9f, 2.5f, Vector3.up * 5f).gameObject.AddComponent<Flicker>();
                    Quad(root, Glow("fx_glow_soft", new Color(ratAmber.r, ratAmber.g, ratAmber.b, 0.6f)), 2.2f, 2.2f, 1f, Face.Camera, offset: Vector3.up * 5f, loop: true);
                    Emit(root, new P { Mat = Glow("fx_flare", Hex("FFC17F")), Rate = 3, LifeMin = 1f, LifeMax = 2f, SizeMin = 0.5f, SizeMax = 1f, A = Hex("FFAB36"), B = Hex("96601F"),
                                       Radius = 0.6f, Velocity = Vector3.up * 0.8f, FadeIn = 0.3f, FadeOutFrom = 0.5f, World = false, Max = 8 }, "rays").transform.localPosition = Vector3.up * 4f;
                    Particles(root, "mat_spark", ratSewer, rate: 8, life: 1.2f, speed: 0.8f, size: 0.12f, shapeRadius: 2.5f, world: true, flat: true, gravity: 0.5f);
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
                    // the rats pour out (ratking_nibble_cast): an amber ring snapping out over the ground, sewer dust at the carrier's feet, sparks
                    QuadAt(Ground(at), Glow("fx_ring_burst", ratAmber), 0.6f, 6f, 0.35f, Face.Ground, 0f, 0.04f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(Ground(at), Soft("fx_puff", Hex("070606", 0.6f)), 2f, 3.6f, 1.5f, Face.Ground, 0f, 0.05f, 0.6f, Random.Range(0f, 360f));
                    DustPuffs(at + Vector3.up * 0.2f, 8, 1.5f, 3f, 0.9f, ratDust, ratDustDark, true, 1f);
                    RatSparks(at + Vector3.up * 0.2f, 10, 4f, 0.6f);
                    return null;
                case "hit":
                    // one bite
                    if (!Every("ratking_bite", 0.05f)) return null;
                    QuadAt(at, Glow("fx_star", ratBite), 0.2f, 0.7f, 0.1f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
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
