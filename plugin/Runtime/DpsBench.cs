using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>Dev-only DPS benchmark (docs/05-DAMAGE-COMPARISON.md). Active only while
    /// %LOCALAPPDATA%\DeadlockOfRain\bench.json exists ({ "out": "&lt;file&gt;", "seconds": 30 }); players never have it.
    /// Starts a Monsoon run, and for Commando and each hero: stands on the same spot facing the same pack of 12 sturdy
    /// enemies (8 Lemurians, 4 Beetles; AI off, so the pack never moves or fights back), then plays timed passes like a
    /// player mashing every key: "all" (gun held, every ability and R pressed whenever ready), then each slot alone.
    /// Records every hit the survivor lands (direct, projectiles, DoTs, item procs) as raw damage and as % of their
    /// damage stat, and writes one line per pass to the out file.</summary>
    public sealed class DpsBench : MonoBehaviour
    {
        static string outFile;
        static float seconds = 30f;
        public static bool Active => outFile != null;

        public static void Init()
        {
            try
            {
                var f = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "bench.json");
                if (!File.Exists(f)) return;
                var text = File.ReadAllText(f);
                outFile = Value(text, "out") ?? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(f), "bench_results.txt");
                if (float.TryParse(Value(text, "seconds"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) && s > 0) seconds = s;
                Log.Info($"DPS bench: {seconds}s passes, writing to {outFile}");
                var go = new GameObject("DeadlockOfRainDpsBench");
                DontDestroyOnLoad(go);
                go.AddComponent<DpsBench>();
            }
            catch (Exception e) { Log.Warn("DPS bench off: " + e.Message); }
        }

        static string Value(string json, string key)
        {
            int i = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (i < 0) return null;
            int c = json.IndexOf(':', i) + 1;
            while (c < json.Length && char.IsWhiteSpace(json[c])) c++;
            if (c < json.Length && json[c] == '"') { int e = json.IndexOf('"', c + 1); return json.Substring(c + 1, e - c - 1).Replace("\\\\", "\\"); }
            int end = c; while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.')) end++;
            return json.Substring(c, end - c);
        }

        static void Write(string line)
        {
            Log.Info("DPS bench: " + line);
            try { File.AppendAllText(outFile, line + Environment.NewLine); } catch (Exception e) { Log.Warn(e.Message); }
        }

        // ---------------------------------------------------------------- the pack
        readonly List<CharacterBody> pack = new List<CharacterBody>();
        readonly List<Vector3> packSpots = new List<Vector3>();
        readonly List<string> packMasters = new List<string>();
        Vector3 arena, forward;

        void SpawnPack()
        {
            foreach (var b in pack) if (b && b.master) b.master.TrueKill();
            pack.Clear();
            for (int i = 0; i < packSpots.Count; i++) pack.Add(Summon(packMasters[i], packSpots[i]));
        }

        void RefillPack()
        {
            for (int i = 0; i < pack.Count; i++)
                if (!pack[i] || !pack[i].healthComponent || !pack[i].healthComponent.alive) pack[i] = Summon(packMasters[i], packSpots[i]);
        }

        static CharacterBody Summon(string masterName, Vector3 at)
        {
            var prefab = MasterCatalog.FindMasterPrefab(masterName);
            if (!prefab) return null;
            var m = new MasterSummon { masterPrefab = prefab, position = at, rotation = Quaternion.identity, teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform();
            if (!m) return null;
            foreach (var ai in m.GetComponents<RoR2.CharacterAI.BaseAI>()) ai.enabled = false;
            // sturdy enough to live through every pass (×101 health), so nothing dies, no XP is gained and levels stay put
            if (m.inventory) m.inventory.GiveItem(RoR2Content.Items.BoostHp, 1000);
            return m.GetBody();
        }

        // ---------------------------------------------------------------- recording
        sealed class Tally
        {
            public double Raw, Pct; public int Hits; public readonly HashSet<CharacterBody> Victims = new HashSet<CharacterBody>();
            public readonly int[] Uses = new int[4];
        }
        Tally tally;
        CharacterBody benchBody;

        void OnDamage(DamageReport r)
        {
            if (tally == null || r == null || !benchBody || r.attackerBody != benchBody || !r.victimBody || r.victimBody == benchBody) return;
            // damageInfo.damage: what the hit was worth after crits and damage mods, before the victim's armor and health cap
            float dmg = r.damageInfo != null ? r.damageInfo.damage : r.damageDealt;
            tally.Raw += dmg;
            tally.Pct += dmg / Mathf.Max(1f, r.attackerBody.damage) * 100f;
            tally.Hits++;
            tally.Victims.Add(r.victimBody);
        }

        void OnSkill(GenericSkill s)
        {
            if (tally == null || !benchBody || !s) return;
            var sl = benchBody.skillLocator;
            int i = s == sl.primary ? 0 : s == sl.secondary ? 1 : s == sl.utility ? 2 : s == sl.special ? 3 : -1;
            if (i >= 0) tally.Uses[i]++;
        }

        // ---------------------------------------------------------------- the run
        IEnumerator Start()
        {
            while (!RoR2Application.loadFinished) yield return null;
            try { File.WriteAllText(outFile, ""); } catch { }
            yield return new WaitForSeconds(3f);
            var user = LocalUserManager.GetFirstLocalUser();
            while (user == null) { yield return null; user = LocalUserManager.GetFirstLocalUser(); }
            user.userProfile.SetSurvivorPreference(SurvivorCatalog.FindSurvivorDefFromBody(BodyCatalog.FindBodyPrefab("CommandoBody")));
            RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode ClassicRun; host 0;\"");
            float t0 = Time.unscaledTime;
            while (!PreGameController.instance && Time.unscaledTime - t0 < 60f) yield return null;
            yield return new WaitForSeconds(2f);
            if (PreGameController.instance) RoR2.Console.instance.SubmitCmd(null, "pregame_start_run");
            t0 = Time.unscaledTime;
            while ((user.cachedBody == null || !Run.instance) && Time.unscaledTime - t0 < 120f) yield return null;
            if (!user.cachedBody) { Write("FAILED: the run did not start"); yield break; }
            yield return new WaitForSeconds(4f);
            Run.instance.selectedDifficulty = DifficultyIndex.Hard; // Monsoon
            var first = user.cachedBody;
            if (first.currentVehicle) first.currentVehicle.EjectPassenger(first.gameObject);
            yield return new WaitForSeconds(2f);
            MeltyTest.QuietStage();

            // one fixed flat spot, a pack 10 m in front: three rows of four, 2.5 m apart
            var body = user.cachedBody;
            var away = body.footPosition + body.inputBank.aimDirection.normalized * 14f;
            if (Physics.Raycast(away + Vector3.up * 30f, Vector3.down, out var gh, 80f, LayerIndex.world.mask)) TeleportHelper.TeleportBody(body, gh.point + Vector3.up * 0.5f, false);
            yield return new WaitForSeconds(0.5f);
            arena = body.footPosition;
            forward = body.inputBank.aimDirection; forward.y = 0; forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 4; col++)
                {
                    var p = arena + forward * (10f + row * 2.5f) + right * ((col - 1.5f) * 2.5f);
                    if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var g, 15f, LayerIndex.world.mask)) p = g.point;
                    packSpots.Add(p + Vector3.up * 0.5f);
                    packMasters.Add(row == 1 && (col == 0 || col == 3) || row == 2 && (col == 1 || col == 2) ? "BeetleMaster" : "LemurianMaster");
                }
            Write($"setup: difficulty {Run.instance.selectedDifficulty}, pack {packSpots.Count} ({packMasters.Count(m => m == "LemurianMaster")} Lemurians, {packMasters.Count(m => m == "BeetleMaster")} Beetles) at 10-15 m, {seconds}s per pass");

            GlobalEventManager.onServerDamageDealt += OnDamage;
            var master = user.cachedMaster;
            var pcmc = master.GetComponent<PlayerCharacterMasterController>();
            var survivors = new List<string> { "CommandoBody" };
            foreach (var h in HeroRow.All) if (Survivors.Bodies.ContainsKey(h.Id)) survivors.Add(Survivors.Bodies[h.Id].name);
            foreach (var bodyName in survivors)
            {
                if (master.GetBody() == null || master.GetBody().name.Replace("(Clone)", "") != bodyName)
                {
                    master.TransformBody(bodyName);
                    yield return new WaitForSeconds(1.5f);
                }
                foreach (var mode in new[] { "all", "primary", "secondary", "utility", "special" })
                {
                    yield return Pass(master, pcmc, bodyName.Replace("Body", ""), mode);
                }
            }
            GlobalEventManager.onServerDamageDealt -= OnDamage;
            if (pcmc) pcmc.enabled = true;
            foreach (var b in pack) if (b && b.master) b.master.TrueKill();
            Write("DPS bench finished");
        }

        IEnumerator Pass(CharacterMaster master, PlayerCharacterMasterController pcmc, string who, string mode)
        {
            var body = master.GetBody();
            if (!body) { Write($"{who} {mode}: no body"); yield break; }
            body.healthComponent.godMode = true;
            // the player's input is switched off: the bench aims and presses the keys
            if (pcmc) pcmc.enabled = false;
            TeleportHelper.TeleportBody(body, arena + Vector3.up * 0.5f, false);
            if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
            SpawnPack();
            yield return new WaitForSeconds(1f);
            // every slot starts ready; an R that is a charge-meter ultimate starts empty in "all" (as in a real run) and full in "special"
            var sl = body.skillLocator;
            foreach (var gs in new[] { sl.primary, sl.secondary, sl.utility, sl.special }) if (gs) gs.Reset();
            var ult = UltController.Of(body);
            if (ult) { ult.Consume(); if (mode == "special") ult.Add(ult.Required); }
            benchBody = body;
            body.onSkillActivatedServer += OnSkill;
            tally = new Tally();
            float start = Time.time, nextRefill = 0f; int frame = 0, ultUsedAt = -1;
            while (Time.time - start < seconds)
            {
                body = master.GetBody();
                if (!body) break;
                var ib = body.inputBank;
                // aim at the nearest live enemy of the pack
                CharacterBody aimAt = null; float best = float.MaxValue;
                foreach (var p in pack)
                {
                    if (!p || !p.healthComponent || !p.healthComponent.alive) continue;
                    float d = (p.corePosition - body.corePosition).sqrMagnitude;
                    if (d < best) { best = d; aimAt = p; }
                }
                if (aimAt && ib)
                {
                    ib.aimDirection = (aimAt.corePosition - ib.aimOrigin).normalized;
                    if (frame % 10 == 0) MeltyTest.AimCamera(body, ib.aimDirection);
                    ib.moveVector = Vector3.zero;
                }
                // keys: the gun held down; abilities and R pressed again every other frame, so each fires the moment it is ready
                bool tap = frame % 2 == 0;
                if (ib)
                {
                    ib.skill1.PushState(mode == "all" || mode == "primary");
                    ib.skill2.PushState((mode == "all" || mode == "secondary") && tap);
                    ib.skill3.PushState((mode == "all" || mode == "utility") && tap);
                    ib.skill4.PushState((mode == "all" || mode == "special") && tap);
                }
                if (mode == "special" && ult && tally.Uses[3] > 0 && ultUsedAt < 0) ultUsedAt = (int)(Time.time - start);
                // dashes and leaps carry the survivor off the spot: once they are back in their main state, put them back
                var esm = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
                if (esm && esm.state is EntityStates.GenericCharacterMain && (body.footPosition - arena).sqrMagnitude > 16f)
                {
                    TeleportHelper.TeleportBody(body, arena + Vector3.up * 0.5f, false);
                    if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
                }
                if (Time.time >= nextRefill) { nextRefill = Time.time + 1f; RefillPack(); }
                frame++;
                yield return new WaitForFixedUpdate();
            }
            var t = tally; tally = null;
            if (body) body.onSkillActivatedServer -= OnSkill;
            var b2 = master.GetBody();
            if (b2 && b2.inputBank) { b2.inputBank.skill1.PushState(false); b2.inputBank.skill2.PushState(false); b2.inputBank.skill3.PushState(false); b2.inputBank.skill4.PushState(false); }
            float dur = Mathf.Max(0.1f, seconds);
            var sb = new StringBuilder();
            sb.Append($"{who} {mode}: raw {t.Raw:F0} ({t.Raw / dur:F1}/s), {t.Pct:F0}% of damage ({t.Pct / dur:F0}%/s), hits {t.Hits}, enemies hit {t.Victims.Count}, ");
            sb.Append($"uses M1 {t.Uses[0]} M2 {t.Uses[1]} Shift {t.Uses[2]} R {t.Uses[3]}, level {(b2 ? b2.level : 0):F0}, damage stat {(b2 ? b2.damage : 0):F1}");
            if (ult) sb.Append($", ult meter end {ult.Fraction * 100f:F0}%");
            Write(sb.ToString());
            // let projectiles, storms and DoTs from this pass end before the next one is counted
            yield return new WaitForSeconds(2.5f);
        }
    }
}
