using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Selection-screen checks for the play smoke test (economy step 3): price table, buy/select mechanics against
// the save file, the real UI driven through its buttons (including a pixel readback of the preview), and a layout
// audit of the prefab at several real screen sizes, portrait and landscape.
public static partial class PlaySmokeTest
{
    private const string SelectionPrefabPath = "Assets/Prefabs/UI/SelectionScreen.prefab";
    private const float MinTouchPixels = 44f;
    private const float Tolerance = 0.75f;

    private static readonly Vector2Int[] LayoutScreens =
    {
        new Vector2Int(1920, 1080), new Vector2Int(1280, 720),
        new Vector2Int(2400, 1080), new Vector2Int(1080, 2400),
        new Vector2Int(1080, 1920), new Vector2Int(720, 1280),
        new Vector2Int(2048, 1536), new Vector2Int(1536, 2048),
    };

    private static IEnumerator SelectionChecks()
    {
        var catalog = UnlockCatalog.Instance;
        var characters = catalog != null ? catalog.InCategory(UnlockCategory.Character) : new List<UnlockableDefinition>();
        var free = catalog?.DefaultFor(UnlockCategory.Character);
        var common = characters.Find(item => item.Tier == PriceTier.Common);
        var rare = characters.Find(item => item.Tier == PriceTier.Rare);
        var ready = free != null && common != null && rare != null;
        Check("unlock catalog has a free, a Common and a Rare character", ready, $"characters={characters.Count}");
        if (!ready)
        {
            yield break;
        }

        // 1. Price table from the plan: PC gems, mobile = x2.5 rounded to the nearest 5.
        var expected = new[]
        {
            (PriceTier.Free, 0, 0), (PriceTier.Common, 600, 1500), (PriceTier.Rare, 1800, 4500),
            (PriceTier.Epic, 3600, 9000), (PriceTier.Legendary, 7200, 18000),
        };
        var badPrices = new List<string>();
        foreach (var (tier, pc, mobile) in expected)
        {
            var gotPc = UnlockService.PriceFor(tier, false);
            var gotMobile = UnlockService.PriceFor(tier, true);
            if (gotPc != pc || gotMobile != mobile)
            {
                badPrices.Add($"{tier}: {gotPc}/{gotMobile} want {pc}/{mobile}");
            }
        }
        Check("price table (PC and mobile x2.5)", badPrices.Count == 0, string.Join("; ", badPrices));

        // 2. Buy / select mechanics against the save.
        UseFreshTempSave();
        Check("default character is owned and selected, others locked",
            UnlockService.IsOwned(free) && UnlockService.Selected(UnlockCategory.Character) == free && !UnlockService.IsOwned(common) && !UnlockService.IsOwned(rare));

        var price = UnlockService.Price(common);
        var boughtBroke = UnlockService.TryBuyWithGems(common);
        Check("buying with too few gems changes nothing",
            !boughtBroke && Wallet.Balance == 0 && !UnlockService.IsOwned(common) && SaveService.Data.owned.Count == 0);

        Wallet.Add(price + 50);
        var bought = UnlockService.TryBuyWithGems(common);
        var source = SaveService.Data.owned.Count == 1 ? SaveService.Data.owned[0].source : "?";
        Check("buying spends the exact price and owns the item", bought && Wallet.Balance == 50 && UnlockService.IsOwned(common) && source == "gems",
            $"bought={bought} balance={Wallet.Balance} source={source}");

        var boughtAgain = UnlockService.TryBuyWithGems(common);
        Check("buying an owned item is refused and free", !boughtAgain && Wallet.Balance == 50 && SaveService.Data.owned.Count == 1);

        var selectedLocked = UnlockService.Select(rare);
        var selectedOwned = UnlockService.Select(common);
        Check("select: locked refused, owned accepted", !selectedLocked && selectedOwned && UnlockService.Selected(UnlockCategory.Character) == common);

        UnlockService.GrantPurchase(rare);
        UnlockService.GrantPurchase(rare);
        Check("granting a purchase is idempotent", UnlockService.IsOwned(rare) && SaveService.Data.owned.Count == 2 && Wallet.Balance == 50);

        SaveService.Load();
        Check("unlocks survive a save reload",
            Wallet.Balance == 50 && UnlockService.IsOwned(common) && UnlockService.IsOwned(rare) && UnlockService.Selected(UnlockCategory.Character) == common && SaveService.Data.owned.Count == 2);

        // 2b. Trials, pacing and purchases restore (pure logic).
        UseFreshTempSave();
        Check("trial cannot start on an owned item", !UnlockService.StartTrial(free));
        var startedTrial = UnlockService.StartTrial(rare);
        Check("trial makes a locked item usable and selected for N runs",
            startedTrial && !UnlockService.IsOwned(rare) && UnlockService.IsUsable(rare) && UnlockService.Selected(UnlockCategory.Character) == rare
            && UnlockService.TrialRunsLeft(rare) == EconomyConfig.Instance.TrialRuns);
        UnlockService.Select(free);
        UnlockService.EndRunForTrials();
        Check("a run with another item selected does not use up the trial", UnlockService.TrialRunsLeft(rare) == EconomyConfig.Instance.TrialRuns);
        UnlockService.Select(rare);
        var expiredNames = new List<string>();
        for (var run = 0; run < EconomyConfig.Instance.TrialRuns; run++)
        {
            foreach (var item in UnlockService.EndRunForTrials())
            {
                expiredNames.Add(item.Id);
            }
        }
        Check("trial ends after N runs and selection goes back to the owned item",
            expiredNames.Count == 1 && expiredNames[0] == rare.Id && !UnlockService.IsUsable(rare) && UnlockService.Selected(UnlockCategory.Character) == free,
            string.Join(",", expiredNames));

        UnlockService.Select(free);
        Wallet.Add(UnlockService.Price(common) + 10);
        UnlockService.TryBuyWithGems(common);
        UnlockService.Select(common);
        UnlockService.StartTrial(rare);
        UnlockService.StartTrial(rare);
        var trialCount = SaveService.Data.trials.Count;
        UnlockService.EndRunForTrials();
        UnlockService.EndRunForTrials();
        UnlockService.EndRunForTrials();
        Check("a new trial replaces the old one; the last owned item comes back", trialCount == 1 && UnlockService.Selected(UnlockCategory.Character) == common);

        UseFreshTempSave();
        var pacerAds = new FakeAdService();
        Services.Ads = pacerAds;
        var every = EconomyConfig.Instance.GameOversPerInterstitial;
        for (var i = 0; i < every - 1; i++)
        {
            InterstitialPacer.RegisterGameOver(false);
            InterstitialPacer.OnLeave(() => { });
        }
        Check($"no interstitial before game over {every}", pacerAds.InterstitialsShown == 0);
        InterstitialPacer.RegisterGameOver(false);
        var proceeded = false;
        InterstitialPacer.OnLeave(() => proceeded = true);
        Check($"interstitial on game over {every}, then the count restarts", pacerAds.InterstitialsShown == 1 && proceeded && SaveService.Data.gameOversSinceInterstitial == 0);
        InterstitialPacer.RegisterGameOver(false);
        InterstitialPacer.OnLeave(() => { });
        Check("no interstitial right after one", pacerAds.InterstitialsShown == 1);

        SaveService.Data.gameOversSinceInterstitial = every - 1;
        InterstitialPacer.RegisterGameOver(false);
        InterstitialPacer.MarkRewardedWatched();
        InterstitialPacer.OnLeave(() => { });
        var afterRewarded = pacerAds.InterstitialsShown;
        InterstitialPacer.RegisterGameOver(false);
        InterstitialPacer.OnLeave(() => { });
        Check("a rewarded ad on the same game over skips it; it stays due for the next", afterRewarded == 1 && pacerAds.InterstitialsShown == 2);

        SaveService.Data.gameOversSinceInterstitial = every - 1;
        InterstitialPacer.RegisterGameOver(true);
        InterstitialPacer.OnLeave(() => { });
        Check("no interstitial on the game over where a trial ended", pacerAds.InterstitialsShown == 2);

        UnlockService.GrantRemoveAds();
        SaveService.Data.gameOversSinceInterstitial = every - 1;
        InterstitialPacer.RegisterGameOver(false);
        InterstitialPacer.OnLeave(() => { });
        Check("Remove Ads stops interstitials", pacerAds.InterstitialsShown == 2);

        UseFreshTempSave();
        Services.Ads = new NullAdService();
        SaveService.Data.gameOversSinceInterstitial = every - 1;
        InterstitialPacer.RegisterGameOver(false);
        var leftOnPc = false;
        InterstitialPacer.OnLeave(() => leftOnPc = true);
        Check("no ad service (PC): leaving the game-over screen still proceeds", leftOnPc);
        Services.Ads = new FakeAdService();

        // 3. The real UI, through its real buttons.
        UseFreshTempSave();
        Wallet.Add(70);
        var mainMenu = CanvasChild("MainMenu");
        var screenTransform = CanvasChild("SelectionScreen");
        var screen = screenTransform != null ? screenTransform.GetComponent<SelectionScreen>() : null;
        var menuGems = CanvasChild("MainMenu/GemCounter");
        Check("main menu has Characters button, gem counter, and Core has the screen",
            CanvasChild("MainMenu/Characters") != null && menuGems != null && screen != null);
        if (screen == null || menuGems == null)
        {
            yield break;
        }
        Check("main menu gem counter shows the wallet", menuGems.GetComponent<TMP_Text>().text == "Gems: 70", menuGems.GetComponent<TMP_Text>().text);

        Click("MainMenu/Characters");
        yield return WaitUntil(() => screen.gameObject.activeInHierarchy);
        yield return 0.4f;
        var screenGems = screenTransform.Find("TopBar/GemCounter").GetComponent<TMP_Text>();
        Check("Characters opens the screen and hides the main menu",
            !timedOut && !mainMenu.gameObject.activeSelf && screen.Tiles.Count == characters.Count && screen.Focused != null && screen.Focused.Definition == free && screenGems.text == "Gems: 70",
            $"tiles={screen.Tiles.Count}/{characters.Count} gems='{screenGems.text}'");

        // The preview must show the focused item: centre differs from the background and follows the item's colour.
        var freeCentre = PreviewCentre(screenTransform, out var freeCorner);
        Check("preview renders the focused character (blue box)", Vector3.Distance(ToVector(freeCentre), ToVector(freeCorner)) > 0.15f && freeCentre.b > freeCentre.r,
            $"centre={freeCentre} corner={freeCorner}");

        var commonTile = screen.Tiles.First(tile => tile.Definition == common);
        screen.Focus(commonTile);
        yield return 0.5f;
        var commonCentre = PreviewCentre(screenTransform, out _);
        Check("preview follows focus (red box)", commonCentre.r > commonCentre.b, $"centre={commonCentre}");
        var stage = GameObject.Find("SelectionPreviewStage");
        var cube = stage != null ? stage.transform.Find("Preview_" + common.Id) : null;
        var rotator = screenTransform.Find("InfoPanel/PreviewFrame/Preview").GetComponent<PreviewRotator>();
        var before = cube != null ? cube.rotation : Quaternion.identity;
        rotator.OnDrag(new PointerEventData(EventSystem.current) { delta = new Vector2(120f, 0f) });
        Check("dragging the preview rotates it", cube != null && Quaternion.Angle(before, cube.rotation) > 1f);

        var unlockButton = screenTransform.Find("InfoPanel/Unlock").GetComponent<Button>();
        var selectButton = screenTransform.Find("InfoPanel/Select").GetComponent<Button>();
        var unlockLabel = unlockButton.GetComponentInChildren<TMP_Text>();
        var commonPrice = UnlockService.Price(common);
        Check("locked item, short of gems: Unlock disabled, says how many more",
            unlockButton.gameObject.activeSelf && !unlockButton.interactable && !selectButton.gameObject.activeSelf && unlockLabel.text == $"Need {commonPrice - 70} more gems",
            unlockLabel.text);

        Wallet.Add(commonPrice - 70);
        Check("locked item, enough gems: Unlock enabled with the price", unlockButton.interactable && unlockLabel.text == $"Unlock ({commonPrice} gems)", unlockLabel.text);

        unlockButton.onClick.Invoke();
        Check("clicking Unlock spends the price and owns the item",
            Wallet.Balance == 0 && UnlockService.IsOwned(common) && !unlockButton.gameObject.activeSelf && selectButton.gameObject.activeSelf && selectButton.interactable && screenGems.text == "Gems: 0",
            $"balance={Wallet.Balance} gems='{screenGems.text}'");

        selectButton.onClick.Invoke();
        var frames = 0;
        foreach (var tile in screen.Tiles)
        {
            if (tile.transform.Find("SelectedFrame").gameObject.activeSelf)
            {
                frames++;
                Check("selected frame is on the selected tile only", tile.Definition == common);
            }
        }
        Check("clicking Select selects it and shows Selected",
            UnlockService.Selected(UnlockCategory.Character) == common && !selectButton.interactable && selectButton.GetComponentInChildren<TMP_Text>().text == "Selected" && frames == 1,
            $"frames={frames}");

        // Step 4: ad / store buttons, driven by service availability.
        UseFreshTempSave();
        var rareTile = screen.Tiles.First(tile => tile.Definition == rare);
        var buyObject = screenTransform.Find("InfoPanel/Buy").gameObject;
        var tryObject = screenTransform.Find("InfoPanel/Try").gameObject;
        var restoreObject = screenTransform.Find("InfoPanel/Restore").gameObject;

        Services.Ads = new NullAdService();
        Services.Store = new NullStoreService();
        screen.Focus(rareTile);
        Check("no ads/store (PC): Buy, Try and Restore are hidden, gem Unlock stays",
            !buyObject.activeSelf && !tryObject.activeSelf && !restoreObject.activeSelf && screenTransform.Find("InfoPanel/Unlock").gameObject.activeSelf);

        var fakeAds = new FakeAdService { RewardedReady = false };
        var fakeStore = new FakeStoreService();
        Services.Ads = fakeAds;
        Services.Store = fakeStore;
        screen.Focus(rareTile);
        var tryButton = tryObject.GetComponent<Button>();
        Check("ads/store present: Buy, Try, Restore shown; Try disabled while no ad is loaded",
            buyObject.activeSelf && buyObject.GetComponentInChildren<TMP_Text>().text == "Buy $0.99" && restoreObject.activeSelf
            && tryObject.activeSelf && !tryButton.interactable && tryObject.GetComponentInChildren<TMP_Text>().text == "Ad not ready");

        fakeAds.RewardedReady = true;
        fakeAds.NextRewardEarned = false;
        screen.Focus(rareTile);
        tryButton.onClick.Invoke();
        Check("closing the rewarded ad early grants no trial", fakeAds.RewardedShown == 1 && !UnlockService.IsUsable(rare) && UnlockService.TrialRunsLeft(rare) == 0);

        fakeAds.NextRewardEarned = true;
        tryButton.onClick.Invoke();
        var trialStatus = screenTransform.Find("InfoPanel/Status").GetComponent<TMP_Text>().text;
        Check("earning the reward starts a 3-run trial: selected, Select shown, Try hidden",
            UnlockService.TrialRunsLeft(rare) == EconomyConfig.Instance.TrialRuns && UnlockService.Selected(UnlockCategory.Character) == rare
            && trialStatus == $"Trial: {EconomyConfig.Instance.TrialRuns} runs left" && !tryObject.activeSelf && screenTransform.Find("InfoPanel/Select").gameObject.activeSelf,
            trialStatus);

        SaveService.Load();
        Check("trial survives a save reload", UnlockService.TrialRunsLeft(rare) == EconomyConfig.Instance.TrialRuns && UnlockService.Selected(UnlockCategory.Character) == rare);

        screenTransform.Find("InfoPanel/Buy").GetComponent<Button>().onClick.Invoke();
        Check("Buy grants the item through the store and ends the trial",
            fakeStore.Owned.Contains(rare.ProductId) && UnlockService.IsOwned(rare) && UnlockService.TrialRunsLeft(rare) == 0 && !buyObject.activeSelf);

        fakeStore.Owned.Add(StoreProducts.RemoveAds);
        fakeStore.Owned.Add(common.ProductId);
        restoreObject.GetComponent<Button>().onClick.Invoke();
        restoreObject.GetComponent<Button>().onClick.Invoke();
        Check("Restore Purchases grants owned products and Remove Ads, idempotently",
            UnlockService.IsOwned(common) && UnlockService.RemoveAdsOwned && SaveService.Data.owned.Count(entry => entry.id == common.Id) == 1);

        UseFreshTempSave();
        Services.Ads = new FakeAdService();
        Services.Store = new FakeStoreService();

        Click("SelectionScreen/TopBar/Back");
        yield return 0.2f;
        Check("Back closes the screen, restores the main menu and frees the preview",
            !screen.gameObject.activeSelf && mainMenu.gameObject.activeSelf && GameObject.Find("SelectionPreviewStage") == null);

        Click("MainMenu/Characters");
        yield return WaitUntil(() => screen.gameObject.activeInHierarchy);
        Check("reopening focuses the selected character", !timedOut && screen.Focused != null && screen.Focused.Definition == UnlockService.Selected(UnlockCategory.Character));
        Click("SelectionScreen/TopBar/Back");
        yield return 0.2f;

        // 4. Layout audit at real screen sizes.
        var problems = LayoutAudit();
        Check($"both selection screens fit {LayoutScreens.Length} screen sizes", problems.Count == 0, string.Join(" | ", problems));
        yield return 0.1f;
        UseFreshTempSave();
    }

