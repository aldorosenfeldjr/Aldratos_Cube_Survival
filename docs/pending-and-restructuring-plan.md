# Handoff: state, open items, roadmap

Updated 2026-09-27 (economy steps 1-4, characters, companions, audio and the unit-test suite are done; only economy step 5,
the real ad/store adapters, is open: see section 3a. The character/companion selection experience was redesigned on
2026-09-27: see section 2c). A fresh session starts by reading `CLAUDE.md` (project map + rules), then this file. Do not
re-explore the project.

## 1. State

- **The restructuring plan is finished (phases 1-5).** GameManager split (`TimeScaleController`,
  `HazardSpawner`, `RunState`), `GameConfig` asset for tunables, menus/HUD as prefabs in `Assets/Prefabs/UI/`
  (Core 6.7k -> 2.5k lines), one-click smoke test, `HazardBuilder`/`LevelBuilder`. Details live in the
  CLAUDE.md map; git history has one commit per step.
- **Branches:** `dev` has everything (theme polish, URP migration merged 2026-09-26), pushed to `origin/dev`. `main` is untouched
  (batched promotions only). Merge convention: features -> `dev`.
- **Verification:** menu `Tools/Smoke Test/Run Play Smoke Test` (~45 s, ~110 checks incl. layout audits at 8 screen sizes
  for both selection screens), then read the console; plus 54 EditMode unit tests (`run_tests`, mode editor). Both green at
  the end of 2026-09-26. Sound itself is judged by ear (the test proves clips exist, are audible and are requested).
- The user's Playground edits, renamed Build Settings scenes and `KayKit_Platformer_Pack/fbx(unity)` are
  committed (8b67a9d on `feature/theme-polish-and-companion`). Untracked `New Folder.meta` is an orphan
  (no folder); Unity deletes it on open. `LevelBuilder` never rewrites Build Settings unless a scene is
  missing from it.

## Done 2026-09-25: URP migration + Input System (branch `feature/urp-migration`)

- **URP 17.6**: `Assets/Settings/URP-Mobile` (Very Low..Medium, Android default) and `URP-PC` (High..Ultra,
  Standalone default + Graphics default). Forward, SRP Batcher, no depth/opaque texture; PC has
  soft shadows/2 cascades/60m/MSAA 2x, Mobile hard shadows/1 cascade/20m/no MSAA. Converter ran Material,
  Read-only Material, Animation Clip. All renderers in the 3 scenes + game prefabs use URP-compatible shaders;
  only unused LeanTween/TMP example materials still use legacy shaders. Core camera/light have URP data.
