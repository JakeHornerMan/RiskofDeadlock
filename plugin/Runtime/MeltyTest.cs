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
    /// <summary>Melty test mode (recipe test.checks). Only active while %LOCALAPPDATA%\Melty\test\risk-of-deadlock-2.json (or deadlock-of-rain.json) exists:
    /// starts a run, plays every hero (gun, three abilities, the ult equipment it starts holding) against summoned Lemurians and
    /// measures what each skill does (damage, stun, burn, buffs, heal, movement: the skills sheet's expect column), checks the
    /// gun rig (gun in hand, aimed where the camera aims), levels up, checks stats and scale, and appends "pass name" / "fail name: why" lines to the checks file Melty gives.</summary>
    public sealed partial class MeltyTest : MonoBehaviour
    {
        /// <summary>Melty names the test file after the listing slug: "risk-of-deadlock-2" since the 0.6.3 rename; the old one stays for the local tools.</summary>
        static readonly string[] Slugs = { "risk-of-deadlock-2", "deadlock-of-rain" };
        static string checksFile;
        static MeltyTest instance;
        static readonly HashSet<string> seen = new HashSet<string>();
        static readonly HashSet<string> written = new HashSet<string>();
        static CharacterBody fireBody;
        static float fireUntil;
        /// <summary>The heroes this test run plays (test-mode file "heroes"); null = all of them.</summary>
        static List<string> TestHeroes;
        static HeroRow[] Tested => Survivors.Released.Where(h => (TestHeroes == null || TestHeroes.Contains(h.Id)) && !Broken.Contains(h.Id)).ToArray();
        /// <summary>Heroes the test marks unreadable on purpose (test-mode file "break": "infernus"): they must get a locked
        /// red ! tile while every other hero still builds.</summary>
        static readonly List<string> Broken = new List<string>();
        static string TestStage;
        static int Expected => Survivors.Released.Count(h => !Broken.Contains(h.Id));

        public static bool Active => checksFile != null;

        static List<string> offParts;
        /// <summary>Test-mode file "off": "audio,harmony" switches parts of the mod off, to find which one a problem needs
        /// (OFF=audio tools/quit_test.sh haze). Never true outside test mode.</summary>
        public static bool Off(string part)
        {
            if (offParts == null)
            {
                offParts = new List<string>();
                try
                {
                    var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Melty", "test");
                    var f = Slugs.Select(s => System.IO.Path.Combine(dir, s + ".json")).FirstOrDefault(File.Exists);
                    var m = f == null ? null : System.Text.RegularExpressions.Regex.Match(File.ReadAllText(f), "\"off\"\\s*:\\s*\"([^\"]*)\"");
                    if (m != null && m.Success) offParts = m.Groups[1].Value.Split(',').Select(p => p.Trim()).Where(p => p != "").ToList();
                    if (offParts.Count > 0) Log.Warn("Melty test mode: switched off " + string.Join(", ", offParts));
                }
                catch { }
            }
            return offParts.Contains(part);
        }

        public static void Init()
        {
            try
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Melty", "test");
                var f = Slugs.Select(s => System.IO.Path.Combine(dir, s + ".json")).FirstOrDefault(File.Exists);
                if (f == null) return;
                var text = File.ReadAllText(f);
                int i = text.IndexOf("\"checks\"", StringComparison.Ordinal);
                if (i < 0) return;
                int a = text.IndexOf('"', text.IndexOf(':', i) + 1), b = text.IndexOf('"', a + 1);
                checksFile = text.Substring(a + 1, b - a - 1).Replace("\\\\", "\\");
                // the co-op test ("coop": true): the copy started with a join= line joins, the other hosts
                Coop = System.Text.RegularExpressions.Regex.IsMatch(text, "\"coop\"\\s*:\\s*true");
                Vanilla = System.Text.RegularExpressions.Regex.IsMatch(text, "\"vanilla\"\\s*:\\s*true");
                Joiner = Coop && MeltyJoin.JoinAddress != "";
                // optional "heroes": "haze,abrams": the test (solo or co-op) plays only these
                var hm = System.Text.RegularExpressions.Regex.Match(text, "\"heroes\"\\s*:\\s*\"([^\"]*)\"");
                if (hm.Success) TestHeroes = hm.Groups[1].Value.Split(',').Select(h => h.Trim()).Where(h => h != "").ToList();
                // optional "stage": "blackbeach": the run moves to that stage before the heroes are tested (STAGE= in run_test.sh)
                var sm = System.Text.RegularExpressions.Regex.Match(text, "\"stage\"\\s*:\\s*\"([^\"]+)\"");
                if (sm.Success) TestStage = sm.Groups[1].Value.Trim();
                var bm = System.Text.RegularExpressions.Regex.Match(text, "\"break\"\\s*:\\s*\"([^\"]*)\"");
                if (bm.Success)
                    foreach (var id in bm.Groups[1].Value.Split(',').Select(h => h.Trim()).Where(h => h != ""))
                    {
                        Broken.Add(id);
                        DeadlockCache.MarkFailed(id, "broken on purpose by the test");
                    }
                if (Coop) CoopTestPatches.Apply(new HarmonyLib.Harmony(Plugin.Guid + ".cooptest"), Joiner);
                Log.Info("Melty test mode: reporting to " + checksFile + (Coop ? Joiner ? " (co-op test, joining)" : " (co-op test, hosting)" : ""));
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
            if (!Active || Joiner || written.Contains(name)) return;
            written.Add(name);
            var line = pass ? "pass " + name : "fail " + name + ": " + (why ?? "not seen");
            Log.Info("Melty check: " + line);
            try { File.AppendAllText(checksFile, line + Environment.NewLine); } catch (Exception e) { Log.Warn(e.Message); }
        }

        /// <summary>Test-mode "vanilla": true: no hero checks, just a normal Commando run for VanillaSeconds (the quit test's
        /// control: with OFF=mod nearly nothing of the mod is loaded).</summary>
        static bool Vanilla;
        const float VanillaSeconds = 150f;

        static IEnumerator VanillaRun()
        {
            yield return new WaitForSeconds(3f);
            var user = LocalUserManager.GetFirstLocalUser();
            while (user == null) { yield return null; user = LocalUserManager.GetFirstLocalUser(); }
            user.userProfile.SetSurvivorPreference(SurvivorCatalog.FindSurvivorDefFromBody(BodyCatalog.FindBodyPrefab("CommandoBody")));
            RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode ClassicRun; host 0;\"");
            float t0 = Time.unscaledTime;
            while (!PreGameController.instance && Time.unscaledTime - t0 < 60f) yield return null;
            yield return new WaitForSeconds(3f);
            if (PreGameController.instance) RoR2.Console.instance.SubmitCmd(null, "pregame_start_run");
            t0 = Time.unscaledTime;
            while ((user.cachedBody == null || !Run.instance) && Time.unscaledTime - t0 < 120f) yield return null;
            if (!user.cachedBody) { Write("vanilla-run", false, "the run did not start"); yield break; }
            yield return new WaitForSeconds(4f);
            if (user.cachedBody && user.cachedBody.currentVehicle) user.cachedBody.currentVehicle.EjectPassenger(user.cachedBody.gameObject);
            // play like a player would: Commando can't die, uses every skill, and the monsters nearby die as they come
            for (float t = 0f; t < VanillaSeconds; t += 0.5f)
            {
                var b = user.cachedBody;
                if (b)
                {
                    if (b.healthComponent) b.healthComponent.godMode = true;
                    var s = b.skillLocator;
                    if (s) { s.primary?.ExecuteIfReady(); s.secondary?.ExecuteIfReady(); s.utility?.ExecuteIfReady(); s.special?.ExecuteIfReady(); }
                    if ((int)(t * 2f) % 4 == 0)
                        foreach (var m in TeamComponent.GetTeamMembers(TeamIndex.Monster).ToArray())
                            if (m && m.body && m.body.healthComponent && m.body.healthComponent.alive && (m.transform.position - b.corePosition).sqrMagnitude < 60f * 60f)
                                m.body.healthComponent.TakeDamage(new DamageInfo { attacker = b.gameObject, inflictor = b.gameObject, damage = m.body.healthComponent.fullCombinedHealth * 2f, position = m.transform.position, procCoefficient = 1f, crit = false, damageColorIndex = DamageColorIndex.Default });
                }
                yield return new WaitForSeconds(0.5f);
            }
            Write("vanilla-run", true, "");
            Log.Info($"Melty test finished: vanilla Commando run, {VanillaSeconds:0} s");
        }

        IEnumerator Start()
        {
            if (Coop) { yield return Joiner ? CoopJoin() : CoopHost(); yield break; }
            // 1. content
            while (!RoR2Application.loadFinished) yield return null;
            if (Vanilla) { yield return VanillaRun(); yield break; }
            Write("deadlock-content", DeadlockCache.Ready && Survivors.Bodies.Count == Expected, $"cache ready={DeadlockCache.Ready}, heroes built={Survivors.Bodies.Count}/{Expected}");
            int listed = Survivors.Released.Count(h => Survivors.Defs.TryGetValue(h.Id, out var d) && SurvivorCatalog.FindSurvivorDefFromBody(d.bodyPrefab) != null);
            Write("survivors-listed", listed == Expected, $"{listed}/{Expected} heroes in the survivor catalog");
            if (Broken.Count > 0)
            {
                // each broken hero: a catalog tile nobody can pick, whose hover text names both version pairs
                var bad = new List<string>();
                var u = LocalUserManager.GetFirstLocalUser();
                foreach (var id in Broken)
                {
                    if (!MissingHeroes.Defs.TryGetValue(id, out var d) || d.survivorIndex == SurvivorIndex.None) { bad.Add(id + ": no tile"); continue; }
                    if (u != null && u.userProfile.HasSurvivorUnlocked(d.survivorIndex)) bad.Add(id + ": pickable");
                    var why = d.unlockableDef ? d.unlockableDef.getHowToUnlockString() : "";
                    if (!why.Contains("Built with") || !why.Contains("This PC")) bad.Add(id + ": hover text has no versions");
                    if (Survivors.Bodies.ContainsKey(id)) bad.Add(id + ": still built");
                }
                Write("missing-hero-tile", bad.Count == 0, string.Join("; ", bad));
            }
            if (Survivors.Bodies.Count == 0) yield break;

            // 2. start a single-player run as Haze
            yield return new WaitForSeconds(3f);
            var user = LocalUserManager.GetFirstLocalUser();
            while (user == null) { yield return null; user = LocalUserManager.GetFirstLocalUser(); }
            var tested = Tested;
            if (tested.Length == 0 || !Survivors.Defs.ContainsKey(tested[0].Id))
            {
                Write("heroes-to-test", false, "none of the asked heroes is in this build (heroes.txt beside the DLL lists the built ones)");
                yield break;
            }
            user.userProfile.SetSurvivorPreference(Survivors.Defs[tested[0].Id]);
            RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode ClassicRun; host 0;\"");
            float t0 = Time.unscaledTime;
            while (!PreGameController.instance && Time.unscaledTime - t0 < 60f) yield return null;
            yield return new WaitForSeconds(3f);
            Shot("lobby");
            yield return new WaitForSeconds(0.5f);
            if (PreGameController.instance) RoR2.Console.instance.SubmitCmd(null, "pregame_start_run");
            t0 = Time.unscaledTime;
            while ((user.cachedBody == null || !Run.instance) && Time.unscaledTime - t0 < 120f) yield return null;
            if (!user.cachedBody) { foreach (var h in tested) Write(h.Id + "-plays", false, "the run did not start"); yield break; }
            if (TestStage != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != TestStage)
            {
                Log.Info($"Melty test: moving the run to stage {TestStage}");
                var old = user.cachedBody;
                UnityEngine.Networking.NetworkManager.singleton.ServerChangeScene(TestStage);
                t0 = Time.unscaledTime;
                while ((UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != TestStage || !user.cachedBody || user.cachedBody == old) && Time.unscaledTime - t0 < 90f) yield return null;
            }
            Log.Info($"Melty test: stage {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
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
            string gunsWhy = "", abilitiesWhy = "", ultsWhy = "", equipWhy = "";
            int heroes = 0, ragdolls = 0; string ragdollWhy = "";
            foreach (var hero in tested)
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
                if (hero == tested[0])
                {
                    arenaForward = body.inputBank.aimDirection; arenaForward.y = 0; arenaForward.Normalize();
                    var spot = FindArena(body.footPosition, ref arenaForward);
                    TeleportHelper.TeleportBody(body, spot + Vector3.up * 0.5f, false);
                    if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
                    yield return new WaitForSeconds(0.5f);
                    arena = spot;
                    Log.Info($"Melty test: arena at {arena}, facing {arenaForward}");
                }

                // scale: capsule follows Deadlock's relative height (or copies capsuleFrom's: Mo moves with MUL-T's), model uses
                // the shared factor times modelScale
                var cap = body.GetComponent<CapsuleCollider>();
                var capFrom = hero.CapsuleFrom != "none" ? BodyCatalog.FindBodyPrefab(hero.CapsuleFrom) : null;
                float want = capFrom ? capFrom.GetComponent<CapsuleCollider>().height : commandoCap.height * hero.HeightScale;
                float mscale = body.modelLocator.modelTransform.localScale.y, wantScale = ModelBuilder.GlobalScale * hero.ModelScale;
                if (Mathf.Abs(cap.height - want) > 0.05f || Mathf.Abs(mscale - wantScale) > 0.01f)
                { scaleOk = false; scaleWhy += $"{hero.Id}: capsule {cap.height:F2} want {want:F2}, model {mscale:F3} want {wantScale:F3}; "; }
                Log.Info($"Melty test: {hero.Id} size: capsule {cap.height:F2} x {cap.radius:F2} m, model scale {mscale:F3}, hurtbox {(body.mainHurtBox ? body.mainHurtBox.collider.bounds.size.ToString("F2") : "none")}");

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

                // the ult is the hero's equipment (Q): the first hero must already hold it from the start of the run; later
                // heroes (the test turns the same player into each) are handed theirs; 120 s cooldown; a hero figure as its pickup
                var eqDef = Equipment.OfHero(hero.Id);
                var inv = master.inventory;
                if (!eqDef) equipWhy += hero.Id + ": no equipment built; ";
                else
                {
                    if (hero == tested[0] && inv.currentEquipmentIndex != eqDef.equipmentIndex)
                        equipWhy += $"{hero.Id}: started holding {(inv.currentEquipmentIndex == EquipmentIndex.None ? "nothing" : EquipmentCatalog.GetEquipmentDef(inv.currentEquipmentIndex).name)}; ";
#pragma warning disable CS0618
                    var fig = eqDef.pickupModelPrefab ? eqDef.pickupModelPrefab.GetComponentInChildren<MeshRenderer>(true) : null;
#pragma warning restore CS0618
                    var mf = fig ? fig.GetComponent<MeshFilter>() : null;
                    if (!mf || !mf.sharedMesh || mf.sharedMesh.vertexCount < 100) equipWhy += hero.Id + ": no hero figure for the pickup; ";
                    if (Mathf.Abs(eqDef.cooldown - Sheet.SkillOf(hero.Id, "equipment").Cooldown) > 0.01f || eqDef.cooldown != 120f) equipWhy += $"{hero.Id}: cooldown {eqDef.cooldown}; ";
                    if (DevShots) yield return ShowPickup(body, eqDef, hero.Id);
                }

                // RoR2's four skills: gun, two abilities and the R move (the ult is the equipment, not a skill)
                var sl = body.skillLocator;
                int skills = body.GetComponents<GenericSkill>().Count(g => g.skillDef);
                var rRow = Survivors.RowFor(sl.special);
                if (skills != 4 || rRow == null || rRow.Slot != "special")
                { slotsOk = false; slotsWhy += $"{hero.Id}: {skills} skills, R holds {(rRow != null ? rRow.Id : "nothing")}; "; }

                // every skill must measurably do what its sheet row's expect column says
                bool gun = false; int abilities = 0; bool ultFired = false;
                bool passive = true; string passiveWhy = "", kitWhy = "";
                foreach (var slot in new[] { "primary", "secondary", "utility", "special", "equipment" })
                {
                    body = master.GetBody();
                    var row = Sheet.SkillOf(hero.Id, slot);
                    var result = new Effects();
                    yield return TestSkill(body, row, result);
                    var missing = result.Missing(row);
                    Log.Info($"Melty test: {row.Id}: {result} -> {(missing.Count == 0 ? "ok" : "MISSING " + string.Join(", ", missing))}");
                    bool ok = missing.Count == 0;
                    if (slot == "primary") { gun = ok; if (!ok) gunsWhy += $"{row.Id}: no {string.Join(", ", missing)}; "; }
                    else if (slot == "equipment") { ultFired = ok; if (!ok) ultsWhy += $"{row.Id}: no {string.Join(", ", missing)}; "; }
                    else { if (ok) abilities++; else abilitiesWhy += $"{row.Id}: no {string.Join(", ", missing)}; "; }
                    if (slot == "equipment" && !result.Charged) equipWhy += $"{hero.Id}: Q did not start a 120 s cooldown; ";
                    // a melee M1 while the move's buff is on (Mo's Frenzy while Krill is away): hold M1 now; it must hit, throw
                    // the target, and the hero must run at the buff's speed
                    if (row.Extra == "punch")
                    {
                        body = master.GetBody();
                        var punch = new Effects();
                        yield return Punches(body, punch);
                        var gunRow = Sheet.SkillOf(hero.Id, "primary");
                        float wantSpeed = 1f + BuffRow.All.Where(br => br.Melee).Sum(br => br.MoveSpeedAdd);
                        Log.Info($"Melty test: {hero.Id} punches while {row.Name} runs: {punch}, speed x{punch.Speed:F2} (want x{wantSpeed:F2}), target thrown {punch.Knocked:F1} m");
                        if (punch.Damage <= 0f) kitWhy += $"{row.Id}: no punch damage while it ran; ";
                        if (punch.Speed < wantSpeed * 0.9f) kitWhy += $"{row.Id}: speed x{punch.Speed:F2} while it ran, want x{wantSpeed:F2}; ";
                        if (gunRow.MeleeKnock > 0 && punch.Knocked < 2f) kitWhy += $"{row.Id}: punched target moved only {punch.Knocked:F1} m; ";
                    }
                }
                // Heavyweight (immuneToForce): a big push moves the hero less than 0.2 m
                if (hero.ImmuneToForce)
                {
                    body = master.GetBody();
                    var p0 = body.footPosition;
                    if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
                    body.healthComponent.TakeDamageForce(body.transform.right * 30f * (body.characterMotor ? body.characterMotor.mass : 100f), true, true);
                    yield return new WaitForSeconds(0.4f);
                    float pushed = Vector3.Distance(p0, body.footPosition);
                    Log.Info($"Melty test: {hero.Id} heavyweight: a 30 m/s push moved him {pushed:F2} m");
                    if (pushed > 0.2f) { passive = false; passiveWhy = $"pushed {pushed:F2} m"; }
                }
                // a lock-on R (Tag Team) with nothing to lock on: nothing happens and the charge stays
                var rLock = Sheet.SkillOf(hero.Id, "special");
                if (rLock.Kind == "krill_hop")
                {
                    body = master.GetBody();
                    KillTargets();
                    yield return new WaitForSeconds(0.5f);
                    var up = Vector3.up * 0.9f + body.transform.forward * 0.1f;
                    AimCamera(body, up);
                    for (int f = 0; f < 10; f++) { body.inputBank.aimDirection = up; yield return null; }
                    var rs = body.skillLocator.special;
                    rs.Reset();
                    rs.ExecuteIfReady();
                    yield return new WaitForSeconds(0.4f);
                    Log.Info($"Melty test: {hero.Id} R with no target: stock {rs.stock}/{rs.maxStock}, krill away {HeroRuntime.Melee(body)}");
                    if (rs.stock < 1 || HeroRuntime.Melee(body)) kitWhy += $"{rLock.Id}: fired with no target (stock {rs.stock}); ";
                }
                // Q underground (Burrow then Dust Devil): Mo bursts out into the tornado, visible again
                var dig = Sheet.SkillOf(hero.Id, "utility");
                var q = Sheet.SkillOf(hero.Id, "equipment");
                if (dig.Kind == "burrow" && q.Kind == "ult_devil")
                {
                    body = master.GetBody();
                    // start from an idle body: right after the R check a state can still hold the machine and drop the Burrow
                    var idle = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
                    if (idle) idle.SetNextStateToMain();
                    yield return new WaitForSeconds(0.5f);
                    var us = body.skillLocator.utility;
                    us.Reset();
                    us.ExecuteIfReady();
                    yield return new WaitForSeconds(States.Burrow.SinkDelay + States.Burrow.SinkTime + 0.3f);
                    var cm = body.modelLocator && body.modelLocator.modelTransform ? body.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
                    for (float w = 0f; w < 1.5f && !(cm && cm.invisibilityCount > 0); w += 0.1f) yield return new WaitForSeconds(0.1f);
                    bool under = cm && cm.invisibilityCount > 0;
                    var qInv = body.inventory; var qDef = Equipment.OfHero(hero.Id);
                    if (qInv && qDef) qInv.SetEquipment(new EquipmentState(qDef.equipmentIndex, Run.FixedTimeStamp.now, 1), qInv.activeEquipmentLocation);
                    yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                    if (body.equipmentSlot) body.equipmentSlot.ExecuteIfReady();
                    yield return new WaitForSeconds(0.4f);
                    var bm = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
                    bool spinning = bm && bm.state is States.UltDevil;
                    bool shown = cm && cm.invisibilityCount == 0;
                    Log.Info($"Melty test: {hero.Id} Q underground: was under {under}, now {(bm ? bm.state.GetType().Name : "?")}, visible {shown}");
                    if (!under || !spinning || !shown) kitWhy += $"{q.Id} underground: under {under}, spinning {spinning}, visible {shown}; ";
                    if (bm) bm.SetNextStateToMain();
                    yield return new WaitForSeconds(0.5f);
                }
                // noFallDamage (Viscous): the body ignores fall damage
                if (hero.NoFallDamage)
                {
                    body = master.GetBody();
                    bool soft = (body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0;
                    Log.Info($"Melty test: {hero.Id} no fall damage: {soft}");
                    if (!soft) { passive = false; passiveWhy += "takes fall damage; "; }
                }
                // a bounce-pad R (Puddle Punch, launchHeight): placed at the hero's own feet it throws the hero up
                var pad = Sheet.SkillOf(hero.Id, "special");
                if (pad.LaunchHeight > 0)
                {
                    body = master.GetBody();
                    KillTargets();
                    var rest = EntityStateMachine.FindByCustomName(body.gameObject, "Weapon");
                    if (rest) rest.SetNextStateToMain();
                    yield return new WaitForSeconds(0.5f);
                    // from the flat test spot, standing still (a 15 s Goo Ball can roll him far off it onto a slope or under a roof)
                    if (arena != Vector3.zero)
                    {
                        TeleportHelper.TeleportBody(body, arena + Vector3.up * 0.5f, false);
                        if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
                        yield return new WaitForSeconds(0.6f);
                    }
                    var down = Vector3.down * 0.95f + body.transform.forward * 0.05f;
                    AimCamera(body, down);
                    for (int f = 0; f < 10; f++) { body.inputBank.aimDirection = down; yield return null; }
                    float y0 = body.footPosition.y, top = y0;
                    var rs = body.skillLocator.special;
                    rs.Reset();
                    rs.ExecuteIfReady();
                    for (float w = 0f; w < 2.2f; w += Time.deltaTime) { body.inputBank.aimDirection = down; top = Mathf.Max(top, body.footPosition.y); yield return null; }
                    Log.Info($"Melty test: {hero.Id} {pad.Id} at his feet: rose {top - y0:F1} m (want about {pad.LaunchHeight:F0})");
                    if (top - y0 < pad.LaunchHeight * 0.5f) kitWhy += $"{pad.Id}: bounced only {top - y0:F1} m; ";
                    yield return new WaitForSeconds(2.5f);
                }
                // a heal-back passive (Abrams' Infernal Resilience) gives part of a hit back over the next seconds
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
                    if (!passive) passiveWhy = "no heal-back";
                }
                if (anim)
                {
                    Log.Info($"Melty test: {hero.Id} gun drift from the hand over all skills: {anim.GunDrift * 100f:F1} cm");
                    if (anim.GunDrift > 0.02f) { handOk = false; handWhy += $"{hero.Id}: gun moved {anim.GunDrift * 100f:F1} cm in the hand; "; }
                }

                bool plays = gun && abilities == 3 && ultFired && passive && kitWhy == "";
                Write(hero.Id + "-plays", plays, $"gun={gun}, abilities={abilities}/3, ult={ultFired}, passive={passive} {passiveWhy}; {gunsWhy}{abilitiesWhy}{ultsWhy}{kitWhy}".Trim());
                if (plays) heroes++;

                // level-up growth matches the sheet (no items in a fresh run)
                body = master.GetBody();
                var st = Sheet.Stats(hero.Id);
                float lvl = body.level;
                float wantHp = st.BaseMaxHealth + st.LevelMaxHealth * (lvl - 1f), wantDmg = st.BaseDamage + st.LevelDamage * (lvl - 1f);
                // plus any max-health buff the skills gave (Tag Team's trophies stay for the stage)
                foreach (var br in BuffRow.All) if (br.MaxHealthAdd != 0) wantHp += br.MaxHealthAdd * Buffs.Count(body, br.Id);
                if (Mathf.Abs(body.maxHealth - wantHp) > 1.5f || Mathf.Abs(body.damage - wantDmg) > 0.3f)
                { levelOk = false; levelWhy += $"{hero.Id} lvl {lvl}: hp {body.maxHealth:F0} want {wantHp:F0}, dmg {body.damage:F1} want {wantDmg:F1}; "; }

                // trophies (Mo & Krill): a kill's stacks raise the max only (no heal), and health above the base max stays
                // through later stat recalculations
                var trophy = SkillRow.All.FirstOrDefault(r => r.Hero == hero.Id && r.Kind == "krill_hop" && r.Debuff != "none");
                if (trophy != null && body.healthComponent)
                {
                    var hc = body.healthComponent;
                    float baseMax = body.maxHealth - Sheet.Buff(trophy.Debuff).MaxHealthAdd * Buffs.Count(body, trophy.Debuff);
                    hc.Networkhealth = baseMax * 0.5f;
                    float before = hc.health;
                    for (int i = 0; i < 10; i++) Buffs.AddTimed(body, trophy.Debuff, 0f, (int)Sheet.Buff(trophy.Debuff).MaxStacks);
                    body.RecalculateStats();
                    float afterGain = hc.health, maxAfter = body.maxHealth;
                    hc.Networkhealth = body.maxHealth;
                    body.RecalculateStats(); body.RecalculateStats();
                    float kept = hc.health;
                    bool ok = maxAfter > baseMax + 1f && Mathf.Abs(afterGain - before) < 0.5f && kept > baseMax + 1f;
                    Log.Info($"Melty test: {hero.Id} trophies: base max {baseMax:F0}, max after 10 stacks {maxAfter:F0}, health {before:F0} -> {afterGain:F0} on gain, full {body.maxHealth:F0} kept {kept:F0} after recalculating");
                    Write("trophies", ok, ok ? $"{hero.Id}: +{maxAfter - baseMax:F0} max, no heal, kept {kept:F0}" : $"{hero.Id}: max {maxAfter:F0} (base {baseMax:F0}), health {before:F0} -> {afterGain:F0} on gain, kept {kept:F0}");
                    Buffs.Clear(body, trophy.Debuff);
                    hc.Networkhealth = body.maxHealth;
                }

                // death: a spare body of this hero, killed in front of the player, must go limp like vanilla survivors
                var rag = new string[1];
                yield return TestRagdoll(master.GetBody(), hero, rag);
                if (rag[0] == "") ragdolls++; else ragdollWhy += $"{hero.Id}: {rag[0]}; ";

                KillTargets();
                // grow a level before the next hero so later heroes are checked at higher levels
                TeamManager.instance.GiveTeamExperience(TeamIndex.Player, (ulong)Mathf.Max(100f, TeamManager.instance.GetTeamNextLevelExperience(TeamIndex.Player) - TeamManager.instance.GetTeamExperience(TeamIndex.Player) + 1f));
                yield return new WaitForSeconds(1f);
            }
            // the ult belongs to the equipment: Commando holding Concussive Combustion gets the same blast and ring, and
            // Commando holding Dust Devil spins in the same tornado
            string anyWhy = ""; bool anyTested = false;
            // (only when its hero is among the tested ones)
            foreach (var bomb in SkillRow.All.Where(r => (r.Kind == "ult_selfbomb" || r.Kind == "ult_devil" || r.Kind == "ult_gooball") && tested.Any(h => h.Id == r.Hero)).ToList())
            {
                master.TransformBody("CommandoBody");
                yield return new WaitForSeconds(1.5f);
                var cb = master.GetBody();
                if (cb)
                {
                    cb.healthComponent.godMode = true;
                    var res = new Effects();
                    yield return TestSkill(cb, bomb, res);
                    // Commando is several levels up by now and the blast can kill the targets before they are stunned:
                    // the blast landing is what shows the ult works on him
                    var miss = res.Missing(bomb).Where(m => m != "enemy_stun").ToList();
                    // Dust Devil on a carrier without the tornado clip turns the whole model (two turns a second)
                    if (bomb.Kind == "ult_devil" && res.Spun < 720f) miss.Add($"spin ({res.Spun:F0} deg)");
                    Log.Info($"Melty test: Commando with {bomb.Id}: {res} -> {(miss.Count == 0 ? "ok" : "MISSING " + string.Join(", ", miss))}");
                    if (miss.Count > 0) anyWhy += $"Commando with {bomb.Id}: no " + string.Join(", ", miss) + "; ";
                }
                else anyWhy += "could not become Commando; ";
                KillTargets();
                anyTested = true;
            }
            if (anyTested) Write("ult-any-survivor", anyWhy == "", anyWhy);
            Write("ragdoll", ragdolls == tested.Length, ragdollWhy);

            var lb = master.GetBody();
            Write("guns-fire", gunsWhy == "", gunsWhy);
            Write("abilities-fire", abilitiesWhy == "", abilitiesWhy);
            Write("abilities-work", abilitiesWhy == "", abilitiesWhy);
            Write("ult-equipment", equipWhy == "", equipWhy);
            Write("ult-fires", ultsWhy == "", ultsWhy);
            Write("ults-work", ultsWhy == "", ultsWhy);
            Write("gun-aims", aimOk, aimWhy);
            Write("gun-in-hand", handOk, handWhy);
            Write("four-slots", slotsOk, slotsWhy);
            Write("level-stats", levelOk && lb && lb.level > 1, levelWhy == "" ? "level did not rise" : levelWhy);
            Write("hero-scale", scaleOk, scaleWhy);
            // only this build's heroes (a release's heroes.txt leaves the others out of the cache)
            var built = new HashSet<string>(Survivors.Released.Select(h => h.Id));
            var missingVoice = VoiceRow.All.Where(v => built.Contains(v.Hero) && !DeadlockAudio.Has("vo:" + v.Group)).Select(v => v.Group).Distinct().ToList();
            Write("voice-lines", missingVoice.Count == 0 && Voice.Spoken > 0,
                missingVoice.Count > 0 ? "not in the cache: " + string.Join(", ", missingVoice.Take(5)) : Voice.Spoken == 0 ? "no voice line played" : "");
            // every sound event the sheet names (design/sounds.json) made it into the cache
            var missingSound = SoundRow.All.Where(s => Sheet.Skill(s.Skill.Split('/')[0]) is var k && (k == null || built.Contains(k.Hero))).SelectMany(s => new[] { s.Cast, s.Loop, s.Hit, s.End, s.Reload, s.CastLayer, s.HitLayer, s.Tick, s.TickFinal })
                                           .Where(e => e != "none" && !DeadlockAudio.Has(e)).Distinct().ToList();
            Write("sound-events", missingSound.Count == 0, "not in the cache: " + string.Join(", ", missingSound.Take(5)));
            Log.Info($"Melty test finished: {heroes}/{tested.Length} heroes: every skill did what its sheet expects; Deadlock sounds played: {DeadlockAudio.Played}");
            if (lb) lb.healthComponent.godMode = false;
        }

        /// <summary>Spawn a spare body of the hero 4 m in front of the player, kill it, and check after 1.5 s that its
        /// RagdollController's bones went limp and its head dropped. why[0] = "" on success.</summary>
        static IEnumerator TestRagdoll(CharacterBody player, HeroRow hero, string[] why)
        {
            why[0] = "";
            var prefab = Survivors.Bodies.TryGetValue(hero.Id, out var bp) ? bp : null;
            if (!player || !prefab) { why[0] = "no body"; yield break; }
            var fwd = player.inputBank.aimDirection; fwd.y = 0; fwd.Normalize();
            var at = player.footPosition + fwd * 4f;
            if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var g, 8f, LayerIndex.world.mask)) at = g.point;
            var go = UnityEngine.Object.Instantiate(prefab, at + Vector3.up * 1f, Quaternion.LookRotation(-fwd));
            NetworkServer.Spawn(go);
            var body = go.GetComponent<CharacterBody>();
            yield return new WaitForSeconds(1f);
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            var rc = model ? model.GetComponent<RagdollController>() : null;
            if (!rc) { why[0] = "no RagdollController"; if (go) UnityEngine.Object.Destroy(go); yield break; }
            var head = model.GetComponent<ChildLocator>()?.FindChild("Head");
            float headBefore = head ? head.position.y - body.footPosition.y : 0f;
            float groundY = body.footPosition.y;
            body.healthComponent.Suicide();
            yield return new WaitForSeconds(1.5f);
            int limp = rc.bones.Count(b => b && b.GetComponent<Rigidbody>() && !b.GetComponent<Rigidbody>().isKinematic);
            float headAfter = head ? head.position.y - groundY : 0f;
            Log.Info($"Melty test: {hero.Id} ragdoll: {limp}/{rc.bones.Length} bones limp, head {headBefore:F2} m -> {headAfter:F2} m above the ground");
            if (DevShots) Shot(hero.Id + "_ragdoll");
            if (limp < rc.bones.Length) why[0] = $"{limp}/{rc.bones.Length} bones limp";
            else if (!head || headAfter > headBefore * 0.6f) why[0] = $"head stayed up ({headBefore:F2} -> {headAfter:F2} m)";
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
            // the four ult pickups' hero figures, in a row in front of the models
            int k = 0;
            foreach (var h in HeroRow.All)
            {
                var def = Equipment.OfHero(h.Id);
#pragma warning disable CS0618
                var fig = def ? def.pickupModelPrefab : null;
#pragma warning restore CS0618
                if (!fig) continue;
                var f = UnityEngine.Object.Instantiate(fig, origin + new Vector3((k++ - (HeroRow.All.Length - 1) / 2f) * 1.2f, 0.4f, -2.5f), Quaternion.LookRotation(Vector3.back));
                f.transform.localScale = Vector3.one * 0.5f;
                f.SetActive(true);
                extra.Add(f);
            }
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

        /// <summary>Dev only: drop the hero's ult as a pickup beside them and photograph the figure, then remove it.</summary>
        static IEnumerator ShowPickup(CharacterBody body, EquipmentDef def, string hero)
        {
            var fwd = body.inputBank.aimDirection; fwd.y = 0; fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            var at = body.corePosition + fwd * 3f + right * 1.5f;
            var before = new HashSet<GenericPickupController>(UnityEngine.Object.FindObjectsOfType<GenericPickupController>());
            PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(def.equipmentIndex), at, Vector3.up * 8f);
            yield return new WaitForSeconds(2.5f);
            var made = UnityEngine.Object.FindObjectsOfType<GenericPickupController>().Where(g => !before.Contains(g)).ToList();
            var cam = Camera.main;
            foreach (var g in made)
                foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true))
                    Log.Info($"Melty test: {hero} pickup renderer {r.name}: active {r.gameObject.activeInHierarchy}, enabled {r.enabled}, bounds {r.bounds.center} size {r.bounds.size}, materials {r.sharedMaterials.Length}");
            if (made.Count > 0 && cam)
            {
                var p = made[0].transform.position;
                var mr = made[0].GetComponentInChildren<MeshRenderer>();
                Log.Info($"Melty test: {hero} pickup at {p}, figure centre {(mr ? mr.bounds.center : p)}");
                if (mr) p = mr.bounds.center - Vector3.up * 0.4f;
                var go = new GameObject("DevPickupCamera");
                var c = go.AddComponent<Camera>(); c.CopyFrom(cam);
                c.transform.position = p + (body.corePosition - p).normalized * 2.2f + Vector3.up * 0.8f;
                c.transform.rotation = Quaternion.LookRotation(p + Vector3.up * 0.4f - c.transform.position);
                c.fieldOfView = 45f;
                var rt = new RenderTexture(1280, 720, 24); c.targetTexture = rt; c.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                RenderTexture.active = null;
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "devshots");
                File.WriteAllBytes(System.IO.Path.Combine(dir, hero + "_pickup.png"), tex.EncodeToPNG());
                c.targetTexture = null; UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(tex);
            }
            foreach (var g in made) if (g) UnityEngine.Object.Destroy(g.gameObject);
        }

        static readonly List<CharacterBody> targets = new List<CharacterBody>();
        static Vector3 arena, arenaForward = Vector3.forward;

        /// <summary>Test mode only: stop the stage's monster spawns and remove the monsters already there, so the skill checks
        /// see only their own target Lemurians (wandering Wisps and Beetles used to soak up the shots).</summary>
        internal static void QuietStage()
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
        internal sealed class Effects
        {
            public float Damage, Healed, Moved, Pulled;
            /// <summary>How far a target rose above where it stood (Puddle Punch's knock-up); the hero was invulnerable (The Cube).</summary>
            public float Lifted; public bool Invulnerable;
            readonly Dictionary<CharacterBody, float> startY = new Dictionary<CharacterBody, float>();
            /// <summary>Degrees the hero's facing turned in all (Dust Devil spinning a carrier); its speed over the sheet's base
            /// speed and how far a punched target was thrown (Mo's Frenzy).</summary>
            public float Spun, Speed, Knocked;
            float lastYaw = float.NaN;
            public bool Stun, Burn, Cloak, Charged, Disarm, Hidden; // Charged: the equipment went on its cooldown
            public readonly HashSet<string> EnemyBuffs = new HashSet<string>(), SelfBuffs = new HashSet<string>();
            readonly Dictionary<CharacterBody, float> startGap = new Dictionary<CharacterBody, float>();

            /// <summary>One frame of watching: the hero (moved, cloaked, hidden, its buffs) and the targets (buffs, stun, burn,
            /// skills locked, drawn toward the hero).</summary>
            public void Poll(CharacterBody b, Vector3 start, IEnumerable<CharacterBody> targets)
            {
                Moved = Mathf.Max(Moved, Vector3.Distance(start, b.corePosition));
                if (b.hasCloakBuff) Cloak = true;
                var cm = b.modelLocator && b.modelLocator.modelTransform ? b.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
                if (cm && cm.invisibilityCount > 0) Hidden = true;
                if (b.HasBuff(RoR2Content.Buffs.HiddenInvincibility)) Invulnerable = true;
                if (b.characterDirection)
                {
                    if (!float.IsNaN(lastYaw)) Spun += Mathf.Abs(Mathf.DeltaAngle(lastYaw, b.characterDirection.yaw));
                    lastYaw = b.characterDirection.yaw;
                }
                foreach (var kv in Buffs.Defs) if (b.HasBuff(kv.Value)) SelfBuffs.Add(kv.Key);
                foreach (var t in targets)
                {
                    if (!t || !t.healthComponent || !t.healthComponent.alive) continue;
                    foreach (var kv in Buffs.Defs) if (t.HasBuff(kv.Value)) EnemyBuffs.Add(kv.Key);
                    var esm = EntityStateMachine.FindByCustomName(t.gameObject, "Body");
                    if (esm && esm.state is EntityStates.StunState) Stun = true;
                    var dc = DotController.FindDotController(t.gameObject);
                    if (dc && dc.HasDotActive(DotController.DotIndex.Burn)) Burn = true;
                    if (DLC2Content.Buffs.DisableAllSkills && t.HasBuff(DLC2Content.Buffs.DisableAllSkills)) Disarm = true;
                    // drawn toward the hero (Dust Devil): how much closer it got than it started
                    var flat = t.corePosition - b.corePosition; flat.y = 0;
                    if (!startGap.ContainsKey(t)) startGap[t] = flat.magnitude;
                    Pulled = Mathf.Max(Pulled, startGap[t] - flat.magnitude);
                    if (!startY.ContainsKey(t)) startY[t] = t.footPosition.y;
                    Lifted = Mathf.Max(Lifted, t.footPosition.y - startY[t]);
                }
            }

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
                        kind == "self_cloak" ? Cloak :
                        kind == "enemy_disarm" ? Disarm :
                        kind == "enemy_pulled" ? Pulled > 1f :
                        kind == "self_hidden" ? Hidden :
                        kind == "self_invulnerable" ? Invulnerable :
                        kind == "enemy_knockup" ? Lifted > 1f : false;
                    if (!ok) miss.Add(e);
                }
                return miss;
            }

            public override string ToString() =>
                $"damage {Damage:F0}, stun {Stun}, burn {Burn}, enemy buffs [{string.Join(",", EnemyBuffs)}], self buffs [{string.Join(",", SelfBuffs)}], healed {Healed:F0}, moved {Moved:F1} m, cloak {Cloak}, disarm {Disarm}, pulled {Pulled:F1} m, hidden {Hidden}, spun {Spun:F0} deg, lifted {Lifted:F1} m, invulnerable {Invulnerable}";
        }

        /// <summary>The flat spot every skill starts from: ground under it, and level ground (within 1.5 m) at the targets' places
        /// 6-12 m ahead and 2 m to the side. Tries 14 m ahead of the landing first (as before), then the landing itself, then the
        /// stage's walkable ground nodes nearest the landing in eight directions. Before 0.7.2 the spot 14 m ahead was taken
        /// unchecked; on one stage Melty's seed picks it hung over a drop, so the hero and targets fell to y = -207 and every
        /// hit check failed (two Melty test runs, 2026-10-19).</summary>
        static Vector3 FindArena(Vector3 landing, ref Vector3 forward)
        {
            var dirs = new List<Vector3> { forward };
            for (int i = 1; i < 8; i++) dirs.Add(Quaternion.Euler(0f, 45f * i, 0f) * forward);
            var candidates = new List<Vector3> { landing + forward * 14f, landing };
            var graph = SceneInfo.instance ? SceneInfo.instance.groundNodes : null;
            if (graph)
                foreach (var n in graph.FindNodesInRange(landing, 0f, 80f, HullMask.Human).Take(80))
                    if (graph.GetNodePosition(n, out var np)) candidates.Add(np);
            foreach (var c in candidates)
            {
                if (!Ground(c, 30f, 60f, out var g)) continue;
                foreach (var dir in dirs)
                {
                    var right = Vector3.Cross(Vector3.up, dir);
                    bool flat = true;
                    foreach (var at in new[] { g + dir * 6f, g + dir * 10f, g + dir * 11.5f + right * 2f })
                        if (!Ground(at, 2f, 4f, out var tg) || Mathf.Abs(tg.y - g.y) > 1.5f
                            || Physics.Linecast(g + Vector3.up * 1f, tg + Vector3.up * 1f, LayerIndex.world.mask)) { flat = false; break; }
                    if (!flat) continue;
                    forward = dir;
                    return g;
                }
            }
            Log.Warn("Melty test: no flat arena found near the landing; using the landing spot");
            return landing;
        }

        static bool Ground(Vector3 at, float above, float depth, out Vector3 point)
        {
            point = at;
            if (!Physics.Raycast(at + Vector3.up * above, Vector3.down, out var hit, above + depth, LayerIndex.world.mask)) return false;
            point = hit.point;
            return true;
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
            float d = TargetDistance(row);
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
                    fireBody = body; fireUntil = Time.time + 6f;
                    body.skillLocator.primary.ExecuteIfReady();
                }
                else if (row.Slot == "equipment")
                {
                    // hold this hero's ult, charged, then Q through RoR2's own equipment slot, like a player pressing it
                    var inv = body.inventory;
                    var def = Equipment.OfHero(row.Hero);
                    if (inv && def)
                    {
                        var loc = inv.activeEquipmentLocation;
                        inv.SetEquipment(new EquipmentState(def.equipmentIndex, Run.FixedTimeStamp.now, 1), loc);
                    }
                    yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                    if (body.equipmentSlot) body.equipmentSlot.ExecuteIfReady();
                    yield return new WaitForFixedUpdate();
                    fx.Charged = inv && inv.currentEquipmentState.charges == 0 && body.equipmentSlot && body.equipmentSlot.cooldownTimer > 100f;
                }
                else
                {
                    var gs = row.Slot == "secondary" ? body.skillLocator.secondary : row.Slot == "utility" ? body.skillLocator.utility : body.skillLocator.special;
                    var hr = body.GetComponent<HeroRuntime>();
                    var gunRow = Survivors.RowFor(body.skillLocator.primary);
                    if (row.AmmoCost > 0 && hr && gunRow != null) { hr.Reloading = false; hr.Ammo = (int)gunRow.ClipSize; }
                    gs.Reset();
                    gs.ExecuteIfReady();
                }

                float window = row.Slot == "primary" ? 6f : Mathf.Clamp(row.Duration + 1.5f, 2.5f, 7f);
                float t0 = Time.time;
                int shots = 0;
                while (Time.time - t0 < window)
                {
                    // the player controller re-aims at the crosshair every Update; keep the aim on a live target
                    var aimAt = targets.FirstOrDefault(x => x && x.healthComponent && x.healthComponent.alive);
                    if (aimAt && body) body.inputBank.aimDirection = (aimAt.corePosition - body.inputBank.aimOrigin).normalized;
                    var b = body;
                    if (!b) break;
                    fx.Poll(b, start, targets);
                    // dev screenshots through the skill: early (a wind-up: during it, to catch the telegraph), middle, late
                    if (shots < 3 && Time.time - t0 > new[] { row.MinDuration > 0f && row.Kind == "cone_wave" ? row.MinDuration * 0.6f : 0.5f, 1.4f, 2.3f }[shots]) { Shot(shots == 0 ? row.Id : row.Id + "_" + (shots + 1)); shots++; }
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

        /// <summary>How far in front of the hero the test's targets stand: guns, the leap, the blink and Tag Team's throw
        /// from 10 m, everything else 6 m.</summary>
        internal static float TargetDistance(SkillRow row) =>
            row.Slot == "primary" || row.Kind == "ult_leap" || row.Kind == "phantom_strike" || row.Kind == "krill_hop" ? 10f : 6f;

        /// <summary>Hold M1 for 1.5 s against the targets still standing (Tag Team's Krill is away: Mo punches), measuring damage.</summary>
        IEnumerator Punches(CharacterBody body, Effects fx)
        {
            if (!body) yield break;
            var victims = new HashSet<CharacterBody>(targets);
            System.Action<DamageReport> onDamage = r => { if (r != null && r.attackerBody == body && victims.Contains(r.victimBody)) fx.Damage += r.damageDealt; };
            GlobalEventManager.onServerDamageDealt += onDamage;
            try
            {
                var aimAt = targets.FirstOrDefault(x => x && x.healthComponent && x.healthComponent.alive);
                // the move just hurt it: at full health it lives through the punches, so the throw can be measured
                if (aimAt) aimAt.healthComponent.HealFraction(1f, default(ProcChainMask));
                var st = Sheet.Stats(Survivors.HeroOf(body));
                if (st != null) fx.Speed = body.moveSpeed / Mathf.Max(0.1f, st.BaseMoveSpeed);
                // walk up to the target: the punch reaches a few metres
                if (aimAt) TeleportHelper.TeleportBody(body, aimAt.footPosition - (aimAt.footPosition - body.footPosition).normalized * (body.radius + aimAt.radius + 1f), false);
                yield return new WaitForSeconds(0.2f);
                var aimFrom = aimAt ? aimAt.footPosition : Vector3.zero;
                fireBody = body; fireUntil = Time.time + 1.5f;
                body.skillLocator.primary.ExecuteIfReady();
                float t0 = Time.time;
                while (Time.time - t0 < 1.6f)
                {
                    if (aimAt && body) body.inputBank.aimDirection = (aimAt.corePosition - body.inputBank.aimOrigin).normalized;
                    if (aimAt) fx.Knocked = Mathf.Max(fx.Knocked, Vector3.Distance(aimFrom, aimAt.footPosition));
                    if (Time.time - t0 > 0.6f && Time.time - t0 < 0.65f) Shot(Survivors.HeroOf(body) + "_punch");
                    yield return null;
                }
            }
            finally { GlobalEventManager.onServerDamageDealt -= onDamage; fireBody = null; }
        }

        /// <summary>Turns the player's camera (and with it the aim) to look along a direction: the camera mode keeps its
        /// pitch/yaw in per-rig instance data, which has SetPitchYawFromLookVector.</summary>
        internal static void AimCamera(CharacterBody body, Vector3 look)
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
