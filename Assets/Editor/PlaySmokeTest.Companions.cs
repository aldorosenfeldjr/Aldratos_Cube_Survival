using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

// Companion checks for the play smoke test (roadmap item 3): the catalog matches the plan, animations exist and loop, the spawner
// places and swaps the selected companion (sized, on the ground, moving only if it can), the companion screen works through its
// real buttons, and the main menu with its extra button still fits the shortest canvas.
public static partial class PlaySmokeTest
{
    private const string CompanionPrefabPath = "Assets/Prefabs/UI/CompanionSelection.prefab";

    private static IEnumerator CompanionChecks()
    {
        var catalog = UnlockCatalog.Instance;
        var companions = catalog.InCategory(UnlockCategory.Companion).Cast<CompanionDefinition>().ToList();
        int Count(PriceTier tier) => companions.Count(item => item.Tier == tier);

        Check("8 companions: default cat + 3 Rare, 2 Epic, 2 Legendary",
            companions.Count == 8 && companions.Count(item => item.IsDefault) == 1 && companions.First(item => item.IsDefault).Id == "comp.cat"
            && Count(PriceTier.Free) == 1 && Count(PriceTier.Rare) == 3 && Count(PriceTier.Epic) == 2 && Count(PriceTier.Legendary) == 2,
            $"total={companions.Count} rare={Count(PriceTier.Rare)} epic={Count(PriceTier.Epic)} legendary={Count(PriceTier.Legendary)}");
        var pcTotal = companions.Sum(item => UnlockService.PriceFor(item.Tier, false));
        var mobileTotal = companions.Sum(item => UnlockService.PriceFor(item.Tier, true));
        Check("all companions cost 27,000 gems on PC and 67,500 on mobile", pcTotal == 27000 && mobileTotal == 67500, $"pc={pcTotal} mobile={mobileTotal}");
        Check("companion ids, product ids and names are unique; characters and companions do not clash",
            companions.Select(item => item.Id).Distinct().Count() == 8 && companions.Select(item => item.ProductId).Distinct().Count() == 8
            && companions.Select(item => item.DisplayName).Distinct().Count() == 8 && catalog.Items.Select(item => item.Id).Distinct().Count() == catalog.Items.Count);

        // Every prefab animates with the state names the wanderer plays; the ones that wander have Walk and Run, and every clip loops.
        var animationProblems = new List<string>();
        foreach (var companion in companions)
        {
            var instance = Object.Instantiate(companion.Prefab);
            var animator = instance.GetComponent<Animator>();
            var controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
            if (controller == null)
            {
                animationProblems.Add(companion.Id + ": no animator controller");
                Object.DestroyImmediate(instance);
                continue;
            }

            var states = controller.layers[0].stateMachine.states.Select(entry => entry.state).ToList();
            var names = states.Select(state => state.name).ToList();
            var needed = companion.Wanders ? new[] { "Idle_A", "Idle_B", "Walk", "Run" } : new[] { "Idle_A", "Idle_B" };
            foreach (var name in needed.Where(name => !names.Contains(name)))
            {
                animationProblems.Add($"{companion.Id}: missing state {name}");
            }
            if (!companion.IsDefault)
            {
                foreach (var state in states.Where(state => needed.Contains(state.name)))
                {
                    var clip = state.motion as AnimationClip;
                    if (clip == null || !clip.isLooping)
                    {
                        animationProblems.Add($"{companion.Id}: {state.name} clip missing or not looping");
                    }
                }
            }
            Object.DestroyImmediate(instance);
        }
        Check("every companion has Idle (and Walk/Run when it wanders) as looping animations", animationProblems.Count == 0, string.Join("; ", animationProblems));

        // The spawner puts the default cat where the old hand-placed one stood: sized, on the ground, wandering, and only one of it.
        UseFreshTempSave();
        var spawner = Object.FindAnyObjectByType<CompanionSpawner>(FindObjectsInactive.Include);
        Check("Core has a companion spawner and the old scene cat is gone", spawner != null && GameObject.Find("Cat Lite") == null);

        // A builder that fails halfway can leave its temporary model in the open scene and get it saved (this happened once: a giant
        // pug stood in the middle of the play field). No animated model may sit at the root of Core except the spawned companion.
        var strayModels = new List<string>();
        foreach (var root in spawner.gameObject.scene.GetRootGameObjects())
        {
            if (!root.name.StartsWith("Companion_") && root.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                strayModels.Add(root.name);
            }
        }
        Check("no stray model instances in the Core scene", strayModels.Count == 0, string.Join(", ", strayModels));
        if (spawner == null)
        {
            yield break;
        }

        spawner.Spawn();
        yield return 0.3f; // sized and seated on the first rendered frame
        var cat = companions.First(item => item.IsDefault);
        Check("default companion (the cat) is spawned, sized, on the ground and wandering",
            spawner.Definition == cat && CompanionLooksRight(spawner, cat) && spawner.Current.GetComponent<CatWanderer>() != null,
            DescribeCompanion(spawner));

        // Buying and selecting a companion swaps it live; a companion without Walk/Run stays put; characters are unaffected.
        var characterBefore = UnlockService.Selected(UnlockCategory.Character);
        var cow = companions.First(item => item.Id == "comp.cow");
        var pig = companions.First(item => item.Id == "comp.pig");
        Wallet.Add(UnlockService.Price(cow) + UnlockService.Price(pig));
        UnlockService.TryBuyWithGems(cow);
        UnlockService.Select(cow);
        yield return 0.3f;
        Check("selecting a wandering companion swaps it (one companion only)",
            spawner.Definition == cow && CompanionLooksRight(spawner, cow) && spawner.Current.GetComponent<CatWanderer>() != null && CompanionCount() == 1 && GameObject.Find("Cat Lite") == null,
            DescribeCompanion(spawner) + $" count={CompanionCount()}");

        UnlockService.TryBuyWithGems(pig);
        UnlockService.Select(pig);
        yield return 0.3f;
        var pigStart = spawner.Current.transform.position;
        Check("a companion without Walk/Run has no wanderer", spawner.Definition == pig && CompanionLooksRight(spawner, pig) && spawner.Current.GetComponent<CatWanderer>() == null && CompanionCount() == 1,
            DescribeCompanion(spawner));

        var pose0 = PoseSnapshot(spawner.Current);
        yield return 1.0f;
        var pose1 = PoseSnapshot(spawner.Current);
        var poseChange = PoseDifference(pose0, pose1);
        Check("the idle animation actually plays (pose changes over time) and the animal stays in place",
            poseChange > 0.0005f && Vector3.Distance(pigStart, spawner.Current.transform.position) < 0.01f, $"pose change {poseChange:0.00000}");

        Check("character selection is independent of the companion", UnlockService.Selected(UnlockCategory.Character) == characterBefore);

        var zebra = companions.First(item => item.Id == "comp.zebra");
        UnlockService.StartTrial(zebra);
        yield return 0.3f;
        var trialWorks = spawner.Definition == zebra && CompanionCount() == 1;
        for (var run = 0; run < EconomyConfig.Instance.TrialRuns; run++)
        {
            UnlockService.EndRunForTrials();
        }
        yield return 0.3f;
        Check("a companion trial swaps it in, and the last owned companion returns when it ends", trialWorks && spawner.Definition == pig && CompanionCount() == 1,
            $"trial={trialWorks} back={spawner.Definition?.Id}");

        // The companion screen through its real buttons.
        UseFreshTempSave();
        spawner.Spawn();
        Wallet.Add(UnlockService.Price(pig) + 5);
        var mainMenu = CanvasChild("MainMenu");
        var screenTransform = CanvasChild("CompanionScreen");
        var screen = screenTransform != null ? screenTransform.GetComponent<SelectionScreen>() : null;
        Check("main menu has a Companions button and Core has the companion screen", CanvasChild("MainMenu/Companions") != null && screen != null);
        if (screen == null)
        {
            yield break;
        }

        Click("MainMenu/Companions");
        yield return WaitUntil(() => screen.gameObject.activeInHierarchy);
        yield return 0.4f;
        var title = screenTransform.Find("TopBar/Title").GetComponent<TMP_Text>().text;
        Check("Companions opens its own screen: title, 8 tiles, the cat focused, main menu hidden",
            !timedOut && title == "Companions" && !mainMenu.gameObject.activeSelf && screen.Tiles.Count == 8 && screen.Focused != null && screen.Focused.Definition == cat,
            $"title='{title}' tiles={screen.Tiles.Count}");

        var catFraming = PreviewFraming(screenTransform);
        Check("preview shows the cat large and centred", catFraming.ok, catFraming.detail);

        var pigTile = screen.Tiles.First(tile => tile.Definition == pig);
        screen.Focus(pigTile);
        yield return 0.4f;
        var pigFraming = PreviewFraming(screenTransform);
        var previewAnimal = GameObject.Find("SelectionPreviewStage")?.transform.Find("Preview_comp.pig");
        var previewPose0 = previewAnimal != null ? PoseSnapshot(previewAnimal.gameObject) : null;
        yield return 0.5f;
        var previewPose1 = previewAnimal != null ? PoseSnapshot(previewAnimal.gameObject) : null;
        var previewChange = previewAnimal != null ? PoseDifference(previewPose0, previewPose1) : -1f;
        Check("preview shows the focused animal and is static (paused, no rotation)",
            previewAnimal != null && pigFraming.ok && previewChange >= 0f && previewChange < 0.000001f,
            $"found={previewAnimal != null} {pigFraming.detail} pose change {previewChange:0.0000000}");

        var framingProblems = new List<string>();
        foreach (var tile in screen.Tiles.ToList())
        {
            screen.Focus(tile);
            yield return 0.35f;
            var framing = PreviewFraming(screenTransform);
            if (!framing.ok)
            {
                framingProblems.Add($"{tile.Definition.Id}: {framing.detail}");
            }
        }
        Check("every companion previews large and centred", framingProblems.Count == 0, string.Join("; ", framingProblems));
        screen.Focus(pigTile);

        var unlockButton = screenTransform.Find("InfoPanel/Unlock").GetComponent<Button>();
        var pigPrice = UnlockService.Price(pig);
        Check("locked companion shows its tier price on the Unlock button",
            unlockButton.gameObject.activeSelf && unlockButton.interactable && unlockButton.GetComponentInChildren<TMP_Text>().text == $"Unlock ({pigPrice} gems)");
        unlockButton.onClick.Invoke();
        screenTransform.Find("InfoPanel/Select").GetComponent<Button>().onClick.Invoke();
        yield return 0.3f;
        Check("Unlock then Select on the screen puts that animal in the world", UnlockService.IsOwned(pig) && spawner.Definition == pig && CompanionCount() == 1, DescribeCompanion(spawner));

        var horse = companions.First(item => item.Id == "comp.horse");
        screen.Focus(screen.Tiles.First(tile => tile.Definition == horse));
        var horseUnlock = screenTransform.Find("InfoPanel/Unlock").GetComponentInChildren<TMP_Text>().text;
        Check("a Legendary companion asks for its full price", horseUnlock == $"Need {UnlockService.Price(horse) - Wallet.Balance} more gems" && UnlockService.Price(horse) == 7200, horseUnlock);

        Click("CompanionScreen/TopBar/Back");
        yield return 0.2f;
        Check("Back closes the companion screen and restores the main menu", !screen.gameObject.activeSelf && mainMenu.gameObject.activeSelf && GameObject.Find("SelectionPreviewStage") == null);

        // The main menu now has five buttons plus Remove Ads: everything must fit the shortest canvas (about 864 units tall).
        UseFreshTempSave();
        spawner.Spawn();
        var menuProblems = MainMenuLayoutProblems(mainMenu);
        Check("main menu fits the shortest canvas with no overlaps", menuProblems.Count == 0, string.Join(" | ", menuProblems));
    }

