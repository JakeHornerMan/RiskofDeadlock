using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadlockOfRain
{
    /// <summary>Builds a hero's model (skeleton, skinned mesh, materials, hurtbox, child locator, animator) from the Deadlock cache.</summary>
    public static class ModelBuilder
    {
        /// <summary>Global Deadlock->RoR2 scale: the anchor hero (Haze) is made as tall as Commando; every hero gets the same factor,
        /// so their relative Deadlock heights are kept.</summary>
        public static float GlobalScale = 0.75f;
        public static Material Template;

        /// <param name="onlyMaterials">keep only the parts whose material name contains this ("rider": just Krill, for his figure).</param>
        public static GameObject Build(HeroRow hero, bool forDisplay, string onlyMaterials = null)
        {
            var asset = DeadlockCache.LoadHero(hero.Id);
            if (asset == null) return null;
            var root = new GameObject("mdlDeadlock" + Survivors.CodeName(hero));
            // modelScale: an extra even scale on top of the shared factor (Mo & Krill are scaled up to Abrams' height)
            root.transform.localScale = Vector3.one * GlobalScale * hero.ModelScale;

            // skeleton
            var armature = new GameObject("Armature").transform;
            armature.SetParent(root.transform, false);
            var bones = new Transform[asset.Bones.Length];
            for (int i = 0; i < bones.Length; i++) bones[i] = new GameObject(asset.Bones[i].Name).transform;
            for (int i = 0; i < bones.Length; i++)
            {
                var b = asset.Bones[i];
                bones[i].SetParent(b.Parent >= 0 ? bones[b.Parent] : armature, false);
                bones[i].localPosition = b.Pos;
                bones[i].localRotation = b.Rot;
                bones[i].localScale = b.Scale;
            }
            // Deadlock exports cloth bones ($cloth_*, Abrams' coat) without a parent, so they would stay put while the body
            // moves and stretch the coat: attach each to the nearest of the sheet's clothParents, keeping its rest pose
            var anims = Sheet.Anims(hero.Id);
            var clothParents = new List<Transform>();
            foreach (var n in anims.ClothParents) { var t = n == "none" ? null : Find(bones, n); if (t) clothParents.Add(t); }
            if (clothParents.Count > 0)
                for (int i = 0; i < bones.Length; i++)
                {
                    if (asset.Bones[i].Parent >= 0 || !asset.Bones[i].Name.StartsWith("$cloth")) continue;
                    Transform best = null; float bd = float.MaxValue;
                    foreach (var c in clothParents) { float d = (c.position - bones[i].position).sqrMagnitude; if (d < bd) { bd = d; best = c; } }
                    bones[i].SetParent(best, true);
                }

            // one skinned renderer per material: RoR2's CharacterModel drives exactly one material per renderer
            var parts = asset.Parts;
            var infos = new List<CharacterModel.RendererInfo>();
            for (int s = 0; s < parts.Length; s++)
            {
                var def = asset.Materials[Mathf.Clamp(asset.Submeshes[s].Key, 0, asset.Materials.Length - 1)];
                if (onlyMaterials != null && (def.Name == null || def.Name.IndexOf(onlyMaterials, System.StringComparison.OrdinalIgnoreCase) < 0)) continue;
                var meshGo = new GameObject("DeadlockMesh_" + def.Name);
                meshGo.transform.SetParent(root.transform, false);
                var smr = meshGo.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = parts[s];
                smr.bones = bones;
                smr.rootBone = armature;
                smr.updateWhenOffscreen = true;
                var mat = MakeMaterial(asset, def);
                smr.sharedMaterial = mat;
                smr.shadowCastingMode = ShadowCastingMode.On;
                infos.Add(new CharacterModel.RendererInfo { renderer = smr, defaultMaterial = mat, defaultShadowCastingMode = ShadowCastingMode.On, ignoreOverlays = false });
            }

            // child locator (muzzles and anchors used by skills and effects)
            var locator = root.AddComponent<ChildLocator>();
            var pairs = new List<ChildLocator.NameTransformPair>();
            void Add(string name, string bone)
            {
                var t = Find(bones, bone);
                if (t) pairs.Add(new ChildLocator.NameTransformPair { name = name, transform = t });
            }
            // the end of the gun first (shots, tracers, muzzle flashes and thrown abilities start there), the hand otherwise
            if (anims.Muzzle != "none") Add("MuzzleRight", anims.Muzzle);
            if (anims.MuzzleLeft != "none") Add("MuzzleLeft", anims.MuzzleLeft);
            // the gun rig's bones may carry a prefix (Krill, who rides on Mo, holds the gun: rider_hand_R)
            var rig = anims.RigPrefix == "none" ? "" : anims.RigPrefix;
            Add("MuzzleRight", rig + "weapon_hand_R"); Add("MuzzleRight", rig + "hand_R");
            Add("MuzzleLeft", rig + "weapon_hand_L"); Add("MuzzleLeft", rig + "hand_L");
            Add("Head", "head"); Add("Chest", "spine_2"); Add("Pelvis", "pelvis");
            Add("HandR", "hand_R"); Add("HandL", "hand_L");
            AccessTools.Field(typeof(ChildLocator), "transformPairs").SetValue(locator, pairs.ToArray());

            var anim = root.AddComponent<DeadlockAnimator>();
            anim.Setup(hero.Id, asset, bones, armature);

            var model = root.AddComponent<CharacterModel>();
            model.baseRendererInfos = infos.ToArray();
            model.autoPopulateLightInfos = true;

            if (!forDisplay)
            {
                // one capsule hurtbox around the body, sized from the sheet (Deadlock height -> RoR2)
                var hbGo = new GameObject("HurtBox");
                hbGo.layer = LayerIndex.entityPrecise.intVal;
                hbGo.transform.SetParent(root.transform, false);
                var height = hero.SourceHeight;          // model space (before GlobalScale)
                var cap = hbGo.AddComponent<CapsuleCollider>();
                cap.direction = 1;
                cap.height = height;
                cap.radius = 0.3f * height * hero.WidthScale / hero.HeightScale;
                cap.center = new Vector3(0, height * 0.5f, 0);
                var hb = hbGo.AddComponent<HurtBox>();
                hb.isBullseye = true;
                hb.isSniperTarget = false;
                hb.damageModifier = HurtBox.DamageModifier.Normal;
                var sniper = new GameObject("HeadHurtBox");
                sniper.layer = LayerIndex.entityPrecise.intVal;
                var head = Find(bones, "head");
                sniper.transform.SetParent(head ? head : root.transform, false);
                var sc = sniper.AddComponent<SphereCollider>();
                sc.radius = 0.18f;
                var shb = sniper.AddComponent<HurtBox>();
                shb.isSniperTarget = true;
                var group = root.AddComponent<HurtBoxGroup>();
                group.hurtBoxes = new[] { hb, shb };
                group.mainHurtBox = hb;
                group.bullseyeCount = 1;
                hb.hurtBoxGroup = group; hb.indexInGroup = 0;
                shb.hurtBoxGroup = group; shb.indexInGroup = 1;

                BuildRagdoll(root, bones, anims, anim);
            }
            return root;
        }

        /// <summary>Death ragdoll, as vanilla survivors have: RoR2's RagdollController on the model, and on each of the sheet's
        /// ragdollBones a kinematic Rigidbody, a capsule along the bone (to the next listed bone below it, or its farthest child;
        /// radius ragdollThickness x that length) and a CharacterJoint to the nearest listed bone above it. The body's death
        /// state (Commando's, cloned) calls BeginRagdoll: the bones go limp and DeadlockAnimator switches off.</summary>
        static void BuildRagdoll(GameObject root, Transform[] bones, AnimRow anims, DeadlockAnimator anim)
        {
            var list = new List<Transform>();
            foreach (var n in anims.RagdollBones) { var t = n == "none" ? null : Find(bones, n); if (t) list.Add(t); else if (n != "none") Log.Warn($"ragdoll: no bone {n}"); }
            if (list.Count < 2) return;
            var parentOf = new Dictionary<Transform, Transform>();
            foreach (var b in list)
                for (var p = b.parent; p; p = p.parent)
                    if (list.Contains(p)) { parentOf[b] = p; break; }
            var length = new Dictionary<Transform, float>();
            foreach (var b in list)
            {
                // the segment this bone covers, in its own space (so the model's scale is already in it)
                Transform end = null;
                foreach (var c in list) if (parentOf.TryGetValue(c, out var pp) && pp == b) { end = c; break; }
                if (!end) { float far = 0f; foreach (Transform c in b) { float d = (c.position - b.position).sqrMagnitude; if (d > far) { far = d; end = c; } } }
                var seg = end ? b.InverseTransformPoint(end.position) : Vector3.zero;
                float parentLen = parentOf.TryGetValue(b, out var par) && length.TryGetValue(par, out var pl) ? pl : seg.magnitude;
                if (seg.magnitude < 0.3f * parentLen)
                {
                    // an end bone with no real child (the head): a ball half its parent's length, pushed out along the chain
                    var dir = par ? b.InverseTransformDirection(b.position - par.position).normalized : Vector3.up;
                    seg = dir * parentLen * 0.6f;
                }
                length[b] = seg.magnitude;
                b.gameObject.layer = LayerIndex.ragdoll.intVal;
                var rb = b.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true; rb.mass = 5f;
                var cap = b.gameObject.AddComponent<CapsuleCollider>();
                var a = Mathf.Abs(seg.x) > Mathf.Abs(seg.y) ? (Mathf.Abs(seg.x) > Mathf.Abs(seg.z) ? 0 : 2) : (Mathf.Abs(seg.y) > Mathf.Abs(seg.z) ? 1 : 2);
                cap.direction = a;
                cap.center = seg * 0.5f;
                cap.radius = Mathf.Max(0.01f, anims.RagdollThickness * seg.magnitude);
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
            var rc = root.AddComponent<RagdollController>();
            rc.bones = list.ToArray();
            rc.componentsToDisableOnRagdoll = new MonoBehaviour[] { anim };
        }

        public static Transform Find(Transform[] bones, string name)
        {
            foreach (var b in bones) if (b.name == name) return b;
            return null;
        }

        static Material MakeMaterial(HeroAsset asset, MaterialDef def)
        {
            Material m = Template ? new Material(Template) : new Material(Shader.Find("Standard"));
            m.name = "matDeadlock_" + def.Name;
            // the hero meshes carry no vertex colours/limb masks: turn off the template's mesh-data driven features
            foreach (var k in m.shaderKeywords)
                if (k.Contains("LIMB") || k.Contains("SPLATMAP") || k.Contains("FLOWMAP") || k.Contains("VERTEX") || k.Contains("FORCE_SPEC") || k.Contains("SKIN"))
                    m.DisableKeyword(k);
            var albedo = asset.Texture(def.Albedo, false);
            if (albedo)
            {
                m.mainTexture = albedo;
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", albedo);
            }
            // RoR2's limb removal hides vertices by a per-vertex limb mask Deadlock meshes don't have: off
            if (m.HasProperty("_LimbRemovalOn")) m.SetFloat("_LimbRemovalOn", 0f);
            m.DisableKeyword("LIMBREMOVAL");
            if (m.HasProperty("_Color")) m.SetColor("_Color", def.Color);
            if (m.HasProperty("_NormalTex")) m.SetTexture("_NormalTex", null);
            if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", null);
            if (m.HasProperty("_EmTex")) m.SetTexture("_EmTex", null);
            if (m.HasProperty("_EmPower")) m.SetFloat("_EmPower", 0f);
            if (m.HasProperty("_FresnelMask")) m.SetTexture("_FresnelMask", null);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
            if (def.AlphaMode != 0)
            {
                if (m.HasProperty("_EnableCutout")) m.SetFloat("_EnableCutout", 1f);
                m.EnableKeyword("CUTOUT");
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            }
            else
            {
                if (m.HasProperty("_EnableCutout")) m.SetFloat("_EnableCutout", 0f);
                m.DisableKeyword("CUTOUT");
            }
            return m;
        }
    }
}
