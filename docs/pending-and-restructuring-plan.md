# Handoff: state, open items, roadmap

Updated 2026-09-26 (economy steps 1-4, characters, companions, audio and the unit-test suite are done; only economy step 5,
the real ad/store adapters, is open: see section 3a). A fresh session starts by reading `CLAUDE.md` (project map + rules),
then this file. Do not re-explore the project.

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
| Selection preview lighting | user | The preview cube uses Core's sunset light/ambient, so it looks lavender next to the swatch's true blue. Faithful to gameplay lighting; decide if the preview needs its own neutral light. |
| Selection screen not eyeballed in portrait | user | Landscape checked visually once; portrait and 7 other sizes only by the numeric layout audit. Also unchecked: the grid scrolling with >1 row (`EnsureVisible`). |
| Editor rewrites URP materials | note | `git status` shows `Gem.mat`, `PowerUp_*_Material.mat`, `Assets/Characters/*.mat` as modified after play/compile: Unity syncing `_Color` from `_BaseColor` (float noise). Do not commit them. |
| Playground decoration/layout | user | User wants to do it personally; do not scatter decoration (see memory). |

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
