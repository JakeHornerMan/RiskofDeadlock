using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Pocket (v1.2.0a): the briefcase in his hand, the point under the crosshair his throws land on, his suitcase prop,
    /// and small helpers his states share.</summary>
    public static class PocketParts
    {
        /// <summary>The briefcase in his left hand (the model's suitcase bone), or the body's core for anyone without one.</summary>
        public static Transform Case(CharacterBody body)
        {
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            if (model)
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                    if (t.name == "suitcase") return t;
            return body ? (body.coreTransform ? body.coreTransform : body.transform) : null;
        }

        /// <summary>Where Barrage's bursts leave the open briefcase (a little above it).</summary>
        public static Vector3 CaseMouth(CharacterBody body)
        {
            var c = Case(body);
            return (c ? c.position : body.corePosition) + Vector3.up * 0.35f;
        }

        /// <summary>The point under the crosshair: the first enemy or surface along the aim (his own team is passed through), else
        /// the point range metres out dropped onto the ground under it.</summary>
        public static Vector3 AimPoint(CharacterBody body, float range)
        {
            var aim = body.inputBank ? body.inputBank.GetAimRay() : new Ray(body.corePosition, body.transform.forward);
            var hits = Physics.RaycastAll(aim, range, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var hb = h.collider.GetComponent<HurtBox>();
                if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == body.teamComponent.teamIndex)) continue;
                return hb && hb.healthComponent.body ? hb.healthComponent.body.footPosition : h.point;
            }
            return Floor(aim.GetPoint(range));
        }

        /// <summary>The ground under a point (up to 30 m below), or the point itself over nothing.</summary>
        public static Vector3 Floor(Vector3 at)
        {
            return Physics.Raycast(at + Vector3.up * 1f, Vector3.down, out var g, 31f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? g.point : at;
        }

        /// <summary>A body on the ground (a short ray under its feet), the same on every machine.</summary>
        public static bool Grounded(CharacterBody body)
        {
            return body && Physics.Raycast(body.footPosition + Vector3.up * 0.3f, Vector3.down, 0.7f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>A Body-machine move that keeps him walking (Flying Cloak's throw): on the owner, as the main state would.</summary>
        public static void KeepWalking(CharacterBody body, CharacterMotor motor, CharacterDirection dir, InputBankTest ib)
        {
            if (!body || !body.hasEffectiveAuthority || !motor || !ib) return;
            motor.moveDirection = ib.moveVector;
            if (dir)
            {
                var a = ib.aimDirection; a.y = 0f;
                if (a.sqrMagnitude > 0.01f) dir.moveVector = a.normalized;
            }
        }

        /// <summary>Deadlock's suitcase (an effects prop) at a spot, facing, scale times its own size; rattle plays its clip on a loop.
        /// Without the model a plain purple box stands in.</summary>
        public static GameObject CaseProp(string propId, Vector3 at, Vector3 facing, float scale, bool rattle)
        {
            var built = propId != "none" ? PropModels.Build(propId) : null;
            var root = built != null ? built.Root : StandInBox(new Color(0.32f, 0.2f, 0.45f), new Vector3(0.75f, 0.57f, 0.3f));
            facing.y = 0f;
            root.transform.SetPositionAndRotation(at, Quaternion.LookRotation(facing.sqrMagnitude > 0.01f ? facing.normalized : Vector3.forward));
            root.transform.localScale = Vector3.one * scale * ModelBuilder.GlobalScale;
            var clips = Sheet.Effect(propId)?.Clips ?? new string[0];
            if (built != null && built.Anim && clips.Length > 0 && clips[0] != "none" && built.Anim[clips[0]])
            {
                built.Anim[clips[0]].wrapMode = rattle ? WrapMode.Loop : WrapMode.Once;
                built.Anim.Play(clips[0]);
            }
            return root;
        }

        /// <summary>A prop pops away: it shrinks to nothing in a moment and is gone.</summary>
        public static void Pop(GameObject go, float time = 0.25f)
        {
            if (!go) return;
            var s = go.AddComponent<PocketShrink>();
            s.Time = time;
        }

        /// <summary>Deadlock's frog at a size: its width (about 3 m in Deadlock) scaled to size metres.</summary>
        public static float FrogScale(float size) => size / 3.04f;

        /// <summary>A green blob for the frog when its model isn't in the cache.</summary>
        public static GameObject FrogStandIn() => StandInBox(new Color(0.36f, 0.62f, 0.25f), new Vector3(2.4f, 0.9f, 2f));

        static GameObject StandInBox(Color c, Vector3 size)
        {
            var root = new GameObject("PocketStandIn");
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = size; box.transform.localPosition = Vector3.up * size.y * 0.5f;
            box.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = c };
            return root;
        }

        /// <summary>The Satchel's tendrils (a "tether" visual of pocket_satchel) to every enemy it drinks from.</summary>
        public static void SetTethers(GameObject tethers, Vector3 from, List<Vector3> to)
        {
            var t = tethers ? tethers.GetComponent<PocketTendrils>() : null;
            if (t) t.Set(from, to);
        }
    }

    /// <summary>Shrinks its object to nothing over Time, then destroys it.</summary>
    public sealed class PocketShrink : MonoBehaviour
    {
        public float Time = 0.25f;
        Vector3 from; float age;
        void Start() => from = transform.localScale;
        void Update()
        {
            age += UnityEngine.Time.deltaTime;
            transform.localScale = from * Mathf.Max(0f, 1f - age / Mathf.Max(0.01f, Time));
            if (age >= Time) Destroy(gameObject);
        }
    }

    /// <summary>One of Barrage's spirit bursts: lobbed from the briefcase to its point in a fixed arc (distance / speed seconds,
    /// 0.35 to 1.4), then a blast of radius: damagePct and a stunDuration stun to every enemy in it, and one stack of the row's buff
    /// (the damage amp) on Pocket when it hit anything (server). Flown on every machine; only the server's copy deals damage.</summary>
    public sealed class DlrBarrageBurst : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 from, to, mid; float age, time; bool damage; GameObject vfx; int loop;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 to, bool damage)
        {
            var go = new GameObject("DlrBarrageBurst_" + row.Id);
            go.transform.position = from;
            var b = go.AddComponent<DlrBarrageBurst>();
            b.owner = owner; b.row = row; b.from = from; b.to = to; b.damage = damage;
            float d = Vector3.Distance(from, to);
            b.time = Mathf.Clamp(d / Mathf.Max(1f, row.Speed), 0.35f, 1.4f);
            b.mid = (from + to) * 0.5f + Vector3.up * (1.5f + d * 0.3f);
            b.vfx = Vfx.Play(row.Vfx, "orb", from, 1f, go.transform);
            b.loop = Fx.Loop(row.Id, from);
        }

        void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            age += Time.fixedDeltaTime;
            float k = Mathf.Clamp01(age / time);
            // a quadratic arc through the raised middle point
            var p = Vector3.Lerp(Vector3.Lerp(from, mid, k), Vector3.Lerp(mid, to, k), k);
            transform.position = p;
            if (k >= 1f) Burst();
        }

        void Burst()
        {
            Vfx.Play(row.Vfx, "hit", to, row.Radius);
            Fx.Sound(row.Id, "hit", to);
            var enemies = Fx.Enemies(owner, to + Vector3.up * 0.5f, row.Radius);
            foreach (var hb in enemies)
            {
                var b = hb.healthComponent.body;
                if (b) Vfx.Play(row.Vfx, "stun", b.corePosition + Vector3.up * b.radius, 1f, b.transform);
            }
            if (enemies.Count > 0) Fx.Sound(row.Id, "end", to);
            if (damage)
            {
                var type = new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Secondary };
                foreach (var hb in enemies)
                {
                    Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, hb.transform.position, type);
                    Fx.Stun(hb.healthComponent, row.StunDuration);
                }
                var amp = Sheet.Buff(row.Buff);
                if (enemies.Count > 0 && amp != null)
                {
                    Buffs.AddTimed(owner, row.Buff, amp.Duration, (int)amp.MaxStacks);
                    MeltyTest.Report("barrage-amp", true, owner.baseNameToken);
                }
            }
            Destroy(gameObject);
        }

        void OnDestroy() { DeadlockAudio.Stop(loop); Vfx.Stop(vfx, 0.3f); }
    }

    /// <summary>Flying Cloak's cloak: flies along its direction at speed m/s for duration, through enemies, hitting each one within
    /// radius once for damagePct (server); a wall stops it and it hangs there for the rest of its life. Pocket can teleport to it
    /// while it lives (<see cref="States.CloakPort"/>). One per owner on each machine.</summary>
    public sealed class DlrCloak : MonoBehaviour
    {
        static readonly Dictionary<CharacterBody, DlrCloak> live = new Dictionary<CharacterBody, DlrCloak>();
        CharacterBody owner; SkillRow row; Vector3 dir; float age; bool damage, stuck, ended;
        readonly HashSet<HealthComponent> hit = new HashSet<HealthComponent>();
        GameObject vfx; int loop;

        /// <summary>The owner's cloak still flying on this machine, or null.</summary>
        public static DlrCloak Of(CharacterBody owner) => owner && live.TryGetValue(owner, out var c) && c && !c.ended ? c : null;

        public float Age => age;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, bool damage)
        {
            var old = Of(owner);
            if (old) old.End(false);
            var go = new GameObject("DlrCloak_" + row.Id);
            go.transform.position = from;
            var c = go.AddComponent<DlrCloak>();
            c.owner = owner; c.row = row; c.dir = dir.normalized; c.damage = damage;
            c.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform);
            if (c.vfx) c.vfx.transform.rotation = Quaternion.LookRotation(c.dir);
            c.loop = Fx.Loop(row.Id, from);
            live[owner] = c;
        }

        void FixedUpdate()
        {
            if (!owner || ended) { Destroy(gameObject); return; }
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (!stuck)
            {
                float step = row.Speed * dt;
                var pos = transform.position;
                if (Physics.SphereCast(pos, 0.4f, dir, out var wall, step, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                {
                    transform.position = pos + dir * wall.distance + wall.normal * 0.3f;
                    stuck = true;
                    Vfx.Play(row.Vfx, "stick", transform.position, 1f);
                }
                else transform.position = pos + dir * step;
            }
            if (vfx) vfx.transform.rotation = Quaternion.Slerp(vfx.transform.rotation, Quaternion.LookRotation(dir), 0.3f);
            foreach (var hb in Fx.Enemies(owner, transform.position, row.Radius))
            {
                if (!hit.Add(hb.healthComponent)) continue;
                var at = hb.healthComponent.body ? hb.healthComponent.body.corePosition : hb.transform.position;
                if (damage)
                    Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, at,
                        new DamageTypeCombo { damageType = DamageType.Generic, damageTypeExtended = DamageTypeExtended.Generic, damageSource = DamageSource.Utility });
                Vfx.Play(row.Vfx, "hit", at, 1f, null, transform.position);
                Fx.Sound(row.Id, "hit", at);
            }
            if (age >= row.Duration) End(true);
        }

        /// <summary>The cloak is gone: expired (its sound and a puff) or taken by the teleport.</summary>
        public void End(bool expired)
        {
            if (ended) return;
            ended = true;
            if (expired) { Fx.Sound(row.Id, "end", transform.position); Vfx.Play(row.Vfx, "end", transform.position, 1f); }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            ended = true;
            if (owner && live.TryGetValue(owner, out var c) && c == this) live.Remove(owner);
            DeadlockAudio.Stop(loop);
            Vfx.Stop(vfx, 0.5f);
        }
    }

    /// <summary>Frog Form's opening, for show on every machine: the suitcase flies from the thrower's hand (or falls from the sky) to
    /// its spot in the first third, swells to giant size, its lid swings open and a giant copy of Deadlock's frog rises out of it with
    /// its mouth opening wide by the end (the carrier leaps out of it then); then the frog sinks back, the case snaps shut and shrinks
    /// away.</summary>
    public sealed class FrogOpening : MonoBehaviour
    {
        SkillRow row; Vector3 from, to, facing; float open, size, age; bool landed, grew, rose, sank;
        GameObject suitcase, giant; Transform lid, jaw; Quaternion lidRest, jawRest;
        const float CaseScale = 1.6f, GrowTo = 4.5f;

        public static FrogOpening Begin(SkillRow row, string casePropId, Vector3 from, Vector3 to, Vector3 facing, float openTime, float size)
        {
            var go = new GameObject("PocketFrogOpening");
            var o = go.AddComponent<FrogOpening>();
            o.row = row; o.from = from; o.to = to; o.facing = facing; o.open = Mathf.Max(0.6f, openTime); o.size = size;
            o.suitcase = PocketParts.CaseProp(casePropId, from, facing, CaseScale, false);
            foreach (var t in o.suitcase.GetComponentsInChildren<Transform>(true)) if (t.name == "case_02") { o.lid = t; o.lidRest = t.localRotation; }
            return o;
        }

        /// <summary>The form ended: anything of the opening still out goes.</summary>
        public void Finish()
        {
            if (suitcase) PocketParts.Pop(suitcase);
            if (giant) PocketParts.Pop(giant);
            Destroy(gameObject, 0.3f);
        }

        void Update()
        {
            age += Time.deltaTime;
            float tFly = open * 0.33f, tGrow = open * 0.6f, tRise = open;
            if (suitcase)
            {
                if (age < tFly)
                {
                    float k = age / tFly;
                    var p = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * (from.y > to.y + 10f ? 0f : 2.5f);
                    suitcase.transform.position = p;
                    suitcase.transform.rotation = Quaternion.LookRotation(facing) * Quaternion.Euler(k * 540f, 0f, 0f);
                }
                else
                {
                    if (!landed)
                    {
                        landed = true;
                        suitcase.transform.SetPositionAndRotation(to, Quaternion.LookRotation(facing));
                        Vfx.Play(row.Vfx, "land", to, size);
                        Fx.Sound(row.Id, "hit", to);
                    }
                    if (age < tGrow)
                    {
                        float k = Mathf.SmoothStep(0f, 1f, (age - tFly) / (tGrow - tFly));
                        suitcase.transform.localScale = Vector3.one * Mathf.Lerp(CaseScale, CaseScale * GrowTo, k) * ModelBuilder.GlobalScale;
                    }
                    else if (!grew)
                    {
                        grew = true;
                        suitcase.transform.localScale = Vector3.one * CaseScale * GrowTo * ModelBuilder.GlobalScale;
                        Vfx.Play(row.Vfx, "grow", to, size * 2f);
                        var built = PropModels.Build(row.Extra);
                        giant = built != null ? built.Root : PocketParts.FrogStandIn();
                        giant.transform.SetPositionAndRotation(to + Vector3.down * size, Quaternion.LookRotation(-facing));
                        giant.transform.localScale = Vector3.one * PocketParts.FrogScale(size * 2.4f);
                        foreach (var t in giant.GetComponentsInChildren<Transform>(true)) if (t.name == "Jaw") { jaw = t; jawRest = t.localRotation; }
                        var clips = Sheet.Effect(row.Extra)?.Clips ?? new string[0];
                        if (built != null && built.Anim && clips.Length > 0 && built.Anim[clips[0]]) { built.Anim[clips[0]].wrapMode = WrapMode.Loop; built.Anim.Play(clips[0]); }
                    }
                }
            }
            if (lid && grew)
            {
                // the lid swings open while the frog rises, and shut again as it sinks
                float k = sank ? Mathf.Clamp01(1f - (age - tRise - open * 0.4f) / (open * 0.2f)) : Mathf.Clamp01((age - tGrow) / (open * 0.2f));
                lid.localRotation = lidRest * Quaternion.Euler(-110f * k, 0f, 0f);
            }
            if (giant)
            {
                float caseTop = 0.57f * CaseScale * GrowTo * ModelBuilder.GlobalScale * 0.6f;
                if (age < tRise)
                {
                    float k = Mathf.SmoothStep(0f, 1f, (age - tGrow) / (tRise - tGrow));
                    giant.transform.position = to + Vector3.up * Mathf.Lerp(-size, caseTop, k);
                    if (jaw) jaw.localRotation = jawRest * Quaternion.Euler(0f, 0f, -35f * Mathf.Clamp01((k - 0.4f) / 0.6f));
                }
                else
                {
                    if (!rose) { rose = true; Vfx.Play(row.Vfx, "mouth", to + Vector3.up * (caseTop + size * 0.4f), size); }
                    // hold the open mouth a moment, then sink back into the case
                    float k = Mathf.Clamp01((age - tRise - open * 0.15f) / (open * 0.35f));
                    giant.transform.position = to + Vector3.up * Mathf.Lerp(caseTop, -size, k);
                    if (jaw) jaw.localRotation = jawRest * Quaternion.Euler(0f, 0f, -35f * (1f - k));
                    if (k >= 1f) { sank = true; Destroy(giant); giant = null; }
                }
            }
            if (sank && suitcase && age > tRise + open * 0.75f)
            {
                PocketParts.Pop(suitcase, 0.35f);
                suitcase = null;
                Vfx.Play(row.Vfx, "end", to + Vector3.up * 0.5f, size);
                Destroy(gameObject, 0.5f);
            }
        }

        void OnDestroy()
        {
            if (suitcase) Destroy(suitcase);
            if (giant) Destroy(giant);
        }
    }
}
