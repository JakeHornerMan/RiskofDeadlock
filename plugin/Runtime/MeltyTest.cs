using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Melty test mode (recipe test.checks). Only active while %LOCALAPPDATA%\Melty\test\deadlock-of-rain.json exists:
    /// starts a run, plays every hero (gun, three abilities, ult charge from real damage, ult) against summoned Lemurians and
    /// measures what each skill does (damage, stun, burn, buffs, heal, movement: the skills sheet's expect column), checks the
    /// gun rig (gun in hand, aimed where the camera aims), levels up, checks stats and scale, and appends "pass name" / "fail name: why" lines to the checks file Melty gives.</summary>
    public sealed class MeltyTest : MonoBehaviour
    {
        const string Slug = "deadlock-of-rain";
        static string checksFile;
        static MeltyTest instance;
        static readonly HashSet<string> seen = new HashSet<string>();
        static readonly HashSet<string> written = new HashSet<string>();
        static CharacterBody fireBody;
        static float fireUntil;

        public static bool Active => checksFile != null;

        public static void Init()
        {
            try
            {
                var f = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Melty", "test", Slug + ".json");
                if (!File.Exists(f)) return;
                var text = File.ReadAllText(f);
                int i = text.IndexOf("\"checks\"", StringComparison.Ordinal);
                if (i < 0) return;
                int a = text.IndexOf('"', text.IndexOf(':', i) + 1), b = text.IndexOf('"', a + 1);
                checksFile = text.Substring(a + 1, b - a - 1).Replace("\\\\", "\\");
                Log.Info("Melty test mode: reporting to " + checksFile);
                var go = new GameObject("DeadlockOfRainMeltyTest");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<MeltyTest>();
            }
            catch (Exception e) { Log.Warn("Melty test mode off: " + e.Message); }
        }

        /// <summary>Called by the mod's own code paths whenever something happens (a gun fires, a skill state starts...).</summary>
        public static void Report(string what, bool ok, string who)
        {
            if (!Active) return;
            seen.Add(what + "|" + who);
            seen.Add(what);
        }

        public static bool WantsFire(CharacterBody b) => Active && b && b == fireBody && Time.time < fireUntil;

        static void Write(string name, bool pass, string why = null)
        {
            if (!Active || written.Contains(name)) return;
            written.Add(name);
            var line = pass ? "pass " + name : "fail " + name + ": " + (why ?? "not seen");
            Log.Info("Melty check: " + line);
            try { File.AppendAllText(checksFile, line + Environment.NewLine); } catch (Exception e) { Log.Warn(e.Message); }
        }

        IEnumerator Start()
        {
            // 1. content
            while (!RoR2Application.loadFinished) yield return null;
            Write("deadlock-content", DeadlockCache.Ready && Survivors.Bodies.Count == 4, $"cache ready={DeadlockCache.Ready}, heroes built={Survivors.Bodies.Count}");
            int listed = HeroRow.All.Count(h => Survivors.Defs.TryGetValue(h.Id, out var d) && SurvivorCatalog.FindSurvivorDefFromBody(d.bodyPrefab) != null);
            Write("survivors-listed", listed == 4, listed + "/4 heroes in the survivor catalog");
            if (Survivors.Bodies.Count == 0) yield break;

            // 2. start a single-player run as Haze
            yield return new WaitForSeconds(3f);
            var user = LocalUserManager.GetFirstLocalUser();
            while (user == null) { yield return null; user = LocalUserManager.GetFirstLocalUser(); }
            user.userProfile.SetSurvivorPreference(Survivors.Defs[HeroRow.All[0].Id]);
            RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode ClassicRun; host 0;\"");
            float t0 = Time.unscaledTime;
            while (!PreGameController.instance && Time.unscaledTime - t0 < 60f) yield return null;
            yield return new WaitForSeconds(3f);
            Shot("lobby");
            yield return new WaitForSeconds(0.5f);
            if (PreGameController.instance) RoR2.Console.instance.SubmitCmd(null, "pregame_start_run");
            t0 = Time.unscaledTime;
            while ((user.cachedBody == null || !Run.instance) && Time.unscaledTime - t0 < 120f) yield return null;
            if (!user.cachedBody) { foreach (var h in HeroRow.All) Write(h.Id + "-plays", false, "the run did not start"); yield break; }
            yield return new WaitForSeconds(4f);
            // leave the drop pod so the hero stands in the open
            var first = user.cachedBody;
            if (first && first.currentVehicle) first.currentVehicle.EjectPassenger(first.gameObject);
            yield return new WaitForSeconds(2f);
            if (DevShots) yield return Lineup();
            QuietStage();

            // 3. every hero
            var master = user.cachedMaster;
            var commandoCap = BodyCatalog.FindBodyPrefab("CommandoBody").GetComponent<CapsuleCollider>();
            bool scaleOk = true; string scaleWhy = "";
            bool levelOk = true; string levelWhy = "";
            bool aimOk = true, handOk = true, slotsOk = true; string aimWhy = "", handWhy = "", slotsWhy = "";
            string gunsWhy = "", abilitiesWhy = "", ultsWhy = "";
            int heroes = 0;
            foreach (var hero in HeroRow.All)
            {
                if (!Survivors.Bodies.ContainsKey(hero.Id)) { Write(hero.Id + "-plays", false, "hero not built"); continue; }
                var bodyName = Survivors.Bodies[hero.Id].name;
                if (Survivors.HeroOf(master.GetBody()) != hero.Id)
                {
                    master.TransformBody(bodyName);
                    yield return new WaitForSeconds(1.5f);
                }
                var body = master.GetBody();
                if (!body || Survivors.HeroOf(body) != hero.Id) { Write(hero.Id + "-plays", false, "could not become " + hero.DisplayName); continue; }
                body.healthComponent.godMode = true;
                var token = body.baseNameToken;
                // stand in the open, away from the drop pod
                if (hero == HeroRow.All[0])
                {
                    var away = body.footPosition + body.inputBank.aimDirection.normalized * 14f;
                    if (Physics.Raycast(away + Vector3.up * 30f, Vector3.down, out var gh, 80f, LayerIndex.world.mask)) TeleportHelper.TeleportBody(body, gh.point + Vector3.up * 0.5f, false);
                    yield return new WaitForSeconds(0.5f);
                    arena = body.footPosition;
                    arenaForward = body.inputBank.aimDirection; arenaForward.y = 0; arenaForward.Normalize();
                }

                // scale: capsule follows Deadlock's relative height, model uses the shared factor
                var cap = body.GetComponent<CapsuleCollider>();
                float want = commandoCap.height * hero.HeightScale;
                float mscale = body.modelLocator.modelTransform.localScale.y;
                if (Mathf.Abs(cap.height - want) > 0.05f || Mathf.Abs(mscale - ModelBuilder.GlobalScale) > 0.01f)
                { scaleOk = false; scaleWhy += $"{hero.Id}: capsule {cap.height:F2} want {want:F2}, model {mscale:F3} want {ModelBuilder.GlobalScale:F3}; "; }

                // gun rig: the gun stays in the hand and points where the camera aims (straight, 30 deg up, 30 deg down)
                var anim = DeadlockAnimator.Of(body);
                if (anim)
                {
                    anim.ResetDrift();
                    var fwd = body.characterDirection ? body.characterDirection.forward : body.transform.forward; fwd.y = 0; fwd.Normalize();
                    var right = Vector3.Cross(Vector3.up, fwd);
                    foreach (var pitch in new[] { 0f, 30f, -30f })
                    {
                        var dir = Quaternion.AngleAxis(-pitch, right) * fwd;
                        AimCamera(body, dir);
                        for (int f = 0; f < 20; f++) { body.inputBank.aimDirection = dir; yield return null; }
                        // what the player sees: the hero from behind, the gun on the crosshair
                        if (DevShots) Shot($"{hero.Id}_aim{pitch:+0;-0;0}_game");
                        body.inputBank.aimDirection = dir; yield return null;
                        float err = anim.AimError;
                        if (DevShots) { body.inputBank.aimDirection = dir; DevCam(body, $"{hero.Id}_aim{pitch:+0;-0;0}"); }
                        Log.Info($"Melty test: {hero.Id} aim {pitch:+0;-0;0} deg: gun is {err:F1} deg off the aim");
                        if (err < 0f || err > 8f) { aimOk = false; aimWhy += $"{hero.Id} aim {pitch:+0;-0;0}: gun {err:F1} deg off; "; }
                    }
                }
                else { aimOk = false; aimWhy += hero.Id + ": no animator; "; }

                // the ult starts every life locked
                var ultCtl = UltController.Of(body);
                bool lockedAtStart = ultCtl && ultCtl.Skill && ultCtl.Skill.stock == 0;

                // RoR2's four skills: gun, two abilities, and the ultimate on R (no extra slot)
                var sl = body.skillLocator;
                int skills = body.GetComponents<GenericSkill>().Count(g => g.skillDef);
                var rRow = Survivors.RowFor(sl.special);
                if (skills != 4 || rRow == null || rRow.Slot != "ult" || !ultCtl || ultCtl.Skill != sl.special)
                { slotsOk = false; slotsWhy += $"{hero.Id}: {skills} skills, R holds {(rRow != null ? rRow.Id : "nothing")}; "; }

                // every skill must measurably do what its sheet row's expect column says
                bool gun = false; int abilities = 0; bool ultFired = false;
                foreach (var slot in new[] { "primary", "secondary", "utility", "ult" })
                {
                    body = master.GetBody();
                    var row = Sheet.SkillOf(hero.Id, slot);
                    var result = new Effects();
                    yield return TestSkill(body, row, result);
                    var missing = result.Missing(row);
                    Log.Info($"Melty test: {row.Id}: {result} -> {(missing.Count == 0 ? "ok" : "MISSING " + string.Join(", ", missing))}");
                    bool ok = missing.Count == 0;
                    if (slot == "primary") { gun = ok; if (!ok) gunsWhy += $"{row.Id}: no {string.Join(", ", missing)}; "; }
                    else if (slot == "ult") { ultFired = ok; if (!ok) ultsWhy += $"{row.Id}: no {string.Join(", ", missing)}; "; }
                    else { if (ok) abilities++; else abilitiesWhy += $"{row.Id}: no {string.Join(", ", missing)}; "; }
                    if (slot == "ult")
                    {
                        if (!lockedAtStart) Write("ult-charges", false, hero.Id + ": ult was not locked at spawn");
                        if (!result.Charged) Write("ult-charges", false, hero.Id + ": damage did not charge the ult");
                    }
                }
                // a heal-back passive (Abrams' Infernal Resilience) gives part of a hit back over the next seconds
                bool passive = true;
                if (hero.DamageHealBack > 0f)
                {
                    body = master.GetBody();
                    var hc = body.healthComponent;
                    float healed = 0f;
                    System.Action<HealthComponent, float, ProcChainMask> onHeal = (h, amount, mask) => { if (h == hc) healed += amount; };
                    HealthComponent.onCharacterHealServer += onHeal;
                    hc.godMode = false;
                    hc.TakeDamage(new DamageInfo { damage = hc.fullHealth * 0.4f, position = body.corePosition, damageType = DamageType.NonLethal | DamageType.BypassArmor, procCoefficient = 0f });
                    hc.godMode = true;
                    yield return new WaitForSeconds(1.5f);
                    HealthComponent.onCharacterHealServer -= onHeal;
                    Log.Info($"Melty test: {hero.Id} passive heal-back: healed {healed:F0} in 1.5s");
                    passive = healed > 0f;
                }
                if (anim)
                {
                    Log.Info($"Melty test: {hero.Id} gun drift from the hand over all skills: {anim.GunDrift * 100f:F1} cm");
                    if (anim.GunDrift > 0.02f) { handOk = false; handWhy += $"{hero.Id}: gun moved {anim.GunDrift * 100f:F1} cm in the hand; "; }
                }

                bool plays = gun && abilities == 2 && ultFired && passive;
                Write(hero.Id + "-plays", plays, $"gun={gun}, abilities={abilities}/2, ult={ultFired}, passive={passive}");
                if (plays) heroes++;

                // level-up growth matches the sheet (no items in a fresh run)
                body = master.GetBody();
                var st = Sheet.Stats(hero.Id);
                float lvl = body.level;
                float wantHp = st.BaseMaxHealth + st.LevelMaxHealth * (lvl - 1f), wantDmg = st.BaseDamage + st.LevelDamage * (lvl - 1f);
                if (Mathf.Abs(body.maxHealth - wantHp) > 1.5f || Mathf.Abs(body.damage - wantDmg) > 0.3f)
                { levelOk = false; levelWhy += $"{hero.Id} lvl {lvl}: hp {body.maxHealth:F0} want {wantHp:F0}, dmg {body.damage:F1} want {wantDmg:F1}; "; }

                KillTargets();
                // grow a level before the next hero so later heroes are checked at higher levels
                TeamManager.instance.GiveTeamExperience(TeamIndex.Player, (ulong)Mathf.Max(100f, TeamManager.instance.GetTeamNextLevelExperience(TeamIndex.Player) - TeamManager.instance.GetTeamExperience(TeamIndex.Player) + 1f));
                yield return new WaitForSeconds(1f);
            }
            var lb = master.GetBody();
            Write("guns-fire", gunsWhy == "", gunsWhy);
            Write("abilities-fire", abilitiesWhy == "", abilitiesWhy);
            Write("abilities-work", abilitiesWhy == "", abilitiesWhy);
            Write("ult-charges", true);
            Write("ult-fires", ultsWhy == "", ultsWhy);
            Write("ults-work", ultsWhy == "", ultsWhy);
            Write("gun-aims", aimOk, aimWhy);
            Write("gun-in-hand", handOk, handWhy);
            Write("four-slots", slotsOk, slotsWhy);
            Write("level-stats", levelOk && lb && lb.level > 1, levelWhy == "" ? "level did not rise" : levelWhy);
            Write("hero-scale", scaleOk, scaleWhy);
            Log.Info($"Melty test finished: {heroes}/4 heroes: every skill did what its sheet expects; Deadlock sounds played: {DeadlockAudio.Played}");
            if (lb) lb.healthComponent.godMode = false;
        }

        /// <summary>Dev only: when %LOCALAPPDATA%/DeadlockOfRain/devshots exists, save a screenshot there.</summary>
        static void Shot(string name)
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "devshots");
            if (Directory.Exists(dir)) ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, name + ".png"));
        }

        static bool DevShots => Directory.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "devshots"));
        static CharacterBody devCommando;

        /// <summary>Dev only: Commando's character-select model and the four heroes in a row facing the camera.</summary>
        static IEnumerator Lineup()
        {
            var cam = Camera.main;
            if (!cam) yield break;
            // a clear spot 40 m above the ground, away from the pod: models stand on an invisible floor there
            var origin = cam.transform.position + Vector3.up * 40f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.position = origin; floor.transform.localScale = Vector3.one * 3f;
            var models = new List<GameObject>();
            var commando = SurvivorCatalog.FindSurvivorDefFromBody(BodyCatalog.FindBodyPrefab("CommandoBody"));
            var list = new List<GameObject> { commando ? commando.displayPrefab : null };
            foreach (var h in HeroRow.All) list.Add(Survivors.Defs.TryGetValue(h.Id, out var d) ? d.displayPrefab : null);
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i]) continue;
                var pos = origin + Vector3.right * (i - (list.Count - 1) / 2f) * 1.5f;
                var go = UnityEngine.Object.Instantiate(list[i], pos, Quaternion.LookRotation(Vector3.back));
                go.SetActive(true);
                models.Add(go);
            }
            yield return new WaitForSeconds(1.5f);
            var extra = new List<GameObject>();
            models.AddRange(extra);
            yield return null;
            var go2 = new GameObject("DevLineupCamera");
            var c = go2.AddComponent<Camera>();
            c.CopyFrom(cam);
            c.transform.position = origin + new Vector3(0, 1.4f, -7.5f);
            c.transform.rotation = Quaternion.LookRotation(origin + Vector3.up * 1.1f - c.transform.position);
            c.fieldOfView = 50f;
            var rt = new RenderTexture(1600, 900, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "devshots");
            File.WriteAllBytes(System.IO.Path.Combine(dir, "lineup_cam.png"), tex.EncodeToPNG());
            c.targetTexture = null; UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go2);
            foreach (var m in models) UnityEngine.Object.Destroy(m);
            UnityEngine.Object.Destroy(floor);
        }

        /// <summary>Dev only: a frozen Commando standing beside the hero, for comparing sizes in screenshots.</summary>
        static void DevCommando(CharacterBody hero)
        {
            if (!NetworkServer.active) return;
            var right = Vector3.Cross(Vector3.up, hero.inputBank.aimDirection).normalized;
            var at = hero.footPosition - right * 2.5f + hero.inputBank.aimDirection * 3f;
            if (devCommando && devCommando.master) devCommando.master.TrueKill();
            var prefab = MasterCatalog.FindMasterPrefab("CommandoMonsterMaster");
            if (!prefab) return;
            var m = new MasterSummon { masterPrefab = prefab, position = at, rotation = Quaternion.LookRotation(-hero.inputBank.aimDirection), teamIndexOverride = TeamIndex.Player, ignoreTeamMemberLimit = true }.Perform();
            if (!m) return;
            foreach (var ai in m.GetComponents<RoR2.CharacterAI.BaseAI>()) ai.enabled = false;
            devCommando = m.GetBody();
            if (devCommando) devCommando.healthComponent.godMode = true;
        }

        /// <summary>Dev only: render the hero from a front three-quarter view (for the listing's screenshots).</summary>
        static void DevCam(CharacterBody body, string name)
        {
            var cam = Camera.main;
            if (!cam || !body) return;
            var fwd = body.characterDirection ? body.characterDirection.forward : body.transform.forward; fwd.y = 0; fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            float h = body.GetComponent<CapsuleCollider>().height;
            var go = new GameObject("DevShowcaseCamera");
            var c = go.AddComponent<Camera>();
            c.CopyFrom(cam);
            c.transform.position = body.footPosition + fwd * 4.2f * Mathf.Max(1f, h / 1.8f) + right * 2.2f + Vector3.up * h * 0.9f;
            c.transform.rotation = Quaternion.LookRotation(body.footPosition + Vector3.up * h * 0.55f - c.transform.position);
            c.fieldOfView = 55f;
            var rt = new RenderTexture(1920, 1080, 24);
            c.targetTexture = rt; c.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
            RenderTexture.active = null;
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "devshots");
            File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            c.targetTexture = null; UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(tex);
        }

        static readonly List<CharacterBody> targets = new List<CharacterBody>();
        static Vector3 arena, arenaForward = Vector3.forward;

        /// <summary>Test mode only: stop the stage's monster spawns and remove the monsters already there, so the skill checks
        /// see only their own target Lemurians (wandering Wisps and Beetles used to soak up the shots).</summary>
        static void QuietStage()
        {
            if (!NetworkServer.active) return;
            foreach (var d in UnityEngine.Object.FindObjectsOfType<CombatDirector>()) d.enabled = false;
            foreach (var tc in TeamComponent.GetTeamMembers(TeamIndex.Monster).ToList())
                if (tc && tc.body && tc.body.master && !targets.Contains(tc.body)) tc.body.master.TrueKill();
        }

        static void KillTargets()
        {
            foreach (var t in targets) if (t && t.master) t.master.TrueKill();
            targets.Clear();
        }

        /// <summary>What one skill measurably did while it ran (polled every frame plus damage and heal events).</summary>
        sealed class Effects
        {
            public float Damage, Healed, Moved;
            public bool Stun, Burn, Cloak, Charged;
            public readonly HashSet<string> EnemyBuffs = new HashSet<string>(), SelfBuffs = new HashSet<string>();

            public List<string> Missing(SkillRow row)
            {
                var miss = new List<string>();
                foreach (var e in row.Expect)
                {
                    var kind = e.Split(':')[0];
                    var arg = e.Contains(":") ? e.Substring(e.IndexOf(':') + 1) : "";
                    bool ok =
                        kind == "enemy_damage" ? Damage > 0f :
                        kind == "enemy_stun" ? Stun :
                        kind == "enemy_burn" ? Burn :
                        kind == "enemy_debuff" ? EnemyBuffs.Contains(arg) :
                        kind == "self_buff" ? SelfBuffs.Contains(arg) :
                        kind == "self_heal" ? Healed > 0f :
                        kind == "self_move" ? Moved > 3f :
                        kind == "self_cloak" ? Cloak : false;
                    if (!ok) miss.Add(e);
                }
                return miss;
            }

            public override string ToString() =>
                $"damage {Damage:F0}, stun {Stun}, burn {Burn}, enemy buffs [{string.Join(",", EnemyBuffs)}], self buffs [{string.Join(",", SelfBuffs)}], healed {Healed:F0}, moved {Moved:F1} m, cloak {Cloak}";
        }

        /// <summary>Puts two sturdy Lemurians in front of the hero, aims the camera at the first, fires the skill through its
        /// real skill slot (the ult through its charge meter) and records everything it does to them and to the hero.</summary>
        IEnumerator TestSkill(CharacterBody body, SkillRow row, Effects fx)
        {
            KillTargets();
            QuietStage();
            // every skill starts from the same flat spot facing the same way (dashes and leaps carry the hero off it)
            if (arena != Vector3.zero)
            {
                TeleportHelper.TeleportBody(body, arena + Vector3.up * 0.5f, false);
                if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
                AimCamera(body, arenaForward);
                for (int f = 0; f < 15; f++) { body.inputBank.aimDirection = arenaForward; yield return null; }
            }
            var aim = body.inputBank.GetAimRay();
            var fwd = aim.direction; fwd.y = 0; fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            float d = row.Slot == "primary" || row.Kind == "ult_leap" ? 10f : 6f;
            foreach (var at in new[] { body.footPosition + fwd * d, body.footPosition + fwd * (d + 1.5f) + right * 2f })
            {
                var p = at;
                // ground just under the spot (from hip height, so trees and arches overhead are not picked)
                if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var g, 8f, LayerIndex.world.mask)) p = g.point;
                var t = Summon(body, p + Vector3.up * 0.5f);
                if (!t) continue;
                // sturdy enough to survive an ult, so stuns and debuffs can still be seen on them
                if (t.master && t.master.inventory) t.master.inventory.GiveItem(RoR2Content.Items.BoostHp, 200);
                targets.Add(t);
            }
            yield return new WaitForSeconds(1f);
            var victims = new HashSet<CharacterBody>(targets);
            Log.Info($"Melty test: {row.Id}: hero at {body.footPosition}, targets " + string.Join(", ", targets.Select(x => x && x.healthComponent && x.healthComponent.alive ? $"{Vector3.Distance(x.footPosition, body.footPosition):F1} m away" : "dead")));

            // aim the camera (and so the skill) at the first target
            if (targets.Count > 0 && targets[0])
            {
                var look = (targets[0].corePosition - body.inputBank.aimOrigin).normalized;
                AimCamera(body, look);
                for (int f = 0; f < 10; f++) { body.inputBank.aimDirection = look; yield return null; }
            }

            if (row.Expect.Contains("self_heal"))
            {
                // hurt the hero first so a heal can show (Abrams' Resilience also reads the damage taken)
                body.healthComponent.godMode = false;
                body.healthComponent.TakeDamage(new DamageInfo { damage = body.healthComponent.fullHealth * 0.4f, position = body.corePosition, damageType = DamageType.NonLethal | DamageType.BypassArmor, procCoefficient = 0f });
                body.healthComponent.godMode = true;
                yield return new WaitForSeconds(0.3f);
            }

            var others = new List<string>();
            System.Action<DamageReport> onDamage = r =>
            {
                if (r == null || !r.victimBody) return;
                if (r.attackerBody == body && victims.Contains(r.victimBody)) fx.Damage += r.damageDealt;
                else if (r.attackerBody == body || victims.Contains(r.victimBody))
                    others.Add($"{(r.attackerBody ? r.attackerBody.name : "?")}->{r.victimBody.name} {r.damageDealt:F0}");
            };
            System.Action<HealthComponent, float, ProcChainMask> onHeal = (hc, amount, mask) => { if (hc == body.healthComponent && amount > 0f) fx.Healed += amount; };
            GlobalEventManager.onServerDamageDealt += onDamage;
            HealthComponent.onCharacterHealServer += onHeal;
            var start = body.corePosition;
            try
            {
                if (row.Slot == "primary")
                {
                    fireBody = body; fireUntil = Time.time + 4f;
                    body.skillLocator.primary.ExecuteIfReady();
                }
                else if (row.Slot == "ult")
                {
                    var ult = UltController.Of(body);
                    if (ult) ult.Consume(); // earlier skills may have filled the meter already
                    float before = 0f;
                    if (targets.Count > 0 && targets[0]) Fx.Hit(body, targets[0].mainHurtBox, 1f, 1f, targets[0].corePosition);
                    yield return new WaitForSeconds(0.2f);
                    fx.Charged = ult && ult.Charge > before;
                    fx.Damage = 0f; // that charging hit is not the ult's
                    if (ult) ult.Add(ult.Required);
                    yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                    // R, through the real skill slot, like a player pressing it
                    body.skillLocator.special.ExecuteIfReady();
                }
                else
                {
                    var gs = row.Slot == "secondary" ? body.skillLocator.secondary : body.skillLocator.utility;
                    gs.Reset();
                    gs.ExecuteIfReady();
                }

                float window = row.Slot == "primary" ? 4f : Mathf.Clamp(row.Duration + 1.5f, 2.5f, 7f);
                float t0 = Time.time;
                int shots = 0;
                while (Time.time - t0 < window)
                {
                    // the player controller re-aims at the crosshair every Update; keep the aim on a live target
                    var aimAt = targets.FirstOrDefault(x => x && x.healthComponent && x.healthComponent.alive);
                    if (aimAt && body) body.inputBank.aimDirection = (aimAt.corePosition - body.inputBank.aimOrigin).normalized;
                    var b = body;
                    if (!b) break;
                    fx.Moved = Mathf.Max(fx.Moved, Vector3.Distance(start, b.corePosition));
                    if (b.hasCloakBuff) fx.Cloak = true;
                    foreach (var kv in Buffs.Defs) if (b.HasBuff(kv.Value)) fx.SelfBuffs.Add(kv.Key);
                    foreach (var t in targets)
                    {
                        if (!t || !t.healthComponent || !t.healthComponent.alive) continue;
                        foreach (var kv in Buffs.Defs) if (t.HasBuff(kv.Value)) fx.EnemyBuffs.Add(kv.Key);
                        var esm = EntityStateMachine.FindByCustomName(t.gameObject, "Body");
                        if (esm && esm.state is EntityStates.StunState) fx.Stun = true;
                        var dc = DotController.FindDotController(t.gameObject);
                        if (dc && dc.HasDotActive(DotController.DotIndex.Burn)) fx.Burn = true;
                    }
                    // dev screenshots through the skill: early, middle, late
                    if (shots < 3 && Time.time - t0 > new[] { 0.5f, 1.4f, 2.3f }[shots]) { Shot(shots == 0 ? row.Id : row.Id + "_" + (shots + 1)); shots++; }
                    yield return null;
                }
            }
            finally
            {
                GlobalEventManager.onServerDamageDealt -= onDamage;
                HealthComponent.onCharacterHealServer -= onHeal;
                fireBody = null;
            }
            if (others.Count > 0) Log.Info($"Melty test: {row.Id}: other damage seen: " + string.Join("; ", others.Take(8)));
            // let dashes, leaps and buffs finish before the next skill
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>Turns the player's camera (and with it the aim) to look along a direction: the camera mode keeps its
        /// pitch/yaw in per-rig instance data, which has SetPitchYawFromLookVector.</summary>
        static void AimCamera(CharacterBody body, Vector3 look)
        {
            foreach (var rig in CameraRigController.readOnlyInstancesList)
            {
                if (rig.targetBody != body || rig.cameraMode == null) continue;
                var dict = HarmonyLib.AccessTools.Field(typeof(RoR2.CameraModes.CameraModeBase), "camToRawInstanceData").GetValue(rig.cameraMode) as System.Collections.IDictionary;
                if (dict == null) continue;
                foreach (var data in dict.Values)
                {
                    var m = data?.GetType().GetMethod("SetPitchYawFromLookVector");
                    if (m != null) m.Invoke(data, new object[] { look });
                }
            }
        }

        static CharacterBody Summon(CharacterBody owner, Vector3 at)
        {
            if (!NetworkServer.active) return null;
            var prefab = MasterCatalog.FindMasterPrefab("LemurianMaster");
            if (!prefab) return null;
            var m = new MasterSummon { masterPrefab = prefab, position = at, rotation = Quaternion.identity, teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform();
            if (!m) return null;
            var ai = m.GetComponent<RoR2.CharacterAI.BaseAI>();
            if (ai) ai.enabled = false;
            return m.GetBody();
        }
    }
}
