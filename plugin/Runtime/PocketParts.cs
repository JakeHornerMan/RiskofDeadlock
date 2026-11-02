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

        /// <summary>The body is coming down onto an enemy (Frog Form, creator 2026-11-02: "if they land on top of an enemy it still does
        /// the damage and stun aoe"): an enemy's hurtbox within StandReach under its feet. Characters never stand on each other, so
        /// the world-only <see cref="Grounded"/> misses it.</summary>
        public static bool OnEnemy(CharacterBody body)
        {
            if (!body) return false;
            var team = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None;
            foreach (var h in Physics.SphereCastAll(body.footPosition + Vector3.up * StandProbe, StandProbe, Vector3.down, StandReach,
                         LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide))
            {
                var hb = h.collider ? h.collider.GetComponent<HurtBox>() : null;
                var hc = hb ? hb.healthComponent : null;
                if (!hc || !hc.alive || !hc.body || hc.body == body) continue;
                if (hc.body.teamComponent && hc.body.teamComponent.teamIndex != team) return true;
            }
            return false;
        }

        /// <summary>OnEnemy's probe: a sphere this big from this high above the feet, swept this far down.</summary>
        const float StandProbe = 0.5f, StandReach = 0.9f;

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
            if (built != null) root.AddComponent<CaseLid>();
            root.AddComponent<CaseFloorGuard>();
            return root;
        }

        /// <summary>The Flying Cloak's coat: an effects prop of the hero's own coat alone (no Pocket inside it), playing its clip (Deadlock's
        /// lift_loop, the pose its cloak particle draws) on a loop, at the hero's size; null when the cache has none.</summary>
        public static GameObject CloakModel(string propId)
        {
            var built = propId != "none" ? PropModels.Build(propId) : null;
            if (built == null) return null;
            var clips = Sheet.Effect(propId)?.Clips ?? new string[0];
            if (built.Anim && clips.Length > 0 && clips[0] != "none" && built.Anim[clips[0]]) { built.Anim[clips[0]].wrapMode = WrapMode.Loop; built.Anim.Play(clips[0]); }
            built.Root.transform.localScale = Vector3.one * ModelBuilder.GlobalScale;
            return built.Root;
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

    /// <summary>The suitcase prop's lid (bone case_02, the front half, hinged at its bottom edge along the case's width): Open 0 shut,
    /// 1 swung wide; it eases there. The hinge and the way it opens are worked out from the mesh (the lid's vertices against the body's),
    /// so no axis is guessed.</summary>
    public sealed class CaseLid : MonoBehaviour
    {
        public float Open, Speed = 5f;
        Transform lid; Quaternion rest; Vector3 axis; float sign = 1f, cur; bool ready;

        void Start()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "case_02") lid = t;
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (!lid || !lid.parent || !smr || !smr.sharedMesh) return;
            rest = lid.localRotation;
            int li = System.Array.IndexOf(smr.bones, lid);
            var verts = smr.sharedMesh.vertices; var w = smr.sharedMesh.boneWeights;
            Vector3 cl = Vector3.zero, cb = Vector3.zero; int nl = 0, nb = 0;
            for (int i = 0; i < verts.Length && i < w.Length; i++)
            {
                var p = smr.transform.TransformPoint(verts[i]);
                if (w[i].boneIndex0 == li) { cl += p; nl++; } else { cb += p; nb++; }
            }
            if (nl == 0 || nb == 0) return;
            cl /= nl; cb /= nb;
            var hinge = lid.position; var worldAxis = transform.right;
            var moved = Quaternion.AngleAxis(10f, worldAxis) * (cl - hinge) + hinge - cl;
            sign = Vector3.Dot(moved, cl - cb) >= 0f ? 1f : -1f;
            axis = lid.parent.InverseTransformDirection(worldAxis);
            ready = true;
        }

        void LateUpdate()
        {
            if (!ready) return;
            cur = Mathf.MoveTowards(cur, Mathf.Clamp01(Open), Speed * Time.deltaTime);
            lid.localRotation = Quaternion.AngleAxis(sign * 105f * cur, axis) * rest;
        }

        public static void Set(GameObject caseRoot, float open, float speed = 5f)
        {
            var l = caseRoot ? caseRoot.GetComponent<CaseLid>() : null;
            if (l) { l.Open = open; l.Speed = speed; }
        }
    }

    /// <summary>Keeps a suitcase prop out of the floor (creator, 2026-11-02: "make the the suitcase is never clipping thorugh the
    /// floor"): after whatever moves it this frame (thrown, swelling, jolting, tilting, floating), its lowest point is held at
    /// least Clearance above the world under it, measured under its middle and its four bottom corners (slopes, steps).</summary>
    public sealed class CaseFloorGuard : MonoBehaviour
    {
        const float Clearance = 0.02f, Probe = 3f;
        Renderer[] rends;

        void Start() => rends = GetComponentsInChildren<Renderer>(true);

        void LateUpdate()
        {
            if (rends == null) return;
            bool any = false; var b = new Bounds();
            foreach (var r in rends)
            {
                if (!r || !r.enabled) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) return;
            float floor = float.MinValue, from = b.max.y + 0.5f, reach = b.size.y + 0.5f + Probe;
            foreach (var xz in new[] { new Vector2(b.center.x, b.center.z), new Vector2(b.min.x, b.min.z), new Vector2(b.min.x, b.max.z),
                                       new Vector2(b.max.x, b.min.z), new Vector2(b.max.x, b.max.z) })
                if (Physics.Raycast(new Vector3(xz.x, from, xz.y), Vector3.down, out var hit, reach, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    floor = Mathf.Max(floor, hit.point.y);
            if (floor == float.MinValue) return;
            float under = floor + Clearance - b.min.y;
            if (under > 0f) transform.position += Vector3.up * under;
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
        CharacterBody owner; SkillRow row; Vector3 from, to, mid, offset; float age, time; bool damage; GameObject vfx; int loop;
        CharacterBody follow;

        /// <param name="follow">the locked enemy: the burst lands on it (plus offset) wherever it has moved by then.</param>
        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 to, bool damage, CharacterBody follow = null, Vector3 offset = default)
        {
            var go = new GameObject("DlrBarrageBurst_" + row.Id);
            go.transform.position = from;
            var b = go.AddComponent<DlrBarrageBurst>();
            b.owner = owner; b.row = row; b.from = from; b.to = to; b.damage = damage; b.follow = follow; b.offset = offset;
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
            if (follow && follow.healthComponent && follow.healthComponent.alive)
            {
                var now = follow.footPosition + offset;
                mid += now - to; to = now;
            }
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

    /// <summary>Barrage's volley (v1.2.0a lock-on): on the hero, every machine, after the cast. chainCount bursts, one every interval
    /// from the briefcase at the locked enemy (scattered up to reach metres by the owner's seed, the same everywhere), each following it;
    /// a circle under the enemy and the briefcase's glow while it runs. A new cast replaces it.</summary>
    public sealed class BarrageVolley : MonoBehaviour
    {
        CharacterBody owner, target; SkillRow row; int seed, fired; float age; bool damage; Vector3 last;
        GameObject marker, aura;

        public static void Begin(CharacterBody owner, SkillRow row, HurtBox target, int seed, bool damage)
        {
            if (!owner || !target || !target.healthComponent) return;
            var old = owner.GetComponent<BarrageVolley>();
            if (old) Destroy(old);
            var v = owner.gameObject.AddComponent<BarrageVolley>();
            v.owner = owner; v.row = row; v.target = target.healthComponent.body; v.seed = seed; v.damage = damage;
            v.last = v.target ? v.target.footPosition : target.transform.position;
            v.marker = Vfx.Play(row.Vfx, "aim", v.last, row.Radius);
            v.aura = Vfx.Play(row.Vfx, "loop", owner.corePosition, 1f, PocketParts.Case(owner));
            // the briefcase stands open while it throws (Deadlock's barrage clips open its lid)
            var anim = DeadlockAnimator.Of(owner);
            if (anim) anim.HoldOpen(row.ChainCount * row.Interval + 0.4f);
        }

        bool Alive => target && target.healthComponent && target.healthComponent.alive;

        void Update()
        {
            if (Alive) last = target.footPosition;
            if (marker) marker.transform.position = last;
        }

        void FixedUpdate()
        {
            if (!owner) { Destroy(this); return; }
            age += Time.fixedDeltaTime;
            if (Alive) last = target.footPosition;
            if (fired < row.ChainCount && age >= fired * row.Interval)
            {
                var rng = new System.Random(seed + fired * 7919);
                fired++;
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Sqrt((float)rng.NextDouble()) * row.Reach;
                var off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                var from = PocketParts.CaseMouth(owner);
                DlrBarrageBurst.Launch(owner, row, from, last + off, damage, Alive ? target : null, off);
                Fx.Sound(row.Id, "fire", from);
                Vfx.Play(row.Vfx, "muzzle", from, 1f);
                var anim = DeadlockAnimator.Of(owner);
                if (anim) anim.Gesture("secondary_air");
            }
            if (fired >= row.ChainCount && age >= (row.ChainCount - 1) * row.Interval + 1.5f) Destroy(this);
        }

        void OnDestroy() { Vfx.Stop(marker, 0.1f); Vfx.Stop(aura, 0.4f); }
    }

    /// <summary>Flying Cloak's cloak: flies along its direction at speed m/s for duration, through enemies, hitting each one within
    /// radius once for damagePct (server); a wall stops it and it hangs there for the rest of its life. Pocket can teleport to it
    /// while it lives (<see cref="States.CloakPort"/>). One per owner on each machine.</summary>
    public sealed class DlrCloak : MonoBehaviour
    {
        static readonly Dictionary<CharacterBody, DlrCloak> live = new Dictionary<CharacterBody, DlrCloak>();
        CharacterBody owner; SkillRow row; Vector3 dir; float age; bool damage, stuck, ended;
        readonly HashSet<HealthComponent> hit = new HashSet<HealthComponent>();
        GameObject vfx, model; int loop;

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
            go.transform.rotation = Quaternion.LookRotation(c.dir);
            c.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform);
            if (c.vfx) c.vfx.transform.rotation = Quaternion.LookRotation(c.dir);
            // Deadlock draws the cloak as Pocket's own coat flying in his lift pose (its particle renders his model): so do we
            c.model = PocketParts.CloakModel(row.Extra);
            if (c.model)
            {
                c.model.transform.SetParent(go.transform, false);
                // the collar to the sky (creator, 2026-10-28: "the head shold be pointing towards the sky ... a 90 degree rotation")
                c.model.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                c.model.transform.localScale *= 1.3f;
                c.model.transform.localPosition = c.model.transform.localRotation * (Vector3.down * 1.1f);
            }
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
                    if (wall.normal.y > 0.6f)
                    {
                        // aimed into the floor: it skims along the ground instead of stopping there (it looked stuck, 2026-10-27)
                        var along = Vector3.ProjectOnPlane(dir, wall.normal);
                        dir = along.sqrMagnitude > 0.01f ? along.normalized : dir;
                        transform.position = pos + wall.normal * 0.15f + dir * step;
                    }
                    else
                    {
                        transform.position = pos + dir * wall.distance + wall.normal * 0.3f;
                        stuck = true;
                        Vfx.Play(row.Vfx, "stick", transform.position, 1f);
                    }
                }
                else transform.position = pos + dir * step;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.3f);
            if (vfx) vfx.transform.rotation = transform.rotation;
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

    /// <summary>Frog Form's opening, for show on every machine (creator, 2026-11-02: "the animation of the entrance of the turning into
    /// the frog does look bad"; picked "the case pops, the frog jumps out"): the suitcase flies from the thrower's hand in a low arc
    /// with one slow flip (or falls from the sky) and lands upright; it swells a little and jolts Jolts times, the lid peeking open
    /// each time, as if something inside wants out; at OpenAt of the opening the lid flips open in a green burst, and when the
    /// opening ends the carrier leaps out of it as the frog (FrogForm.StartHopping); the lid then shuts and the case pops away.</summary>
    public sealed class FrogOpening : MonoBehaviour
    {
        SkillRow row; Vector3 from, to, facing; float open, size, age, peekUntil; int jolts; bool landed, burst, shut, falling;
        GameObject suitcase;
        const float CaseScale = 1.6f, SwellTo = 1.5f, FlyPart = 0.3f, OpenAt = 0.75f, PeekTime = 0.12f, ShutAfter = 0.25f, PopAfter = 0.5f;
        const int Jolts = 3;

        public static FrogOpening Begin(SkillRow row, string casePropId, Vector3 from, Vector3 to, Vector3 facing, float openTime, float size)
        {
            var go = new GameObject("PocketFrogOpening");
            var o = go.AddComponent<FrogOpening>();
            o.row = row; o.from = from; o.to = to; o.facing = facing; o.open = Mathf.Max(0.6f, openTime); o.size = size;
            o.falling = from.y > to.y + 10f;
            o.suitcase = PocketParts.CaseProp(casePropId, from, facing, CaseScale, false);
            return o;
        }

        /// <summary>The form ended: anything of the opening still out goes.</summary>
        public void Finish()
        {
            if (suitcase) PocketParts.Pop(suitcase);
            suitcase = null;
            Destroy(gameObject, 0.3f);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (!suitcase) return;
            float tFly = open * FlyPart, tOpen = open * OpenAt;
            var upright = Quaternion.LookRotation(facing);
            if (age < tFly)
            {
                // the throw: a low arc and one slow flip (dropped from the sky: straight down, no flip)
                float k = age / tFly;
                var p = Vector3.Lerp(from, to, falling ? k * k : k) + Vector3.up * (falling ? 0f : Mathf.Sin(k * Mathf.PI) * 2f);
                suitcase.transform.SetPositionAndRotation(p, upright * Quaternion.Euler(falling ? 0f : k * 360f, 0f, 0f));
                return;
            }
            if (!landed)
            {
                landed = true;
                Vfx.Play(row.Vfx, "land", to, size);
                Fx.Sound(row.Id, "hit", to);
            }
            if (age < tOpen)
            {
                // it swells a little, and jolts Jolts times: a small hop and tilt, the lid peeking open
                float k = (age - tFly) / (tOpen - tFly);
                suitcase.transform.localScale = Vector3.one * CaseScale * Mathf.Lerp(1f, SwellTo, Mathf.SmoothStep(0f, 1f, k)) * ModelBuilder.GlobalScale;
                float slot = k * Jolts;
                int n = Mathf.Min(Jolts - 1, Mathf.FloorToInt(slot));
                float j = slot - Mathf.Floor(slot);
                if (n >= jolts && j > 0.5f)
                {
                    jolts = n + 1;
                    peekUntil = age + PeekTime;
                    CaseLid.Set(suitcase, 0.3f, 10f);
                    Vfx.Play(row.Vfx, "jolt", to, size);
                }
                if (peekUntil > 0f && age >= peekUntil) { peekUntil = 0f; CaseLid.Set(suitcase, 0f, 10f); }
                float hop = j > 0.5f ? Mathf.Sin((j - 0.5f) * 2f * Mathf.PI) : 0f;
                float tilt = hop * 8f * (n % 2 == 0 ? 1f : -1f);
                suitcase.transform.SetPositionAndRotation(to + Vector3.up * hop * 0.3f, upright * Quaternion.Euler(0f, 0f, tilt));
                return;
            }
            if (!burst)
            {
                // the lid flips open in a green burst: the frog is coming out
                burst = true;
                suitcase.transform.localScale = Vector3.one * CaseScale * SwellTo * ModelBuilder.GlobalScale;
                suitcase.transform.SetPositionAndRotation(to, upright);
                CaseLid.Set(suitcase, 1f, 12f);
                Vfx.Play(row.Vfx, "burst", to, size * 1.2f);
                Fx.Sound(row.Id, "end", to);
            }
            if (!shut && age >= open + ShutAfter) { shut = true; CaseLid.Set(suitcase, 0f, 6f); }
            if (age >= open + PopAfter)
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
        }
    }
}
