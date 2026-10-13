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
    /// <summary>The Deadlock ultimate in a 5th skill slot: locked (0 stock) at spawn, charged by damage dealt and kills,
    /// fired with the ult key (default F), then empty again.</summary>
    public sealed class UltController : MonoBehaviour
    {
        public GenericSkill Skill;
        public float Charge;
        public float Required = 5000f;
        public float KillCharge = 150f;
        public bool UltDamageInFlight;
        CharacterBody body;
        public static readonly List<UltController> Instances = new List<UltController>();

        public float Fraction => Required > 0 ? Mathf.Clamp01(Charge / Required) : 0f;
        public bool IsReady => Charge >= Required;

        void Awake()
        {
            body = GetComponent<CharacterBody>();
            foreach (var gs in GetComponents<GenericSkill>()) if (gs.skillName == "DLR_Ult") Skill = gs;
            var hero = Survivors.HeroOf(body);
            var row = hero != null ? Sheet.SkillOf(hero, "ult") : null;
            if (row != null) { Required = row.UltCharge; KillCharge = row.KillCharge; }
        }

        void OnEnable() => Instances.Add(this);
        void OnDisable() => Instances.Remove(this);

        public void Add(float amount)
        {
            if (amount <= 0 || IsReady) return;
            Charge = Mathf.Min(Required, Charge + amount);
        }

        public void Consume()
        {
            Charge = 0f;
        }

        void FixedUpdate()
        {
            if (!Skill) return;
            // the stock mirrors the meter: nothing (not items, not ammo packs) unlocks the ult except charging it
            int want = IsReady ? 1 : 0;
            if (Skill.stock != want && !(want == 0 && Skill.stock > 0 && IsUltRunning())) Skill.stock = want;
        }

        bool IsUltRunning() => false;

        void Update()
        {
            if (!Skill || !body || !body.hasEffectiveAuthority) return;
            var lu = LocalUserManager.GetFirstLocalUser();
            if (lu == null || lu.cachedBody != body) return;
            if (lu.isUIFocused || PauseManager.isPaused) return;
            if (Input.GetKeyDown(Plugin.UltKey.Value) || MeltyTest.WantsUlt(body))
            {
                if (IsReady && Skill.ExecuteIfReady())
                {
                    Consume();
                    Skill.stock = 0;
                    MeltyTest.Report("ult-fires", true, body.baseNameToken);
                }
            }
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

    [HarmonyPatch(typeof(HUD), "Update")]
    static class UltHudPatch
    {
        static readonly Dictionary<HUD, SkillIcon> icons = new Dictionary<HUD, SkillIcon>();
        static readonly Dictionary<HUD, TextMeshProUGUI> labels = new Dictionary<HUD, TextMeshProUGUI>();

        static void Postfix(HUD __instance)
        {
            var hud = __instance;
            var body = hud.targetBodyObject ? hud.targetBodyObject.GetComponent<CharacterBody>() : null;
            var u = UltController.Of(body);
            icons.TryGetValue(hud, out var icon);
            if (!u || !u.Skill)
            {
                if (icon) icon.gameObject.SetActive(false);
                return;
            }
            if (!icon)
            {
                if (hud.skillIcons == null || hud.skillIcons.Length < 4 || !hud.skillIcons[3]) return;
                var special = hud.skillIcons[3];
                var go = Object.Instantiate(special.gameObject, special.transform.parent);
                go.name = "DeadlockUltIcon";
                var rt = (RectTransform)go.transform;
                var srt = (RectTransform)special.transform;
                var prev = (RectTransform)hud.skillIcons[2].transform;
                float step = srt.anchoredPosition.x - prev.anchoredPosition.x;
                if (Mathf.Abs(step) < 1f) step = srt.rect.width + 8f;
                rt.anchoredPosition = srt.anchoredPosition + new Vector2(step, 0);
                icon = go.GetComponent<SkillIcon>();
                icons[hud] = icon;
                // key label: reuse the special icon's key text if it has one
                foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (t.gameObject.name.ToLowerInvariant().Contains("key") || t.GetComponent<InputBindingDisplayController>())
                    {
                        var ib = t.GetComponent<InputBindingDisplayController>();
                        if (ib) Object.Destroy(ib);
                        t.text = Plugin.UltKey.Value.ToString();
                    }
                var labelGo = new GameObject("DeadlockUltCharge", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                var lrt = (RectTransform)labelGo.transform;
                lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1); lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                var any = go.GetComponentInChildren<TextMeshProUGUI>(true);
                if (any) label.font = any.font;
                label.fontSize = 18; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                labels[hud] = label;
            }
            icon.gameObject.SetActive(true);
            icon.targetSkill = u.Skill;
            icon.targetSkillSlot = SkillSlot.None;
            icon.playerCharacterMasterController = hud.targetMaster ? hud.targetMaster.playerCharacterMasterController : null;
            if (labels.TryGetValue(hud, out var l) && l)
            {
                l.text = u.IsReady ? "<color=#FFE36B>READY</color>" : Mathf.FloorToInt(u.Fraction * 100f) + "%";
            }
        }
    }
}