    // Positions every main-menu element from its anchors on a synthetic worst-case canvas (968 wide: portrait; 864 tall: a landscape
    // phone), so corner-anchored items (Sound toggle, gem counter) are judged against that canvas, not whatever the Game view is.
    private static List<string> MainMenuLayoutProblems(Transform mainMenu)
    {
        mainMenu.Find("RemoveAds").gameObject.SetActive(true);
        return MenuLayoutProblems(mainMenu, new[] { "Title", "Play", "Characters", "Companions", "Exit", "ClearHighScore", "RemoveAds", "SoundToggle", "GemCounter" });
    }

    private static List<string> MenuLayoutProblems(Transform mainMenu, string[] names)
    {
        var problems = new List<string>();
        var canvas = new Vector2(968f, 864f);
        var limit = new Rect(-canvas.x / 2f, -canvas.y / 2f, canvas.x, canvas.y);
        var rects = new List<(string, Rect)>();
        foreach (var name in names)
        {
            var child = (RectTransform)mainMenu.Find(name);
            if (child == null)
            {
                problems.Add($"missing {name}");
                continue;
            }

            var anchor = new Vector2(Mathf.Lerp(-canvas.x / 2f, canvas.x / 2f, child.anchorMin.x), Mathf.Lerp(-canvas.y / 2f, canvas.y / 2f, child.anchorMin.y));
            var size = child.sizeDelta;
            var rect = new Rect(anchor + child.anchoredPosition - Vector2.Scale(child.pivot, size), size);
            if (Outside(rect, limit))
            {
                problems.Add($"{name} leaves the shortest canvas");
            }
            rects.Add((name, rect));
        }
        for (var i = 0; i < rects.Count; i++)
        {
            for (var j = i + 1; j < rects.Count; j++)
            {
                if (Overlaps(rects[i].Item2, rects[j].Item2))
                {
                    problems.Add($"{rects[i].Item1}/{rects[j].Item1} overlap");
                }
            }
        }
        return problems;
    }

