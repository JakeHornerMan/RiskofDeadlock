using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DeadlockOfRain.Sheets;
using EntityStates;
using HarmonyLib;
using KinematicCharacterController;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Marks a body as one of our heroes.</summary>
    public sealed class DeadlockHero : MonoBehaviour
    {
        public string HeroId;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class KindAttribute : Attribute
    {
        public readonly string Kind;
        public KindAttribute(string kind) { Kind = kind; }
    }

    public static class Survivors
    {
        public static readonly Dictionary<string, GameObject> Bodies = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, SurvivorDef> Defs = new Dictionary<string, SurvivorDef>();
        public static readonly Dictionary<string, SkillDef> SkillDefs = new Dictionary<string, SkillDef>();
        public static readonly Dictionary<SkillDef, SkillRow> RowOfSkill = new Dictionary<SkillDef, SkillRow>();
        public static readonly List<SkillFamily> Families = new List<SkillFamily>();
        public static readonly List<Type> StateTypes = new List<Type>();
        public static readonly Dictionary<string, string> Tokens = new Dictionary<string, string>();
        public static float CommandoHeight;
        static GameObject holder;

        public static bool IsHero(CharacterBody b, string id) { var h = b ? b.GetComponent<DeadlockHero>() : null; return h && h.HeroId == id; }
        public static string HeroOf(CharacterBody b) { var h = b ? b.GetComponent<DeadlockHero>() : null; return h ? h.HeroId : null; }

        static Dictionary<string, Type> KindTypes()
        {
            var d = new Dictionary<string, Type>();
            foreach (var t in typeof(Survivors).Assembly.GetTypes())
            {
                var k = (KindAttribute)Attribute.GetCustomAttribute(t, typeof(KindAttribute));
                if (k != null) d[k.Kind] = t;
                if (typeof(EntityState).IsAssignableFrom(t) && !t.IsAbstract) StateTypes.Add(t);
            }
            return d;
        }

        static string MachineFor(string kind) =>
            kind == "dash_charge" || kind == "dash_trail" || kind == "ult_leap" || kind == "ult_storm" ? "Body" : "Weapon";

        public static IEnumerator Build()
        {
            holder = new GameObject("DeadlockOfRainPrefabs");
            holder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(holder);

            var commando = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/CommandoBody.prefab").WaitForCompletion();
            var cmdModel = commando.GetComponent<ModelLocator>().modelTransform;
            var cmdSmr = cmdModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
            // RoR2's own deferred shader (Unity's Standard shader is stripped from the game): take Commando's material
            ModelBuilder.Template = Addressables.LoadAssetAsync<Material>("RoR2/Base/Commando/matCommandoDualies.mat").WaitForCompletion();
            Log.Info("hero material template: " + (ModelBuilder.Template ? ModelBuilder.Template.shader.name : "none"));
            var cap = commando.GetComponent<CapsuleCollider>();
            float meshH = 0;
            if (cmdSmr && cmdSmr.sharedMesh) meshH = cmdSmr.sharedMesh.bounds.size.y * cmdSmr.transform.lossyScale.y;
            CommandoHeight = meshH > 1f && meshH < 3f ? meshH : cap.height;
            var anchor = Sheet.Hero(SheetInfo.AnchorHero);
            ModelBuilder.GlobalScale = CommandoHeight / anchor.SourceHeight;
            Log.Info($"Commando height {CommandoHeight:F3} m (mesh {meshH:F3}, capsule {cap.height:F3}); Deadlock->RoR2 scale {ModelBuilder.GlobalScale:F4}");

            var kinds = KindTypes();
            Buffs.Create();
            Fx.Preload();
            yield return null;

            foreach (var hero in HeroRow.All)
            {
                if (DeadlockCache.LoadHero(hero.Id) == null) { Log.Error("hero " + hero.Id + " skipped: no Deadlock content"); continue; }
                yield return null;
                var body = BuildBody(commando, hero, kinds);
                Bodies[hero.Id] = body;
                var display = ModelBuilder.Build(hero, true);
                display.transform.SetParent(holder.transform, false);
                display.name = "Deadlock" + hero.DisplayName + "Display";

                var sd = ScriptableObject.CreateInstance<SurvivorDef>();
                ((ScriptableObject)sd).name = "Deadlock" + hero.DisplayName;
                sd.cachedName = "Deadlock" + hero.DisplayName;
                sd.bodyPrefab = body;
                sd.displayPrefab = display;
                sd.displayNameToken = Sheet.Token("name", hero.Id);
                sd.descriptionToken = Sheet.Token("desc", hero.Id);
                sd.outroFlavorToken = Sheet.Token("outro", hero.Id);
                sd.mainEndingEscapeFailureFlavorToken = Sheet.Token("fail", hero.Id);
                sd.primaryColor = Sheet.Color(hero.PrimaryColor);
                sd.desiredSortPosition = hero.SortPosition;
                Defs[hero.Id] = sd;
                AddTokens(hero);
                Log.Info($"Built survivor {hero.DisplayName}");
            }
        }

        static GameObject BuildBody(GameObject commando, HeroRow hero, Dictionary<string, Type> kinds)
        {
            var body = UnityEngine.Object.Instantiate(commando, holder.transform);
            body.name = "Deadlock" + hero.DisplayName + "Body";
            var ni = body.GetComponent<NetworkIdentity>();
            if (ni)
            {
                var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("DeadlockOfRain." + body.name));
                var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                AccessTools.Field(typeof(NetworkIdentity), "m_AssetId").SetValue(ni, NetworkHash128.Parse(hex));
            }

            // swap Commando's model for the Deadlock hero
            var ml = body.GetComponent<ModelLocator>();
            var oldModel = ml.modelTransform;
            var modelBase = ml.modelBaseTransform ? ml.modelBaseTransform : oldModel.parent;
            var model = ModelBuilder.Build(hero, false);
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
            if (cd) { cd.modelAnimator = null; }
            foreach (var aa in body.GetComponentsInChildren<AimAnimator>(true)) UnityEngine.Object.DestroyImmediate(aa);

            // identity, stats (base + per-level growth; RoR2 applies the levels itself)
            var st = Sheet.Stats(hero.Id);
            cb.baseNameToken = Sheet.Token("name", hero.Id);
            cb.subtitleNameToken = Sheet.Token("subtitle", hero.Id);
            cb.portraitIcon = DeadlockCache.Image(hero.ImageSmall);
            cb.bodyColor = Sheet.Color(hero.PrimaryColor);
            cb.baseMaxHealth = st.BaseMaxHealth; cb.levelMaxHealth = st.LevelMaxHealth;
            cb.baseRegen = st.BaseRegen; cb.levelRegen = st.LevelRegen;
            cb.baseDamage = st.BaseDamage; cb.levelDamage = st.LevelDamage;
            cb.baseArmor = st.BaseArmor; cb.levelArmor = st.LevelArmor;
            cb.baseAttackSpeed = st.BaseAttackSpeed; cb.levelAttackSpeed = st.LevelAttackSpeed;
            cb.baseCrit = st.BaseCrit; cb.levelCrit = st.LevelCrit;
            cb.baseMoveSpeed = st.BaseMoveSpeed; cb.levelMoveSpeed = 0;
            cb.baseJumpPower = st.BaseJumpPower; cb.levelJumpPower = 0;
            cb.baseJumpCount = (int)st.BaseJumpCount;
            cb.baseAcceleration = st.BaseAcceleration;
            cb.baseMaxShield = st.BaseMaxShield; cb.levelMaxShield = 0;
            cb.sprintingSpeedMultiplier = st.SprintingSpeedMultiplier;

            // size: capsule and camera follow the hero's Deadlock height relative to Haze (= Commando)
            var capsule = body.GetComponent<CapsuleCollider>();
            float h = capsule.height * hero.HeightScale, r = capsule.radius * hero.WidthScale, y = capsule.center.y * hero.HeightScale;
            capsule.height = h; capsule.radius = Mathf.Min(r, h * 0.5f); capsule.center = new Vector3(0, y, 0);
            var kcm = body.GetComponent<KinematicCharacterMotor>();
            if (kcm)
            {
                AccessTools.Field(typeof(KinematicCharacterMotor), "CapsuleHeight").SetValue(kcm, h);
                AccessTools.Field(typeof(KinematicCharacterMotor), "CapsuleRadius").SetValue(kcm, Mathf.Min(r, h * 0.5f));
                AccessTools.Field(typeof(KinematicCharacterMotor), "CapsuleYOffset").SetValue(kcm, y);
            }
            if (cb.aimOriginTransform) cb.aimOriginTransform.localPosition = Vector3.Scale(cb.aimOriginTransform.localPosition, new Vector3(1, hero.HeightScale, 1));
            var ctp = body.GetComponent<CameraTargetParams>();
            if (ctp && ctp.cameraParams)
            {
                var cp = UnityEngine.Object.Instantiate(ctp.cameraParams);
                cp.name = "ccpDeadlock" + hero.DisplayName;
                var d = cp.data;
                d.idealLocalCameraPos.value = d.idealLocalCameraPos.value * Mathf.Lerp(1f, hero.HeightScale, 0.8f);
                d.pivotVerticalOffset.value = d.pivotVerticalOffset.value * hero.HeightScale;
                cp.data = d;
                ctp.cameraParams = cp;
            }

            // skills: Commando's four slots get the hero's families, plus a 5th slot for the Deadlock ultimate
            var sl = body.GetComponent<SkillLocator>();
            var familyField = AccessTools.Field(typeof(GenericSkill), "_skillFamily");
            foreach (var row in Sheet.SkillsOf(hero.Id))
            {
                var def = MakeSkillDef(row, kinds);
                var fam = ScriptableObject.CreateInstance<SkillFamily>();
                ((ScriptableObject)fam).name = "sfDeadlock_" + row.Id;
                fam.variants = new[] { new SkillFamily.Variant { skillDef = def, viewableNode = new ViewablesCatalog.Node(def.skillNameToken, false, null) } };
                Families.Add(fam);
                GenericSkill gs;
                switch (row.Slot)
                {
                    case "primary": gs = sl.primary; break;
                    case "secondary": gs = sl.secondary; break;
                    case "utility": gs = sl.utility; break;
                    case "special": gs = sl.special; break;
                    default:
                        gs = body.AddComponent<GenericSkill>();
                        gs.skillName = "DLR_Ult";
                        gs.hideInCharacterSelect = false;
                        break;
                }
                familyField.SetValue(gs, fam);
            }
            if (sl.passiveSkill.enabled) sl.passiveSkill.enabled = false;

            var tag = body.AddComponent<DeadlockHero>();
            tag.HeroId = hero.Id;
            body.AddComponent<UltController>();
            body.AddComponent<HeroRuntime>();
            return body;
        }

        static SkillDef MakeSkillDef(SkillRow row, Dictionary<string, Type> kinds)
        {
            var def = ScriptableObject.CreateInstance<SkillDef>();
            ((ScriptableObject)def).name = "sdDeadlock_" + row.Id;
            def.skillName = "Deadlock_" + row.Id;
            def.skillNameToken = Sheet.Token("skill", row.Id);
            def.skillDescriptionToken = Sheet.Token("skilldesc", row.Id);
            def.icon = DeadlockCache.Sprite(row.Icon);
            if (!kinds.TryGetValue(row.Kind, out var type)) { Log.Error("no state for kind " + row.Kind); type = typeof(Idle); }
            def.activationState = new SerializableEntityStateType(type);
            def.activationStateMachineName = MachineFor(row.Kind);
            bool primary = row.Slot == "primary", ult = row.Slot == "ult";
            def.interruptPriority = primary ? InterruptPriority.Any : ult ? InterruptPriority.PrioritySkill : InterruptPriority.Skill;
            def.baseRechargeInterval = primary ? 0f : ult ? 1f : row.Cooldown;
            def.baseMaxStock = (int)Mathf.Max(1, row.Stocks);
            def.rechargeStock = ult ? 0 : 1;
            def.requiredStock = 1;
            def.stockToConsume = primary ? 0 : 1;
            def.fullRestockOnAssign = !ult;
            def.isCombatSkill = row.Kind != "self_heal";
            def.mustKeyPress = !primary;
            bool movement = row.Kind.StartsWith("dash") || row.Kind == "self_buff" && row.Buff == "smoke";
            def.cancelSprintingOnActivation = !movement;
            def.canceledFromSprinting = false;
            def.hideCooldown = ult;
            def.hideStockCount = ult || primary;
            def.beginSkillCooldownOnSkillEnd = false;
            SkillDefs[row.Id] = def;
            RowOfSkill[def] = row;
            Tokens[def.skillNameToken] = row.Name;
            Tokens[def.skillDescriptionToken] = row.Description;
            return def;
        }

        static void AddTokens(HeroRow hero)
        {
            Tokens[Sheet.Token("name", hero.Id)] = hero.DisplayName;
            Tokens[Sheet.Token("subtitle", hero.Id)] = hero.Subtitle;
            Tokens[Sheet.Token("outro", hero.Id)] = "..and so " + hero.DisplayName + " left, the contract fulfilled.";
            Tokens[Sheet.Token("fail", hero.Id)] = "..and so " + hero.DisplayName + " vanished into the storm.";
            var sb = new StringBuilder();
            sb.Append(hero.Lore).Append("\n\n");
            foreach (var s in Sheet.SkillsOf(hero.Id))
            {
                string key = s.Slot == "primary" ? "M1" : s.Slot == "secondary" ? "M2" : s.Slot == "utility" ? "Shift" : s.Slot == "special" ? "R" : "F (Ultimate)";
                sb.Append("< ! > <style=cIsUtility>").Append(key).Append(" - ").Append(s.Name).Append("</style>\n");
            }
            sb.Append("\n<style=cIsUtility>Your Deadlock ultimate starts locked: deal damage and get kills to charge it, then press F.</style>");
            Tokens[Sheet.Token("desc", hero.Id)] = sb.ToString();
            foreach (var b in BuffRow.All) Tokens[Sheet.Token("buff", b.Id)] = b.Name;
        }

        public static SkillRow RowFor(GenericSkill gs) => gs && gs.skillDef && RowOfSkill.TryGetValue(gs.skillDef, out var r) ? r : null;
    }

    /// <summary>Per-body runtime state for the hero kits: magazine and reload, queued heals, smoke timer.</summary>
    public sealed class HeroRuntime : MonoBehaviour
    {
        public int Ammo = -1;
        public bool Reloading;
        public bool AltMuzzle;
        public float SmokeUntil;
        float reloadEnd;
        int clip;
        float healLeft, healRate;
        CharacterBody body;

        void Awake() => body = GetComponent<CharacterBody>();

        public void BeginReload(SkillRow gun, float attackSpeed)
        {
            if (Reloading) return;
            Reloading = true;
            clip = (int)gun.ClipSize;
            reloadEnd = Time.time + gun.ReloadTime / Mathf.Max(0.1f, attackSpeed);
            Fx.Sound(gun.Id, "reload", transform.position);
        }

        public void QueueHeal(float amount, float duration)
        {
            if (amount <= 0) return;
            healLeft += amount;
            healRate = healLeft / Mathf.Max(0.25f, duration);
        }

        void Update()
        {
            if (Reloading && Time.time >= reloadEnd)
            {
                Reloading = false;
                Ammo = clip;
                MeltyTest.Report("gun-reloads", true, body ? body.baseNameToken : "");
            }
        }

        void FixedUpdate()
        {
            if (healLeft > 0 && body && body.healthComponent && UnityEngine.Networking.NetworkServer.active)
            {
                float h = Mathf.Min(healLeft, healRate * Time.fixedDeltaTime);
                healLeft -= h;
                body.healthComponent.Heal(h, default);
            }
        }
    }

    sealed class DeadlockContentProvider : IContentPackProvider
    {
        readonly ContentPack pack = new ContentPack();
        public string identifier => Plugin.Guid;

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            if (DeadlockCache.Ready)
            {
                var e = Survivors.Build();
                while (e.MoveNext()) { args.ReportProgress(0.5f); yield return e.Current; }
            }
            pack.bodyPrefabs.Add(Survivors.Bodies.Values.ToArray());
            pack.survivorDefs.Add(Survivors.Defs.Values.ToArray());
            pack.skillDefs.Add(Survivors.SkillDefs.Values.ToArray());
            pack.skillFamilies.Add(Survivors.Families.ToArray());
            pack.entityStateTypes.Add(Survivors.StateTypes.ToArray());
            pack.buffDefs.Add(Buffs.Defs.Values.ToArray());
            args.ReportProgress(1f);
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(pack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }

    [HarmonyPatch(typeof(Language), nameof(Language.GetLocalizedStringByToken))]
    static class LanguagePatch
    {
        static bool Prefix(string token, ref string __result)
        {
            if (token != null && token.StartsWith("DLR_", StringComparison.Ordinal) && Survivors.Tokens.TryGetValue(token, out var s))
            {
                __result = s;
                return false;
            }
            return true;
        }
    }
}
