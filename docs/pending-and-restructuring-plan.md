# Handoff: state, open items, roadmap

Updated 2026-09-25 at the end of the restructuring session. A fresh session starts by reading `CLAUDE.md`
(project map + rules), then this file. Do not re-explore the project.

## 1. State

- **The restructuring plan is finished (phases 1-5).** GameManager split (`TimeScaleController`,
  `HazardSpawner`, `RunState`), `GameConfig` asset for tunables, menus/HUD as prefabs in `Assets/Prefabs/UI/`
  (Core 6.7k -> 2.5k lines), one-click smoke test, `HazardBuilder`/`LevelBuilder`. Details live in the
  CLAUDE.md map; git history has one commit per step.
- **Branches:** `dev` == `feature/theme-polish-and-companion`, pushed to `origin/dev`. `main` is untouched
  (batched promotions only). Merge convention: features -> `dev`.
- **Verification:** menu `Tools/Smoke Test/Run Play Smoke Test` (~15s), then read the console. 9/9 at the
  end of this session.
- The user's Playground edits, renamed Build Settings scenes and `KayKit_Platformer_Pack/fbx(unity)` are
  committed (8b67a9d on `feature/theme-polish-and-companion`). Untracked `New Folder.meta` is an orphan
  (no folder); Unity deletes it on open. `LevelBuilder` never rewrites Build Settings unless a scene is
  missing from it.

## NEXT TASK: migrate to URP (clears Unity Hub's "Built-In Render Pipeline is deprecated")

Branch `feature/urp-migration` (off 8b67a9d) is already created and checked out. Needs the Editor open
with unity-editor-mcp connected. Facts already checked, do not re-explore:
- Unity 6000.6.0f1. No pipeline asset anywhere (`GraphicsSettings`/`QualitySettings` customRenderPipeline 0).
- `com.unity.postprocessing` 3.5.4 is in the manifest but **unused** (no PostProcessVolume/Layer in scenes,
  prefabs or scripts): remove it.
- No custom rendering code (no `OnRenderImage`, `Blit`, `CommandBuffer`, `Shader.Find`, `new Material`).
- Custom shaders: only BOXOPHOBIC `Skybox Cubemap Blend/Extended` (skybox shaders, check they still
  render) and LeanTween's archived example shader (unused). ~80 `.mat` files + KayKit fbx embedded materials.

Steps:
1. Add `com.unity.render-pipelines.universal` (let Package Manager pick the 6000.6 version), remove PPv2.
2. Create `Assets/Settings/URP-Asset` + renderer. Carry over the current quality tuning (2 cascades, 60
   shadow distance, 1 per-pixel additional light, no reflection probes); keep it mobile-friendly (SRP
   Batcher on, no HDR/MSAA unless visibly needed). Assign as the default pipeline and on every quality level.
3. Window > Rendering > Render Pipeline Converter, Built-in to URP: Rendering Settings, Material Upgrade,
   Read-only Material (fbx), Animation Clip. Then grep `.mat` files for the Built-in Standard shader
   (`guid: 0000000000000000f000000000000000`) and particle shaders to catch anything left pink.
4. Check lights/ambient per level (URP lighting looks different), cameras get `UniversalAdditionalCameraData`.
5. Verify: recompile + console (error), smoke test, one before/after screenshot per level (Main menu,
   Meadow, Playground) since this is a visual change. Update `PowerUpPickupBuilder` / `HazardBuilder`
   material paths only if the converter replaced materials rather than upgrading in place.
6. Update the `project_performance` memory (no longer Built-in RP) and this doc, then commit.

## 2. Open items

| Item | Who | Notes |
|---|---|---|
| **CI is skipped, not compiling** | user | Repo has no Unity license secrets. Needs `UNITY_LICENSE` (contents of `C:\ProgramData\Unity\Unity_lic.ulf`), `UNITY_EMAIL`, `UNITY_PASSWORD` under GitHub Settings > Secrets > Actions. That `.ulf` did not exist on the PC: generate it in Unity Hub > Preferences > Licenses > Add > "Get a free personal license". Unverified whether Unity 6 writes it and whether the first licensed compile passes. The workflow skips with a warning until the secrets exist. |
| Eyeball pickup badges while falling in a real run | user | Only checked statically and by script. |
| HUD container sits under `Score`, not the Canvas | optional | Intentional (hides with the score on game over via `GameOverMenu.scoreHud`); reparenting needs that coupling replicated. |
| "Clear High Score" is smaller (340x50, 28pt) than other buttons | decided | User chose to keep it as is for now. |
| EditMode tests (`PowerUpManager`, `TimeScaleController`) | later | Belongs to the separate test-suite session. |
| Playground decoration/layout | user | User wants to do it personally; do not scatter decoration (see memory). |

## 3. Product roadmap (agreed order, none started)

1. **Unlock/economy core**: unlock state, save/load, "watch ad to trial", "buy to own", shared selection-screen
   shell. Needs an ads/IAP integration decision first (packages not installed).
2. **Character selection**: 12 colour variants of the box player, playful names, thumbnails in the main menu.
3. **Companion selection**: Farm Animals Animated (Quaternius) from `D:\Unity\Aldera_Assets\`; today the
   companion is one hardcoded `CatWanderer`.
4. **Audio**: nothing exists (no mixer, clips or scripts). Its own design session.
5. Automated test suite: its own session.

Order: characters and companions both need the economy core, so build that once first. Audio is independent.
When 2 and 3 start, give them the same table-driven builder pattern (`PowerUpPickupBuilder`, `HazardBuilder`,
`LevelBuilder`): a new character/companion is a table row, not a hand-edited prefab.

## 4. Working agreement

- Follow the "Token budget" rules in `CLAUDE.md` (one task per session; no screenshots, log dumps or
  subagents unless asked).
- Read `CLAUDE.md` and this file first. Never read whole `.unity`/`.prefab` files; query the live Editor or
  grep one field.
- Do only what is asked; treat stated visual directions as requirements.
- Show the design in a few lines and get a yes before larger restructuring. Small changes: do directly,
  verify with the smoke test.
- Tunables go in `GameConfig`; UI changes go in the prefabs, not in Core.

## 5. Tooling gotchas (unity-editor-mcp)

- The first `eval` right after entering play mode usually fails with a network error: retry it.
- For anything longer than a few lines, write a `.cs` file to the scratchpad and run it with `eval_file`.
- `capture_game_view` needs a project-relative `save_path` (no `..`) and saves under `Assets/`: move the PNG
  out and delete its `.meta`, or it lands in the project.
- Children of a prefab instance cannot be deleted from script: unpack the instance first.
- Play-mode waits: `wait_for isPlaying == false` returns "interrupted" when play exits; read the console instead.
- Before a risky asset edit, back up the file and compare `md5sum` afterwards (Unity may rewrite line endings
  even when content is unchanged; only mark assets dirty when a value really changed).
