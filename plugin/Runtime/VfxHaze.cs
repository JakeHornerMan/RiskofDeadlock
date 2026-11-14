using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Haze's effects, rebuilt from Deadlock's own recipes (docs/VfxRebuild/hazeVfxRebuild.md). Deadlock's Haze is
    /// amber dream-sand and hot orange sparks over inky nightmare smoke with dark violet flecks; her dagger glows orange
    /// with a thin blue edge. (Ours used to be pink and violet.)</summary>
    public static partial class Vfx
    {
        // particles/abilities/haze: flurry #F69936 / #FFBC57 / #CC4F1A, dream sand #A07445, smoke-bomb ring #533A21 and flash #F1D2AF,
        // nightmare flecks #60308D on #1B0336, dagger sparks #F9AE75 > #C36920 > #9C3F10, dagger edge #7895EE
        static readonly Color hazeAmber = Hex("F69936"), hazeHot = Hex("FFCF78"), hazeDeep = Hex("CC4F1A"), hazeSand = Hex("D9A066"),
            hazeInk = Hex("140A22", 0.85f), hazeFleck = Hex("60308D"), hazeEdge = Hex("7895EE"), hazeFlash = Hex("F1D2AF");

        static GameObject Haze(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (id)
            {
                case "haze_gun": return HazeGun(moment, at, to);
                case "haze_dagger": return HazeDagger(moment, at, follow);
                case "haze_smoke": return HazeSmoke(moment, at);
                case "haze_phantom": return HazePhantom(moment, at, radius, to);
                case "haze_dance": return HazeDance(moment, at, radius, follow);
                case "haze_fixation": return HazeFixation(moment, at, radius);
            }
            return null;
        }

        /// <summary>Ink smoke: a few dark blotches that swell and thin out (haze_invis_smoke_bomb / _nightmare).</summary>
        static void InkPuff(Vector3 at, int count, float size, float speed, float life)
        {
            Puff(at, new P { Mat = Soft("fx_puff", Color.white), Count = count, LifeMin = life * 0.6f, LifeMax = life, SpeedMin = speed * 0.3f, SpeedMax = speed,
                             SizeMin = size * 0.7f, SizeMax = size, A = hazeInk, B = new Color(0.16f, 0.08f, 0.24f, 0.5f), FadeIn = 0.08f, FadeOutFrom = 0.35f,
                             Size0 = 0.5f, Size1 = 1.7f, Radius = 0.35f, Drag = 2.5f, SpinMin = -0.6f, SpinMax = 0.6f, Max = 24 });
        }

        // ------------------------------------------------------------------ Twin SMGs
        static GameObject HazeGun(string moment, Vector3 at, Vector3? to)
        {
            switch (moment)
            {
                case "muzzle":
                    // the anime muzzle flash: a wide hot flare and an amber glow, gone in two frames
                    if (!Every("haze_mz", 0.045f)) return null;
                    QuadAt(at, Glow("fx_flare_wide", hazeHot), 0.5f, 1.1f, 0.07f, Face.Camera, 0f, 0f, 0.2f, Random.Range(-20f, 20f));
                    if (Rich) QuadAt(at, Glow("fx_glow", hazeAmber), 0.5f, 0.9f, 0.09f, Face.Camera, 0f, 0f, 0.2f);
                    return null;
                case "hit":
                    if (!Every("haze_hit", 0.05f)) return null;
                    QuadAt(at, Glow("fx_star", hazeHot), 0.25f, 0.75f, 0.1f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Puff(at, new P { Mat = Mat("mat_spark", hazeAmber), Count = 5, LifeMin = 0.12f, LifeMax = 0.25f, SpeedMin = 3f, SpeedMax = 8f, SizeMin = 0.04f, SizeMax = 0.08f,
                                     A = hazeHot, B = hazeDeep, Stretch = 3f, Gravity = 1.5f, Max = 8 });
                    return null;
                case "impact":
                    if (!Every("haze_imp", 0.08f) || !Rich) return null;
                    Puff(at, new P { Mat = Soft("fx_fleks", Color.white), Count = 3, LifeMin = 0.3f, LifeMax = 0.6f, SpeedMin = 1.5f, SpeedMax = 4f, SizeMin = 0.06f, SizeMax = 0.12f,
                                     A = hazeSand, B = hazeDeep, Gravity = 2.5f, TilesX = 8, TilesY = 8, SpinMin = -6f, SpinMax = 6f, Max = 6 });
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Sleep Dagger
        static GameObject HazeDagger(string moment, Vector3 at, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // haze_sleep_dagger_proj: an orange glow and the blade's shape, a blue edge whirling round it, a ring of sparks,
                    // and two trails: bright amber inside a wider streak of darkness
                    var root = Root(at, 0f, follow);
                    Quad(root, Glow("fx_glow_soft", hazeAmber), 1.7f, 1.7f, 1f, Face.Camera, loop: true);
                    Quad(root, Glow("haze_dagger_tex", hazeHot), 0.9f, 0.9f, 1f, Face.Camera, 520f, loop: true);
                    Quad(root, Glow("fx_swoop", hazeEdge), 1.3f, 1.3f, 1f, Face.Camera, -300f, loop: true);
                    Ribbon(root, Soft("fx_beam", Color.white), hazeInk, new Color(0.1f, 0.05f, 0.16f, 0.6f), 0.7f, 0.4f, 0.2f);
                    Ribbon(root, Glow("fx_beam", hazeAmber), hazeHot, hazeDeep, 0.3f, 0.22f);
                    Emit(root, new P { Mat = Mat("mat_spark", hazeAmber), Rate = 36, LifeMin = 0.2f, LifeMax = 0.4f, SpeedMin = 0.5f, SpeedMax = 2.2f, SizeMin = 0.04f, SizeMax = 0.09f,
                                       A = hazeHot, B = hazeDeep, Radius = 0.3f, Stretch = 2.5f, Max = 24 });
                    Light(root, hazeAmber, 6f, 2.5f, Vector3.zero);
                    return root;
                }
                case "hit":
                    // haze_sleep_dagger_impact: sparks, a puff of dark gas turning orange, a flare, a few chips
                    QuadAt(at, Glow("fx_flare", hazeHot), 0.6f, 2.4f, 0.16f, Face.Camera, 0f, 0f, 0.25f);
                    QuadAt(at, Glow("fx_ring_burst", hazeAmber), 0.4f, 2.6f, 0.25f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Puff(at, new P { Mat = Mat("mat_spark", hazeAmber), Count = 20, LifeMin = 0.3f, LifeMax = 0.7f, SpeedMin = 2f, SpeedMax = 11f, SizeMin = 0.05f, SizeMax = 0.1f,
                                     A = Hex("F9AE75"), B = Hex("9C3F10"), Stretch = 3f, Gravity = 2.2f, Drag = 1f, Max = 24 });
                    Puff(at, new P { Mat = Soft("fx_puff", Color.white), Count = 4, LifeMin = 0.25f, LifeMax = 0.5f, SpeedMin = 0.5f, SpeedMax = 3f, SizeMin = 0.7f, SizeMax = 1.3f,
                                     A = Hex("29211C", 0.5f), B = Hex("DE7A34", 0.35f), Size0 = 0.4f, Size1 = 1.5f, Drag = 2f, Max = 6 });
                    if (Rich) Puff(at, new P { Mat = Soft("fx_fleks", Color.white), Count = 4, LifeMin = 0.5f, LifeMax = 1.1f, SpeedMin = 3f, SpeedMax = 5f, SizeMin = 0.08f, SizeMax = 0.16f,
                                               A = Hex("D6AC8C"), B = Hex("AB9F92"), Gravity = 2.5f, TilesX = 8, TilesY = 8, SpinMin = -9f, SpinMax = 9f, Max = 6 });
                    Glint(at, Hex("E58C3F"), 6f, 0.15f, 5f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Smoke Bomb
        static GameObject HazeSmoke(string moment, Vector3 at)
        {
            if (moment != "cast") return null;
            // haze_invis_smoke_bomb: a burst of inky smoke, a pale flash, a tan ring on the ground, dream sand drifting up, violet flecks
            var mid = at + Vector3.up * 1f;
            InkPuff(mid, 14, 2.6f, 4.5f, 1.5f);
            QuadAt(mid, Glow("fx_flare", hazeFlash), 0.8f, 3.6f, 0.16f, Face.Camera, 0f, 0f, 0.2f);
            QuadAt(at, Glow("fx_ring", Hex("C9965A", 0.7f)), 0.8f, 7.5f, 0.5f, Face.Ground, 0f, 0.05f, 0.25f);
            Puff(mid, new P { Mat = Glow("fx_plasma", hazeFleck), Count = 5, LifeMin = 0.6f, LifeMax = 1.1f, SpeedMin = 0.5f, SpeedMax = 2f, SizeMin = 1.6f, SizeMax = 2.4f,
                              A = hazeFleck, B = Hex("1B0336"), Size0 = 0.6f, Size1 = 1.4f, SpinMin = -1f, SpinMax = 1f, Radius = 0.5f, Max = 8 });
            Puff(at + Vector3.up * 0.3f, new P { Mat = Glow("fx_glow", hazeSand), Count = 22, LifeMin = 0.9f, LifeMax = 1.7f, SpeedMin = 0.3f, SpeedMax = 1.6f, SizeMin = 0.05f, SizeMax = 0.12f,
                                                 A = hazeSand, B = hazeAmber, Shape = Shape.Disc, Radius = 1.6f, Velocity = Vector3.up * 1.4f, Noise = 0.8f, FadeOutFrom = 0.6f, Max = 26 });
            if (Rich) Puff(mid, new P { Mat = Soft("fx_fleks", Color.white), Count = 10, LifeMin = 0.5f, LifeMax = 1f, SpeedMin = 2f, SpeedMax = 6f, SizeMin = 0.07f, SizeMax = 0.14f,
                                        A = hazeFleck, B = Hex("1B0336"), Gravity = 1.2f, TilesX = 8, TilesY = 8, SpinMin = -8f, SpinMax = 8f, Drag = 1.5f, Max = 12 });
            return null;
        }

        // ------------------------------------------------------------------ Phantom Strike
        static GameObject HazePhantom(string moment, Vector3 at, float radius, Vector3? to)
        {
            switch (moment)
            {
                case "cast":
                {
                    // ink where she vanishes and where she lands; between them a hot amber streak inside a wider streak of darkness
                    var end = to ?? at;
                    InkPuff(at, 6, 1.8f, 2.5f, 0.9f);
                    InkPuff(end, 5, 1.6f, 3f, 0.8f);
                    Beam(at, end, Soft("fx_beam", Color.white), hazeInk, 1.5f, 0.4f);
                    Beam(at, end, Glow("fx_beam", hazeAmber), hazeHot, 0.6f, 0.25f);
                    int n = Mathf.Clamp((int)(Vector3.Distance(at, end) / 3f), 2, 5);
                    for (int i = 1; i <= n; i++)
                        QuadAt(Vector3.Lerp(at, end, i / (n + 1f)), Glow("fx_glow_soft", i % 2 == 0 ? hazeAmber : hazeDeep), 1.6f, 0.6f, 0.3f + i * 0.04f, Face.Camera, 0f, 0f, 0.1f);
                    QuadAt(end, Glow("fx_flare", hazeHot), 0.6f, 3f, 0.18f, Face.Camera, 0f, 0f, 0.2f);
                    QuadAt(Ground(end), Glow("fx_ring", hazeAmber), 0.5f, 4.5f, 0.3f, Face.Ground, 0f, 0.05f, 0.25f);
                    Glint(end, hazeAmber, 8f, 0.3f, 5f);
                    return null;
                }
                case "hit":
                {
                    // a curved cut across the target, turned a different way each time (radius = which cut), sparks off the blade
                    int cut = Mathf.RoundToInt(radius);
                    float roll = cut * 70f + 25f;
                    QuadAt(at, Glow("fx_swoop", hazeHot), 1.4f, 3.2f, 0.18f, Face.Camera, 0f, 0f, 0.3f, roll);
                    QuadAt(at, Glow("fx_swoop", hazeEdge), 1.1f, 2.6f, 0.24f, Face.Camera, 0f, 0f, 0.3f, roll + 180f);
                    QuadAt(at, Glow("fx_star", hazeHot), 0.4f, 1.3f, 0.12f, Face.Camera, 0f, 0f, 0.3f, roll);
                    Puff(at, new P { Mat = Mat("mat_spark", hazeAmber), Count = 12, LifeMin = 0.2f, LifeMax = 0.4f, SpeedMin = 3f, SpeedMax = 10f, SizeMin = 0.05f, SizeMax = 0.09f,
                                     A = hazeHot, B = hazeDeep, Stretch = 3f, Gravity = 1.5f, Max = 14 });
                    if (cut >= 3)
                    {
                        QuadAt(at, Glow("fx_ring_burst", hazeAmber), 0.6f, 5f, 0.3f, Face.Camera, 0f, 0f, 0.3f, roll);
                        InkPuff(at, 4, 1.4f, 3f, 0.6f);
                        Glint(at, hazeAmber, 9f, 0.25f, 6f);
                    }
                    return null;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Bullet Dance
        static GameObject HazeDance(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // haze_bullet_flurry_cast: amber rings rolling out over the ground three times a second, two smears whirling
                    // round her (one bright, one dark), a swirl under her feet and streaks of light rising through it
                    var root = Root(at, 0f, follow);
                    Quad(root, Glow("fx_ring_spiral", hazeAmber), 5f, 5f, 1f, Face.Ground, 620f, offset: Vector3.up * 0.12f, loop: true);
                    Quad(root, Soft("fx_spin", new Color(0.08f, 0.04f, 0.13f, 0.45f)), 4.2f, 4.2f, 1f, Face.Ground, -480f, offset: Vector3.up * 0.08f, loop: true);
                    Quad(root, Glow("fx_swoop", new Color(0.8f, 0.42f, 0.14f, 0.7f)), 3.4f, 3.4f, 1f, Face.Ground, 900f, offset: Vector3.up * 1.0f, loop: true);
                    Quad(root, Glow("fx_swoop", new Color(0.75f, 0.3f, 0.08f, 0.7f)), 2.8f, 2.8f, 1f, Face.Ground, -760f, offset: Vector3.up * 1.45f, loop: true);
                    Emit(root, new P { Mat = Glow("fx_ring", hazeAmber), Rate = 3f, LifeMin = 0.7f, LifeMax = 0.7f, SizeMin = 9f, SizeMax = 9f, A = hazeAmber, B = hazeDeep,
                                       Shape = Shape.Point, Flat = true, Size0 = 0.15f, Size1 = 1f, FadeIn = 0.05f, FadeOutFrom = 0.3f, World = false, RandomRoll = false, Max = 4 }, "rings")
                        .transform.localPosition = Vector3.up * 0.15f;
                    Emit(root, new P { Mat = Mat("mat_spark", hazeAmber), Rate = 34, LifeMin = 0.3f, LifeMax = 0.5f, SizeMin = 0.07f, SizeMax = 0.13f, A = hazeHot, B = hazeDeep,
                                       Shape = Shape.Ring, Radius = 1.5f, Velocity = Vector3.up * 5.5f, Stretch = 4f, World = false, Max = 24 }, "streaks");
                    if (Rich) Emit(root, new P { Mat = Glow("fx_glow", hazeSand), Rate = 14, LifeMin = 0.6f, LifeMax = 1f, SizeMin = 0.05f, SizeMax = 0.1f, A = hazeSand, B = hazeAmber,
                                                 Shape = Shape.Disc, Radius = 2.2f, Velocity = Vector3.up * 1.2f, Orbit = 2.5f, World = false, Max = 16 }, "sand");
                    Light(root, hazeAmber, 9f, 2.6f, Vector3.up);
                    return root;
                }
                case "cast":
                    // one shot of the dance (haze_flurry_muzzle_flash)
                    if (!Every("haze_dance_mz", 0.04f)) return null;
                    QuadAt(at, Glow("fx_flare_wide", hazeHot), 0.5f, 1.2f, 0.07f, Face.Camera, 0f, 0f, 0.2f, Random.Range(-30f, 30f));
                    return null;
                case "hit":
                    if (!Every("haze_dance_hit", 0.035f)) return null;
                    QuadAt(at, Glow("fx_star", hazeHot), 0.3f, 0.95f, 0.1f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Puff(at, new P { Mat = Mat("mat_spark", hazeAmber), Count = 6, LifeMin = 0.15f, LifeMax = 0.3f, SpeedMin = 3f, SpeedMax = 8f, SizeMin = 0.05f, SizeMax = 0.08f,
                                     A = Hex("FFBC57"), B = Hex("F07C2F"), Stretch = 3f, Gravity = 1.2f, Max = 8 });
                    return null;
                case "end":
                    QuadAt(Ground(at), Glow("fx_ring", hazeAmber), 0.8f, 9f, 0.45f, Face.Ground, 0f, 0.05f, 0.25f);
                    QuadAt(at + Vector3.up, Glow("fx_flare", hazeHot), 0.8f, 3.5f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
                    InkPuff(at + Vector3.up * 0.8f, 5, 1.8f, 3f, 0.8f);
                    Glint(at + Vector3.up, hazeAmber, 10f, 0.3f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Fixation (passive)
        static GameObject HazeFixation(string moment, Vector3 at, float radius)
        {
            switch (moment)
            {
                case "hit":
                    // the first stack: a small amber sight closing on the target (haze_max_stacks_debuff_ring)
                    QuadAt(at, Glow("fx_ring", Hex("F8C272")), 1.6f, 0.7f, 0.25f, Face.Camera, 0f, 0.1f, 0.6f);
                    return null;
                case "trigger":
                    // ten stacks: the sight snaps shut, a burst ring, a spray of orange streaks (haze_max_stacks_start)
                    QuadAt(at, Glow("fx_ring_burst", Hex("F19028")), 0.6f, Mathf.Max(3f, radius * 1.1f), 0.3f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_flare", Hex("FF6982")), 0.5f, 2.2f, 0.16f, Face.Camera, 0f, 0f, 0.25f);
                    Puff(at, new P { Mat = Mat("mat_spark", hazeAmber), Count = 22, LifeMin = 0.25f, LifeMax = 0.5f, SpeedMin = 4f, SpeedMax = 13f, SizeMin = 0.05f, SizeMax = 0.1f,
                                     A = Hex("FF8B24"), B = Hex("D83E06"), Stretch = 3.5f, Drag = 1.5f, Max = 24 });
                    Glint(at, Hex("E98616"), 7f, 0.25f, 5f);
                    return null;
            }
            return null;
        }
    }
}