    private static Color PreviewCentre(Transform screen, out Color corner)
    {
        var raw = screen.Find("InfoPanel/PreviewFrame/Preview").GetComponent<RawImage>();
        var target = (RenderTexture)raw.texture;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        RenderTexture.active = previous;
        var centre = texture.GetPixel(target.width / 2, target.height / 2);
        corner = texture.GetPixel(4, 4);
        Object.Destroy(texture);
        return centre;
    }

    private static Vector3 ToVector(Color color)
    {
        return new Vector3(color.r, color.g, color.b);
    }

    // Instantiates the prefab on a canvas of each real screen size and measures it. Returns human-readable problems.
    private static List<string> LayoutAudit()
    {
        var problems = new List<string>();

        // Guard against a vacuous pass: the helpers must flag a known-bad case.
        var a = new Rect(0, 0, 10, 10);
        if (!Overlaps(a, a) || Overlaps(a, new Rect(20, 0, 10, 10)) || !Outside(new Rect(-5, 0, 10, 10), a))
        {
            problems.Add("audit helpers are broken");
            return problems;
        }

        var scaler = GameObject.Find("Canvas").GetComponent<CanvasScaler>();
        foreach (var prefabPath in new[] { SelectionPrefabPath, CompanionPrefabPath })
        {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        foreach (var screenSize in LayoutScreens)
        {
            var log = Mathf.Lerp(Mathf.Log(screenSize.x / scaler.referenceResolution.x, 2f), Mathf.Log(screenSize.y / scaler.referenceResolution.y, 2f), scaler.matchWidthOrHeight);
            var scale = Mathf.Pow(2f, log);
            var canvasSize = new Vector2(screenSize.x / scale, screenSize.y / scale);
            var label = $"{System.IO.Path.GetFileNameWithoutExtension(prefabPath)} {screenSize.x}x{screenSize.y}";

            var canvasObject = new GameObject("LayoutTestCanvas", typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = canvasSize;
            canvasRect.position = new Vector3(0f, -3000f, 0f);

            var instance = Object.Instantiate(prefab, canvasRect);
            instance.SetActive(true);
            var root = (RectTransform)instance.transform;
            var screen = instance.GetComponent<SelectionScreen>();

            // Worst case for size: both action buttons shown, with the longest label the price table can produce.
            var actions = new List<RectTransform>();
            foreach (var actionName in new[] { "Select", "Unlock", "Buy", "Try", "Restore" })
            {
                var action = (RectTransform)root.Find("InfoPanel/" + actionName);
                action.gameObject.SetActive(true);
                actions.Add(action);
            }
            actions[1].GetComponentInChildren<TMP_Text>().text = "Need 17995 more gems";
            actions[2].GetComponentInChildren<TMP_Text>().text = "Buy CHF 999.99";

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            foreach (var group in root.GetComponentsInChildren<LayoutGroup>())
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
            }

            // Keyboard/gamepad focus on the last tile must scroll it into view wherever the grid is too short to show every tile.
            screen.Focus(screen.Tiles[screen.Tiles.Count - 1]);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            AuditOne(problems, label, scale, root, screen.Tiles, actions);
            Object.DestroyImmediate(canvasObject);
        }
        }
        return problems;
    }

