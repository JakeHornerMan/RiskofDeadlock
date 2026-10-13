using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>The Deadlock ultimate in RoR2's special slot (R): locked (0 stock) at spawn, charged by damage dealt and kills,
    /// usable once the meter is full, then empty again.</summary>
    public sealed class UltController : MonoBehaviour
    {
        public GenericSkill Skill;
        public float Charge;
        public float Required = 5000f;
        public float KillCharge = 150f;
        public bool UltDamageInFlight;
        CharacterBody body;

        public float Fraction => Required > 0 ? Mathf.Clamp01(Charge / Required) : 0f;
        public bool IsReady => Charge >= Required;

        void Awake()
        {
            body = GetComponent<CharacterBody>();
            var sl = GetComponent<SkillLocator>();
            Skill = sl ? sl.special : null;
            var hero = Survivors.HeroOf(body);
            var row = hero != null ? Sheet.SkillOf(hero, "ult") : null;
            if (row != null) { Required = row.UltCharge; KillCharge = row.KillCharge; }
            // empty the meter the moment R is used, before the stock mirror below could refill the stock
            if (body) body.onSkillActivatedAuthority += s => { if (s == Skill) Consume(); };
        }

        public void Add(float amount)
        {
            if (amount <= 0 || IsReady) return;
            Charge = Mathf.Min(Required, Charge + amount);
        }

        public void Consume() => Charge = 0f;

        void FixedUpdate()
        {
            if (!Skill) return;
            // the stock mirrors the meter: nothing (not items, not cooldown resets) unlocks the ult except charging it
            int want = IsReady ? 1 : 0;
            if (Skill.stock != want) Skill.stock = want;
        }

        public static UltController Of(CharacterBody b) => b ? b.GetComponent<UltController>() : null;
    }

    public static class Ult
    {
        public static void Init()
        {
            GlobalEventManager.onServerDamageDealt += OnDamage;
            GlobalEventManager.onCharacterDeathGlobal += OnDeath;
        }

        static void OnDamage(DamageReport r)
        {
            if (r == null || !r.attackerBody) return;
            var u = UltController.Of(r.attackerBody);
            if (!u || u.UltDamageInFlight) return;
            float dmgStat = Mathf.Max(1f, r.attackerBody.damage);
            u.Add(r.damageDealt / dmgStat * 100f);
            if (u.Fraction > 0) MeltyTest.Report("ult-charges", true, r.attackerBody.baseNameToken);
        }

        static void OnDeath(DamageReport r)
        {
            if (r == null || !r.attackerBody) return;
            var u = UltController.Of(r.attackerBody);
            if (u && !u.UltDamageInFlight) u.Add(u.KillCharge);
        }
    }

    /// <summary>Shows the ult meter on the R skill icon: charge % while it fills, READY when full.</summary>
    [HarmonyPatch(typeof(HUD), "Update")]
    static class UltHudPatch
    {
        static readonly Dictionary<HUD, TextMeshProUGUI> labels = new Dictionary<HUD, TextMeshProUGUI>();

        static void Postfix(HUD __instance)
        {
            var hud = __instance;
            var body = hud.targetBodyObject ? hud.targetBodyObject.GetComponent<CharacterBody>() : null;
            var u = UltController.Of(body);
            labels.TryGetValue(hud, out var label);
            if (!u || !u.Skill)
            {
                if (label) label.gameObject.SetActive(false);
                return;
            }
            if (!label)
            {
                if (hud.skillIcons == null || hud.skillIcons.Length < 4 || !hud.skillIcons[3]) return;
                var icon = hud.skillIcons[3];
                var go = new GameObject("DeadlockUltCharge", typeof(RectTransform));
                go.transform.SetParent(icon.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                label = go.AddComponent<TextMeshProUGUI>();
                var any = icon.GetComponentInChildren<TextMeshProUGUI>(true);
                if (any) label.font = any.font;
                label.fontSize = 18; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                labels[hud] = label;
            }
            label.gameObject.SetActive(true);
            label.text = u.IsReady ? "<color=#FFE36B>READY</color>" : Mathf.FloorToInt(u.Fraction * 100f) + "%";
        }
    }
}
