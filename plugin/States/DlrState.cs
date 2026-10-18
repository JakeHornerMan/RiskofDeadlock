using System.Linq;
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
        /// <summary>The row to run, for a state started without a skill slot: an ult fired from its equipment (any body can hold it).</summary>
        public SkillRow RowOverride;
        protected SkillRow row;
        protected HeroRuntime rt;
        protected DeadlockAnimator anim;
        protected float dur;
        CharacterMotor.WalkSpeedPenaltyModifier slow;

        public override void OnEnter()
        {
            base.OnEnter();
            row = RowOverride ?? Survivors.RowFor(activatorSkillSlot) ?? FallbackRow();
            rt = characterBody ? characterBody.GetComponent<HeroRuntime>() : null;
            anim = DeadlockAnimator.Of(characterBody);
            if (isAuthority) OwnerAim = GetAimRay().direction;
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

        /// <summary>The enemy a lock-on move (Phantom Strike, Haymaker) picked. The hero's owner picks it in OnEnter and it
        /// travels with the state, so the server and the other players act on the same enemy.</summary>
        protected HurtBox LockTarget;

        /// <summary>Where the owner aimed when the skill started. RoR2 doesn't send a player's aim to the server, so there
        /// a joined player's body looks straight ahead; throws use this instead (<see cref="ThrowDir"/>).</summary>
        protected Vector3 OwnerAim;

        /// <summary>The direction to throw along: the live aim on the owner, the aim it sent with the state elsewhere.</summary>
        protected Vector3 ThrowDir => isAuthority || OwnerAim == Vector3.zero ? GetAimRay().direction : OwnerAim;

        // the row travels with the state, so an ult fired from equipment runs the same row on the server and every client;
        // so does the lock-on target (OnSerialize runs after OnEnter on the owner, OnDeserialize before it everywhere else)
        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(RowOverride?.Id ?? "");
            writer.Write(HurtBoxReference.FromHurtBox(LockTarget));
            writer.Write(OwnerAim);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            var id = reader.ReadString();
            if (!string.IsNullOrEmpty(id)) RowOverride = Sheet.Skill(id);
            LockTarget = reader.ReadHurtBoxReference().ResolveHurtBox();
            OwnerAim = reader.ReadVector3();
        }

        /// <summary>Lock-on: the owner picks the enemy under the crosshair; everyone else uses the one it sent.</summary>
        protected HurtBox LockOn(float range)
        {
            if (isAuthority) LockTarget = AimTarget(range);
            if (MeltyTest.Active) Log.Info($"lock-on {row?.Id} ({(isAuthority ? "owner" : "")}{(Server ? " server" : "")}): {(LockTarget && LockTarget.healthComponent ? LockTarget.healthComponent.name : "no target")}");
            return LockTarget && LockTarget.healthComponent && LockTarget.healthComponent.alive && LockTarget.healthComponent.body ? LockTarget : null;
        }

        Vector3 lastMotionPos;
        /// <summary>Where the body is heading: the owner steers it, so elsewhere (the server, other players) read it from how
        /// the body moved since the last tick; keeps the given direction while it barely moves.</summary>
        protected Vector3 MotionDir(Vector3 current)
        {
            var p = transform.position;
            var d = p - lastMotionPos; d.y = 0;
            bool first = lastMotionPos == Vector3.zero;
            lastMotionPos = p;
            if (isAuthority || first || d.sqrMagnitude < 0.0025f) return current;
            return d.normalized;
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

        /// <summary>The living enemy closest to the crosshair within range (and angle degrees of the aim), in line of sight; null if none.
        /// Lock-on moves (Phantom Strike, Haymaker) cast only on a target.</summary>
        protected HurtBox AimTarget(float range, float angle = 15f)
        {
            var aim = GetAimRay();
            var search = new BullseyeSearch
            {
                teamMaskFilter = TeamMask.GetEnemyTeams(teamComponent.teamIndex),
                filterByLoS = true,
                searchOrigin = aim.origin,
                searchDirection = aim.direction,
                maxDistanceFilter = range,
                maxAngleFilter = angle,
                sortMode = BullseyeSearch.SortMode.Angle,
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(gameObject);
            return search.GetResults().FirstOrDefault(h => h && h.healthComponent && h.healthComponent.alive && h.healthComponent.body);
        }

        /// <summary>A lock-on move found no target: give the charge back so the key stays ready.</summary>
        protected void RefundStock()
        {
            if (isAuthority && activatorSkillSlot) activatorSkillSlot.AddOneStock();
        }

        protected int Sound(string moment) => Fx.Sound(row?.Id, moment, transform.position);

        protected bool Server => NetworkServer.active;

        protected float Coef => row.DamagePct / 100f;

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}