    private static void AuditOne(List<string> problems, string label, float scale, RectTransform root, IReadOnlyList<UnlockTile> tiles, List<RectTransform> actions)
    {
        var layout = root.GetComponent<SelectionLayout>();
        var mode = layout.IsLandscape ? "landscape" : "portrait";
        var tag = $"{label} {mode}";
        var rootRect = root.rect;

        var topBar = (RectTransform)root.Find("TopBar");
        var back = (RectTransform)topBar.Find("Back");
        var title = (RectTransform)topBar.Find("Title");
        var gems = (RectTransform)topBar.Find("GemCounter");
        var info = (RectTransform)root.Find("InfoPanel");
        var grid = (RectTransform)root.Find("GridPanel");
        var viewport = (RectTransform)grid.Find("Viewport");

        var named = new (string Name, RectTransform Rect)[] { ("TopBar", topBar), ("Back", back), ("Title", title), ("GemCounter", gems), ("InfoPanel", info), ("GridPanel", grid) };
        foreach (var (name, rect) in named)
        {
            if (Outside(LocalRect(rect, root), rootRect))
            {
                problems.Add($"{tag}: {name} leaves the screen");
            }
        }

        AddOverlap(problems, tag, "Back/Title", LocalRect(back, root), LocalRect(title, root));
        AddOverlap(problems, tag, "Title/GemCounter", LocalRect(title, root), LocalRect(gems, root));
        AddOverlap(problems, tag, "InfoPanel/GridPanel", LocalRect(info, root), LocalRect(grid, root));
        AddOverlap(problems, tag, "TopBar/InfoPanel", LocalRect(topBar, root), LocalRect(info, root));
        AddOverlap(problems, tag, "TopBar/GridPanel", LocalRect(topBar, root), LocalRect(grid, root));

        var infoRect = LocalRect(info, root);
        var gridRect = LocalRect(grid, root);
        if (layout.IsLandscape ? gridRect.center.x >= infoRect.center.x : gridRect.center.y <= infoRect.center.y)
        {
            problems.Add($"{tag}: panels are not arranged for {mode}");
        }

        // Info column: every child inside the panel, none overlapping, all texts fitting their boxes.
        var children = new List<RectTransform>();
        foreach (RectTransform child in info)
        {
            if (child.gameObject.activeSelf)
            {
                children.Add(child);
                if (Outside(LocalRect(child, root), infoRect))
                {
                    problems.Add($"{tag}: InfoPanel/{child.name} overflows the panel");
                }
            }
        }
        for (var i = 0; i < children.Count; i++)
        {
            for (var j = i + 1; j < children.Count; j++)
            {
                AddOverlap(problems, tag, $"{children[i].name}/{children[j].name}", LocalRect(children[i], root), LocalRect(children[j], root));
            }
        }

        foreach (var text in root.GetComponentsInChildren<TMP_Text>())
        {
            if (text.enableAutoSizing || !text.gameObject.activeInHierarchy)
            {
                continue;
            }
            var width = ((RectTransform)text.transform).rect.width;
            if (text.preferredWidth > width + Tolerance)
            {
                problems.Add($"{tag}: text '{text.text}' needs {text.preferredWidth:0} but has {width:0}");
            }
        }

        var tapTargets = new List<(string, RectTransform)> { ("Back", back) };
        foreach (var action in actions)
        {
            tapTargets.Add((action.name, action));
        }
        foreach (var (name, button) in tapTargets)
        {
            if (button.rect.height * scale < MinTouchPixels)
            {
                problems.Add($"{tag}: {name} is {button.rect.height * scale:0} px tall (< {MinTouchPixels:0})");
            }
        }

        // Tiles: inside the grid's width, first row inside its height, big enough to tap.
        var viewportRect = LocalRect(viewport, root);
        foreach (var tile in tiles)
        {
            var tileRect = LocalRect((RectTransform)tile.transform, root);
            if (tileRect.xMin < viewportRect.xMin - Tolerance || tileRect.xMax > viewportRect.xMax + Tolerance)
            {
                problems.Add($"{tag}: tile {tile.Definition.DisplayName} sticks out sideways");
            }
            if (tileRect.width * scale < MinTouchPixels || tileRect.height * scale < MinTouchPixels)
            {
                problems.Add($"{tag}: tile {tile.Definition.DisplayName} too small to tap");
            }
        }
        if (tiles.Count > 0 && Outside(LocalRect((RectTransform)tiles[tiles.Count - 1].transform, root), viewportRect))
        {
            problems.Add($"{tag}: the focused (last) tile is not scrolled into view");
        }
    }

    private static void AddOverlap(List<string> problems, string tag, string pair, Rect first, Rect second)
    {
        if (Overlaps(first, second))
        {
            problems.Add($"{tag}: {pair} overlap");
        }
    }

    private static Rect LocalRect(RectTransform rect, RectTransform root)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var min = root.InverseTransformPoint(corners[0]);
        var max = root.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static bool Overlaps(Rect first, Rect second)
    {
        return first.xMin < second.xMax - Tolerance && second.xMin < first.xMax - Tolerance
            && first.yMin < second.yMax - Tolerance && second.yMin < first.yMax - Tolerance;
    }

    private static bool Outside(Rect inner, Rect outer)
    {
        return inner.xMin < outer.xMin - Tolerance || inner.xMax > outer.xMax + Tolerance
            || inner.yMin < outer.yMin - Tolerance || inner.yMax > outer.yMax + Tolerance;
    }
}
