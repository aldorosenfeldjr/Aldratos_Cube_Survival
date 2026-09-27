# Character & Companion Selection Experience Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the placeholder character/companion selection (cube-primitive preview, frozen companion animation, plain text menu buttons, flat particle background) with an interactive, curiosity-driving experience: a real player/companion look you can drag-rotate, an animated low-cost "out of focus" menu background that shifts with a camera move when you open a category, Main Menu teaser panels that spoil the grid, and — as a final skin pass — the Hyper Casual UI Pack applied to just this screen.

**Architecture:** Nine independently-testable increments, ordered so the functional/interactive pieces (background rig, drag-rotate previews, real meshes/animation, flipped layout, teaser widgets, open/close transition) are built and smoke-tested with the project's existing plain UI look first; the external art-pack skin is a pure re-skin applied last, so the whole feature isn't blocked on an Asset Store download from day one. Everything is additive on top of the existing economy-step-3 selection system (`Assets/Scripts/Selection/`, `Assets/Scripts/Economy/`) — no change to `UnlockService`/`SaveService`/`Wallet`/the catalog's public shape.

**Tech Stack:** Unity 6000.6.0f1 (URP), LeanTween (existing, only tweening library used), Unity Input System (existing `IDragHandler` UI events), TextMeshPro, Unity Editor MCP tools (`mcp__unity-editor-mcp__*`) for every scene/prefab/asset edit and for driving `PlaySmokeTest`. No automated unit-test framework applies here (this is Play Mode UI/visual work) — verification is the existing `PlaySmokeTest` (`Tools/Smoke Test/Run Play Smoke Test`), extended task-by-task, plus manual play checks for purely subjective visual calls, matching how `2026-09-20-powerup-visual-ui-overhaul.md` and the original economy-step-3 selection screens were both verified.

**Spec:** `docs/superpowers/specs/2026-09-27-selection-experience-redesign-design.md`

## Global Constraints

- Target platforms are PC + mobile (iOS/Android) — the background must never depend on real-time depth-of-field (URP-Mobile ships no DoF/SSAO, PC-only, per `docs/pending-and-restructuring-plan.md`); the "out of focus" look comes from rendering at reduced resolution only.
- LeanTween only — no new tweening dependency, matching every other animated script in the project.
- No change to `UnlockService`, `SaveService`, `Wallet`, or `UnlockableDefinition`/`CharacterDefinition`/`CompanionDefinition`'s public shape beyond what each task explicitly adds.
- Never hand-edit a generated `CharacterDefinition`/`CompanionDefinition` asset — change the row in `CharacterBuilder`/`CompanionBuilder` and re-run its `Tools >` menu item.
- The Hyper Casual UI Pack (Task 8) restyles only the pieces this feature touches (`SelectionScreen`, `CompanionSelection`, the two teasers) — the shared `MenuButton`/`MenuLabel` prefabs used by every other menu are not touched by this plan.
- After every task: `mcp__unity-editor-mcp__recompile`, then `mcp__unity-editor-mcp__console` (level=error) confirming 0 errors, before committing.
- `PlaySmokeTest` must stay green end-to-end after every task that touches it — a task that invalidates an existing `Check(...)` updates that same check in the same task, never leaves it broken for a later task to discover.

## Review Focus

- A locked/unowned item that lands in a teaser's 3-item spoiler row must still show its lock/price badge, not look selectable — Task 6 must reuse `UnlockTile`'s existing lock logic, not a stripped-down copy that forgets it.
- Double-tapping a teaser (or Back) while its own open/close transition is still running must not open two `SelectionScreen`s or start overlapping `MenuBackgroundRig` pose tweens — Task 7 disables the tapped control for the duration of its own transition.
- `PreviewIdleLoop` (Task 4) must never `CrossFade` to `"Walk"`/`"Run"` on a companion whose `Wanders` is `false` (those states don't exist on that companion's Animator Controller) — gated by the same flag `CompanionSpawner` already uses for `CatWanderer`.
- The Main Menu at the shortest audited canvas (864 units tall, the existing `MainMenuLayoutProblems` check) must still fit both teaser panels alongside Play/Exit/ClearHighScore/RemoveAds/SoundToggle/GemCounter with no overlaps — teasers are visually larger than the plain text buttons they replace, so Task 6 re-runs this exact check.
- The app backgrounded or the Main Menu destroyed mid-transition (mobile pause, or leaving Play Mode) must not leave `MenuBackgroundRig`'s pose/drift tweens running against a destroyed camera, nor the Main Menu `CanvasGroup` stuck below full alpha — Task 1 and Task 7 give `MenuBackgroundRig` and the transition code the same defensive `OnDisable`/`OnDestroy` cancellation `SelectionScreen` already uses for its own preview stage.

---

## Task 1: `MenuShowcaseStage` prefab + `MenuBackgroundRig`

**Files:**
- Create: `Assets/Scripts/MenuBackgroundRig.cs`
- Create: `Assets/Prefabs/MenuShowcaseStage.prefab`
- Modify (scene wiring, via MCP): `Assets/Scenes/Core.unity` — new root object `MenuBackgroundRig` (own root object, like `TimeScaleController`; no `DontDestroyOnLoad` needed since Core never reloads during menu navigation), and a full-screen `RawImage` inserted behind the Main Menu's existing background/particles.

**Interfaces:**
- Produces: `MenuBackgroundRig.Instance` (static), `MoveToMainMenu()`, `MoveToCharacters()`, `MoveToCompanions()`. Consumed by Task 7 (open/close transition).

- [ ] **Step 1: Write `MenuBackgroundRig`**

