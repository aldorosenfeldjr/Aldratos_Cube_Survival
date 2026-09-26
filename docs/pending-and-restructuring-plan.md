# Handoff: state, open items, roadmap

Updated 2026-09-26 (economy core designed, section 3a). A fresh session starts by reading `CLAUDE.md`
(project map + rules), then this file. Do not re-explore the project.

## 1. State

- **The restructuring plan is finished (phases 1-5).** GameManager split (`TimeScaleController`,
  `HazardSpawner`, `RunState`), `GameConfig` asset for tunables, menus/HUD as prefabs in `Assets/Prefabs/UI/`
  (Core 6.7k -> 2.5k lines), one-click smoke test, `HazardBuilder`/`LevelBuilder`. Details live in the
  CLAUDE.md map; git history has one commit per step.
- **Branches:** `dev` has everything (theme polish, URP migration merged 2026-09-26), pushed to `origin/dev`. `main` is untouched
  (batched promotions only). Merge convention: features -> `dev`.
- **Verification:** menu `Tools/Smoke Test/Run Play Smoke Test` (~15s), then read the console. 9/9 at the
  end of this session.
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
| EditMode tests (`PowerUpManager`, `TimeScaleController`) | later | Belongs to the separate test-suite session. |
| Check post-processing on a phone | user | Mobile gets tonemapping/grading/vignette + SMAA + HDR (no DoF/SSAO). Profile on a real device; drop SMAA or HDR on URP-Mobile if it costs too much. |
| Playground decoration/layout | user | User wants to do it personally; do not scatter decoration (see memory). |

## 3. Product roadmap (agreed order, none started)

1. **Unlock/economy core**: unlock state, save/load, "watch ad to trial", "buy to own", shared selection-screen
   shell. Ads/IAP decided 2026-09-26 (packages not installed yet):
   - **Ads: Google AdMob** (Google Mobile Ads Unity plugin). Formats: **rewarded** (trial unlocks) +
     **interstitials between runs**: rare, roughly **one every ~10 game overs** (never every run; the count
     goes in `GameConfig`, never at app launch or mid-run) + a **"Remove Ads" IAP**.
   - **IAP: Unity IAP** (`com.unity.purchasing`): character/companion purchases + Remove Ads.
   - Game code talks only to own `IAdService` / `IStoreService` interfaces. Editor/PC/smoke test use a fake
     (placeholder ad, instant reward); the AdMob/Unity IAP adapters exist only in mobile builds.
   - **PC: earn by playing** (score milestones or coins); no ad/store code in the PC build.
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
auto-rotates (portrait and landscape both allowed), and PC has no ad or store code.

**Play-to-unlock: coins, not score milestones.**
- Milestones unlock items in a fixed order and stop paying out once a player has passed them. Coins let the
  player choose what to unlock, pay out every run, and give one number per platform to tune.
- `coins earned = floor(score x coinsPerSecond)`, paid at every run end (game over, and quitting to the menu
  from pause, so leaving a run never loses coins). No bonuses and no daily rewards in v1.
- `coinsPerSecond`: **PC 1.0** (earning is the only way to unlock there), **mobile 0.4** (2.5x slower:
  unlocking by play is a long-term goal next to ads and IAP). Both values live in `EconomyConfig`.
- There are no coin packs for money: purchases buy items, never currency. This avoids pay-to-win optics and
  keeps the under-13 / Families policy side simple.

**Prices: one tier table; items pick a tier.** A price is never set on an item directly.

| Tier | Coins | PC play time | Mobile play time | IAP price point |
|---|---|---|---|---|
| Free (defaults: current box colour, `CatWanderer` cat) | 0 | - | - | - |
| Common | 300 | 5 min | 12.5 min | $0.99 |
| Rare | 900 | 15 min | 37.5 min | $1.99 |
| Epic | 2000 | 33 min | 83 min | $2.99 |
| Legendary (companions only) | 4000 | 67 min | 2.8 h | $3.99 |
| Remove Ads | - | - | - | $2.99 |

- Characters (11 locked colours): 4 Common, 4 Rare, 3 Epic = 10,800 coins, about 3 h of survival time on PC
  and 7.5 h on mobile. Companions use Rare to Legendary (3D models are the bigger reward); the split is set when
  the companion list exists (roadmap item 3).
- Real-money prices are configured in the Play Console / App Store Connect. At runtime the game shows the
  store's localised price string, never a hardcoded one. The table only records the intended price points.
- Every locked item can be bought with coins or with money: nothing is IAP-only.

**Data model (same table-driven pattern as the other builders).**
- `UnlockableDefinition` ScriptableObject: stable `id` (e.g. `char.red`, `comp.cow`; **never renamed**, because
  saves reference it), display name, category (Character/Companion), tier, thumbnail, look (material or prefab),
  store product id (derived: `char_red`), `isDefault`. Character/companion subclasses add their own look fields.
- `UnlockCatalog` asset lists every definition. It is created by the character/companion builders, one table
  row per item.