    // The rendered subject (pixels that differ from the background) must be big enough to see and sit near the middle of the frame.
    private static (bool ok, string detail) PreviewFraming(Transform screen)
    {
        var raw = screen.Find("InfoPanel/PreviewFrame/Preview").GetComponent<RawImage>();
        var target = (RenderTexture)raw.texture;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        RenderTexture.active = previous;

        var pixels = texture.GetPixels();
        var background = pixels[0];
        int minX = target.width, maxX = -1, minY = target.height, maxY = -1;
        for (var y = 0; y < target.height; y++)
        {
            for (var x = 0; x < target.width; x++)
            {
                var pixel = pixels[y * target.width + x];
                if (Mathf.Abs(pixel.r - background.r) + Mathf.Abs(pixel.g - background.g) + Mathf.Abs(pixel.b - background.b) > 0.06f)
                {
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }
        Object.Destroy(texture);

        if (maxX < 0)
        {
            return (false, "nothing rendered");
        }

        var width = (maxX - minX + 1) / (float)target.width;
        var height = (maxY - minY + 1) / (float)target.height;
        var centreX = (minX + maxX) / 2f / target.width - 0.5f;
        var centreY = (minY + maxY) / 2f / target.height - 0.5f;
        var biggest = Mathf.Max(width, height);
        var ok = biggest >= 0.3f && biggest <= 0.95f && Mathf.Abs(centreX) < 0.15f && Mathf.Abs(centreY) < 0.15f;
        return (ok, $"subject {width:P0} x {height:P0} of the frame, off-centre ({centreX:+0.00;-0.00}, {centreY:+0.00;-0.00})");
    }

    private static int CompanionCount()
    {
        return Object.FindObjectsByType<Animator>(FindObjectsInactive.Include).Count(animator => animator.gameObject.name.StartsWith("Companion_"));
    }

    private static bool CompanionLooksRight(CompanionSpawner spawner, CompanionDefinition definition)
    {
        if (spawner.Current == null)
        {
            return false;
        }

        var size = CompanionDefinition.MaxExtent(spawner.Current);
        var feet = CompanionDefinition.WorldBounds(spawner.Current).min.y;
        return Mathf.Abs(size - definition.WorldSize) < definition.WorldSize * 0.12f && Mathf.Abs(feet - spawner.transform.position.y) < 0.1f;
    }

    private static string DescribeCompanion(CompanionSpawner spawner)
    {
        if (spawner.Current == null)
        {
            return "no companion";
        }
        return $"{spawner.Current.name} size={CompanionDefinition.MaxExtent(spawner.Current):0.00} feetY={CompanionDefinition.WorldBounds(spawner.Current).min.y:0.00} groundY={spawner.transform.position.y:0.00}";
    }

    // Every bone rotation, in order: comparing two snapshots bone by bone tells a playing animation (some bone moved) from a frozen pose
    // (identical to the last digit). A single summed number could cancel out and hide subtle idle motion.
    private static float[] PoseSnapshot(GameObject animal)
    {
        var values = new List<float>();
        foreach (var bone in animal.GetComponentsInChildren<Transform>())
        {
            var rotation = bone.localRotation;
            values.Add(rotation.x);
            values.Add(rotation.y);
            values.Add(rotation.z);
            values.Add(rotation.w);
        }
        return values.ToArray();
    }

    private static float PoseDifference(float[] first, float[] second)
    {
        if (first == null || second == null || first.Length != second.Length)
        {
            return -1f;
        }

        var total = 0f;
        for (var i = 0; i < first.Length; i++)
        {
            total += Mathf.Abs(first[i] - second[i]);
        }
        return total;
    }
}
