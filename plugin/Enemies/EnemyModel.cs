#if OWN_FILES
using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadlockOfRain.Enemies
{
    /// <summary>An enemy's model (design/enemies.json; docs/11): every look of it (amber, sapphire) built from the cache under one
    /// root, one shown per spawn (EnemyLook), with a capsule hurtbox, a melee hitbox, a child locator and its own animator. Built
    /// the way ModelBuilder builds a hero (same materials, same clip conversion), without the hero's gun rig.</summary>
    public static class EnemyModel
    {
        public const string MeleeHitBoxGroup = "TrooperMelee";

        public static GameObject Build(EnemyRow row, float reach)
        {
            var root = new GameObject("mdlDeadlock" + row.Id);
            root.transform.localScale = Vector3.one * ModelBuilder.GlobalScale;
            var looks = new List<EnemyLook.Look>();
            foreach (var id in row.Models)
            {
                var look = BuildLook(root.transform, id, row);
                if (look != null) looks.Add(look);
            }
            if (looks.Count == 0) { Object.Destroy(root); return null; }
            // one look shown until EnemyLook picks this body's own (Start)
            for (int i = 1; i < looks.Count; i++) looks[i].Root.SetActive(false);

            // the child locator of the first look (every look has the same skeleton names); EnemyLook repoints it to the shown one
            var locator = root.AddComponent<ChildLocator>();
            AccessTools.Field(typeof(ChildLocator), "transformPairs").SetValue(locator, looks[0].Locator);

            // every look's renderers, always: CharacterModel turns each listed renderer on every frame, so the look not shown and the
            // health heads not shown are switched off through their GameObjects instead (EnemyLook)
            var all = new List<CharacterModel.RendererInfo>();
            foreach (var l in looks) all.AddRange(l.Infos);
            var model = root.AddComponent<CharacterModel>();
            model.baseRendererInfos = all.ToArray();
            model.autoPopulateLightInfos = true;
            var lk = root.AddComponent<EnemyLook>();
            lk.Looks = looks.ToArray();
            lk.Row = row.Id;

            // one capsule hurtbox around the body (Deadlock height, model space), as heroes have
            var hbGo = new GameObject("HurtBox");
            hbGo.layer = LayerIndex.entityPrecise.intVal;
            hbGo.transform.SetParent(root.transform, false);
            var cap = hbGo.AddComponent<CapsuleCollider>();
            cap.direction = 1;
            cap.height = row.SourceHeight;
            cap.radius = row.Width;
            cap.center = new Vector3(0, row.SourceHeight * 0.5f, 0);
            var hb = hbGo.AddComponent<HurtBox>();
            hb.isBullseye = true;
            hb.damageModifier = HurtBox.DamageModifier.Normal;
            var group = root.AddComponent<HurtBoxGroup>();
            group.hurtBoxes = new[] { hb };
            group.mainHurtBox = hb;
            group.bullseyeCount = 1;
            hb.hurtBoxGroup = group; hb.indexInGroup = 0;

            // the melee hitbox: reach metres in front, as wide as the body, from the knees to over the head (world metres, under the
            // model's scale)
            if (reach > 0f)
            {
                float s = ModelBuilder.GlobalScale;
                var box = new GameObject("MeleeHitBox");
                box.transform.SetParent(root.transform, false);
                float h = row.SourceHeight * s;
                box.transform.localPosition = new Vector3(0f, h * 0.55f, reach * 0.5f) / s;
                box.transform.localScale = new Vector3(Mathf.Max(1.6f, row.Width * 4f * s), h * 1.1f, reach) / s;
                var hitbox = box.AddComponent<HitBox>();
                var hbg = root.AddComponent<HitBoxGroup>();
                hbg.groupName = MeleeHitBoxGroup;
                hbg.hitBoxes = new[] { hitbox };
            }
            return root;
        }

        static EnemyLook.Look BuildLook(Transform root, string id, EnemyRow row)
        {
            var asset = DeadlockCache.LoadHero(id);
            if (asset == null) return null;
            var go = new GameObject(id);
            go.transform.SetParent(root, false);
            var armature = new GameObject("Armature").transform;
            armature.SetParent(go.transform, false);
            var bones = new Transform[asset.Bones.Length];
            for (int i = 0; i < bones.Length; i++) bones[i] = new GameObject(asset.Bones[i].Name).transform;
            for (int i = 0; i < bones.Length; i++)
            {
                var b = asset.Bones[i];
                bones[i].SetParent(b.Parent >= 0 ? bones[b.Parent] : armature, false);
                bones[i].localPosition = b.Pos; bones[i].localRotation = b.Rot; bones[i].localScale = b.Scale;
            }
            var infos = new List<CharacterModel.RendererInfo>();
            var health = new List<GameObject>[row.HealthMeshes.Length];
            for (int k = 0; k < health.Length; k++) health[k] = new List<GameObject>();
            var parts = asset.Parts;
            for (int s = 0; s < parts.Length; s++)
            {
                var def = asset.Materials[Mathf.Clamp(asset.Submeshes[s].Key, 0, asset.Materials.Length - 1)];
                // a health head mesh (healthMeshes, kept as "<material>@<mesh>" parts): shown by EnemyLook one at a time
                int healthPart = -1;
                if (def.Name != null && def.Name.Contains("@"))
                    healthPart = System.Array.IndexOf(row.HealthMeshes, def.Name.Substring(def.Name.IndexOf('@') + 1));
                var meshGo = new GameObject("DeadlockMesh_" + def.Name);
                meshGo.transform.SetParent(go.transform, false);
                var smr = meshGo.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = parts[s];
                smr.bones = bones;
                smr.rootBone = armature;
                smr.updateWhenOffscreen = true;
                var mat = ModelBuilder.MakeMaterial(asset, def);
                smr.sharedMaterial = mat;
                smr.shadowCastingMode = ShadowCastingMode.On;
                infos.Add(new CharacterModel.RendererInfo { renderer = smr, defaultMaterial = mat, defaultShadowCastingMode = ShadowCastingMode.On });
                if (healthPart >= 0) health[healthPart].Add(meshGo);
            }
            var pairs = new List<ChildLocator.NameTransformPair>();
            void Add(string name, string bone) { var t = ModelBuilder.Find(bones, bone); if (t) pairs.Add(new ChildLocator.NameTransformPair { name = name, transform = t }); }
            Add("Muzzle", row.MuzzleBone); Add("Muzzle", "hand_R");
            Add("Head", "head"); Add("Chest", "spine_1"); Add("Pelvis", "pelvis");
            Add("Flame", "flame_0"); Add("Flame", "wick_end"); Add("Flame", "head_end");
            Add("Banner", "banner_end");

            var anim = go.AddComponent<EnemyAnimator>();
            anim.Setup(id, row, asset, bones);
            var rb = BuildRagdoll(go, bones, row, anim);
            return new EnemyLook.Look { Root = go, Infos = infos.ToArray(), Locator = pairs.ToArray(), Health = health, Animator = anim, Ragdoll = rb };
        }

        /// <summary>A death ragdoll on the look's own bones (RoR2's RagdollController), as ModelBuilder gives heroes.</summary>
        static RagdollController BuildRagdoll(GameObject look, Transform[] bones, EnemyRow row, EnemyAnimator anim)
        {
            var list = new List<Transform>();
            foreach (var n in row.RagdollBones) { var t = n == "none" ? null : ModelBuilder.Find(bones, n); if (t) list.Add(t); }
            if (list.Count < 2) return null;
            var parentOf = new Dictionary<Transform, Transform>();
            foreach (var b in list)
                for (var p = b.parent; p; p = p.parent)
                    if (list.Contains(p)) { parentOf[b] = p; break; }
            foreach (var b in list)
            {
                Transform end = null;
                foreach (var c in list) if (parentOf.TryGetValue(c, out var pp) && pp == b) { end = c; break; }
                if (!end) { float far = 0f; foreach (Transform c in b) { float d = (c.position - b.position).sqrMagnitude; if (d > far) { far = d; end = c; } } }
                var seg = end ? b.InverseTransformPoint(end.position) : Vector3.up * 0.15f;
                if (seg.magnitude < 0.05f) seg = Vector3.up * 0.15f;
                b.gameObject.layer = LayerIndex.ragdoll.intVal;
                var body = b.gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true; body.mass = 5f;
                var cap = b.gameObject.AddComponent<CapsuleCollider>();
                cap.direction = Mathf.Abs(seg.x) > Mathf.Abs(seg.y) ? (Mathf.Abs(seg.x) > Mathf.Abs(seg.z) ? 0 : 2) : (Mathf.Abs(seg.y) > Mathf.Abs(seg.z) ? 1 : 2);
                cap.center = seg * 0.5f;
                cap.radius = Mathf.Max(0.01f, 0.3f * seg.magnitude);
                cap.height = seg.magnitude + cap.radius;
                cap.enabled = false;
            }
            foreach (var b in list)
            {
                if (!parentOf.TryGetValue(b, out var par)) continue;
                var j = b.gameObject.AddComponent<CharacterJoint>();
                j.connectedBody = par.GetComponent<Rigidbody>();
                j.enableProjection = true;
                j.lowTwistLimit = new SoftJointLimit { limit = -30f };
                j.highTwistLimit = new SoftJointLimit { limit = 30f };
                j.swing1Limit = new SoftJointLimit { limit = 50f };
                j.swing2Limit = new SoftJointLimit { limit = 40f };
            }
            var rc = look.AddComponent<RagdollController>();
            rc.bones = list.ToArray();
            rc.componentsToDisableOnRagdoll = new MonoBehaviour[] { anim };
            return rc;
        }
    }

    /// <summary>Which look of an enemy is shown (amber or sapphire, by its network id so every player sees the same one, creator
    /// 2026-11-11: "random each spawn"), the candle health readout (Deadlock's head meshes, full/mid/low by thirds of health) and the
    /// candle flame (a small light that goes out on death).</summary>
    public sealed class EnemyLook : MonoBehaviour
    {
        public sealed class Look
        {
            public GameObject Root;
            public CharacterModel.RendererInfo[] Infos;
            public ChildLocator.NameTransformPair[] Locator;
            public List<GameObject>[] Health;
            public EnemyAnimator Animator;
            public RagdollController Ragdoll;
        }

        public Look[] Looks;
        public string Row;
        public int Shown = -1;
        public int HealthStage = -1;
        Look look;
        CharacterBody body;
        Light flame;
        float flameBase;

        /// <summary>The animator of the look shown on this body (null before Start).</summary>
        public EnemyAnimator Animator => look?.Animator;
        /// <summary>The ragdoll of the look shown (its death state starts it).</summary>
        public RagdollController Ragdoll => look?.Ragdoll;
        public static EnemyLook Of(CharacterBody b) => b && b.modelLocator && b.modelLocator.modelTransform ? b.modelLocator.modelTransform.GetComponent<EnemyLook>() : null;

        void Start()
        {
            body = GetComponent<CharacterModel>() ? GetComponent<CharacterModel>().body : null;
            if (Looks == null || Looks.Length == 0) return;
            uint seed = body && body.networkIdentity ? body.networkIdentity.netId.Value : (uint)GetInstanceID();
            Pick((int)(seed % (uint)Looks.Length));
        }

        public void Pick(int k)
        {
            Shown = k;
            for (int i = 0; i < Looks.Length; i++) Looks[i].Root.SetActive(i == k);
            look = Looks[k];
            var locator = GetComponent<ChildLocator>();
            if (locator) AccessTools.Field(typeof(ChildLocator), "transformPairs").SetValue(locator, look.Locator);
            HealthStage = -1;
            // the candle flame: a warm point light at the wick (green on the Medic, whose candle burns green)
            var at = locator ? locator.FindChild("Flame") : null;
            if (at && !flame)
            {
                var go = new GameObject("CandleFlame");
                go.transform.SetParent(at, false);
                flame = go.AddComponent<Light>();
                flame.type = LightType.Point;
                flame.color = Row == "trooper_medic" ? new Color(0.45f, 1f, 0.5f) : new Color(1f, 0.7f, 0.3f);
                flame.range = 2.5f; flame.intensity = flameBase = 2.2f;
                flame.shadows = LightShadows.None;
            }
        }

        void Update()
        {
            if (look == null || look.Health.Length == 0) return;
            var hc = body ? body.healthComponent : null;
            float f = hc ? hc.combinedHealthFraction : 1f;
            bool dead = hc && !hc.alive;
            int n = look.Health.Length;
            // full health shows the first mesh; each third lost shows the next (Deadlock's candle melting down)
            int stage = Mathf.Clamp(Mathf.FloorToInt((1f - f) * n), 0, n - 1);
            if (stage != HealthStage)
            {
                HealthStage = stage;
                for (int i = 0; i < n; i++) foreach (var g in look.Health[i]) if (g) g.SetActive(i == stage);
            }
            if (flame)
            {
                if (dead) flame.enabled = false;
                else flame.intensity = flameBase * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 6f, GetInstanceID() * 0.01f)) * Mathf.Lerp(0.5f, 1f, f);
            }
        }
    }
}
#endif
