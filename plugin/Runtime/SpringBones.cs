using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Bone chains simulated as springs on top of the animation (animations springRoots: Rat King's cape and tail).
    /// Each bone below a chain root is a point that keeps its momentum, falls under springGravity, is pulled back toward
    /// where the animation puts it by springStiffness (stiff near the root, loose at the tip) and slowed by springDamping;
    /// it keeps its length to the bone above it, bends at most MaxBend away from the animated pose, stays out of capsules
    /// on the spine, arms and legs and above the ground. The body's own movement is what makes the chain trail: the points
    /// live in the world, so running, jumping, falling and turning all swing it without a clip for each.
    /// DeadlockAnimator steps it after its gun rig (which turns the spine the chains hang from); on the death ragdoll, with
    /// the animator off, it steps itself.</summary>
    public sealed class SpringBones : MonoBehaviour
    {
        sealed class Chain
        {
            public Transform[] Bones;
            public Vector3[] Pos, Prev, Target;
            public Quaternion[] Rest, Written;
            public float[] Length;
        }

        // settings in public fields: Instantiate copies only those, so every clone (display model, spawned body) builds its own
        // chains from its own bones the first time it steps
        public string[] Roots;
        public float Stiffness, Damping, Gravity, Height;

        readonly List<Chain> chains = new List<Chain>();
        readonly List<(Transform a, Transform b, float r)> capsules = new List<(Transform, Transform, float)>();
        float groundY = float.NegativeInfinity, nextGround;
        Vector3 lastRoot;
        bool primed, built;
        DeadlockAnimator anim;
        const float MaxBend = 75f;

        /// <param name="height">the hero's height in the world (m): the body capsules are a share of it</param>
        public static SpringBones Add(GameObject model, AnimRow rows, float height)
        {
            if (rows.SpringRoots.Length == 0 || rows.SpringRoots[0] == "none") return null;
            var s = model.AddComponent<SpringBones>();
            s.Roots = rows.SpringRoots; s.Stiffness = rows.SpringStiffness; s.Damping = rows.SpringDamping; s.Gravity = rows.SpringGravity;
            s.Height = height;
            return s;
        }

        void Build()
        {
            built = true;
            var bones = GetComponentsInChildren<Transform>(true);
            Transform Find(string n) { foreach (var b in bones) if (b.name == n) return b; return null; }
            foreach (var root in Roots ?? new string[0])
            {
                var t = Find(root);
                if (!t) { Log.Warn($"springs: no bone {root}"); continue; }
                var list = new List<Transform> { t };
                // down the chain: the child with the most bones below it (a cape strip, a tail)
                for (var b = t; b.childCount > 0;)
                {
                    Transform next = null; int most = -1;
                    foreach (Transform c in b) { int n = c.GetComponentsInChildren<Transform>(true).Length; if (n > most) { most = n; next = c; } }
                    list.Add(next); b = next;
                }
                if (list.Count < 2) continue;
                var ch = new Chain { Bones = list.ToArray() };
                int k = ch.Bones.Length;
                ch.Pos = new Vector3[k]; ch.Prev = new Vector3[k]; ch.Target = new Vector3[k];
                ch.Rest = new Quaternion[k]; ch.Written = new Quaternion[k]; ch.Length = new float[k];
                for (int i = 0; i < k; i++) { ch.Rest[i] = ch.Bones[i].localRotation; ch.Written[i] = new Quaternion(0, 0, 0, 0); }
                chains.Add(ch);
            }
            // the body the chains must not pass through: spine, upper legs, arms (radius a share of the hero's height)
            void Cap(string a, string b, float share)
            {
                var ta = Find(a); var tb = Find(b);
                if (ta && tb) capsules.Add((ta, tb, share * Height));
            }
            Cap("pelvis", "spine_3", 0.11f); Cap("spine_3", "head", 0.08f);
            Cap("leg_upper_L", "leg_lower_L", 0.05f); Cap("leg_upper_R", "leg_lower_R", 0.05f);
            Cap("arm_upper_L", "arm_lower_L", 0.035f); Cap("arm_upper_R", "arm_lower_R", 0.035f);
        }

        /// <summary>The chains started over at their animated pose (a teleport, a respawn, the model shown again).</summary>
        public void Reset() => primed = false;

        void LateUpdate()
        {
            // the animator steps the chains after its gun rig; with it off (the death ragdoll) they step here
            if (!anim) anim = GetComponent<DeadlockAnimator>();
            if (!anim || !anim.isActiveAndEnabled) Step(Time.deltaTime);
        }

        public void Step(float dt)
        {
            if (!built) Build();
            if (dt <= 0f || chains.Count == 0) return;
            dt = Mathf.Min(dt, 1f / 30f);
            // a jump of several metres in one frame (teleport, respawn): start over rather than whip the cape across the map
            var rootPos = transform.position;
            if (primed && (rootPos - lastRoot).sqrMagnitude > 25f) primed = false;
            lastRoot = rootPos;
            if (Time.time >= nextGround)
            {
                nextGround = Time.time + 0.2f;
                groundY = Physics.Raycast(rootPos + Vector3.up * 1.5f, Vector3.down, out var g, 4f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)
                    ? g.point.y : float.NegativeInfinity;
            }
            float keep = 1f - Damping;
            float pull = 1f - Mathf.Pow(1f - Mathf.Clamp01(Stiffness), dt * 60f);
            var g3 = Vector3.down * Gravity * dt * dt;
            foreach (var ch in chains)
            {
                int n = ch.Bones.Length;
                // 1. the animated pose: a bone the clip doesn't move still holds what we wrote last frame, so it goes back to rest
                for (int i = 0; i < n; i++)
                {
                    var b = ch.Bones[i];
                    if (!b) return;
                    if (b.localRotation == ch.Written[i]) b.localRotation = ch.Rest[i];
                }
                for (int i = 0; i < n; i++) ch.Target[i] = ch.Bones[i].position;
                for (int i = 1; i < n; i++) ch.Length[i] = Vector3.Distance(ch.Target[i], ch.Target[i - 1]);
                if (!primed)
                {
                    for (int i = 0; i < n; i++) { ch.Pos[i] = ch.Target[i]; ch.Prev[i] = ch.Target[i]; }
                    continue;
                }
                ch.Pos[0] = ch.Target[0];
                for (int i = 1; i < n; i++)
                {
                    // 2. momentum and gravity, then a pull back toward the animated pose (stiff at the root, loose at the tip)
                    var p = ch.Pos[i];
                    var v = (p - ch.Prev[i]) * keep;
                    ch.Prev[i] = p;
                    p += v + g3;
                    float along = (float)i / (n - 1);
                    p += (ch.Target[i] - p) * pull * Mathf.Lerp(1.8f, 0.45f, along);
                    // 3. its length to the bone above, and at most MaxBend from the animated direction
                    var up = ch.Pos[i - 1];
                    var dir = p - up;
                    var want = ch.Target[i] - ch.Target[i - 1];
                    if (dir.sqrMagnitude < 1e-8f) dir = want;
                    if (want.sqrMagnitude > 1e-8f && Vector3.Angle(dir, want) > MaxBend)
                        dir = Vector3.RotateTowards(want, dir, MaxBend * Mathf.Deg2Rad, 0f);
                    p = up + dir.normalized * ch.Length[i];
                    // 4. out of the body's capsules and above the ground
                    if (i >= 2) foreach (var c in capsules) p = PushOut(p, c.a.position, c.b.position, c.r);
                    if (p.y < groundY + 0.04f) p.y = groundY + 0.04f;
                    ch.Pos[i] = p;
                }
                // 5. turn each bone so its child sits on the simulated point
                for (int i = 0; i < n - 1; i++)
                {
                    var b = ch.Bones[i];
                    var cur = ch.Bones[i + 1].position - b.position;
                    var to = ch.Pos[i + 1] - ch.Pos[i];
                    if (cur.sqrMagnitude > 1e-8f && to.sqrMagnitude > 1e-8f) b.rotation = Quaternion.FromToRotation(cur, to) * b.rotation;
                }
                for (int i = 0; i < n; i++) ch.Written[i] = ch.Bones[i].localRotation;
            }
            primed = true;
        }

        static Vector3 PushOut(Vector3 p, Vector3 a, Vector3 b, float r)
        {
            var ab = b - a;
            float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            var c = a + ab * t;
            var d = p - c;
            float m = d.magnitude;
            if (m >= r || m < 1e-5f) return p;
            return c + d / m * r;
        }
    }
}
