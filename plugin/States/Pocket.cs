using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ---------------- Pocket: Barrage (M2) ----------------
    /// <summary>Pocket opens the briefcase and channels for duration on the Weapon machine, so walking, his air dash (Body) and
    /// Flying Cloak (Body) carry on under it. chainCount bursts, the first after minDuration and then one every interval, each a
    /// lobbed spirit burst (<see cref="DlrBarrageBurst"/>) to the point under the crosshair, scattered up to reach metres. A fresh cast
    /// starts the damage amp (buff) over; each burst that hits adds a stack. M1 or M2 again ends it (before the first burst with the
    /// charge back), so does the Satchel. Cast in the air, he floats for it. The scatter comes from a seed the owner picks, so every
    /// machine throws the same bursts.</summary>
    [Kind("barrage")]
    public sealed class Barrage : DlrState
    {
        int fired, seed;
        bool floating;
        GameObject aura, marker;

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority) seed = Random.Range(1, int.MaxValue);
            Sound("cast");
            if (anim) anim.Gesture("secondary");
            characterBody.SetAimTimer(row.Duration + 1f);
            aura = Vfx.Play(row.Vfx, "loop", characterBody.corePosition, 1f, PocketParts.Case(characterBody));
            if (isAuthority) marker = Vfx.Play(row.Vfx, "aim", PocketParts.AimPoint(characterBody, row.Range), row.Radius);
            if (Server) Buffs.Clear(characterBody, row.Buff);
            if (characterMotor && !characterMotor.isGrounded)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
        }

        bool Bot => MeltyTest.Active || DpsBench.Active;

        public override void Update()
        {
            base.Update();
            if (marker) marker.transform.position = PocketParts.AimPoint(characterBody, row.Range);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            characterBody.SetAimTimer(1f);
            if (isAuthority && floating && characterMotor && characterMotor.velocity.y < 0f)
            {
                var v = characterMotor.velocity; v.y = 0f; characterMotor.velocity = v;
            }
            if (fired < row.ChainCount && fixedAge >= row.MinDuration + fired * row.Interval) Fire();
            if (!isAuthority) return;
            // shooting or M2 again ends it; before the first burst the charge comes back (Deadlock)
            if (!Bot && inputBank && (inputBank.skill1.down || inputBank.skill2.justPressed && fixedAge > 0.2f))
            {
                if (fired == 0) RefundStock();
                outer.SetNextStateToMain();
                return;
            }
            if (fixedAge >= row.Duration) outer.SetNextStateToMain();
        }

        void Fire()
        {
            var rng = new System.Random(seed + fired * 7919);
            fired++;
            var from = PocketParts.CaseMouth(characterBody);
            float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Sqrt((float)rng.NextDouble()) * row.Reach;
            var to = PocketParts.Floor(PocketParts.AimPoint(characterBody, row.Range) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r);
            DlrBarrageBurst.Launch(characterBody, row, from, to, Server);
            Sound("fire");
            if (anim) anim.Gesture("secondary_air");
            Vfx.Play(row.Vfx, "muzzle", from, 1f);
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(seed); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); seed = reader.ReadInt32(); }

        public override void OnExit()
        {
            Vfx.Stop(aura, 0.4f);
            Vfx.Stop(marker, 0.1f);
            if (floating && characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
            }
            floating = false;
            base.OnExit();
        }
    }

    // ---------------- Pocket: Flying Cloak (Shift) ----------------
    /// <summary>The throw (minDuration, on the Body machine so Barrage runs on; he keeps walking): a living cloak
    /// (<see cref="DlrCloak"/>) flies along the aim on every machine, the server's copy dealing the damage. While it lives, Shift
    /// again teleports him to it (<see cref="PocketCloakRecast"/>, <see cref="CloakPort"/>); the key's cooldown waits until it ends.</summary>
    [Kind("flying_cloak")]
    public sealed class FlyingCloak : DlrState
    {
        bool thrown;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("utility");
            characterBody.SetAimTimer(1f);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            PocketParts.KeepWalking(characterBody, characterMotor, characterDirection, inputBank);
            if (!thrown && fixedAge >= row.MinDuration * 0.6f) Throw();
            if (isAuthority && fixedAge >= row.MinDuration) outer.SetNextStateToMain();
        }

        void Throw()
        {
            thrown = true;
            var aim = GetAimRay();
            var dir = ThrowDir.normalized;
            DlrCloak.Launch(characterBody, row, aim.origin + dir * 1.2f, dir, Server);
            if (isAuthority)
            {
                var rc = characterBody.GetComponent<PocketCloakRecast>() ?? characterBody.gameObject.AddComponent<PocketCloakRecast>();
                rc.Begin(row);
            }
        }

        public override void OnExit()
        {
            if (!thrown && row != null) Throw();
            base.OnExit();
        }
    }

    /// <summary>Flying Cloak's recast: Pocket appears where his cloak is (in the air if it is) and the cloak ends. The owner reads
    /// where it is and sends it with the state; every machine ends its own copy and plays the puffs.</summary>
    public sealed class CloakPort : DlrState
    {
        Vector3 from, to;

        public override void OnEnter()
        {
            base.OnEnter();
            from = characterBody.corePosition;
            var cloak = DlrCloak.Of(characterBody);
            if (isAuthority) to = cloak ? cloak.transform.position : from;
            if (cloak) cloak.End(false);
            Vfx.Play(row.Vfx, "port", from, 1f);
            Vfx.Play(row.Vfx, "port", to, 1f);
            Fx.Sound(row.Id, "reload", to);
            if (isAuthority)
            {
                var feet = to - (characterBody.corePosition - characterBody.footPosition);
                TeleportHelper.TeleportBody(characterBody, feet, false);
                if (characterMotor) { characterMotor.velocity = Vector3.zero; characterMotor.Motor.ForceUnground(); }
            }
            MeltyTest.Report("cloak-teleport", true, characterBody.baseNameToken);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge > 0.05f) outer.SetNextStateToMain();
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(to); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); to = reader.ReadVector3(); }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    /// <summary>Flying Cloak's recast window, on the owner: while his cloak flies, Shift again teleports him to it (the key's own
    /// cooldown is held at its start meanwhile, so the 8 s begin when the cloak ends). Test mode and the bench teleport after a moment.</summary>
    public sealed class PocketCloakRecast : MonoBehaviour
    {
        SkillRow row;
        float since;
        CharacterBody body;

        public void Begin(SkillRow r) { body = GetComponent<CharacterBody>(); row = r; since = Time.fixedTime; enabled = true; }

        void FixedUpdate()
        {
            if (!body || row == null || !body.hasEffectiveAuthority) return;
            var cloak = DlrCloak.Of(body);
            if (!cloak) { row = null; return; }
            var gs = body.skillLocator ? body.skillLocator.utility : null;
            if (gs) gs.rechargeStopwatch = 0f;
            bool pressed = body.inputBank && body.inputBank.skill3.justPressed && Time.fixedTime > since + 0.1f;
            if ((MeltyTest.Active || DpsBench.Active) && Time.fixedTime > since + row.Duration * 0.35f) pressed = true;
            var esm = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
            if (pressed && esm && esm.state is GenericCharacterMain)
            {
                esm.SetNextState(new CloakPort { RowOverride = row });
                row = null;
            }
        }
    }

    // ---------------- Pocket: Enchanter's Satchel (R) ----------------
    /// <summary>After minDuration (his hop into it) Pocket is inside his suitcase for up to duration where he stood (it floats there,
    /// in the air too): hidden, untouchable (HiddenInvincibility) and cloaked (monsters lose him), every skill locked. Every interval
    /// each enemy within radius takes splashPct % a second (proc tickProc) and he heals healFraction of it (server); purple tendrils
    /// reach to them on every machine. Any key but movement ends it early; then the lid bursts open: damagePct to everything within
    /// radius. Going in ends Barrage and finishes the gun's reload (his Unpredictable passive). Cooldown from the burst.</summary>
    [Kind("satchel")]
    public sealed class Satchel : DlrState
    {
        bool inside, burst, locked, floating, hidden;
        float nextTick, nextDrain;
        int loop;
        Vector3 spot;
        GameObject glow, tethers, caseRoot;
        CharacterModel model;
        readonly List<Vector3> tetherTo = new List<Vector3>();

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            spot = characterBody.corePosition;
            if (anim) anim.Override("special_start", row.MinDuration + 0.1f);
            var weapon = EntityStateMachine.FindByCustomName(gameObject, "Weapon");
            if (isAuthority && weapon && weapon.state is Barrage) weapon.SetNextStateToMain();
            var gun = skillLocator ? Survivors.RowFor(skillLocator.primary) : null;
            if (rt != null && gun != null) rt.FinishReload(gun);
            if (characterMotor)
            {
                if (isAuthority) characterMotor.velocity = Vector3.zero;
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
            if (isAuthority) { LockSkills(true); locked = true; }
            model = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            if (Server) characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, row.MinDuration + row.Duration + 0.2f);
        }

        bool Bot => MeltyTest.Active || DpsBench.Active;

        void GoIn()
        {
            inside = true;
            nextTick = fixedAge + row.Interval;
            if (model) { model.invisibilityCount++; hidden = true; }
            if (Server) characterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, row.Duration + 0.3f);
            loop = Fx.Loop(row.Id, spot);
            Vfx.Play(row.Vfx, "cast", spot, 1f);
            caseRoot = PocketParts.CaseProp(row.Extra, spot, characterDirection ? characterDirection.forward : transform.forward, 1.6f, true);
            glow = Vfx.Play(row.Vfx, "loop", spot, row.Radius, caseRoot ? caseRoot.transform : null);
            tethers = Vfx.Play(row.Vfx, "tether", spot, 1f);
            MeltyTest.Report("satchel-in", true, characterBody.baseNameToken);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterMotor.moveDirection = Vector3.zero;
                characterMotor.velocity = Vector3.zero;
                characterBody.isSprinting = false;
            }
            if (!inside && fixedAge >= row.MinDuration) GoIn();
            if (inside)
            {
                // the tendrils, on every machine: every enemy the case drinks from
                tetherTo.Clear();
                var enemies = Fx.Enemies(characterBody, spot, row.Radius);
                foreach (var hb in enemies) tetherTo.Add(hb.healthComponent.body ? hb.healthComponent.body.corePosition : hb.transform.position);
                PocketParts.SetTethers(tethers, spot, tetherTo);
                if (fixedAge >= nextDrain)
                {
                    nextDrain = fixedAge + row.Interval;
                    foreach (var p in tetherTo) Vfx.Play(row.Vfx, "drain", p, 1f, null, spot);
                    if (tetherTo.Count > 0) Sound("hit");
                }
                if (Server && fixedAge >= nextTick)
                {
                    nextTick += row.Interval;
                    float coef = row.SplashPct / 100f * row.Interval;
                    foreach (var hb in enemies)
                        Fx.Hit(characterBody, hb, coef, row.TickProc, hb.transform.position,
                            new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Special });
                    if (enemies.Count > 0 && characterBody.healthComponent)
                        characterBody.healthComponent.Heal(characterBody.damage * coef * enemies.Count * row.HealFraction, default);
                }
            }
            if (!isAuthority) return;
            bool anyKey = !Bot && inputBank && fixedAge >= row.MinDuration + 0.1f &&
                (inputBank.skill1.justPressed || inputBank.skill2.justPressed || inputBank.skill3.justPressed || inputBank.skill4.justPressed ||
                 inputBank.jump.justPressed || inputBank.activateEquipment.justPressed);
            if (anyKey || fixedAge >= row.MinDuration + row.Duration) outer.SetNextStateToMain();
        }

        void Burst()
        {
            burst = true;
            DeadlockAudio.Stop(loop);
            Vfx.Stop(glow, 0.3f); glow = null;
            Vfx.Stop(tethers, 0.1f); tethers = null;
            if (caseRoot) PocketParts.Pop(caseRoot);
            caseRoot = null;
            Vfx.Play(row.Vfx, "end", spot, row.Radius);
            Sound("end");
            if (hidden && model) model.invisibilityCount--;
            hidden = false;
            if (anim) anim.Override("special", 0.5f);
            if (!Server) return;
            characterBody.ClearTimedBuffs(RoR2Content.Buffs.HiddenInvincibility);
            Fx.Blast(characterBody, spot, row.Radius, Coef, row.ProcCoefficient,
                new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Special });
        }

        public override void OnExit()
        {
            if (row != null && !burst) Burst();
            if (floating && characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
            }
            floating = false;
            if (locked) LockSkills(false);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    // ---------------- Pocket's item: Frog Form (Q, any survivor) ----------------
    /// <summary>The opening (minDuration): the carrier vanishes (hidden, untouchable, held still) while the suitcase is thrown
    /// (Pocket) or drops from the sky beside them (anyone else), grows huge and a giant frog rises from it opening its mouth
    /// (<see cref="FrogOpening"/>). Then the carrier, hidden behind Deadlock's frog at their size, hops every interval for duration:
    /// range metres along the held keys (camera-relative; a short hop with none), speed metres high; Space makes the next a high leap
    /// (launchHeight up, reach far, once every floatTime). Each landing (seen by the server from how the body moves, so a joined
    /// player's frog counts too) deals damagePct within radius, a high leap splashPct within nearRadius, pushing non-bosses
    /// knockback m/s. buff (+200 armor) and no fall damage throughout; no moves; Q again ends it. Keeps the CharacterMotor (Goo
    /// Ball's way), so co-op and the stage's collisions work as usual.</summary>
    [Kind("ult_frog")]
    public sealed class FrogForm : UltState
    {
        bool hidden, locked, frozen, hadNoFall, hopping, leapQueued, sAir;
        float nextHop, nextLeap, hopUntil, sTake, sTop, sAirSince, airControl;
        Vector3 hopVel, caseAt;
        GameObject frog;
        FrogOpening opening;
        CharacterModel model;
        Animation frogAnim;
        string clipSit, clipHop;

        float Size
        {
            get
            {
                var cap = characterBody.GetComponent<CapsuleCollider>();
                return Mathf.Max(1.6f, (cap ? cap.height : 2f) * 1.35f);
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            model = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            hadNoFall = (characterBody.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0;
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            if (Server)
            {
                characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, row.MinDuration + 0.2f);
                Buffs.Clear(characterBody, row.Buff);
                Buffs.AddTimed(characterBody, row.Buff, row.MinDuration + row.Duration + 0.5f);
            }
            if (isAuthority) { LockSkills(true); locked = true; }
            // Pocket throws the case from his hand; anyone else gets it dropped from the sky beside them
            bool thrower = Survivors.HeroOf(characterBody) == row.Hero;
            var fwd = ThrowDir; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
            fwd.Normalize();
            var side = Vector3.Cross(Vector3.up, fwd);
            caseAt = PocketParts.Floor(characterBody.footPosition + (thrower ? fwd * 6f : fwd * 2.5f + side * 2.5f) + Vector3.up * 1.5f);
            if (thrower && anim) anim.Override("ult_start", 0.5f);
            var caseRow = Sheet.SkillsOf(row.Hero).FirstOrDefault(r => r.Kind == "satchel");
            opening = FrogOpening.Begin(row, caseRow != null ? caseRow.Extra : "none", thrower ? PocketParts.CaseMouth(characterBody) : caseAt + Vector3.up * 25f, caseAt, fwd, row.MinDuration, Size);
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, Size);
            // hidden as soon as the throw leaves his hand
            Hide(true);
            if (characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                frozen = true;
                if (isAuthority) { characterMotor.velocity = Vector3.zero; airControl = characterMotor.airControl; }
            }
        }

        void Hide(bool on)
        {
            if (!model || hidden == on) return;
            hidden = on;
            model.invisibilityCount += on ? 1 : -1;
        }

        bool Bot => MeltyTest.Active || DpsBench.Active;

        void StartHopping()
        {
            hopping = true;
            if (frozen && characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
                frozen = false;
            }
            var built = PropModels.Build(row.Extra);
            frog = built != null ? built.Root : PocketParts.FrogStandIn();
            frog.transform.localScale = Vector3.one * PocketParts.FrogScale(Size);
            frogAnim = built != null ? built.Anim : null;
            var clips = Sheet.Effect(row.Extra)?.Clips ?? new string[0];
            clipSit = clips.Length > 0 && clips[0] != "none" ? clips[0] : null;
            clipHop = clips.Length > 1 ? clips[1] : null;
            if (frogAnim && clipSit != null && frogAnim[clipSit]) { frogAnim[clipSit].wrapMode = WrapMode.Loop; frogAnim.Play(clipSit); }
            frog.transform.position = characterBody.footPosition;
            if (isAuthority && characterMotor)
            {
                // out of the giant frog's mouth: up at the case and the first hop forward
                var feet = caseAt + Vector3.up * 0.6f;
                TeleportHelper.TeleportBody(characterBody, feet, false);
                characterMotor.airControl = 0f;
                characterMotor.onMovementHit += OnWall;
                Hop(true);
            }
            Vfx.Play(row.Vfx, "cast", caseAt + Vector3.up * Size * 0.5f, Size);
        }

        public override void Update()
        {
            base.Update();
            if (!frog) return;
            frog.transform.position = characterBody.footPosition;
            var face = isAuthority ? hopVel : characterBody.characterDirection ? characterBody.characterDirection.forward : transform.forward;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f) frog.transform.rotation = Quaternion.Slerp(frog.transform.rotation, Quaternion.LookRotation(face.normalized), Mathf.Min(1f, Time.deltaTime * 12f));
            if (frogAnim)
            {
                bool air = !PocketParts.Grounded(characterBody);
                var want = air && clipHop != null ? clipHop : clipSit;
                if (want != null && frogAnim[want] && !frogAnim.IsPlaying(want))
                {
                    frogAnim[want].wrapMode = want == clipHop ? WrapMode.ClampForever : WrapMode.Loop;
                    frogAnim.CrossFade(want, 0.08f);
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!hopping)
            {
                if (isAuthority && characterMotor) { characterMotor.velocity = Vector3.zero; characterMotor.moveDirection = Vector3.zero; }
                if (fixedAge >= row.MinDuration) StartHopping();
                return;
            }
            if (isAuthority && characterMotor)
            {
                characterBody.isSprinting = false;
                characterMotor.moveDirection = Vector3.zero;
                if (inputBank && inputBank.jump.justPressed || Bot && fixedAge >= nextLeap && Mathf.Repeat(fixedAge, 3f) < Time.fixedDeltaTime * 2f) leapQueued = true;
                bool grounded = characterMotor.isGrounded;
                if (grounded)
                {
                    var v = characterMotor.velocity; characterMotor.velocity = new Vector3(v.x * 0.5f, v.y, v.z * 0.5f);
                    if (fixedAge >= nextHop) Hop(false);
                }
                else if (fixedAge < hopUntil)
                {
                    // the hop carries on at its own speed for its flight time only (on a steep slope the motor never reads grounded:
                    // past that it slides or falls as the motor lets it)
                    var v = characterMotor.velocity; characterMotor.velocity = new Vector3(hopVel.x, v.y, hopVel.z);
                }
                if (characterDirection && hopVel.sqrMagnitude > 0.01f) characterDirection.forward = new Vector3(hopVel.x, 0f, hopVel.z).normalized;
                bool again = !DpsBench.Active && !MeltyTest.Active && inputBank && inputBank.activateEquipment.justPressed && fixedAge > row.MinDuration + 0.5f;
                if (fixedAge >= row.MinDuration + row.Duration || again) { outer.SetNextStateToMain(); return; }
            }
            if (Server) ServerLandings();
        }

        /// <summary>Owner: one hop along the held keys (the aim with none), a high leap when Space was pressed and it is ready.</summary>
        void Hop(bool first)
        {
            if (!characterMotor) return;
            var dir = inputBank ? inputBank.moveVector : Vector3.zero; dir.y = 0f;
            // test mode and the bench steer onto the nearest enemy, never past it (a player stays on the pack the same way)
            float botMax = float.MaxValue;
            if (Bot)
            {
                var near = Fx.Enemies(characterBody, characterBody.corePosition, 40f, 1).FirstOrDefault();
                var to = near ? near.transform.position - characterBody.corePosition : Vector3.zero;
                to.y = 0f; dir = to.sqrMagnitude > 1f ? to.normalized : Vector3.zero;
                botMax = to.magnitude;
            }
            bool steer = dir.sqrMagnitude > 0.04f || first;
            if (!steer || first && !Bot) { dir = GetAimRay().direction; dir.y = 0f; }
            dir = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
            bool high = leapQueued && fixedAge >= nextLeap;
            if (high) { leapQueued = false; nextLeap = fixedAge + row.FloatTime; }
            float dist = Mathf.Min(botMax, high ? row.Reach : steer ? row.Range : row.Range * 0.15f);
            float height = high ? row.LaunchHeight : steer ? row.Speed : row.Speed * 0.6f;
            float g = Mathf.Max(1f, -Physics.gravity.y);
            float vy = Mathf.Sqrt(2f * g * height), t = 2f * vy / g;
            hopVel = dir * (dist / t);
            characterMotor.Motor.ForceUnground();
            characterMotor.velocity = hopVel + Vector3.up * vy;
            nextHop = fixedAge + row.Interval;
            hopUntil = fixedAge + t + 0.15f;
            if (high) MeltyTest.Report("frog-leap", true, characterBody.baseNameToken);
        }

        /// <summary>The owner hopped into a wall: the hop stops there and the frog drops straight down.</summary>
        void OnWall(ref CharacterMotor.MovementHitInfo info)
        {
            if (Mathf.Abs(info.hitNormal.y) < 0.5f) hopVel = Vector3.zero;
        }

        /// <summary>The server's landings, from how the body really moves (a joined player's frog is moved on their PC): in the
        /// air a moment, then on the ground again. A landing from high enough above the take-off is a high leap.</summary>
        void ServerLandings()
        {
            bool grounded = PocketParts.Grounded(characterBody);
            float y = characterBody.footPosition.y;
            if (!grounded)
            {
                if (!sAir) { sAir = true; sAirSince = fixedAge; sTake = y; sTop = y; }
                sTop = Mathf.Max(sTop, y);
                return;
            }
            if (!sAir) return;
            sAir = false;
            if (fixedAge - sAirSince < 0.15f) return;
            bool high = sTop - sTake > (row.Speed + row.LaunchHeight) * 0.5f;
            Slam(characterBody.footPosition, high);
        }

        void Slam(Vector3 at, bool high)
        {
            float radius = high ? row.NearRadius : row.Radius;
            Relay.Vfx(row.Vfx, "slam", at, radius, null, null, high ? 1f : 0f);
            Relay.Sound(row.Id, "hit", at);
            var type = new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Equipment };
            Fx.Blast(characterBody, at + Vector3.up * 0.5f, radius, (high ? row.SplashPct : row.DamagePct) / 100f, row.ProcCoefficient, type);
            foreach (var hb in Fx.Enemies(characterBody, at, radius))
            {
                var b = hb.healthComponent.body;
                if (b && !b.isChampion) Fx.KnockBack(characterBody, hb.healthComponent, b.corePosition - at, row.Knockback);
            }
            MeltyTest.Report("frog-landing", true, characterBody.baseNameToken);
        }

        public override void OnExit()
        {
            var at = characterBody.corePosition;
            Vfx.Play(row.Vfx, "end", at, Size);
            Sound("end");
            if (frog) Object.Destroy(frog);
            if (opening) opening.Finish();
            Hide(false);
            if (frozen && characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
            }
            frozen = false;
            if (isAuthority && characterMotor && hopping)
            {
                characterMotor.airControl = airControl;
                characterMotor.onMovementHit -= OnWall;
            }
            if (!hadNoFall && !Survivors.NoFallDamage(characterBody))
            {
                characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
                // popped back in the air: no fall damage from the drop
                if (characterBody.GetComponent<SoftLanding>() == null) characterBody.gameObject.AddComponent<SoftLanding>();
            }
            if (locked) LockSkills(false);
            if (Server) { Buffs.Clear(characterBody, row.Buff); characterBody.ClearTimedBuffs(RoR2Content.Buffs.HiddenInvincibility); }
            base.OnExit();
        }
    }
}
