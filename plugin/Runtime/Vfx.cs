using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Our own visual effects (effects rows of kind custom), built at runtime from Unity particle systems, line and
    /// trail renderers and lights, drawn with Risk of Rain 2's own materials (effects rows of kind material). They are made
    /// to look like the Deadlock abilities; nothing is read from Deadlock's particle files.
    /// Each skill's vfx id answers moments like its sounds do: cast, loop (returns an object to keep or move), hit, trigger, end.</summary>
    public static class Vfx
    {
        static readonly Dictionary<string, Material> baseMats = new Dictionary<string, Material>();
        static readonly Dictionary<string, Material> tinted = new Dictionary<string, Material>();

        public static GameObject Play(string id, string moment, Vector3 at, float radius = 1f, Transform follow = null, Vector3? to = null)
        {
            if (string.IsNullOrEmpty(id) || id == "none") return null;
            try
            {
                switch (id)
                {
                    case "haze_dance": return HazeDance(moment, at, radius, follow);
                    case "abrams_slam": return AbramsSlam(moment, at, radius, follow);
                    case "seven_ball": return SevenBall(moment, at, follow, to);
                    case "seven_static": return SevenStatic(moment, at, radius, follow);
                    case "seven_storm": return SevenStorm(moment, at, radius, to);
                    case "infernus_afterburn": return InfernusAfterburn(moment, at, follow);
                    case "infernus_napalm": return InfernusNapalm(moment, at, radius);
                }
            }
            catch (System.Exception e) { Log.Warn($"vfx {id}/{moment}: {e.Message}"); }
            return null;
        }

        // ------------------------------------------------------------------ Haze: Bullet Dance
        static readonly Color hazePink = new Color(1f, 0.37f, 0.64f), hazeViolet = new Color(0.62f, 0.42f, 1f);

        static GameObject HazeDance(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // a spinning ring of blades around Haze's waist, three afterimage ribbons, a violet glow and a faint range ring
                    var root = Root(at, 0f, follow);
                    var spin = new GameObject("spin"); spin.transform.SetParent(root.transform, false); spin.transform.localPosition = Vector3.up * 1.0f;
                    spin.AddComponent<Spin>().DegPerSec = 900f;
                    for (int i = 0; i < 3; i++)
                    {
                        var arm = new GameObject("arm" + i); arm.transform.SetParent(spin.transform, false);
                        float a = i * 120f * Mathf.Deg2Rad;
                        arm.transform.localPosition = new Vector3(Mathf.Cos(a), (i - 1) * 0.25f, Mathf.Sin(a)) * 1.3f;
                        Trail(arm, i == 1 ? hazeViolet : hazePink, 0.35f, 0.18f);
                        Particles(arm, "mat_glow", hazePink, rate: 30, life: 0.25f, speed: 0.2f, size: 0.35f, world: true);
                    }
                    Particles(spin, "mat_spark", hazeViolet, rate: 60, life: 0.4f, speed: 4f, size: 0.08f, shapeRadius: 1.3f, world: true);
                    Light(root, hazeViolet, 8f, 2.5f, Vector3.up);
                    return root;
                }
                case "cast":
                    Burst(at, "mat_glow", hazePink, 6, 0.12f, 0.5f, 0.6f);
                    Burst(at, "mat_spark", hazeViolet, 10, 0.2f, 6f, 0.06f);
                    return null;
                case "hit":
                    Burst(at, "mat_spark", hazePink, 8, 0.25f, 7f, 0.07f);
                    Burst(at, "mat_glow", hazeViolet, 2, 0.15f, 0f, 0.8f);
                    return null;
                case "end":
                    Ring(at + Vector3.up * 0.2f, hazePink, 0.5f, 6f, 0.4f, 0.25f);
                    Flash(at + Vector3.up, hazeViolet, 10f, 0.3f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Abrams: Seismic Impact
        static readonly Color abramsRed = new Color(1f, 0.35f, 0.15f), abramsDust = new Color(0.62f, 0.52f, 0.42f);

        static GameObject AbramsSlam(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "aim":
                {
                    // the reticle: RoR2's area indicator dome (it draws where it meets the ground), a spinning dashed ring, an inner
                    // ring, a centre beam and a glow; the state moves it every frame
                    var root = Root(at, 0f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", abramsRed);
                    // embers rising from the edge of the circle, and a glow at its centre
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.4f;
                    Particles(edge, "mat_glow", abramsRed, rate: 90, life: 0.6f, speed: 1.5f, size: 0.7f, shapeRadius: radius, world: true, ring: true);
                    Particles(edge, "mat_glow", new Color(1f, 0.75f, 0.4f), rate: 10, life: 0.5f, speed: 0f, size: 2.5f);
                    var beam = Line(root.transform, abramsRed, 0.4f, Vector3.up * 0.2f, Vector3.up * 14f, "mat_tracer");
                    beam.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
                    Light(root, abramsRed, radius, 3f, Vector3.up * 2f);
                    return root;
                }
                case "cast":
                    Burst(at, "mat_smoke", abramsDust, 14, 1.0f, 6f, 2.2f, shapeRadius: 1.5f, flat: true);
                    Ring(at + Vector3.up * 0.2f, abramsDust, 0.5f, 5f, 0.5f, 0.6f);
                    return null;
                case "loop":
                {
                    // the dive: burning streaks behind Abrams
                    var root = Root(at, 0f, follow);
                    root.transform.localPosition = Vector3.up;
                    Trail(root, abramsRed, 1.6f, 0.35f);
                    Particles(root, "mat_fire", abramsRed, rate: 80, life: 0.35f, speed: 1f, size: 0.9f, shapeRadius: 0.6f, world: true);
                    return root;
                }
                case "hit":
                    Ring(at + Vector3.up * 0.3f, abramsRed, 1f, radius * 1.15f, 0.45f, 1.2f);
                    Ring(at + Vector3.up * 0.3f, new Color(1f, 0.8f, 0.5f), 0.5f, radius * 0.7f, 0.3f, 0.6f);
                    Burst(at, "mat_smoke", abramsDust, 40, 1.6f, radius * 0.9f, 3.5f, shapeRadius: 2f, flat: true);
                    Burst(at, "mat_spark", new Color(1f, 0.6f, 0.3f), 60, 1.2f, 18f, 0.2f, gravity: 2.5f, upward: true);
                    Burst(at, "mat_glow", abramsRed, 4, 0.35f, 0f, radius * 0.8f);
                    Flash(at + Vector3.up * 2f, abramsRed, radius * 2.5f, 0.5f, 8f);
                    Shake(at, radius * 3f, 3f, 0.6f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Seven: Lightning Ball, Static Charge, Thunderhead
        static readonly Color sevenBlue = new Color(0.5f, 0.83f, 1f), sevenWhite = new Color(0.85f, 0.95f, 1f);

        static GameObject SevenBall(string moment, Vector3 at, Transform follow, Vector3? to)
        {
            switch (moment)
            {
                case "loop":
                {
                    // a white-hot core, a blue halo, crackling arcs around it and a light
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_glow", sevenWhite, rate: 40, life: 0.2f, speed: 0f, size: 1.4f);
                    Particles(root, "mat_glow", sevenBlue, rate: 20, life: 0.4f, speed: 0f, size: 3.2f);
                    Particles(root, "mat_spark", sevenBlue, rate: 50, life: 0.3f, speed: 5f, size: 0.06f, shapeRadius: 0.5f, world: true);
                    var c = root.AddComponent<Crackle>(); c.Color = sevenBlue; c.Radius = 2.2f; c.Count = 4;
                    Light(root, sevenBlue, 12f, 4f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                    if (to.HasValue) Bolt(at, to.Value, sevenBlue, 0.3f, 0.12f);
                    return null;
                case "end":
                    Burst(at, "mat_glow", sevenWhite, 3, 0.3f, 0f, 5f);
                    Burst(at, "mat_spark", sevenBlue, 40, 0.5f, 14f, 0.1f);
                    Ring(at, sevenBlue, 0.5f, 7f, 0.35f, 0.4f, vertical: true);
                    Flash(at, sevenBlue, 14f, 0.3f, 6f);
                    return null;
            }
            return null;
        }

        static GameObject SevenStatic(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                {
                    // the charge: a ring that closes in on the target while it counts down, sparks and a pulsing light
                    var root = Root(at, 0f, follow);
                    var ring = new GameObject("ring"); ring.transform.SetParent(root.transform, false); ring.transform.localPosition = Vector3.up * 0.4f;
                    Particles(ring, "mat_glow", sevenBlue, rate: 120, life: 0.25f, speed: 0f, size: 0.5f, shapeRadius: radius, ring: true);
                    ring.AddComponent<Spin>().DegPerSec = 200f;
                    ring.AddComponent<Shrink>().To = 0.12f;
                    Particles(root, "mat_spark", sevenBlue, rate: 40, life: 0.3f, speed: 3f, size: 0.07f, shapeRadius: 0.8f, world: true);
                    Particles(root, "mat_glow", sevenWhite, rate: 12, life: 0.25f, speed: 0f, size: 1.2f);
                    var c = root.AddComponent<Crackle>(); c.Color = sevenBlue; c.Radius = 1.4f; c.Count = 2;
                    Light(root, sevenBlue, 6f, 3f, Vector3.up).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                {
                    // a thunderbolt from the sky like Risk of Rain 2's lightning strikes, a flash and a shock ring
                    var sky = at + Vector3.up * 40f + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                    Bolt(sky, at, sevenWhite, 1.4f, 0.35f, 14, 2.5f);
                    Bolt(sky, at, sevenBlue, 2.6f, 0.25f, 10, 4f);
                    Ring(at + Vector3.up * 0.3f, sevenBlue, 0.5f, radius, 0.35f, 0.7f);
                    Burst(at, "mat_spark", sevenWhite, 50, 0.6f, 16f, 0.12f, upward: true);
                    Burst(at, "mat_glow", sevenBlue, 3, 0.3f, 0f, radius * 0.9f);
                    Flash(at + Vector3.up * 3f, sevenWhite, radius * 3f, 0.25f, 10f);
                    Shake(at, radius * 3f, 1.5f, 0.3f);
                    return null;
                }
            }
            return null;
        }

        static GameObject SevenStorm(string moment, Vector3 at, float radius, Vector3? to)
        {
            const float cloudHeight = 16f;
            switch (moment)
            {
                case "loop":
                {
                    // the thunderhead: a dark churning cloud deck high over the zone, blue light flickering inside it, a few
                    // rain streaks, and a dashed ring on the ground marking the storm's edge
                    var root = Root(at, 0f, null);
                    var cloud = new GameObject("cloud"); cloud.transform.SetParent(root.transform, false); cloud.transform.localPosition = Vector3.up * cloudHeight;
                    Particles(cloud, "mat_smoke", new Color(0.16f, 0.18f, 0.24f, 0.75f), rate: 18, life: 3f, speed: 0.3f, size: radius * 0.3f, shapeRadius: radius * 0.8f, flat: true);
                    Particles(cloud, "mat_smoke", new Color(0.28f, 0.33f, 0.45f, 0.55f), rate: 10, life: 2.5f, speed: 0.2f, size: radius * 0.22f, shapeRadius: radius * 0.6f, flat: true);
                    cloud.AddComponent<Spin>().DegPerSec = 12f;
                    var flash = Light(cloud, sevenBlue, radius * 1.2f, 0f, Vector3.down * 2f);
                    flash.gameObject.AddComponent<StormFlash>();
                    return root;
                }
                case "hit":
                {
                    if (!to.HasValue) return null;
                    var from = new Vector3(to.Value.x + Random.Range(-2f, 2f), at.y + cloudHeight, to.Value.z + Random.Range(-2f, 2f));
                    Bolt(from, to.Value, sevenWhite, 0.35f, 0.2f, 12, 1.5f);
                    Bolt(from, to.Value, sevenBlue, 0.8f, 0.15f, 9, 2.2f);
                    Burst(to.Value, "mat_spark", sevenWhite, 14, 0.4f, 9f, 0.08f, upward: true);
                    Flash(to.Value + Vector3.up * 2f, sevenBlue, 8f, 0.15f, 1.5f);
                    return null;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Infernus: Afterburn, Napalm
        static readonly Color fireOrange = new Color(1f, 0.48f, 0.12f), fireYellow = new Color(1f, 0.8f, 0.35f);

        static GameObject InfernusAfterburn(string moment, Vector3 at, Transform follow)
        {
            var body = follow ? follow.GetComponent<CharacterBody>() : null;
            if (!body) return null;
            switch (moment)
            {
                case "hit":
                    if (!body.GetComponent<EmberFx>()) body.gameObject.AddComponent<EmberFx>();
                    return null;
                case "trigger":
                    if (!body.GetComponent<EmberFx>()) body.gameObject.AddComponent<EmberFx>();
                    // ignition: a flare of fire up the whole body
                    Burst(body.corePosition, "mat_fire", fireOrange, 24, 0.6f, 5f, 1.4f * Mathf.Max(1f, body.radius), upward: true);
                    Burst(body.corePosition, "mat_glow", fireYellow, 3, 0.25f, 0f, 3f * Mathf.Max(1f, body.radius));
                    Flash(body.corePosition, fireOrange, 8f, 0.3f, 5f);
                    return null;
            }
            return null;
        }

        static GameObject InfernusNapalm(string moment, Vector3 at, float radius)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the flask in flight: a hot glowing core, a fire trail and drips
                    var root = Root(at, 0f, null);
                    Particles(root, "mat_glow", fireYellow, rate: 30, life: 0.15f, speed: 0f, size: 0.9f);
                    Particles(root, "mat_fire", fireOrange, rate: 60, life: 0.45f, speed: 0.5f, size: 0.7f, world: true);
                    Trail(root, fireOrange, 0.5f, 0.3f);
                    Light(root, fireOrange, 8f, 3f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                    Ring(at + Vector3.up * 0.25f, fireOrange, 0.5f, radius, 0.4f, 0.6f);
                    Burst(at, "mat_fire", fireOrange, 30, 0.9f, radius * 1.2f, 1.6f, shapeRadius: radius * 0.4f, upward: true);
                    Burst(at, "mat_fire", fireYellow, 12, 1.4f, 2f, 1.2f, shapeRadius: radius * 0.8f, flat: true);
                    Burst(at, "mat_smoke", new Color(0.2f, 0.15f, 0.12f, 0.8f), 10, 1.6f, 2f, 2.5f, shapeRadius: radius * 0.5f, upward: true);
                    Flash(at + Vector3.up, fireOrange, radius * 3f, 0.35f, 6f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ building blocks

        /// <summary>A RoR2 material (effects row of kind material), copied and tinted.</summary>
        public static Material Mat(string id, Color tint)
        {
            string key = id + "|" + ColorUtility.ToHtmlStringRGBA(tint);
            if (tinted.TryGetValue(key, out var m) && m) return m;
            if (!baseMats.TryGetValue(id, out var b) || !b)
            {
                var row = Sheet.Effect(id);
                try { if (row != null && row.Addressable != "none") b = Addressables.LoadAssetAsync<Material>(row.Addressable).WaitForCompletion(); }
                catch (System.Exception e) { Log.Warn($"vfx material {id}: {e.Message}"); }
                if (!b) b = new Material(Shader.Find("Sprites/Default"));
                baseMats[id] = b;
            }
            m = new Material(b);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", tint);
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            tinted[key] = m;
            return m;
        }

        public static void Preload()
        {
            foreach (var e in EffectRow.All) if (e.Kind == "material") Mat(e.Id, Color.white);
        }

        static GameObject Root(Vector3 at, float life, Transform follow)
        {
            var go = new GameObject("DlrVfx");
            go.transform.position = at;
            var l = go.AddComponent<VfxLife>();
            l.Follow = follow; l.Following = follow; l.Life = life;
            return go;
        }

        /// <summary>A particle system on parent: continuous (rate) or a one-off burst (count).</summary>
        static ParticleSystem Particles(GameObject parent, string mat, Color c, float rate = 0f, int count = 0, float life = 0.5f,
            float speed = 1f, float size = 0.5f, float shapeRadius = 0f, bool world = false, bool flat = false, bool upward = false,
            bool down = false, float gravity = 0f, float stretch = 0f, bool ring = false)
        {
            var go = new GameObject("ps");
            go.transform.SetParent(parent.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = count > 0 ? 0.1f : 1f;
            main.loop = count == 0;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = c;
            main.gravityModifier = gravity;
            main.maxParticles = 500;
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var em = ps.emission;
            em.rateOverTime = rate;
            if (count > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.enabled = true;
            if (ring) { shape.shapeType = ParticleSystemShapeType.Circle; shape.rotation = new Vector3(90f, 0, 0); shape.radiusThickness = 0f; }
            else if (flat) { shape.shapeType = ParticleSystemShapeType.Circle; shape.rotation = new Vector3(90f, 0, 0); }
            else if (upward) { shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.rotation = new Vector3(-90f, 0, 0); }
            else if (down) { shape.shapeType = ParticleSystemShapeType.Circle; shape.rotation = new Vector3(90f, 0, 0); }
            else shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.01f, shapeRadius);
            if (down)
            {
                var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f); vel.y = new ParticleSystem.MinMaxCurve(-speed);
                main.startSpeed = 0f;
            }
            // fade in fast, out slow; shrink at the end
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.4f)));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mat(mat, c);
            if (stretch > 0f) { r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = stretch; }
            ps.Play();
            return ps;
        }

        /// <summary>A one-shot burst of particles at a point that cleans itself up.</summary>
        public static void Burst(Vector3 at, string mat, Color c, int count, float life, float speed, float size, float shapeRadius = 0.2f,
            bool flat = false, bool upward = false, float gravity = 0f)
        {
            var root = Root(at, life + 0.2f, null);
            Particles(root, mat, c, count: count, life: life, speed: speed, size: size, shapeRadius: shapeRadius, world: true, flat: flat, upward: upward, gravity: gravity);
        }

        static TrailRenderer Trail(GameObject parent, Color c, float width, float time)
        {
            var t = parent.AddComponent<TrailRenderer>();
            t.sharedMaterial = Mat("mat_tracer", c);
            t.time = time; t.minVertexDistance = 0.1f;
            t.widthCurve = new AnimationCurve(new Keyframe(0, width), new Keyframe(1, 0));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            t.colorGradient = g;
            return t;
        }

        static LineRenderer Line(Transform parent, Color c, float width, Vector3 a, Vector3 b, string mat = "mat_tracer")
        {
            var go = new GameObject("line"); go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.sharedMaterial = Mat(mat, c);
            lr.startColor = lr.endColor = c;
            lr.widthMultiplier = width;
            lr.positionCount = 2; lr.SetPosition(0, a); lr.SetPosition(1, b);
            return lr;
        }

        /// <summary>A flat circle (dashed: every other segment) in parent's space.</summary>
        static LineRenderer Circle(Transform parent, Color c, float radius, float width, int segments = 48, bool dashed = false)
        {
            var go = new GameObject(dashed ? "dashes" : "circle"); go.transform.SetParent(parent, false);
            if (dashed)
            {
                for (int i = 0; i < segments; i += 2)
                {
                    float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                    Line(go.transform, c, width, new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius, new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius);
                }
                return go.GetComponentInChildren<LineRenderer>();
            }
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.loop = true;
            lr.sharedMaterial = Mat("mat_tracer", c);
            lr.startColor = lr.endColor = c;
            lr.widthMultiplier = width;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++) { float a = i * Mathf.PI * 2f / segments; lr.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius); }
            return lr;
        }

        /// <summary>An expanding, fading ring (a shockwave), flat on the ground or facing the camera-ish (vertical).</summary>
        public static void Ring(Vector3 at, Color c, float from, float to, float life, float width, bool vertical = false)
        {
            var root = Root(at, life, null);
            var lr = Circle(root.transform, c, 1f, width, 64);
            if (vertical) lr.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var g = lr.gameObject.AddComponent<RingGrow>();
            g.From = from; g.To = to; g.Life = life; g.Width = width;
            lr.transform.localScale = Vector3.one * from;
        }

        /// <summary>A jagged lightning bolt between two points that flickers and fades.</summary>
        public static void Bolt(Vector3 a, Vector3 b, Color c, float width, float life, int segments = 8, float jitter = 0f)
        {
            var root = Root(a, life, null);
            var lr = Line(root.transform, c, width, Vector3.zero, b - a, "mat_lightning");
            lr.useWorldSpace = true;
            lr.SetPosition(0, a); lr.SetPosition(1, b);
            var bolt = lr.gameObject.AddComponent<BoltFx>();
            bolt.A = a; bolt.B = b; bolt.Segments = segments; bolt.Jitter = jitter > 0f ? jitter : Mathf.Max(0.3f, Vector3.Distance(a, b) * 0.08f);
            bolt.Life = life; bolt.Width = width;
        }

        public static void Flash(Vector3 at, Color c, float range, float life, float intensity = 4f)
        {
            var root = Root(at, life, null);
            var l = Light(root, c, range, intensity, Vector3.zero);
            var f = l.gameObject.AddComponent<FadeLight>(); f.Life = life; f.From = intensity;
        }

        static Light Light(GameObject parent, Color c, float range, float intensity, Vector3 offset)
        {
            var go = new GameObject("light"); go.transform.SetParent(parent.transform, false); go.transform.localPosition = offset;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            return l;
        }

        static void Shake(Vector3 at, float radius, float amplitude, float duration)
        {
            ShakeEmitter.CreateSimpleShakeEmitter(at, new Wave { amplitude = amplitude, frequency = 30f, cycleOffset = 0f }, duration, radius, true);
        }

        // ------------------------------------------------------------------ small behaviours

        /// <summary>Lifetime and follow: life 0 lives until destroyed by its owner; a root whose followed object is gone stops
        /// emitting and fades out.</summary>
        sealed class VfxLife : MonoBehaviour
        {
            public Transform Follow; public bool Following; public float Life;
            float age;
            void LateUpdate()
            {
                age += Time.deltaTime;
                if (Following)
                {
                    if (Follow) transform.position = Follow.position;
                    else { Stop(gameObject); return; }
                }
                if (Life > 0f && age >= Life) Destroy(gameObject);
            }
        }

        /// <summary>Stop a looping effect: particles stop emitting and the object goes once they have faded.</summary>
        public static void Stop(GameObject go, float linger = 1.5f)
        {
            if (!go) return;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            foreach (var l in go.GetComponentsInChildren<Light>()) { var f = l.gameObject.AddComponent<FadeLight>(); f.Life = 0.3f; f.From = l.intensity; }
            foreach (var lr in go.GetComponentsInChildren<LineRenderer>()) lr.enabled = false;
            var life = go.GetComponent<VfxLife>();
            if (life) { life.Following = false; }
            Object.Destroy(go, linger);
        }

        sealed class Spin : MonoBehaviour
        {
            public float DegPerSec;
            void Update() => transform.Rotate(0f, DegPerSec * Time.deltaTime, 0f, Space.Self);
        }

        sealed class Pulse : MonoBehaviour
        {
            public float Speed = 3f;
            void Update() => transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.time * Speed * Mathf.PI));
        }

        sealed class Shrink : MonoBehaviour
        {
            public float To = 0.1f, Over = 1.5f;
            float age;
            void Update() { age += Time.deltaTime; transform.localScale = Vector3.one * Mathf.Lerp(1f, To, Mathf.Clamp01(age / Over)); }
        }

        sealed class RingGrow : MonoBehaviour
        {
            public float From, To, Life, Width;
            float age; LineRenderer lr;
            void Awake() => lr = GetComponent<LineRenderer>();
            void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / Life), e = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.one * Mathf.Lerp(From, To, e);
                // the line's width is in world units, so undo the scale and thin it out as it fades
                lr.widthMultiplier = Width * (1f - t) / Mathf.Max(0.01f, transform.localScale.x);
            }
        }

        sealed class BoltFx : MonoBehaviour
        {
            public Vector3 A, B; public int Segments; public float Jitter, Life, Width;
            float age, next; LineRenderer lr;
            void Awake() => lr = GetComponent<LineRenderer>();
            void Update()
            {
                age += Time.deltaTime;
                lr.widthMultiplier = Width * Mathf.Clamp01(1f - age / Life);
                if (age < next) return;
                next = age + 0.04f;
                lr.positionCount = Segments + 1;
                var side = Vector3.Cross(B - A, Vector3.up).normalized;
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                var up = Vector3.Cross(side, B - A).normalized;
                for (int i = 0; i <= Segments; i++)
                {
                    float t = (float)i / Segments;
                    var p = Vector3.Lerp(A, B, t);
                    if (i > 0 && i < Segments) p += (side * Random.Range(-1f, 1f) + up * Random.Range(-1f, 1f)) * Jitter * Mathf.Sin(t * Mathf.PI);
                    lr.SetPosition(i, p);
                }
            }
        }

        /// <summary>Short arcs leaping around a centre (Lightning Ball, Static Charge).</summary>
        sealed class Crackle : MonoBehaviour
        {
            public Color Color; public float Radius = 2f; public int Count = 3;
            float next;
            void Update()
            {
                if (Time.time < next) return;
                next = Time.time + Random.Range(0.05f, 0.12f);
                for (int i = 0; i < Count; i++)
                {
                    var end = transform.position + Random.onUnitSphere * Radius * Random.Range(0.5f, 1f);
                    Bolt(transform.position, end, Color, 0.12f, 0.08f, 5, Radius * 0.2f);
                }
            }
        }

        sealed class Flicker : MonoBehaviour
        {
            Light l; float base_;
            void Awake() { l = GetComponent<Light>(); base_ = l.intensity; }
            void Update() { if (l) l.intensity = base_ * Random.Range(0.6f, 1.2f); }
        }

        /// <summary>Lightning flickering inside the thunderhead.</summary>
        sealed class StormFlash : MonoBehaviour
        {
            Light l; float next, until;
            void Awake() => l = GetComponent<Light>();
            void Update()
            {
                if (!l) return;
                if (Time.time >= next) { next = Time.time + Random.Range(0.15f, 0.6f); until = Time.time + Random.Range(0.05f, 0.12f); }
                l.intensity = Time.time < until ? Random.Range(1.2f, 2.5f) : 0.2f;
            }
        }

        sealed class FadeLight : MonoBehaviour
        {
            public float Life = 0.3f, From = 4f;
            float age; Light l;
            void Awake() => l = GetComponent<Light>();
            void Update() { age += Time.deltaTime; if (l) l.intensity = From * Mathf.Clamp01(1f - age / Life); }
        }

        /// <summary>Afterburn on an enemy: embers that grow with the build-up stacks, then flames and a glow while it burns;
        /// it goes away by itself once the enemy has neither.</summary>
        sealed class EmberFx : MonoBehaviour
        {
            CharacterBody body; ParticleSystem embers, flames; Light glow; float idle; GameObject root;
            void Start()
            {
                body = GetComponent<CharacterBody>();
                float r = Mathf.Max(0.6f, body ? body.radius : 1f);
                root = Root(body ? body.corePosition : transform.position, 0f, body && body.coreTransform ? body.coreTransform : transform);
                embers = Particles(root, "mat_spark", fireOrange, rate: 0f, life: 0.8f, speed: 1.5f, size: 0.08f, shapeRadius: r, world: true, upward: true, gravity: -0.3f);
                flames = Particles(root, "mat_fire", fireOrange, rate: 0f, life: 0.6f, speed: 1.5f, size: 0.9f * r, shapeRadius: r * 0.8f, world: true, upward: true);
                glow = Light(root, fireOrange, 4f * r, 0f, Vector3.zero);
            }
            void Update()
            {
                if (!body || !body.healthComponent || !body.healthComponent.alive) { Done(); return; }
                var row = Sheet.Buff("afterburn_buildup");
                float build = row != null && row.TriggerStacks > 0 ? Buffs.Count(body, row.Id) / row.TriggerStacks : 0f;
                var dc = DotController.FindDotController(body.gameObject);
                bool burning = dc && dc.HasDotActive(DotController.DotIndex.Burn);
                var e = embers.emission; e.rateOverTime = build * 60f;
                var f = flames.emission; f.rateOverTime = burning ? 45f : build * 6f;
                glow.intensity = burning ? Random.Range(2.5f, 4f) : build * 1.5f;
                if (build <= 0f && !burning) { idle += Time.deltaTime; if (idle > 1f) Done(); } else idle = 0f;
            }
            void Done() { Stop(root); Destroy(this); }
            void OnDestroy() { if (root) Stop(root); }
        }
    }
}
