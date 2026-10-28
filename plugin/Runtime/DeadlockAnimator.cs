using System.Collections.Generic;
using System.Linq;
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
        // each clip's hip rotation (the upper body's parent, pelvis) per frame, so gestures lay the upper body on in model
        // space the way Deadlock layers them (see ModelSpaceGesture)
        static readonly Dictionary<string, Dictionary<string, HipCurve>> hipCache = new Dictionary<string, Dictionary<string, HipCurve>>();
        Dictionary<string, HipCurve> hips;
        string gestureClip;
        sealed class HipCurve { public Quaternion[] Rot; public float Fps; }
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
        /// <summary>The model's spring chains (null when its row has none): a move can kick them (Wraith's coat flaring on a throw).</summary>
        public SpringBones Springs => springs;
        // bones carrying a prop the hero only holds during a move (animations propBones: Rat King's banner), at scale 0 otherwise
        Transform[] propBones;
        Vector3[] propScale;
        bool propsShown;
        // King's Leap (heroes airJumpMult): a tucked forward spin of the whole skeleton on a jump in the air
        Transform armature;
        float flipStart = -1f, flipTime, lastY, lastVy, airSince;
        // a held pose played as the locomotion (animations float: Mina's umbrella glide; her Nox Nostra flight), the arm without the
        // gun let go of the rig meanwhile so the clip can hold the umbrella up
        string holdLogical;
        bool[] rigOff;
        float offWeight = 1f;
        // while a held pose plays, gestures (a reload) leave that arm to it: the upper body's bones without the off arm, and which
        // mask each gesture clip has now (true: without the off arm) (creator, 2026-10-26: her hand left the umbrella on a reload)
        List<Transform> upperNoOff;
        readonly Dictionary<string, bool> maskNoOff = new Dictionary<string, bool>();
        // animations holdBones: a prop on the root (Mina's umbrella) kept in its hand the way each clip holds it, whatever the gun
        // rig does to that arm
        Transform[] weldBone, weldHand;
        Vector3[] weldPos;
        Quaternion[] weldRot;
        int lastJumpCount;
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
            hipCache.TryGetValue(HeroId, out hips);
            rows = Sheet.Anims(HeroId);
            logical["idle"] = rows.Idle; logical["run_n"] = rows.RunN; logical["run_s"] = rows.RunS; logical["run_e"] = rows.RunE; logical["run_w"] = rows.RunW;
            logical["sprint"] = rows.Sprint; logical["jump_start"] = rows.JumpStart; logical["jump_air"] = rows.JumpAir; logical["land"] = rows.Land;
            logical["fire"] = rows.Fire; logical["fire_alt"] = rows.FireAlt; logical["reload"] = rows.Reload;
            logical["secondary"] = rows.Secondary; logical["secondary_air"] = rows.SecondaryAir; logical["utility"] = rows.Utility; logical["utility_loop"] = rows.UtilityLoop;
            logical["special_start"] = rows.SpecialStart; logical["special"] = rows.Special;
            logical["ult_start"] = rows.UltStart; logical["ult_loop"] = rows.UltLoop; logical["ult_end"] = rows.UltEnd; logical["death"] = rows.Death;
            logical["special_loop"] = rows.SpecialLoop; logical["melee"] = rows.Melee; logical["melee_alt"] = rows.MeleeAlt;
            logical["float"] = rows.Float;
            logical["melee_heavy"] = rows.MeleeHeavy; logical["melee_air"] = rows.MeleeAir; logical["dash_air"] = rows.DashAir; logical["slam"] = rows.Slam;
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
            // restBones (Pocket's briefcase lid): their bind pose, put back when a gesture or override that moved them ends
            var rest = new List<Transform>();
            foreach (var n in rows.RestBones) { var t = n == "none" ? null : Bone(n); if (t) rest.Add(t); }
            restBones = rest.ToArray();
            restRot = restBones.Select(t => t.localRotation).ToArray();
            restPos = restBones.Select(t => t.localPosition).ToArray();
            var wb = new List<Transform>(); var wh = new List<Transform>();
            foreach (var pair in rows.HoldBones)
            {
                var parts = pair.Split('>');
                if (parts.Length != 2) continue;
                var b = Bone(parts[0].Trim()); var h = Bone(parts[1].Trim());
                if (b && h) { wb.Add(b); wh.Add(h); }
            }
            weldBone = wb.ToArray(); weldHand = wh.ToArray();
            weldPos = new Vector3[weldBone.Length]; weldRot = new Quaternion[weldBone.Length];
            holdLogical = null;
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
            var off = new List<bool>();
            foreach (var side in rows.GunArms == "LR" ? new[] { "R", "L" } : new[] { "R" })
            {
                var clav = Bone(rig + "clavicle_" + side);
                if (!clav) continue;
                // the arm whose hand doesn't hold the gun (Mina's umbrella arm) is let go while a held pose plays
                bool isOff = mainHand && !mainHand.IsChildOf(clav);
                foreach (var t in clav.GetComponentsInChildren<Transform>(true)) if (!gun.Contains(t)) { arms.Add(t); off.Add(isOff); }
                if (isOff && upperBody && clav.IsChildOf(upperBody))
                    upperNoOff = upperBody.GetComponentsInChildren<Transform>(true).Where(t => !t.IsChildOf(clav)).ToList();
            }
            Snapshot(arms, out rigBones, out rigPos, out rigRot);
            rigOff = off.ToArray();
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
            // (props build their clips here too: Rat King's rats have no animations row)
            var rows = AnimRow.All.FirstOrDefault(a => a.Hero == hero);
            int ub = rows != null ? System.Array.FindIndex(asset.Bones, b => b.Name == rows.UpperBodyBone) : -1;
            int hip = ub >= 0 ? asset.Bones[ub].Parent : -1;
            var hipCurves = new Dictionary<string, HipCurve>();
            // steadyClips: the hips keep the idle's facing (the clip's own spin taken out), measured along the hip axis
            // that points forward in the idle's first frame
            var steady = new HashSet<string>(rows != null ? rows.SteadyClips : new string[0]);
            Vector3 hipFwd = Vector3.forward;
            if (hip >= 0 && rows != null && asset.Clips.TryGetValue(rows.Idle, out var idleDef))
            {
                var idleHip = RootFolded(asset, idleDef, rm).Find(t => t.Bone == hip);
                hipFwd = Quaternion.Inverse(idleHip != null && idleHip.Rot.Length > 0 ? idleHip.Rot[0] : asset.Bones[hip].Rot) * Vector3.forward;
            }
            foreach (var c in asset.Clips.Values)
            {
                var clip = new AnimationClip { legacy = true, name = c.Name, frameRate = c.Fps };
                // (a copy: RootFolded hands back the asset's own list when the clip has no root track)
                var tracks = new List<TrackDef>(RootFolded(asset, c, rm));
                if (hip >= 0)
                {
                    int k = tracks.FindIndex(t => t.Bone == hip);
                    if (k >= 0 && steady.Contains(c.Name)) tracks[k] = Steady(tracks[k], hipFwd);
                    var rot = k >= 0 && tracks[k].Rot.Length > 0 ? tracks[k].Rot : new[] { asset.Bones[hip].Rot };
                    hipCurves[c.Name] = new HipCurve { Rot = rot, Fps = c.Fps };
                }
                foreach (var t in tracks)
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
            hipCache[hero] = hipCurves;
            return result;
        }

        /// <summary>The hip track with its turn about the model's up axis taken out: every frame faces where the idle's hips
        /// face (hipFwd, in hip space), so a clip that spins the whole body round (Seven's Storm Cloud hover) keeps its
        /// tilt, sway and limbs but faces where RoR2 turns the body.</summary>
        static TrackDef Steady(TrackDef t, Vector3 hipFwd)
        {
            var rot = new Quaternion[t.Rot.Length];
            for (int f = 0; f < rot.Length; f++)
            {
                var d = t.Rot[f] * hipFwd;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                rot[f] = Quaternion.AngleAxis(-yaw, Vector3.up) * t.Rot[f];
            }
            return new TrackDef { Bone = t.Bone, Pos = t.Pos, Rot = rot };
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
            MaskUpper(st, c, holdLogical != null && upperNoOff != null);
            st.layer = 1;
            st.speed = speed;
            st.time = 0;
            anim.CrossFade(c, 0.05f, PlayMode.StopSameLayer);
            gestureClip = c;
            // reload and ability clips move the arms; landing/jump layers leave the gun arms in their holding pose
            if (logicalName != "land" && logicalName != "jump_start") armFreeUntil = Mathf.Max(armFreeUntil, Time.time + st.length / Mathf.Max(0.1f, speed));
        }

        /// <summary>A gesture clip plays on the upper body; with noOff (a held pose playing) on every upper-body bone but the off
        /// arm, so that arm keeps the held pose (Mina's umbrella hand stays up through a reload).</summary>
        void MaskUpper(AnimationState st, string c, bool noOff)
        {
            if (!upperBody) return;
            if (maskNoOff.TryGetValue(c, out var was) && was == noOff) return;
            Unmask(st, c);
            if (noOff) foreach (var t in upperNoOff) st.AddMixingTransform(t, false);
            else st.AddMixingTransform(upperBody, true);
            maskNoOff[c] = noOff;
        }

        /// <summary>Takes a clip's upper-body mask off again (an override plays it on the whole body).</summary>
        void Unmask(AnimationState st, string c)
        {
            if (!upperBody || !maskNoOff.TryGetValue(c, out var was)) return;
            if (was) foreach (var t in upperNoOff) st.RemoveMixingTransform(t);
            else st.RemoveMixingTransform(upperBody);
            maskNoOff.Remove(c);
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
            Unmask(st, c);
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

        /// <summary>The clip's length in seconds at speed 1 (0 when the hero has none): a move fits its clip to its own time.</summary>
        public float ClipLength(string logicalName)
        {
            var c = Clip(logicalName);
            return c != null && anim && anim[c] != null ? anim[c].length : 0f;
        }

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

        /// <summary>Plays a held pose as the locomotion until Hold(null) (Mina: "float" while her umbrella is open, "special_loop" in
        /// Nox Nostra's flight): the gun arm keeps aiming, the other arm follows the clip.</summary>
        public void Hold(string logicalName)
        {
            holdLogical = logicalName != null && Clip(logicalName) != null ? logicalName : null;
        }

        /// <summary>A held pose is playing (the test asks).</summary>
        public string Holding => holdLogical;

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
            var held = Clip(holdLogical);
            if (held != null)
            {
                // a held pose: loops on the locomotion layer, whatever the body does
                var hs = anim[held];
                hs.layer = 0; hs.wrapMode = WrapMode.Loop;
                if (held != loco) { anim.CrossFade(held, 0.2f, PlayMode.StopSameLayer); loco = held; }
                anim[held].speed = 1f;
                if (motor) wasGrounded = motor.isGrounded;
                return;
            }
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
                    if (grounded) airSince = Time.time;
                    bool ready = Time.time > flipStart + flipTime + 0.1f;
                    if (body && body.hasEffectiveAuthority)
                    {
                        // the owner knows the jump count exactly: only a jump made in the air (the second) spins him
                        if (motor.jumpCount > lastJumpCount && lastJumpCount >= 1 && !grounded && ready) Flip();
                        lastJumpCount = motor.jumpCount;
                    }
                    // other players' machines: a sharp climb, but only once he has been in the air a while (the take-off's
                    // first frames can still read grounded while he already rises)
                    else if (!grounded && !wasGrounded && Time.time - airSince > 0.25f && vy - lastVy > 6f && vy > 4f && vy < 40f && ready) Flip();
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

        Transform[] restBones; Quaternion[] restRot; Vector3[] restPos;

        /// <summary>restBones (Pocket's briefcase lid) stay at their bind pose while no gesture or override plays: the case is shut
        /// whenever it just sits in his hand (creator, 2026-10-28).</summary>
        void RestPose()
        {
            if (restBones == null || restBones.Length == 0 || anim == null) return;
            // held open by a move (Barrage's volley): the open pose from the secondary_air clip (Deadlock's open briefcase)
            if (Time.time < openUntil)
            {
                if (openRot == null) openRot = SamplePose("secondary_air", restBones, 0f);
                if (openRot != null) { for (int i = 0; i < restBones.Length; i++) if (restBones[i]) restBones[i].localRotation = openRot[i]; return; }
            }
            if (MovePlaying()) return;
            for (int i = 0; i < restBones.Length; i++) if (restBones[i]) { restBones[i].localRotation = restRot[i]; restBones[i].localPosition = restPos[i]; }
        }

        // runArm: the arm's pose over the jump_air clip, sampled once (every bone under the arm's root, RunArmFrames frames)
        const int RunArmFrames = 16;
        Transform[] runArm; Quaternion[][] runArmRot; float runArmLen, runArmWeight; bool runArmTried;

        /// <summary>runArm (Pocket's briefcase arm): while he runs on the ground and no move plays, that arm takes its pose from the
        /// jump_air clip (the case trailing behind, as in his jump) instead of the run's swing, blended in with his speed.</summary>
        void RunArm()
        {
            if (anim == null || rows == null || rows.RunArm == "none" || dead) return;
            if (!runArmTried) { runArmTried = true; SampleRunArm(); }
            if (runArm == null) return;
            bool busy = MovePlaying() || holdLogical != null;
            var m = body ? body.characterMotor : null;
            float speed = 0f;
            if (m) { var v = m.velocity; v.y = 0f; speed = v.magnitude; }
            // sprinting only: walking keeps the run clip's own arm (creator, 2026-10-28: "a little to far behind while walking")
            bool running = m && m.isGrounded && !busy && body.isSprinting;
            float want = running ? Mathf.Clamp01((speed - 1f) / 2f) : 0f;
            runArmWeight = Mathf.MoveTowards(runArmWeight, want, Time.deltaTime / 0.15f);
            if (runArmWeight <= 0.001f) return;
            float f = Mathf.Repeat(Time.time / Mathf.Max(0.1f, runArmLen), 1f) * RunArmFrames;
            int i0 = (int)f % RunArmFrames, i1 = (i0 + 1) % RunArmFrames;
            float k = f - Mathf.Floor(f);
            for (int b = 0; b < runArm.Length; b++)
            {
                var pose = Quaternion.Slerp(runArmRot[i0][b], runArmRot[i1][b], k);
                runArm[b].localRotation = Quaternion.Slerp(runArm[b].localRotation, pose, runArmWeight);
            }
        }

        /// <summary>A move's clip is playing: any gesture or override layer clip still running (seen from its own state, so a clip
        /// cross-fading in on its first frame counts; the gesture field is cleared while its weight is still 0).</summary>
        bool MovePlaying()
        {
            if (overrideClip != null) return true;
            foreach (AnimationState st in anim) if (st.layer >= 1 && st.enabled && (st.weight > 0.001f || st.time < st.length)) return true;
            return false;
        }

        float openUntil;
        Quaternion[] openRot;

        /// <summary>A move holds the restBones open (Pocket's briefcase lid while Barrage throws its volley) for seconds from now.</summary>
        public void HoldOpen(float seconds) => openUntil = Mathf.Max(openUntil, Time.time + seconds);

        /// <summary>The local rotations of bones at time t of a logical clip, sampled once (the rest of the pose put back); null without the clip.</summary>
        Quaternion[] SamplePose(string logicalName, Transform[] of, float t)
        {
            var c = Clip(logicalName);
            if (c == null || anim[c] == null) return null;
            var clip = anim[c].clip;
            var all = GetComponentsInChildren<Transform>(true);
            var savedR = all.Select(x => x.localRotation).ToArray();
            var savedP = all.Select(x => x.localPosition).ToArray();
            clip.SampleAnimation(gameObject, Mathf.Min(t, clip.length));
            var r = of.Select(x => x ? x.localRotation : Quaternion.identity).ToArray();
            for (int i = 0; i < all.Length; i++) { all[i].localRotation = savedR[i]; all[i].localPosition = savedP[i]; }
            return r;
        }

        void SampleRunArm()
        {
            var c = Clip("jump_air");
            Transform root = null;
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == rows.RunArm) { root = t; break; }
            if (c == null || !root || anim[c] == null) return;
            var clip = anim[c].clip;
            var all = GetComponentsInChildren<Transform>(true);
            var savedR = all.Select(t => t.localRotation).ToArray();
            var savedP = all.Select(t => t.localPosition).ToArray();
            runArm = root.GetComponentsInChildren<Transform>(true);
            runArmLen = clip.length;
            runArmRot = new Quaternion[RunArmFrames][];
            for (int f = 0; f < RunArmFrames; f++)
            {
                clip.SampleAnimation(gameObject, clip.length * f / RunArmFrames);
                runArmRot[f] = runArm.Select(t => t.localRotation).ToArray();
            }
            for (int i = 0; i < all.Length; i++) { all[i].localRotation = savedR[i]; all[i].localPosition = savedP[i]; }
        }

        void LateUpdate()
        {
            RunArm();
            RestPose();
            CaptureWelds();
            ModelSpaceGesture();
            Rig();
            ApplyWelds();
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

        /// <summary>Deadlock lays an upper-body clip on in model space: the chest keeps the clip's own facing whatever the legs
        /// do. Unity's mixing transform keeps the clip's spine relative to the legs' hips instead, which adds the clip's twist to
        /// the hips' own (Seven's static_charge_upper_body turned his head ~100 deg sideways). So the upper body is turned by
        /// the difference between the clip's hips and the hips actually playing, in proportion to the gesture's weight.</summary>
        void ModelSpaceGesture()
        {
            if (gestureClip == null || hips == null || !upperBody || !upperBody.parent || frozen || dead) return;
            var st = anim[gestureClip];
            if (st == null || !st.enabled || st.weight <= 0f) { gestureClip = null; return; }
            if (overrideClip != null || !hips.TryGetValue(gestureClip, out var h)) return;
            int last = h.Rot.Length - 1;
            float f = Mathf.Clamp(st.time * h.Fps, 0f, last);
            int i = Mathf.Min((int)f, last);
            var clipHip = Quaternion.Slerp(h.Rot[i], h.Rot[Mathf.Min(i + 1, last)], f - i);
            var delta = Quaternion.Inverse(upperBody.parent.localRotation) * clipHip;
            upperBody.localRotation = Quaternion.Slerp(Quaternion.identity, delta, Mathf.Clamp01(st.weight)) * upperBody.localRotation;
        }

        void Rig()
        {
            if (anim == null || rigBones == null || frozen) return;
            bool free = dead || overrideClip != null || Time.time < armFreeUntil;
            armWeight = Mathf.MoveTowards(armWeight, free ? 0f : 1f, Time.deltaTime / 0.15f);
            offWeight = Mathf.MoveTowards(offWeight, holdLogical != null ? 0f : 1f, Time.deltaTime / 0.2f);

            // 1. gun arms rigid in the holding pose (no animation on them); the off arm let go while a held pose plays
            if (armWeight > 0f)
                for (int i = 0; i < rigBones.Length; i++)
                {
                    var t = rigBones[i];
                    if (!t) continue;
                    float w = rigOff != null && i < rigOff.Length && rigOff[i] ? Mathf.Min(armWeight, offWeight) : armWeight;
                    if (w <= 0f) continue;
                    t.localPosition = Vector3.Lerp(t.localPosition, rigPos[i], w);
                    t.localRotation = Quaternion.Slerp(t.localRotation, rigRot[i], w);
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

        /// <summary>holdBones: where each clip puts the prop in its hand, read before the gesture and gun rig move that hand.</summary>
        void CaptureWelds()
        {
            if (weldBone == null || frozen || dead) return;
            for (int i = 0; i < weldBone.Length; i++)
            {
                if (!weldBone[i] || !weldHand[i]) continue;
                weldPos[i] = weldHand[i].InverseTransformPoint(weldBone[i].position);
                weldRot[i] = Quaternion.Inverse(weldHand[i].rotation) * weldBone[i].rotation;
            }
        }

        /// <summary>holdBones: the prop put back in its hand after the hand moved.</summary>
        void ApplyWelds()
        {
            if (weldBone == null || frozen || dead) return;
            for (int i = 0; i < weldBone.Length; i++)
            {
                if (!weldBone[i] || !weldHand[i]) continue;
                weldBone[i].position = weldHand[i].TransformPoint(weldPos[i]);
                weldBone[i].rotation = weldHand[i].rotation * weldRot[i];
            }
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
