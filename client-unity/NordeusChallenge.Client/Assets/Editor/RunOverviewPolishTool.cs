// Editor-only tool to rebuild the RunOverview scene and the RunMapNode prefab
// in a controlled, repeatable way. The tool never deletes scripts or game data
// and never overwrites SerializedObject references on RunOverviewController
// without finding (or creating) a replacement target object first.
//
// Menu items:
//   Tools/Nordeus Challenge/UI/Rebuild Run Overview Layout
//   Tools/Nordeus Challenge/UI/Rebuild Run Map Node Prefab
//   Tools/Nordeus Challenge/UI/Validate Run Overview References
//
// Run order recommended:
//   1. Rebuild Run Map Node Prefab (so the controller's prefab field stays valid).
//   2. Rebuild Run Overview Layout (creates / updates the scene hierarchy and rebinds).
//   3. Validate Run Overview References (sanity check; logs anything still missing).
//
// After running the layout rebuild, double-check the inspector on
// RunOverviewController for any field the tool flagged as "manual" in the
// console.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NordeusChallenge.Client.UI.Common;
using NordeusChallenge.Client.UI.RunOverview;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.EditorTools
{
    public static class RunOverviewPolishTool
    {
        private const string ScenePath = "Assets/Scenes/RunOverview.unity";
        private const string MapNodePrefabPath = "Assets/Prefabs/UI/RunMapNode.prefab";

        private const string ArtBackground = "Assets/Art/UI/RunOverviewBackground.png";
        private const string ArtParchment = "Assets/Art/UI/ParchmentPanel.png";
        private const string ArtFrame = "Assets/Art/UI/FantasyPanelFrame.png";
        private const string ArtButton = "Assets/Art/UI/ButtonFrame.png";
        private const string ArtGold = "Assets/Art/UI/GoldIcon.png";
        private const string ArtNode = "Assets/Art/UI/NodeToken.png";

        private const string ArtIconBattle = "Assets/Art/UI/Icon_Battle.png";
        private const string ArtIconElite = "Assets/Art/UI/Icon_Elite.png";
        private const string ArtIconShop = "Assets/Art/UI/Icon_Shop.png";
        private const string ArtIconBoss = "Assets/Art/UI/Icon_Boss.png";
        private const string ArtIconTreasure = "Assets/Art/UI/Icon_Treasure.png";
        private const string ArtIconEvent = "Assets/Art/UI/Icon_Event.png";
        private const string ArtIconRest = "Assets/Art/UI/Icon_Rest.png";

        // Dark fantasy palette anchored on the parchment/torchlight art set.
        private static readonly Color BgTint = new Color(0.10f, 0.09f, 0.10f, 1f);
        private static readonly Color OverlayTint = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color ParchmentTint = new Color(0.93f, 0.86f, 0.70f, 1f);
        private static readonly Color FrameTint = new Color(1f, 0.93f, 0.78f, 1f);
        private static readonly Color GoldText = new Color(1f, 0.84f, 0.36f, 1f);
        private static readonly Color BodyText = new Color(0.18f, 0.13f, 0.08f, 1f);
        private static readonly Color HeaderText = new Color(0.95f, 0.86f, 0.62f, 1f);
        private static readonly Color SubtleText = new Color(0.30f, 0.24f, 0.18f, 1f);

        // ------------------------------------------------------------------
        //  Menu items
        // ------------------------------------------------------------------

        // Clears the Editor Selection before any destructive operation
        // (DestroyImmediate, OpenScene, prefab rebuild). Without this, if the
        // user has a RectTransform / UI object selected, the Inspector tries
        // to refresh its editor against a now-destroyed target and throws
        //   SerializedObjectNotCreatableException: Object at index 0 is null
        // from RectTransformEditor.OnEnable().
        private static void ClearSelectionToAvoidInspectorErrors()
        {
            Selection.activeObject = null;
            Selection.objects = System.Array.Empty<Object>();
        }

        [MenuItem("Tools/Nordeus Challenge/UI/Rebuild Run Map Node Prefab")]
        public static void RebuildMapNodePrefab()
        {
            ClearSelectionToAvoidInspectorErrors();
            var dir = Path.GetDirectoryName(MapNodePrefabPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            // Build a fresh root in memory.
            var root = new GameObject("RunMapNode", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(120f, 120f);
            var rootImage = root.GetComponent<Image>();
            rootImage.sprite = LoadSprite(ArtNode);
            rootImage.type = Image.Type.Simple;
            rootImage.preserveAspect = true;
            rootImage.color = Color.white;
            rootImage.raycastTarget = true;
            var le = root.GetComponent<LayoutElement>();
            le.preferredWidth = 120f;
            le.preferredHeight = 120f;

            // BackgroundFrame as visual layer behind the icon (uses ButtonFrame for a metal ring feel).
            var bgFrame = CreateChildImage(root.transform, "BackgroundFrame", ArtButton, FrameTint, fill: true);
            bgFrame.GetComponent<Image>().raycastTarget = false;

            // Highlight (glow ring) toggled by RunMapNodeView based on state.
            var highlight = CreateChildImage(root.transform, "Highlight", ArtNode, new Color(1f, 0.8f, 0.4f, 0.45f), fill: true);
            var highlightRect = highlight.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = new Vector2(-12f, -12f);
            highlightRect.offsetMax = new Vector2(12f, 12f);
            var highlightImg = highlight.GetComponent<Image>();
            highlightImg.raycastTarget = false;
            highlight.SetActive(false);

            // Center icon (the type icon) — driven by RunMapNodeView at runtime.
            var icon = CreateChildImage(root.transform, "Icon", ArtIconBattle, Color.white, fill: false);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(72f, 72f);
            var iconImg = icon.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Optional label (RunMapNodeView hides it for the new design, but the field still has to bind).
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(root.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -4f);
            labelRect.sizeDelta = new Vector2(0f, 24f);
            var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontSize = 14f;
            labelTmp.color = HeaderText;
            labelTmp.text = string.Empty;
            labelTmp.raycastTarget = false;

            // Add the view component last so SerializedObject can reference siblings.
            var view = root.AddComponent<RunMapNodeView>();
            var so = new SerializedObject(view);
            BindField(so, "button", root.GetComponent<Button>());
            BindField(so, "label", labelTmp);
            BindField(so, "typeIcon", iconImg);
            BindField(so, "highlight", highlightImg);

            BindField(so, "battleSprite", LoadSprite(ArtIconBattle));
            BindField(so, "eliteSprite", LoadSprite(ArtIconElite));
            BindField(so, "shopSprite", LoadSprite(ArtIconShop));
            BindField(so, "bossSprite", LoadSprite(ArtIconBoss));
            BindField(so, "treasureSprite", LoadSprite(ArtIconTreasure));
            BindField(so, "eventSprite", LoadSprite(ArtIconEvent));
            BindField(so, "restSprite", LoadSprite(ArtIconRest));

            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, MapNodePrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RunOverview Polish] Saved prefab {MapNodePrefabPath}.");
        }

        [MenuItem("Tools/Nordeus Challenge/UI/Rebuild Run Overview Layout")]
        public static void RebuildRunOverviewLayout()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Run Overview Polish", $"Scene not found: {ScenePath}", "OK");
                return;
            }

            ClearSelectionToAvoidInspectorErrors();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureCamera();
            EnsureEventSystem();

            var canvas = EnsureCanvas();
            var canvasGo = canvas.gameObject;

            // Create or fetch every named child the controller needs.
            var background = EnsureBackground(canvasGo);
            var header = EnsureHeader(canvasGo);
            var mainLayout = EnsureMainLayout(canvasGo);
            var leftPanel = EnsureLeftPanel(mainLayout, out var goldText, out var shopButton, out var itemBtn, out var moveBtn);
            var centerPanel = EnsureCenterPanel(mainLayout, out var mapContent, out var mapTitleText, out var runVictoryText);
            var rightPanel = EnsureRightPanel(mainLayout, out var heroText, out var equippedMovesText, out var equippedItemsText, out var inventoryText);
            var footer = EnsureFooter(canvasGo, out var saveAndExitBtn, out var saveAndExitLabel, out var statusText);
            var endless = EnsureEndlessPanel(canvasGo);

            // Hidden host objects for fields we still have to satisfy but don't show.
            var hidden = EnsureHidden(canvasGo);

            // Hook controller references via SerializedObject so we never lose the link.
            var controller = FindOrCreateRunOverviewController(canvasGo);
            BindController(controller,
                heroText: heroText,
                equippedMovesText: equippedMovesText,
                moveManagementButton: moveBtn,
                equippedItemsText: equippedItemsText,
                inventoryText: inventoryText,
                itemManagementButton: itemBtn,
                goldText: goldText,
                shopButton: shopButton,
                mapContainer: mapContent,
                mapTitleText: mapTitleText,
                runVictoryText: runVictoryText,
                endlessPanel: endless.root,
                endlessTitleText: endless.title,
                endlessFloorText: endless.floor,
                endlessNextEnemyText: endless.next,
                endlessShopText: endless.shop,
                endlessBestFloorText: endless.best,
                endlessDefeatText: endless.defeat,
                endlessStartFloorButton: endless.startBtn,
                endlessStartFloorButtonLabel: endless.startLbl,
                endlessReturnToMenuButton: endless.menuBtn,
                hidden: hidden);

            // Wire SaveAndExitButton if present in the scene.
            BindSaveAndExit(canvasGo, saveAndExitBtn, saveAndExitLabel, statusText);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[RunOverview Polish] Layout rebuilt and references rebound. Verify the inspector and run the scene.");
        }

        [MenuItem("Tools/Nordeus Challenge/UI/Fix Run Overview Map Layout")]
        public static void FixRunOverviewMapLayout()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Run Overview Polish", $"Scene not found: {ScenePath}", "OK");
                return;
            }

            ClearSelectionToAvoidInspectorErrors();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = Object.FindObjectOfType<RunOverviewController>();
            if (controller == null)
            {
                Debug.LogWarning("[RunOverview Polish] No RunOverviewController found. Run 'Rebuild Run Overview Layout' first.");
                return;
            }

            // Find or rebuild the scroll plumbing.
            var canvas = Object.FindObjectOfType<Canvas>();
            var centerPanel = canvas != null
                ? canvas.transform.Find("MainLayout/CenterPanel")
                : null;
            if (centerPanel == null)
            {
                Debug.LogWarning("[RunOverview Polish] CenterPanel not found. Run 'Rebuild Run Overview Layout' first.");
                return;
            }

            var mapFrame = centerPanel.Find("MapFrame");
            if (mapFrame == null)
            {
                Debug.LogWarning("[RunOverview Polish] MapFrame not found.");
                return;
            }

            // Make sure the scroll/viewport/content chain exists with the right rects.
            var scroll = (RectTransform)EnsureChildRect(mapFrame, "MapScrollView");
            var scrollRectComp = scroll.GetComponent<ScrollRect>() ?? scroll.gameObject.AddComponent<ScrollRect>();
            scroll.anchorMin = Vector2.zero;
            scroll.anchorMax = Vector2.one;
            scroll.offsetMin = new Vector2(20f, 60f);
            scroll.offsetMax = new Vector2(-20f, -20f);
            var scrollImg = scroll.GetComponent<Image>() ?? scroll.gameObject.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.18f);
            scrollImg.raycastTarget = true;

            var viewport = (RectTransform)EnsureChildRect(scroll, "Viewport");
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            var viewportImg = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImg.raycastTarget = true;
            var mask = viewport.GetComponent<Mask>() ?? viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = (RectTransform)EnsureChildRect(viewport, "MapContent");
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.localScale = Vector3.one;
            if (content.sizeDelta.y < 600f) content.sizeDelta = new Vector2(600f, 1200f);

            // Strip layout components from MapContent (the controller drives positions absolutely).
            StripAutoLayout(content.gameObject);

            scrollRectComp.viewport = viewport;
            scrollRectComp.content = content;
            scrollRectComp.horizontal = false;
            scrollRectComp.vertical = true;
            scrollRectComp.movementType = ScrollRect.MovementType.Clamped;

            // Ensure layered children (lines below nodes).
            var lines = (RectTransform)EnsureChildRect(content, "LinesLayer");
            lines.anchorMin = new Vector2(0.5f, 1f);
            lines.anchorMax = new Vector2(0.5f, 1f);
            lines.pivot = new Vector2(0.5f, 1f);
            lines.anchoredPosition = Vector2.zero;
            lines.sizeDelta = Vector2.zero;
            lines.localScale = Vector3.one;
            StripAutoLayout(lines.gameObject);
            // Lines layer should not block clicks on nodes above it.
            var linesGraphic = lines.GetComponent<Graphic>();
            if (linesGraphic != null) linesGraphic.raycastTarget = false;

            var nodes = (RectTransform)EnsureChildRect(content, "NodesLayer");
            nodes.anchorMin = new Vector2(0.5f, 1f);
            nodes.anchorMax = new Vector2(0.5f, 1f);
            nodes.pivot = new Vector2(0.5f, 1f);
            nodes.anchoredPosition = Vector2.zero;
            nodes.sizeDelta = Vector2.zero;
            nodes.localScale = Vector3.one;
            StripAutoLayout(nodes.gameObject);

            // Render order: lines first (below), nodes second (above).
            lines.SetSiblingIndex(0);
            nodes.SetSiblingIndex(1);

            // Rebind the controller's mapContainer to MapContent (in case it drifted).
            var so = new SerializedObject(controller);
            BindField(so, "mapContainer", content);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            // Make sure the prefab root is sane (RectTransform, scale 1, RunMapNodeView present).
            FixMapNodePrefabBasics();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[RunOverview Polish] Map layout repaired. MapContent now has LinesLayer and NodesLayer; controller's mapContainer rebound.");
        }

        private static Transform EnsureChildRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.GetComponent<RectTransform>() == null)
                {
                    Debug.LogWarning($"[RunOverview Polish] '{name}' missing RectTransform; recreating.");
                    Object.DestroyImmediate(existing.gameObject);
                    existing = null;
                }
            }
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                existing = go.transform;
            }
            return existing;
        }

        private static void StripAutoLayout(GameObject go)
        {
            var lg = go.GetComponent<LayoutGroup>();
            if (lg != null) Object.DestroyImmediate(lg);
            var csf = go.GetComponent<ContentSizeFitter>();
            if (csf != null) Object.DestroyImmediate(csf);
        }

        private static void FixMapNodePrefabBasics()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapNodePrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[RunOverview Polish] {MapNodePrefabPath} not found. Run 'Rebuild Run Map Node Prefab' first.");
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var rect = instance.GetComponent<RectTransform>();
                if (rect == null) rect = instance.AddComponent<RectTransform>();
                if (rect.sizeDelta.x < 80f || rect.sizeDelta.y < 80f)
                {
                    rect.sizeDelta = new Vector2(110f, 110f);
                }
                instance.transform.localScale = Vector3.one;
                if (instance.GetComponent<RunMapNodeView>() == null)
                {
                    Debug.LogWarning("[RunOverview Polish] RunMapNode prefab is missing the RunMapNodeView script. Run 'Rebuild Run Map Node Prefab'.");
                }
                PrefabUtility.SaveAsPrefabAsset(instance, MapNodePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [MenuItem("Tools/Nordeus Challenge/UI/Validate Run Overview Map Layout")]
        public static void ValidateMapLayout()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Run Overview Polish", $"Scene not found: {ScenePath}", "OK");
                return;
            }

            ClearSelectionToAvoidInspectorErrors();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = Object.FindObjectOfType<RunOverviewController>();
            int issues = 0;

            if (controller == null) { Debug.LogError("[RunOverview Polish] RunOverviewController missing."); issues++; }
            else
            {
                var so = new SerializedObject(controller);
                var mc = so.FindProperty("mapContainer");
                var mp = so.FindProperty("mapNodePrefab");

                if (mc == null || mc.objectReferenceValue == null) { Debug.LogError("[RunOverview Polish] mapContainer not assigned."); issues++; }
                else
                {
                    var content = mc.objectReferenceValue as Component;
                    var rect = content != null ? content.GetComponent<RectTransform>() : null;
                    if (rect == null) { Debug.LogError("[RunOverview Polish] mapContainer is not a UI RectTransform."); issues++; }
                    else
                    {
                        if (rect.GetComponent<LayoutGroup>() != null) { Debug.LogError("[RunOverview Polish] MapContent has a LayoutGroup; remove it (controller uses absolute positions)."); issues++; }
                        if (rect.GetComponent<ContentSizeFitter>() != null) { Debug.LogError("[RunOverview Polish] MapContent has a ContentSizeFitter; remove it."); issues++; }
                        if (Mathf.Abs(rect.localScale.x - 1f) > 0.001f || Mathf.Abs(rect.localScale.y - 1f) > 0.001f) { Debug.LogError("[RunOverview Polish] MapContent localScale is not (1,1,1)."); issues++; }

                        var parentName = rect.parent != null ? rect.parent.name : "<none>";
                        if (parentName != "Viewport") { Debug.LogWarning($"[RunOverview Polish] MapContent parent is '{parentName}', expected 'Viewport'. Run 'Fix Run Overview Map Layout'."); issues++; }

                        if (rect.Find("NodesLayer") == null) { Debug.LogWarning("[RunOverview Polish] MapContent has no NodesLayer child. Run 'Fix Run Overview Map Layout'."); }
                        if (rect.Find("LinesLayer") == null) { Debug.LogWarning("[RunOverview Polish] MapContent has no LinesLayer child. Run 'Fix Run Overview Map Layout'."); }
                    }
                }

                if (mp == null || mp.objectReferenceValue == null) { Debug.LogError("[RunOverview Polish] mapNodePrefab not assigned."); issues++; }
                else
                {
                    var prefab = ((Component)mp.objectReferenceValue).gameObject;
                    var prefabRect = prefab.GetComponent<RectTransform>();
                    if (prefabRect == null) { Debug.LogError("[RunOverview Polish] mapNodePrefab root has no RectTransform."); issues++; }
                    else
                    {
                        if (prefabRect.sizeDelta.x < 80f || prefabRect.sizeDelta.y < 80f)
                        {
                            Debug.LogWarning($"[RunOverview Polish] mapNodePrefab size is small ({prefabRect.sizeDelta}); expected ~110x110.");
                        }
                    }
                }
            }

            // Detect leftover placeholder TMP labels with default text.
            foreach (var tmp in Object.FindObjectsOfType<TMPro.TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (string.Equals(tmp.text, "New Text", System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[RunOverview Polish] Placeholder text 'New Text' found on '{GetPath(tmp.transform)}'. Replace it with meaningful copy.");
                }
            }

            Debug.Log($"[RunOverview Polish] Map layout validation done. Issues: {issues}.");
        }

        private static string GetPath(Transform t)
        {
            if (t.parent == null) return t.name;
            return GetPath(t.parent) + "/" + t.name;
        }

        [MenuItem("Tools/Nordeus Challenge/UI/Validate Run Overview References")]
        public static void ValidateReferences()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Run Overview Polish", $"Scene not found: {ScenePath}", "OK");
                return;
            }

            ClearSelectionToAvoidInspectorErrors();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = Object.FindObjectOfType<RunOverviewController>();
            if (controller == null)
            {
                Debug.LogWarning("[RunOverview Polish] No RunOverviewController found in scene.");
                return;
            }

            var so = new SerializedObject(controller);
            var prop = so.GetIterator();
            int missing = 0;
            int ok = 0;
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null)
                    {
                        Debug.LogWarning($"[RunOverview Polish] Missing reference: {prop.propertyPath}");
                        missing++;
                    }
                    else ok++;
                }
            }
            Debug.Log($"[RunOverview Polish] Validation complete. OK: {ok}. Missing: {missing}.");
        }

        // ------------------------------------------------------------------
        //  Hierarchy builders
        // ------------------------------------------------------------------

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera", typeof(Camera));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BgTint;
            cam.orthographic = true;
        }

        private static void EnsureEventSystem()
        {
            // If the scene already has an EventSystem (with whichever input module the project uses),
            // leave it alone. Creating a fresh one here risks adding the wrong module type.
            var existing = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (existing == null)
            {
                Debug.LogWarning("[RunOverview Polish] No EventSystem in scene. Add one manually (GameObject > UI > Event System) so buttons receive input.");
            }
        }

        private static Canvas EnsureCanvas()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            GameObject go;
            if (canvas == null)
            {
                go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
            }
            else
            {
                go = canvas.gameObject;
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = go.GetComponent<CanvasScaler>() ?? go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject EnsureBackground(GameObject canvas)
        {
            var bg = FindOrCreateChild(canvas.transform, "SceneBackground");
            var rect = EnsureRect(bg);
            StretchToParent(rect);
            var img = bg.GetComponent<Image>() ?? bg.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = LoadSprite(ArtBackground);
            img.color = Color.white;
            img.preserveAspect = false;
            img.type = Image.Type.Simple;

            // Dim overlay above the background for text readability.
            var overlay = FindOrCreateChild(bg.transform, "DimOverlay");
            var overlayRect = EnsureRect(overlay);
            StretchToParent(overlayRect);
            var overlayImg = overlay.GetComponent<Image>() ?? overlay.AddComponent<Image>();
            overlayImg.raycastTarget = false;
            overlayImg.color = OverlayTint;
            overlayImg.sprite = null;
            return bg;
        }

        private static GameObject EnsureHeader(GameObject canvas)
        {
            var header = FindOrCreateChild(canvas.transform, "Header");
            var rect = EnsureRect(header);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(-160f, 96f);

            var title = EnsureTextChild(header, "TitleText", "Run Overview", 44f, HeaderText, FontStyles.Bold);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            titleRect.sizeDelta = new Vector2(0f, 56f);
            title.alignment = TextAlignmentOptions.Center;

            var sub = EnsureTextChild(header, "SubtitleText", "Plan your route", 22f, FrameTint, FontStyles.Italic);
            var subRect = sub.rectTransform;
            subRect.anchorMin = new Vector2(0f, 1f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, -56f);
            subRect.sizeDelta = new Vector2(0f, 32f);
            sub.alignment = TextAlignmentOptions.Center;
            return header;
        }

        private static GameObject EnsureMainLayout(GameObject canvas)
        {
            var main = FindOrCreateChild(canvas.transform, "MainLayout");
            var rect = EnsureRect(main);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(40f, 100f);
            rect.offsetMax = new Vector2(-40f, -140f);

            var hg = main.GetComponent<HorizontalLayoutGroup>() ?? main.AddComponent<HorizontalLayoutGroup>();
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.spacing = 24f;
            hg.padding = new RectOffset(0, 0, 0, 0);
            hg.childControlWidth = true;
            hg.childControlHeight = true;
            hg.childForceExpandWidth = true;
            hg.childForceExpandHeight = true;
            return main;
        }

        private static GameObject EnsurePanel(GameObject parent, string name, float flexibleWidth)
        {
            var go = FindOrCreateChild(parent.transform, name);
            EnsureRect(go);
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = LoadSprite(ArtParchment);
            img.type = Image.Type.Sliced;
            img.color = ParchmentTint;
            img.raycastTarget = true;

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = flexibleWidth;
            le.flexibleHeight = 1f;
            le.minHeight = 200f;

            var vg = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
            vg.padding = new RectOffset(28, 28, 28, 28);
            vg.spacing = 14f;
            vg.childAlignment = TextAnchor.UpperCenter;
            vg.childControlWidth = true;
            vg.childControlHeight = false;
            vg.childForceExpandWidth = true;
            vg.childForceExpandHeight = false;

            return go;
        }

        private static GameObject EnsureLeftPanel(GameObject parent,
            out TMP_Text goldText, out Button shopButton, out Button itemButton, out Button moveButton)
        {
            var panel = EnsurePanel(parent, "LeftPanel", 1f);
            EnsureTextChild(panel, "PanelTitle", "Run Hub", 28f, BodyText, FontStyles.Bold).alignment = TextAlignmentOptions.Center;

            var goldRow = FindOrCreateChild(panel.transform, "GoldRow");
            var rowRect = EnsureRect(goldRow);
            var rowLayout = goldRow.GetComponent<HorizontalLayoutGroup>() ?? goldRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            var rowLe = goldRow.GetComponent<LayoutElement>() ?? goldRow.AddComponent<LayoutElement>();
            rowLe.minHeight = 56f;

            var goldIcon = FindOrCreateChild(goldRow.transform, "GoldIcon");
            var goldIconImg = goldIcon.GetComponent<Image>() ?? goldIcon.AddComponent<Image>();
            goldIconImg.sprite = LoadSprite(ArtGold);
            goldIconImg.preserveAspect = true;
            goldIconImg.raycastTarget = false;
            var goldIconLe = goldIcon.GetComponent<LayoutElement>() ?? goldIcon.AddComponent<LayoutElement>();
            goldIconLe.preferredWidth = 48f;
            goldIconLe.preferredHeight = 48f;

            goldText = EnsureTextChild(goldRow, "GoldText", "Gold: 0", 28f, GoldText, FontStyles.Bold);
            goldText.alignment = TextAlignmentOptions.MidlineLeft;

            shopButton = EnsureFantasyButton(panel, "ShopButton", "Shop");
            itemButton = EnsureFantasyButton(panel, "ItemManagementButton", "Manage Items");
            moveButton = EnsureFantasyButton(panel, "MoveManagementButton", "Manage Moves");

            return panel;
        }

        private static GameObject EnsureCenterPanel(GameObject parent,
            out Transform mapContent, out TMP_Text mapTitleText, out TMP_Text runVictoryText)
        {
            var panel = EnsurePanel(parent, "CenterPanel", 2.4f);
            mapTitleText = EnsureTextChild(panel, "PanelTitle", "Run Map", 30f, BodyText, FontStyles.Bold);
            mapTitleText.alignment = TextAlignmentOptions.Center;

            var mapFrame = FindOrCreateChild(panel.transform, "MapFrame");
            EnsureRect(mapFrame);
            var frameImg = mapFrame.GetComponent<Image>() ?? mapFrame.AddComponent<Image>();
            frameImg.sprite = LoadSprite(ArtFrame);
            frameImg.type = Image.Type.Sliced;
            frameImg.color = FrameTint;
            frameImg.raycastTarget = false;
            var frameLe = mapFrame.GetComponent<LayoutElement>() ?? mapFrame.AddComponent<LayoutElement>();
            frameLe.flexibleHeight = 1f;
            frameLe.minHeight = 480f;

            // Inside the frame we host a ScrollRect with a scrolling Content RectTransform.
            var scroll = FindOrCreateChild(mapFrame.transform, "MapScrollView");
            var scrollRectComp = scroll.GetComponent<ScrollRect>() ?? scroll.AddComponent<ScrollRect>();
            var scrollRectTr = EnsureRect(scroll);
            scrollRectTr.anchorMin = Vector2.zero;
            scrollRectTr.anchorMax = Vector2.one;
            scrollRectTr.offsetMin = new Vector2(20f, 60f);
            scrollRectTr.offsetMax = new Vector2(-20f, -20f);
            var scrollImg = scroll.GetComponent<Image>() ?? scroll.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.18f);
            scrollImg.raycastTarget = true;

            var viewport = FindOrCreateChild(scroll.transform, "Viewport");
            var viewportRect = EnsureRect(viewport);
            StretchToParent(viewportRect);
            var viewportImg = viewport.GetComponent<Image>() ?? viewport.AddComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImg.raycastTarget = true;
            var mask = viewport.GetComponent<Mask>() ?? viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = FindOrCreateChild(viewport.transform, "MapContent");
            var contentRect = EnsureRect(content);
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.localScale = Vector3.one;
            contentRect.sizeDelta = new Vector2(600f, 1200f);

            // The controller positions nodes by anchoredPosition, so MapContent
            // must not have any layout component pushing children around.
            var lg = content.GetComponent<LayoutGroup>();
            if (lg != null) Object.DestroyImmediate(lg);
            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf != null) Object.DestroyImmediate(csf);

            // Layered children: lines below, nodes above.
            var linesGo = FindOrCreateChild(content.transform, "LinesLayer");
            var linesRect = EnsureRect(linesGo);
            linesRect.anchorMin = new Vector2(0.5f, 1f);
            linesRect.anchorMax = new Vector2(0.5f, 1f);
            linesRect.pivot = new Vector2(0.5f, 1f);
            linesRect.anchoredPosition = Vector2.zero;
            linesRect.sizeDelta = Vector2.zero;
            linesRect.localScale = Vector3.one;
            var linesGraphic = linesGo.GetComponent<Graphic>();
            if (linesGraphic != null) linesGraphic.raycastTarget = false;

            var nodesGo = FindOrCreateChild(content.transform, "NodesLayer");
            var nodesRect = EnsureRect(nodesGo);
            nodesRect.anchorMin = new Vector2(0.5f, 1f);
            nodesRect.anchorMax = new Vector2(0.5f, 1f);
            nodesRect.pivot = new Vector2(0.5f, 1f);
            nodesRect.anchoredPosition = Vector2.zero;
            nodesRect.sizeDelta = Vector2.zero;
            nodesRect.localScale = Vector3.one;

            linesGo.transform.SetSiblingIndex(0);
            nodesGo.transform.SetSiblingIndex(1);

            scrollRectComp.viewport = viewportRect;
            scrollRectComp.content = contentRect;
            scrollRectComp.horizontal = false;
            scrollRectComp.vertical = true;
            scrollRectComp.movementType = ScrollRect.MovementType.Clamped;

            // Run victory banner inside the frame, hidden by default.
            runVictoryText = EnsureTextChild(mapFrame, "RunVictoryText", "Run complete!", 32f, GoldText, FontStyles.Bold);
            var rvRect = runVictoryText.rectTransform;
            rvRect.anchorMin = new Vector2(0f, 1f);
            rvRect.anchorMax = new Vector2(1f, 1f);
            rvRect.pivot = new Vector2(0.5f, 1f);
            rvRect.anchoredPosition = new Vector2(0f, -16f);
            rvRect.sizeDelta = new Vector2(0f, 40f);
            runVictoryText.alignment = TextAlignmentOptions.Center;
            runVictoryText.gameObject.SetActive(false);

            mapContent = content.transform;
            return panel;
        }

        private static GameObject EnsureRightPanel(GameObject parent,
            out TMP_Text heroText, out TMP_Text equippedMovesText,
            out TMP_Text equippedItemsText, out TMP_Text inventoryText)
        {
            var panel = EnsurePanel(parent, "RightPanel", 1.1f);
            EnsureTextChild(panel, "PanelTitle", "Hero", 28f, BodyText, FontStyles.Bold).alignment = TextAlignmentOptions.Center;

            heroText = EnsureTextChild(panel, "HeroSummaryText", "...", 20f, BodyText, FontStyles.Normal);
            heroText.alignment = TextAlignmentOptions.TopLeft;
            var heroLe = heroText.gameObject.GetComponent<LayoutElement>() ?? heroText.gameObject.AddComponent<LayoutElement>();
            heroLe.minHeight = 90f;

            equippedMovesText = EnsureTextChild(panel, "EquippedMovesText", "...", 18f, BodyText, FontStyles.Normal);
            equippedMovesText.alignment = TextAlignmentOptions.TopLeft;
            var movesLe = equippedMovesText.gameObject.GetComponent<LayoutElement>() ?? equippedMovesText.gameObject.AddComponent<LayoutElement>();
            movesLe.minHeight = 110f;

            equippedItemsText = EnsureTextChild(panel, "EquippedItemsText", "...", 18f, BodyText, FontStyles.Normal);
            equippedItemsText.alignment = TextAlignmentOptions.TopLeft;
            var itemsLe = equippedItemsText.gameObject.GetComponent<LayoutElement>() ?? equippedItemsText.gameObject.AddComponent<LayoutElement>();
            itemsLe.minHeight = 90f;

            inventoryText = EnsureTextChild(panel, "InventoryText", "...", 18f, SubtleText, FontStyles.Normal);
            inventoryText.alignment = TextAlignmentOptions.TopLeft;
            var invLe = inventoryText.gameObject.GetComponent<LayoutElement>() ?? inventoryText.gameObject.AddComponent<LayoutElement>();
            invLe.minHeight = 100f;

            return panel;
        }

        private static GameObject EnsureFooter(GameObject canvas, out Button saveBtn, out TMP_Text saveLbl, out TMP_Text statusText)
        {
            var footer = FindOrCreateChild(canvas.transform, "Footer");
            var rect = EnsureRect(footer);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(-160f, 64f);

            var hg = footer.GetComponent<HorizontalLayoutGroup>() ?? footer.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = 16f;
            hg.padding = new RectOffset(0, 0, 0, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childControlWidth = false;
            hg.childControlHeight = true;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;

            saveBtn = EnsureFantasyButton(footer, "SaveAndExitButton", "Save & Exit");
            saveLbl = saveBtn.GetComponentInChildren<TMP_Text>(true);

            statusText = EnsureTextChild(footer, "StatusText", string.Empty, 22f, FrameTint, FontStyles.Italic);
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            var statusLe = statusText.gameObject.GetComponent<LayoutElement>() ?? statusText.gameObject.AddComponent<LayoutElement>();
            statusLe.preferredWidth = 480f;

            return footer;
        }

        private struct EndlessRefs
        {
            public GameObject root;
            public TMP_Text title;
            public TMP_Text floor;
            public TMP_Text next;
            public TMP_Text shop;
            public TMP_Text best;
            public TMP_Text defeat;
            public Button startBtn;
            public TMP_Text startLbl;
            public Button menuBtn;
        }

        private static EndlessRefs EnsureEndlessPanel(GameObject canvas)
        {
            var panel = FindOrCreateChild(canvas.transform, "EndlessPanel");
            var rect = EnsureRect(panel);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(720f, 540f);

            var img = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
            img.sprite = LoadSprite(ArtParchment);
            img.type = Image.Type.Sliced;
            img.color = ParchmentTint;

            var vg = panel.GetComponent<VerticalLayoutGroup>() ?? panel.AddComponent<VerticalLayoutGroup>();
            vg.padding = new RectOffset(36, 36, 36, 36);
            vg.spacing = 14f;
            vg.childAlignment = TextAnchor.UpperCenter;
            vg.childControlWidth = true;
            vg.childControlHeight = false;

            var refs = new EndlessRefs();
            refs.root = panel;
            refs.title = EnsureTextChild(panel, "EndlessTitleText", "Endless Mode", 32f, BodyText, FontStyles.Bold);
            refs.title.alignment = TextAlignmentOptions.Center;
            refs.floor = EnsureTextChild(panel, "EndlessFloorText", "Floor 1", 26f, BodyText, FontStyles.Bold);
            refs.floor.alignment = TextAlignmentOptions.Center;
            refs.next = EnsureTextChild(panel, "EndlessNextEnemyText", "...", 22f, BodyText, FontStyles.Normal);
            refs.next.alignment = TextAlignmentOptions.Center;
            refs.shop = EnsureTextChild(panel, "EndlessShopText", string.Empty, 20f, SubtleText, FontStyles.Italic);
            refs.shop.alignment = TextAlignmentOptions.Center;
            refs.best = EnsureTextChild(panel, "EndlessBestFloorText", string.Empty, 18f, SubtleText, FontStyles.Italic);
            refs.best.alignment = TextAlignmentOptions.Center;
            refs.defeat = EnsureTextChild(panel, "EndlessDefeatText", string.Empty, 22f, BodyText, FontStyles.Bold);
            refs.defeat.alignment = TextAlignmentOptions.Center;

            refs.startBtn = EnsureFantasyButton(panel, "EndlessStartFloorButton", "Start Floor");
            refs.startLbl = refs.startBtn.GetComponentInChildren<TMP_Text>(true);

            refs.menuBtn = EnsureFantasyButton(panel, "EndlessReturnToMenuButton", "Return to Menu");

            panel.SetActive(false);
            return refs;
        }

        private static GameObject EnsureHidden(GameObject canvas)
        {
            var hidden = FindOrCreateChild(canvas.transform, "_HiddenLegacy");
            EnsureRect(hidden);
            hidden.SetActive(false);
            return hidden;
        }

        // ------------------------------------------------------------------
        //  Controller binding
        // ------------------------------------------------------------------

        private static RunOverviewController FindOrCreateRunOverviewController(GameObject canvas)
        {
            var existing = Object.FindObjectOfType<RunOverviewController>();
            if (existing != null) return existing;
            var go = new GameObject("RunOverviewController");
            go.transform.SetParent(canvas.transform.parent, false);
            return go.AddComponent<RunOverviewController>();
        }

        private static void BindController(RunOverviewController controller,
            TMP_Text heroText, TMP_Text equippedMovesText, Button moveManagementButton,
            TMP_Text equippedItemsText, TMP_Text inventoryText, Button itemManagementButton,
            TMP_Text goldText, Button shopButton,
            Transform mapContainer, TMP_Text mapTitleText, TMP_Text runVictoryText,
            GameObject endlessPanel, TMP_Text endlessTitleText, TMP_Text endlessFloorText,
            TMP_Text endlessNextEnemyText, TMP_Text endlessShopText, TMP_Text endlessBestFloorText,
            TMP_Text endlessDefeatText, Button endlessStartFloorButton, TMP_Text endlessStartFloorButtonLabel,
            Button endlessReturnToMenuButton, GameObject hidden)
        {
            var so = new SerializedObject(controller);

            BindField(so, "heroText", heroText);
            BindField(so, "equippedMovesText", equippedMovesText);
            BindField(so, "moveManagementButton", moveManagementButton);
            BindField(so, "equippedItemsText", equippedItemsText);
            BindField(so, "inventoryText", inventoryText);
            BindField(so, "itemManagementButton", itemManagementButton);
            BindField(so, "goldText", goldText);
            BindField(so, "shopButton", shopButton);

            BindField(so, "mapContainer", mapContainer);
            BindField(so, "mapTitleText", mapTitleText);
            BindField(so, "runVictoryText", runVictoryText);

            // Map node prefab: only assign if the prefab asset is on disk.
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MapNodePrefabPath);
            if (prefabAsset != null)
            {
                var view = prefabAsset.GetComponent<RunMapNodeView>();
                if (view != null) BindField(so, "mapNodePrefab", view);
            }
            else
            {
                Debug.LogWarning($"[RunOverview Polish] {MapNodePrefabPath} not found. Run 'Rebuild Run Map Node Prefab' first.");
            }

            BindField(so, "endlessPanel", endlessPanel);
            BindField(so, "endlessTitleText", endlessTitleText);
            BindField(so, "endlessFloorText", endlessFloorText);
            BindField(so, "endlessNextEnemyText", endlessNextEnemyText);
            BindField(so, "endlessShopText", endlessShopText);
            BindField(so, "endlessBestFloorText", endlessBestFloorText);
            BindField(so, "endlessDefeatText", endlessDefeatText);
            BindField(so, "endlessStartFloorButton", endlessStartFloorButton);
            BindField(so, "endlessStartFloorButtonLabel", endlessStartFloorButtonLabel);
            BindField(so, "endlessReturnToMenuButton", endlessReturnToMenuButton);

            // Legacy fields (encountersContainer, encounterButtonPrefab, mapLinePrefab) are left
            // alone if the user already wired them. The current controller's branching path uses
            // mapContainer + mapNodePrefab, so the linear list is just a fallback.

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
        }

        private static void BindSaveAndExit(GameObject canvas, Button button, TMP_Text label, TMP_Text status)
        {
            var existing = Object.FindObjectOfType<SaveAndExitButton>();
            if (existing == null)
            {
                var go = new GameObject("SaveAndExitController");
                go.transform.SetParent(canvas.transform.parent, false);
                existing = go.AddComponent<SaveAndExitButton>();
            }

            var so = new SerializedObject(existing);
            BindField(so, "button", button);
            BindField(so, "buttonLabel", label);
            BindField(so, "statusText", status);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
        }

        // ------------------------------------------------------------------
        //  Small helpers
        // ------------------------------------------------------------------

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == name) return parent.GetChild(i).gameObject;
            }
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateChildImage(Transform parent, string name, string spritePath, Color color, bool fill)
        {
            var go = FindOrCreateChild(parent, name);
            var rect = EnsureRect(go);
            if (fill) StretchToParent(rect);
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = LoadSprite(spritePath);
            img.color = color;
            img.preserveAspect = !fill;
            return go;
        }

        private static TMP_Text EnsureTextChild(GameObject parent, string name, string text, float size, Color color, FontStyles style)
        {
            var go = FindOrCreateChild(parent.transform, name);
            EnsureRect(go);
            var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button EnsureFantasyButton(GameObject parent, string name, string labelText)
        {
            var go = FindOrCreateChild(parent.transform, name);
            EnsureRect(go);

            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = LoadSprite(ArtButton);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(1f, 0.95f, 0.7f, 1f);
            colors.pressedColor = new Color(0.85f, 0.75f, 0.55f, 1f);
            colors.disabledColor = new Color(0.55f, 0.5f, 0.4f, 0.7f);
            btn.colors = colors;

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = 60f;
            le.preferredWidth = 240f;

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                StretchToParent(labelGo.GetComponent<RectTransform>());
                label = labelGo.GetComponent<TextMeshProUGUI>();
            }
            label.text = labelText;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.16f, 0.10f, 0.06f, 1f);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            return btn;
        }

        private static RectTransform EnsureRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[RunOverview Polish] Sprite not found at {path}. Skipping.");
            }
            return sprite;
        }

        private static void BindField(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[RunOverview Polish] Field '{fieldName}' not found on {so.targetObject.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
        }
    }
}
#endif
