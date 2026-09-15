# Multi-Scene Levels (Theming) Design

**Status:** Approved by user in chat, written up for record and self-review.

## Goal

Introduce the ability to play the existing game (falling-hazard survival with
powerups) under different visual themes, starting with a KayKit-Platformer-Pack
("Mario-ish") theme alongside the current grass/farm theme. The gameplay loop,
scoring, and powerup system do not change — only the environment, hazard
model, and pickup models vary per theme.

This is the first time the project has more than one Unity scene. Today,
`Assets/Scenes/Game.unity` contains everything: cameras, lighting, the
player, `GameManager`, `PowerUpManager`, the full UI `Canvas`, and all
environment art, mixed together in one scene. `ProjectSettings/EditorBuildSettings.asset`
currently lists zero scenes — even the existing scene was never registered
for a build.

## Non-goals (explicitly out of scope for this pass)

- Sequential level unlocking (clear level N to unlock N+1, gated by score).
  The user has stated this is planned for later. This design's data model
  must not preclude adding it (see "Future: unlock conditions" below), but
  no locking logic is implemented now — every level is freely selectable.
- A "change level" option from the Game Over screen. Today `GameOverMenu`
  offers Restart (replay same run) and Quit (exit app entirely); that stays
  as-is. Switching themes only happens via Main Menu → Level Select.
- Per-theme physical layout (different ground height, platform footprint,
  or fall-boundary). See "Physical footprint constraint" below.
- Per-theme lighting/camera tuning. Cameras and the directional light stay
  in the Core scene, shared across all themes, for this pass.
- Resolving the pre-existing `NewPlayer`/`Player` duplicate root objects, or
  the world-space `ScoreLabel`/`ScoreText` vs. Canvas `Score` duplication.
  Both predate this work; noted here so they aren't rediscovered as new
  bugs, but fixing them is a separate, unrelated cleanup.

## Architecture: Core scene + additive Level scenes

**`Assets/Scenes/Core.unity`** (renamed from today's `Game.unity`): the
persistent scene, loaded once and never reloaded for the lifetime of a play
session. Contains everything that must exist exactly once regardless of
theme:

- `Main Camera`, `Main VCam`, `Zoom VCam`, `ZoomTarget`, `Directional Light`
- `NewPlayer` (and the pre-existing, already-inactive `Player` duplicate —
  left as-is, out of scope)
- `FallDownTrigger`
- `GameManager`, `PowerUpManager`
- `EventSystem`
- `Canvas` (MainMenu, the new Level Select screen, Score/HUD, PauseMenu,
  GameOverMenu, NewRecordScreen — all UI)
- `ScoreLabel`/`ScoreText` (world-space, pre-existing, unrelated to Canvas)

**`Assets/Scenes/Level_Grass.unity`** (new; content moved out of today's
`Game.unity`): today's `Environment` GameObject hierarchy unchanged —
platforms, trees, fence, mushrooms, rocks, grass clumps, `AirParticles`,
the wandering cat (`Cat Lite` + rig), decorative `Pipe`/`Ladder_short`/
`Sign_LeftRight`.

**`Assets/Scenes/Level_KayKit.unity`** (new): a KayKit-Platformer-Pack-themed
rebuild of the same physical layout — see "KayKit content plan" below.

Level scenes are loaded **additively** on top of Core
(`SceneManager.LoadSceneAsync(levelSceneName, LoadSceneMode.Additive)`) when
the player picks a level from Level Select, and the previously-loaded Level
scene (if any) is unloaded first when switching themes.

### Physical footprint constraint

`GameManager`'s hazard/pickup spawn math (`spawnMinX`/`spawnMaxX`,
`spawnHeight` on `PowerUpSpawner`; the `Random.Range(-7, 7)` / height-11
spawn in `GameManager.SpawnHazards()`) and `FallDownTrigger`'s bounds are
Core-side and fixed — they are not part of any per-theme data. For this to
remain a true "reskin" with zero gameplay-code branching per theme, every
Level scene must present the same physical footprint as the current grass
level: same ground height (~0.04–0.05, the value already established this
session), same platform X-range, same fall-off boundary. `Level_KayKit`'s
platform pieces must be sized/positioned to match this footprint; only the
visual meshes change.

