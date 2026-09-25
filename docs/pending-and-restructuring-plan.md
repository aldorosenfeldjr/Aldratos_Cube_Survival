# Pending work and restructuring plan

Written 2026-09-25 at the end of the power-up overhaul session, for a fresh session to pick up.
Start by reading `CLAUDE.md` (project map + rules). Do not re-explore the project.

## 1. State of the branch

- Branch `feature/theme-polish-and-companion`, **not merged to `dev`** (workflow: features -> `dev`,
  `main` only gets batched promotions).
- Done and committed this cycle: power-up overhaul (importance tiers, camera juice, vertical HUD with
  fly-in, flat badge pickups), unified camera shake (`CameraShaker`), single-level loading in `LevelSelect`.
- **Uncommitted, not from the AI, do not touch without asking:** `Assets/Scenes/Level_Playground.unity`
  (level design work), `ProjectSettings/EditorBuildSettings.asset` (Grass/KayKit -> Meadow/Playground
  scene renames), untracked `Assets/KayKit_Platformer_Pack/fbx(unity)*` and `New Folder.meta`.
- `Assets/_Recovery/` is most likely Unity crash-recovery output from the power failure. Check it, then
  delete it if it holds nothing you need.

## 2. Pending fixes and decisions (small)

**Status 2026-09-25:** Phase 1 done (fixes 1, 5, 6). Also done: 2 (top-right "II" pause button, `GameManager.pauseButton`),
3 (`CameraShaker.deathShakeForce` = 0.6), 4 (Meadow pickups get a BoxCollider fitted to the upright item, via the builder),
8 (decided: keep the tweens slowing with time scale), 9 (HUD prefab raycastTarget off, juice refs guarded, `PickupLifetime`
12s set by the builder on all six pickups; `GetComponentInChildren` left as is: one Image per label, once per grant;
`Major` tier left for future use). Still open: 7, 10.

| # | Item | Size | Notes |
|---|---|---|---|
| 1 | Escape-to-pause does nothing during a power-up slowdown | S | `GameManager.Update` infers state from `Time.timeScale == 0/1` (float equality). Use an explicit `isPaused`. Folded into Phase 1. |
| 2 | Mobile has no pause entry point | S | `Pause()` is private and Escape-only. Needs a UI button (PC + mobile target). |
| 3 | Player death shake is full strength | S | `Player.cs` calls `GenerateImpulse()` (force 1). Route through `CameraShaker` with its own scale. Your call on the amount. |
| 4 | Meadow pickup collider is still the flat original mesh while the visual is upright | S | Kept to avoid changing collection difficulty. Decide if a matching collider is wanted. |
| 5 | `Time.fixedDeltaTime = 0.02f * value` duplicated in two files; `ReturnToMainMenu` resets timeScale but not fixedDeltaTime | S | Folded into Phase 1 (`TimeScaleController`). |
| 6 | GameOver during a slowdown still shows a small double-dip | S | `Resume()` tweens from 0, not from the live value. Folded into Phase 1. |
| 7 | HUD container is parented under the `Score` group, not the Canvas | S | It hides with the score on game over via `GameOverMenu.scoreHud` (intentional). Reparenting needs that coupling replicated. |
| 8 | Collect-fly tweens are not time-scale independent | S | They stretch during the slowdown. Decide if that is wanted. |
| 9 | Minor polish: `raycastTarget` on decorative HUD graphics, unguarded serialized refs in `PowerUpJuiceController`, `GetComponentInChildren` in `PowerUpCollectFX`, `Major` importance tier unused, `AutoDestroyer` only on Meadow pickups | S | Low priority. |
| 10 | Pickup badges were checked statically and for rotation, not yet eyeballed while falling in a real run | S | Quick visual check next time you play. |

## 3. Product roadmap (agreed order, none started)

1. **Unlock/economy core**: unlock state, save/load, "watch ad to trial", "buy to own", shared selection-screen shell.
   Needs an ads/IAP integration decision (packages not installed yet).
2. **Character selection**: 12 colour variants of the box player, playful names, thumbnails in the main menu.
3. **Companion selection**: Farm Animals Animated (Quaternius) from `D:\Unity\Aldera_Assets\`; today the companion is a single
   hardcoded `CatWanderer`.
4. **Audio**: nothing exists yet (no mixer, no clips, no audio scripts). Its own design session.
5. Automated test suite, already deferred as its own session (see memory).

## 3b. Why this order

Characters and companions both need the economy core, so build it once first. Audio is independent and can go anywhere.

## 4. Restructuring plan (cheaper, more modular changes)

Findings: C# is only ~2,000 lines, so token cost comes from huge scene YAML, settings duplicated per
prefab/level, no project map (now added), and heavy process. Goal: each future change touches one small
file and is verified with one command.

**Phase 1: GameManager decomposition (M, do first)**
`GameManager` (345 lines) owns run loop, score, pause, hazard spawning, theme, camera swap and time-scale.
- Extract `TimeScaleController`: the only writer of `Time.timeScale`/`fixedDeltaTime` (pause, power-up
  slowdown, reset). Removes the dual-writer coordination (`CancelPauseTween`, `ForceResetTimeScale`) and
  fixes items 1, 5 and 6.
- Extract `HazardSpawner` (mirror `PowerUpSpawner`) and `RunState` (score, high score).
- Keep `GameManager` as a thin coordinator.

**Phase 2: one config asset (S): DONE 2026-09-25** (`GameConfig`, see CLAUDE.md map)
A `GameConfig` ScriptableObject for tunables now scattered in code/prefabs (shake scales, spawn intervals,
time constants). `LevelTheme` keeps only what genuinely differs per level.

**Phase 3: smaller scenes (M, supervised): UI DONE 2026-09-25** (Core 6.6k -> 2.5k lines; menus/HUD are prefabs in `Assets/Prefabs/UI/`; level scenes deliberately untouched, only with you present)
`Core.unity` (~6.6k lines): move Main Menu, HUD, Pause, Game Over, Level Select into prefabs so scene
edits and AI reads stay small. Level scenes (~8k / ~1.9k lines) are mostly decoration: only restructure
with you present (see memory: level-design scope feedback).

**Phase 4: cheap verification (S): smoke test DONE 2026-09-25** (`Tools/Smoke Test/Run Play Smoke Test`; EditMode tests still deferred to the test-suite session)
- An editor menu "Play Smoke Test" that runs the standard checks (enter play, grant each power-up, drop a
  crate, restart, game over) and prints a 5-line report. Replaces the long throwaway `eval` scripts.
- EditMode tests for `PowerUpManager` and `TimeScaleController` (fits the deferred test-suite session).

**Phase 5: same pattern for other content (S each)**
Editor builders like `PowerUpPickupBuilder` for hazards and level themes; new characters/companions become
table entries, not hand-edited prefabs.

## 5. Working agreement for the next session

- Read `CLAUDE.md` and this file first. Do not read whole `.unity`/`.prefab` files.
- Do only what is asked; treat stated visual directions as requirements.
- Small/medium change: do it directly, verify with one targeted measurement. No multi-agent review loops.
- Keep tunables in one place. Show the design in a few lines and get a yes before larger restructuring.
