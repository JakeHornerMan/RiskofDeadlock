using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using DeadlockOfRain.States;
using EntityStates;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>The heroes' Deadlock ultimates as RoR2 equipment (Q, design/equipment.json). Each hero starts every run holding
    /// their own; like any equipment they can be swapped away, and anyone can find them wherever the Preon Accumulator drops.
    /// Firing one starts the ult's state (States/Ultimates.cs) on whichever body holds it, with the skill row it names.</summary>
    public static class Equipment
    {
        public static readonly Dictionary<string, EquipmentDef> Defs = new Dictionary<string, EquipmentDef>();
        static readonly Dictionary<EquipmentDef, SkillRow> rowOf = new Dictionary<EquipmentDef, SkillRow>();
        static readonly HashSet<CharacterMaster> given = new HashSet<CharacterMaster>();

        const float FigurePadding = 2.2f;

        public static SkillRow RowOf(EquipmentDef def) => def && rowOf.TryGetValue(def, out var r) ? r : null;

        /// <summary>The equipment a hero starts with (null for any other body).</summary>
        public static EquipmentDef OfHero(string hero)
        {
            if (hero == null) return null;
            foreach (var e in EquipmentRow.All) if (e.Hero == hero && Defs.TryGetValue(e.Id, out var d)) return d;
            return null;
        }

        public static void Init()
        {
            CharacterBody.onBodyStartGlobal += GiveStartingEquipment;
            Run.onRunStartGlobal += _ => given.Clear();
        }

        public static void Build(Transform holder)
        {
            foreach (var e in EquipmentRow.All)
            {
                var skill = Sheet.Skill(e.Skill);
                var hero = Sheet.Hero(e.Hero);
                if (skill == null || hero == null || !Survivors.Bodies.ContainsKey(hero.Id)) continue;
                var def = ScriptableObject.CreateInstance<EquipmentDef>();
                ((ScriptableObject)def).name = "edDeadlock_" + skill.Id;
                def.nameToken = Sheet.Token("equip", e.Id);
                def.pickupToken = Sheet.Token("equippickup", e.Id);
                def.descriptionToken = Sheet.Token("equipdesc", e.Id);
                def.loreToken = Sheet.Token("equiplore", e.Id);
                def.cooldown = skill.Cooldown;
                def.canDrop = e.CanDrop;
                def.enigmaCompatible = e.EnigmaCompatible;
                def.canBeRandomlyTriggered = e.RandomTrigger;
                def.appearsInSinglePlayer = true;
                def.appearsInMultiPlayer = true;
                def.colorIndex = ColorCatalog.ColorIndex.Equipment;
#pragma warning disable CS0618 // the direct-reference fields are still read when no Addressables reference is set
                def.pickupIconSprite = DeadlockCache.Sprite(skill.Icon);
                def.pickupModelPrefab = PickupModel(hero, holder);
#pragma warning restore CS0618
                Defs[e.Id] = def;
                rowOf[def] = skill;
                Survivors.Tokens[def.nameToken] = hero.DisplayName + "'s " + skill.Name;
                Survivors.Tokens[def.pickupToken] = e.PickupText;
                Survivors.Tokens[def.descriptionToken] = skill.Description;
                Survivors.Tokens[def.loreToken] = e.Lore;
                Log.Info($"Built equipment {skill.Name} ({skill.Cooldown:F0}s)");
            }
        }

        /// <summary>The pickup: a small statue of the hero in their idle pose (the model baked into one static mesh, centred, so
        /// RoR2's pickup display can size and spin it like any item model).</summary>
        static GameObject PickupModel(HeroRow hero, Transform holder)
        {
            var model = ModelBuilder.Build(hero, true);
            if (!model) return null;
            try
            {
                var anim = model.GetComponent<Animation>();
                var idle = Sheet.Anims(hero.Id).Idle;
                if (anim && idle != "none" && anim[idle] != null)
                {
                    var st = anim[idle];
                    st.enabled = true; st.weight = 1f; st.time = 0f;
                    anim.Sample();
                    st.enabled = false;
                }
                var smrs = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var baked = new List<Mesh>();
                var mats = new List<Material>();
                var bounds = new Bounds();
                bool first = true;
                foreach (var smr in smrs)
                {
                    if (!smr.sharedMesh) continue;
                    var m = new Mesh();
                    smr.BakeMesh(m);
                    m.RecalculateBounds();
                    // RoR2's character shader dithers by _Fade, which CharacterModel normally drives; the figure has none, so
                    // its own copies are fully opaque
                    var mat = new Material(smr.sharedMaterial) { name = smr.sharedMaterial.name + "_Figure" };
                    if (mat.HasProperty("_Fade")) mat.SetFloat("_Fade", 1f);
                    mat.DisableKeyword("DITHER");
                    baked.Add(m); mats.Add(mat);
                    if (first) { bounds = m.bounds; first = false; } else bounds.Encapsulate(m.bounds);
                }
                if (baked.Count == 0) return null;
                var combine = new CombineInstance[baked.Count];
                var centre = Matrix4x4.Translate(-bounds.center);
                for (int i = 0; i < baked.Count; i++) combine[i] = new CombineInstance { mesh = baked[i], transform = centre };
                var mesh = new Mesh { name = "mshDeadlock" + hero.DisplayName + "Figure", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(combine, false, true);
                mesh.RecalculateBounds();
                // RoR2's pickup display scales every model to the same volume by its renderer's bounds, which makes a slim
                // hero life-size; padded bounds make it a small figure about 0.7 m tall, like other pickups
                mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size * FigurePadding);
                foreach (var m in baked) Object.Destroy(m);

                var go = new GameObject("PickupDeadlock" + hero.DisplayName);
                go.transform.SetParent(holder, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = mats.ToArray();
                return go;
            }
            finally { Object.DestroyImmediate(model); }
        }

        static void GiveStartingEquipment(CharacterBody body)
        {
            if (!NetworkServer.active || !body || !Run.instance) return;
            var def = OfHero(Survivors.HeroOf(body));
            var master = body.master;
            if (!def || !master || !master.inventory || !master.playerCharacterMasterController) return;
            // once per player per run, at the start: a hero who swapped it away does not get it back
            if (given.Contains(master) || Run.instance.stageClearCount > 0) return;
            given.Add(master);
            if (master.inventory.currentEquipmentIndex != EquipmentIndex.None) return;
            var loc = master.inventory.activeEquipmentLocation;
            master.inventory.SetEquipmentIndex(def.equipmentIndex, in loc);
            MeltyTest.Report("starting-equipment", true, body.baseNameToken);
        }

        /// <summary>Start the ult on the body that fired it. It runs where the body has authority (states are networked from
        /// there, the row id with them); returns false when the body has no state machine to run it on.</summary>
        public static bool Fire(CharacterBody body, SkillRow row)
        {
            if (!body || row == null) return false;
            var type = Survivors.StateFor(row.Kind);
            if (type == null) return false;
            var machine = EntityStateMachine.FindByCustomName(body.gameObject, Survivors.MachineFor(row.Kind));
            if (!machine) machine = EntityStateMachine.FindByCustomName(body.gameObject, "Weapon");
            if (!machine) machine = body.GetComponent<EntityStateMachine>();
            if (!machine) return false;
            var state = (DlrState)System.Activator.CreateInstance(type);
            state.RowOverride = row;
            // false while another ult (or a stun) holds the machine: no charge is spent
            return machine.SetInterruptState(state, InterruptPriority.PrioritySkill);
        }

        /// <summary>Our equipment: RoR2 spends the charge (and starts the cooldown) when this answers true. The ult starts here
        /// when the server owns the body (the host's own hero, drones and other AI, Bottled Chaos).</summary>
        [HarmonyPatch(typeof(EquipmentSlot), "PerformEquipmentAction")]
        static class FirePatch
        {
            static bool Prefix(EquipmentSlot __instance, EquipmentDef equipmentDef, ref bool __result)
            {
                var row = RowOf(equipmentDef);
                if (row == null) return true;
                var body = __instance.characterBody;
                __result = body && (!body.hasEffectiveAuthority || Fire(body, row));
                if (__result) MeltyTest.Report("equipment-fires", true, body.baseNameToken);
                return false;
            }
        }

        /// <summary>On a remote client that owns the body, the activation RPC starts the ult there.</summary>
        [HarmonyPatch(typeof(EquipmentSlot), "RpcOnClientEquipmentActivationRecieved")]
        static class ClientFirePatch
        {
            static void Postfix(EquipmentSlot __instance)
            {
                if (NetworkServer.active) return;
                var body = __instance.characterBody;
                if (!body || !body.hasEffectiveAuthority) return;
                var row = RowOf(EquipmentCatalog.GetEquipmentDef(__instance.equipmentIndex));
                if (row != null) Fire(body, row);
            }
        }
    }
}