- **Post-processing rebuilt in URP** (PPv2 had been active on Core's Main Camera: SMAA, DoF, AO, Color
  Grading, Vignette). Shared look for all platforms: global `PostProcessVolume` in Core with
  `Assets/Settings/PostProcessLook` (ACES tonemapping, exposure +1, hue +3, lift/gamma tint, vignette 0.4).
  PC only: Bokeh DoF in `URP-PC_Volume` (the PC pipeline asset's own volume profile) and SSAO on
  `URP-PC_Renderer` (ambient-only). Camera: SMAA, HDR on. Both URP assets: HDR + HDR grading (needed for
  ACES/exposure). Old PPv2 profile deleted. Visual match checked against the pre-migration capture.
- **iOS default quality** was already Medium (URP-Mobile), same as Android; no change was needed.
- **Input System 1.20** (`activeInputHandler: 1`, new only): `Player` (Pointer = mouse/touch halves, keyboard
  steer ramp from `GameConfig.KeyboardSteerRamp` matching the old axis feel, gamepad left stick, Space jump),
  `GameManager` (Esc / Android back = pause). Core's EventSystem uses `InputSystemUIInputModule`.
  TMP "Examples & Extras" scripts still call `UnityEngine.Input` (unused; they would throw if run).
  Menus: `Assets/Settings/UIInputActions` = default UI actions + Space as Submit (old Input Manager parity).
  During a live run `GameManager` clears UI selection so Space jumps instead of re-pressing the Pause button.
- Smoke test 9/9 after each step.

## 2. Open items

| Item | Who | Notes |
|---|---|---|
| **CI is skipped, not compiling** | user | Repo has no Unity license secrets. Needs `UNITY_LICENSE` (contents of `C:\ProgramData\Unity\Unity_lic.ulf`), `UNITY_EMAIL`, `UNITY_PASSWORD` under GitHub Settings > Secrets > Actions. That `.ulf` did not exist on the PC: generate it in Unity Hub > Preferences > Licenses > Add > "Get a free personal license". Unverified whether Unity 6 writes it and whether the first licensed compile passes. The workflow skips with a warning until the secrets exist. |
| Eyeball pickup badges while falling in a real run | user | Only checked statically and by script. |
| HUD container sits under `Score`, not the Canvas | optional | Intentional (hides with the score on game over via `GameOverMenu.scoreHud`); reparenting needs that coupling replicated. |
| "Clear High Score" is smaller (340x50, 28pt) than other buttons | decided | User chose to keep it as is for now. |
| Unit tests for `PowerUpManager` / `TimeScaleController` | later | They need Play Mode (coroutines, singletons); covered by the smoke test only. The 54 EditMode tests cover the pure logic. No PlayMode test assembly yet. |
| **Economy step 5: real ad/store adapters** | needs you | **Platform reality (2026-09-26):** the user has a Windows PC and an iPhone 12, no Mac. Unity iOS builds need macOS + Xcode, and Expo Go cannot run Unity games. Unity Android and iOS build support are NOT installed yet (only Windows/WebGL). Realistic path: install Android build support (free, Windows) and test ads/IAP on an Android emulator or borrowed Android phone; for the iPhone use a cloud Mac / macOS CI runner + an Apple Developer account. Development uses Google test ad IDs, so no AdMob app has to be registered yet (AdMob account: rosen.softwarebuilder@gmail.com). Rest of the note: Not built: needs your AdMob app + ad unit IDs, store product setup (Play Console / App Store Connect) and a phone to test on. Also open: age question at first launch, parent check before purchases for under-13, UMP consent (EU), ATT (iOS). Game code is ready: implement `IAdService` / `IStoreService` and assign `Services.Ads` / `Services.Store` at startup on mobile. The Unity plugin must be re-enabled for this (see CLAUDE.md). |
| **Placeholder audio** | user | All sounds and the music are generated tones (`Tools > Audio > Rebuild Placeholder Audio`). Drop real clips into `Assets/Resources/AudioLibrary.asset` (the rebuild never overwrites filled slots). Not tested by ear. No volume sliders (Sound On/Off exists on the main menu and the pause menu). Hazard landing sound is wired but not covered by a test. |
| Animals without Walk/Run | note | Pug, Piggy, Woolly and Llama only ship Idle and Jump animations, so they stand and idle instead of wandering. Only Daisy (cow), Bolt (horse) and Stripes (zebra) wander, like the cat. The cat's Idle clips do not loop (existing setup). |
| Companion sizing | user | Each animal's on-screen size is a `WorldSize` column in `CompanionBuilder` (cat 0.52 = its old size). Values for the animals are a first guess: check them in a real run. `CatWanderer` also drives the new animals (name kept to avoid churn). |
| Check post-processing on a phone | user | Mobile gets tonemapping/grading/vignette + SMAA + HDR (no DoF/SSAO). Profile on a real device; drop SMAA or HDR on URP-Mobile if it costs too much. |
| Selection preview lighting | done 2026-09-27 | The preview stage now has its own range-limited neutral Point light (`SelectionScreen.CreateStage`); Core's sunset ambient still adds a faint lavender cast (see 2c). |
| Selection screen not eyeballed in portrait | user | Landscape checked visually once; portrait and 7 other sizes only by the numeric layout audit. Also unchecked: the grid scrolling with >1 row (`EnsureVisible`). |
| Editor rewrites URP materials | note | `git status` shows `Gem.mat`, `PowerUp_*_Material.mat`, `Assets/Characters/*.mat` as modified after play/compile: Unity syncing `_Color` from `_BaseColor` (float noise). Do not commit them. |
| Playground decoration/layout | user | User wants to do it personally; do not scatter decoration (see memory). |

## 2b. Web test build (iPhone Safari), added 2026-09-27

Purpose: try the game on the user's iPhone 12 without a Mac or Apple account. Branch `feature/web-test`; output folder
`E:\GitHub\Aldratos_Cube_Survival_WebTest` (its own repo, served by GitHub Pages; the game repo stays free of build binaries).

- **Build:** switch the target to WebGL (installed), then `build` with option `Development` and that output path. A Development
  build turns on the fake ad/store services and starts a fresh save with 10,000 gems (`SaveService.DevelopmentBuildStartingGems`;
  never in the Editor). Release builds keep ads/store "unavailable" until the real adapters exist.
- **WebGL settings:** default quality Medium (URP-Mobile), Gzip + decompression fallback (GitHub Pages sends no compression headers),
  `FileSync.jslib` flushes saves to IndexedDB, `File.Replace` falls back to copy + delete.
- **Memory gotcha:** WebGL builds need several GB. The first attempt died with Windows error 1455 ("paging file too small") and
  Unity then reported a bogus "scripts have compile errors" that stuck until a real script change forced a recompile. Close other
  apps before building. The C# code itself compiles fine under WebGL.
- **Editor target:** after building, switch back to Windows (*File > Build Profiles*, or `switch_build_target` StandaloneWindows64),
  otherwise Play Mode and the smoke test run on the WebGL target.
- **Published:** https://aldorosenfeldjr.github.io/Aldratos_Cube_Survival_WebTest/ (public repo `Aldratos_Cube_Survival_WebTest`,
  GitHub Pages from `main`). ~17 MB. Verified in headless Chrome with an iPhone user agent and 390x844 viewport: loads in ~18 s,
  mobile layout, menu, level select and gameplay render, no console errors. Open item: Chrome logs "Shader 'Hidden/Universal Render
  Pipeline/Edge Adaptive Spatial Upsampling' is stripped ... PostProcessing render passes will not execute": the web build may lose
  the colour grading/vignette look (not yet compared side by side).
