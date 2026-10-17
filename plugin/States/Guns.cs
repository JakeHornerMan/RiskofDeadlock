using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    /// <summary>Deadlock gun on M1: holds while the button is held, fires at the sheet's cycle time (scaled by attack speed),
    /// uses the hero's magazine and reload, and breaks Smoke Bomb. The shots are the owner's (RoR2 bullets fire where the
    /// hero has authority); each one is relayed so the other players hear and see it. The on-hit passive (Fixation, Static,
    /// Afterburn) and Power Surge's arcs run on the server, from the damage the bullets deal (<see cref="GunHits"/>).</summary>
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
                // FireOne ends the burst (burstLeft = 0) when the magazine runs dry mid-burst; never count below zero,
                // or the gun would never fire, reload or let go again
                burstLeft = Mathf.Max(0, burstLeft - 1);
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
            Relay.Reload(characterBody);
        }

        /// <summary>What another player's machine plays for one of this hero's shots: sound, flash, muzzle visual, recoil.</summary>
        public static void ShotFx(CharacterBody body, bool left)
        {
            var row = Survivors.RowFor(body.skillLocator ? body.skillLocator.primary : null);
            if (row == null) return;
            var anim = DeadlockAnimator.Of(body);
            if (anim) anim.Recoil(row.Pellets > 1 ? 9f : 3f);
            Fx.Sound(row.Id, "cast", body.transform.position);
            Fx.MuzzleFlash(body, left);
            var muzzle = Fx.Muzzle(body, left);
            if (muzzle) Vfx.Play(row.Vfx, "muzzle", muzzle.position, 1f, muzzle);
        }

        public static void BreakSmoke(CharacterBody body)
        {
            if (!NetworkServer.active || !body || Buffs.Count(body, "smoke") <= 0) return;
            Buffs.Clear(body, "smoke");
            if (body.HasBuff(RoR2Content.Buffs.Cloak)) body.RemoveBuff(RoR2Content.Buffs.Cloak);
            Relay.Sound("haze_smoke_bomb", "end", body.transform.position);
        }

        public static void ReloadFx(CharacterBody body)
        {
            var row = Survivors.RowFor(body.skillLocator ? body.skillLocator.primary : null);
            if (row == null) return;
            var anim = DeadlockAnimator.Of(body);
            if (anim) anim.Gesture("reload", 1f);
            Fx.Sound(row.Id, "reload", body.transform.position);
        }

        void FireOne()
        {
            if (rt != null)
            {
                if (rt.Ammo <= 0) { StartReload(); burstLeft = 0; return; }
                rt.Ammo--;
                rt.AltMuzzle = !rt.AltMuzzle;
            }
            // firing reveals Haze from Smoke Bomb (the server breaks it: here for the host's hero, from the relayed shot for a
            // joined player's)
            if (Server) BreakSmoke(characterBody);
            bool left = rt != null && rt.AltMuzzle && row.Hero == "haze";
            // the sound, the flash on the end of the gun (the sheet's muzzle bone), the hero's own per-shot muzzle visual
            // (Infernus' smoke) and the recoil; the other players get the same through the relay
            ShotFx(characterBody, left);
            Relay.Shot(characterBody, left);
            var aim = GetAimRay();
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
                };
                ba.Fire();
            }
            MeltyTest.Report("gun-fires", true, characterBody.baseNameToken);
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }

    /// <summary>Server side of the guns: a bullet from a hero's gun (damage source Primary, dealt by the hero itself) stacks
    /// the gun's buff (Fixation, Static, Afterburn build-up; its trigger fires at full stacks) and, while Power Surge runs,
    /// sends Tesla arcs from the target once per burst. Works the same for the host's hero and a joined player's, whose
    /// bullets RoR2 resolves on their PC and sends here as damage.</summary>
    public static class GunHits
    {
        public static void Init() => GlobalEventManager.onServerDamageDealt += OnDamage;

        static void OnDamage(DamageReport r)
        {
            var info = r?.damageInfo;
            var owner = r?.attackerBody;
            var victim = r?.victimBody;
            if (info == null || !owner || !victim || info.damageType.damageSource != DamageSource.Primary || info.inflictor != owner.gameObject) return;
            if (victim.teamComponent.teamIndex == owner.teamComponent.teamIndex) return;
            var row = Survivors.RowFor(owner.skillLocator ? owner.skillLocator.primary : null);
            if (row == null || !row.Kind.StartsWith("gun_")) return;
            if (row.Buff != "none")
            {
                bool fired = Buffs.Stack(owner, victim, row.Buff, info.crit);
                Relay.Vfx(row.Vfx, fired ? "trigger" : "hit", info.position, 1f, victim.gameObject);
            }
            // Power Surge: once per burst (the gun's interval), Tesla arcs from the target to the enemies around it
            var surge = PowerSurgeCast.Active(owner);
            var rt = owner.GetComponent<HeroRuntime>();
            if (surge == null || rt == null || Time.fixedTime < rt.NextSurge) return;
            rt.NextSurge = Time.fixedTime + 0.9f * row.Interval / Mathf.Max(0.1f, owner.attackSpeed);
            PowerSurgeCast.Arc(owner, victim, surge, row.Buff);
        }
    }

    [Kind("gun_auto")] public sealed class GunAuto : GunState { }
    [Kind("gun_burst")] public sealed class GunBurst : GunState { }
    [Kind("gun_shotgun")] public sealed class GunShotgun : GunState { }
}
