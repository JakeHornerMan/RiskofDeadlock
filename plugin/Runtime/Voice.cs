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
            if (DeadlockAudio.IsPlaying(playing))
            {
                if (!important) return;
                DeadlockAudio.Stop(playing);
            }
            playing = DeadlockAudio.Play(ev, null, false, 1.15f);
            if (playing == 0) return;
            lastLine = Time.time;
            nextIdle = Time.time + Random.Range(IdleMin, IdleMax);
            Spoken++;
            MeltyTest.Report("voice-lines", true, v.Id);
        }

        IEnumerator After(float s, System.Action a)
        {
            yield return new WaitForSeconds(s);
            a();
        }
    }
}
