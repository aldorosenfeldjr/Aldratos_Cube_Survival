# Multi-Scene Levels (Theming) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the single existing scene into a persistent `Core` scene plus additive, per-theme `Level` scenes, add a Level Select screen between Main Menu and gameplay, migrate the current grass world to be theme #1 under this system, and add a new KayKit-Platformer-Pack ("Mario-ish") theme as theme #2.

**Architecture:** `Core.unity` holds everything that exists exactly once (cameras, light, player, `GameManager`, `PowerUpManager`, all UI). Each `Level_*.unity` scene holds only environment art and is loaded/unloaded additively when the player picks it from a new Level Select screen. A `LevelTheme` ScriptableObject per theme supplies the hazard/pickup prefabs to spawn; `GameManager`/`PowerUpSpawner` gain a runtime `ApplyTheme()` call instead of using fixed Inspector references. HUD icons stay global per power-up type (not per-theme) — an accepted, disclosed simplification (see Global Constraints).

**Tech Stack:** Unity 6000.6.0f1, Built-in Render Pipeline, C# single assembly `NewAssembly`, live-Editor verification via `mcp__unity-editor-mcp__*` MCP tools (see Global Constraints for the exact toolset — the CLI-wrapper syntax `unity command --format json X -- --args` seen in older docs does NOT apply).

**Spec:** `docs/superpowers/specs/2026-09-15-multi-scene-levels-design.md`

## Global Constraints

