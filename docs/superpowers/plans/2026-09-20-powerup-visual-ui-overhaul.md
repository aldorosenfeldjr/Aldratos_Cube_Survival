# Power-Up Visual & UI Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make power-up pickups read as exciting collectibles (badge-framed, camera-facing, idle motion) instead of stock props that look like hazards, and make collecting one feel like a game — tiered camera zoom/shake/slowdown by importance, and an animated left-edge HUD with fill-bar timers.

**Architecture:** Additive-only changes on top of the existing data-driven power-up system (`Assets/Scripts/PowerUps/`). A new `PowerUpImportance` tier on `PowerUpDefinition` drives a single tunable `PowerUpJuiceSettings` asset, read by a new `PowerUpJuiceController` that reacts to the existing `PowerUpManager.OnPowerUpGranted` event using Cinemachine's own Lens/Impulse mechanisms (not manual Camera transform tweening, since a `CinemachineBrain` drives the live Camera every frame). World pickup prefabs (all six — three per level theme) get a new nested pivot for badge-frame + tilt + idle motion. The HUD switches from instant horizontal icons to an animated vertical fill-bar stack.

**Tech Stack:** Unity (C#), Cinemachine 3.x (`com.unity.cinemachine` 6.6.0, already installed — `CinemachineCamera`, `CinemachineImpulseSource`, `CinemachineImpulseListener`), LeanTween (already installed, project's only tweening library — no new dependency), TextMeshPro (existing HUD text), Unity Editor MCP tools (`mcp__unity-editor-mcp__*`) for all scene/prefab/asset edits — this project has no automated test framework, so verification is live-Editor (recompile clean, console clean, manual play checks), matching how the rest of this codebase has been built and verified.

**Spec:** `docs/superpowers/specs/2026-09-20-powerup-visual-ui-overhaul-design.md`

## Global Constraints

- No changes to `PowerUpManager`'s public surface (`Grant`, `TryConsumeShield`, `ResetAll`, `SpeedMultiplier`/`IsInvincible`/`HasShield`, `OnPowerUpGranted`/`OnPowerUpExpired` events) — every task here only *subscribes* to it or adds new sibling files.
- No new tweening dependency — LeanTween only, matching every other animated script in the project (`GameOverMenu`, `GameManager`, `MenuBackgroundEffect`, `MenuSelectionAnimator`, `SunGlowPulse`).
- Camera juice must go through Cinemachine's own mechanisms (`CinemachineCamera.Lens.FieldOfView`, `CinemachineImpulseSource`/`CinemachineImpulseListener`) — never tween the Main Camera GameObject's transform/FOV directly, since `CinemachineBrain` overwrites it every frame.
- Slowdown must never leave the player unable to act — `Time.timeScale` dips but player input/movement keeps working (per approved design), and must hard-reset to 1 on `PowerUpManager.ResetAll()`.
- Target platforms are PC + mobile (iOS/Android) — all new effects must be cheap per-grant triggers, not per-frame cost, and the HUD layout must not collide with other UI on a narrow/portrait aspect ratio.
- Badge tint is per-type (gold=Invincibility, blue=Shield, green=SpeedBoost), identical across both level themes (Meadow, Playground) so the color meaning stays consistent.
- All six world pickup prefabs (`PowerUp_Shield`, `PowerUp_SpeedBoost`, `PowerUp_Invincibility` for Meadow; `KayKit_Shield`, `KayKit_SpeedBoost`, `KayKit_Invincibility` for Playground) get identical treatment.
- After every task: recompile via `mcp__unity-editor-mcp__recompile` (or the relevant scene/asset save) and confirm 0 console errors via `mcp__unity-editor-mcp__console` before committing.

---

## Task 1: `PowerUpIdleMotion` — idle spin/bob for pickups

**Files:**
- Create: `Assets/Scripts/PowerUps/PowerUpIdleMotion.cs`

**Interfaces:**
- Consumes: nothing (self-contained `MonoBehaviour`).
- Produces: `PowerUpIdleMotion` component, attached to each `VisualPivot` child object created in Tasks 7–8. Public serialized fields: `spinSpeedDegreesPerSecond` (float), `bobAmplitude` (float), `bobSpeed` (float).

- [ ] **Step 1: Write the component**

```csharp
// Assets/Scripts/PowerUps/PowerUpIdleMotion.cs
using UnityEngine;

public class PowerUpIdleMotion : MonoBehaviour
{
    [SerializeField]
    private float spinSpeedDegreesPerSecond = 90f;
    [SerializeField]
    private float bobAmplitude = 0.08f;
    [SerializeField]
    private float bobSpeed = 2f;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeedDegreesPerSecond * Time.deltaTime, 0f, Space.Self);

        var offset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = basePosition + new Vector3(0f, offset, 0f);
    }
}
```

- [ ] **Step 2: Recompile and verify**

Call `mcp__unity-editor-mcp__recompile`, then `mcp__unity-editor-mcp__console` and confirm 0 errors. `PowerUpIdleMotion` won't be attached to anything yet (Tasks 7–8 do that), so there is nothing to play-test until then — this step only confirms the script compiles cleanly.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpIdleMotion.cs Assets/Scripts/PowerUps/PowerUpIdleMotion.cs.meta
git commit -m "Add PowerUpIdleMotion for pickup spin/bob"
```

---

## Task 2: Import badge frame asset + tinted materials

**Files:**
- Import (via `mcp__unity-editor-mcp__import_asset`): `D:\Unity\Aldera_Assets\KayKit_Adventurers_2.0_FREE\KayKit_Adventurers_2.0_FREE\Assets\fbx(unity)\shield_badge.fbx` → `Assets/KayKit_Adventurers_2.0_FREE/fbx(unity)/shield_badge.fbx` (only this one file — do not import the rest of the Adventurers pack).
- Create: `Assets/Materials/PowerUp_Badge_Gold.mat`
- Create: `Assets/Materials/PowerUp_Badge_Blue.mat`
- Create: `Assets/Materials/PowerUp_Badge_Green.mat`

**Interfaces:**
- Produces: an importable `shield_badge` mesh, and three materials referenced by name in Tasks 7–8's `BadgeFrame` child objects.

- [ ] **Step 1: Create the destination folder and import the mesh**

Use `mcp__unity-editor-mcp__create_folder` for `Assets/KayKit_Adventurers_2.0_FREE/fbx(unity)` (create parent folders as needed), then `mcp__unity-editor-mcp__import_asset` with source `D:\Unity\Aldera_Assets\KayKit_Adventurers_2.0_FREE\KayKit_Adventurers_2.0_FREE\Assets\fbx(unity)\shield_badge.fbx` and destination `Assets/KayKit_Adventurers_2.0_FREE/fbx(unity)/shield_badge.fbx`.

- [ ] **Step 2: Verify the import**

Use `mcp__unity-editor-mcp__find_assets` with a query for `shield_badge` and confirm the `.fbx` and its generated mesh sub-asset are present. Check `mcp__unity-editor-mcp__console` for 0 import errors.

- [ ] **Step 3: Create the three tinted materials**

Use `mcp__unity-editor-mcp__create_asset` (material) three times at the paths above, using the project's existing lit shader (match whatever shader `Assets/Prefabs/PowerUp_Shield.prefab`'s current material uses — inspect via `mcp__unity-editor-mcp__get_material_properties` on that prefab's material first, then create the three new materials with the same shader). Set base color:
- `PowerUp_Badge_Gold.mat`: `(1.0, 0.82, 0.2, 1.0)`
- `PowerUp_Badge_Blue.mat`: `(0.25, 0.55, 1.0, 1.0)`
- `PowerUp_Badge_Green.mat`: `(0.3, 0.9, 0.35, 1.0)`

via `mcp__unity-editor-mcp__set_material_properties`.

- [ ] **Step 4: Verify materials**

`mcp__unity-editor-mcp__get_material_properties` on each of the three, confirm the color set in Step 3. Check console for 0 errors.

- [ ] **Step 5: Commit**

```bash
git add "Assets/KayKit_Adventurers_2.0_FREE" Assets/Materials/PowerUp_Badge_Gold.mat* Assets/Materials/PowerUp_Badge_Blue.mat* Assets/Materials/PowerUp_Badge_Green.mat*
git commit -m "Import badge frame mesh and add tinted badge materials"
```

---

## Task 3: `PowerUpImportance` tier on `PowerUpDefinition`

**Files:**
- Modify: `Assets/Scripts/PowerUps/PowerUpDefinition.cs`
- Modify (data, via MCP): `Assets/Levels` power-up definition assets — locate them via `mcp__unity-editor-mcp__find_assets` (type `ShieldDefinition`/`SpeedBoostDefinition`/`InvincibilityDefinition`); there is one asset per type shared across both themes (the theme difference is only in the world prefab, not the definition).

**Interfaces:**
- Produces: `public enum PowerUpImportance { Minor, Major, Supreme }` and `PowerUpDefinition.Importance` (public getter, `[SerializeField]` backing field, default `Minor`). Consumed by `PowerUpJuiceSettings`/`PowerUpJuiceController` in Tasks 4 and 6.

- [ ] **Step 1: Add the enum and field**

```csharp
// Assets/Scripts/PowerUps/PowerUpDefinition.cs
using UnityEngine;

public enum PowerUpImportance
{
    Minor,
    Major,
    Supreme
}

public abstract class PowerUpDefinition : ScriptableObject
{
    [SerializeField]
    private string displayName;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    [Tooltip("0 = no timer; the effect is removed by being consumed instead (e.g. Shield).")]
    private float duration;
    [SerializeField]
    private PowerUpImportance importance = PowerUpImportance.Minor;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public float Duration => duration;
    public PowerUpImportance Importance => importance;

    public abstract IPowerUpEffect CreateEffect();
}
```

- [ ] **Step 2: Recompile and verify**

`mcp__unity-editor-mcp__recompile`, then `mcp__unity-editor-mcp__console`, confirm 0 errors. Existing definition assets keep working unchanged (new field defaults to `Minor`, no data loss).

- [ ] **Step 3: Set Importance on the three definition assets**

Use `mcp__unity-editor-mcp__set_serialized_field` on each asset found in Step 1's asset search:
- `SpeedBoostDefinition` asset → `importance = Minor`
- `ShieldDefinition` asset → `importance = Minor`
- `InvincibilityDefinition` asset → `importance = Supreme`

(`Minor` is already the default, so this step is a no-op confirmation for the first two and an explicit change for Invincibility only — but set all three explicitly so the values are visible in the Inspector, not relying on an implicit default.)

- [ ] **Step 4: Verify**

`mcp__unity-editor-mcp__get_serialized_fields` on each of the three assets, confirm `importance` reads back correctly.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpDefinition.cs
git commit -m "Add PowerUpImportance tier to PowerUpDefinition"
```

---

## Task 4: `PowerUpJuiceSettings` — tiered preset data

**Files:**
- Create: `Assets/Scripts/PowerUps/PowerUpJuiceSettings.cs`
- Create (data, via MCP): `Assets/Levels/PowerUpJuiceSettings.asset` (or `Assets/PowerUps/PowerUpJuiceSettings.asset` — place alongside the other `PowerUps/` config, use `mcp__unity-editor-mcp__create_folder` for `Assets/PowerUps` if it doesn't exist, this is a data folder distinct from the `Assets/Scripts/PowerUps` code folder)

**Interfaces:**
- Consumes: `PowerUpImportance` (Task 3).
- Produces: `PowerUpJuiceSettings.GetPreset(PowerUpImportance tier) : PowerUpJuicePreset`, a single project-wide asset instance, consumed by `PowerUpJuiceController` in Task 6.

- [ ] **Step 1: Write the ScriptableObject**

```csharp
// Assets/Scripts/PowerUps/PowerUpJuiceSettings.cs
using System;
using UnityEngine;

[Serializable]
public struct PowerUpJuicePreset
{
    public float shakeAmplitude;
    public float shakeDuration;
    public float zoomPunchFovDelta;
    public float zoomDuration;
    public float slowdownTimeScale;
    public float slowdownHoldDuration;
    public float slowdownEaseBackDuration;
}

[CreateAssetMenu(fileName = "PowerUpJuiceSettings", menuName = "PowerUps/Juice Settings")]
public class PowerUpJuiceSettings : ScriptableObject
{
    [SerializeField]
    private PowerUpJuicePreset minor = new PowerUpJuicePreset
    {
        shakeAmplitude = 0.15f,
        shakeDuration = 0.1f,
        zoomPunchFovDelta = 3f,
        zoomDuration = 0.08f,
        slowdownTimeScale = 0.6f,
        slowdownHoldDuration = 0.05f,
        slowdownEaseBackDuration = 0.1f
    };
    [SerializeField]
    private PowerUpJuicePreset major = new PowerUpJuicePreset
    {
        shakeAmplitude = 0.3f,
        shakeDuration = 0.15f,
        zoomPunchFovDelta = 6f,
        zoomDuration = 0.12f,
        slowdownTimeScale = 0.45f,
        slowdownHoldDuration = 0.1f,
        slowdownEaseBackDuration = 0.15f
    };
    [SerializeField]
    private PowerUpJuicePreset supreme = new PowerUpJuicePreset
    {
        shakeAmplitude = 0.5f,
        shakeDuration = 0.25f,
        zoomPunchFovDelta = 10f,
        zoomDuration = 0.2f,
        slowdownTimeScale = 0.3f,
        slowdownHoldDuration = 0.2f,
        slowdownEaseBackDuration = 0.25f
    };

    public PowerUpJuicePreset GetPreset(PowerUpImportance tier)
    {
        switch (tier)
        {
            case PowerUpImportance.Major:
                return major;
            case PowerUpImportance.Supreme:
                return supreme;
            default:
                return minor;
        }
    }
}
```

- [ ] **Step 2: Recompile and verify**

`mcp__unity-editor-mcp__recompile`, `mcp__unity-editor-mcp__console`, confirm 0 errors.

- [ ] **Step 3: Create the asset instance**

`mcp__unity-editor-mcp__create_asset` for `Assets/PowerUps/PowerUpJuiceSettings.asset` of type `PowerUpJuiceSettings`. Default field values from Step 1 are used as-is (already tuned per the tiered escalation the spec describes — Minor fast/small, Supreme longer/bigger); no further edits needed for this task.

- [ ] **Step 4: Verify**

`mcp__unity-editor-mcp__get_serialized_fields` on the new asset, confirm the three presets read back with the Step 1 values.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpJuiceSettings.cs Assets/PowerUps/PowerUpJuiceSettings.asset*
git commit -m "Add PowerUpJuiceSettings tiered preset asset"
```

---

## Task 5: Cinemachine Impulse components on Main VCam / Zoom VCam

**Files:**
- Modify (scene, via MCP): `Assets/Scenes/Core.unity` — `Main VCam` and `Zoom VCam` GameObjects.

**Interfaces:**
- Produces: a `CinemachineImpulseSource` component on `Main VCam` (referenced by `PowerUpJuiceController` in Task 6), and `CinemachineImpulseListener` components on both `Main VCam` and `Zoom VCam` (so a shake mid-game-over-zoom-swap still reads).

- [ ] **Step 1: Open the Core scene**

`mcp__unity-editor-mcp__open_scene` for `Assets/Scenes/Core.unity` if not already open. `mcp__unity-editor-mcp__find_gameobjects` for `Main VCam` and `Zoom VCam` to get their instance IDs.

- [ ] **Step 2: Add components**

`mcp__unity-editor-mcp__add_component`:
- `Main VCam` → `Unity.Cinemachine.CinemachineImpulseSource`
- `Main VCam` → `Unity.Cinemachine.CinemachineImpulseListener`
- `Zoom VCam` → `Unity.Cinemachine.CinemachineImpulseListener`

- [ ] **Step 3: Save scene and verify**

`mcp__unity-editor-mcp__save_scene`. `mcp__unity-editor-mcp__get_component_properties` on `Main VCam` to confirm `CinemachineImpulseSource` and `CinemachineImpulseListener` are both present, and on `Zoom VCam` to confirm `CinemachineImpulseListener` is present. `mcp__unity-editor-mcp__console` for 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/Core.unity
git commit -m "Add Cinemachine Impulse source/listener to camera rigs"
```

---

## Task 6: `PowerUpJuiceController` — tiered zoom/shake/slowdown on grant

**Files:**
- Create: `Assets/Scripts/PowerUps/PowerUpJuiceController.cs`
- Modify (scene, via MCP): `Assets/Scenes/Core.unity` — add the component to `Main VCam` and wire its references.

**Interfaces:**
- Consumes: `PowerUpManager.Instance.OnPowerUpGranted` (`Action<PowerUpDefinition, float>`, existing), `PowerUpManager.Instance.OnPowerUpExpired`/`ResetAll()` is not directly subscribed — instead this component subscribes to a small addition below; `PowerUpDefinition.Importance` (Task 3); `PowerUpJuiceSettings.GetPreset` (Task 4); `CinemachineCamera.Lens.FieldOfView`; `CinemachineImpulseSource.GenerateImpulseWithForce(float)` (Task 5).
- Produces: nothing new consumed by later tasks — this is a leaf subscriber.

Note on the timescale safety net: `PowerUpManager.ResetAll()` does not currently fire a distinct "reset" event separate from per-item `OnPowerUpExpired` — it calls `OnPowerUpExpired` once per active item during the reset loop (see `PowerUpManager.cs:91-99`). `PowerUpJuiceController` cannot distinguish "expired normally" from "reset because game restarted" from that event alone, so instead of listening for a reset signal, it subscribes to `GameManager`'s existing `OnEnable`/`RestartGame` timing indirectly by resetting `Time.timeScale` to 1 in its own `OnDisable` (this component lives on `Main VCam`, which is never disabled/re-enabled independent of the scene, so `OnDisable` alone isn't sufficient) — instead, add a tiny public method `PowerUpJuiceController.ForceResetTimeScale()` and call it from `GameManager.RestartGame()` and `GameManager.OnEnable()`, right where `PowerUpManager.Instance.ResetAll()` is already called. This keeps `PowerUpManager` untouched (per Global Constraints) and makes the reset explicit rather than inferred.

- [ ] **Step 1: Write the controller**

```csharp
// Assets/Scripts/PowerUps/PowerUpJuiceController.cs
using Unity.Cinemachine;
using UnityEngine;

public class PowerUpJuiceController : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera mainVCam;
    [SerializeField]
    private CinemachineImpulseSource impulseSource;
    [SerializeField]
    private PowerUpJuiceSettings juiceSettings;

    private float baseFieldOfView;
    private bool hasCapturedBaseFov;

    private void OnEnable()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpGranted += HandleGranted;
        }
    }

    private void OnDisable()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpGranted -= HandleGranted;
        }
    }

    private void HandleGranted(PowerUpDefinition definition, float duration)
    {
        if (!hasCapturedBaseFov)
        {
            baseFieldOfView = mainVCam.Lens.FieldOfView;
            hasCapturedBaseFov = true;
        }

        var preset = juiceSettings.GetPreset(definition.Importance);

        LeanTween.cancel(gameObject);

        impulseSource.GenerateImpulseWithForce(preset.shakeAmplitude);

        var targetFov = baseFieldOfView - preset.zoomPunchFovDelta;
        LeanTween.value(gameObject, baseFieldOfView, targetFov, preset.zoomDuration)
            .setOnUpdate(SetFieldOfView)
            .setEase(LeanTweenType.easeOutQuad)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                LeanTween.value(gameObject, targetFov, baseFieldOfView, preset.zoomDuration)
                    .setOnUpdate(SetFieldOfView)
                    .setEase(LeanTweenType.easeInQuad)
                    .setIgnoreTimeScale(true);
            });

        LeanTween.value(gameObject, Time.timeScale, preset.slowdownTimeScale, 0.02f)
            .setOnUpdate(SetTimeScale)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                LeanTween.value(gameObject, preset.slowdownTimeScale, 1f, preset.slowdownEaseBackDuration)
                    .setDelay(preset.slowdownHoldDuration)
                    .setOnUpdate(SetTimeScale)
                    .setIgnoreTimeScale(true);
            });
    }

    private void SetFieldOfView(float value)
    {
        var lens = mainVCam.Lens;
        lens.FieldOfView = value;
        mainVCam.Lens = lens;
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
        Time.fixedDeltaTime = 0.02f * value;
    }

    public void ForceResetTimeScale()
    {
        LeanTween.cancel(gameObject);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (hasCapturedBaseFov)
        {
            SetFieldOfView(baseFieldOfView);
        }
    }
}
```

- [ ] **Step 2: Recompile and verify**

`mcp__unity-editor-mcp__recompile`, `mcp__unity-editor-mcp__console`, confirm 0 errors.

- [ ] **Step 3: Wire `GameManager` to call `ForceResetTimeScale`**

Modify `Assets/Scripts/GameManager.cs`: add a serialized field and call it at both existing `PowerUpManager.Instance.ResetAll()` call sites.

```csharp
// Add near the other [SerializeField] fields (after powerUpSpawner):
[SerializeField]
private PowerUpJuiceController powerUpJuiceController;
```

In `OnEnable()`, immediately after the existing `PowerUpManager.Instance.ResetAll();` block:
```csharp
if (powerUpJuiceController != null)
{
    powerUpJuiceController.ForceResetTimeScale();
}
```

In `RestartGame()`, immediately after its existing `PowerUpManager.Instance.ResetAll();` block, add the same three lines.

- [ ] **Step 4: Add the component to the scene and wire references**

`mcp__unity-editor-mcp__add_component` — `Main VCam` → `PowerUpJuiceController`.
`mcp__unity-editor-mcp__set_component_properties` on `Main VCam`'s new `PowerUpJuiceController`:
- `mainVCam` → the `Main VCam` GameObject's own `CinemachineCamera` component
- `impulseSource` → the `Main VCam` GameObject's `CinemachineImpulseSource` (from Task 5)
- `juiceSettings` → `Assets/PowerUps/PowerUpJuiceSettings.asset` (from Task 4)

Then find the `GameManager` GameObject (`mcp__unity-editor-mcp__find_gameobjects`) and `mcp__unity-editor-mcp__set_component_properties` its `GameManager` component's new `powerUpJuiceController` field → `Main VCam`'s `PowerUpJuiceController`.

- [ ] **Step 5: Save scene and verify**

`mcp__unity-editor-mcp__save_scene`. `mcp__unity-editor-mcp__console`, confirm 0 errors.

- [ ] **Step 6: Manual play verification**

`mcp__unity-editor-mcp__editor_play`. Use `mcp__unity-editor-mcp__eval` to call `PowerUpManager.Instance.Grant(...)` with each of the three definition assets in turn (via `Resources.FindObjectsOfTypeAll` or a direct asset load in the eval script) and confirm via `mcp__unity-editor-mcp__capture_game_view` that a brief zoom-in is visible, and via a short polled read of `Time.timeScale` that it dips and recovers to 1 within roughly the configured durations. Grant `InvincibilityDefinition` and confirm the dip/zoom is visibly larger/longer than for `SpeedBoostDefinition`/`ShieldDefinition`. `mcp__unity-editor-mcp__editor_stop` when done.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpJuiceController.cs Assets/Scripts/GameManager.cs Assets/Scenes/Core.unity
git commit -m "Add PowerUpJuiceController for tiered camera zoom/shake/slowdown"
```

---

## Task 7: Restructure Meadow power-up prefabs (badge, tilt, idle motion)

**Files:**
- Modify (via MCP): `Assets/Prefabs/PowerUp_Shield.prefab`, `Assets/Prefabs/PowerUp_SpeedBoost.prefab`, `Assets/Prefabs/PowerUp_Invincibility.prefab`

**Interfaces:**
- Consumes: `shield_badge` mesh + tinted materials (Task 2), `PowerUpIdleMotion` (Task 1).
- Produces: nothing consumed by later tasks — this is the visual leaf.

For **each** of the three prefabs, apply the identical recipe (only the item mesh/material/tint differ, called out per-prefab below):

- [ ] **Step 1: Open `PowerUp_Shield.prefab` for editing**

Use the Unity Editor MCP prefab-editing flow: open the prefab (`mcp__unity-editor-mcp__open_scene` supports prefab mode, or use `mcp__unity-editor-mcp__create_gameobjects`/`set_parent`/`set_transform` against the prefab asset directly if the toolset supports in-place prefab editing — otherwise instantiate the prefab into the open scene via `mcp__unity-editor-mcp__instantiate_prefab`, edit the instance, then `mcp__unity-editor-mcp__save_prefab_contents` / `mcp__unity-editor-mcp__apply_prefab_overrides` back onto the source asset, then delete the temporary scene instance).

- [ ] **Step 2: Create the `VisualPivot` → `BadgeFrame` + `ItemMesh` hierarchy**

Under the prefab's existing mesh-holding transform (currently the object rotated to local X ≈ -90°):
1. `mcp__unity-editor-mcp__create_gameobject` named `VisualPivot`, parented under the prefab root (same parent the current mesh transform has).
2. Reparent the existing mesh child under `VisualPivot` via `mcp__unity-editor-mcp__set_parent`, rename it `ItemMesh`, and set its local scale to `(0.9, 0.9, 0.9)` via `mcp__unity-editor-mcp__set_transform` (keep its existing local rotation/position relative to its old parent — since it's now nested one level deeper under `VisualPivot`, zero out `ItemMesh`'s own local rotation/position so `VisualPivot` alone carries the tilt).
3. `mcp__unity-editor-mcp__create_gameobject` named `BadgeFrame`, parented under `VisualPivot`, local position `(0,0,0)`, local rotation identity, local scale `(1,1,1)`.
4. `mcp__unity-editor-mcp__add_component` — `BadgeFrame` → `MeshFilter` (mesh = the imported `shield_badge` mesh) and `MeshRenderer` (material = `Assets/Materials/PowerUp_Badge_Blue.mat` for Shield).
5. Set `VisualPivot`'s local rotation to X = **-55°** (Y=0, Z=0) via `mcp__unity-editor-mcp__set_transform` — tilts the item from the old flat -90° toward the Cinemachine-follow camera's elevated-forward view.
6. `mcp__unity-editor-mcp__add_component` — `VisualPivot` → `PowerUpIdleMotion` (defaults from Task 1 are fine).

- [ ] **Step 3: Save the prefab and verify**

Apply/save the prefab (`mcp__unity-editor-mcp__save_prefab_contents` or `apply_prefab_overrides`, per whichever editing flow Step 1 used), clean up any temporary scene instance, `mcp__unity-editor-mcp__console` for 0 errors.

- [ ] **Step 4: Repeat Steps 1–3 for `PowerUp_SpeedBoost.prefab`**

Same recipe; `BadgeFrame` material = `Assets/Materials/PowerUp_Badge_Green.mat`.

- [ ] **Step 5: Repeat Steps 1–3 for `PowerUp_Invincibility.prefab`**

Same recipe; `BadgeFrame` material = `Assets/Materials/PowerUp_Badge_Gold.mat`.

- [ ] **Step 6: Manual play verification**

`mcp__unity-editor-mcp__editor_play` on the Meadow level scene (`Level_Meadow.unity`), let a power-up spawn and fall, `mcp__unity-editor-mcp__capture_game_view` to confirm: badge frame visible behind a smaller, camera-tilted item mesh, slow spin/bob visible over a couple of frames of polling. `mcp__unity-editor-mcp__editor_stop`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Prefabs/PowerUp_Shield.prefab Assets/Prefabs/PowerUp_SpeedBoost.prefab Assets/Prefabs/PowerUp_Invincibility.prefab
git commit -m "Add badge frame, camera tilt, and idle motion to Meadow power-up prefabs"
```

---

## Task 8: Restructure Playground power-up prefabs (badge, tilt, idle motion)

**Files:**
- Modify (via MCP): `Assets/Prefabs/KayKit_Shield.prefab`, `Assets/Prefabs/KayKit_SpeedBoost.prefab`, `Assets/Prefabs/KayKit_Invincibility.prefab`

**Interfaces:**
- Consumes: same as Task 7.
- Produces: nothing consumed by later tasks.

Same mechanical recipe as Task 7 (open prefab, build `VisualPivot` → `BadgeFrame` + `ItemMesh`, attach `PowerUpIdleMotion`), spelled out per-prefab below. One difference from Task 7: the Playground meshes (`heart_blue`/`diamond_blue`/`star_blue`) start at near-identity rotation (already close to upright), not flat -90° — so `VisualPivot`'s tilt only needs a small adjustment (**-15°** on X), not a large rotation change.

- [ ] **Step 1: `KayKit_Shield.prefab`**

Open the prefab for editing (same flow as Task 7 Step 1). Under the prefab's existing mesh-holding transform:
1. `mcp__unity-editor-mcp__create_gameobject` named `VisualPivot`, parented under the prefab root (same parent the current mesh transform has).
2. Reparent the existing mesh child under `VisualPivot` via `mcp__unity-editor-mcp__set_parent`, rename it `ItemMesh`, set local scale `(0.9, 0.9, 0.9)` via `mcp__unity-editor-mcp__set_transform`, zero out its own local rotation/position (the tilt lives on `VisualPivot` alone).
3. `mcp__unity-editor-mcp__create_gameobject` named `BadgeFrame`, parented under `VisualPivot`, local position `(0,0,0)`, local rotation identity, local scale `(1,1,1)`.
4. `mcp__unity-editor-mcp__add_component` — `BadgeFrame` → `MeshFilter` (mesh = the imported `shield_badge` mesh from Task 2) and `MeshRenderer` (material = `Assets/Materials/PowerUp_Badge_Blue.mat`).
5. Set `VisualPivot`'s local rotation to X = **-15°** (Y=0, Z=0) via `mcp__unity-editor-mcp__set_transform`.
6. `mcp__unity-editor-mcp__add_component` — `VisualPivot` → `PowerUpIdleMotion` (defaults from Task 1).
7. Save/apply the prefab, `mcp__unity-editor-mcp__console` for 0 errors.

- [ ] **Step 2: `KayKit_SpeedBoost.prefab`**

Repeat Step 1's sub-steps 1–7 on this prefab, `BadgeFrame` material = `Assets/Materials/PowerUp_Badge_Green.mat`, `VisualPivot` X = -15°.

- [ ] **Step 3: `KayKit_Invincibility.prefab`**

Repeat Step 1's sub-steps 1–7 on this prefab, `BadgeFrame` material = `Assets/Materials/PowerUp_Badge_Gold.mat`, `VisualPivot` X = -15°.

- [ ] **Step 4: Save all three prefabs and verify**

`mcp__unity-editor-mcp__console` for 0 errors after each.

- [ ] **Step 5: Manual play verification**

`mcp__unity-editor-mcp__editor_play` on `Level_Playground.unity`, confirm the same visual treatment as Task 7 Step 6, and specifically confirm the power-ups now read as visually distinct from `KayKit_Hazard` (badge frame + tint + idle motion the hazard doesn't have). `mcp__unity-editor-mcp__editor_stop`.

- [ ] **Step 6: Commit**

```bash
git add Assets/Prefabs/KayKit_Shield.prefab Assets/Prefabs/KayKit_SpeedBoost.prefab Assets/Prefabs/KayKit_Invincibility.prefab
git commit -m "Add badge frame, camera tilt, and idle motion to Playground power-up prefabs"
```

---

## Task 9: HUD container — vertical layout, left-edge anchor

**Files:**
- Modify (scene, via MCP): `Assets/Scenes/Core.unity` — `PowerUpHUDContainer` GameObject.

**Interfaces:**
- Consumes: nothing new.
- Produces: a left-anchored, top-down `VerticalLayoutGroup` container that Task 11's collect-FX animates into.

- [ ] **Step 1: Locate the container**

`mcp__unity-editor-mcp__find_gameobjects` for `PowerUpHUDContainer`.

- [ ] **Step 2: Remove `HorizontalLayoutGroup`, add `VerticalLayoutGroup`**

`mcp__unity-editor-mcp__remove_component` — `PowerUpHUDContainer` → `HorizontalLayoutGroup`.
`mcp__unity-editor-mcp__add_component` — `PowerUpHUDContainer` → `UnityEngine.UI.VerticalLayoutGroup`.
`mcp__unity-editor-mcp__set_component_properties` on the new `VerticalLayoutGroup`: `childAlignment = UpperLeft`, `spacing = 8`, `childForceExpandWidth = true`, `childForceExpandHeight = false`, `childControlWidth = false`, `childControlHeight = false` (mirrors the removed `HorizontalLayoutGroup`'s settings, swapped for vertical stacking).

- [ ] **Step 3: Re-anchor to the left edge**

`mcp__unity-editor-mcp__set_transform` (RectTransform) on `PowerUpHUDContainer`: `anchorMin = (0, 1)`, `anchorMax = (0, 1)`, `pivot = (0, 1)`, `anchoredPosition = (24, -90)` (top-left, matching the existing vertical offset the score/HUD area already uses, just moved to the left edge instead of center). `sizeDelta` stays `(300, 60)` for now — Task 10 changes `PowerUpHUDIcon`'s own size, and the container's height will grow via the layout group as icons stack, so `sizeDelta.y` here is not load-bearing once `VerticalLayoutGroup` is active.

- [ ] **Step 4: Save scene and verify**

`mcp__unity-editor-mcp__save_scene`, `mcp__unity-editor-mcp__console` for 0 errors.

- [ ] **Step 5: Manual play verification**

`mcp__unity-editor-mcp__editor_play`, grant a power-up via `mcp__unity-editor-mcp__eval`, `mcp__unity-editor-mcp__capture_game_view` to confirm the icon now appears top-left and stacks vertically when a second is granted. `mcp__unity-editor-mcp__editor_stop`.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Core.unity
git commit -m "Switch power-up HUD to a left-anchored vertical stack"
```

---

## Task 10: `PowerUpHUDIcon` — fill-bar timer

**Files:**
- Modify: `Assets/Scripts/PowerUps/PowerUpHUDIcon.cs`
- Modify (via MCP): `Assets/Prefabs/PowerUpHUDIcon.prefab`

**Interfaces:**
- Consumes: nothing new (still driven by `PowerUpHUD.HandleGranted`'s existing `Initialize(string, Sprite, float)` call — signature unchanged).
- Produces: `PowerUpHUDIcon.Initialize` behavior extended to also drive a fill bar; consumed by Task 11 (which adds the pulse-on-refresh behavior on top of this).

- [ ] **Step 1: Add a `fillBar` field and drive it from `Update`**

```csharp
// Assets/Scripts/PowerUps/PowerUpHUDIcon.cs
using UnityEngine;
using UnityEngine.UI;

public class PowerUpHUDIcon : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private TMPro.TextMeshProUGUI countdownText;
    [SerializeField]
    private TMPro.TextMeshProUGUI nameText;
    [SerializeField]
    private Image fillBar;

    private float remainingTime;
    private float totalDuration;
    private bool hasTimer;

    public void Initialize(string displayName, Sprite icon, float duration)
    {
        nameText.text = displayName;
        iconImage.sprite = icon;
        hasTimer = duration > 0f;
        remainingTime = duration;
        totalDuration = duration;
        countdownText.gameObject.SetActive(hasTimer);
        fillBar.gameObject.SetActive(hasTimer);
        UpdateCountdownText();
        UpdateFillBar();
    }

    private void Update()
    {
        if (!hasTimer)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime < 0f)
        {
            remainingTime = 0f;
        }
        UpdateCountdownText();
        UpdateFillBar();
    }

    private void UpdateCountdownText()
    {
        if (hasTimer)
        {
            countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
        }
    }

    private void UpdateFillBar()
    {
        if (hasTimer && totalDuration > 0f)
        {
            fillBar.fillAmount = remainingTime / totalDuration;
        }
    }
}
```

- [ ] **Step 2: Recompile and verify**

`mcp__unity-editor-mcp__recompile`, `mcp__unity-editor-mcp__console` — expect an error here since `fillBar` isn't wired in the prefab yet; that's fine, it's a missing-reference warning at runtime, not a compile error. Confirm specifically 0 **compile** errors.

- [ ] **Step 3: Add the fill bar to the prefab**

Open `Assets/Prefabs/PowerUpHUDIcon.prefab` for editing (same flow as Task 7 Step 1). Under the prefab root (currently `48x66`, children `Icon` at top `48x48` and `NameLabel` at bottom `48x18`):
1. `mcp__unity-editor-mcp__create_gameobject` named `FillBar`, parented under the prefab root, RectTransform: `anchorMin = (0,0)`, `anchorMax = (1,0)`, `pivot = (0.5,0)`, `anchoredPosition = (0, 18)`, `sizeDelta = (48, 4)` (a thin horizontal bar sitting just above `NameLabel`).
2. `mcp__unity-editor-mcp__add_component` — `FillBar` → `UnityEngine.UI.Image`. `mcp__unity-editor-mcp__set_component_properties`: `type = Filled`, `fillMethod = Horizontal`, `fillOrigin = 0` (left-to-right drain), `fillAmount = 1`, `color` a bright accent (e.g. `(1, 0.85, 0.2, 1)` — reuse the same gold used for `PowerUp_Badge_Gold.mat` for visual consistency between world pickup and HUD).
3. `mcp__unity-editor-mcp__set_component_properties` on the prefab root's `PowerUpHUDIcon` component: wire the new `fillBar` field → the `FillBar` GameObject's `Image`.
4. Resize the prefab root's `sizeDelta` to `(48, 70)` to fit the added bar without overlapping `NameLabel`.

- [ ] **Step 4: Save prefab and verify**

Save/apply, `mcp__unity-editor-mcp__console` for 0 errors (the earlier missing-reference warning should now be gone).

- [ ] **Step 5: Manual play verification**

`mcp__unity-editor-mcp__editor_play`, grant a timed power-up (SpeedBoost or Invincibility, not Shield which has `duration = 0`), `mcp__unity-editor-mcp__capture_game_view` at two points a couple seconds apart to confirm the fill bar visibly drains alongside the numeric countdown. `mcp__unity-editor-mcp__editor_stop`.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpHUDIcon.cs Assets/Prefabs/PowerUpHUDIcon.prefab
git commit -m "Add fill-bar timer to PowerUpHUDIcon"
```

---

## Task 11: `PowerUpCollectFX` — fly-to-camera-then-HUD animation

**Files:**
- Create: `Assets/Scripts/PowerUps/PowerUpCollectFX.cs`
- Create (via MCP): `Assets/Prefabs/PowerUpCollectFXLabel.prefab` (a large icon+name UI element used only during the fly-in)
- Modify: `Assets/Scripts/PowerUps/PowerUpHUD.cs`

**Interfaces:**
- Consumes: `PowerUpHUDIcon` (Task 10), `PowerUpManager.OnPowerUpGranted`/`OnPowerUpExpired` (existing, via `PowerUpHUD` which already subscribes).
- Produces: `PowerUpCollectFX.PlayNewGrant(PowerUpDefinition definition, RectTransform targetSlot, System.Action onArrived)` and `PowerUpCollectFX.PlayRefreshPulse(RectTransform existingIcon)` — called from `PowerUpHUD` below.

- [ ] **Step 1: Create the large fly-in label prefab**

Via the prefab-editing flow: a `RectTransform` root (`160x160`) with an `Image` child (`icon`, `120x120`, centered) and a `TextMeshProUGUI` child (`name`, below the icon, large bold font matching `NameLabel`'s font asset from `PowerUpHUDIcon.prefab`). Save as `Assets/Prefabs/PowerUpCollectFXLabel.prefab`. This is instantiated fresh per grant and destroyed after landing, so it does not need a dedicated controller script — `PowerUpCollectFX` drives its `Image`/`TextMeshProUGUI` directly by component reference on instantiation.

- [ ] **Step 2: Write `PowerUpCollectFX`**

```csharp
// Assets/Scripts/PowerUps/PowerUpCollectFX.cs
using UnityEngine;
using UnityEngine.UI;

public class PowerUpCollectFX : MonoBehaviour
{
    [SerializeField]
    private RectTransform flyInLabelPrefab;
    [SerializeField]
    private RectTransform canvasRoot;

    public void PlayNewGrant(PowerUpDefinition definition, RectTransform targetSlot, System.Action onArrived)
    {
        var label = Instantiate(flyInLabelPrefab, canvasRoot);
        var icon = label.GetComponentInChildren<Image>();
        var name = label.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        icon.sprite = definition.Icon;
        name.text = definition.DisplayName;

        label.anchoredPosition = Vector2.zero;
        label.localScale = Vector3.zero;

        LeanTween.scale(label, Vector3.one * 1.2f, 0.15f)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                LeanTween.move(label, targetSlot.position, 0.25f)
                    .setEase(LeanTweenType.easeInQuad);
                LeanTween.scale(label, Vector3.one * 0.3f, 0.25f)
                    .setEase(LeanTweenType.easeInQuad)
                    .setOnComplete(() =>
                    {
                        Destroy(label.gameObject);
                        onArrived?.Invoke();
                    });
            });
    }

    public void PlayRefreshPulse(RectTransform existingIcon)
    {
        LeanTween.cancel(existingIcon.gameObject);
        LeanTween.scale(existingIcon, Vector3.one * 1.15f, 0.1f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(existingIcon, Vector3.one, 0.1f)
                    .setEase(LeanTweenType.easeInQuad);
            });
    }
}
```

- [ ] **Step 3: Recompile and verify**

`mcp__unity-editor-mcp__recompile`, `mcp__unity-editor-mcp__console`, confirm 0 errors.

- [ ] **Step 4: Wire `PowerUpHUD` to use it**

```csharp
// Assets/Scripts/PowerUps/PowerUpHUD.cs
using System.Collections.Generic;
using UnityEngine;

public class PowerUpHUD : MonoBehaviour
{
    [SerializeField]
    private PowerUpManager powerUpManager;
    [SerializeField]
    private PowerUpHUDIcon iconPrefab;
    [SerializeField]
    private Transform container;
    [SerializeField]
    private PowerUpCollectFX collectFX;

    private readonly Dictionary<PowerUpDefinition, PowerUpHUDIcon> activeIcons = new Dictionary<PowerUpDefinition, PowerUpHUDIcon>();

    private void OnEnable()
    {
        powerUpManager.OnPowerUpGranted += HandleGranted;
        powerUpManager.OnPowerUpExpired += HandleExpired;
    }

    private void OnDisable()
    {
        powerUpManager.OnPowerUpGranted -= HandleGranted;
        powerUpManager.OnPowerUpExpired -= HandleExpired;

        foreach (var icon in activeIcons.Values)
        {
            Destroy(icon.gameObject);
        }
        activeIcons.Clear();
    }

    private void HandleGranted(PowerUpDefinition definition, float duration)
    {
        if (activeIcons.TryGetValue(definition, out var existingIcon))
        {
            existingIcon.Initialize(definition.DisplayName, definition.Icon, duration);
            collectFX.PlayRefreshPulse((RectTransform)existingIcon.transform);
            return;
        }

        var icon = Instantiate(iconPrefab, container);
        icon.gameObject.SetActive(false);
        collectFX.PlayNewGrant(definition, (RectTransform)container, () =>
        {
            icon.gameObject.SetActive(true);
            icon.Initialize(definition.DisplayName, definition.Icon, duration);
        });
        activeIcons[definition] = icon;
    }

    private void HandleExpired(PowerUpDefinition definition)
    {
        if (activeIcons.TryGetValue(definition, out var icon))
        {
            Destroy(icon.gameObject);
            activeIcons.Remove(definition);
        }
    }
}
```

- [ ] **Step 5: Recompile and verify**

`mcp__unity-editor-mcp__recompile`, `mcp__unity-editor-mcp__console`, confirm 0 errors.

- [ ] **Step 6: Add `PowerUpCollectFX` to the scene and wire references**

`mcp__unity-editor-mcp__add_component` — same GameObject `PowerUpHUDContainer` lives on (or its parent Canvas) → `PowerUpCollectFX`. `mcp__unity-editor-mcp__set_component_properties`: `flyInLabelPrefab` → `Assets/Prefabs/PowerUpCollectFXLabel.prefab`'s RectTransform, `canvasRoot` → the top-level Canvas RectTransform (so the fly-in label isn't clipped by the small HUD container's own RectTransform bounds). Then `mcp__unity-editor-mcp__set_component_properties` on the `PowerUpHUD` component: `collectFX` → the new `PowerUpCollectFX`.

- [ ] **Step 7: Save scene and verify**

`mcp__unity-editor-mcp__save_scene`, `mcp__unity-editor-mcp__console` for 0 errors.

- [ ] **Step 8: Manual play verification**

`mcp__unity-editor-mcp__editor_play`. Grant one power-up via `mcp__unity-editor-mcp__eval`: confirm via `mcp__unity-editor-mcp__capture_game_view` across a couple of polled frames that a large icon+name punches in then flies/shrinks to the HUD's top-left slot, landing as the real icon. Grant the **same** definition again while still active: confirm no second fly-in, existing icon pulses instead and its timer/fill bar resets. `mcp__unity-editor-mcp__editor_stop`.

- [ ] **Step 9: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpCollectFX.cs Assets/Scripts/PowerUps/PowerUpHUD.cs Assets/Prefabs/PowerUpCollectFXLabel.prefab Assets/Scenes/Core.unity
git commit -m "Add fly-to-HUD collect animation and refresh pulse for power-ups"
```

---

## Task 12: Full integration playtest

**Files:** none (verification-only task).

**Interfaces:** none.

- [ ] **Step 1: Both-types-back-to-back**

`mcp__unity-editor-mcp__editor_play` on `Level_Meadow.unity`. Via `mcp__unity-editor-mcp__eval`, grant SpeedBoost then immediately grant Invincibility. Confirm (via `capture_game_view` + polled `Time.timeScale` reads) the juice sequences interrupt-and-replace cleanly — no stacked/compounding zoom or shake, and the final state matches Invincibility's (larger) preset, not a blend of both.

- [ ] **Step 2: Death mid-slowdown**

Grant Invincibility, then within its slowdown window force `GameManager.Instance.GameOver()` via `eval`. Confirm `Time.timeScale` reads back `1` shortly after (via a polled `eval` read), not stuck below 1.

- [ ] **Step 3: Restart mid-slowdown**

From the game-over state, call `GameManager.Instance.RestartGame()` via `eval` while a slowdown would otherwise still be easing. Confirm `Time.timeScale == 1` immediately after.

- [ ] **Step 4: Three-stacked HUD**

Grant all three power-up types in sequence (staggered a fraction of a second apart via `eval` calls). `capture_game_view`: confirm all three stack vertically top-left without overlapping, each with an independent fill bar.

- [ ] **Step 5: Playground level parity**

Repeat Step 1 on `Level_Playground.unity` — confirm the Playground prefabs (Task 8) show the same badge/tilt/idle-motion treatment and the same juice behavior (juice is level-agnostic, driven by `PowerUpManager`/`PowerUpJuiceController` in the shared `Core` hierarchy, not per-level).

- [ ] **Step 6: Mobile aspect ratio check**

`mcp__unity-editor-mcp__resize_window` (or the equivalent Game View aspect override) to a narrow portrait ratio (e.g. 9:19.5). `capture_game_view` with three power-ups active: confirm the left-edge HUD doesn't collide with the score/pause UI.

- [ ] **Step 7: Console cleanliness**

`mcp__unity-editor-mcp__console`: confirm 0 errors/warnings introduced by this feature across the full sequence above. `mcp__unity-editor-mcp__editor_stop`.

- [ ] **Step 8: Profiler spot-check**

`mcp__unity-editor-mcp__get_performance_stats` during Step 4 (three power-ups active, HUD animating): confirm no sustained frame-time regression versus a baseline reading with no power-ups active (a brief spike during the grant-triggered tweens is expected and fine; a sustained per-frame cost is not).

No commit for this task — it's verification only. If any step surfaces a bug, fix it as part of the task whose code is at fault (re-open that task's file, patch, recompile, re-verify, commit a fix on top of that task's existing commit history) rather than deferring it.
