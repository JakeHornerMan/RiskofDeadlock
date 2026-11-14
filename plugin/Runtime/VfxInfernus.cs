using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Infernus' effects after the 2026-11 rebuild (docs/VfxRebuild/infernusVfxRebuild.md). His flames were already
    /// RoR2's fire in Deadlock's oranges (the creator's choice), so they stay; these are the layers Deadlock's recipes have
    /// and ours lacked: glowing flecks for embers instead of plain sparks, scorched and glowing ground under every fire, a
    /// ragged red-orange muzzle flash, Napalm as black-brown tar, and the bomb's pale shock ring, tan dust and hot streaks.
    /// The fire rings and ground fires also draw far fewer flame particles than before (they were his dearest effects).</summary>
    public static partial class Vfx
    {
        // particles/abilities/inferno: embers #FFA23F > #FF711E > #4F0900 (fleks3), fire #FFB986 / #FF8E68 / #E02C00, afterburn ring
        // #DC143C / #8B0000, napalm tar #0F0906 / #5E2700 with flash #FF7525, bomb shock #FBD9B4 > #FFBB5C > #C05A16, dust #D7B38A,
        // scorch #620F0F; weapon_fx/inferno muzzle #FF4F23 / #FF8B24
        static readonly Color infEmber = Hex("FFA23F"), infEmberDeep = Hex("4F0900"), infRed = Hex("DC143C"), infTar = Hex("0F0906", 0.9f), infTarBrown = Hex("5E2700", 0.8f);

        /// <summary>Embers the way Deadlock draws them: small glowing flecks tumbling upward and cooling to dark red.</summary>
        static void InfernusEmbers(GameObject root, float rate, float radius, float speed)
        {
            Emit(root, new P { Mat = Glow("fx_fleks", infEmber), Rate = rate, LifeMin = 0.5f, LifeMax = 1.1f, SpeedMin = speed * 0.4f, SpeedMax = speed, SizeMin = 0.07f, SizeMax = 0.16f,
                               A = infEmber, B = infEmberDeep, Shape = Shape.Up, Angle = 35f, Radius = radius, Gravity = -0.35f, Noise = 0.6f, TilesX = 8, TilesY = 8,
                               SpinMin = -5f, SpinMax = 5f, Max = Mathf.RoundToInt(rate * 1.1f) + 4 }, "embers");
        }

        /// <summary>One Flame Gun shot (inferno_muzzle_flash): a ragged red-orange flash and a ball of hot gas; smoke on some.</summary>
        static void InfernusMuzzle(Vector3 at)
        {
            if (!Every("infernus_mz", 0.05f)) return;
            QuadAt(at, Glow("fx_muzzle", Hex("FF4F23")), 0.5f, 1.25f, 0.08f, Face.Camera, 0f, 0f, 0.25f, Random.Range(0f, 360f));
            QuadAt(at, Glow("fx_glow", Hex("FF8B24")), 0.4f, 0.9f, 0.1f, Face.Camera, 0f, 0f, 0.2f);
            if (Rich && Every("infernus_mz_smoke", 0.2f))
                Puff(at, new P { Mat = Soft("fx_cloud", Color.white), Count = 2, LifeMin = 0.5f, LifeMax = 0.9f, SpeedMin = 0.4f, SpeedMax = 1.2f, SizeMin = 0.35f, SizeMax = 0.6f,
                                 A = Hex("4E4B47", 0.4f), B = Hex("2E2D2C", 0.15f), Size0 = 0.5f, Size1 = 1.6f, Velocity = Vector3.up * 0.8f, Max = 3 });
        }

        /// <summary>Afterburn catching (inferno_afterburn_explode): a crimson ring, a hot flare and a shower of embers.</summary>
        static void InfernusIgnite(Vector3 at, float size)
        {
            QuadAt(at, Glow("fx_glow", fireYellow), size, size * 3.4f, 0.25f, Face.Camera, 0f, 0f, 0.2f);
            QuadAt(at, Glow("fx_ring_burst", infRed), size * 0.5f, size * 4.5f, 0.35f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
            Puff(at, new P { Mat = Glow("fx_fleks", infEmber), Count = 20, LifeMin = 0.5f, LifeMax = 1.6f, SpeedMin = 2f, SpeedMax = 7f, SizeMin = 0.08f, SizeMax = 0.18f,
                             A = Hex("FF885C"), B = Hex("7E2E03"), Gravity = 0.6f, Drag = 1f, TilesX = 8, TilesY = 8, SpinMin = -6f, SpinMax = 6f, Max = 22 });
            Glint(at, fireOrange, 10f, 0.35f, 6f);
        }

        /// <summary>Napalm bursting (inferno_napalm_explode): black-brown tar flung out low, an orange flash, a tar stain.</summary>
        static void InfernusNapalmSplash(Vector3 at, float radius)
        {
            var g = Ground(at);
            // one stain per burst of splashes, not one per splash: they land on top of each other and the stacked quads cost frames
            if (Every("infernus_tar", 0.3f)) QuadAt(g, Soft("fx_puff", infTar), radius * 0.6f, radius * 1.9f, 3.2f, Face.Ground, 0f, 0.03f, 0.6f, Random.Range(0f, 360f));
            QuadAt(at + Vector3.up * 0.5f, Glow("fx_flare", Hex("FF7525")), radius * 0.4f, radius * 1.6f, 0.18f, Face.Camera, 0f, 0f, 0.2f);
            Puff(at + Vector3.up * 0.3f, new P { Mat = Soft("fx_puff", Color.white), Count = 12, LifeMin = 0.5f, LifeMax = 0.9f, SpeedMin = radius * 0.6f, SpeedMax = radius * 1.6f,
                                                 SizeMin = 0.8f, SizeMax = 1.5f, A = infTarBrown, B = infTar, Shape = Shape.Up, Angle = 70f, Radius = radius * 0.2f, Gravity = 1.6f,
                                                 Drag = 1.5f, Size0 = 0.6f, Size1 = 1.3f, FadeOutFrom = 0.6f, Max = 14 });
            Puff(at + Vector3.up * 0.3f, new P { Mat = Glow("fx_glow", infRed), Count = 14, LifeMin = 0.5f, LifeMax = 1f, SpeedMin = 2f, SpeedMax = radius * 1.4f, SizeMin = 0.08f, SizeMax = 0.2f,
                                                 A = Hex("E24421"), B = Hex("760000"), Shape = Shape.Up, Angle = 70f, Gravity = 1.2f, Max = 16 });
        }

        /// <summary>What every burning patch of ground has under its flames: a scorch, a glow that breathes, and embers.</summary>
        static void InfernusGroundLayers(GameObject root, float radius, float life)
        {
            // the patches of a dash overlap: a scorch and a glow under every third one covers the same ground at a third of the cost
            if (Every("infernus_scorch", 0.2f))
            {
                Quad(root, Soft("fx_puff", Hex("160806", 0.75f)), radius * 1.8f, radius * 2.6f, life + 1f, Face.Ground, 0f, 0.05f, 0.7f, default, Random.Range(0f, 360f));
                Quad(root, Glow("fx_glow_soft", Hex("EA6A15", 0.7f)), radius * 2.2f, radius * 2.6f, life + 0.4f, Face.Ground, 12f, 0.1f, 0.75f, Vector3.up * 0.05f);
            }
            InfernusEmbers(root, N(2f * radius), radius, 2.2f);
        }

        /// <summary>The ring racing out with a fire ring: two flat rings on the ground and a glow.</summary>
        static void InfernusRingLayers(Vector3 at, float radius, float life)
        {
            var g = Ground(at);
            QuadAt(g, Glow("fx_ring", fireOrange), 1f, radius * 2.1f, life + 0.15f, Face.Ground, 0f, 0.05f, 0.35f);
            Glint(at + Vector3.up, fireOrange, radius * 1.5f, life, 4f);
        }

        /// <summary>Concussive Combustion going off (inferno_fire_bomb_explode): a pale shock ring, the flash, hot streaks and embers
        /// flung out, tan dust rolling away, and a dark red scorch left on the ground.</summary>
        static void InfernusBombBlast(Vector3 at, float radius)
        {
            var g = Ground(at);
            float d = radius * 2f;
            QuadAt(g, Glow("fx_ring_thick", Hex("FFBB5C")), 2f, d * 1.05f, 0.5f, Face.Ground, 0f, 0.03f, 0.3f);
            QuadAt(g, Glow("fx_ring", Hex("FBD9B4")), 1f, d * 1.2f, 0.42f, Face.Ground, 0f, 0.03f, 0.3f, 0f, 0.04f);
            QuadAt(g, Soft("fx_puff", Hex("3A0A0A", 0.8f)), d * 0.5f, d * 0.75f, 4f, Face.Ground, 0f, 0.03f, 0.6f, Random.Range(0f, 360f));
            float fl = Mathf.Min(radius, 9f);
            QuadAt(at + Vector3.up * 1.5f, Glow("fx_glow", new Color(fireYellow.r, fireYellow.g, fireYellow.b, 0.7f)), fl * 0.5f, fl * 1.6f, 0.3f, Face.Camera, 0f, 0f, 0.2f);
            QuadAt(at + Vector3.up * 1.5f, Glow("fx_star", Hex("FFE8C3", 0.8f)), fl * 0.4f, fl * 1.1f, 0.2f, Face.Camera, 0f, 0f, 0.2f);
            Puff(at + Vector3.up * 0.5f, new P { Mat = Mat("mat_spark", fireYellow), Count = 22, LifeMin = 0.15f, LifeMax = 0.45f, SpeedMin = radius * 1.2f, SpeedMax = radius * 2.6f,
                                                 SizeMin = 0.12f, SizeMax = 0.22f, A = Hex("FFDDB7"), B = Hex("C0390C"), Shape = Shape.Up, Angle = 80f, Stretch = 4f, Max = 24 });
            Puff(at + Vector3.up * 0.5f, new P { Mat = Glow("fx_fleks", infEmber), Count = 30, LifeMin = 1.2f, LifeMax = 2.6f, SpeedMin = 4f, SpeedMax = radius * 0.9f, SizeMin = 0.1f, SizeMax = 0.24f,
                                                 A = Hex("FFB76B"), B = Hex("763500"), Shape = Shape.Up, Angle = 65f, Gravity = 0.9f, Drag = 0.8f, TilesX = 8, TilesY = 8,
                                                 SpinMin = -6f, SpinMax = 6f, Max = 32 });
            Puff(at + Vector3.up * 0.4f, new P { Mat = Soft("fx_cloud", Color.white), Count = 16, LifeMin = 1.4f, LifeMax = 2.6f, SpeedMin = radius * 0.35f, SpeedMax = radius * 0.9f,
                                                 SizeMin = 2.6f, SizeMax = 4.2f, A = Hex("D7B38A", 0.7f), B = Hex("3C3C3C", 0.3f), Shape = Shape.Disc, Radius = 2f, Drag = 2.2f,
                                                 Size0 = 0.45f, Size1 = 1.5f, FadeOutFrom = 0.35f, Velocity = Vector3.up * 1.2f, SpinMin = -0.4f, SpinMax = 0.4f, Max = 18 });
        }
    }
}
