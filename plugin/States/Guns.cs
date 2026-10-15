using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    /// <summary>Deadlock gun on M1: holds while the button is held, fires at the sheet's cycle time (scaled by attack speed),
    /// uses the hero's magazine and reload, and applies the hero's on-hit passive (Fixation, Static, Afterburn) and breaks Smoke Bomb.</summary>
    public abstract class GunState : DlrState
    {
        float nextShot;
        int burstLeft;
        float nextBurstShot;

        public override void OnEnter()
        {
            base.OnEnter();
            if (rt != null && rt.Ammo < 0) rt.Ammo = (int)row.ClipSize;
            nextShot = 0f;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { outer.SetNextStateToMain(); return; }
            characterBody.SetAimTimer(1f);
            float t = fixedAge;
            if (burstLeft > 0 && t >= nextBurstShot)
            {
                FireOne();
                burstLeft--;
                nextBurstShot = t + row.Duration / attackSpeedStat;
            }
            if (t >= nextShot && burstLeft == 0 && isAuthority)
            {
                if (!(inputBank && inputBank.skill1.down) && !MeltyTest.WantsFire(characterBody)) { outer.SetNextStateToMain(); return; }
                if (rt != null && rt.Reloading) return;
                if (rt != null && rt.Ammo <= 0) { StartReload(); return; }
                nextShot = t + row.Interval / attackSpeedStat;
                int burst = (int)Mathf.Max(1, row.Burst);
                if (row.Kind == "gun_burst" && burst > 1)
                {
                    FireOne();
                    burstLeft = burst - 1;
                    nextBurstShot = t + row.Duration / attackSpeedStat;
                }
                else FireOne();
            }
        }

        void StartReload()
        {
            if (rt == null || rt.Reloading) return;
            rt.BeginReload(row, attackSpeedStat);
            if (anim) anim.Gesture("reload", 1f);
        }

        void FireOne()
        {
            if (rt != null)
            {
                if (rt.Ammo <= 0) { StartReload(); burstLeft = 0; return; }
                rt.Ammo--;
                rt.AltMuzzle = !rt.AltMuzzle;
            }
            // firing reveals Haze from Smoke Bomb
            if (Server && Buffs.Count(characterBody, "smoke") > 0)
            {
                Buffs.Clear(characterBody, "smoke");
                if (characterBody.HasBuff(RoR2Content.Buffs.Cloak)) characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);
                Fx.Sound("haze_smoke_bomb", "end", transform.position);
            }
            bool left = rt != null && rt.AltMuzzle && row.Hero == "haze";
            if (anim) anim.Recoil(row.Pellets > 1 ? 9f : 3f);
            Sound("cast");
            var aim = GetAimRay();
            var muzzle = Fx.Muzzle(characterBody, left);
            Fx.Impact("muzzle", muzzle ? muzzle.position : aim.origin);
            if (isAuthority)
            {
                var ba = new BulletAttack
                {
                    owner = gameObject,
                    weapon = gameObject,
                    origin = aim.origin,
                    aimVector = aim.direction,
                    minSpread = 0f,
                    maxSpread = row.SpreadDeg,
                    bulletCount = (uint)Mathf.Max(1, row.Pellets),
                    damage = damageStat * Coef,
                    procCoefficient = row.ProcCoefficient,
                    force = 100f,
                    tracerEffectPrefab = Fx.Prefab(row.Effect),
                    muzzleName = left ? "MuzzleLeft" : "MuzzleRight",
                    hitEffectPrefab = null,
                    isCrit = RollCrit(),
                    radius = row.Pellets > 1 ? 0.3f : 0.2f,
                    smartCollision = true,
                    maxDistance = row.Range,
                    falloffModel = row.Pellets > 1 ? BulletAttack.FalloffModel.Buckshot : BulletAttack.FalloffModel.DefaultBullet,
                    damageType = new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Primary },
                    hitCallback = OnHit,
                };
                ba.Fire();
            }
            MeltyTest.Report("gun-fires", true, characterBody.baseNameToken);
        }

        bool OnHit(BulletAttack attack, ref BulletAttack.BulletHit hit)
        {
            bool r = BulletAttack.DefaultHitCallback(attack, ref hit);
            var hb = hit.hitHurtBox;
            if (!hb || !hb.healthComponent || !NetworkServer.active) return r;
            var victim = hb.healthComponent.body;
            if (!victim || victim.teamComponent.teamIndex == characterBody.teamComponent.teamIndex) return r;
            OnBulletHit(victim, hb, hit.point, attack.isCrit);
            return r;
        }

        protected virtual void OnBulletHit(CharacterBody victim, HurtBox hb, Vector3 point, bool crit)
        {
            // the hero's passive: every hit stacks the gun's buff (Fixation, Static, Afterburn build-up); its trigger fires at full stacks
            if (row.Buff == "none") return;
            bool fired = Buffs.Stack(characterBody, victim, row.Buff, crit);
            Vfx.Play(row.Vfx, fired ? "trigger" : "hit", point, 1f, victim.transform);
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }

    [Kind("gun_auto")] public sealed class GunAuto : GunState { }
    [Kind("gun_burst")] public sealed class GunBurst : GunState { }
    [Kind("gun_shotgun")] public sealed class GunShotgun : GunState { }
}