```csharp
// Assets/Scripts/MenuBackgroundRig.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a small diorama into a low-resolution RenderTexture behind the Main Menu's UI — the reduced
/// resolution is the "out of focus" look, cheap on every platform (no real-time depth-of-field). Own root
/// object in Core, like <c>TimeScaleController</c>: never needs to survive a scene reload, since menu
/// navigation never leaves Core. A slow idle drift plays under whichever named pose is current;
/// <see cref="MoveTo"/> eases to a new pose, cancelling any pose tween already in flight.
/// </summary>
public class MenuBackgroundRig : MonoBehaviour
{
    [System.Serializable]
    public struct Pose
    {
        public Vector3 position;
        public Vector3 eulerAngles;
        public float fieldOfView;
    }

    private const string TweenId = "MenuBackgroundRigPose";
    private const string DriftId = "MenuBackgroundRigDrift";

    [SerializeField] private GameObject stagePrefab;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField, Range(2, 8)] private int downscaleFactor = 4;
    [SerializeField] private float poseTweenDuration = 0.7f;
    [SerializeField] private Pose mainMenuIdle = new Pose { position = new Vector3(0f, 1.6f, -6f), eulerAngles = new Vector3(6f, 0f, 0f), fieldOfView = 32f };
    [SerializeField] private Pose charactersOpen = new Pose { position = new Vector3(2.4f, 1.4f, -4.6f), eulerAngles = new Vector3(8f, -18f, 0f), fieldOfView = 28f };
    [SerializeField] private Pose companionsOpen = new Pose { position = new Vector3(-2.4f, 1.2f, -4.6f), eulerAngles = new Vector3(8f, 18f, 0f), fieldOfView = 28f };
    [SerializeField] private float driftAmplitude = 0.15f;
    [SerializeField] private float driftSpeed = 0.15f;

    public static MenuBackgroundRig Instance { get; private set; }

    private Camera stageCamera;
    private RenderTexture texture;
    private Pose currentPose;
    private Pose basePose;

    private void Awake()
    {
        Instance = this;
        Instantiate(stagePrefab, transform);

        var cameraObject = new GameObject("MenuBackgroundCamera");
        cameraObject.transform.SetParent(transform, false);
        stageCamera = cameraObject.AddComponent<Camera>();
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = new Color(0.1f, 0.12f, 0.2f, 1f);
        stageCamera.nearClipPlane = 0.3f;
        stageCamera.farClipPlane = 60f;
        stageCamera.allowHDR = false;
        stageCamera.allowMSAA = false;

        RebuildTexture();
        SetPoseImmediate(mainMenuIdle);
    }

    private void OnEnable()
    {
        StartDrift();
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject, TweenId);
        LeanTween.cancel(gameObject, DriftId);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (backgroundImage != null)
        {
            backgroundImage.texture = null;
        }
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }

    private void RebuildTexture()
    {
        var width = Mathf.Max(4, Screen.width / downscaleFactor);
        var height = Mathf.Max(4, Screen.height / downscaleFactor);
        texture = new RenderTexture(width, height, 16) { name = "MenuBackgroundTexture", filterMode = FilterMode.Bilinear };
        stageCamera.targetTexture = texture;
        backgroundImage.texture = texture;
    }

    /// <summary>Eases to the given pose, cancelling any pose tween already running — never stacks.</summary>
    public void MoveTo(Pose pose)
    {
        LeanTween.cancel(gameObject, TweenId);
        var start = currentPose;
        LeanTween.value(gameObject, 0f, 1f, poseTweenDuration)
            .setId(TweenId)
            .setEaseInOutSine()
            .setOnUpdate((float t) => basePose = LerpPose(start, pose, t));
        currentPose = pose;
    }

    public void MoveToMainMenu() => MoveTo(mainMenuIdle);
    public void MoveToCharacters() => MoveTo(charactersOpen);
    public void MoveToCompanions() => MoveTo(companionsOpen);

    private void SetPoseImmediate(Pose pose)
    {
        currentPose = pose;
        basePose = pose;
    }

    private static Pose LerpPose(Pose a, Pose b, float t)
    {
        return new Pose
        {
            position = Vector3.Lerp(a.position, b.position, t),
            eulerAngles = Vector3.Lerp(a.eulerAngles, b.eulerAngles, t),
            fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t),
        };
    }

    private void StartDrift()
    {
        LeanTween.value(gameObject, 0f, 1f, 1f)
            .setId(DriftId)
            .setLoopClamp()
            .setOnUpdate((float unused) =>
            {
                var t = Time.unscaledTime * driftSpeed;
                var drift = new Vector3(Mathf.Sin(t) * driftAmplitude, Mathf.Sin(t * 0.7f) * driftAmplitude * 0.5f, 0f);
                stageCamera.transform.localPosition = basePose.position + drift;
                stageCamera.transform.localEulerAngles = basePose.eulerAngles;
                stageCamera.fieldOfView = basePose.fieldOfView;
            });
    }
}
```

- [ ] **Step 2: Build the starter `MenuShowcaseStage` diorama**

Write a one-off Editor script to the scratchpad and run it via `mcp__unity-editor-mcp__eval_file` (this is a hand-authored starter arrangement, not a repeatable table-driven builder — final set-dressing is the user's own pass, per the spec):

```csharp
using UnityEditor;
using UnityEngine;

var arch = (GameObject)AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KayKit_Platformer_Pack/fbx(unity)/blue/arch_blue.fbx");
var platform = (GameObject)AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KayKit_Platformer_Pack/fbx(unity)/blue/platform_4x4x1_blue.fbx");
var flag = (GameObject)AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KayKit_Platformer_Pack/fbx(unity)/blue/flag_A_blue.fbx");

var root = new GameObject("MenuShowcaseStage");
var platformInstance = (GameObject)PrefabUtility.InstantiatePrefab(platform, root.transform);
platformInstance.transform.localPosition = new Vector3(0f, -1f, 0f);
var archInstance = (GameObject)PrefabUtility.InstantiatePrefab(arch, root.transform);
archInstance.transform.localPosition = new Vector3(0f, -0.5f, 2f);
var flagInstance = (GameObject)PrefabUtility.InstantiatePrefab(flag, root.transform);
flagInstance.transform.localPosition = new Vector3(-1.5f, -0.5f, 0.5f);

var light = new GameObject("StageLight", typeof(Light));
light.transform.SetParent(root.transform, false);
light.transform.localPosition = new Vector3(2f, 3f, -2f);
light.transform.localEulerAngles = new Vector3(40f, -30f, 0f);
var lightComponent = light.GetComponent<Light>();
lightComponent.type = LightType.Directional;
lightComponent.color = new Color(1f, 0.93f, 0.8f);
lightComponent.intensity = 1.2f;

PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/MenuShowcaseStage.prefab");
Object.DestroyImmediate(root);
Debug.Log("MenuShowcaseStage prefab created.");
```

- [ ] **Step 3: Wire `MenuBackgroundRig` into Core**

Using `mcp__unity-editor-mcp__create_gameobject` (root of Core scene, name `MenuBackgroundRig`), `add_component` for `MenuBackgroundRig`, then `set_component_properties` to assign `stagePrefab` = `Assets/Prefabs/MenuShowcaseStage.prefab`. Under the Main Menu's Canvas, insert a new full-screen `RawImage` (`create_gameobject` with `RectTransform`+`RawImage`, anchors stretched 0-1, positioned as the first child so `MenuBackgroundEffect`'s particles and the rest of the Main Menu UI render on top of it) and wire it into `MenuBackgroundRig.backgroundImage`.

