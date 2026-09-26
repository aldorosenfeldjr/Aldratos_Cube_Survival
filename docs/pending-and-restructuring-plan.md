# Handoff: state, open items, roadmap

Updated 2026-09-25 at the end of the URP + Input System session. A fresh session starts by reading `CLAUDE.md`
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

## Done 2026-09-25: URP migration + Input System (branch `feature/urp-migration`)

- **URP 17.6**: `Assets/Settings/URP-Mobile` (Very Low..Medium, Android default) and `URP-PC` (High..Ultra,
  Standalone default + Graphics default). Forward, SRP Batcher, no HDR, no depth/opaque texture; PC has
  soft shadows/2 cascades/60m/MSAA 2x, Mobile hard shadows/1 cascade/20m/no MSAA. Converter ran Material,
  Read-only Material, Animation Clip. All renderers in the 3 scenes + game prefabs use URP-compatible shaders;
  only unused LeanTween/TMP example materials still use legacy shaders. Core camera/light have URP data.
- **PPv2 was not unused** (the old note was wrong): Core's Main Camera had a PostProcessLayer (SMAA) and a
  global PostProcessVolume with `Assets/Scenes/SampleScene_Profiles/Main Camera Profile.asset` (Depth of Field,
  Ambient Occlusion, Color Grading). Package removed, dead components stripped. Before/after captures look
  nearly identical. The profile asset is kept (now unreadable) until a decision on recreating it as a URP
  Volume + SSAO (see open items).
- **Input System 1.20** (`activeInputHandler: 1`, new only): `Player` (Pointer = mouse/touch halves, keyboard
  steer ramp from `GameConfig.KeyboardSteerRamp` matching the old axis feel, gamepad left stick, Space jump),
  `GameManager` (Esc / Android back = pause). Core's EventSystem uses `InputSystemUIInputModule`.
  TMP "Examples & Extras" scripts still call `UnityEngine.Input` (unused; they would throw if run).
- Smoke test 9/9 after each step.

## 2. Open items

| Item | Who | Notes |
|---|---|---|
| **CI is skipped, not compiling** | user | Repo has no Unity license secrets. Needs `UNITY_LICENSE` (contents of `C:\ProgramData\Unity\Unity_lic.ulf`), `UNITY_EMAIL`, `UNITY_PASSWORD` under GitHub Settings > Secrets > Actions. That `.ulf` did not exist on the PC: generate it in Unity Hub > Preferences > Licenses > Add > "Get a free personal license". Unverified whether Unity 6 writes it and whether the first licensed compile passes. The workflow skips with a warning until the secrets exist. |
| Eyeball pickup badges while falling in a real run | user | Only checked statically and by script. |
| HUD container sits under `Score`, not the Canvas | optional | Intentional (hides with the score on game over via `GameOverMenu.scoreHud`); reparenting needs that coupling replicated. |
| "Clear High Score" is smaller (340x50, 28pt) than other buttons | decided | User chose to keep it as is for now. |
| EditMode tests (`PowerUpManager`, `TimeScaleController`) | later | Belongs to the separate test-suite session. |
| Test controls after an Editor restart | user | The new input backend only activates after restarting Unity (script restart did not work). Check arrows/A-D, mouse-hold halves, Space, Esc. Then on a phone: touch halves + Android back. |
| Recreate old PPv2 look in URP? | user | Old profile: DoF + AO + Color Grading + SMAA. URP equivalents: Volume (DoF, Tonemapping) + SSAO renderer feature + camera SMAA. SSAO/DoF are costly on mobile: PC asset only, or drop. |
| iOS default quality | user | `QualitySettings` has no iOS entry; Android uses Medium (URP-Mobile). Probably set iOS to Medium too. |
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
