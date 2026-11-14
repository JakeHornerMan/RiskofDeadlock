using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Mo &amp; Krill's effects after the 2026-11 rebuild (docs/VfxRebuild/mokrillVfxRebuild.md). Their colours already
    /// came from Deadlock's particles (tan sand, orange heat, dark cracks), so the palette stays. What changed is how it is
    /// drawn: Deadlock's cartoon dust clouds, debris flecks, rings and ground marks instead of piles of RoR2 smoke and sparks,
    /// at a fraction of the particle count (Sand Blast's wave alone emitted about 1000 particles a second; now about 180).</summary>
    public static partial class Vfx
    {
        // particles/abilities/digger: sand #ECDABA / #C7B18D / #AC9371, throw ring #E28420 > #451B0C, light #FF911C, cone lines
        // #DCAA72 / #FFE1C5; burrow ring #FC7208, cracks #332E29, burst cracks #FE843E / #9F4217, hot ground #ED6B14, dark ground
        // #19060C, debris #8D583C / #6A6A6A, smoke ends #F28B47; Krill's arcs #FED776 / #FF9B22, ring #F97D3A;
        // weapon_fx/digger: muzzle #FF8B24 / #FECF78, pellets #FFD455 > #F56A00
        static readonly Color moSand = Hex("ECDABA", 0.55f), moSandMid = Hex("C7B18D", 0.5f), moSandDeep = Hex("AC9371", 0.5f), moSandEnd = Hex("B9A684", 0.15f),
            moDebris = Hex("8D583C"), moDebrisGrey = Hex("6A6A6A"), moDarkGround = Hex("19060C", 0.8f), moBurst = Hex("FE843E"), moHotGround = Hex("ED6B14", 0.7f),
            moRing = Hex("FC7208"), krillRing = Hex("F97D3A"), krillHot = Hex("FED776"), krillDeep = Hex("FF9B22");

        /// <summary>Chunks of dirt and stone thrown up and falling back (digger_burrow_start_debris, _explode_debris).</summary>
        static void MoDebris(Vector3 at, int count, float speed, float radius, float angle = 45f)
        {
            Puff(at, new P { Mat = Soft("fx_fleks", Color.white), Count = count, LifeMin = 0.6f, LifeMax = 1.1f, SpeedMin = speed * 0.4f, SpeedMax = speed, SizeMin = 0.12f, SizeMax = 0.34f,
                             A = moDebris, B = moDebrisGrey, Shape = Shape.Up, Angle = angle, Radius = radius, Gravity = 2.6f, FadeIn = 0.02f, FadeOutFrom = 0.8f,
                             TilesX = 8, TilesY = 8, SpinMin = -8f, SpinMax = 8f, Max = count + 2 });
        }

        static GameObject MokrillGun(string moment, Vector3 at, Transform follow)
        {
            switch (moment)
            {
                case "muzzle":
                    // default_muzzle_flash in Krill's oranges: a ragged flash, a hot core, a wisp of grey smoke on some shots
                    if (!Every("mokrill_mz", 0.07f)) return null;
                    QuadAt(at, Glow("fx_muzzle", Hex("FF8B24")), 0.6f, 1.5f, 0.09f, Face.Camera, 0f, 0f, 0.25f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_glow", Hex("FECF78")), 0.4f, 0.9f, 0.1f, Face.Camera, 0f, 0f, 0.2f);
                    if (Rich && Every("mokrill_mz_smoke", 0.2f))
                        Puff(at, new P { Mat = Soft("fx_cloud", Color.white), Count = 2, LifeMin = 0.4f, LifeMax = 0.7f, SpeedMin = 0.4f, SpeedMax = 1.4f, SizeMin = 0.35f, SizeMax = 0.6f,
                                         A = Hex("9C9C9C", 0.4f), B = Hex("9C9C9C", 0.15f), Size0 = 0.5f, Size1 = 1.6f, Drag = 2f, Max = 3 });
                    return null;
                case "hit":
                    // digger_tracer_impact: a hot star, yellow-orange bits flung off, dirt chips
                    if (!Every("mokrill_hit", 0.06f)) return null;
                    QuadAt(at, Glow("fx_star", Hex("FFD455")), 0.3f, 1f, 0.1f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Puff(at, new P { Mat = Mat("mat_spark", krillOrange), Count = 6, LifeMin = 0.2f, LifeMax = 0.4f, SpeedMin = 3f, SpeedMax = 8f, SizeMin = 0.05f, SizeMax = 0.08f,
                                     A = Hex("FFFB28"), B = Hex("DD9913"), Stretch = 3f, Gravity = 1.5f, Max = 7 });
                    if (Rich) MoDebris(at, 3, 4f, 0.1f, 60f);
                    return null;
            }
            return null;
        }

        static GameObject MokrillSand(string moment, Vector3 at, float radius, Transform follow, Vector3? to, float spreadDeg, float windUp)
        {
            switch (moment)
            {
                case "telegraph":
                {
                    // the wind-up: the cone on the ground (edges and far end following the terrain, faint cross lines) and a
                    // bright line sweeping out from Mo; it stays while the wave runs along it, then fades
                    var end = to ?? at + Vector3.forward;
                    var dir = end - at; dir.y = 0;
                    float range = dir.magnitude;
                    var root = Root(at, windUp + 1.2f, null);
                    var m = root.AddComponent<ConeMarker>();
                    m.Origin = at; m.Dir = dir.normalized; m.Range = range; m.Near = radius * 0.5f;
                    m.Tan = Mathf.Tan(spreadDeg * Mathf.Deg2Rad); m.Sweep = Mathf.Max(0.1f, windUp);
                    return root;
                }
                case "gather":
                {
                    // digger_throw_sand_cast: Mo scooping up sand: grains drawn in to his hands, a spiral winding up, an orange glow growing
                    var root = Root(at, 0f, follow);
                    Emit(root, new P { Mat = Mat("mat_spark", sandEdge), Rate = 60, LifeMin = 0.3f, LifeMax = 0.36f, SizeMin = 0.07f, SizeMax = 0.11f, A = Hex("FFE1C5"), B = sandEdge,
                                       Radius = 2.2f, Radial = -6.2f, Stretch = 2f, World = false, Max = 26 }, "grains");
                    Quad(root, Glow("fx_ring_spiral", Hex("E28420")), 1.7f, 1.7f, 1f, Face.Camera, -420f, loop: true);
                    Quad(root, Glow("fx_glow", new Color(moLight.r, moLight.g, moLight.b, 0.7f)), 1.1f, 1.1f, 1f, Face.Camera, loop: true);
                    var glow = Light(root, moLight, 6f, 0.5f, Vector3.zero);
                    var g = glow.gameObject.AddComponent<GrowLight>(); g.To = 5f; g.Over = 0.4f;
                    return root;
                }
                case "cast":
                {
                    // the throw: a spiked ring and a flash at Mo's hands, a ring over the ground, sand clouds and grit thrown along the cone
                    QuadAt(at, Glow("fx_ring_burst", Hex("E28420")), 0.5f, 4.5f, 0.3f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_glow", new Color(moLight.r, moLight.g, moLight.b, 0.8f)), 1f, 3.2f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
                    QuadAt(Ground(at), Glow("fx_ring", sandEdge), 0.6f, Mathf.Min(radius * 2f, 12f), 0.4f, Face.Ground, 0f, 0.05f, 0.3f);
                    var dir = to.HasValue ? to.Value - at : Vector3.forward;
                    if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
                    var rot = Quaternion.LookRotation(dir);
                    Puff(at, new P { Mat = Soft("fx_cloud", Color.white), Count = 12, LifeMin = 0.6f, LifeMax = 1f, SpeedMin = 8f, SpeedMax = 22f, SizeMin = 1.3f, SizeMax = 2.2f,
                                     A = moSand, B = moSandEnd, Shape = Shape.Cone, Angle = 18f, Radius = 0.4f, Drag = 2.5f, Size0 = 0.45f, Size1 = 1.5f, FadeOutFrom = 0.4f,
                                     SpinMin = -0.6f, SpinMax = 0.6f, Max = 14 }, rot);
                    Puff(at, new P { Mat = Mat("mat_spark", sandEdge), Count = 36, LifeMin = 0.35f, LifeMax = 0.55f, SpeedMin = 14f, SpeedMax = 30f, SizeMin = 0.08f, SizeMax = 0.14f,
                                     A = Hex("FFE1C5"), B = sandEdge, Shape = Shape.Cone, Angle = 18f, Radius = 0.4f, Stretch = 3f, Gravity = 1f, Max = 38 }, rot);
                    Glint(at, moLight, 14f, 0.3f, 6f);
                    Shake(at, 18f, 3f, 0.35f);
                    return null;
                }
                case "loop":
                {
                    // digger_throw_sand_linear: the wave: a rolling wall of cartoon sand clouds racing along the ground, grit flying ahead
                    // of it, chips of dirt kicked up, dust left hanging where it has passed, and a bright ribbon at each end of the
                    // front. SetWave moves it, turns it and sets its width. The clouds are alpha-blended so they read on bright ground
                    var root = Root(at, 0f, null);
                    var wave = new GameObject("wave"); wave.transform.SetParent(root.transform, false);
                    var box = new Vector3(radius, 2.2f, 0.8f);
                    Emit(wave, new P { Mat = Soft("fx_cloud", Color.white), Rate = 15, LifeMin = 0.6f, LifeMax = 0.85f, SpeedMin = 1.5f, SpeedMax = 3.5f, SizeMin = 1.7f, SizeMax = 2.5f,
                                       A = moSand, B = moSandEnd, Shape = Shape.Box, Box = box, Size0 = 0.5f, Size1 = 1.4f, FadeIn = 0.08f, FadeOutFrom = 0.4f,
                                       Velocity = Vector3.up * 0.8f, SpinMin = -0.6f, SpinMax = 0.6f, Max = 34 }, "clouds");
                    Emit(wave, new P { Mat = Mat("mat_spark", sandEdge), Rate = 110, LifeMin = 0.35f, LifeMax = 0.5f, SpeedMin = 5f, SpeedMax = 10f, SizeMin = 0.08f, SizeMax = 0.13f,
                                       A = Hex("FFE1C5"), B = sandEdge, Shape = Shape.Box, Box = box, Stretch = 2.5f, Gravity = 1f, Max = 110 }, "grit");
                    Emit(wave, new P { Mat = Soft("fx_fleks", Color.white), Rate = 30, LifeMin = 0.5f, LifeMax = 0.8f, SpeedMin = 3f, SpeedMax = 7f, SizeMin = 0.12f, SizeMax = 0.26f,
                                       A = moDebris, B = moDebrisGrey, Shape = Shape.Box, Box = box, Gravity = 2.2f, Velocity = Vector3.up * 5f, FadeIn = 0.02f, FadeOutFrom = 0.8f,
                                       TilesX = 8, TilesY = 8, SpinMin = -8f, SpinMax = 8f, Max = 40 }, "chips");
                    var trail = new GameObject("trail"); trail.transform.SetParent(root.transform, false);
                    Emit(trail, new P { Mat = Soft("fx_cloud", Color.white), Rate = 6, LifeMin = 1f, LifeMax = 1.4f, SpeedMin = 0.1f, SpeedMax = 0.4f, SizeMin = 1.6f, SizeMax = 2.2f,
                                        A = new Color(moSandMid.r, moSandMid.g, moSandMid.b, 0.5f), B = moSandEnd, Shape = Shape.Box, Box = new Vector3(radius, 0.4f, 0.4f),
                                        Size0 = 0.6f, Size1 = 1.4f, FadeIn = 0.15f, FadeOutFrom = 0.35f, SpinMin = -0.3f, SpinMax = 0.3f, Max = 30 }, "settle");
                    foreach (var side in new[] { "edgeL", "edgeR" })
                    {
                        var e = new GameObject(side); e.transform.SetParent(root.transform, false);
                        Ribbon(e, Glow("fx_beam", Hex("DCAA72")), Hex("FFE1C5"), Hex("5C5145"), 0.7f, 0.6f);
                    }
                    Light(root, moLight, 10f, 1.5f, Vector3.up);
                    return root;
                }
                case "hit":
                    // digger_throw_sand_debuff_flash: a burst of sand and a flash on each enemy the wave catches, then (SandedFx) its
                    // model coated in sand and sand swirling round it while it is Sanded
                    if (follow && !follow.GetComponent<SandedFx>()) follow.gameObject.AddComponent<SandedFx>();
                    if (!Every("mokrill_sand_hit", 0.05f)) return null;
                    QuadAt(at, Glow("fx_ring_burst", sandEdge), 0.5f, 3.5f, 0.3f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_glow", new Color(moOrange.r, moOrange.g, moOrange.b, 0.8f)), 0.8f, 2.8f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
                    DustPuffs(at, 5, 1.6f, 5f, 0.9f, moSand, moSandEnd, false, 0.5f);
                    Puff(at, new P { Mat = Mat("mat_spark", sandEdge), Count = 14, LifeMin = 0.3f, LifeMax = 0.5f, SpeedMin = 4f, SpeedMax = 12f, SizeMin = 0.08f, SizeMax = 0.12f,
                                     A = Hex("F5A158"), B = Hex("DA8122"), Stretch = 3f, Gravity = 1.5f, Max = 16 });
                    Glint(at, moLight, 8f, 0.25f, 5f);
                    return null;
            }
            return null;
        }

        static GameObject MokrillBurrow(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // digger_burrow_start: digging in: the ground darkens and cracks, an orange ring, dirt and stone thrown up, dust, a thump
                    var g = Ground(at);
                    QuadAt(g, Soft("fx_puff", moDarkGround), 2f, 4.4f, 3f, Face.Ground, 0f, 0.05f, 0.7f, Random.Range(0f, 360f));
                    QuadAt(g, Glow("fx_ring_burst", moRing), 0.6f, 5.5f, 0.35f, Face.Ground, 0f, 0.05f, 0.3f, Random.Range(0f, 360f));
                    DustPuffs(at + Vector3.up * 0.2f, 6, 1.3f, 5f, 1.2f, moSandDeep, moSandMid, true, 1.2f);
                    MoDebris(at + Vector3.up * 0.2f, 24, 10f, 0.8f);
                    Glint(at + Vector3.up, Hex("FF9E58"), 8f, 0.4f, 4f);
                    Shake(at, 12f, 1.5f, 0.25f);
                    return null;
                }
                case "under":
                    // the ground closing over him: dirt falling in, a dark ring drawing shut
                    QuadAt(Ground(at), Soft("fx_ring_soft", crack), 5f, 0.8f, 0.3f, Face.Ground, 0f, 0.05f, 0.5f);
                    MoDebris(at + Vector3.up * 0.2f, 12, 4f, 1.1f, 30f);
                    DustPuffs(at + Vector3.up * 0.2f, 4, 1.5f, 1.5f, 0.9f, moSandDeep, moSandMid, true, 0.9f);
                    return null;
                case "loop":
                {
                    // digger_burrow_channel: the mound travelling with him: cartoon dust boiling up, chips of stone bobbing, a turning
                    // orange ring on the ground, a dark crack left behind
                    var root = Root(at, 0f, follow);
                    Emit(root, new P { Mat = Soft("fx_cloud", Color.white), Rate = 9, LifeMin = 0.9f, LifeMax = 1.4f, SpeedMin = 0.5f, SpeedMax = 1.6f, SizeMin = 1.2f, SizeMax = 1.9f,
                                       A = moSandDeep, B = moSandEnd, Shape = Shape.Disc, Radius = 0.9f, Size0 = 0.5f, Size1 = 1.5f, FadeIn = 0.1f, FadeOutFrom = 0.4f,
                                       Velocity = Vector3.up * 0.9f, SpinMin = -0.5f, SpinMax = 0.5f, Max = 16 }, "dust");
                    Emit(root, new P { Mat = Soft("fx_fleks", Color.white), Rate = 22, LifeMin = 0.4f, LifeMax = 0.75f, SpeedMin = 2f, SpeedMax = 4.5f, SizeMin = 0.1f, SizeMax = 0.26f,
                                       A = Hex("806D59"), B = Hex("3B3B3B"), Shape = Shape.Up, Angle = 35f, Radius = 0.9f, Gravity = 2f, FadeIn = 0.02f, FadeOutFrom = 0.8f,
                                       TilesX = 8, TilesY = 8, SpinMin = -8f, SpinMax = 8f, Max = 20 }, "chips");
                    Quad(root, Glow("fx_ring_soft", new Color(moRing.r, moRing.g, moRing.b, 0.7f)), 2.8f, 2.8f, 1f, Face.Ground, 70f, offset: Vector3.up * 0.12f, loop: true);
                    var low = new GameObject("crack"); low.transform.SetParent(root.transform, false); low.transform.localPosition = Vector3.up * 0.1f;
                    Ribbon(low, Soft("fx_beam", Color.white), new Color(crack.r, crack.g, crack.b, 0.7f), new Color(0.13f, 0.13f, 0.13f, 0f), 1.1f, 1.5f, 0.5f);
                    return root;
                }
                case "hit":
                {
                    // digger_burrow_explode: bursting out: orange rings racing to the spin's reach, the ground dark and cracked and glowing
                    // hot for a few seconds, a flash, a fountain of stone and dust thrown up, dust rolling away, a shake
                    var g = Ground(at);
                    float d = radius * 2f, fl = Mathf.Min(radius, 8f);
                    QuadAt(g, Glow("fx_ring", moBurst), 1f, d * 1.1f, 0.45f, Face.Ground, 0f, 0.03f, 0.3f);
                    QuadAt(g, Glow("fx_ring_burst", Hex("FF853E")), 0.8f, d * 0.7f, 0.3f, Face.Ground, 0f, 0.03f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(g, Soft("fx_puff", moDarkGround), d * 0.4f, d * 0.62f, 3f, Face.Ground, 0f, 0.03f, 0.7f, Random.Range(0f, 360f));
                    QuadAt(g, Glow("fx_ring_spiral", moHotGround), d * 0.36f, d * 0.46f, 2.2f, Face.Ground, 20f, 0.03f, 0.3f);
                    QuadAt(at + Vector3.up, Glow("fx_star", new Color(1f, 0.52f, 0.24f, 0.8f)), fl * 0.4f, fl * 1.3f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
                    MoDebris(at + Vector3.up * 0.3f, 30, 15f, radius * 0.25f, 50f);
                    Puff(at + Vector3.up * 0.3f, new P { Mat = Mat("mat_spark", hotCrack), Count = 20, LifeMin = 0.3f, LifeMax = 0.7f, SpeedMin = 5f, SpeedMax = 14f, SizeMin = 0.06f, SizeMax = 0.12f,
                                                         A = Hex("FFC98A"), B = Hex("9F4217"), Shape = Shape.Up, Angle = 60f, Stretch = 3f, Gravity = 2f, Max = 22 });
                    Puff(at + Vector3.up * 0.4f, new P { Mat = Soft("fx_cloud", Color.white), Count = 10, LifeMin = 0.9f, LifeMax = 1.5f, SpeedMin = radius * 0.4f, SpeedMax = radius * 0.9f,
                                                         SizeMin = 1.3f, SizeMax = 2.1f, A = moSandMid, B = moSandEnd, Shape = Shape.Disc, Radius = 1.4f, Drag = 2.4f,
                                                         Size0 = 0.45f, Size1 = 1.5f, FadeOutFrom = 0.35f, Velocity = Vector3.up * 0.8f, SpinMin = -0.4f, SpinMax = 0.4f, Max = 16 });
                    Puff(at + Vector3.up * 0.5f, new P { Mat = Soft("fx_cloud", Color.white), Count = 5, LifeMin = 0.7f, LifeMax = 1.1f, SpeedMin = 6f, SpeedMax = 11f, SizeMin = 1f, SizeMax = 1.6f,
                                                         A = moSand, B = moSandEnd, Shape = Shape.Up, Angle = 14f, Radius = 0.6f, Drag = 2f, Size0 = 0.5f, Size1 = 1.5f, FadeOutFrom = 0.4f, Max = 8 });
                    Glint(at + Vector3.up * 1.5f, moLight, radius * 2.5f, 0.5f, 6f);
                    Shake(at, radius * 3f, 4f, 0.5f);
                    return null;
                }
            }
            return null;
        }

        static GameObject MokrillDevil(string moment, Vector3 at, float radius, Transform follow, float pull)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the tornado (styled on digger_burrow_spin): thin bright ribbons of sand winding round Mo, wider with height (the old
                    // build's clean swirl lines, kept: stacked fan sprites drew as flat cream discs, seen in game 2026-11-12), two
                    // faint spirals turning on the ground and at his chest, a little cartoon dust carried up the column and rolling at
                    // its foot, the damage ring on the ground (area dome) and grit streaming inward from the pull's edge
                    var root = Root(at, 0f, follow);
                    Quad(root, Glow("fx_ring_spiral", Hex("FFE1C5")), 4.6f, 4.6f, 1f, Face.Ground, 520f, offset: Vector3.up * 0.3f, loop: true);
                    Quad(root, Glow("fx_ring_spiral", new Color(1f, 0.88f, 0.77f, 0.6f)), 5.8f, 5.8f, 1f, Face.Ground, -380f, offset: Vector3.up * 2.4f, loop: true);
                    var spin = new GameObject("spin"); spin.transform.SetParent(root.transform, false);
                    spin.AddComponent<Spin>().DegPerSec = 720f;
                    for (int i = 0; i < 4; i++)
                    {
                        var arm = new GameObject("arm" + i); arm.transform.SetParent(spin.transform, false);
                        float a = i * 90f * Mathf.Deg2Rad;
                        arm.transform.localPosition = new Vector3(Mathf.Cos(a) * (1.4f + i * 0.5f), 0.6f + i * 1.1f, Mathf.Sin(a) * (1.4f + i * 0.5f));
                        Trail(arm, sandEdge, 0.3f, 0.25f);   // the old build's own trail: the kit ribbon came out too faint here twice (2026-11-13)
                    }
                    Emit(root, new P { Mat = Soft("fx_cloud", Color.white), Rate = 7, LifeMin = 0.6f, LifeMax = 0.9f, SizeMin = 0.9f, SizeMax = 1.4f, A = new Color(moSandMid.r, moSandMid.g, moSandMid.b, 0.35f), B = moSandEnd,
                                       Shape = Shape.Disc, Radius = 1.7f, Orbit = 5f, Velocity = Vector3.up * 3.8f, World = false, Size0 = 0.5f, Size1 = 1.4f,
                                       FadeIn = 0.12f, FadeOutFrom = 0.4f, SpinMin = -1f, SpinMax = 1f, Max = 10 }, "column");
                    Emit(root, new P { Mat = Soft("fx_cloud", Color.white), Rate = 5, LifeMin = 0.7f, LifeMax = 1f, SpeedMin = 1.5f, SpeedMax = 3f, SizeMin = 1f, SizeMax = 1.5f,
                                       A = new Color(moSandDeep.r, moSandDeep.g, moSandDeep.b, 0.35f), B = moSandEnd, Shape = Shape.Disc, Radius = 2f, Drag = 1.5f, Size0 = 0.5f, Size1 = 1.5f, FadeOutFrom = 0.4f, Max = 8 }, "foot");
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", sandEdge);
                    if (pull > radius)
                    {
                        var edge = new GameObject("pull"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.6f;
                        Emit(edge, new P { Mat = Mat("mat_spark", sandEdge), Rate = 50, LifeMin = 0.65f, LifeMax = 0.7f, SizeMin = 0.1f, SizeMax = 0.14f, A = Hex("FFE1C5"), B = sandEdge,
                                           Shape = Shape.Ring, Radius = pull, Radial = -pull / 0.7f, Stretch = 2.5f, World = false, Max = 40 }, "streaks");
                    }
                    Light(root, moLight, radius * 1.5f, 1.2f, Vector3.up * 2f);
                    return root;
                }
                case "cast":
                case "end":
                    DustPuffs(at + Vector3.up * 0.2f, 5, 1.3f, 6f, 0.9f, moSand, moSandEnd, true, 1.5f);
                    QuadAt(Ground(at), Glow("fx_ring", sandEdge), 0.8f, radius * 2f, 0.35f, Face.Ground, 0f, 0.05f, 0.3f);
                    return null;
                case "hit":
                    if (!Every("mokrill_devil_hit", 0.1f)) return null;
                    DustPuffs(at, 2, 0.7f, 2f, 0.4f, moSand, moSandEnd, false, 0.3f);
                    return null;
            }
            return null;
        }

        static GameObject MokrillKrill(string moment, Vector3 at, float radius, Vector3? to)
        {
            switch (moment)
            {
                case "cast":
                    QuadAt(at, Glow("fx_glow", krillOrange), 0.5f, 1.6f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
                    DustPuffs(at, 3, 0.8f, 3f, 0.5f, moSand, moSandEnd, false, 0.3f);
                    return null;
                case "hop":
                {
                    // Krill flying in an arc from where he took off (to) to where he lands (at), tumbling, with an orange ribbon;
                    // radius carries the flight time
                    var from = to ?? at;
                    float time = Mathf.Max(0.1f, radius);
                    var root = Root(from, time + 0.05f, null);
                    var fig = Equipment.FigureFor("mokrill_krill");
                    if (fig)
                    {
                        var k = Object.Instantiate(fig, root.transform);
                        k.SetActive(true);
                        k.transform.localPosition = Vector3.zero;
                        k.transform.localScale = Vector3.one * 0.6f;
                        var spin = k.AddComponent<Spin>(); spin.DegPerSec = 540f;
                    }
                    Ribbon(root, Glow("fx_beam", krillOrange), krillHot, Hex("AF5A00"), 0.45f, 0.3f);
                    Emit(root, new P { Mat = Mat("mat_spark", krillArc), Rate = 40, LifeMin = 0.25f, LifeMax = 0.35f, SpeedMin = 0.5f, SpeedMax = 1.5f, SizeMin = 0.06f, SizeMax = 0.09f,
                                       A = krillHot, B = krillDeep, Max = 16 });
                    var arc = root.AddComponent<ArcMover>(); arc.From = from; arc.To = at; arc.Time = time; arc.Height = 2f + Vector3.Distance(from, at) * 0.15f;
                    return null;
                }
                case "hit":
                    // digger_ult_tgt_hit: the landing: an orange ring over the ground at the splash's reach, a star, electric arcs, sparks
                    QuadAt(Ground(at), Glow("fx_ring", krillRing), 0.5f, radius * 2f, 0.35f, Face.Ground, 0f, 0.05f, 0.3f);
                    QuadAt(at, Glow("fx_star", krillHot), 0.5f, Mathf.Min(radius, 5f) * 1.2f, 0.15f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Arcs(at + Vector3.up * 0.3f, krillHot, krillDeep, 4, Mathf.Max(1.2f, radius * 0.5f), radius * 0.3f, 0.2f);
                    Puff(at, new P { Mat = Mat("mat_spark", krillArc), Count = 14, LifeMin = 0.25f, LifeMax = 0.4f, SpeedMin = 4f, SpeedMax = 10f, SizeMin = 0.06f, SizeMax = 0.09f,
                                     A = krillHot, B = krillDeep, Stretch = 3f, Max = 16 });
                    Glint(at, krillOrange, radius * 3f, 0.25f, 5f);
                    return null;
                case "end":
                    DustPuffs(at, 3, 0.8f, 3f, 0.5f, moSand, moSandEnd, false, 0.3f);
                    return null;
            }
            return null;
        }

        static GameObject MokrillPunch(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "hit":
                    // a Frenzy punch landing (melee_impact_heavy in red): a star, a spiked ring, sparks, a kick of dust
                    if (!Every("mokrill_punch_hit", 0.05f)) return null;
                    QuadAt(at, Glow("fx_star", frenzyRed), 0.5f, 2.6f, 0.14f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    QuadAt(at, Glow("fx_ring_burst", frenzyEmber), 0.4f, 3f, 0.22f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
                    Puff(at, new P { Mat = Mat("mat_spark", frenzyEmber), Count = 12, LifeMin = 0.25f, LifeMax = 0.4f, SpeedMin = 5f, SpeedMax = 12f, SizeMin = 0.06f, SizeMax = 0.09f,
                                     A = Hex("FFC08A"), B = frenzyRed, Stretch = 3f, Max = 14 });
                    if (Rich) DustPuffs(at, 3, 1f, 3.5f, 0.6f, moSand, moSandEnd, false, 0.3f);
                    Glint(at, frenzyRed, 6f, 0.15f, 4f);
                    return null;
                case "frenzy":
                {
                    // Mo's Frenzy (Krill is away): red embers rising around him, a turning red ring and a glow at his feet
                    var root = Root(at, 0f, follow);
                    var feet = new GameObject("feet"); feet.transform.SetParent(root.transform, false);
                    if (follow) feet.transform.localPosition = at - follow.position;
                    float r = Mathf.Max(1f, radius);
                    Emit(feet, new P { Mat = Glow("fx_fleks", frenzyEmber), Rate = 20, LifeMin = 0.7f, LifeMax = 1.1f, SpeedMin = 0.3f, SpeedMax = 1f, SizeMin = 0.1f, SizeMax = 0.2f,
                                       A = frenzyEmber, B = Hex("5E0A05"), Shape = Shape.Disc, Radius = r, Velocity = Vector3.up * 2.6f, Noise = 0.5f, TilesX = 8, TilesY = 8,
                                       SpinMin = -5f, SpinMax = 5f, Max = 24 }, "embers");
                    Quad(feet, Glow("fx_ring_soft", frenzyRed), r * 2.7f, r * 2.7f, 1f, Face.Ground, 90f, offset: Vector3.up * 0.1f, loop: true);
                    Quad(feet, Glow("fx_glow_soft", new Color(frenzyRed.r, frenzyRed.g, frenzyRed.b, 0.5f)), r * 3.2f, r * 3.2f, 1f, Face.Ground, 0f, offset: Vector3.up * 0.12f, loop: true);
                    Light(feet, frenzyRed, r * 5f, 2.5f, Vector3.up * 1.5f).gameObject.AddComponent<Flicker>();
                    return root;
                }
            }
            return null;
        }
    }
}
