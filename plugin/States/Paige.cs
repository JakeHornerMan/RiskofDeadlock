using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.States
{
    // ================================================================== Paige (2026-11-06, the creator's own DLL build only)
    // The bibliomancer, from the creator's own files (charactersFinal/paigeFiles): Left Click reads piercing spirit pages off her
    // book, Right Click conjures her wyvern breathing ghost fire on the floor, Shift drops her giant sword into a magic circle,
    // R sends a row of spectral knights, Q one giant green knight (any carrier). Passive Plot Armor: +25 % damage below 40 % health.
    // Every model, clip, texture and sound is Paige's own; the ghost look is RoR2's ghost material recoloured green (VfxPaige.cs).

    /// <summary>Paige's passive on her body: the passive buff (Plot Armor, +25 % damage) on while her health is below
    /// heroes.passiveBelowPct (server), and its glow while it is on (every machine).</summary>
    public sealed class PaigeRuntime : MonoBehaviour
    {
        CharacterBody body; HeroRow hero; GameObject glow; bool wasOn;

        public static bool Wanted(HeroRow h) => h != null && h.PassiveBelowPct > 0 && h.PassiveBuff != "none";

        void Awake() => body = GetComponent<CharacterBody>();
        void Start() => hero = Sheet.Hero(Survivors.HeroOf(body));

        void FixedUpdate()
        {
            if (!NetworkServer.active || hero == null || !body || !body.healthComponent) return;
            var hc = body.healthComponent;
            bool want = hc.alive && hc.combinedHealthFraction < hero.PassiveBelowPct / 100f;
            bool on = Buffs.Count(body, hero.PassiveBuff) > 0;
            if (want && !on) { Buffs.AddTimed(body, hero.PassiveBuff, 0f, 1); MeltyTest.Report("plot-armor", true, body.baseNameToken); }
            else if (!want && on) Buffs.Clear(body, hero.PassiveBuff);
        }

        void Update()
        {
            if (hero == null || !body) return;
            bool on = Buffs.Count(body, hero.PassiveBuff) > 0;
            if (on == wasOn) return;
            wasOn = on;
            if (on) { glow = Vfx.Play("paige_plot_armor", "loop", body.corePosition, 1f, body.transform); Fx.Sound("paige_plot_armor", "cast", body.corePosition); }
            else { Vfx.Stop(glow, 0.6f); glow = null; }
        }

        void OnDestroy() => Vfx.Stop(glow, 0.2f);

        /// <summary>The point on the ground an aimed Paige move lands on: under the enemy nearest the crosshair (within range), else the
        /// ground under the crosshair at most range metres away (flat). The bench's bot puts it on its pack's middle.</summary>
        public static Vector3 GroundTarget(CharacterBody body, Ray aim, float range)
        {
            if (DpsBench.PackCentre is Vector3 pc) return pc;
            // test mode starts the move before it turns to the new dummies: the nearest one in range
            if (MeltyTest.Active)
            {
                CharacterBody best = null; float bd = float.MaxValue;
                foreach (var hb in Fx.Enemies(body, body.corePosition, range))
                {
                    var eb = hb.healthComponent.body; if (!eb) continue;
                    float d = (eb.footPosition - body.footPosition).sqrMagnitude;
                    if (d < bd) { bd = d; best = eb; }
                }
                if (best) return Ground(best.footPosition);
            }
            var t = DlrState.AimTargetOf(body, range, 12f);
            if (t && t.healthComponent && t.healthComponent.body) return Ground(t.healthComponent.body.footPosition);
            var point = aim.origin + aim.direction * range;
            if (Physics.Raycast(aim, out var hit, range * 1.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) point = hit.point;
            var flat = point - body.footPosition; flat.y = 0f;
            if (flat.magnitude > range) point = body.footPosition + flat.normalized * range + Vector3.up * (point.y - body.footPosition.y);
            return Ground(point);
        }

        /// <summary>The ground straight below (or just above) a point; the point itself over a pit.</summary>
        public static Vector3 Ground(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var g, 200f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return g.point;
            return p;
        }

        public static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward; }
    }

    // ---------------------------------------------------------------- M1: Chapter's End
    /// <summary>The book reads a spirit page per shot: a glowing bolt (<see cref="PaigePage"/>) from the book toward the point under
    /// the crosshair at speed m/s that pierces every enemy it meets (each hit once), gone on the world or after range. Damage falls
    /// off with the distance flown (falloffStart to falloffEnd, down to falloffMinPct). Magazine, reload and timing are the shared gun's.</summary>
    [Kind("gun_page")]
    public sealed class GunPage : GunState
    {
        protected override bool Shoot(Ray aim, bool left)
        {
            var target = aim.origin + aim.direction * row.Range;
            if (Physics.Raycast(aim, out var hit, row.Range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore)) target = hit.point;
            var from = MuzzlePos(left);
            if (Physics.Linecast(aim.origin, from, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) from = aim.origin;
            var dir = target - from;
            if (dir.sqrMagnitude < 1f) dir = aim.direction;
            PaigePage.Launch(characterBody, row, from, dir.normalized, Server);
            return true;
        }
    }

    /// <summary>One spirit page in flight (every machine; only the server's copy deals damage).</summary>
    public sealed class PaigePage : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 dir, start; float travelled; bool damage; GameObject vfx;
        readonly HashSet<HealthComponent> struck = new HashSet<HealthComponent>();

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, bool damage)
        {
            if (!owner || row == null || dir.sqrMagnitude < 1e-6f) return;
            var go = new GameObject("PaigePage");
            go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(dir));
            var p = go.AddComponent<PaigePage>();
            p.owner = owner; p.row = row; p.dir = dir.normalized; p.start = from; p.damage = damage;
            p.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform);
            if (p.vfx) p.vfx.transform.rotation = go.transform.rotation;
        }

        void FixedUpdate()
        {
            if (!owner || row == null) { End(); return; }
            float step = row.Speed * Time.fixedDeltaTime;
            var pos = transform.position;
            var hits = Physics.SphereCastAll(pos, Mathf.Max(0.1f, row.Radius * 0.5f), dir, step, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (!hb)
                {
                    // the world stops it
                    var at = h.distance > 0f ? h.point : pos;
                    Vfx.Play(row.Vfx, "impact", at, 1f, null, at + h.normal);
                    End();
                    return;
                }
                var hc = hb.healthComponent;
                if (!hc || !hc.body || hc.body.teamComponent.teamIndex == owner.teamComponent.teamIndex || !struck.Add(hc)) continue;
                var p = h.distance > 0f ? h.point : hb.transform.position;
                Vfx.Play(row.Vfx, "hit", p, 1f, null, p - dir);
                Fx.Sound(row.Id, "hit", p);
                if (damage)
                    Fx.Hit(owner, hb, row.DamagePct / 100f * DlrGoo.Falloff(row, travelled + h.distance), row.ProcCoefficient, p,
                           new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Primary });
            }
            transform.position = pos + dir * step;
            travelled += step;
            if (travelled > row.Range) End();
        }

        void End() { Vfx.Stop(vfx, 0.6f); vfx = null; Destroy(gameObject); }
    }

    // ---------------------------------------------------------------- M2: Bookwyrm
    /// <summary>Paige opens the book (secondary clip) and after minDuration her wyvern appears on the ground target (at most range away,
    /// <see cref="PaigeRuntime.GroundTarget"/>) and flies reach metres along her aim's heading at speed, breathing ghost fire on the
    /// floor (<see cref="PaigeWyrm"/>). The target travels with the state so every machine flies the same wyvern.</summary>
    [Kind("bookwyrm")]
    public sealed class Bookwyrm : DlrState
    {
        Vector3 target, heading; bool cast;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            if (isAuthority) { target = PaigeRuntime.GroundTarget(characterBody, GetAimRay(), row.Range); heading = PaigeRuntime.Flat(GetAimRay().direction); }
            characterBody.SetAimTimer(1.5f);
            Sound("cast");
            if (anim) anim.Gesture("secondary", Mathf.Max(1f, anim.ClipLength("secondary") / Mathf.Max(0.2f, row.Duration)));
            var m = Fx.Muzzle(characterBody);
            Vfx.Play(row.Vfx, "cast", m ? m.position : characterBody.corePosition, 1f, m);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (!cast && fixedAge >= row.MinDuration)
            {
                cast = true;
                if (heading == Vector3.zero) heading = PaigeRuntime.Flat(ThrowDir);
                if (target == Vector3.zero) target = PaigeRuntime.GroundTarget(characterBody, GetAimRay(), row.Range);
                PaigeWyrm.Launch(characterBody, row, target, heading, Server);
                MeltyTest.Report("bookwyrm", true, characterBody.baseNameToken);
            }
            if (isAuthority && fixedAge >= row.Duration) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (!cast && row != null && Server) PaigeWyrm.Launch(characterBody, row, target != Vector3.zero ? target : characterBody.footPosition, heading != Vector3.zero ? heading : PaigeRuntime.Flat(ThrowDir), true);
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(target); writer.Write(heading); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); target = reader.ReadVector3(); heading = reader.ReadVector3(); }
    }

    /// <summary>The wyvern's flight (every machine): from 30 % of reach behind the target to 70 % past it, at speed m/s, a few metres
    /// over the ground. Server: everything it passes within radius takes damagePct once; the floor it breathes on burns for dotTime
    /// seconds (a patch every radius/2 metres): every interval each enemy within radius of a patch takes splashPct x interval
    /// (proc tickProc), once per tick however many patches overlap.</summary>
    public sealed class PaigeWyrm : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 from, to, heading; float flown, length, nextPatch, nextTick; bool damage, done;
        GameObject wyrm; int loop, burnLoop;
        readonly HashSet<HealthComponent> struck = new HashSet<HealthComponent>();
        readonly List<KeyValuePair<Vector3, float>> patches = new List<KeyValuePair<Vector3, float>>();
        public const float Height = 2.6f;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 target, Vector3 heading, bool damage)
        {
            if (!owner || row == null) return;
            var go = new GameObject("PaigeWyrm");
            var w = go.AddComponent<PaigeWyrm>();
            w.owner = owner; w.row = row; w.heading = heading; w.damage = damage;
            w.from = target - heading * row.Reach * 0.3f; w.to = target + heading * row.Reach * 0.7f;
            w.length = Vector3.Distance(w.from, w.to);
            go.transform.SetPositionAndRotation(w.from + Vector3.up * Height, Quaternion.LookRotation(heading));
            Vfx.Play(row.Vfx, "spawn", PaigeRuntime.Ground(w.from), row.Radius, null, w.to);
            w.wyrm = Vfx.Play(row.Vfx, "loop", go.transform.position, row.Radius, go.transform, w.to);
            w.loop = Fx.Loop(row.Id, go.transform.position);
            Fx.Sound(row.Id + "/burn", "cast", target);
            w.burnLoop = Fx.Loop(row.Id + "/burn", target);
        }

        void FixedUpdate()
        {
            if (!owner || row == null) { Destroy(gameObject); return; }
            float dt = Time.fixedDeltaTime, now = Time.fixedTime;
            if (!done)
            {
                flown += row.Speed * dt;
                var flat = Vector3.Lerp(from, to, Mathf.Clamp01(flown / Mathf.Max(0.1f, length)));
                var ground = PaigeRuntime.Ground(flat);
                transform.position = ground + Vector3.up * Height;
                // the wyvern burns the floor under its breath: a fire patch every half radius
                if (flown >= nextPatch)
                {
                    nextPatch = flown + row.Radius * 0.5f;
                    patches.Add(new KeyValuePair<Vector3, float>(ground, now + row.DotTime));
                    Vfx.Play(row.Vfx, "burn", ground, row.Radius * 0.6f, null, ground + heading, 0f, row.DotTime);
                }
                if (damage)
                    foreach (var hb in Fx.Enemies(owner, transform.position - Vector3.up * Height * 0.5f, row.Radius))
                    {
                        if (!struck.Add(hb.healthComponent)) continue;
                        var at = hb.healthComponent.body ? hb.healthComponent.body.corePosition : hb.transform.position;
                        Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, at);
                        Relay.Vfx(row.Vfx, "hit", at, 1f);
                        Relay.Sound(row.Id, "hit", at);
                    }
                if (flown >= length)
                {
                    done = true;
                    Fx.Sound(row.Id, "end", transform.position);
                    Vfx.Play(row.Vfx, "end", transform.position, row.Radius);
                    Vfx.Stop(wyrm, 0.4f); wyrm = null;
                    DeadlockAudio.Stop(loop); loop = 0;
                }
            }
            patches.RemoveAll(p => p.Value <= now);
            if (done && patches.Count == 0)
            {
                Fx.Sound(row.Id + "/burn", "end", from);
                Destroy(gameObject);
                return;
            }
            if (!damage || now < nextTick || patches.Count == 0) return;
            nextTick = now + row.Interval;
            var hit = new HashSet<HealthComponent>();
            foreach (var p in patches)
                foreach (var hb in Fx.Enemies(owner, p.Key, row.Radius))
                {
                    if (!hit.Add(hb.healthComponent)) continue;
                    var b = hb.healthComponent.body;
                    // only what stands on (or just above) the burning floor
                    if (b && b.footPosition.y - p.Key.y > 3f) continue;
                    Fx.Hit(owner, hb, row.SplashPct / 100f * row.Interval, row.TickProc, b ? b.corePosition : hb.transform.position, default, DamageColorIndex.Item);
                }
        }

        void OnDestroy() { Vfx.Stop(wyrm, 0.2f); DeadlockAudio.Stop(loop); DeadlockAudio.Stop(burnLoop); }
    }

    // ---------------------------------------------------------------- Shift: Captivating Read
    /// <summary>Paige raises her book (utility clip, the Excalibur cast) and marks a radius circle on the ground target: everything
    /// inside gets the buff (a 36 % slow) at once and the circle builds for duration seconds (<see cref="PaigeCircle"/>); then her
    /// giant sword slams into its centre: damagePct to everything inside and the debuff (immobilized, with RoR2's own Entangle root).</summary>
    [Kind("captivating_read")]
    public sealed class CaptivatingRead : DlrState
    {
        Vector3 target;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            if (isAuthority) target = PaigeRuntime.GroundTarget(characterBody, GetAimRay(), row.Range);
            characterBody.SetAimTimer(1.5f);
            Sound("cast");
            if (anim) anim.Gesture("utility", Mathf.Max(1f, anim.ClipLength("utility") / Mathf.Max(0.3f, row.Duration)));
            var m = Fx.Muzzle(characterBody);
            Vfx.Play(row.Vfx, "cast", m ? m.position : characterBody.corePosition, 1f, m);
            PaigeCircle.Plant(characterBody, row, target, Server);
            MeltyTest.Report("captivating-read", true, characterBody.baseNameToken);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && (row == null || fixedAge >= row.MinDuration)) outer.SetNextStateToMain();
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(target); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); target = reader.ReadVector3(); }
    }

    /// <summary>The Captivating Read circle (every machine; the server slows, hits and roots).</summary>
    public sealed class PaigeCircle : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; float age, nextSlow; bool damage, landed; GameObject circle; int loop;

        public static void Plant(CharacterBody owner, SkillRow row, Vector3 at, bool damage)
        {
            if (!owner || row == null) return;
            var go = new GameObject("PaigeCircle");
            go.transform.position = at;
            var c = go.AddComponent<PaigeCircle>();
            c.owner = owner; c.row = row; c.damage = damage;
            c.circle = Vfx.Play(row.Vfx, "loop", at, row.Radius, null, null, 0f, row.Duration);
            c.loop = Fx.Loop(row.Id, at);
        }

        void FixedUpdate()
        {
            if (!owner || row == null) { Destroy(gameObject); return; }
            age += Time.fixedDeltaTime;
            var at = transform.position;
            if (!landed && damage && age >= nextSlow)
            {
                nextSlow = age + 0.25f;
                foreach (var hb in Fx.Enemies(owner, at, row.Radius))
                    if (hb.healthComponent.body) Buffs.Apply(owner, hb.healthComponent.body, row.Buff);
            }
            if (!landed && age >= row.Duration)
            {
                landed = true;
                DeadlockAudio.Stop(loop); loop = 0;
                Fx.Sound(row.Id, "hit", at);
                Vfx.Play(row.Vfx, "hit", at, row.Radius);
                Vfx.Stop(circle, 0.8f); circle = null;
                if (damage)
                {
                    var root = Sheet.Buff(row.Debuff);
                    float rootTime = root != null ? root.Duration : 1f;
                    foreach (var hb in Fx.Enemies(owner, at, row.Radius))
                    {
                        var b = hb.healthComponent.body;
                        var p = b ? b.corePosition : hb.transform.position;
                        Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, p);
                        if (!b) continue;
                        Buffs.Apply(owner, b, row.Debuff);
                        b.AddTimedBuff(RoR2Content.Buffs.Entangle, rootTime);
                        Relay.Vfx(row.Vfx, "root", b.footPosition, b.radius, b.gameObject, null, 0f);
                        Relay.Sound(row.Id, "end", p);
                    }
                }
            }
            if (landed && age >= row.Duration + 1.5f) Destroy(gameObject);
        }

        void OnDestroy() { Vfx.Stop(circle, 0.2f); DeadlockAudio.Stop(loop); }
    }

    // ---------------------------------------------------------------- R: Rallying Charge / Q: The Green Knight
    /// <summary>Paige reads the charge (special_start clip, then special) and after minDuration her knights ride out
    /// (<see cref="PaigeKnights"/>). The heading travels with the state.</summary>
    [Kind("knight_charge")]
    public sealed class KnightCharge : DlrState
    {
        Vector3 heading; bool cast;

        public override void OnEnter()
        {
            base.OnEnter();
            if (row == null) return;
            if (isAuthority) heading = PaigeRuntime.Flat(GetAimRay().direction);
            characterBody.SetAimTimer(1.5f);
            Sound("cast");
            if (anim) anim.Gesture("special_start", Mathf.Max(1f, anim.ClipLength("special_start") / Mathf.Max(0.2f, row.MinDuration)));
            var m = Fx.Muzzle(characterBody);
            Vfx.Play(row.Vfx, "cast", m ? m.position : characterBody.corePosition, row.PropScale, m);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (row == null) { if (isAuthority) outer.SetNextStateToMain(); return; }
            if (!cast && fixedAge >= row.MinDuration)
            {
                cast = true;
                if (anim) anim.Gesture("special", 1f);
                PaigeKnights.Ride(characterBody, row, heading != Vector3.zero ? heading : PaigeRuntime.Flat(ThrowDir), Server);
            }
            if (isAuthority && fixedAge >= row.Duration) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (!cast && row != null && Server) PaigeKnights.Ride(characterBody, row, heading != Vector3.zero ? heading : PaigeRuntime.Flat(ThrowDir), true);
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(heading); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); heading = reader.ReadVector3(); }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    /// <summary>The Green Knight (Q, any carrier): the carrier reads the last chapter (Paige: her knights clips; anyone else just keeps
    /// going) and after minDuration one giant knight (propScale) rides out along the aim (<see cref="PaigeKnights"/>), its trample
    /// growing from damageMinPct to damagePct over its range.</summary>
    [Kind("ult_giant_knight")]
    public sealed class UltGiantKnight : UltState
    {
        Vector3 heading; bool cast;

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority) heading = PaigeRuntime.Flat(GetAimRay().direction);
            Sound("cast");
            if (anim && anim.HasClip("ult_start")) anim.Gesture("ult_start", Mathf.Max(1f, anim.ClipLength("ult_start") / Mathf.Max(0.2f, row.MinDuration)));
            Vfx.Play(row.Vfx, "cast", characterBody.corePosition, row.PropScale, transform);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!cast && fixedAge >= row.MinDuration)
            {
                cast = true;
                if (anim && anim.HasClip("ult_loop")) anim.Gesture("ult_loop", 1f);
                PaigeKnights.Ride(characterBody, row, heading != Vector3.zero ? heading : PaigeRuntime.Flat(ThrowDir), Server);
            }
            if (isAuthority && fixedAge >= row.Duration) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (!cast && Server) PaigeKnights.Ride(characterBody, row, heading != Vector3.zero ? heading : PaigeRuntime.Flat(ThrowDir), true);
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(heading); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); heading = reader.ReadVector3(); }
    }

    /// <summary>Knights riding out (every machine; the server tramples and heals): chainCount knights side by side, reach metres
    /// apart, starting 3 m behind the caster (× propScale), riding along the ground at speed for range metres. Each enemy within
    /// radius (× propScale for a giant one... radius is already the giant's) of any knight takes the trample once: damagePct, or for
    /// a row with damageMinPct the damage grows from damageMinPct to damagePct with the distance ridden; and a stunDuration stun.
    /// Each ally they pass heals healFraction of their max health once.</summary>
    public sealed class PaigeKnights : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 heading, side, origin; float ridden; bool damage;
        readonly List<GameObject> knights = new List<GameObject>();
        readonly List<float> offsets = new List<float>();
        readonly HashSet<HealthComponent> struck = new HashSet<HealthComponent>();
        int loop;

        public static void Ride(CharacterBody owner, SkillRow row, Vector3 heading, bool damage)
        {
            if (!owner || row == null) return;
            var go = new GameObject("PaigeKnights");
            var k = go.AddComponent<PaigeKnights>();
            k.owner = owner; k.row = row; k.heading = heading; k.damage = damage;
            k.side = Vector3.Cross(Vector3.up, heading).normalized;
            float scale = Mathf.Max(1f, row.PropScale);
            k.origin = owner.footPosition - heading * 3f * scale;
            int n = Mathf.Max(1, (int)row.ChainCount);
            for (int i = 0; i < n; i++)
            {
                float off = (i - (n - 1) * 0.5f) * row.Reach;
                k.offsets.Add(off);
                var at = PaigeRuntime.Ground(k.origin + k.side * off);
                k.knights.Add(Vfx.Play(row.Vfx, "knight", at, scale, null, at + heading));
            }
            Vfx.Play(row.Vfx, "ride", PaigeRuntime.Ground(k.origin), scale, null, k.origin + heading * row.Range);
            k.loop = Fx.Loop(row.Id, owner.footPosition);
            MeltyTest.Report("knights-ride", true, owner.baseNameToken);
        }

        void FixedUpdate()
        {
            if (!owner || row == null) { Destroy(gameObject); return; }
            ridden += row.Speed * Time.fixedDeltaTime;
            float scale = Mathf.Max(1f, row.PropScale);
            var centre = origin + heading * ridden;
            for (int i = 0; i < knights.Count; i++)
            {
                var at = PaigeRuntime.Ground(centre + side * offsets[i]);
                if (knights[i]) knights[i].transform.SetPositionAndRotation(at, Quaternion.LookRotation(heading));
                if (!damage) continue;
                var core = at + Vector3.up * 1.2f * scale;
                foreach (var hb in Fx.Enemies(owner, core, row.Radius))
                {
                    if (!struck.Add(hb.healthComponent)) continue;
                    var b = hb.healthComponent.body;
                    var p = b ? b.corePosition : hb.transform.position;
                    float pct = row.DamageMinPct > 0f ? Mathf.Lerp(row.DamageMinPct, row.DamagePct, Mathf.Clamp01(ridden / Mathf.Max(1f, row.Range))) : row.DamagePct;
                    Fx.Hit(owner, hb, pct / 100f, row.ProcCoefficient, p);
                    Fx.Stun(hb.healthComponent, row.StunDuration);
                    Relay.Vfx(row.Vfx, "hit", p, scale);
                    Relay.Sound(row.Id, "hit", p);
                }
                // allies they ride through are healed once
                if (row.HealFraction > 0f)
                    foreach (var tm in TeamComponent.GetTeamMembers(owner.teamComponent.teamIndex))
                    {
                        var ab = tm ? tm.body : null;
                        if (!ab || !ab.healthComponent || !ab.healthComponent.alive || (ab.corePosition - core).sqrMagnitude > row.Radius * row.Radius * 1.5f) continue;
                        if (!struck.Add(ab.healthComponent)) continue;
                        ab.healthComponent.HealFraction(row.HealFraction, default);
                        Relay.Vfx(row.Vfx, "heal", ab.corePosition, 1f, ab.gameObject);
                    }
            }
            if (ridden >= row.Range + 3f * scale)
            {
                foreach (var k in knights) Vfx.Play(row.Vfx, "fade", k ? k.transform.position : centre, scale);
                Fx.Sound(row.Id, "end", centre);
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            foreach (var k in knights) Vfx.Stop(k, 0.3f);
            DeadlockAudio.Stop(loop);
        }
    }
}
