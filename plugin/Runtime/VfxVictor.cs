using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Victor's visuals (2026-11-05, the creator's own DLL build): red-violet pain and white-violet electricity, built from
    /// RoR2's glow, spark, smoke, tracer, lightning and area materials like every other hero's.</summary>
    public static partial class Vfx
    {
        static readonly Color victorRed = new Color(0.9f, 0.18f, 0.4f), victorViolet = new Color(0.62f, 0.35f, 1f),
                              victorArc = new Color(0.85f, 0.8f, 1f), victorBlood = new Color(0.7f, 0.05f, 0.12f),
                              victorDust = new Color(0.6f, 0.5f, 0.45f), victorBolt = new Color(0.65f, 0.85f, 1f);

        static GameObject Victor(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle)
        {
            switch (id)
            {
                case "victor_punch": return VictorPunch(moment, at, to, angle);
                case "victor_gun": return VictorGun(moment, at);
                case "victor_battery": return VictorBattery(moment, at, radius, to);
                case "victor_aura": return VictorAuraFx(moment, at, radius, follow, to);
                case "victor_wire": return VictorWire(moment, at, follow);
                case "victor_reanimation": return VictorReanimation(moment, at, radius, follow);
            }
            return null;
        }

        // ------------------------------------------------------------------ Heavy melee
        static GameObject VictorPunch(string moment, Vector3 at, Vector3? to, float side)
        {
            switch (moment)
            {
                case "cast":
                {
                    // a short red-violet smear swept across the front, from the swinging arm's side
                    var end = to ?? at;
                    var fwd = end - at; fwd.y = 0;
                    if (fwd.sqrMagnitude < 0.01f) return null;
                    var right = Vector3.Cross(Vector3.up, fwd.normalized);
                    var root = Root(at + right * 1.1f * side, 0.35f, null);
                    var tip = new GameObject("fist"); tip.transform.SetParent(root.transform, false);
                    Trail(tip, victorRed, 0.35f, 0.18f);
                    var m = tip.AddComponent<Mover>(); m.From = at + right * 1.1f * side; m.To = end - right * 0.6f * side; m.Time = 0.12f;
                    return root;
                }
                case "hit":
                    Burst(at, "mat_spark", victorRed, 10, 0.3f, 7f, 0.07f);
                    Burst(at, "mat_dust", victorDust, 4, 0.5f, 2f, 0.4f, 0.3f);
                    Burst(at, "mat_glow", victorRed, 1, 0.12f, 0f, 1.2f);
                    Flash(at, victorRed, 5f, 0.1f, 3f);
                    return null;
                case "spark":
                    // from 5 Pain the barbed wire discharges into whatever the fist hits
                    VictorPunch("hit", at, null, 0f);
                    for (int i = 0; i < 3; i++) Bolt(at, at + Random.onUnitSphere * 1.6f, victorArc, 0.12f, 0.12f, 5, 0.3f);
                    Burst(at, "mat_spark", victorArc, 12, 0.25f, 9f, 0.05f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Memento Mori
        static GameObject VictorGun(string moment, Vector3 at)
        {
            switch (moment)
            {
                case "muzzle":
                {
                    var root = Root(at, 0.5f, null);
                    Particles(root, "mat_glow", victorRed, count: 2, life: 0.07f, speed: 0.3f, size: 0.45f, world: true);
                    Particles(root, "mat_smoke", new Color(0.75f, 0.6f, 0.65f, 1f), count: 3, life: 0.4f, speed: 1f, size: 0.3f, shapeRadius: 0.08f, world: true);
                    Particles(root, "mat_spark", victorRed, count: 4, life: 0.15f, speed: 6f, size: 0.04f, world: true, stretch: 2f);
                    Flash(at, victorRed, 3.5f, 0.06f, 2.5f);
                    return root;
                }
                case "hit":
                    Burst(at, "mat_spark", victorRed, 5, 0.22f, 4f, 0.05f);
                    Burst(at, "mat_glow", victorRed, 1, 0.1f, 0f, 0.25f);
                    return null;
                case "impact":
                    Burst(at, "mat_spark", new Color(1f, 0.7f, 0.5f), 4, 0.2f, 3.5f, 0.04f);
                    Burst(at, "mat_dust", victorDust, 2, 0.4f, 1f, 0.25f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Pain Battery
        static GameObject VictorBattery(string moment, Vector3 at, float radius, Vector3? to)
        {
            switch (moment)
            {
                case "cast":
                {
                    // a jagged arc from the barbed-wire arm to where the bolt lands, with a thinner second one around it
                    var end = to ?? at;
                    Bolt(at, end, victorArc, 0.5f, 0.3f, 14, 0f);
                    Bolt(at, end, victorViolet, 0.25f, 0.22f, 10, 0f, true);
                    Burst(at, "mat_glow", victorViolet, 2, 0.2f, 0f, 0.8f);
                    Flash(at, victorViolet, 6f, 0.15f, 4f);
                    return null;
                }
                case "hit":
                {
                    Burst(at, "mat_spark", victorArc, 24, 0.4f, 10f, 0.07f, 0.3f);
                    Burst(at, "mat_glow", victorViolet, 3, 0.3f, 0f, radius * 0.9f);
                    Ring(at + Vector3.up * 0.2f, victorViolet, 0.3f, radius, 0.35f, 0.25f);
                    for (int i = 0; i < 5; i++) Bolt(at, at + Random.onUnitSphere * radius, victorArc, 0.15f, 0.2f, 6, 0.4f);
                    Flash(at, victorArc, radius * 3f, 0.25f, 5f);
                    var root = Root(at, 0.6f, null);
                    var c = root.AddComponent<Crackle>(); c.Color = victorArc; c.Radius = radius * 0.8f; c.Count = 2;
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Aura of Suffering
        static GameObject VictorAuraFx(string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "cast":
                    // the jolt that switches it on
                    Ring(at + Vector3.up * 0.3f, victorRed, 0.5f, radius, 0.4f, 0.3f);
                    Burst(at + Vector3.up, "mat_spark", victorArc, 16, 0.3f, 8f, 0.06f, 0.5f);
                    Flash(at + Vector3.up, victorRed, radius, 0.2f, 4f);
                    return null;
                case "loop":
                {
                    // the field: RoR2's area dome in red-violet (it draws where it meets the ground), glowing motes and small arcs
                    // along its edge, a dim light, and the whole field beating like a faulty heart; the owner scales it with Pain
                    var root = Root(at, 0f, follow);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", victorRed);
                    dome.AddComponent<Heartbeat>();
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.3f;
                    Particles(edge, "mat_glow", victorRed, rate: 50, life: 0.7f, speed: 0.8f, size: 0.45f, shapeRadius: radius, ring: true);
                    Particles(edge, "mat_spark", victorArc, rate: 20, life: 0.25f, speed: 3f, size: 0.05f, shapeRadius: radius, ring: true, stretch: 2f);
                    var core = new GameObject("core"); core.transform.SetParent(root.transform, false); core.transform.localPosition = Vector3.up * 1.2f;
                    var c = core.AddComponent<Crackle>(); c.Color = victorViolet; c.Radius = 1.4f; c.Count = 1; c.Recolor = true;
                    var l = Light(root, victorRed, radius * 1.3f, 1.5f, Vector3.up * 1.5f);
                    l.gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                {
                    // a thin red thread drawing health from an enemy to Victor
                    var end = to ?? at;
                    var root = Root(at, 0.35f, null);
                    var lr = Line(root.transform, victorBlood, 0.08f, Vector3.zero, end - at);
                    root.AddComponent<FadeLine>().Life = 0.35f;
                    Burst(at, "mat_glow", victorRed, 2, 0.3f, 1f, 0.3f);
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Pain: the barbed-wire arm
        static GameObject VictorWire(string moment, Vector3 at, Transform follow)
        {
            if (moment != "loop") return null;
            var root = Root(at, 0f, follow);
            var c = root.AddComponent<Crackle>(); c.Color = victorArc; c.Radius = 0.35f; c.Count = 1;
            Particles(root, "mat_spark", victorArc, rate: 12, life: 0.2f, speed: 2f, size: 0.03f, shapeRadius: 0.15f, world: true, stretch: 2f);
            Light(root, victorViolet, 2f, 1.2f, Vector3.zero).gameObject.AddComponent<Flicker>();
            return root;
        }

        // ------------------------------------------------------------------ Shocking Reanimation
        static GameObject VictorReanimation(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // rising: lightning crackling around the body, a light growing toward the strike
                    var root = Root(at, 3.3f, follow);
                    var c = root.AddComponent<Crackle>(); c.Color = victorBolt; c.Radius = 1.6f; c.Count = 2;
                    Particles(root, "mat_spark", victorBolt, rate: 40, life: 0.3f, speed: 3f, size: 0.05f, shapeRadius: 1f, world: true, stretch: 2f);
                    var l = Light(root, victorBolt, 8f, 1f, Vector3.zero);
                    var g = l.gameObject.AddComponent<GrowLight>(); g.To = 8f; g.Over = 3f;
                    return null;
                }
                case "hit":
                {
                    // the bolt from the sky into the carrier, then the ball of lightning bursting out to radius
                    var sky = at + Vector3.up * 40f;
                    Bolt(sky, at, Color.white, 1.4f, 0.4f, 18, 2f);
                    Bolt(sky + Random.insideUnitSphere * 3f, at, victorBolt, 0.7f, 0.35f, 16, 2.5f);
                    Flash(at, Color.white, radius, 0.5f, 8f);
                    Burst(at, "mat_glow", victorBolt, 4, 0.5f, 0f, 6f);
                    Burst(at, "mat_spark", victorBolt, 60, 0.8f, 30f, 0.12f, 1f);
                    Ring(at, victorBolt, 1f, radius, 0.7f, 0.8f);
                    Ring(at, victorArc, 1f, radius, 0.7f, 0.6f, true);
                    for (int i = 0; i < 10; i++) Bolt(at, at + Random.onUnitSphere * radius * Random.Range(0.4f, 1f), victorBolt, 0.3f, 0.3f, 10, 1.5f);
                    Shake(at, radius * 1.5f, 3f, 0.6f);
                    return null;
                }
                case "loop":
                {
                    // charged: small arcs around the carrier for the 20 s
                    var root = Root(at, 0f, follow);
                    var c = root.AddComponent<Crackle>(); c.Color = victorBolt; c.Radius = 1.2f; c.Count = 1;
                    Particles(root, "mat_glow", victorBolt, rate: 8, life: 0.4f, speed: 0.5f, size: 0.3f, shapeRadius: 0.8f);
                    Light(root, victorBolt, 5f, 1.2f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
            }
            return null;
        }

        /// <summary>A faulty heartbeat: two quick swells, then a pause (Aura of Suffering's edge).</summary>
        sealed class Heartbeat : MonoBehaviour
        {
            Vector3 size; float age;
            void Start() => size = transform.localScale;
            void Update()
            {
                age += Time.deltaTime;
                float t = age % 1.1f;
                float beat = Mathf.Exp(-Mathf.Pow((t - 0.1f) * 18f, 2f)) + 0.6f * Mathf.Exp(-Mathf.Pow((t - 0.32f) * 18f, 2f));
                transform.localScale = size * (1f + 0.05f * beat);
            }
        }
    }
}
