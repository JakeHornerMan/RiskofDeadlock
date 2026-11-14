using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Effects for the enemies (plugin/Enemies), built with the kit; docs/VfxRebuild/troopersVfxRebuild.md.
    /// The soul orb's extra layers follow Deadlock's own recipe for the orb a dead trooper leaves
    /// (particles/environment/spirit_orb_dropped_*): light rays streaming off it, a short green wisp behind it as it rises, a
    /// ring and glow where it appears, flares and rays when it is claimed, a scatter of teal motes when it ends.</summary>
    public static partial class Vfx
    {
        // spirit_orb_dropped_ambient_model #2DECA9 on #002C32, _rays #2DECA9, _trail #16C786, _start #2CEA9E, _endcap #30DBBE / #1BEE8F /
        // #025C41 with #9ED6F5, _claim #11D9C8, _claim_ray #1BE5B6
        static readonly Color orbTrail = Hex("16C786"), orbMote = Hex("30DBBE"), orbMoteDeep = Hex("025C41"), orbClaim = Hex("11D9C8"), orbPale = Hex("9ED6F5");

        /// <summary>On the orb while it floats: thin rays of light streaming slowly out of it (six at once as it appears, then seven a
        /// second) and a short green wisp trailing under it. Parented to the orb; goes when the orb goes.</summary>
        public static void SoulOrbAmbient(GameObject orb, Color c, float r)
        {
            if (!orb) return;
            var rays = new GameObject("Rays"); rays.transform.SetParent(orb.transform, false);
            Emit(rays, new P { Mat = Glow("fx_glow", c), Rate = 7, Count = 6, LifeMin = 1.2f, LifeMax = 2.2f, SpeedMin = r * 1.2f, SpeedMax = r * 2.6f, SizeMin = r * 0.5f, SizeMax = r * 0.85f,
                               A = new Color(c.r, c.g, c.b, 0.9f), B = new Color(c.r * 0.4f, c.g * 0.6f, c.b * 0.6f, 0f), Radius = r * 0.6f, Stretch = 7f, FadeIn = 0.25f, FadeOutFrom = 0.5f,
                               World = false, Max = 22 }, "rays");
            var wisp = new GameObject("Wisp"); wisp.transform.SetParent(orb.transform, false);
            Ribbon(wisp, Glow("fx_beam", orbTrail), new Color(orbTrail.r, orbTrail.g, orbTrail.b, 0.85f), new Color(orbTrail.r, orbTrail.g, orbTrail.b, 0f), r * 1.1f, 0.5f);
        }

        /// <summary>Where the orb appears: a ring snapping out and a soft glow (spirit_orb_dropped_ambient_start).</summary>
        public static void SoulOrbAppear(Vector3 at, Color c, float r)
        {
            QuadAt(at, Glow("fx_ring", c), r * 1.5f, r * 9f, 0.5f, Face.Camera, 0f, 0.05f, 0.3f);
            QuadAt(at, Glow("fx_glow", new Color(c.r, c.g, c.b, 0.7f)), r * 3f, r * 8f, 0.6f, Face.Camera, 0f, 0.1f, 0.3f);
        }

        /// <summary>Claimed (shot): bright flares, three rays of light thrown out of it and the scatter of motes
        /// (spirit_orb_dropped_claim, _claim_ray, _ambient_endcap).</summary>
        public static void SoulOrbClaim(Vector3 at, Color c, float r)
        {
            QuadAt(at, Glow("fx_star", orbClaim), r * 2f, r * 10f, 0.25f, Face.Camera, 0f, 0f, 0.3f, Random.Range(0f, 360f));
            Puff(at, new P { Mat = Glow("fx_flare", orbClaim), Count = 6, LifeMin = 0.4f, LifeMax = 1.2f, SpeedMin = r * 2f, SpeedMax = r * 9f, SizeMin = r * 1.6f, SizeMax = r * 3.2f,
                             A = orbClaim, B = new Color(orbClaim.r, orbClaim.g, orbClaim.b, 0f), Drag = 2f, FadeIn = 0.02f, FadeOutFrom = 0.3f, Max = 8 });
            for (int i = 0; i < 3; i++)
            {
                var dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * 0.6f + 0.2f;
                Beam(at, at + dir.normalized * r * Random.Range(9f, 14f), Glow("fx_beam", Hex("1BE5B6")), c, r * 0.9f, Random.Range(0.35f, 0.55f));
            }
            SoulOrbEnd(at, c, r, 16);
            Glint(at, c, r * 30f, 0.3f, 3f);
        }

        /// <summary>The orb ending (claimed, or faded unclaimed with fewer): teal motes scattering and dimming, a few pale blue among them.</summary>
        public static void SoulOrbEnd(Vector3 at, Color c, float r, int count = 8)
        {
            Puff(at, new P { Mat = Glow("fx_glow", orbMote), Count = count, LifeMin = 0.5f, LifeMax = 1f, SpeedMin = r * 3f, SpeedMax = r * 12f, SizeMin = r * 0.3f, SizeMax = r * 1f,
                             A = orbMote, B = orbMoteDeep, Drag = 2.5f, FadeIn = 0.02f, FadeOutFrom = 0.4f, Max = count + 2 });
            Puff(at, new P { Mat = Glow("fx_glow", orbPale), Count = Mathf.Max(2, count / 4), LifeMin = 0.5f, LifeMax = 1f, SpeedMin = r * 3f, SpeedMax = r * 10f, SizeMin = r * 0.3f, SizeMax = r * 0.7f,
                             A = orbPale, B = new Color(orbPale.r, orbPale.g, orbPale.b, 0f), Drag = 2.5f, FadeIn = 0.02f, FadeOutFrom = 0.4f, Max = count / 4 + 3 });
        }
    }
}