- `EconomyConfig` asset (in `Resources`, like `GameConfig`): tier table, `coinsPerSecond` per platform,
  `trialRuns`, `gameOversPerInterstitial` (the ads decision put this in `GameConfig`; it moves here next to the
  other economy tunables).

**Saving.**
- A single `SaveService` (plain C#) writes JSON to `Application.persistentDataPath/save.json`. It writes a temp
  file first and then replaces the old one, so a crash mid-write cannot corrupt the save.
- Contents: `version`, `coins`, `owned` (id + source `coins`/`purchase`), `selected` per category, `trial` per
  category (id + runs left), `gameOversSinceInterstitial`, `removeAds`, `ageGate` (under13 / adult / unset),
  `highScore`.
- **Migration:** on first load, `highScore` is copied from the `HighScore` PlayerPrefs key and the key is
  deleted. After that, `RunState` reads and writes the high score through `SaveService`.
- Purchases are non-consumable, so the store is the source of truth. At startup, and from a
  **"Restore Purchases"** button (required on iOS, shown only when a store exists), owned products are granted
  again. Granting is idempotent. The local `purchase` flag lets the game work offline.
- Coin unlocks exist only in the local save: deleting the app loses them. Cloud save (Play Games / iCloud) is a
  later, separate item. There is no anti-tamper: the game is single-player, and editing the save only affects
  that player.
- The smoke test uses a temp save path so it never touches the real save.

**Services (game code sees only these).**
- `Wallet`: balance, add, spend.
- `UnlockService`: `IsOwned`, `TryBuyWithCoins`, `GrantPurchase`, `StartTrial`, `IsUsable` (owned or on
  trial), `Select`/`Selected`.
- `IAdService`: rewarded ready/show, interstitial show-if-ready.
- `IStoreService`: localised price, buy, restore, owned products.
- `InterstitialPacer`: counts game overs, shows an interstitial on every Nth one unless Remove Ads is owned or
  a rewarded ad was watched on that same game-over screen.
- Fakes: `FakeAdService` (placeholder panel, instant reward) and `FakeStoreService` (instant purchase) for the
  Editor and smoke test. The PC build gets "unavailable" services. The UI decides which buttons to show from
  each service's availability, never from `#if` platform checks.

**"Watch ad to trial" flow (mobile, and the Editor fake).**
1. A locked item is focused in the selection screen, showing its buttons: `Unlock (300 coins)` (disabled with
   "need N more" when short), `Buy <store price>`, `Try: watch ad`.
2. `Try` shows a rewarded ad. The trial is granted only on the SDK's "user earned reward" callback: closing the
   ad early or a failed load grants nothing. When no ad is loaded, the button reads "Ad not ready" and is disabled.
3. A granted trial makes the item selected for the next **3 runs** (`trialRuns`). The count is saved, so
   restarting the app does not lose or reset it. Only one trial per category runs at a time, and starting a new
   one replaces the old one.
4. The count drops at each run end. When it reaches 0, the game-over screen adds a small "Trial over: keep
   <name>?" panel with `Buy`, `Unlock (coins)` if affordable, and `Watch ad: 3 more runs`. Selection then goes
   back to the last owned item. That game over never also shows an interstitial.
5. Trial runs earn coins as normal.
6. PC has no trial button. The selection screen's preview serves as the "try".
7. Under-13 (age gate): rewarded ads stay, non-personalised. `Buy` and `Remove Ads` go through a parent check
   first (a simple arithmetic question).
8. Remove Ads removes interstitials only. Rewarded ads stay, because the player opts into them.

**Shared selection-screen shell (`SelectionScreen` prefab, used for Characters and Companions).**
- **Top bar:** Back (left), title (centre), coin counter (right). The same coin counter prefab is also shown on
  the main menu.
- **Preview:** the focused item rendered by a small preview camera into a `RawImage`, seen from a static 3/4
  view facing the camera. Adding rotation or idle animation is the user's call; it is not part of the design.
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
1. `SaveService` + high score migration + `Wallet` + `EconomyConfig`. Coins are earned at run end and shown on
   the game-over screen and main menu.
2. `UnlockableDefinition`/`UnlockCatalog`/`UnlockService` + the `SelectionScreen` shell, tested with
   2-3 placeholder character rows. Add smoke checks for coin buy, select, and a save round trip.
3. `IAdService`/`IStoreService` + fakes + trial flow + trial-over panel + interstitial pacer. Add smoke checks
   for trial start/expiry and the pacer count.
4. Real adapters (AdMob, Unity IAP, UMP consent, ATT, age gate, parent check) on a mobile build. This step
   re-enables the Unity plugin (see `CLAUDE.md`).

**Open questions (the defaults above apply unless the user changes them):** the currency's name and icon;
the mobile rate 0.4 (whether the first unlock at about 12 min of survival is too slow); trial length (3 runs);
preview rotation; whether PC should get free trials.

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
