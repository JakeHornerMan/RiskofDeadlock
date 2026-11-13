#if OWN_FILES
using System;
using System.Collections.Generic;
using System.Linq;
using DeadlockOfRain.Sheets;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadlockOfRain.Enemies
{
    /// <summary>The stage dropdown under the Troopers checkbox in the lobby's Risk of Deadlock box (creator, 2026-11-11, "Feature:
    /// Trooper stage selection" and their mock-up: a "N selected" button opening a list of ticked rows). Built from code in the
    /// lobby's own font, with HGButtons for input. Greyed and closed while the Troopers box is unticked (the saved list is kept) and
    /// on a joining client (the host's list counts).</summary>
    public sealed class TrooperStagesUi : MonoBehaviour
    {
        public static TrooperStagesUi Instance;
        RuleCategoryController cat;
        TMP_Text font;
        CanvasGroup group;
        TextMeshProUGUI buttonText, noteText, panelNote;
        GameObject panel, blocker;
        RectTransform buttonRect, content;
        readonly List<(TrooperStages.Stage stage, Image box)> rows = new List<(TrooperStages.Stage, Image)>();
        bool lastEnabled = true, built;
        static Sprite on, off, arrow;

        public bool Interactable { get; private set; }
        public bool PanelOpen => panel && panel.activeSelf;
        public int RowCount => rows.Count;

        [HarmonyPatch(typeof(RuleCategoryController), nameof(RuleCategoryController.SetData))]
        static class Hook
        {
            static void Postfix(RuleCategoryController __instance, RuleCategoryDef categoryDef)
            {
                try
                {
                    if (TrooperRule.Def == null || categoryDef != TrooperRule.Def.category) return;
                    // one item per row (Troopers, Enemy test scene), each with its label beside it
                    var grid = __instance.voteResultGridContainer ? __instance.voteResultGridContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>() : null;
                    if (grid) { grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 1; grid.childAlignment = TextAnchor.UpperLeft; grid.spacing = new Vector2(grid.spacing.x, 6f); }
                    var ui = __instance.GetComponent<TrooperStagesUi>() ?? __instance.gameObject.AddComponent<TrooperStagesUi>();
                    ui.cat = __instance;
                    ui.Build();
                }
                catch (Exception e) { Log.Warn($"Enemies: stage dropdown: {e}"); }
            }
        }

        void OnEnable() { Instance = this; TrooperStages.Changed += Refresh; }
        void OnDisable() { TrooperStages.Changed -= Refresh; if (panel) panel.SetActive(false); if (blocker) blocker.SetActive(false); }
        void OnDestroy() { if (panel) Destroy(panel); if (blocker) Destroy(blocker); if (playButton) Destroy(playButton); if (Instance == this) Instance = null; }

        void Build()
        {
            if (built) { Refresh(); return; }
            built = true;
            on = on ? on : TrooperRule.BoxSprite(true);
            off = off ? off : TrooperRule.BoxSprite(false);
            arrow = arrow ? arrow : Arrow();
            font = cat.categoryHeaderLanguageController ? cat.categoryHeaderLanguageController.GetComponent<TMP_Text>() : null;
            if (!font) font = Object.FindObjectOfType<TextMeshProUGUI>();

            // inside the box, under the Troopers tick (creator, 2026-11-13: "a Risk of deadlock box ... inside is the troopers check
            // box and then below it within the box is the selection for the levels"): a child of the box's icon panel, out of its grid
            // layout, at the bottom of a room the grid leaves free
            var grid = cat.voteResultGridContainer;
            var parent = grid ? (Transform)grid : cat.transform;
            var gl = grid ? grid.GetComponent<GridLayoutGroup>() : null;
            if (gl) gl.padding = new RectOffset(gl.padding.left, gl.padding.right, gl.padding.top, gl.padding.bottom + (int)Room);
            var holder = new GameObject("DeadlockTrooperStages", typeof(RectTransform));
            var hr = (RectTransform)holder.transform;
            hr.SetParent(parent, false);
            holder.AddComponent<LayoutElement>().ignoreLayout = true;
            hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(1f, 0f); hr.pivot = new Vector2(0.5f, 0f);
            hr.anchoredPosition = new Vector2(0f, 4f); hr.sizeDelta = new Vector2(-12f, Room - 8f);
            var vl = holder.AddComponent<VerticalLayoutGroup>();
            vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.spacing = 2f;
            vl.padding = new RectOffset(4, 4, 0, 0);
            group = holder.AddComponent<CanvasGroup>();
            BuildPlayButton();

            var btn = Box(hr, "StagesButton", new Color(0.12f, 0.1f, 0.08f, 0.95f), 34f);
            buttonRect = (RectTransform)btn.transform;
            buttonText = Text(buttonRect, "", 20f, TextAlignmentOptions.MidlineLeft, new Vector2(12f, 0f), new Vector2(-36f, 0f));
            var ar = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            var arr = (RectTransform)ar.transform; arr.SetParent(buttonRect, false);
            arr.anchorMin = arr.anchorMax = new Vector2(1f, 0.5f); arr.pivot = new Vector2(1f, 0.5f); arr.anchoredPosition = new Vector2(-10f, 0f); arr.sizeDelta = new Vector2(16f, 10f);
            ar.GetComponent<Image>().sprite = arrow; ar.GetComponent<Image>().raycastTarget = false;
            Click(btn, () => Open(!PanelOpen));

            var note = new GameObject("Note", typeof(RectTransform));
            note.transform.SetParent(hr, false);
            noteText = note.AddComponent<HGTextMeshProUGUI>(); Style(noteText, 16f, TextAlignmentOptions.MidlineLeft);
            noteText.color = new Color(1f, 0.55f, 0.45f);
            note.AddComponent<LayoutElement>().minHeight = 20f;

            BuildPanel();
            Refresh();
        }

        const float Room = 66f;
        GameObject playButton;
        CanvasGroup playGroup;
        bool starting;

        /// <summary>Below the Risk of Deadlock box (creator, 2026-11-13: "outside of the box at the bottom we can have an orange button
        /// labeled Play Test scene"): starts a run on the Enemy test scene at once. The host's: it votes the hidden test-scene rule on
        /// (so the run, and every player's game, knows it) and starts the run.</summary>
        void BuildPlayButton()
        {
            var parent = cat.transform.parent;
            if (!parent) return;
            playButton = new GameObject("DeadlockPlayTestScene", typeof(RectTransform), typeof(Image));
            var pr = (RectTransform)playButton.transform;
            pr.SetParent(parent, false);
            pr.SetSiblingIndex(cat.transform.GetSiblingIndex() + 1);
            playButton.GetComponent<Image>().color = TrooperRule.Orange;
            var le = playButton.AddComponent<LayoutElement>(); le.minHeight = 46f; le.preferredHeight = 46f; le.flexibleWidth = 1f;
            playGroup = playButton.AddComponent<CanvasGroup>();
            var label = Text(pr, (EnemyTestSceneRow.All.FirstOrDefault() != null ? "Play Test scene" : "Play Test scene"), 22f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
            label.color = Color.white;
            Click(playButton, () => { if (!starting && NetworkServer.active && PreGameController.instance) StartCoroutine(PlayTestScene()); });
            if (!parent.GetComponent<LayoutGroup>())
            {
                var cr = (RectTransform)cat.transform;
                pr.anchorMin = cr.anchorMin; pr.anchorMax = cr.anchorMax; pr.pivot = new Vector2(0.5f, 1f);
                pr.sizeDelta = new Vector2(cr.sizeDelta.x, 46f);
                pr.anchoredPosition = cr.anchoredPosition - new Vector2(0f, cr.rect.height + 8f);
            }
        }

        public static int PlayPressed;

        System.Collections.IEnumerator PlayTestScene()
        {
            starting = true;
            PlayPressed++;
            var voter = PreGameRuleVoteController.FindForUser(LocalUserManager.GetFirstLocalUser()?.currentNetworkUser);
            if (voter && TrooperRule.TestDef != null) voter.SetVote(TrooperRule.TestDef.globalIndex, 1);
            float t0 = Time.unscaledTime;
            while (!TrooperRule.TestSceneOn() && Time.unscaledTime - t0 < 3f) yield return null;
            var pg = PreGameController.instance;
            if (pg && TrooperRule.TestSceneOn()) AccessTools.Method(typeof(PreGameController), "StartRun").Invoke(pg, null);
            else { Log.Warn("Enemy test scene: the lobby didn't take the vote; not started"); starting = false; }
        }

        /// <summary>Public for the test: press the button.</summary>
        public void PressPlay() { if (!starting && NetworkServer.active && PreGameController.instance) StartCoroutine(PlayTestScene()); }
        public bool HasPlayButton => playButton;

        void BuildPanel()
        {
            var canvas = cat.GetComponentInParent<Canvas>();
            var root = canvas ? canvas.rootCanvas.transform : cat.transform;
            // a full-screen catcher behind the open list: a click outside closes it
            blocker = new GameObject("DeadlockTrooperStagesBlocker", typeof(RectTransform), typeof(Image));
            var br = (RectTransform)blocker.transform; br.SetParent(root, false);
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            Click(blocker, () => Open(false));
            blocker.SetActive(false);

            panel = new GameObject("DeadlockTrooperStagesList", typeof(RectTransform), typeof(Image));
            var pr = (RectTransform)panel.transform; pr.SetParent(root, false);
            pr.pivot = new Vector2(0f, 1f); pr.sizeDelta = new Vector2(380f, Mathf.Min(Screen.height * 0.6f, 620f));
            panel.GetComponent<Image>().color = new Color(0.07f, 0.06f, 0.05f, 0.97f);
            var outline = panel.AddComponent<Outline>(); outline.effectColor = TrooperRule.Orange; outline.effectDistance = new Vector2(2f, -2f);

            var reset = Box(pr, "Reset", new Color(0.2f, 0.15f, 0.1f, 1f), 34f, true);
            var rr = (RectTransform)reset.transform;
            rr.anchorMin = new Vector2(0f, 1f); rr.anchorMax = new Vector2(1f, 1f); rr.pivot = new Vector2(0.5f, 1f);
            rr.offsetMin = new Vector2(8f, -42f); rr.offsetMax = new Vector2(-8f, -8f);
            Text(rr, "Reset to default", 18f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
            Click(reset, () => { TrooperStages.Reset(); });

            var pn = new GameObject("Note", typeof(RectTransform)); var pnr = (RectTransform)pn.transform; pnr.SetParent(pr, false);
            pnr.anchorMin = new Vector2(0f, 1f); pnr.anchorMax = new Vector2(1f, 1f); pnr.pivot = new Vector2(0.5f, 1f);
            pnr.offsetMin = new Vector2(12f, -66f); pnr.offsetMax = new Vector2(-12f, -44f);
            panelNote = pn.AddComponent<HGTextMeshProUGUI>(); Style(panelNote, 16f, TextAlignmentOptions.MidlineLeft); panelNote.color = new Color(1f, 0.55f, 0.45f);

            // the scrolling list
            var view = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            var vr = (RectTransform)view.transform; vr.SetParent(pr, false);
            vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = new Vector2(6f, 6f); vr.offsetMax = new Vector2(-6f, -68f);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
            var c = new GameObject("Content", typeof(RectTransform));
            content = (RectTransform)c.transform; content.SetParent(vr, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var cl = c.AddComponent<VerticalLayoutGroup>(); cl.childControlHeight = true; cl.childControlWidth = true; cl.childForceExpandHeight = false; cl.spacing = 2f;
            cl.padding = new RectOffset(4, 4, 4, 4);
            c.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = panel.AddComponent<ScrollRect>(); sr.viewport = vr; sr.content = content; sr.horizontal = false; sr.scrollSensitivity = 30f;
            sr.movementType = ScrollRect.MovementType.Clamped;
            panel.SetActive(false);
        }

        void FillRows()
        {
            foreach (Transform t in content) Destroy(t.gameObject);
            rows.Clear();
            string last = null;
            foreach (var s in TrooperStages.Visible)
            {
                if (s.Group != last)
                {
                    last = s.Group;
                    var h = new GameObject("Group " + s.Group, typeof(RectTransform)); h.transform.SetParent(content, false);
                    var ht = h.AddComponent<HGTextMeshProUGUI>(); Style(ht, 18f, TextAlignmentOptions.BottomLeft); ht.text = s.Group; ht.color = TrooperRule.Orange;
                    h.AddComponent<LayoutElement>().minHeight = 28f;
                }
                var row = Box(content, "Row " + s.Key, new Color(1f, 1f, 1f, 0.03f), 30f, true);
                var rr = (RectTransform)row.transform;
                var b = new GameObject("Box", typeof(RectTransform), typeof(Image));
                var bt = (RectTransform)b.transform; bt.SetParent(rr, false);
                bt.anchorMin = bt.anchorMax = new Vector2(0f, 0.5f); bt.pivot = new Vector2(0f, 0.5f); bt.anchoredPosition = new Vector2(8f, 0f); bt.sizeDelta = new Vector2(24f, 24f);
                var img = b.GetComponent<Image>(); img.raycastTarget = false;
                Text(rr, s.Name, 18f, TextAlignmentOptions.MidlineLeft, new Vector2(42f, 0f), new Vector2(-6f, 0f));
                var stage = s;
                Click(row, () => { if (Interactable) TrooperStages.Toggle(stage.Key, !TrooperStages.Current().Contains(stage.Key)); });
                rows.Add((s, img));
            }
        }

        public void Open(bool open)
        {
            if (open && !Interactable) return;
            if (open)
            {
                if (rows.Count == 0 || rows.Count != TrooperStages.Visible.Count()) FillRows();
                Refresh();
                // just under the button
                var corners = new Vector3[4]; buttonRect.GetWorldCorners(corners);
                var pr = (RectTransform)panel.transform;
                pr.sizeDelta = new Vector2(Mathf.Max(380f, buttonRect.rect.width), Mathf.Min(Screen.height * 0.6f, 620f));
                // under the button if it fits on screen, else above it (2026-11-11: it ran off the bottom); kept inside the screen sideways
                var canvas = panel.GetComponentInParent<Canvas>();
                float scale = canvas ? canvas.rootCanvas.scaleFactor : 1f;
                float h = pr.sizeDelta.y * scale, w = pr.sizeDelta.x * scale;
                var cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var bottom = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                var top = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
                bool below = bottom.y - h >= 0f;
                pr.pivot = below ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
                var at = below ? bottom : top;
                at.x = Mathf.Clamp(at.x, 0f, Screen.width - w);
                if (!below) at.y = Mathf.Min(at.y, Screen.height - h);
                RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)pr.parent, at, cam, out var world);
                pr.position = world;
                blocker.transform.SetAsLastSibling(); panel.transform.SetAsLastSibling();
            }
            blocker.SetActive(open);
            panel.SetActive(open);
        }

        void Refresh()
        {
            if (!built || !buttonText) return;
            var cur = TrooperStages.Current();
            var visible = TrooperStages.Visible.ToList();
            int n = visible.Count(s => cur.Contains(s.Key));
            bool host = NetworkServer.active;
            buttonText.text = !TrooperStages.Ready ? "Stages: reading the game's stages..." :
                host ? $"Stages: {n} selected" + (TrooperStages.IsDefault ? " (default)" : "") : "Stages: the host's choice";
            string none = TrooperStages.Ready && n == 0 ? "No stages selected - Troopers won't spawn." : "";
            noteText.text = host ? none : "";
            noteText.gameObject.SetActive(noteText.text != "");
            if (panelNote) panelNote.text = none;
            foreach (var (s, img) in rows) if (img) img.sprite = cur.Contains(s.Key) ? on : off;
        }

        void Update()
        {
            bool ruleOn = true;
            var pg = PreGameController.instance;
            if (pg && TrooperRule.Def != null && pg.readOnlyRuleBook != null)
                ruleOn = !(pg.readOnlyRuleBook.GetRuleChoice(TrooperRule.Def)?.extraData is bool b) || b;
            Interactable = ruleOn && NetworkServer.active && TrooperStages.Ready;
            if (!starting && NetworkServer.active && pg && TrooperRule.TestDef != null && TrooperRule.TestSceneOn())
            {
                var voter = PreGameRuleVoteController.FindForUser(LocalUserManager.GetFirstLocalUser()?.currentNetworkUser);
                if (voter) voter.SetVote(TrooperRule.TestDef.globalIndex, 0);
            }
            if (playGroup) { bool canPlay = NetworkServer.active && !starting; playGroup.interactable = canPlay; playGroup.alpha = canPlay ? 1f : 0.4f; }
            if (group) { group.interactable = Interactable; group.alpha = Interactable ? 1f : 0.4f; }
            if (!Interactable && PanelOpen) Open(false);
            bool en = Interactable;
            if (en != lastEnabled || Time.frameCount % 30 == 0) { lastEnabled = en; Refresh(); }
        }

        // ---- small builders ----
        GameObject Box(Transform parent, string name, Color color, float height, bool flat = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = color;
            if (!flat) { var o = go.AddComponent<Outline>(); o.effectColor = TrooperRule.Orange; o.effectDistance = new Vector2(1.5f, -1.5f); }
            var le = go.AddComponent<LayoutElement>(); le.minHeight = height; le.preferredHeight = height; le.flexibleWidth = 1f;
            return go;
        }

        void Click(GameObject go, Action a)
        {
            var b = go.AddComponent<HGButton>();
            b.allowAllEventSystems = true;
            b.targetGraphic = go.GetComponent<Image>();
            var cb = b.colors; cb.highlightedColor = new Color(1f, 0.85f, 0.6f); cb.pressedColor = new Color(1f, 0.7f, 0.4f); cb.disabledColor = new Color(0.6f, 0.6f, 0.6f); b.colors = cb;
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(() => a());
        }

        TextMeshProUGUI Text(RectTransform parent, string s, float size, TextAlignmentOptions align, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            var r = (RectTransform)go.transform; r.SetParent(parent, false);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = offMin; r.offsetMax = offMax;
            var t = go.AddComponent<HGTextMeshProUGUI>(); Style(t, size, align); t.text = s;
            return t;
        }

        void Style(TextMeshProUGUI t, float size, TextAlignmentOptions align)
        {
            if (font) { t.font = font.font; t.fontSharedMaterial = font.fontSharedMaterial; }
            t.fontSize = size; t.alignment = align; t.color = Color.white; t.raycastTarget = false; t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
        }

        static Sprite Arrow()
        {
            const int w = 32, h = 20;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // a down-pointing triangle: full width at the top, a point at the bottom
                    float half = (y / (float)(h - 1)) * (w * 0.5f);
                    px[y * w + x] = Mathf.Abs(x + 0.5f - w * 0.5f) <= half ? (Color32)TrooperRule.Orange : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
#endif
