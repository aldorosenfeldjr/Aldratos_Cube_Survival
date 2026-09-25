# Aldratos Cube Survival — Project Guidelines

## Target platforms
Build for **PC and mobile (iOS and Android)**. Keep this in mind for every feature:
input handling (touch + mouse/keyboard), performance/GPU budget, UI scaling across
aspect ratios, and platform-specific behavior (e.g. no "Exit"/"Quit" option on mobile
builds — App Store/Play Store guidelines prohibit in-app quit buttons; PC/Editor keep it).

## Project status
This is the developer's first game project. It's an ongoing, long-term iterative
project — new features and ideas will keep being added over time. A public release
is planned, but far in the future, after the current backlog of ideas is implemented.
Prioritize correctness and maintainability over shipping speed.

## Quality bar
The goal is to bring this from a personal/learning project to a professional-quality
one: consistent UI/UX patterns (fonts, sizes, animations) across all menus, clean
Editor/console output, and solid performance on real hardware, not just "works in
the Editor."

## Project map (read this instead of exploring)
Pending work and the restructuring plan: `docs/pending-and-restructuring-plan.md`.
Scenes are huge YAML (Core ~6.6k lines, Level_Meadow ~8k). **Never read `.unity`/`.prefab`
files whole** — query the live Editor (unity-editor-mcp `eval`, `get_component_properties`)
or grep for one field. Levels are additive scenes loaded into `Core` by `LevelSelect`; each
level scene holds a `LevelInfo` -> `LevelTheme` asset (hazard + power-up prefabs per level).

| Concern | Where |
|---|---|
| Run loop, score, pause, theme apply | `GameManager` (also owns time-scale pause tween) |
| Level loading (one level live at a time) | `LevelSelect`, `LevelRegistry`, `LevelInfo`, `LevelTheme` |
| Power-ups (data-driven, one asset per type) | `Scripts/PowerUps/` — `PowerUpDefinition` (+ `PowerUpImportance` tier), `PowerUpManager`, `PowerUpHUD`, `PowerUpJuiceController` |
| Power-up camera juice strength | `Assets/PowerUps/PowerUpJuiceSettings.asset` (one preset per tier) |
| Camera shake from landings | `CameraShaker` in Core (single owner; hazards call `ShakeImpact(force)`) |
| Pickup prefab look (all 6) | `Assets/Editor/PowerUpPickupBuilder.cs` -> menu *Tools > PowerUps > Rebuild Pickup Prefabs* |

## Rules that keep changes cheap
- **One source of truth per setting.** Never copy tuning (shake, badge size, ...) into each
  prefab/level. Put it in one script/asset, and make levels reference it.
- New pickup type/level look: edit the table in `PowerUpPickupBuilder`, run the menu item.
- Do only what was asked. Do not add unrequested behavior (e.g. idle spin/bob) — stated
  visual directions (e.g. "face the camera, no rotation") are requirements, not suggestions.
- Verify cheaply: recompile + `console` (level=error, small `tail`), then one targeted
  `eval` measurement. Avoid dumping full console logs (stack traces are huge) and avoid
  multi-agent review loops for small changes.
- Scene state gotchas: hazards can end a test run; disabling the player collider makes it
  fall. Editor "open scenes" persist in `Library/LastSceneManagerSetup.txt`.