- Live-Editor scripting uses direct MCP tools: `mcp__unity-editor-mcp__eval_file` (params `file` + `timeout` — write each script to a temp `.cs` file, then call with its path), `editor_status`, `editor_play`, `editor_stop`, `console`, `clear_console`, `recompile`, `save_scene`, `open_scene`, `capture_game_view`. If not visible as callable tools, `ToolSearch` with `select:mcp__unity-editor-mcp__<name>,...` first.
- No automated test framework exists. Acceptance bar: clean compile (`compilationFailed: false`) and clean console (`consoleErrors: 0`) after every live-Editor step, verified via a fresh `clear_console` + `recompile` + `console` read (don't trust stale/historical entries).
- Git workflow: work happens on a feature branch off `dev` (per the project's established dev/main split), merged to `dev` when the plan is complete and verified — never commit straight to `main`.
- HUD icons for Speed Boost/Invincibility/Shield are read from the existing global `PowerUpDefinition` assets (`Assets/PowerUps/SpeedBoost.asset` etc.) regardless of which theme is active — this was decided during planning, not the original spec: `LevelTheme` only supplies which *world* prefab to spawn per type, not a per-theme icon. Every new pickup prefab's `PowerUpPickup.definition` field must still point at the SAME shared global definition asset used by the grass theme (not a new, theme-specific definition) — this is required for `PowerUpManager.Grant()`'s dedupe-by-type and the HUD's icon lookup to keep working across themes.
- Static, non-Rigidbody ground/platform colliders in this project use `MeshCollider.convex = false` (confirmed on the existing `Platform_TopMiddle`) — this differs from the moving hazard/pickup prefabs elsewhere in the project, which need `convex = true`. Do not convex-ify static ground meshes.
- Physical footprint every Level scene must match (captured from the current `Environment` group, live-read 2026-09-15): combined top-platform collider footprint spans roughly X -7.95..7.95, Z -3.49..3.49, top surface at world Y ≈ 0.25 (`Platform_TopLeft` pos (7,-0.75,0) scale (1,2,1); `Platform_TopMiddle` pos (0,-0.75,0) scale (6,2,1); `Platform_TopRight` pos (-7,-0.75,0) scale (1,2,1) — all three share Y=-0.75, all have `MeshCollider`, `convex=false`, no Rigidbody). `FallDownTrigger` stays in Core, unchanged, at its current position/size — do not move or resize it.
- No "return to Level Select / Main Menu" UI is added in this plan (out of scope per spec). The additive load/unload mechanism is still verified directly via script in Task 12, not through an in-game round-trip UI path that doesn't exist yet.
- Follow this project's established live-verification discipline: after any live-Editor mutation, verify with a fresh eval read before trusting it landed — don't assume success from the absence of a thrown exception (this caught a real silent-failure bug during the powerups plan).

---

### Task 1: `LevelTheme` and `LevelInfo` scripts

**Files:**
- Create: `Assets/Scripts/Levels/LevelTheme.cs`
- Create: `Assets/Scripts/Levels/LevelInfo.cs`

**Interfaces:**
- Produces: `class LevelTheme : ScriptableObject` with `DisplayName`, `HazardPrefab`, `SpeedBoostPrefab`, `InvincibilityPrefab`, `ShieldPrefab` (all `get`-only public properties backed by private serialized fields). `class LevelInfo : MonoBehaviour` with `LevelTheme Theme` (get-only property backed by a private serialized field).

- [ ] **Step 1: Write `LevelTheme.cs`**

```csharp
// Assets/Scripts/Levels/LevelTheme.cs
using UnityEngine;

[CreateAssetMenu(fileName = "LevelTheme", menuName = "Levels/Level Theme")]
public class LevelTheme : ScriptableObject
{
    [SerializeField]
    private string displayName;
    [SerializeField]
    private GameObject hazardPrefab;
    [SerializeField]
    private GameObject speedBoostPrefab;
    [SerializeField]
    private GameObject invincibilityPrefab;
    [SerializeField]
    private GameObject shieldPrefab;

    public string DisplayName => displayName;
    public GameObject HazardPrefab => hazardPrefab;
    public GameObject SpeedBoostPrefab => speedBoostPrefab;
    public GameObject InvincibilityPrefab => invincibilityPrefab;
    public GameObject ShieldPrefab => shieldPrefab;
}
```

- [ ] **Step 2: Write `LevelInfo.cs`**

```csharp
// Assets/Scripts/Levels/LevelInfo.cs
using UnityEngine;

public class LevelInfo : MonoBehaviour
{
    [SerializeField]
    private LevelTheme theme;

    public LevelTheme Theme => theme;
}
```

- [ ] **Step 3: Refresh and verify clean compile**

Run the refresh/console-check sequence (Global Constraints). Report the exact `compilationFailed`/`consoleErrors` values.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Levels/LevelTheme.cs Assets/Scripts/Levels/LevelTheme.cs.meta Assets/Scripts/Levels/LevelInfo.cs Assets/Scripts/Levels/LevelInfo.cs.meta
git commit -m "Add LevelTheme and LevelInfo scripts"
```

(The `Levels` folder and its own `.meta` will be created by the Editor on refresh — include it too if `git status` shows it as untracked.)

---

### Task 2: `LevelRegistry` script

**Files:**
- Create: `Assets/Scripts/Levels/LevelRegistry.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: `class LevelRegistry : ScriptableObject` with `LevelEntries` — a public get-only property returning a read-only list of `LevelRegistry.Entry`, a `[System.Serializable]` nested struct/class with `SceneName` (string) and `DisplayName` (string) fields. Task 6's `LevelSelect` reads this to populate tiles without needing to load each scene just to get a label.

- [ ] **Step 1: Write `LevelRegistry.cs`**

```csharp
// Assets/Scripts/Levels/LevelRegistry.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelRegistry", menuName = "Levels/Level Registry")]
public class LevelRegistry : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [SerializeField]
        private string sceneName;
        [SerializeField]
        private string displayName;

        public string SceneName => sceneName;
        public string DisplayName => displayName;
    }

    [SerializeField]
    private List<Entry> levelEntries = new List<Entry>();

    public IReadOnlyList<Entry> LevelEntries => levelEntries;
}
```

- [ ] **Step 2: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Levels/LevelRegistry.cs Assets/Scripts/Levels/LevelRegistry.cs.meta
git commit -m "Add LevelRegistry script"
```

---

### Task 3: `GameManager.ApplyTheme()` and `PowerUpSpawner.ApplyTheme()`

**Files:**
- Modify: `Assets/Scripts/GameManager.cs`
- Modify: `Assets/Scripts/PowerUps/PowerUpSpawner.cs`

**Interfaces:**
- Consumes: `LevelTheme` (Task 1).
- Produces: `GameManager.ApplyTheme(LevelTheme theme)` (public), `PowerUpSpawner.ApplyTheme(LevelTheme theme)` (public). `GameManager.ApplyTheme` calls `powerUpSpawner.ApplyTheme(theme)` internally, so callers (Task 6's `LevelSelect`) only need a `GameManager` reference, not a separate `PowerUpSpawner` reference.

- [ ] **Step 1: Add `PowerUpSpawner.ApplyTheme()`**

In `Assets/Scripts/PowerUps/PowerUpSpawner.cs`, add this public method (anywhere inside the class, e.g. right after `StopSpawning()`):

```csharp
    public void ApplyTheme(LevelTheme theme)
    {
        pickupPrefabs = new[] { theme.SpeedBoostPrefab, theme.InvincibilityPrefab, theme.ShieldPrefab };
    }
```

- [ ] **Step 2: Add `GameManager.ApplyTheme()`**

In `Assets/Scripts/GameManager.cs`, add this public method (anywhere inside the class, e.g. right after `Enable()`):

```csharp
    public void ApplyTheme(LevelTheme theme)
    {
        hazardPrefab = theme.HazardPrefab;
        powerUpSpawner.ApplyTheme(theme);
    }
```

- [ ] **Step 3: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/GameManager.cs Assets/Scripts/PowerUps/PowerUpSpawner.cs
git commit -m "Add ApplyTheme to GameManager and PowerUpSpawner"
```

---

### Task 4: `LevelTheme_Grass.asset`

**Files:**
- Create (via Editor): `Assets/Levels/LevelTheme_Grass.asset`

This task is live Unity-Editor asset work via `mcp__unity-editor-mcp__eval_file`, same technique used throughout this project.

- [ ] **Step 1: Create the asset**

```csharp
// create_leveltheme_grass.cs
System.IO.Directory.CreateDirectory("Assets/Levels");

var theme = ScriptableObject.CreateInstance<LevelTheme>();
var so = new UnityEditor.SerializedObject(theme);
so.FindProperty("displayName").stringValue = "Grass";
so.FindProperty("hazardPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Crate.prefab");
so.FindProperty("speedBoostPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_SpeedBoost.prefab");
so.FindProperty("invincibilityPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_Invincibility.prefab");
so.FindProperty("shieldPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_Shield.prefab");
so.ApplyModifiedProperties();

UnityEditor.AssetDatabase.CreateAsset(theme, "Assets/Levels/LevelTheme_Grass.asset");
UnityEditor.AssetDatabase.SaveAssets();
return "Created LevelTheme_Grass.asset";
```

- [ ] **Step 2: Verify**

Read the asset back and confirm all 5 fields are non-null/correct (`displayName == "Grass"`, each prefab reference matches the path it was assigned from).

- [ ] **Step 3: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 4: Commit**

```bash
git add Assets/Levels/LevelTheme_Grass.asset Assets/Levels/LevelTheme_Grass.asset.meta Assets/Levels.meta
git commit -m "Add LevelTheme_Grass asset"
```

(Review `git status` first — include `Assets/Levels.meta` only if it was actually generated as a new untracked file.)

---

### Task 5: Scene split — `Core.unity` + `Level_Grass.unity`

**Files:**
- Modify (via Editor, rename): `Assets/Scenes/Game.unity` → `Assets/Scenes/Core.unity`
- Create (via Editor): `Assets/Scenes/Level_Grass.unity`

This is the highest-risk task in the plan — it restructures already-working content. Do it in Edit mode. Do not enter Play mode until Task 7.

- [ ] **Step 1: Rename the scene asset**

```csharp
// rename_game_to_core.cs
var error = UnityEditor.AssetDatabase.RenameAsset("Assets/Scenes/Game.unity", "Core");
return string.IsNullOrEmpty(error) ? "Renamed Game.unity to Core.unity" : "ERROR: " + error;
```

This preserves the scene asset's GUID (important — nothing currently references it by GUID since Build Settings is empty, but preserving it is still the correct, safe rename mechanism vs. a raw filesystem rename).

- [ ] **Step 2: Open Core, create an empty Level_Grass scene additively**

```csharp
// create_level_grass_scene.cs
var coreScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Core.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var levelGrassScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
    UnityEditor.SceneManagement.NewSceneMode.Additive);
return "Core loaded, empty additive scene created: " + levelGrassScene.name;
```

- [ ] **Step 3: Move the Environment hierarchy into the new scene**

```csharp
// move_environment_to_level_grass.cs
// Unity names a freshly created, not-yet-saved empty scene "New Scene" -
// that's the additive scene Step 2 created.
var coreScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Core");
var newScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("New Scene");
if (!newScene.IsValid())
{
    return "ERROR: could not find the additive 'New Scene' created in Step 2 - check it's still open.";
}

GameObject environment = null;
foreach (var root in coreScene.GetRootGameObjects())
{
    if (root.name == "Environment") environment = root;
}
if (environment == null) return "ERROR: Environment root not found in Core scene";

UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(environment, newScene);
return "Moved Environment (and its " + environment.transform.childCount + " children) into " + newScene.name;
```

- [ ] **Step 4: Save the new scene as Level_Grass.unity**

```csharp
// save_level_grass_scene.cs
var newScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("New Scene");
var saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, "Assets/Scenes/Level_Grass.unity");
return "SaveScene returned: " + saved;
```

- [ ] **Step 5: Add the `LevelInfo` component to Level_Grass, wired to `LevelTheme_Grass`**

```csharp
// wire_level_grass_info.cs
var levelGrassScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Level_Grass");
GameObject environment = null;
foreach (var root in levelGrassScene.GetRootGameObjects())
{
    if (root.name == "Environment") environment = root;
}
if (environment == null) return "ERROR: Environment root not found in Level_Grass scene";

var info = environment.AddComponent<LevelInfo>();
var so = new UnityEditor.SerializedObject(info);
so.FindProperty("theme").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelTheme>("Assets/Levels/LevelTheme_Grass.asset");
so.ApplyModifiedProperties();

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(levelGrassScene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(levelGrassScene);
return "Added LevelInfo to Environment in Level_Grass, wired to LevelTheme_Grass.";
```

- [ ] **Step 6: Verify Core no longer contains Environment, and save Core**

```csharp
// verify_and_save_core.cs
var coreScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Core");
var sb = new System.Text.StringBuilder();
foreach (var root in coreScene.GetRootGameObjects())
{
    sb.AppendLine(root.name);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(coreScene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(coreScene);
return "Core scene roots after split:\n" + sb.ToString();
```

Confirm the printed root list does NOT include `Environment`, and DOES include `FallDownTrigger`, `GameManager`, `PowerUpManager`, `NewPlayer`, `Player`, `Main Camera`, `Main VCam`, `Zoom VCam`, `ZoomTarget`, `Directional Light`, `EventSystem`, `Canvas`, `ScoreLabel`, `ScoreText` — the exact set captured in Global Constraints minus `Environment`.

- [ ] **Step 7: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Split scene: Core.unity (persistent) + Level_Grass.unity (environment)"
```

(Review `git status` first — expect `Assets/Scenes/Game.unity` → `Assets/Scenes/Core.unity` rename, new `Assets/Scenes/Level_Grass.unity` + `.meta`. No other files should have changed.)

---

### Task 6: Level Select screen + wiring + Build Settings

**Files:**
- Create: `Assets/Scripts/LevelSelect.cs`
- Create (via Editor): `Assets/Levels/LevelRegistry.asset` (grass-only for now — Task 11 adds the KayKit entry)
- Modify: `Assets/Scripts/MainMenu.cs`
- Modify (via Editor): `Assets/Scenes/Core.unity` (new `LevelSelect` UI panel under `Canvas`, `MainMenu` wiring, Build Settings)

**Interfaces:**
- Consumes: `LevelRegistry`, `LevelTheme`, `LevelInfo` (Tasks 1-2), `GameManager.ApplyTheme` (Task 3).
- Produces: `class LevelSelect : MonoBehaviour` with a public `SelectLevel(string sceneName)` method (wired to each generated tile's Button `onClick`).

- [ ] **Step 1: Write `LevelSelect.cs`**

```csharp
// Assets/Scripts/LevelSelect.cs
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;
    [SerializeField]
    private LevelRegistry registry;
    [SerializeField]
    private GameObject levelTilePrefab;
    [SerializeField]
    private Transform tileContainer;
    [SerializeField]
    private CanvasGroup canvasGroup;

    private string loadedLevelSceneName;

    private void OnEnable()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        foreach (Transform child in tileContainer)
        {
            Destroy(child.gameObject);
        }

        GameObject firstTile = null;
        foreach (var entry in registry.LevelEntries)
        {
            var tile = Instantiate(levelTilePrefab, tileContainer);
            var label = tile.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            label.text = entry.DisplayName;

            var capturedSceneName = entry.SceneName;
            tile.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectLevel(capturedSceneName));

            if (firstTile == null)
            {
                firstTile = tile;
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstTile);
    }

    public void SelectLevel(string sceneName)
    {
        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (!string.IsNullOrEmpty(loadedLevelSceneName))
        {
            yield return SceneManager.UnloadSceneAsync(loadedLevelSceneName);
        }

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        loadedLevelSceneName = sceneName;

        var levelScene = SceneManager.GetSceneByName(sceneName);
        LevelInfo levelInfo = null;
        foreach (var root in levelScene.GetRootGameObjects())
        {
            levelInfo = root.GetComponentInChildren<LevelInfo>();
            if (levelInfo != null)
            {
                break;
            }
        }

        gameManager.ApplyTheme(levelInfo.Theme);

        canvasGroup.alpha = 0f;
        gameManager.Enable();
        gameObject.SetActive(false);
    }
}
```

- [ ] **Step 2: Refresh and verify clean compile**

Run the refresh/console-check sequence before touching the scene.

- [ ] **Step 3: Build the Level Select UI panel and a level-tile prefab, live via Editor**

Read `Assets/Scenes/Core.unity`'s `Canvas/MainMenu` hierarchy first (`eval` a small script printing its children's `RectTransform` values) so the new panel matches existing fonts/sizes/positions — do not guess values, this project has an established "normalize fonts/sizes across all menus" convention from an earlier phase. Build:

- A `LevelSelect` GameObject under `Canvas` (sibling of `MainMenu`, `Score`, `GameOverMenu`, `NewRecordScreen`), inactive by default, with a `CanvasGroup` and the `LevelSelect` component.
- A title text ("Select Level" or similar, matching `MainMenu`'s `Title` text style).
- A `Transform` container named exactly `TileContainer` (a child with a `HorizontalLayoutGroup` or `GridLayoutGroup`, depending on how many tiles read well at 2 entries — a simple horizontal row is enough for 2 levels) — this is `tileContainer`.
- A level-tile prefab (`Assets/Prefabs/LevelTile.prefab`): a `Button` + `Image` (matching `MainMenu`/`GameOverMenu`'s existing button visual style) with a child `TextMeshProUGUI` for the display name — this is `levelTilePrefab`.
- Wire the `LevelSelect` component's `gameManager` (→ the scene's `GameManager`), `registry` (→ `Assets/Levels/LevelRegistry.asset`, created in Step 4 below), `levelTilePrefab`, `tileContainer`, `canvasGroup` fields.

Verify via eval after building: the `LevelSelect` GameObject exists under `Canvas`, is inactive, has all 5 `LevelSelect` fields non-null, and the tile prefab has both a `Button` and a `TextMeshProUGUI` child.

- [ ] **Step 4: Create `LevelRegistry.asset` (grass-only)**

```csharp
// create_level_registry.cs
var registry = ScriptableObject.CreateInstance<LevelRegistry>();
var so = new UnityEditor.SerializedObject(registry);
var entriesProp = so.FindProperty("levelEntries");
entriesProp.arraySize = 1;
var entry0 = entriesProp.GetArrayElementAtIndex(0);
entry0.FindPropertyRelative("sceneName").stringValue = "Level_Grass";
entry0.FindPropertyRelative("displayName").stringValue = "Grass";
so.ApplyModifiedProperties();

