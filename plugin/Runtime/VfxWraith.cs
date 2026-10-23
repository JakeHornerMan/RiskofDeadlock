using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Wraith's visuals (v0.11.0): purple smoke magic, the creator's first ask for her (2026-10-23), built like the others from
    /// RoR2's smoke, glow and spark materials tinted violet, with Deadlock's own card model for Card Trick. Each card trick has its
    /// colour on top of the purple: bleed red, crit gold, stun pale blue, burn orange (the trick rides in the angle argument).</summary>
    public static partial class Vfx
    {
        static readonly Color wraithViolet = new Color(0.56f, 0.31f, 0.85f), wraithDeep = new Color(0.33f, 0.12f, 0.6f),
                              wraithLilac = new Color(0.8f, 0.62f, 1f), wraithMagenta = new Color(0.86f, 0.42f, 1f),
                              wraithSmoke = new Color(0.78f, 0.52f, 1f, 1f), wraithSmokeDark = new Color(0.55f, 0.3f, 0.9f, 0.95f);
        static readonly Color[] cardColors = { new Color(0.92f, 0.12f, 0.16f), new Color(1f, 0.8f, 0.28f), new Color(0.6f, 0.85f, 1f), new Color(1f, 0.5f, 0.15f) };

        static Color CardColor(float suit) => cardColors[Mathf.Clamp(Mathf.RoundToInt(suit), 0, cardColors.Length - 1)];

        static GameObject Wraith(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle)
        {
            switch (id)
            {
                case "wraith_gun": return WraithGun(moment, at);
                case "wraith_card": return WraithCard(moment, at, radius, follow, angle);
                case "wraith_mind": return WraithMind(moment, at, radius);
                case "wraith_fullauto": return WraithFullAuto(moment, at, radius, follow);
                case "wraith_tk": return WraithTk(moment, at, radius, follow, to);
            }
            return null;
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
            switch (moment)
            {
                case "cast":
                    // the card conjured in her hand from a puff of purple smoke, glinting in its trick's colour
                    Burst(at, "mat_smoke", wraithSmoke, 6, 0.5f, 0.6f, 0.45f, shapeRadius: 0.15f, upward: true);
                    Burst(at, "mat_glow", c, 2, 0.15f, 0f, 0.6f);
                    Burst(at, "mat_spark", wraithLilac, 8, 0.25f, 2.5f, 0.04f, shapeRadius: 0.1f);
                    return null;
                case "loop":
                {
                    // Deadlock's card spinning end over end, a violet smoke trail and a glow in the trick's colour
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
                    Trail(root, wraithViolet, 0.22f, 0.25f);
                    Light(root, c, 3f, 1.2f, Vector3.zero);
                    return root;
                }
                case "hit":
                {
                    // a burst of purple smoke and the trick's colour: blood, gold, frost-blue crackle or fire
                    PurpleSmoke(at, 1.1f, 12, 0.9f, 2.2f, 0.3f);
                    Burst(at, "mat_glow", c, 3, 0.2f, 0f, 2.2f);
                    Burst(at, "mat_spark", c, 22, 0.45f, 7f, 0.07f, shapeRadius: 0.2f, gravity: 0.5f);
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
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Project Mind
        static GameObject WraithMind(string moment, Vector3 at, float radius)
        {
            switch (moment)
            {
                case "aim":
                {
                    // where she will appear: a swirling purple smoke circle on the ground and a faint column rising from it
                    var root = Root(at, 0f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", wraithViolet);
                    var swirl = new GameObject("swirl"); swirl.transform.SetParent(root.transform, false); swirl.transform.localPosition = Vector3.up * 0.2f;
                    swirl.AddComponent<Spin>().DegPerSec = 140f;
                    Particles(swirl, "mat_smoke", wraithSmoke, rate: 45, life: 1f, speed: 0.4f, size: 1.2f, shapeRadius: radius, ring: true);
                    Particles(swirl, "mat_glow", wraithMagenta, rate: 40, life: 0.5f, speed: 0.2f, size: 0.5f, shapeRadius: radius, ring: true);
                    Particles(root, "mat_glow", wraithViolet, rate: 20, life: 0.8f, speed: 1.2f, size: 0.8f, shapeRadius: radius * 0.6f, upward: true);
                    Particles(root, "mat_smoke", wraithSmoke, rate: 20, life: 1.3f, speed: 1.5f, size: 0.9f, shapeRadius: radius * 0.5f, upward: true);
                    Particles(root, "mat_spark", wraithLilac, rate: 20, life: 0.6f, speed: 1.8f, size: 0.04f, shapeRadius: radius * 0.6f, upward: true);
                    Light(root, wraithViolet, radius * 3f, 1.5f, Vector3.up * 0.6f).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "end":
                case "hit":
                {
                    // where she leaves and where she lands: a column of purple smoke, a violet flash and a ring
                    bool arrive = moment == "hit";
                    PurpleSmoke(at + Vector3.up * 0.8f, 1.5f, arrive ? 26 : 20, 1.2f, 2.5f, 0.7f);
                    Burst(at, "mat_smoke", wraithSmoke, 14, 1f, radius * 2.5f, 1.1f, shapeRadius: 0.4f, flat: true);
                    Burst(at + Vector3.up, "mat_spark", wraithLilac, 30, 0.6f, 5f, 0.06f, shapeRadius: 0.5f, upward: true);
                    Burst(at + Vector3.up, "mat_glow", wraithMagenta, 3, 0.25f, 0f, 2.6f);
                    Ring(at + Vector3.up * 0.15f, wraithViolet, 0.4f, radius * 2.5f, 0.45f, 0.35f);
                    Flash(at + Vector3.up, wraithViolet, 8f, 0.4f, 6f);
                    if (arrive) Shake(at, 12f, 0.6f, 0.2f);
                    return null;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Full Auto
        static GameObject WraithFullAuto(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // a ring of purple smoke bursting out around her as she slams the new magazine in
                    var root = Root(at, 2f, follow);
                    Particles(root, "mat_smoke", wraithSmoke, count: 26, life: 1.1f, speed: 3f, size: 1.1f, shapeRadius: radius + 0.3f, ring: true);
                    Particles(root, "mat_smoke", wraithSmokeDark, count: 12, life: 1.3f, speed: 1.5f, size: 1.4f, shapeRadius: radius, upward: true);
                    Particles(root, "mat_spark", wraithLilac, count: 35, life: 0.6f, speed: 5f, size: 0.05f, shapeRadius: radius, upward: true);
                    Particles(root, "mat_glow", wraithMagenta, count: 4, life: 0.3f, speed: 0f, size: 3f);
                    Ring(at - Vector3.up * 0.6f, wraithViolet, 0.5f, 4f, 0.45f, 0.4f);
                    Flash(at, wraithViolet, 7f, 0.4f, 5f);
                    return root;
                }
                case "loop":
                {
                    // smoke and sparks curling off The Rake while it runs hot
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_smoke", wraithSmoke, rate: 30, life: 0.8f, speed: 0.8f, size: 0.6f, shapeRadius: 0.1f, world: true, upward: true);
                    Particles(root, "mat_spark", wraithMagenta, rate: 18, life: 0.3f, speed: 2f, size: 0.04f, world: true);
                    Particles(root, "mat_glow", wraithViolet, rate: 25, life: 0.4f, speed: 0.5f, size: 0.6f, world: true);
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
                    // her mind reaching out: a purple shockwave and a rolling smoke wave sweeping out to the edge of the grab
                    Ring(at + Vector3.up * 0.3f, wraithViolet, 1f, radius, 0.7f, 1.2f);
                    Ring(at + Vector3.up * 1.2f, wraithMagenta, 0.5f, radius * 0.8f, 0.9f, 0.6f);
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
                    // under each lifted enemy: purple smoke pouring off it and a glow holding it up (radius = how long)
                    var root = Root(at, radius, follow);
                    Particles(root, "mat_smoke", wraithSmoke, rate: 45, life: 1.1f, speed: 1.2f, size: 1.8f, shapeRadius: 1f, world: true);
                    Particles(root, "mat_smoke", wraithSmokeDark, rate: 20, life: 1.3f, speed: 1.5f, size: 2.2f, shapeRadius: 0.8f, world: true, down: true);
                    Particles(root, "mat_glow", wraithViolet, rate: 30, life: 0.6f, speed: 0.6f, size: 1.6f, shapeRadius: 0.9f, world: true);
                    Particles(root, "mat_glow", wraithMagenta, rate: 12, life: 0.3f, speed: 0.3f, size: 2.2f, shapeRadius: 0.4f);
                    Particles(root, "mat_spark", wraithLilac, rate: 25, life: 0.5f, speed: 1.5f, size: 0.05f, shapeRadius: 0.8f, world: true);
                    Light(root, wraithViolet, 4f, 1.5f, Vector3.up * 0.5f).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "wave":
                {
                    // thrown: a violet smoke trail behind it (radius = the flight's length)
                    var root = Root(at, radius, follow);
                    Trail(root, wraithViolet, 0.5f, 0.35f);
                    Particles(root, "mat_smoke", wraithSmoke, rate: 70, life: 0.9f, speed: 0.3f, size: 1.6f, shapeRadius: 0.5f, world: true);
                    Particles(root, "mat_glow", wraithViolet, rate: 40, life: 0.4f, speed: 0.2f, size: 1.2f, shapeRadius: 0.4f, world: true);
                    Particles(root, "mat_spark", wraithMagenta, rate: 30, life: 0.3f, speed: 1f, size: 0.05f, shapeRadius: 0.4f, world: true);
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
                    return null;
                }
                case "hit":
                case "slam":
                {
                    // where it lands: a blast of purple smoke and sparks; harder on a wall
                    float k = moment == "slam" ? 1.6f : 1f;
                    PurpleSmoke(at + Vector3.up * 0.5f, 1.8f * k, (int)(16 * k), 1.2f, 3f * k, radius * 0.4f);
                    Burst(at, "mat_smoke", wraithSmoke, (int)(10 * k), 0.9f, radius * 2f, 1.2f * k, shapeRadius: 0.4f, flat: true);
                    Burst(at, "mat_spark", wraithLilac, (int)(30 * k), 0.7f, 9f * k, 0.07f, shapeRadius: 0.3f, upward: true, gravity: 1.5f);
                    Ring(at + Vector3.up * 0.2f, wraithViolet, 0.4f, radius * 2f * k, 0.4f, 0.6f);
                    Flash(at + Vector3.up, wraithViolet, 8f * k, 0.35f, 6f * k);
                    Shake(at, 25f, 1.2f * k, 0.3f);
                    return null;
                }
            }
            return null;
        }
    }
}
