using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Wraith's four card tricks (Card Trick draws one per throw): Bleed and Burn put buffStacks stacks of RoR2's own DoT on
    /// for dotTime s, Crit makes the hit a certain critical hit, Stun stuns for stunDuration s.</summary>
    public enum CardSuit { Bleed = 0, Crit = 1, Stun = 2, Burn = 3 }

    /// <summary>Card Trick's card: flies on every machine (launched by the state that runs everywhere) and follows the owner's
    /// crosshair while it flies (the aim is synced to every machine with the body): it leaves the hand launchArcDeg off to the
    /// side and curves in, then steers at turnRate toward the enemy under the crosshair, or the locked target while the
    /// crosshair stays on it, or the point the crosshair is on (bending onto an enemy within seekRange on the way). A miss
    /// swings round in an arc and comes back. Only the server's copy (damage = true) deals the hit and its trick.</summary>
    public sealed class DlrCard : MonoBehaviour
    {
        CharacterBody owner; SkillRow row; Vector3 dir; HurtBox target, aimed; CardSuit suit; bool damage;
        float travelled, nextLook; int loop; GameObject vfx;
        /// <summary>The crosshair's cone (degrees) that picks the enemy the card goes for, and how far off the aim the locked
        /// target may drift before the crosshair takes over.</summary>
        const float AimCone = 8f, KeepLock = 20f;
        /// <summary>Within this many metres of its goal the card turns harder (see FixedUpdate).</summary>
        const float CloseIn = 8f;

        public static void Launch(CharacterBody owner, SkillRow row, Vector3 from, Vector3 dir, HurtBox target, CardSuit suit, bool damage)
        {
            // out of the hand at an angle (to the left, a little up), curving in toward where she aims
            var side = Vector3.Cross(Vector3.up, dir).normalized;
            var start = Quaternion.AngleAxis(-row.LaunchArcDeg, Vector3.up) * Quaternion.AngleAxis(-row.LaunchArcDeg * 0.3f, side) * dir;
            var go = new GameObject("DlrCard_" + row.Id);
            go.transform.position = from;
            go.transform.rotation = Quaternion.LookRotation(start);
            var c = go.AddComponent<DlrCard>();
            c.owner = owner; c.row = row; c.dir = start.normalized; c.target = target; c.suit = suit; c.damage = damage;
            c.loop = Fx.Loop(row.Id, from);
            // the trick's colour rides in the angle argument (Vfx.WraithCard)
            c.vfx = Vfx.Play(row.Vfx, "loop", from, row.Radius, go.transform, null, (float)suit);
        }

        bool Alive(HurtBox hb) => hb && hb.healthComponent && hb.healthComponent.alive && hb.healthComponent.body;

        /// <summary>The owner's aim right now (synced from its machine) and the enemy under it, if any.</summary>
        Ray Aim(out HurtBox under)
        {
            under = null;
            var ib = owner.inputBank;
            var ray = ib ? ib.GetAimRay() : new Ray(owner.corePosition, owner.transform.forward);
            var s = new BullseyeSearch
            {
                searchOrigin = ray.origin, searchDirection = ray.direction, maxAngleFilter = AimCone, maxDistanceFilter = row.Range,
                teamMaskFilter = TeamMask.GetEnemyTeams(owner.teamComponent.teamIndex), filterByLoS = true,
                sortMode = BullseyeSearch.SortMode.Angle,
            };
            s.RefreshCandidates();
            s.FilterOutGameObject(owner.gameObject);
            foreach (var hb in s.GetResults()) { under = hb; break; }
            return ray;
        }

        void FixedUpdate()
        {
            if (!owner || !owner.healthComponent || !owner.healthComponent.alive) { Done(); return; }
            float dt = Time.fixedDeltaTime;
            var pos = transform.position;
            Vector3 goal;
            var ray = Aim(out var under);
            if (Time.time >= nextLook)
            {
                nextLook = Time.time + 0.1f;
                aimed = under;
                // heading for the crosshair's point: it bends onto the first enemy within seekRange of it
                if (!Alive(aimed) && !(Alive(target) && Vector3.Angle(ray.direction, target.healthComponent.body.corePosition - ray.origin) <= KeepLock))
                {
                    var near = Fx.Enemies(owner, pos, row.SeekRange, 1);
                    target = near.Count > 0 ? near[0] : null;
                }
            }
            if (Alive(aimed)) { target = aimed; goal = aimed.healthComponent.body.corePosition; }
            else if (Alive(target) && Vector3.Angle(ray.direction, target.healthComponent.body.corePosition - ray.origin) <= KeepLock
                     || Alive(target) && Vector3.Distance(pos, target.healthComponent.body.corePosition) <= row.SeekRange)
                goal = target.healthComponent.body.corePosition;
            else
            {
                goal = Physics.Raycast(ray, out var wh, row.Range * 2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? wh.point : ray.origin + ray.direction * row.Range;
            }
            var want = goal - pos;
            // near its goal it turns three times as hard: otherwise an enemy inside its turning circle (speed / turnRate,
            // ~7 m) would be circled for ever instead of hit
            float turn = row.TurnRate * (want.sqrMagnitude < CloseIn * CloseIn ? 3f : 1f);
            if (want.sqrMagnitude > 0.01f)
                dir = Vector3.RotateTowards(dir, want.normalized, turn * Mathf.Deg2Rad * dt, 0f).normalized;
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
                    else { Fx.Sound(row.Id, "end", hit.point); Vfx.Play(row.Vfx, "end", hit.point, row.Radius, null, null, (float)suit); Splash(hit.point, null); }
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
            Splash(at, hb.healthComponent);
        }

        /// <summary>The trick splashes (creator, 2026-10-26): every other enemy within reach of where the card hit (an enemy or a
        /// wall) gets the same trick at full strength, but no damage: buffStacks of Bleed or Burn, the stun, or for Crit the debuff
        /// row (a mark: the next hit on it is a certain crit, Content/Buffs.cs). Every machine plays the suit's splash and a mark on
        /// each enemy it reaches; only the server's card applies the trick.</summary>
        void Splash(Vector3 at, HealthComponent skip)
        {
            if (row.Reach <= 0f) return;
            Vfx.Play(row.Vfx, "splash", at, row.Reach, null, null, (float)suit);
            int n = 0;
            foreach (var hb in Fx.Enemies(owner, at, row.Reach))
            {
                var hc = hb.healthComponent;
                if (!hc || hc == skip || !hc.body || !hc.alive) continue;
                n++;
                var b = hc.body;
                Vfx.Play(row.Vfx, "mark", b.corePosition, row.Radius, b.coreTransform ? b.coreTransform : b.transform, null, (float)suit);
                if (!damage) continue;
                int stacks = Mathf.Max(1, (int)row.BuffStacks);
                if (suit == CardSuit.Bleed) Buffs.Dot(owner, b, DotController.DotIndex.Bleed, row.DotTime, stacks);
                else if (suit == CardSuit.Burn) Buffs.Dot(owner, b, DotController.DotIndex.Burn, row.DotTime, stacks);
                else if (suit == CardSuit.Stun) Fx.Stun(hc, row.StunDuration);
                else if (suit == CardSuit.Crit && row.Debuff != "none") Buffs.AddTimed(b, row.Debuff, Sheet.Buff(row.Debuff)?.Duration ?? 6f);
            }
            if (damage && n > 0) MeltyTest.Report("card-splash", true, owner.baseNameToken);
            if (damage && MeltyTest.Active) Log.Info($"card trick: {suit} splashed onto {n} enemies within {row.Reach:F0} m");
        }

        void Done()
        {
            if (done) return;
            done = true;
            DeadlockAudio.Stop(loop);
            Vfx.Stop(vfx, 0.4f);
            Destroy(gameObject);
        }
        bool done;

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
            // her kill from here on, however it dies: a kill zone off a cliff pays the last attacker (OnOutOfBounds)
            Credit(victim.healthComponent, owner);
            if (victim.characterMotor) victim.characterMotor.Motor.ForceUnground();
            // held and stunned until it lands (bosses get their own stun at the throw)
            Fx.Stun(victim.healthComponent, row.Interval + 2f);
            if (MeltyTest.Active)
            {
                var esm = EntityStateMachine.FindByCustomName(victim.gameObject, "Body");
                Log.Info($"telekinesis: {victim.name} lifted, body state {(esm ? esm.state?.GetType().Name : "none")}");
            }
            // a beam from her hand to it while it rises
            Relay.Vfx(row.Vfx, "loop", victim.corePosition, row.Interval + 0.3f, victim.gameObject, owner.corePosition + Vector3.up * 0.3f);
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
                        // fully risen: thrown where she aims at this moment (her aim is synced to the server with her body)
                        var aim = owner.inputBank ? owner.inputBank.aimDirection : Vector3.zero;
                        aim.y = 0f;
                        if (aim.sqrMagnitude > 0.01f) flat = aim.normalized;
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

        static System.Reflection.MethodInfo lastHitSetter;
        /// <summary>Wraith becomes the enemy's last attacker (RoR2 gives a death's gold and XP to it).</summary>
        static void Credit(HealthComponent hc, CharacterBody owner)
        {
            if (!hc || !owner) return;
            if (lastHitSetter == null) lastHitSetter = HarmonyLib.AccessTools.PropertySetter(typeof(HealthComponent), nameof(HealthComponent.lastHitAttacker));
            try { lastHitSetter?.Invoke(hc, new object[] { owner.gameObject }); }
            catch (System.Exception e) { Log.Warn("telekinesis credit: " + e.Message); }
        }

        /// <summary>Server, hooked once: an enemy Telekinesis threw that falls into a kill zone (off a cliff, into the void) dies
        /// as Wraith's kill, so she gets its gold and XP even if someone else hit it last.</summary>
        public static void Init() => MapZone.onOOBPreDeathServer += OnOutOfBounds;

        static void OnOutOfBounds(CharacterBody body, MapZone zone)
        {
            var t = body ? body.GetComponent<WraithThrown>() : null;
            if (!t || !t.owner) return;
            Credit(body.healthComponent, t.owner);
            MeltyTest.Report("telekinesis-oob-credit", true, t.owner.baseNameToken);
            if (MeltyTest.Active || DpsBench.Active) Log.Info($"telekinesis: {body.name} thrown out of bounds, Wraith's kill");
        }
    }

    /// <summary>Card Trick's stored cards fanned above Wraith's head like a hand of cards, one per charge (Backup Magazine adds
    /// more), on every machine. Charges live on the owner: it relays the count when it changes (Relay.Cards) and the others
    /// draw what they were sent. Added by HeroRuntime to a hero with a card_trick skill.</summary>
    public sealed class WraithCardHalo : MonoBehaviour
    {
        CharacterBody body; Transform head;
        readonly System.Collections.Generic.List<GameObject> cards = new System.Collections.Generic.List<GameObject>();
        GameObject root;
        int count = -1, max = -1, sentCount = -1, sentMax = -1;
        /// <summary>The count drawn now (test mode reads it).</summary>
        public int Shown => count;
        /// <summary>How far above the head the fan sits (m), each card's size against Deadlock's card, the fan's step (degrees).</summary>
        const float Above = 0.35f, CardScale = 2.4f, FanStep = 17f, Pivot = 0.55f;

        void Awake() => body = GetComponent<CharacterBody>();

        /// <summary>The owner's count reached this machine.</summary>
        public void Set(int c, int m) { count = c; max = m; }

        void LateUpdate()
        {
            if (!body) return;
            var gs = body.skillLocator ? body.skillLocator.secondary : null;
            if (body.hasEffectiveAuthority && gs)
            {
                count = gs.stock; max = gs.maxStock;
                if (count != sentCount || max != sentMax) { sentCount = count; sentMax = max; Relay.Cards(body, count, max); }
            }
            else if (count < 0 && gs) { count = max = gs.maxStock; }
            if (!head)
            {
                var model = body.modelLocator ? body.modelLocator.modelTransform : null;
                if (!model) return;
                foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "head") { head = t; break; }
                if (!head) return;
            }
            if (!root) root = new GameObject("WraithCardHalo");
            bool visible = body.healthComponent && body.healthComponent.alive && head.gameObject.activeInHierarchy;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;
            while (cards.Count < count) { var c = MakeCard(); if (!c) break; cards.Add(c); }
            while (cards.Count > Mathf.Max(0, count)) { Vfx.Burst(cards[cards.Count - 1].transform.position, "mat_glow", new Color(0.8f, 0.62f, 1f), 3, 0.3f, 1f, 0.4f); Destroy(cards[cards.Count - 1]); cards.RemoveAt(cards.Count - 1); }
            // the fan faces the camera, centred over the head, bobbing gently
            var cam = Camera.main;
            var face = cam ? cam.transform.position - head.position : -body.transform.forward;
            face.y = 0f;
            if (face.sqrMagnitude < 0.01f) face = Vector3.forward;
            root.transform.position = head.position + Vector3.up * (Above + Mathf.Sin(Time.time * 2.2f) * 0.04f);
            root.transform.rotation = Quaternion.LookRotation(-face.normalized, Vector3.up);
            int n = cards.Count;
            for (int i = 0; i < n; i++)
            {
                float a = (i - (n - 1) * 0.5f) * FanStep;
                var t = cards[i].transform;
                t.localRotation = Quaternion.AngleAxis(-a, Vector3.forward);
                // a hand of cards: each turned about a point Pivot below the fan, so they spread at the top like held cards
                t.localPosition = t.localRotation * Vector3.up * Pivot + Vector3.forward * (i * 0.01f) - Vector3.up * Pivot;
            }
        }

        GameObject MakeCard()
        {
            var holder = new GameObject("card");
            holder.transform.SetParent(root.transform, false);
            var built = PropModels.Build("wraith_card_prop");
            if (built != null)
            {
                // the card's thin side faces the camera, whichever way the model was made
                var orient = built.Root.transform;
                orient.SetParent(holder.transform, false);
                var mf = built.Root.GetComponentInChildren<MeshFilter>();
                if (mf && mf.sharedMesh)
                {
                    var s = mf.sharedMesh.bounds.size;
                    var thin = s.x <= s.y && s.x <= s.z ? Vector3.right : s.y <= s.z ? Vector3.up : Vector3.forward;
                    var tall = s.x >= s.y && s.x >= s.z ? Vector3.right : s.y >= s.z ? Vector3.up : Vector3.forward;
                    orient.localRotation = Quaternion.Inverse(Quaternion.LookRotation(thin, tall));
                    orient.localPosition = -(orient.localRotation * mf.sharedMesh.bounds.center) * ModelBuilder.GlobalScale * CardScale;
                }
                orient.localScale = Vector3.one * ModelBuilder.GlobalScale * CardScale;
            }
            else
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(holder.transform, false);
                quad.transform.localScale = new Vector3(0.2f, 0.3f, 0.01f);
            }
            Vfx.Play("wraith_card", "halo", holder.transform.position, 0.2f, holder.transform);
            return holder;
        }

        void OnDestroy() { if (root) Destroy(root); }
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
