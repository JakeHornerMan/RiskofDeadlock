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

        public static GameObject Play(string id, string moment, Vector3 at, float radius = 1f, Transform follow = null, Vector3? to = null, float angle = 0f)
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
                    case "infernus_napalm": return InfernusNapalm(moment, at, radius, follow);
                    case "infernus_trail": return InfernusTrail(moment, at, radius, follow);
                    case "infernus_bomb": return InfernusBomb(moment, at, radius, follow);
                    case "infernus_pulse": return InfernusPulse(moment, at, radius, follow);
                    case "haze_phantom": return HazePhantom(moment, at, radius, to);
                    case "haze_dagger": return HazeDagger(moment, at, follow);
                    case "abrams_haymaker": return AbramsHaymaker(moment, at, radius, follow, to, angle);
                    case "seven_surge": return SevenSurge(moment, at, follow);
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

        static float nextBallImpact;

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
                {
                    if (!to.HasValue) return null;
                    // a clear lightning strike from the ball to the enemy: white core, blue glow, sparks and a flash on it,
                    // and RoR2's lightning impact (rate-limited, it is bright)
                    Bolt(at, to.Value, sevenWhite, 0.25f, 0.16f, 10, 0.6f);
                    Bolt(at, to.Value, sevenBlue, 0.7f, 0.12f, 8, 0.9f);
                    Burst(to.Value, "mat_spark", sevenWhite, 8, 0.3f, 7f, 0.07f);
                    Flash(to.Value, sevenBlue, 5f, 0.12f, 2f);
                    if (Time.time >= nextBallImpact) { nextBallImpact = Time.time + 0.1f; Fx.Impact("storm_strike", to.Value); }
                    return null;
                }
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
            const float cloudHeight = 35f;
            var stormDark = new Color(0.07f, 0.08f, 0.12f, 0.95f);
            switch (moment)
            {
                case "loop":
                {
                    // the thunderhead: a thick, dark, churning cloud deck over the zone (RoR2's opaque dust, so it really is
                    // dark), a dim haze under it that darkens the area, lightning flickering inside the clouds, and bolts
                    // cracking down from the cloud base onto the ground all over the storm
                    var root = Root(at, 0f, null);
                    var cloud = new GameObject("cloud"); cloud.transform.SetParent(root.transform, false); cloud.transform.localPosition = Vector3.up * cloudHeight;
                    // build the deck at once, then keep it churning
                    var deck = Particles(cloud, "mat_dust", stormDark, rate: 45, life: 4f, speed: 0.4f, size: radius * 0.45f, shapeRadius: radius * 1.05f, flat: true);
                    deck.Emit(70);
                    var belly = Particles(cloud, "mat_dust", new Color(0.13f, 0.15f, 0.22f, 0.9f), rate: 30, life: 3.5f, speed: 0.3f, size: radius * 0.35f, shapeRadius: radius * 0.9f, flat: true);
                    belly.transform.localPosition = Vector3.down * 2.5f;
                    belly.Emit(40);
                    var top = Particles(cloud, "mat_dust", new Color(0.2f, 0.22f, 0.3f, 0.85f), rate: 15, life: 4f, speed: 0.2f, size: radius * 0.4f, shapeRadius: radius * 0.75f, flat: true);
                    top.transform.localPosition = Vector3.up * 3f;
                    top.Emit(20);
                    cloud.AddComponent<Spin>().DegPerSec = 10f;
                    // the dim haze filling the storm, from the ground up to the clouds
                    var haze = new GameObject("haze"); haze.transform.SetParent(root.transform, false); haze.transform.localPosition = Vector3.up * (cloudHeight * 0.45f);
                    var hz = Particles(haze, "mat_dust", new Color(0.05f, 0.06f, 0.09f, 0.22f), rate: 25, life: 4f, speed: 0.2f, size: radius * 0.5f, shapeRadius: radius * 0.85f, flat: true);
                    hz.Emit(30);
                    // lightning lighting the clouds from inside
                    var flash = Light(cloud, sevenBlue, radius * 1.4f, 0f, Vector3.down * 3f);
                    flash.gameObject.AddComponent<StormFlash>();
                    // bolts striking the ground at random inside the storm (looks only; the damage strikes are "hit")
                    var amb = root.AddComponent<StormBolts>();
                    amb.Radius = radius * 0.9f; amb.Height = cloudHeight - 2f;
                    return root;
                }
                case "hit":
                {
                    if (!to.HasValue) return null;
                    var from = new Vector3(to.Value.x + Random.Range(-2f, 2f), at.y + cloudHeight - 2f, to.Value.z + Random.Range(-2f, 2f));
                    StrikeDown(from, to.Value, true);
                    return null;
                }
            }
            return null;
        }

        /// <summary>One lightning strike from the cloud base down to a point: a white core, a blue glow around it, sparks
        /// and a short light on the ground.</summary>
        static void StrikeDown(Vector3 from, Vector3 to, bool big)
        {
            Bolt(from, to, sevenWhite, big ? 0.35f : 0.22f, 0.16f, 12, big ? 1.6f : 1.2f);
            Bolt(from, to, sevenBlue, big ? 0.9f : 0.55f, 0.12f, 9, big ? 2.2f : 1.6f);
            Burst(to, "mat_spark", sevenWhite, big ? 14 : 6, 0.35f, 8f, 0.07f, upward: true);
            Flash(to + Vector3.up * 1.5f, sevenBlue, big ? 9f : 6f, 0.12f, big ? 2.2f : 1.4f);
        }

        /// <summary>Thunderhead's extra bolts: every 0.15–0.45 s a bolt from the cloud to a random spot on the ground inside the storm.</summary>
        sealed class StormBolts : MonoBehaviour
        {
            public float Radius, Height;
            float next;
            void Update()
            {
                if (Time.time < next) return;
                next = Time.time + Random.Range(0.15f, 0.45f);
                var c = Random.insideUnitCircle * Radius;
                var top = transform.position + new Vector3(c.x, Height, c.y);
                var ground = top + Vector3.down * Height;
                if (Physics.Raycast(top, Vector3.down, out var hit, Height * 2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) ground = hit.point;
                StrikeDown(top, ground, false);
            }
        }

        // ------------------------------------------------------------------ Infernus: Afterburn, Napalm
        static readonly Color fireOrange = new Color(1f, 0.48f, 0.12f), fireYellow = new Color(1f, 0.8f, 0.35f), bombYellow = new Color(1f, 0.9f, 0.15f);

        static GameObject InfernusAfterburn(string moment, Vector3 at, Transform follow)
        {
            if (moment == "muzzle")
            {
                // every Flame Gun shot: a hot flash and a puff of smoke rolling off the muzzle
                Burst(at, "mat_glow", fireYellow, 1, 0.06f, 0f, 0.9f);
                Burst(at, "mat_fire_puff", fireOrange, 3, 0.15f, 3f, 0.35f, shapeRadius: 0.05f);
                Burst(at, "mat_dust", new Color(0.32f, 0.28f, 0.25f, 0.45f), 2, 0.9f, 1.2f, 0.6f, shapeRadius: 0.1f, upward: true);
                Flash(at, fireOrange, 4f, 0.06f, 2f);
                return null;
            }
            var body = follow ? follow.GetComponent<CharacterBody>() : null;
            if (!body) return null;
            switch (moment)
            {
                case "hit":
                    if (!body.GetComponent<EmberFx>()) body.gameObject.AddComponent<EmberFx>();
                    return null;
                case "trigger":
                    if (!body.GetComponent<EmberFx>()) body.gameObject.AddComponent<EmberFx>();
                    // ignition: RoR2's Helfire flare, then a column of big flames up the whole body
                    Fx.Impact("helfire_ignite", body.corePosition);
                    Burst(body.corePosition, "mat_fire_large", fireOrange, 18, 0.7f, 4f, 1.8f * Mathf.Max(1f, body.radius), shapeRadius: body.radius * 0.6f, upward: true);
                    Burst(body.corePosition, "mat_fire_puff", fireYellow, 10, 0.5f, 6f, 0.9f * Mathf.Max(1f, body.radius), upward: true);
                    Burst(body.corePosition, "mat_glow", fireYellow, 3, 0.25f, 0f, 3f * Mathf.Max(1f, body.radius));
                    Flash(body.corePosition, fireOrange, 10f, 0.35f, 6f);
                    return null;
            }
            return null;
        }

        static GameObject InfernusNapalm(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // the flask in flight, riding on the lob: Molotov's bottle (a glowing ball if it can't load), a hot core,
                    // a fire trail and drips
                    var root = Root(at, 0f, follow);
                    var bottle = Fx.Ghost("flask", at, Quaternion.identity);
                    if (!bottle)
                    {
                        bottle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Object.Destroy(bottle.GetComponent<Collider>());
                        bottle.transform.localScale = Vector3.one * 0.45f;
                        bottle.GetComponent<Renderer>().sharedMaterial = Mat("mat_glow", fireOrange);
                    }
                    bottle.transform.SetParent(root.transform, false);
                    bottle.transform.localPosition = Vector3.zero;
                    bottle.AddComponent<Spin>().DegPerSec = 540f;
                    Particles(root, "mat_glow", fireYellow, rate: 30, life: 0.15f, speed: 0f, size: 0.9f);
                    Particles(root, "mat_fire_large", fireOrange, rate: 50, life: 0.45f, speed: 0.5f, size: 0.9f, world: true);
                    Trail(root, fireOrange, 0.5f, 0.3f);
                    Light(root, fireOrange, 8f, 3f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "aim":
                {
                    // the aim marker (owner only): the flight path as a thin line and, where it lands, a circle the size of
                    // the splash (the area dome, drawn where it meets the ground) with embers on its edge; the state moves it
                    // and redraws the path every frame (SetArc)
                    var root = Root(at, 0f, null);
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(root.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", fireOrange);
                    var edge = new GameObject("edge"); edge.transform.SetParent(root.transform, false); edge.transform.localPosition = Vector3.up * 0.3f;
                    Particles(edge, "mat_glow", fireOrange, rate: 40, life: 0.4f, speed: 1f, size: 0.4f, shapeRadius: radius, world: true, ring: true);
                    var arc = Line(root.transform, fireYellow, 0.12f, Vector3.zero, Vector3.zero);
                    arc.name = "arc"; arc.useWorldSpace = true;
                    arc.widthCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.15f, 1f), new Keyframe(1, 1f));
                    return root;
                }
                case "hit":
                    FireRing(at, radius, 0.35f);
                    Burst(at, "mat_fire_large", fireOrange, 26, 0.9f, radius * 1.1f, 2.2f, shapeRadius: radius * 0.4f, upward: true);
                    Burst(at, "mat_fire_puff", fireYellow, 14, 0.6f, radius * 1.5f, 1.2f, shapeRadius: radius * 0.3f, upward: true);
                    Burst(at, "mat_dust", new Color(0.18f, 0.13f, 0.1f, 0.7f), 8, 1.8f, 1.5f, 3f, shapeRadius: radius * 0.5f, upward: true);
                    GroundFire(at, radius * 0.8f, 2.5f);
                    Flash(at + Vector3.up, fireOrange, radius * 3f, 0.4f, 7f);
                    return null;
            }
            return null;
        }

        /// <summary>A burning patch of ground (RoR2 Gasoline fire with big flames licking up) that lasts life seconds.</summary>
        static void GroundFire(Vector3 at, float radius, float life)
        {
            var root = Root(at + Vector3.up * 0.1f, life + 1.2f, null);
            Particles(root, "mat_gas_fire", fireOrange, rate: 10f * radius, life: 0.9f, speed: 0.3f, size: 1.1f, shapeRadius: radius, world: true, flat: true);
            Particles(root, "mat_fire_large", fireOrange, rate: 5f * radius, life: 0.7f, speed: 1.6f, size: 1.0f, shapeRadius: radius * 0.8f, world: true, flat: true);
            Particles(root, "mat_spark", fireYellow, rate: 6f * radius, life: 1f, speed: 2f, size: 0.07f, shapeRadius: radius, world: true, flat: true, gravity: -0.4f);
            Light(root, fireOrange, radius * 2.5f, 1.5f, Vector3.up).gameObject.AddComponent<Flicker>();
            root.AddComponent<StopAfter>().After = life;
        }

        /// <summary>A ring of flame racing out along the ground to radius.</summary>
        static void FireRing(Vector3 at, float radius, float life)
        {
            var root = Root(at + Vector3.up * 0.3f, life + 0.8f, null);
            int n = Mathf.Clamp((int)(radius * 7f), 24, 140);
            Particles(root, "mat_fire_large", fireOrange, count: n, life: life + 0.3f, speed: radius / life, size: Mathf.Clamp(radius * 0.22f, 1.4f, 3f), shapeRadius: 0.4f, world: true, ring: true);
            Particles(root, "mat_fire_puff", fireYellow, count: n, life: life, speed: radius / life * 0.9f, size: 1.1f, shapeRadius: 0.4f, world: true, ring: true);
            Particles(root, "mat_gas_fire", fireOrange, count: n / 2, life: life + 0.5f, speed: radius / life * 0.8f, size: 1.6f, shapeRadius: 0.4f, world: true, ring: true);
            Ring(at + Vector3.up * 0.25f, fireOrange, 0.5f, radius, life + 0.1f, 1.2f);
            Flash(at + Vector3.up, fireOrange, radius * 1.5f, life, 4f);
        }

        static GameObject InfernusTrail(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // flames licking around his legs and a heat glow while he runs
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_fire_large", fireOrange, rate: 45, life: 0.45f, speed: 1.5f, size: 1.1f, shapeRadius: 0.6f, world: true, upward: true);
                    Particles(root, "mat_spark", fireYellow, rate: 40, life: 0.6f, speed: 3f, size: 0.08f, shapeRadius: 0.5f, world: true, upward: true, gravity: -0.3f);
                    Light(root, fireOrange, 7f, 3f, Vector3.up).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                    // one patch of burning ground left behind (radius = how long it burns)
                    GroundFire(at, 1.6f, radius);
                    return null;
            }
            return null;
        }

        /// <summary>Move an aim marker made by a moment "aim" (Napalm) to where the throw lands and redraw its path.</summary>
        public static void SetArc(GameObject marker, List<Vector3> path, Vector3 landing)
        {
            if (!marker) return;
            marker.transform.position = landing;
            foreach (var lr in marker.GetComponentsInChildren<LineRenderer>())
            {
                if (lr.name != "arc") continue;
                lr.positionCount = path.Count;
                for (int i = 0; i < path.Count; i++) lr.SetPosition(i, path[i]);
            }
        }

        static GameObject InfernusBomb(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "aim":
                {
                    // the blast's reach while the fuse burns, on whoever holds the equipment: a yellow circle on the ground
                    // at the full radius (the area dome, drawn only where it meets the ground, so it hugs slopes), embers
                    // rising from its edge, and a second dome shrinking once a second, whose outline on the ground sweeps
                    // inward as a countdown; it glows hotter toward the blast (no flat line circles: they float on slopes)
                    var root = Root(at, 0f, follow);
                    var feet = new GameObject("feet"); feet.transform.SetParent(root.transform, false);
                    if (follow) feet.transform.localPosition = at - follow.position;
                    var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(dome.GetComponent<Collider>());
                    dome.transform.SetParent(feet.transform, false);
                    dome.transform.localScale = Vector3.one * radius * 2f;
                    dome.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", bombYellow);
                    var rise = new GameObject("rise"); rise.transform.SetParent(feet.transform, false); rise.transform.localPosition = Vector3.up * 0.3f;
                    Particles(rise, "mat_glow", bombYellow, rate: 8f * radius, life: 0.7f, speed: 2f, size: 0.6f, shapeRadius: radius, world: true, ring: true);
                    var sweep = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(sweep.GetComponent<Collider>());
                    sweep.transform.SetParent(feet.transform, false);
                    sweep.GetComponent<MeshRenderer>().sharedMaterial = Mat("mat_area", fireYellow);
                    var p = sweep.AddComponent<DomeSweep>(); p.From = radius * 2f; p.To = radius * 0.2f; p.Period = 1f;
                    var l = Light(feet, bombYellow, radius, 1f, Vector3.up * 2f);
                    var g = l.gameObject.AddComponent<GrowLight>(); g.To = 6f; g.Over = 3f;
                    return root;
                }
                case "loop":
                {
                    // the fuse: fire and embers gathering on him, a glow growing hotter
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_fire_large", fireOrange, rate: 35, life: 0.5f, speed: 1.2f, size: 1.3f, shapeRadius: 0.9f, world: true, upward: true);
                    Particles(root, "mat_fire_puff", fireYellow, rate: 20, life: 0.6f, speed: 1.5f, size: 0.5f, shapeRadius: 1.2f, world: true);
                    Particles(root, "mat_spark", fireYellow, rate: 60, life: 0.7f, speed: 4f, size: 0.09f, shapeRadius: 0.6f, world: true);
                    var l = Light(root, fireOrange, 10f, 1f, Vector3.zero);
                    var g = l.gameObject.AddComponent<GrowLight>(); g.To = 8f; g.Over = 3f;
                    return root;
                }
                case "hit":
                    FireRing(at, radius, 0.45f);
                    Burst(at + Vector3.up, "mat_fire_large", fireOrange, 60, 1.1f, radius * 0.7f, 4f, shapeRadius: 1.5f, upward: true);
                    Burst(at + Vector3.up, "mat_glow", fireYellow, 4, 0.35f, 0f, radius * 0.9f);
                    Burst(at, "mat_dust", new Color(0.16f, 0.12f, 0.1f, 0.75f), 18, 2.4f, radius * 0.35f, 5f, shapeRadius: 2f, upward: true);
                    GroundFire(at, radius * 0.45f, 3f);
                    Flash(at + Vector3.up * 2f, fireOrange, radius * 3f, 0.6f, 10f);
                    Shake(at, radius * 3f, 4f, 0.7f);
                    return null;
            }
            return null;
        }

        static GameObject InfernusPulse(string moment, Vector3 at, float radius, Transform follow)
        {
            switch (moment)
            {
                case "loop":
                {
                    // Torment Pulse running: embers rising around him and a hot glow at his feet
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_spark", fireOrange, rate: 50, life: 0.9f, speed: 1.5f, size: 0.09f, shapeRadius: 1.4f, world: true, upward: true, gravity: -0.4f);
                    Particles(root, "mat_fire_puff", fireOrange, rate: 14, life: 0.5f, speed: 0.6f, size: 0.8f, shapeRadius: 1f, world: true, flat: true);
                    Light(root, fireOrange, 9f, 2.5f, Vector3.up * 0.5f).gameObject.AddComponent<Flicker>();
                    return root;
                }
                case "hit":
                    FireRing(at, radius, 0.3f);
                    Burst(at + Vector3.up, "mat_glow", fireOrange, 2, 0.2f, 0f, 3f);
                    return null;
            }
            return null;
        }

        // ------------------------------------------------------------------ Haze: Phantom Strike
        static GameObject HazePhantom(string moment, Vector3 at, float radius, Vector3? to)
        {
            switch (moment)
            {
                case "cast":
                {
                    var end = to ?? at;
                    // smoke where she vanishes and where she lands, a violet streak with afterimage glows between
                    Burst(at, "mat_dust", new Color(0.35f, 0.3f, 0.45f, 0.8f), 10, 0.9f, 2f, 1.6f, shapeRadius: 0.4f);
                    Burst(end, "mat_dust", new Color(0.35f, 0.3f, 0.45f, 0.8f), 8, 0.8f, 2.5f, 1.4f, shapeRadius: 0.4f);
                    var mover = Root(at, 0.6f, null);
                    Trail(mover, hazeViolet, 0.9f, 0.35f);
                    var m = mover.AddComponent<Mover>(); m.From = at; m.To = end; m.Time = 0.08f;
                    int n = Mathf.Clamp((int)Vector3.Distance(at, end), 3, 12);
                    for (int i = 1; i < n; i++) Burst(Vector3.Lerp(at, end, i / (float)n), "mat_glow", i % 2 == 0 ? hazePink : hazeViolet, 1, 0.35f, 0f, 1.4f);
                    Flash(end, hazeViolet, 8f, 0.3f, 5f);
                    return null;
                }
                case "hit":
                {
                    // a slash arc across the target, alternating sides (radius = which cut)
                    int cut = Mathf.RoundToInt(radius);
                    Slash(at, cut % 2 == 0 ? hazePink : hazeViolet, 1.3f, cut * 70f + 25f);
                    Burst(at, "mat_spark", hazePink, 14, 0.3f, 8f, 0.08f);
                    Burst(at, "mat_glow", hazeViolet, 2, 0.15f, 0f, 1.6f);
                    if (cut >= 3) { Ring(at, hazeViolet, 0.3f, 3.5f, 0.3f, 0.2f, true); Flash(at, hazePink, 8f, 0.25f, 6f); }
                    return null;
                }
            }
            return null;
        }

        /// <summary>Sleep Dagger in flight: a violet glow and a pink trail that always show, so the throw is seen even when the
        /// knife model is small or fails to load.</summary>
        static GameObject HazeDagger(string moment, Vector3 at, Transform follow)
        {
            if (moment != "loop") return null;
            var root = Root(at, 0f, follow);
            Particles(root, "mat_glow", hazeViolet, rate: 40, life: 0.12f, speed: 0f, size: 0.8f);
            Trail(root, hazePink, 0.35f, 0.25f);
            Light(root, hazeViolet, 6f, 2.5f, Vector3.zero);
            return root;
        }

        /// <summary>A bright curved cut facing the camera that fades fast.</summary>
        static void Slash(Vector3 at, Color c, float size, float rollDeg)
        {
            var root = Root(at, 0.3f, null);
            var cam = Camera.main;
            var face = cam ? (cam.transform.position - at).normalized : Vector3.back;
            root.transform.rotation = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, rollDeg);
            var lr = new GameObject("slash").AddComponent<LineRenderer>();
            lr.transform.SetParent(root.transform, false);
            lr.useWorldSpace = false;
            lr.sharedMaterial = Mat("mat_tracer", c);
            lr.startColor = lr.endColor = c;
            const int seg = 14;
            lr.positionCount = seg;
            for (int i = 0; i < seg; i++)
            {
                float a = Mathf.Lerp(-70f, 70f, i / (seg - 1f)) * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Sin(a), Mathf.Cos(a) - 0.6f, 0f) * size);
            }
            lr.widthCurve = new AnimationCurve(new Keyframe(0, 0f), new Keyframe(0.5f, 1f), new Keyframe(1, 0f));
            lr.widthMultiplier = 0.25f;
            root.AddComponent<FadeLine>().Life = 0.25f;
        }

        // ------------------------------------------------------------------ Abrams: Haymaker
        static GameObject AbramsHaymaker(string moment, Vector3 at, float radius, Transform follow, Vector3? to, float angle)
        {
            switch (moment)
            {
                case "cast":
                {
                    // the wind-up: the fist glows and smoulders
                    var root = Root(at, 0f, follow);
                    Particles(root, "mat_glow", abramsRed, rate: 30, life: 0.2f, speed: 0f, size: 0.9f);
                    Particles(root, "mat_spark", new Color(1f, 0.7f, 0.35f), rate: 50, life: 0.4f, speed: 2f, size: 0.07f, world: true);
                    Light(root, abramsRed, 5f, 3f, Vector3.zero);
                    return root;
                }
                case "hit":
                {
                    var dir = (to ?? at + Vector3.forward) - at; dir.y = 0; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward; dir.Normalize();
                    // the punch lands: a shockwave ring, sparks thrown forward along the punch, dust, a red flash
                    Ring(at, abramsRed, 0.4f, radius * 1.2f, 0.3f, 0.45f, true);
                    var cone = Root(at, 1.2f, null); cone.transform.rotation = Quaternion.LookRotation(dir);
                    var ps = Particles(cone, "mat_spark", new Color(1f, 0.75f, 0.4f), count: 30, life: 0.35f, speed: 18f, size: 0.09f, shapeRadius: 0.2f, world: true, stretch: 2f);
                    var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 30f; sh.rotation = Vector3.zero;
                    Burst(at, "mat_glow", abramsRed, 3, 0.2f, 0f, radius);
                    Burst(at + Vector3.down * 0.8f, "mat_dust", abramsDust, 10, 1.2f, 4f, 2f, shapeRadius: 1f, flat: true);
                    Flash(at, abramsRed, radius * 3f, 0.3f, 7f);
                    Flash(at, new Color(1f, 0.85f, 0.6f), radius * 2f, 0.1f, 12f);
                    Ring(at, new Color(1f, 0.8f, 0.5f), 0.3f, radius * 2f, 0.22f, 0.3f, true);
                    Burst(at, "mat_fire_puff", abramsRed, 10, 0.4f, 10f, 0.8f, shapeRadius: 0.3f);
                    Shake(at, radius * 6f, 4.5f, 0.45f);
                    return null;
                }
                case "wave":
                {
                    // the shockwave rolls forward over the ground in a cone (radius = its length, angle = its half-angle):
                    // a wall of dust and sparks racing out, and red arcs sweeping across the cone
                    var dir = (to ?? at + Vector3.forward) - at; dir.y = 0; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward; dir.Normalize();
                    float life = 0.35f, half = Mathf.Max(5f, angle);
                    var ground = at + Vector3.down * 0.8f;
                    var cone = Root(ground, 1.5f, null); cone.transform.rotation = Quaternion.LookRotation(dir);
                    var dust = Particles(cone, "mat_dust", abramsDust, count: 40, life: life * 2.5f, speed: radius / life, size: 1.6f, shapeRadius: 0.4f, world: true);
                    var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = half; sh.rotation = Vector3.zero;
                    var dl = dust.limitVelocityOverLifetime; dl.enabled = true; dl.limit = 1f; dl.dampen = 0.15f;
                    var sparks = Particles(cone, "mat_spark", new Color(1f, 0.6f, 0.3f), count: 40, life: life, speed: radius / life, size: 0.08f, shapeRadius: 0.3f, world: true, stretch: 3f);
                    var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Cone; ss.angle = half; ss.rotation = Vector3.zero;
                    for (int i = 1; i <= 3; i++)
                    {
                        float d = radius * i / 3f;
                        var arc = Root(ground + dir * d, life, null);
                        arc.transform.rotation = Quaternion.LookRotation(dir);
                        var ap = Particles(arc, "mat_glow", abramsRed, count: 14, life: life * 0.8f, speed: 0f, size: 0.9f);
                        var ash = ap.shape; ash.shapeType = ParticleSystemShapeType.Circle; ash.radius = Mathf.Max(0.2f, d * Mathf.Tan(half * Mathf.Deg2Rad));
                        ash.arc = 2f * half; ash.rotation = new Vector3(90f, 0f, 90f - half); ash.radiusThickness = 0f;
                        var am = ap.main; am.startDelay = life * (i - 1) / 3f;
                    }
                    Shake(at + dir * radius * 0.5f, radius * 3f, 2f, 0.3f);
                    return null;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ Seven: Power Surge
        static GameObject SevenSurge(string moment, Vector3 at, Transform follow)
        {
            switch (moment)
            {
                case "cast":
                    Ring(at, sevenBlue, 0.5f, 5f, 0.35f, 0.25f);
                    Burst(at, "mat_spark", sevenWhite, 24, 0.35f, 9f, 0.07f);
                    Flash(at, sevenBlue, 10f, 0.35f, 6f);
                    return null;
                case "loop":
                {
                    // Seven crackles with lightning the whole time it runs
                    var root = Root(at, 0f, follow);
                    var c = root.AddComponent<Crackle>(); c.Color = sevenBlue; c.Radius = 1.3f; c.Count = 2;
                    Particles(root, "mat_glow", sevenBlue, rate: 10, life: 0.25f, speed: 0f, size: 1.6f);
                    Particles(root, "mat_spark", sevenWhite, rate: 30, life: 0.3f, speed: 3f, size: 0.05f, shapeRadius: 0.6f, world: true);
                    Light(root, sevenBlue, 6f, 2f, Vector3.zero).gameObject.AddComponent<Flicker>();
                    return root;
                }
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
            // solid models riding on the visual (Napalm's bottle) vanish with the thing they belong to
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>()) mr.enabled = false;
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

        /// <summary>A dome that shrinks from From to To (diameters) every Period seconds: with the area material its outline
        /// on the ground sweeps inward, a countdown that follows the terrain.</summary>
        sealed class DomeSweep : MonoBehaviour
        {
            public float From, To, Period = 1f;
            float age;
            void Update()
            {
                age += Time.deltaTime;
                float t = (age % Period) / Period;
                transform.localScale = Vector3.one * Mathf.Lerp(From, To, t * t);
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

        sealed class Mover : MonoBehaviour
        {
            public Vector3 From, To; public float Time = 0.1f;
            float age;
            void Update() { age += UnityEngine.Time.deltaTime; transform.position = Vector3.Lerp(From, To, Mathf.Clamp01(age / Time)); }
        }

        sealed class FadeLine : MonoBehaviour
        {
            public float Life = 0.25f;
            float age, w; LineRenderer lr;
            void Start() { lr = GetComponentInChildren<LineRenderer>(); if (lr) w = lr.widthMultiplier; }
            void Update() { age += Time.deltaTime; if (lr) lr.widthMultiplier = w * Mathf.Clamp01(1f - age / Life); }
        }

        sealed class GrowLight : MonoBehaviour
        {
            public float To = 8f, Over = 3f;
            float age, from; Light l;
            void Awake() { l = GetComponent<Light>(); from = l ? l.intensity : 0f; }
            void Update() { age += Time.deltaTime; if (l) l.intensity = Mathf.Lerp(from, To, Mathf.Clamp01(age / Over)) * Random.Range(0.85f, 1.1f); }
        }

        /// <summary>Stops a looping effect after a while (ground fire that burns out).</summary>
        sealed class StopAfter : MonoBehaviour
        {
            public float After;
            float age;
            void Update() { age += Time.deltaTime; if (age >= After) { Destroy(this); Stop(gameObject, 1f); } }
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
                embers = Particles(root, "mat_spark", fireOrange, rate: 0f, life: 0.8f, speed: 1.5f, size: 0.1f, shapeRadius: r, world: true, upward: true, gravity: -0.3f);
                flames = Particles(root, "mat_fire_large", fireOrange, rate: 0f, life: 0.7f, speed: 2f, size: 1.3f * r, shapeRadius: r * 0.7f, world: true, upward: true);
                glow = Light(root, fireOrange, 4f * r, 0f, Vector3.zero);
            }
            void Update()
            {
                if (!body || !body.healthComponent || !body.healthComponent.alive) { Done(); return; }
                var row = Sheet.Buff("afterburn_buildup");
                float build = row != null && row.TriggerStacks > 0 ? Buffs.Count(body, row.Id) / row.TriggerStacks : 0f;
                // the Burn DoT's own buff: synced to every player (DotController exists only on the server)
                bool burning = body.HasBuff(RoR2Content.Buffs.OnFire);
                var e = embers.emission; e.rateOverTime = build * 60f;
                var f = flames.emission; f.rateOverTime = burning ? 40f : build * 8f;
                glow.intensity = burning ? Random.Range(3f, 5f) : build * 2f;
                if (build <= 0f && !burning) { idle += Time.deltaTime; if (idle > 1f) Done(); } else idle = 0f;
            }
            void Done() { Stop(root); Destroy(this); }
            void OnDestroy() { if (root) Stop(root); }
        }
    }
}
