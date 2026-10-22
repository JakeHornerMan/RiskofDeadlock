using System.Collections.Generic;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace DeadlockOfRain
{
    // Rat King's moving parts. Co-op as everywhere in the mod: each of these runs on every machine (started by the skill state
    // that runs everywhere) for its look and sound; only the server's copy deals damage, slows and stuns.

    /// <summary>Rat King's ammo belt (heroes splitMeshes): Deadlock models one belt per round count (ammo_full, ammo_18 ...
    /// ammo_01); one renderer shows the one for the rounds left, so the belt shortens shot by shot and fills along the reload.
    /// The rounds are the owner's (HeroRuntime.Ammo), so other players see a full belt.</summary>
    public sealed class AmmoBelt : MonoBehaviour
    {
        // in public fields: Instantiate copies them to every clone of the model
        public SkinnedMeshRenderer Renderer;
        public Mesh[] ByRounds;   // [0] empty ... [n] n rounds ... [last] the full belt
        CharacterBody body; HeroRuntime rt; SkillRow gun; int shown = -1;

        public static CharacterModel.RendererInfo Build(GameObject root, HeroAsset asset, Mesh[] parts, Dictionary<string, int> belt, Transform[] bones, Transform armature)
        {
            int max = 0;
            foreach (var k in belt.Keys) if (k.StartsWith("ammo_") && int.TryParse(k.Substring(5), out var n)) max = Mathf.Max(max, n);
            var by = new Mesh[max + 2];
            int first = -1;
            foreach (var kv in belt) { first = kv.Value; break; }
            var empty = new Mesh { name = "ammo_none" };
            empty.bindposes = parts[first].bindposes;
            by[0] = empty;
            for (int i = 1; i <= max; i++) by[i] = belt.TryGetValue($"ammo_{i:00}", out var s) ? parts[s] : by[i - 1];
            int full = belt.TryGetValue("ammo_full", out var f) ? f : first;
            by[max + 1] = parts[full];
            var go = new GameObject("DeadlockMesh_ammo_belt");
            go.transform.SetParent(root.transform, false);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = by[max + 1]; smr.bones = bones; smr.rootBone = armature; smr.updateWhenOffscreen = true;
            var def = asset.Materials[Mathf.Clamp(asset.Submeshes[full].Key, 0, asset.Materials.Length - 1)];
            var mat = ModelBuilder.MakeMaterial(asset, def);
            smr.sharedMaterial = mat;
            var b = root.AddComponent<AmmoBelt>();
            b.Renderer = smr; b.ByRounds = by;
            return new CharacterModel.RendererInfo { renderer = smr, defaultMaterial = mat, defaultShadowCastingMode = ShadowCastingMode.On, ignoreOverlays = false };
        }

        void LateUpdate()
        {
            if (!Renderer || ByRounds == null) return;
            if (!body) { var cm = GetComponent<CharacterModel>(); body = cm ? cm.body : null; if (body) { rt = body.GetComponent<HeroRuntime>(); gun = Survivors.RowFor(body.skillLocator ? body.skillLocator.primary : null); } }
            int full = ByRounds.Length - 1;
            int idx = full;
            if (rt != null && gun != null && body.hasEffectiveAuthority)
            {
                int clip = (int)gun.ClipSize;
                int rounds = rt.Reloading ? Mathf.RoundToInt(clip * rt.ReloadFraction) : rt.Ammo < 0 ? clip : rt.Ammo;
                // the belt models stop at 18 rounds; more than that shows the full belt
                idx = rounds >= full ? full : Mathf.Clamp(rounds, 0, full);
            }
            if (idx != shown) { shown = idx; Renderer.sharedMesh = ByRounds[idx]; }
        }
    }

    /// <summary>Royal Pestments, on every machine: a ring of running rats on the ground for duration, centred under Rat King
    /// (never in the air: the centre is the ground found below him, within range, and stays put over a drop with none).
    /// chainCount rats run in three lanes (0.4, 0.7, 1 x radius) in turn directions and speeds; a rat hides where the ground
    /// under its lane drops away. Now and then one darts in at an enemy inside and back (only for show). Server: every
    /// interval each enemy inside the short cylinder (radius, from the ground up to reach) is bitten for damagePct.</summary>
    public sealed class RatRing : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; float start, nextTick, nextDart, nextBiteSound; int loop;
        Vector3 center; bool ending; float endAt;
        List<Rat> rats; float[] angle, lane, turn, speed, dart; Vector3[] dartAt;
        GameObject vfx;

        public static void Begin(CharacterBody owner, SkillRow row)
        {
            foreach (var old in owner.GetComponents<RatRing>()) old.End();
            var r = owner.gameObject.AddComponent<RatRing>();
            r.owner = owner; r.row = row; r.start = Time.time;
            r.center = Ground(owner, row, owner.footPosition);
            int n = Mathf.Max(3, (int)row.ChainCount);
            r.rats = RatCrowd.Take(n);
            r.angle = new float[n]; r.lane = new float[n]; r.turn = new float[n]; r.speed = new float[n]; r.dart = new float[n]; r.dartAt = new Vector3[n];
            float[] lanes = { 0.4f, 0.7f, 1f };
            for (int i = 0; i < n; i++)
            {
                int l = i % 3;
                r.lane[i] = lanes[l] * row.Radius;
                r.turn[i] = l == 1 ? -1f : 1f;
                r.speed[i] = 6f + l * 1.5f + Random.Range(-0.5f, 0.5f);
                r.angle[i] = (i / 3) * Mathf.PI * 2f / Mathf.Ceil(n / 3f) + l * 0.4f;
                r.dart[i] = -1f;
            }
            r.loop = Fx.Loop(row.Id, owner.footPosition);
            r.vfx = Vfx.Play(row.Vfx, "loop", r.center, row.Radius);
            MeltyTest.Report("rat-ring", true, owner.baseNameToken);
        }

        /// <summary>The ground below the hero (within the row's range), or the last centre over a drop with none.</summary>
        static Vector3 Ground(CharacterBody owner, SkillRow row, Vector3 fallback)
        {
            var from = owner.footPosition + Vector3.up * 0.5f;
            return Physics.Raycast(from, Vector3.down, out var hit, row.Range + 0.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? hit.point : fallback;
        }

        void Update()
        {
            if (rats == null) return;
            float t = Time.time - start, dt = Time.deltaTime;
            if (!ending && (!owner || t >= row.Duration || !owner.healthComponent || !owner.healthComponent.alive)) End();
            if (owner && !ending) center = Ground(owner, row, center);
            if (vfx) vfx.transform.position = center;
            for (int i = 0; i < rats.Count; i++)
            {
                var r = rats[i];
                if (r == null || !r.Go) continue;
                float rad = lane[i];
                if (ending)
                {
                    // scatter outward, then sink into the ground
                    float e = Time.time - endAt;
                    var outDir = new Vector3(Mathf.Cos(angle[i]), 0, Mathf.Sin(angle[i]));
                    var p = center + outDir * (rad + e * 9f);
                    if (e < 0.55f) r.Run(p, outDir, 9f, center.y);
                    else r.Pose(r.Pos + Vector3.down * (e - 0.55f) * 1.5f, r.Go.transform.rotation, 0, 2f);
                    continue;
                }
                // pour out from under the cape over the first 0.4 s
                float grow = Mathf.Clamp01(t / 0.4f);
                angle[i] += turn[i] * speed[i] / Mathf.Max(1f, rad) * dt;
                var on = new Vector3(Mathf.Cos(angle[i]), 0, Mathf.Sin(angle[i]));
                var tangent = new Vector3(-on.z, 0, on.x) * turn[i];
                var at = center + on * rad * grow;
                if (dart[i] >= 0f)
                {
                    // darting in at an enemy and back (0.25 s each way)
                    float d = Time.time - dart[i];
                    float k = d < 0.25f ? d / 0.25f : Mathf.Max(0f, 1f - (d - 0.25f) / 0.25f);
                    if (d >= 0.5f) dart[i] = -1f;
                    at = Vector3.Lerp(at, dartAt[i], k * 0.85f);
                    tangent = d < 0.25f ? dartAt[i] - at : at - dartAt[i];
                }
                // a rat whose lane crosses a drop or a wall hides there rather than float or clip
                r.Run(at, grow < 1f ? on : tangent, grow < 1f ? 10f : speed[i], center.y + 0.5f, 1.5f);
            }
            if (ending)
            {
                if (Time.time - endAt > 1f) { RatCrowd.Give(rats); rats = null; Destroy(this); }
                return;
            }
            var inside = Inside();
            if (Time.time >= nextDart && inside.Count > 0)
            {
                nextDart = Time.time + 0.35f;
                int i = Random.Range(0, rats.Count);
                var e = inside[Random.Range(0, inside.Count)].healthComponent.body;
                if (e && dart[i] < 0f) { dart[i] = Time.time; dartAt[i] = e.footPosition; Vfx.Play(row.Vfx, "hit", e.corePosition, 1f); }
            }
            if (inside.Count > 0 && Time.time >= nextBiteSound) { nextBiteSound = Time.time + 0.5f; Fx.Sound(row.Id, "hit", center); }
        }

        /// <summary>Enemies inside the ring's cylinder: within radius (flat) of the centre, from the ground up to reach above it.</summary>
        List<HurtBox> Inside()
        {
            var list = Fx.Enemies(owner, center, row.Radius + row.Reach);
            list.RemoveAll(h =>
            {
                var b = h.healthComponent.body;
                var p = b ? b.footPosition : h.transform.position;
                var flat = p - center; float dy = flat.y; flat.y = 0;
                return flat.magnitude > row.Radius || dy < -1.5f || dy > row.Reach;
            });
            return list;
        }

        void FixedUpdate()
        {
            if (ending || !owner || !NetworkServer.active || Time.fixedTime < nextTick) return;
            nextTick = Time.fixedTime + row.Interval;
            foreach (var hb in Inside())
            {
                var b = hb.healthComponent.body;
                Fx.Hit(owner, hb, row.DamagePct / 100f, row.ProcCoefficient, b ? b.corePosition : hb.transform.position);
            }
        }

        public void End()
        {
            if (ending) return;
            ending = true; endAt = Time.time;
            DeadlockAudio.Stop(loop); loop = 0;
            if (row != null) { Fx.Sound(row.Id, "end", center); Vfx.Play(row.Vfx, "end", center, row.Radius); }
            Vfx.Stop(vfx, 0.5f); vfx = null;
        }

        void OnDestroy()
        {
            DeadlockAudio.Stop(loop);
            Vfx.Stop(vfx, 0.2f);
            if (rats != null) RatCrowd.Give(rats);
        }
    }

    /// <summary>Rat Swarm, on every machine, on whoever fired the item: buffStacks rats pour out from under them and race to
    /// enemies within range (fliers only if a rat can leap reach metres up to them). Each free rat takes the enemy with the
    /// fewest rats on it (the nearest on a tie), never more than chainCount: it runs there, leaps the last 2 m and latches on,
    /// biting every interval (server: damagePct, the carrier's damage) until that enemy dies, then drops off and picks again.
    /// Spare rats circle the carrier. At duration, or after 2 s with no enemy in range, they run home and sink away.</summary>
    public sealed class RatSwarm : MonoBehaviour
    {
        enum S { Pour, Wait, Seek, Leap, Latched, Drop, Home, Gone }
        sealed class SRat
        {
            public Rat R; public S State; public HealthComponent Target; public HurtBox Box; public Transform Anchor;
            public Vector3 Local, LocalNormal, From, To; public float T0, NextBite, Angle, Delay; public bool Spent;
        }

        CharacterBody owner; SkillRow row; float start, nextPick, nextBiteSound, idleSince; int loop; bool home;
        readonly List<SRat> rats = new List<SRat>();
        readonly Dictionary<HealthComponent, int> onTarget = new Dictionary<HealthComponent, int>();
        List<HurtBox> enemies = new List<HurtBox>();
        public static int Bites, Spent;

        public static void Begin(CharacterBody owner, SkillRow row)
        {
            // no time limit: the rats stay with the carrier until each has bitten an enemy to death (or the stage ends);
            // using it again tops the pack back up to buffStacks
            var s = owner.GetComponent<RatSwarm>();
            if (s && s.home) s = null;
            if (!s)
            {
                s = owner.gameObject.AddComponent<RatSwarm>();
                s.owner = owner; s.row = row;
            }
            s.start = Time.time; s.idleSince = -1f;
            int have = 0;
            foreach (var r in s.rats) if (r.State != S.Gone) have++;
            int n = Mathf.Max(0, Mathf.Max(1, (int)row.BuffStacks) - have);
            foreach (var r in RatCrowd.Take(n))
                s.rats.Add(new SRat { R = r, State = S.Pour, Angle = Random.value * Mathf.PI * 2f, Delay = Random.value * 0.5f });
            if (s.loop == 0) s.loop = Fx.Loop(row.Id, owner.footPosition);
            Log.Info($"rat swarm: {n} rats out for {owner.GetUserName()} (server {NetworkServer.active}), {Bites} bites so far");
            MeltyTest.Report("rat-swarm", true, owner.baseNameToken);
        }

        Vector3 Home => owner ? owner.footPosition : (rats.Count > 0 ? rats[0].R.Pos : transform.position);

        void GoHome()
        {
            if (home) return;
            home = true;
            foreach (var r in rats) if (r.State != S.Gone) { Release(r); r.State = S.Home; }
            DeadlockAudio.Stop(loop); loop = 0;
            if (row != null) Fx.Sound(row.Id, "end", Home);
        }

        void Release(SRat r)
        {
            if (r.Target && onTarget.TryGetValue(r.Target, out var c)) onTarget[r.Target] = Mathf.Max(0, c - 1);
            r.Target = null; r.Box = null; r.Anchor = null;
        }

        /// <summary>The ground point under an enemy a rat runs to, and whether it can reach it (a flier no higher than reach).</summary>
        bool Reachable(HurtBox hb, out Vector3 foot)
        {
            var b = hb.healthComponent.body;
            foot = b ? b.footPosition : hb.transform.position;
            if (b && b.isFlying)
            {
                if (!Physics.Raycast(b.corePosition, Vector3.down, out var g, 60f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return false;
                if (b.corePosition.y - g.point.y > row.Reach) return false;
                foot = g.point;
            }
            return true;
        }

        /// <summary>Most rats on one enemy: chainCount, x bossMult on a boss.</summary>
        int Cap(HurtBox hb)
        {
            var b = hb.healthComponent.body;
            return Mathf.RoundToInt(row.ChainCount * (b && b.isChampion && row.BossMult > 0f ? row.BossMult : 1f));
        }

        void Pick()
        {
            enemies = owner ? Fx.Enemies(owner, owner.corePosition, row.SeekRange > 0 ? row.SeekRange : row.Range, 64) : new List<HurtBox>();
            enemies.RemoveAll(h => !Reachable(h, out _));
            foreach (var r in rats)
            {
                if (r.State != S.Wait) continue;
                HurtBox best = null; int fewest = int.MaxValue; float near = float.MaxValue;
                foreach (var hb in enemies)
                {
                    onTarget.TryGetValue(hb.healthComponent, out var c);
                    if (c >= Cap(hb)) continue;
                    float d = (hb.transform.position - r.R.Pos).sqrMagnitude;
                    if (c < fewest || c == fewest && d < near) { best = hb; fewest = c; near = d; }
                }
                if (!best) continue;
                r.Target = best.healthComponent; r.Box = best; r.State = S.Seek;
                onTarget.TryGetValue(best.healthComponent, out var n); onTarget[best.healthComponent] = n + 1;
            }
        }

        void Update()
        {
            float t = Time.time - start, dt = Time.deltaTime;
            if (!home && (!owner || !owner.healthComponent || !owner.healthComponent.alive)) GoHome();
            if (!home && Time.time >= nextPick) { nextPick = Time.time + 0.25f; Pick(); }
            var homeAt = Home;
            int alive = 0, busy = 0;
            foreach (var r in rats)
            {
                if (r.State == S.Gone || r.R == null || !r.R.Go) continue;
                alive++;
                var rat = r.R;
                // its enemy died with the rat on it: the rat drops off and is gone (spent)
                if ((r.State == S.Leap || r.State == S.Latched) && (!r.Target || !r.Target.alive))
                {
                    Release(r);
                    r.Spent = true; r.State = S.Drop; r.T0 = Time.time; r.From = rat.Pos;
                }
                // not on it yet, or it went away alive: back to the carrier to wait for another
                else if ((r.State == S.Seek || r.State == S.Leap || r.State == S.Latched) && (!r.Target || !r.Target.alive || !r.Box
                         || r.State == S.Seek && owner && (r.Box.transform.position - owner.corePosition).sqrMagnitude > Sq(1.5f * (row.SeekRange > 0 ? row.SeekRange : row.Range))))
                {
                    Release(r);
                    r.State = r.State == S.Seek ? S.Wait : S.Drop; r.T0 = Time.time; r.From = rat.Pos;
                }
                if (r.State == S.Seek || r.State == S.Leap || r.State == S.Latched) busy++;
                switch (r.State)
                {
                    case S.Pour:
                    {
                        // out from under the carrier's cape over 0.5 s, to a circle around them
                        float k = Mathf.Clamp01((t - r.Delay) / 0.35f);
                        if (t < r.Delay) { rat.Show(false); break; }
                        var on = new Vector3(Mathf.Cos(r.Angle), 0, Mathf.Sin(r.Angle));
                        rat.Run(homeAt + on * (0.3f + 2.2f * k), on, 9f, homeAt.y + 0.5f);
                        if (k >= 1f) r.State = S.Wait;
                        break;
                    }
                    case S.Wait:
                    {
                        // circle the carrier until a spot frees up
                        r.Angle += dt * 6f / 2.5f;
                        var on = new Vector3(Mathf.Cos(r.Angle), 0, Mathf.Sin(r.Angle));
                        rat.Run(homeAt + on * 2.5f, new Vector3(-on.z, 0, on.x), 6f, homeAt.y + 0.5f);
                        break;
                    }
                    case S.Seek:
                    {
                        Reachable(r.Box, out var foot);
                        var to = foot - rat.Pos; to.y = 0;
                        float reachR = r.Target.body ? r.Target.body.radius : 0.5f;
                        if (to.magnitude < 2f + reachR)
                        {
                            // the last 2 m: a leap onto a point on the enemy's hurtbox, facing the rat
                            r.State = S.Leap; r.T0 = Time.time; r.From = rat.Pos; rat.Phase = 0f;
                            var col = r.Box.collider;
                            var core = r.Target.body ? r.Target.body.corePosition : r.Box.transform.position;
                            var b = col ? col.bounds : new Bounds(core, Vector3.one);
                            var side = rat.Pos - core; side.y = 0; side = side.sqrMagnitude > 1e-4f ? side.normalized : Vector3.forward;
                            var probe = core + side * (b.extents.magnitude + 1f) + Vector3.up * Random.Range(-0.3f, 0.45f) * b.size.y;
                            var p = col ? col.ClosestPoint(probe) : core + side * reachR;
                            r.Anchor = r.Box.transform;
                            r.Local = r.Anchor.InverseTransformPoint(p);
                            r.LocalNormal = r.Anchor.InverseTransformDirection(side);
                            break;
                        }
                        rat.Run(rat.Pos + to.normalized * Mathf.Min(to.magnitude, row.Speed * dt), to, row.Speed, rat.Pos.y + 0.5f);
                        break;
                    }
                    case S.Leap:
                    {
                        float k = Mathf.Clamp01((Time.time - r.T0) / 0.3f);
                        var to = r.Anchor ? r.Anchor.TransformPoint(r.Local) : r.From;
                        var p = Vector3.Lerp(r.From, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                        var dir = to - r.From;
                        rat.Pose(p, Quaternion.LookRotation(dir.sqrMagnitude > 1e-4f ? dir : Vector3.forward), 1, 1.6f);
                        if (k >= 1f) { r.State = S.Latched; r.NextBite = Time.time + row.Interval * Random.Range(0.2f, 1f); }
                        break;
                    }
                    case S.Latched:
                    {
                        // clinging head-in, gnawing (the run clip, fast)
                        var p = r.Anchor.TransformPoint(r.Local);
                        var n = r.Anchor.TransformDirection(r.LocalNormal);
                        rat.Pose(p, Quaternion.LookRotation(-n, Vector3.up), 0, 2.4f);
                        if (Time.time >= r.NextBite)
                        {
                            r.NextBite += row.Interval;
                            Bite(r, p);
                        }
                        break;
                    }
                    case S.Drop:
                    {
                        // fall off to the ground, then free again
                        float k = Time.time - r.T0;
                        var p = r.From + Vector3.down * 0.5f * 30f * k * k;
                        bool down = false;
                        if (Physics.Raycast(r.From + Vector3.up, Vector3.down, out var g, 40f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) && p.y <= g.point.y) { p.y = g.point.y; down = true; }
                        else if (k > 1f) down = true;
                        rat.Pose(p, rat.Go.transform.rotation, 1, 1f);
                        if (down)
                        {
                            if (r.Spent) { Vfx.Play(row.Vfx, "hit", p, 0.6f); Gone(r); Spent++; }
                            else r.State = home ? S.Home : S.Wait;
                        }
                        break;
                    }
                    case S.Home:
                    {
                        var to = homeAt - rat.Pos; to.y = 0;
                        if (to.magnitude < 0.8f || !owner)
                        {
                            // under the cape: sink away
                            Gone(r);
                            break;
                        }
                        rat.Run(rat.Pos + to.normalized * Mathf.Min(to.magnitude, 14f * dt), to, 14f, rat.Pos.y + 0.5f);
                        break;
                    }
                }
            }
            // the scurrying loop only while rats are on the hunt
            if (busy > 0) { idleSince = -1f; if (loop == 0 && !home) loop = Fx.Loop(row.Id, homeAt); }
            else if (loop != 0)
            {
                if (idleSince < 0f) idleSince = Time.time;
                else if (Time.time - idleSince > 1.5f) { DeadlockAudio.Stop(loop); loop = 0; }
            }
            if (alive == 0 && t > 1f) Destroy(this);
        }

        static float Sq(float x) => x * x;

        /// <summary>A rat leaves for good: hidden and back to the pool at once.</summary>
        void Gone(SRat r)
        {
            r.State = S.Gone;
            if (r.R != null) { r.R.Show(false); RatCrowd.Give(new[] { r.R }); r.R = null; }
        }

        void Bite(SRat r, Vector3 at)
        {
            Vfx.Play(row.Vfx, "hit", at, 1f);
            if (Time.time >= nextBiteSound)
            {
                // a handful of bite sounds at most, not one per rat
                nextBiteSound = Time.time + 0.2f;
                Fx.Sound(row.Id, "tick", at);
            }
            if (NetworkServer.active && owner && r.Box)
            {
                Fx.Hit(owner, r.Box, row.DamagePct / 100f, row.ProcCoefficient, at);
                Bites++;
            }
        }

        void OnDestroy()
        {
            DeadlockAudio.Stop(loop);
            var list = new List<Rat>();
            foreach (var r in rats) if (r.R != null) list.Add(r.R);
            RatCrowd.Give(list);
        }
    }

    /// <summary>The rats bursting out of the ground across Rule, Ratannia!'s outer ring (only for show): each pops up out of the
    /// ground in a leap, runs outward and sinks away again.</summary>
    public sealed class RatBurst : MonoBehaviour
    {
        List<Rat> rats; Vector3[] at, dir; float start; Vector3 center;

        public static void Play(Vector3 center, float radius, int count)
        {
            var b = new GameObject("DeadlockRatBurst").AddComponent<RatBurst>();
            b.center = center; b.start = Time.time;
            b.rats = RatCrowd.Take(count);
            b.at = new Vector3[count]; b.dir = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.value * 0.6f) * Mathf.PI * 2f / count;
                b.dir[i] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                b.at[i] = center + b.dir[i] * radius * Random.Range(0.35f, 0.9f);
                b.rats[i].Phase = 0f;
            }
        }

        void Update()
        {
            float t = Time.time - start;
            for (int i = 0; i < rats.Count; i++)
            {
                var r = rats[i];
                if (r == null || !r.Go) continue;
                if (t < 0.35f)
                {
                    // out of the ground in a leap
                    float k = t / 0.35f;
                    var p = at[i] + dir[i] * k * 1.2f + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.4f - 0.3f * (1f - k));
                    r.Pose(p, Quaternion.LookRotation(dir[i] + Vector3.up * (1f - 2f * k)), 1, 1.2f);
                }
                else if (t < 1.3f) r.Run(at[i] + dir[i] * (1.2f + (t - 0.35f) * 8f), dir[i], 8f, center.y + 0.5f);
                else r.Pose(r.Pos + Vector3.down * (t - 1.3f) * 1.2f, r.Go.transform.rotation, 0, 2f);
            }
            if (t > 1.8f) Destroy(gameObject);
        }

        void OnDestroy() { if (rats != null) RatCrowd.Give(rats); }
    }

    /// <summary>Rule, Ratannia!'s banner planted where he lands (effects ratking_banner_prop: Deadlock's banner and the sewer
    /// diorama, rising with its own emerge clip, then its idle), for the row's chainCount seconds, then sinking away. Only for
    /// show. Without the model a simple gold pole and flag stand in.</summary>
    public sealed class RatBanner : MonoBehaviour
    {
        float until, sinkFrom; Animation anim; string idle;

        public static void Plant(SkillRow row, Vector3 at, Vector3 facing)
        {
            facing.y = 0;
            var rot = Quaternion.LookRotation(facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward);
            GameObject root;
            var built = PropModels.Build(row.Extra);
            var b = (built != null ? built.Root : StandIn()).AddComponent<RatBanner>();
            root = b.gameObject;
            root.transform.SetPositionAndRotation(at, rot);
            root.transform.localScale = Vector3.one * ModelBuilder.GlobalScale;
            b.until = Time.time + Mathf.Max(1f, row.ChainCount);
            var clips = Sheet.Effect(row.Extra)?.Clips ?? new string[0];
            if (built != null && built.Anim && clips.Length > 0 && clips[0] != "none")
            {
                b.anim = built.Anim;
                var emerge = clips[0];
                b.idle = clips.Length > 1 ? clips[1] : null;
                if (b.anim[emerge]) { b.anim[emerge].wrapMode = WrapMode.Once; b.anim.Play(emerge); }
                if (b.idle != null && b.anim[b.idle]) { b.anim[b.idle].wrapMode = WrapMode.Loop; b.anim.PlayQueued(b.idle); }
            }
            Vfx.Play(row.Vfx, "banner", at, row.Reach, root.transform);
        }

        static GameObject StandIn()
        {
            var root = new GameObject("DeadlockBannerStandIn");
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(pole.GetComponent<Collider>());
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(0.12f, 3.5f, 0.12f); pole.transform.localPosition = Vector3.up * 3.5f;
            pole.GetComponent<MeshRenderer>().sharedMaterial = new Material(ModelBuilder.Template) { color = new Color(0.35f, 0.25f, 0.15f) };
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(flag.GetComponent<Collider>());
            flag.transform.SetParent(root.transform, false);
            flag.transform.localScale = new Vector3(0.05f, 1.6f, 2.2f); flag.transform.localPosition = new Vector3(0, 6f, 1.15f);
            flag.GetComponent<MeshRenderer>().sharedMaterial = new Material(ModelBuilder.Template) { color = new Color(0.79f, 0.64f, 0.15f) };
            return root;
        }

        void Update()
        {
            if (Time.time < until) return;
            // sink into the ground over a second
            if (sinkFrom == 0f) sinkFrom = Time.time;
            transform.position += Vector3.down * Time.deltaTime * 6f * ModelBuilder.GlobalScale;
            if (Time.time - sinkFrom > 1.2f) Destroy(gameObject);
        }
    }

    /// <summary>The Scrap Grenade, on every machine: thrown along the aim at speed, falling under gravity, it blasts on the first
    /// enemy or surface it touches (radius, damagePct, the buff slow, a small shove). Before each blast but the last it pops
    /// up and hops about 4 m toward the nearest enemy within seekRange (straight up with none), so it stays in the fight; the
    /// chainCount-th landing sits for interval seconds, glowing and ticking, and goes off big (reach, splashPct, the debuff
    /// slow). The hop target is each machine's own nearest enemy (they agree but for a frame of lag); the server's copy deals
    /// the damage.</summary>
    public sealed class DlrScrap : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 vel; bool damage, sitting; int landed, ticks, loop; float age, ignoreUntil, sitAt;
        GameObject vfx;
        const float HopDistance = 4f, HopTime = 0.6f;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, bool damage)
        {
            var go = new GameObject("DlrScrap_" + row.Id);
            go.transform.position = from;
            if (MeltyTest.Active) Log.Info($"scrap: thrown from {from} along {dir.normalized} at {row.Speed} m/s (damage {damage})");
            var s = go.AddComponent<DlrScrap>();
            s.owner = owner; s.row = row; s.vel = dir.normalized * row.Speed; s.damage = damage;
            s.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform);
            s.loop = Fx.Loop(row.Id, from);
        }

        void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (sitting)
            {
                // the fuse: a tick at once, the final tick halfway, then the big blast
                float t = Time.fixedTime - sitAt;
                if (ticks == 0) { ticks = 1; Fx.Sound(row.Id, "tick", transform.position); }
                if (ticks == 1 && t >= row.Interval * 0.5f) { ticks = 2; Fx.Sound(row.Id, "tickFinal", transform.position); }
                if (t >= row.Interval) Final();
                return;
            }
            vel += Physics.gravity * dt;
            var pos = transform.position;
            var step = vel * dt;
            if (age >= ignoreUntil && step.sqrMagnitude > 1e-8f)
            {
                var hits = Physics.SphereCastAll(pos, 0.35f, step.normalized, step.magnitude, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    var hb = h.collider.GetComponent<HurtBox>();
                    if (hb && (!hb.healthComponent || !hb.healthComponent.body || hb.healthComponent.body.teamComponent.teamIndex == owner.teamComponent.teamIndex)) continue;
                    Land(h.distance > 0 ? h.point : pos, hb != null);
                    return;
                }
            }
            transform.position = pos + step;
            // never airborne forever (thrown off the map): it goes off where it is
            if (age > 6f) Final();
        }

        void Land(Vector3 at, bool onEnemy)
        {
            // on an enemy it drops to the floor under it before hopping on
            if (onEnemy) at = DlrLob.Floor(at);
            transform.position = at + Vector3.up * 0.25f;
            landed++;
            if (MeltyTest.Active) Log.Info($"scrap: landing {landed} at {at} (on an enemy: {onEnemy}) after {age:F2} s");
            if (landed >= row.ChainCount)
            {
                sitting = true; sitAt = Time.fixedTime; vel = Vector3.zero;
                Vfx.Play(row.Vfx, "sit", at, row.Reach, transform);
                return;
            }
            Blast(at, row.Radius, row.DamagePct, row.Buff, "hit");
            Hop(at);
        }

        void Hop(Vector3 at)
        {
            var seek = Fx.Enemies(owner, at, row.SeekRange, 1);
            float g = -Physics.gravity.y;
            Vector3 v;
            if (seek.Count > 0)
            {
                var b = seek[0].healthComponent.body;
                var p = b ? b.footPosition : seek[0].transform.position;
                var flat = p - at; flat.y = 0;
                var land = at + (flat.sqrMagnitude > 1e-4f ? flat.normalized * Mathf.Min(flat.magnitude, HopDistance) : Vector3.zero);
                land = DlrLob.Floor(land + Vector3.up * 2f);
                var d = land - at;
                v = new Vector3(d.x / HopTime, d.y / HopTime + 0.5f * g * HopTime, d.z / HopTime);
            }
            else v = Vector3.up * 0.5f * g * HopTime;
            vel = v;
            // one landing can't count twice: no hits for a moment after it pops up
            ignoreUntil = age + 0.12f;
            Fx.Sound(row.Id + "/ready", "hit", at);
        }

        void Blast(Vector3 at, float radius, float pct, string slow, string moment)
        {
            Vfx.Play(row.Vfx, moment, at, radius);
            Fx.Sound(row.Id, moment, at);
            if (MeltyTest.Active) Log.Info($"scrap: {moment} blast at {at}, {radius} m, {Fx.Enemies(owner, at + Vector3.up * 0.5f, radius).Count} enemies, damage {damage}");
            if (!damage) return;
            foreach (var hb in Fx.Enemies(owner, at + Vector3.up * 0.5f, radius))
            {
                var b = hb.healthComponent.body;
                Fx.Hit(owner, hb, pct / 100f, row.ProcCoefficient, b ? b.corePosition : hb.transform.position);
                if (!b) continue;
                Buffs.Apply(owner, b, slow);
                // small monsters are nudged back a step, not launched; bosses x bossMult
                Fx.KnockBack(owner, hb.healthComponent, b.corePosition - at, row.Knockback * (b.isChampion ? row.BossMult : 1f));
            }
        }

        void Final()
        {
            if (!this || !gameObject) return;
            var at = transform.position;
            Blast(at, row.Reach, row.SplashPct, row.Debuff, "end");
            MeltyTest.Report("scrap-blasts", true, owner ? owner.baseNameToken : "");
            Destroy(gameObject);
        }

        void OnDestroy() { DeadlockAudio.Stop(loop); Vfx.Stop(vfx, 0.2f); }
    }
}