- [ ] **Step 4: Manual play verification**

`mcp__unity-editor-mcp__editor_play`, open the Main Menu, `mcp__unity-editor-mcp__capture_game_view` — confirm the diorama renders behind the UI, softly out of focus from the resolution drop, with a slow idle drift. If the low-res-only look doesn't read as "out of focus" enough, note this for a possible follow-up box-blur pass (not built speculatively here, per the spec).

- [ ] **Step 5: Recompile, verify console, commit**

```bash
git add Assets/Scripts/MenuBackgroundRig.cs Assets/Scripts/MenuBackgroundRig.cs.meta Assets/Prefabs/MenuShowcaseStage.prefab*
git commit -m "Add MenuBackgroundRig: animated low-res menu background"
```

---

## Task 2: `PreviewRotator` — drag-to-rotate on both selection previews

**Files:**
- Create: `Assets/Scripts/Selection/PreviewRotator.cs`
- Modify: `Assets/Scripts/Selection/SelectionScreen.cs` (`ShowPreview`, plus a new serialized field)
- Modify (prefab wiring, via MCP): `Assets/Prefabs/UI/SelectionScreen.prefab` and `Assets/Prefabs/UI/CompanionSelection.prefab` — add `PreviewRotator` to each prefab's `InfoPanel/PreviewFrame/Preview` object.
- Modify: `Assets/Editor/PlaySmokeTest.Selection.cs`, `Assets/Editor/PlaySmokeTest.Companions.cs`

**Interfaces:**
- Produces: `PreviewRotator` (public `Transform Target` property, `IDragHandler.OnDrag`). Consumed by `SelectionScreen`.

- [ ] **Step 1: Write `PreviewRotator`**

```csharp
// Assets/Scripts/Selection/PreviewRotator.cs
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag-to-rotate for a selection preview. Lives on the same RawImage the preview camera renders into.
/// Rotates <see cref="Target"/> — the previewed item itself, not the stage/camera — around world up while
/// the pointer drags across it. Replaces the previous "no rotation" preview behaviour.
/// </summary>
public class PreviewRotator : MonoBehaviour, IDragHandler
{
    [SerializeField] private float degreesPerPixel = 0.3f;

    public Transform Target { get; set; }

    public void OnDrag(PointerEventData eventData)
    {
        if (Target == null)
        {
            return;
        }
        Target.Rotate(Vector3.up, -eventData.delta.x * degreesPerPixel, Space.World);
    }
}
```

- [ ] **Step 2: Wire it into `SelectionScreen`**

In `Assets/Scripts/Selection/SelectionScreen.cs`, add a field and set the target whenever the preview object is (re)created:

```csharp
    [SerializeField] private PreviewRotator previewRotator;
```

```csharp
    private void ShowPreview(UnlockableDefinition definition)
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
        previewObject = definition.CreatePreview(stage.transform);
        previewObject.transform.localPosition = Vector3.zero;
        previewRotator.Target = previewObject.transform;
    }
```

- [ ] **Step 3: Add `PreviewRotator` to both prefabs**

`mcp__unity-editor-mcp__add_component` on `InfoPanel/PreviewFrame/Preview` in both `Assets/Prefabs/UI/SelectionScreen.prefab` and `Assets/Prefabs/UI/CompanionSelection.prefab`, then wire each `SelectionScreen` component instance's `previewRotator` field to that same object via `set_component_properties`.

- [ ] **Step 4: Update the existing "static preview" checks**

`Assets/Editor/PlaySmokeTest.Selection.cs` needs `using UnityEngine.EventSystems;` added to its `using` block. Replace the block at lines 205–209:

```csharp
        var stage = GameObject.Find("SelectionPreviewStage");
        var cube = stage != null ? stage.transform.Find("Preview_" + common.Id) : null;
        var before = cube != null ? cube.rotation : Quaternion.identity;
        yield return 0.5f;
        Check("preview is static (no rotation)", cube != null && Quaternion.Angle(before, cube.rotation) < 0.01f);
```

with:

```csharp
        var stage = GameObject.Find("SelectionPreviewStage");
        var cube = stage != null ? stage.transform.Find("Preview_" + common.Id) : null;
        var rotator = screenTransform.Find("InfoPanel/PreviewFrame/Preview").GetComponent<PreviewRotator>();
        var before = cube != null ? cube.rotation : Quaternion.identity;
        rotator.OnDrag(new PointerEventData(EventSystem.current) { delta = new Vector2(120f, 0f) });
        Check("dragging the preview rotates it", cube != null && Quaternion.Angle(before, cube.rotation) > 1f);
```

