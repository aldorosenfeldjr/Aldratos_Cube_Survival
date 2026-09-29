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

## Token budget (sessions were costing ~0.4-1B tokens each; keep them small)
- **One task per session.** Finish, update the handoff doc, commit; the next task starts in a fresh
  session. Context is capped by `autoCompactWindow: 200k` in `.claude/settings.json`.
- **Screenshots only when the user asks** or a visual check truly needs one (each stays in context for
  the rest of the session). Prefer one `eval` measurement.
- **Console reads:** `level=error` (or `warn`), `tail` <= 5. Never `level=log` with a big tail.
- **No subagents or review loops unless the user asks.** Never read session transcripts or plugin skill files.
- The Unity plugin (`unity@unity-agent-plugin`, ~13k tokens/turn of skill listings) is disabled in
  `.claude/settings.json`; re-enable it only for a task that needs it (IAP/ads, URP migration).

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
| **Unlocks / selection screen** | `Scripts/Economy/` (`UnlockableDefinition`, `UnlockCatalog`, `UnlockService`, `SaveService`, `Wallet`) and `Scripts/Selection/` (`SelectionScreen`, `SelectionLayout`, `UnlockTile`, `GemCounter`). Prefabs `SelectionScreen`, `UnlockTile`, `GemCounter` in `Assets/Prefabs/UI/`. New character = a row in `CharacterBuilder` (*Tools > Characters > Rebuild Character Assets*); UI rebuilt by *Tools > UI > Rebuild Selection Screen*. Prices come from `EconomyConfig` tiers (mobile x2.5): never set on an item. |
| **Selection UI (teasers, transition, background, skin)** | Main Menu shows two `SelectionTeaser` panels (right side); `MainMenu.OpenCharacters/OpenCompanions` run the open transition (fade, teaser slide, `MenuBackgroundRig` pose move); `SelectionLayout` puts the card left/top and the preview right/bottom; `PreviewRotator` (drag-rotate) and `PreviewIdleLoop` (companion idle) live in `Scripts/Selection/`. Background = `MenuBackgroundRig` (own root in Core, parked at x=1000; diorama prefab `Assets/Prefabs/MenuShowcaseStage.prefab`, low-res RenderTexture, no real-time DoF). **These prefabs are generated: run *Tools > UI > Rebuild Selection Screen* (it also runs *Apply Selection Skin*, the Hyper Casual UI Pack table in `SelectionSkinBuilder`); never hand-edit them.** After a rebuild revert the churned `GameOverMenu.prefab` / `UnlockTile.prefab`. Screenshot UI with `eval` `ScreenCapture.CaptureScreenshot("Assets/_tmp.png")` (the MCP `screenshot`/`capture_game_view` do not show Overlay canvases); delete the file after. |
| **Ads / store / trials / interstitials** | `Scripts/Services/` (`IAdService`, `IStoreService`, `Services` locator; `Fake*` in the Editor, `Null*` elsewhere), `UnlockService` (trials), `InterstitialPacer`. Real mobile adapters (economy step 5) are not built: they implement the interfaces and set `Services.Ads` / `Services.Store` at startup. UI decides what to show from `IsAvailable`, never `#if`. |
| **Characters / companions** | Rows in `CharacterBuilder` (12 colours) and `CompanionBuilder` (cat + 7 animals; size = `WorldSize` column), run *Tools > Characters / Companions > Rebuild ... Assets*. `Player.ApplyLook` wears the selected colour; `CompanionSpawner` (in Core) spawns the selected companion, sized on its first frame by `CompanionFitter`. Never hand-edit the generated assets. |
| **Audio** | `AudioLibrary` asset (all clips + volumes), `AudioManager` (in Core, survives reloads), `ClickSound` on the shared `MenuButton` prefab, `SoundToggle` on the main menu. Call `AudioManager.Play(Sfx.X)`. Clips are generated placeholders: `Tools > Audio > Rebuild Placeholder Audio` fills only empty slots. |
| Power-up camera juice strength | `Assets/PowerUps/PowerUpJuiceSettings.asset` (one preset per tier) |
| Rendering (URP): shadows, MSAA, lights | `Assets/Settings/URP-Mobile.asset` (low tiers, Android/iOS) and `URP-PC.asset` (high tiers, PC) |
| Post-processing | Shared look: `Assets/Settings/PostProcessLook` (global `PostProcessVolume` in Core). PC-only: DoF in `URP-PC_Volume`, SSAO feature on `URP-PC_Renderer` |
| Input (Input System package only; no `UnityEngine.Input`) | `Player` (steer/jump), `GameManager` (Esc/back = pause); UI via `InputSystemUIInputModule` in Core with `Assets/Settings/UIInputActions` (default UI actions + Space as Submit) |
| Camera shake from landings | `CameraShaker` in Core (single owner; hazards call `ShakeImpact(force)`) |
| Pickup prefab look (all 8) + HUD power-up icons | `Assets/Editor/PowerUpPickupBuilder.cs` -> menu *Tools > PowerUps > Rebuild Pickup Prefabs*: one small glossy collectable per kind (heart/bolt/star/diamond), soft glow, sparkles, sphere collider; the HUD icons are rendered from the same models (`PortraitBuilder.RenderToSprite`) |
| **UI skin (Hyper Casual UI Pack)** | `Editor/UIPack.cs` (pack paths, 9-slice borders, Baloo2 text style, rect helpers), `GameUIBuilder` (*Tools > UI > Apply Game UI Skin*: HUD score card + chips, power-up rows with shrinking bar, pause tile, Pause / Game Over + trial / Success / New Record / Level Select), `SelectionSkinBuilder` (*Apply Selection Skin*: selection screens, tiles, gem chip, teasers, Main Menu buttons). One table row per element: change the row, run the menu item, never hand-edit prefabs. Edits to prefab *instances* in Core must be recorded (`PrefabUtility.RecordPrefabInstancePropertyModifications`) or they vanish on save; layout overrides on the Score / New Record instances are cleared by the builder. |
| **Web test build (play on a phone, no Mac/App Store needed)** | Separate repo `E:\GitHub\Aldratos_Cube_Survival_WebTest`, pushed `main` -> GitHub Pages at https://aldorosenfeldjr.github.io/Aldratos_Cube_Survival_WebTest/. Full process, gotchas and history: `docs/pending-and-restructuring-plan.md` sections 2b/2d. Short version: switch target to WebGL (`switch_build_target`, ~1-2 min, spawns a shader-compiler batch that can make the Editor unresponsive for a bit — that's normal, not a hang), `build` into that folder, commit + push that repo, switch back to `StandaloneWindows64`. Custom template `Assets/WebGLTemplates/MobileFullscreen` (iOS "Add to Home Screen" = no Safari chrome); WebGL debug symbols are ON (`PlayerSettings.WebGL.debugSymbolMode = External`) so a crash reported from the phone shows real C# stack frames, not `wasm-function[N]`. **Gotcha:** any TMP font asset left in `Dynamic` atlas mode can get its texture silently corrupted by a target switch (reimport clears/partially-resets it) — `UIPack.EnsureFont()` locks the shared Baloo2 asset to `Static` for this reason; verify `font.characterTable.Count` after a switch before trusting a font-heavy build. **Gotcha:** this dev machine runs low on RAM (< 1 GB free) with Unity + VS Code both open; before a WebGL build, kill any idle `AssetImportWorker*` process (`Get-Process`, check `Responding`/CPU not advancing, `Stop-Process` — Unity respawns one on demand) and close scenes/tabs not needed, or the build can fail with Windows error 1455. |

