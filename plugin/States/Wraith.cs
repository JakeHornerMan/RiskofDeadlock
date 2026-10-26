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
            if (anim)
            {
                // Deadlock's whole card-trick throw (its in-air one off the ground), and her coat flaring out behind the arm
                bool air = characterMotor && !characterMotor.isGrounded && anim.HasClip("secondary_air");
                anim.Gesture(air ? "secondary_air" : "secondary", 1.4f);
                if (anim.Springs)
                {
                    var aim = GetAimRay().direction; aim.y = 0f;
                    var side = Vector3.Cross(Vector3.up, aim).normalized;
                    anim.Springs.Kick((side * 0.6f - aim.normalized * 0.5f + Vector3.up * 0.4f) * CoatFlare);
                }
            }
            Vfx.Play(row.Vfx, "cast", From(), row.Radius, null, null, (float)suit);
            characterBody.SetAimTimer(1f);
        }

        /// <summary>How hard the coat flares on a throw (m/s at the tips).</summary>
        const float CoatFlare = 9f;

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
    /// <summary>A purple projection of Wraith follows the crosshair up to range metres away, standing on the ground or floating
    /// in the air, with cards drifting to it from her; any key (M1, M2, Shift, R, jump) pressed after minDuration teleports her
    /// there, and after duration she goes on her own. She can only go where she can see (no spot behind a rock) and where she
    /// fits: elsewhere the marker turns a dim red and a key gives the charge back. Only the owner sees the marker and teleports;
    /// where she leaves and lands is relayed to every player.</summary>
    [Kind("mind_port")]
    public sealed class MindPort : DlrState
    {
        GameObject marker, ghost, link;
        Vector3 point;
        bool valid, gone;
        /// <summary>How close the ground must be under an aimed spot for her to stand on it (else she floats there, in the air).</summary>
        const float GroundSnap = 2.5f;

        public override void OnEnter()
        {
            base.OnEnter();
            Sound("cast");
            if (isAuthority)
            {
                valid = AimPoint(out point);
                marker = Vfx.Play(row.Vfx, "aim", point, row.Radius);
                ghost = Vfx.MindGhost(characterBody);
                if (ghost)
                {
                    link = new GameObject("WraithMindLink");
                    var ml = link.AddComponent<Vfx.MindLink>();
                    ml.From = Fx.Muzzle(characterBody) ? Fx.Muzzle(characterBody) : transform; ml.To = ghost.transform;
                }
                Place();
            }
        }

        /// <summary>The spot under the crosshair, at most range away: on the ground (or just in front of the wall or enemy it
        /// is on), or in the air where the crosshair points at the sky. Valid when she can see it from her eyes and her body
        /// fits there.</summary>
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
            // standing on the ground when it is close under the spot; otherwise she floats there (the projection in the air)
            if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var ground, GroundSnap + 1f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                p = ground.point;
            return CanGo(characterBody, aim.origin, p);
        }

        /// <summary>She can go to p: nothing solid between her eyes and the spot's chest height (no spot behind a rock), and
        /// her body fits there.</summary>
        public static bool CanGo(CharacterBody body, Vector3 eye, Vector3 p)
        {
            var to = p + Vector3.up * 1f - eye;
            if (Physics.Raycast(eye, to.normalized, out _, Mathf.Max(0f, to.magnitude - 0.3f), LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return false;
            float r = body.radius * 0.8f;
            return !Physics.CheckCapsule(p + Vector3.up * (r + 0.15f), p + Vector3.up * Mathf.Max(r + 0.2f, 1.6f), r, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }

        void Place()
        {
            if (marker)
            {
                marker.transform.position = point;
                var mm = marker.GetComponent<Vfx.MindMarker>();
                if (mm) mm.SetValid(valid);
            }
            if (ghost)
            {
                ghost.SetActive(valid);
                ghost.transform.position = point;
                var face = GetAimRay().direction; face.y = 0f;
                if (face.sqrMagnitude > 0.01f) ghost.transform.rotation = Quaternion.LookRotation(face.normalized);
            }
            if (link) link.SetActive(valid);
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
            Place();
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
                // she flies there as purple smoke and cards (MindTravel, on every machine): up to interval s for a full-range trip
                var from = characterBody.footPosition;
                float dist = Vector3.Distance(from, point);
                float time = row.Interval > 0f ? Mathf.Clamp(dist / Mathf.Max(1f, row.Range) * row.Interval, MindTravel.MinTime, row.Interval) : MindTravel.MinTime;
                MeltyTest.Report("project-mind", true, characterBody.baseNameToken);
                outer.SetNextState(new MindTravel { RowOverride = row, From = from, To = point, Time = time });
                return;
            }
            else
            {
                // somewhere she can't see or doesn't fit: nothing happens and the charge comes back
                Sound("reload");
                RefundStock();
                MeltyTest.Report("project-mind-refused", true, characterBody.baseNameToken);
            }
            outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            Vfx.Stop(marker, 0.1f);
            if (ghost) Object.Destroy(ghost);
            if (link) Object.Destroy(link);
            base.OnExit();
        }

        /// <summary>Test mode: where the state is aiming and whether it would go (the "behind a wall" check reads it).</summary>
        public bool AimValid => valid;

        // nothing she presses to go (M2, R) may start its own move instead
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Pain;
    }

    /// <summary>Project Mind's flight (creator, 2026-10-25: "a moment she turn in to purple vfx and card and travel to the location"):
    /// MindPort hands over From, To and Time; on every machine her model is hidden and a cloud of purple smoke and cards (the
    /// "travel" visual) flies with her body; the owner carries the body along a low arc with gravity off, the server keeps her
    /// untouchable (HiddenInvincibility). At the end she lands exactly on the spot and reappears in a burst ("hit").</summary>
    public sealed class MindTravel : DlrState
    {
        public Vector3 From, To;
        public float Time;
        /// <summary>The shortest flight, for a spot right next to her (s).</summary>
        public const float MinTime = 0.4f;
        CharacterModel model;
        GameObject cloud;
        bool antiGravity, landed;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            if (Time <= 0f) Time = MinTime;
            model = characterBody.modelLocator && characterBody.modelLocator.modelTransform ? characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            if (model) model.invisibilityCount++;
            // where she leaves: she bursts into smoke and cards
            Vfx.Play(row.Vfx, "end", From, row.Radius);
            Fx.Sound(row.Id, "end", From);
            cloud = Vfx.Play(row.Vfx, "travel", characterBody.corePosition, row.Radius, transform);
            if (Server) characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, Time + 0.2f);
            if (isAuthority && characterMotor)
            {
                var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount++; characterMotor.gravityParameters = gp;
                antiGravity = true;
                characterMotor.velocity = Vector3.zero;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (isAuthority && characterMotor)
            {
                float t = Mathf.Clamp01(fixedAge / Time);
                // ease in and out, on a low arc (higher for a longer trip, at most 3 m)
                float s = t * t * (3f - 2f * t);
                float lift = Mathf.Min(3f, Vector3.Distance(From, To) * 0.08f) * Mathf.Sin(Mathf.PI * t);
                var p = Vector3.Lerp(From, To, s) + Vector3.up * lift;
                // the body's position is its centre, From/To are the feet
                characterMotor.Motor.SetPosition(p + (characterBody.corePosition - characterBody.footPosition));
                characterMotor.velocity = Vector3.zero;
            }
            if (fixedAge >= Time && isAuthority) outer.SetNextStateToMain();
        }

        void Land()
        {
            if (landed) return;
            landed = true;
            if (isAuthority)
            {
                TeleportHelper.TeleportBody(characterBody, To, false);
                if (characterMotor)
                {
                    characterMotor.velocity = Vector3.zero;
                    if (antiGravity) { var gp = characterMotor.gravityParameters; gp.channeledAntiGravityGranterCount--; characterMotor.gravityParameters = gp; antiGravity = false; }
                }
            }
        }

        public override void OnExit()
        {
            if (row != null)
            {
                Land();
                if (model) model.invisibilityCount--;
                Vfx.Stop(cloud, 0.6f);
                // where she lands: she pulls back together out of the smoke
                Vfx.Play(row.Vfx, "hit", To, row.Radius);
                Fx.Sound(row.Id, "hit", To);
                if (anim) { anim.Gesture("utility", 1.5f); if (anim.Springs) anim.Springs.Reset(); }
                if (Server) characterBody.ClearTimedBuffs(RoR2Content.Buffs.HiddenInvincibility);
                // no fall damage from the drop after it (a spot in the air), until she next lands; other falls hurt as usual
                // (creator, 2026-10-26)
                if (!Survivors.NoFallDamage(characterBody) && characterBody.GetComponent<SoftLanding>() == null)
                {
                    characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                    characterBody.gameObject.AddComponent<SoftLanding>();
                }
                MeltyTest.Report("mind-travel", true, characterBody.baseNameToken);
            }
            base.OnExit();
        }

        // nothing cuts the flight short
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(From); writer.Write(To); writer.Write(Time);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            From = reader.ReadVector3(); To = reader.ReadVector3(); Time = reader.ReadSingle();
        }
    }

    // ---------------- Wraith: Full Auto (R) ----------------
    /// <summary>The reload clip in a burst of purple smoke: the magazine is loaded with the row's clipSize rounds (more than the gun's
    /// own magazine holds; 0 = the gun's), and buff (its attackSpeedAdd: the fire rate) stays on
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
            // Deadlock's own Full Auto cast, fitted to the move: it ends as the magazine goes in (the 3 s reload clip at x1.6 ran
            // on long after she could fire again; creator, 2026-10-25)
            if (anim)
            {
                float len = anim.ClipLength("special");
                anim.Gesture(len > 0f ? "special" : "reload", len > 0f ? len / Mathf.Max(0.1f, row.Duration) : 1.6f);
            }
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, characterBody.radius, transform);
            WraithAura.Ensure(characterBody);
            var gun = Survivors.RowFor(skillLocator ? skillLocator.primary : null);
            if (isAuthority && rt != null && gun != null && gun.ClipSize > 0)
            {
                rt.Reloading = false;
                rt.Ammo = (int)(row.ClipSize > 0 ? row.ClipSize : gun.ClipSize);
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
