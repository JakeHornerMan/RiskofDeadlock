#if OWN_FILES
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Enemies;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain
{
    /// <summary>The enemies' test mode (test-mode file "enemies": "melee,ranged,medic"; ENEMIES= run_test.sh; docs/11): the creator's
    /// Trooper test plan. The player (can't be hurt) is the target and is moved by the test; each Trooper runs its own AI; what the
    /// Troopers do is noted by their states (EnemyNote) and checked against the plan.</summary>
    public sealed partial class MeltyTest
    {
        static List<string> TestEnemies;
        static CharacterBody enemyTarget;
        struct Note { public float T; public string What; public string Who; public float Dist; }
        static readonly List<Note> notes = new List<Note>();

        /// <summary>An enemy did something (swing, hit, shot); kept with its distance to the test's target.</summary>
        public static void EnemyNote(string what, CharacterBody who)
        {
            if (!Active || !who) return;
            float d = enemyTarget ? Vector3.Distance(Flat(who.footPosition), Flat(enemyTarget.footPosition)) : -1f;
            notes.Add(new Note { T = Time.time, What = what, Who = EnemyTag.Of(who), Dist = d });
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static void ReadEnemies(string text)
        {
            var m = System.Text.RegularExpressions.Regex.Match(text, "\"enemies\"\\s*:\\s*\"([^\"]*)\"");
            if (m.Success) TestEnemies = m.Groups[1].Value.Split(',').Select(s => s.Trim()).Where(s => s != "").ToList();
        }

        static Vector3 Ground(Vector3 near)
        {
            var g = SceneInfo.instance ? SceneInfo.instance.groundNodes : null;
            if (g)
            {
                var n = g.FindClosestNode(near, HullClassification.Human);
                if (g.GetNodePosition(n, out var p)) return p;
            }
            return near;
        }

        static void Put(CharacterBody b, Vector3 at)
        {
            TeleportHelper.TeleportBody(b, at + Vector3.up * 0.5f, false);
            if (b.characterMotor) b.characterMotor.velocity = Vector3.zero;
        }

        static CharacterBody SpawnEnemy(string id, Vector3 at, bool ai)
        {
            if (!Enemies.Enemies.Masters.TryGetValue(id, out var prefab)) return null;
            var m = new MasterSummon { masterPrefab = prefab, position = Ground(at), rotation = Quaternion.identity, teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform();
            if (!m) return null;
            if (!ai) foreach (var a in m.GetComponents<BaseAI>()) a.enabled = false;
            var b = m.GetBody();
            if (b) targets.Add(b);
            return b;
        }

        static CharacterBody SpawnVanilla(string master, Vector3 at)
        {
            var prefab = MasterCatalog.FindMasterPrefab(master);
            var m = prefab ? new MasterSummon { masterPrefab = prefab, position = Ground(at), rotation = Quaternion.identity, teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform() : null;
            if (!m) return null;
            foreach (var a in m.GetComponents<BaseAI>()) a.enabled = false;
            var b = m.GetBody();
            if (b) targets.Add(b);
            return b;
        }

        static void Clear()
        {
            foreach (var t in targets.ToList()) if (t && t.master) t.master.TrueKill();
            targets.Clear();
        }

        static float Dist(CharacterBody a, CharacterBody b) => a && b ? Vector3.Distance(Flat(a.footPosition), Flat(b.footPosition)) : -1f;

        /// <summary>The player walks (its own controller off, the test pushes the move input each physics step).</summary>
        static Vector3 walk;
        static PlayerCharacterMasterController walkerPcmc;
        void FixedUpdate()
        {
            if (walkerPcmc && enemyTarget && enemyTarget.inputBank) enemyTarget.inputBank.moveVector = walk;
        }

        IEnumerator EnemyRun(LocalUser user)
        {
            var body = user.cachedBody;
            enemyTarget = body;
            body.healthComponent.godMode = true;
            QuietStage();
            arenaForward = body.inputBank.aimDirection; arenaForward.y = 0; arenaForward.Normalize();
            arena = FindArena(body.footPosition, ref arenaForward);
            Put(body, arena);
            walkerPcmc = user.cachedMaster.GetComponent<PlayerCharacterMasterController>();
            if (walkerPcmc) walkerPcmc.enabled = false;
            var side = Vector3.Cross(Vector3.up, arenaForward);
            int passed = 0, total = 0;
            void Check(string name, bool ok, string why) { total++; if (ok) passed++; Write(name, ok, why); }

            // content: every enemy built with both looks; spawn cards; the stage's pool reached; the family; the Medic's gate
            var built = (TestEnemies ?? new List<string>()).Select(e => "trooper_" + e).ToList();
            var missing = built.Where(e => !Enemies.Enemies.Bodies.ContainsKey(e)).ToList();
            Check("enemies-built", missing.Count == 0, missing.Count == 0 ? $"{Enemies.Enemies.Bodies.Count} built" : "missing " + string.Join(", ", missing));
            var looks = Enemies.Enemies.Bodies.Values.Select(b => b.GetComponent<ModelLocator>().modelTransform.GetComponent<EnemyLook>()).ToList();
            Check("enemy-looks", looks.All(l => l && l.Looks.Length == 2), string.Join(", ", looks.Select(l => l ? l.Looks.Length.ToString() : "none")) + " looks");
            var sel = ClassicStageInfo.instance ? ClassicStageInfo.instance.monsterSelection : null;
            int inPool = 0;
            if (sel != null) for (int i = 0; i < sel.Count; i++) if (Enemies.Enemies.Cards.Values.Contains(sel.GetChoice(i).value?.spawnCard)) inPool++;
            Check("enemy-spawn-pool", TrooperDirector.StagesTouched > 0 && inPool == Enemies.Enemies.Cards.Count,
                $"R2API pools touched {TrooperDirector.StagesTouched}; {inPool}/{Enemies.Enemies.Cards.Count} Trooper cards in this stage's monster list; costs " +
                string.Join(", ", Enemies.Enemies.Cards.Select(kv => $"{kv.Key} {kv.Value.directorCreditCost}")));
            Check("enemy-family", TrooperDirector.Family("troopers") != null, "Trooper family event made");
            Check("medic-spawn-gate", !MedicSpawnGate.AlliesNear(arena + Vector3.up * 500f), "a spot with no monsters near turns the Medic down");

            if (TestEnemies.Contains("melee")) yield return MeleeTest(body, side, Check);
            if (TestEnemies.Contains("ranged")) yield return RangedTest(body, Check);
            if (TestEnemies.Contains("medic")) yield return MedicTest(body, side, Check);

            walk = Vector3.zero;
            if (walkerPcmc) walkerPcmc.enabled = true;
            Clear();
            Log.Info($"Melty test finished: enemies {passed}/{total} checks passed");
        }

        IEnumerator MeleeTest(CharacterBody p, Vector3 side, System.Action<string, bool, string> Check)
        {
            Put(p, arena);
            notes.Clear();
            var t = SpawnEnemy("trooper_melee", arena + arenaForward * 20f, true);
            if (!t) { Check("trooper-melee", false, "could not spawn"); yield break; }
            yield return new WaitForSeconds(2f);
            Shot("trooper_melee_spawn");
            // the target walks a steady path across the arena, turning every 3 s
            float start = Time.time, closest = 99f;
            for (float s = 0f; s < 12f; s += Time.fixedDeltaTime)
            {
                walk = (((int)(s / 3f)) % 2 == 0 ? side : -side) * 0.6f;
                closest = Mathf.Min(closest, Dist(p, t));
                yield return new WaitForFixedUpdate();
            }
            walk = Vector3.zero;
            var swings = notes.Where(n => n.What == "swing" && n.T >= start).ToList();
            Check("trooper-melee-chase", closest <= 3.5f, $"closest {closest:F1} m to the walking target");
            Check("trooper-melee-swing", swings.Count > 0 && swings.All(n => n.Dist <= 4f), $"{swings.Count} swings, at {string.Join(", ", swings.Select(n => n.Dist.ToString("F1")))} m");
            // the target stops: the Trooper stays beside it and keeps hitting
            float hold = Time.time; var at = Flat(t.footPosition); float wander = 0f;
            for (float s = 0f; s < 8f; s += 0.1f)
            {
                if (s > 2f) wander = Mathf.Max(wander, Dist(p, t));
                yield return new WaitForSeconds(0.1f);
            }
            Shot("trooper_melee_hold");
            int hits = notes.Count(n => n.What == "hit" && n.T >= hold);
            Check("trooper-melee-hold", hits >= 3 && wander <= 4f, $"{hits} hits on the standing target in 8 s; farthest it stood {wander:F1} m");
            Clear();
            yield return new WaitForSeconds(1f);
        }

        IEnumerator RangedTest(CharacterBody p, System.Action<string, bool, string> Check)
        {
            Put(p, arena);
            notes.Clear();
            var t = SpawnEnemy("trooper_ranged", arena + arenaForward * 50f, true);
            if (!t) { Check("trooper-ranged", false, "could not spawn"); yield break; }
            float start = Time.time; float firstShot = -1f, nearest = 99f;
            while (Time.time - start < 25f)
            {
                var shot = notes.FirstOrDefault(n => n.What == "shot");
                if (shot.What != null && firstShot < 0f) firstShot = shot.Dist;
                if (firstShot >= 0f && Time.time - start > 8f) break;
                yield return new WaitForSeconds(0.2f);
            }
            // after the first shot: it holds its ground (no closer than the band)
            for (float s = 0f; s < 5f; s += 0.2f) { nearest = Mathf.Min(nearest, Dist(p, t)); yield return new WaitForSeconds(0.2f); }
            Shot("trooper_ranged_band");
            var shots = notes.Where(n => n.What == "shot").ToList();
            Check("trooper-ranged-approach", firstShot >= 14f && firstShot <= 31f && nearest >= 13f, $"first shot at {firstShot:F1} m, nearest after {nearest:F1} m");
            Check("trooper-ranged-no-oor", shots.Count > 0 && shots.All(n => n.Dist >= 6.5f && n.Dist <= 31f), $"{shots.Count} shots, at {string.Join(", ", shots.Select(n => n.Dist.ToString("F0")))} m");
            // the target goes past it and out the other side, beyond 30 m: it moves to catch up
            var from = Flat(t.footPosition);
            Put(p, Ground(t.footPosition + arenaForward * 40f));
            float moved = 0f;
            for (float s = 0f; s < 8f; s += 0.2f) { moved = Mathf.Max(moved, Vector3.Distance(from, Flat(t.footPosition))); yield return new WaitForSeconds(0.2f); }
            Check("trooper-ranged-follow", moved > 3f, $"moved {moved:F1} m after the target left the band ({Dist(p, t):F1} m now)");
            // back into the band: it stops and shoots, no extra steps
            var dir = Flat(p.footPosition - t.footPosition).normalized;
            Put(p, Ground(t.footPosition + dir * 22f));
            yield return new WaitForSeconds(1.5f);
            float back = Time.time; var stand = Flat(t.footPosition); float drift = 0f;
            for (float s = 0f; s < 5f; s += 0.2f) { drift = Mathf.Max(drift, Vector3.Distance(stand, Flat(t.footPosition))); yield return new WaitForSeconds(0.2f); }
            int again = notes.Count(n => n.What == "shot" && n.T >= back - 1.5f);
            Check("trooper-ranged-resume", again > 0 && drift <= 2f, $"{again} shots once back in range, moved {drift:F1} m meanwhile (at {Dist(p, t):F1} m)");
            Clear();
            yield return new WaitForSeconds(1f);
        }

        IEnumerator MedicTest(CharacterBody p, Vector3 side, System.Action<string, bool, string> Check)
        {
            Put(p, arena);
            notes.Clear();
            var g = arena + arenaForward * 12f;
            var group = new List<CharacterBody>
            {
                SpawnVanilla("BeetleMaster", g + side * 3f), SpawnVanilla("LemurianMaster", g - side * 3f),
                SpawnVanilla("BeetleMaster", g + arenaForward * 3f), SpawnEnemy("trooper_melee", g - arenaForward * 2f, false),
            }.Where(b => b).ToList();
            var medic = SpawnEnemy("trooper_medic", g + arenaForward * 6f, true);
            if (!medic || group.Count == 0) { Check("trooper-medic", false, "could not spawn the group"); yield break; }
            var pulse = medic.GetComponent<MedicPulse>();
            yield return new WaitForSeconds(2f);
            // the group is hurt steadily; each must climb back
            var healedBy = new Dictionary<CharacterBody, float>();
            HealthComponent.onCharacterHealServer += OnHeal;
            void OnHeal(HealthComponent hc, float amount, ProcChainMask _) { if (hc && hc.body) healedBy[hc.body] = (healedBy.TryGetValue(hc.body, out var a) ? a : 0f) + amount; }
            int pulses0 = pulse ? pulse.Pulses : 0, rings0 = MedicFx.RingsShown;
            for (float s = 0f; s < 8f; s += 0.5f)
            {
                foreach (var b in group.Append(medic))
                    if (b && b.healthComponent.alive && b.healthComponent.combinedHealthFraction > 0.4f)
                        b.healthComponent.TakeDamage(new DamageInfo { damage = b.healthComponent.fullHealth * 0.05f, position = b.corePosition, procCoefficient = 0f, damageColorIndex = DamageColorIndex.Default });
                if (Mathf.Abs(s - 4f) < 0.01f) Shot("trooper_medic_heal");
                yield return new WaitForSeconds(0.5f);
            }
            HealthComponent.onCharacterHealServer -= OnHeal;
            var unhealed = group.Where(b => b && (!healedBy.TryGetValue(b, out var a) || a <= 0f)).Select(b => b.name).ToList();
            float medicHealed = 0f;
            // the Medic's own regen counts as healing too: only a heal of its pulse size would be the pulse
            if (healedBy.TryGetValue(medic, out var mh)) medicHealed = mh;
            Check("trooper-medic-heal", unhealed.Count == 0 && medicHealed < medic.healthComponent.fullHealth * 0.04f,
                $"healed {group.Count - unhealed.Count}/{group.Count} ({string.Join(", ", group.Select(b => $"{b.name.Replace("(Clone)", "")} +{(healedBy.TryGetValue(b, out var a) ? a : 0f):F0}"))}); the Medic itself +{medicHealed:F0}" + (unhealed.Count > 0 ? "; not healed: " + string.Join(", ", unhealed) : ""));
            int pulses = (pulse ? pulse.Pulses : 0) - pulses0, rings = MedicFx.RingsShown - rings0;
            Check("trooper-medic-fx", pulses >= 6 && rings >= pulses - 1 && MedicFx.BeamsShown > 0, $"{pulses} pulses in 8 s, {rings} rings and {MedicFx.BeamsShown} beams shown here");
            // nobody left to heal: it fights like a Ranged Trooper, 15-30 m away, shooting
            foreach (var b in group) if (b && b.master) { targets.Remove(b); b.master.TrueKill(); }
            notes.Clear();
            float d = 0f;
            for (float s = 0f; s < 14f; s += 0.2f) { d = Dist(p, medic); yield return new WaitForSeconds(0.2f); }
            Shot("trooper_medic_alone");
            int shots = notes.Count(n => n.What == "shot");
            Check("trooper-medic-alone", shots > 0 && d >= 9f && d <= 32f, $"{shots} shots with nobody to heal, standing {d:F1} m from the target");
            // its kill reward: a healing orb
            seen.Remove("medic-orb");
            medic.healthComponent.TakeDamage(new DamageInfo { damage = medic.healthComponent.fullCombinedHealth * 10f, attacker = p.gameObject, position = medic.corePosition, procCoefficient = 0f });
            yield return new WaitForSeconds(1f);
            Check("trooper-medic-orb", seen.Contains("medic-orb"), "a healing orb dropped where it died");
            Clear();
        }
    }
}
#endif