If a future theme genuinely needs a different physical layout, that is a
larger change (per-theme spawn config) explicitly deferred, not silently
supported by this design.

## Data model: `LevelTheme`

A new ScriptableObject, `Assets/Scripts/Levels/LevelTheme.cs`:

```csharp
public class LevelTheme : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private GameObject hazardPrefab;
    [SerializeField] private GameObject speedBoostPrefab;
    [SerializeField] private GameObject invincibilityPrefab;
    [SerializeField] private GameObject shieldPrefab;

    public string DisplayName => displayName;
    public GameObject HazardPrefab => hazardPrefab;
    public GameObject SpeedBoostPrefab => speedBoostPrefab;
    public GameObject InvincibilityPrefab => invincibilityPrefab;
    public GameObject ShieldPrefab => shieldPrefab;
}
```

One asset per theme: `Assets/Levels/LevelTheme_Grass.asset` (referencing
today's `Crate`/`PowerUp_SpeedBoost`/`PowerUp_Invincibility`/`PowerUp_Shield`
prefabs — the current game becomes theme #1 under this system, not a
special case) and `Assets/Levels/LevelTheme_KayKit.asset` (referencing the
new KayKit-reskinned prefabs).

Each Level scene exposes its theme via a small marker component,
`Assets/Scripts/Levels/LevelInfo.cs`, placed on a root GameObject in that
scene:

```csharp
public class LevelInfo : MonoBehaviour
{
    [SerializeField] private LevelTheme theme;
    public LevelTheme Theme => theme;
}
```

### Future: unlock conditions

Not implemented now. When sequential unlocking is built, it is expected to
add a field like `[SerializeField] private int requiredScoreToUnlock` to
`LevelTheme` and read it in Level Select's tile-population logic. Nothing
in this pass's design should need rework to accommodate that — noted here
so the shape is anticipated, not so it gets built early.

## Wiring: `GameManager`/`PowerUpSpawner` accept a theme

`GameManager.hazardPrefab` and `PowerUpSpawner.pickupPrefabs` are currently
fixed `[SerializeField]` references set once on the Core-scene components.
They need a runtime setter so a loaded Level's theme can override them
before a run starts:

- `GameManager` gains `public void ApplyTheme(LevelTheme theme)` setting
  `hazardPrefab = theme.HazardPrefab`.
- `PowerUpSpawner` gains `public void ApplyTheme(LevelTheme theme)` setting
  `pickupPrefabs = new[] { theme.SpeedBoostPrefab, theme.InvincibilityPrefab, theme.ShieldPrefab }`.

Both existing `[SerializeField]` fields stay as the fallback/default
(so the Core scene still compiles and runs standalone during editing without
a theme applied), but at runtime they are always overwritten before a run
starts.

## Menu flow: Level Select

A new `LevelSelect` panel under `Canvas`, following the existing menu
patterns (`MainMenu`, `GameOverMenu` — `CanvasGroup` fade, `EventSystem`
first-selected handling, matching font/animation style).

Flow:
1. `MainMenu.Play()` — same fade-out as today, but `OnComplete()` now shows
   `LevelSelect` instead of calling `gameManager.Enable()` directly.
2. `LevelSelect` populates its level list at `OnEnable`/`Start` from a
   small discoverable set of `LevelInfo`-bearing Level scene names (see
   "Level registry" below) — not hardcoded UI buttons — so adding a third
   theme later is a data change, not a UI rebuild. Each tile shows the
   theme's `DisplayName`.
