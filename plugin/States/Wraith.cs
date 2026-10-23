using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ---------------- Wraith: Card Trick (M2) ----------------
    /// <summary>One card, thrown a moment into the clip at the enemy under the crosshair (lockRange) and homing on it. The owner
    /// draws its trick (bleed, crit, stun, burn) and picks the target; both travel with the state, so every machine throws the same
    /// card and the server's deals the hit.</summary>
    [Kind("card_trick")]
    public sealed class CardTrick : DlrState
    {
        /// <summary>Test mode: the next card's trick (-1 = drawn at random), so each trick is checked in turn.</summary>
        public static int TestSuit = -1;
        CardSuit suit;
        bool thrown;
        /// <summary>Where the card leaves from, against the body's core: the owner's, sent with the state (doc 02 gotcha 30).</summary>
        Vector3 fromOffset;

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority)
            {
                LockOn(row.LockRange);
                suit = TestSuit >= 0 ? (CardSuit)(TestSuit % 4) : (CardSuit)Random.Range(0, 4);
                fromOffset = HandPos() - characterBody.corePosition;
            }
            Sound("cast");
            if (anim) anim.Gesture("secondary", 1.4f);
            Vfx.Play(row.Vfx, "cast", From(), row.Radius, null, null, (float)suit);
            characterBody.SetAimTimer(1f);
        }

        Vector3 HandPos()
        {
            var aim = GetAimRay();
            var right = Vector3.Cross(Vector3.up, aim.direction).normalized;
            return aim.origin + aim.direction * 0.8f - right * 0.35f;
        }

        Vector3 From() => fromOffset != Vector3.zero ? characterBody.corePosition + fromOffset : HandPos();

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (!thrown && fixedAge >= row.Duration * 0.4f) Throw();
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }

        void Throw()
        {
            thrown = true;
            DlrCard.Launch(characterBody, row, From(), ThrowDir, LockTarget && LockTarget.healthComponent && LockTarget.healthComponent.alive ? LockTarget : null, suit, Server);
        }

        public override void OnExit()
        {
            // cut short (stunned, frozen): the card still goes
            if (row != null && !thrown) Throw();
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write((byte)suit);
            writer.Write(fromOffset);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            suit = (CardSuit)reader.ReadByte();
            fromOffset = reader.ReadVector3();
        }
    }

    // ---------------- Wraith: Project Mind (Shift) ----------------
    /// <summary>A purple marker follows the crosshair on the ground up to range metres away; any key (M1, M2, Shift, R, jump)
    /// pressed after minDuration teleports her there, and after duration she goes on her own. Only the owner sees the marker and
    /// teleports; where she leaves and lands is relayed to every player.</summary>
    [Kind("mind_port")]
    public sealed class MindPort : DlrState
    {
        GameObject marker;
        Vector3 point;
        bool valid, gone;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (isAuthority)
            {
                valid = AimPoint(out point);
                marker = Vfx.Play(row.Vfx, "aim", point, row.Radius);
                if (marker) marker.SetActive(valid);
            }
        }

        /// <summary>The ground under the crosshair (or under the enemy it is on), in sight, at most range away; a step back
        /// toward her from an enemy so she doesn't land inside it. False when there is no ground there (the sky, a pit).</summary>
        bool AimPoint(out Vector3 p)
        {
            var aim = GetAimRay();
            p = aim.origin + aim.direction * row.Range;
            var hits = Physics.SphereCastAll(aim, 0.3f, row.Range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body == characterBody)) continue;
                p = h.point;
                if (hb)
                {
                    var back = aim.direction; back.y = 0;
                    p = hb.healthComponent.body.footPosition - back.normalized * (hb.healthComponent.body.radius + characterBody.radius + 0.5f);
                }
                else p -= aim.direction * (characterBody.radius + 0.2f);
                break;
            }
            var flat = p - transform.position; flat.y = 0;
            if (flat.magnitude > row.Range) p = transform.position + flat.normalized * row.Range + Vector3.up * (p.y - transform.position.y);
            if (!Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var ground, 60f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return false;
            p = ground.point;
            return true;
        }

        bool AnyKey()
        {
            if (!inputBank) return false;
            return inputBank.skill1.justPressed || inputBank.skill2.justPressed || inputBank.skill3.justPressed
                || inputBank.skill4.justPressed || inputBank.jump.justPressed;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null || !isAuthority) return;
            valid = AimPoint(out point);
            if (marker) { marker.SetActive(valid); marker.transform.position = point; }
            // test mode and the bench have no player to press: she goes a moment after the marker is up
            bool bot = (MeltyTest.Active || DpsBench.Active) && fixedAge >= 0.8f;
            if (fixedAge >= row.MinDuration && (AnyKey() || bot) || fixedAge >= row.Duration) Go();
        }

        void Go()
        {
            if (gone) return;
            gone = true;
            if (valid)
            {
                var from = characterBody.footPosition;
                TeleportHelper.TeleportBody(characterBody, point, false);
                if (characterMotor) characterMotor.velocity = Vector3.zero;
                Relay.Vfx(row.Vfx, "end", from, row.Radius);
                Relay.Vfx(row.Vfx, "hit", point, row.Radius);
                Relay.Sound(row.Id, "end", from);
                Relay.Sound(row.Id, "hit", point);
                if (anim) anim.Gesture("utility", 1.5f);
                MeltyTest.Report("project-mind", true, characterBody.baseNameToken);
            }
            else
            {
                // nowhere to land (the sky, a pit): nothing happens and the charge comes back
                Sound("reload");
                RefundStock();
            }
            outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            Vfx.Stop(marker, 0.1f);
            base.OnExit();
        }

        // nothing she presses to go (M2, R) may start its own move instead
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Pain;
    }

    // ---------------- Wraith: Full Auto (R) ----------------
    /// <summary>The reload clip in a burst of purple smoke: the magazine is full, and buff (its attackSpeedAdd: the fire rate) stays on
    /// until that magazine is empty; GunState then reloads at the normal speed, and the server takes the buff off when the reload is
    /// relayed. The kick is 20% stronger meanwhile (GunState).</summary>
    [Kind("full_auto")]
    public sealed class FullAuto : DlrState
    {
        bool clipIn;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (anim) anim.Gesture("reload", 1.6f);
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, characterBody.radius, transform);
            WraithAura.Ensure(characterBody);
            var gun = Survivors.RowFor(skillLocator ? skillLocator.primary : null);
            if (isAuthority && rt != null && gun != null && gun.ClipSize > 0)
            {
                rt.Reloading = false;
                rt.Ammo = (int)gun.ClipSize;
            }
            if (Server) Buffs.AddTimed(characterBody, row.Buff, Sheet.Buff(row.Buff)?.Duration ?? 30f);
            MeltyTest.Report("full-auto", true, characterBody.baseNameToken);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (!clipIn && fixedAge >= row.Duration * 0.6f) { clipIn = true; Sound("reload"); }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }
    }

    // ---------------- Wraith: Telekinesis (Q ult equipment) ----------------
    /// <summary>Every enemy within radius rises, hangs and is thrown along the carrier's aim (WraithThrown, on the server); bosses
    /// only rise a little and take the hit and a stun. The carrier is slowed while it casts. Works on any body.</summary>
    [Kind("ult_telekinesis")]
    public sealed class UltTelekinesis : UltState
    {
        int loop;
        bool thrown;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            Sound("cast");
            if (anim) anim.Override("ult_start", row.Interval);
            Vfx.Play(row.Vfx, "cast", characterBody.footPosition, row.Radius, transform);
            SlowWhileActive(row.MoveSpeedMult);
            characterBody.SetAimTimer(row.Duration + 0.5f);
            loop = Fx.Loop(row.Id, transform.position);
            if (Server)
                foreach (var hb in Fx.Enemies(characterBody, characterBody.corePosition, row.Radius))
                    WraithThrown.Begin(characterBody, hb.healthComponent.body, row, ThrowDir);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (!thrown && fixedAge >= row.Interval)
            {
                thrown = true;
                Sound("end");
                DeadlockAudio.Stop(loop);
                if (anim) anim.Override("ult_loop", row.Duration - row.Interval);
                var aim = ThrowDir; aim.y = 0;
                Vfx.Play(row.Vfx, "trigger", characterBody.corePosition, row.Radius, null, characterBody.corePosition + aim.normalized * row.Reach);
            }
            if (fixedAge >= row.Duration && isAuthority) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            DeadlockAudio.Stop(loop);
            if (anim) anim.StopOverride();
            base.OnExit();
        }
    }
}
