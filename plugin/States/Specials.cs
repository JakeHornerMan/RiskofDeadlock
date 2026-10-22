using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // The R moves (RoR2's special slot, normal cooldowns). The heroes' Deadlock ultimates are their Q equipment (Ultimates.cs).

    // ---------------- Haze: Phantom Strike (Deadlock item) ----------------
    [Kind("phantom_strike")]
    public sealed class PhantomStrike : DlrState
    {
        HurtBox target; int cuts; float nextCut; bool started;

        public override void OnEnter()
        {
            base.OnEnter();
            target = LockOn(row.Range);
            if (!target)
            {
                // nothing under the crosshair in range: no blink, and R stays ready
                RefundStock();
                return;
            }
            started = true;
            Sound("cast");
            var victim = target.healthComponent.body;
            var from = characterBody.footPosition;
            var toTarget = victim.footPosition - from; toTarget.y = 0;
            if (toTarget.sqrMagnitude < 0.01f) toTarget = characterDirection ? characterDirection.forward : Vector3.forward;
            toTarget.Normalize();
            // just behind the target, facing it
            var behind = victim.footPosition + toTarget * (victim.radius + 1.2f);
            if (Physics.Raycast(behind + Vector3.up * 3f, Vector3.down, out var g, 8f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) behind = g.point;
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, 1f, null, behind + Vector3.up * characterBody.radius * 2f);
            if (isAuthority)
            {
                TeleportHelper.TeleportBody(characterBody, behind, false);
                if (characterMotor) characterMotor.velocity = Vector3.zero;
            }
            if (characterDirection) characterDirection.forward = -toTarget;
            if (anim) anim.Gesture("special_start");
            nextCut = 0.1f;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!started) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (isAuthority && characterMotor) { characterMotor.velocity = Vector3.zero; characterMotor.moveDirection = Vector3.zero; }
            if (cuts < row.Burst && fixedAge >= nextCut)
            {
                cuts++;
                nextCut = fixedAge + row.Interval / attackSpeedStat;
                var victim = target && target.healthComponent && target.healthComponent.alive ? target.healthComponent.body : null;
                if (anim) anim.Gesture("special", 1.6f);
                var at = victim ? victim.corePosition : transform.position + (characterDirection ? characterDirection.forward : transform.forward) * 1.5f;
                Vfx.Play(row.Vfx, "hit", at, cuts);
                Fx.Sound(row.Id, "hit", at);
                if (victim)
                {
                    // each cut sweeps every enemy within radius of the target, not just the target (every player draws
                    // the cuts; the server deals the damage)
                    var swept = Fx.Enemies(characterBody, at, row.Radius);
                    if (!swept.Exists(h => h.healthComponent == target.healthComponent)) swept.Insert(0, target);
                    foreach (var hb in swept)
                    {
                        var b = hb.healthComponent.body;
                        if (!b) continue;
                        if (hb.healthComponent != target.healthComponent) Vfx.Play(row.Vfx, "hit", b.corePosition, cuts);
                        if (!Server) continue;
                        Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition);
                        // each cut adds buffStacks of Haze's Fixation (its 20-stack burst can fire from here)
                        for (int i = 0; i < Mathf.Max(1, (int)row.BuffStacks); i++) Buffs.Stack(characterBody, b, row.Buff, false);
                    }
                }
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Abrams: Haymaker (Deadlock heavy melee) ----------------
    // Locks onto the enemy under the crosshair within range (no target: no punch, and R stays ready), winds up (interval),
    // then flies straight at it at speed m/s, even up to one in the air, and punches along that line, so the fist never
    // lands in the floor. On contact a short hit-stop freezes him, then he recovers for duration seconds.
    [Kind("heavy_melee")]
    public sealed class HeavyMelee : DlrState
    {
        const float HitStop = 0.08f;
        HurtBox target; Vector3 dir, aimPoint; bool started, punched, floating; float windup, rushEnd, punchAt; GameObject glow;

        public override void OnEnter()
        {
            base.OnEnter();
            target = LockOn(row.Range);
            if (!target)
            {
                RefundStock();
                return;
            }
            started = true;
            Sound("cast");
            aimPoint = target.healthComponent.body.corePosition;
            dir = (aimPoint - characterBody.corePosition).normalized;
            Face();
            windup = row.Interval / attackSpeedStat;
            // time to cover the whole lock range, with slack for a target that moves
            rushEnd = windup + 1.5f * row.Range / Mathf.Max(1f, row.Speed);
            if (anim) anim.Gesture("special_start", anim.HasClip("special_start") ? 1.5f : 1f);
            var loc = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>() : null;
            var hand = loc ? loc.FindChild("HandL") : null;
            glow = Vfx.Play(row.Vfx, "cast", hand ? hand.position : characterBody.corePosition, 1f, hand ? hand : transform);
            characterBody.SetAimTimer(2f);
            // no gravity while he winds up and flies at the target
            if (characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
        }

        void Face()
        {
            var flat = dir; flat.y = 0;
            if (flat.sqrMagnitude > 0.01f && characterDirection) characterDirection.forward = flat.normalized;
        }

        void Land()
        {
            if (!floating || !characterMotor) return;
            floating = false;
            var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!started) { if (isAuthority) outer.SetNextStateToMain(); return; }
            var victim = target && target.healthComponent && target.healthComponent.alive ? target.healthComponent.body : null;
            if (!punched)
            {
                if (victim) aimPoint = victim.corePosition;
                var to = aimPoint - characterBody.corePosition;
                if (to.sqrMagnitude > 0.01f) dir = to.normalized;
                Face();
                if (isAuthority && characterMotor)
                {
                    characterMotor.moveDirection = Vector3.zero;
                    if (fixedAge < windup) characterMotor.velocity = Vector3.zero;
                    else
                    {
                        if (characterMotor.isGrounded && dir.y > 0.05f) characterMotor.Motor.ForceUnground();
                        characterMotor.velocity = dir * row.Speed;
                    }
                }
                float reach = characterBody.radius + (victim ? victim.radius : 0f) + 1f;
                if (fixedAge >= windup && (to.magnitude <= reach || fixedAge >= rushEnd)) Punch();
            }
            else
            {
                bool stopped = fixedAge < punchAt + HitStop;
                if (isAuthority && characterMotor)
                {
                    characterMotor.moveDirection = Vector3.zero;
                    if (stopped) characterMotor.velocity = Vector3.zero;
                }
                if (anim) anim.enabled = !stopped;
                if (!stopped) Land();
                if (fixedAge >= punchAt + HitStop + row.Duration && isAuthority) outer.SetNextStateToMain();
            }
        }

        void Punch()
        {
            punched = true;
            punchAt = fixedAge;
            Vfx.Stop(glow, 0.2f);
            if (anim) anim.Gesture("special", 1.4f);
            // the fist lands along the line to the target (up, down or level)
            var at = characterBody.corePosition + dir * (characterBody.radius + 1f + row.Radius * 0.4f);
            Fx.Sound(row.Id, "hit", at);
            Fx.Impact(row.Effect, at, row.Radius, -dir);
            Vfx.Play(row.Vfx, "hit", at, row.Radius, null, at + dir);
            bool wave = row.Extra == "shockwave";
            // the shockwave cone is twice the fist's radius long (range is the lock-on reach)
            float waveLength = row.Radius * 2f;
            if (wave) Vfx.Play(row.Vfx, "wave", at, waveLength, null, at + dir, row.SpreadDeg);
            if (!Server) return;
            var punchedBodies = new HashSet<CharacterBody>();
            void Smash(HurtBox hb)
            {
                var b = hb.healthComponent.body;
                if (!b || !punchedBodies.Add(b)) return;
                Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, b.corePosition);
                Fx.Stun(hb.healthComponent, row.StunDuration);
                Fx.KnockBack(characterBody, hb.healthComponent, dir, row.Knockback);
            }
            // the locked target always takes the fist if it is still in reach
            if (target && target.healthComponent && target.healthComponent.alive && target.healthComponent.body
                && Vector3.Distance(target.healthComponent.body.corePosition, at) <= row.Radius + target.healthComponent.body.radius + 1f) Smash(target);
            foreach (var hb in Fx.Enemies(characterBody, at, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                // only what is in front of him: a punch, not a stomp
                var to = b.corePosition - characterBody.corePosition;
                if (to.sqrMagnitude > 1f && Vector3.Dot(to.normalized, dir) < 0.2f) continue;
                Smash(hb);
            }
            if (!wave) return;
            // the shockwave: every other enemy in a cone along the punch (half-angle spreadDeg) takes the same hit
            float cos = Mathf.Cos(row.SpreadDeg * Mathf.Deg2Rad);
            foreach (var hb in Fx.Enemies(characterBody, at, waveLength))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                var to = b.corePosition - at;
                if (to.sqrMagnitude > 0.25f && Vector3.Dot(to.normalized, dir) < cos) continue;
                Smash(hb);
            }
        }

        public override void OnExit()
        {
            Vfx.Stop(glow, 0.2f);
            Land();
            if (anim) anim.enabled = true;
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    // ---------------- Seven: Storm Cloud (his Deadlock ultimate, as an R flight since the 2026-10-21 redesign) ----------------
    /// <summary>Seven rises launchHeight metres and flies for duration with gravity off, inside a sphere of radius: Jump climbs and
    /// Sprint (Shift by default; Utility too) sinks at speed m/s, he drifts sideways at moveSpeedMult of his move speed, at most reach metres above where he
    /// started. Every interval everything inside takes damagePct and a stack of buff (Static). No gun or skills meanwhile; the
    /// cooldown starts when it ends (cooldownOnEnd). Runs on the Body machine, so RoR2's own jump and walk don't act on him.</summary>
    [Kind("storm_flight")]
    public sealed class StormFlight : DlrState
    {
        const float RiseTime = 0.6f;
        // Deadlock's Storm Cloud take-off (special_start, 1.4 s) plays a little quicker than its own pace, then the hover loops
        const float IntroTime = 1.1f;
        GameObject sphere, hint; int loop; float nextTick, startY, sinkFrom; bool floating, locked, ended;
        readonly List<HurtBox> inside = new List<HurtBox>();

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            loop = Fx.Loop(row.Id, transform.position);
            if (anim) anim.Override("special_start", IntroTime);
            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            startY = transform.position.y;
            Vfx.Play(row.Vfx, "cast", characterBody.footPosition, row.Radius);
            sphere = Vfx.Play(row.Vfx, "loop", characterBody.corePosition, row.Radius, characterBody.coreTransform ? characterBody.coreTransform : transform);
            if (characterMotor)
            {
                characterMotor.Motor.ForceUnground();
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                floating = true;
            }
            if (isAuthority)
            {
                LockSkills(true); locked = true;
                hint = FlightHint.Show(characterBody, row.Duration);
            }
            nextTick = RiseTime;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor)
            {
                characterBody.isSprinting = false;
                characterMotor.moveDirection = Vector3.zero;
                float vy;
                if (fixedAge < RiseTime) vy = row.LaunchHeight / RiseTime;
                else
                {
                    // Jump (Space) up, Sprint (Shift) or Utility down, neither holds him where he is; a ceiling above where he started.
                    // rawSprintInput is the key itself (sprint is a toggle in RoR2, and he can't sprint while flying)
                    bool sink = inputBank && (inputBank.rawSprintInput.down || inputBank.skill3.down);
                    // test mode has no player to press: it sinks for the last 3 s, so the descent is measured
                    if (MeltyTest.Active && fixedAge > row.Duration - 3f) sink = true;
                    int dir = (inputBank && inputBank.jump.down ? 1 : 0) - (sink ? 1 : 0);
                    vy = dir * row.Speed;
                    if (vy > 0f && transform.position.y - startY >= row.Reach) vy = 0f;
                }
                var move = inputBank ? inputBank.moveVector : Vector3.zero;
                var flat = move * moveSpeedStat * row.MoveSpeedMult;
                characterMotor.velocity = new Vector3(flat.x, vy, flat.z);
                // he turns smoothly to where he aims through RoR2's own CharacterDirection, but only while the aim has a clear
                // sideways part: looking nearly straight down at enemies under him, the flat aim swings wildly and he whipped
                // round with it (the test saw 1444 deg in one flight); then he keeps facing the way he was
                var aim = GetAimRay().direction; aim.y = 0f;
                if (characterDirection) characterDirection.moveVector = aim.sqrMagnitude > 0.09f ? aim.normalized : characterDirection.forward;
            }
            if (fixedAge >= IntroTime && anim && fixedAge - Time.fixedDeltaTime < IntroTime) anim.Override("special_loop", row.Duration, true);

            if (fixedAge >= nextTick && fixedAge < row.Duration)
            {
                nextTick += row.Interval;
                // on every machine: a bolt to each of the nearest chainCount enemies inside (drawn) and the zap; on the server the damage and Static
                inside.Clear();
                inside.AddRange(Fx.Enemies(characterBody, characterBody.corePosition, row.Radius, row.ChainCount > 0 ? (int)row.ChainCount : 64));
                foreach (var hb in inside)
                {
                    var v = hb.healthComponent.body;
                    if (Server)
                    {
                        Fx.Hit(characterBody, hb, Coef, row.ProcCoefficient, hb.transform.position);
                        if (row.Buff != "none" && v) Buffs.Stack(characterBody, v, row.Buff, false);
                    }
                    Vfx.Play(row.Vfx, "hit", characterBody.corePosition, row.Radius, null, v ? v.corePosition : hb.transform.position);
                }
                if (inside.Count > 0) Sound("hit");
            }

            if (MeltyTest.Active && isAuthority)
            {
                if (fixedAge > row.Duration - 3f && sinkFrom == 0f) sinkFrom = transform.position.y;
                if (fixedAge >= row.Duration && sinkFrom != 0f) { Log.Info($"storm flight: sank {sinkFrom - transform.position.y:F1} m in 3 s (want about {row.Speed * 3f:F0}, or to the ground)"); sinkFrom = 0f; }
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }

        void End()
        {
            if (ended) return;
            ended = true;
            DeadlockAudio.Stop(loop);
            Vfx.Stop(sphere, 0.4f); sphere = null;
            if (hint) Object.Destroy(hint);
            Sound("end");
            Vfx.Play(row.Vfx, "end", characterBody.corePosition, row.Radius);
            if (floating && characterMotor)
            {
                floating = false;
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp;
            }
            if (locked) { LockSkills(false); locked = false; }
        }

        public override void OnExit()
        {
            End();
            // a soft fall: the landing never hurts; the hover pose blends back into his falling and running clips
            if (!Survivors.NoFallDamage(characterBody) && characterBody.GetComponent<SoftLanding>() == null) characterBody.gameObject.AddComponent<SoftLanding>();
            if (anim) anim.StopOverride();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }

    /// <summary>Keeps fall damage off after Storm Cloud until the hero next touches the ground.</summary>
    public sealed class SoftLanding : MonoBehaviour
    {
        CharacterBody body; float age;
        void Awake() => body = GetComponent<CharacterBody>();
        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            bool down = body && body.characterMotor && body.characterMotor.isGrounded && age > 0.2f;
            if (!body || down || age > 30f)
            {
                if (body) body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
                Destroy(this);
            }
        }
    }

    /// <summary>The flight controls on the flying player's own screen: "[Space] Higher [Shift] Lower" with the Jump and Sprint keys
    /// they really bound (their Rewired player's keyboard maps), "Jump" / "Sprint" on a gamepad, and a bar for the time left.</summary>
    public sealed class FlightHint : MonoBehaviour
    {
        float until, total; string up = "Space", down = "Shift"; GUIStyle box, keys;

        public static GameObject Show(CharacterBody body, float duration)
        {
            var user = LocalUserManager.readOnlyLocalUsersList.FirstOrDefault(u => u != null && u.cachedBody == body);
            if (user == null) return null;
            var go = new GameObject("DlrFlightHint");
            var h = go.AddComponent<FlightHint>();
            h.total = Mathf.Max(0.1f, duration); h.until = Time.time + duration;
            try
            {
                bool pad = user.eventSystem && user.eventSystem.currentInputSource == RoR2.UI.MPEventSystem.InputSource.Gamepad;
                h.up = pad ? "Jump" : Key(user, RewiredConsts.Action.Jump) ?? "Space";
                h.down = pad ? "Sprint" : Key(user, RewiredConsts.Action.Sprint) ?? "Shift";
            }
            catch (System.Exception e) { Log.Warn("flight hint keys: " + e.Message); }
            return go;
        }

        static string Key(LocalUser user, int action)
        {
            var em = user.inputPlayer?.controllers.maps.GetFirstElementMapWithAction(Rewired.ControllerType.Keyboard, action, true);
            return em != null && !string.IsNullOrEmpty(em.elementIdentifierName) ? em.elementIdentifierName : null;
        }

        void Update() { if (Time.time >= until + 0.2f) Destroy(gameObject); }

        void OnGUI()
        {
            if (box == null)
            {
                var bg = new Texture2D(1, 1); bg.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.03f, 0.75f)); bg.Apply();
                box = new GUIStyle(GUI.skin.box) { normal = { background = bg } };
                keys = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true, normal = { textColor = new Color(1f, 0.95f, 0.8f) } };
            }
            float w = 460f, h = 64f;
            var r = new Rect((Screen.width - w) / 2f, Screen.height * 0.72f, w, h);
            GUI.Box(r, GUIContent.none, box);
            GUI.Label(new Rect(r.x, r.y + 4f, w, 40f), $"<color=#FFD633>[{up}]</color> Higher      <color=#FFD633>[{down}]</color> Lower", keys);
            // the time left
            float left = Mathf.Clamp01((until - Time.time) / total);
            var bar = new Rect(r.x + 16f, r.y + h - 14f, (w - 32f) * left, 6f);
            var c = GUI.color; GUI.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = c;
        }
    }

    // ---------------- Infernus: Torment Pulse (Deadlock item) ----------------
    [Kind("aura_pulse")]
    public sealed class AuraPulse : DlrState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("special");
            // on every machine: the rings and sound everywhere, the damage on the server
            var aura = gameObject.GetComponent<PulseAura>() ?? gameObject.AddComponent<PulseAura>();
            aura.Begin(characterBody, row);
            TimedLoop.Run(gameObject, row, row.Duration, Vfx.Play(row.Vfx, "loop", characterBody.footPosition, row.Radius, transform));
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= 0.2f && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;
    }

    /// <summary>Torment Pulse, on every machine: every interval for duration a ring and its sound; on the server everything
    /// within radius takes damagePct and buffStacks of the buff (Afterburn build-up, whose ignition is the usual burn).</summary>
    public sealed class PulseAura : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; float until, next; string hitVfx;

        public void Begin(CharacterBody body, SkillRow r)
        {
            owner = body; row = r;
            until = Time.fixedTime + r.Duration;
            next = Time.fixedTime;
            // the hero's gun draws the build-up (embers, ignition) for the same buff
            var hero = Survivors.HeroOf(body);
            var gun = hero != null ? Sheet.SkillOf(hero, "primary") : null;
            hitVfx = gun != null && gun.Buff == r.Buff ? gun.Vfx : "none";
            enabled = true;
        }

        void FixedUpdate()
        {
            if (!owner || row == null || Time.fixedTime >= until || !owner.healthComponent || !owner.healthComponent.alive) { enabled = false; return; }
            if (Time.fixedTime < next) return;
            next = Time.fixedTime + row.Interval;
            var at = owner.footPosition;
            Vfx.Play(row.Vfx, "hit", at, row.Radius);
            Fx.Sound(row.Id, "hit", at);
            if (!NetworkServer.active) return;
            foreach (var hb in Fx.Enemies(owner, owner.corePosition, row.Radius))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, b.corePosition);
                bool fired = false;
                for (int k = 0; k < Mathf.Max(1, (int)row.BuffStacks); k++) fired |= Buffs.Stack(owner, b, row.Buff, false);
                // the build-up depends on the stacks, which only the server knows: every player sees it through the relay
                Relay.Vfx(hitVfx, fired ? "trigger" : "hit", b.corePosition, 1f, b.gameObject);
            }
        }
    }

    /// <summary>Keeps a skill's loop sound and visual going on the body for a while after its (short) state has ended.</summary>
    public sealed class TimedLoop : MonoBehaviour
    {
        GameObject vfx; int sound; string skill; float until;

        public static void Run(GameObject on, SkillRow row, float duration, GameObject vfx)
        {
            foreach (var old in on.GetComponents<TimedLoop>()) if (old.skill == row.Id) old.Finish();
            var t = on.AddComponent<TimedLoop>();
            t.vfx = vfx; t.skill = row.Id; t.until = Time.time + duration;
            t.sound = Fx.Loop(row.Id, on.transform.position);
        }

        void Update() { if (Time.time >= until) Finish(); }

        void Finish()
        {
            if (!this) return;
            DeadlockAudio.Stop(sound); sound = 0;
            Vfx.Stop(vfx, 1f); vfx = null;
            Fx.Sound(skill, "end", transform.position);
            Destroy(this);
        }

        void OnDestroy() { DeadlockAudio.Stop(sound); Vfx.Stop(vfx, 1f); }
    }
}
