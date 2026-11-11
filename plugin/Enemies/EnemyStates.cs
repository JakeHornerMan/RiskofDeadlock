#if OWN_FILES
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace DeadlockOfRain.Enemies
{
    /// <summary>Marks a body as one of our enemies (enemies.json id).</summary>
    public sealed class EnemyTag : MonoBehaviour
    {
        public string Id;
        public static string Of(CharacterBody b) => b ? b.GetComponent<EnemyTag>()?.Id : null;
    }

    /// <summary>An enemy attack (enemySkills.json row of this state's kind, for the body's enemy). It runs where the body has
    /// authority (the server, for monsters) and on every client from the network state machine, so sounds and clips play in it;
    /// the hit itself only on authority.</summary>
    public abstract class EnemyAttack : BaseState
    {
        protected EnemySkillRow row;
        protected EnemyAnimator anim;
        protected float windup, duration;
        protected bool fired;

        protected abstract string Kind { get; }

        public override void OnEnter()
        {
            base.OnEnter();
            var id = EnemyTag.Of(characterBody);
            row = EnemySkillRow.All.FirstOrDefault(s => s.Enemy == id && s.Kind == Kind) ?? EnemySkillRow.All.First(s => s.Kind == Kind);
            float speed = Mathf.Max(0.1f, attackSpeedStat);
            windup = row.Windup / speed; duration = row.Duration / speed;
            anim = EnemyLook.Of(characterBody)?.Animator;
            StartAimMode(duration + 0.5f);
            // the move's clip timed so its swing (or shot) lands at the end of the windup: the raise is the counter-play
            if (anim != null && row.Clip != "none")
            {
                float len = anim.Length(row.Clip);
                anim.Gesture(row.Clip, len > 0f ? Mathf.Clamp(len * 0.5f / Mathf.Max(0.1f, windup), 0.5f, 2.5f) : 1f);
            }
            Begin();
        }

        protected virtual void Begin() { }
        protected abstract void Fire();

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            // rooted while it attacks, as the Beetle and Lemurian are
            if (isAuthority && characterMotor) characterMotor.moveDirection = Vector3.zero;
            if (!fired && fixedAge >= windup) { fired = true; Fire(); }
            if (isAuthority && fixedAge >= duration) outer.SetNextStateToMain();
        }

        protected Vector3 MuzzlePos()
        {
            var loc = GetModelChildLocator();
            var t = loc ? loc.FindChild("Muzzle") : null;
            return t ? t.position : (characterBody ? characterBody.corePosition : transform.position);
        }

        protected void Sound(string ev, Vector3 at)
        {
            if (ev != "none") DeadlockAudio.Play(ev, at);
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }

    /// <summary>A melee hit: a box reach metres in front (EnemyModel's hitbox), damagePct x damage, after the windup.</summary>
    public abstract class EnemyMelee : EnemyAttack
    {
        protected override void Begin() => Sound(row.CastSound, transform.position);

        protected override void Fire()
        {
            if (!isAuthority) return;
            MeltyTest.EnemyNote("swing", characterBody);
            var attack = new OverlapAttack
            {
                attacker = gameObject,
                inflictor = gameObject,
                teamIndex = GetTeam(),
                damage = damageStat * row.DamagePct,
                procCoefficient = 1f,
                isCrit = RollCrit(),
                hitBoxGroup = FindHitBoxGroup(EnemyModel.MeleeHitBoxGroup),
                hitEffectPrefab = Enemies.MeleeHitEffect,
                forceVector = characterDirection ? characterDirection.forward * 300f : Vector3.zero,
            };
            if (attack.hitBoxGroup == null) { Log.Warn($"{row.Id}: no melee hitbox"); return; }
            var hits = new List<HurtBox>();
            if (!attack.Fire(hits)) return;
            if (row.HitSound != "none") DeadlockAudio.Play(row.HitSound, hits[0].transform.position);
            if (hits.Any(h => h && h.healthComponent && h.healthComponent.body && h.healthComponent.body.isPlayerControlled || h && h.healthComponent && h.healthComponent.body && h.healthComponent.body.master && h.healthComponent.body.master.GetComponent<PlayerCharacterMasterController>()))
                MeltyTest.EnemyNote("hit", characterBody);
        }
    }

    [Kind("trooper_swing")]
    public sealed class TrooperSwing : EnemyMelee { protected override string Kind => "trooper_swing"; }

    [Kind("trooper_close_hit")]
    public sealed class TrooperCloseHit : EnemyMelee { protected override string Kind => "trooper_close_hit"; }

    /// <summary>An aimed shot: the muzzle glows for the windup, then a visible bolt (the Lemurian's fireball, recoloured) flies
    /// at projectileSpeed for damagePct x damage.</summary>
    public abstract class EnemyShot : EnemyAttack
    {
        Light glow;

        protected override void Begin()
        {
            var loc = GetModelChildLocator();
            var at = loc ? loc.FindChild("Muzzle") : null;
            if (!at) return;
            var go = new GameObject("ShotGlow");
            go.transform.SetParent(at, false);
            glow = go.AddComponent<Light>();
            glow.type = LightType.Point; glow.color = Sheet.Color(row.Color); glow.range = 3f; glow.intensity = 0f; glow.shadows = LightShadows.None;
        }

        public override void Update()
        {
            base.Update();
            if (glow) glow.intensity = fired ? 0f : 6f * Mathf.Clamp01(age / Mathf.Max(0.05f, windup));
        }

        protected override void Fire()
        {
            var at = MuzzlePos();
            Sound(row.CastSound, at);
            if (glow) Object.Destroy(glow.gameObject);
            if (Enemies.MuzzleFlash) EffectManager.SimpleMuzzleFlash(Enemies.MuzzleFlash, gameObject, "Muzzle", false);
            if (!isAuthority) return;
            var prefab = Enemies.ShotFor(row.Id);
            if (!prefab) return;
            var aim = GetAimRay();
            // from the muzzle toward where the body aims (its target)
            var dir = aim.direction;
            if (Physics.Raycast(aim, out var hit, 200f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                dir = (hit.point - at).normalized;
            ProjectileManager.instance.FireProjectile(prefab, at, Util.QuaternionSafeLookRotation(dir), gameObject, damageStat * row.DamagePct, 0f, RollCrit(),
                speedOverride: row.ProjectileSpeed);
            MeltyTest.EnemyNote("shot", characterBody);
        }

        public override void OnExit()
        {
            if (glow) Object.Destroy(glow.gameObject);
            base.OnExit();
        }
    }

    [Kind("trooper_shot")]
    public sealed class TrooperShot : EnemyShot { protected override string Kind => "trooper_shot"; }

    [Kind("medic_shot")]
    public sealed class MedicShot : EnemyShot { protected override string Kind => "medic_shot"; }

    /// <summary>Bursting up out of the ground the way a Lemurian does: its spawn effect and sound, the model's landing clip and
    /// the Trooper's own deploy sound.</summary>
    public sealed class EnemySpawn : BaseState
    {
        const float Duration = 1.4f;

        public override void OnEnter()
        {
            base.OnEnter();
            var row = Sheet.Enemy(EnemyTag.Of(characterBody));
            Util.PlaySound("Play_lemurian_spawn", gameObject);
            if (Enemies.SpawnEffect)
            {
                if (Enemies.SpawnEffect.GetComponent<EffectComponent>())
                    EffectManager.SpawnEffect(Enemies.SpawnEffect, new EffectData { origin = transform.position, scale = 1f }, false);
                else Object.Destroy(Object.Instantiate(Enemies.SpawnEffect, transform.position, Quaternion.identity), 3f);
            }
            if (row != null)
            {
                if (row.SpawnSound != "none") DeadlockAudio.Play(row.SpawnSound, transform.position);
                EnemyLook.Of(characterBody)?.Animator?.Gesture(row.SpawnClip);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= Duration) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }

    /// <summary>Death: the shown look goes limp (its ragdoll), the candle goes out, Deadlock's death sound.</summary>
    public sealed class EnemyDeath : GenericCharacterDeath
    {
        protected override void PlayDeathSound()
        {
            var row = Sheet.Enemy(EnemyTag.Of(characterBody));
            if (row != null && row.DeathSound != "none") DeadlockAudio.Play(row.DeathSound, transform.position);
        }

        protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
        {
            var look = EnemyLook.Of(characterBody);
            var rd = look ? look.Ragdoll : null;
            if (rd) rd.BeginRagdoll(characterDirection ? -characterDirection.forward * 200f + Vector3.up * 200f : Vector3.up * 200f);
        }
    }

    /// <summary>A stagger (SetStateOnHurt, a hit over staggerPct of max health): the flinch clip.</summary>
    public sealed class EnemyHurt : HurtState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            var row = Sheet.Enemy(EnemyTag.Of(characterBody));
            if (row != null) EnemyLook.Of(characterBody)?.Animator?.Gesture(row.Flinch);
        }
    }
}
#endif
