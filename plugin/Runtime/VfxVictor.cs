using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Victor's visuals (2026-11-05, the creator's own DLL build): red pain and green electricity (wavy green strikes
    /// pulsing out of him through the aura, from the creator's reference), built from
    /// RoR2's glow, spark, smoke, tracer, lightning and area materials like every other hero's.</summary>
    public static partial class Vfx
    {
        static readonly Color victorRed = new Color(0.9f, 0.18f, 0.4f), victorViolet = new Color(0.45f, 1f, 0.2f),
                              victorArc = new Color(0.82f, 1f, 0.55f), victorBlood = new Color(0.7f, 0.05f, 0.12f),
                              victorDust = new Color(0.6f, 0.5f, 0.45f), victorBolt = new Color(0.5f, 1f, 0.25f),
                              // the aura's field: a sickly electric green (creator, 2026-11-05: "the wavy green strikes it pulses from Victor")
                              victorField = new Color(0.3f, 0.85f, 0.15f);

        static GameObject Victor(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle)
        {
            switch (id)
            {
                case "victor_jumpstart": return VictorJumpstart(moment, at, radius, to);
                case "victor_gun": return VictorGun(moment, at);
                case "victor_battery": return VictorBattery(moment, at, radius, to);
                case "victor_aura": return VictorAuraFx(moment, at, radius, follow, to);
                case "victor_wire": return VictorWire(moment, at, follow);
                case "victor_reanimation": return VictorReanimation(moment, at, radius, follow);
            }
            return null;
        }

        // ------------------------------------------------------------------ Jumpstart
        /// <summary>The defibrillator jolt: white-violet arcs from the barbed-wire arm (at) across his chest (to), sparks off the arm
        /// and a short flash. "strike" (at = his feet, to = his chest): a bolt of green lightning from the sky into him (creator,
        /// 2026-11-09: "Ability need to be clearer have a strike of green lightning come down!"): a thick white core inside two green
        /// bolts, a scorch ring and sparks on the ground, a bright green flash and a small shake.</summary>
        static GameObject VictorJumpstart(string moment, Vector3 at, float radius, Vector3? to)
        {
            if (moment == "nova")
            {
                // the blast round him as the bolt lands (at = his feet, radius = the blast): a cage of green bolts striking the
                // ground all round the edge, arcs crawling out along the floor like cracks to meet them, a green shock ring racing out
                // to the edge with a pale one behind it, a glowing disc burnt into the floor, and sparks and a column of green rising
                float r = Mathf.Max(1f, radius);
                var floor = at + Vector3.up * 0.1f;
                int n = 9;
                for (int i = 0; i < n; i++)
                {
                    float ang = (i + Random.Range(-0.25f, 0.25f)) / n * Mathf.PI * 2f;
                    var edge = floor + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r * Random.Range(0.85f, 1f);
                    Bolt(edge + Vector3.up * Random.Range(9f, 14f) + Random.insideUnitSphere * 1.2f, edge, victorBolt, 0.22f, 0.35f, 12, 0.9f, true);
                    Bolt(floor, edge, victorArc, 0.1f, 0.4f, 12, 0.45f, true, true);
                    Burst(edge, "mat_spark", victorBolt, 8, 0.35f, 6f, 0.05f, 0.2f, false, true);
                }
                Ring(floor, victorBolt, 0.4f, r, 0.3f, 0.6f);
                Ring(floor + Vector3.up * 0.05f, victorArc, 0.2f, r * 0.9f, 0.5f, 0.25f);
                Ring(at + Vector3.up * 1.2f, victorField, 0.3f, r * 0.7f, 0.35f, 0.2f);
                Burst(floor, "mat_glow", new Color(victorField.r, victorField.g, victorField.b, 0.6f), 10, 0.5f, r * 1.6f, 1.6f, 0.4f, true);
                Burst(floor, "mat_glow", victorBolt, 2, 0.6f, 0f, r * 1.2f);
                Burst(at, "mat_spark", victorArc, 40, 0.6f, 9f, 0.06f, r * 0.5f, false, true, 0.6f);
                Flash(at + Vector3.up, victorBolt, r * 3f, 0.3f, 6f);
                return null;
            }
            if (moment == "zap")
            {
                // an arc from him (to) to an enemy the blast hit (at), and a green pop on it
                var from = to ?? at;
                Bolt(from, at, victorBolt, 0.12f, 0.25f, 10, 0.5f, true);
                Burst(at, "mat_spark", victorArc, 10, 0.3f, 6f, 0.05f, 0.3f);
                Burst(at, "mat_glow", victorBolt, 1, 0.25f, 0f, 1.2f);
                return null;
            }
            if (moment == "strike")
            {
                var body = to ?? at + Vector3.up;
                var sky = at + Vector3.up * 30f + Random.insideUnitSphere * 1.5f;
                Bolt(sky, body, Color.white, 0.55f, 0.3f, 18, 1.2f);
                Bolt(sky, at, victorBolt, 0.9f, 0.35f, 16, 1.8f, true);
                Bolt(sky + Random.insideUnitSphere * 2f, body, victorArc, 0.35f, 0.4f, 14, 2.2f, true);
                for (int i = 0; i < 6; i++)
                {
                    float ang = i / 6f * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                    Bolt(at + Vector3.up * 0.1f, at + new Vector3(Mathf.Cos(ang), 0.1f, Mathf.Sin(ang)) * 2.5f, victorBolt, 0.12f, 0.3f, 10, 0.3f, true, true);
                }
                Ring(at + Vector3.up * 0.15f, victorBolt, 0.3f, 3f, 0.4f, 0.35f);
                Burst(at + Vector3.up * 0.2f, "mat_spark", victorArc, 30, 0.4f, 10f, 0.07f, 0.4f);
                Burst(body, "mat_glow", victorBolt, 2, 0.3f, 0f, 2.2f);
                Flash(body, victorBolt, 12f, 0.3f, 7f);
                Shake(at, 8f, 0.6f, 0.2f);
                return null;
            }
            if (moment != "cast") return null;
            var chest = to ?? at;
            Bolt(at, chest, Color.white, 0.25f, 0.18f, 8, 0.25f);
            for (int i = 0; i < 3; i++) Bolt(at, chest + Random.insideUnitSphere * 0.5f, victorArc, 0.2f, 0.1f, 6, 0.3f);
            for (int i = 0; i < 3; i++) Bolt(chest, chest + Random.onUnitSphere * 1.2f, victorViolet, 0.15f, 0.08f, 5, 0.35f);
            Burst(at, "mat_spark", victorArc, 14, 0.25f, 7f, 0.05f);
            Burst(chest, "mat_glow", victorViolet, 1, 0.15f, 0f, 1.4f);
            Flash(chest, victorViolet, 6f, 0.15f, 3f);
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
                    Bolt(at, end, Color.white, 0.7f, 0.3f, 18, 0f);
                    Bolt(at, end, victorBolt, 0.5f, 0.35f, 16, 0f, true);
                    Bolt(at, end, victorArc, 0.3f, 0.25f, 12, 0f, true);
                    Burst(at, "mat_glow", victorViolet, 2, 0.2f, 0f, 0.8f);
                    Flash(at, victorViolet, 6f, 0.15f, 4f);
                    return null;
                }
                case "impact":
                {
                    // it met an enemy (creator, 2026-11-09: "make it be way more impact when we hit an enemy"): the normal burst,
                    // plus a white-green core, a second wider shockwave, more arcs, a bigger flash and a camera shake
                    VictorBattery("hit", at, radius, to);
                    Burst(at, "mat_glow", Color.white, 2, 0.2f, 0f, radius * 0.6f);
                    Burst(at, "mat_spark", victorBolt, 40, 0.6f, 18f, 0.09f, 0.5f);
                    Ring(at + Vector3.up * 0.2f, victorBolt, 0.5f, radius * 1.6f, 0.5f, 0.45f);
                    Ring(at, victorArc, 0.3f, radius * 1.2f, 0.4f, 0.3f, true);
                    for (int i = 0; i < 8; i++) Bolt(at, at + Random.onUnitSphere * radius * 1.4f, victorBolt, 0.22f, 0.3f, 8, 0.6f, true);
                    Flash(at, Color.white, radius * 4f, 0.2f, 9f);
                    Shake(at, radius * 6f, 1.5f, 0.3f);
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
                    // the jolt that switches it on: a green shockwave and a ring of strikes out to the edge
                    Ring(at + Vector3.up * 0.3f, victorBolt, 0.5f, radius, 0.4f, 0.5f);
                    for (int i = 0; i < 14; i++)
                    {
                        float ang = i / 14f * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                        Bolt(at + Vector3.up * 0.12f, at + new Vector3(Mathf.Cos(ang), 0.12f, Mathf.Sin(ang)) * radius, victorBolt, 0.2f, 0.35f, 28, 0.4f, true, true);
                    }
                    Burst(at + Vector3.up, "mat_spark", victorArc, 40, 0.4f, 14f, 0.08f, 0.5f);
                    Flash(at + Vector3.up, victorBolt, radius * 1.5f, 0.3f, 8f);
                    Shake(at, radius, 1.2f, 0.25f);
                    return null;
                case "loop":
                {
                    // the field: RoR2's area dome in green (it draws where it meets the ground), green motes and sparks along its
                    // edge, crackling arcs around him and, on every beat of his faulty heart, a burst of wavy green strikes from his
                    // chest out across the ground to the edge (AuraStrikes); the owner scales it all with Pain
                    var root = Root(at, 0f, follow);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", victorField);
                    dome.AddComponent<Heartbeat>();
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.3f;
                    Particles(edge, "mat_glow", victorField, rate: 60, life: 0.7f, speed: 0.8f, size: 0.5f, shapeRadius: radius, ring: true);
                    Particles(edge, "mat_spark", victorArc, rate: 45, life: 0.3f, speed: 4f, size: 0.06f, shapeRadius: radius, ring: true, stretch: 2.5f);
                    var core = new GameObject("core"); core.transform.SetParent(root.transform, false); core.transform.localPosition = Vector3.up * 1.2f;
                    var st = core.AddComponent<AuraStrikes>(); st.BaseRadius = radius;
                    var l = Light(root, victorBolt, radius * 1.4f, 2.2f, Vector3.up * 1.5f);
                    l.gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "zap":
                {
                    // a flying enemy inside the field took a tick: one green bolt from Victor up to it (at = the enemy, to = Victor)
                    var from = to ?? at;
                    Bolt(from, at, victorBolt, 0.2f, 0.2f, 16, 0.35f, true);
                    Bolt(from, at, victorArc, 0.07f, 0.15f, 12, 0.45f, true);
                    Burst(at, "mat_spark", victorArc, 8, 0.2f, 6f, 0.05f);
                    return null;
                }
                case "mark":
                {
                    // an enemy inside the field (taking +33 % from him): a short green arc from Victor to it and a spark on it
                    var from = to ?? at;
                    Bolt(from + Vector3.up * 0.5f, at, victorField, 0.06f, 0.18f, 10, 0.25f, true);
                    Burst(at, "mat_spark", victorArc, 4, 0.2f, 3f, 0.04f);
                    Burst(at, "mat_glow", victorField, 1, 0.25f, 0f, 0.5f);
                    return null;
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

        /// <summary>Aura of Suffering's strikes: on every beat (two quick ones, then a pause, like <see cref="Heartbeat"/>) 10 wavy
        /// green bolts from his chest out across the ground to the edge of the field, a green ring and a flash; between beats a
        /// couple of smaller strikes now and then. The edge follows the field's scale (it grows with Pain).</summary>
        sealed class AuraStrikes : MonoBehaviour
        {
            public float BaseRadius = 9f;
            float age, nextSmall; int beats = -1;
            void Update()
            {
                age += Time.deltaTime;
                float r = BaseRadius * (transform.parent ? transform.parent.lossyScale.x : 1f);
                // along the floor only (creator: "should only travel across the floor"): from his feet to the edge, kinking sideways
                var ground = transform.position - Vector3.up * 1.15f + Vector3.up * 0.12f; var c = ground;
                int beat = Mathf.FloorToInt(age / 1.1f) * 2 + ((age % 1.1f) >= 0.32f ? 1 : 0);
                if ((age % 1.1f) < 0.1f) beat--;
                if (beat > beats)
                {
                    beats = beat;
                    bool big = beat % 2 == 0;
                    int n = big ? 16 : 9;
                    float spin = Random.value * Mathf.PI * 2f;
                    for (int i = 0; i < n; i++)
                    {
                        float ang = spin + i / (float)n * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                        var end = ground + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r * Random.Range(0.75f, 1.05f);
                        // fine, wavy strands: many short kinks, a small jitter; a whiter core strand on the big beat, and a branch
                        Bolt(c, end, victorBolt, big ? 0.16f : 0.11f, 0.3f, 28, 0.35f, true, true);
                        if (big) Bolt(c, end, victorArc, 0.06f, 0.22f, 24, 0.45f, true, true);
                        if (Random.value < 0.5f)
                        {
                            var mid = Vector3.Lerp(c, end, Random.Range(0.4f, 0.7f));
                            var side = Vector3.Cross(Vector3.up, end - c).normalized * r * Random.Range(-0.3f, 0.3f);
                            Bolt(mid, mid + side + (end - c) * 0.25f, victorBolt, 0.07f, 0.2f, 12, 0.25f, true, true);
                        }
                    }
                    Ring(ground, victorBolt, 0.8f, r, 0.35f, big ? 0.45f : 0.25f);
                    Flash(c + Vector3.up, victorBolt, r * 1.2f, 0.18f, big ? 7f : 4f);
                }
                if (Time.time >= nextSmall)
                {
                    nextSmall = Time.time + Random.Range(0.08f, 0.18f);
                    float ang = Random.value * Mathf.PI * 2f;
                    var end = ground + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r * Random.Range(0.4f, 1f);
                    Bolt(c, end, victorBolt, 0.09f, 0.15f, 20, 0.3f, true, true);
                }
            }
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
