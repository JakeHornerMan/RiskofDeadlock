using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>BuffDefs built from design/buffs.json plus the hooks that give them their effects.</summary>
    public static class Buffs
    {
        public static readonly Dictionary<string, BuffDef> Defs = new Dictionary<string, BuffDef>();
        // who applied owner-specific debuffs (Fixation, Napalm: damageTakenFromOwnerPerStack) to which victim, per buff id
        static readonly Dictionary<string, Dictionary<CharacterBody, CharacterBody>> owners = new Dictionary<string, Dictionary<CharacterBody, CharacterBody>>();

        public static BuffDef Get(string id) => id != null && Defs.TryGetValue(id, out var b) ? b : null;

        public static void Create()
        {
            foreach (var row in BuffRow.All)
            {
                var def = ScriptableObject.CreateInstance<BuffDef>();
                ((ScriptableObject)def).name = "bdDeadlock" + row.Id;
                def.buffColor = Sheet.Color(row.Color);
                def.canStack = row.CanStack;
                def.isDebuff = row.IsDebuff;
                def.isHidden = row.IsHidden;
                def.iconSprite = DeadlockCache.Sprite(row.Icon);
                Defs[row.Id] = def;
            }
        }

        public static void InitHooks()
        {
            GlobalEventManager.onServerDamageDealt += HealBack.OnDamage;
        }

        public static int Count(CharacterBody b, string id)
        {
            var d = Get(id);
            return b && d ? b.GetBuffCount(d) : 0;
        }

        public static CharacterBody OwnerOf(CharacterBody victim, string id) =>
            victim && owners.TryGetValue(id, out var m) && m.TryGetValue(victim, out var o) ? o : null;

        public static void AddTimed(CharacterBody b, string id, float duration, int max = 0)
        {
            var d = Get(id);
            if (!b || !d || !NetworkServer.active) return;
            if (max > 0 && b.GetBuffCount(d) >= max)
            {
                // refresh: drop the oldest timed stack and add a new one
                b.ClearTimedBuffs(d);
                for (int i = 0; i < max; i++) b.AddTimedBuff(d, duration);
                return;
            }
            b.AddTimedBuff(d, duration);
        }

        /// <summary>A one-stack debuff (Napalm, Fixation Slow...) from its row: refreshed, owner recorded, withBuff applied too.</summary>
        public static void Apply(CharacterBody owner, CharacterBody victim, string id)
        {
            var row = Sheet.Buff(id);
            if (row == null || !victim || !NetworkServer.active) return;
            Clear(victim, id);
            AddTimed(victim, id, row.Duration);
            SetOwner(owner, victim, id);
            if (row.WithBuff != "none" && row.WithBuff != id) Apply(owner, victim, row.WithBuff);
        }

        /// <summary>One hit's worth of a stacking debuff (Fixation, Static, Afterburn build-up): a crit adds critStacks, and the
        /// row's trigger fires at triggerStacks. Returns true when it fired.</summary>
        public static bool Stack(CharacterBody owner, CharacterBody victim, string id, bool crit)
        {
            var row = Sheet.Buff(id);
            if (row == null || !owner || !victim || !NetworkServer.active) return false;
            int max = row.CanStack ? (int)row.MaxStacks : 1;
            int before = Count(victim, id);
            int add = crit && row.CritStacks > 0 ? (int)row.CritStacks : 1;
            for (int i = 0; i < add; i++) AddTimed(victim, id, row.Duration, max);
            SetOwner(owner, victim, id);
            if (row.WithBuff != "none") Apply(owner, victim, row.WithBuff);
            int n = (int)row.TriggerStacks;
            if (n <= 0) return false;
            int after = Mathf.Min(max, before + add);
            bool fire = row.TriggerClears ? after >= n : before / n < after / n;
            if (!fire) return false;
            if (row.TriggerClears) Clear(victim, id);
            Trigger(owner, victim, row);
            return true;
        }

        /// <summary>The stack trigger: damage to the victim and nearby enemies, a stun, a follow-up buff and/or a burn, drawn
        /// in the buff's color (arcs to the extra targets).</summary>
        static void Trigger(CharacterBody owner, CharacterBody victim, BuffRow row)
        {
            var hit = new List<HurtBox>();
            if (victim.mainHurtBox) hit.Add(victim.mainHurtBox);
            if (row.TriggerTargets > 0)
                foreach (var hb in Fx.Enemies(owner, victim.corePosition, row.TriggerRadius))
                {
                    if (hit.Count > row.TriggerTargets) break;
                    if (hb.healthComponent.body != victim) hit.Add(hb);
                }
            // server only: every player sees the flash and arcs through the relay
            var color = Sheet.Color(row.Color);
            Relay.Flash(victim.corePosition, color, 6f, 0.25f);
            foreach (var hb in hit)
            {
                var b = hb.healthComponent.body;
                if (b != victim) Relay.Bolt(victim.corePosition, b ? b.corePosition : hb.transform.position, color, 0.35f, 0.25f);
                // a trigger is one hit, not a chain of procs
                if (row.TriggerDamagePct > 0) Fx.Hit(owner, hb, row.TriggerDamagePct / 100f, 0.5f, hb.transform.position, default, DamageColorIndex.WeakPoint);
                if (row.TriggerStun > 0) Fx.Stun(hb.healthComponent, row.TriggerStun);
                if (row.TriggerBuff != "none" && b) Apply(owner, b, row.TriggerBuff);
                if (row.DotDamagePct > 0 && b) Burn(owner, b, row.DotDamagePct);
            }
            MeltyTest.Report("trigger:" + row.Id, true, owner.baseNameToken);
        }

        /// <summary>Infernus' Afterburn ignition: the vanilla Burn DoT for 3 s at pct %/s.</summary>
        public static void Burn(CharacterBody owner, CharacterBody victim, float pct)
        {
            if (!owner || !victim || !NetworkServer.active) return;
            var dot = new InflictDotInfo
            {
                attackerObject = owner.gameObject,
                victimObject = victim.gameObject,
                dotIndex = DotController.DotIndex.Burn,
                duration = 3f,
                damageMultiplier = pct / 100f * 2f, // vanilla Burn ticks 50%/s per 1.0 multiplier
            };
            DotController.InflictDot(ref dot);
            Relay.Sound("infernus_gun", "hit", victim.corePosition);
        }

        public static void Clear(CharacterBody b, string id)
        {
            var d = Get(id);
            if (!b || !d || !NetworkServer.active) return;
            b.ClearTimedBuffs(d);
            while (b.HasBuff(d)) b.RemoveBuff(d);
        }

        static void SetOwner(CharacterBody owner, CharacterBody victim, string id)
        {
            if (!owners.TryGetValue(id, out var m)) owners[id] = m = new Dictionary<CharacterBody, CharacterBody>();
            m[victim] = owner;
        }
    }

    /// <summary>Abrams' Infernal Resilience: heals back damageHealBack of every hit taken, spread over damageHealBackTime.</summary>
    public sealed class HealBack : MonoBehaviour
    {
        struct Part { public float Rate, Until; }
        readonly List<Part> parts = new List<Part>();
        HealthComponent hc;
        float next;

        public static void OnDamage(DamageReport r)
        {
            if (r == null || !r.victimBody || r.damageDealt <= 0f) return;
            var hero = Sheet.Hero(Survivors.HeroOf(r.victimBody));
            if (hero == null || hero.DamageHealBack <= 0f || hero.DamageHealBackTime <= 0f) return;
            var hb = r.victimBody.GetComponent<HealBack>();
            if (!hb) hb = r.victimBody.gameObject.AddComponent<HealBack>();
            hb.parts.Add(new Part { Rate = r.damageDealt * hero.DamageHealBack / hero.DamageHealBackTime, Until = Time.fixedTime + hero.DamageHealBackTime });
        }

        void Awake() => hc = GetComponent<HealthComponent>();

        void FixedUpdate()
        {
            if (!NetworkServer.active || !hc || !hc.alive || parts.Count == 0 || Time.fixedTime < next) return;
            // heal in quarter-second pulses so the numbers stay readable
            const float pulse = 0.25f;
            next = Time.fixedTime + pulse;
            float amount = 0f;
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                amount += parts[i].Rate * pulse;
                if (Time.fixedTime >= parts[i].Until) parts.RemoveAt(i);
            }
            if (amount > 0f) hc.Heal(amount, default);
        }
    }

    [HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
    static class StatsPatch
    {
        static void Postfix(CharacterBody __instance)
        {
            var b = __instance;
            float moveAdd = 0, attackAdd = 0, armorAdd = 0, regenAdd = 0, slowResist = 0;
            foreach (var row in BuffRow.All)
            {
                if (row.MoveSpeedAdd == 0 && row.AttackSpeedAdd == 0 && row.ArmorAdd == 0 && row.RegenAdd == 0 && row.SlowResist == 0) continue;
                var d = Buffs.Get(row.Id);
                if (!d) continue;
                int n = b.GetBuffCount(d);
                if (n <= 0) continue;
                int k = row.CanStack ? n : 1;
                moveAdd += row.MoveSpeedAdd * k; attackAdd += row.AttackSpeedAdd * k; armorAdd += row.ArmorAdd * k; regenAdd += row.RegenAdd * k;
                slowResist = Mathf.Max(slowResist, row.SlowResist);
            }
            if (moveAdd == 0 && attackAdd == 0 && armorAdd == 0 && regenAdd == 0 && slowResist == 0) return;
            // move speed: positive adds like vanilla speed buffs, negative like slows
            float ms = b.moveSpeed;
            if (moveAdd > 0) ms *= 1f + moveAdd;
            else if (moveAdd < 0) ms /= 1f - moveAdd;
            if (slowResist > 0)
            {
                // ignore that fraction of whatever slows pulled the speed under the unslowed base speed
                float floor = (b.baseMoveSpeed + b.levelMoveSpeed * (b.level - 1f)) * (b.isSprinting ? b.sprintingSpeedMultiplier : 1f);
                if (ms < floor) ms += (floor - ms) * slowResist;
            }
            Set(b, "moveSpeed", ms);
            Set(b, "attackSpeed", b.attackSpeed * (1f + attackAdd));
            Set(b, "armor", b.armor + armorAdd);
            // regen scales with level like vanilla regen
            Set(b, "regen", b.regen + regenAdd * (1f + 0.2f * (b.level - 1f)));
        }

        static readonly Dictionary<string, System.Reflection.MethodInfo> setters = new Dictionary<string, System.Reflection.MethodInfo>();
        static void Set(CharacterBody b, string prop, float v)
        {
            if (!setters.TryGetValue(prop, out var m)) setters[prop] = m = AccessTools.PropertySetter(typeof(CharacterBody), prop);
            m.Invoke(b, new object[] { v });
        }
    }

    /// <summary>Infernal Resilience's debuff resistance: timed debuffs on a hero with debuffResist last that much less.</summary>
    [HarmonyPatch(typeof(CharacterBody), "AddTimedBuffInternal")]
    static class DebuffResistPatch
    {
        static void Prefix(CharacterBody __instance, BuffDef buffDef, ref float duration)
        {
            if (!buffDef || !buffDef.isDebuff) return;
            var hero = Sheet.Hero(Survivors.HeroOf(__instance));
            if (hero != null && hero.DebuffResist > 0f) duration *= 1f - hero.DebuffResist;
        }
    }

    [HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
    static class DamagePatch
    {
        static readonly System.Random rng = new System.Random();

        static void Prefix(HealthComponent __instance, DamageInfo damageInfo)
        {
            if (damageInfo == null || !__instance) return;
            var victim = __instance.body;
            if (!victim) return;
            var attacker = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;

            // Bullet Dance evasion
            var dance = Sheet.Buff("bullet_dance");
            if (dance != null && Buffs.Count(victim, "bullet_dance") > 0 && attacker && attacker != victim && rng.NextDouble() < dance.EvasionChance)
            {
                damageInfo.rejected = true;
                return;
            }
            if (!attacker) return;
            // Fixation, Napalm: the victim takes more damage from whoever applied the debuff
            foreach (var row in BuffRow.All)
            {
                if (row.DamageTakenFromOwnerPerStack <= 0) continue;
                int stacks = Buffs.Count(victim, row.Id);
                if (stacks <= 0 || Buffs.OwnerOf(victim, row.Id) != attacker) continue;
                damageInfo.damage *= 1f + (row.CanStack ? stacks : 1) * row.DamageTakenFromOwnerPerStack;
            }
        }
    }
}
