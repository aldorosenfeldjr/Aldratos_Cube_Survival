# Power-Up Visual & UI Overhaul — Design Spec

Status: approved for implementation planning
Date: 2026-09-20

## 1. Goal

The power-up system (see `2026-09-13-powerups-design.md`) works functionally
but reads as low-budget: pickups (`Heart_Full`/`Coin`/`Star` from the
Platformer Pack) are shrunk-and-tinted stock props lying flat on the ground,
visually too close to the hazard crates (`KayKit_Hazard`) they share a scene
with. The HUD is a plain horizontal icon+text strip with no motion. This
overhaul makes power-ups read as *collectible, exciting items* and makes
collecting one feel like a game, not a variable increment — while building a
**tiered juice system** (importance → shake/zoom/slowdown intensity) that
scales cleanly as more power-up types are added later, not something
re-tuned per item.

Fourth of four related features brainstormed together in this session
(character selection, companion selection, powerup overhaul, audio); this
one is independent — no files shared with the other three.

## 2. Current state (for reference)

- `PowerUpPickup`/`PowerUpDefinition`/`PowerUpManager`/`PowerUpHUD`/`PowerUpHUDIcon`
  in `Assets/Scripts/PowerUps/` — data-driven, one `ScriptableObject` per type.
  No changes to this data model's public shape; this overhaul is additive.
- World meshes — **two full prefab sets, one per level theme**
  (`LevelTheme.SpeedBoostPrefab`/`InvincibilityPrefab`/`ShieldPrefab`,
  swapped by `PowerUpSpawner.ApplyTheme`), both need the same treatment:
  - Meadow (`PowerUp_Shield`/`PowerUp_SpeedBoost`/`PowerUp_Invincibility`,
    Platformer Pack `Heart_Full`/`Coin`/`Star` meshes) — rotated flat (local
    X ≈ -90°, lying face-up) at `m_LocalScale (1,1,1)`.
  - Playground (`KayKit_Shield`/`KayKit_SpeedBoost`/`KayKit_Invincibility`,
    KayKit_Platformer_Pack `heart_blue`/`diamond_blue`/`star_blue` meshes) —
    rotation near-identity (already close to upright) at `m_LocalScale (1,1,1)`.
  - `KayKit_Hazard` (Playground's hazard) is from the same KayKit_Platformer_Pack
    as the Playground power-ups, which is why they currently read as
    near-identical props.
- Main Camera (`Core.unity`): position `(0, 2.75, -10)`, rotation identity
  (looking down +Z, no tilt), FOV 60, perspective. Not top-down — a slightly
  elevated, forward-facing view.
- HUD: `PowerUpHUDContainer` under a `HorizontalLayoutGroup`, anchored
  center-ish (`AnchoredPosition (-107.36, -90)`), spawns/destroys
  `PowerUpHUDIcon` instances instantly on `OnPowerUpGranted`/`OnPowerUpExpired`.
- LeanTween is already the project's tweening library (used in
  `GameOverMenu`, `GameManager`, `MenuBackgroundEffect`,
  `MenuSelectionAnimator`, `SunGlowPulse`) — this design uses it exclusively,
  no new tweening dependency.

## 3. World pickup visual

Applies identically to **all six** prefabs (both theme sets). Each gets the
same new pivot structure:

```
<PowerUp prefab root>       (pickup collider/rigidbody/PowerUpPickup stay here)
 └─ VisualPivot              (rotated to tilt the item toward the camera's
    │                          elevated-forward Cinemachine-follow view — the
    │                          Meadow set goes from flat -90° to ~-50°/-60° on
    │                          X; the Playground set, already near-upright,
    │                          only needs a small tilt adjustment, not a full
    │                          rotation change)
    ├─ BadgeFrame             (shield_badge.fbx from KayKit_Adventurers_2.0_FREE,
    │                          not yet imported — import just this mesh once,
    │                          reused by all six prefabs; material tinted per
    │                          type — gold=Invincibility, blue=Shield,
    │                          green=SpeedBoost — same tint regardless of
    │                          theme, so the type-color meaning stays
    │                          consistent across levels)
    └─ ItemMesh                (existing Heart_Full/Coin/Star or
                                 heart_blue/diamond_blue/star_blue, scaled to
                                 90% of current size, nested in front of the
                                 badge)
```

