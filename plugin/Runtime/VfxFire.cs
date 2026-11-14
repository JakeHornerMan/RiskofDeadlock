using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Fire drawn as flames (creator, 2026-11-14: Infernus' and Paige's fire "is represented through these circular type
    /// shape, can we please have something that more represents fire"). RoR2's fire materials draw soft round puffs; these are
    /// Deadlock's own stylised flame licks (fire_stylized_licks: three tongue shapes), cut out into one small strip and drawn as
    /// upright tongues that rise, swell and thin out, pale at the root and darkening as they climb. Every call that asked for
    /// one of RoR2's fire materials (Particles / Burst with mat_fire_large, mat_gas_fire, mat_fire_puff, mat_fire) is drawn
    /// this way when the texture is there; without it (an older cache) the old puffs still show.</summary>
    public static partial class Vfx
    {
        // the three licks in fire_stylized_licks.vtex (x, y from the top, width, height; fractions of the texture)
        static readonly Rect[] flameLicks = { new Rect(0.093f, 0.005f, 0.300f, 0.475f), new Rect(0.521f, 0.068f, 0.263f, 0.356f), new Rect(0.138f, 0.534f, 0.240f, 0.442f) };
        static Material flameMat; static bool flameTried;

        static bool IsFireMat(string mat) => mat == "mat_fire_large" || mat == "mat_gas_fire" || mat == "mat_fire_puff" || mat == "mat_fire";

        /// <summary>The flame strip material (three licks side by side), or null when Deadlock's texture is not in the cache.</summary>
        static Material FlameMat()
        {
            if (flameTried) return flameMat;
            flameTried = true;
            var row = Sheet.Effect("fx_fire");
            var src = row != null && row.Kind == "texture" ? DeadlockCache.Image(row.Addressable) : null;
            if (!src) return null;
            try
            {
                const int cw = 96, ch = 160;
                var px = src.GetPixels32();
                var o = new Color32[cw * 3 * ch];
                for (int i = 0; i < 3; i++)
                {
                    var r = flameLicks[i];
                    for (int y = 0; y < ch; y++)
                        for (int x = 0; x < cw; x++)
                        {
                            // 4 % margin inside each cell so neighbouring licks never bleed into each other
                            float u = (x / (cw - 1f) - 0.04f) / 0.92f, v = (y / (ch - 1f) - 0.04f) / 0.92f;
                            byte g = 0;
                            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                            {
                                int sx = Mathf.Clamp(Mathf.RoundToInt((r.xMin + u * r.width) * (src.width - 1)), 0, src.width - 1);
                                // the rect's y runs from the top of the image, Unity's rows from the bottom; v = 0 is the lick's foot
                                int sy = Mathf.Clamp(Mathf.RoundToInt((1f - (r.yMin + (1f - v) * r.height)) * (src.height - 1)), 0, src.height - 1);
                                var c = px[sy * src.width + sx];
                                float lum = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) / 255f;
                                // a filled tongue (the alpha is its shape), a little brighter on its rim like Deadlock's
                                g = (byte)(c.a * (0.6f + 0.4f * lum) * 0.95f);
                            }
                            o[y * cw * 3 + i * cw + x] = new Color32(g, g, g, g);
                        }
                }
                var tex = new Texture2D(cw * 3, ch, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "dlrFlameLicks" };
                tex.SetPixels32(o); tex.Apply(true, true);
                flameMat = new Material(Mat("mat_glow", Color.white));
                flameMat.SetTexture("_MainTex", tex);
                glowColor[flameMat] = Color.white;
            }
            catch (System.Exception e) { Log.Warn($"flame texture: {e.Message}"); flameMat = null; }
            return flameMat;
        }

        /// <summary>A flame emitter in place of one of RoR2's fire puff systems: the same arguments Particles takes.</summary>
        static ParticleSystem FlameEmit(GameObject parent, Color c, float rate, int count, float life, float speed, float size, float shapeRadius,
            bool world, bool flat, bool upward, bool ring)
        {
            var m = FlameMat();
            if (!m) return null;
            // pale and hot at the root, the colour asked for through its body, darker and redder as it thins
            var hot = Color.Lerp(c, Color.white, 0.2f);
            var cool = new Color(c.r * 0.7f, c.g * 0.3f, c.b * 0.25f, 1f);
            // seen in game 2026-11-14: sizes meant for round puffs made tongues taller than the hero, pale and see-through
            float tall = Mathf.Clamp(size * 0.7f, 0.45f, 1.7f);
            var ps = Emit(parent, new P { Mat = m, Rate = rate, Count = count, LifeMin = Mathf.Max(0.25f, life * 0.7f), LifeMax = Mathf.Max(0.35f, life),
                                        SpeedMin = speed * 0.5f, SpeedMax = speed, SizeMin = tall * 0.75f, SizeMax = tall * 1.25f, A = hot, B = cool,
                                        FadeIn = 0.12f, FadeOutFrom = 0.5f, Size0 = 0.5f, SizePeakAt = 0.3f, SizePeak = 1.1f, Size1 = 0.4f,
                                        Shape = ring ? Shape.Ring : flat ? Shape.Disc : upward ? Shape.Up : Shape.Sphere, Angle = 30f, Radius = Mathf.Max(0.05f, shapeRadius),
                                        World = world, RandomRoll = false, TilesX = 3, TilesY = 1, Velocity = Vector3.up * (0.7f + tall * 0.6f),
                                        Max = Mathf.Max(40, Mathf.CeilToInt(rate * Mathf.Max(0.35f, life) * 1.3f) + count + 4) }, "flames");
            // a caller may start it at rate 0 and turn it up later (a burning enemy): it must keep looping, as Particles' systems do
            var main = ps.main; main.loop = count == 0;
            return ps;
        }

        /// <summary>The floor under a point, however far above it the point is (an effect that belongs on the ground, started where a
        /// projectile hit an enemy in the air): the point itself when there is no ground within 60 m.</summary>
        static Vector3 Floor(Vector3 at)
        {
            return Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out var hit, 60f, RoR2.LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? hit.point : at;
        }
    }
}
