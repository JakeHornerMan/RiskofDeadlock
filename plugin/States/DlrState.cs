using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    /// <summary>Base for every hero skill state. One state type per sheet 'kind'; the row (numbers, buff, effect, sounds)
    /// comes from the skill that activated it, so the sheet drives every value.</summary>
    public abstract class DlrState : BaseState, ISkillState
    {
        public GenericSkill activatorSkillSlot { get; set; }
        protected SkillRow row;
        protected HeroRuntime rt;
        protected DeadlockAnimator anim;
        protected float dur;
        CharacterMotor.WalkSpeedPenaltyModifier slow;

        public override void OnEnter()
        {
            base.OnEnter();
            row = Survivors.RowFor(activatorSkillSlot) ?? FallbackRow();
            rt = characterBody ? characterBody.GetComponent<HeroRuntime>() : null;
            anim = DeadlockAnimator.Of(characterBody);
            if (row != null) MeltyTest.Report("skill:" + row.Id, true, characterBody ? characterBody.baseNameToken : "");
        }

        /// <summary>When a state is entered without a skill slot (e.g. test mode), use the hero's row for this state's kind.</summary>
        SkillRow FallbackRow()
        {
            var hero = Survivors.HeroOf(characterBody);
            var k = (KindAttribute)System.Attribute.GetCustomAttribute(GetType(), typeof(KindAttribute));
            if (hero == null || k == null) return null;
            foreach (var r in Sheet.SkillsOf(hero)) if (r.Kind == k.Kind) return r;
            return null;
        }

        protected void SlowWhileActive(float mult)
        {
            if (mult >= 1f || !characterMotor || slow != null) return;
            slow = characterMotor.AddWalkSpeedPenalty(1f - mult);
        }

        public override void OnExit()
        {
            if (slow != null && characterMotor) characterMotor.RemoveWalkSpeedPenalty(slow);
            slow = null;
            base.OnExit();
        }

        protected Vector3 MuzzlePos(bool left = false)
        {
            var m = Fx.Muzzle(characterBody, left);
            return m ? m.position : GetAimRay().origin;
        }

        protected void Sound(string moment) => Fx.Sound(row?.Id, moment, transform.position);

        protected bool Server => NetworkServer.active;

        protected float Coef => row.DamagePct / 100f;

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}