3. Picking a tile: `LevelSelect` loads the chosen Level scene additively,
   unloading any previously-loaded Level scene first, finds the new
   scene's `LevelInfo` component, calls `GameManager.Instance.ApplyTheme(...)`
   and `PowerUpSpawner`'s `ApplyTheme(...)`, then calls
   `gameManager.Enable()` (mirroring today's `MainMenu.OnComplete()`) and
   hides `LevelSelect`.

### Level registry

Since scenes can't be discovered generically at runtime without one, a
small `Assets/Scripts/Levels/LevelRegistry.cs` ScriptableObject holds an
ordered list of Level scene names (just strings — `"Level_Grass"`,
`"Level_KayKit"`) for `LevelSelect` to iterate and load by name. This is
the one piece of hand-maintained data when a new theme is added, alongside
the new scene and its `LevelTheme` asset.

## KayKit content plan

Source: `D:\Unity\Aldera_Assets\KayKit_Platformer_Pack_1.0_FREE` (CC0
license, no attribution required). Using the `blue` color variant for the
initial build (consistent with existing UI accent colors this session has
used — final color call made during implementation if a different variant
reads better once placed).

- **Ground/platform pieces:** `platform_*_blue.fbx` variants sized to match
  the current three top platform pieces' footprint (`Platform_TopLeft/Mid/Right`
  with `MeshCollider`s) plus the two decorative bottom pieces (no collider).
- **Decoration:** `arch_blue`, `railing_*_blue`, `signage_*_blue`,
  `pipe_*_blue` (a literal pipe fits the Mario theme directly, mirrors the
  existing level's decorative `Pipe` object), `spring_pad_blue` as a purely
  decorative nod (no new gameplay mechanic — springs do not add bounce
  behavior in this pass, that would be new gameplay, out of scope).
- **Hazard reskin (`LevelTheme_KayKit.HazardPrefab`):** `bomb_A_blue.fbx`
  (or `ball_blue.fbx` if bomb's collision shape proves awkward during
  implementation — a bomb reads clearly as "avoid this" and fits the
  platformer theme).
- **Pickup reskins:** `diamond_blue.fbx` → Speed Boost, `star_blue.fbx` →
  Invincibility, `heart_blue.fbx` → Shield — mirrors the current
  Coin/Star/Heart conceptual mapping (fast-valuable / power / survival).
- No wandering-cat equivalent is planned for this theme (the cat is a
  grass-theme-specific detail); `Level_KayKit` simply omits an NPC unless a
  natural KayKit equivalent surfaces during implementation.

## Build Settings

Register `Core`, `Level_Grass`, `Level_KayKit` in
`ProjectSettings/EditorBuildSettings.asset` (`Core` first/index 0, since a
built player's boot scene must be the persistent one). This closes the
pre-existing gap where zero scenes were registered.

## Testing plan

Mirrors this session's established live-Editor verification discipline:

1. After the Core/Level_Grass split: load `Core` + `Level_Grass`
   additively, play through a full run (menu → level select → play → take
   damage → game over → restart) exactly as today, confirm no regression —
   this is the highest-risk step, since it's a structural migration of
   already-working content, not new functionality.
2. After `Level_KayKit` is built: same full-run pass under the new theme —
   hazard falls and ends a run on contact, all three pickups fall, land,
   grant their effect, expire/consume correctly, HUD shows the KayKit
   theme's icons (existing icon-rendering script from the powerups plan
   reused for the new prefabs).
3. Confirm switching themes twice in a row (Grass → KayKit → Grass) leaves
   no duplicate/orphaned scene objects — the additive-unload step must
   actually unload, not just load on top.
4. Clean console throughout (`compilationFailed: false`, `consoleErrors: 0`),
   consistent with every prior phase this session.

## Summary of what ships

Two playable themes (Grass, KayKit) selectable from a new Level Select
screen between Main Menu and gameplay. Adding a third theme later is: one
new Level scene built to the same physical footprint, one new `LevelTheme`
asset, one line in `LevelRegistry` — no changes to `GameManager`,
`PowerUpManager`, `Player`, or the powerup system.
