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

        /// <summary>The display name in letters and digits only, for object and catalog names ("Mo & Krill" -> MoKrill; the
        /// bench, the body catalog and console commands want one word).</summary>
        public static string CodeName(HeroRow hero) => System.Text.RegularExpressions.Regex.Replace(hero.DisplayName, "[^A-Za-z0-9]", "");

        static HeroRow[] released;
        /// <summary>The heroes this build ships: the ids in heroes.txt beside the DLL (a Thunderstore release, written by
        /// tools/package.py from thunderstore/release.json), or every hero row when there is no such file (Melty, dev).</summary>
        public static HeroRow[] Released
        {
            get
            {
                if (released != null) return released;
                var file = System.IO.Path.Combine(Plugin.PluginDir ?? "", "heroes.txt");
                if (!System.IO.File.Exists(file)) return released = HeroRow.All;
                var ids = new HashSet<string>(System.IO.File.ReadAllLines(file).Select(l => l.Trim()).Where(l => l.Length > 0));
                foreach (var h in HeroRow.All) if (!ids.Contains(h.Id)) Log.Info($"{h.DisplayName}: not in this release (heroes.txt)");
                return released = HeroRow.All.Where(h => ids.Contains(h.Id)).ToArray();
            }
        }

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

        /// <summary>Movement skills run on the Body machine (they steer the character), everything else on Weapon.</summary>
        public static string MachineFor(string kind) =>
            kind == "dash_charge" || kind == "dash_trail" || kind == "ult_leap" || kind == "phantom_strike" || kind == "heavy_melee" || kind == "burrow" || kind == "ult_devil" || kind == "goo_cube" ? "Body" : "Weapon";

        /// <summary>The hero never takes fall damage (heroes.json noFallDamage: Viscous). Moves that switch fall damage off for a while
        /// (Seismic Impact, Goo Ball) leave it off for such a hero when they end.</summary>
        public static bool NoFallDamage(CharacterBody b) => Sheet.Hero(HeroOf(b))?.NoFallDamage ?? false;

        public static Type StateFor(string kind) => kindTypes != null && kindTypes.TryGetValue(kind, out var t) ? t : null;
        static Dictionary<string, Type> kindTypes;

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

            var kinds = kindTypes = KindTypes();
            Buffs.Create();
            Fx.Preload();
            yield return null;

            foreach (var hero in Released)
            {
                if (DeadlockCache.Failed.ContainsKey(hero.Id) || DeadlockCache.LoadHero(hero.Id) == null)
                {
                    var why = DeadlockCache.Failed.TryGetValue(hero.Id, out var err) ? err : "no Deadlock content for it";
                    Log.Error("hero " + hero.Id + " skipped: Deadlock could not be read for it (" + why + ")");
                    MissingHeroes.Add(commando, holder.transform, hero, why);
                    continue;
                }
                yield return null;
                var body = BuildBody(commando, hero, kinds);
                Bodies[hero.Id] = body;
                var display = ModelBuilder.Build(hero, true);
                display.transform.SetParent(holder.transform, false);
                display.name = "Deadlock" + CodeName(hero) + "Display";

                var sd = ScriptableObject.CreateInstance<SurvivorDef>();
                ((ScriptableObject)sd).name = "Deadlock" + CodeName(hero);
                sd.cachedName = "Deadlock" + CodeName(hero);
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
            // the ults, as Q equipment anyone can find; each hero starts holding their own
            Equipment.Build(holder.transform);
            // the co-op relay effect (Runtime/Net.cs)
            Net.Build(holder.transform);
        }

        static GameObject BuildBody(GameObject commando, HeroRow hero, Dictionary<string, Type> kinds)
        {
            var body = UnityEngine.Object.Instantiate(commando, holder.transform);
            body.name = "Deadlock" + CodeName(hero) + "Body";
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
            if (hero.NoFallDamage) cb.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;

            // size: capsule and camera follow the hero's Deadlock height relative to Haze (= Commando)
            var capsule = body.GetComponent<CapsuleCollider>();
            float h = capsule.height * hero.HeightScale, r = capsule.radius * hero.WidthScale, y = capsule.center.y * hero.HeightScale;
            // capsuleFrom: a huge hero (Mo & Krill) moves with a vanilla body's capsule (MUL-T's), so it fits wherever that one does
            var capFrom = VanillaBody(hero.CapsuleFrom);
            var capSrc = capFrom ? capFrom.GetComponent<CapsuleCollider>() : null;
            if (capSrc) { h = capSrc.height; r = capSrc.radius; y = capSrc.center.y; }
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
            // cameraFrom: a vanilla body's camera (MUL-T's, further back), pushed back by cameraScale and right by cameraRight
            var camFrom = VanillaBody(hero.CameraFrom);
            var camSrc = camFrom ? camFrom.GetComponent<CameraTargetParams>() : null;
            if (ctp && (camSrc && camSrc.cameraParams || ctp.cameraParams))
            {
                var cp = UnityEngine.Object.Instantiate(camSrc && camSrc.cameraParams ? camSrc.cameraParams : ctp.cameraParams);
                cp.name = "ccpDeadlock" + hero.DisplayName;
                var d = cp.data;
                if (!camSrc)
                {
                    d.idealLocalCameraPos.value = d.idealLocalCameraPos.value * Mathf.Lerp(1f, hero.HeightScale, 0.8f);
                    d.pivotVerticalOffset.value = d.pivotVerticalOffset.value * hero.HeightScale;
                }
                var pos = d.idealLocalCameraPos.value * hero.CameraScale;
                pos.x += hero.CameraRight;
                d.idealLocalCameraPos.value = pos;
                cp.data = d;
                ctp.cameraParams = cp;
            }
            // a wide hero reaches chests, shrines and the teleporter from the edge of his body
            var interactor = body.GetComponent<Interactor>();
            if (interactor) interactor.maxInteractionDistance *= hero.InteractRange;

            // skills: Commando's four slots get the hero's four families (gun, two abilities, the new R); the Deadlock
            // ultimate is not a skill but the hero's equipment (Q, Equipment.cs)
            var sl = body.GetComponent<SkillLocator>();
            var familyField = AccessTools.Field(typeof(GenericSkill), "_skillFamily");
            foreach (var row in Sheet.SkillsOf(hero.Id))
            {
                if (row.Slot == "equipment") continue;
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
                    default: Log.Error($"skill {row.Id}: unknown slot {row.Slot}"); continue;
                }
                familyField.SetValue(gs, fam);
            }
            // passive (what the gun already does on hit), shown on the character select screen
            var passive = sl.passiveSkill;
            passive.enabled = hero.PassiveName != "none";
            if (passive.enabled)
            {
                passive.skillNameToken = Sheet.Token("passive", hero.Id);
                passive.skillDescriptionToken = Sheet.Token("passivedesc", hero.Id);
                passive.icon = DeadlockCache.Sprite(hero.PassiveIcon);
                passive.keywordToken = null;
                Tokens[passive.skillNameToken] = hero.PassiveName;
                Tokens[passive.skillDescriptionToken] = hero.PassiveDescription;
            }
            sl.passiveSkill = passive;

            var tag = body.AddComponent<DeadlockHero>();
            tag.HeroId = hero.Id;
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
            bool primary = row.Slot == "primary", special = row.Slot == "special";
            def.interruptPriority = primary ? InterruptPriority.Any : special ? InterruptPriority.PrioritySkill : InterruptPriority.Skill;
            def.baseRechargeInterval = primary ? 0f : row.Cooldown;
            def.baseMaxStock = (int)Mathf.Max(1, row.Stocks);
            def.rechargeStock = 1;
            def.requiredStock = 1;
            def.stockToConsume = primary ? 0 : 1;
            def.fullRestockOnAssign = true;
            def.isCombatSkill = true;
            def.mustKeyPress = !primary;
            bool movement = row.Kind.StartsWith("dash") || row.Kind == "burrow" || row.Kind == "self_buff" && row.Buff == "smoke";
            def.cancelSprintingOnActivation = !movement;
            def.canceledFromSprinting = false;
            def.hideCooldown = false;
            def.hideStockCount = primary;
            def.beginSkillCooldownOnSkillEnd = row.CooldownOnEnd;
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
                string key = s.Slot == "primary" ? "M1" : s.Slot == "secondary" ? "M2" : s.Slot == "utility" ? "Shift" : s.Slot == "special" ? "R" : "Q (Ultimate equipment)";
                sb.Append("< ! > <style=cIsUtility>").Append(key).Append(" - ").Append(s.Name).Append("</style>\n");
            }
            sb.Append("\n<style=cIsUtility>You start every run holding your Deadlock ultimate as equipment (Q, 120s). Swap it for another and it stays on the ground for anyone to take.</style>");
            Tokens[Sheet.Token("desc", hero.Id)] = sb.ToString();
            foreach (var b in BuffRow.All) Tokens[Sheet.Token("buff", b.Id)] = b.Name;
        }

        /// <summary>A vanilla survivor's body prefab by name (ToolbotBody), for a hero that copies its capsule or camera; null for none.</summary>
        static GameObject VanillaBody(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "none") return null;
            try { return Addressables.LoadAssetAsync<GameObject>("RoR2/Base/" + name.Replace("Body", "") + "/" + name + ".prefab").WaitForCompletion(); }
            catch (Exception e) { Log.Warn($"vanilla body {name}: {e.Message}"); return null; }
        }

        public static SkillRow RowFor(GenericSkill gs) => gs && gs.skillDef && RowOfSkill.TryGetValue(gs.skillDef, out var r) ? r : null;
    }

    /// <summary>Per-body runtime state for the hero kits: magazine and reload, queued heals, smoke timer; Heavyweight, the
    /// rider shown or hidden (Krill on Mo), heavy footsteps and the landing thud.</summary>
    public sealed class HeroRuntime : MonoBehaviour
    {
        public int Ammo = -1;
        public bool Reloading;
        public bool AltMuzzle;
        public float SmokeUntil;
        /// <summary>Server: when Power Surge may arc again (once per burst of the gun).</summary>
        public float NextSurge;
        float reloadEnd;
        int clip;
        CharacterBody body;

        HeroRow hero;
        SoundRow steps;
        Transform rider;
        Vector3 riderScale = Vector3.one;
        bool riderHidden;
        GameObject frenzy;
        float nextStep, airTime;
        bool wasGrounded = true;

        void Awake() => body = GetComponent<CharacterBody>();

        void Start()
        {
            hero = Sheet.Hero(Survivors.HeroOf(body));
            if (hero == null) return;
            // Heavyweight: monsters' hits can't push, pull or knock the hero (jump pads and his own moves still move him)
            if (hero.ImmuneToForce && NetworkServer.active && !body.HasBuff(RoR2Content.Buffs.ImmuneToPhysicsForces)) body.AddBuff(RoR2Content.Buffs.ImmuneToPhysicsForces);
            var gun = Sheet.SkillsOf(hero.Id).FirstOrDefault(s => s.Slot == "primary");
            steps = gun != null ? Sheet.Sound(gun.Id + "/step") : null;
            var model = body.modelLocator ? body.modelLocator.modelTransform : null;
            if (model && hero.RiderBone != "none")
                foreach (var tr in model.GetComponentsInChildren<Transform>(true)) if (tr.name == hero.RiderBone) { rider = tr; riderScale = tr.localScale; break; }
        }

        /// <summary>A buff with melee=true is on (Krill is away): the M1 punches and the rider is hidden.</summary>
        public static bool Melee(CharacterBody b)
        {
            if (!b) return false;
            foreach (var r in BuffRow.All) if (r.Melee && Buffs.Count(b, r.Id) > 0) return true;
            return false;
        }

        /// <summary>A buff with noGun=true is on (burrowed, spinning): the gun can't fire.</summary>
        public static bool NoGun(CharacterBody b)
        {
            if (!b) return false;
            foreach (var r in BuffRow.All) if (r.NoGun && Buffs.Count(b, r.Id) > 0) return true;
            // every skill locked (The Cube, Goo Ball, the DisableAllSkills debuff): a gun still held down stops too
            var gun = b.skillLocator ? b.skillLocator.primary : null;
            return gun && gun.skillDef && gun.skillDef == CharacterBody.CommonAssets.disabledSkill;
        }

        void LateUpdate()
        {
            if (rider)
            {
                bool hide = Melee(body);
                if (hide != riderHidden)
                {
                    riderHidden = hide; rider.localScale = hide ? Vector3.zero : riderScale;
                    // Mo's Frenzy while Krill is away: a red aura around him (read from the synced buff: every player sees it)
                    if (hide) frenzy = Vfx.Play("mokrill_punch", "frenzy", body.footPosition, body.radius, body.transform);
                    else { Vfx.Stop(frenzy, 0.6f); frenzy = null; }
                }
                else if (hide) rider.localScale = Vector3.zero;
            }
            if (steps == null || !body || !body.characterMotor || !body.healthComponent || !body.healthComponent.alive) return;
            var m = body.characterMotor;
            bool grounded = m.isGrounded;
            if (!grounded) airTime += Time.deltaTime;
            else
            {
                // a heavy landing after a real fall: the thud and a short shake (heroes.landShake strong; Viscous' is a tenth)
                if (!wasGrounded && airTime > 0.6f)
                {
                    Fx.Sound(steps.Skill, "hit", body.footPosition);
                    if (hero.LandShake > 0f) ShakeEmitter.CreateSimpleShakeEmitter(body.footPosition, new Wave { amplitude = hero.LandShake, frequency = 25f }, 0.3f, 12f, true);
                }
                airTime = 0f;
                // footsteps paced by how fast he walks (one every 0.55 s at 5 m/s)
                var v = m.velocity; v.y = 0;
                if (v.magnitude > 1f && Time.time >= nextStep)
                {
                    nextStep = Time.time + 0.55f * 5f / Mathf.Max(2f, v.magnitude);
                    Fx.Sound(steps.Skill, "cast", body.footPosition);
                }
            }
            wasGrounded = grounded;
        }

        public void BeginReload(SkillRow gun, float attackSpeed)
        {
            if (Reloading) return;
            Reloading = true;
            clip = (int)gun.ClipSize;
            reloadEnd = Time.time + gun.ReloadTime / Mathf.Max(0.1f, attackSpeed);
            Fx.Sound(gun.Id, "reload", transform.position);
        }

        /// <summary>Owner: n rounds on top of what is left, up to the magazine plus n (Viscous's puddles, puddleAmmo); a reload under
        /// way is called off (the rounds replace it). False without a magazine.</summary>
        public bool GiveAmmo(SkillRow gun, int n)
        {
            if (gun == null || gun.ClipSize <= 0 || n <= 0) return false;
            int clipSize = (int)gun.ClipSize;
            if (Ammo < 0) Ammo = clipSize;
            if (Reloading) { Reloading = false; Ammo = 0; }
            Ammo = Mathf.Min(Ammo + n, clipSize + n);
            MeltyTest.Report("puddle-ammo", true, body ? body.baseNameToken : "");
            return true;
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

        float nextUntarget;
        void FixedUpdate()
        {
            // invisible (Smoke Bomb's vanilla Cloak): RoR2's AI won't pick a cloaked body as a new target, but keeps one it
            // already has, so drop the hero from every enemy AI that is aiming at them; the cloak stops them finding the hero again
            if (!UnityEngine.Networking.NetworkServer.active || !body || !body.hasCloakBuff || Time.fixedTime < nextUntarget) return;
            nextUntarget = Time.fixedTime + 0.2f;
            foreach (var m in CharacterMaster.readOnlyInstancesList)
            {
                if (!m || m.teamIndex == body.teamComponent.teamIndex || m.AiComponents == null) continue;
                foreach (var ai in m.AiComponents)
                    if (ai && ai.currentEnemy != null && ai.currentEnemy.gameObject == body.gameObject) ai.currentEnemy.Reset();
            }
        }
    }

    sealed class DeadlockContentProvider : IContentPackProvider
    {
        readonly ContentPack pack = new ContentPack();
        public string identifier => Plugin.Guid;

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            if (FirstRun.Started)
            {
                // first launch from Thunderstore: the loading screen waits while the heroes are read from Deadlock
                while (FirstRun.Running) { args.ReportProgress(0.45f * FirstRun.Progress); yield return null; }
                FirstRun.Finish(DeadlockCache.DefaultDir());
                Plugin.OpenCache();
                DeadlockAudio.Init();
            }
            if (DeadlockCache.Ready)
            {
                var e = Survivors.Build();
                while (e.MoveNext()) { args.ReportProgress(0.5f); yield return e.Current; }
            }
            pack.bodyPrefabs.Add(Survivors.Bodies.Values.ToArray());
            pack.survivorDefs.Add(Survivors.Defs.Values.ToArray());
            pack.bodyPrefabs.Add(MissingHeroes.Bodies.Values.ToArray());
            pack.survivorDefs.Add(MissingHeroes.Defs.Values.ToArray());
            pack.unlockableDefs.Add(MissingHeroes.Unlockables.Values.ToArray());
            pack.skillDefs.Add(Survivors.SkillDefs.Values.ToArray());
            pack.skillFamilies.Add(Survivors.Families.ToArray());
            pack.entityStateTypes.Add(Survivors.StateTypes.ToArray());
            pack.buffDefs.Add(Buffs.Defs.Values.ToArray());
            pack.equipmentDefs.Add(Equipment.Defs.Values.ToArray());
            if (Net.RelayDef != null) pack.effectDefs.Add(new[] { Net.RelayDef });
            Net.RegisterModList();
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
