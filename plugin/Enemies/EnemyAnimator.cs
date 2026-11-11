#if OWN_FILES
using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain.Enemies
{
    /// <summary>Plays an enemy look's Deadlock clips (enemies.json idle/runN.../jumpAir, the attacks' clips) with the legacy
    /// Animation component, as DeadlockAnimator does for heroes but without the gun rig: locomotion blended from the body's
    /// velocity on layer 0, attacks and flinches as one-shot gestures on layer 1.</summary>
    public sealed class EnemyAnimator : MonoBehaviour
    {
        public string ModelId;
        public string RowId;
        Animation anim;
        EnemyRow row;
        CharacterBody body;
        string current;
        float gestureUntil;

        public void Setup(string modelId, EnemyRow r, HeroAsset asset, Transform[] bones)
        {
            ModelId = modelId; RowId = r.Id;
            var a = gameObject.AddComponent<Animation>();
            a.playAutomatically = false;
            a.cullingType = AnimationCullingType.AlwaysAnimate;
            foreach (var kv in DeadlockAnimator.BuildClips(modelId, asset, bones, transform)) a.AddClip(kv.Value, kv.Key);
        }

        void Init()
        {
            if (anim) return;
            anim = GetComponent<Animation>();
            row = Sheet.Enemy(RowId);
            foreach (AnimationState st in anim)
            {
                bool loco = row != null && (st.name == row.Idle || st.name == row.RunN || st.name == row.RunS || st.name == row.RunE || st.name == row.RunW || st.name == row.JumpAir);
                st.layer = loco ? 0 : 1;
                st.wrapMode = loco ? WrapMode.Loop : WrapMode.Once;
            }
        }

        void Start() { Init(); body = GetComponentInParent<CharacterModel>()?.body; }

        public bool Has(string clip) { Init(); return anim && clip != "none" && anim[clip] != null; }

        /// <summary>Plays a clip once over the locomotion (an attack, a flinch). speed: its rate (1 = Deadlock's).</summary>
        public void Gesture(string clip, float speed = 1f)
        {
            if (!Has(clip)) return;
            var st = anim[clip];
            st.speed = speed;
            anim.Stop(clip);
            anim.CrossFade(clip, 0.08f);
            gestureUntil = Time.time + st.length / Mathf.Max(0.05f, speed) - 0.08f;
        }

        public float Length(string clip) => Has(clip) ? anim[clip].length : 0f;

        /// <summary>The clips playing now, with their weights and times (test-mode logging).</summary>
        public string Describe()
        {
            Init();
            if (!anim) return "no animation";
            var parts = new List<string>();
            foreach (AnimationState st in anim) if (st.enabled) parts.Add($"{st.name} L{st.layer} w{st.weight:F2} t{st.normalizedTime:F2}");
            return parts.Count > 0 ? string.Join(" | ", parts) : "nothing playing";
        }

        void LateUpdate()
        {
            if (!anim || row == null) return;
            if (!body) body = GetComponentInParent<CharacterModel>()?.body;
            var motor = body ? body.characterMotor : null;
            Vector3 v = motor ? motor.velocity : Vector3.zero;
            v.y = 0f;
            float speed = v.magnitude;
            string want = row.Idle;
            float rate = 1f;
            if (motor && !motor.isGrounded) want = row.JumpAir;
            else if (speed > 0.6f)
            {
                var local = transform.InverseTransformDirection(v / speed);
                if (Mathf.Abs(local.z) >= Mathf.Abs(local.x)) want = local.z >= 0 ? row.RunN : row.RunS;
                else want = local.x >= 0 ? row.RunE : row.RunW;
                // the clips are made for Deadlock's trooper pace (about 5 m/s): run them faster or slower with the body
                rate = Mathf.Clamp(speed / 5f, 0.6f, 1.8f);
            }
            if (!Has(want)) want = row.Idle;
            if (!Has(want)) return;
            anim[want].speed = rate;
            if (want != current || !anim.IsPlaying(want)) { anim.CrossFade(want, 0.2f); current = want; }
            // a gesture that finished hands the pose back to the locomotion
            if (Time.time > gestureUntil)
                foreach (AnimationState st in anim) if (st.layer == 1 && st.enabled && st.normalizedTime >= 1f) st.enabled = false;
        }
    }
}
#endif
