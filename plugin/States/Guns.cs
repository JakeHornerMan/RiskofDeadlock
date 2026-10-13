using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    /// <summary>Deadlock gun on M1: holds while the button is held, fires at the sheet's cycle time (scaled by attack speed),
    /// uses the hero's magazine and reload, and applies the hero's on-hit rules (Fixation, Afterburn, Power Surge, Smoke break).</summary>
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
                float rate = attackSpeedStat * (Buffs.Count(characterBody, "fixation_focus") > 0 ? 1f + Sheet.Buff("fixation_focus").AttackSpeedAdd : 1f);
                nextShot = t + row.Interval / rate;
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
            if (anim) anim.Gesture(left ? "fire_alt" : "fire", 1f);
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
            OnBulletHit(victim, hb, hit.point);
            return r;
        }

        protected virtual void OnBulletHit(CharacterBody victim, HurtBox hb, Vector3 point)
        {
            var owner = characterBody;
            // Haze: every hit stacks Fixation (3 per hit during Fixation focus)
            if (row.Buff == "fixation")
            {
                var fix = Sheet.Buff("fixation");
                int add = Buffs.Count(owner, "fixation_focus") > 0 ? (int)Sheet.Skill("haze_fixation").Burst : 1;
                for (int i = 0; i < add; i++) Buffs.AddTimed(victim, "fixation", fix.Duration, (int)fix.MaxStacks);
                Buffs.FixationOwner[victim] = owner;
            }
            // Infernus: hits build Afterburn; at full build-up (or always during Afterburn) the target burns
            if (row.Buff == "afterburn_buildup")
            {
                var bu = Sheet.Buff("afterburn_buildup");
                bool overdrive = Buffs.Count(owner, "afterburn_overdrive") > 0;
                Buffs.AddTimed(victim, "afterburn_buildup", bu.Duration, (int)bu.MaxStacks);
                if (overdrive || Buffs.Count(victim, "afterburn_buildup") >= bu.MaxStacks)
                {
                    Buffs.Clear(victim, "afterburn_buildup");
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
                    Fx.Sound("infernus_afterburn", "cast", point);
                }
            }
            // Seven: Power Surge chains lightning from every hit
            if (row.Hero == "seven" && Buffs.Count(owner, "power_surge") > 0)
            {
                var ps = Sheet.Skill("seven_power_surge");
                var orb = new LightningOrb
                {
                    origin = point,
                    damageValue = owner.damage * ps.DamagePct / 100f,
                    isCrit = owner.RollCrit(),
                    bouncesRemaining = (int)ps.ChainCount - 1,
                    teamIndex = owner.teamComponent.teamIndex,
                    attacker = owner.gameObject,
                    procCoefficient = ps.ProcCoefficient,
                    lightningType = LightningOrb.LightningType.Ukulele,
                    damageColorIndex = DamageColorIndex.Item,
                    range = ps.Radius,
                    bouncedObjects = new System.Collections.Generic.List<HealthComponent> { hb.healthComponent },
                };
                var next = orb.PickNextTarget(point);
                if (next) { orb.target = next; OrbManager.instance.AddOrb(orb); Fx.Sound("seven_power_surge", "hit", point); }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }

    [Kind("gun_auto")] public sealed class GunAuto : GunState { }
    [Kind("gun_burst")] public sealed class GunBurst : GunState { }
    [Kind("gun_shotgun")] public sealed class GunShotgun : GunState { }
}