In `Assets/Editor/PlaySmokeTest.Companions.cs`, replace the block at lines 166–173 (the `previewChange < 0.000001f` static check) with the same drag check, using `Preview_comp.pig` and `screenTransform` (the companion screen's transform already in scope there).

- [ ] **Step 5: Recompile, run the smoke test, verify the new checks pass, commit**

`mcp__unity-editor-mcp__recompile`, confirm 0 errors, `Tools/Smoke Test/Run Play Smoke Test`, read console tail 2 and confirm "dragging the preview rotates it" passes for both screens.

```bash
git add Assets/Scripts/Selection/PreviewRotator.cs Assets/Scripts/Selection/PreviewRotator.cs.meta Assets/Scripts/Selection/SelectionScreen.cs Assets/Prefabs/UI/SelectionScreen.prefab* Assets/Prefabs/UI/CompanionSelection.prefab* Assets/Editor/PlaySmokeTest.Selection.cs Assets/Editor/PlaySmokeTest.Companions.cs
git commit -m "Add drag-to-rotate on both selection previews"
```

---

## Task 3: Character preview uses the real player mesh

**Files:**
- Modify: `Assets/Scripts/Economy/CharacterDefinition.cs`
- Modify: `Assets/Editor/CharacterBuilder.cs`

**Interfaces:**
- Produces: `CharacterDefinition.previewMesh` (new serialized field, assigned by the builder). `CreatePreview` no longer uses `GameObject.CreatePrimitive`.

- [ ] **Step 1: Add the field and use it in `CreatePreview`**

```csharp
// Assets/Scripts/Economy/CharacterDefinition.cs
using UnityEngine;

public class CharacterDefinition : UnlockableDefinition
{
    [SerializeField] private Material material;
    [SerializeField] private Mesh previewMesh;

    public Material Material => material;

    public override Color TileColor => material != null ? material.color : Color.white;

    public override GameObject CreatePreview(Transform parent)
    {
        var previewObject = new GameObject("Preview_" + Id, typeof(MeshFilter), typeof(MeshRenderer));
        previewObject.GetComponent<MeshFilter>().sharedMesh = previewMesh;
        previewObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        previewObject.transform.SetParent(parent, false);
        return previewObject;
    }
}
```

- [ ] **Step 2: Have the builder assign the real player mesh**

In `Assets/Editor/CharacterBuilder.cs`, add a constant and assign it in `BuildDefinition`:

```csharp
    private const string PlayerMeshPath = "Assets/Platformer Pack/FBX/YellowBox.fbx";
```

```csharp
        fields.FindProperty("material").objectReferenceValue = BuildMaterial(row);
        fields.FindProperty("previewMesh").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Mesh>(PlayerMeshPath);
        fields.ApplyModifiedPropertiesWithoutUndo();
```

- [ ] **Step 3: Rebuild the character assets**

`mcp__unity-editor-mcp__menu` → `Tools/Characters/Rebuild Character Assets`. Confirm via console: "Rebuilt 12 character rows in the unlock catalog."

- [ ] **Step 4: Recompile, run the smoke test, verify existing preview checks still pass**

The existing "preview renders the focused character (blue box)" / "preview follows focus (red box)" checks (`PlaySmokeTest.Selection.cs` ~196–204) read pixel colour from the render, not the mesh shape, so they should keep passing unchanged with the new mesh — confirm this by running the full smoke test rather than assuming it.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Economy/CharacterDefinition.cs Assets/Editor/CharacterBuilder.cs Assets/Characters
git commit -m "Character preview uses the real player mesh instead of a primitive cube"
```

---

## Task 4: Companion preview animates instead of freezing (+ cat colour fix)

**Files:**
- Create: `Assets/Scripts/Selection/PreviewIdleLoop.cs`
- Modify: `Assets/Scripts/Economy/CompanionDefinition.cs`
- Modify (data, via MCP, only if the diagnostic in Step 2 finds it's needed): the cat's material or `Assets/Prefabs/MenuShowcaseStage.prefab`'s/preview stage's lighting.
- Modify: `Assets/Editor/PlaySmokeTest.Companions.cs`

**Interfaces:**
- Produces: `PreviewIdleLoop.Begin(Animator, bool canWander)`. Consumed by `CompanionDefinition.CreatePreview`.

- [ ] **Step 1: Write `PreviewIdleLoop`**

```csharp
// Assets/Scripts/Selection/PreviewIdleLoop.cs
using UnityEngine;

/// <summary>
/// Keeps a companion's preview alive: loops Idle_A/Idle_B like <see cref="CatWanderer"/> does at rest, and for
/// companions that can wander, occasionally plays a short in-place Walk cycle too (root motion disabled, so the
/// model never leaves its spot on the preview stage). Replaces freezing the Animator on one still frame.
/// </summary>
public class PreviewIdleLoop : MonoBehaviour
{
    private const float CrossFadeTime = 0.2f;

    [SerializeField] private float idleMinTime = 2f;
    [SerializeField] private float idleMaxTime = 4f;
    [SerializeField] private float walkDuration = 1.5f;

    private Animator animator;
    private bool wanders;
    private float timer;
    private bool walking;

    public void Begin(Animator targetAnimator, bool canWander)
    {
        animator = targetAnimator;
        wanders = canWander;
        animator.applyRootMotion = false;
        animator.speed = 1f;
        PlayIdle();
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }
        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }
        if (!walking && wanders && Random.value < 0.5f)
        {
            walking = true;
            timer = walkDuration;
            animator.CrossFade("Walk", CrossFadeTime);
        }
        else
        {
            PlayIdle();
        }
    }

    private void PlayIdle()
    {
        walking = false;
        timer = Random.Range(idleMinTime, idleMaxTime);
        animator.CrossFade(Random.value < 0.5f ? "Idle_A" : "Idle_B", CrossFadeTime);
    }
}
```

- [ ] **Step 2: Use it in `CompanionDefinition.CreatePreview`, remove the freeze**

```csharp
    public override GameObject CreatePreview(Transform parent)
    {
        var instance = Instantiate(prefab, parent);
        instance.name = "Preview_" + Id;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(0f, PreviewYaw, 0f);

        var animator = instance.GetComponent<Animator>();
        if (animator != null)
        {
            instance.AddComponent<PreviewIdleLoop>().Begin(animator, wanders);
        }
        CompanionFitter.CentredOn(instance, PreviewSize, parent.position);
        return instance;
    }
