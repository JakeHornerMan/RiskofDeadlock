#if OWN_FILES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Configuration;
using DeadlockOfRain.Sheets;
using EntityStates;
using HarmonyLib;
using KinematicCharacterController;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace DeadlockOfRain.Enemies
{
    /// <summary>Builds the enemies of design/enemies.json (docs/11; the creator's Trooper spec, 2026-11-11): each a clone of a vanilla
    /// monster's body and master (cloneBody/cloneMaster) with our model, stats, attacks (enemySkills.json) and AI drivers, plus a
    /// spawn card the director uses (TrooperDirector). The creator's own DLL build only (OWN_FILES).</summary>
    public static class Enemies
    {
        public static ConfigEntry<bool> Enabled;
        public static readonly Dictionary<string, GameObject> Bodies = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, GameObject> Masters = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, CharacterSpawnCard> Cards = new Dictionary<string, CharacterSpawnCard>();
        public static readonly List<SkillDef> SkillDefs = new List<SkillDef>();
        public static readonly List<SkillFamily> Families = new List<SkillFamily>();
        public static readonly List<GameObject> Projectiles = new List<GameObject>();
        public static readonly List<EffectDef> EffectDefs = new List<EffectDef>();
        static readonly Dictionary<string, GameObject> shots = new Dictionary<string, GameObject>();
        public static GameObject MeleeHitEffect, MuzzleFlash, SpawnEffect, HealPack;
        static Transform holder;

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind("Enemies", "Troopers", true,
                "Deadlock's Troopers (Melee, Ranged, Medic) join the monsters on every stage. Off: they never spawn.");
            GlobalEventManager.onCharacterDeathGlobal += OnDeath;
        }

        /// <summary>The projectile of an enemy's shot row (null when it has none).</summary>
        public static GameObject ShotFor(string skillId) => shots.TryGetValue(skillId, out var p) ? p : null;

        public static bool IsMedic(CharacterBody b) => EnemyTag.Of(b) == "trooper_medic";

        static T Load<T>(string key) where T : UnityEngine.Object
        {
            try { return Addressables.LoadAssetAsync<T>(key).WaitForCompletion(); }
            catch (Exception e) { Log.Warn($"enemies: {key}: {e.Message}"); return null; }
        }

        public static IEnumerator Build(Transform parent)
        {
            if (!Enabled.Value) { Log.Info("Enemies: switched off in the config (Enemies / Troopers)"); yield break; }
            holder = new GameObject("DeadlockOfRainEnemies").transform;
            holder.SetParent(parent, false);
            MeleeHitEffect = Load<GameObject>("RoR2/Base/Common/VFX/OmniImpactVFXSlash.prefab");
            MuzzleFlash = Load<GameObject>("RoR2/Base/Lemurian/MuzzleflashFire.prefab");
            SpawnEffect = Load<GameObject>("RoR2/Base/Lemurian/SpawnLemurian.prefab");
            HealPack = Load<GameObject>("RoR2/Base/Tooth/HealPack.prefab");
            MedicFx.Build(holder);
            foreach (var row in EnemyRow.All)
            {
                if (!row.Models.Any(m => LocalHero.Enemies.Contains(m) && DeadlockCache.LoadHero(m) != null))
                {
                    Log.Warn($"Enemy {row.Name}: none of its models ({string.Join(", ", row.Models)}) are in the hero files: left out");
                    continue;
                }
                var bodyLoad = Addressables.LoadAssetAsync<GameObject>(row.CloneBody);
                var masterLoad = Addressables.LoadAssetAsync<GameObject>(row.CloneMaster);
                var cardLoad = Addressables.LoadAssetAsync<CharacterSpawnCard>(row.CostFrom);
                while (!bodyLoad.IsDone || !masterLoad.IsDone || !cardLoad.IsDone) yield return null;
                try { BuildOne(row, bodyLoad.Result, masterLoad.Result, cardLoad.Result); }
                catch (Exception e) { Log.Error($"Enemy {row.Name} failed, left out: {e}"); }
            }
            Log.Info($"Enemies built: {string.Join(", ", Bodies.Keys)}");
        }

        static void AssetId(GameObject go)
        {
            var ni = go.GetComponent<NetworkIdentity>();
            if (!ni) return;
            var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("DeadlockOfRain." + go.name));
            AccessTools.Field(typeof(NetworkIdentity), "m_AssetId").SetValue(ni, NetworkHash128.Parse(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()));
        }

        static string Code(EnemyRow row) => string.Concat(row.Id.Split('_').Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));

        static void BuildOne(EnemyRow row, GameObject vanillaBody, GameObject vanillaMaster, CharacterSpawnCard vanillaCard)
        {
            var skills = Sheet.EnemySkillsOf(row.Id).ToList();
            float reach = skills.Where(s => s.Slot != "passive").Select(s => s.Reach).DefaultIfEmpty(0f).Max();

            // body
            var body = UnityEngine.Object.Instantiate(vanillaBody, holder);
            body.name = "Deadlock" + Code(row) + "Body";
            AssetId(body);
            var ml = body.GetComponent<ModelLocator>();
            var oldModel = ml.modelTransform;
            var modelBase = ml.modelBaseTransform ? ml.modelBaseTransform : oldModel.parent;
            var model = EnemyModel.Build(row, reach);
            if (!model) throw new Exception("no model");
            model.transform.SetParent(modelBase, false);
            model.transform.localPosition = oldModel.localPosition;
            model.transform.localRotation = Quaternion.identity;
            UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
            AccessTools.Field(typeof(ModelLocator), "_modelTransform").SetValue(ml, model.transform);
            var cb = body.GetComponent<CharacterBody>();
            var hc = body.GetComponent<HealthComponent>();
            model.GetComponent<CharacterModel>().body = cb;
            foreach (var hb in model.GetComponentsInChildren<HurtBox>(true)) hb.healthComponent = hc;
            var cd = body.GetComponent<CharacterDirection>();
            if (cd) cd.modelAnimator = null;
            foreach (var aa in body.GetComponentsInChildren<AimAnimator>(true)) UnityEngine.Object.DestroyImmediate(aa);

            // name and stats (RoR2 levels health and damage itself)
            cb.baseNameToken = Sheet.Token("name", row.Id);
            cb.subtitleNameToken = Sheet.Token("subtitle", row.Id);
            Survivors.Tokens[cb.baseNameToken] = row.Name;
            Survivors.Tokens[cb.subtitleNameToken] = row.Subtitle;
            cb.baseMaxHealth = row.Health; cb.levelMaxHealth = row.HealthLvl;
            cb.baseDamage = row.Damage; cb.levelDamage = row.DamageLvl;
            cb.baseMoveSpeed = row.Speed; cb.levelMoveSpeed = 0f;
            // the Beetle walks by its Mecanim clip's root motion (GenericCharacterMain then ignores the move input); our clips play
            // without it, so the body moves from its input like a Lemurian (2026-11-11: the Melee Trooper never took a step)
            cb.rootMotionInMainState = false;
            cb.baseArmor = row.Armor; cb.levelArmor = 0f;

            // size: the vanilla capsule scaled to the trooper's height (Deadlock metres x the heroes' shared factor)
            var capsule = body.GetComponent<CapsuleCollider>();
            if (capsule)
            {
                float h = row.SourceHeight * ModelBuilder.GlobalScale, k = h / Mathf.Max(0.1f, capsule.height);
                float r = Mathf.Min(Mathf.Max(capsule.radius * k, row.Width * ModelBuilder.GlobalScale * 1.2f), h * 0.5f), y = capsule.center.y * k;
                capsule.height = h; capsule.radius = r; capsule.center = new Vector3(0, y, 0);
                var kcm = body.GetComponent<KinematicCharacterMotor>();
                if (kcm)
                {
                    AccessTools.Field(typeof(KinematicCharacterMotor), "CapsuleHeight").SetValue(kcm, h);
                    AccessTools.Field(typeof(KinematicCharacterMotor), "CapsuleRadius").SetValue(kcm, r);
                    AccessTools.Field(typeof(KinematicCharacterMotor), "CapsuleYOffset").SetValue(kcm, y);
                }
                if (cb.aimOriginTransform) cb.aimOriginTransform.localPosition = Vector3.Scale(cb.aimOriginTransform.localPosition, new Vector3(1, k, 1));
            }

            // states: spawning, dying, staggering
            var machines = body.GetComponents<EntityStateMachine>();
            var bodyMachine = machines.FirstOrDefault(m => m.customName == "Body");
            var weapon = machines.FirstOrDefault(m => m.customName == "Weapon") ?? bodyMachine;
            if (bodyMachine) bodyMachine.initialStateType = new SerializableEntityStateType(typeof(EnemySpawn));
            var death = body.GetComponent<CharacterDeathBehavior>();
            if (death) death.deathState = new SerializableEntityStateType(typeof(EnemyDeath));
            var hurt = body.GetComponent<SetStateOnHurt>();
            if (hurt) { hurt.hurtState = new SerializableEntityStateType(typeof(EnemyHurt)); hurt.hitThreshold = row.StaggerPct; }
            // no Beetle or Lemurian noises or log
            var sfx = body.GetComponent<SfxLocator>();
            if (sfx) { sfx.deathSound = ""; sfx.barkSound = ""; }
            var dr = body.GetComponent<DeathRewards>();
            if (dr) dr.logUnlockableDef = null;

            // attacks
            var sl = body.GetComponent<SkillLocator>();
            var familyField = AccessTools.Field(typeof(GenericSkill), "_skillFamily");
            foreach (var s in skills)
            {
                if (s.Slot == "passive") continue;
                var type = Survivors.StateFor(s.Kind);
                if (type == null) { Log.Error($"enemy skill {s.Id}: no state for {s.Kind}"); continue; }
                var def = ScriptableObject.CreateInstance<SkillDef>();
                ((ScriptableObject)def).name = "sdDeadlock_" + s.Id;
                def.skillName = "Deadlock_" + s.Id;
                def.activationState = new SerializableEntityStateType(type);
                def.activationStateMachineName = weapon ? weapon.customName : "Body";
                def.interruptPriority = InterruptPriority.Skill;
                def.baseRechargeInterval = s.Cooldown;
                def.baseMaxStock = 1; def.rechargeStock = 1; def.requiredStock = 1; def.stockToConsume = 1;
                def.fullRestockOnAssign = true; def.isCombatSkill = true; def.mustKeyPress = false;
                def.beginSkillCooldownOnSkillEnd = true;
                var fam = ScriptableObject.CreateInstance<SkillFamily>();
                ((ScriptableObject)fam).name = "sfDeadlock_" + s.Id;
                fam.variants = new[] { new SkillFamily.Variant { skillDef = def, viewableNode = new ViewablesCatalog.Node(def.skillName, false, null) } };
                SkillDefs.Add(def); Families.Add(fam);
                var gs = s.Slot == "primary" ? sl.primary : sl.secondary;
                if (gs) familyField.SetValue(gs, fam);
                if (s.ProjectileSpeed > 0f) shots[s.Id] = Shot(s);
            }
            var tag = body.AddComponent<EnemyTag>();
            tag.Id = row.Id;
            var pulse = skills.FirstOrDefault(s => s.Kind == "medic_pulse");
            if (pulse != null) body.AddComponent<MedicPulse>().RowId = pulse.Id;

            // master: our body and our AI drivers
            var master = UnityEngine.Object.Instantiate(vanillaMaster, holder);
            master.name = "Deadlock" + Code(row) + "Master";
            AssetId(master);
            master.GetComponent<CharacterMaster>().bodyPrefab = body;
            foreach (var d in master.GetComponents<AISkillDriver>()) UnityEngine.Object.DestroyImmediate(d);
            Drivers(master, row, skills);
            if (row.FleeRange > 0f) master.AddComponent<MedicFollow>();

            // the spawn card: the vanilla card's cost x costMult (Beetle 8 -> 10)
            var card = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            ((ScriptableObject)card).name = "cscDeadlock" + Code(row);
            card.prefab = master;
            card.sendOverNetwork = true;
            card.hullSize = HullClassification.Human;
            card.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            card.requiredFlags = RoR2.Navigation.NodeFlags.None;
            card.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn;
            int baseCost = vanillaCard ? vanillaCard.directorCreditCost : 10;
            card.directorCreditCost = Mathf.Max(1, Mathf.RoundToInt(baseCost * row.CostMult));
            card.occupyPosition = false;
            card.eliteRules = SpawnCard.EliteRules.Default;
            Cards[row.Id] = card;

            Bodies[row.Id] = body;
            Masters[row.Id] = master;
            Log.Info($"Built enemy {row.Name}: {row.Models.Length} looks, cost {card.directorCreditCost} (vanilla {baseCost}), health {row.Health} (+{row.HealthLvl})");
        }

        /// <summary>The AI: our own driver list, first match wins (BaseAI.EvaluateSkillDrivers). See docs/11 for the rules.</summary>
        static void Drivers(GameObject master, EnemyRow row, List<EnemySkillRow> skills)
        {
            var prim = skills.FirstOrDefault(s => s.Slot == "primary");
            var sec = skills.FirstOrDefault(s => s.Slot == "secondary");
            AISkillDriver D(string name, SkillSlot slot, float min, float max, AISkillDriver.MovementType mv, bool los = false,
                AISkillDriver.TargetType target = AISkillDriver.TargetType.CurrentEnemy)
            {
                var d = master.AddComponent<AISkillDriver>();
                d.customName = name; d.skillSlot = slot; d.requireSkillReady = slot != SkillSlot.None;
                d.minDistance = min; d.maxDistance = max; d.movementType = mv; d.moveTargetType = target;
                d.selectionRequiresTargetLoS = los; d.activationRequiresTargetLoS = los;
                d.aimType = AISkillDriver.AimType.AtCurrentEnemy;
                d.selectionRequiresAimTarget = false;
                d.ignoreNodeGraph = false; d.shouldSprint = false;
                d.buttonPressType = AISkillDriver.ButtonPressType.Hold;
                return d;
            }
            const float Far = float.PositiveInfinity;
            var chase = AISkillDriver.MovementType.ChaseMoveTarget;
            var stop = AISkillDriver.MovementType.Stop;
            var flee = AISkillDriver.MovementType.FleeMoveTarget;
            if (row.FleeRange > 0f)
            {
                // the Medic: back off from the player, keep behind its allies (MedicFollow), shoot only with nobody to heal
                D("Flee", SkillSlot.None, 0f, row.FleeRange, flee);
                D(MedicFollow.FollowDriver, SkillSlot.None, 3f, Far, chase, false, AISkillDriver.TargetType.Custom);
                D(MedicFollow.StayDriver, SkillSlot.None, 0f, 3f, stop, false, AISkillDriver.TargetType.Custom);
            }
            if (prim != null)
            {
                D("Hit", SkillSlot.Primary, 0f, prim.Reach, chase);
                if (prim.MaxRange > prim.Reach) D("CloseChase", SkillSlot.None, 0f, prim.MaxRange, chase);
                else
                {
                    // waits beside the target for the whole reach; close up it walks straight at it, off the node graph, as the
                    // vanilla melee monsters do (2026-11-11: on the graph it overshot a player standing still and wandered 4-7 m off)
                    D("Close", SkillSlot.None, 0f, prim.Reach, stop);
                    D("ChaseDirect", SkillSlot.None, 0f, 8f, chase).ignoreNodeGraph = true;
                }
            }
            if (sec != null)
            {
                float near = Mathf.Max(prim != null ? prim.MaxRange : 0f, row.FleeRange);
                D("Shoot", SkillSlot.Secondary, near, sec.MaxRange, stop, true);
                if (row.KeepMin > near) D("BackOff", SkillSlot.None, near, row.KeepMin, flee);
                D("Hold", SkillSlot.None, row.KeepMin, row.KeepMax, stop, true);
            }
            D("Chase", SkillSlot.None, 0f, Far, chase);
        }

        /// <summary>The shot's projectile: the Lemurian's fireball, flying at the row's speed, its look recoloured to the row's colour.</summary>
        static GameObject Shot(EnemySkillRow s)
        {
            var src = Load<GameObject>("RoR2/Base/Lemurian/Fireball.prefab");
            var ghostSrc = Load<GameObject>("RoR2/Base/Lemurian/FireballGhost.prefab");
            if (!src) return null;
            var p = UnityEngine.Object.Instantiate(src, holder);
            p.name = "Deadlock" + s.Id + "Projectile";
            AssetId(p);
            var simple = p.GetComponent<ProjectileSimple>();
            if (simple) simple.desiredForwardSpeed = s.ProjectileSpeed;
            var pc = p.GetComponent<ProjectileController>();
            if (pc && ghostSrc)
            {
                var ghost = UnityEngine.Object.Instantiate(ghostSrc, holder);
                ghost.name = "Deadlock" + s.Id + "Ghost";
                var c = Sheet.Color(s.Color);
                foreach (var ps in ghost.GetComponentsInChildren<ParticleSystem>(true)) { var m = ps.main; m.startColor = c; }
                foreach (var r in ghost.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials.Select(m => { if (!m) return m; var n = new Material(m); if (n.HasProperty("_TintColor")) n.SetColor("_TintColor", c); if (n.HasProperty("_Color")) n.SetColor("_Color", c); return n; }).ToArray();
                    r.sharedMaterials = mats;
                }
                foreach (var l in ghost.GetComponentsInChildren<Light>(true)) l.color = c;
#pragma warning disable CS0618
                pc.ghostPrefab = ghost;
#pragma warning restore CS0618
                pc.ghostPrefabAddress = new UnityEngine.AddressableAssets.AssetReferenceT<GameObject>("");
            }
            Projectiles.Add(p);
            return p;
        }

        /// <summary>The Medic's kill reward (creator's spec): one healing orb, the one Monster Tooth drops, for the players.</summary>
        static void OnDeath(DamageReport report)
        {
            if (!NetworkServer.active || report == null || !IsMedic(report.victimBody) || !HealPack) return;
            var orb = UnityEngine.Object.Instantiate(HealPack, report.victimBody.corePosition, UnityEngine.Random.rotation);
            var tf = orb.GetComponent<TeamFilter>();
            if (tf) tf.teamIndex = TeamIndex.Player;
            var hp = orb.GetComponentInChildren<HealthPickup>();
            if (hp) { hp.flatHealing = 8f; hp.fractionalHealing = 0.02f; }
            var timer = orb.GetComponent<DestroyOnTimer>();
            if (timer) timer.duration = 10f;
            NetworkServer.Spawn(orb);
            MeltyTest.Report("medic-orb", true, "trooper_medic");
        }
    }
}
#endif
