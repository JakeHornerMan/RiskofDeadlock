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
        // optional "only": hero ids, comma-separated; the bench then runs those heroes only (plus Commando and their RoR2
        // matches unless "survivors" is "heroes")
        static string only = "";
        static readonly HashSet<string> ReleasePrimary = new HashSet<string> { "CaptainBody" };
        // optional "survivors": "all" = every playable survivor (the DPS library, docs/dps-library.json); "heroes" = our heroes
        // only, no Commando or matches (they are stored in the library, so they are never re-measured); else Commando + heroes + matches
        static string survivorsMode = "";
        // optional "ults": 1 = the ult bench (creator, 2026-10-24): every hero's Q (and Commando's Preon Accumulator) once against a
        // bigger pack of 30 (5 rows of 6) plus a Beetle Queen behind it, for the "just for fun" table; no other passes
        static bool ults;
        const string UltBoss = "BeetleQueenMaster";
        // optional "repeat": how many times the whole set of passes runs in this session (each run is headed "run <k>")
        static int repeat = 1;
        // optional "portraits": a folder; studio renders of every hero, its RoR2 match and its ult figure go there first
        static string portraits = "";
        public static bool Active => outFile != null;

        public static void Init()
        {
            try
            {
                var f = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeadlockOfRain", "bench.json");
                if (!File.Exists(f)) return;
                var text = File.ReadAllText(f);
                outFile = Value(text, "out") ?? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(f), "bench_results.txt");
                only = Value(text, "only") ?? "";
                portraits = Value(text, "portraits") ?? "";
                survivorsMode = Value(text, "survivors") ?? "";
                if (int.TryParse(Value(text, "repeat"), out var rp) && rp > 0) repeat = rp;
                ults = (Value(text, "ults") ?? "0") != "0";
                // seconds 0: portraits only, no passes
                if (float.TryParse(Value(text, "seconds"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) && s >= 0) seconds = s;
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
            public double Raw, Pct, BossPct; public int Hits; public readonly HashSet<CharacterBody> Victims = new HashSet<CharacterBody>();
            public readonly int[] Uses = new int[5];
        }
        Tally tally;
        CharacterBody benchBody;

        void OnDamage(DamageReport r)
        {
            if (tally == null || r == null || !benchBody || !r.victimBody || r.victimBody == benchBody) return;
            // the survivor's own hits, and its minions' (Engineer's turrets) as part of its kit
            bool own = r.attackerBody == benchBody
                || r.attackerMaster && r.attackerMaster.minionOwnership && r.attackerMaster.minionOwnership.ownerMaster == benchBody.master;
            if (!own) return;
            // damageInfo.damage: what the hit was worth after crits and damage mods, before the victim's armor and health cap
            float dmg = r.damageInfo != null ? r.damageInfo.damage : r.damageDealt;
            tally.Raw += dmg;
            tally.Pct += dmg / Mathf.Max(1f, benchBody.damage) * 100f;
            tally.Hits++;
            tally.Victims.Add(r.victimBody);
            if (r.victimBody.isChampion) tally.BossPct += dmg / Mathf.Max(1f, benchBody.damage) * 100f;
        }

        void OnSkill(GenericSkill s)
        {
            if (tally == null || !benchBody || !s) return;
            var sl = benchBody.skillLocator;
            int i = s == sl.primary ? 0 : s == sl.secondary ? 1 : s == sl.utility ? 2 : s == sl.special ? 3 : -1;
            if (i >= 0) tally.Uses[i]++;
        }

        void OnEquipment(EquipmentSlot slot, EquipmentIndex index)
        {
            if (tally != null && benchBody && slot && slot.characterBody == benchBody) tally.Uses[4]++;
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
            if (portraits != "") yield return Portraits(user.cachedBody);
            if (seconds <= 0f) { Write("DPS bench finished"); yield break; }

            // one fixed flat spot, a pack 10 m in front: three rows of four, 2.5 m apart. The spot must see the whole pack: a rock
            // or a slope between them once left a run with almost no hits, so up to 16 spot/direction pairs are tried and the
            // first where every pack spot is on ground within 2 m of the arena's height and in clear line of sight wins.
            var body = user.cachedBody;
            var start = body.footPosition;
            var aim0 = body.inputBank.aimDirection; aim0.y = 0; aim0.Normalize();
            List<Vector3> spots = null;
            int tries = 0, seen = 0;
            int rows = ults ? 5 : 3, cols = ults ? 6 : 4, want = rows * cols;
            // the ult pack is wide: up to 4 m of height and no line of sight needed (ults reach round corners too); if no
            // spot holds all 30, the best one (24+) is used and the rest are filled in beside its spots
            float tolerance = ults ? 4f : 2f;
            List<Vector3> best = null; Vector3 bestCentre = default, bestFwd = default;
            for (int t = 0; t < 16 && spots == null; t++)
            {
                tries = t + 1;
                var dir = Quaternion.Euler(0f, 45f * (t % 8), 0f) * aim0;
                var from = start + dir * (t < 8 ? 14f : 30f);
                if (!Physics.Raycast(from + Vector3.up * 30f, Vector3.down, out var gh, 80f, LayerIndex.world.mask)) continue;
                var centre = gh.point;
                var fwd = Quaternion.Euler(0f, 45f * (t % 8), 0f) * aim0;
                var rgt = Vector3.Cross(Vector3.up, fwd);
                var cand = new List<Vector3>(); seen = 0;
                for (int row = 0; row < rows; row++)
                    for (int col = 0; col < cols; col++)
                    {
                        var p = centre + fwd * (10f + row * 2.5f) + rgt * ((col - (cols - 1) / 2f) * 2.5f);
                        if (!Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var g, 15f, LayerIndex.world.mask)) continue;
                        if (Mathf.Abs(g.point.y - centre.y) > tolerance) continue;
                        if (!ults && Physics.Linecast(centre + Vector3.up * 1.5f, g.point + Vector3.up * 1f, LayerIndex.world.mask)) continue;
                        cand.Add(g.point); seen++;
                    }
                if (cand.Count == want) { spots = cand; arena = centre; forward = fwd; }
                else if (best == null || cand.Count > best.Count) { best = cand; bestCentre = centre; bestFwd = fwd; }
            }
            if (spots == null && ults && best != null && best.Count >= want * 0.8f)
            {
                Write($"arena: no spot holds all {want}; using the best ({best.Count}) and filling in {want - best.Count} beside its spots");
                spots = new List<Vector3>(best); arena = bestCentre; forward = bestFwd;
                var side = Vector3.Cross(Vector3.up, forward);
                for (int k = 0; spots.Count < want; k++) spots.Add(best[k % best.Count] + side * 1.25f);
            }
            if (spots == null) { Write("FAILED: no flat spot with a clear view of the pack on this stage"); yield break; }
            TeleportHelper.TeleportBody(body, arena + Vector3.up * 0.5f, false);
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < want; i++)
            {
                int row = i / cols, col = i % cols;
                packSpots.Add(spots[i] + Vector3.up * 0.5f);
                packMasters.Add(ults ? (i % 3 == 2 ? "BeetleMaster" : "LemurianMaster")
                    : row == 1 && (col == 0 || col == 3) || row == 2 && (col == 1 || col == 2) ? "BeetleMaster" : "LemurianMaster");
            }
            if (ults)
            {
                // the boss stands behind the pack, on the ground in line with its middle
                var bp = arena + forward * (10f + rows * 2.5f + 5f);
                if (Physics.Raycast(bp + Vector3.up * 20f, Vector3.down, out var bg, 60f, LayerIndex.world.mask)) bp = bg.point;
                packSpots.Add(bp + Vector3.up * 0.5f);
                packMasters.Add(UltBoss);
            }
            Write($"arena: spot {tries} of 16 tried, all {want} pack spots flat and in clear view");
            Write($"setup: difficulty {Run.instance.selectedDifficulty}, pack {packSpots.Count} ({packMasters.Count(m => m == "LemurianMaster")} Lemurians, {packMasters.Count(m => m == "BeetleMaster")} Beetles{(ults ? ", 1 Beetle Queen" : "")}) at 10-{10 + rows * 2.5f:F0} m, {seconds}s per pass{(ults ? " (ults only)" : "")}");

            GlobalEventManager.onServerDamageDealt += OnDamage;
            var master = user.cachedMaster;
            var pcmc = master.GetComponent<PlayerCharacterMasterController>();
            var survivors = survivorsMode == "heroes" && !ults ? new List<string>() : new List<string> { "CommandoBody" };
            if (survivorsMode == "all")
            {
                // every survivor this run can use: not hidden, and its expansion (DLC) enabled in this run
                foreach (var sd in SurvivorCatalog.orderedSurvivorDefs)
                {
                    if (!sd || sd.hidden || !sd.bodyPrefab) continue;
                    var req = sd.bodyPrefab.GetComponent<RoR2.ExpansionManagement.ExpansionRequirementComponent>();
                    if (req && req.requiredExpansion && !Run.instance.IsExpansionEnabled(req.requiredExpansion)) { Write($"skipped {sd.bodyPrefab.name}: its expansion is not enabled"); continue; }
                    // "only" in this mode: a comma-separated list of body names to measure (Commando always runs as the baseline)
                    if (only != "" && !only.Split(',').Contains(sd.bodyPrefab.name)) continue;
                    if (!survivors.Contains(sd.bodyPrefab.name)) survivors.Add(sd.bodyPrefab.name);
                }
            }
            else
                foreach (var h in HeroRow.All)
                {
                    if (!Survivors.Bodies.ContainsKey(h.Id) || only != "" && !only.Split(',').Contains(h.Id)) continue;
                    if (survivorsMode == "heroes" || ults) { survivors.Add(Survivors.Bodies[h.Id].name); continue; }
                    var match = StatRow.All.FirstOrDefault(r => r.Hero == h.Id)?.Analog;
                    if (!string.IsNullOrEmpty(match) && match != "none" && !survivors.Contains(match) && BodyCatalog.FindBodyPrefab(match)) survivors.Add(match);
                    survivors.Add(Survivors.Bodies[h.Id].name);
                }
            for (int run = 1; run <= repeat; run++)
            {
                if (repeat > 1) Write($"run {run}");
                foreach (var bodyName in survivors)
                {
                    if (master.GetBody() == null || master.GetBody().name.Replace("(Clone)", "") != bodyName)
                    {
                        master.TransformBody(bodyName);
                        yield return new WaitForSeconds(1.5f);
                    }
                    if (run == 1 && !ults) { WriteStats(bodyName); WriteSkills(master.GetBody(), bodyName); }
                    // the Q pass only where it is shown: the heroes' ults and Commando's Preon Accumulator
                    bool equip = survivorsMode != "all" || bodyName == "CommandoBody" || Survivors.HeroOf(master.GetBody()) != null;
                    foreach (var mode in new[] { "all", "primary", "secondary", "utility", "special", "equipment" })
                    {
                        if (mode == "equipment" && !equip || ults && mode != "equipment") continue;
                        yield return Pass(master, pcmc, bodyName.Replace("Body", ""), mode);
                    }
                }
            }
            GlobalEventManager.onServerDamageDealt -= OnDamage;
            if (pcmc) pcmc.enabled = true;
            foreach (var b in pack) if (b && b.master) b.master.TrueKill();
            Write("DPS bench finished");
        }

        /// <summary>The survivor's base stats, as the body prefab has them (the pages show them beside the hero's).</summary>
        void WriteStats(string bodyName)
        {
            var b = BodyCatalog.FindBodyPrefab(bodyName)?.GetComponent<CharacterBody>();
            if (!b) return;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            Write(string.Format(inv, "stats {0}: name {1}, health {2} +{3}, regen {4} +{5}, armor {6}, damage {7} +{8}, speed {9}, jumps {10}, crit {11}",
                bodyName.Replace("Body", ""), Language.GetString(b.baseNameToken), b.baseMaxHealth, b.levelMaxHealth, b.baseRegen, b.levelRegen,
                b.baseArmor, b.baseDamage, b.levelDamage, b.baseMoveSpeed, b.baseJumpCount, b.baseCrit));
        }

        /// <summary>The survivor's default skill names per slot, in the game's language (for the DPS library page).</summary>
        void WriteSkills(CharacterBody body, string bodyName)
        {
            if (!body || !body.skillLocator) return;
            var sl = body.skillLocator;
            string N(GenericSkill g) => g && g.skillDef ? Language.GetString(g.skillDef.skillNameToken).Replace(";", ",").Replace("=", "-") : "none";
            Write($"skills {bodyName.Replace("Body", "")}: primary={N(sl.primary)}; secondary={N(sl.secondary)}; utility={N(sl.utility)}; special={N(sl.special)}");
        }

        static void KillMinions(CharacterMaster owner)
        {
            foreach (var m in CharacterMaster.readOnlyInstancesList.ToList())
                if (m && m != owner && m.minionOwnership && m.minionOwnership.ownerMaster == owner) m.TrueKill();
        }

        // ---------------------------------------------------------------- studio portraits (dev only, for the kit pages)
        /// <summary>Each hero, its RoR2 match and its ult's pickup figure, alone high above the stage with a key light and a
        /// rim light in the hero's colour. Each is rendered twice, on black and on white, so tools/portraits.py can cut it out
        /// (alpha = 1 - (white - black)).</summary>
        IEnumerator Portraits(CharacterBody near)
        {
            Directory.CreateDirectory(portraits);
            var origin = near.transform.position + Vector3.up * 400f;
            var jobs = new List<(string name, GameObject prefab, Color rim, bool figure)>();
            foreach (var h in HeroRow.All)
            {
                var rim = Sheet.Color(h.PrimaryColor);
                if (Survivors.Defs.TryGetValue(h.Id, out var d) && d.displayPrefab) jobs.Add((h.Id, d.displayPrefab, rim, false));
                var match = StatRow.All.FirstOrDefault(r => r.Hero == h.Id)?.Analog;
                // the match: its body's own model (the character-select display prefabs of vanilla survivors render empty, doc 02 §10.1),
                // the display prefab only when the body has none
                var bodyPrefab = string.IsNullOrEmpty(match) ? null : BodyCatalog.FindBodyPrefab(match);
                var ml = bodyPrefab ? bodyPrefab.GetComponent<ModelLocator>() : null;
                var md = bodyPrefab ? SurvivorCatalog.FindSurvivorDefFromBody(bodyPrefab) : null;
                var matchModel = ml && ml.modelTransform ? ml.modelTransform.gameObject : md ? md.displayPrefab : null;
                if (matchModel && !jobs.Any(j => j.name == match)) jobs.Add((match, matchModel, new Color(0.75f, 0.85f, 0.8f), false));
                var def = Equipment.OfHero(h.Id);
#pragma warning disable CS0618
                if (def && def.pickupModelPrefab) jobs.Add((h.Id + "_figure", def.pickupModelPrefab, rim, true));
#pragma warning restore CS0618
            }
            foreach (var job in jobs)
            {
                GameObject go = null;
                try
                {
                    go = Instantiate(job.prefab, origin, Quaternion.LookRotation(Vector3.back));
                    go.SetActive(true);
                }
                catch (Exception e) { Log.Warn($"portrait {job.name}: {e.Message}"); }
                if (!go) continue;
                // let the character-select intro settle into the idle pose
                yield return new WaitForSeconds(job.figure ? 0.3f : 3f);
                // vanilla display models dither themselves out near a camera (CharacterModel's fade): switch it off, fully opaque
                if (!go) continue;
                foreach (var cm in go.GetComponentsInChildren<CharacterModel>()) cm.enabled = false;
                var block = new MaterialPropertyBlock();
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    r.GetPropertyBlock(block); block.SetFloat("_Fade", 1f); r.SetPropertyBlock(block);
                }
                try { Render(go, job.name, job.rim, job.figure, origin); }
                catch (Exception e) { Log.Warn($"portrait {job.name}: {e.Message}"); }
                Destroy(go);
                yield return null;
            }
            // Puddle Punch's fist (its hit effect) caught at the top of its punch, turned to the studio camera
            foreach (var r in SkillRow.All.Where(r => r.Kind == "placed_punch"))
            {
                var fx = Vfx.Play(r.Vfx, "hit", origin, r.Radius);
                if (!fx) continue;
                fx.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, 22f, 0f) * Vector3.back);
                Vfx.StudioView = origin + Vector3.up * 4f + Quaternion.Euler(0f, 22f, 0f) * Vector3.back * 30f;
                yield return new WaitForSeconds(0.4f);
                try { Render(fx, r.Id + "_fx", Sheet.Color(HeroRow.All.First(h => h.Id == r.Hero).PrimaryColor), false, origin); }
                catch (Exception e) { Log.Warn($"portrait {r.Id}: {e.Message}"); }
                Destroy(fx);
                Vfx.StudioView = null;
                yield return null;
            }
            Write($"portraits: {jobs.Count} written to {portraits}");
        }

        void Render(GameObject go, string name, Color rim, bool figure, Vector3 origin)
        {
            var b = new Bounds(origin + Vector3.up, Vector3.one * 0.1f); bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            Log.Info($"portrait {name}: {go.GetComponentsInChildren<Renderer>(true).Length} renderers, {(any ? "bounds " + b.size : "none drawn")}");
            if (figure) b = new Bounds(b.center, b.size / Equipment.FigurePadding);
            float h = Mathf.Max(0.3f, b.size.y);
            // a front three-quarter view, slightly from above for the figure, at eye level for the heroes
            var dir = Quaternion.Euler(0f, figure ? 30f : 22f, 0f) * Vector3.back;
            const float fov = 28f;
            float dist = h * 0.62f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) + b.extents.z;
            var target = b.center;
            var lights = new List<GameObject>();
            void L(Vector3 at, Color c, float intensity, float range, LightType type = LightType.Point)
            {
                var lg = new GameObject("PortraitLight"); lg.transform.position = at;
                lg.transform.rotation = Quaternion.LookRotation(target - at);
                var l = lg.AddComponent<Light>(); l.type = type; l.color = c; l.intensity = intensity; l.range = range;
                l.renderMode = LightRenderMode.ForcePixel;
                lights.Add(lg);
            }
            var right = Vector3.Cross(Vector3.up, -dir).normalized;
            // a three-point setup: a warm directional key from front-left and above, a cool fill from the front-right,
            // and two strong rim lights in the hero's colour behind
            L(target + dir * h * 2f - right * h * 1.5f + Vector3.up * h * 2f, new Color(1f, 0.92f, 0.82f), 1.6f, 0f, LightType.Directional);
            L(target + dir * h * 1.2f + right * h * 1.2f + Vector3.up * h * 0.3f, new Color(0.75f, 0.82f, 1f), 6f, h * 4f);
            L(target - dir * h * 0.7f + right * h * 0.8f + Vector3.up * h * 0.4f, rim, 16f, h * 2.5f);
            L(target - dir * h * 0.7f - right * h * 0.8f + Vector3.up * h * 0.6f, rim, 12f, h * 2.5f);
            var cg = new GameObject("PortraitCamera");
            var c = cg.AddComponent<Camera>();
            if (Camera.main) c.CopyFrom(Camera.main);
            c.transform.position = target + dir * dist + Vector3.up * h * (figure ? 0.25f : 0.05f);
            c.transform.rotation = Quaternion.LookRotation(target - c.transform.position);
            c.fieldOfView = fov; c.nearClipPlane = 0.05f; c.farClipPlane = dist + h * 4f;
            c.clearFlags = CameraClearFlags.SolidColor;
            int w = figure ? 800 : 1200, hh = figure ? 800 : 1500;
            var rt = new RenderTexture(w, hh, 24);
            c.targetTexture = rt; c.aspect = (float)w / hh;
            var tex = new Texture2D(w, hh, TextureFormat.RGB24, false);
            foreach (var (bg, suffix) in new[] { (Color.black, "black"), (Color.white, "white") })
            {
                c.backgroundColor = bg;
                c.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, hh), 0, 0); tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(System.IO.Path.Combine(portraits, $"{name}_{suffix}.png"), tex.EncodeToPNG());
            }
            c.targetTexture = null; Destroy(rt); Destroy(cg); Destroy(tex);
            foreach (var l in lights) Destroy(l);
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
            // every slot starts ready. Balance is measured on the main moves only: "all" holds no equipment. The Q ults are
            // pickups any survivor can carry and are left out of balance; the "equipment" pass fires the hero's own ult
            // (Commando: the Preon Accumulator) once, for the "just for fun" table
            var sl = body.skillLocator;
            foreach (var gs in new[] { sl.primary, sl.secondary, sl.utility, sl.special }) if (gs) gs.Reset();
            bool useEquip = mode == "equipment";
            var inv = body.inventory;
            if (inv)
            {
                var def = Equipment.OfHero(Survivors.HeroOf(body)) ?? (Survivors.HeroOf(body) == null ? RoR2Content.Equipment.BFG : null);
                var loc = inv.activeEquipmentLocation;
                inv.SetEquipment(new EquipmentState(useEquip && def ? def.equipmentIndex : EquipmentIndex.None, Run.FixedTimeStamp.now, 1), loc);
            }
            KillMinions(master);
            benchBody = body;
            body.onSkillActivatedServer += OnSkill;
            EquipmentSlot.onServerEquipmentActivated += OnEquipment;
            tally = new Tally();
            float start = Time.time, nextRefill = 0f; int frame = 0;
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
                    // a primary that fires on release (Captain's Vulcan Shotgun charges while held) is let go for one frame every 0.6 s
                    bool hold = mode == "all" || mode == "primary";
                    if (ReleasePrimary.Contains(body.name.Replace("(Clone)", "")) && frame % 36 == 0) hold = false;
                    ib.skill1.PushState(hold);
                    ib.skill2.PushState((mode == "all" || mode == "secondary") && tap);
                    ib.skill3.PushState((mode == "all" || mode == "utility") && tap);
                    ib.skill4.PushState((mode == "all" || mode == "special") && tap);
                    ib.activateEquipment.PushState(useEquip && tap);
                }
                // dashes and leaps carry the survivor off the spot: once they are back in their main state, put them back
                var esm = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
                // (not while Billy's air combo runs: up in the air with a floating pack is where his moves land)
                if (esm && esm.state is EntityStates.GenericCharacterMain && (body.footPosition - arena).sqrMagnitude > 16f && !BillyAir.InCombo(body))
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
            EquipmentSlot.onServerEquipmentActivated -= OnEquipment;
            var b2 = master.GetBody();
            if (b2 && b2.inputBank) { b2.inputBank.skill1.PushState(false); b2.inputBank.skill2.PushState(false); b2.inputBank.skill3.PushState(false); b2.inputBank.skill4.PushState(false); b2.inputBank.activateEquipment.PushState(false); }
            float dur = Mathf.Max(0.1f, seconds);
            var sb = new StringBuilder();
            sb.Append($"{who} {mode}: raw {t.Raw:F0} ({t.Raw / dur:F1}/s), {t.Pct:F0}% of damage ({t.Pct / dur:F0}%/s), hits {t.Hits}, enemies hit {t.Victims.Count}, ");
            sb.Append($"uses M1 {t.Uses[0]} M2 {t.Uses[1]} Shift {t.Uses[2]} R {t.Uses[3]} Q {t.Uses[4]}, level {(b2 ? b2.level : 0):F0}, damage stat {(b2 ? b2.damage : 0):F1}");
            if (ults) sb.Append($", boss {t.BossPct:F0}%");
            Write(sb.ToString());
            // let projectiles, storms and DoTs from this pass end before the next one is counted
            yield return new WaitForSeconds(2.5f);
        }
    }
}