## Rules that keep changes cheap
- **One source of truth per setting.** Never copy tuning (shake, badge size, ...) into each
  prefab/level. Put it in one script/asset, and make levels reference it.
- New pickup type/level look: edit the table in `PowerUpPickupBuilder`, run the menu item.
- New hazard: make the prefab (mesh + material), add a row to `HazardBuilder`, run *Tools > Hazards >
  Rebuild Hazard Prefabs* (adds tag, rigidbody, `Hazard`, breaking effect, collider).
- New level: author the scene with a `LevelInfo`, add a row to `LevelBuilder`, run *Tools > Levels >
  Rebuild Level Assets* (creates the `LevelTheme` asset, `LevelRegistry` entry and Build Settings entry;
  warns if the scene's `LevelInfo` does not point at the theme). Both builders are idempotent.
- Do only what was asked. Do not add unrequested behavior (e.g. idle spin/bob) — stated
  visual directions (e.g. "face the camera, no rotation") are requirements, not suggestions.
- Hand-testing shortcuts (Editor only, Edit Mode only, they change your REAL save): *Tools > Debug > Add 10000 Gems To Save* and *Reset Save*. Automated tests never touch the real save.
- Unit tests (EditMode, `Assets/Tests/EditMode`, ~5 s): unity-editor-mcp `run_tests` with `mode=editor` and a `filter` (the unfiltered result is long). Pure logic only (save, wallet, pricing, unlocks/trials, interstitial pacer, run state, catalog data). New pure logic gets tests here; anything needing Play Mode goes in the smoke test.
- Verify cheaply: recompile + `console` (level=error, small `tail`), then run the flow check:
  unity-editor-mcp `menu` -> `Tools/Smoke Test/Run Play Smoke Test` (~15s), then read
  `console` (tail 2): one "SMOKE TEST PASS/FAIL n/n" entry, failures named. Covers menus,
  score, every power-up + HUD, hazards, pause-in-slowdown, restart, game over. When a feature
  adds a flow, add a check block to `Assets/Editor/PlaySmokeTest.cs` (or a `PlaySmokeTest.<Feature>.cs` partial; the selection checks include a layout audit at 8 screen sizes: reuse it for new screens). For anything narrower,
  one targeted `eval` measurement (first `eval` right after entering play mode often fails: retry). Avoid dumping full console logs (stack traces are huge) and avoid
  multi-agent review loops for small changes.
- Scene state gotchas: hazards can end a test run; disabling the player collider makes it
  fall. Editor "open scenes" persist in `Library/LastSceneManagerSetup.txt`.
