using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Lookups over the generated sheet rows.</summary>
    public static class Sheet
    {
        static Dictionary<string, SkillRow> skills;
        static Dictionary<string, BuffRow> buffs;
        static Dictionary<string, EffectRow> effects;
        static Dictionary<string, SoundRow> sounds;

        public static HeroRow Hero(string id) => HeroRow.All.First(h => h.Id == id);
        public static StatRow Stats(string hero) => StatRow.All.First(s => s.Hero == hero);
        public static AnimRow Anims(string hero) => AnimRow.All.First(a => a.Hero == hero);
        public static IEnumerable<SkillRow> SkillsOf(string hero) => SkillRow.All.Where(s => s.Hero == hero);
        public static SkillRow SkillOf(string hero, string slot) => SkillRow.All.First(s => s.Hero == hero && s.Slot == slot);

        public static SkillRow Skill(string id)
        {
            if (skills == null) skills = SkillRow.All.ToDictionary(s => s.Id);
            return id != null && skills.TryGetValue(id, out var r) ? r : null;
        }

        public static BuffRow Buff(string id)
        {
            if (buffs == null) buffs = BuffRow.All.ToDictionary(s => s.Id);
            return id != null && buffs.TryGetValue(id, out var r) ? r : null;
        }

        public static EffectRow Effect(string id)
        {
            if (effects == null) effects = EffectRow.All.ToDictionary(s => s.Id);
            return id != null && effects.TryGetValue(id, out var r) ? r : null;
        }

        public static SoundRow Sound(string skill)
        {
            if (sounds == null) sounds = SoundRow.All.ToDictionary(s => s.Skill);
            return skill != null && sounds.TryGetValue(skill, out var r) ? r : null;
        }

        public static Color Color(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : UnityEngine.Color.white;
        }

        public static string Token(string kind, string id) => ("DLR_" + kind + "_" + id).ToUpperInvariant();
    }
}
