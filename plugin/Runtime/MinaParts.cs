using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Mina's two passives, on her body on every machine (added by HeroRuntime when the hero row has an umbrella,
    /// floatFallSpeed > 0, or her gun's buff counts per enemy, refreshAll).
    /// Love Bites (owner): every New Money bullet that lands counts one bite on that enemy (the count starts over the buff's
    /// duration after her last hit on it, as the debuff the server puts on the enemy does); every jumpEvery bites on one enemy she
    /// gets a jump back (2 at most, one per trip in the air), and every skill row's hitsPerRefund bites take its refundSeconds off
    /// that skill's cooldown (Rake 3 s per 10, Nox Nostra 10 s per 20). Her own game counts them, so the rewards land at once.
    /// The umbrella (owner): Jump held floatHold seconds in the air, or pressed with no jump left, opens it once she is falling:
    /// she drifts down at floatFallSpeed with floatAirControl times the air control and takes no fall damage; letting go or landing
    /// closes it. Every machine shows it (the glide clip with the umbrella held over her head, its sounds, a soft glow, the skirt
    /// fluttering); the other players' machines read it from her slow, steady fall.</summary>
    public sealed class MinaRuntime : MonoBehaviour
    {
        sealed class Tally { public int N; public float Last; }

        CharacterBody body;
        HeroRow hero;
        BuffRow bites;
        string gunId;
        readonly Dictionary<HealthComponent, Tally> tallies = new Dictionary<HealthComponent, Tally>();

        /// <summary>A stored jump from Love Bites (0 or 1), used by a Jump press in the air with every other jump spent.</summary>
        public int Bonus { get; private set; }
        bool gainedThisTrip;
        /// <summary>Love Bites on an enemy as Mina's own game counts them (the test reads it).</summary>
        public int BitesOn(HealthComponent hc) => hc && tallies.TryGetValue(hc, out var t) && Time.time - t.Last <= Window ? t.N : 0;
        float Window => bites != null && bites.Duration > 0f ? bites.Duration : 6f;

        /// <summary>The gun row's buff counts per enemy (refreshAll): its bullets count Love Bites.</summary>
        public static bool Bites(SkillRow gun) => gun != null && (Sheet.Buff(gun.Buff)?.RefreshAll ?? false);

        /// <summary>Whether a hero gets this component.</summary>
        public static bool Wanted(HeroRow h)
        {
            if (h == null) return false;
            if (h.FloatFallSpeed > 0f) return true;
            foreach (var s in Sheet.SkillsOf(h.Id)) if (s.Slot == "primary" && Bites(s)) return true;
            return false;
        }

        void Awake() => body = GetComponent<CharacterBody>();

        void Start()
        {
            hero = Sheet.Hero(Survivors.HeroOf(body));
            foreach (var s in Sheet.SkillsOf(hero?.Id))
                if (s.Slot == "primary") { gunId = s.Id; bites = Bites(s) ? Sheet.Buff(s.Buff) : null; }
        }

        // ---------------------------------------------------------------- Love Bites (owner)

        /// <summary>One of her bullets landed on hc (owner side, from the bullet's hit callback).</summary>
        public void Bite(HealthComponent hc)
        {
            if (!hc || bites == null || !body || !body.hasEffectiveAuthority) return;
            float now = Time.time;
            if (!tallies.TryGetValue(hc, out var t)) tallies[hc] = t = new Tally();
            if (now - t.Last > Window) t.N = 0;
            t.N++; t.Last = now;
            MeltyTest.Report("love-bites", true, body.baseNameToken);
            var at = hc.body ? hc.body.corePosition : hc.transform.position;
            bool milestone = false;
            if (bites.JumpEvery > 0 && t.N % (int)bites.JumpEvery == 0) { GainJump(); milestone = true; }
            var sl = body.skillLocator;
            if (sl)
                foreach (var gs in new[] { sl.secondary, sl.utility, sl.special })
                {
                    var row = Survivors.RowFor(gs);
                    if (row == null || row.HitsPerRefund <= 0 || t.N % (int)row.HitsPerRefund != 0) continue;
                    milestone = true;
                    if (gs.stock >= gs.maxStock) continue;
                    // never banks below zero: a cut bigger than what is left only makes it ready
                    gs.RunRecharge(Mathf.Min(row.RefundSeconds, gs.cooldownRemaining + 0.01f));
                    MeltyTest.Report("love-bites-cut", true, body.baseNameToken);
                    if (MeltyTest.Active) Log.Info($"love bites: {t.N} on {hc.name}, {row.Name} {row.RefundSeconds:F0} s sooner (now {gs.cooldownRemaining:F1} s)");
                }
            if (milestone)
            {
                // Deadlock's bite mark pops over the enemy and red motes fly to Mina
                Vfx.Play("mina_bites", "trigger", at, 1f, null, body.corePosition);
                Fx.Sound(gunId + "/bites", "hit", at);
            }
            // forget enemies long gone
            if (tallies.Count > 64)
            {
                var old = new List<HealthComponent>();
                foreach (var kv in tallies) if (!kv.Key || now - kv.Value.Last > Window) old.Add(kv.Key);
                foreach (var k in old) tallies.Remove(k);
            }
        }

        /// <summary>A jump from Love Bites: on the ground one is stored (at most one); in the air only one per trip, given straight
        /// back if she has spent a jump, otherwise stored.</summary>
        void GainJump()
        {
            var m = body.characterMotor;
            if (!m) return;
            bool got = false;
            if (m.isGrounded) { if (Bonus == 0) { Bonus = 1; got = true; } }
            else if (!gainedThisTrip)
            {
                gainedThisTrip = true;
                if (m.jumpCount > 0) { m.jumpCount--; got = true; }
                else if (Bonus == 0) { Bonus = 1; got = true; }
            }
            if (!got) return;
            Fx.Sound(gunId + "/bites", "end", body.corePosition);
            Vfx.Play("mina_bites", "jump", body.footPosition, 1f, body.transform);
            MeltyTest.Report("love-bites-jump", true, body.baseNameToken);
        }

        // ---------------------------------------------------------------- the umbrella

        /// <summary>The umbrella is open (exact on the owner; read from her fall elsewhere).</summary>
        public bool Floating { get; private set; }
        /// <summary>Test mode: Jump counts as held (there is no player to hold it).</summary>
        public bool TestHold;
        float held, baseAir = -1f;
        bool pressOpen, noFallSet;

        bool JumpDown => TestHold || body.inputBank && body.inputBank.jump.down;

        void Update()
        {
            if (!body || !body.hasEffectiveAuthority || !body.characterMotor || hero == null) return;
            var m = body.characterMotor;
            // Jump in the air with every jump spent: the stored Love Bites jump if there is one (RoR2 then jumps as usual),
            // otherwise the umbrella opens at once
            if (body.inputBank && body.inputBank.jump.justPressed && !m.isGrounded && m.jumpCount >= body.maxJumpCount && InMain())
            {
                if (Bonus > 0) { Bonus = 0; m.jumpCount = Mathf.Max(0, body.maxJumpCount - 1); MeltyTest.Report("love-bites-jump-used", true, body.baseNameToken); }
                else if (hero.FloatFallSpeed > 0f) pressOpen = true;
            }
        }

        /// <summary>Her Body machine is in its main state (no flight, dash or ult moving her).</summary>
        bool InMain()
        {
            var esm = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
            return !esm || esm.state is GenericCharacterMain;
        }

        void FixedUpdate()
        {
            if (!body || hero == null) return;
            if (body.hasEffectiveAuthority && body.characterMotor) OwnerFloat();
        }

        void OwnerFloat()
        {
            var m = body.characterMotor;
            if (m.isGrounded) { gainedThisTrip = false; held = 0f; pressOpen = false; }
            if (hero.FloatFallSpeed <= 0f) return;
            bool down = JumpDown;
            if (down && !m.isGrounded) held += Time.fixedDeltaTime;
            else { held = 0f; pressOpen = false; }
            // one clear rule (creator, 2026-10-26): Jump held in the air opens it, after a jump or not, and it stays open as long
            // as Jump is held (floatHold 0: at once); let go and it closes. Never without Jump held, even after a move that ends in
            // the air (creator, 2026-11-02: "ONLY FLOAT WITH THE UMBRELLA WHEN THE SPACE KEY IS DOWN"; Nox Nostra and Mist Form
            // opened it by themselves until she landed)
            bool open = !m.isGrounded && down && (held >= hero.FloatHold || pressOpen) && InMain() && body.healthComponent && body.healthComponent.alive;
            if (open != Floating) SetFloat(open);
            if (Floating)
            {
                var v = m.velocity;
                // the motor adds this step's gravity after us: start from that much above, so she really falls at floatFallSpeed
                float floor = -hero.FloatFallSpeed - Physics.gravity.y * Time.fixedDeltaTime;
                if (v.y < floor) { v.y = floor; m.velocity = v; }
            }
        }

        void SetFloat(bool on)
        {
            var m = body.characterMotor;
            Floating = on;
            if (baseAir < 0f) baseAir = m.airControl;
            // floatAirControl: her air control while it is open (0.8: 80 % of her grip on the ground); floatMoveSpeedMult on her
            // move speed meanwhile (StatsPatch reads Floating)
            m.airControl = on && hero.FloatAirControl > 0f ? hero.FloatAirControl : baseAir;
            body.MarkAllStatsDirty();
            // no fall damage while it is open (and on the landing it closes for)
            if (on && (body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) == 0) { body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage; noFallSet = true; }
            if (on) MeltyTest.Report("umbrella-float", true, body.baseNameToken);
        }

        // after landing, the flag set for the umbrella goes once she is down (a landing with it open never hurts)
        void LateUpdateOwnerFlags()
        {
            if (!noFallSet || Floating) return;
            var m = body.characterMotor;
            if (m && m.isGrounded && !Survivors.NoFallDamage(body)) { body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage; noFallSet = false; }
            else if (m && !m.isGrounded && m.velocity.y < -hero.FloatFallSpeed * 3f && !Survivors.NoFallDamage(body))
            {
                // closed high up and falling fast: the fall counts again
                body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage; noFallSet = false;
            }
        }

        // ---------------------------------------------------------------- what everyone sees

        bool shown;
        int loop;
        GameObject glow;
        float slowFor, lastY, flutter;
        DeadlockAnimator anim;
        Transform umbrellaTop;

        /// <summary>The umbrella is shown open on this machine.</summary>
        public bool Shown => shown;

        void LateUpdate()
        {
            if (!body || hero == null || hero.FloatFallSpeed <= 0f) return;
            if (body.hasEffectiveAuthority) LateUpdateOwnerFlags();
            bool on;
            if (body.hasEffectiveAuthority) on = Floating;
            else
            {
                // another player's Mina: a slow, steady fall is her umbrella
                float dt = Time.deltaTime, y = transform.position.y;
                float vy = dt > 0f ? (y - lastY) / dt : 0f;
                lastY = y;
                bool air = body.characterMotor ? !body.characterMotor.isGrounded : true;
                bool slow = air && vy < -hero.FloatFallSpeed * 0.4f && vy > -hero.FloatFallSpeed * 1.6f && InMain();
                slowFor = slow ? slowFor + dt : 0f;
                on = slowFor > 0.12f || shown && air && vy < 0.5f && vy > -hero.FloatFallSpeed * 2f;
            }
            if (!(body.healthComponent && body.healthComponent.alive)) on = false;
            if (on != shown) Show(on);
            if (shown)
            {
                // the skirt and hair lift and flutter in the updraft
                if (!anim) anim = DeadlockAnimator.Of(body);
                flutter += Time.deltaTime;
                var springs = anim ? anim.Springs : null;
                if (springs) springs.Kick(Vector3.up * 0.9f + new Vector3(Mathf.PerlinNoise(flutter * 1.7f, 0.3f) - 0.5f, 0f, Mathf.PerlinNoise(0.7f, flutter * 1.9f) - 0.5f) * 1.2f);
            }
        }

        void Show(bool on)
        {
            shown = on;
            if (!anim) anim = DeadlockAnimator.Of(body);
            if (anim) anim.Hold(on ? "float" : null);
            if (on)
            {
                Fx.Sound(gunId + "/umbrella", "cast", body.corePosition);
                loop = Fx.Loop(gunId + "/umbrella", body.corePosition);
                if (!umbrellaTop && body.modelLocator && body.modelLocator.modelTransform)
                    foreach (var t in body.modelLocator.modelTransform.GetComponentsInChildren<Transform>(true)) if (t.name == "rib_inner_slide") { umbrellaTop = t; break; }
                glow = Vfx.Play("mina_umbrella", "loop", body.corePosition, 1f, umbrellaTop ? umbrellaTop : body.transform);
            }
            else
            {
                DeadlockAudio.Stop(loop); loop = 0;
                Fx.Sound(gunId + "/umbrella", "end", body.corePosition);
                Fx.Sound(gunId + "/umbrella", "reload", body.corePosition);
                Vfx.Stop(glow, 0.6f); glow = null;
            }
        }

        void OnDisable()
        {
            if (shown) Show(false);
            if (body && body.characterMotor && baseAir >= 0f) body.characterMotor.airControl = baseAir;
            Floating = false;
        }
    }
}
