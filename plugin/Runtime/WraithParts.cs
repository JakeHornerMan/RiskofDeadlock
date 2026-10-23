using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Wraith's four card tricks (Card Trick draws one per throw): Bleed and Burn put buffStacks stacks of RoR2's own DoT on
    /// for dotTime s, Crit makes the hit a certain critical hit, Stun stuns for stunDuration s.</summary>
    public enum CardSuit { Bleed = 0, Crit = 1, Stun = 2, Burn = 3 }

    /// <summary>Card Trick's card: flies on every machine (launched by the state that runs everywhere), homing on the enemy the
    /// owner locked on to, or on any enemy within seekRange of its path; only the server's copy (damage = true) deals the hit and
    /// its trick. The other copies are the same flight, drawn and heard.</summary>
    public sealed class DlrCard : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 dir; HurtBox target; CardSuit suit; bool damage;
        float travelled, nextLook; int loop; GameObject vfx;
        /// <summary>Degrees a second the card turns toward its target.</summary>
        const float TurnRate = 720f;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, HurtBox target, CardSuit suit, bool damage)
        {
            var go = new GameObject("DlrCard_" + row.Id);
            go.transform.position = from;
            go.transform.rotation = Quaternion.LookRotation(dir);
            var c = go.AddComponent<DlrCard>();
            c.owner = owner; c.row = row; c.dir = dir.normalized; c.target = target; c.suit = suit; c.damage = damage;
            c.loop = Fx.Loop(row.Id, from);
            // the trick's colour rides in the angle argument (Vfx.WraithCard)
            c.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform, null, (float)suit);
        }

        bool Alive(HurtBox hb) => hb && hb.healthComponent && hb.healthComponent.alive && hb.healthComponent.body;

        void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            float dt = Time.fixedDeltaTime;
            var pos = transform.position;
            // thrown at nothing: it bends toward the first enemy that comes within seekRange of it
            if (!Alive(target) && Time.time >= nextLook)
            {
                nextLook = Time.time + 0.1f;
                var near = Fx.Enemies(owner, pos, row.SeekRange, 1);
                target = near.Count > 0 ? near[0] : null;
            }
            if (Alive(target))
            {
                var want = (target.healthComponent.body.corePosition - pos).normalized;
                dir = Vector3.RotateTowards(dir, want, TurnRate * Mathf.Deg2Rad * dt, 0f).normalized;
            }
            float step = row.Speed * dt;
            transform.rotation = Quaternion.LookRotation(dir);
            int mask = LayerIndex.world.mask | LayerIndex.entityPrecise.mask;
            if (Physics.SphereCast(pos, Mathf.Max(0.2f, row.Radius), dir, out var hit, step, mask, QueryTriggerInteraction.Ignore))
            {
                var hb = hit.collider.GetComponent<HurtBox>();
                bool enemy = hb && hb.healthComponent && hb.healthComponent.body && hb.healthComponent.body.teamComponent.teamIndex != owner.teamComponent.teamIndex;
                if (hb && !enemy) transform.position += dir * step;
                else
                {
                    transform.position = hit.point;
                    if (enemy) HitEnemy(hb, hit.point);
                    else { Fx.Sound(row.Id, "end", hit.point); Vfx.Play(row.Vfx, "end", hit.point, row.Radius, null, null, (float)suit); }
                    Done();
                    return;
                }
            }
            else transform.position += dir * step;
            travelled += step;
            if (travelled > row.Range) { Vfx.Play(row.Vfx, "end", transform.position, row.Radius, null, null, (float)suit); Done(); }
        }

        void HitEnemy(HurtBox hb, Vector3 at)
        {
            var victim = hb.healthComponent.body;
            if (damage)
            {
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, at, default, suit == CardSuit.Crit ? DamageColorIndex.WeakPoint : DamageColorIndex.Default, null, suit == CardSuit.Crit);
                int stacks = Mathf.Max(1, (int)row.BuffStacks);
                if (suit == CardSuit.Bleed) Buffs.Dot(owner, victim, DotController.DotIndex.Bleed, row.DotTime, stacks);
                else if (suit == CardSuit.Burn) Buffs.Dot(owner, victim, DotController.DotIndex.Burn, row.DotTime, stacks);
                else if (suit == CardSuit.Stun) Fx.Stun(hb.healthComponent, row.StunDuration);
                MeltyTest.Report("card:" + suit.ToString().ToLowerInvariant(), true, owner.baseNameToken);
                if (MeltyTest.Active) Log.Info($"card trick: {suit} on {victim.name}");
            }
            Fx.Sound(row.Id, "hit", at);
            Vfx.Play(row.Vfx, "hit", victim ? victim.corePosition : at, row.Radius, null, null, (float)suit);
        }

        void Done()
        {
            DeadlockAudio.Stop(loop);
            Vfx.Stop(vfx, 0.4f);
            Destroy(gameObject);
        }

        void OnDestroy() => DeadlockAudio.Stop(loop);
    }

    /// <summary>One enemy in Wraith's Telekinesis (server; its motion reaches everyone the usual way): it rises, hangs, then is
    /// thrown along the carrier's aim (gravity off, our own arc) until it lands, hits a wall or has flown its distance, and then
    /// falls normally. The hit lands where it stops: damagePct, x airMult on a wall. A boss only rises a little and takes the hit
    /// and a stun where it hangs. The pickup skill row drives every number.</summary>
    public sealed class WraithThrown : MonoBehaviour
    {
        enum Phase { Rise, Hold, Throw, Fall }
        Phase phase;
        CharacterBody owner, body; SkillRow row; Vector3 flat;
        float start, riseSpeed, throwAt, vy, travelled, fallUntil;
        bool antiGravity, boss, done;
        /// <summary>The arc's own gravity while thrown (m/s2) and the speed it is thrown upward with.</summary>
        const float ArcGravity = 20f, ArcLift = 6f;

        public static void Begin(CharacterBody owner, CharacterBody victim, SkillRow row, Vector3 aim)
        {
            if (!owner || !victim || !NetworkServer.active || victim.GetComponent<WraithThrown>()) return;
            var t = victim.gameObject.AddComponent<WraithThrown>();
            t.owner = owner; t.body = victim; t.row = row;
            aim.y = 0f;
            t.flat = aim.sqrMagnitude > 0.01f ? aim.normalized : owner.transform.forward;
            t.boss = victim.isChampion;
            t.start = Time.fixedTime;
            float height = row.LaunchHeight * (t.boss ? row.BossMult : 1f);
            t.riseSpeed = height / Mathf.Max(0.05f, row.MinDuration);
            t.throwAt = t.start + row.Interval;
            t.GravityOff(true);
            if (victim.characterMotor) victim.characterMotor.Motor.ForceUnground();
            // held and stunned until it lands (bosses get their own stun at the throw)
            Fx.Stun(victim.healthComponent, row.Interval + 2f);
            if (MeltyTest.Active)
            {
                var esm = EntityStateMachine.FindByCustomName(victim.gameObject, "Body");
                Log.Info($"telekinesis: {victim.name} lifted, body state {(esm ? esm.state?.GetType().Name : "none")}");
            }
            Relay.Vfx(row.Vfx, "loop", victim.corePosition, row.Interval + 0.3f, victim.gameObject);
        }

        void GravityOff(bool off)
        {
            var m = body ? body.characterMotor : null;
            if (!m || antiGravity == off) return;
            var gp = m.gravityParameters; gp.channeledAntiGravityGranterCount += off ? 1 : -1; m.gravityParameters = gp;
            antiGravity = off;
        }

        void Drive(Vector3 v)
        {
            if (body.characterMotor)
            {
                var m = body.characterMotor;
                m.velocity = v; m.moveDirection = Vector3.zero;
                if (v.y > 0f && m.isGrounded) m.Motor.ForceUnground();
            }
            else if (body.rigidbody) body.rigidbody.velocity = v;
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active || !body || !body.healthComponent || !body.healthComponent.alive || !owner) { Destroy(this); return; }
            float now = Time.fixedTime, dt = Time.fixedDeltaTime;
            switch (phase)
            {
                case Phase.Rise:
                    Drive(Vector3.up * riseSpeed);
                    if (now >= start + row.MinDuration) phase = Phase.Hold;
                    break;
                case Phase.Hold:
                    Drive(Vector3.zero);
                    if (now >= throwAt)
                    {
                        if (boss) { Land(false); return; }
                        phase = Phase.Throw;
                        vy = ArcLift;
                        // stunned again for the flight (a stun from the moment it left the ground did not hold through the lift)
                        Fx.Stun(body.healthComponent, row.Reach / Mathf.Max(1f, row.Speed) + 1f);
                        Relay.Vfx(row.Vfx, "wave", body.corePosition, row.Reach / Mathf.Max(1f, row.Speed) + 0.6f, body.gameObject);
                    }
                    break;
                case Phase.Throw:
                {
                    vy -= ArcGravity * dt;
                    var v = flat * row.Speed + Vector3.up * vy;
                    // a wall (steep) or the floor ahead: it stops there
                    float r = Mathf.Clamp(body.radius * 0.8f, 0.3f, 1.5f);
                    if (Physics.SphereCast(body.corePosition, r, v.normalized, out var hit, v.magnitude * dt + 0.2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    {
                        Drive(Vector3.zero);
                        Land(hit.normal.y < 0.5f, hit.point);
                        return;
                    }
                    if (body.characterMotor && body.characterMotor.isGrounded && now > throwAt + 0.15f) { Land(false); return; }
                    Drive(v);
                    travelled += row.Speed * dt;
                    // flown its distance: from here it falls like anything else (off a cliff, a long way)
                    if (travelled >= row.Reach)
                    {
                        if (!body.characterMotor) { Land(false); return; }
                        GravityOff(false);
                        phase = Phase.Fall;
                        fallUntil = now + 4f;
                    }
                    break;
                }
                case Phase.Fall:
                    if (body.characterMotor && body.characterMotor.isGrounded || now >= fallUntil) Land(false);
                    break;
            }
        }

        void Land(bool wall, Vector3? at = null)
        {
            if (done) return;
            done = true;
            var p = at ?? body.footPosition;
            float coef = row.DamagePct / 100f * (wall ? row.AirMult : 1f);
            Fx.Hit(owner, body.mainHurtBox, coef, row.ProcCoefficient, body.corePosition, default, wall ? DamageColorIndex.WeakPoint : DamageColorIndex.Default);
            // a boss or a wall hit: the full stun; a landing: a moment to get up
            Fx.Stun(body.healthComponent, boss || wall ? row.StunDuration : 1f);
            Relay.Vfx(row.Vfx, wall ? "slam" : "hit", p, Mathf.Max(2f, body.radius * 2f));
            Relay.Sound(row.Id, "hit", p);
            MeltyTest.Report(wall ? "telekinesis-wall" : "telekinesis-land", true, owner.baseNameToken);
            if (MeltyTest.Active || DpsBench.Active) Log.Info($"telekinesis: {body.name} {(boss ? "boss hit" : wall ? "hit a wall" : "landed")} after {travelled:F1} m");
            Destroy(this);
        }

        void OnDestroy() => GravityOff(false);
    }

    /// <summary>Full Auto's smoke on The Rake while the buff lasts, on every machine (the buff is synced; the server ends it when
    /// the magazine runs dry and the gun reloads).</summary>
    public sealed class WraithAura : MonoBehaviour
    {
        CharacterBody body; GameObject fx; bool on;

        public static void Ensure(CharacterBody b)
        {
            if (b && !b.GetComponent<WraithAura>()) b.gameObject.AddComponent<WraithAura>();
        }

        void Awake() => body = GetComponent<CharacterBody>();

        void Update()
        {
            bool now = States.GunState.FullAutoOn(body);
            if (now == on) return;
            on = now;
            if (on)
            {
                var m = Fx.Muzzle(body);
                fx = Vfx.Play("wraith_fullauto", "loop", m ? m.position : body.corePosition, 1f, m ? m : body.transform);
            }
            else { Vfx.Stop(fx, 0.8f); fx = null; }
        }

        void OnDestroy() => Vfx.Stop(fx, 0.3f);
    }
}
