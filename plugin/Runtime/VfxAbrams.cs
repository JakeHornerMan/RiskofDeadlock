using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Abrams' effects, rebuilt from Deadlock's own recipes (docs/VfxRebuild/abramsVfxRebuild.md). Deadlock's Abrams
    /// is electric blue: a blue shield of air and arcs on his Shoulder Charge, blue rings and a column of light on Seismic
    /// Impact with grey debris, smoke and the pages of his book thrown up, crimson-and-blue book magic on Siphon Life, and a
    /// plain orange shotgun flash. (Ours used to be red and yellow.)</summary>
    public static partial class Vfx
    {
        // particles/abilities/abrams: charge #2982EE / #30ADF6 / #5CD1FF over #0D3871, streaks #C3DFFF, dust #AEA798 / #8E8376;
        // leap #3E65FE / #5787FF / #1C49A4, flash #FF3FCC, debris #697082, pages #EBDBC2; siphon #105CD0 / #1F5DE2 with #CD3259;
        // weapon_fx/bull: muzzle #F39F2A / #FF8B24, smoke #DEDEDE
        static readonly Color abBlue = Hex("2982EE"), abSky = Hex("5CD1FF"), abPale = Hex("C3DFFF"), abDeep = Hex("0D3871"), abLeap = Hex("5787FF"),
            abDust = Hex("AEA798", 0.7f), abDustDark = Hex("8E8376", 0.6f), abPage = Hex("EBDBC2"), abCrimson = Hex("CD3259"), abMuzzle = Hex("FF8B24"), abMuzzleHot = Hex("FFC95C");

        static GameObject Abrams(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle, float life = 0f)
        {
            switch (id)
            {
                case "abrams_gun": return AbramsGun(moment, at);
                case "abrams_siphon": return AbramsSiphon(moment, at, radius, follow, life);
                case "abrams_charge": return AbramsCharge(moment, at, follow);
                case "abrams_haymaker": return AbramsHaymaker(moment, at, radius, follow, to, angle);
                case "abrams_slam": return AbramsSlam(moment, at, radius, follow);
            }
            return null;
        }

        /// <summary>Deadlock's cartoon dust: beige cloud puffs that swell and thin (abrams_charge_dust_puff, _leap_start_smoke).</summary>
        static void DustPuffs(Vector3 at, int count, float size, float speed, float life, Color a, Color b, bool flat = true, float radius = 0.6f)
        {
            Puff(at, new P { Mat = Soft("fx_cloud", Color.white), Count = count, LifeMin = life * 0.6f, LifeMax = life, SpeedMin = speed * 0.4f, SpeedMax = speed,
                             SizeMin = size * 0.6f, SizeMax = size, A = a, B = new Color(b.r, b.g, b.b, b.a * 0.6f), FadeIn = 0.06f, FadeOutFrom = 0.4f, Size0 = 0.45f, Size1 = 1.5f,
                             Shape = flat ? Shape.Disc : Shape.Sphere, Radius = radius, Drag = 2.2f, SpinMin = -0.5f, SpinMax = 0.5f, Velocity = Vector3.up * 0.6f, Max = Mathf.Max(8, count + 4) });
        }

        /// <summary>Pages of his book thrown into the air, tumbling down (abrams_leap_impact_paper, _siphon_life_buff_paper).</summary>
        static void Pages(Vector3 at, int count, float speed, float radius, Color tint, float life = 2.2f, float orbit = 0f)
        {
            if (!Rich) count = Mathf.Max(3, count / 2);
            Puff(at, new P { Mat = Raw("abrams_paper_tex", Color.white), Count = count, LifeMin = life * 0.6f, LifeMax = life, SpeedMin = speed * 0.4f, SpeedMax = speed,
                             SizeMin = 0.28f, SizeMax = 0.42f, A = tint, B = tint, FadeIn = 0.02f, FadeOutFrom = 0.75f, Shape = Shape.Up, Angle = 55f, Radius = radius,
                             Gravity = 0.55f, Drag = 1.2f, Noise = 1.2f, SpinMin = -4f, SpinMax = 4f, TilesX = 4, TilesY = 4, Animate = true, Orbit = orbit, Max = count + 2 });
        }

        // ------------------------------------------------------------------ Shotgun
        static GameObject AbramsGun(string moment, Vector3 at)
        {
            switch (moment)
            {
                case "muzzle":
                    // bull_muzzle_flash: a ragged orange flash, a ball of hot gas, a few embers and pale smoke
                    if (!Every("abrams_mz", 0.07f)) return null;
                    QuadAt(at, Glow("fx_muzzle", abMuzzle), 0.7f, 1.7f, 0.09f, Face.Camera, 0f, 0f, 0.25f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_glow", Hex("E47413")), 0.6f, 1.2f, 0.12f, Face.Camera, 0f, 0f, 0.2f);
                    if (Rich)
                    {
                        Puff(at, new P { Mat = Mat("mat_spark", abMuzzle), Count = 5, LifeMin = 0.15f, LifeMax = 0.6f, SpeedMin = 2f, SpeedMax = 9f, SizeMin = 0.04f, SizeMax = 0.07f,
                                         A = Hex("FF885C"), B = Hex("4C1607"), Stretch = 2f, Gravity = 0.8f, Drag = 2f, Max = 6 });
                        Puff(at, new P { Mat = Soft("fx_cloud", Color.white), Count = 3, LifeMin = 0.25f, LifeMax = 0.5f, SpeedMin = 0.5f, SpeedMax = 2f, SizeMin = 0.35f, SizeMax = 0.6f,
                                         A = Hex("DEDEDE", 0.4f), B = Hex("F4C682", 0.15f), Size0 = 0.5f, Size1 = 1.6f, Drag = 2f, Max = 4 });
                    }
                    return null;
                case "hit":
                    if (!Every("abrams_hit", 0.06f)) return null;
                    QuadAt(at, Glow("fx_star", abMuzzleHot), 0.3f, 1f, 0.1f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Puff(at, new P { Mat = Mat("mat_spark", abMuzzle), Count = 7, LifeMin = 0.15f, LifeMax = 0.3f, SpeedMin = 3f, SpeedMax = 9f, SizeMin = 0.05f, SizeMax = 0.09f,
                                     A = abMuzzleHot, B = Hex("C6430B"), Stretch = 3f, Gravity = 1.5f, Max = 8 });
                    return null;
                case "impact":
                    if (!Every("abrams_imp", 0.1f) || !Rich) return null;
                    Puff(at, new P { Mat = Soft("fx_fleks", Color.white), Count = 4, LifeMin = 0.3f, LifeMax = 0.6f, SpeedMin = 1.5f, SpeedMax = 5f, SizeMin = 0.07f, SizeMax = 0.13f,
                                     A = Hex("998877"), B = Hex("41332F"), Gravity = 2.5f, TilesX = 8, TilesY = 8, SpinMin = -6f, SpinMax = 6f, Max = 6 });
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Siphon Life
        static GameObject AbramsSiphon(string moment, Vector3 at, float radius, Transform follow = null, float life = 0f)
        {
            if (moment == "loop")
            {
                // abrams_siphon_life_buff, for as long as the drain lasts (creator, 2026-11-13: "where is Abrams' blue aura in Siphon
                // Life and the open of the book, the pages around his feet"): a blue aura over his whole body with motes rising
                // through it, the book's ring and its red symbol turning on the ground under him, pages of the book whirling round
                // his feet, and a ring on the ground at the drain's reach. The root follows him and ends by itself after life seconds
                var root = Root(at, Mathf.Max(0.5f, life), follow);
                var feet = new GameObject("feet"); feet.transform.SetParent(root.transform, false);
                if (follow) feet.transform.localPosition = at - follow.position;
                float reach = Mathf.Clamp(radius, 4f, 25f);
                // the aura on him
                Quad(feet, Glow("fx_glow", Hex("2A96E9", 0.85f)), 4.4f, 4.4f, 1f, Face.Camera, offset: Vector3.up * 1.1f, loop: true);
                Quad(feet, Glow("fx_ring_thick", Hex("5CD1FF", 0.7f)), 3.3f, 3.3f, 1f, Face.Camera, 90f, offset: Vector3.up * 1.1f, loop: true);
                Quad(feet, Glow("fx_plasma", Hex("1F5DE2", 0.8f)), 4f, 4f, 1f, Face.Camera, -70f, offset: Vector3.up * 1.1f, loop: true);
                // bands of light climbing him
                Emit(feet, new P { Mat = Glow("fx_ring", Hex("5CD1FF")), Rate = 2.2f, LifeMin = 1f, LifeMax = 1f, SizeMin = 2.3f, SizeMax = 2.3f, A = Hex("5CD1FF"), B = Hex("105CD0"),
                                   Shape = Shape.Point, Velocity = Vector3.up * 2.1f, Flat = true, RandomRoll = false, FadeIn = 0.15f, FadeOutFrom = 0.6f, World = false, Max = 4 }, "bands");
                Emit(feet, new P { Mat = Glow("fx_glow", Hex("1F5DE2")), Rate = 22, LifeMin = 0.9f, LifeMax = 1.5f, SpeedMin = 0.1f, SpeedMax = 0.5f, SizeMin = 0.12f, SizeMax = 0.26f,
                                   A = Hex("5CD1FF"), B = Hex("1443C7"), Shape = Shape.Disc, Radius = 0.9f, Velocity = Vector3.up * 1.7f, Noise = 0.5f, World = false, Max = 26 }, "motes");
                // the open book under him: its ring, its glyph and its red symbol
                Quad(feet, Glow("fx_ring_spiral", Hex("2A7BFF")), 5.2f, 5.2f, 1f, Face.Ground, 60f, offset: Vector3.up * 0.3f, loop: true);
                Quad(feet, Glow("fx_ring_thick", Hex("188BFF", 0.9f)), 3.6f, 3.6f, 1f, Face.Ground, -45f, offset: Vector3.up * 0.32f, loop: true);
                Quad(feet, Glow("fx_glow", Hex("FF3D64", 0.6f)), 0.9f, 0.9f, 1f, Face.Ground, offset: Vector3.up * 0.14f, loop: true);
                // pages whirling round his feet and up past his knees
                Emit(feet, new P { Mat = Raw("abrams_paper_tex", Color.white), Rate = 16, LifeMin = 1.1f, LifeMax = 1.7f, SpeedMin = 0.1f, SpeedMax = 0.4f, SizeMin = 0.55f, SizeMax = 0.8f,
                                   A = Hex("C0D7FF"), B = Hex("7F9BFF"), FadeIn = 0.1f, FadeOutFrom = 0.7f, Shape = Shape.Ring, Radius = 1.5f, Velocity = Vector3.up * 1.1f, Orbit = 3.2f,
                                   Noise = 0.4f, SpinMin = -3f, SpinMax = 3f, TilesX = 4, TilesY = 4, Animate = true, World = false, Max = 30 }, "pages");
                // the drain's reach
                Quad(feet, Glow("fx_ring_soft", Hex("105CD0", 0.6f)), reach * 2f, reach * 2f, 1f, Face.Ground, 12f, offset: Vector3.up * 0.1f, loop: true);
                Light(feet, abBlue, 7f, 1.6f, Vector3.up * 1.2f);
                return root;
            }
            if (moment != "cast") return null;
            // abrams_siphon_life_buff_start: the book opens: a blue wave over the ground, a glyph over him, blue motes, pages whirling up,
            // a flick of crimson (the life he takes)
            float r = Mathf.Clamp(radius, 4f, 9f);
            var g = Ground(at);
            QuadAt(g, Glow("fx_ring_thick", Hex("105CD0")), 1f, r * 2f, 0.6f, Face.Ground, 0f, 0.05f, 0.3f);
            QuadAt(g, Glow("fx_ring", Hex("208CBF")), 0.6f, r * 1.4f, 0.45f, Face.Ground, 0f, 0.05f, 0.3f, 0f, 0.08f);
            QuadAt(at + Vector3.up * 1.2f, Glow("fx_flare", Hex("1F5DE2")), 0.8f, 3.2f, 0.25f, Face.Camera, 0f, 0f, 0.2f);
            Puff(at + Vector3.up * 2.6f, new P { Mat = Glow("fx_glyphs", abSky), Count = 1, LifeMin = 1.1f, LifeMax = 1.1f, SizeMin = 1.1f, SizeMax = 1.1f, A = abSky, B = Hex("1443C7"),
                                               Shape = Shape.Point, Velocity = Vector3.up * 0.5f, TilesX = 4, TilesY = 4, RandomRoll = false, Size0 = 0.6f, Size1 = 1.1f, FadeIn = 0.15f, Max = 2 });
            Puff(at + Vector3.up * 0.2f, new P { Mat = Glow("fx_glow", Hex("1F5DE2")), Count = 28, LifeMin = 0.8f, LifeMax = 1.6f, SpeedMin = 0.2f, SpeedMax = 1.2f, SizeMin = 0.06f, SizeMax = 0.14f,
                                                 A = Hex("1F5DE2"), B = Hex("1443C7"), Shape = Shape.Disc, Radius = 1.4f, Velocity = Vector3.up * 1.8f, Noise = 0.7f, Max = 30 });
            Puff(at + Vector3.up * 1.2f, new P { Mat = Glow("fx_plasma", abCrimson), Count = 4, LifeMin = 0.4f, LifeMax = 0.7f, SpeedMin = 0.5f, SpeedMax = 1.5f, SizeMin = 1.2f, SizeMax = 1.8f,
                                                 A = abCrimson, B = Hex("8E0C3B"), Size0 = 0.5f, Size1 = 1.3f, SpinMin = -2f, SpinMax = 2f, Max = 5 });
            Pages(at + Vector3.up * 0.3f, 16, 5f, 1f, Hex("C0D7FF"), 1.8f, 2.5f);
            Glint(at + Vector3.up, abBlue, 9f, 0.35f, 5f);
            return null;
        }

        // ------------------------------------------------------------------ Shoulder Charge
        static GameObject AbramsCharge(string moment, Vector3 at, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // abrams_charge: the state turns this root to face the charge each frame, so +z is his front. A blue shield of air
                    // on his shoulder crackling with arcs, pale sparks streaming back past him, blue ribbons off both shoulders and one
                    // along the ground, and cartoon dust rolling up behind
                    var root = Root(at, 0f, follow);
                    var front = new GameObject("front"); front.transform.SetParent(root.transform, false); // centred on his middle and tall enough to cover him from foot to head (creator, 2026-11-13: its bottom sat at his mid section:
                    // the root follows the body's centre, so the old 1.2 m lift put the ring's centre at his head)
                    front.transform.localPosition = new Vector3(0f, 0.1f, 1.0f);
                    Quad(front, Glow("fx_ring_thick", abBlue), 4.2f, 4.2f, 1f, Face.Camera, 240f, loop: true);
                    Quad(front, Glow("fx_glow", Hex("5D87BB")), 2.6f, 2.6f, 1f, Face.Camera, loop: true);
                    Emit(front, new P { Mat = Glow("fx_arc", abSky, new Rect(0.015f, 0.005f, 0.241f, 0.891f)), Rate = 11, LifeMin = 0.12f, LifeMax = 0.2f, SizeMin = 0.9f, SizeMax = 1.7f,
                                        A = abSky, B = Hex("239BEB"), Radius = 0.8f, FadeIn = 0.02f, FadeOutFrom = 0.5f, World = false, Max = 6 }, "arcs");
                    Emit(front, new P { Mat = Spark(abPale), Rate = 60, LifeMin = 0.25f, LifeMax = 0.35f, SpeedMin = 4f, SpeedMax = 8f, SizeMin = 0.05f, SizeMax = 0.1f,
                                        A = abPale, B = Hex("278FFF"), Radius = 0.9f, Stretch = 3f, Max = 30 }, "streaks");
                    Light(front, Hex("28A6EA"), 9f, 3.5f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    foreach (var x in new[] { -0.55f, 0.55f })
                    {
                        var s = new GameObject("shoulder"); s.transform.SetParent(root.transform, false); s.transform.localPosition = new Vector3(x, 0.6f, 0f);
                        Ribbon(s, Glow("fx_beam", abBlue), Hex("0789E0"), Hex("070F2C"), 0.32f, 0.45f);
                    }
                    var low = new GameObject("ground"); low.transform.SetParent(root.transform, false); low.transform.localPosition = new Vector3(0f, -0.95f, -0.2f);
                    Ribbon(low, Glow("fx_beam", Hex("1E76CF")), Hex("1E76CF"), abDeep, 1.1f, 0.8f, 0.5f);
                    Emit(root, new P { Mat = Soft("fx_cloud", Color.white), Rate = 14, LifeMin = 1.1f, LifeMax = 1.8f, SpeedMin = 0.4f, SpeedMax = 1.4f, SizeMin = 1.1f, SizeMax = 1.8f,
                                       A = Hex("CBC7C1", 0.55f), B = Hex("A0A0A0", 0.25f), Shape = Shape.Disc, Radius = 0.7f, Size0 = 0.4f, Size1 = 1.5f, FadeIn = 0.08f, FadeOutFrom = 0.35f,
                                       Velocity = Vector3.up * 0.7f, SpinMin = -0.5f, SpinMax = 0.5f, Max = 26 }, "dust");
                    return root;
                }
                case "cast":
                    // abrams_charge_start: two blue rings snap out where he sets off, over a kick of dust
                    QuadAt(Ground(at), Glow("fx_ring_thick", Hex("0054FF")), 1.2f, 6.5f, 0.4f, Face.Ground, 0f, 0.05f, 0.3f);
                    QuadAt(Ground(at), Glow("fx_ring", abSky), 0.8f, 4.5f, 0.3f, Face.Ground, 0f, 0.05f, 0.3f, 0f, 0.05f);
                    DustPuffs(at + Vector3.up * 0.2f, 8, 1.8f, 6f, 1.2f, abDust, abDustDark);
                    Glint(at + Vector3.up * 1.2f, Hex("28A6EA"), 8f, 0.3f, 5f);
                    return null;
                case "hit":
                    // abrams_charge_wall_impact: he hits something: a dust ring, a pale flash, sparks
                    if (!Every("abrams_charge_hit", 0.08f)) return null;
                    QuadAt(at, Glow("fx_ring_burst", abSky), 0.6f, 4.2f, 0.28f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_glow_soft", abPale), 1.2f, 3f, 0.15f, Face.Camera, 0f, 0f, 0.2f);
                    Puff(at, new P { Mat = Spark(abPale), Count = 14, LifeMin = 0.3f, LifeMax = 0.7f, SpeedMin = 3f, SpeedMax = 11f, SizeMin = 0.05f, SizeMax = 0.1f,
                                     A = Hex("EEDDBD"), B = abBlue, Stretch = 3f, Gravity = 1.6f, Max = 16 });
                    DustPuffs(at, 5, 1.3f, 3.5f, 0.7f, abDustDark, Hex("504133", 0.4f), false, 0.4f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Haymaker
        static GameObject AbramsHaymaker(string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle)
        {
            switch (moment)
            {
                case "cast":
                {
                    // the wind-up: the fist gathers blue light and arcs
                    var root = Root(at, 0f, follow);
                    Quad(root, Glow("fx_glow", abBlue), 1.1f, 1.1f, 1f, Face.Camera, loop: true);
                    Quad(root, Glow("fx_ring_spiral", abSky), 1.5f, 1.5f, 1f, Face.Camera, -540f, loop: true);
                    Emit(root, new P { Mat = Spark(abPale), Rate = 34, LifeMin = 0.25f, LifeMax = 0.4f, SpeedMin = 1f, SpeedMax = 3f, SizeMin = 0.05f, SizeMax = 0.08f,
                                       A = abPale, B = abBlue, Radius = 0.9f, Radial = -4f, Stretch = 2f, Max = 16 });
                    Light(root, abBlue, 5f, 3f, Vector3.zero);
                    return root;
                }
                case "hit":
                {
                    var dir = (to ?? at + Vector3.forward) - at; dir.y = 0; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward; dir.Normalize();
                    // the punch lands: a spiked burst and a ring facing the camera, sparks thrown forward along the punch, dust underfoot
                    QuadAt(at, Glow("fx_ring_burst", abSky), 0.6f, radius * 2.6f, 0.3f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_ring", abBlue), 0.4f, radius * 3.6f, 0.26f, Face.Camera, 0f, 0f, 0.3f);
                    QuadAt(at, Glow("fx_star", abPale), 0.8f, radius * 1.6f, 0.14f, Face.Camera, 0f, 0f, 0.3f);
                    Puff(at, new P { Mat = Spark(abPale), Count = 26, LifeMin = 0.25f, LifeMax = 0.4f, SpeedMin = 8f, SpeedMax = 19f, SizeMin = 0.06f, SizeMax = 0.1f,
                                     A = abPale, B = abBlue, Shape = Shape.Cone, Angle = 30f, Radius = 0.2f, Stretch = 3f, Max = 28 }, Quaternion.LookRotation(dir));
                    DustPuffs(at + Vector3.down * 0.8f, 7, 1.7f, 4.5f, 1.1f, abDust, abDustDark);
                    Glint(at, abBlue, radius * 3f, 0.3f, 7f);
                    Shake(at, radius * 6f, 4.5f, 0.45f);
                    return null;
                }
                case "wave":
                {
                    // the shockwave rolls forward over the ground in a cone (radius = its length, angle = its half-angle): dust and blue
                    // sparks racing out, and blue rings laid along it one after another
                    var dir = (to ?? at + Vector3.forward) - at; dir.y = 0; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward; dir.Normalize();
                    float life = 0.35f, half = Mathf.Max(5f, angle);
                    var ground = at + Vector3.down * 0.8f;
                    var rot = Quaternion.LookRotation(dir);
                    Puff(ground, new P { Mat = Soft("fx_cloud", Color.white), Count = 22, LifeMin = life * 1.6f, LifeMax = life * 2.6f, SpeedMin = radius / life * 0.5f, SpeedMax = radius / life,
                                         SizeMin = 1.1f, SizeMax = 1.9f, A = abDust, B = Hex("8E8376", 0.25f), Shape = Shape.Cone, Angle = half, Radius = 0.4f, Drag = 3.5f,
                                         Size0 = 0.5f, Size1 = 1.5f, FadeOutFrom = 0.35f, Max = 24 }, rot);
                    Puff(ground, new P { Mat = Spark(abPale), Count = 30, LifeMin = life * 0.7f, LifeMax = life, SpeedMin = radius / life * 0.6f, SpeedMax = radius / life,
                                         SizeMin = 0.06f, SizeMax = 0.1f, A = abPale, B = abBlue, Shape = Shape.Cone, Angle = half, Radius = 0.3f, Stretch = 3f, Max = 32 }, rot);
                    for (int i = 1; i <= 3; i++)
                    {
                        float d = radius * i / 3f, w = Mathf.Max(1.5f, 2f * d * Mathf.Tan(half * Mathf.Deg2Rad));
                        QuadAt(Ground(ground + dir * d + Vector3.up), Glow("fx_ring", i == 3 ? abSky : abBlue), w * 0.4f, w * 1.15f, life * 1.1f, Face.Ground, 0f, 0.1f, 0.3f, 0f, life * (i - 1) / 3f);
                    }
                    Shake(at + dir * radius * 0.5f, radius * 3f, 2f, 0.3f);
                    return null;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Seismic Impact
        static GameObject AbramsSlam(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "aim":
                {
                    // the reticle (abrams_leap_hover): RoR2's area dome in blue (it draws where it meets the ground), a turning spiral
                    // and a glyph at its centre, motes rising round its edge and a thin column of light; the state moves it every frame
                    var root = Root(at, 0f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", Hex("2C70F7"));
                    Quad(root, Glow("fx_ring_spiral", Hex("2C70F7")), 7f, 7f, 1f, Face.Ground, 140f, offset: Vector3.up * 0.35f, loop: true);
                    Quad(root, Glow("fx_ring_soft", abSky), 3.4f, 3.4f, 1f, Face.Ground, -90f, offset: Vector3.up * 0.4f, loop: true);
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.4f;
                    Emit(edge, new P { Mat = Glow("fx_glow", Hex("2C70F7")), Rate = 44, LifeMin = 0.6f, LifeMax = 0.9f, SizeMin = 0.25f, SizeMax = 0.5f, A = abSky, B = Hex("2C70F7"),
                                       Shape = Shape.Ring, Radius = radius, Velocity = Vector3.up * 2.2f, Max = 44 }, "motes");
                    var beam = Line(root.transform, Hex("2890FF"), 0.4f, Vector3.up * 0.2f, Vector3.up * 14f, "mat_tracer");
                    beam.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
                    Light(root, Hex("2C70F7"), radius, 3f, Vector3.up * 2f);
                    return root;
                }
                case "cast":
                {
                    // abrams_leap_start: he kicks off: blue rings, a streak of light going up with him, smoke and dust
                    var g = Ground(at);
                    QuadAt(g, Glow("fx_ring", Hex("52A7EE")), 0.8f, 6f, 0.4f, Face.Ground, 0f, 0.05f, 0.3f);
                    QuadAt(g, Glow("fx_ring_soft", Hex("334FFF")), 0.6f, 3.6f, 0.6f, Face.Ground, 0f, 0.05f, 0.3f, 0f, 0.06f);
                    Beam(at + Vector3.up * 0.3f, at + Vector3.up * 10f, Glow("fx_beam", Hex("6FADFF")), Hex("B1CAFF"), 1.4f, 0.5f);
                    DustPuffs(at + Vector3.up * 0.2f, 12, 2.2f, 6f, 1.7f, Hex("AEA9A3", 0.7f), Hex("7D7975", 0.5f), true, 1.2f);
                    Glint(at + Vector3.up, Hex("328AD3"), 10f, 0.6f, 5f);
                    return null;
                }
                case "loop":
                {
                    // the dive (abrams_leap_crash_trail): a wide blue ribbon and a glow tearing down behind him
                    var root = Root(at, 0f, follow);
                    var mid = new GameObject("mid"); mid.transform.SetParent(root.transform, false); mid.transform.localPosition = Vector3.up;
                    Ribbon(mid, Glow("fx_beam", abLeap), Hex("59A8F8"), Hex("1C49A4"), 1.7f, 0.4f, 0.3f);
                    Quad(mid, Glow("fx_glow_soft", abLeap), 3.2f, 3.2f, 1f, Face.Camera, loop: true);
                    Emit(mid, new P { Mat = Spark(abPale), Rate = 50, LifeMin = 0.25f, LifeMax = 0.4f, SpeedMin = 1f, SpeedMax = 4f, SizeMin = 0.06f, SizeMax = 0.12f,
                                      A = abPale, B = abLeap, Radius = 0.7f, Stretch = 3f, Max = 28 });
                    return root;
                }
                case "hit":
                {
                    // abrams_leap_impact: he lands. Blue rings race out over the ground, a column of light stands on the spot, the ground
                    // glows blue and cools, and debris, smoke and the pages of his book are thrown up
                    var g = Ground(at);
                    float d = radius * 2f;
                    QuadAt(g, Glow("fx_ring", abLeap), 2f, d * 1.15f, 0.5f, Face.Ground, 0f, 0.03f, 0.3f);
                    QuadAt(g, Glow("fx_ring_thick", Hex("3E65FE")), 1f, d * 0.8f, 0.4f, Face.Ground, 0f, 0.03f, 0.3f, 0f, 0.04f);
                    QuadAt(g, Glow("fx_ring_burst", abSky), 1f, d * 0.55f, 0.3f, Face.Ground, 0f, 0.02f, 0.3f);
                    // the hot ground cooling for a few seconds
                    QuadAt(g, Glow("fx_ring_spiral", Hex("4870FF", 0.6f)), d * 0.5f, d * 0.6f, 2.6f, Face.Ground, 20f, 0.02f, 0.25f);
                    float fl = Mathf.Min(radius, 9f);
                    QuadAt(at + Vector3.up * 2f, Glow("fx_glow", new Color(abLeap.r, abLeap.g, abLeap.b, 0.7f)), fl * 0.5f, fl * 1.5f, 0.3f, Face.Camera, 0f, 0f, 0.2f);
                    if (Rich) QuadAt(at + Vector3.up * 1.5f, Glow("fx_star", Hex("FF3FCC", 0.8f)), fl * 0.3f, fl * 0.9f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
                    Beam(at + Vector3.up * 0.2f, at + Vector3.up * 16f, Glow("fx_beam", Hex("4B7EFF")), abSky, radius * 0.35f, 0.55f);
                    Puff(at + Vector3.up * 0.3f, new P { Mat = Soft("fx_fleks", Color.white), Count = 28, LifeMin = 0.7f, LifeMax = 1.2f, SpeedMin = 6f, SpeedMax = 17f, SizeMin = 0.12f, SizeMax = 0.4f,
                                                         A = Hex("8E8E8E"), B = Hex("697082"), Shape = Shape.Up, Angle = 50f, Radius = radius * 0.3f, Gravity = 2.6f,
                                                         TilesX = 8, TilesY = 8, SpinMin = -8f, SpinMax = 8f, Max = 30 });
                    Puff(at + Vector3.up * 0.3f, new P { Mat = Spark(abPale), Count = 36, LifeMin = 0.5f, LifeMax = 1.1f, SpeedMin = 6f, SpeedMax = 18f, SizeMin = 0.08f, SizeMax = 0.18f,
                                                         A = abPale, B = Hex("1C49A4"), Shape = Shape.Up, Angle = 60f, Radius = 0.5f, Stretch = 3f, Gravity = 2.2f, Max = 38 });
                    Puff(at + Vector3.up * 0.4f, new P { Mat = Soft("fx_cloud", Color.white), Count = 18, LifeMin = 1.1f, LifeMax = 2f, SpeedMin = radius * 0.4f, SpeedMax = radius * 0.95f,
                                                         SizeMin = 2.2f, SizeMax = 3.6f, A = Hex("ADACAA", 0.75f), B = Hex("6891DA", 0.3f), Shape = Shape.Disc, Radius = 1.5f, Drag = 2.4f,
                                                         Size0 = 0.45f, Size1 = 1.5f, FadeOutFrom = 0.35f, Velocity = Vector3.up * 0.8f, SpinMin = -0.4f, SpinMax = 0.4f, Max = 20 });
                    Pages(at + Vector3.up * 0.5f, 16, 13f, radius * 0.25f, abPage);
                    Flash(at + Vector3.up * 2f, Hex("D6E2FF"), radius * 2.5f, 0.5f, 4f);
                    Shake(at, radius * 3f, 3f, 0.6f);
                    return null;
                }
            }
            return null;
        }
    }
}
