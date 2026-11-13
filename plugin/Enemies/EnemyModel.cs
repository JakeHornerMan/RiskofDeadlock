#if OWN_FILES
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadlockOfRain.Enemies
{
    /// <summary>An enemy's model (design/enemies.json; docs/11): every look of it (amber, sapphire) built from the cache under one
    /// root, one shown per spawn (EnemyLook), with a capsule hurtbox, a melee hitbox, a child locator and its own animator. Built
    /// the way ModelBuilder builds a hero (same materials, same clip conversion), without the hero's gun rig.</summary>
    public static class EnemyModel
    {
        public const string MeleeHitBoxGroup = "TrooperMelee";

        /// <summary>Deadlock metres to world metres for this enemy: the heroes' shared factor x the row's sizeMult (creator, 2026-11-11:
        /// "make all trooper like 1.5 time thir height").</summary>
        public static float Scale(EnemyRow row) => ModelBuilder.GlobalScale * row.SizeMult;

        public static GameObject Build(EnemyRow row, float reach)
        {
            var root = new GameObject("mdlDeadlock" + row.Id);
            root.transform.localScale = Vector3.one * Scale(row);
            var looks = new List<EnemyLookPart>();
            foreach (var id in row.Models)
            {
                var look = BuildLook(root.transform, id, row);
                if (look != null) looks.Add(look);
            }
            if (looks.Count == 0) { Object.Destroy(root); return null; }
            // one look shown until EnemyLook picks this body's own (Start)
            for (int i = 1; i < looks.Count; i++) looks[i].gameObject.SetActive(false);

            // the child locator of the first look (every look has the same skeleton names); EnemyLook repoints it to the shown one
            var locator = root.AddComponent<ChildLocator>();
            AccessTools.Field(typeof(ChildLocator), "transformPairs").SetValue(locator, looks[0].Pairs());

            // every look's renderers, always: CharacterModel turns each listed renderer on every frame, so the look not shown and the
            // health heads not shown are switched off through their GameObjects instead (EnemyLook)
            var all = new List<CharacterModel.RendererInfo>();
            foreach (var l in looks) all.AddRange(l.Infos);
            var model = root.AddComponent<CharacterModel>();
            model.baseRendererInfos = all.ToArray();
            model.autoPopulateLightInfos = true;
            var lk = root.AddComponent<EnemyLook>();
            lk.Looks = looks.ToArray();
            lk.Row = row.Id;

            // one capsule hurtbox around the body (Deadlock height, model space), as heroes have
            var hbGo = new GameObject("HurtBox");
            hbGo.layer = LayerIndex.entityPrecise.intVal;
            hbGo.transform.SetParent(root.transform, false);
            var cap = hbGo.AddComponent<CapsuleCollider>();
            cap.direction = 1;
            cap.height = row.SourceHeight;
            cap.radius = row.Width;
            cap.center = new Vector3(0, row.SourceHeight * 0.5f, 0);
            var hb = hbGo.AddComponent<HurtBox>();
            hb.isBullseye = true;
            hb.damageModifier = HurtBox.DamageModifier.Normal;
            var group = root.AddComponent<HurtBoxGroup>();
            group.hurtBoxes = new[] { hb };
            group.mainHurtBox = hb;
            group.bullseyeCount = 1;
            hb.hurtBoxGroup = group; hb.indexInGroup = 0;

            // the melee hitbox: reach metres in front, as wide as the body, from the knees to over the head (world metres, under the
            // model's scale)
            if (reach > 0f)
            {
                float s = Scale(row);
                var box = new GameObject("MeleeHitBox");
                box.transform.SetParent(root.transform, false);
                float h = row.SourceHeight * s;
                box.transform.localPosition = new Vector3(0f, h * 0.55f, reach * 0.5f) / s;
                box.transform.localScale = new Vector3(Mathf.Max(1.6f, row.Width * 4f * s), h * 1.1f, reach) / s;
                var hitbox = box.AddComponent<HitBox>();
                var hbg = root.AddComponent<HitBoxGroup>();
                hbg.groupName = MeleeHitBoxGroup;
                hbg.hitBoxes = new[] { hitbox };
            }
            return root;
        }

        static Color Tint(EnemyRow row, string id)
        {
            int k = System.Array.IndexOf(row.Models, id);
            if (k < 0 || row.LookTints == null || k >= row.LookTints.Length || row.LookTints[k] == "none") return Color.white;
            var p = row.LookTints[k].Split(',');
            float F(int i) => p.Length > i && float.TryParse(p[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 1f;
            return new Color(F(0), F(1), F(2), 1f);
        }

        static EnemyLookPart BuildLook(Transform root, string id, EnemyRow row)
        {
            var asset = DeadlockCache.LoadHero(id);
            if (asset == null) return null;
            var go = new GameObject(id);
            go.transform.SetParent(root, false);
            var armature = new GameObject("Armature").transform;
            armature.SetParent(go.transform, false);
            var bones = new Transform[asset.Bones.Length];
            for (int i = 0; i < bones.Length; i++) bones[i] = new GameObject(asset.Bones[i].Name).transform;
            for (int i = 0; i < bones.Length; i++)
            {
                var b = asset.Bones[i];
                bones[i].SetParent(b.Parent >= 0 ? bones[b.Parent] : armature, false);
                bones[i].localPosition = b.Pos; bones[i].localRotation = b.Rot; bones[i].localScale = b.Scale;
            }
            var infos = new List<CharacterModel.RendererInfo>();
            var health = new List<GameObject>[row.HealthMeshes.Length];
            for (int k = 0; k < health.Length; k++) health[k] = new List<GameObject>();
            // the vertex-coloured materials (vertexColorMaterials) get a part with its own painted texture, as heroes' do
            asset.AtlasVertexColors = true;
            var parts = asset.Parts;
            for (int s = 0; s < parts.Length; s++)
            {
                var def = asset.Materials[Mathf.Clamp(asset.Submeshes[s].Key, 0, asset.Materials.Length - 1)];
                // a health head mesh (healthMeshes, kept as "<material>@<mesh>" parts): shown by EnemyLook one at a time
                int healthPart = -1;
                if (def.Name != null && def.Name.Contains("@"))
                    healthPart = System.Array.IndexOf(row.HealthMeshes, def.Name.Substring(def.Name.IndexOf('@') + 1));
                var meshGo = new GameObject("DeadlockMesh_" + def.Name);
                meshGo.transform.SetParent(go.transform, false);
                var smr = meshGo.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = parts[s];
                smr.bones = bones;
                smr.rootBone = armature;
                smr.updateWhenOffscreen = true;
                // hero: true paints the vertex-coloured materials (*_vc, vertcolor_pbr_basic) into a texture: RoR2's shader shows no vertex
                // colours, so they came out white (2026-11-11, the creator's material check)
                var mat = ModelBuilder.MakeMaterial(asset, def, true);
                // the look's tone (lookTints): a little toward Deadlock's muted colours
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", mat.GetColor("_Color") * Tint(row, id));
                smr.sharedMaterial = mat;
                smr.shadowCastingMode = ShadowCastingMode.On;
                infos.Add(new CharacterModel.RendererInfo { renderer = smr, defaultMaterial = mat, defaultShadowCastingMode = ShadowCastingMode.On });
                if (healthPart >= 0) health[healthPart].Add(meshGo);
            }
            var pairs = new List<ChildLocator.NameTransformPair>();
            void Add(string name, string bone) { var t = ModelBuilder.Find(bones, bone); if (t) pairs.Add(new ChildLocator.NameTransformPair { name = name, transform = t }); }
            Add("Muzzle", row.MuzzleBone); Add("Muzzle", "hand_R");
            Add("Head", "head"); Add("Chest", "spine_1"); Add("Pelvis", "pelvis");
            Add("Flame", "flame_0"); Add("Flame", "wick_end"); Add("Flame", "head_end");
            Add("Banner", "banner_end");

            var anim = go.AddComponent<EnemyAnimator>();
            anim.Setup(id, row, asset, bones);
            var part = go.AddComponent<EnemyLookPart>();
            part.Infos = infos.ToArray();
            part.LocatorNames = pairs.Select(x => x.name).ToArray();
            part.LocatorTransforms = pairs.Select(x => x.transform).ToArray();
            part.HealthParts = health.SelectMany(h => h).ToArray();
            part.HealthStageOf = health.SelectMany((h, k) => h.Select(_ => k)).ToArray();
            part.HealthStages = health.Length;
            part.Animator = anim;
            return part;
        }
    }

    /// <summary>One look of an enemy (amber, sapphire), on that look's root: its renderers, child-locator points, health head meshes
    /// and animator. Only Unity objects, strings, numbers and their arrays, so Unity copies it onto every spawned body
    /// (2026-11-11: a [Serializable] data class in EnemyLook came out empty on the copies: no look, no attack clips, no candle; only
    /// the prefab had them).</summary>
    public sealed class EnemyLookPart : MonoBehaviour
    {
        public CharacterModel.RendererInfo[] Infos;
        public string[] LocatorNames;
        public Transform[] LocatorTransforms;
        public GameObject[] HealthParts;
        public int[] HealthStageOf;
        public int HealthStages;
        public EnemyAnimator Animator;

        public ChildLocator.NameTransformPair[] Pairs()
        {
            var n = LocatorNames ?? new string[0];
            var r = new ChildLocator.NameTransformPair[n.Length];
            for (int i = 0; i < n.Length; i++) r[i] = new ChildLocator.NameTransformPair { name = n[i], transform = LocatorTransforms[i] };
            return r;
        }
    }

    /// <summary>Which look of an enemy is shown (amber or sapphire, by its network id so every player sees the same one, creator
    /// 2026-11-11: "random each spawn"), the candle health readout (Deadlock's head meshes, full/mid/low by thirds of health) and the
    /// candle flame (a small light that goes out on death).</summary>
    public sealed class EnemyLook : MonoBehaviour
    {
        public EnemyLookPart[] Looks;
        public string Row;
        public int Shown = -1;
        public int HealthStage = -1;
        EnemyLookPart look;
        CharacterBody body;
        Light flame;
        float flameBase;

        /// <summary>The animator of the look shown on this body (null before Start).</summary>
        public EnemyAnimator Animator => look ? look.Animator : null;
        // through the body's EnemyTag first (2026-11-11: through the model locator it came back empty on spawned Troopers, so no attack
        // or flinch clip ever played)
        public static EnemyLook Of(CharacterBody b)
        {
            if (!b) return null;
            var tag = b.GetComponent<EnemyTag>();
            if (tag && tag.Look) return tag.Look;
            return b.modelLocator && b.modelLocator.modelTransform ? b.modelLocator.modelTransform.GetComponent<EnemyLook>() : null;
        }

        void Start()
        {
            body = GetComponent<CharacterModel>() ? GetComponent<CharacterModel>().body : null;
            if (Looks == null || Looks.Length == 0) return;
            uint seed = body && body.networkIdentity ? body.networkIdentity.netId.Value : (uint)GetInstanceID();
            Pick((int)(seed % (uint)Looks.Length));
        }

        public void Pick(int k)
        {
            Shown = k;
            for (int i = 0; i < Looks.Length; i++) if (Looks[i]) Looks[i].gameObject.SetActive(i == k);
            look = Looks[k];
            var locator = GetComponent<ChildLocator>();
            if (locator) AccessTools.Field(typeof(ChildLocator), "transformPairs").SetValue(locator, look.Pairs());
            HealthStage = -1;
            // the candle flame: a warm point light at the wick (green on the Medic, whose candle burns green)
            var at = locator ? locator.FindChild("Flame") : null;
            if (at && !flame)
            {
                var go = new GameObject("CandleFlame");
                go.transform.SetParent(at, false);
                flame = go.AddComponent<Light>();
                flame.type = LightType.Point;
                flame.color = Row == "trooper_medic" ? new Color(0.45f, 1f, 0.5f) : new Color(1f, 0.7f, 0.3f);
                flame.range = 2.5f; flame.intensity = flameBase = 2.2f;
                flame.shadows = LightShadows.None;
            }
        }

        void Update()
        {
            if (!look || look.HealthStages == 0) return;
            var hc = body ? body.healthComponent : null;
            float f = hc ? hc.combinedHealthFraction : 1f;
            bool dead = hc && !hc.alive;
            int n = look.HealthStages;
            // full health shows the first mesh; each third lost shows the next (Deadlock's candle melting down)
            int stage = Mathf.Clamp(Mathf.FloorToInt((1f - f) * n), 0, n - 1);
            if (stage != HealthStage)
            {
                HealthStage = stage;
                for (int i = 0; i < look.HealthParts.Length; i++) if (look.HealthParts[i]) look.HealthParts[i].SetActive(look.HealthStageOf[i] == stage);
            }
            if (flame)
            {
                if (dead) flame.enabled = false;
                else flame.intensity = flameBase * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 6f, GetInstanceID() * 0.01f)) * Mathf.Lerp(0.5f, 1f, f);
            }
        }
    }

    /// <summary>A dead enemy's corpse (creator, 2026-11-13: "can the enemy die like a rag doll on the floor and after a second they
    /// fall through it"): the model, frozen in the pose it died in, topples onto the ground as one physics body (a capsule its size:
    /// the per-bone ragdoll threw the bones about), lies there corpseTime seconds after it comes to rest, then sinks through the floor
    /// over sinkTime seconds while fading (CharacterModel.corpseFade, RoR2's corpse fade), and goes. Every machine runs its own.</summary>
    public sealed class EnemyFade : MonoBehaviour
    {
        public float Rest = 1f, Sink = 1.2f, Height = 2f, Radius = 0.5f;
        public Vector3 Push;
        CharacterModel cm;
        Rigidbody rb;
        CapsuleCollider col;
        float t, still, sinkT = -1f;
        public float Value => cm ? cm.corpseFade : 0f;
        public bool Sinking => sinkT >= 0f;

        void Start()
        {
            cm = GetComponent<CharacterModel>();
            col = gameObject.AddComponent<CapsuleCollider>();
            col.direction = 1; col.height = Height; col.radius = Mathf.Min(Radius, Height * 0.5f);
            col.center = transform.InverseTransformVector(Vector3.up * Height * 0.5f);
            col.height = Height / Mathf.Max(0.001f, transform.lossyScale.y);
            col.radius = col.radius / Mathf.Max(0.001f, transform.lossyScale.x);
            gameObject.layer = LayerIndex.ragdoll.intVal;   // the world, not players or bullets
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 60f; rb.drag = 0.4f; rb.angularDrag = 1.5f; rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            // a shove the way it was hit and a turn about the axis across it, so it falls over rather than standing
            var push = Push.sqrMagnitude > 0.01f ? Push : Random.onUnitSphere;
            push.y = 0f; push = push.sqrMagnitude > 0.01f ? push.normalized : Vector3.forward;
            rb.AddForce((push * 2.5f + Vector3.up * 1.5f), ForceMode.VelocityChange);
            rb.AddTorque(Vector3.Cross(Vector3.up, push) * 4f, ForceMode.VelocityChange);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (sinkT < 0f)
            {
                // on the floor and still (or a hard limit): it lies there Rest seconds, then sinks
                bool atRest = rb && rb.velocity.sqrMagnitude < 0.05f && rb.angularVelocity.sqrMagnitude < 0.05f && t > 0.4f;
                still = atRest ? still + Time.deltaTime : 0f;
                if (still >= Rest || t > Rest + 3f)
                {
                    sinkT = 0f;
                    if (rb) { rb.isKinematic = true; rb.detectCollisions = false; }
                    if (col) col.enabled = false;
                }
                return;
            }
            sinkT += Time.deltaTime;
            transform.position += Vector3.down * (Height * 0.6f / Mathf.Max(0.05f, Sink)) * Time.deltaTime;
            if (cm) cm.corpseFade = Mathf.Clamp01(1f - sinkT / Mathf.Max(0.05f, Sink));
            if (sinkT >= Sink + 0.05f) Destroy(gameObject);
        }
    }
}
#endif
