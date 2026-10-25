using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Wraith's visuals: purple smoke magic (the creator's first ask for her, 2026-10-23), built like the others from RoR2's
    /// smoke, glow and spark materials tinted violet, with Deadlock's own card model and, since the 2026-10-25 overhaul, Deadlock's
    /// own Wraith textures read from the player's install (effects kind texture): the card suits (club, diamond, heart, spade) flying
    /// off every card and bursting where it hits, her eye sigil under Project Mind's projection, the Full Auto ring, her eye over each
    /// enemy Telekinesis lifts. Each card trick has its colour and suit on top of the purple: bleed a red heart, crit a gold diamond,
    /// stun a pale-blue spade, burn an orange club (the trick rides in the angle argument).</summary>
    public static partial class Vfx
    {
        static readonly Color wraithViolet = new Color(0.56f, 0.31f, 0.85f), wraithDeep = new Color(0.33f, 0.12f, 0.6f),
                              wraithLilac = new Color(0.8f, 0.62f, 1f), wraithMagenta = new Color(0.86f, 0.42f, 1f),
                              wraithSmoke = new Color(0.78f, 0.52f, 1f, 1f), wraithSmokeDark = new Color(0.55f, 0.3f, 0.9f, 0.95f),
                              wraithBad = new Color(1f, 0.3f, 0.35f, 0.8f);
        static readonly Color[] cardColors = { new Color(0.92f, 0.12f, 0.16f), new Color(1f, 0.8f, 0.28f), new Color(0.6f, 0.85f, 1f), new Color(1f, 0.5f, 0.15f) };
        /// <summary>Each trick's suit on Deadlock's suit sheet (frames across its top row: club 0, diamond 1, heart 2, spade 3).</summary>
        static readonly int[] cardSuits = { 2, 1, 3, 0 };

        static Color CardColor(float suit) => cardColors[Mathf.Clamp(Mathf.RoundToInt(suit), 0, cardColors.Length - 1)];
        static int CardSuitFrame(float suit) => cardSuits[Mathf.Clamp(Mathf.RoundToInt(suit), 0, cardSuits.Length - 1)];

        static GameObject Wraith(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle)
        {
            switch (id)
            {
                case "wraith_gun": return WraithGun(moment, at);
                case "wraith_card": return WraithCard(moment, at, radius, follow, angle);
                case "wraith_mind": return WraithMind(moment, at, radius, to);
                case "wraith_fullauto": return WraithFullAuto(moment, at, radius, follow);
                case "wraith_tk": return WraithTk(moment, at, radius, follow, to);
            }
            return null;
        }

        // ------------------------------------------------------------------ Deadlock's Wraith textures

        static readonly Dictionary<string, Material> symbolMats = new Dictionary<string, Material>();

        /// <summary>A Deadlock texture (effects row of kind texture) on Unity's sprite shader, tinted; null when the cache has
        /// no such image (an older cache: the effect goes without its symbols).</summary>
        static Material SymbolMat(string texId, Color tint)
        {
            string key = texId + "|" + ColorUtility.ToHtmlStringRGBA(tint);
            if (symbolMats.TryGetValue(key, out var m)) return m;
            var row = Sheet.Effect(texId);
            var tex = row != null ? DeadlockCache.Image(row.Addressable) : null;
            if (tex)
            {
                m = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, color = tint, renderQueue = 3100 };
            }
            symbolMats[key] = m;
            return m;
        }

        /// <summary>Particles drawing one frame of Deadlock's suit sheet (4 x 4: the top row's club, diamond, heart, spade) or a
        /// whole texture (frame -1); the rest as <see cref="Particles"/>. Null without the texture.</summary>
        static ParticleSystem Symbols(GameObject parent, string texId, int frame, Color c, float rate = 0f, int count = 0, float life = 0.5f,
            float speed = 1f, float size = 0.4f, float shapeRadius = 0f, bool world = false, bool upward = false, float gravity = 0f, bool ring = false)
        {
            var mat = SymbolMat(texId, Color.white);
            if (!mat) return null;
            var ps = Particles(parent, "mat_glow", c, rate: rate, count: count, life: life, speed: speed, size: size, shapeRadius: shapeRadius,
                               world: world, upward: upward, gravity: gravity, ring: ring);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            if (frame >= 0)
            {
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = true;
                sheet.numTilesX = 4; sheet.numTilesY = 4;
                sheet.animation = ParticleSystemAnimationType.WholeSheet;
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                sheet.startFrame = new ParticleSystem.MinMaxCurve(frame / 16f);
            }
            // symbols spin slowly as they drift
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            return ps;
        }

        /// <summary>A one-off burst of symbols at a point.</summary>
        static void SymbolBurst(Vector3 at, string texId, int frame, Color c, int count, float life, float speed, float size, float shapeRadius = 0.2f, bool upward = false)
        {
            var root = Root(at, life + 0.2f, null);
            if (!Symbols(root, texId, frame, c, count: count, life: life, speed: speed, size: size, shapeRadius: shapeRadius, world: true, upward: upward))
                Object.Destroy(root);
        }

        /// <summary>A flat textured quad (a sigil on the ground, a ring): lying down, or facing the camera (billboard).</summary>
        static GameObject SymbolQuad(GameObject parent, string texId, Color c, float size, bool billboard)
        {
            var mat = SymbolMat(texId, c);
            if (!mat) return null;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "sym_" + texId;
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

        /// <summary>A quad that grows from from to to times its size and fades out over life (a ring sweeping out).</summary>
        static void SymbolPulse(Vector3 at, string texId, Color c, float from, float to, float life, bool billboard = false)
        {
            var root = Root(at, life + 0.05f, null);
            var q = SymbolQuad(root, texId, c, from, billboard);
            if (!q) { Object.Destroy(root); return; }
            var p = q.AddComponent<QuadPulse>(); p.From = from; p.To = to; p.Life = life; p.Tint = c;
        }

        sealed class FaceCamera : MonoBehaviour
        {
            void LateUpdate()
            {
                var cam = Camera.main;
                if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
            }
        }

        sealed class QuadPulse : MonoBehaviour
        {
            public float From, To, Life; public Color Tint;
            float age; MeshRenderer mr; MaterialPropertyBlock mpb;
            void Start() { mr = GetComponent<MeshRenderer>(); mpb = new MaterialPropertyBlock(); }
            void LateUpdate()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / Mathf.Max(0.01f, Life));
                transform.localScale = Vector3.one * Mathf.Lerp(From, To, 1f - (1f - t) * (1f - t));
                if (mr) { mpb.SetColor("_Color", new Color(Tint.r, Tint.g, Tint.b, Tint.a * (1f - t))); mr.SetPropertyBlock(mpb); }
            }
        }

        /// <summary>A rolling cloud of purple smoke: a dark core, lighter puffs on top and a few violet glints.</summary>
        static void PurpleSmoke(Vector3 at, float size, int count, float life, float speed, float spread, bool upward = true)
        {
            // RoR2's smoke material darkens on a dark stage: bright lilac smoke, big and plenty, with an additive violet glow inside it
            Burst(at, "mat_smoke", wraithSmokeDark, Mathf.Max(1, count / 2), life * 1.3f, speed * 0.6f, size * 1.9f, shapeRadius: spread, upward: upward);
            Burst(at, "mat_smoke", wraithSmoke, count, life * 1.1f, speed, size * 1.5f, shapeRadius: spread, upward: upward);
            Burst(at, "mat_glow", wraithViolet, Mathf.Max(2, count / 2), life * 0.8f, speed * 0.7f, size * 1.1f, shapeRadius: spread, upward: upward);
            Burst(at, "mat_glow", wraithMagenta, Mathf.Max(1, count / 4), life * 0.5f, speed * 0.5f, size * 0.5f, shapeRadius: spread);
        }

        /// <summary>A scatter of all four suits in lilac and violet (a teleport, a reload).</summary>
        static void SuitScatter(Vector3 at, int count, float speed, float size, float spread)
        {
            for (int f = 0; f < 4; f++)
                SymbolBurst(at, "wraith_suits_tex", f, f % 2 == 0 ? wraithLilac : wraithMagenta, Mathf.Max(1, count / 4), 0.9f, speed, size, spread, true);
        }

        // ------------------------------------------------------------------ The Rake
        static GameObject WraithGun(string moment, Vector3 at)
        {
            if (moment != "muzzle") return null;
            // a violet flash and a small curl of purple smoke off the barrel (ten times a second: kept light)
            Burst(at, "mat_glow", wraithLilac, 1, 0.06f, 0f, 0.45f);
            Burst(at, "mat_smoke", wraithSmoke, 1, 0.45f, 0.8f, 0.25f, shapeRadius: 0.05f, upward: true);
            Burst(at, "mat_spark", wraithMagenta, 2, 0.1f, 6f, 0.04f);
            return null;
        }

        // ------------------------------------------------------------------ Card Trick
        static GameObject WraithCard(string moment, Vector3 at, float radius, Transform follow, float suit)
        {
            var c = CardColor(suit);
            int frame = CardSuitFrame(suit);
            switch (moment)
            {
                case "cast":
                    // the card conjured in her hand from a puff of purple smoke, its suit flaring in its trick's colour
                    Burst(at, "mat_smoke", wraithSmoke, 6, 0.5f, 0.6f, 0.45f, shapeRadius: 0.15f, upward: true);
                    Burst(at, "mat_glow", c, 2, 0.15f, 0f, 0.6f);
                    Burst(at, "mat_spark", wraithLilac, 8, 0.25f, 2.5f, 0.04f, shapeRadius: 0.1f);
                    SymbolBurst(at, "wraith_suits_tex", frame, c, 3, 0.45f, 1.2f, 0.3f, 0.1f, true);
                    return null;
                case "loop":
                {
                    // Deadlock's card spinning end over end, a violet ribbon and smoke, and its suit shedding behind it in its colour
                    var root = Root(at, 0f, follow);
                    var spin = new GameObject("spin"); spin.transform.SetParent(root.transform, false);
                    var card = PropModels.Build("wraith_card_prop");
                    if (card != null)
                    {
                        card.Root.transform.SetParent(spin.transform, false);
                        card.Root.transform.localScale = Vector3.one * ModelBuilder.GlobalScale * 6f;
                    }
                    else
                    {
                        var quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Object.Destroy(quad.GetComponent<Collider>());
                        quad.transform.SetParent(spin.transform, false);
                        quad.transform.localScale = new Vector3(0.45f, 0.7f, 0.04f);
                        quad.GetComponent<MeshRenderer>().sharedMaterial = new Material(ModelBuilder.Template) { color = new Color(0.95f, 0.92f, 0.85f) };
                    }
                    spin.AddComponent<Tumble>().DegPerSec = 900f;
                    Particles(root, "mat_smoke", wraithSmoke, rate: 60, life: 0.7f, speed: 0.4f, size: 0.7f, shapeRadius: 0.1f, world: true);
                    Particles(root, "mat_glow", wraithViolet, rate: 40, life: 0.35f, speed: 0.2f, size: 0.6f, shapeRadius: 0.1f, world: true);
                    Particles(root, "mat_glow", c, rate: 25, life: 0.12f, speed: 0f, size: 0.7f);
                    Particles(root, "mat_spark", wraithLilac, rate: 30, life: 0.25f, speed: 1f, size: 0.04f, world: true);
                    Symbols(root, "wraith_suits_tex", frame, c, rate: 22, life: 0.5f, speed: 0.6f, size: 0.32f, shapeRadius: 0.1f, world: true);
                    Trail(root, wraithViolet, 0.22f, 0.3f);
                    Trail(spin, wraithLilac, 0.08f, 0.18f);
                    Light(root, c, 3f, 1.2f, Vector3.zero);
                    return root;
                }
                case "hit":
                {
                    // a burst of purple smoke and the trick's suit and colour: blood, gold, frost-blue crackle or fire
                    PurpleSmoke(at, 1.1f, 12, 0.9f, 2.2f, 0.3f);
                    Burst(at, "mat_glow", c, 3, 0.2f, 0f, 2.2f);
                    Burst(at, "mat_spark", c, 22, 0.45f, 7f, 0.07f, shapeRadius: 0.2f, gravity: 0.5f);
                    SymbolBurst(at, "wraith_suits_tex", frame, c, 8, 0.7f, 5f, 0.45f, 0.2f);
                    SymbolPulse(at, "wraith_suits_tex", c, 0.4f, 2.4f, 0.35f, true);
                    Ring(at, c, 0.3f, 2.5f, 0.3f, 0.25f, true);
                    Flash(at, c, 6f, 0.25f, 5f);
                    switch (Mathf.RoundToInt(suit))
                    {
                        case 0: Burst(at, "mat_glow", new Color(0.6f, 0.02f, 0.05f), 14, 0.7f, 4f, 0.18f, shapeRadius: 0.2f, gravity: 2.5f); break;
                        case 1:
                            Burst(at, "mat_spark", new Color(1f, 0.92f, 0.6f), 30, 0.6f, 11f, 0.09f, shapeRadius: 0.2f);
                            Flash(at, cardColors[1], 10f, 0.35f, 8f);
                            break;
                        case 2:
                            for (int i = 0; i < 4; i++) Bolt(at, at + Random.onUnitSphere * 1.8f, c, 0.12f, 0.25f, 6, 0.3f, true);
                            break;
                        case 3: Burst(at, "mat_fire_puff", c, 10, 0.6f, 2.5f, 0.9f, shapeRadius: 0.3f, upward: true); break;
                    }
                    return null;
                }
                case "end":
                    PurpleSmoke(at, 0.7f, 6, 0.6f, 1.2f, 0.2f);
                    Burst(at, "mat_spark", c, 8, 0.3f, 4f, 0.05f);
                    SymbolBurst(at, "wraith_suits_tex", frame, c, 3, 0.5f, 1.5f, 0.3f);
                    return null;
                case "halo":
                {
                    // behind each stored card over her head (Deadlock's cards are dark purple aces): a bright lilac glow so the
                    // hand reads on any stage, and the odd lilac glint
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_glow", wraithLilac, rate: 14, life: 0.5f, speed: 0f, size: 0.7f);
                    Particles(root, "mat_glow", wraithMagenta, rate: 6, life: 0.4f, speed: 0f, size: 0.45f);
                    Particles(root, "mat_spark", wraithLilac, rate: 3, life: 0.6f, speed: 0.3f, size: 0.025f, shapeRadius: 0.1f, world: true, upward: true);
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Project Mind
        static GameObject WraithMind(string moment, Vector3 at, float radius, Vector3? to)
        {
            switch (moment)
            {
                case "aim":
                {
                    // where she will appear: her eye sigil turning on a smoke circle with a faint column over it ("good"), or a
                    // dim red circle where she can't go ("bad"; MindMarker swaps them). The projection of her is the state's.
                    var root = Root(at, 0f, null);
                    var good = new GameObject("good"); good.transform.SetParent(root.transform, false);
                    var bad = new GameObject("bad"); bad.transform.SetParent(root.transform, false);
                    var swirl = new GameObject("swirl"); swirl.transform.SetParent(good.transform, false); swirl.transform.localPosition = Vector3.up * 0.06f;
                    swirl.AddComponent<Spin>().DegPerSec = 60f;
                    SymbolQuad(swirl, "wraith_sigil_tex", new Color(0.86f, 0.6f, 1f, 0.95f), radius * 2.6f, false);
                    var ring = new GameObject("ring"); ring.transform.SetParent(good.transform, false); ring.transform.localPosition = Vector3.up * 0.04f;
                    ring.AddComponent<Spin>().DegPerSec = -25f;
                    SymbolQuad(ring, "wraith_ring_tex", new Color(0.62f, 0.35f, 0.95f, 0.55f), radius * 3.4f, false);
                    Particles(swirl, "mat_smoke", wraithSmoke, rate: 40, life: 1f, speed: 0.4f, size: 1.1f, shapeRadius: radius, ring: true);
                    Particles(swirl, "mat_glow", wraithMagenta, rate: 30, life: 0.5f, speed: 0.2f, size: 0.45f, shapeRadius: radius, ring: true);
                    Particles(good, "mat_glow", wraithViolet, rate: 18, life: 0.8f, speed: 1.2f, size: 0.8f, shapeRadius: radius * 0.6f, upward: true);
                    Particles(good, "mat_spark", wraithLilac, rate: 20, life: 0.6f, speed: 1.8f, size: 0.04f, shapeRadius: radius * 0.6f, upward: true);
                    Symbols(good, "wraith_suits_tex", 1, wraithLilac, rate: 4, life: 1.4f, speed: 0.8f, size: 0.28f, shapeRadius: radius * 0.8f, upward: true);
                    Light(good, wraithViolet, radius * 3f, 1.5f, Vector3.up * 0.6f).gameObject.AddComponent<Flicker>();
                    SymbolQuad(bad, "wraith_ring_tex", wraithBad, radius * 2.6f, false);
                    Particles(bad, "mat_smoke", new Color(0.9f, 0.3f, 0.35f, 0.6f), rate: 12, life: 0.8f, speed: 0.3f, size: 0.8f, shapeRadius: radius, ring: true);
                    bad.SetActive(false);
                    root.AddComponent<MindMarker>();
                    return root;
                }
                case "end":
                case "hit":
                {
                    // where she leaves and where she lands: a column of purple smoke, a scatter of suits, her sigil flashing out, a ring
                    bool arrive = moment == "hit";
                    PurpleSmoke(at + Vector3.up * 0.8f, 1.5f, arrive ? 26 : 20, 1.2f, 2.5f, 0.7f);
                    Burst(at, "mat_smoke", wraithSmoke, 14, 1f, radius * 2.5f, 1.1f, shapeRadius: 0.4f, flat: true);
                    Burst(at + Vector3.up, "mat_spark", wraithLilac, 30, 0.6f, 5f, 0.06f, shapeRadius: 0.5f, upward: true);
                    Burst(at + Vector3.up, "mat_glow", wraithMagenta, 3, 0.25f, 0f, 2.6f);
                    SuitScatter(at + Vector3.up, arrive ? 16 : 12, 4f, 0.4f, 0.5f);
                    SymbolPulse(at + Vector3.up * 0.1f, "wraith_sigil_tex", new Color(0.86f, 0.6f, 1f, 1f), radius * 1.5f, radius * 4.5f, 0.5f);
                    Ring(at + Vector3.up * 0.15f, wraithViolet, 0.4f, radius * 2.5f, 0.45f, 0.35f);
                    Flash(at + Vector3.up, wraithViolet, 8f, 0.4f, 6f);
                    if (arrive) Shake(at, 12f, 0.6f, 0.2f);
                    return null;
                }
            }
            return null;
        }

        /// <summary>Project Mind's marker: shows the "good" sigil or the dim red "bad" circle.</summary>
        public sealed class MindMarker : MonoBehaviour
        {
            GameObject good, bad; bool shown = true;
            void Awake() { good = transform.Find("good")?.gameObject; bad = transform.Find("bad")?.gameObject; }
            public void SetValid(bool v)
            {
                if (v == shown) return;
                shown = v;
                if (good) good.SetActive(v);
                if (bad) bad.SetActive(!v);
            }
        }

        static Material ghostMat;
        /// <summary>Project Mind's projection: a see-through purple snapshot of the body's pose (its skinned meshes baked once),
        /// floating where she will appear, turned the way she faces. Owner only (the state builds it).</summary>
        public static GameObject MindGhost(CharacterBody body)
        {
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            if (!model) return null;
            if (!ghostMat)
            {
                var row = Sheet.Effect("mat_ghost");
                ghostMat = row != null ? Remapped("mat_ghost", wraithMagenta, true) : Mat("mat_shell", wraithViolet);
            }
            var root = new GameObject("WraithGhost");
            var pose = new GameObject("pose"); pose.transform.SetParent(root.transform, false);
            var basePos = body.footPosition; var baseRot = Quaternion.Inverse(Quaternion.LookRotation(Flat(body.transform.forward)));
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled || !smr.gameObject.activeInHierarchy || !smr.sharedMesh) continue;
                var mesh = new Mesh();
                smr.BakeMesh(mesh);
                var go = new GameObject("ghost_" + smr.name);
                go.transform.SetParent(pose.transform, false);
                // the baked mesh is in the renderer's space, without its scale: put it where the renderer sits against her feet
                go.transform.localPosition = baseRot * (smr.transform.position - basePos);
                go.transform.localRotation = baseRot * smr.transform.rotation;
                go.transform.localScale = smr.transform.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                var mats = new Material[Mathf.Max(1, smr.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                go.AddComponent<GhostMesh>();
            }
            // a shimmer of smoke and suits rising off it
            Particles(root, "mat_glow", wraithViolet, rate: 14, life: 0.7f, speed: 0.6f, size: 0.7f, shapeRadius: 0.4f, upward: true);
            Particles(root, "mat_smoke", wraithSmoke, rate: 10, life: 1.1f, speed: 0.5f, size: 0.8f, shapeRadius: 0.4f, upward: true);
            Symbols(root, "wraith_suits_tex", 3, wraithMagenta, rate: 3, life: 1.2f, speed: 0.7f, size: 0.25f, shapeRadius: 0.5f, upward: true);
            root.AddComponent<GhostBob>();
            return root;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward; }

        sealed class GhostMesh : MonoBehaviour
        {
            void OnDestroy() { var mf = GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) Destroy(mf.sharedMesh); }
        }

        sealed class GhostBob : MonoBehaviour
        {
            void LateUpdate()
            {
                var p = transform.Find("pose");
                if (p) p.localPosition = Vector3.up * (0.08f + Mathf.Sin(Time.time * 2.5f) * 0.06f);
            }
        }

        /// <summary>Cards drifting from her to the projection, end over end, along the line between them (owner only).</summary>
        public sealed class MindLink : MonoBehaviour
        {
            public Transform From, To;
            readonly List<Transform> cards = new List<Transform>();
            float phase;
            const int Count = 6; const float Speed = 0.45f;

            void Start()
            {
                for (int i = 0; i < Count; i++)
                {
                    var holder = new GameObject("linkcard");
                    holder.transform.SetParent(transform, false);
                    var card = PropModels.Build("wraith_card_prop");
                    if (card != null)
                    {
                        card.Root.transform.SetParent(holder.transform, false);
                        card.Root.transform.localScale = Vector3.one * ModelBuilder.GlobalScale * 3.2f;
                    }
                    holder.AddComponent<Tumble>().DegPerSec = 200f + i * 40f;
                    Particles(holder, "mat_glow", wraithViolet, rate: 10, life: 0.3f, speed: 0f, size: 0.4f);
                    Trail(holder, wraithLilac, 0.06f, 0.25f);
                    cards.Add(holder.transform);
                }
            }

            void LateUpdate()
            {
                if (!From || !To) return;
                phase += Time.deltaTime * Speed;
                var a = From.position; var b = To.position + Vector3.up * 1f;
                for (int i = 0; i < cards.Count; i++)
                {
                    float t = Mathf.Repeat(phase + (float)i / cards.Count, 1f);
                    // a gentle arch between them; each card shrinks to nothing at either end so it appears and goes
                    var p = Vector3.Lerp(a, b, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.2f;
                    cards[i].position = p;
                    cards[i].localScale = Vector3.one * Mathf.Sin(t * Mathf.PI);
                }
            }
        }

        // ------------------------------------------------------------------ Full Auto
        static GameObject WraithFullAuto(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // a ring of purple smoke and Deadlock's Full Auto ring bursting out around her as she slams the new magazine in
                    var root = Root(at, 2f, follow);
                    Particles(root, "mat_smoke", wraithSmoke, count: 26, life: 1.1f, speed: 3f, size: 1.1f, shapeRadius: radius + 0.3f, ring: true);
                    Particles(root, "mat_smoke", wraithSmokeDark, count: 12, life: 1.3f, speed: 1.5f, size: 1.4f, shapeRadius: radius, upward: true);
                    Particles(root, "mat_spark", wraithLilac, count: 35, life: 0.6f, speed: 5f, size: 0.05f, shapeRadius: radius, upward: true);
                    Particles(root, "mat_glow", wraithMagenta, count: 4, life: 0.3f, speed: 0f, size: 3f);
                    SymbolPulse(at - Vector3.up * 0.85f, "wraith_ring_tex", new Color(0.75f, 0.45f, 1f, 0.9f), 1f, 6f, 0.6f);
                    SuitScatter(at, 12, 3.5f, 0.35f, radius);
                    Ring(at - Vector3.up * 0.6f, wraithViolet, 0.5f, 4f, 0.45f, 0.4f);
                    Flash(at, wraithViolet, 7f, 0.4f, 5f);
                    return root;
                }
                case "loop":
                {
                    // smoke and sparks curling off The Rake while it runs hot, and suits spiralling round the barrel
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_smoke", wraithSmoke, rate: 30, life: 0.8f, speed: 0.8f, size: 0.6f, shapeRadius: 0.1f, world: true, upward: true);
                    Particles(root, "mat_spark", wraithMagenta, rate: 18, life: 0.3f, speed: 2f, size: 0.04f, world: true);
                    Particles(root, "mat_glow", wraithViolet, rate: 25, life: 0.4f, speed: 0.5f, size: 0.6f, world: true);
                    var spiral = new GameObject("spiral"); spiral.transform.SetParent(root.transform, false);
                    spiral.AddComponent<Spin>().DegPerSec = 420f;
                    var arm = new GameObject("arm"); arm.transform.SetParent(spiral.transform, false); arm.transform.localPosition = new Vector3(0.28f, 0f, 0f);
                    Symbols(arm, "wraith_suits_tex", 0, wraithLilac, rate: 14, life: 0.45f, speed: 0.2f, size: 0.16f, world: true);
                    var arm2 = new GameObject("arm2"); arm2.transform.SetParent(spiral.transform, false); arm2.transform.localPosition = new Vector3(-0.28f, 0f, 0f);
                    Symbols(arm2, "wraith_suits_tex", 2, wraithMagenta, rate: 14, life: 0.45f, speed: 0.2f, size: 0.16f, world: true);
                    Light(root, wraithViolet, 3f, 1f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Telekinesis
        static GameObject WraithTk(string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "cast":
                {
                    // her mind reaching out: a purple shockwave, Deadlock's ring and a rolling smoke wave sweeping out to the edge of the grab
                    Ring(at + Vector3.up * 0.3f, wraithViolet, 1f, radius, 0.7f, 1.2f);
                    Ring(at + Vector3.up * 1.2f, wraithMagenta, 0.5f, radius * 0.8f, 0.9f, 0.6f);
                    SymbolPulse(at + Vector3.up * 0.15f, "wraith_ring_tex", new Color(0.7f, 0.4f, 1f, 0.8f), 2f, radius * 2f, 0.8f);
                    SymbolPulse(at + Vector3.up * 0.2f, "wraith_sigil_tex", new Color(0.86f, 0.6f, 1f, 1f), 1f, radius * 0.7f, 0.7f);
                    Burst(at, "mat_smoke", wraithSmoke, 40, 1.4f, radius * 0.9f, 2.6f, shapeRadius: 1f, flat: true);
                    Burst(at, "mat_smoke", wraithSmokeDark, 16, 1.6f, radius * 0.5f, 3.2f, shapeRadius: 1f, flat: true);
                    PurpleSmoke(at + Vector3.up, 1.6f, 18, 1.4f, 2f, 0.8f);
                    Burst(at + Vector3.up, "mat_spark", wraithLilac, 60, 0.9f, 12f, 0.07f, shapeRadius: 0.6f, upward: true);
                    Flash(at + Vector3.up * 1.5f, wraithViolet, radius * 0.6f, 0.6f, 7f);
                    Shake(at, radius, 1.2f, 0.4f);
                    return null;
                }
                case "loop":
                {
                    // each lifted enemy: smoke pouring off it, her eye over it, a ring under it and a violet beam from her hand
                    // (to: where she stood) to it (radius = how long)
                    var root = Root(at, radius, follow);
                    Particles(root, "mat_smoke", wraithSmoke, rate: 45, life: 1.1f, speed: 1.2f, size: 1.8f, shapeRadius: 1f, world: true);
                    Particles(root, "mat_smoke", wraithSmokeDark, rate: 20, life: 1.3f, speed: 1.5f, size: 2.2f, shapeRadius: 0.8f, world: true, down: true);
                    Particles(root, "mat_glow", wraithViolet, rate: 30, life: 0.6f, speed: 0.6f, size: 1.6f, shapeRadius: 0.9f, world: true);
                    Particles(root, "mat_glow", wraithMagenta, rate: 12, life: 0.3f, speed: 0.3f, size: 2.2f, shapeRadius: 0.4f);
                    Particles(root, "mat_spark", wraithLilac, rate: 25, life: 0.5f, speed: 1.5f, size: 0.05f, shapeRadius: 0.8f, world: true);
                    var eye = new GameObject("eye"); eye.transform.SetParent(root.transform, false); eye.transform.localPosition = Vector3.up * 2.2f;
                    SymbolQuad(eye, "wraith_eye_tex", new Color(0.9f, 0.65f, 1f, 0.95f), 0.9f, true);
                    var under = new GameObject("under"); under.transform.SetParent(root.transform, false); under.transform.localPosition = Vector3.down * 1f;
                    under.AddComponent<Spin>().DegPerSec = 90f;
                    SymbolQuad(under, "wraith_ring_tex", new Color(0.7f, 0.4f, 1f, 0.7f), 3f, false);
                    if (to.HasValue)
                    {
                        var beam = Line(root.transform, wraithMagenta, 0.18f, Vector3.zero, to.Value - at);
                        beam.useWorldSpace = true;
                        root.AddComponent<TkBeam>().Init(beam, to.Value);
                    }
                    Light(root, wraithViolet, 4f, 1.5f, Vector3.up * 0.5f).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "wave":
                {
                    // thrown: a violet smoke trail behind it, suits shedding off it (radius = the flight's length)
                    var root = Root(at, radius, follow);
                    Trail(root, wraithViolet, 0.5f, 0.35f);
                    Particles(root, "mat_smoke", wraithSmoke, rate: 70, life: 0.9f, speed: 0.3f, size: 1.6f, shapeRadius: 0.5f, world: true);
                    Particles(root, "mat_glow", wraithViolet, rate: 40, life: 0.4f, speed: 0.2f, size: 1.2f, shapeRadius: 0.4f, world: true);
                    Particles(root, "mat_spark", wraithMagenta, rate: 30, life: 0.3f, speed: 1f, size: 0.05f, shapeRadius: 0.4f, world: true);
                    Symbols(root, "wraith_suits_tex", 3, wraithLilac, rate: 12, life: 0.6f, speed: 0.5f, size: 0.35f, shapeRadius: 0.5f, world: true);
                    return root;
                }
                case "trigger":
                {
                    // the throw: a flick of violet light from her hand along the way they fly
                    var end = to ?? at + Vector3.forward * 20f;
                    var root = Root(at, 0.4f, null);
                    var lr = Line(root.transform, wraithMagenta, 0.5f, Vector3.zero, end - at);
                    lr.widthCurve = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0f));
                    root.AddComponent<FadeLine>().Life = 0.35f;
                    PurpleSmoke(at, 1f, 10, 0.8f, 3f, 0.3f);
                    SuitScatter(at, 8, 5f, 0.35f, 0.3f);
                    return null;
                }
                case "hit":
                case "slam":
                {
                    // where it lands: a blast of purple smoke, sparks and suits; harder on a wall
                    float k = moment == "slam" ? 1.6f : 1f;
                    PurpleSmoke(at + Vector3.up * 0.5f, 1.8f * k, (int)(16 * k), 1.2f, 3f * k, radius * 0.4f);
                    Burst(at, "mat_smoke", wraithSmoke, (int)(10 * k), 0.9f, radius * 2f, 1.2f * k, shapeRadius: 0.4f, flat: true);
                    Burst(at, "mat_spark", wraithLilac, (int)(30 * k), 0.7f, 9f * k, 0.07f, shapeRadius: 0.3f, upward: true, gravity: 1.5f);
                    SuitScatter(at + Vector3.up * 0.5f, (int)(8 * k), 4f * k, 0.4f, 0.4f);
                    Ring(at + Vector3.up * 0.2f, wraithViolet, 0.4f, radius * 2f * k, 0.4f, 0.6f);
                    Flash(at + Vector3.up, wraithViolet, 8f * k, 0.35f, 6f * k);
                    Shake(at, 25f, 1.2f * k, 0.3f);
                    return null;
                }
            }
            return null;
        }

        /// <summary>Telekinesis' beam: from where she cast (fixed) to the lifted enemy as it rises.</summary>
        sealed class TkBeam : MonoBehaviour
        {
            LineRenderer lr; Vector3 from;
            public void Init(LineRenderer l, Vector3 f) { lr = l; from = f; }
            void LateUpdate()
            {
                if (!lr || !lr.enabled) return;
                lr.SetPosition(0, transform.position);
                lr.SetPosition(1, from);
            }
        }
    }
}
