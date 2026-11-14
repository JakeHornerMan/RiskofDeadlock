using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Our own visual effects (effects rows of kind custom), built at runtime from Unity particle systems, line and
    /// trail renderers and lights, drawn with Risk of Rain 2's own materials (effects rows of kind material). They are made
    /// to look like the Deadlock abilities. Since the 2026-11 rebuild (VfxKit.cs, docs/VfxRebuild) their colours, layers and
    /// timing follow Deadlock's own particle recipes and they draw with Deadlock's particle textures where the cache has them.
    /// Each skill's vfx id answers moments like its sounds do: cast, loop (returns an object to keep or move), hit, trigger, end.</summary>
    public static partial class Vfx
    {
        static readonly Dictionary<string, Material> baseMats = new Dictionary<string, Material>();
        static readonly Dictionary<string, Material> tinted = new Dictionary<string, Material>();

        public static GameObject Play(string id, string moment, Vector3 at, float radius = 1f, Transform follow = null, Vector3? to = null, float angle = 0f, float life = 0f)
        {
            if (string.IsNullOrEmpty(id) || id == "none") return null;
            try
            {
                switch (id)
                {
                    case "haze_gun": case "haze_dagger": case "haze_smoke": case "haze_phantom": case "haze_dance": case "haze_fixation":
                        return Haze(id, moment, at, radius, follow, to);
                    case "abrams_gun": case "abrams_siphon": case "abrams_charge": case "abrams_haymaker": case "abrams_slam":
                        return Abrams(id, moment, at, radius, follow, to, angle, life);
                    case "seven_gun": case "seven_ball": case "seven_static": case "seven_flight": case "seven_storm":
                        return Seven(id, moment, at, radius, follow, to);
                    case "infernus_afterburn": return InfernusAfterburn(moment, at, follow);
                    case "infernus_napalm": return InfernusNapalm(moment, at, radius, follow);
                    case "infernus_trail": return InfernusTrail(moment, at, radius, follow, life);
                    case "infernus_bomb": return InfernusBomb(moment, at, radius, follow);
                    case "infernus_pulse": return InfernusPulse(moment, at, radius, follow);
                    case "mokrill_gun": return MokrillGun(moment, at, follow);
                    case "mokrill_sand": return MokrillSand(moment, at, radius, follow, to, angle, life);
                    case "mokrill_burrow": return MokrillBurrow(moment, at, radius, follow);
                    case "mokrill_devil": return MokrillDevil(moment, at, radius, follow, angle);
                    case "mokrill_krill": return MokrillKrill(moment, at, radius, to);
                    case "mokrill_punch": return MokrillPunch(moment, at, radius, follow);
                    case "viscous_goo": return ViscousGoo(moment, at, radius, follow, to);
                    case "viscous_splatter": return ViscousSplatter(moment, at, radius, follow, life);
                    case "viscous_cube": return ViscousCube(moment, at, radius, follow);
                    case "viscous_punch": return ViscousPunch(moment, at, radius);
                    case "viscous_ball": return ViscousBall(moment, at, radius);
                    case "viscous_gooed": return ViscousGooed(moment, follow);
                    case "ratking_gun": case "ratking_scrap": case "ratking_ring": case "ratking_banner": case "ratking_swarm":
                        return RatKing(id, moment, at, radius, follow);
                    case "billy_gun": case "billy_bat": case "billy_bashdown": case "billy_ram": case "billy_float": case "billy_dodge": case "billy_chains":
                        return Billy(id, moment, at, radius, follow, to, angle);
                    case "wraith_gun": case "wraith_card": case "wraith_mind": case "wraith_fullauto": case "wraith_tk":
                        return Wraith(id, moment, at, radius, follow, to, angle);
                    case "mina_gun": case "mina_bites": case "mina_umbrella": case "mina_rake": case "mina_bats": case "mina_nox": case "mina_mist":
                        return Mina(id, moment, at, radius, follow, to, life);
                    case "pocket_gun": case "pocket_dodge": case "pocket_barrage": case "pocket_cloak": case "pocket_satchel": case "pocket_frog":
                        return Pocket(id, moment, at, radius, follow, to, angle);
                    case "victor_jumpstart": case "victor_gun": case "victor_battery": case "victor_aura": case "victor_wire": case "victor_reanimation":
                        return Victor(id, moment, at, radius, follow, to, angle);
                    case "paige_gun": case "paige_bookwyrm": case "paige_read": case "paige_knights": case "paige_plot_armor":
                        return Paige(id, moment, at, radius, follow, to, angle, life);
                }
            }
            catch (System.Exception e) { Log.Warn($"vfx {id}/{moment}: {e.Message}"); }
            return null;
        }

        // ------------------------------------------------------------------ Infernus: Afterburn, Napalm
        static readonly Color fireOrange = new Color(1f, 0.48f, 0.12f), fireYellow = new Color(1f, 0.8f, 0.35f), bombYellow = new Color(1f, 0.9f, 0.15f);

        static GameObject InfernusAfterburn(string moment, Vector3 at, Transform follow)
        {
            if (moment == "muzzle")
            {
                // every Flame Gun shot: a hot flash and a puff of smoke rolling off the muzzle
                InfernusMuzzle(at);
                return null;
            }
            var body = follow ? follow.GetComponent<CharacterBody>() : null;
            if (!body) return null;
            switch (moment)
            {
                case "hit":
                    if (!body.GetComponent<EmberFx>()) body.gameObject.AddComponent<EmberFx>();
                    return null;
                case "trigger":
                    if (!body.GetComponent<EmberFx>()) body.gameObject.AddComponent<EmberFx>();
                    // ignition: RoR2's Helfire flare, then a column of big flames up the whole body
                    Fx.Impact("helfire_ignite", body.corePosition);
                    Burst(body.corePosition, "mat_fire_large", fireOrange, 18, 0.7f, 4f, 1.8f * Mathf.Max(1f, body.radius), shapeRadius: body.radius * 0.6f, upward: true);
                    Burst(body.corePosition, "mat_fire_puff", fireYellow, 10, 0.5f, 6f, 0.9f * Mathf.Max(1f, body.radius), upward: true);
                    InfernusIgnite(body.corePosition, Mathf.Max(1f, body.radius));
                    return null;
            }
            return null;
        }

        static GameObject InfernusNapalm(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the flask in flight, riding on the lob: Molotov's bottle (a glowing ball if it can't load), a hot core,
                    // a fire trail and drips
                    var root = Root(at, 0f, follow);
                    var bottle = Fx.Ghost("flask", at, Quaternion.identity);
                    if (!bottle)
                    {
                        bottle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Object.Destroy(bottle.GetComponent<Collider>());
                        bottle.transform.localScale = Vector3.one * 0.45f;
                        bottle.GetComponent<Renderer>().sharedMaterial = Mat("mat_glow", fireOrange);
                    }
                    bottle.transform.SetParent(root.transform, false);
                    bottle.transform.localPosition = Vector3.zero;
                    bottle.AddComponent<Spin>().DegPerSec = 540f;
                    Particles(root, "mat_glow", fireYellow, rate: 30, life: 0.15f, speed: 0f, size: 0.9f);
                    Particles(root, "mat_fire_large", fireOrange, rate: 50, life: 0.45f, speed: 0.5f, size: 0.9f, world: true);
                    Trail(root, fireOrange, 0.5f, 0.3f);
                    Light(root, fireOrange, 8f, 3f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "aim":
                {
                    // the aim marker (owner only): the flight path as a thin line and, where it lands, a circle the size of
                    // the splash (the area dome, drawn where it meets the ground) with embers on its edge; the state moves it
                    // and redraws the path every frame (SetArc)
                    var root = Root(at, 0f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", fireOrange);
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.3f;
                    Particles(edge, "mat_glow", fireOrange, rate: 40, life: 0.4f, speed: 1f, size: 0.4f, shapeRadius: radius, world: true, ring: true);
                    var arc = Line(root.transform, fireYellow, 0.12f, Vector3.zero, Vector3.zero);
                    arc.name = "arc"; arc.useWorldSpace = true;
                    arc.widthCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.15f, 1f), new Keyframe(1, 1f));
                    return root;
                }
                case "hit":
                    // on the floor under the hit, never floating where the flask struck an enemy (creator, 2026-11-14: "the vfx must
                    // always play on the floor not on where the projectile hit the enemy")
                    at = Floor(at);
                    FireRing(at, radius, 0.35f);
                    Burst(at, "mat_fire_large", fireOrange, 26, 0.9f, radius * 1.1f, 2.2f, shapeRadius: radius * 0.4f, upward: true);
                    Burst(at, "mat_fire_puff", fireYellow, 14, 0.6f, radius * 1.5f, 1.2f, shapeRadius: radius * 0.3f, upward: true);
                    InfernusNapalmSplash(at, radius);
                    GroundFire(at, radius * 0.8f, 2.5f);
                    Glint(at + Vector3.up, fireOrange, radius * 3f, 0.4f, 7f);
                    return null;
            }
            return null;
        }

        /// <summary>A burning patch of ground (RoR2 Gasoline fire with big flames licking up) that lasts life seconds.</summary>
        static void GroundFire(Vector3 at, float radius, float life)
        {
            var root = Root(at + Vector3.up * 0.1f, life + 1.2f, null);
            Particles(root, "mat_gas_fire", fireOrange, rate: N(6f * radius), life: 0.9f, speed: 0.3f, size: 1.3f, shapeRadius: radius, world: true, flat: true);
            Particles(root, "mat_fire_large", fireOrange, rate: N(3f * radius), life: 0.7f, speed: 1.6f, size: 1.2f, shapeRadius: radius * 0.8f, world: true, flat: true);
            InfernusGroundLayers(root, radius, life);
            if (Alive < 50) Light(root, fireOrange, radius * 2.5f, 1.5f, Vector3.up).gameObject.AddComponent<Flicker>();
            root.AddComponent<StopAfter>().After = life;
        }

        /// <summary>A ring of flame racing out along the ground to radius.</summary>
        static void FireRing(Vector3 at, float radius, float life)
        {
            var root = Root(at + Vector3.up * 0.3f, life + 0.8f, null);
            int n = N(Mathf.Clamp((int)(radius * 3f), 12, 44));
            Particles(root, "mat_fire_large", fireOrange, count: n, life: life + 0.3f, speed: radius / life, size: Mathf.Clamp(radius * 0.26f, 1.6f, 3.4f), shapeRadius: 0.4f, world: true, ring: true);
            Particles(root, "mat_fire_puff", fireYellow, count: n / 2, life: life, speed: radius / life * 0.9f, size: 1.3f, shapeRadius: 0.4f, world: true, ring: true);
            InfernusRingLayers(at, radius, life);
        }

        static GameObject InfernusTrail(string moment, Vector3 at, float radius, Transform follow, float life)
        {
            switch (moment)
            {
                case "loop":
                {
                    // flames licking around his legs and a heat glow while he runs
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_fire_large", fireOrange, rate: 45, life: 0.45f, speed: 1.5f, size: 1.1f, shapeRadius: 0.6f, world: true, upward: true);
                    InfernusEmbers(root, 26f, 0.6f, 3f);
                    Quad(root, Glow("fx_glow_soft", Hex("FF952B")), 2.6f, 2.6f, 1f, Face.Ground, 30f, offset: Vector3.up * 0.1f, loop: true);
                    Light(root, fireOrange, 7f, 3f, Vector3.up).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                    // one pocket of burning ground left behind (radius = the pocket's, life = how long it burns)
                    GroundFire(at, radius, life > 0f ? life : 5f);
                    return null;
            }
            return null;
        }

        /// <summary>Move an aim marker made by a moment "aim" (Napalm) to where the throw lands and redraw its path.</summary>
        public static void SetArc(GameObject marker, List<Vector3> path, Vector3 landing)
        {
            if (!marker) return;
            marker.transform.position = landing;
            foreach (var lr in marker.GetComponentsInChildren<LineRenderer>())
            {
                if (lr.name != "arc") continue;
                lr.positionCount = path.Count;
                for (int i = 0; i < path.Count; i++) lr.SetPosition(i, path[i]);
            }
        }

        static GameObject InfernusBomb(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "aim":
                {
                    // the blast's reach while the fuse burns, on whoever holds the equipment: a yellow circle on the ground
                    // at the full radius (the area dome, drawn only where it meets the ground, so it hugs slopes), embers
                    // rising from its edge, and a second dome shrinking once a second, whose outline on the ground sweeps
                    // inward as a countdown; it glows hotter toward the blast (no flat line circles: they float on slopes)
                    var root = Root(at, 0f, follow);
                    var feet = new GameObject("feet"); feet.transform.SetParent(root.transform, false);
                    if (follow) feet.transform.localPosition = at - follow.position;
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(feet.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", bombYellow);
                    var rise = new GameObject("rise"); rise.transform.SetParent(feet.transform, false); rise.transform.localPosition = Vector3.up * 0.3f;
                    Particles(rise, "mat_glow", bombYellow, rate: 8f * radius, life: 0.7f, speed: 2f, size: 0.6f, shapeRadius: radius, world: true, ring: true);
                    var sweep = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(sweep.GetComponent<Collider>());
                    sweep.transform.SetParent(feet.transform, false);
                    sweep.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", fireYellow);
                    var p = sweep.AddComponent<DomeSweep>(); p.From = radius * 2f; p.To = radius * 0.2f; p.Period = 1f;
                    var l = Light(feet, bombYellow, radius, 1f, Vector3.up * 2f);
                    var g = l.gameObject.AddComponent<GrowLight>(); g.To = 6f; g.Over = 3f;
                    return root;
                }
                case "loop":
                {
                    // the fuse: fire and embers gathering on him, a glow growing hotter
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_fire_large", fireOrange, rate: 35, life: 0.5f, speed: 1.2f, size: 1.3f, shapeRadius: 0.9f, world: true, upward: true);
                    Particles(root, "mat_fire_puff", fireYellow, rate: 20, life: 0.6f, speed: 1.5f, size: 0.5f, shapeRadius: 1.2f, world: true);
                    Particles(root, "mat_spark", fireYellow, rate: 60, life: 0.7f, speed: 4f, size: 0.09f, shapeRadius: 0.6f, world: true);
                    var l = Light(root, fireOrange, 10f, 1f, Vector3.zero);
                    var g = l.gameObject.AddComponent<GrowLight>(); g.To = 8f; g.Over = 3f;
                    return root;
                }
                case "hit":
                    FireRing(at, radius, 0.45f);
                    Burst(at + Vector3.up, "mat_fire_large", fireOrange, N(34), 1.1f, radius * 0.7f, 4.6f, shapeRadius: 1.5f, upward: true);
                    InfernusBombBlast(at, radius);
                    GroundFire(at, radius * 0.45f, 3f);
                    Flash(at + Vector3.up * 2f, fireOrange, radius * 3f, 0.6f, 10f);
                    Shake(at, radius * 3f, 4f, 0.7f);
                    return null;
            }
            return null;
        }

        static GameObject InfernusPulse(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // Torment Pulse running: embers rising around him and a hot glow at his feet
                    var root = Root(at, 0f, follow);
                    InfernusEmbers(root, 30f, 1.4f, 1.8f);
                    Particles(root, "mat_fire_puff", fireOrange, rate: 10, life: 0.5f, speed: 0.6f, size: 0.9f, shapeRadius: 1f, world: true, flat: true);
                    Quad(root, Glow("fx_ring_spiral", Hex("FD953A")), 3.6f, 3.6f, 1f, Face.Ground, 200f, offset: Vector3.up * 0.12f, loop: true);
                    Light(root, fireOrange, 9f, 2.5f, Vector3.up * 0.5f).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                    FireRing(at, radius, 0.3f);
                    QuadAt(at + Vector3.up, Glow("fx_glow", fireOrange), 1.5f, 3.6f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
                    return null;
            }
            return null;
        }

        /// <summary>A bright curved cut facing the camera that fades fast.</summary>
        static void Slash(Vector3 at, Color c, float size, float rollDeg)
        {
            var root = Root(at, 0.3f, null);
            var cam = Camera.main;
            var face = cam ? (cam.transform.position - at).normalized : Vector3.back;
            root.transform.rotation = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, rollDeg);
            var lr = new GameObject("slash").AddComponent<LineRenderer>();
            lr.transform.SetParent(root.transform, false);
            lr.useWorldSpace = false;
            lr.sharedMaterial = Mat("mat_tracer", c);
            lr.startColor = lr.endColor = c;
            const int seg = 14;
            lr.positionCount = seg;
            for (int i = 0; i < seg; i++)
            {
                float a = Mathf.Lerp(-70f, 70f, i / (seg - 1f)) * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Sin(a), Mathf.Cos(a) - 0.6f, 0f) * size);
            }
            lr.widthCurve = new AnimationCurve(new Keyframe(0, 0f), new Keyframe(0.5f, 1f), new Keyframe(1, 0f));
            lr.widthMultiplier = 0.25f;
            root.AddComponent<FadeLine>().Life = 0.25f;
        }

        // ------------------------------------------------------------------ Seven: Power Surge
        // ------------------------------------------------------------------ Mo & Krill (colours from Deadlock's own particles)
        // mat_dust is RoR2's opaque dust: keep its particles small, few and light-coloured, or they hide the screen (Dust Devil's
        // first build did) and turn into black blobs (dark tints, or seen from the unlit side); big clouds use mat_smoke
        static readonly Color sandTan = new Color32(236, 218, 186, 255), sandDark = new Color32(185, 166, 132, 255), sandEdge = new Color32(220, 170, 114, 255),
            moOrange = new Color32(226, 132, 32, 255), moLight = new Color32(255, 145, 28, 255), dirt = new Color32(141, 88, 60, 255),
            crack = new Color32(51, 46, 41, 220), hotCrack = new Color32(237, 107, 20, 255), krillOrange = new Color32(255, 147, 65, 255),
            krillArc = new Color32(254, 215, 118, 255), tracerHot = new Color32(255, 212, 85, 255), smokeGrey = new Color32(156, 156, 156, 160),
            frenzyRed = new Color32(232, 38, 28, 255), frenzyEmber = new Color32(255, 92, 40, 255);

        /// <summary>Move Sand Blast's wave (moment loop) to its front, facing along the cone, halfWidth to each side.</summary>
        public static void SetWave(GameObject wave, Vector3 front, Vector3 dir, float halfWidth)
        {
            if (!wave) return;
            wave.transform.position = front + Vector3.up * 1.2f;
            if (dir.sqrMagnitude > 0.01f) wave.transform.rotation = Quaternion.LookRotation(dir);
            foreach (var part in new[] { "wave", "trail" })
            {
                var w = wave.transform.Find(part);
                if (!w) continue;
                foreach (var ps in w.GetComponentsInChildren<ParticleSystem>())
                {
                    var sh = ps.shape; sh.scale = new Vector3(halfWidth * 2f, sh.scale.y, sh.scale.z);
                    var em = ps.emission; em.rateMultiplier = Mathf.Max(1f, halfWidth / 2.5f);
                }
            }
            var l = wave.transform.Find("edgeL"); if (l) l.localPosition = new Vector3(-halfWidth, 0.2f, 0f);
            var r = wave.transform.Find("edgeR"); if (r) r.localPosition = new Vector3(halfWidth, 0.2f, 0f);
        }

        /// <summary>A Sanded enemy while the buff lasts (read from the synced buff, so every player sees it): its whole model
        /// coated in sand (a TemporaryOverlay of mat_sand_overlay), sand swirling round its head and pouring off it.</summary>
        sealed class SandedFx : MonoBehaviour
        {
            CharacterBody body; GameObject root; float idle;
            TemporaryOverlayInstance coat;
            static Material coatMat;
            void Start()
            {
                body = GetComponent<CharacterBody>();
                if (!body) { Destroy(this); return; }
                float r = Mathf.Max(0.6f, body.radius);
                var head = body.corePosition + Vector3.up * r;
                root = Root(head, 0f, body.coreTransform ? body.coreTransform : transform);
                var spin = new GameObject("spin"); spin.transform.SetParent(root.transform, false); spin.transform.localPosition = Vector3.up * r;
                spin.AddComponent<Spin>().DegPerSec = 300f;
                // digger_throw_sand_debuff: cartoon sand clouds and grit whirling round its head, sand pouring off it
                Emit(spin, new P { Mat = Soft("fx_cloud", Color.white), Rate = 8, LifeMin = 0.6f, LifeMax = 0.9f, SpeedMin = 0.1f, SpeedMax = 0.3f, SizeMin = 0.5f, SizeMax = 0.85f,
                                   A = moSand, B = moSandEnd, Shape = Shape.Ring, Radius = r * 1.2f, World = false, Size0 = 0.6f, Size1 = 1.3f, FadeIn = 0.15f, FadeOutFrom = 0.4f, Max = 9 }, "clouds");
                Emit(spin, new P { Mat = Mat("mat_spark", sandEdge), Rate = 22, LifeMin = 0.4f, LifeMax = 0.5f, SpeedMin = 0.2f, SpeedMax = 0.4f, SizeMin = 0.07f, SizeMax = 0.1f,
                                   A = Hex("FFBF9A"), B = Hex("EC9749"), Shape = Shape.Ring, Radius = r * 1.2f, World = false, Max = 12 }, "grit");
                Emit(root, new P { Mat = Soft("fx_fleks", Color.white), Rate = 9, LifeMin = 0.5f, LifeMax = 0.8f, SpeedMin = 0.2f, SpeedMax = 0.6f, SizeMin = 0.08f, SizeMax = 0.16f,
                                   A = sandTan, B = sandDark, Radius = r, Gravity = 1.5f, FadeIn = 0.02f, FadeOutFrom = 0.8f, TilesX = 8, TilesY = 8, Max = 8 }, "pour");
                Coat();
            }

            void Coat()
            {
                var model = body.modelLocator && body.modelLocator.modelTransform ? body.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
                if (!model) return;
                if (!coatMat)
                {
                    coatMat = new Material(Mat("mat_sand_overlay", sandEdge));
                    if (coatMat.HasProperty("_TintColor")) coatMat.SetColor("_TintColor", sandEdge);
                }
                coat = TemporaryOverlayManager.AddOverlay(model.gameObject);
                coat.duration = float.PositiveInfinity;
                coat.animateShaderAlpha = false;
                coat.destroyComponentOnEnd = false;
                coat.originalMaterial = coatMat;
                coat.AddToCharacterModel(model);
            }

            void Uncoat()
            {
                if (coat == null) return;
                if (coat.managerIndex >= 0) TemporaryOverlayManager.RemoveOverlay(coat.managerIndex);
                else coat.CleanupEffect();
                coat = null;
            }
            void Update()
            {
                if (!body || !body.healthComponent || !body.healthComponent.alive) { Done(); return; }
                bool on = false;
                foreach (var row in BuffRow.All) if (row.Disarm && Buffs.Count(body, row.Id) > 0) { on = true; break; }
                if (!on) { idle += Time.deltaTime; if (idle > 0.3f) Done(); } else idle = 0f;
            }
            void Done() { Uncoat(); Stop(root); Destroy(this); }
            void OnDestroy() { Uncoat(); if (root) Stop(root); }
        }

        /// <summary>Moves an object along a parabola from From to To over Time seconds, Height above the straight line at its top.</summary>
        sealed class ArcMover : MonoBehaviour
        {
            public Vector3 From, To; public float Time = 0.3f, Height = 2f;
            float age;
            void Update()
            {
                age += UnityEngine.Time.deltaTime;
                float t = Mathf.Clamp01(age / Time);
                transform.position = Vector3.Lerp(From, To, t) + Vector3.up * Height * 4f * t * (1f - t);
            }
        }

        // ------------------------------------------------------------------ building blocks

        /// <summary>A RoR2 material (effects row of kind material), copied and tinted.</summary>
        public static Material Mat(string id, Color tint)
        {
            string key = id + "|" + ColorUtility.ToHtmlStringRGBA(tint);
            if (tinted.TryGetValue(key, out var m) && m) return m;
            if (!baseMats.TryGetValue(id, out var b) || !b)
            {
                var row = Sheet.Effect(id);
                try { if (row != null && row.Addressable != "none") b = Addressables.LoadAssetAsync<Material>(row.Addressable).WaitForCompletion(); }
                catch (System.Exception e) { Log.Warn($"vfx material {id}: {e.Message}"); }
                if (!b) b = new Material(SpriteShader());
                baseMats[id] = b;
            }
            m = new Material(b);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", tint);
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            tinted[key] = m;
            return m;
        }

        /// <summary>A RoR2 material whose colour comes from its ramp (_RemapTex: the lightning materials are blue whatever the
        /// tint), with that ramp recoloured to c (<see cref="RecolorMat"/>); twoSided draws it from inside too.</summary>
        public static Material Remapped(string id, Color c, bool twoSided = false)
        {
            string key = "remap|" + id + "|" + ColorUtility.ToHtmlStringRGBA(c) + (twoSided ? "|2" : "");
            if (tinted.TryGetValue(key, out var m) && m) return m;
            m = RecolorMat(Mat(id, Color.white), c);
            if (twoSided && m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            tinted[key] = m;
            return m;
        }

        static readonly Dictionary<string, Texture2D> ramps = new Dictionary<string, Texture2D>();

        /// <summary>A copy of a material in c. A ramp material (_RemapTex) keeps its own ramp's brightness and see-through at
        /// every step, only the hue becomes c (its white-hot end stays white), so it looks like RoR2's effect in another colour;
        /// its tint alpha becomes c.a. Other materials take c as their tint, keeping their alpha.</summary>
        static Material RecolorMat(Material src, Color c)
        {
            var m = new Material(src);
            var orig = m.HasProperty("_RemapTex") ? m.GetTexture("_RemapTex") : null;
            if (orig)
            {
                string key = orig.name + "|" + ColorUtility.ToHtmlStringRGB(c);
                if (!ramps.TryGetValue(key, out var ramp) || !ramp)
                {
                    // the ramp textures aren't readable: draw the original into a render texture and read that back
                    const int w = 128;
                    var rt = RenderTexture.GetTemporary(w, 1, 0, RenderTextureFormat.ARGB32);
                    var prev = RenderTexture.active;
                    Graphics.Blit(orig, rt);
                    RenderTexture.active = rt;
                    ramp = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                    ramp.ReadPixels(new Rect(0, 0, w, 1), 0, 0);
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                    for (int i = 0; i < w; i++)
                    {
                        var o = ramp.GetPixel(i, 0);
                        float lum = Mathf.Max(o.r, Mathf.Max(o.g, o.b)), white = lum > 0.001f ? Mathf.Min(o.r, Mathf.Min(o.g, o.b)) / lum : 0f;
                        // the white-hot core stays a little white, the rest takes the colour (RoR2's tracers are nearly white)
                        var col = Color.Lerp(new Color(c.r, c.g, c.b), Color.white, white * 0.4f) * lum;
                        col.a = o.a;
                        ramp.SetPixel(i, 0, col);
                    }
                    ramp.Apply();
                    ramps[key] = ramp;
                }
                m.SetTexture("_RemapTex", ramp);
                if (m.HasProperty("_TintColor")) { var t = m.GetColor("_TintColor"); m.SetColor("_TintColor", new Color(1f, 1f, 1f, t.a * c.a)); }
            }
            else
            {
                if (m.HasProperty("_TintColor")) { var a = m.GetColor("_TintColor").a; m.SetColor("_TintColor", new Color(c.r, c.g, c.b, a)); }
                if (m.HasProperty("_Color")) { var a = m.GetColor("_Color").a; m.SetColor("_Color", new Color(c.r, c.g, c.b, a)); }
            }
            return m;
        }

        /// <summary>Recolours a copied RoR2 effect in place: every renderer's materials (<see cref="RecolorMat"/>), line, trail and
        /// particle colours, and lights.</summary>
        public static void Recolor(GameObject root, Color c)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) if (mats[i]) mats[i] = RecolorMat(mats[i], c);
                r.sharedMaterials = mats;
            }
            Color Keep(Color o) => new Color(c.r, c.g, c.b, o.a);
            foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true)) { lr.startColor = Keep(lr.startColor); lr.endColor = Keep(lr.endColor); }
            foreach (var tr in root.GetComponentsInChildren<TrailRenderer>(true)) { tr.startColor = Keep(tr.startColor); tr.endColor = Keep(tr.endColor); }
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true)) { var main = ps.main; main.startColor = Keep(main.startColor.color); }
            foreach (var l in root.GetComponentsInChildren<Light>(true)) l.color = c;
        }

        static Shader spriteShader;

        /// <summary>Unity's built-in Sprites/Default, taken from a SpriteRenderer's default material rather than looked up by name:
        /// RoR2BepInExPack (which r2modman always installs) sends Shader.Find to RoR2's LegacyShaderAPI, which throws for built-in
        /// names, so every material made with Shader.Find("Sprites/Default") came out missing or magenta (doc 02 §12).</summary>
        internal static Shader SpriteShader()
        {
            if (spriteShader) return spriteShader;
            var probe = new GameObject("dlrSpriteShaderProbe");
            try { var sr = probe.AddComponent<SpriteRenderer>(); if (sr.sharedMaterial) spriteShader = sr.sharedMaterial.shader; }
            catch (System.Exception e) { Log.Warn($"sprite shader probe: {e.Message}"); }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
            if (!spriteShader) { try { spriteShader = Shader.Find("Sprites/Default"); } catch (System.Exception e) { Log.Warn($"sprite shader: {e.Message}"); } }
            return spriteShader;
        }

        /// <summary>A plain alpha-blended colour (Unity's built-in sprite shader) for lines that must read on bright ground.</summary>
        static Material Solid(Color tint)
        {
            string key = "solid|" + ColorUtility.ToHtmlStringRGBA(tint);
            if (tinted.TryGetValue(key, out var m) && m) return m;
            m = new Material(SpriteShader()) { color = tint };
            tinted[key] = m;
            return m;
        }

        public static void Preload()
        {
            foreach (var e in EffectRow.All) if (e.Kind == "material") Mat(e.Id, Color.white);
            WarmKit();
            WarmHeroFx();
        }

        static GameObject Root(Vector3 at, float life, Transform follow)
        {
            var go = new GameObject("DlrVfx");
            go.transform.position = at;
            var l = go.AddComponent<VfxLife>();
            l.Follow = follow; l.Following = follow; l.Life = life;
            return go;
        }

        /// <summary>A particle system on parent: continuous (rate) or a one-off burst (count).</summary>
        static ParticleSystem Particles(GameObject parent, string mat, Color c, float rate = 0f, int count = 0, float life = 0.5f,
            float speed = 1f, float size = 0.5f, float shapeRadius = 0f, bool world = false, bool flat = false, bool upward = false,
            bool down = false, float gravity = 0f, float stretch = 0f, bool ring = false)
        {
            if (IsFireMat(mat) && !down)
            {
                var flames = FlameEmit(parent, c, rate, count, life, speed, size, shapeRadius, world, flat, upward, ring);
                if (flames) return flames;
            }
            var go = new GameObject("ps");
            go.transform.SetParent(parent.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = count > 0 ? 0.1f : 1f;
            main.loop = count == 0;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = c;
            main.gravityModifier = gravity;
            main.maxParticles = 500;
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var em = ps.emission;
            em.rateOverTime = rate;
            if (count > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.enabled = true;
            if (ring) { shape.shapeType = ParticleSystemShapeType.Circle; shape.rotation = new Vector3(90f, 0, 0); shape.radiusThickness = 0f; }
            else if (flat) { shape.shapeType = ParticleSystemShapeType.Circle; shape.rotation = new Vector3(90f, 0, 0); }
            else if (upward) { shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.rotation = new Vector3(-90f, 0, 0); }
            else if (down) { shape.shapeType = ParticleSystemShapeType.Circle; shape.rotation = new Vector3(90f, 0, 0); }
            else shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.01f, shapeRadius);
            if (down)
            {
                var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f); vel.y = new ParticleSystem.MinMaxCurve(-speed);
                main.startSpeed = 0f;
            }
            // fade in fast, out slow; shrink at the end
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.4f)));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mat(mat, c);
            if (stretch > 0f) { r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = stretch; }
            ps.Play();
            return ps;
        }

        /// <summary>A one-shot burst of particles at a point that cleans itself up. Since the 2026-11 rebuild (docs/VfxRebuild)
        /// the two commonest kinds are drawn the way Deadlock draws them: a still glow (mat_glow, no speed) is one soft glow
        /// quad with a star core instead of a few stacked sprites, and smoke (mat_smoke) is Deadlock's cartoon cloud, fewer and
        /// swelling as they thin. Other materials are RoR2's as before, their counts following the effects quality.</summary>
        public static void Burst(Vector3 at, string mat, Color c, int count, float life, float speed, float size, float shapeRadius = 0.2f,
            bool flat = false, bool upward = false, float gravity = 0f)
        {
            if (mat == "mat_glow" && speed <= 0.01f && gravity == 0f)
            {
                float s = Mathf.Min(size, 14f);
                QuadAt(at, Glow("fx_glow", new Color(c.r, c.g, c.b, Mathf.Min(c.a, 0.65f))), s * 0.5f, s * 1.05f, Mathf.Max(0.1f, life), Face.Camera, 0f, 0.1f, 0.3f);
                if (count >= 3 && Rich) QuadAt(at, Glow("fx_star", new Color(c.r, c.g, c.b, Mathf.Min(c.a, 0.7f))), s * 0.3f, s * 0.9f, Mathf.Max(0.08f, life * 0.7f), Face.Camera, 0f, 0.05f, 0.3f, Random.Range(0f, 360f));
                return;
            }
            // dust only (grey, tan): coloured magic smoke stays RoR2's smoke. As cartoon clouds Wraith's purple smoke drew as flat pink blobs (2026-11-13)
            if (mat == "mat_smoke" && gravity == 0f && Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b)) < 0.22f)
            {
                int n = Mathf.Max(2, Mathf.RoundToInt(count * 0.45f));   // big see-through clouds cost frames by the area they cover: fewer, thinner (Billy's slam, 2026-11-13)
                float a = Mathf.Min(c.a, 0.5f);
                Puff(at, new P { Mat = Soft("fx_cloud", Color.white), Count = n, LifeMin = life * 0.7f, LifeMax = life * 1.1f, SpeedMin = speed * 0.4f, SpeedMax = speed,
                                 SizeMin = size * 0.7f, SizeMax = size * 1.1f, A = new Color(c.r, c.g, c.b, a), B = new Color(c.r * 0.85f, c.g * 0.85f, c.b * 0.85f, a * 0.35f),
                                 FadeIn = 0.08f, FadeOutFrom = 0.4f, Size0 = 0.5f, Size1 = 1.45f, Shape = flat ? Shape.Disc : upward ? Shape.Up : Shape.Sphere, Angle = 60f,
                                 Radius = Mathf.Max(0.05f, shapeRadius), Drag = 1.8f, SpinMin = -0.5f, SpinMax = 0.5f, Max = n + 2 });
                return;
            }
            var root = Root(at, life + 0.2f, null);
            Particles(root, mat, c, count: N(count), life: life, speed: speed, size: size, shapeRadius: shapeRadius, world: true, flat: flat, upward: upward, gravity: gravity);
        }

        static TrailRenderer Trail(GameObject parent, Color c, float width, float time)
        {
            var t = parent.AddComponent<TrailRenderer>();
            t.sharedMaterial = Mat("mat_tracer", c);
            t.time = time; t.minVertexDistance = 0.1f;
            t.widthCurve = new AnimationCurve(new Keyframe(0, width), new Keyframe(1, 0));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            t.colorGradient = g;
            return t;
        }

        static LineRenderer Line(Transform parent, Color c, float width, Vector3 a, Vector3 b, string mat = "mat_tracer")
        {
            var go = new GameObject("line"); go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.sharedMaterial = Mat(mat, c);
            lr.startColor = lr.endColor = c;
            lr.widthMultiplier = width;
            lr.positionCount = 2; lr.SetPosition(0, a); lr.SetPosition(1, b);
            return lr;
        }

        /// <summary>A flat circle (dashed: every other segment) in parent's space.</summary>
        static LineRenderer Circle(Transform parent, Color c, float radius, float width, int segments = 48, bool dashed = false)
        {
            var go = new GameObject(dashed ? "dashes" : "circle"); go.transform.SetParent(parent, false);
            if (dashed)
            {
                for (int i = 0; i < segments; i += 2)
                {
                    float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                    Line(go.transform, c, width, new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius, new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius);
                }
                return go.GetComponentInChildren<LineRenderer>();
            }
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.loop = true;
            lr.sharedMaterial = Mat("mat_tracer", c);
            lr.startColor = lr.endColor = c;
            lr.widthMultiplier = width;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++) { float a = i * Mathf.PI * 2f / segments; lr.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius); }
            return lr;
        }

        /// <summary>An expanding, fading ring (a shockwave), flat on the ground or facing the camera-ish (vertical).</summary>
        /// <remarks>Since the 2026-11 rebuild this is one quad with Deadlock's shockwave ring on it (a thick one for wide rings)
        /// instead of a 64-point line: laid on the terrain's slope when the ground is just below, level in the air otherwise.</remarks>
        public static void Ring(Vector3 at, Color c, float from, float to, float life, float width, bool vertical = false)
        {
            var mat = Glow(width >= 0.8f ? "fx_ring_thick" : "fx_ring", c);
            if (vertical) { QuadAt(at, mat, from * 2f, to * 2f, life, Face.Camera, 0f, 0.05f, 0.3f); return; }
            if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, 1.7f, RoR2.LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            { QuadAt(at, mat, from * 2f, to * 2f, life, Face.Ground, 0f, 0.05f, 0.3f); return; }
            var root = Root(at, life + 0.1f, null);
            Quad(root, mat, from * 2f, to * 2f, life, Face.Ground, 0f, 0.05f, 0.3f);
        }

        /// <summary>A jagged lightning bolt between two points that flickers and fades.</summary>
        /// <param name="recolor">draw it in c through <see cref="Remapped"/> (the lightning material is otherwise blue: Seven's yellow)</param>
        /// <param name="flat">kinks only sideways, never up or down (Victor's aura strikes run along the floor)</param>
        public static void Bolt(Vector3 a, Vector3 b, Color c, float width, float life, int segments = 8, float jitter = 0f, bool recolor = false, bool flat = false)
        {
            var root = Root(a, life, null);
            var lr = Line(root.transform, c, width, Vector3.zero, b - a, "mat_lightning");
            if (recolor) lr.sharedMaterial = Remapped("mat_lightning", c);
            lr.useWorldSpace = true;
            lr.SetPosition(0, a); lr.SetPosition(1, b);
            var bolt = lr.gameObject.AddComponent<BoltFx>();
            bolt.A = a; bolt.B = b; bolt.Segments = segments; bolt.Jitter = jitter > 0f ? jitter : Mathf.Max(0.3f, Vector3.Distance(a, b) * 0.08f);
            bolt.Life = life; bolt.Width = width; bolt.Flat = flat;
        }

        public static void Flash(Vector3 at, Color c, float range, float life, float intensity = 4f)
        {
            if (Alive > 110) return;   // a great many effects alive (four ults at once): the flash lights go first
            var root = Root(at, life, null);
            var l = Light(root, c, range, intensity, Vector3.zero);
            var f = l.gameObject.AddComponent<FadeLight>(); f.Life = life; f.From = intensity;
        }

        static Light Light(GameObject parent, Color c, float range, float intensity, Vector3 offset)
        {
            var go = new GameObject("light"); go.transform.SetParent(parent.transform, false); go.transform.localPosition = offset;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            return l;
        }

        static void Shake(Vector3 at, float radius, float amplitude, float duration)
        {
            ShakeEmitter.CreateSimpleShakeEmitter(at, new Wave { amplitude = amplitude, frequency = 30f, cycleOffset = 0f }, duration, radius, true);
        }

        // ------------------------------------------------------------------ small behaviours

        /// <summary>Lifetime and follow: life 0 lives until destroyed by its owner; a root whose followed object is gone stops
        /// emitting and fades out.</summary>
        sealed class VfxLife : MonoBehaviour
        {
            public Transform Follow; public bool Following; public float Life;
            float age;
            void OnEnable() { Alive++; }
            void OnDisable() { Alive--; }
            void LateUpdate()
            {
                age += Time.deltaTime;
                if (Following)
                {
                    if (Follow) transform.position = Follow.position;
                    else { Stop(gameObject); return; }
                }
                if (Life > 0f && age >= Life) Destroy(gameObject);
            }
        }

        /// <summary>Stop a looping effect: particles stop emitting and the object goes once they have faded.</summary>
        public static void Stop(GameObject go, float linger = 1.5f)
        {
            if (!go) return;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            foreach (var l in go.GetComponentsInChildren<Light>()) { var f = l.gameObject.AddComponent<FadeLight>(); f.Life = 0.3f; f.From = l.intensity; }
            foreach (var lr in go.GetComponentsInChildren<LineRenderer>()) lr.enabled = false;
            // solid models riding on the visual (Napalm's bottle) vanish with the thing they belong to
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                // the kit's quads fade out by themselves instead of blinking off
                var q = mr.GetComponent<QuadFx>();
                if (q) q.FadeOut(Mathf.Min(0.25f, linger)); else mr.enabled = false;
            }
            var life = go.GetComponent<VfxLife>();
            if (life) { life.Following = false; }
            Object.Destroy(go, linger);
        }

        /// <summary>Sand Blast's cone on the ground for the wind-up: its outline (the edges widen by Tan per metre from Near at
        /// Mo, out to Range, every point dropped onto the terrain), faint cross lines, and a bright line sweeping out over
        /// Sweep seconds; then it fades while the wave runs along it.</summary>
        sealed class ConeMarker : MonoBehaviour
        {
            public Vector3 Origin, Dir; public float Range, Near, Tan, Sweep;
            LineRenderer outline, sweep; readonly List<LineRenderer> hatch = new List<LineRenderer>();
            Material fillMat; const float FillAlpha = 0.38f;
            readonly List<float> centreY = new List<float>();
            float age;
            const float Step = 0.5f, Width = 0.28f, HatchWidth = 0.1f;

            /// <summary>The ground under a point of the cone, looked for from a few metres above the centre line there.</summary>
            Vector3 Ground(float along, float side)
            {
                var right = Vector3.Cross(Vector3.up, Dir);
                var p = Origin + Dir * along + right * side;
                int i = Mathf.Clamp(Mathf.RoundToInt(along / Step), 0, centreY.Count - 1);
                p.y = centreY.Count > 0 ? centreY[i] : Origin.y;
                if (Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out var hit, 10f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) p = hit.point;
                return p + Vector3.up * 0.12f;
            }

            /// <summary>The centre line's height every Step metres, each looked for from just above the last (up slopes and down).</summary>
            void MarchCentre()
            {
                float y = Origin.y;
                for (float d = 0f; d <= Range + Step; d += Step)
                {
                    var p = Origin + Dir * d; p.y = y;
                    if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 9f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) y = hit.point.y;
                    centreY.Add(y);
                }
            }

            float Half(float d) => Near + d * Tan;

            LineRenderer World(Color c, float width, int points, bool solid)
            {
                var go = new GameObject("line"); go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                // the outline is drawn solid (alpha-blended, no glow): an additive line vanishes on snow and sand
                lr.sharedMaterial = solid ? Solid(c) : Mat("mat_tracer", c);
                lr.startColor = lr.endColor = c;
                lr.widthMultiplier = width;
                lr.positionCount = points;
                return lr;
            }

            void Start()
            {
                MarchCentre();
                const int steps = 12;
                var pts = new List<Vector3>();
                for (int i = 0; i <= steps; i++) { float d = Range * i / steps; pts.Add(Ground(d, -Half(d))); }
                for (int i = 1; i < steps; i++) { float s = Mathf.Lerp(-Half(Range), Half(Range), (float)i / steps); pts.Add(Ground(Range, s)); }
                for (int i = steps; i >= 0; i--) { float d = Range * i / steps; pts.Add(Ground(d, Half(d))); }
                outline = World(moOrange, Width, pts.Count, true);
                outline.loop = true;
                outline.SetPositions(pts.ToArray());
                for (float d = 2.5f; d < Range - 0.5f; d += 2.5f)
                {
                    var h = World(new Color(moOrange.r, moOrange.g, moOrange.b, 0.45f), HatchWidth, 5, true);
                    for (int i = 0; i < 5; i++) h.SetPosition(i, Ground(d, Mathf.Lerp(-Half(d), Half(d), i / 4f)));
                    hatch.Add(h);
                }
                Fill();
                sweep = World(tracerHot, 0.35f, 5, false);
                Update();
            }

            /// <summary>The cone filled with sand colour (a mesh laid on the terrain, alpha-blended) so the area reads at a glance.</summary>
            void Fill()
            {
                const int along = 16, across = 8;
                var verts = new Vector3[(along + 1) * (across + 1)];
                for (int i = 0; i <= along; i++)
                {
                    float d = Range * i / along;
                    for (int j = 0; j <= across; j++) verts[i * (across + 1) + j] = Ground(d, Mathf.Lerp(-Half(d), Half(d), (float)j / across)) - Vector3.up * 0.04f;
                }
                var tris = new List<int>();
                for (int i = 0; i < along; i++)
                    for (int j = 0; j < across; j++)
                    {
                        int a = i * (across + 1) + j, b = a + 1, c = a + across + 1, e = c + 1;
                        tris.AddRange(new[] { a, c, b, b, c, e });
                    }
                var mesh = new Mesh { vertices = verts, triangles = tris.ToArray() };
                mesh.RecalculateBounds();
                var go = new GameObject("fill");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fillMat = new Material(Solid(new Color(sandEdge.r, sandEdge.g, sandEdge.b, FillAlpha)));
                mr.sharedMaterial = fillMat;
                // the mesh is in world space: keep it out of the root's transform
                go.transform.SetParent(transform, true);
                go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
            }

            void OnDestroy() { if (fillMat) Destroy(fillMat); }

            void Update()
            {
                age += Time.deltaTime;
                // sweep out over the wind-up, easing out; afterwards everything fades over a second
                float t = Mathf.Clamp01(age / Sweep), d = Range * (1f - (1f - t) * (1f - t));
                if (sweep)
                {
                    for (int i = 0; i < 5; i++) sweep.SetPosition(i, Ground(d, Mathf.Lerp(-Half(d), Half(d), i / 4f)));
                    sweep.widthMultiplier = age < Sweep ? 0.35f : 0f;
                }
                float fade = Mathf.Clamp01(1f - (age - Sweep) / 1f);
                if (outline) outline.widthMultiplier = Width * fade;
                if (fillMat) { var c = fillMat.color; c.a = FillAlpha * fade; fillMat.color = c; }
                foreach (var h in hatch) if (h) h.widthMultiplier = HatchWidth * fade;
            }
        }

        sealed class Spin : MonoBehaviour
        {
            public float DegPerSec;
            void Update() => transform.Rotate(0f, DegPerSec * Time.deltaTime, 0f, Space.Self);
        }

        sealed class Pulse : MonoBehaviour
        {
            public float Speed = 3f;
            void Update() => transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.time * Speed * Mathf.PI));
        }

        sealed class Shrink : MonoBehaviour
        {
            public float To = 0.1f, Over = 1.5f;
            float age;
            void Update() { age += Time.deltaTime; transform.localScale = Vector3.one * Mathf.Lerp(1f, To, Mathf.Clamp01(age / Over)); }
        }

        sealed class RingGrow : MonoBehaviour
        {
            public float From, To, Life, Width;
            float age; LineRenderer lr;
            void Awake() => lr = GetComponent<LineRenderer>();
            void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / Life), e = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.one * Mathf.Lerp(From, To, e);
                // the line's width is in world units, so undo the scale and thin it out as it fades
                lr.widthMultiplier = Width * (1f - t) / Mathf.Max(0.01f, transform.localScale.x);
            }
        }

        /// <summary>A dome that shrinks from From to To (diameters) every Period seconds: with the area material its outline
        /// on the ground sweeps inward, a countdown that follows the terrain.</summary>
        sealed class DomeSweep : MonoBehaviour
        {
            public float From, To, Period = 1f;
            float age;
            void Update()
            {
                age += Time.deltaTime;
                float t = (age % Period) / Period;
                transform.localScale = Vector3.one * Mathf.Lerp(From, To, t * t);
            }
        }

        sealed class BoltFx : MonoBehaviour
        {
            public Vector3 A, B; public int Segments; public float Jitter, Life, Width; public bool Flat;
            float age, next; LineRenderer lr;
            void Awake() => lr = GetComponent<LineRenderer>();
            void Update()
            {
                age += Time.deltaTime;
                lr.widthMultiplier = Width * Mathf.Clamp01(1f - age / Life);
                if (age < next) return;
                next = age + 0.04f;
                lr.positionCount = Segments + 1;
                var side = Vector3.Cross(B - A, Vector3.up).normalized;
                if (Flat) side.y = 0f;
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                var up = Vector3.Cross(side, B - A).normalized;
                for (int i = 0; i <= Segments; i++)
                {
                    float t = (float)i / Segments;
                    var p = Vector3.Lerp(A, B, t);
                    if (i > 0 && i < Segments) p += (side * Random.Range(-1f, 1f) + (Flat ? Vector3.zero : up * Random.Range(-1f, 1f))) * Jitter * Mathf.Sin(t * Mathf.PI);
                    lr.SetPosition(i, p);
                }
            }
        }

        /// <summary>Short arcs leaping around a centre (Lightning Ball, Static Charge, Storm Cloud).</summary>
        sealed class Crackle : MonoBehaviour
        {
            public Color Color; public float Radius = 2f; public int Count = 3; public bool Recolor;
            float next;
            void Update()
            {
                if (Time.time < next) return;
                next = Time.time + Random.Range(0.05f, 0.12f);
                for (int i = 0; i < Count; i++)
                {
                    var end = transform.position + Random.onUnitSphere * Radius * Random.Range(0.5f, 1f);
                    Bolt(transform.position, end, Color, 0.12f, 0.08f, 5, Radius * 0.2f, Recolor);
                }
            }
        }

        sealed class Mover : MonoBehaviour
        {
            public Vector3 From, To; public float Time = 0.1f;
            float age;
            void Update() { age += UnityEngine.Time.deltaTime; transform.position = Vector3.Lerp(From, To, Mathf.Clamp01(age / Time)); }
        }

        sealed class FadeLine : MonoBehaviour
        {
            public float Life = 0.25f;
            float age, w; LineRenderer lr;
            void Start() { lr = GetComponentInChildren<LineRenderer>(); if (lr) w = lr.widthMultiplier; }
            void Update() { age += Time.deltaTime; if (lr) lr.widthMultiplier = w * Mathf.Clamp01(1f - age / Life); }
        }

        sealed class GrowLight : MonoBehaviour
        {
            public float To = 8f, Over = 3f;
            float age, from; Light l;
            void Awake() { l = GetComponent<Light>(); from = l ? l.intensity : 0f; }
            void Update() { age += Time.deltaTime; if (l) l.intensity = Mathf.Lerp(from, To, Mathf.Clamp01(age / Over)) * Random.Range(0.85f, 1.1f); }
        }

        /// <summary>Stops a looping effect after a while (ground fire that burns out).</summary>
        sealed class StopAfter : MonoBehaviour
        {
            public float After;
            float age;
            void Update() { age += Time.deltaTime; if (age >= After) { Destroy(this); Stop(gameObject, 1f); } }
        }

        sealed class Flicker : MonoBehaviour
        {
            Light l; float base_;
            void Awake() { l = GetComponent<Light>(); base_ = l.intensity; }
            void Update() { if (l) l.intensity = base_ * Random.Range(0.6f, 1.2f); }
        }

        /// <summary>Lightning flickering inside the thunderhead.</summary>
        sealed class StormFlash : MonoBehaviour
        {
            Light l; float next, until;
            void Awake() => l = GetComponent<Light>();
            void Update()
            {
                if (!l) return;
                if (Time.time >= next) { next = Time.time + Random.Range(0.15f, 0.6f); until = Time.time + Random.Range(0.05f, 0.12f); }
                l.intensity = Time.time < until ? Random.Range(1.2f, 2.5f) : 0.2f;
            }
        }

        sealed class FadeLight : MonoBehaviour
        {
            public float Life = 0.3f, From = 4f;
            float age; Light l;
            void Awake() => l = GetComponent<Light>();
            void Update() { age += Time.deltaTime; if (l) l.intensity = From * Mathf.Clamp01(1f - age / Life); }
        }

        /// <summary>Afterburn on an enemy: embers that grow with the build-up stacks, then flames and a glow while it burns;
        /// it goes away by itself once the enemy has neither.</summary>
        sealed class EmberFx : MonoBehaviour
        {
            CharacterBody body; ParticleSystem embers, flames; Light glow; float idle; GameObject root;
            void Start()
            {
                body = GetComponent<CharacterBody>();
                float r = Mathf.Max(0.6f, body ? body.radius : 1f);
                root = Root(body ? body.corePosition : transform.position, 0f, body && body.coreTransform ? body.coreTransform : transform);
                embers = Particles(root, "mat_spark", fireOrange, rate: 0f, life: 0.8f, speed: 1.5f, size: 0.1f, shapeRadius: r, world: true, upward: true, gravity: -0.3f);
                flames = Particles(root, "mat_fire_large", fireOrange, rate: 0f, life: 0.7f, speed: 2f, size: 1.3f * r, shapeRadius: r * 0.7f, world: true, upward: true);
                glow = Light(root, fireOrange, 4f * r, 0f, Vector3.zero);
            }
            void Update()
            {
                if (!body || !body.healthComponent || !body.healthComponent.alive) { Done(); return; }
                var row = Sheet.Buff("afterburn_buildup");
                float build = row != null && row.TriggerStacks > 0 ? Buffs.Count(body, row.Id) / row.TriggerStacks : 0f;
                // the Burn DoT's own buff: synced to every player (DotController exists only on the server)
                bool burning = body.HasBuff(RoR2Content.Buffs.OnFire);
                var e = embers.emission; e.rateOverTime = build * 60f;
                var f = flames.emission; f.rateOverTime = burning ? 40f : build * 8f;
                glow.intensity = burning ? Random.Range(3f, 5f) : build * 2f;
                if (build <= 0f && !burning) { idle += Time.deltaTime; if (idle > 1f) Done(); } else idle = 0f;
            }
            void Done() { Stop(root); Destroy(this); }
            void OnDestroy() { if (root) Stop(root); }
        }
    }
}
