using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the selection-screen prefabs from the shared MenuButton / MenuLabel prefabs (Tools > UI > Rebuild Selection Screen):
//   GemCounter (wallet balance label, also on the main menu), UnlockTile (grid tile), SelectionScreen (Characters).
// Also adds the Characters button and the gem counter to the MainMenu prefab, and wires the Core scene's MainMenu
// to a SelectionScreen instance (Tools > UI > Wire Selection Screen In Core). Idempotent: rebuilding replaces the
// prefab files in place, so scene instances keep their links.
public static class SelectionScreenBuilder
{
    private const string UiFolder = "Assets/Prefabs/UI/";
    private const string LabelPath = UiFolder + "MenuLabel.prefab";
    private const string ButtonPath = UiFolder + "MenuButton.prefab";
    private const string GemCounterPath = UiFolder + "GemCounter.prefab";
    private const string TilePath = UiFolder + "UnlockTile.prefab";
    private const string ScreenPath = UiFolder + "SelectionScreen.prefab";
    private const string CompanionScreenPath = UiFolder + "CompanionSelection.prefab";
    private const string MainMenuPath = UiFolder + "MainMenu.prefab";
    private const string GameOverPath = UiFolder + "GameOverMenu.prefab";

    private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.13f, 1f);
    private static readonly Color FrameColor = new Color(1f, 0.85f, 0.2f, 1f);

    [MenuItem("Tools/UI/Rebuild Selection Screen")]
    public static void RebuildAll()
    {
        var gemCounter = BuildGemCounter();
        var tile = BuildTile();
        BuildScreen(gemCounter, tile, UnlockCategory.Character, ScreenPath, "SelectionScreen", "Characters");
        BuildScreen(gemCounter, tile, UnlockCategory.Companion, CompanionScreenPath, "CompanionScreen", "Companions");
        UpdateMainMenuPrefab(gemCounter);
        UpdateGameOverPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("Rebuilt selection screen prefabs and the main menu additions.");
    }

    [MenuItem("Tools/UI/Wire Selection Screen In Core")]
    public static void WireCore()
    {
        var mainMenu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (mainMenu == null)
        {
            Debug.LogError("Open the Core scene first: no MainMenu found.");
            return;
        }

        var scene = mainMenu.gameObject.scene;
        var fields = new SerializedObject(mainMenu);
        fields.FindProperty("selectionScreen").objectReferenceValue = EnsureScreenInstance(mainMenu, ScreenPath, "SelectionScreen");
        fields.FindProperty("companionScreen").objectReferenceValue = EnsureScreenInstance(mainMenu, CompanionScreenPath, "CompanionScreen");
        fields.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Wired the selection screens into " + scene.name);
    }

    // One inactive, full-screen instance per screen prefab, next to the main menu.
    private static SelectionScreen EnsureScreenInstance(MainMenu mainMenu, string prefabPath, string instanceName)
    {
        var existing = mainMenu.transform.parent.Find(instanceName);
        if (existing != null)
        {
            return existing.GetComponent<SelectionScreen>();
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mainMenu.transform.parent);
        instance.name = instanceName;
        instance.SetActive(false);
        var rect = (RectTransform)instance.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return instance.GetComponent<SelectionScreen>();
    }

    private static GameObject BuildGemCounter()
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LabelPath));
        root.name = "GemCounter";
        root.AddComponent<GemCounter>();
        var text = root.GetComponent<TextMeshProUGUI>();
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Right;
        return Save(root, GemCounterPath);
    }

    private static UnlockTile BuildTile()
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath));
        root.name = "UnlockTile";
        ((RectTransform)root.transform).sizeDelta = new Vector2(180f, 180f);

        // Frame first so it draws behind the swatch and shows as a border on the selected item.
        var frame = NewImage(root.transform, "SelectedFrame", FrameColor, 0.1f, 0.3f, 0.9f, 0.9f);
        var swatch = NewImage(root.transform, "Swatch", Color.white, 0.14f, 0.34f, 0.86f, 0.86f);

        var badge = NewLabel(root.transform, "Badge", 22f, 0f, 0.86f, 1f, 1f);
        badge.color = FrameColor;
        var name = root.GetComponentInChildren<TextMeshProUGUI>();
        var nameRect = (RectTransform)name.transform;
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0.3f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        name.enableAutoSizing = true;
        name.fontSizeMin = 14f;
        name.fontSizeMax = 30f;

        var tile = root.AddComponent<UnlockTile>();
        var fields = new SerializedObject(tile);
        fields.FindProperty("button").objectReferenceValue = root.GetComponent<Button>();
        fields.FindProperty("swatch").objectReferenceValue = swatch;
        fields.FindProperty("nameText").objectReferenceValue = name;
        fields.FindProperty("badgeText").objectReferenceValue = badge;
        fields.FindProperty("selectedFrame").objectReferenceValue = frame.gameObject;
        fields.ApplyModifiedPropertiesWithoutUndo();

        var saved = Save(root, TilePath);
        return saved.GetComponent<UnlockTile>();
    }

    private static void BuildScreen(GameObject gemCounterPrefab, UnlockTile tilePrefab, UnlockCategory category, string path, string rootName, string titleText)
    {
        var root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.layer = LayerMask.NameToLayer("UI");
        Stretch((RectTransform)root.transform);
        root.GetComponent<Image>().color = PanelColor;
        var screen = root.AddComponent<SelectionScreen>();
        var layout = root.AddComponent<SelectionLayout>();

        // Top bar: Back (left), title (centre), gem counter (right).
        var topBar = NewRect("TopBar", root.transform);
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 100f);
        topBar.anchoredPosition = Vector2.zero;
        var back = AddButton(topBar, "Back", "Back");
        Place(back.transform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(200f, 60f));
        var title = AddLabel(topBar, "Title", titleText, 56f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 28f;
        title.fontSizeMax = 56f;
        // Fills the gap between Back and the gem counter, so it never overlaps them on a narrow (portrait) screen.
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.offsetMin = new Vector2(240f, -40f);
        titleRect.offsetMax = new Vector2(-320f, 40f);
        var gems = (GameObject)PrefabUtility.InstantiatePrefab(gemCounterPrefab, topBar);
        gems.name = "GemCounter";
        Place(gems.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(280f, 60f));

        // Info panel: preview, name, tier, status, actions. Stacked; SelectionLayout decides where the panel sits.
        var info = NewRect("InfoPanel", root.transform);
        var stack = info.gameObject.AddComponent<VerticalLayoutGroup>();
        stack.spacing = 8f;
        stack.padding = new RectOffset(20, 20, 0, 0);
        stack.childAlignment = TextAnchor.UpperCenter;
        stack.childControlWidth = true;
        stack.childControlHeight = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;

        var previewFrame = NewRect("PreviewFrame", info);
        SetLayout(previewFrame.gameObject, minHeight: 160f, preferredHeight: 320f, flexibleHeight: 1f);
        var previewRect = NewRect("Preview", previewFrame);
        Stretch(previewRect);
        var preview = previewRect.gameObject.AddComponent<RawImage>();
        preview.raycastTarget = false;
        var fitter = previewRect.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1f;

        var nameText = AddLabel(info, "Name", "Name", 48f);
        SetLayout(nameText.gameObject, minHeight: 50f, preferredHeight: 50f);
        var tierText = AddLabel(info, "Tier", "Tier", 32f);
        SetLayout(tierText.gameObject, minHeight: 36f, preferredHeight: 36f);
        var statusText = AddLabel(info, "Status", "Status", 32f);
        SetLayout(statusText.gameObject, minHeight: 36f, preferredHeight: 36f);
        var select = AddButton(info, "Select", "Select");
        var unlock = AddButton(info, "Unlock", "Unlock");
        var buy = AddButton(info, "Buy", "Buy");
        var tryAd = AddButton(info, "Try", "Try: watch ad");
        var restore = AddButton(info, "Restore", "Restore Purchases");
        foreach (var button in new[] { select, unlock, buy, tryAd, restore })
        {
            SetLayout(button, minHeight: 60f, preferredHeight: 60f);
        }

        // Grid panel: vertical scroll of tiles.
        var gridPanel = NewRect("GridPanel", root.transform);
        var scrollImage = gridPanel.gameObject.AddComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0.05f);
        var scroll = gridPanel.gameObject.AddComponent<ScrollRect>();
        var viewport = NewRect("Viewport", gridPanel);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(180f, 180f);
        grid.spacing = new Vector2(24f, 24f);
        grid.padding = new RectOffset(24, 24, 24, 24);
        grid.childAlignment = TextAnchor.UpperCenter;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        UnityEventTools.AddPersistentListener(back.GetComponent<Button>().onClick, screen.Back);
        UnityEventTools.AddPersistentListener(select.GetComponent<Button>().onClick, screen.Select);
        UnityEventTools.AddPersistentListener(unlock.GetComponent<Button>().onClick, screen.Unlock);
        UnityEventTools.AddPersistentListener(buy.GetComponent<Button>().onClick, screen.Buy);
        UnityEventTools.AddPersistentListener(tryAd.GetComponent<Button>().onClick, screen.Try);
        UnityEventTools.AddPersistentListener(restore.GetComponent<Button>().onClick, screen.Restore);

        var fields = new SerializedObject(screen);
        fields.FindProperty("category").enumValueIndex = (int)category;
        fields.FindProperty("titleText").objectReferenceValue = title;
        fields.FindProperty("nameText").objectReferenceValue = nameText;
        fields.FindProperty("tierText").objectReferenceValue = tierText;
        fields.FindProperty("statusText").objectReferenceValue = statusText;
        fields.FindProperty("previewImage").objectReferenceValue = preview;
        fields.FindProperty("selectButton").objectReferenceValue = select.GetComponent<Button>();
        fields.FindProperty("selectLabel").objectReferenceValue = select.GetComponentInChildren<TextMeshProUGUI>();
        fields.FindProperty("unlockButton").objectReferenceValue = unlock.GetComponent<Button>();
        fields.FindProperty("unlockLabel").objectReferenceValue = unlock.GetComponentInChildren<TextMeshProUGUI>();
        fields.FindProperty("buyButton").objectReferenceValue = buy.GetComponent<Button>();
        fields.FindProperty("buyLabel").objectReferenceValue = buy.GetComponentInChildren<TextMeshProUGUI>();
        fields.FindProperty("tryButton").objectReferenceValue = tryAd.GetComponent<Button>();
        fields.FindProperty("tryLabel").objectReferenceValue = tryAd.GetComponentInChildren<TextMeshProUGUI>();
        fields.FindProperty("restoreButton").objectReferenceValue = restore.GetComponent<Button>();
        fields.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
        fields.FindProperty("tileContainer").objectReferenceValue = content;
        fields.FindProperty("scroll").objectReferenceValue = scroll;
        fields.ApplyModifiedPropertiesWithoutUndo();

        var layoutFields = new SerializedObject(layout);
        layoutFields.FindProperty("infoPanel").objectReferenceValue = info;
        layoutFields.FindProperty("gridPanel").objectReferenceValue = gridPanel;
        layoutFields.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        Save(root, path);
    }

    // The main menu keeps its own prefab: add a Characters button (between Play and Exit) and the gem counter.
    private static void UpdateMainMenuPrefab(GameObject gemCounterPrefab)
    {
        var contents = PrefabUtility.LoadPrefabContents(MainMenuPath);
        try
        {
            var menu = contents.GetComponent<MainMenu>();
            var characters = contents.transform.Find("Characters");
            if (characters == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), contents.transform);
                instance.name = "Characters";
                instance.GetComponentInChildren<TextMeshProUGUI>().text = "Characters";
                UnityEventTools.AddPersistentListener(instance.GetComponent<Button>().onClick, menu.OpenCharacters);
                characters = instance.transform;
            }
            var companions = contents.transform.Find("Companions");
            if (companions == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), contents.transform);
                instance.name = "Companions";
                instance.GetComponentInChildren<TextMeshProUGUI>().text = "Companions";
                UnityEventTools.AddPersistentListener(instance.GetComponent<Button>().onClick, menu.OpenCompanions);
                companions = instance.transform;
            }
            // Vertical budget: the menu must stay inside the shortest canvas (about 864 units tall, i.e. +-432).
            SetRect(characters, new Vector2(0f, -75f), new Vector2(340f, 60f));
            SetRect(companions, new Vector2(0f, -150f), new Vector2(340f, 60f));
            SetRect(contents.transform.Find("Exit"), new Vector2(0f, -225f), new Vector2(280f, 60f));
            SetRect(contents.transform.Find("ClearHighScore"), new Vector2(0f, -300f), new Vector2(340f, 50f));

            var removeAds = contents.transform.Find("RemoveAds");
            if (removeAds == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), contents.transform);
                instance.name = "RemoveAds";
                instance.GetComponentInChildren<TextMeshProUGUI>().text = "Remove Ads";
                UnityEventTools.AddPersistentListener(instance.GetComponent<Button>().onClick, menu.RemoveAds);
                removeAds = instance.transform;
            }
            SetRect(removeAds, new Vector2(0f, -365f), new Vector2(400f, 60f));
            var menuFields = new SerializedObject(menu);
            menuFields.FindProperty("removeAdsButton").objectReferenceValue = removeAds.gameObject;
            menuFields.ApplyModifiedPropertiesWithoutUndo();

            if (contents.transform.Find("GemCounter") == null)
            {
                var gems = (GameObject)PrefabUtility.InstantiatePrefab(gemCounterPrefab, contents.transform);
                gems.name = "GemCounter";
                Place(gems.transform, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(320f, 60f));
            }

            PrefabUtility.SaveAsPrefabAsset(contents, MainMenuPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // The game-over screen gets a centred modal for the trial-over prompt (built once, then only repositioned).
    private static void UpdateGameOverPrefab()
    {
        var contents = PrefabUtility.LoadPrefabContents(GameOverPath);
        try
        {
            var menu = contents.GetComponent<GameOverMenu>();
            var existing = contents.transform.Find("TrialPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var panel = new GameObject("TrialPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.layer = LayerMask.NameToLayer("UI");
            panel.transform.SetParent(contents.transform, false);
            Stretch((RectTransform)panel.transform);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

            var title = AddLabel(panel.transform, "Title", "Trial over", 48f);
            Place(title.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1100f, 80f));
            var buy = AddButton(panel.transform, "Buy", "Buy");
            var unlock = AddButton(panel.transform, "Unlock", "Unlock");
            var ad = AddButton(panel.transform, "WatchAd", "Watch ad");
            var dismiss = AddButton(panel.transform, "NoThanks", "No thanks");
            var y = 50f;
            foreach (var button in new[] { buy, unlock, ad, dismiss })
            {
                Place(button.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(640f, 60f));
                y -= 80f;
            }

            UnityEventTools.AddPersistentListener(buy.GetComponent<Button>().onClick, menu.TrialBuy);
            UnityEventTools.AddPersistentListener(unlock.GetComponent<Button>().onClick, menu.TrialUnlock);
            UnityEventTools.AddPersistentListener(ad.GetComponent<Button>().onClick, menu.TrialWatchAd);
            UnityEventTools.AddPersistentListener(dismiss.GetComponent<Button>().onClick, menu.TrialDismiss);

            var fields = new SerializedObject(menu);
            fields.FindProperty("trialPanel").objectReferenceValue = panel;
            fields.FindProperty("trialTitle").objectReferenceValue = title;
            fields.FindProperty("trialBuyButton").objectReferenceValue = buy.GetComponent<Button>();
            fields.FindProperty("trialUnlockButton").objectReferenceValue = unlock.GetComponent<Button>();
            fields.FindProperty("trialAdButton").objectReferenceValue = ad.GetComponent<Button>();
            fields.FindProperty("trialDismissButton").objectReferenceValue = dismiss.GetComponent<Button>();
            fields.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(contents, GameOverPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static GameObject Save(GameObject root, string path)
    {
        var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(Transform parent, string name, Color color, float minX, float minY, float maxX, float maxY)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI NewLabel(Transform parent, string name, float size, float minX, float minY, float maxX, float maxY)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LabelPath), parent);
        instance.name = name;
        var rect = (RectTransform)instance.transform;
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var text = instance.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static TextMeshProUGUI AddLabel(Transform parent, string name, string text, float size)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LabelPath), parent);
        instance.name = name;
        var label = instance.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private static GameObject AddButton(Transform parent, string name, string text)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), parent);
        instance.name = name;
        instance.GetComponentInChildren<TextMeshProUGUI>().text = text;
        return instance;
    }

    private static void SetLayout(GameObject go, float minHeight = -1f, float preferredHeight = -1f, float flexibleHeight = -1f)
    {
        var element = go.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = go.AddComponent<LayoutElement>();
        }
        element.minHeight = minHeight;
        element.preferredHeight = preferredHeight;
        element.flexibleHeight = flexibleHeight;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Place(Transform transform, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetRect(Transform transform, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
