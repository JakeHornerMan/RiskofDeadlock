#if OWN_FILES
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using RoR2;
using RoR2.Audio;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace DeadlockOfRain.Enemies
{
    /// <summary>The Medic's heal pulse (enemySkills kind medic_pulse; creator's spec): on the server, every interval, every other
    /// monster on its team within radius that is hurt (not itself, not another Medic) heals healPct of its max health
    /// (healPctBoss on champions). The ring, beams and crosses reach every player through MedicFx's effects.</summary>
    [Kind("medic_pulse")]
    public sealed class MedicPulse : MonoBehaviour
    {
        public string RowId;
        EnemySkillRow row;
        CharacterBody body;
        float next;
        /// <summary>Server: someone in range was hurt at the last pulse (the AI shoots only when nobody needs healing).</summary>
        public bool Healing;
        public int Pulses, Healed;

        void Start() { body = GetComponent<CharacterBody>(); row = EnemySkillRow.All.FirstOrDefault(s => s.Id == RowId); next = Time.fixedTime + 1f; }

        void FixedUpdate()
        {
            if (!NetworkServer.active || row == null || !body || !body.healthComponent || !body.healthComponent.alive) return;
            if (Time.fixedTime < next) return;
            next = Time.fixedTime + row.Interval;
            var team = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.Monster;
            float r2 = row.Radius * row.Radius;
            var healed = new List<CharacterBody>();
            foreach (var tc in TeamComponent.GetTeamMembers(team))
            {
                var b = tc ? tc.body : null;
                if (!b || b == body || !b.healthComponent || !b.healthComponent.alive || Enemies.IsMedic(b)) continue;
                if ((b.corePosition - body.corePosition).sqrMagnitude > r2) continue;
                if (b.healthComponent.health >= b.healthComponent.fullHealth) continue;
                float pct = b.isChampion ? row.HealPctBoss : row.HealPct;
                b.healthComponent.Heal(b.healthComponent.fullHealth * pct, default);
                healed.Add(b);
            }
            Healing = healed.Count > 0;
            if (!Healing) return;
            Pulses++; Healed += healed.Count;
            MedicFx.Pulse(body, healed, row);
        }
    }

    /// <summary>The heal pulse's visuals, sent to every player once (EffectManager): a green ring rolling out to the pulse radius with
    /// a soft chime at the Medic, and on each healed monster a green beam from the Medic and crosses rising off it.</summary>
    public static class MedicFx
    {
        public static EffectDef RingDef, BeamDef;
        /// <summary>How many rings and beams this machine has shown (test mode counts them).</summary>
        public static int RingsShown, BeamsShown;

        public static void Build(Transform holder)
        {
            RingDef = Def(holder, "DeadlockMedicRing", typeof(MedicRingFx));
            BeamDef = Def(holder, "DeadlockMedicBeam", typeof(MedicBeamFx));
            Enemies.EffectDefs.Add(RingDef); Enemies.EffectDefs.Add(BeamDef);
        }

        static EffectDef Def(Transform holder, string name, System.Type fx)
        {
            var go = new GameObject(name);
            go.transform.SetParent(holder, false);
            go.AddComponent<EffectComponent>();
            var va = go.AddComponent<VFXAttributes>();
            va.DoNotPool = true;
            va.vfxPriority = VFXAttributes.VFXPriority.Always;
            va.vfxIntensity = VFXAttributes.VFXIntensity.Low;
            go.AddComponent(fx);
            return new EffectDef(go);
        }

        public static void Pulse(CharacterBody medic, List<CharacterBody> healed, EnemySkillRow row)
        {
            var c = Sheet.Color(row.Color);
            EffectManager.SpawnEffect(RingDef.index, new EffectData { origin = medic.footPosition, scale = row.Radius, color = c }, true);
            foreach (var b in healed)
                EffectManager.SpawnEffect(BeamDef.index, new EffectData { origin = b.corePosition, start = medic.corePosition + Vector3.up * 0.6f, scale = b.radius, color = c }, true);
        }

        static EffectData Data(MonoBehaviour fx) => fx.GetComponent<EffectComponent>()?.effectData;

        sealed class MedicRingFx : MonoBehaviour
        {
            void Start()
            {
                var d = Data(this);
                RingsShown++;
                if (d != null)
                {
                    // a darker, half see-through green: a bright additive ring blows out to white (doc 02 gotcha 36)
                    var c = (Color)d.color; c = new Color(c.r * 0.6f, c.g * 0.75f, c.b * 0.6f, 0.7f);
                    Vfx.Ring(d.origin + Vector3.up * 0.15f, c, 0.5f, d.scale, 0.7f, 0.35f);
                    Vfx.Flash(d.origin + Vector3.up, (Color)d.color, 6f, 0.4f, 2f);
                    var row = EnemySkillRow.All.FirstOrDefault(s => s.Kind == "medic_pulse");
                    if (row != null && row.Ror2Sound != "none") PointSoundManager.EmitSoundLocal(row.Ror2Sound, d.origin);
                    if (row != null && row.CastSound != "none") DeadlockAudio.Play(row.CastSound, d.origin);
                }
                Destroy(gameObject);
            }
        }

        sealed class MedicBeamFx : MonoBehaviour
        {
            void Start()
            {
                var d = Data(this);
                BeamsShown++;
                if (d != null)
                {
                    Vfx.Bolt(d.start, d.origin, (Color)d.color, 0.12f, 0.45f, 6, 0.15f, true);
                    Crosses(d.origin, (Color)d.color, Mathf.Max(0.5f, d.scale));
                }
                Destroy(gameObject);
            }
        }

        /// <summary>Three small green crosses rising off a healed monster and fading.</summary>
        static void Crosses(Vector3 at, Color c, float radius)
        {
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("HealCross");
                go.transform.position = at + new Vector3(Random.Range(-radius, radius), Random.Range(0f, radius), Random.Range(-radius, radius)) * 0.6f;
                float s = 0.25f;
                foreach (var dir in new[] { Vector3.up, Vector3.right })
                {
                    var l = new GameObject("bar").AddComponent<LineRenderer>();
                    l.transform.SetParent(go.transform, false);
                    l.useWorldSpace = false;
                    l.sharedMaterial = Vfx.Mat("mat_tracer", c);
                    l.startColor = l.endColor = c;
                    l.widthMultiplier = 0.09f;
                    l.positionCount = 2; l.SetPosition(0, -dir * s); l.SetPosition(1, dir * s);
                }
                var rise = go.AddComponent<RiseFade>();
                rise.Delay = i * 0.12f;
                Object.Destroy(go, 1.3f + rise.Delay);
            }
        }

        sealed class RiseFade : MonoBehaviour
        {
            public float Delay;
            float age;
            void Update()
            {
                age += Time.deltaTime;
                if (age < Delay) { foreach (var l in GetComponentsInChildren<LineRenderer>()) l.enabled = false; return; }
                float t = (age - Delay) / 1.2f;
                transform.position += Vector3.up * Time.deltaTime * 1.2f;
                var cam = Camera.main;
                if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
                foreach (var l in GetComponentsInChildren<LineRenderer>())
                {
                    l.enabled = true;
                    var c = l.startColor; c.a = Mathf.Clamp01(1f - t); l.startColor = l.endColor = c;
                }
            }
        }
    }

    /// <summary>The Medic's place in a fight (creator's spec): behind the nearest group of other monsters, 15-25 m on the far side of
    /// them from its target. A marker it moves there is the AI's custom target; the Follow/Stay drivers that walk to it are on only
    /// while such a group exists, so with nobody to heal it fights like a Ranged Trooper.</summary>
    public sealed class MedicFollow : MonoBehaviour
    {
        public const string FollowDriver = "FollowAllies", StayDriver = "StayBehindAllies";
        // Behind: past the group's middle, away from the target, but close enough that the pulse (15 m) reaches the group's far side
        // (2026-11-11: 20 m left the far members out of reach)
        const float Search = 40f, Group = 10f, Behind = 7f;
        BaseAI ai;
        CharacterMaster master;
        AISkillDriver[] follow;
        GameObject marker;
        float next;
        public bool HasAllies;

        void Start()
        {
            ai = GetComponent<BaseAI>();
            master = GetComponent<CharacterMaster>();
            follow = GetComponents<AISkillDriver>().Where(d => d.customName == FollowDriver || d.customName == StayDriver).ToArray();
            marker = new GameObject("MedicFollowPoint");
            Set(false);
        }

        void OnDestroy() { if (marker) Destroy(marker); }

        void Set(bool on)
        {
            HasAllies = on;
            foreach (var d in follow) if (d) d.enabled = on;
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active || !ai || Time.fixedTime < next) return;
            next = Time.fixedTime + 0.5f;
            var body = master ? master.GetBody() : null;
            if (!body) return;
            var team = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.Monster;
            CharacterBody nearest = null; float best = Search * Search;
            foreach (var tc in TeamComponent.GetTeamMembers(team))
            {
                var b = tc ? tc.body : null;
                if (!b || b == body || Enemies.IsMedic(b) || !b.healthComponent || !b.healthComponent.alive) continue;
                float d = (b.corePosition - body.corePosition).sqrMagnitude;
                if (d < best) { best = d; nearest = b; }
            }
            if (!nearest) { Set(false); return; }
            // the group around the nearest ally, and the side away from the Medic's target (or from where the Medic stands)
            var sum = Vector3.zero; int n = 0;
            foreach (var tc in TeamComponent.GetTeamMembers(team))
            {
                var b = tc ? tc.body : null;
                if (!b || b == body || Enemies.IsMedic(b) || !b.healthComponent || !b.healthComponent.alive) continue;
                if ((b.footPosition - nearest.footPosition).sqrMagnitude < Group * Group) { sum += b.footPosition; n++; }
            }
            var center = sum / Mathf.Max(1, n);
            var enemy = ai.currentEnemy != null && ai.currentEnemy.gameObject ? ai.currentEnemy.gameObject.transform.position : body.footPosition - body.transform.forward * 10f;
            var away = center - enemy; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -body.transform.forward;
            marker.transform.position = center + away.normalized * Behind;
            ai.SetCustomTargetGameObject(marker);
            Set(true);
        }
    }
}
#endif
