using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Seven's effects, rebuilt from Deadlock's own recipes (docs/VfxRebuild/sevenVfxRebuild.md). In Deadlock his gun,
    /// Lightning Ball, Static Charge and Power Surge are amber-gold electricity (with the odd blue arc), and only Storm Cloud is
    /// blue-white under a grey cloud. So the ball, the charge and his flight are amber here and Thunderhead keeps its blue storm.</summary>
    public static partial class Vfx
    {
        // particles/abilities/gigawatt: ball #FFA53F / #FF8D41 / #FA703E, zap #F3B137, hot #FFCE89; static charge #FFD181 / #F6A259 / #FF7B00
        // with #69B0FF arcs; storm cloud #53B8F8 / #6AB2FF / #CBDCFF, ground bolt #83B2FF / #2478FF, cloud #AFB9C5
        static readonly Color sevenAmber = Hex("FFA53F"), sevenGold = Hex("FFCE89"), sevenDeep = Hex("FA703E"), sevenZap = Hex("F3B137"),
            sevenBlue = Hex("6AB2FF"), sevenWhite = Hex("CBDCFF"), sevenBoltBlue = Hex("53B8F8");
        static readonly Rect arcA = new Rect(0.015f, 0.005f, 0.241f, 0.891f), arcB = new Rect(0.501f, 0.026f, 0.222f, 0.942f);

        static float nextBallImpact;

        static GameObject Seven(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (id)
            {
                case "seven_gun": return SevenGun(moment, at);
                case "seven_ball": return SevenBall(moment, at, radius, follow, to);
                case "seven_static": return SevenStatic(moment, at, radius, follow);
                case "seven_flight": return SevenFlight(moment, at, radius, follow, to);
                case "seven_storm": return SevenStorm(moment, at, radius, to);
            }
            return null;
        }

        /// <summary>A few stylised lightning arcs flickering round a point (Deadlock's electric_beam_arc_stylized sprites).</summary>
        static void Arcs(Vector3 at, Color a, Color b, int count, float size, float spread, float life = 0.2f)
        {
            Puff(at, new P { Mat = Glow("fx_arc", a, arcA), Count = Mathf.Max(1, count / 2), LifeMin = life * 0.6f, LifeMax = life, SizeMin = size * 0.6f, SizeMax = size,
                             A = a, B = b, Radius = spread, FadeIn = 0.02f, FadeOutFrom = 0.5f, Max = count });
            Puff(at, new P { Mat = Glow("fx_arc", a, arcB), Count = Mathf.Max(1, count - count / 2), LifeMin = life * 0.6f, LifeMax = life, SizeMin = size * 0.6f, SizeMax = size,
                             A = a, B = b, Radius = spread, FadeIn = 0.02f, FadeOutFrom = 0.5f, Delay = life * 0.4f, Max = count });
        }

        /// <summary>A see-through sphere of influence: RoR2's lightning sphere, tinted, radius metres round, slowly turning, with
        /// glints on its surface so its edge reads against any ground.</summary>
        /// <param name="inside">also drawn from inside (the ball, seen from outside nearly always). Storm Cloud's camera is inside its
        /// sphere, so there the sphere is drawn from outside only (other players see it) and RoR2's area dome draws where it meets
        /// the ground: a ring of its reach under him.</param>
        static void Shell(GameObject root, float radius, Color c, bool inside = true)
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(shell.GetComponent<Collider>());
            shell.transform.SetParent(root.transform, false);
            shell.transform.localScale = Vector3.one * radius * 2f;
            var mr = shell.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Remapped("mat_shell", new Color(c.r, c.g, c.b, 0.5f), inside);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            shell.AddComponent<Spin>().DegPerSec = 25f;
            var edge = Emit(root, new P { Mat = Glow("fx_flare", c), Rate = 4f * radius, LifeMin = 0.35f, LifeMax = 0.5f, SizeMin = 0.4f, SizeMax = 0.7f, A = c, B = c,
                                          Radius = radius, World = false, Max = Mathf.RoundToInt(3f * radius) + 4 }, "edge");
            var shape = edge.shape; shape.radiusThickness = 0f;
            if (inside) return;
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(dome.GetComponent<Collider>());
            dome.transform.SetParent(root.transform, false);
            dome.transform.localScale = Vector3.one * radius * 2f;
            dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", c);
        }

        // ------------------------------------------------------------------ Shock Rifle
        static GameObject SevenGun(string moment, Vector3 at)
        {
            switch (moment)
            {
                case "muzzle":
                    // gigawatt_muzzle_flash: a lick of orange plasma, one small arc and a hot streak
                    if (!Every("seven_mz", 0.05f)) return null;
                    QuadAt(at, Glow("fx_flare_wide", Hex("FFAE23")), 0.5f, 1.3f, 0.08f, Face.Camera, 0f, 0f, 0.2f, Random.Range(-25f, 25f));
                    QuadAt(at, Glow("fx_arc", Hex("FECF78"), Random.value < 0.5f ? arcA : arcB), 0.5f, 0.9f, 0.09f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    return null;
                case "hit":
                    if (!Every("seven_hit", 0.06f)) return null;
                    QuadAt(at, Glow("fx_star", sevenGold), 0.3f, 0.9f, 0.1f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_arc", sevenAmber, arcA), 0.5f, 1.1f, 0.12f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Puff(at, new P { Mat = Mat("mat_spark", sevenAmber), Count = 5, LifeMin = 0.15f, LifeMax = 0.3f, SpeedMin = 3f, SpeedMax = 8f, SizeMin = 0.04f, SizeMax = 0.08f,
                                     A = sevenGold, B = sevenDeep, Stretch = 3f, Gravity = 1.2f, Max = 6 });
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Lightning Ball
        static GameObject SevenBall(string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "loop":
                {
                    // gigawatt_lightning_ball: a white-gold core in an amber ring, a turning halo of plasma, arcs snapping round it, inside
                    // a clear sphere showing its reach
                    var root = Root(at, 0f, follow);
                    Quad(root, Glow("fx_glow", sevenGold), 1.5f, 1.5f, 1f, Face.Camera, loop: true);
                    Quad(root, Glow("fx_ring_thick", sevenAmber), 2.4f, 2.4f, 1f, Face.Camera, 300f, loop: true);
                    Quad(root, Glow("fx_plasma", sevenDeep), 3.6f, 3.6f, 1f, Face.Camera, -120f, loop: true);
                    Emit(root, new P { Mat = Glow("fx_arc", sevenAmber, arcA), Rate = 7, LifeMin = 0.2f, LifeMax = 0.35f, SizeMin = 1.4f, SizeMax = 2.4f, A = Hex("FF9035"), B = Hex("DA6425"),
                                       Radius = 0.9f, FadeIn = 0.02f, FadeOutFrom = 0.5f, World = false, Max = 5 }, "arcs");
                    Emit(root, new P { Mat = Mat("mat_spark", sevenAmber), Rate = 24, LifeMin = 0.2f, LifeMax = 0.35f, SpeedMin = 2f, SpeedMax = 5.5f, SizeMin = 0.05f, SizeMax = 0.08f,
                                       A = sevenGold, B = sevenDeep, Radius = 0.5f, Stretch = 2.5f, Max = 14 }, "sparks");
                    var c = root.AddComponent<Crackle>(); c.Color = sevenAmber; c.Recolor = true; c.Radius = 2.2f; c.Count = 3;
                    Light(root, Hex("FF8D41"), 12f, 4f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    Shell(root, radius, sevenAmber);
                    return root;
                }
                case "hit":
                {
                    if (!to.HasValue) return null;
                    // gigawatt_lightning_ball_zap: a golden bolt from the ball to the enemy, a flare and sparks on it
                    Bolt(at, to.Value, sevenGold, 0.25f, 0.16f, 10, 0.6f, recolor: true);
                    Bolt(at, to.Value, sevenZap, 0.7f, 0.12f, 8, 0.9f, recolor: true);
                    if (Every("seven_ball_hit", 0.05f))
                    {
                        QuadAt(to.Value, Glow("fx_flare", sevenGold), 0.5f, 1.8f, 0.14f, Face.Camera, 0f, 0f, 0.25f);
                        Puff(to.Value, new P { Mat = Mat("mat_spark", sevenAmber), Count = 7, LifeMin = 0.25f, LifeMax = 0.5f, SpeedMin = 3f, SpeedMax = 8f, SizeMin = 0.05f, SizeMax = 0.08f,
                                               A = Hex("F38116"), B = Hex("E96915"), Stretch = 3f, Gravity = 1f, Max = 8 });
                    }
                    if (Time.time >= nextBallImpact) { nextBallImpact = Time.time + 0.1f; Fx.Impact("storm_strike_seven", to.Value); Glint(to.Value, sevenAmber, 5f, 0.12f, 2f); }
                    return null;
                }
                case "end":
                    // gigawatt_lightning_ball_explode: a wide flare, a glow, hot chips flung out, arcs (one or two of them blue)
                    QuadAt(at, Glow("fx_flare_wide", Hex("EC8831")), 1.5f, 7f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
                    QuadAt(at, Glow("fx_glow", Hex("FF8D40")), 1.5f, 5.5f, 0.28f, Face.Camera, 0f, 0f, 0.2f);
                    QuadAt(at, Glow("fx_ring", sevenAmber), 1f, 9f, 0.35f, Face.Camera, 0f, 0f, 0.3f);
                    Puff(at, new P { Mat = Glow("fx_fleks", sevenAmber), Count = 16, LifeMin = 0.2f, LifeMax = 0.5f, SpeedMin = 5f, SpeedMax = 15f, SizeMin = 0.14f, SizeMax = 0.28f,
                                     A = Hex("FC9E4D"), B = Hex("BB480A"), TilesX = 8, TilesY = 8, SpinMin = -8f, SpinMax = 8f, Drag = 2f, Max = 18 });
                    Puff(at, new P { Mat = Mat("mat_spark", sevenAmber), Count = 26, LifeMin = 0.3f, LifeMax = 0.5f, SpeedMin = 5f, SpeedMax = 15f, SizeMin = 0.06f, SizeMax = 0.11f,
                                     A = sevenGold, B = sevenDeep, Stretch = 3f, Max = 28 });
                    Arcs(at, Hex("FCC258"), Hex("F9762A"), 4, 2.4f, 1.2f, 0.3f);
                    if (Rich) QuadAt(at, Glow("fx_arc", Hex("309AF2"), arcB), 1.5f, 3.2f, 0.2f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Glint(at, Hex("FF8D41"), 14f, 0.3f, 6f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Storm Cloud (R): the flight
        /// <summary>Storm Cloud (R): Seven flies inside a clear amber sphere of radius that shocks everything in it.</summary>
        static GameObject SevenFlight(string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "cast":
                    // gigawatt_power_surge_cast: an amber ring snapping out, a column of light, arcs
                    QuadAt(Ground(at), Glow("fx_ring", sevenDeep), 1f, radius * 1.2f, 0.45f, Face.Ground, 0f, 0.05f, 0.3f);
                    Beam(at, at + Vector3.up * 9f, Glow("fx_beam", Hex("FA9C55")), sevenGold, 1.2f, 0.5f);
                    Arcs(at + Vector3.up, sevenGold, sevenDeep, 6, 2.2f, 1.2f, 0.25f);
                    Puff(at, new P { Mat = Mat("mat_spark", sevenAmber), Count = 22, LifeMin = 0.3f, LifeMax = 0.5f, SpeedMin = 4f, SpeedMax = 11f, SizeMin = 0.05f, SizeMax = 0.09f,
                                     A = sevenGold, B = sevenDeep, Shape = Shape.Up, Angle = 50f, Stretch = 3f, Max = 24 });
                    Glint(at, sevenAmber, 12f, 0.35f, 6f);
                    return null;
                case "loop":
                {
                    // Seven as the living storm: plasma and arcs on him, sparks, an amber light, and the sphere of his reach
                    var root = Root(at, 0f, follow);
                    Quad(root, Glow("fx_plasma", new Color(sevenDeep.r, sevenDeep.g, sevenDeep.b, 0.7f)), 2.6f, 2.6f, 1f, Face.Camera, 90f, loop: true);
                    Quad(root, Glow("fx_glow_soft", new Color(sevenAmber.r, sevenAmber.g, sevenAmber.b, 0.6f)), 2.2f, 2.2f, 1f, Face.Camera, loop: true);
                    Emit(root, new P { Mat = Glow("fx_arc", sevenAmber, arcB), Rate = 6, LifeMin = 0.15f, LifeMax = 0.3f, SizeMin = 1.6f, SizeMax = 2.6f, A = sevenGold, B = sevenDeep,
                                       Radius = 1f, FadeIn = 0.02f, FadeOutFrom = 0.5f, World = false, Max = 4 }, "arcs");
                    Emit(root, new P { Mat = Mat("mat_spark", sevenAmber), Rate = 22, LifeMin = 0.25f, LifeMax = 0.4f, SpeedMin = 2f, SpeedMax = 4.5f, SizeMin = 0.05f, SizeMax = 0.08f,
                                       A = sevenGold, B = sevenDeep, Radius = 0.8f, Stretch = 2.5f, Max = 14 }, "sparks");
                    var c = root.AddComponent<Crackle>(); c.Color = sevenAmber; c.Recolor = true; c.Radius = 1.8f; c.Count = 3;
                    Light(root, sevenAmber, radius, 3f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    Shell(root, radius, sevenAmber, inside: false);
                    return root;
                }
                case "hit":
                {
                    if (!to.HasValue) return null;
                    Bolt(at, to.Value, sevenGold, 0.3f, 0.18f, 12, 0.8f, recolor: true);
                    Bolt(at, to.Value, sevenZap, 0.8f, 0.14f, 10, 1.2f, recolor: true);
                    if (Every("seven_flight_hit", 0.05f))
                    {
                        QuadAt(to.Value, Glow("fx_flare", sevenGold), 0.6f, 2f, 0.14f, Face.Camera, 0f, 0f, 0.25f);
                        Puff(to.Value, new P { Mat = Mat("mat_spark", sevenAmber), Count = 8, LifeMin = 0.2f, LifeMax = 0.4f, SpeedMin = 3f, SpeedMax = 9f, SizeMin = 0.05f, SizeMax = 0.08f,
                                               A = sevenGold, B = sevenDeep, Stretch = 3f, Max = 10 });
                    }
                    if (Time.time >= nextBallImpact) { nextBallImpact = Time.time + 0.1f; Fx.Impact("storm_strike_seven", to.Value); Glint(to.Value, sevenAmber, 6f, 0.15f, 2.5f); }
                    return null;
                }
                case "end":
                    QuadAt(at, Glow("fx_glow", sevenGold), 2f, 6.5f, 0.3f, Face.Camera, 0f, 0f, 0.2f);
                    QuadAt(at, Glow("fx_ring_burst", sevenAmber), 1f, radius * 1.2f, 0.4f, Face.Camera, 0f, 0f, 0.3f);
                    Puff(at, new P { Mat = Mat("mat_spark", sevenAmber), Count = 34, LifeMin = 0.3f, LifeMax = 0.5f, SpeedMin = 6f, SpeedMax = 17f, SizeMin = 0.06f, SizeMax = 0.11f,
                                     A = sevenGold, B = sevenDeep, Stretch = 3f, Max = 36 });
                    Arcs(at, sevenGold, sevenDeep, 4, 2.6f, 1.4f, 0.3f);
                    Glint(at, sevenAmber, 16f, 0.3f, 6f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Static Charge
        static GameObject SevenStatic(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // gigawatt_static_charge_debuff: a ring of arcs closing in on the target while it counts down, a glow on it, sparks
                    var root = Root(at, 0f, follow);
                    var ring = new GameObject("ring"); ring.transform.SetParent(root.transform, false); ring.transform.localPosition = Vector3.up * 0.4f;
                    Emit(ring, new P { Mat = Glow("fx_glow", sevenAmber), Rate = 30, LifeMin = 0.2f, LifeMax = 0.3f, SizeMin = 0.4f, SizeMax = 0.7f, A = Hex("FFD181"), B = Hex("FF7B00"),
                                       Shape = Shape.Ring, Radius = radius, World = false, Max = 24 }, "dots");
                    Emit(ring, new P { Mat = Glow("fx_arc", Hex("FFCF41"), arcA), Rate = 9, LifeMin = 0.12f, LifeMax = 0.25f, SizeMin = 0.9f, SizeMax = 1.6f, A = Hex("FFD17C"), B = Hex("B2641B"),
                                       Shape = Shape.Ring, Radius = radius, FadeIn = 0.02f, FadeOutFrom = 0.5f, World = false, Max = 5 }, "arcs");
                    ring.AddComponent<Spin>().DegPerSec = 200f;
                    ring.AddComponent<Shrink>().To = 0.12f;
                    Quad(root, Glow("fx_glow_soft", Hex("F6A259")), 2.2f, 2.2f, 1f, Face.Camera, offset: Vector3.up, loop: true);
                    Emit(root, new P { Mat = Mat("mat_spark", sevenAmber), Rate = 22, LifeMin = 0.2f, LifeMax = 0.35f, SpeedMin = 1.5f, SpeedMax = 3.5f, SizeMin = 0.05f, SizeMax = 0.09f,
                                       A = sevenGold, B = sevenDeep, Radius = 0.8f, Stretch = 2.5f, Max = 12 }, "sparks");
                    var c = root.AddComponent<Crackle>(); c.Color = sevenAmber; c.Recolor = true; c.Radius = 1.4f; c.Count = 2;
                    Light(root, sevenAmber, 6f, 3f, Vector3.up).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                {
                    // gigawatt_static_charge_explosion: the thunderbolt from the sky, then arcs (some blue), hot streaks and rings on the ground
                    var sky = at + Vector3.up * 40f + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                    Bolt(sky, at, sevenGold, 1.4f, 0.35f, 14, 2.5f, recolor: true);
                    Bolt(sky, at, sevenAmber, 2.6f, 0.25f, 10, 4f, recolor: true);
                    var g = Ground(at);
                    QuadAt(g, Glow("fx_ring", Hex("FFC061")), 1f, radius * 2f, 0.4f, Face.Ground, 0f, 0.05f, 0.3f);
                    QuadAt(g, Glow("fx_ring_burst", sevenDeep), 0.8f, radius * 1.3f, 0.3f, Face.Ground, 0f, 0.05f, 0.3f);
                    QuadAt(at + Vector3.up, Glow("fx_glow", sevenGold), radius * 0.5f, radius * 1.8f, 0.3f, Face.Camera, 0f, 0f, 0.2f);
                    Puff(at, new P { Mat = Mat("mat_spark", sevenAmber), Count = 30, LifeMin = 0.3f, LifeMax = 0.75f, SpeedMin = 5f, SpeedMax = 17f, SizeMin = 0.08f, SizeMax = 0.14f,
                                     A = Hex("FFB77F"), B = Hex("F97C39"), Shape = Shape.Up, Angle = 65f, Stretch = 3.5f, Gravity = 1.6f, Max = 32 });
                    Arcs(at + Vector3.up * 0.8f, Hex("EE851D"), Hex("FA7625"), 4, radius * 0.6f, radius * 0.3f, 0.25f);
                    if (Rich) QuadAt(at + Vector3.up, Glow("fx_arc", Hex("69B0FF"), arcA), radius * 0.4f, radius * 0.8f, 0.2f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Flash(at + Vector3.up * 3f, sevenGold, radius * 3f, 0.25f, 10f);
                    Shake(at, radius * 3f, 1.5f, 0.3f);
                    return null;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Thunderhead (Q): the blue storm
        static GameObject SevenStorm(string moment, Vector3 at, float radius, Vector3? to)
        {
            const float cloudHeight = 35f;
            var stormDark = new Color(0.07f, 0.08f, 0.12f, 0.95f);
            switch (moment)
            {
                case "loop":
                {
                    // the thunderhead: a thick, dark, churning cloud deck over the zone (RoR2's opaque dust, so it really is dark), a
                    // dim haze under it, lightning flickering inside the clouds, and bolts cracking down onto the ground all over the storm.
                    // (Fewer, larger cloud puffs than before: the deck was the dearest thing he drew.)
                    var root = Root(at, 0f, null);
                    var cloud = new GameObject("cloud"); cloud.transform.SetParent(root.transform, false); cloud.transform.localPosition = Vector3.up * cloudHeight;
                    var deck = Particles(cloud, "mat_dust", stormDark, rate: 30, life: 4f, speed: 0.4f, size: radius * 0.5f, shapeRadius: radius * 1.05f, flat: true);
                    deck.Emit(50);
                    var belly = Particles(cloud, "mat_dust", new Color(0.13f, 0.15f, 0.22f, 0.9f), rate: 20, life: 3.5f, speed: 0.3f, size: radius * 0.4f, shapeRadius: radius * 0.9f, flat: true);
                    belly.transform.localPosition = Vector3.down * 2.5f;
                    belly.Emit(28);
                    var top = Particles(cloud, "mat_dust", new Color(0.2f, 0.22f, 0.3f, 0.85f), rate: 10, life: 4f, speed: 0.2f, size: radius * 0.45f, shapeRadius: radius * 0.75f, flat: true);
                    top.transform.localPosition = Vector3.up * 3f;
                    top.Emit(14);
                    cloud.AddComponent<Spin>().DegPerSec = 10f;
                    var haze = new GameObject("haze"); haze.transform.SetParent(root.transform, false); haze.transform.localPosition = Vector3.up * (cloudHeight * 0.45f);
                    var hz = Particles(haze, "mat_dust", new Color(0.05f, 0.06f, 0.09f, 0.22f), rate: 16, life: 4f, speed: 0.2f, size: radius * 0.55f, shapeRadius: radius * 0.85f, flat: true);
                    hz.Emit(20);
                    // the blue-white glow inside the cloud base (gigawatt_storm_cloud_cast_glow)
                    Quad(cloud, Glow("fx_glow_soft", Hex("8AA9F2", 0.4f)), radius * 0.8f, radius * 0.8f, 1f, Face.Ground, 6f, offset: Vector3.down * 4f, loop: true);
                    var flash = Light(cloud, sevenBlue, radius * 1.4f, 0f, Vector3.down * 3f);
                    flash.gameObject.AddComponent<StormFlash>();
                    var amb = root.AddComponent<StormBolts>();
                    amb.Radius = radius * 0.9f; amb.Height = cloudHeight - 2f;
                    return root;
                }
                case "hit":
                {
                    if (!to.HasValue) return null;
                    var from = new Vector3(to.Value.x + Random.Range(-2f, 2f), at.y + cloudHeight - 2f, to.Value.z + Random.Range(-2f, 2f));
                    StrikeDown(from, to.Value, true);
                    return null;
                }
            }
            return null;
        }

        /// <summary>One lightning strike from the cloud base down to a point (gigawatt_storm_cloud_ground_bolt): a white core in a blue
        /// glow, and where it lands a flare, a ring on the ground, blue sparks and a puff of pale dust.</summary>
        static void StrikeDown(Vector3 from, Vector3 to, bool big)
        {
            Bolt(from, to, sevenWhite, big ? 0.35f : 0.22f, 0.16f, 12, big ? 1.6f : 1.2f);
            Bolt(from, to, sevenBoltBlue, big ? 0.9f : 0.55f, 0.12f, 9, big ? 2.2f : 1.6f);
            QuadAt(to + Vector3.up * 0.4f, Glow("fx_flare", Hex("7C9FFF")), 0.6f, big ? 3.2f : 2f, 0.16f, Face.Camera, 0f, 0f, 0.2f);
            if (big || Rich) QuadAt(to, Glow("fx_ring", Hex("83B2FF")), 0.5f, big ? 5f : 3f, 0.35f, Face.Ground, 0f, 0.05f, 0.3f);
            Puff(to, new P { Mat = Spark(sevenBlue), Count = big ? 14 : 6, LifeMin = 0.25f, LifeMax = 0.7f, SpeedMin = 3f, SpeedMax = 9f, SizeMin = 0.05f, SizeMax = 0.09f,
                             A = Hex("87A7F9"), B = Hex("2478FF"), Shape = Shape.Up, Angle = 60f, Stretch = 3f, Gravity = 1.5f, Max = 16 });
            if (big)
            {
                Puff(to + Vector3.up * 0.2f, new P { Mat = Soft("fx_cloud", Color.white), Count = 4, LifeMin = 0.5f, LifeMax = 1f, SpeedMin = 1f, SpeedMax = 3f, SizeMin = 0.9f, SizeMax = 1.4f,
                                                     A = Hex("DADADA", 0.28f), B = Hex("D0C4B6", 0.1f), Shape = Shape.Disc, Radius = 0.6f, Size0 = 0.5f, Size1 = 1.5f, Drag = 2f, Max = 5 });
                Glint(to + Vector3.up * 1.5f, sevenBoltBlue, 9f, 0.12f, 2.2f);
            }
            else if (Alive < 40) Flash(to + Vector3.up * 1.5f, sevenBoltBlue, 6f, 0.12f, 1.4f);
        }

        /// <summary>Thunderhead's extra bolts: every 0.15–0.45 s a bolt from the cloud to a random spot on the ground inside the storm.</summary>
        sealed class StormBolts : MonoBehaviour
        {
            public float Radius, Height;
            float next;
            void Update()
            {
                if (Time.time < next) return;
                next = Time.time + Random.Range(0.15f, 0.45f);
                var c = Random.insideUnitCircle * Radius;
                var top = transform.position + new Vector3(c.x, Height, c.y);
                var ground = top + Vector3.down * Height;
                if (Physics.Raycast(top, Vector3.down, out var hit, Height * 2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) ground = hit.point;
                StrikeDown(top, ground, false);
            }
        }
    }
}