- **Lesson:** the web screenshot exposed a stray `Pug` instance in the Core scene (a failed `CompanionBuilder` run left its
  temporary model in the open scene and it was saved). Removed; the builder now always destroys its instance and the smoke test fails
  if an animated model other than the spawned companion sits at the root of Core.
- To republish: switch to WebGL, `build` (no Development option) into `E:\GitHub\Aldratos_Cube_Survival_WebTest`, commit and push
  that repo, switch back to Windows. A WebGL build takes ~10 min the first time and needs several GB of free memory.
- Untested assumptions: audio starts only after the first tap (browser rule); touch input and layouts on iPhone Safari; save
  persistence across reloads; performance.

## 2c. Selection experience redesign, added 2026-09-27 (branch `feature/selection-experience-redesign`)

Spec `docs/superpowers/specs/2026-09-27-selection-experience-redesign-design.md`, plan `docs/superpowers/plans/2026-09-27-selection-experience-redesign.md`.
Built: the Main Menu shows two teaser panels on the right (`SelectionTeaser`: title + the catalog's first three items, the equipped one
framed, locked ones badged; the panel is the button); tapping one fades the menu, slides the teaser toward the card's side, eases the
menu background to a new pose and fades the screen in; Back reverses it. The screens are flipped (card/grid left or top, hero preview
right or bottom, one `heroShare` = 0.5 in `SelectionLayout`) and sit on a see-through scrim. The preview is the real player mesh
(`CharacterDefinition.previewMesh`, `YellowBox.fbx`) or the companion's own idle loop (`PreviewIdleLoop`; wanderers also walk in
place), and both drag-rotate (`PreviewRotator`). The background is `MenuBackgroundRig` (own root object in Core, parked at x=1000):
a diorama (`Assets/Prefabs/MenuShowcaseStage.prefab`, KayKit props + one Point light) rendered by its own camera at 1/4 resolution
(the low resolution is the "out of focus" look, no real-time DoF, so it is cheap on mobile); it drifts slowly and eases between
three poses (`MainMenu`, `Characters`, `Companions`, editable on the component); its camera renders only while `MenuBackground` is
visible. The selection UI wears the Hyper Casual UI Pack (`Assets/Hyper_Casual_UI`): `SelectionSkinBuilder` (*Tools > UI > Apply
Selection Skin*) is the one table for it; the shared `MenuButton`/`MenuLabel` prefabs and every other menu are NOT skinned yet.
- **`SelectionScreenBuilder` (*Tools > UI > Rebuild Selection Screen*) now generates all of the above** (teaser prefab, Main Menu
  teasers and layout, draggable preview, scrim) and runs the skin: rebuild instead of hand-editing these prefabs. It also churns
  `GameOverMenu.prefab` (regenerated fileIDs) and `UnlockTile.prefab` (TMP override noise): revert those two after a rebuild.
- Cat: it rendered default grey because `Cat Lite.fbx` imports no materials and `CompanionBuilder` never assigned one. Rows now take a
  `Material` (the cat wears `Tex_Cat_Lite.mat`, what the old scene cat had). The selection preview stage has its own neutral Point light.
- Debug: *Tools > Debug > Unlock All Characters & Companions* (Edit Mode only, changes your REAL save).
- Smoke test: 134 checks, green; EditMode unit tests 57/57. Running `run_tests` with a modified open scene pops a modal "Scene(s) Have Been
  Modified" dialog that blocks the Editor and every MCP call (dismiss it by hand, or `save_all` first).

