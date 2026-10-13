using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Plays the hero's own Deadlock animation clips (legacy Animation, clips built at runtime from the cache):
    /// locomotion on layer 0 chosen from the body's movement, gestures (fire, reload, abilities) on the upper body,
    /// full-body overrides for movement abilities and ultimates, and spine pitch toward the aim.</summary>
    public sealed class DeadlockAnimator : MonoBehaviour
    {
        public string HeroId;
        Animation anim;
        AnimRow rows;
        Transform upperBody;
        Transform[] spine;
        CharacterBody body;
        CharacterMotor motor;
        InputBankTest inputs;
        string loco;
        string overrideClip;
        float overrideUntil;
        bool wasGrounded = true;
        float landUntil;
        bool dead;
        static readonly Dictionary<string, Dictionary<string, AnimationClip>> clipCache = new Dictionary<string, Dictionary<string, AnimationClip>>();
        Dictionary<string, AnimationClip> clips;
        readonly Dictionary<string, string> logical = new Dictionary<string, string>();

        public void Setup(string heroId, HeroAsset asset, Transform[] bones, Transform armature)
        {
            HeroId = heroId;
            var a = gameObject.AddComponent<Animation>();
            a.playAutomatically = false;
            a.cullingType = AnimationCullingType.AlwaysAnimate;
            foreach (var kv in BuildClips(heroId, asset, bones, transform)) a.AddClip(kv.Value, kv.Key);
            Init();
        }

        /// <summary>Instantiate does not copy private fields, so every clone (display model, spawned body) rebuilds them here.</summary>
        void Init()
        {
            if (anim != null) return;
            anim = GetComponent<Animation>();
            if (!anim || string.IsNullOrEmpty(HeroId) || !clipCache.TryGetValue(HeroId, out clips)) { anim = null; return; }
            rows = Sheet.Anims(HeroId);
            logical["idle"] = rows.Idle; logical["run_n"] = rows.RunN; logical["run_s"] = rows.RunS; logical["run_e"] = rows.RunE; logical["run_w"] = rows.RunW;
            logical["sprint"] = rows.Sprint; logical["jump_start"] = rows.JumpStart; logical["jump_air"] = rows.JumpAir; logical["land"] = rows.Land;
            logical["fire"] = rows.Fire; logical["fire_alt"] = rows.FireAlt; logical["reload"] = rows.Reload;
            logical["secondary"] = rows.Secondary; logical["utility"] = rows.Utility; logical["utility_loop"] = rows.UtilityLoop; logical["special"] = rows.Special;
            logical["ult_start"] = rows.UltStart; logical["ult_loop"] = rows.UltLoop; logical["ult_end"] = rows.UltEnd; logical["death"] = rows.Death;
            var all = GetComponentsInChildren<Transform>(true);
            Transform Bone(string n) { foreach (var t in all) if (t.name == n) return t; return null; }
            upperBody = Bone(rows.UpperBodyBone);
            spine = new[] { Bone("spine_1"), Bone("spine_2"), Bone("spine_3") };
            foreach (var kv in clips)
            {
                var st = anim[kv.Key];
                if (st == null) continue;
                bool locomotion = IsLocomotion(kv.Key);
                st.layer = locomotion ? 0 : 1;
                st.wrapMode = locomotion || kv.Key == rows.UltLoop || kv.Key == rows.UtilityLoop || (kv.Key == rows.Secondary && HeroId == "abrams") ? WrapMode.Loop : WrapMode.Once;
            }
            loco = null;
            Play(rows.Idle, 0f);
            loco = rows.Idle;
        }

        void Awake() => Init();

        bool IsLocomotion(string clip) =>
            clip == rows.Idle || clip == rows.RunN || clip == rows.RunS || clip == rows.RunE || clip == rows.RunW ||
            clip == rows.Sprint || clip == rows.JumpAir;

        static Dictionary<string, AnimationClip> BuildClips(string hero, HeroAsset asset, Transform[] bones, Transform root)
        {
            if (clipCache.TryGetValue(hero, out var cached)) return cached;
            var result = new Dictionary<string, AnimationClip>();
            var paths = new string[bones.Length];
            for (int i = 0; i < bones.Length; i++) paths[i] = PathTo(bones[i], root);
            foreach (var c in asset.Clips.Values)
            {
                var clip = new AnimationClip { legacy = true, name = c.Name, frameRate = c.Fps };
                foreach (var t in c.Tracks)
                {
                    if (t.Bone < 0 || t.Bone >= bones.Length) continue;
                    var p = paths[t.Bone];
                    // Deadlock clips carry root motion (runs move the root metres per loop); RoR2 moves the body itself
                    bool isRoot = asset.Bones[t.Bone].Parent < 0 || asset.Bones[t.Bone].Name == "root_motion";
                    if (t.Pos.Length > 0 && !isRoot)
                    {
                        var px = new Keyframe[t.Pos.Length]; var py = new Keyframe[t.Pos.Length]; var pz = new Keyframe[t.Pos.Length];
                        for (int f = 0; f < t.Pos.Length; f++)
                        {
                            float time = t.Pos.Length == 1 ? 0 : Mathf.Min(f / c.Fps, c.Length);
                            px[f] = new Keyframe(time, t.Pos[f].x); py[f] = new Keyframe(time, t.Pos[f].y); pz[f] = new Keyframe(time, t.Pos[f].z);
                        }
                        clip.SetCurve(p, typeof(Transform), "localPosition.x", Linear(px));
                        clip.SetCurve(p, typeof(Transform), "localPosition.y", Linear(py));
                        clip.SetCurve(p, typeof(Transform), "localPosition.z", Linear(pz));
                    }
                    var n = t.Rot.Length;
                    var rx = new Keyframe[n]; var ry = new Keyframe[n]; var rz = new Keyframe[n]; var rw = new Keyframe[n];
                    Quaternion prev = t.Rot[0];
                    for (int f = 0; f < n; f++)
                    {
                        var q = t.Rot[f];
                        if (f > 0 && Quaternion.Dot(prev, q) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                        prev = q;
                        float time = n == 1 ? 0 : Mathf.Min(f / c.Fps, c.Length);
                        rx[f] = new Keyframe(time, q.x); ry[f] = new Keyframe(time, q.y); rz[f] = new Keyframe(time, q.z); rw[f] = new Keyframe(time, q.w);
                    }
                    clip.SetCurve(p, typeof(Transform), "localRotation.x", Linear(rx));
                    clip.SetCurve(p, typeof(Transform), "localRotation.y", Linear(ry));
                    clip.SetCurve(p, typeof(Transform), "localRotation.z", Linear(rz));
                    clip.SetCurve(p, typeof(Transform), "localRotation.w", Linear(rw));
                }
                clip.EnsureQuaternionContinuity();
                result[c.Name] = clip;
            }
            clipCache[hero] = result;
            return result;
        }

        static AnimationCurve Linear(Keyframe[] k)
        {
            var curve = new AnimationCurve(k);
            for (int i = 0; i < k.Length; i++)
            {
                AnimationUtilityLite.SetLinear(curve, i);
            }
            return curve;
        }

        static string PathTo(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != root; x = x.parent) parts.Insert(0, x.name);
            return string.Join("/", parts);
        }

        void OnEnable()
        {
            Init();
            if (anim && rows != null && !anim.isPlaying) Play(rows.Idle, 0f);
        }

        void Start()
        {
            var ml = GetComponentInParent<CharacterModel>();
            body = ml ? ml.body : null;
            if (!body) body = GetComponentInParent<CharacterBody>();
            if (body)
            {
                motor = body.characterMotor;
                inputs = body.inputBank;
            }
            // re-bake paths: animation paths are relative to this object
        }

        public static DeadlockAnimator Of(CharacterBody b)
        {
            if (!b || !b.modelLocator || !b.modelLocator.modelTransform) return null;
            return b.modelLocator.modelTransform.GetComponent<DeadlockAnimator>();
        }

        string Clip(string logicalName)
        {
            if (logicalName == null) return null;
            if (logical.TryGetValue(logicalName.ToLowerInvariant(), out var c) && c != "none" && clips.ContainsKey(c)) return c;
            return null;
        }

        void Play(string clip, float fade)
        {
            if (clip == null || clip == "none" || !clips.ContainsKey(clip)) return;
            if (fade <= 0) anim.Play(clip); else anim.CrossFade(clip, fade);
        }

        /// <summary>Upper-body gesture (fire, reload, secondary...). Legs keep their locomotion.</summary>
        public void Gesture(string logicalName, float speed = 1f)
        {
            var c = Clip(logicalName);
            if (c == null) return;
            var st = anim[c];
            if (upperBody) { st.RemoveMixingTransform(upperBody); st.AddMixingTransform(upperBody, true); }
            st.layer = 1;
            st.speed = speed;
            st.time = 0;
            anim.CrossFade(c, 0.05f, PlayMode.StopSameLayer);
        }

        /// <summary>Full-body override for movement abilities and ultimates, for the given time (or until StopOverride).</summary>
        public void Override(string logicalName, float duration, bool loop = false)
        {
            var c = Clip(logicalName);
            if (c == null) return;
            var st = anim[c];
            if (upperBody) st.RemoveMixingTransform(upperBody);
            st.layer = 2;
            st.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
            st.time = 0;
            st.speed = !loop && duration > 0 ? Mathf.Max(0.2f, st.length / duration) : 1f;
            anim.CrossFade(c, 0.1f, PlayMode.StopSameLayer);
            overrideClip = c;
            overrideUntil = duration > 0 ? Time.time + duration : float.MaxValue;
        }

        public void StopOverride()
        {
            if (overrideClip != null) anim.Blend(overrideClip, 0f, 0.15f);
            overrideClip = null;
        }

        public bool HasClip(string logicalName) => Clip(logicalName) != null;

        void Update()
        {
            if (anim == null) return;
            if (overrideClip != null && Time.time > overrideUntil) StopOverride();
            if (body && body.healthComponent && !body.healthComponent.alive)
            {
                if (!dead) { dead = true; var d = Clip("death"); if (d != null) { anim.Stop(); anim[d].layer = 3; anim[d].wrapMode = WrapMode.ClampForever; anim.Play(d); } }
                return;
            }
            string next = rows.Idle;
            float speed = 1f;
            if (motor)
            {
                var v = motor.velocity; v.y = 0;
                bool grounded = motor.isGrounded;
                if (!grounded) next = Clip("jump_air") ?? rows.Idle;
                else if (v.sqrMagnitude > 0.5f)
                {
                    var fwd = transform.forward; fwd.y = 0; fwd.Normalize();
                    var right = new Vector3(fwd.z, 0, -fwd.x);
                    float f = Vector3.Dot(v, fwd), r = Vector3.Dot(v, right);
                    if (body && body.isSprinting && f > 0) next = Clip("sprint") ?? rows.RunN;
                    else if (Mathf.Abs(f) >= Mathf.Abs(r)) next = f >= 0 ? rows.RunN : rows.RunS;
                    else next = r >= 0 ? rows.RunE : rows.RunW;
                    float baseSpeed = body ? Mathf.Max(1f, body.moveSpeed) : 7f;
                    speed = Mathf.Clamp(v.magnitude / baseSpeed, 0.6f, 1.8f);
                }
                if (grounded && !wasGrounded)
                {
                    var land = Clip("land");
                    if (land != null) { Gesture("land"); landUntil = Time.time + 0.2f; }
                }
                if (!grounded && wasGrounded && inputs && inputs.jump.down) Gesture("jump_start");
                wasGrounded = grounded;
            }
            if (next != loco && clips.ContainsKey(next))
            {
                anim.CrossFade(next, 0.15f, PlayMode.StopSameLayer);
                loco = next;
            }
            if (loco != null && anim[loco] != null) anim[loco].speed = speed;
        }

        void LateUpdate()
        {
            // pitch the spine toward the aim so shots visibly come from where the hero looks
            if (!inputs || dead || overrideClip != null) return;
            var aim = inputs.aimDirection;
            float pitch = -Mathf.Asin(Mathf.Clamp(aim.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -70f, 70f) / 3f;
            var axis = transform.right;
            foreach (var s in spine)
                if (s) s.rotation = Quaternion.AngleAxis(pitch, axis) * s.rotation;
        }
    }

    /// <summary>Linear tangents without UnityEditor.AnimationUtility (not available in the player).</summary>
    static class AnimationUtilityLite
    {
        public static void SetLinear(AnimationCurve curve, int i)
        {
            var keys = curve.keys;
            var k = keys[i];
            if (i > 0) { var p = keys[i - 1]; float dt = k.time - p.time; k.inTangent = dt > 0 ? (k.value - p.value) / dt : 0; }
            if (i < keys.Length - 1) { var n = keys[i + 1]; float dt = n.time - k.time; k.outTangent = dt > 0 ? (n.value - k.value) / dt : 0; }
            curve.MoveKey(i, k);
        }
    }
}
