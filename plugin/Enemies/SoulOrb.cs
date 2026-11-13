#if OWN_FILES
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace DeadlockOfRain.Enemies
{
    /// <summary>The soul orb a Trooper leaves (enemyDrops.json; creator, 2026-11-11: "a soul will arise ... if you shoot you will gain 5
    /// currency ... disappear after 5 seconds and if you don't destroy them you don't get the money"). A clone of RoR2's floating Time
    /// Crystal (a shootable Neutral destructible), its look swapped for a glowing orb drawn with the creator's own soul-orb glow
    /// texture, 1 health. Spawned by the server where the Trooper died; its states run on every machine (NetworkStateMachine).
    /// Sounds are RoR2's own events (the creator's files have no orb sounds; "do not steal anything that isn't mine").</summary>
    public static class SoulOrb
    {
        public static GameObject Body;
        internal static EnemyDropRow Row;
        public static readonly List<CharacterBody> Live = new List<CharacterBody>();
        public static int Spawned, Broken, Missed;
        public static uint LastGold;
        static GameObject coinEmitter;

        public static void Build(Transform holder)
        {
            Row = EnemyDropRow.All.FirstOrDefault();
            if (Row == null) return;
            GameObject src = null;
            try { src = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/WeeklyRun/TimeCrystalBody.prefab").WaitForCompletion(); }
            catch (System.Exception e) { Log.Warn($"Enemies: soul orb base: {e.Message}"); }
            if (!src) { Log.Warn("Enemies: no Time Crystal to build the soul orb from: Troopers leave no orb"); return; }
            try { coinEmitter = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/CoinEmitter.prefab").WaitForCompletion(); } catch { }

            Body = Object.Instantiate(src, holder);
            Body.name = "DeadlockSoulOrbBody";
            Enemies.AssetIdOf(Body);
            // the weekly-run counting the crystals came from goes
            foreach (var c in Body.GetComponentsInChildren<MonoBehaviour>(true))
                if (c && (c.GetType().Name.Contains("TimeCrystal") || c.GetType().Name.Contains("Weekly"))) Object.DestroyImmediate(c);
            var cb = Body.GetComponent<CharacterBody>();
            cb.baseNameToken = "DLR_SOUL_ORB_NAME";
            Survivors.Tokens["DLR_SOUL_ORB_NAME"] = "Soul Orb";
            cb.baseMaxHealth = 1f; cb.levelMaxHealth = 0f; cb.baseArmor = 0f; cb.levelArmor = 0f; cb.baseRegen = 0f; cb.levelRegen = 0f;
            var death = Body.GetComponent<CharacterDeathBehavior>();
            if (death) death.deathState = new SerializableEntityStateType(typeof(SoulOrbBreak));
            var esm = EntityStateMachine.FindByCustomName(Body, "Body") ?? Body.GetComponent<EntityStateMachine>();
            if (esm) { esm.initialStateType = new SerializableEntityStateType(typeof(SoulOrbIdle)); esm.mainStateType = new SerializableEntityStateType(typeof(SoulOrbIdle)); }

            // the look: the crystal's renderers off (CharacterModel would turn listed ones back on), our orb in their place
            var model = Body.GetComponent<ModelLocator>() ? Body.GetComponent<ModelLocator>().modelTransform : Body.transform;
            // removed outright, not just switched off: its skin controller turned the crystal back on (2026-11-12, the test showed
            // the pink crystal in place of the orb)
            foreach (var sk in Body.GetComponentsInChildren<ModelSkinController>(true)) Object.DestroyImmediate(sk);
            var cm = model.GetComponent<CharacterModel>();
            if (cm) { cm.baseRendererInfos = new CharacterModel.RendererInfo[0]; cm.baseLightInfos = new CharacterModel.LightInfo[0]; }
            foreach (var ps in Body.GetComponentsInChildren<ParticleSystem>(true)) Object.DestroyImmediate(ps.gameObject);
            foreach (var r in Body.GetComponentsInChildren<Renderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>();
                Object.DestroyImmediate(r);
                if (mf) Object.DestroyImmediate(mf);
            }
            foreach (var l in Body.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);
            foreach (var rb in Body.GetComponentsInChildren<Rigidbody>(true)) { rb.isKinematic = true; rb.useGravity = false; }
            // the hurtbox: one sphere of the row's radius
            float scale = Mathf.Max(0.001f, model.lossyScale.x);
            foreach (var hb in model.GetComponentsInChildren<HurtBox>(true))
            {
                foreach (var col in hb.GetComponents<Collider>()) Object.DestroyImmediate(col);
                var sc = hb.gameObject.AddComponent<SphereCollider>();
                sc.radius = Row.Radius * 1.5f / Mathf.Max(0.001f, hb.transform.lossyScale.x);   // a little bigger than the orb: fair to hit
                sc.center = hb.transform.InverseTransformPoint(model.position);
            }
            var look = new GameObject("SoulOrbLook");
            look.transform.SetParent(model, false);
            look.transform.localPosition = Vector3.zero;
            look.transform.localScale = Vector3.one / scale;
            look.AddComponent<SoulOrbLook>();
            Log.Info($"Enemies: soul orb built from the Time Crystal (gold {Row.Gold}, {Row.Lifetime} s, shell {(DeadlockCache.Image(Row.ShellTexture) ? "found" : "MISSING")})");
        }

        /// <summary>Server: an orb above a dead enemy that drops one.</summary>
        public static void SpawnFor(CharacterBody victim)
        {
            if (!NetworkServer.active || !Body || Row == null || !victim) return;
            var id = EnemyTag.Of(victim);
            if (id == null || !Row.Enemies.Contains(id)) return;
            var go = Object.Instantiate(Body, ChestOf(victim), Quaternion.identity);
            var tc = go.GetComponent<TeamComponent>();
            if (tc) tc.teamIndex = TeamIndex.Neutral;
            NetworkServer.Spawn(go);
            Spawned++;
            MeltyTest.EnemyNote("orb", victim);
        }

        static Vector3 ChestOf(CharacterBody b)
        {
            var loc = b.modelLocator && b.modelLocator.modelTransform ? b.modelLocator.modelTransform.GetComponent<ChildLocator>() : null;
            var t = loc ? loc.FindChild("Chest") : null;
            return t ? t.position : b.corePosition;
        }

        /// <summary>Server: the money for whoever broke it: a player, or the player whose drone or minion did.</summary>
        internal static void Reward(CharacterBody orb)
        {
            var attacker = orb && orb.healthComponent ? orb.healthComponent.lastHitAttacker : null;
            var ab = attacker ? attacker.GetComponent<CharacterBody>() : null;
            var master = ab ? ab.master : null;
            if (master && !master.playerCharacterMasterController && master.minionOwnership && master.minionOwnership.ownerMaster)
                master = master.minionOwnership.ownerMaster;
            if (!master || !master.playerCharacterMasterController) return;
            uint gold = (uint)Mathf.Max(0, Mathf.RoundToInt(Row.Gold));
            master.GiveMoney(gold);
            LastGold = gold;
            if (coinEmitter)
                EffectManager.SpawnEffect(coinEmitter, new EffectData { origin = orb.corePosition, genericFloat = gold, scale = gold }, true);
        }
    }

    /// <summary>The orb's look, on every machine, built like Deadlock's soul orb (generic_soul_orb + generic_soul_orb_shell on a
    /// sphere): a self-lit core in the orb colour, the shell around it (the voronoi noise cut out by its alpha, both faces, scrolling)
    /// and a soft glow; it shrinks away when it goes. Shared mesh and materials: nothing is made per orb.</summary>
    public sealed class SoulOrbLook : MonoBehaviour
    {
        Transform core, shell;
        float gone = -1f;
        static Material coreMat, shellMat, glowMat;
        static Mesh sphereMesh;

        void Start()
        {
            var row = SoulOrb.Row;
            if (row == null) return;
            var c = Sheet.Color(row.OrbColor);
            float r = row.Radius;
            core = Part("Core", r * 1.1f, CoreMat(c));
            shell = Part("Shell", r * 2f, ShellMat(row, c));
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(transform, false);
            var ps = glowGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.duration = 1f; main.startLifetime = 0.6f; main.startSpeed = 0f;
            main.startSize = r * 6f; main.startColor = new Color(c.r, c.g, c.b, 1f); main.simulationSpace = ParticleSystemSimulationSpace.Local; main.maxParticles = 4;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var em = ps.emission; em.rateOverTime = 5f;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0.5f, 1f) });
            col.color = g;
            if (!glowMat) glowMat = Vfx.Mat("mat_glow", c);
            glowGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = glowMat;
            // the dark smoky trail it leaves as it rises out of the body (Deadlock's soul orb, the creator's picture)
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 1.1f; trail.minVertexDistance = 0.05f; trail.startWidth = r * 1.3f; trail.endWidth = 0f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.startColor = new Color(0.05f, 0.09f, 0.08f, 0.9f); trail.endColor = new Color(0.05f, 0.09f, 0.08f, 0f);
            trail.sharedMaterial = TrailMat();
            ps.Play();
        }

        Transform Part(string name, float size, Material m)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = SphereMesh();
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * size;
            return go.transform;
        }

        // generic_soul_orb: RoR2's own shader, lit by itself in the orb colour
        static Material CoreMat(Color c)
        {
            if (coreMat) return coreMat;
            coreMat = ModelBuilder.Template ? new Material(ModelBuilder.Template) : new Material(Vfx.Mat("mat_glow", c));
            coreMat.name = "matDeadlockSoulOrbCore";
            foreach (var k in coreMat.shaderKeywords) if (k.Contains("LIMB") || k.Contains("SPLATMAP") || k.Contains("FLOWMAP") || k.Contains("VERTEX")) coreMat.DisableKeyword(k);
            if (coreMat.HasProperty("_Color")) coreMat.SetColor("_Color", c * 0.12f);
            if (coreMat.HasProperty("_MainTex")) coreMat.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (coreMat.HasProperty("_NormalTex")) coreMat.SetTexture("_NormalTex", null);
            if (coreMat.HasProperty("_EmTex")) coreMat.SetTexture("_EmTex", Texture2D.whiteTexture);
            if (coreMat.HasProperty("_EmColor")) coreMat.SetColor("_EmColor", c * 0.25f);
            if (coreMat.HasProperty("_EmPower")) coreMat.SetFloat("_EmPower", 0.6f);
            if (coreMat.HasProperty("_LimbRemovalOn")) coreMat.SetFloat("_LimbRemovalOn", 0f);
            return coreMat;
        }

        // generic_soul_orb_shell: the voronoi noise, cut out by its alpha (alpha test 0.01 in Deadlock: here a little higher so the
        // cells read), both faces, in the orb colour and faintly lit
        static Material ShellMat(EnemyDropRow row, Color c)
        {
            if (shellMat) return shellMat;
            shellMat = ModelBuilder.Template ? new Material(ModelBuilder.Template) : new Material(Vfx.Mat("mat_glow", c));
            shellMat.name = "matDeadlockSoulOrbShell";
            foreach (var k in shellMat.shaderKeywords) if (k.Contains("LIMB") || k.Contains("SPLATMAP") || k.Contains("FLOWMAP") || k.Contains("VERTEX")) shellMat.DisableKeyword(k);
            var tex = DeadlockCache.Image(row.ShellTexture);
            if (tex) { tex.wrapMode = TextureWrapMode.Repeat; shellMat.mainTexture = tex; if (shellMat.HasProperty("_MainTex")) shellMat.SetTexture("_MainTex", tex); }
            shellMat.mainTextureScale = new Vector2(2f, 1f);
            if (shellMat.HasProperty("_Color")) shellMat.SetColor("_Color", c);
            if (shellMat.HasProperty("_NormalTex")) shellMat.SetTexture("_NormalTex", null);
            if (shellMat.HasProperty("_EmTex")) shellMat.SetTexture("_EmTex", Texture2D.whiteTexture);
            if (shellMat.HasProperty("_EmColor")) shellMat.SetColor("_EmColor", c);
            if (shellMat.HasProperty("_EmPower")) shellMat.SetFloat("_EmPower", 2.4f);
            if (shellMat.HasProperty("_EnableCutout")) shellMat.SetFloat("_EnableCutout", 1f);
            shellMat.EnableKeyword("CUTOUT");
            if (shellMat.HasProperty("_Cutoff")) shellMat.SetFloat("_Cutoff", 0.45f);
            if (shellMat.HasProperty("_Cull")) shellMat.SetFloat("_Cull", 0f);
            if (shellMat.HasProperty("_LimbRemovalOn")) shellMat.SetFloat("_LimbRemovalOn", 0f);
            return shellMat;
        }

        static Material trailMat;

        // RoR2's smoke, darkened: the trail is a dark wisp under a bright orb
        static Material TrailMat()
        {
            if (trailMat) return trailMat;
            trailMat = new Material(Vfx.Remapped("mat_smoke", new Color(0.06f, 0.1f, 0.09f, 0.9f)));
            return trailMat;
        }

        static Mesh SphereMesh()
        {
            if (sphereMesh) return sphereMesh;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            return sphereMesh;
        }

        public void Vanish() { if (gone < 0f) gone = 0f; }

        void Update()
        {
            // the shell scrolls as Deadlock's does (0.5, 1 a second); one shared material, so every orb turns together
            if (shellMat) shellMat.mainTextureOffset = new Vector2(Time.time * 0.5f, Time.time * 1f);
            if (core) core.Rotate(0f, 40f * Time.deltaTime, 0f, Space.Self);
            if (shell) shell.Rotate(0f, -25f * Time.deltaTime, 0f, Space.Self);
            if (gone >= 0f)
            {
                gone += Time.deltaTime;
                float k = Mathf.Clamp01(1f - gone / 0.5f);
                transform.localScale = Vector3.one * k / Mathf.Max(0.001f, transform.parent ? transform.parent.lossyScale.x : 1f);
            }
        }
    }

    /// <summary>The orb floating: on the server, ends it unbroken after the row's lifetime.</summary>
    public sealed class SoulOrbIdle : BaseState
    {
        Vector3 from;

        public override void OnEnter()
        {
            base.OnEnter();
            if (characterBody && !SoulOrb.Live.Contains(characterBody)) SoulOrb.Live.Add(characterBody);
            // spawned in the Trooper's chest (SpawnFor): drifts up rise metres over riseTime, easing out, then bobs (creator: "Rise from
            // the chest, slow"); every machine moves it the same way from where it was spawned
            from = transform.position;
        }

        public override void Update()
        {
            base.Update();
            var row = SoulOrb.Row;
            if (row == null) return;
            float t = Mathf.Clamp01(age / Mathf.Max(0.05f, row.RiseTime));
            float ease = 1f - (1f - t) * (1f - t);
            float bob = t >= 1f ? Mathf.Sin((age - row.RiseTime) * 2.5f) * 0.08f : 0f;
            transform.position = from + Vector3.up * (row.Rise * ease + bob);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (NetworkServer.active && SoulOrb.Row != null && fixedAge >= SoulOrb.Row.Lifetime) outer.SetNextState(new SoulOrbFade());
        }

        public override void OnExit() { if (characterBody) SoulOrb.Live.Remove(characterBody); base.OnExit(); }
    }

    /// <summary>Not broken in time: it shrinks away with the miss sound on every machine; nobody gets the money.</summary>
    public sealed class SoulOrbFade : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            if (characterBody) SoulOrb.Live.Remove(characterBody);
            var row = SoulOrb.Row;
            if (row != null && row.MissSound != "none") Util.PlaySound(row.MissSound, gameObject);
            foreach (var hb in gameObject.GetComponentsInChildren<HurtBox>(true)) foreach (var c in hb.GetComponents<Collider>()) c.enabled = false;
            var look = gameObject.GetComponentInChildren<SoulOrbLook>(true);
            if (look) look.Vanish();
            if (NetworkServer.active) SoulOrb.Missed++;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (NetworkServer.active && fixedAge >= 0.6f) EntityState.Destroy(gameObject);
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }

    /// <summary>Broken: the money to the player who did it (server), the break sound and a burst in the orb colour on every machine.</summary>
    public sealed class SoulOrbBreak : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            if (characterBody) SoulOrb.Live.Remove(characterBody);
            var row = SoulOrb.Row;
            var at = characterBody ? characterBody.corePosition : transform.position;
            if (row != null)
            {
                if (row.BreakSound != "none") Util.PlaySound(row.BreakSound, gameObject);
                Vfx.Burst(at, "mat_glow", Sheet.Color(row.OrbColor), 14, 0.45f, 5f, 0.5f, 0.2f);
            }
            var look = gameObject.GetComponentInChildren<SoulOrbLook>(true);
            if (look) look.gameObject.SetActive(false);
            if (NetworkServer.active)
            {
                SoulOrb.Reward(characterBody);
                SoulOrb.Broken++;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (NetworkServer.active && fixedAge >= 0.1f) EntityState.Destroy(gameObject);
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }

    /// <summary>Coloured smoke plumes where an enemy dies (creator: "plumes of smoke in their colour variant"): the enemy rip's own
    /// smoke puff on RoR2's smoke material, its ramp in the look's colour, rising and spreading. Every machine (EnemyDeath).</summary>
    public static class DeathSmoke
    {
        public static int Played;
        public static Color LastColor;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        // one material per smoke colour, made once (a new one per death was never freed)
        static Material Mat(EnemyDropRow row, Color c)
        {
            var key = ColorUtility.ToHtmlStringRGB(c);
            if (mats.TryGetValue(key, out var m) && m) return m;
            m = new Material(Vfx.Remapped("mat_smoke", c));
            var tex = DeadlockCache.Image(row.SmokeTexture);
            if (tex) m.mainTexture = tex;
            mats[key] = m;
            return m;
        }

        public static void Play(CharacterBody body)
        {
            var row = SoulOrb.Row ?? EnemyDropRow.All.FirstOrDefault();
            if (row == null || !body) return;
            var id = EnemyTag.Of(body);
            if (id == null || !row.Enemies.Contains(id)) return;
            var look = EnemyLook.Of(body);
            int k = look ? Mathf.Max(0, look.Shown) : 0;
            var c = Sheet.Color(row.SmokeColors[Mathf.Min(k, row.SmokeColors.Length - 1)]);
            var er = Sheet.Enemy(id);
            float h = er != null ? er.SourceHeight * EnemyModel.Scale(er) : 2.5f;

            var root = new GameObject("DeadlockDeathSmoke");
            root.transform.position = body.footPosition + Vector3.up * h * 0.3f;
            var ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(row.SmokeTime * 0.6f, row.SmokeTime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(h * 0.35f, h * 0.6f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = c;
            main.gravityModifier = -0.08f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, Mathf.RoundToInt(row.SmokePuffs * 0.6f))), new ParticleSystem.Burst(0.15f, (short)Mathf.Max(1, Mathf.RoundToInt(row.SmokePuffs * 0.4f))) });
            var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.radius = Mathf.Max(0.2f, h * 0.25f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.12f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.4f, 1f), new Keyframe(1f, 1.6f)));
            root.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(row, c);
            ps.Play();
            Object.Destroy(root, row.SmokeTime + 0.5f);
            Played++; LastColor = c;
        }
    }
}
#endif
