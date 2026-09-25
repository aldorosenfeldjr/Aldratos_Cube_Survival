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
| Run loop, score UI, pause input, theme apply | `GameManager` (thin coordinator) |
| Score + high score (PlayerPrefs) | `RunState` (plain C#, owned by `GameManager`) |
| Hazard spawning | `HazardSpawner` (on the GameManager object) |
| `Time.timeScale` / `fixedDeltaTime` (pause, power-up slowdown, reset) | `TimeScaleController` — the only writer; own root object in Core |
| Level loading (one level live at a time) | `LevelSelect`, `LevelRegistry`, `LevelInfo`, `LevelTheme` |
| Power-ups (data-driven, one asset per type) | `Scripts/PowerUps/` — `PowerUpDefinition` (+ `PowerUpImportance` tier), `PowerUpManager`, `PowerUpHUD`, `PowerUpJuiceController` |
| **Gameplay tunables** (player, spawn timing/area, drag, shake, pause fade) | `Assets/Resources/GameConfig.asset` via `GameConfig.Instance` — edit values there, never in scripts/prefabs/scenes |
| **UI** (menus, HUD, buttons) | Prefabs in `Assets/Prefabs/UI/`: `MenuButton` + `MenuLabel` (shared style: edit once), and one prefab per menu (`PauseMenu`, `GameOverMenu`, `MainMenu`, `LevelSelect`, `Score`, ...). Core only holds an instance of each: change UI in the prefab, not in Core. Menu buttons call methods on their own menu script (no scene refs inside prefabs). |
| Power-up camera juice strength | `Assets/PowerUps/PowerUpJuiceSettings.asset` (one preset per tier) |
| Camera shake from landings | `CameraShaker` in Core (single owner; hazards call `ShakeImpact(force)`) |
| Pickup prefab look (all 6) | `Assets/Editor/PowerUpPickupBuilder.cs` -> menu *Tools > PowerUps > Rebuild Pickup Prefabs* |

## Rules that keep changes cheap
- **One source of truth per setting.** Never copy tuning (shake, badge size, ...) into each
  prefab/level. Put it in one script/asset, and make levels reference it.
- New pickup type/level look: edit the table in `PowerUpPickupBuilder`, run the menu item.
- Do only what was asked. Do not add unrequested behavior (e.g. idle spin/bob) — stated
  visual directions (e.g. "face the camera, no rotation") are requirements, not suggestions.
- Verify cheaply: recompile + `console` (level=error, small `tail`), then run the flow check:
  unity-editor-mcp `menu` -> `Tools/Smoke Test/Run Play Smoke Test` (~15s), then read
  `console` (tail 2): one "SMOKE TEST PASS/FAIL n/n" entry, failures named. Covers menus,
  score, every power-up + HUD, hazards, pause-in-slowdown, restart, game over. When a feature
  adds a flow, add a check block to `Assets/Editor/PlaySmokeTest.cs`. For anything narrower,
  one targeted `eval` measurement (first `eval` right after entering play mode often fails: retry). Avoid dumping full console logs (stack traces are huge) and avoid
  multi-agent review loops for small changes.
- Scene state gotchas: hazards can end a test run; disabling the player collider makes it
  fall. Editor "open scenes" persist in `Library/LastSceneManagerSetup.txt`.