| Open item | Who | Notes |
|---|---|---|
| Eyeball it | user | Blur strength (is 1/4 resolution "out of focus" enough? else add one cheap box-blur pass), pose values (`MenuBackgroundRig`), diorama look/brightness (its light is a placeholder Point light), teaser and `heroShare` proportions, the cat's remaining faint lavender cast (Core's sunset ambient), and the portrait phone layout of the Main Menu (only the numeric audit covers it). The background texture rebuilds on rotation, but the diorama's vertical FOV was tuned for landscape, so portrait crops it. |
| Diorama set-dressing | user | `MenuShowcaseStage` is three KayKit props on purpose; arrange it yourself (see the level-decoration note in memory). |
| Skin the rest of the UI | done, see 2d | HUD, power-up pickups and icons, Pause / Game Over / Success / Level Select / New Record were done in 2d; the shared `MenuButton`/`MenuLabel` prefabs themselves are still plain (nothing uses them un-skinned except the mobile-only Quit paths). |
| Cat spawns below the ground | later | Level ground is flat at y=0.250 but `CompanionSpawner` sits at y=0.0527, so a freshly spawned cat is about 0.2 below the ground plane and only pops up when `CatWanderer` first moves. It made the smoke check "default companion (the cat) ... on the ground and wandering" fail in ~40% of runs (it measured a cat that had already wandered); the check now destroys and respawns the cat first, so it is deterministic and still asserts the spawn-time seating. Fixing the real quirk = move the spawner onto the ground (then update the check's expectation). |
| Scene-authored pieces | note | `MenuBackgroundRig`, its `Background` RawImage (must stay the FIRST child of `Canvas/MenuBackground`, checked by the smoke test) and `MenuShowcaseStage.prefab` live in Core / are hand-authored, not produced by a builder: `Rebuild Selection Screen` does not touch them. |
| Portrait phones | done, eyeball | Below a 1000-unit canvas width (portrait phones are about 850-880 wide) `MainMenu.ApplyTeaserLayout` puts the teasers under the centred buttons instead of at the right edge; the smoke test audits 1920 / 1440 / 1000 / 876 / 844 / 760-wide canvases. Not seen on a real phone. |
| Deferred review notes | later | Menu background renders every frame (up to the 120 Hz `targetFrameRate` set in `MainMenu.Start`; consider ~30 Hz or cancelling the drift while the camera is off); `Play()` is not covered by the open-transition guard (a teaser tap in its 0.2 s fade moves the background and the menu is then destroyed); `OnScreenClosed` fades in while already interactable (a tap in that 0.3 s starts a fade-out on the same CanvasGroup); teaser tile names/lock badge are ~9-12 units tall (check legibility on a phone); `CharacterDefinition.previewMesh` renders nothing if null and `CharacterBuilder` does not warn when `YellowBox.fbx` fails to load; a companion with an off-centre pivot may wobble when dragged. |

## 2d. UI professionalization + collectables, added 2026-09-28 (same branch)

Done after the first delivery was judged too thin. Everything uses the Hyper Casual UI Pack's own composed panels, pills, chips and icons.
- **Power-ups**: all 8 pickup prefabs are small glossy collectables (heart = Shield, bolt = Speed Boost, star = Invincibility, purple
  diamond = Gem Multiplier) with a soft glow (`Assets/UI/SoftGlow.png`), sparkles and a sphere collider (0.42), built by
  `PowerUpPickupBuilder`; the HUD icons are rendered from the same models (`PortraitBuilder.RenderToSprite`), so they always match.
- **HUD** (`GameUIBuilder`): score card top-left, Best and Gems chips under it, power-up rows (icon, name, seconds left, shrinking
  green bar over a dark track; the track hides for power-ups without a timer), orange pause tile top-right. The Score object now hangs
  from its top-left corner; `MainMenu` hides it above the screen (`ScoreHiddenY`) and slides it to `ScoreShownY` on Play.
- **Menus** (`GameUIBuilder`): Pause, Game Over (+ trial-over offer), Success, New Record, Level Select each sit on a teal/gold pack
  panel with correctly sized pills (nothing wider than ~560). Selection screens (`SelectionSkinBuilder`): gold-framed teal card,
  dark tiles with a gold selected frame, orange back tile, coin gem chip (`GemCounter` is now a chip: slot + icon + label), buttons at
  fixed width via `LayoutElement` (the VerticalLayoutGroup no longer stretches them), teasers on the pack's HUD panel.
- **Text**: Baloo2 ExtraBold TMP asset + dark-outline material (`Assets/Fonts`, generated by `UIPack.EnsureFont`); the title on the
  Main Menu keeps its display font.
- **Gotchas found**: edits made to a prefab *instance* from an editor script are lost on save unless recorded
  (`PrefabUtility.RecordPrefabInstancePropertyModifications`); Core's `Score` and `NewRecordScreen` instances carried old size/position
  overrides that hid the new design (the builder clears them); some pack PNGs import as plain textures (`UIPack.Sprite` fixes that);
  `PortraitBuilder` used to destroy its RenderTexture before its camera (console error, fixed); an `EditorApplication.update` callback
  left over from a screenshot script spams errors until a script reload (`EditorUtility.RequestScriptReload`).
- Smoke test 139/139 (5 new checks in `PlaySmokeTest.GameUI.cs`: pack panels/buttons and no oversized buttons, HUD, panels fit 5
  screen sizes, every pickup small with a sphere collider and glow, icons are the collectable renders).
- **2026-09-28, follow-up pass**: the power-up collect fly-in label (`PowerUpCollectFXLabel.prefab`, via `GameUIBuilder.ApplyCollectFxLabel`)
  now has a soft glow behind the icon and Baloo2 text (found and fixed along the way: `PowerUpCollectFX` used
  `GetComponentInChildren<Image>()` to find the icon, which started picking the new glow sibling instead — now a named `Find("Icon")`
  lookup; the name label also needed `textWrappingMode = NoWrap` + autosizing so e.g. "Invincibility" doesn't wrap to two lines).
  `SuccessMenu` no longer leaves a gap when Next level is hidden (last level cleared): Keep going / Quit read their normal slot Y
  from the positions the builder already set (`SuccessMenuBuilder` now wires a `quitButton` field) and slide up to fill Next level's
  slot instead — the spacing stays defined once, in the builder table. Checked the New Record badge in play mode (diagonal gold
  ribbon, -18° rotation baked into the prefab, Baloo2 text): reads fine, no change made. Every label/button in every menu already
  gets Baloo2 explicitly, per-instance, from the two skin builders' tables — the "remaining `MenuLabel`s" item turned out to already
  be resolved as a side effect of that; only the Main Menu title intentionally keeps its display font.
- Not done: real-phone check.

## 3. Product roadmap (all built 2026-09-26 except step 5 of item 1; see status lines)

Status: **1 done through step 4** (real adapters open), **2 done** (`CharacterBuilder`, 12 colours, `Player.ApplyLook`),
**3 done** (`CompanionBuilder`, `CompanionSpawner`, `CompanionFitter`, 8 companions), **4 done as placeholders**
(`Scripts/Audio/`, `AudioBuilder`), **5 done** (`Assets/Tests/EditMode`, 54 tests). The original plan text follows for reference.


1. **Unlock/economy core**: unlock state, save/load, "watch ad to trial", "buy to own", shared selection-screen
   shell. Ads/IAP decided 2026-09-26 (packages not installed yet):
   - **Ads: Google AdMob** (Google Mobile Ads Unity plugin). Formats: **rewarded** (trial unlocks) +
     **interstitials between runs**: rare, roughly **one every ~10 game overs** (never every run; the count
     goes in `GameConfig`, never at app launch or mid-run) + a **"Remove Ads" IAP**.
   - **IAP: Unity IAP** (`com.unity.purchasing`): character/companion purchases + Remove Ads.
   - Game code talks only to own `IAdService` / `IStoreService` interfaces. Editor/PC/smoke test use a fake
     (placeholder ad, instant reward); the AdMob/Unity IAP adapters exist only in mobile builds.
   - **PC: earn by playing** (level clears + gems, see 3a); no ad/store code in the PC build.
   - **Audience: mixed / not sure**: neutral age question at first launch; under-13 players get
     non-personalised ads (AdMob under-age tag) and a parent check before purchases. Google Play Families
     policy applies (AdMob is Families-certified); UMP consent form for EU; ATT prompt on iOS (adults only).
   - **Mobile also unlocks by playing**, but slowly: a long-term goal, not a fast way around ads/IAP.
   - Curve, prices, saving, trial flow and selection screen: designed 2026-09-26, see section 3a.
2. **Character selection**: 12 colour variants of the box player, playful names, thumbnails in the main menu.
3. **Companion selection**: Farm Animals Animated (Quaternius) from `D:\Unity\Aldera_Assets\`; today the
   companion is one hardcoded `CatWanderer`.
4. **Audio**: nothing exists (no mixer, clips or scripts). Its own design session.
5. Automated test suite: its own session.

Order: characters and companions both need the economy core, so build that once first. Audio is independent.
When 2 and 3 start, give them the same table-driven builder pattern (`PowerUpPickupBuilder`, `HazardBuilder`,
`LevelBuilder`): a new character/companion is a table row, not a hand-edited prefab.

## 3a. Economy core design (2026-09-26, design only, nothing built)

Facts it builds on: score = whole seconds survived (`RunState.Tick`), high score in PlayerPrefs, the app
auto-rotates (portrait and landscape both allowed), PC has no ad or store code, and every level can be picked
freely today.

**Level progression (revised 2026-09-26 after user feedback).**
- Each level has a **clear target**, `targetSeconds`. Surviving that long clears the level and unlocks the next
  one. The first level is always open. Levels unlock only by playing: they cannot be bought or trialled.
- The target and the level's gem rewards are new columns in the `LevelBuilder` table, stored on the level's
  `LevelTheme` (one row per level, the only place they are set). Starting values: Meadow 60 s, Playground 90 s,
  and each later level +30 s. Level difficulty (hazard rate per level) stays the same today; tuning it per level
  is a separate item.
- At the moment the target is reached, the run pauses (through `TimeScaleController`) on a **Success screen**.
  The clear reward is credited, the next level unlocks and the save is written right away. The screen shows the
  reward breakdown and three buttons: `Next level`, **`Keep going (endless)`** and `Menu`.
- `Keep going` resumes the same run with no further target. The player keeps collecting gems until they die or
  quit, and the game-over screen then shows the whole run's breakdown. A run clears its level at most once.
- Level Select shows locked levels with a lock and the goal ("Survive 60 s in Meadow"). Cleared levels show a
  check mark and that level's best score.
- **High score becomes per level** (best score per level in the save). Game over and Level Select show that
  level's best. "Clear High Score" clears the bests only, not gems or progress.

**Gems: three sources, all identical on PC and mobile.**
1. **Clear reward** per level: a big one the first time the level is cleared, and a small one for every repeat
   clear. Starting values: Meadow 100 / 15, Playground 150 / 20, and each later level +50 / +5.
2. **Falling gems:** a gem pickup (worth 1) falls like the other pickups, one every 4-8 s (spawn timing in
   `GameConfig`, next to the other spawn tunables; spawn area shared). One gem look for every level; it
   **spins** while falling (user's choice). Gems collected
   are kept even when the run fails, so no run is wasted.
3. **Gem Multiplier power-up:** a new `PowerUpDefinition` (x2 value for 10 s, applies to gems picked up
   while it is active, not to the clear reward). It gets a normal pickup row in `PowerUpPickupBuilder`, a HUD
   icon and a smoke-test check like the other power-ups.
- The run HUD shows a gem counter next to the score. The game-over screen shows a breakdown (gems collected,
  multiplier bonus, clear reward, total). After an endless continuation it also offers `Next level`.
- Gems go into the wallet as they are earned. The save is written at run end and at the moment of a clear.
- There are no gem packs for money: purchases buy items, never currency. This avoids pay-to-win optics and
  keeps the under-13 / Families policy side simple.
- **Mobile is slower through prices, not earnings:** gameplay stays identical on both platforms, and every gem
  price is multiplied by `mobilePriceMultiplier` = **2.5** (in `EconomyConfig`), rounded to the nearest 5.

**Prices: one tier table; items pick a tier.** A price is never set on an item directly. Play times assume
about **10 gems per minute of play** (roughly 6 caught falling gems plus repeat-clear rewards). That is an
estimate: re-tune the table after real playtests.

| Tier | Gems PC | Gems mobile | PC play time | Mobile play time | IAP price point |
|---|---|---|---|---|---|
| Free (defaults: current box colour, `CatWanderer` cat) | 0 | 0 | - | - | - |
| Common | 600 | 1500 | ~1 h | ~2.5 h | $0.99 |
| Rare | 1800 | 4500 | ~3 h | ~7.5 h | $1.99 |
| Epic | 3600 | 9000 | ~6 h | ~15 h | $2.99 |
| Legendary (companions only) | 7200 | 18000 | ~12 h | ~30 h | $3.99 |
| Remove Ads | - | - | - | - | $2.99 |

- The first clears of Meadow and Playground (250 gems together) cover about 40% of the first Common unlock
  on PC, so early progress feels quick before the longer grind.
- Characters (11 locked colours): 4 Common, 4 Rare, 3 Epic = 20,400 gems on PC, about 34 h of play; about
  85 h on mobile. Companions use Rare to Legendary (3D models are the bigger reward); the split is set when the
  companion list exists (roadmap item 3).
- Real-money prices are configured in the Play Console / App Store Connect. At runtime the game shows the
  store's localised price string, never a hardcoded one. The table only records the intended price points.
- Every locked character/companion can be bought with gems or with money: nothing is IAP-only.

**Data model (same table-driven pattern as the other builders).**
- `UnlockableDefinition` ScriptableObject: stable `id` (e.g. `char.red`, `comp.cow`; **never renamed**, because
  saves reference it), display name, category (Character/Companion), tier, thumbnail, look (material or prefab),
  store product id (derived: `char_red`), `isDefault`. Character/companion subclasses add their own look fields.
- `UnlockCatalog` asset lists every definition. It is created by the character/companion builders, one table
  row per item.
- `EconomyConfig` asset (in `Resources`, like `GameConfig`): tier table, `mobilePriceMultiplier`, gem value,
  `trialRuns`, `gameOversPerInterstitial` (the ads decision put this in `GameConfig`; it moves here next to the
  other economy tunables).

**Saving.**
- A single `SaveService` (plain C#) writes JSON to `Application.persistentDataPath/save.json`. It writes a temp
  file first and then replaces the old one, so a crash mid-write cannot corrupt the save.
- Contents: `version`, `gems`, `owned` (id + source `gems`/`purchase`), `selected` per category, `trial` per
  category (id + runs left), `levels` (per level id: `cleared`, `bestScore`), `gameOversSinceInterstitial`,
  `removeAds`, `ageGate` (under13 / adult / unset). Level ids are the scene names, which already never change.
- **Migration:** on first load, the old global `HighScore` PlayerPrefs value becomes Meadow's `bestScore` and the
  key is deleted. It grants no clears. After that, `RunState` reads and writes bests through `SaveService`.
- Purchases are non-consumable, so the store is the source of truth. At startup, and from a
  **"Restore Purchases"** button (required on iOS, shown only when a store exists), owned products are granted
  again. Granting is idempotent. The local `purchase` flag lets the game work offline.
- Gem unlocks exist only in the local save: deleting the app loses them. Cloud save (Play Games / iCloud) is a
  later, separate item. There is no anti-tamper: the game is single-player, and editing the save only affects
  that player.
- The smoke test uses a temp save path so it never touches the real save.

**Services (game code sees only these).**
- `Wallet`: balance, add, spend.
- `UnlockService`: `IsOwned`, `TryBuyWithGems`, `GrantPurchase`, `StartTrial`, `IsUsable` (owned or on
  trial), `Select`/`Selected`.
- `IAdService`: rewarded ready/show, interstitial show-if-ready.
- `IStoreService`: localised price, buy, restore, owned products.
- `InterstitialPacer`: counts game overs, shows an interstitial on every Nth one unless Remove Ads is owned or
  a rewarded ad was watched on that same game-over screen.
- Fakes: `FakeAdService` (placeholder panel, instant reward) and `FakeStoreService` (instant purchase) for the
  Editor and smoke test. The PC build gets "unavailable" services. The UI decides which buttons to show from
  each service's availability, never from `#if` platform checks.

**"Watch ad to trial" flow (mobile, and the Editor fake).**
1. A locked item is focused in the selection screen, showing its buttons: `Unlock (600 gems)` (disabled with
   "need N more" when short), `Buy <store price>`, `Try: watch ad`.
2. `Try` shows a rewarded ad. The trial is granted only on the SDK's "user earned reward" callback: closing the
   ad early or a failed load grants nothing. When no ad is loaded, the button reads "Ad not ready" and is disabled.
3. A granted trial makes the item selected for the next **3 runs** (`trialRuns`). The count is saved, so
   restarting the app does not lose or reset it. Only one trial per category runs at a time, and starting a new
   one replaces the old one.
4. The count drops at each run end. When it reaches 0, the game-over screen adds a small "Trial over: keep
   <name>?" panel with `Buy`, `Unlock (gems)` if affordable, and `Watch ad: 3 more runs`. Selection then goes
   back to the last owned item. That game over never also shows an interstitial.
5. Trial runs earn gems as normal.
6. PC has no trial button. The selection screen's preview serves as the "try".
7. Under-13 (age gate): rewarded ads stay, non-personalised. `Buy` and `Remove Ads` go through a parent check
   first (a simple arithmetic question).
8. Remove Ads removes interstitials only. Rewarded ads stay, because the player opts into them.

**Shared selection-screen shell (`SelectionScreen` prefab, used for Characters and Companions).**
- **Top bar:** Back (left), title (centre), gem counter (right). The same gem counter prefab is also shown on
  the main menu.
- **Preview:** the focused item rendered by a small preview camera into a `RawImage`, seen from a static 3/4
  view facing the camera. **No rotation or idle animation** (user's choice).
- **Info:** name, tier label, and status (`Owned`, `Selected`, `Trial: 2 runs left`, or the price).
- **Action row:** `Select` for owned items; `Unlock` / `Buy` / `Try` for locked ones (following the service
  availability above). `Restore Purchases` sits in a corner when a store exists.
- **Item grid:** scrollable tiles with thumbnail, lock icon or price badge, and a highlight on the selected item.
  It takes tap, click, arrow keys and gamepad (Input System UI module), and uses the existing
  `MenuButton`/`MenuLabel` styles and `MenuSelectionAnimator`.
- **Layout:** in landscape, preview and info on the left and grid on the right. In portrait, preview on top and
  grid below. One small layout script switches between them on aspect-ratio change, because the app
  auto-rotates.
- The screen is driven by a category plus the catalog, with no per-screen code. `LevelSelect` could move onto
  it later if levels ever become unlockable (not planned).

**Implementation order (each step its own session, smoke test green after each):**
1. **DONE 2026-09-26 (branch `feature/economy-step1`; smoke test 15/15).** Not built: the game-over screen's `Next level` after an endless continuation (do it with the step 2 breakdown). Level Select shows locked/goal/best as text (no lock icon). `SaveService` + migration + level progression: `targetSeconds` and clear rewards in `LevelBuilder`, clear
   banner, next-level unlock, locked levels in Level Select, per-level bests. `Wallet` + `EconomyConfig` +
   clear rewards credited, Success screen with `Keep going`. Smoke checks: clear at target (test override for a short target), next level
   unlocks, save round trip.
2. **DONE 2026-09-26 (branch `feature/economy-step2`; smoke test 19/19, stable over 3 runs).** Falling gem pickup + `GemSpawner` + Gem Multiplier power-up + HUD gem counter + game-over breakdown.
   Smoke checks: gem pickup, multiplier doubles value. Built by `Tools > Gems > Rebuild Gem Assets` (`GemBuilder`). Placeholder look: procedural octahedron gem and icon.  Game-over `Next level` (after a clear + Keep going) is built. The smoke test now clears leftover hazards after each restart (a first-wave crate could kill the test run). 
3. **DONE 2026-09-26 (branch `feature/economy-step3`; smoke test 42/42).** `UnlockableDefinition` (+ `CharacterDefinition`),
   `UnlockCatalog` (Resources), `UnlockService` (owned, gem buy, purchase grant, select), `SaveData.owned/selected`,
   `GemCounter`, `SelectionScreen` + `SelectionLayout` + `UnlockTile` (`Scripts/Selection/`). Built by
   `Tools > Characters > Rebuild Character Assets` (3 **placeholder** rows: Boxy free/default = the current blue
   material, Ruby Common, Sunny Rare; ids `char.blue/red/yellow`; roadmap item 2 replaces the table) and
   `Tools > UI > Rebuild Selection Screen` + `Wire Selection Screen In Core`. The main menu has a `Characters`
   button and a gem counter. **Not built (by design, later steps):** Buy (store), Try (ad), Restore Purchases,
   trial state (`IsUsable` == owned until step 4), Companions screen, applying the selected character to the
   player (roadmap item 2). Everything the screen does is covered by smoke checks (mechanics, real button flow,
   preview pixel readback, layout audit at 8 screen sizes, portrait and landscape).
   **Next session: step 4** (ad/store interfaces + fakes + trial flow + interstitial pacer).
4. **DONE 2026-09-26 (branch `feature/economy-step4`; smoke test 71/71 then).** `IAdService`/`IStoreService` (`Scripts/Services/`),
   `Services` locator (Editor = fakes, every other build = "unavailable"), trials in `UnlockService` (3 runs, one per category,
   returns to the last owned item), `InterstitialPacer` (every 10th game over, on leaving the screen; skipped after a rewarded
   ad, a trial end or Remove Ads), Buy / Try / Restore on the selection screens, trial-over modal on game over, Remove Ads on
   the main menu. Everything is driven by service availability, never `#if` platform checks (except the Editor default).
5. Real adapters (AdMob, Unity IAP, UMP consent, ATT, age gate, parent check) on a mobile build. This step
   re-enables the Unity plugin (see `CLAUDE.md`).

**User decisions (2026-09-26):** on reaching the target, the run ends on a Success screen with a `Keep going
(endless)` option; PC prices doubled from the first proposal (Common ~1 h); mobile prices 2.5x; high score per
level; currency named **Gems**; ad trial 3 runs; PC gets no trial (preview only); gem pickups spin, the
selection preview stays static. Not asked, left at the defaults above: clear targets and rewards per level,
gem multiplier x2 for 10 s, gem spawn every 4-8 s, levels unlock only by play.

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
