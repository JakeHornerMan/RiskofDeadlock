using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using RoR2;
using UnityEngine;

namespace DeadlockOfRain
{
    /// <summary>Deadlock voice lines (design/voice.json), played through DeadlockAudio for the local player's hero:
    /// select at every stage start, unselect when the teleporter finishes, a line when a skill starts (an ult equipment
    /// speaks with its own hero's voice, whoever fires it), killstreak lines on big multikills, and an idle line after
    /// 2-5 min of quiet. One line at a time; `chance` and `gap` keep it from chattering.</summary>
    public sealed class Voice : MonoBehaviour
    {
        const float MultikillWindow = 1.5f, IdleMin = 120f, IdleMax = 300f;
        static Voice instance;
        static readonly System.Random rng = new System.Random();
        public static int Spoken;

        int playing;                 // the DeadlockAudio voice id of the line in progress
        float lastLine = -999f;      // Time.time of the hero's last line
        float nextIdle;
        Stage selectedOn;            // the stage the select line was played on
        CharacterBody watched;       // the local hero body whose skills are listened to
        int burst; float lastKill = -999f; int burstLevel;
        Coroutine chain;                                                   // a chained row in progress (Royal Pestments' rat names)
        readonly Dictionary<string, Queue<int>> decks = new Dictionary<string, Queue<int>>();  // shuffled takes left, per row

        public static void Init()
        {
            if (instance) return;
            var go = new GameObject("DeadlockOfRainVoice");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Voice>();
            CharacterBody.onBodyStartGlobal += instance.OnBodyStart;
            TeleporterInteraction.onTeleporterFinishGlobal += instance.OnTeleporterFinish;
            GlobalEventManager.onCharacterDeathGlobal += instance.OnDeath;
        }

        static string HeroOfBody(CharacterBody b) => b ? Survivors.HeroOf(b) : null;
        static bool IsLocalPlayer(CharacterBody b) => b && b.isPlayerControlled && b.hasEffectiveAuthority;

        // ---------------------------------------------------------------- triggers
        void OnBodyStart(CharacterBody body)
        {
            if (!IsLocalPlayer(body)) return;
            if (watched) watched.onSkillActivatedAuthority -= OnSkill;
            watched = body;
            body.onSkillActivatedAuthority += OnSkill;
            var hero = HeroOfBody(body);
            if (hero == null) return;
            nextIdle = Time.time + Random.Range(IdleMin, IdleMax);
            // once per stage: the run's start and every new world after a teleport (not respawns)
            if (Stage.instance && Stage.instance != selectedOn)
            {
                selectedOn = Stage.instance;
                StartCoroutine(After(2f, () => Say(hero, "select", null, 0, true)));
            }
        }

        void OnTeleporterFinish(TeleporterInteraction tp)
        {
            foreach (var pc in PlayerCharacterMasterController.instances)
            {
                var b = pc && pc.master ? pc.master.GetBody() : null;
                if (IsLocalPlayer(b) && HeroOfBody(b) is string hero) Say(hero, "unselect", null, 0, true);
            }
        }

        void OnSkill(GenericSkill gs)
        {
            var row = Survivors.RowFor(gs);
            if (row != null) SkillLine(row);
        }

        /// <summary>A skill row started (a hero skill, or an ult equipment on any body): its own hero speaks.</summary>
        public static void SkillLine(SkillRow row)
        {
            if (!instance || row == null) return;
            instance.Say(row.Hero, "skill", row.Id, 0, row.Slot == "equipment");
        }

        void OnDeath(DamageReport r)
        {
            var b = r?.attackerBody;
            if (!IsLocalPlayer(b) || HeroOfBody(b) is not string hero) return;
            if (Time.time - lastKill > MultikillWindow) { burst = 0; burstLevel = 0; }
            lastKill = Time.time; burst++;
            // the biggest threshold reached plays once per burst
            var best = VoiceRow.All.Where(v => v.Hero == hero && v.Trigger == "multikill" && burst >= v.Kills && v.Kills > burstLevel)
                                   .OrderByDescending(v => v.Kills).FirstOrDefault();
            if (best == null) return;
            burstLevel = (int)best.Kills;
            StartCoroutine(After(0.4f, () => Play(best, false)));
        }

