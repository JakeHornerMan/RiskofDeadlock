using System.Linq;
using DeadlockOfRain.Sheets;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Paige's visuals (2026-11-06, the creator's own DLL build): everything a see-through ghost green, as her spirits are in
    /// Deadlock. Her own models (the wyvern, the horse knight, the giant sword and its stone: effects props read from
    /// charactersFinal/paigeFiles) are drawn with RoR2's Happiest Mask ghost material recoloured green and play their own clips; her
    /// own particle textures (the letter atlas, the dragon / knight / sword-in-stone graphics, the Excalibur lettering, the heptagram,
    /// the astrological ring and the green text streak) float, turn and scorch the ground; RoR2's glow, fire, spark and area materials
    /// do the rest, all in her greens.</summary>
    public static partial class Vfx
    {
        static readonly Color paigeGhost = new Color(0.38f, 1f, 0.62f), paigePale = new Color(0.78f, 1f, 0.86f),
                              paigeDeep = new Color(0.1f, 0.75f, 0.38f), paigeFire = new Color(0.32f, 1f, 0.48f),
                              paigeMist = new Color(0.55f, 1f, 0.75f, 0.55f);

        static GameObject Paige(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle, float life)
        {
            switch (id)
            {
                case "paige_gun": return PaigeGun(moment, at, follow, to);
                case "paige_bookwyrm": return PaigeWyrmFx(moment, at, radius, follow, to, life);
                case "paige_read": return PaigeReadFx(moment, at, radius, follow, life);
                case "paige_knights": return PaigeKnightsFx(moment, at, radius, follow, to);
                case "paige_plot_armor": return PaigePlotArmor(moment, at, follow);
            }
            return null;
        }

        // ------------------------------------------------------------------ shared pieces

        static Material paigeGhostMat;
        /// <summary>The ghost skin: Happiest Mask's ghost material, its ramp in Paige's green, drawn from both sides.</summary>
        static Material PaigeGhostMat()
        {
            if (paigeGhostMat) return paigeGhostMat;
            paigeGhostMat = Sheet.Effect("mat_ghost") != null ? Remapped("mat_ghost", paigeGhost, true) : Mat("mat_shell", paigeGhost);
            return paigeGhostMat;
        }

        static readonly System.Collections.Generic.Dictionary<Texture, Material> spiritMats = new System.Collections.Generic.Dictionary<Texture, Material>();
        static Material spiritPlain;
        /// <summary>The see-through green body of a ghost: the model's own texture (its detail) on the alpha-blended sprite shader,
        /// tinted ghost green at about half opacity.</summary>
        static Material PaigeSpiritMat(Texture tex)
        {
            var tint = new Color(0.42f, 1f, 0.62f, 0.42f);
            if (!tex) return spiritPlain ? spiritPlain : spiritPlain = new Material(SpriteShader()) { color = tint, renderQueue = 3000 };
            if (spiritMats.TryGetValue(tex, out var m) && m) return m;
            m = new Material(SpriteShader()) { mainTexture = tex, color = tint, renderQueue = 3000 };
            spiritMats[tex] = m;
            return m;
        }

        static readonly System.Collections.Generic.Dictionary<string, Material> paigeTexMats = new System.Collections.Generic.Dictionary<string, Material>();
        /// <summary>One of her particle textures as a see-through material: Deadlock draws these additively, so some have no alpha (the
        /// dragon graphic, the green text streak: black around them). Here each pixel's brightness becomes its alpha and its colour is kept
        /// at full brightness; then tinted. Null without the texture.</summary>
        static Material PaigeTexMat(string texId, Color tint)
        {
            string key = texId + "|" + ColorUtility.ToHtmlStringRGBA(tint);
            if (paigeTexMats.TryGetValue(key, out var m)) return m;
            var row = Sheet.Effect(texId);
            var src = row != null ? DeadlockCache.Image(row.Addressable) : null;
            if (src)
            {
                Texture2D tex;
                try
                {
                    var px = src.GetPixels32();
                    for (int i = 0; i < px.Length; i++)
                    {
                        var c = px[i];
                        int mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                        if (mx == 0) { px[i] = new Color32(0, 0, 0, 0); continue; }
                        px[i] = new Color32((byte)(c.r * 255 / mx), (byte)(c.g * 255 / mx), (byte)(c.b * 255 / mx), (byte)(mx * c.a / 255));
                    }
                    tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = src.name + "_alpha" };
                    tex.SetPixels32(px); tex.Apply(true, true);
                }
                catch (System.Exception e) { Log.Warn($"paige texture {texId}: {e.Message}"); tex = src; }
                m = new Material(SpriteShader()) { mainTexture = tex, color = tint, renderQueue = 3100 };
            }
            paigeTexMats[key] = m;
            return m;
        }

        /// <summary>A quad of one of her textures (<see cref="PaigeTexMat"/>): flat on the ground or facing the camera.</summary>
        static GameObject PaigeQuad(GameObject parent, string texId, Color c, float size, bool billboard)
        {
            var mat = PaigeTexMat(texId, c);
            if (!mat) return null;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "paige_" + texId;
            q.transform.SetParent(parent.transform, false);
            q.transform.localScale = Vector3.one * size;
            if (billboard) q.AddComponent<FaceCamera>();
            else q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return q;
        }

        /// <summary>One of Paige's own models as a green ghost: built from the cache (PropModels), every renderer on the ghost skin, its
        /// clip (the first whose name contains clip) looping at rate, a soft green light inside and spirit mist streaming off it.
        /// Without the model (an older cache) a ball of green light stands in. The root has no life of its own (Stop it).</summary>
        static GameObject PaigeGhostProp(string propId, Vector3 at, Quaternion rot, float scale, string clip, float rate = 1f, bool mist = true)
        {
            var root = Root(at, 0f, null);
            root.transform.rotation = rot;
            var built = PropModels.Build(propId);
            if (built != null)
            {
                built.Root.transform.SetParent(root.transform, false);
                built.Root.transform.localScale = Vector3.one * scale;
                var m = PaigeGhostMat();
                foreach (var r in built.Root.GetComponentsInChildren<Renderer>(true))
                {
                    // two layers on the one submesh (Unity draws extra materials over the last submesh): the model's own texture in a
                    // see-through green, then the ghost rim on top
                    var tex = r.sharedMaterial ? r.sharedMaterial.mainTexture : null;
                    r.sharedMaterials = new[] { PaigeSpiritMat(tex), m };
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                if (built.Anim && clip != null)
                {
                    string pick = null;
                    foreach (AnimationState s in built.Anim) { s.wrapMode = WrapMode.Loop; s.speed = rate; if (pick == null && s.name.Contains(clip)) pick = s.name; }
                    if (pick != null) built.Anim.Play(pick);
                }
                built.Root.AddComponent<PropMeshCleanup>();
            }
            else Particles(root, "mat_glow", paigeGhost, rate: 20, life: 0.4f, speed: 0.2f, size: 1.6f * scale, shapeRadius: 0.4f * scale);
            Light(root, paigeGhost, 7f * scale, 1.6f, Vector3.up * 1.2f * scale).gameObject.AddComponent<Flicker>();
            if (mist)
            {
                // spirit mist trailing off it in the world, and a few of her letters
                // a giant (the ult) keeps its mist and letters near normal size, so they don't fog the screen as it rides past
                float k = Mathf.Sqrt(scale);
                Particles(root, "mat_glow", paigeMist, rate: 18 * k, life: 0.8f, speed: 0.4f, size: 0.9f * k, shapeRadius: 0.7f * scale, world: true);
                PaigeLetters(root, paigePale, rate: 5 * k, life: 1.0f, speed: 0.6f, size: 0.28f * k, shapeRadius: 0.8f * scale, world: true, upward: true);
            }
            return root;
        }

        /// <summary>Particles drawing Paige's letter atlas (2 x 2: A, B, C, z), a random letter each, turning as they drift. Falls back
        /// to green glow without the texture.</summary>
        static ParticleSystem PaigeLetters(GameObject parent, Color c, float rate = 0f, int count = 0, float life = 0.6f, float speed = 1f,
            float size = 0.3f, float shapeRadius = 0f, bool world = false, bool upward = false, float gravity = 0f)
        {
            var ps = Particles(parent, "mat_glow", c, rate: rate, count: count, life: life, speed: speed, size: size, shapeRadius: shapeRadius,
                               world: world, upward: upward, gravity: gravity);
            var mat = SymbolMat("paige_text_tex", Color.white);
            if (!mat) return ps;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.numTilesX = 2; sheet.numTilesY = 2;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.999f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
            return ps;
        }

        static void PaigeLetterBurst(Vector3 at, Color c, int count, float life, float speed, float size, float shapeRadius = 0.2f, bool upward = false)
        {
            var root = Root(at, life + 0.2f, null);
            PaigeLetters(root, c, count: count, life: life, speed: speed, size: size, shapeRadius: shapeRadius, world: true, upward: upward);
        }

        /// <summary>The book flaring as she reads a move: a green glow on the cover, pages of letters fluttering up off it, a flash.</summary>
        static void PaigeBookFlare(Vector3 at, Transform follow, float size = 1f)
        {
            var root = Root(at, 1.2f, follow);
            Particles(root, "mat_glow", paigeGhost, count: 2, life: 0.35f, speed: 0f, size: 1.1f * size);
            PaigeLetters(root, paigePale, count: (int)(10 * size), life: 0.9f, speed: 2.2f, size: 0.22f, shapeRadius: 0.2f, world: true, upward: true);
            Particles(root, "mat_spark", paigePale, count: 8, life: 0.4f, speed: 4f, size: 0.04f, world: true, stretch: 2f);
            Flash(at, paigeGhost, 5f * size, 0.25f, 3f);
        }

        /// <summary>A flat quad of one of her textures holding still (a graphic on the ground), fading in and out over life.</summary>
        static GameObject PaigeGroundGraphic(Vector3 at, string texId, Color c, float size, float life, float yawDeg)
        {
            var root = Root(at + Vector3.up * 0.06f, life + 0.1f, null);
            root.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            var q = PaigeQuad(root, texId, c, size, false);
            if (!q) { Object.Destroy(root); return null; }
            var p = q.AddComponent<QuadPulse>(); p.From = size; p.To = size * 1.08f; p.Life = life; p.Tint = c;
            return root;
        }

        // ------------------------------------------------------------------ M1: Chapter's End
        static GameObject PaigeGun(string moment, Vector3 at, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the spirit page in flight: a bright core, a thin green trail and tumbling letters left behind
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_glow", paigeGhost, rate: 30, life: 0.12f, speed: 0f, size: 1.1f);
                    Particles(root, "mat_glow", paigePale, rate: 30, life: 0.08f, speed: 0f, size: 0.3f);
                    PaigeLetters(root, paigePale, rate: 22, life: 0.55f, speed: 0.5f, size: 0.2f, shapeRadius: 0.15f, world: true);
                    Particles(root, "mat_spark", paigeGhost, rate: 20, life: 0.25f, speed: 1f, size: 0.03f, shapeRadius: 0.1f, world: true);
                    Trail(root, paigeGhost, 0.32f, 0.2f);
                    Light(root, paigeGhost, 4f, 1.2f, Vector3.zero);
                    return root;
                }
                case "muzzle":
                {
                    var root = Root(at, 0.6f, follow);
                    Particles(root, "mat_glow", paigeGhost, count: 2, life: 0.1f, speed: 0f, size: 0.6f);
                    PaigeLetters(root, paigePale, count: 3, life: 0.45f, speed: 1.6f, size: 0.18f, world: true, upward: true);
                    Flash(at, paigeGhost, 3.5f, 0.08f, 2.2f);
                    return root;
                }
                case "hit":
                    PaigeLetterBurst(at, paigePale, 6, 0.55f, 3f, 0.22f);
                    Burst(at, "mat_glow", paigeGhost, 1, 0.15f, 0f, 1.0f);
                    Burst(at, "mat_spark", paigeGhost, 6, 0.25f, 5f, 0.05f);
                    Flash(at, paigeGhost, 3f, 0.1f, 2f);
                    return null;
                case "impact":
                    Burst(at, "mat_glow", paigeGhost, 1, 0.15f, 0f, 0.7f);
                    PaigeLetterBurst(at, paigePale, 3, 0.4f, 1.5f, 0.18f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ M2: Bookwyrm
        static GameObject PaigeWyrmFx(string moment, Vector3 at, float radius, Transform follow, Vector3? to, float life)
        {
            switch (moment)
            {
                case "cast":
                    PaigeBookFlare(at, follow, 1.2f);
                    return null;
                case "spawn":
                {
                    // the dragon graphic flares on the ground where the wyvern appears, and a ring of ghost fire
                    var dir = to.HasValue ? to.Value - at : Vector3.forward; dir.y = 0f;
                    float yaw = dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir).eulerAngles.y : 0f;
                    PaigeGroundGraphic(at, "paige_dragon_tex", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.9f), radius * 1.1f, 1.2f, yaw);
                    Ring(at + Vector3.up * 0.1f, paigeGhost, 0.5f, radius * 1.4f, 0.6f, 0.25f);
                    Burst(at, "mat_glow", paigeGhost, 2, 0.5f, 0f, radius * 1.2f, 0.5f);
                    return null;
                }
                case "loop":
                {
                    // the wyvern itself: Paige's own model and fly clip, a see-through green spirit, breathing ghost fire at the floor
                    var dir = to.HasValue ? to.Value - at : Vector3.forward; dir.y = 0f;
                    var rot = Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward);
                    var root = Root(at, 0f, follow);
                    root.transform.rotation = rot;
                    var wyrm = PaigeGhostProp("paige_dragon", at, rot, 3.6f, "dragon_fly", 1.2f, false);
                    Particles(wyrm, "mat_glow", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.35f), rate: 25, life: 0.6f, speed: 0.3f, size: 1.0f, shapeRadius: 1.4f, world: true);
                    PaigeLetters(wyrm, paigePale, rate: 10, life: 0.9f, speed: 0.8f, size: 0.3f, shapeRadius: 1.6f, world: true);
                    wyrm.transform.SetParent(root.transform, true);
                    // the breath: a cone of green flame from the head (ahead and below it) streaming down onto the floor
                    var mouth = new GameObject("breath"); mouth.transform.SetParent(root.transform, false);
                    mouth.transform.localPosition = new Vector3(0f, -0.3f, 3.0f);
                    mouth.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
                    var cone = Particles(mouth, "mat_fire", paigeFire, rate: 90, life: 0.35f, speed: 11f, size: 0.9f, shapeRadius: 0.15f, world: true, stretch: 0.6f);
                    cone.GetComponent<ParticleSystemRenderer>().sharedMaterial = Remapped("mat_fire", paigeFire);
                    var sh = cone.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14f; sh.radius = 0.15f; sh.rotation = Vector3.zero;
                    var core = Particles(mouth, "mat_glow", paigePale, rate: 40, life: 0.25f, speed: 9f, size: 0.5f, shapeRadius: 0.1f, world: true);
                    var sh2 = core.shape; sh2.shapeType = ParticleSystemShapeType.Cone; sh2.angle = 8f; sh2.radius = 0.1f; sh2.rotation = Vector3.zero;
                    Light(mouth, paigeFire, 8f, 2.2f, Vector3.forward * 2f).gameObject.AddComponent<Flicker>();
                    root.AddComponent<PaigeChildCleanup>().Child = wyrm;
                    return root;
                }
                case "burn":
                {
                    // a patch of floor burning with green ghost fire for life seconds, her green text streak scorched under it
                    float l = life > 0f ? life : 3f;
                    var root = Root(at + Vector3.up * 0.08f, l + 1.2f, null);
                    Particles(root, "mat_gas_fire", paigeFire, rate: 9f * radius, life: 0.9f, speed: 0.3f, size: 1.0f, shapeRadius: radius, world: true, flat: true)
                        .GetComponent<ParticleSystemRenderer>().sharedMaterial = Remapped("mat_gas_fire", paigeFire);
                    Particles(root, "mat_fire", paigeFire, rate: 6f * radius, life: 0.7f, speed: 1.5f, size: 0.8f, shapeRadius: radius * 0.8f, world: true, flat: true)
                        .GetComponent<ParticleSystemRenderer>().sharedMaterial = Remapped("mat_fire", paigeFire);
                    Particles(root, "mat_spark", paigePale, rate: 5f * radius, life: 1f, speed: 2f, size: 0.06f, shapeRadius: radius, world: true, flat: true, gravity: -0.4f);
                    PaigeLetters(root, paigeGhost, rate: 2f * radius, life: 1.2f, speed: 0.8f, size: 0.25f, shapeRadius: radius, world: true, upward: true);
                    Light(root, paigeFire, radius * 2.2f, 1.2f, Vector3.up).gameObject.AddComponent<Flicker>();
                    root.AddComponent<StopAfter>().After = l;
                    var dir = to.HasValue ? to.Value - at : Vector3.forward; dir.y = 0f;
                    PaigeGroundGraphic(at, "paige_streak_tex", new Color(1f, 1f, 1f, 0.7f), radius * 1.3f, l, dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir).eulerAngles.y : 0f);
                    return root;
                }
                case "hit":
                    Burst(at, "mat_glow", paigeGhost, 1, 0.2f, 0f, 1.4f);
                    Burst(at, "mat_spark", paigePale, 10, 0.35f, 6f, 0.05f);
                    PaigeLetterBurst(at, paigePale, 5, 0.6f, 2.5f, 0.22f);
                    return null;
                case "end":
                    // the wyvern unravels back into pages
                    Burst(at, "mat_glow", paigeGhost, 2, 0.5f, 0.5f, 2.5f, 0.8f);
                    PaigeLetterBurst(at, paigePale, 18, 1.1f, 3f, 0.3f, 1.2f, true);
                    Flash(at, paigeGhost, 8f, 0.3f, 2.5f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Shift: Captivating Read
        /// <summary>The giant sword's size, and the height of its root when its tip is 0.6 m into the ground (the model stands tip-up, 0.5
        /// to 5.18 m; it is drawn turned over, and turned 180° about the vertical so its edge, not its flat, leads: creator, 2026-11-10:
        /// "the sword is facing the wrong way ... can we rotate this by 180 on the y axis").</summary>
        const float SwordScale = 1.8f;
        static float SwordRest => 5.18f * SwordScale - 0.6f;
        static Quaternion SwordRot => Quaternion.Euler(180f, 180f, 0f);
        /// <summary>How long the sword rises before it slams (creator, 2026-11-10: "the sword need to rise for 1 second slowly and then
        /// quickly slam into the ground"), and how high above its resting place it climbs.</summary>
        const float SwordRiseTime = 1f, SwordRiseHeight = 8f;
        static GameObject PaigeReadFx(string moment, Vector3 at, float radius, Transform follow, float life)
        {
            switch (moment)
            {
                case "cast":
                    PaigeBookFlare(at, follow, 1.4f);
                    return null;
                case "loop":
                {
                    // the magic circle builds for life seconds: her heptagram and the astrological ring turning, a green dome, the
                    // edge brightening, the Excalibur lettering rising, and her giant sword gathering high above, coming down
                    float l = life > 0f ? life : 1.25f;
                    var root = Root(at, l + 0.3f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.8f));
                    var spinA = new GameObject("hepta"); spinA.transform.SetParent(root.transform, false); spinA.transform.localPosition = Vector3.up * 0.08f;
                    spinA.AddComponent<Spin>().DegPerSec = 25f;
                    PaigeQuad(spinA, "paige_heptagram_tex", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.95f), radius * 2f, false);
                    var spinB = new GameObject("ring"); spinB.transform.SetParent(root.transform, false); spinB.transform.localPosition = Vector3.up * 0.1f;
                    spinB.AddComponent<Spin>().DegPerSec = -40f;
                    PaigeQuad(spinB, "paige_ring_tex", new Color(paigePale.r, paigePale.g, paigePale.b, 0.9f), radius * 2.15f, false);
                    Particles(root, "mat_glow", paigeGhost, rate: 60, life: 0.5f, speed: 0.6f, size: 0.5f, shapeRadius: radius, world: true, ring: true, upward: false);
                    PaigeLetters(root, paigePale, rate: 16, life: 1.0f, speed: 1.4f, size: 0.3f, shapeRadius: radius * 0.9f, world: true, upward: true);
                    var text = new GameObject("excalibur"); text.transform.SetParent(root.transform, false); text.transform.localPosition = Vector3.up * 2.5f;
                    var tq = PaigeQuad(text, "paige_excalibur_tex", paigePale, 2.6f, true);
                    if (tq) tq.transform.localScale = new Vector3(0.65f, 2.6f, 1f);
                    text.AddComponent<PaigeDrift>().Velocity = Vector3.up * 1.2f;
                    var g = Light(root, paigeGhost, radius * 1.8f, 0.5f, Vector3.up).gameObject.AddComponent<GrowLight>(); g.To = 3f; g.Over = l;
                    // the sword, point down, rises slowly out of the circle for a second, then slams down to land exactly at life
                    var low = at + Vector3.up * (SwordRest - 1.5f);
                    var sword = PaigeGhostProp("paige_sword", low, SwordRot, SwordScale, null, 1f, false);
                    var rs = sword.AddComponent<PaigeSwordRiseSlam>();
                    rs.Low = low; rs.High = at + Vector3.up * (SwordRest + SwordRiseHeight); rs.Rest = at + Vector3.up * SwordRest;
                    rs.Rise = Mathf.Min(SwordRiseTime, l - 0.12f); rs.Slam = l - rs.Rise;
                    sword.transform.SetParent(root.transform, true);
                    return root;
                }
                case "hit":
                {
                    // the sword has slammed point-first into the centre (the loop's sword ends here) and stands in its stone,
                    // see-through green; a shockwave runs out across the circle (kept inside it), a flash, a shake, pages and letters
                    // thrown out, the sword-in-stone graphic flashing on the ground
                    var root = Root(at, 2.4f, null);
                    var sword = PaigeGhostProp("paige_sword", at + Vector3.up * SwordRest, SwordRot, SwordScale, null, 1f, false);
                    sword.transform.SetParent(root.transform, true);
                    var stone = PaigeGhostProp("paige_stone", at - Vector3.up * 0.15f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), 2.2f, null, 1f, false);
                    stone.transform.SetParent(root.transform, true);
                    sword.AddComponent<PaigeFadeOut>().Begin(1.6f, 0.7f);
                    stone.AddComponent<PaigeFadeOut>().Begin(1.6f, 0.7f);
                    // the shockwave: two rings racing out to the circle's edge, a flat wave of sparks and mist along the ground
                    Ring(at + Vector3.up * 0.15f, paigeGhost, 0.5f, radius, 0.35f, 0.7f);
                    Ring(at + Vector3.up * 0.25f, paigePale, 0.3f, radius * 0.97f, 0.5f, 0.3f);
                    Ring(at + Vector3.up * 0.1f, paigeGhost, 0.2f, radius * 0.6f, 0.25f, 0.4f);
                    Burst(at + Vector3.up * 0.2f, "mat_spark", paigePale, 50, 0.4f, radius / 0.45f, 0.08f, 0.5f, true);
                    Burst(at + Vector3.up * 0.3f, "mat_glow", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.5f), 14, 0.45f, radius / 0.5f, 1.4f, 0.5f, true);
                    Burst(at, "mat_glow", paigeGhost, 3, 0.5f, 1f, radius * 0.9f, 0.6f);
                    Burst(at, "mat_spark", paigePale, 30, 0.6f, 12f, 0.07f, 0.5f, false, true, 1f);
                    PaigeLetterBurst(at + Vector3.up * 0.5f, paigePale, 26, 1.2f, 6f, 0.32f, radius * 0.4f, true);
                    PaigeGroundGraphic(at, "paige_sword_tex", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.9f), radius * 1.3f, 0.9f, Random.Range(0f, 360f));
                    Flash(at + Vector3.up, paigeGhost, radius * 3f, 0.4f, 6f);
                    Shake(at, radius * 3f, 2.5f, 0.35f);
                    return root;
                }
                case "root":
                {
                    // immobilized: a small heptagram under the enemy and letters wound around it for the root's second
                    var root = Root(at, 1.1f, follow);
                    var hold = new GameObject("glyph"); hold.transform.SetParent(root.transform, false); hold.transform.localPosition = Vector3.up * 0.05f;
                    hold.AddComponent<Spin>().DegPerSec = 90f;
                    PaigeQuad(hold, "paige_heptagram_tex", paigeGhost, Mathf.Max(1.2f, radius * 2.4f), false);
                    PaigeLetters(root, paigePale, rate: 18, life: 0.6f, speed: 0.4f, size: 0.2f, shapeRadius: Mathf.Max(0.5f, radius), upward: true);
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ R / Q: the knights
        static GameObject PaigeKnightsFx(string moment, Vector3 at, float scale, Transform follow, Vector3? to)
        {
            scale = Mathf.Max(1f, scale);
            switch (moment)
            {
                case "cast":
                {
                    // the book flares and the knight graphic rises over her as she reads the charge
                    PaigeBookFlare(at, follow, 1.2f + 0.3f * scale);
                    var root = Root(at + Vector3.up * 1.6f, 1.0f, null);
                    var q = PaigeQuad(root, "paige_knight_tex", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.9f), 1.6f * Mathf.Sqrt(scale), true);
                    if (q) { var p = q.AddComponent<QuadPulse>(); p.From = 1.2f * Mathf.Sqrt(scale); p.To = 2.4f * Mathf.Sqrt(scale); p.Life = 1.0f; p.Tint = paigeGhost; }
                    root.AddComponent<PaigeDrift>().Velocity = Vector3.up * 1.5f;
                    return null;
                }
                case "knight":
                {
                    // one spectral horse knight, Paige's own model galloping with its own clip, moved by PaigeKnights; hoof sparks and
                    // a low green mist behind it on the ground
                    var dir = to.HasValue ? to.Value - at : Vector3.forward; dir.y = 0f;
                    var k = PaigeGhostProp("paige_knight", at, Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward), scale, "knight", 1.3f);
                    float sq = Mathf.Sqrt(scale);
                    Particles(k, "mat_spark", paigePale, rate: 18 * sq, life: 0.35f, speed: 2.5f, size: 0.05f * sq, shapeRadius: 0.6f * scale, world: true, flat: true, gravity: 0.6f);
                    Particles(k, "mat_glow", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.4f), rate: 12 * sq, life: 0.9f, speed: 0.2f, size: 1.4f * sq, shapeRadius: 0.8f * scale, world: true, flat: true);
                    k.AddComponent<PaigeFadeIn>();
                    return k;
                }
                case "ride":
                    Ring(at + Vector3.up * 0.1f, paigeGhost, 0.5f, 3f * scale, 0.5f, 0.2f * scale);
                    Burst(at, "mat_glow", paigeGhost, 2, 0.4f, 0f, 2f * scale, 1f);
                    return null;
                case "hit":
                    Burst(at, "mat_glow", paigeGhost, 1, 0.25f, 0f, 1.5f * scale);
                    Burst(at, "mat_spark", paigePale, (int)(10 * scale), 0.4f, 6f, 0.06f);
                    PaigeLetterBurst(at, paigePale, (int)(6 * scale), 0.7f, 3f, 0.25f);
                    Flash(at, paigeGhost, 4f * scale, 0.15f, 2.5f);
                    return null;
                case "heal":
                {
                    var root = Root(at, 1.2f, follow);
                    Particles(root, "mat_glow", paigeGhost, count: 14, life: 0.9f, speed: 1.6f, size: 0.4f, shapeRadius: 0.6f, upward: true);
                    PaigeLetters(root, paigePale, count: 6, life: 1f, speed: 1.4f, size: 0.22f, shapeRadius: 0.5f, upward: true);
                    return root;
                }
                case "fade":
                    Burst(at + Vector3.up * scale, "mat_glow", paigeGhost, 2, 0.5f, 0.6f, 1.8f * scale, 0.6f * scale);
                    PaigeLetterBurst(at + Vector3.up * scale, paigePale, (int)(10 * scale), 1f, 2.5f, 0.28f, 0.8f * scale, true);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Plot Armor
        static GameObject PaigePlotArmor(string moment, Vector3 at, Transform follow)
        {
            if (moment != "loop") return null;
            var root = Root(at, 0f, follow);
            Particles(root, "mat_glow", new Color(paigeGhost.r, paigeGhost.g, paigeGhost.b, 0.45f), rate: 8, life: 0.8f, speed: 0.3f, size: 1.2f, shapeRadius: 0.5f, upward: true);
            var orbit = new GameObject("orbit"); orbit.transform.SetParent(root.transform, false); orbit.AddComponent<Spin>().DegPerSec = 70f;
            PaigeLetters(orbit, paigePale, rate: 6, life: 1.2f, speed: 0.1f, size: 0.2f, shapeRadius: 1.0f);
            Light(root, paigeGhost, 4f, 0.8f, Vector3.up * 0.6f);
            return root;
        }

        // ------------------------------------------------------------------ small behaviours

        /// <summary>Still prop meshes are made per copy (PropModels): free them with the copy.</summary>
        sealed class PropMeshCleanup : MonoBehaviour
        {
            void OnDestroy() { foreach (var mf in GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh) Destroy(mf.sharedMesh); }
        }

        /// <summary>Destroys a prop that was parented elsewhere when its effect root goes.</summary>
        sealed class PaigeChildCleanup : MonoBehaviour
        {
            public GameObject Child;
            void OnDestroy() { if (Child) Destroy(Child); }
        }

        /// <summary>The sword moving from From to To over Over seconds, easing in (gathering, then the slam).</summary>
        /// <summary>The Captivating Read sword: rises slowly (easing out) from Low to High over Rise seconds, then drops fast (easing
        /// in) to Rest over Slam seconds and hides there, where the hit moment's sword takes over.</summary>
        sealed class PaigeSwordRiseSlam : MonoBehaviour
        {
            public Vector3 Low, High, Rest; public float Rise = 1f, Slam = 0.25f;
            float age;
            void Update()
            {
                age += Time.deltaTime;
                if (age < Rise)
                {
                    float t = age / Mathf.Max(0.01f, Rise);
                    transform.position = Vector3.LerpUnclamped(Low, High, 1f - (1f - t) * (1f - t));
                    return;
                }
                float u = Mathf.Clamp01((age - Rise) / Mathf.Max(0.01f, Slam));
                transform.position = Vector3.LerpUnclamped(High, Rest, u * u * u);
                if (u >= 1f) gameObject.SetActive(false);
            }
        }

        /// <summary>Drifts at a steady velocity (letters and graphics rising).</summary>
        sealed class PaigeDrift : MonoBehaviour
        {
            public Vector3 Velocity;
            void Update() => transform.position += Velocity * Time.deltaTime;
        }

        /// <summary>A ghost growing out of nothing over a quarter second (the knights riding out of the page).</summary>
        sealed class PaigeFadeIn : MonoBehaviour
        {
            float age; Vector3 full;
            void Start() { full = transform.localScale; transform.localScale = full * 0.2f; }
            void Update()
            {
                if (age >= 0.25f) return;
                age += Time.deltaTime;
                transform.localScale = full * Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(age / 0.25f));
            }
        }

        /// <summary>A ghost that holds for a while, then shrinks away and is gone (the sword in its stone).</summary>
        sealed class PaigeFadeOut : MonoBehaviour
        {
            float hold, over, age; Vector3 full;
            public void Begin(float holdFor, float fadeOver) { hold = holdFor; over = fadeOver; full = transform.localScale; }
            void Update()
            {
                age += Time.deltaTime;
                if (age < hold) return;
                float t = Mathf.Clamp01((age - hold) / Mathf.Max(0.01f, over));
                transform.localScale = full * (1f - t);
                if (t >= 1f) Destroy(gameObject);
            }
        }
    }
}