```

- [ ] **Step 3: Diagnose and fix the cat's colour**

Run via `mcp__unity-editor-mcp__eval`:

```csharp
var cat = AssetDatabase.LoadAssetAtPath<CompanionDefinition>("Assets/Companions/comp_cat.asset");
var renderer = cat.Prefab.GetComponentInChildren<SkinnedMeshRenderer>();
foreach (var mat in renderer.sharedMaterials)
{
    Debug.Log(mat.name + " _BaseColor=" + (mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor").ToString() : "n/a")
        + " _Color=" + (mat.HasProperty("_Color") ? mat.GetColor("_Color").ToString() : "n/a"));
}
```

- If a material's `_BaseColor`/`_Color` reads noticeably grey/tinted rather than near-white/cream, that's the URP-conversion rewrite already documented as a gotcha (`docs/pending-and-restructuring-plan.md`: "Editor rewrites URP materials"). Fix by setting it back to the cat's intended near-white tone via `mcp__unity-editor-mcp__set_material_properties`, then re-check with the same eval snippet to confirm it holds after a recompile.
- If the material already reads correctly, the issue is the preview stage's lighting (the same caveat already noted for the character cube preview: "the preview cube uses Core's sunset light/ambient, so it looks lavender next to the swatch's true blue"). Fix by giving the `SelectionScreen`'s preview stage (`Assets/Scripts/Selection/SelectionScreen.cs`, `CreateStage()`) its own small neutral-white `Light` (a point or directional light parented to `stage`, colour `(1,1,1)`, moderate intensity), rather than relying on Core's ambient sunset light reaching the far-away stage position.
- Confirm visually with `mcp__unity-editor-mcp__capture_game_view` on the Companions screen with the cat focused.

- [ ] **Step 4: Update the existing "frozen preview" check**

In `Assets/Editor/PlaySmokeTest.Companions.cs`, replace lines 166–173:

```csharp
        var previewAnimal = GameObject.Find("SelectionPreviewStage")?.transform.Find("Preview_comp.pig");
        var previewPose0 = previewAnimal != null ? PoseSnapshot(previewAnimal.gameObject) : null;
        yield return 0.5f;
        var previewPose1 = previewAnimal != null ? PoseSnapshot(previewAnimal.gameObject) : null;
        var previewChange = previewAnimal != null ? PoseDifference(previewPose0, previewPose1) : -1f;
        Check("preview shows the focused animal and is static (paused, no rotation)",
            previewAnimal != null && pigFraming.ok && previewChange >= 0f && previewChange < 0.000001f,
            $"found={previewAnimal != null} {pigFraming.detail} pose change {previewChange:0.0000000}");
```

with:

```csharp
        var previewAnimal = GameObject.Find("SelectionPreviewStage")?.transform.Find("Preview_comp.pig");
        var previewPose0 = previewAnimal != null ? PoseSnapshot(previewAnimal.gameObject) : null;
        yield return 1.0f;
        var previewPose1 = previewAnimal != null ? PoseSnapshot(previewAnimal.gameObject) : null;
        var previewChange = previewAnimal != null ? PoseDifference(previewPose0, previewPose1) : -1f;
        Check("preview shows the focused animal and its idle animation is playing (not frozen)",
            previewAnimal != null && pigFraming.ok && previewChange > 0.0005f,
            $"found={previewAnimal != null} {pigFraming.detail} pose change {previewChange:0.0000000}");
```

`pig` (`comp.pig`) does not wander (no Walk/Run), so this exercises the non-wandering, Idle-only branch of `PreviewIdleLoop` — directly covering the Review Focus item about never `CrossFade`-ing to `"Walk"` on a non-wanderer, since a bad `CrossFade` call there would either throw (caught by the console-clean check) or silently no-op (still leaving `previewChange` healthy either way, but the console check catches a warning if Unity logs one for a missing state).

- [ ] **Step 5: Recompile, run the smoke test, verify, commit**

```bash
git add Assets/Scripts/Selection/PreviewIdleLoop.cs Assets/Scripts/Selection/PreviewIdleLoop.cs.meta Assets/Scripts/Economy/CompanionDefinition.cs Assets/Scripts/Selection/SelectionScreen.cs Assets/Editor/PlaySmokeTest.Companions.cs Assets/Companions
git commit -m "Companion preview animates instead of freezing; fix cat colour"
```

---

## Task 5: Flip `SelectionLayout` (grid/card left or top, preview/hero right or bottom)

**Files:**
- Modify: `Assets/Scripts/Selection/SelectionLayout.cs`
- Modify: `Assets/Editor/PlaySmokeTest.Selection.cs`

**Interfaces:**
- Produces: `SelectionLayout.heroShare` (replaces `landscapeInfoShare`/`portraitInfoShare` with one field, since both orientations now follow the same "hero gets this share, card gets the rest" rule).

- [ ] **Step 1: Rewrite `Apply()`**

```csharp
// Assets/Scripts/Selection/SelectionLayout.cs
using UnityEngine;

/// <summary>
/// Switches the selection screen between landscape (card/grid on the left, hero preview on the right) and
/// portrait (card/grid on top, hero preview below) — matching the reference layout. Re-runs whenever the
/// screen's size changes, since the app auto-rotates. Panels are positioned by anchors only, so it works at
/// any resolution.
/// </summary>
public class SelectionLayout : MonoBehaviour
{
    [SerializeField] private RectTransform infoPanel; // hero: live preview + name/actions
    [SerializeField] private RectTransform gridPanel;  // card: scrollable grid
    [Tooltip("Height of the top bar (Back, title, gem counter) that both panels sit below.")]
    [SerializeField] private float topBarHeight = 100f;
    [SerializeField] private float padding = 24f;
    [Tooltip("Share of the screen given to the hero preview: width in landscape, height in portrait.")]
    [SerializeField, Range(0.3f, 0.6f)] private float heroShare = 0.5f;

    public bool IsLandscape { get; private set; }

    private void OnEnable()
    {
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    public void Apply()
    {
        if (infoPanel == null || gridPanel == null)
        {
            return;
        }

        var size = ((RectTransform)transform).rect.size;
        IsLandscape = size.x >= size.y;

        if (IsLandscape)
        {
            SetAnchors(gridPanel, 0f, 0f, 1f - heroShare, 1f);
            SetAnchors(infoPanel, 1f - heroShare, 0f, 1f, 1f);
        }
        else
        {
            SetAnchors(gridPanel, 0f, 1f - heroShare, 1f, 1f);
            SetAnchors(infoPanel, 0f, 0f, 1f, 1f - heroShare);
        }
    }

    private void SetAnchors(RectTransform panel, float minX, float minY, float maxX, float maxY)
    {
        panel.anchorMin = new Vector2(minX, minY);
        panel.anchorMax = new Vector2(maxX, maxY);
        var half = padding * 0.5f;
        panel.offsetMin = new Vector2(minX <= 0f ? padding : half, minY <= 0f ? padding : half);
        panel.offsetMax = new Vector2(maxX >= 1f ? -padding : -half, maxY >= 1f ? -(topBarHeight + half) : -half);
    }
}
```

- [ ] **Step 2: Update the layout-audit direction check**

In `Assets/Editor/PlaySmokeTest.Selection.cs`, the `AuditOne` method (~line 434) currently asserts the *old* arrangement. Replace:

```csharp
        if (layout.IsLandscape ? gridRect.center.x <= infoRect.center.x : gridRect.center.y >= infoRect.center.y)
        {
            problems.Add($"{tag}: panels are not arranged for {mode}");
        }
```

with:

```csharp
        if (layout.IsLandscape ? gridRect.center.x >= infoRect.center.x : gridRect.center.y <= infoRect.center.y)
        {
            problems.Add($"{tag}: panels are not arranged for {mode}");
        }
```

- [ ] **Step 3: Recompile, run the smoke test (layout audit + selection checks), verify, commit**

Confirm "both selection screens fit 8 screen sizes" still reports 0 problems, and confirm no leftover reference to `landscapeInfoShare`/`portraitInfoShare` remains anywhere (`Grep` the two prefab files and the script for the old names).

```bash
git add Assets/Scripts/Selection/SelectionLayout.cs Assets/Prefabs/UI/SelectionScreen.prefab* Assets/Prefabs/UI/CompanionSelection.prefab* Assets/Editor/PlaySmokeTest.Selection.cs
git commit -m "Flip SelectionLayout: card on the left/top, hero preview on the right/bottom"
```

---

## Task 6: `SelectionTeaser` — Main Menu spoiler panels

**Files:**
- Create: `Assets/Scripts/Selection/SelectionTeaser.cs`
- Create: `Assets/Prefabs/UI/SelectionTeaser.prefab`
- Modify (scene wiring, via MCP): Main Menu prefab/instance — replace the plain `Characters`/`Companions` text buttons with two `SelectionTeaser` instances (Characters on top, Companions on bottom, on the right side).
- Modify: `Assets/Editor/PlaySmokeTest.Selection.cs`, `Assets/Editor/PlaySmokeTest.Companions.cs`

**Interfaces:**
- Produces: `SelectionTeaser` (`Bind(UnlockCategory)`, refreshes on `Wallet.Changed`/selection change, exposes a `Button OpenButton`). Consumed by Task 7's transition wiring.

- [ ] **Step 1: Write `SelectionTeaser`**

```csharp
// Assets/Scripts/Selection/SelectionTeaser.cs
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A Main Menu spoiler for one <see cref="UnlockCategory"/>: a title and the catalog's first three items as
/// small, non-interactive <see cref="UnlockTile"/> swatches (reusing their existing lock/selected-frame
/// rendering), so a locked item still shows locked even in the spoiler. Tapping <see cref="OpenButton"/> is
/// wired by the Main Menu (Task 7); this component only keeps the spoiler in sync with the catalog.
/// </summary>
public class SelectionTeaser : MonoBehaviour
{
    private const int SpoilerCount = 3;

    [SerializeField] private UnlockCategory category;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button openButton;
    [SerializeField] private UnlockTile tilePrefab;
    [SerializeField] private RectTransform tileContainer;

    public UnlockCategory Category => category;
    public Button OpenButton => openButton;

    private readonly System.Collections.Generic.List<UnlockTile> tiles = new System.Collections.Generic.List<UnlockTile>();

    private void OnEnable()
    {
        titleText.text = category == UnlockCategory.Character ? "Characters" : "Companions";
        BuildTiles();
        Refresh();
        Wallet.Changed += OnWalletChanged;
    }

    private void OnDisable()
    {
        Wallet.Changed -= OnWalletChanged;
    }

    private void BuildTiles()
    {
        if (tiles.Count > 0 || UnlockCatalog.Instance == null)
        {
            return;
        }
        foreach (var definition in UnlockCatalog.Instance.InCategory(category).Take(SpoilerCount))
        {
            var tile = Instantiate(tilePrefab, tileContainer);
            tile.Bind(definition);
            tile.Button.interactable = false;
            tiles.Add(tile);
        }
    }

    private void Refresh()
    {
        foreach (var tile in tiles)
        {
            tile.Refresh();
        }
    }

    private void OnWalletChanged(int balance)
    {
        Refresh();
    }
}
```

- [ ] **Step 2: Build the prefab**

Using `mcp__unity-editor-mcp__create_gameobject`/`add_component`/`set_component_properties`: a panel (title `TextMeshProUGUI` at top, a `HorizontalLayoutGroup` row of 3 `UnlockTile` instances below, sized small — e.g. 72×72 each), a `Button` covering the whole panel (this is `openButton`), wired to a new `SelectionTeaser` component with `tilePrefab` = the existing `UnlockTile` prefab used by `SelectionScreen`'s grid. Save with `mcp__unity-editor-mcp__create_prefab` at `Assets/Prefabs/UI/SelectionTeaser.prefab`.

- [ ] **Step 3: Replace the Main Menu buttons**

In the Main Menu prefab/instance: delete the plain `Characters`/`Companions` `Button` objects (`MainMenu/Characters`, `MainMenu/Companions`), instantiate two `SelectionTeaser` prefabs stacked on the right side (Characters on top, Companions below), named `MainMenu/CharacterTeaser` and `MainMenu/CompanionTeaser`, with `category` set to `Character`/`Companion` respectively. Leave `MainMenu.OpenCharacters()`/`OpenCompanions()` unwired for now — Task 7 wires the new teasers' `OpenButton.onClick` to them (with the transition added).

- [ ] **Step 4: Update `PlaySmokeTest` references to the old button names**

`Assets/Editor/PlaySmokeTest.Selection.cs` line ~180 (`CanvasChild("MainMenu/Characters")`) and line ~187 (`Click("MainMenu/Characters")`), and the equivalent `Companions` lines in `Assets/Editor/PlaySmokeTest.Companions.cs` (~145, ~151), become `CanvasChild("MainMenu/CharacterTeaser")` / `Click("MainMenu/CharacterTeaser")` and `MainMenu/CompanionTeaser` respectively — clicking the teaser's whole panel opens the screen, same as the old button did (Task 7 wires this; if this task lands before Task 7's wiring, temporarily wire `OpenButton.onClick` directly to `MainMenu.OpenCharacters`/`OpenCompanions` here so these existing checks stay green, and Task 7 replaces that direct wiring with the animated version).

`Assets/Editor/PlaySmokeTest.Companions.cs`'s `MainMenuLayoutProblems` (line 219) name list changes from `"Characters", "Companions"` to `"CharacterTeaser", "CompanionTeaser"`.

- [ ] **Step 5: Recompile, run the smoke test, verify the renamed checks and the layout-fit check pass, commit**

```bash
git add Assets/Scripts/Selection/SelectionTeaser.cs Assets/Scripts/Selection/SelectionTeaser.cs.meta Assets/Prefabs/UI/SelectionTeaser.prefab* Assets/Editor/PlaySmokeTest.Selection.cs Assets/Editor/PlaySmokeTest.Companions.cs
git commit -m "Add Main Menu SelectionTeaser panels, replacing the plain Characters/Companions buttons"
```

---

## Task 7: Open/close transition (background pose, fade, slide, Back button)

**Files:**
- Modify: `Assets/Scripts/MainMenu.cs`
- Modify: `Assets/Scripts/Selection/SelectionScreen.cs`
- Modify: `Assets/Editor/PlaySmokeTest.Selection.cs`, `Assets/Editor/PlaySmokeTest.Companions.cs`

**Interfaces:**
- Consumes: `MenuBackgroundRig.Instance.MoveToCharacters()/.MoveToCompanions()/.MoveToMainMenu()` (Task 1), `SelectionTeaser.OpenButton` (Task 6).
- Produces: `MainMenu.OpenCharacters()`/`OpenCompanions()` now animate instead of instant-cutting; `SelectionScreen.Back()` reverses the same animation.

- [ ] **Step 1: Animate `MainMenu`'s open methods**

```csharp
    public void OpenCharacters()
    {
        MenuBackgroundRig.Instance?.MoveToCharacters();
        StartTransitionOpen(selectionScreen);
    }

    public void OpenCompanions()
    {
        MenuBackgroundRig.Instance?.MoveToCompanions();
        StartTransitionOpen(companionScreen);
    }

    private void StartTransitionOpen(SelectionScreen screen)
    {
        GetComponent<CanvasGroup>()
            .LeanAlpha(0f, 0.3f)
            .setOnComplete(() =>
            {
                gameObject.SetActive(false);
                screen.Open(() =>
                {
                    MenuBackgroundRig.Instance?.MoveToMainMenu();
                    gameObject.SetActive(true);
                    GetComponent<CanvasGroup>().alpha = 1f;
                });
            });
    }
```

(`characterTeaser`/`companionTeaser`'s `OpenButton.onClick` are wired to `OpenCharacters`/`OpenCompanions` respectively via `mcp__unity-editor-mcp__set_component_properties` on the Button's `onClick` persistent listener list, matching how the old plain buttons were wired — same target method names, no signature change.)

- [ ] **Step 2: Disable the teaser button and Back button for the duration of their own transition**

This directly covers the Review Focus item about double-tapping mid-transition. In `MainMenu.StartTransitionOpen`, disable the tapped teaser's button at the start and re-enable it once the Main Menu's `CanvasGroup` is fully hidden (it's about to be deactivated anyway, so this only matters if the tap happens again before the fade completes):

```csharp
    private void StartTransitionOpen(SelectionScreen screen, Button openButton)
    {
        openButton.interactable = false;
        GetComponent<CanvasGroup>()
            .LeanAlpha(0f, 0.3f)
            .setOnComplete(() =>
            {
                gameObject.SetActive(false);
                screen.Open(() =>
                {
                    MenuBackgroundRig.Instance?.MoveToMainMenu();
                    gameObject.SetActive(true);
                    GetComponent<CanvasGroup>().alpha = 1f;
                    openButton.interactable = true;
                });
            });
    }
```

Update `OpenCharacters`/`OpenCompanions` to pass their teaser's `OpenButton` through. In `Assets/Scripts/Selection/SelectionScreen.cs`, guard `Back()` the same way — add a `private bool closing;` set at the start of `Back()` and checked at the top so a second Back tap mid-close is a no-op:

```csharp
    public void Back()
    {
        if (closing)
        {
            return;
        }
        closing = true;
        gameObject.SetActive(false);
        onClose?.Invoke();
        closing = false;
    }
```

- [ ] **Step 3: Manual play verification**

`mcp__unity-editor-mcp__editor_play`: tap the Characters teaser — confirm the background pans/zooms toward the `charactersOpen` pose while the Main Menu fades out and the screen appears; tap Back — confirm it reverses; repeat for Companions. Rapidly double-tap a teaser and confirm it doesn't open two screens or visibly stutter the background.

- [ ] **Step 4: Update `PlaySmokeTest` timing waits**

The existing waits after `Click("MainMenu/CharacterTeaser")` (`yield return WaitUntil(() => screen.gameObject.activeInHierarchy); yield return 0.4f;` in both `PlaySmokeTest.Selection.cs` and `.Companions.cs`) already tolerate the fade — `WaitUntil` polls until the screen is active, which now happens slightly later (after the 0.3s fade) than before (instant); no code change needed there, but re-run the full smoke test to confirm the existing `0.4f`/`0.2f` fixed waits after `Click(".../Back")` (~line 296–299 in `.Selection.cs`, ~204–205 in `.Companions.cs`) are still long enough to cover the new fade-back — if the smoke test fails on those specific checks with a timing-looking mismatch (screen not yet hidden or Main Menu not yet visible), bump those particular waits to `0.4f` to match the new transition duration.

- [ ] **Step 5: Recompile, run the full smoke test, verify, commit**

```bash
git add Assets/Scripts/MainMenu.cs Assets/Scripts/Selection/SelectionScreen.cs Assets/Editor/PlaySmokeTest.Selection.cs Assets/Editor/PlaySmokeTest.Companions.cs
git commit -m "Animate the Main Menu <-> selection screen transition (background pose, fade, Back)"
```

---

## Task 8: Apply the Hyper Casual UI Pack skin

**Files:**
- Manual import (user action, cannot be automated — needs the Unity ID that owns the asset).
- Create: `Assets/Prefabs/UI/SelectionButton.prefab`, `Assets/Prefabs/UI/SelectionPanelFrame.prefab`
- Modify (prefab wiring, via MCP): `Assets/Prefabs/UI/SelectionScreen.prefab`, `Assets/Prefabs/UI/CompanionSelection.prefab`, `Assets/Prefabs/UI/SelectionTeaser.prefab` — swap their `Image` backgrounds/button visuals to the new pieces.

**Interfaces:**
- Produces: no new script surface — this task only swaps `Image.sprite` references on existing objects built in Tasks 5–6.

- [ ] **Step 1: Import the pack**

USER ACTION: in the Unity Editor, open Window > Asset Store (or Package Manager's "My Assets" tab), find "Hyper Casual UI Pack" (https://assetstore.unity.com/packages/2d/gui/hyper-casual-ui-pack-375832), Download, then Import all files. Report back the folder it imported into (e.g. `Assets/Hyper Casual UI Pack/...`) before continuing — the exact folder name is decided by the package, not by us.

- [ ] **Step 2: Locate the sprites to use**

`mcp__unity-editor-mcp__find_assets` inside the imported folder for a plain rounded button sprite and a plain rounded panel/frame sprite. Confirm each is set to `Sprite (2D and UI)` with a 9-slice border via `mcp__unity-editor-mcp__get_import_settings`; if a needed sprite has no border set, set one via `mcp__unity-editor-mcp__set_import_settings` so it can be used `Sliced`.

- [ ] **Step 3: Build the two style prefabs**

`Assets/Prefabs/UI/SelectionButton.prefab`: `Button` + `Image` (`Image.Type = Sliced`, sprite from Step 2) + child `TextMeshProUGUI`, sized 320×88 to match the existing `MenuButton` footprint. `Assets/Prefabs/UI/SelectionPanelFrame.prefab`: an `Image` (`Sliced`, the panel/frame sprite), anchors stretched to fill its parent, no fixed size.

- [ ] **Step 4: Swap them into the three touched prefabs**

In `Assets/Prefabs/UI/SelectionScreen.prefab`, `CompanionSelection.prefab`, and `SelectionTeaser.prefab`: replace each plain background `Image`'s sprite with `SelectionPanelFrame`'s sprite (same `Sliced` settings), and each action `Button`'s visuals (`Select`/`Unlock`/`Buy`/`Try`/`Restore`/`Back`/the teaser's `OpenButton`) with `SelectionButton`'s sprite/`Sliced` settings — via `mcp__unity-editor-mcp__set_component_properties` on each `Image`, not by replacing the GameObjects themselves (keeps every existing `Button`/`TextMeshProUGUI` reference the smoke test already looks up by path intact).

- [ ] **Step 5: Recompile, run the full smoke test, verify nothing broke, manual visual check, commit**

The smoke test doesn't assert visual style, only structure/behaviour, so it should be unaffected by a pure sprite swap — running it confirms that. Follow with `mcp__unity-editor-mcp__capture_game_view` on both screens and the Main Menu teasers to confirm the new look renders correctly at runtime (not just in the Editor Inspector).

```bash
git add Assets/Prefabs/UI/SelectionButton.prefab* Assets/Prefabs/UI/SelectionPanelFrame.prefab* Assets/Prefabs/UI/SelectionScreen.prefab* Assets/Prefabs/UI/CompanionSelection.prefab* Assets/Prefabs/UI/SelectionTeaser.prefab* "Assets/Hyper Casual UI Pack"
git commit -m "Apply Hyper Casual UI Pack skin to the selection screens and teasers"
```

---

## Task 9: Final verification pass

**Files:** none (verification only).

- [ ] **Step 1: Full smoke test**

`mcp__unity-editor-mcp__recompile` (0 errors), `Tools/Smoke Test/Run Play Smoke Test`, read console tail 2, confirm one clean "SMOKE TEST PASS n/n" line.

- [ ] **Step 2: Manual visual pass**

`editor_play`, walk through: Main Menu (both teasers visible, spoiler rows show locked items as locked) → open Characters → drag-rotate the preview → Back → open Companions → confirm the cat reads white/cream and its idle animation plays → Back. `capture_game_view` once on the Main Menu and once on each open screen for a final by-eye check against the Lego reference composition (subjective call the automated checks can't make, per the spec's testing plan).

- [ ] **Step 3: Update the handoff doc**

Add a short entry to `docs/pending-and-restructuring-plan.md` noting this feature is done, and that `MenuShowcaseStage`'s set-dressing is intentionally minimal pending the user's own decoration pass (matching the existing Playground-decoration note style).

- [ ] **Step 4: Commit**

```bash
git add docs/pending-and-restructuring-plan.md
git commit -m "Selection experience redesign: verified end-to-end"
```

## Self-Review

**Spec coverage:** §3 (background) → Task 1. §4 (shared shell: layout flip, teasers, transition, style) → Tasks 5, 6, 7, 8. §5 (character preview) → Tasks 2, 3. §6 (companion preview) → Tasks 2, 4. §8 (edge cases) → covered inline per task and in Review Focus. §9 (testing plan) → every task updates or adds the relevant `PlaySmokeTest` checks; Task 9 is the final full pass plus the manual/subjective checks the spec explicitly calls out as needing a human.

**Placeholder scan:** no TBD/TODO; the one open-ended step (Task 8, Step 1) is a genuine external dependency (the asset doesn't exist in the repo until the user imports it) with a concrete instruction, not vague guidance.

**Type consistency:** `MenuBackgroundRig.MoveToCharacters/MoveToCompanions/MoveToMainMenu` (Task 1) match the calls used in Task 7. `PreviewRotator.Target` (Task 2) matches `SelectionScreen.ShowPreview`'s assignment. `PreviewIdleLoop.Begin(Animator, bool)` (Task 4) matches its call site in `CompanionDefinition.CreatePreview`. `SelectionTeaser.OpenButton` (Task 6) matches Task 7's wiring.

**Review Focus:** all five items map to a task each, as listed above.