`VisualPivot` gets a slow idle spin (Y-axis) + gentle bob (LeanTween,
looping) so it reads as "collectible" at rest, not a static prop — further
separating it from the inert hazard crates at a glance, independent of the
badge/tint work.

Badge tint is per-type, not per-theme, and applied at the prefab level (each
of the six prefabs gets its own tinted `BadgeFrame` material instance
matching its type) — no new `PowerUpDefinition` field required.

## 4. Tiered juice system (camera + time effects)

### Importance tiers

Add an `Importance` enum to the base `PowerUpDefinition`:

```csharp
public enum PowerUpImportance { Minor, Major, Supreme }
```

Every power-up, present and future, picks one tier at data-authoring time —
no per-item tuning. Current roster: `SpeedBoostDefinition` = `Minor`,
`ShieldDefinition` = `Minor`, `InvincibilityDefinition` = `Supreme`. `Major`
is reserved for a future power-up stronger than the basics but short of
Invincibility — the tier exists now so later additions don't need new plumbing.

**Cinemachine note:** the game's Main Camera is driven live by Cinemachine
(`GameManager.mainVCam`/`zoomVCam`, `CinemachineCamera` + `CinemachineFollow`
following the player; `zoomVCam` already swaps in on game-over). Tweening the
Camera GameObject's transform/FOV directly would be overwritten every frame
by the `CinemachineBrain`. Juice effects must instead use Cinemachine's own
mechanisms: `mainVCam.Lens.FieldOfView` (a live-read field, safe to tween
directly) for the zoom punch, and a `CinemachineImpulseSource` +
`CinemachineImpulseListener` pair (Cinemachine's built-in shake system) for
the shake, not manual position tweening.

### PowerUpJuiceSettings (new ScriptableObject, single instance)

One preset per tier, each with:
- `shakeAmplitude` / `shakeFrequency` / `shakeDuration` (fed to
  `CinemachineImpulseSource.GenerateImpulseWithForce`/impulse definition)
- `zoomPunchFovDelta` (subtracted from `mainVCam.Lens.FieldOfView` for the
  punch-in) / `zoomDuration`
- `slowdownTimeScale` / `slowdownHoldDuration` / `slowdownEaseBackDuration`
  (`slowdownTimeScale = 1` for tiers that shouldn't dip time, if ever needed)

All tiers get *some* zoom/slowdown per the "every power-up should feel like a
process" requirement — `Minor` = super-fast, small punch (e.g. ~0.08s
zoom, ~0.1s slowdown to ~0.6 scale); `Supreme` = the same shape but
larger/longer (bigger zoom punch, deeper slowdown ~0.3 scale, longer hold),
signaling the bigger moment without a different mechanic. `Major` sits
between. Tuning is designer-editable on this one asset, not scattered across
power-up definitions.

### PowerUpJuiceController (new, sits on `GameManager`'s GameObject alongside `mainVCam`)

Holds a reference to `mainVCam` (`CinemachineCamera`) and a
`CinemachineImpulseSource` (added to the same object). Subscribes to
`PowerUpManager.OnPowerUpGranted`. On grant:
1. Look up the preset for `definition.Importance` in `PowerUpJuiceSettings`.
2. Cancel any in-flight zoom/slowdown LeanTween IDs it owns, then start new
   ones — never stacks concurrent tweens from rapid/overlapping grants
   (`mainVCam.Lens.FieldOfView` tweened from base 60 down by
   `zoomPunchFovDelta` and back, via `LeanTween.value(float,float,float)`
   driving the Lens field on update).
3. Call `impulseSource.GenerateImpulseWithForce(shakeAmplitude)` for the
   shake — Cinemachine's own impulse system, picked up automatically by a
   `CinemachineImpulseListener` added to `mainVCam` (and `zoomVCam`, so a
   shake during the game-over zoom swap still reads). No manual position
   tweening.
4. All LeanTween tweens (zoom FOV, slowdown value) run on **unscaled time**
   (LeanTween's unscaled-time mode), so the slowdown doesn't slow down its
   own ease-back.
5. On `PowerUpManager.ResetAll()` (game-over/restart), force `Time.timeScale
   = 1` immediately — safety net so a death mid-slowdown can never leave the
   game stuck slow.

This is entirely additive to `PowerUpManager` — no changes to its public
surface, it only gains one more subscriber.

## 5. HUD redesign

- `PowerUpHUDContainer`: swap `HorizontalLayoutGroup` → `VerticalLayoutGroup`;
  re-anchor from the current center-ish position to the left edge of the
  screen (standard buff-bar placement), top-anchored so it doesn't collide
  with bottom-anchored mobile safe-area UI.
- `PowerUpHUDIcon` restructured from icon+two-text-labels into a proper item
  row: badge-framed icon, name label, and a **horizontal fill bar** (`Image`,
  `fillAmount = remainingTime / duration`) replacing/supplementing the plain
  numeric countdown — reads at a glance without requiring the player to read
  numbers mid-run.
- **Collect animation** (new `PowerUpCollectFX`, triggered by `PowerUpHUD`
  instead of its current instant `Instantiate`): on a *new* icon (first grant
  of a type not already active), spawn a large icon+name label that
  LeanTween-punches toward the camera, holds briefly, then flies/shrinks into
  the HUD's left-edge slot, becoming the real `PowerUpHUDIcon`. On a
  **re-grant** of an already-active type, skip the fly-in and instead play a
  lighter pulse/flash on the existing icon (refresh feedback without a
  duplicate flying label).
- The fly-in animates toward the *container*, not a precomputed slot
  position, so `VerticalLayoutGroup` can reflow freely if another icon
  expires mid-animation.

## 6. Edge cases & error handling

- **Rapid/overlapping triggers** (two different power-ups grabbed in quick
  succession, or the same one re-grabbed): `PowerUpJuiceController`
  cancel-and-restarts its own tween IDs rather than stacking multiple
  concurrent shake/zoom/timescale tweens.
- **Timescale safety net**: `Time.timeScale` is force-reset to 1 on
  `PowerUpManager.ResetAll()`, so a death immediately after a Supreme-tier
  grant can never leave the game stuck in slowdown.
- **Re-grant vs. new grant**: `PowerUpHUD` must distinguish "icon already
  exists for this definition" (pulse/refresh) from "new icon" (full fly-in),
  matching `PowerUpManager.Grant`'s existing refresh-vs-create branch.
- **HUD reflow during animation**: fly-in targets the container so mid-flight
  layout changes (another power-up expiring) don't leave it animating to a
  stale position.

## 7. Out of scope for this pass (explicitly deferred)

- Sound effects for pickup/juice moments — audio is its own architectural
  sub-project (zero audio implementation exists in the project yet), to be
  brainstormed and spec'd separately.
- Any change to `PowerUpManager`'s public API, `IPowerUpEffect`, or the
  spawner — this pass is visual/feel only.
- A `Major`-tier power-up itself (the enum value exists now so it's ready,
  but no new power-up type is being added in this pass).

## 8. Testing/verification plan

No automated test suite exists for gameplay feel yet (deferred as its own
future session). Manual playtest checklist, via the Unity CLI/MCP connection:

- Each power-up type collected solo: confirm badge-framed, tilted-toward-
  camera visual; idle spin/bob; correct tier juice (shake/zoom/slowdown
  scaled to `Minor` vs `Supreme`); HUD fly-in animation lands correctly.
- Two different types collected back-to-back: both juice sequences
  interrupt-and-replace cleanly, no stacked/compounding shake or zoom.
- Same type re-collected while active: HUD icon pulses/refreshes, no
  duplicate fly-in, timer bar resets.
- Invincibility (Supreme) collected, player dies moments later while the
  slowdown is still easing back: confirm `Time.timeScale` recovers to 1
  immediately on restart, not left stuck low.
- Three power-ups active simultaneously: HUD vertical stack reflows
  correctly, fill bars all track independently.
- Narrow/portrait mobile aspect ratio: left-edge HUD doesn't collide with
  other UI (score, pause button); PC aspect ratio unaffected.
- Console stays clean (0 errors) through a full grant → juice → expire →
  restart cycle.
- Quick Profiler pass: confirm no per-frame GC churn from the new tweens
  (camera shake/zoom run every grant, not every frame, but worth confirming).
