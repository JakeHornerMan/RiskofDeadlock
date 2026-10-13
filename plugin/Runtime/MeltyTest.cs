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
    /// starts a run, plays every hero (gun, three abilities, ult charge from real damage, ult), levels up, checks stats and
    /// scale, and appends "pass name" / "fail name: why" lines to the checks file Melty gives.</summary>
    public sealed class MeltyTest : MonoBehaviour
    {
        const string Slug = "deadlock-of-rain";
        static string checksFile;
        static MeltyTest instance;
        static readonly HashSet<string> seen = new HashSet<string>();
        static readonly HashSet<string> written = new HashSet<string>();
        static CharacterBody fireBody, ultBody;
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
        public static bool WantsUlt(CharacterBody b) => Active && b && b == ultBody;

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

            // 3. every hero
            var master = user.cachedMaster;
            var commandoCap = BodyCatalog.FindBodyPrefab("CommandoBody").GetComponent<CapsuleCollider>();
            bool scaleOk = true; string scaleWhy = "";
            bool levelOk = true; string levelWhy = "";
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
                }

                // scale: capsule follows Deadlock's relative height, model uses the shared factor
                var cap = body.GetComponent<CapsuleCollider>();
                float want = commandoCap.height * hero.HeightScale;
                float mscale = body.modelLocator.modelTransform.localScale.y;
                if (Mathf.Abs(cap.height - want) > 0.05f || Mathf.Abs(mscale - ModelBuilder.GlobalScale) > 0.01f)
                { scaleOk = false; scaleWhy += $"{hero.Id}: capsule {cap.height:F2} want {want:F2}, model {mscale:F3} want {ModelBuilder.GlobalScale:F3}; "; }

                // a target in front
                var aim = body.inputBank.GetAimRay();
                var target = Summon(body, aim.origin + aim.direction * 10f);
                if (DevShots) DevCommando(body);
                yield return new WaitForSeconds(1f);
                Shot(hero.Id + "_0_spawn");

                // gun
                fireBody = body; fireUntil = Time.time + 2f;
                body.skillLocator.primary.ExecuteIfReady();
                yield return new WaitForSeconds(0.8f);
                Shot(hero.Id + "_1_gun");
                if (DevShots) DevCam(body, hero.Id + "_1_guncam");
                yield return new WaitForSeconds(1.7f);
                fireBody = null;
                bool gun = seen.Contains("gun-fires|" + token);

                // three abilities
                int abilities = 0;
                foreach (var gs in new[] { body.skillLocator.secondary, body.skillLocator.utility, body.skillLocator.special })
                {
                    gs.Reset();
                    var row = Survivors.RowFor(gs);
                    gs.ExecuteIfReady();
                    yield return new WaitForSeconds(0.4f);
                    if (row != null) Shot(hero.Id + "_2_" + row.Slot);
                    yield return new WaitForSeconds(Mathf.Clamp(row != null ? row.Duration + 0.8f : 2f, 1.5f, 5f));
                    if (row != null && seen.Contains("skill:" + row.Id + "|" + token)) abilities++;
                    body = master.GetBody();
                }

                // ult: locked, then charged by real damage, then fired with the ult key path
                var ult = UltController.Of(body);
                bool lockedAtStart = ult && ult.Skill && ult.Skill.stock == 0;
                if (!target || !target.healthComponent || !target.healthComponent.alive) target = Summon(body, body.corePosition + body.inputBank.aimDirection * 8f);
                yield return new WaitForSeconds(0.5f);
                float before = ult ? ult.Charge : 0;
                if (target && target.mainHurtBox) Fx.Hit(body, target.mainHurtBox, 3f, 1f, target.corePosition);
                yield return new WaitForSeconds(0.3f);
                bool charged = ult && ult.Charge > before;
                if (ult) ult.Add(ult.Required);
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                ultBody = body;
                yield return new WaitForSeconds(0.5f);
                ultBody = null;
                var ultRow = Sheet.SkillOf(hero.Id, "ult");
                yield return new WaitForSeconds(Mathf.Min(1.2f, ultRow.Duration * 0.5f));
                Shot(hero.Id + "_3_ult");
                if (DevShots) DevCam(master.GetBody(), hero.Id + "_4_ultcam");
                yield return new WaitForSeconds(ultRow.Duration + 2.5f);
                bool ultFired = seen.Contains("skill:" + ultRow.Id + "|" + token);
                if (!lockedAtStart) Write("ult-charges", false, hero.Id + ": ult was not locked at spawn");
                if (!charged) Write("ult-charges", false, hero.Id + ": damage did not charge the ult");
                if (!ultFired) Write("ult-fires", false, hero.Id + ": ult did not fire");

                bool plays = gun && abilities == 3 && ultFired;
                Write(hero.Id + "-plays", plays, $"gun={gun}, abilities={abilities}/3, ult={ultFired}");
                if (!gun) Write("guns-fire", false, hero.Id + ": gun did not fire");
                if (abilities < 3) Write("abilities-fire", false, $"{hero.Id}: {abilities}/3 abilities ran");
                if (plays) heroes++;

                // level-up growth matches the sheet (no items in a fresh run)
                body = master.GetBody();
                var st = Sheet.Stats(hero.Id);
                float lvl = body.level;
                float wantHp = st.BaseMaxHealth + st.LevelMaxHealth * (lvl - 1f), wantDmg = st.BaseDamage + st.LevelDamage * (lvl - 1f);
                if (Mathf.Abs(body.maxHealth - wantHp) > 1.5f || Mathf.Abs(body.damage - wantDmg) > 0.3f)
                { levelOk = false; levelWhy += $"{hero.Id} lvl {lvl}: hp {body.maxHealth:F0} want {wantHp:F0}, dmg {body.damage:F1} want {wantDmg:F1}; "; }

                if (target) { var tm = target.master; if (tm) tm.TrueKill(); }
                // grow a level before the next hero so later heroes are checked at higher levels
                TeamManager.instance.GiveTeamExperience(TeamIndex.Player, (ulong)Mathf.Max(100f, TeamManager.instance.GetTeamNextLevelExperience(TeamIndex.Player) - TeamManager.instance.GetTeamExperience(TeamIndex.Player) + 1f));
                yield return new WaitForSeconds(1f);
            }
            var lb = master.GetBody();
            Write("guns-fire", true);
            Write("abilities-fire", true);
            Write("ult-charges", true);
            Write("ult-fires", true);
            Write("level-stats", levelOk && lb && lb.level > 1, levelWhy == "" ? "level did not rise" : levelWhy);
            Write("hero-scale", scaleOk, scaleWhy);
            Log.Info($"Melty test finished: {heroes}/4 heroes played every skill; Deadlock sounds played: {DeadlockAudio.Played}");
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
