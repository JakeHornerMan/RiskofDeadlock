using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Billy's air combat (v0.11.0): the 5 m lock-on, the hang time that holds him up after each air bat hit, his
    /// G.O.A.T. air dodge, and the slam that drives enemies into the floor. The enemies' float itself is <see cref="DlrLift"/>.</summary>
    public static class BillyAir
    {
        /// <summary>The living enemy nearest the body within range that it can see (Bashdown and the air bat lock onto it), or null.</summary>
        public static HurtBox Nearest(CharacterBody body, float range)
        {
            if (!body) return null;
            foreach (var hb in Fx.Enemies(body, body.corePosition, range))
            {
                var b = hb.healthComponent.body;
                if (!b) continue;
                if (Physics.Linecast(body.corePosition, b.corePosition, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)
                    && Physics.Linecast(body.corePosition, b.corePosition + Vector3.up * b.radius, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                return hb;
            }
            return null;
        }

        /// <summary>Drive an enemy into the floor at speed m/s (air Bashdown, the chains' slam): ends its float, and a body in the
        /// air without one (a flyer, a falling monster) is pushed down too. Server only.</summary>
        public static void SlamDown(HealthComponent hc, float speed)
        {
            if (!hc || !hc.body || !NetworkServer.active) return;
            var lift = hc.body.GetComponent<DlrLift>();
            if (!lift) lift = hc.body.gameObject.AddComponent<DlrLift>();
            lift.Slam(speed);
        }

        /// <summary>Off the floor, for Billy's air moves: not grounded, or rising from a jump this very moment (the motor still
        /// reads grounded for a frame or two after the jump, when M2 pressed at once would otherwise swing on the ground).</summary>
        public static bool InAir(CharacterBody body)
        {
            var m = body ? body.characterMotor : null;
            return m && (!m.isGrounded || m.velocity.y > 1f);
        }

        /// <summary>A body in Billy's combo (in the air, or with a float of his still running): the DPS bench leaves it where it is
        /// instead of putting it back on its spot.</summary>
        public static bool InCombo(CharacterBody body)
        {
            var rt = body ? body.GetComponent<HeroRuntime>() : null;
            return rt && rt.Dodges > 0 && (body.characterMotor && !body.characterMotor.isGrounded || Time.time < rt.ComboUntil);
        }

        /// <summary>Owner: start the G.O.A.T. dodge now if one is left (the test calls it; a player gets it from a jump press in the air).</summary>
        public static bool TryDodge(CharacterBody body)
        {
            var rt = body ? body.GetComponent<HeroRuntime>() : null;
            if (!rt || !body.hasEffectiveAuthority || rt.DodgesLeft <= 0) return false;
            var esm = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
            if (!esm || !(esm.state is GenericCharacterMain)) return false;
            var ib = body.inputBank;
            var dir = ib ? ib.aimDirection : body.transform.forward;
            if (ib && ib.moveVector.sqrMagnitude > 0.1f) { dir = ib.moveVector; dir.y = 0; }
            esm.SetNextState(new States.BillyDodge { Dir = dir.normalized });
            return true;
        }
    }

    /// <summary>An enemy held by one of Billy's moves (server; its motion reaches everyone the usual way). Float: up
    /// height metres in riseTime, then drifting down at driftSpeed until the time is up (Rising Ram's juggle; an air bat hit adds
    /// time, up to a cap). Haul: up, then held still (Chain Gang) until its owner slams it. Slam: driven down hard until it lands.
    /// Gravity is off on its motor the whole time (a flyer's rigidbody is driven the same way) and it is stunned while held.</summary>
    public sealed class DlrLift : MonoBehaviour
    {
        enum Phase { Rise, Drift, Hold, Slam }
        Phase phase;
        CharacterBody body;
        float riseSpeed, riseEnd, until, cap, driftSpeed, slamSpeed, start, slamUntil;
        bool antiGravity, flyer, hold, lifted;

        /// <summary>Rising Ram's float on one enemy (bosses are skipped by the caller).</summary>
        public static DlrLift Float(CharacterBody victim, float height, float riseTime, float floatTime)
        {
            if (!victim || !NetworkServer.active) return null;
            var l = victim.GetComponent<DlrLift>() ?? victim.gameObject.AddComponent<DlrLift>();
            l.Begin(height, riseTime, floatTime, false);
            return l;
        }

        /// <summary>Chain Gang: hauled height metres up in riseTime, then held until the chains slam it.</summary>
        public static DlrLift Haul(CharacterBody victim, float height, float riseTime, float holdTime)
        {
            if (!victim || !NetworkServer.active) return null;
            var l = victim.GetComponent<DlrLift>() ?? victim.gameObject.AddComponent<DlrLift>();
            l.Begin(height, riseTime, riseTime + holdTime, true);
            return l;
        }

        public static bool Floating(CharacterBody b) { var l = b ? b.GetComponent<DlrLift>() : null; return l && l.phase != Phase.Slam; }

        void Begin(float height, float riseTime, float total, bool holdOnly)
        {
            body = GetComponent<CharacterBody>();
            start = Time.fixedTime;
            riseSpeed = height / Mathf.Max(0.05f, riseTime);
            riseEnd = start + riseTime;
            until = start + total;
            cap = start + total;
            hold = holdOnly;
            // what is left of the height drifts down over the rest of the float, slowly (about 1 m/s)
            driftSpeed = Mathf.Min(1f, height / Mathf.Max(0.5f, total - riseTime));
            phase = Phase.Rise;
            flyer = body && !body.characterMotor && body.rigidbody;
            GravityOff(true);
            if (body && body.characterMotor) body.characterMotor.Motor.ForceUnground();
            Fx.Stun(body ? body.healthComponent : null, total + 0.1f);
            lifted = true;
            Relay.Vfx("billy_float", "loop", body ? body.corePosition : transform.position, total, gameObject);
        }

        /// <summary>An air bat hit: the float lasts seconds more, at most up to capTotal seconds after it started.</summary>
        public void Extend(float seconds, float capTotal)
        {
            if (phase == Phase.Slam || hold) return;
            float newUntil = Mathf.Min(Mathf.Max(until, Time.fixedTime) + seconds, start + capTotal);
            if (newUntil <= until) return;
            until = newUntil;
            if (phase == Phase.Drift) driftSpeed = Mathf.Min(driftSpeed, 1f);
            Fx.Stun(body ? body.healthComponent : null, until - Time.fixedTime + 0.1f);
        }

        /// <summary>Driven into the floor at speed m/s until it lands (at most a second).</summary>
        public void Slam(float speed)
        {
            if (!body) body = GetComponent<CharacterBody>();
            if (!body) { Destroy(this); return; }
            flyer = !body.characterMotor && body.rigidbody;
            // a body already standing on the ground (never lifted) has nowhere to go
            if (!lifted && body.characterMotor && body.characterMotor.isGrounded) { Destroy(this); return; }
            lifted = true;
            if (!antiGravity) GravityOff(true);
            phase = Phase.Slam;
            slamSpeed = speed;
            slamUntil = Time.fixedTime + (flyer ? 0.3f : 1f);
        }

        void GravityOff(bool off)
        {
            var m = body ? body.characterMotor : null;
            if (!m) return;
            if (antiGravity == off) return;
            var gp = m.gravityParameters; gp.channeledAntiGravityGranterCount += off ? 1 : -1; m.gravityParameters = gp;
            antiGravity = off;
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active || !body || !body.healthComponent || !body.healthComponent.alive) { Destroy(this); return; }
            float now = Time.fixedTime;
            Vector3 v;
            switch (phase)
            {
                case Phase.Rise:
                    v = Vector3.up * riseSpeed;
                    if (now >= riseEnd) phase = hold ? Phase.Hold : Phase.Drift;
                    break;
                case Phase.Drift:
                    v = flyer ? Vector3.zero : Vector3.down * driftSpeed;
                    if (now >= until) { Destroy(this); return; }
                    break;
                case Phase.Hold:
                    v = Vector3.zero;
                    // the chains' owner slams it; if that never comes (the carrier died), let go after a while
                    if (now >= until + 1f) { Destroy(this); return; }
                    break;
                default:
                    v = Vector3.down * slamSpeed;
                    if (now >= slamUntil || body.characterMotor && body.characterMotor.isGrounded && now > slamUntil - 0.95f) { Destroy(this); return; }
                    break;
            }
            if (body.characterMotor)
            {
                var m = body.characterMotor;
                m.velocity = v;
                m.moveDirection = Vector3.zero;
                if (v.y > 0f && m.isGrounded) m.Motor.ForceUnground();
            }
            else if (body.rigidbody) body.rigidbody.velocity = v;
        }

        void OnDestroy()
        {
            GravityOff(false);
        }
    }
}
