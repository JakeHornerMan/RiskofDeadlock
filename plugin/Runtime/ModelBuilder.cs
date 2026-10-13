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

        public static GameObject Build(HeroRow hero, bool forDisplay)
        {
            var asset = DeadlockCache.LoadHero(hero.Id);
            if (asset == null) return null;
            var root = new GameObject("mdlDeadlock" + hero.DisplayName);
            root.transform.localScale = Vector3.one * GlobalScale;

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

            // one skinned renderer per material: RoR2's CharacterModel drives exactly one material per renderer
            var parts = asset.Parts;
            var infos = new List<CharacterModel.RendererInfo>();
            for (int s = 0; s < parts.Length; s++)
            {
                var def = asset.Materials[Mathf.Clamp(asset.Submeshes[s].Key, 0, asset.Materials.Length - 1)];
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
            Add("MuzzleRight", "weapon_hand_R"); Add("MuzzleRight", "hand_R");
            Add("MuzzleLeft", "weapon_hand_L"); Add("MuzzleLeft", "hand_L");
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
            }
            return root;
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
