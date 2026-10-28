using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Pocket's visuals (v1.2.0a): violet spirit magic like Deadlock's, built from RoR2's glow, tracer, smoke and spark
    /// materials, with Deadlock's own suitcase and frog models (effects props) where the move shows them. The Flying Cloak is a
    /// living cloth made in code (a rippling, twisting sheet with glowing edges); the Satchel's tendrils are bent lines that writhe.</summary>
    public static partial class Vfx
    {
        static readonly Color pocketViolet = new Color(0.55f, 0.32f, 0.95f), pocketLilac = new Color(0.8f, 0.66f, 1f),
                              pocketDeep = new Color(0.22f, 0.1f, 0.42f), pocketSmoke = new Color(0.72f, 0.56f, 1f, 1f),
                              pocketRed = new Color(1f, 0.22f, 0.28f),
                              pocketTeal = new Color(0.25f, 0.95f, 0.8f), pocketTealDeep = new Color(0.1f, 0.6f, 0.55f),
                              treasure = new Color(0.4f, 1f, 0.35f), gold = new Color(1f, 0.82f, 0.3f), slime = new Color(0.5f, 0.66f, 0.32f), water = new Color(0.66f, 0.85f, 0.95f);

        static GameObject Pocket(string id, string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle)
        {
            switch (id)
            {
                case "pocket_gun": return PocketGun(moment, at);
                case "pocket_dodge": return PocketDodge(moment, at, follow);
                case "pocket_barrage": return PocketBarrage(moment, at, radius, follow);
                case "pocket_cloak": return PocketCloak(moment, at, radius, follow, to);
                case "pocket_satchel": return PocketSatchel(moment, at, radius, follow, to);
                case "pocket_frog": return PocketFrog(id, moment, at, radius, angle);
            }
            return null;
        }

        // ------------------------------------------------------------------ The Black Sheep
        static GameObject PocketGun(string moment, Vector3 at)
        {
            switch (moment)
            {
                case "muzzle":
                {
                    var root = Root(at, 0.5f, null);
                    Particles(root, "mat_glow", pocketLilac, count: 3, life: 0.08f, speed: 0.5f, size: 0.5f, world: true);
                    Particles(root, "mat_smoke", pocketSmoke, count: 5, life: 0.45f, speed: 1.2f, size: 0.35f, shapeRadius: 0.1f, world: true);
                    Particles(root, "mat_spark", pocketViolet, count: 6, life: 0.2f, speed: 6f, size: 0.05f, world: true, stretch: 2f);
                    Flash(at, pocketLilac, 4f, 0.08f, 3f);
                    return root;
                }
                case "hit":
                    Burst(at, "mat_spark", pocketViolet, 5, 0.25f, 4f, 0.05f);
                    Burst(at, "mat_glow", pocketLilac, 1, 0.12f, 0f, 0.25f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Unpredictable's air dash
        static GameObject PocketDodge(string moment, Vector3 at, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    Burst(at, "mat_smoke", pocketSmoke, 10, 0.6f, 2f, 0.6f, 0.4f);
                    Burst(at, "mat_glow", pocketViolet, 8, 0.4f, 3f, 0.2f, 0.3f);
                    var root = Root(at, 0f, follow);
                    var tip = new GameObject("streak"); tip.transform.SetParent(root.transform, false); tip.transform.localPosition = Vector3.up * 1f;
                    Trail(tip, pocketViolet, 0.35f, 0.25f);
                    Particles(tip, "mat_glow", pocketLilac, rate: 40, life: 0.3f, speed: 0.3f, size: 0.18f, shapeRadius: 0.3f, world: true);
                    return root;
                }
                case "end":
                    Burst(at, "mat_smoke", pocketSmoke, 6, 0.5f, 1.2f, 0.45f, 0.3f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Barrage
        static GameObject PocketBarrage(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the open briefcase glows, a spirit ring turning over it
                    var root = Root(at, 0f, follow);
                    var ring = new GameObject("ring"); ring.transform.SetParent(root.transform, false); ring.transform.localPosition = Vector3.up * 0.4f;
                    ring.AddComponent<Spin>().DegPerSec = 240f;
                    Circle(ring.transform, pocketViolet, 0.45f, 0.05f, 32, true);
                    Particles(root, "mat_glow", pocketViolet, rate: 25, life: 0.5f, speed: 1.2f, size: 0.22f, shapeRadius: 0.25f, upward: true);
                    Particles(root, "mat_smoke", pocketSmoke, rate: 8, life: 0.7f, speed: 0.6f, size: 0.4f, shapeRadius: 0.2f, world: true, upward: true);
                    Light(root, pocketViolet, 5f, 2.5f, Vector3.up * 0.4f);
                    return root;
                }
                case "aim":
                {
                    // the owner's landing circle under the crosshair
                    var root = Root(at, 0f, null);
                    var lift = new GameObject("lift"); lift.transform.SetParent(root.transform, false); lift.transform.localPosition = Vector3.up * 0.15f;
                    Circle(lift.transform, pocketLilac, radius, 0.06f, 48, true);
                    Particles(root, "mat_glow", pocketViolet, rate: 18, life: 0.5f, speed: 0.4f, size: 0.25f, shapeRadius: radius, ring: true);
                    return root;
                }
                case "orb":
                {
                    // a glowing purple spirit with a ribbon tail
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_glow", pocketLilac, rate: 40, life: 0.15f, speed: 0f, size: 0.7f);
                    Particles(root, "mat_glow", pocketViolet, rate: 30, life: 0.35f, speed: 0.4f, size: 0.3f, shapeRadius: 0.2f, world: true);
                    Trail(root, pocketViolet, 0.3f, 0.3f);
                    Light(root, pocketViolet, 4f, 2f, Vector3.zero);
                    return root;
                }
                case "muzzle":
                    Burst(at, "mat_glow", pocketLilac, 6, 0.25f, 3f, 0.25f, 0.15f);
                    return null;
                case "hit":
                {
                    Flash(at + Vector3.up, pocketViolet, radius * 2.5f, 0.25f, 5f);
                    Ring(at + Vector3.up * 0.2f, pocketLilac, 0.5f, radius, 0.35f, 0.25f);
                    Burst(at + Vector3.up * 0.5f, "mat_glow", pocketViolet, 18, 0.5f, radius * 2f, 0.35f, 0.4f);
                    Burst(at, "mat_spark", pocketLilac, 16, 0.4f, radius * 2.5f, 0.07f, 0.3f, upward: true, gravity: 1f);
                    Burst(at, "mat_smoke", pocketSmoke, 8, 0.8f, radius * 0.6f, radius * 0.35f, radius * 0.4f, flat: true);
                    Shake(at, radius * 3f, 1f, 0.15f);
                    return null;
                }
                case "stun":
                {
                    // a dizzy swirl of motes over a stunned enemy
                    var root = Root(at, 1.2f, follow);
                    var spin = new GameObject("swirl"); spin.transform.SetParent(root.transform, false); spin.transform.localPosition = Vector3.up * 0.5f;
                    spin.AddComponent<Spin>().DegPerSec = 360f;
                    for (int i = 0; i < 3; i++)
                    {
                        var m = new GameObject("mote"); m.transform.SetParent(spin.transform, false);
                        float a = i * 120f * Mathf.Deg2Rad; m.transform.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.45f;
                        Particles(m, "mat_glow", pocketLilac, rate: 20, life: 0.25f, speed: 0f, size: 0.18f, world: true);
                    }
                    return root;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Flying Cloak
        static GameObject PocketCloak(string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "loop":
                {
                    // Deadlock's look round his flying coat (the coat itself is his own model in its lift pose, DlrCloak): a teal
                    // glow and aura, streaks trailing it, motes and a faint ring of its reach
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_glow", pocketTeal, rate: 40, life: 0.25f, speed: 0.2f, size: 1.6f, shapeRadius: 0.4f);
                    Particles(root, "mat_glow", pocketTealDeep, rate: 60, life: 0.6f, speed: 0.4f, size: 0.35f, shapeRadius: 1.1f, world: true);
                    Particles(root, "mat_smoke", new Color(0.55f, 1f, 0.9f, 1f), rate: 12, life: 0.8f, speed: 0.3f, size: 0.6f, shapeRadius: 0.7f, world: true);
                    foreach (var x in new[] { -0.7f, 0.7f })
                    {
                        var streak = new GameObject("streak"); streak.transform.SetParent(root.transform, false); streak.transform.localPosition = new Vector3(x, 0.2f, -0.3f);
                        Trail(streak, pocketTeal, 0.3f, 0.45f);
                    }
                    var reach = new GameObject("reach"); reach.transform.SetParent(root.transform, false);
                    Circle(reach.transform, new Color(pocketTeal.r, pocketTeal.g, pocketTeal.b, 0.35f), radius, 0.04f, 48, true);
                    reach.AddComponent<Spin>().DegPerSec = 60f;
                    Light(root, pocketTeal, 7f, 2.5f, Vector3.zero);
                    return root;
                }
                case "hit":
                {
                    // a slash of light across the enemy it passes through
                    var root = Root(at, 0.4f, null);
                    var from = to ?? at;
                    var dir = (at - from); dir.y = 0f;
                    var side = dir.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, dir.normalized) : Vector3.right;
                    var lr = Line(root.transform, pocketTeal, 0.12f, -side * 0.9f + Vector3.up * 0.4f, side * 0.9f - Vector3.up * 0.4f);
                    lr.gameObject.AddComponent<FadeLine>().Life = 0.3f;
                    Burst(at, "mat_spark", pocketLilac, 10, 0.3f, 5f, 0.06f, 0.2f);
                    Burst(at, "mat_glow", pocketViolet, 4, 0.25f, 1f, 0.35f);
                    return root;
                }
                case "stick":
                    Burst(at, "mat_spark", pocketViolet, 10, 0.3f, 4f, 0.06f);
                    return null;
                case "end":
                case "port":
                    Burst(at, "mat_smoke", new Color(0.55f, 1f, 0.9f, 1f), 12, 0.7f, 2f, 0.7f, 0.5f);
                    Burst(at, "mat_glow", pocketTeal, 14, 0.4f, 4f, 0.3f, 0.4f);
                    Flash(at, pocketTeal, 7f, 0.2f, 3f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Enchanter's Satchel
        static GameObject PocketSatchel(string moment, Vector3 at, float radius, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "cast":
                    Burst(at, "mat_smoke", pocketSmoke, 14, 0.7f, 2f, 0.8f, 0.6f);
                    Burst(at, "mat_glow", pocketViolet, 10, 0.4f, 3f, 0.3f, 0.5f);
                    return null;
                case "loop":
                {
                    // a violet glow round the floating case and its reach on the ground
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_glow", pocketViolet, rate: 30, life: 0.5f, speed: 0.4f, size: 0.5f, shapeRadius: 0.5f);
                    Particles(root, "mat_smoke", pocketSmoke, rate: 6, life: 1f, speed: 0.3f, size: 0.6f, shapeRadius: 0.4f, world: true);
                    Light(root, pocketViolet, 7f, 2.5f, Vector3.zero);
                    var ground = new GameObject("ground"); ground.transform.SetParent(root.transform, false);
                    ground.transform.position = PocketParts.Floor(at) + Vector3.up * 0.1f;
                    var turn = new GameObject("turn"); turn.transform.SetParent(ground.transform, false);
                    turn.AddComponent<Spin>().DegPerSec = 20f;
                    Circle(turn.transform, pocketLilac, radius, 0.07f, 64, true);
                    Particles(ground, "mat_glow", pocketViolet, rate: 30, life: 0.8f, speed: 0.6f, size: 0.25f, shapeRadius: radius, ring: true);
                    return root;
                }
                case "enter":
                    // he is in: the lid slams, a purple shockwave and a shake
                    Flash(at, pocketViolet, 8f, 0.2f, 5f);
                    Ring(PocketParts.Floor(at) + Vector3.up * 0.15f, pocketLilac, 0.5f, radius * 0.6f, 0.4f, 0.3f);
                    Burst(at, "mat_smoke", pocketSmoke, 20, 0.8f, 3.5f, 0.9f, 0.6f);
                    Burst(at, "mat_glow", pocketViolet, 16, 0.4f, 5f, 0.3f, 0.4f);
                    Burst(at, "mat_spark", pocketLilac, 14, 0.35f, 6f, 0.07f, 0.3f);
                    Shake(at, 25f, 2.5f, 0.25f);
                    return null;
                case "tether":
                {
                    var root = Root(at, 0f, null);
                    root.AddComponent<PocketTendrils>().Mat = Mat("mat_tracer", pocketViolet);
                    return root;
                }
                case "drain":
                {
                    // a red mote drawn out of the enemy back into the case
                    var root = Root(at, 0.45f, null);
                    var mv = root.AddComponent<Mover>(); mv.From = at; mv.To = to ?? at; mv.Time = 0.4f;
                    Particles(root, "mat_glow", pocketRed, rate: 40, life: 0.15f, speed: 0f, size: 0.3f, world: true);
                    return root;
                }
                case "end":
                {
                    // the lid bursts open: a 12 m spirit blast
                    Flash(at, pocketLilac, radius * 1.5f, 0.35f, 6f);
                    Ring(PocketParts.Floor(at) + Vector3.up * 0.2f, pocketLilac, 1f, radius, 0.45f, 0.35f);
                    Ring(at, pocketViolet, 0.5f, radius * 0.8f, 0.35f, 0.25f, true);
                    Burst(at, "mat_glow", pocketViolet, 40, 0.6f, radius * 1.6f, 0.4f, 0.5f);
                    Burst(at, "mat_smoke", pocketSmoke, 18, 1f, radius * 0.5f, 1f, 0.8f);
                    Burst(at, "mat_spark", pocketLilac, 30, 0.5f, radius * 2f, 0.08f, 0.4f);
                    Shake(at, radius * 2f, 2f, 0.25f);
                    return null;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Frog Form
        static GameObject PocketFrog(string id, string moment, Vector3 at, float radius, float angle)
        {
            switch (moment)
            {
                case "cast":
                case "end":
                    // a puff of smoke: the carrier vanishes into the frog, or pops back out of it
                    Burst(at, "mat_smoke", new Color(0.75f, 0.9f, 0.7f, 1f), 16, 0.8f, 2.5f, radius * 0.4f, radius * 0.3f);
                    Burst(at, "mat_glow", pocketViolet, 10, 0.4f, 3f, 0.3f, 0.4f);
                    return null;
                case "land":
                    // the thrown case thuds down
                    Burst(at, "mat_dust", new Color(0.55f, 0.5f, 0.45f), 6, 0.6f, 2f, 0.4f, 0.5f, flat: true);
                    Shake(at, 15f, 1f, 0.15f);
                    return null;
                case "jolt":
                    // something inside bangs against the lid
                    Burst(at, "mat_smoke", pocketSmoke, 4, 0.5f, 2f, radius * 0.15f, radius * 0.2f, flat: true);
                    Burst(at + Vector3.up * radius * 0.3f, "mat_glow", treasure, 4, 0.3f, 3f, 0.25f, radius * 0.2f);
                    Shake(at, 20f, 0.8f, 0.1f);
                    return null;
                case "burst":
                {
                    // the case bursts open: a treasure chest's green explosion and a column of light out of it
                    var up = at + Vector3.up * radius * 0.25f;
                    Flash(up, treasure, radius * 3f, 0.6f, 8f);
                    Flash(up, gold, radius * 2f, 0.4f, 4f);
                    Ring(at + Vector3.up * 0.2f, treasure, 1f, radius * 1.6f, 0.6f, 0.45f);
                    Ring(up, gold, 0.5f, radius * 1.2f, 0.45f, 0.3f, true);
                    Burst(up, "mat_glow", treasure, 60, 1.1f, radius * 2.2f, 0.5f, radius * 0.2f, upward: true, gravity: 0.6f);
                    Burst(up, "mat_spark", gold, 70, 1.3f, radius * 2.8f, 0.12f, radius * 0.2f, upward: true, gravity: 1.2f);
                    Burst(up, "mat_glow", new Color(0.85f, 1f, 0.6f), 30, 1.6f, radius * 0.6f, 0.3f, radius * 0.3f);
                    Burst(at, "mat_smoke", new Color(0.6f, 0.95f, 0.55f, 1f), 30, 1.4f, radius * 0.8f, radius * 0.3f, radius * 0.3f, flat: true);
                    var beam = Root(at, 1.2f, null);
                    var lr = Line(beam.transform, treasure, radius * 0.5f, Vector3.zero, Vector3.up * radius * 5f, "mat_glow");
                    lr.gameObject.AddComponent<FadeLine>().Life = 1.1f;
                    Shake(at, 40f, 4f, 0.4f);
                    return null;
                }
                case "grow":
                    Burst(at, "mat_smoke", pocketSmoke, 20, 0.9f, radius * 0.6f, radius * 0.25f, radius * 0.2f, flat: true);
                    Ring(at + Vector3.up * 0.2f, pocketLilac, 0.5f, radius, 0.4f, 0.25f);
                    Flash(at + Vector3.up, pocketViolet, radius * 2f, 0.3f, 4f);
                    return null;
                case "mouth":
                    Burst(at, "mat_glow", new Color(0.9f, 1f, 0.55f), 14, 0.6f, 2f, 0.4f, 0.6f);
                    Flash(at, new Color(0.8f, 1f, 0.5f), radius * 1.5f, 0.4f, 3f);
                    return null;
                case "slam":
                {
                    // a very heavy landing: a flash, two shockwaves over the 10 m it damages and one to the 15 m ring it stuns, a column of
                    // dust and slime, water and debris thrown out, a hard shake and small frogs hopping out of it
                    bool high = angle > 0.5f;
                    float stun = radius * 1.5f;
                    var floor = PocketParts.Floor(at + Vector3.up * 0.5f) + Vector3.up * 0.15f;
                    Flash(floor + Vector3.up, treasure, radius * 2.5f, 0.3f, high ? 8f : 5f);
                    Ring(floor, slime, 0.8f, radius, 0.45f, 0.5f);
                    Ring(floor, water, 0.5f, radius * 0.75f, 0.35f, 0.3f);
                    Ring(floor + Vector3.up * 0.1f, new Color(0.9f, 1f, 0.7f), radius, stun, 0.6f, 0.2f);
                    Ring(floor + Vector3.up * 1f, slime, 0.5f, radius * 0.6f, 0.3f, 0.25f, true);
                    Burst(floor, "mat_spark", water, high ? 80 : 50, 0.9f, radius * 1.6f, 0.14f, 0.8f, upward: true, gravity: 2f);
                    Burst(floor, "mat_glow", slime, high ? 30 : 20, 0.7f, radius * 1.3f, 0.45f, 0.8f, flat: true);
                    Burst(floor, "mat_smoke", new Color(0.7f, 0.75f, 0.6f, 1f), high ? 26 : 16, 1.2f, radius * 0.5f, radius * 0.25f, radius * 0.3f, upward: true);
                    Burst(floor, "mat_dust", new Color(0.42f, 0.4f, 0.32f), high ? 14 : 9, 1f, radius * 1.2f, 0.18f, 0.6f, upward: true, gravity: 2.5f);
                    Shake(floor, stun * 3f, high ? 9f : 5f, 0.45f);
                    PocketFrogs.Scatter(id + "_prop", floor, radius, high ? 6 : 4);
                    return null;
                }
            }
            return null;
        }
    }

    /// <summary>The Satchel's tendrils: one bent, writhing line from the case to each enemy it drinks from.</summary>
    public sealed class PocketTendrils : MonoBehaviour
    {
        public Material Mat;
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        Vector3 from;
        readonly List<Vector3> to = new List<Vector3>();
        const int Points = 12;

        public void Set(Vector3 a, List<Vector3> b) { from = a; to.Clear(); to.AddRange(b); }

        void Update()
        {
            while (lines.Count < to.Count)
            {
                var go = new GameObject("tendril"); go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = Mat; lr.useWorldSpace = true; lr.positionCount = Points;
                lr.widthCurve = new AnimationCurve(new Keyframe(0, 0.16f), new Keyframe(1, 0.05f));
                lines.Add(lr);
            }
            for (int i = 0; i < lines.Count; i++)
            {
                var lr = lines[i];
                lr.enabled = i < to.Count;
                if (!lr.enabled) continue;
                var b = to[i];
                var d = b - from;
                var side = Vector3.Cross(d, Vector3.up).normalized;
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                for (int p = 0; p < Points; p++)
                {
                    float k = p / (Points - 1f);
                    float bend = Mathf.Sin(k * Mathf.PI) * (0.6f + 0.35f * Mathf.Sin(Time.time * 6f + i * 1.7f + k * 5f));
                    lr.SetPosition(p, Vector3.Lerp(from, b, k) + side * bend * Mathf.Sin(Time.time * 3f + i) + Vector3.up * bend * 0.6f);
                }
            }
        }
    }

    /// <summary>Keeps Deadlock's frog's root bone where it sits: its hop clip lifts the root about 3 m on its own, which the frog's real
    /// flight already does (the frog floated out of its body).</summary>
    public sealed class FrogRootPin : MonoBehaviour
    {
        Transform root; Vector3 rest;
        void Start()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "root") { root = t; break; }
            if (root) rest = root.localPosition;
        }
        void LateUpdate() { if (root) root.localPosition = rest; }
    }

    /// <summary>The small frogs that hop out of a Frog Form landing: Deadlock's frog, small, two hops away from the splash, then
    /// shrinking away. Only for show.</summary>
    public sealed class PocketFrogs : MonoBehaviour
    {
        Vector3 start, dir; float age, life = 1.3f, size; Animation anim;

        public static void Scatter(string propId, Vector3 at, float radius, int count)
        {
            var clips = Sheet.Effect(propId)?.Clips ?? new string[0];
            string hop = clips.Length > 1 ? clips[1] : null;
            for (int i = 0; i < count; i++)
            {
                var built = PropModels.Build(propId);
                if (built == null) return;
                var f = built.Root.AddComponent<PocketFrogs>();
                float a = (i + Random.value * 0.6f) * Mathf.PI * 2f / count;
                f.dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                f.start = at + f.dir * radius * 0.25f;
                f.size = PocketParts.FrogScale(0.45f + Random.value * 0.2f);
                built.Root.transform.SetPositionAndRotation(f.start, Quaternion.LookRotation(f.dir));
                built.Root.transform.localScale = Vector3.one * f.size;
                f.anim = built.Anim;
                built.Root.AddComponent<FrogRootPin>();
                if (f.anim && hop != null && f.anim[hop]) { f.anim[hop].wrapMode = WrapMode.Loop; f.anim[hop].speed = 1.4f; f.anim.Play(hop); }
            }
        }

        void Update()
        {
            age += Time.deltaTime;
            // two hops outward, then shrink away
            float k = Mathf.Clamp01(age / life);
            float hopK = Mathf.Repeat(k * 2f, 1f);
            var p = start + dir * k * 3.5f + Vector3.up * Mathf.Sin(hopK * Mathf.PI) * 0.7f;
            transform.position = p;
            if (k > 0.75f) transform.localScale = Vector3.one * size * Mathf.Max(0f, 1f - (k - 0.75f) / 0.25f);
            if (age >= life) Destroy(gameObject);
        }
    }
}
