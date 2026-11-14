using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>The 2026-11 effect rebuild's building blocks (docs/VfxRebuild/README.md): Deadlock's own particle textures
    /// (effects rows of kind texture, read from the player's install like the icons) drawn additively or alpha-blended,
    /// particle systems described by one <see cref="P"/> spec (colour and size over life, flipbook frames, drag, noise),
    /// single quads that grow and fade (shockwave rings, flares, ground marks), textured ribbons and beams, and a throttle
    /// so repeated effects never pile up. Every effect still falls back to RoR2's materials when a texture is missing
    /// (an older cache): the look gets plainer, nothing breaks.</summary>
    public static partial class Vfx
    {
        // ------------------------------------------------------------------ Deadlock textures as materials

        static readonly Dictionary<string, Texture2D> fxTex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> fxMats = new Dictionary<string, Material>();
        /// <summary>The colour of each additive material: RoR2's glow shader takes its colour from the vertices, not from the
        /// material's tint (a quad drawn with white vertices came out white whatever the tint), so quads paint it into their mesh.</summary>
        static readonly Dictionary<Material, Color> glowColor = new Dictionary<Material, Color>();

        /// <summary>A Deadlock particle texture prepared once: its brightness times its alpha becomes the alpha
        /// (additive textures have no alpha of their own and would draw as black squares, doc 02 §12). lum: grey = that
        /// same value (for the additive material); else white (for alpha blending, tinted by the material).
        /// rect: a cut-out of an atlas (x0, y0, x1, y1 in 0..1, y from the top), null = the whole texture.</summary>
        static Texture2D FxTex(string texId, bool lum, Rect? rect = null)
        {
            string key = texId + (lum ? "|l" : "|a") + (rect.HasValue ? "|" + rect.Value : "");
            if (fxTex.TryGetValue(key, out var tex)) return tex;
            if (texId == "fx_beam") return fxTex[key] = Strip(lum);
            var row = Sheet.Effect(texId);
            var src = row != null && row.Kind == "texture" ? DeadlockCache.Image(row.Addressable) : null;
            tex = null;
            if (src)
            {
                try
                {
                    int x0 = 0, y0 = 0, w = src.width, h = src.height;
                    if (rect.HasValue)
                    {
                        var r = rect.Value;
                        x0 = Mathf.Clamp(Mathf.RoundToInt(r.xMin * src.width), 0, src.width - 1);
                        w = Mathf.Clamp(Mathf.RoundToInt(r.width * src.width), 1, src.width - x0);
                        h = Mathf.Clamp(Mathf.RoundToInt(r.height * src.height), 1, src.height);
                        y0 = Mathf.Clamp(src.height - Mathf.RoundToInt(r.yMin * src.height) - h, 0, src.height - h);
                    }
                    var px = src.GetPixels32();
                    var o = new Color32[w * h];
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            var c = px[(y0 + y) * src.width + x0 + x];
                            byte v = (byte)(Mathf.Max(c.r, Mathf.Max(c.g, c.b)) * c.a / 255);
                            // additive: kept under the white-hot top of RoR2's glow ramp, so a solid white shape takes the tint instead of burning out
                            byte g = (byte)(v * 0.6f);
                            o[y * w + x] = lum ? new Color32(g, g, g, g) : new Color32(255, 255, 255, v);
                        }
                    tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = src.name + (lum ? "_lum" : "_alpha") };
                    tex.SetPixels32(o); tex.Apply(true, true);
                }
                catch (System.Exception e) { Log.Warn($"fx texture {texId}: {e.Message}"); tex = null; }
            }
            fxTex[key] = tex;
            return tex;
        }

        /// <summary>The strip ribbons, beams and trails are drawn with: bright along its middle, fading to both edges, the same
        /// all along its length. Made in code: Source's rope textures (beam_generic_2) run the other way from Unity's line and
        /// trail renderers, so Deadlock's own strip drew as a flat hard-edged rectangle (seen in game, 2026-11-12).</summary>
        static Texture2D Strip(bool lum)
        {
            const int w = 4, h = 64;
            var o = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = Mathf.Pow(Mathf.Sin(Mathf.PI * y / (h - 1f)), 1.6f);
                byte a = (byte)(v * 255f), g = (byte)(v * 255f * 0.6f);
                for (int x = 0; x < w; x++) o[y * w + x] = lum ? new Color32(g, g, g, g) : new Color32(255, 255, 255, a);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = lum ? "dlrStrip_lum" : "dlrStrip_alpha" };
            tex.SetPixels32(o); tex.Apply(false, true);
            return tex;
        }

        /// <summary>Sparks in any colour: the round glow, stretched along its flight by the particle system. (RoR2's spark material
        /// has its own yellow, so blue or violet sparks drawn with it came out yellow.)</summary>
        static Material Spark(Color c) => Glow("fx_glow", c);

        /// <summary>Additive material with a Deadlock texture's shape (RoR2's soft-glow material with its texture swapped), tinted.
        /// Without the texture: RoR2's own glow, so the effect still shows.</summary>
        static Material Glow(string texId, Color c, Rect? rect = null, bool wrap = false)
        {
            string key = "g|" + texId + "|" + ColorUtility.ToHtmlStringRGBA(c) + (rect.HasValue ? "|" + rect.Value : "") + (wrap ? "|w" : "");
            if (fxMats.TryGetValue(key, out var m) && m) return m;
            var tex = FxTex(texId, true, rect);
            m = new Material(Mat("mat_glow", c));
            if (tex)
            {
                if (wrap) { tex = Object.Instantiate(tex); tex.wrapMode = TextureWrapMode.Repeat; }
                m.SetTexture("_MainTex", tex);
            }
            glowColor[m] = c;
            fxMats[key] = m;
            return m;
        }

        /// <summary>Alpha-blended material with a Deadlock texture's shape (smoke, clouds, solid marks that must read on bright
        /// ground), tinted. Without the texture: RoR2's smoke.</summary>
        static Material Soft(string texId, Color c, Rect? rect = null)
        {
            string key = "s|" + texId + "|" + ColorUtility.ToHtmlStringRGBA(c) + (rect.HasValue ? "|" + rect.Value : "");
            if (fxMats.TryGetValue(key, out var m) && m) return m;
            var tex = FxTex(texId, false, rect);
            m = tex ? new Material(SpriteShader()) { mainTexture = tex, color = c, renderQueue = 3050 } : Mat("mat_smoke", c);
            fxMats[key] = m;
            return m;
        }

        /// <summary>A Deadlock texture drawn as it is, colours and all (the pages of Abrams' book), alpha-blended.</summary>
        static Material Raw(string texId, Color c)
        {
            string key = "r|" + texId + "|" + ColorUtility.ToHtmlStringRGBA(c);
            if (fxMats.TryGetValue(key, out var m) && m) return m;
            var row = Sheet.Effect(texId);
            var tex = row != null && row.Kind == "texture" ? DeadlockCache.Image(row.Addressable) : null;
            m = tex ? new Material(SpriteShader()) { mainTexture = tex, color = c, renderQueue = 3050 } : Mat("mat_smoke", c);
            fxMats[key] = m;
            return m;
        }

        /// <summary>Loading screen: every shared texture and the listed ones, prepared before a fight needs them.</summary>
        static void WarmKit()
        {
            foreach (var e in EffectRow.All)
                if (e.Kind == "texture" && (e.Id.StartsWith("fx_") || e.Id == "haze_dagger_tex" || e.Id == "abrams_paper_tex")) { FxTex(e.Id, true); FxTex(e.Id, false); }
            // the cut-outs too: preparing a texture on a move's first use was a visible hitch
            FxTex("fx_arc", true, arcA); FxTex("fx_arc", true, arcB);
        }

        // ------------------------------------------------------------------ throttle and budget

        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        /// <summary>True at most once per interval for a key: repeated hits and shots show one effect, not a pile.</summary>
        static bool Every(string key, float interval)
        {
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(key, out var t) && now - t < interval) return false;
            lastPlayed[key] = now;
            return true;
        }

        /// <summary>How many effect roots are alive (kept by <see cref="VfxLife"/>).</summary>
        internal static int Alive;
        /// <summary>Effects quality: 1 high, 0.6 medium, 0.35 low (BepInEx config "Effects quality"); scales particle counts
        /// and drops optional layers (<see cref="Rich"/>).</summary>
        public static float Quality = 1f;
        /// <summary>Whether an optional layer (extra debris, a second glow) should be drawn now: not on low quality, and not
        /// while a great many effects are already alive (four players' ults at once).</summary>
        static bool Rich => Quality >= 0.5f && Alive < 70;
        static int N(int count) => Mathf.Max(1, Mathf.RoundToInt(count * Quality * (Alive > 90 ? 0.5f : 1f)));
        static float N(float rate) => rate * Quality * (Alive > 90 ? 0.5f : 1f);

        // ------------------------------------------------------------------ particles

        internal enum Shape { Sphere, Ring, Disc, Cone, Up, Point, Box }

        /// <summary>One particle system, described. Colours fade from A to B over a particle's life.</summary>
        internal sealed class P
        {
            public Material Mat;
            public float Rate; public int Count; public float Delay;
            public float LifeMin = 0.4f, LifeMax = 0.6f;
            public float SpeedMin, SpeedMax;
            public float SizeMin = 0.4f, SizeMax = 0.6f;
            public Color A = Color.white, B = Color.white;
            /// <summary>Alpha over life: in = fraction of life to fade in, hold = where the fade-out starts.</summary>
            public float FadeIn = 0.1f, FadeOutFrom = 0.5f, Alpha = 1f;
            /// <summary>Size over life, as multiples of the start size.</summary>
            public float Size0 = 1f, Size1 = 1f; public float SizePeakAt = -1f, SizePeak = 1f;
            public Shape Shape = Shape.Sphere; public float Radius = 0.1f, Angle = 25f; public Vector3 Box = Vector3.one;
            public bool World = true; public float Gravity; public float Drag; public float Noise; public float NoiseFreq = 0.6f;
            public float SpinMin, SpinMax; public bool RandomRoll = true;
            public float Stretch; public bool Flat; public bool FaceVelocity;
            public int TilesX = 1, TilesY = 1; public bool Animate; public int FrameMin, FrameMax = -1;
            public int Max = 200; public float Duration = 1f; public bool Loop = true;
            public Vector3 Velocity; public float Orbit; public float Radial;
        }

        internal static ParticleSystem Emit(GameObject parent, P p, string name = "ps")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            bool burst = p.Count > 0;
            main.duration = burst && p.Rate <= 0f ? Mathf.Max(0.1f, p.Delay + 0.1f) : p.Duration;
            main.loop = p.Rate > 0f && p.Loop;
            main.startDelay = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(p.LifeMin, p.LifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(p.SpeedMin, p.SpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(p.SizeMin, p.SizeMax);
            main.startRotation = p.RandomRoll ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f) : new ParticleSystem.MinMaxCurve(0f);
            main.startColor = Color.white;
            main.gravityModifier = p.Gravity;
            main.maxParticles = Mathf.Max(4, Mathf.RoundToInt(p.Max * Mathf.Max(0.35f, Quality)));
            main.simulationSpace = p.World ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var em = ps.emission;
            em.rateOverTime = N(p.Rate);
            if (burst) em.SetBursts(new[] { new ParticleSystem.Burst(p.Delay, (short)N(p.Count)) });
            var shape = ps.shape;
            shape.enabled = p.Shape != Shape.Point;
            switch (p.Shape)
            {
                case Shape.Ring: shape.shapeType = ParticleSystemShapeType.Circle; shape.rotation = new Vector3(90f, 0, 0); shape.radiusThickness = 0f; break;
                case Shape.Disc: shape.shapeType = ParticleSystemShapeType.Circle; shape.rotation = new Vector3(90f, 0, 0); break;
                case Shape.Cone: shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = p.Angle; break;
                case Shape.Up: shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = p.Angle; shape.rotation = new Vector3(-90f, 0, 0); break;
                case Shape.Box: shape.shapeType = ParticleSystemShapeType.Box; shape.scale = p.Box; break;
                default: shape.shapeType = ParticleSystemShapeType.Sphere; break;
            }
            shape.radius = Mathf.Max(0.01f, p.Radius);
            if (p.Velocity != Vector3.zero || p.Orbit != 0f || p.Radial != 0f)
            {
                var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(p.Velocity.x); vel.y = new ParticleSystem.MinMaxCurve(p.Velocity.y); vel.z = new ParticleSystem.MinMaxCurve(p.Velocity.z);
                if (p.Orbit != 0f) vel.orbitalY = new ParticleSystem.MinMaxCurve(p.Orbit);
                if (p.Radial != 0f) vel.radial = new ParticleSystem.MinMaxCurve(p.Radial);
            }
            if (p.Drag > 0f)
            {
                var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.drag = new ParticleSystem.MinMaxCurve(p.Drag); lim.multiplyDragByParticleSize = false; lim.multiplyDragByParticleVelocity = false;
                lim.limit = new ParticleSystem.MinMaxCurve(1000f); lim.dampen = 0f;
            }
            if (p.Noise > 0f)
            {
                var nz = ps.noise; nz.enabled = true; nz.strength = new ParticleSystem.MinMaxCurve(p.Noise); nz.frequency = p.NoiseFreq; nz.scrollSpeed = new ParticleSystem.MinMaxCurve(0.4f);
                nz.quality = ParticleSystemNoiseQuality.Low;
            }
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(p.A, 0f), new GradientColorKey(p.B, 1f) },
                      new[] { new GradientAlphaKey(p.FadeIn <= 0f ? p.Alpha * p.A.a : 0f, 0f), new GradientAlphaKey(p.Alpha * p.A.a, Mathf.Clamp(p.FadeIn, 0.001f, 0.9f)),
                              new GradientAlphaKey(p.Alpha * Mathf.Lerp(p.A.a, p.B.a, p.FadeOutFrom), Mathf.Clamp(p.FadeOutFrom, p.FadeIn + 0.001f, 0.99f)), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            if (p.Size0 != 1f || p.Size1 != 1f || p.SizePeakAt >= 0f)
            {
                var sol = ps.sizeOverLifetime; sol.enabled = true;
                var curve = p.SizePeakAt >= 0f
                    ? new AnimationCurve(new Keyframe(0f, p.Size0), new Keyframe(p.SizePeakAt, p.SizePeak), new Keyframe(1f, p.Size1))
                    : new AnimationCurve(new Keyframe(0f, p.Size0, 0f, (p.Size1 - p.Size0) * 2f), new Keyframe(1f, p.Size1, 0f, 0f));
                sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
            }
            if (p.SpinMin != 0f || p.SpinMax != 0f)
            {
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(p.SpinMin, p.SpinMax);
            }
            if (p.TilesX > 1 || p.TilesY > 1)
            {
                var sheet = ps.textureSheetAnimation; sheet.enabled = true;
                sheet.numTilesX = p.TilesX; sheet.numTilesY = p.TilesY;
                sheet.animation = ParticleSystemAnimationType.WholeSheet;
                int frames = p.TilesX * p.TilesY, hi = p.FrameMax < 0 ? frames - 1 : p.FrameMax;
                if (p.Animate) sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, p.FrameMin / (float)frames, 1f, (hi + 0.999f) / frames));
                else { sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f); sheet.startFrame = new ParticleSystem.MinMaxCurve(p.FrameMin / (float)frames, (hi + 0.999f) / frames); }
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = p.Mat ? p.Mat : Mat("mat_glow", p.A);
            if (p.Stretch > 0f) { r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = p.Stretch; r.velocityScale = 0f; }
            else if (p.Flat) r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            else if (p.FaceVelocity) r.alignment = ParticleSystemRenderSpace.Velocity;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>A one-off burst at a point that cleans itself up.</summary>
        internal static void Puff(Vector3 at, P p, Quaternion? rot = null)
        {
            var root = Root(at, p.LifeMax + p.Delay + 0.3f, null);
            if (rot.HasValue) root.transform.rotation = rot.Value;
            Emit(root, p);
        }

        // ------------------------------------------------------------------ quads: rings, flares, ground marks

        static Mesh quadMesh;
        static Mesh QuadMesh()
        {
            if (quadMesh) return quadMesh;
            quadMesh = new Mesh { name = "dlrFxQuad" };
            quadMesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            quadMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };
            quadMesh.RecalculateBounds();
            return quadMesh;
        }

        internal enum Face { Camera, Ground, Fixed }

        /// <summary>One textured quad that grows from size0 to size1, fades and goes: a shockwave ring, a flare, a mark on the
        /// ground (laid on the terrain's slope). Returns it (its parent root cleans up).</summary>
        internal static GameObject Quad(GameObject parent, Material mat, float size0, float size1, float life, Face face = Face.Camera,
            float spin = 0f, float fadeIn = 0.08f, float holdUntil = 0.3f, Vector3 offset = default, float roll = 0f, float delay = 0f, bool loop = false)
        {
            var go = new GameObject("quad");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = offset;
            go.AddComponent<MeshFilter>().mesh = Object.Instantiate(QuadMesh());
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            var q = go.AddComponent<QuadFx>();
            q.Size0 = size0; q.Size1 = size1; q.Life = life; q.FaceMode = face; q.Spin = spin; q.FadeIn = fadeIn; q.Hold = holdUntil; q.Roll = roll; q.Delay = delay; q.Loop = loop;
            q.Begin();
            return go;
        }

        /// <summary>A quad on its own root at a point (ground marks are dropped onto the terrain under the point).</summary>
        internal static void QuadAt(Vector3 at, Material mat, float size0, float size1, float life, Face face = Face.Camera, float spin = 0f,
            float fadeIn = 0.08f, float holdUntil = 0.3f, float roll = 0f, float delay = 0f)
        {
            var root = Root(at, life + delay + 0.1f, null);
            if (face == Face.Ground && Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out var hit, 8f, RoR2.LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                root.transform.position = hit.point + hit.normal * 0.06f;
                root.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            Quad(root, mat, size0, size1, life, face, spin, fadeIn, holdUntil, default, roll, delay);
        }

        sealed class QuadFx : MonoBehaviour
        {
            public float Size0, Size1, Life, Spin, FadeIn, Hold, Roll, Delay; public Face FaceMode; public bool Loop;
            float age; MeshRenderer mr; MaterialPropertyBlock block; Color baseColor, vertex = Color.white; bool hasColor, additive; float angle; Mesh mesh;
            readonly Color[] cols = new Color[4];
            static readonly int ColorId = Shader.PropertyToID("_Color");
            void OnDestroy() { if (mesh) Destroy(mesh); }
            /// <summary>Stop looping and fade out over a moment (Vfx.Stop).</summary>
            public void FadeOut(float over)
            {
                if (!Loop && Life > 0f && age - Delay > Life) return;
                Size0 = Size1 = transform.localScale.x; Loop = false; Life = Mathf.Max(0.05f, over); age = Delay; FadeIn = 0f; Hold = 0f;
            }
            public void Begin()
            {
                mr = GetComponent<MeshRenderer>(); block = new MaterialPropertyBlock();
                var m = mr.sharedMaterial;
                mesh = GetComponent<MeshFilter>().mesh;
                additive = m && glowColor.TryGetValue(m, out vertex);
                if (!additive) vertex = Color.white;
                hasColor = !additive && m && m.HasProperty(ColorId);
                if (hasColor) baseColor = m.GetColor(ColorId);
                angle = Roll;
                Apply(0f);
            }
            void LateUpdate()
            {
                age += Time.deltaTime;
                float t = Life > 0f ? (age - Delay) / Life : 0f;
                if (t >= 1f) { if (Loop) { age = Delay; t = 0f; } else { mr.enabled = false; return; } }
                angle += Spin * Time.deltaTime;
                Apply(Mathf.Max(0f, t));
            }
            void Apply(float t)
            {
                mr.enabled = age >= Delay;
                // fast out of the gate, slowing (a shockwave)
                float k = 1f - (1f - t) * (1f - t);
                float s = Mathf.Lerp(Size0, Size1, k);
                transform.localScale = new Vector3(s, s, s);
                if (FaceMode == Face.Ground) transform.localRotation = Quaternion.Euler(90f, angle, 0f);
                else if (FaceMode == Face.Camera)
                {
                    var cam = Camera.main;
                    if (cam) transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, angle);
                }
                else transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                float a = Loop ? 1f : t < FadeIn ? t / Mathf.Max(0.0001f, FadeIn) : t < Hold ? 1f : 1f - (t - Hold) / Mathf.Max(0.0001f, 1f - Hold);
                if (additive)
                {
                    // additive: colour and fade both ride on the vertices
                    var c = new Color(vertex.r, vertex.g, vertex.b, vertex.a * a);
                    cols[0] = cols[1] = cols[2] = cols[3] = c;
                    mesh.colors = cols;
                }
                else if (hasColor)
                {
                    block.SetColor(ColorId, new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * a));
                    mr.SetPropertyBlock(block);
                }
            }
        }

        // ------------------------------------------------------------------ ribbons and beams

        /// <summary>A textured ribbon behind a moving object: bright at the head, fading to its tail colour.</summary>
        internal static TrailRenderer Ribbon(GameObject parent, Material mat, Color head, Color tail, float width, float time, float endWidth = 0f)
        {
            var go = new GameObject("ribbon"); go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<TrailRenderer>();
            t.sharedMaterial = mat;
            t.time = time; t.minVertexDistance = 0.15f; t.numCapVertices = 2;
            t.textureMode = LineTextureMode.Stretch;
            t.widthCurve = new AnimationCurve(new Keyframe(0, width), new Keyframe(1, endWidth));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 1f) },
                      new[] { new GradientAlphaKey(head.a, 0f), new GradientAlphaKey(tail.a * 0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            t.colorGradient = g;
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }

        /// <summary>A straight textured beam between two points that thins and fades.</summary>
        internal static void Beam(Vector3 a, Vector3 b, Material mat, Color c, float width, float life)
        {
            var root = Root(a, life + 0.05f, null);
            var lr = root.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.sharedMaterial = mat; lr.startColor = lr.endColor = c;
            lr.textureMode = LineTextureMode.Stretch; lr.numCapVertices = 2;
            lr.positionCount = 2; lr.SetPosition(0, a); lr.SetPosition(1, b);
            lr.widthMultiplier = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var f = root.AddComponent<BeamFade>(); f.Life = life; f.Width = width; f.C = c;
        }

        sealed class BeamFade : MonoBehaviour
        {
            public float Life, Width; public Color C; float age; LineRenderer lr;
            void Awake() { lr = GetComponent<LineRenderer>(); }
            void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / Mathf.Max(0.01f, Life));
                lr.widthMultiplier = Width * (1f - t * t);
                var c = new Color(C.r, C.g, C.b, C.a * (1f - t));
                lr.startColor = lr.endColor = c;
            }
        }

        /// <summary>A curved swoosh (a melee cut): a strip of quads along an arc in the root's local XZ plane, from startDeg
        /// to endDeg at a radius, wiped on over the sweep time and fading. The root is turned by the caller.</summary>
        internal static GameObject Swoosh(GameObject parent, Material mat, Color c, float radius, float width, float startDeg, float endDeg, float sweep, float life, float tilt = 0f)
        {
            var go = new GameObject("swoosh"); go.transform.SetParent(parent.transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.sharedMaterial = mat; lr.textureMode = LineTextureMode.Stretch; lr.alignment = LineAlignment.TransformZ;
            const int seg = 18;
            lr.positionCount = seg;
            for (int i = 0; i < seg; i++)
            {
                float a = Mathf.Lerp(startDeg, endDeg, i / (seg - 1f)) * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Sin(a) * radius, 0f, Mathf.Cos(a) * radius));
            }
            // the strip lies in XZ: its face must look along Y
            go.transform.localRotation = Quaternion.Euler(0f, 0f, tilt) * Quaternion.Euler(0f, 0f, 0f);
            lr.alignment = LineAlignment.View;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(0.55f, 1f), new Keyframe(1f, 0.1f));
            lr.widthMultiplier = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var s = go.AddComponent<SwooshFx>(); s.Sweep = sweep; s.Life = life; s.C = c; s.Width = width;
            return go;
        }

        sealed class SwooshFx : MonoBehaviour
        {
            public float Sweep, Life, Width; public Color C; float age; LineRenderer lr;
            void Awake() { lr = GetComponent<LineRenderer>(); Set(0f); }
            void Update() { age += Time.deltaTime; Set(age); }
            void Set(float t)
            {
                if (!lr) return;
                float head = Mathf.Clamp01(t / Mathf.Max(0.01f, Sweep));
                float fade = 1f - Mathf.Clamp01((t - Sweep * 0.5f) / Mathf.Max(0.01f, Life - Sweep * 0.5f));
                var g = new Gradient();
                // lit from the start of the arc up to the head; the tail dims first
                g.SetKeys(new[] { new GradientColorKey(C, 0f), new GradientColorKey(Color.Lerp(C, Color.white, 0.5f), Mathf.Clamp(head, 0.01f, 0.99f)), new GradientColorKey(C, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(fade * C.a, Mathf.Clamp(head * 0.9f, 0.01f, 0.97f)), new GradientAlphaKey(fade * C.a, Mathf.Clamp(head, 0.02f, 0.98f)), new GradientAlphaKey(0f, Mathf.Clamp(head + 0.02f, 0.03f, 1f)) });
                lr.colorGradient = g;
                if (fade <= 0f) lr.enabled = false;
            }
        }

        // ------------------------------------------------------------------ small helpers

        static Color Hex(string rgb, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(rgb.StartsWith("#") ? rgb : "#" + rgb, out var c);
            c.a = a;
            return c;
        }

        /// <summary>The ground under a point (a little above it), or the point itself when there is none close.</summary>
        static Vector3 Ground(Vector3 at, float up = 0.08f)
        {
            return Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var hit, 12f, RoR2.LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? hit.point + Vector3.up * up : at;
        }

        /// <summary>A light that flashes and fades, only when the effect budget allows (lights are dear in a crowd).</summary>
        static void Glint(Vector3 at, Color c, float range, float life, float intensity = 4f)
        {
            if (Alive < 60) Flash(at, c, range, life, intensity);
        }
    }
}
