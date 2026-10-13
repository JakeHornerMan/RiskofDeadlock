using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>BuffDefs built from design/buffs.json plus the hooks that give them their effects.</summary>
    public static class Buffs
    {
        public static readonly Dictionary<string, BuffDef> Defs = new Dictionary<string, BuffDef>();
        // who applied owner-specific debuffs (Fixation, Catalyst) to which victim
        public static readonly Dictionary<CharacterBody, CharacterBody> FixationOwner = new Dictionary<CharacterBody, CharacterBody>();
        public static readonly Dictionary<CharacterBody, CharacterBody> CatalystOwner = new Dictionary<CharacterBody, CharacterBody>();
        // damage taken log for Infernal Resilience: body -> (time, amount)
        public static readonly Dictionary<CharacterBody, List<KeyValuePair<float, float>>> DamageTaken = new Dictionary<CharacterBody, List<KeyValuePair<float, float>>>();

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
            On_Stats();
        }

        static void On_Stats() { }

        public static int Count(CharacterBody b, string id)
        {
            var d = Get(id);
            return b && d ? b.GetBuffCount(d) : 0;
        }

        public static void AddTimed(CharacterBody b, string id, float duration, int max = 0)
        {
            var d = Get(id);
            if (!b || !d || !UnityEngine.Networking.NetworkServer.active) return;
            if (max > 0 && b.GetBuffCount(d) >= max)
            {
                // refresh: drop the oldest timed stack and add a new one
                b.ClearTimedBuffs(d);
                for (int i = 0; i < max; i++) b.AddTimedBuff(d, duration);
                return;
            }
            b.AddTimedBuff(d, duration);
        }

        /// <summary>Infernus' Afterburn ignition: clears the build-up and burns the victim (vanilla Burn DoT, 3 s).</summary>
        public static void Ignite(CharacterBody owner, CharacterBody victim)
        {
            if (!owner || !victim || !UnityEngine.Networking.NetworkServer.active) return;
            Clear(victim, "afterburn_buildup");
            var ab = Sheet.Skill("infernus_afterburn");
            var dot = new InflictDotInfo
            {
                attackerObject = owner.gameObject,
                victimObject = victim.gameObject,
                dotIndex = DotController.DotIndex.Burn,
                duration = 3f,
                damageMultiplier = ab.DamagePct / 100f * 2f, // vanilla Burn ticks 50%/s per 1.0 multiplier
            };
            DotController.InflictDot(ref dot);
            Fx.Sound("infernus_afterburn", "cast", victim.corePosition);
        }

        public static void Clear(CharacterBody b, string id)
        {
            var d = Get(id);
            if (!b || !d || !UnityEngine.Networking.NetworkServer.active) return;
            b.ClearTimedBuffs(d);
            while (b.HasBuff(d)) b.RemoveBuff(d);
        }
    }

    [HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
    static class StatsPatch
    {
        static void Postfix(CharacterBody __instance)
        {
            var b = __instance;
            float moveAdd = 0, attackAdd = 0, armorAdd = 0, regenAdd = 0;
            foreach (var row in BuffRow.All)
            {
                if (row.MoveSpeedAdd == 0 && row.AttackSpeedAdd == 0 && row.ArmorAdd == 0 && row.RegenAdd == 0) continue;
                var d = Buffs.Get(row.Id);
                if (!d) continue;
                int n = b.GetBuffCount(d);
                if (n <= 0) continue;
                int k = row.CanStack ? n : 1;
                moveAdd += row.MoveSpeedAdd * k; attackAdd += row.AttackSpeedAdd * k; armorAdd += row.ArmorAdd * k; regenAdd += row.RegenAdd * k;
            }
            if (moveAdd == 0 && attackAdd == 0 && armorAdd == 0 && regenAdd == 0) return;
            // move speed: positive adds like vanilla speed buffs, negative like slows
            float ms = b.moveSpeed;
            if (moveAdd > 0) ms *= 1f + moveAdd;
            else if (moveAdd < 0) ms /= 1f - moveAdd;
            Set(b, "moveSpeed", ms);
            Set(b, "attackSpeed", b.attackSpeed * (1f + attackAdd));
            Set(b, "armor", b.armor + armorAdd);
            // regen scales with level like vanilla regen
            Set(b, "regen", b.regen + regenAdd * (1f + 0.2f * (b.level - 1f)));
        }

        static readonly System.Collections.Generic.Dictionary<string, System.Reflection.MethodInfo> setters = new System.Collections.Generic.Dictionary<string, System.Reflection.MethodInfo>();
        static void Set(CharacterBody b, string prop, float v)
        {
            if (!setters.TryGetValue(prop, out var m)) setters[prop] = m = AccessTools.PropertySetter(typeof(CharacterBody), prop);
            m.Invoke(b, new object[] { v });
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
            if (attacker)
            {
                var fix = Sheet.Buff("fixation");
                int stacks = Buffs.Count(victim, "fixation");
                if (stacks > 0 && Buffs.FixationOwner.TryGetValue(victim, out var fo) && fo == attacker)
                    damageInfo.damage *= 1f + stacks * fix.DamageTakenFromOwnerPerStack;
                var cat = Sheet.Buff("catalyst");
                if (Buffs.Count(victim, "catalyst") > 0 && Buffs.CatalystOwner.TryGetValue(victim, out var co) && co == attacker)
                    damageInfo.damage *= 1f + cat.DamageTakenFromOwnerPerStack;
            }
        }

        static void Postfix(HealthComponent __instance, DamageInfo damageInfo)
        {
            if (damageInfo == null || damageInfo.rejected || !__instance || !__instance.body) return;
            var b = __instance.body;
            if (!Survivors.IsHero(b, "abrams")) return;
            if (!Buffs.DamageTaken.TryGetValue(b, out var list)) Buffs.DamageTaken[b] = list = new List<KeyValuePair<float, float>>();
            list.Add(new KeyValuePair<float, float>(Time.time, damageInfo.damage));
            if (list.Count > 200) list.RemoveRange(0, 100);
        }
    }
}