UnityEditor.AssetDatabase.CreateAsset(registry, "Assets/Levels/LevelRegistry.asset");
UnityEditor.AssetDatabase.SaveAssets();
return "Created LevelRegistry.asset with 1 entry (Level_Grass).";
```

- [ ] **Step 5: Wire `MainMenu.Play()`'s completion to show Level Select instead of starting the game directly**

In `Assets/Scripts/MainMenu.cs`, the existing `OnComplete()` is:

```csharp
    public void OnComplete()
    {
        scoreRectTransform
            .LeanMoveY(-72f, 0.75f)
            .setEaseOutBounce();

        gameManager.Enable();
        Destroy(gameObject);
    }
```

Change it to:

```csharp
    public void OnComplete()
    {
        scoreRectTransform
            .LeanMoveY(-72f, 0.75f)
            .setEaseOutBounce();

        levelSelect.SetActive(true);
        Destroy(gameObject);
    }
```

And add a new serialized field near the top of the class, alongside the existing `gameManager` field:

```csharp
    [SerializeField]
    private GameObject levelSelect;
```

The existing `[SerializeField] private GameManager gameManager;` field can stay (unused after this change is also fine to leave — it's a minor, harmless unused-Inspector-reference; removing it is optional cleanup, not required). Wire the new `levelSelect` field to the `LevelSelect` GameObject built in Step 3, live via Editor.

- [ ] **Step 6: Register scenes in Build Settings**

```csharp
// register_build_settings.cs
var scenes = new[]
{
    new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/Core.unity", true),
    new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/Level_Grass.unity", true),
};
UnityEditor.EditorBuildSettings.scenes = scenes;
return "Registered " + scenes.Length + " scenes in Build Settings (Core, Level_Grass).";
```

- [ ] **Step 7: Save the Core scene, refresh, verify clean console**

```
mcp__unity-editor-mcp__save_scene
```

Then run the refresh/console-check sequence.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add Level Select screen, wire MainMenu to it, register scenes in Build Settings"
```