        void Update()
        {
            if (!watched || HeroOfBody(watched) is not string hero || !watched.healthComponent || !watched.healthComponent.alive) return;
            if (Time.time < nextIdle) return;
            nextIdle = Time.time + Random.Range(IdleMin, IdleMax);
            if (Time.time - lastLine < IdleMin) return;
            Say(hero, "idle", null, 0, false);
        }

        // ---------------------------------------------------------------- playing
        void Say(string hero, string trigger, string skill, int kills, bool important)
        {
            var pool = VoiceRow.All.Where(v => v.Hero == hero && v.Trigger == trigger && (skill == null || v.Skill == skill)).ToList();
            if (pool.Count == 0) return;
            Play(pool[rng.Next(pool.Count)], important);
        }

        void Play(VoiceRow v, bool important)
        {
            string ev = "vo:" + v.Group;
            if (!DeadlockAudio.Has(ev)) return;
            if (rng.NextDouble() > v.Chance) return;
            if (Time.time - lastLine < v.Gap) return;
            if (DeadlockAudio.IsPlaying(playing) || chain != null)
            {
                if (!important) return;
                DeadlockAudio.Stop(playing);
            }
            if (chain != null) { StopCoroutine(chain); chain = null; }
            if (v.Chain > 0f) { chain = StartCoroutine(Chain(v, ev)); return; }
            playing = Speak(v, ev, NextTake(v, ev));
            if (playing == 0) return;
            lastLine = Time.time;
            nextIdle = Time.time + Random.Range(IdleMin, IdleMax);
            Spoken++;
            MeltyTest.Report("voice-lines", true, v.Id);
        }

        // heroes.voiceGain on top (Viscous' lines +20 %, creator 2026-10-20)
        static int Speak(VoiceRow v, string ev, int take) => DeadlockAudio.Play(ev, null, false, 1.15f * (Sheet.Hero(v.Hero)?.VoiceGain ?? 1f), take);

        /// <summary>The row's next take: a shuffled deck of every take not in skipTakes, dealt in turn and reshuffled when empty,
        /// so no line repeats until all have played. -1 = any (the row skips nothing, or the event has no numbered takes).</summary>
        int NextTake(VoiceRow v, string ev)
        {
            if (v.Chain <= 0f && v.SkipTakes == "none") return -1;
            if (!decks.TryGetValue(v.Id, out var deck) || deck.Count == 0)
            {
                var skip = new HashSet<int>();
                if (v.SkipTakes != "none") foreach (var t in v.SkipTakes.Split(',')) if (int.TryParse(t.Trim(), out var n)) skip.Add(n);
                var takes = DeadlockAudio.Takes(ev).Where(t => t >= 0 && !skip.Contains(t)).ToList();
                if (takes.Count == 0) return -1;
                for (int i = takes.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (takes[i], takes[j]) = (takes[j], takes[i]); }
                decks[v.Id] = deck = new Queue<int>(takes);
            }
            return deck.Dequeue();
        }

        /// <summary>A chained row: one take after another, each as soon as the last ends, until chain seconds have passed.</summary>
        IEnumerator Chain(VoiceRow v, string ev)
        {
            float end = Time.time + v.Chain;
            lastLine = Time.time;
            nextIdle = Time.time + Random.Range(IdleMin, IdleMax);
            while (Time.time < end)
            {
                int take = NextTake(v, ev);
                playing = Speak(v, ev, take);
                if (playing == 0) break;
                if (MeltyTest.Active) Log.Info($"Melty test: voice chain {v.Id}: take {take} at {Time.time - (end - v.Chain):F1} s");
                Spoken++;
                MeltyTest.Report("voice-lines", true, v.Id);
                while (DeadlockAudio.IsPlaying(playing)) yield return null;
                lastLine = Time.time;
                yield return new WaitForSeconds(0.15f);
            }
            chain = null;
        }

        IEnumerator After(float s, System.Action a)
        {
            yield return new WaitForSeconds(s);
            a();
        }
    }
}
