using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeadlockOfRain.Sheets;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>The co-op test (tools/run_coop_test.sh): two copies of the game on this PC. The first hosts (it plays Abrams
    /// and measures everything, as the server), the second joins through Melty's join= line and plays every hero's skills
    /// as a remote player: the case where the hero's owner is not the server. The copies take turns through two small files
    /// in %LOCALAPPDATA%\DeadlockOfRain\coop (the host writes the step, the joiner answers). Only the host writes checks.</summary>
    public sealed partial class MeltyTest
    {
        public static bool Coop, Joiner;
        static string coopDir => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "coop");
        static string StepFile => System.IO.Path.Combine(coopDir, "step.txt");
        static string AckFile => System.IO.Path.Combine(coopDir, "ack.txt");

        static void Put(string file, string line)
        {
            var tmp = file + ".tmp";
            File.WriteAllText(tmp, line);
            if (File.Exists(file)) File.Delete(file);
            File.Move(tmp, file);
        }

        static string[] Get(string file)
        {
            try { return File.Exists(file) ? File.ReadAllText(file).Trim().Split('|') : null; } catch { return null; }
        }

        static IEnumerator WaitAck(string what, int n, float timeout)
        {
            float t0 = Time.unscaledTime;
            while (Time.unscaledTime - t0 < timeout)
            {
                var a = Get(AckFile);
                if (a != null && a.Length >= 2 && a[0] == what && a[1] == n.ToString()) yield break;
                yield return new WaitForSecondsRealtime(0.1f);
            }
            Log.Warn($"co-op test: no '{what} {n}' from the joiner after {timeout} s");
        }

        static bool Acked(string what, int n)
        {
            var a = Get(AckFile);
            return a != null && a.Length >= 2 && a[0] == what && a[1] == n.ToString();
        }

        static string V(Vector3 v) => $"{v.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        static Vector3 V(string s)
        {
            var p = s.Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return new Vector3(p[0], p[1], p[2]);
        }

        // ---------------------------------------------------------------- host
        IEnumerator CoopHost()
        {
            Directory.CreateDirectory(coopDir);
            foreach (var f in new[] { StepFile, AckFile }) if (File.Exists(f)) File.Delete(f);
            while (!RoR2Application.loadFinished) yield return null;
            Write("deadlock-content", DeadlockCache.Ready && Survivors.Bodies.Count == Survivors.Released.Length, $"cache ready={DeadlockCache.Ready}, heroes built={Survivors.Bodies.Count}/{Survivors.Released.Length}");
            if (Survivors.Bodies.Count == 0) yield break;

            // 1. host a game others can join (the copy on this PC joins through loopback)
            yield return new WaitForSeconds(3f);
            var user = LocalUserManager.GetFirstLocalUser();
            while (user == null) { yield return null; user = LocalUserManager.GetFirstLocalUser(); }
            var hostHero = HeroRow.All[1];
            user.userProfile.SetSurvivorPreference(Survivors.Defs[hostHero.Id]);
            RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode ClassicRun; host 1;\"");
            float t0 = Time.unscaledTime;
            while (!PreGameController.instance && Time.unscaledTime - t0 < 60f) yield return null;
            bool listening = NetworkServer.active && !NetworkServer.dontListen;
            Write("coop-hosts", PreGameController.instance && listening, $"lobby={(bool)PreGameController.instance}, listening={listening}");
            MeltyJoin.AnnounceDirect(NetworkManagerSystem.singleton.networkPort);

            // 2. the second copy joins
            t0 = Time.unscaledTime;
            while (NetworkUser.readOnlyInstancesList.Count < 2 && Time.unscaledTime - t0 < 600f) yield return null;
            var remoteUser = NetworkUser.readOnlyInstancesList.FirstOrDefault(u => u && !u.isLocalPlayer);
            Write("coop-joined", remoteUser, $"{NetworkUser.readOnlyInstancesList.Count} player(s) in the lobby after 600 s");
            if (!remoteUser) { Log.Info($"Melty test finished: co-op 0/{(TestHeroes != null ? TestHeroes.Count : HeroRow.All.Length)} heroes (nobody joined)"); yield break; }
            yield return new WaitForSeconds(5f);
            Shot("coop_lobby_host");
            if (PreGameController.instance) RoR2.Console.instance.SubmitCmd(null, "pregame_start_run");
            t0 = Time.unscaledTime;
            while ((!user.cachedBody || !remoteUser.master || !remoteUser.master.GetBody()) && Time.unscaledTime - t0 < 120f) yield return null;
            var master = remoteUser.master;
            if (!master || !master.GetBody()) { Write("coop-run", false, "the run did not start for both players"); yield break; }
            Write("coop-run", true);
            yield return new WaitForSeconds(4f);
            foreach (var b in new[] { user.cachedBody, master.GetBody() })
                if (b && b.currentVehicle) b.currentVehicle.EjectPassenger(b.gameObject);
            yield return new WaitForSeconds(3f);
            QuietStage();

            // the arena: open ground in front of the host's hero, which stands aside and fires its gun once so the joiner
            // can see another player's shots
            var hostBody = user.cachedBody;
            hostBody.healthComponent.godMode = true;
            var away = hostBody.footPosition + hostBody.inputBank.aimDirection.normalized * 14f;
            if (Physics.Raycast(away + Vector3.up * 30f, Vector3.down, out var gh, 80f, LayerIndex.world.mask)) away = gh.point;
            arena = away;
            arenaForward = hostBody.inputBank.aimDirection; arenaForward.y = 0; arenaForward.Normalize();
            var right = Vector3.Cross(Vector3.up, arenaForward);
            TeleportHelper.TeleportBody(hostBody, arena - right * 6f + Vector3.up * 0.5f, false);
            yield return new WaitForSeconds(0.5f);
            fireBody = hostBody; fireUntil = Time.time + 2f;
            hostBody.skillLocator.primary.ExecuteIfReady();
            yield return new WaitForSeconds(2.5f);

            // 3. every hero, played by the joiner
            int n = 0, heroes = 0;
            string why = "";
            foreach (var hero in HeroRow.All)
            {
                if (!Survivors.Bodies.ContainsKey(hero.Id) || TestHeroes != null && !TestHeroes.Contains(hero.Id)) continue;
                if (Survivors.HeroOf(master.GetBody()) != hero.Id)
                {
                    master.TransformBody(Survivors.Bodies[hero.Id].name);
                    yield return new WaitForSeconds(2.5f);
                }
                var body = master.GetBody();
                if (!body || Survivors.HeroOf(body) != hero.Id) { Write("coop-" + hero.Id, false, "the joiner could not become " + hero.DisplayName); continue; }
                body.healthComponent.godMode = true;
                int ok = 0;
                string heroWhy = "";
                foreach (var slot in new[] { "primary", "secondary", "utility", "special", "equipment" })
                {
                    n++;
                    var row = Sheet.SkillOf(hero.Id, slot);
                    body = master.GetBody();
                    // the joiner walks onto the arena and faces along it
                    Put(StepFile, $"prep|{n}|{V(arena)}|{V(arenaForward)}");
                    yield return WaitAck("ready", n, 120f);
                    yield return new WaitForSeconds(0.5f);
                    KillTargets(); QuietStage();
                    var fwd = arenaForward;
                    var r = Vector3.Cross(Vector3.up, fwd);
                    float d = TargetDistance(row);
                    foreach (var at in new[] { body.footPosition + fwd * d, body.footPosition + fwd * (d + 1.5f) + r * 2f })
                    {
                        var p = at;
                        if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var g, 8f, LayerIndex.world.mask)) p = g.point;
                        var t = Summon(body, p + Vector3.up * 0.5f);
                        if (!t) continue;
                        if (t.master && t.master.inventory) t.master.inventory.GiveItem(RoR2Content.Items.BoostHp, 200);
                        targets.Add(t);
                    }
                    if (row.Slot == "equipment")
                    {
                        var inv = body.inventory; var def = Equipment.OfHero(row.Hero);
                        if (inv && def) inv.SetEquipment(new EquipmentState(def.equipmentIndex, Run.FixedTimeStamp.now, 1), inv.activeEquipmentLocation);
                    }
                    if (row.Expect.Contains("self_heal"))
                    {
                        body.healthComponent.godMode = false;
                        body.healthComponent.TakeDamage(new DamageInfo { damage = body.healthComponent.fullHealth * 0.4f, position = body.corePosition, damageType = DamageType.NonLethal | DamageType.BypassArmor, procCoefficient = 0f });
                        body.healthComponent.godMode = true;
                    }
                    yield return new WaitForSeconds(1f);
                    Log.Info($"Melty test: co-op {row.Id}: joined player at {body.footPosition} (arena {arena}), targets " + string.Join(", ", targets.Select(x => x && x.healthComponent && x.healthComponent.alive ? $"{Vector3.Distance(x.footPosition, body.footPosition):F1} m away" : "dead")) + $" ({TeamComponent.GetTeamMembers(TeamIndex.Monster).Count} monsters)");

                    var fx = new Effects();
                    var victims = new HashSet<CharacterBody>(targets);
                    // Rat Swarm has no duration: its rats pour out, run and leap before the first bite (~2 s, plus the joiner's lag);
                    // Telekinesis lands its hit after the state ends (lift, hold, a 50 m flight: ~2.4 s on the server, plus the lag)
                    float window = row.Slot == "primary" ? 6f : Mathf.Clamp(row.Duration + 1.5f, row.Kind == "ult_swarm" || row.Kind == "ult_telekinesis" ? 5f : 2.5f, 7f);
                    System.Action<DamageReport> onDamage = rep => { if (rep != null && rep.attackerBody == body && rep.victimBody && victims.Contains(rep.victimBody)) fx.Damage += rep.damageDealt; };
                    System.Action<HealthComponent, float, ProcChainMask> onHeal = (hc, amount, mask) => { if (hc == body.healthComponent && amount > 0f) fx.Healed += amount; };
                    GlobalEventManager.onServerDamageDealt += onDamage;
                    HealthComponent.onCharacterHealServer += onHeal;
                    var start = body.corePosition;
                    // where the first target stands: the joiner may not have seen the last step's targets die yet
                    var aimAt = targets.FirstOrDefault(x => x) is CharacterBody t0b ? t0b.corePosition : body.corePosition + arenaForward * 6f;
                    Put(StepFile, $"fire|{n}|{row.Slot}|{window.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{row.Id}|{V(aimAt)}");
                    float tf = Time.time;
                    int shots = 0;
                    try
                    {
                        while (Time.time - tf < window + 90f && !Acked("done", n))
                        {
                            var b = master.GetBody();
                            if (!b) break;
                            fx.Poll(b, start, targets);
                            if (row.Slot == "equipment" && !fx.Charged && b.inventory)
                                fx.Charged = b.inventory.currentEquipmentState.charges == 0 && b.equipmentSlot && b.equipmentSlot.cooldownTimer > row.Cooldown * 0.8f;   // 60-120 s ults (Goo Ball 100 s)
                            if (shots < 2 && Time.time - tf > new[] { 0.7f, 1.6f }[shots]) { Shot(row.Id + "_host" + (shots + 1)); shots++; }
                            yield return null;
                        }
                    }
                    finally
                    {
                        GlobalEventManager.onServerDamageDealt -= onDamage;
                        HealthComponent.onCharacterHealServer -= onHeal;
                    }
                    var missing = fx.Missing(row);
                    if (row.Slot == "equipment" && !fx.Charged) missing.Add($"{row.Cooldown:0} s cooldown");
                    Log.Info($"Melty test: co-op {row.Id} (joined player): {fx}, rat bites {RatSwarm.Bites} -> {(missing.Count == 0 ? "ok" : "MISSING " + string.Join(", ", missing))}");
                    if (missing.Count == 0) ok++; else heroWhy += $"{row.Id}: no {string.Join(", ", missing)}; ";
                    yield return new WaitForSeconds(0.5f);
                }
                bool plays = ok == 5;
                Write("coop-" + hero.Id, plays, heroWhy);
                if (plays) heroes++; else why += heroWhy;
                KillTargets();
            }

            // 4. what each side saw of the other: the joiner's shots here, the host's shots and the server's effects there
            n++;
            Put(StepFile, $"end|{n}");
            yield return WaitAck("stats", n, 120f);
            var st = Get(AckFile) ?? new string[0];
            int relayed = st.Length > 2 ? int.Parse(st[2]) : 0;
            bool sawHostShots = st.Length > 3 && st[3] == "1";
            bool sawJoinerShots = seen.Contains("coop-remote-shot");
            Write("coop-shots-seen", sawJoinerShots && sawHostShots, $"host saw the joiner's shots={sawJoinerShots}, joiner saw the host's shots={sawHostShots}");
            Write("coop-effects-seen", relayed > 0, $"the joiner received {relayed} server effects (hit marks, stack bursts, strikes)");
            Log.Info($"Melty test finished: co-op {heroes}/{(TestHeroes != null ? TestHeroes.Count : HeroRow.All.Length)} heroes played by a joined player; joiner received {relayed} relayed effects");
            fireBody = null;
            if (hostBody) hostBody.healthComponent.godMode = false;
        }

        // ---------------------------------------------------------------- joiner
        IEnumerator CoopJoin()
        {
            while (!RoR2Application.loadFinished) yield return null;
            var user = LocalUserManager.GetFirstLocalUser();
            while (user == null) { yield return null; user = LocalUserManager.GetFirstLocalUser(); }
            // the hero the joiner picks in the lobby (MeltyJoin connects once the title screen is up)
            user.userProfile.SetSurvivorPreference(Survivors.Defs[HeroRow.All[0].Id]);
            // each step is "<what>|<n>|...": prep and fire share n, so a step is new when either changed
            string last = "";
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                var s = Get(StepFile);
                if (s == null || s.Length < 2 || !int.TryParse(s[1], out var n) || s[0] + n == last) continue;
                var body = user.cachedBody;
                if (s[0] == "end")
                {
                    last = s[0] + n;
                    Put(AckFile, $"stats|{n}|{Relay.Received}|{(seen.Contains("coop-remote-shot") ? 1 : 0)}");
                    Log.Info($"Melty test: co-op joiner done: {Relay.Received} relayed effects received");
                    yield break;
                }
                if (!body) continue;
                last = s[0] + n;
                // the player controller re-aims at the camera's crosshair every frame, and in this slow second copy the
                // camera never settled on the enemy: the test drives this hero itself, so switch it off
                var pcmc = user.cachedMaster ? user.cachedMaster.playerCharacterMasterController : null;
                if (pcmc && pcmc.enabled) pcmc.enabled = false;
                if (s[0] == "prep")
                {
                    if (body.currentVehicle) body.currentVehicle.EjectPassenger(body.gameObject);
                    var at = V(s[2]); var fwd = V(s[3]);
                    TeleportHelper.TeleportBody(body, at + Vector3.up * 0.5f, false);
                    if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
                    AimCamera(body, fwd);
                    for (int f = 0; f < 15; f++) { body.inputBank.aimDirection = fwd; yield return null; }
                    Put(AckFile, $"ready|{n}");
                }
                else if (s[0] == "fire")
                {
                    var slot = s[2];
                    float window = float.Parse(s[3], System.Globalization.CultureInfo.InvariantCulture);
                    var rowId = s.Length > 4 ? s[4] : slot;
                    var near = s.Length > 5 ? V(s[5]) : body.corePosition;
                    // aim at the host's first target (the living enemy nearest where the host says it stands)
                    CharacterBody Aim() => TeamComponent.GetTeamMembers(TeamIndex.Monster).Select(t => t ? t.body : null)
                        .Where(b => b && b.healthComponent && b.healthComponent.alive).OrderBy(b => Vector3.Distance(b.corePosition, near)).FirstOrDefault();
                    var first = Aim();
                    Log.Info($"Melty test: co-op joiner fires {rowId}: at {body.footPosition}, nearest enemy " + (first ? $"{Vector3.Distance(first.corePosition, body.corePosition):F1} m away" : "none") + $" ({TeamComponent.GetTeamMembers(TeamIndex.Monster).Count} monsters)");
                    if (first)
                    {
                        // this copy runs slowly beside the host: turn the camera (which sets the aim) until the crosshair
                        // really sits on the enemy, up to 5 s, before pressing anything
                        float ta = Time.unscaledTime; int steady = 0;
                        while (Time.unscaledTime - ta < 5f && steady < 5)
                        {
                            var look = (first.corePosition - body.inputBank.aimOrigin).normalized;
                            AimCamera(body, look);
                            body.inputBank.aimDirection = look;
                            yield return null;
                            steady = Vector3.Angle(body.inputBank.aimDirection, look) < 2f ? steady + 1 : 0;
                        }
                        Log.Info($"Melty test: co-op joiner aim {Vector3.Angle(body.inputBank.aimDirection, (first.corePosition - body.inputBank.aimOrigin).normalized):F1} deg off the enemy");
                    }
                    if (slot == "primary") { fireBody = body; fireUntil = Time.time + window; body.skillLocator.primary.ExecuteIfReady(); }
                    else if (slot == "equipment") body.requestEquipmentFire = true;
                    else
                    {
                        var gs = slot == "secondary" ? body.skillLocator.secondary : slot == "utility" ? body.skillLocator.utility : body.skillLocator.special;
                        gs.Reset();
                        gs.ExecuteIfReady();
                    }
                    float t0 = Time.time; int shots = 0;
                    while (Time.time - t0 < window)
                    {
                        var b = user.cachedBody;
                        var target = Aim();
                        if (b && target)
                        {
                            var look = (target.corePosition - b.inputBank.aimOrigin).normalized;
                            AimCamera(b, look);
                            b.inputBank.aimDirection = look;
                        }
                        if (shots < 2 && Time.time - t0 > new[] { 0.5f, 1.4f }[shots]) { Shot(rowId + "_join" + (shots + 1)); shots++; }
                        yield return null;
                    }
                    fireBody = null;
                    Put(AckFile, $"done|{n}");
                }
            }
        }
    }
}