(Review `git status` first — expect `Assets/Scripts/LevelSelect.cs(+.meta)`, `Assets/Scripts/MainMenu.cs`, `Assets/Scenes/Core.unity`, `Assets/Levels/LevelRegistry.asset(+.meta)`, `Assets/Prefabs/LevelTile.prefab(+.meta)`, `ProjectSettings/EditorBuildSettings.asset`.)

---

### Task 7: End-to-end live verification — Grass theme through Level Select

**Files:** none (verification only)

This is the regression-test checkpoint for the scene split — confirm nothing broke before building new KayKit content on top of it.

- [ ] **Step 1: Enter Play mode, click Play, pick the Grass tile**

```csharp
// click_play.cs
var playBtn = GameObject.Find("Canvas/MainMenu/Play").GetComponent<UnityEngine.UI.Button>();
playBtn.onClick.Invoke();
return "Play clicked";
```

Wait for the transition (poll for `Canvas/LevelSelect` becoming active rather than a fixed sleep — this project's established pattern from earlier live-testing found fixed sleeps unreliable across tool-call round-trips). Then:

```csharp
// click_grass_tile.cs
GameObject Find(string path)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var parts = path.Split('/');
    Transform current = null;
    foreach (var root in scene.GetRootGameObjects())
        if (root.name == parts[0]) current = root.transform;
    if (current == null) return null;
    for (int i = 1; i < parts.Length; i++)
    {
        current = current.Find(parts[i]);
        if (current == null) return null;
    }
    return current.gameObject;
}

var levelSelect = Find("Canvas/LevelSelect");
var tileContainer = levelSelect.transform.Find("TileContainer");
var firstTile = tileContainer.GetChild(0).GetComponent<UnityEngine.UI.Button>();
firstTile.onClick.Invoke();
return "Grass tile clicked";
```

- [ ] **Step 2: Confirm the level loaded and gameplay started**

Verify: `SceneManager.GetSceneByName("Level_Grass").isLoaded` is `true`; `GameObject.Find("Environment")` finds the moved environment; `GameObject.Find("NewPlayer")` is active; `GameManager.Instance` is active and its (now-private, so check via the theme it was given, or just confirm hazards/pickups spawn) is running.

- [ ] **Step 3: Full gameplay pass**

Park the player safely (established pattern: elevate Y, zero velocity, disable gravity), then: confirm hazards spawn and fall (crates), confirm a granted power-up (`PowerUpManager.Instance.Grant(...)` on `Assets/PowerUps/SpeedBoost.asset`) shows a HUD icon, force a Game Over via unprotected hazard contact, confirm `Canvas/GameOverMenu` activates, confirm `Restart` replays cleanly (hazards/powerups reset, score resets).

- [ ] **Step 4: Clean console check**

Run the refresh/console-check sequence — `consoleErrors: 0` through the entire pass above.

- [ ] **Step 5: Clean up**

Exit Play mode, reload `Core.unity` from disk (`open_scene`), delete any stray `Assets/Screenshots/`.

- [ ] **Step 6: No commit expected**

This task is verification-only. If `git status` shows anything, investigate before deciding whether to commit or discard.

---

### Task 8: Import KayKit_Platformer_Pack assets

**Files:**
- Create: `Assets/KayKit_Platformer_Pack/fbx/*.fbx` (blue color variant, ~83 files) + `Assets/KayKit_Platformer_Pack/Textures/platformer_texture.png`

**Interfaces:** none (asset import only).

- [ ] **Step 1: Copy the source files into the project**

```bash
mkdir -p "Assets/KayKit_Platformer_Pack/fbx"
mkdir -p "Assets/KayKit_Platformer_Pack/Textures"
cp "D:/Unity/Aldera_Assets/KayKit_Platformer_Pack_1.0_FREE/KayKit_Platformer_Pack_1.0_FREE/Assets/fbx(unity)/blue/"*.fbx "Assets/KayKit_Platformer_Pack/fbx/"
cp "D:/Unity/Aldera_Assets/KayKit_Platformer_Pack_1.0_FREE/KayKit_Platformer_Pack_1.0_FREE/Textures/platformer_texture.png" "Assets/KayKit_Platformer_Pack/Textures/"
```

(Run from the repo root, `E:/GitHub/Aldratos_Cube_Survival`, via Bash — this is a plain file copy, not live-Editor scripting.)

- [ ] **Step 2: Also copy the pack's `License.txt` for provenance**

```bash
cp "D:/Unity/Aldera_Assets/KayKit_Platformer_Pack_1.0_FREE/KayKit_Platformer_Pack_1.0_FREE/License.txt" "Assets/KayKit_Platformer_Pack/License.txt"
```

- [ ] **Step 3: Refresh and let Unity import**

```
mcp__unity-editor-mcp__recompile
```

(Asset import for a large batch of FBX files can take real time — poll `recompile_status`/`editor_status` for `compiling: false` before proceeding rather than assuming it's instant.)

- [ ] **Step 4: Verify import and check material/texture setup**

Load a representative piece (e.g. `Assets/KayKit_Platformer_Pack/fbx/platform_6x6x1_blue.fbx`) and inspect its `MeshRenderer.sharedMaterials` — confirm materials imported with a real (non-pink/missing) shader and that they reference `platformer_texture.png` (or note if Unity auto-generated per-FBX materials instead of sharing one — either is fine, just confirm nothing is broken/magenta). Render a quick Edit-mode camera preview (same technique used earlier this session — instantiate, position a temp camera, `cam.Render()` to a `RenderTexture`, save PNG, read it back, then clean up all temp objects) of 2-3 pieces to visually confirm textures are applied correctly before proceeding to Task 9.

- [ ] **Step 5: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 6: Commit**

```bash
git add Assets/KayKit_Platformer_Pack
git commit -m "Import KayKit Platformer Pack assets (blue variant)"
```

---

### Task 9: `Level_KayKit.unity` — ground layout and decoration

**Files:**
- Create (via Editor): `Assets/Scenes/Level_KayKit.unity`

**Interfaces:**
- Consumes: KayKit FBX assets (Task 8), the physical footprint constants (Global Constraints).

- [ ] **Step 1: Create the new scene**

```csharp
// create_level_kaykit_scene.cs
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
    UnityEditor.SceneManagement.NewSceneMode.Single);
var root = new GameObject("Environment");
return "Created empty Level_KayKit scene with an Environment root.";
```

- [ ] **Step 2: Build the ground/platform layout matching the footprint**

Using `platform_*_blue.fbx` pieces (the flat, non-slope variants), build a ground surface under the `Environment` root that matches the footprint captured in Global Constraints: combined collider spanning X -7.95..7.95, Z -3.49..3.49, top surface at world Y ≈ 0.25. Use `transform.localScale` to size whichever piece(s) you choose (e.g. a single `platform_6x6x1_blue` scaled non-uniformly, or several pieces placed side by side like the original `Platform_TopLeft/Middle/TopRight` split) — match `Platform_TopMiddle`'s pattern: a `MeshCollider` with `convex = false`, no `Rigidbody`. This is inherently a visual/iterative step — screenshot or Edit-mode-render the result and check the footprint numerically (sum of collider bounds) before moving on, rather than trusting placement by eye alone.

Optionally add 1-2 decorative non-collidable pieces below the ground (mirroring the original's `Platform_BottomLeft/Middle/Right`, which have no `MeshCollider`) for visual depth when the camera angle reveals underneath.

- [ ] **Step 3: Add decoration**

Place a modest set of decorative, non-colliding KayKit pieces around the platform for visual interest — arches (`arch_blue`, `arch_tall_blue`), railings (`railing_straight_single_blue` etc.), a `pipe_straight_A_blue` + `pipe_end_blue` pair (mirrors the existing level's decorative `Pipe` object and reads clearly as Mario-themed), and `signage_arrow_stand_blue` (mirrors the existing `Sign_LeftRight`). None of these need colliders (matching how `Tree1`/`Fence`/`Mushroom` etc. in the original level are pure visual dressing with no collider). No wandering-NPC equivalent is needed for this theme (per spec).

- [ ] **Step 4: Verify the footprint numerically**

```csharp
// verify_kaykit_footprint.cs
var sb = new System.Text.StringBuilder();
float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue, topY = float.MinValue;
foreach (var mc in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
{
    var b = mc.bounds;
    minX = Mathf.Min(minX, b.min.x); maxX = Mathf.Max(maxX, b.max.x);
    minZ = Mathf.Min(minZ, b.min.z); maxZ = Mathf.Max(maxZ, b.max.z);
    topY = Mathf.Max(topY, b.max.y);
}
sb.AppendLine("X range: " + minX + " .. " + maxX + " (target -7.95..7.95)");
sb.AppendLine("Z range: " + minZ + " .. " + maxZ + " (target -3.49..3.49)");
sb.AppendLine("Top Y: " + topY + " (target ~0.25)");
return sb.ToString();
```

Adjust piece scale/position until this is a close match (within roughly 0.5 units is fine — exact parity isn't necessary, matching order-of-magnitude footprint is what keeps `GameManager`'s spawn math and `FallDownTrigger` behaving the same).

- [ ] **Step 5: Save the scene**

```csharp
// save_level_kaykit_scene.cs
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Level_KayKit");
if (!scene.IsValid())
{
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
}
var saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Level_KayKit.unity");
return "SaveScene returned: " + saved;
```

- [ ] **Step 6: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scenes/Level_KayKit.unity Assets/Scenes/Level_KayKit.unity.meta
git commit -m "Add Level_KayKit scene: ground layout and decoration"
```

---

### Task 10: KayKit hazard and pickup prefabs

**Files:**
- Create (via Editor): `Assets/Prefabs/KayKit_Hazard.prefab`, `Assets/Prefabs/KayKit_SpeedBoost.prefab`, `Assets/Prefabs/KayKit_Invincibility.prefab`, `Assets/Prefabs/KayKit_Shield.prefab`

**Interfaces:**
- Consumes: KayKit FBX assets (Task 8), `Hazard.cs`/`PowerUpPickup.cs` (pre-existing), the global `PowerUpDefinition` assets (pre-existing — reused, not duplicated, per Global Constraints).

This mirrors the pattern already proven twice in this project (the original `Crate.prefab`/pickup prefabs, and their rebuild during the powerups plan's Task 10) — including its hard-won lessons: **check the source FBX's baked-in scale before instantiating** (the Platformer Pack FBX assets carried a 100x scale that had to be normalized to `(1,1,1)` after instantiation — check whether KayKit's FBX assets have the same issue and normalize if so), and **persist any runtime-tinted `Material` as a real `.mat` asset** (`AssetDatabase.CreateAsset`) before assigning it to a renderer — a transient, non-persisted `Material` silently loses its reference when `PrefabUtility.SaveAsPrefabAsset` runs.

- [ ] **Step 1: Check KayKit FBX baked-in scale**

```csharp
// check_kaykit_scale.cs
var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KayKit_Platformer_Pack/fbx/bomb_A_blue.fbx");
return "localScale=" + fbx.transform.localScale;
```

If not `(1,1,1)`, plan to normalize each hazard/pickup prefab's root scale after instantiating (same fix applied during the powerups plan).

- [ ] **Step 2: Build the hazard prefab**

```csharp
// build_kaykit_hazard.cs
var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KayKit_Platformer_Pack/fbx/bomb_A_blue.fbx");
var instance = GameObject.Instantiate(fbx);
instance.name = "KayKit_Hazard";
instance.tag = "Hazard";
instance.transform.localScale = Vector3.one; // adjust if Step 1 found a different baked-in scale

var rb = instance.AddComponent<Rigidbody>();
rb.mass = 1f;

var meshFilter = instance.GetComponentInChildren<MeshFilter>();
var meshCollider = instance.AddComponent<MeshCollider>();
meshCollider.sharedMesh = meshFilter.sharedMesh;
meshCollider.convex = true;

var hazard = instance.AddComponent<Hazard>();
var so = new UnityEditor.SerializedObject(hazard);
// Reuse the existing crate-breaking particle effect rather than authoring a
// new one - an accepted simplification for this pass (Global Constraints).
var existingCrate = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Crate.prefab");
var breakingEffectField = so.FindProperty("breakingEffect");
var crateHazard = existingCrate.GetComponent<Hazard>();
var crateSo = new UnityEditor.SerializedObject(crateHazard);
breakingEffectField.objectReferenceValue = crateSo.FindProperty("breakingEffect").objectReferenceValue;
so.ApplyModifiedProperties();

var cinemachineSource = instance.AddComponent<Unity.Cinemachine.CinemachineImpulseSource>();

var prefabPath = "Assets/Prefabs/KayKit_Hazard.prefab";
var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
GameObject.DestroyImmediate(instance);
return "Created KayKit_Hazard.prefab";
```

(If `bomb_A_blue.fbx`'s convex `MeshCollider` throws during import — unlikely for a simple low-poly shape, but check — fall back to a `BoxCollider` sized to the renderer's bounds, matching the fallback already documented in the powerups plan.)

- [ ] **Step 3: Verify the hazard prefab**

Confirm: `tag == "Hazard"`, has `Rigidbody`, `MeshCollider.convex == true` with non-null `sharedMesh`, has `Hazard` component with non-null `breakingEffect`, has `CinemachineImpulseSource`.

- [ ] **Step 4: Build the three pickup prefabs**

Same pattern as the powerups plan's Task 10 `BuildPickupPrefab`, reused here with KayKit source meshes and — critically — **wired to the existing global `PowerUpDefinition` assets, not new ones**:

```csharp
// build_kaykit_pickups.cs
var sb = new System.Text.StringBuilder();
System.IO.Directory.CreateDirectory("Assets/KayKit_Platformer_Pack/Materials");

GameObject BuildPickupPrefab(string fbxPath, string prefabName, string tint, string definitionAssetPath)
{
    var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
    var instance = GameObject.Instantiate(fbx);
    instance.name = prefabName;
    instance.tag = "PowerUp";
    instance.transform.localScale = Vector3.one; // adjust if Task 8/Step 1 found a different baked-in scale

    var rb = instance.AddComponent<Rigidbody>();
    rb.mass = 1f;

    var meshFilter = instance.GetComponentInChildren<MeshFilter>();
    var meshCollider = instance.AddComponent<MeshCollider>();
    meshCollider.sharedMesh = meshFilter.sharedMesh;
    meshCollider.convex = true;

    var pickup = instance.AddComponent<PowerUpPickup>();
    var pickupSo = new UnityEditor.SerializedObject(pickup);
    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(definitionAssetPath);
    pickupSo.FindProperty("definition").objectReferenceValue = definition;
    pickupSo.ApplyModifiedProperties();

    var renderer = instance.GetComponentInChildren<Renderer>();
    if (renderer != null && renderer.sharedMaterial != null && !string.IsNullOrEmpty(tint))
    {
        var mat = new Material(renderer.sharedMaterial);
        ColorUtility.TryParseHtmlString(tint, out var color);
        mat.color = color;
        var matPath = "Assets/KayKit_Platformer_Pack/Materials/" + prefabName + "_Material.mat";
        UnityEditor.AssetDatabase.CreateAsset(mat, matPath);
        renderer.sharedMaterial = mat;
    }

    var prefabPath = "Assets/Prefabs/" + prefabName + ".prefab";
    var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
    GameObject.DestroyImmediate(instance);
    return prefab;
}

// KayKit pieces already ship distinct per-piece colors in their own texture
// atlas region, so tinting is likely unnecessary here (unlike the original
// Coin/Star/Heart_Full pieces, which shared one base color needing an
// explicit tint per power-up type) - pass null/empty tint first and check
// the result visually; only add a tint string if the pieces render visually
// too similar to distinguish at a glance.
var speedPrefab = BuildPickupPrefab("Assets/KayKit_Platformer_Pack/fbx/diamond_blue.fbx", "KayKit_SpeedBoost", null, "Assets/PowerUps/SpeedBoost.asset");
sb.AppendLine("Created " + speedPrefab.name + ".prefab");

var invincibilityPrefab = BuildPickupPrefab("Assets/KayKit_Platformer_Pack/fbx/star_blue.fbx", "KayKit_Invincibility", null, "Assets/PowerUps/Invincibility.asset");
sb.AppendLine("Created " + invincibilityPrefab.name + ".prefab");

var shieldPrefab = BuildPickupPrefab("Assets/KayKit_Platformer_Pack/fbx/heart_blue.fbx", "KayKit_Shield", null, "Assets/PowerUps/Shield.asset");
sb.AppendLine("Created " + shieldPrefab.name + ".prefab");

UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
```

- [ ] **Step 5: Verify all three pickup prefabs**

For each: `tag == "PowerUp"`, has `Rigidbody`, `MeshCollider.convex == true` with non-null `sharedMesh`, `PowerUpPickup.definition` points at the correct **shared global** definition asset (`Assets/PowerUps/SpeedBoost.asset` etc. — not a new KayKit-specific one), `Renderer.sharedMaterial` is non-null. Render a quick Edit-mode preview of all three side by side (or read the game view after a brief Edit-mode instantiate, matching the technique from Task 8/Step 4) and visually confirm they're distinguishable from each other — if not, revisit the tint decision from Step 4's comment.

- [ ] **Step 6: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 7: Commit**

```bash
git add Assets/Prefabs/KayKit_Hazard.prefab Assets/Prefabs/KayKit_Hazard.prefab.meta Assets/Prefabs/KayKit_SpeedBoost.prefab Assets/Prefabs/KayKit_SpeedBoost.prefab.meta Assets/Prefabs/KayKit_Invincibility.prefab Assets/Prefabs/KayKit_Invincibility.prefab.meta Assets/Prefabs/KayKit_Shield.prefab Assets/Prefabs/KayKit_Shield.prefab.meta Assets/KayKit_Platformer_Pack/Materials
git commit -m "Add KayKit hazard and pickup prefabs"
```

---

### Task 11: `LevelTheme_KayKit.asset`, `LevelInfo`, registry + Build Settings

**Files:**
- Create (via Editor): `Assets/Levels/LevelTheme_KayKit.asset`
- Modify (via Editor): `Assets/Scenes/Level_KayKit.unity` (add `LevelInfo`), `Assets/Levels/LevelRegistry.asset` (add entry), Build Settings

**Interfaces:**
- Consumes: Tasks 9-10's scene and prefabs.

- [ ] **Step 1: Create `LevelTheme_KayKit.asset`**

```csharp
// create_leveltheme_kaykit.cs
var theme = ScriptableObject.CreateInstance<LevelTheme>();
var so = new UnityEditor.SerializedObject(theme);
so.FindProperty("displayName").stringValue = "KayKit";
so.FindProperty("hazardPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KayKit_Hazard.prefab");
so.FindProperty("speedBoostPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KayKit_SpeedBoost.prefab");
so.FindProperty("invincibilityPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KayKit_Invincibility.prefab");
so.FindProperty("shieldPrefab").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KayKit_Shield.prefab");
so.ApplyModifiedProperties();

UnityEditor.AssetDatabase.CreateAsset(theme, "Assets/Levels/LevelTheme_KayKit.asset");
UnityEditor.AssetDatabase.SaveAssets();
return "Created LevelTheme_KayKit.asset";
```

- [ ] **Step 2: Add `LevelInfo` to Level_KayKit's Environment root**

```csharp
// wire_level_kaykit_info.cs
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level_KayKit.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
GameObject environment = null;
foreach (var root in scene.GetRootGameObjects())
{
    if (root.name == "Environment") environment = root;
}
if (environment == null) return "ERROR: Environment root not found in Level_KayKit scene";

var info = environment.AddComponent<LevelInfo>();
var so = new UnityEditor.SerializedObject(info);
so.FindProperty("theme").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelTheme>("Assets/Levels/LevelTheme_KayKit.asset");
so.ApplyModifiedProperties();

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Added LevelInfo to Level_KayKit, wired to LevelTheme_KayKit.";
```

- [ ] **Step 3: Add the KayKit entry to `LevelRegistry.asset`**

```csharp
// add_kaykit_to_registry.cs
var registry = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelRegistry>("Assets/Levels/LevelRegistry.asset");
var so = new UnityEditor.SerializedObject(registry);
var entriesProp = so.FindProperty("levelEntries");
var newIndex = entriesProp.arraySize;
entriesProp.arraySize = newIndex + 1;
var entry = entriesProp.GetArrayElementAtIndex(newIndex);
entry.FindPropertyRelative("sceneName").stringValue = "Level_KayKit";
entry.FindPropertyRelative("displayName").stringValue = "KayKit";
so.ApplyModifiedProperties();
UnityEditor.AssetDatabase.SaveAssets();
return "Added Level_KayKit entry to LevelRegistry (now " + entriesProp.arraySize + " entries).";
```

- [ ] **Step 4: Register `Level_KayKit` in Build Settings**

```csharp
// register_kaykit_build_settings.cs
var existing = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
existing.Add(new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/Level_KayKit.unity", true));
UnityEditor.EditorBuildSettings.scenes = existing.ToArray();
return "Build Settings now has " + existing.Count + " scenes.";
```

- [ ] **Step 5: Open Core.unity again (Step 2 switched the active scene), refresh, verify clean console**

```csharp
// reopen_core.cs
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Core.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
return "Reopened Core.unity as the active scene.";
```

Run the refresh/console-check sequence.

- [ ] **Step 6: Commit**

```bash
git add Assets/Levels/LevelTheme_KayKit.asset Assets/Levels/LevelTheme_KayKit.asset.meta Assets/Scenes/Level_KayKit.unity Assets/Levels/LevelRegistry.asset ProjectSettings/EditorBuildSettings.asset
git commit -m "Add LevelTheme_KayKit, wire Level_KayKit's LevelInfo, register in Level Select and Build Settings"
```

---

### Task 12: End-to-end live verification — KayKit theme + theme-switch regression

**Files:** none (verification only)

- [ ] **Step 1: Full gameplay pass under the KayKit theme**

Same shape as Task 7: enter Play mode, click Play, pick the KayKit tile in Level Select, confirm `Level_KayKit` loaded and `Level_Grass` did NOT (only one Level scene should ever be loaded at a time), park the player safely, confirm the KayKit hazard spawns/falls/ends a run on contact, confirm each KayKit pickup spawns/falls/lands/grants its effect/expires or consumes correctly (reuse the exact verification approach from the original powerups plan's Task 12 — direct `PowerUpManager.Instance.Grant()` calls plus the inactive-aware `Find` helper for `Canvas/GameOverMenu`), confirm the HUD icon shown matches the GLOBAL icon for that power-up type (same icon as under the Grass theme — this is expected per Global Constraints, not a bug).

- [ ] **Step 2: Theme-switch regression (scripted, not via UI — no round-trip UI path exists yet, see Global Constraints)**

With the KayKit level currently loaded from Step 1, directly exercise the load/unload path `LevelSelect` uses, twice, via script. `SelectLevel()` starts its own coroutine internally and returns immediately, so each switch is a separate eval call followed by a separate verification call after a short wait — the same two-call pattern used throughout this project's live Play-mode testing.

Switch to Grass:

```csharp
// switch_to_grass.cs
var levelSelect = UnityEngine.Object.FindAnyObjectByType<LevelSelect>(FindObjectsInactive.Include);
levelSelect.SelectLevel("Level_Grass");
return "Called SelectLevel(Level_Grass)";
```

Wait, then verify exactly one `Environment` root exists and it's in `Level_Grass`:

```csharp
// check_loaded_environments.cs
var found = new System.Collections.Generic.List<string>();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
{
    var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    if (!s.isLoaded) continue;
    foreach (var root in s.GetRootGameObjects())
    {
        if (root.name == "Environment") found.Add(s.name);
    }
}
return "Environment root(s) found in loaded scene(s): " + string.Join(", ", found) + " (expect exactly one entry)";
```

Then switch to KayKit:

```csharp
// switch_to_kaykit.cs
var levelSelect = UnityEngine.Object.FindAnyObjectByType<LevelSelect>(FindObjectsInactive.Include);
levelSelect.SelectLevel("Level_KayKit");
return "Called SelectLevel(Level_KayKit)";
```

Wait, then re-run `check_loaded_environments.cs` again — expect the single entry to now read `Level_KayKit`, with `Level_Grass` no longer present in the loaded-scene list at all (confirms the unload actually happened, not just an additive pile-up).

- [ ] **Step 3: Clean console check**

Run the refresh/console-check sequence — `consoleErrors: 0` through both passes.

- [ ] **Step 4: Clean up**

Exit Play mode, reload `Core.unity` from disk, delete any stray `Assets/Screenshots/`.

- [ ] **Step 5: No commit expected**

Verification-only. If `git status` shows anything, investigate before deciding whether to commit or discard.

---

## Summary of what ships

Two playable themes (Grass, KayKit) selectable from a Level Select screen between Main Menu and gameplay, built on a `Core` scene + additive `Level_*` scenes architecture. Adding a third theme later is: one new Level scene built to the same physical footprint, one new `LevelTheme` asset, one new `LevelRegistry` entry and Build Settings line — no changes to `GameManager`, `PowerUpManager`, `Player`, or the powerup system. Sequential level unlocking (explicitly deferred) would add an unlock-condition field to `LevelTheme` and a check in `LevelSelect`'s tile population, without needing to touch anything else.
