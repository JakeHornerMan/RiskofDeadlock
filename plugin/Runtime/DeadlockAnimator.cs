using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Plays the hero's own Deadlock animation clips (legacy Animation, clips built at runtime from the cache):
    /// locomotion on layer 0 chosen from the body's movement, gestures (fire, reload, abilities) on the upper body,
    /// full-body overrides for movement abilities and ultimates, and the gun rig (rigid gun arms aimed where the camera aims).</summary>
    public sealed class DeadlockAnimator : MonoBehaviour
    {
        public string HeroId;
        Animation anim;
        AnimRow rows;
        Transform upperBody;
        Transform[] spine;
        /// <summary>The gun rig's bone-name prefix (animations rigPrefix): "rider_" when the gun is held by Krill on Mo.</summary>
        string rig = "";
        CharacterBody body;
        CharacterMotor motor;
        InputBankTest inputs;
        string loco;
        string overrideClip;
        float overrideUntil;
        bool wasGrounded = true;
        float landUntil;
        bool dead, frozen;
        static readonly Dictionary<string, Dictionary<string, AnimationClip>> clipCache = new Dictionary<string, Dictionary<string, AnimationClip>>();
        Dictionary<string, AnimationClip> clips;
        readonly Dictionary<string, string> logical = new Dictionary<string, string>();

        // gun rig: arms held rigid in the sheet's aimPose, gun welded to its hand, chest turned so the gun points at the aim
        Transform[] rigBones, gunParts;
        Vector3[] rigPos, gunPartPos;
        Quaternion[] rigRot, gunPartRot;
        Transform gunHand, gunBone, mainHand;
        Vector3 gunOffPos, gunFwdLocal;
        Quaternion gunOffRot;
        float armWeight = 1f, armFreeUntil;
        float recoil, recoilTime;
        /// <summary>Degrees between where the gun points and the aim direction after the last frame (-1 when not aiming; test mode).</summary>
        public float AimError { get; private set; } = -1f;
        /// <summary>Largest distance (metres) the gun moved inside its hand since the pose was captured: 0 while the gun
        /// stays in the hand (test mode; reset with ResetDrift).</summary>
        public float GunDrift { get; private set; }
        Transform gunRoot;
        Vector3 gunRootInHand;
        public void ResetDrift() => GunDrift = 0f;

        // the cape and tail springs (animations springRoots), stepped after the gun rig turns the spine they hang from
        SpringBones springs;
        // bones carrying a prop the hero only holds during a move (animations propBones: Rat King's banner), at scale 0 otherwise
        Transform[] propBones;
        Vector3[] propScale;
        bool propsShown;
        // King's Leap (heroes airJumpMult): a tucked forward spin of the whole skeleton on a jump in the air
        Transform armature;
        float flipStart = -1f, flipTime, lastY, lastVy;
        Vector3 flipMid = Vector3.up;
        bool airLeap;

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
            logical["secondary"] = rows.Secondary; logical["utility"] = rows.Utility; logical["utility_loop"] = rows.UtilityLoop;
            logical["special_start"] = rows.SpecialStart; logical["special"] = rows.Special;
            logical["ult_start"] = rows.UltStart; logical["ult_loop"] = rows.UltLoop; logical["ult_end"] = rows.UltEnd; logical["death"] = rows.Death;
            logical["special_loop"] = rows.SpecialLoop; logical["melee"] = rows.Melee; logical["melee_alt"] = rows.MeleeAlt;
            rig = rows.RigPrefix == "none" ? "" : rows.RigPrefix;
            var all = GetComponentsInChildren<Transform>(true);
            Transform Bone(string n) { foreach (var t in all) if (t.name == n) return t; return null; }
            upperBody = Bone(rows.UpperBodyBone);
            // the gun rig turns the spine of whoever holds the gun (Krill's, rider_spine_*, on Mo)
            spine = new[] { Bone(rig + "spine_1"), Bone(rig + "spine_2"), Bone(rig + "spine_3") };
            foreach (var kv in clips)
            {
                var st = anim[kv.Key];
                if (st == null) continue;
                bool locomotion = IsLocomotion(kv.Key);
                st.layer = locomotion ? 0 : 1;
                st.wrapMode = locomotion || kv.Key == rows.UltLoop || kv.Key == rows.UtilityLoop || kv.Key == rows.SpecialLoop || (kv.Key == rows.Secondary && rows.LoopSecondary) ? WrapMode.Loop : WrapMode.Once;
            }
            CaptureRig(all);
            springs = GetComponent<SpringBones>();
            armature = transform.Find("Armature");
            var hero = Sheet.Hero(HeroId);
            airLeap = hero != null && hero.AirJumpMult > 1f;
            if (hero != null) flipMid = Vector3.up * hero.SourceHeight * 0.45f;
            var props = new List<Transform>();
            foreach (var n in rows.PropBones) { var t = n == "none" ? null : Bone(n); if (t) props.Add(t); }
            propBones = props.ToArray();
            propScale = new Vector3[propBones.Length];
            // (a clone of the prefab, whose props are already hidden, keeps Deadlock's own scale 1)
            for (int i = 0; i < propBones.Length; i++)
            {
                var sc = propBones[i].localScale;
                propScale[i] = sc.sqrMagnitude < 1e-6f ? Vector3.one : sc;
                propBones[i].localScale = Vector3.zero;
            }
            propsShown = false;
            loco = null;
            Play(rows.Idle, 0f);
            loco = rows.Idle;
        }

        /// <summary>Samples the aimPose clip's first frame and records the gun arms' pose, the gun's offset in its hand and
        /// the gun's forward axis (hand to muzzle) in hand space.</summary>
        void CaptureRig(Transform[] all)
        {
            Transform Bone(string n) { foreach (var t in all) if (t.name == n) return t; return null; }
            if (rows.AimPose == "none" || !clips.TryGetValue(rows.AimPose, out var pose)) return;
            pose.SampleAnimation(gameObject, 0f);
            gunHand = Bone(rows.GunHand);
            mainHand = gunHand ? gunHand : Bone(rig + "hand_R");
            gunBone = rows.GunBone != "none" ? Bone(rows.GunBone) : null;
            // the gun's own bones (weapon_hand_* subtrees, everything under a welded gun bone) never animate: ability and
            // reload clips would otherwise slide the gun around inside the hand
            var gun = new List<Transform>();
            foreach (var w in new[] { Bone(rig + "weapon_hand_R"), Bone(rig + "weapon_hand_L") }) if (w) gun.AddRange(w.GetComponentsInChildren<Transform>(true));
            if (gunBone && gunHand)
            {
                foreach (var t in gunBone.GetComponentsInChildren<Transform>(true)) if (t != gunBone) gun.Add(t);
                gunOffPos = gunHand.InverseTransformPoint(gunBone.position);
                gunOffRot = Quaternion.Inverse(gunHand.rotation) * gunBone.rotation;
            }
            // the gun arms hold the pose while the hero shoots, moves and jumps (abilities and reloads may move them)
            var arms = new List<Transform>();
            foreach (var side in rows.GunArms == "LR" ? new[] { "R", "L" } : new[] { "R" })
            {
                var clav = Bone(rig + "clavicle_" + side);
                if (clav) foreach (var t in clav.GetComponentsInChildren<Transform>(true)) if (!gun.Contains(t)) arms.Add(t);
            }
            Snapshot(arms, out rigBones, out rigPos, out rigRot);
            Snapshot(gun, out gunParts, out gunPartPos, out gunPartRot);
            gunRoot = gunBone ? gunBone : Bone(rig + "weapon_hand_R");
            if (gunRoot && mainHand) gunRootInHand = mainHand.InverseTransformPoint(gunRoot.position);
            var muzzle = Bone("muzzle_fx");
            var dir = muzzle && mainHand ? muzzle.position - mainHand.position : transform.forward;
            // a muzzle at the grip (animations muzzleForward, Rat King): the gun points along its own axis
            var tip = Bone("muzzle_tip");
            if (tip && tip.parent) dir = tip.position - tip.parent.position;
            if (dir.sqrMagnitude < 1e-6f) dir = transform.forward;
            gunFwdLocal = mainHand ? Quaternion.Inverse(mainHand.rotation) * dir.normalized : Vector3.forward;
            if (MeltyTest.Active) Log.Info($"gun rig {HeroId}: gun points {transform.InverseTransformDirection(dir.normalized).ToString("F2")} (model space) in {rows.AimPose}, tip {(tip ? "yes" : "no")}, hand {(mainHand ? mainHand.name : "-")}, gun bone {(gunBone ? gunBone.name : "-")}");
        }

        static void Snapshot(List<Transform> bones, out Transform[] t, out Vector3[] pos, out Quaternion[] rot)
        {
            t = bones.ToArray(); pos = new Vector3[t.Length]; rot = new Quaternion[t.Length];
            for (int i = 0; i < t.Length; i++) { pos[i] = t[i].localPosition; rot[i] = t[i].localRotation; }
        }

        void Awake() => Init();

        bool IsLocomotion(string clip) =>
            clip == rows.Idle || clip == rows.RunN || clip == rows.RunS || clip == rows.RunE || clip == rows.RunW ||
            clip == rows.Sprint || clip == rows.JumpAir;

        internal static Dictionary<string, AnimationClip> BuildClips(string hero, HeroAsset asset, Transform[] bones, Transform root)
        {
            if (clipCache.TryGetValue(hero, out var cached)) return cached;
            var result = new Dictionary<string, AnimationClip>();
            var paths = new string[bones.Length];
            for (int i = 0; i < bones.Length; i++) paths[i] = PathTo(bones[i], root);
            int rm = System.Array.FindIndex(asset.Bones, b => b.Name == "root_motion");
            foreach (var c in asset.Clips.Values)
            {
                var clip = new AnimationClip { legacy = true, name = c.Name, frameRate = c.Fps };
                foreach (var t in RootFolded(asset, c, rm))
                {
                    if (t.Bone < 0 || t.Bone >= bones.Length) continue;
                    // a parentless bone the model builder attached to the body (Abrams' coat cloth) keeps its rest pose:
                    // its tracks are in model space and would twist it now that it has a parent
                    if (asset.Bones[t.Bone].Parent < 0 && bones[t.Bone].parent && bones[t.Bone].parent.name != "Armature") continue;
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

        /// <summary>The clip's tracks with root_motion's rotation folded into its direct children (pelvis, a gun pivot, a
        /// banner), and no track on root_motion itself. Deadlock's clips turn root_motion (most to 270/90/0, some leave it at
        /// rest), and a legacy clip without a track never resets it, so a clip played after one that turned it inherited the
        /// turn: Rat King's idle (no root track) laid him on his back after his first run or jump. Folded, the root always
        /// stays at rest and every clip poses the bones exactly as before on its own.</summary>
        static List<TrackDef> RootFolded(HeroAsset asset, ClipDef c, int rm)
        {
            var root = rm >= 0 ? c.Tracks.Find(t => t.Bone == rm) : null;
            if (root == null || root.Rot.Length == 0) return c.Tracks;
            var restInv = Quaternion.Inverse(asset.Bones[rm].Rot);
            var outList = new List<TrackDef>();
            var done = new HashSet<int>();
            foreach (var t in c.Tracks)
            {
                if (t.Bone == rm) continue;
                if (t.Bone >= 0 && t.Bone < asset.Bones.Length && asset.Bones[t.Bone].Parent == rm) { outList.Add(Fold(asset, c, root, restInv, t)); done.Add(t.Bone); }
                else outList.Add(t);
            }
            // children the clip doesn't move still hang off the turned root: give them their rest pose, turned the same way
            for (int i = 0; i < asset.Bones.Length; i++)
                if (asset.Bones[i].Parent == rm && !done.Contains(i))
                    outList.Add(Fold(asset, c, root, restInv, new TrackDef { Bone = i, Pos = new[] { asset.Bones[i].Pos }, Rot = new[] { asset.Bones[i].Rot } }));
            return outList;
        }

        static TrackDef Fold(HeroAsset asset, ClipDef c, TrackDef root, Quaternion restInv, TrackDef t)
        {
            var b = asset.Bones[t.Bone];
            int n = root.Rot.Length > 1 || t.Rot.Length > 1 || t.Pos.Length > 1 ? Mathf.Max(1, c.Frames) : 1;
            var pos = new Vector3[n]; var rot = new Quaternion[n];
            for (int f = 0; f < n; f++)
            {
                var r = restInv * root.Rot[Mathf.Min(f, root.Rot.Length - 1)];
                var q = t.Rot.Length > 0 ? t.Rot[Mathf.Min(f, t.Rot.Length - 1)] : b.Rot;
                var p = t.Pos.Length > 0 ? t.Pos[Mathf.Min(f, t.Pos.Length - 1)] : b.Pos;
                rot[f] = r * q; pos[f] = r * p;
            }
            return new TrackDef { Bone = t.Bone, Pos = pos, Rot = rot };
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
            // reload and ability clips move the arms; landing/jump layers leave the gun arms in their holding pose
            if (logicalName != "land" && logicalName != "jump_start") armFreeUntil = Mathf.Max(armFreeUntil, Time.time + st.length / Mathf.Max(0.1f, speed));
        }

        /// <summary>A shot: a short kick of the chest instead of a fire clip on the arms.</summary>
        public void Recoil(float degrees = 4f)
        {
            recoil = degrees;
            recoilTime = Time.time;
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

        /// <summary>Test mode: how the pose stands (the spin, the skeleton's tilt, the head's height above the feet, the spine).</summary>
        public string Describe()
        {
            var all = GetComponentsInChildren<Transform>(true);
            Transform B(string n) { foreach (var t in all) if (t.name == n) return t; return null; }
            var head = B("head"); var pelvis = B("pelvis");
            string sp = "";
            if (spine != null) foreach (var t in spine) if (t) sp += $" {t.name} {t.localEulerAngles.ToString("F0")}";
            var tipT = B("muzzle_tip");
            string gun = mainHand ? $"rig gun {transform.InverseTransformDirection(mainHand.rotation * gunFwdLocal).ToString("F2")}" : "";
            if (tipT && tipT.parent) gun += $", barrel {transform.InverseTransformDirection((tipT.position - tipT.parent.position).normalized).ToString("F2")}";
            return $"{gun}, flip {(flipStart >= 0f ? "on" : "off")}, armature {(armature ? armature.localEulerAngles.ToString("F0") : "-")}, model up {transform.up.ToString("F2")}, " +
                   $"head {(head ? (head.position.y - transform.position.y).ToString("F2") : "-")} m, pelvis {(pelvis ? (pelvis.position.y - transform.position.y).ToString("F2") : "-")} m, override {overrideClip ?? "-"}, loco {loco},{sp}" +
                   $", pelvis local {(pelvis ? pelvis.localPosition.ToString("F2") + " " + pelvis.localEulerAngles.ToString("F0") + " under " + pelvis.parent.name : "-")}" +
                   $", root {(B("root_motion") ? B("root_motion").localPosition.ToString("F2") + " " + B("root_motion").localEulerAngles.ToString("F0") : "-")}, enabled {(anim ? anim.enabled.ToString() : "-")}, playing {(anim ? anim.isPlaying.ToString() : "-")}";
        }

        /// <summary>Shows or hides the props the hero only holds during a move (propBones: Rat King's banner while he leaps).</summary>
        public void ShowProps(bool on)
        {
            if (propBones == null || propsShown == on) return;
            propsShown = on;
            for (int i = 0; i < propBones.Length; i++) if (propBones[i]) propBones[i].localScale = on ? propScale[i] : Vector3.zero;
        }

        /// <summary>King's Leap: one tucked forward spin of the whole body over the given time (the cape whips through it).</summary>
        public void Flip(float time = 0.5f)
        {
            if (!armature || dead) return;
            flipStart = Time.time; flipTime = time;
        }

        /// <summary>Holds the pose exactly as it is (The Cube: trapped in goo, nothing moves) until Freeze(false).</summary>
        public void Freeze(bool on)
        {
            frozen = on;
            if (anim) anim.enabled = !on;
        }

        void Update()
        {
            if (anim == null || frozen) return;
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
                // a jump in the air (King's Leap): read from the body's own climb, so every player's machine sees it, not only
                // the owner's (the jump count lives there)
                float y = transform.position.y, dt = Time.deltaTime;
                if (airLeap && dt > 0f)
                {
                    float vy = (y - lastY) / dt;
                    if (!grounded && !wasGrounded && vy - lastVy > 6f && vy > 4f && vy < 40f && Time.time > flipStart + flipTime + 0.1f) Flip();
                    lastVy = vy;
                }
                lastY = y;
                wasGrounded = grounded;
            }
            if (next != loco && clips.ContainsKey(next))
            {
                anim.CrossFade(next, 0.15f, PlayMode.StopSameLayer);
                loco = next;
            }
            if (loco != null && anim[loco] != null) anim[loco].speed = speed;
        }

        /// <summary>Switched off by the death ragdoll (RagdollController): stop posing the bones, let the gun fall with the
        /// hand, and drop the hurtboxes riding on the bones so they don't join the ragdoll's physics.</summary>
        void OnDisable()
        {
            if (!body || !body.healthComponent || body.healthComponent.alive) return;
            if (anim) { anim.Stop(); anim.enabled = false; }
            if (gunBone && gunHand) gunBone.SetParent(gunHand, true);
            foreach (var hb in GetComponentsInChildren<HurtBox>(true)) { var c = hb.GetComponent<Collider>(); if (c) c.enabled = false; }
        }

        void LateUpdate()
        {
            Rig();
            FlipPose();
            if (springs) springs.Step(Time.deltaTime);
        }

        /// <summary>The King's Leap spin: the skeleton turns once forward about the middle of the body.</summary>
        void FlipPose()
        {
            if (!armature) return;
            if (flipStart < 0f || dead) { armature.localRotation = Quaternion.identity; armature.localPosition = Vector3.zero; return; }
            float t = (Time.time - flipStart) / Mathf.Max(0.1f, flipTime);
            if (t >= 1f) { flipStart = -1f; armature.localRotation = Quaternion.identity; armature.localPosition = Vector3.zero; return; }
            // ease in and out, so he tucks, whips round and opens up
            float k = t * t * (3f - 2f * t);
            var r = Quaternion.AngleAxis(360f * k, Vector3.right);
            armature.localRotation = r;
            armature.localPosition = flipMid - r * flipMid;
        }

        void Rig()
        {
            if (anim == null || rigBones == null || frozen) return;
            bool free = dead || overrideClip != null || Time.time < armFreeUntil;
            armWeight = Mathf.MoveTowards(armWeight, free ? 0f : 1f, Time.deltaTime / 0.15f);

            // 1. gun arms rigid in the holding pose (no animation on them)
            if (armWeight > 0f)
                for (int i = 0; i < rigBones.Length; i++)
                {
                    var t = rigBones[i];
                    if (!t) continue;
                    t.localPosition = Vector3.Lerp(t.localPosition, rigPos[i], armWeight);
                    t.localRotation = Quaternion.Slerp(t.localRotation, rigRot[i], armWeight);
                }

            for (int i = 0; i < gunParts.Length; i++)
                if (gunParts[i]) { gunParts[i].localPosition = gunPartPos[i]; gunParts[i].localRotation = gunPartRot[i]; }

            // 2. turn the chest so the gun points where the camera aims; everything above spine_3 turns rigidly,
            //    so two-handed grips stay together
            if (inputs && mainHand && armWeight > 0f)
            {
                // yaw about the up axis, then pitch about the gun's own horizontal right axis: no roll, so the torso
                // leans and turns instead of twisting sideways
                var want = ClampAim(inputs.aimDirection);
                float wantYaw = Mathf.Atan2(want.x, want.z) * Mathf.Rad2Deg, wantPitch = Mathf.Asin(Mathf.Clamp(want.y, -1f, 1f)) * Mathf.Rad2Deg;
                float[] share = { 1f / 3f, 0.5f, 1f };
                for (int k = 0; k < spine.Length; k++)
                {
                    if (!spine[k]) continue;
                    var cur = mainHand.rotation * gunFwdLocal;
                    float yaw = Mathf.DeltaAngle(Mathf.Atan2(cur.x, cur.z) * Mathf.Rad2Deg, wantYaw);
                    var turn = Quaternion.AngleAxis(yaw * share[k] * armWeight, Vector3.up);
                    cur = turn * cur;
                    float pitch = wantPitch - Mathf.Asin(Mathf.Clamp(cur.y, -1f, 1f)) * Mathf.Rad2Deg;
                    var flat = new Vector3(cur.x, 0f, cur.z);
                    var axis = flat.sqrMagnitude > 1e-6f ? Vector3.Cross(flat.normalized, Vector3.up) : transform.right;
                    var tilt = Quaternion.AngleAxis(pitch * share[k] * armWeight, axis);
                    spine[k].rotation = tilt * turn * spine[k].rotation;
                }
                AimError = Vector3.Angle(mainHand.rotation * gunFwdLocal, inputs.aimDirection);
            }
            else AimError = -1f;

            // 3. recoil kick: the chest pitches up and settles in about 0.1 s
            if (recoil > 0f && spine.Length > 2 && spine[2])
            {
                float k = recoil * Mathf.Exp(-(Time.time - recoilTime) / 0.05f);
                if (k < 0.05f) recoil = 0f;
                else spine[2].rotation = Quaternion.AngleAxis(-k, transform.right) * spine[2].rotation;
            }

            // 4. a gun that is not a child of the hand (Abrams' shotgun) is welded to it, always
            if (gunBone && gunHand)
            {
                gunBone.position = gunHand.TransformPoint(gunOffPos);
                gunBone.rotation = gunHand.rotation * gunOffRot;
            }
            // (not while the gun's holder is hidden: Krill scaled to 0 while he is away on Tag Team)
            if (gunRoot && mainHand && mainHand.lossyScale.x > 1e-3f)
                GunDrift = Mathf.Max(GunDrift, Vector3.Distance(mainHand.InverseTransformPoint(gunRoot.position), gunRootInHand) * transform.lossyScale.x);
        }

        /// <summary>Limits how far the chest twists: pitch within 80 degrees and yaw within 70 degrees of where the body faces.</summary>
        Vector3 ClampAim(Vector3 aim)
        {
            var local = transform.InverseTransformDirection(aim.normalized);
            float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -70f, 70f); pitch = Mathf.Clamp(pitch, -80f, 80f);
            return transform.TransformDirection(Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward);
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
