# Character & Companion Selection Experience Redesign — Design Spec

Status: approved for implementation planning
Date: 2026-09-27

## 1. Goal

Character and companion selection (economy step 3, 2026-09-26) works
functionally but reads as a placeholder: the character preview is a literal
`GameObject.CreatePrimitive(Cube)` wearing a material, the companion preview
is frozen on one animation frame at a fixed angle, and both screens are
reached through plain text buttons on the Main Menu with a flat particle
background. The goal is to make this feel like a real game's collection
screen — closer to the reference (Lego-style minifigure customizer): a
curiosity-inducing preview of the equipped character/companion on the Main
Menu, an interactive full-screen picker with the real 3D look in focus
against an animated, out-of-focus environment, and a shared "professional
cartoon UI" visual language (imported asset pack) applied to this screen.
Selecting a character/companion is not currently broken — most items just
appeared unselectable because they were locked and unaffordable, which the
new `Tools/Debug/Unlock All Characters & Companions` menu item (already
shipped, `093b35b`) now works around for testing.

Related but explicitly separate future sub-projects (not in this pass): the
gameplay HUD (powerup icons, survival timer bar), the powerup world-pickup
visuals, and rolling the new UI art pack out to every other menu (Pause,
Game Over, Level Select, Success). See section 7.

## 2. Current state (for reference)

- `SelectionScreen` (`Assets/Scripts/Selection/SelectionScreen.cs`): one
  shared shell for both categories. Renders the focused item into a
  `RenderTexture` via a small offstage camera (`CreateStage`), static 3/4
  angle, no rotation, no idle animation (explicit 2026-09-26 decision, now
  superseded by this spec).
- `SelectionLayout`: landscape = info/preview panel on the **left**, grid on
  the **right**; portrait = info/preview on **top**, grid **below**. This
  spec flips both to put the grid/info "card" on the left and the live
  preview on the right, matching the reference image.
- `CharacterDefinition.CreatePreview` — `GameObject.CreatePrimitive(Cube)` +
  material. No connection to the real `Player` look.
- `CompanionDefinition.CreatePreview` — instantiates the real companion
  prefab, but freezes its `Animator` (`speed = 0f`) on whatever frame it
  happens to load on, at a fixed yaw.
- `MainMenu.OpenCharacters`/`OpenCompanions` — `gameObject.SetActive(false)`
  then `selectionScreen.Open(...)`; an instant cut, no transition.
- `MenuBackgroundEffect` — floating particle sprites over a flat 2D
  background; this is the "way too basic" background being replaced.
- The Playground level already ships `Assets/KayKit_Platformer_Pack` assets
  in the project (imported, licensed, already used for level art) — the
  source material for the new background diorama, avoiding a new asset
  purchase decision.
- LeanTween is the project's existing tweening library (used by
  `MenuBackgroundEffect`, `GameOverMenu`, `MainMenu.Play`, etc.) — this
  design uses it exclusively for all new animation, no new dependency.
- New asset: **Hyper Casual UI Pack** (Unity Asset Store, free, URP-compatible,
  requires Unity 2022.3.62+; project is on 6000.6.0f1) — chosen by the user
  as the source for panel/button/frame art in this pass.
- Mobile has no real-time depth-of-field (`docs/pending-and-restructuring-plan.md`:
  URP-Mobile ships no DoF/SSAO, PC-only, for GPU budget reasons) — the new
  background must not depend on real-time DoF to read as "out of focus."

## 3. Menu background system

A small diorama, rendered by a dedicated persistent camera rig, replacing
`MenuBackgroundEffect` as the Main Menu's background.

- **`MenuShowcaseStage` (prefab)** — a handful of KayKit props arranged into
  a pleasant backdrop (platforms, foliage, a light or two), positioned far
  from the play area like the existing preview stage pattern. This spec adds
  a minimal starting arrangement (a few pieces) only, so there is something
  to look at immediately; **final composition/decoration is the user's own
  pass**, consistent with the existing preference for hand-placed level
  decoration (not scattered programmatically) — same as Playground's set
  dressing.
- **`MenuBackgroundRig` (new MonoBehaviour, persistent singleton, same
  survive-reload pattern as `AudioManager`/`TimeScaleController`)** — owns:
  - One `Camera` rendering `MenuShowcaseStage` into a `RenderTexture` at a
    reduced resolution (e.g. 1/2–1/4 of screen height; exact factor tuned by
    eye) displayed full-screen behind the UI canvas via a `RawImage`. The
    resolution drop *is* the out-of-focus look — no real-time DoF, no
    per-pixel blur shader, so the cost is close to free on mobile. If the
    low-res look alone doesn't read as "out of focus" enough once it's on
    screen, add one cheap single-pass box-blur material on top — not
    speculatively built up front.
  - Three named **poses** (position, euler rotation, FOV): `MainMenuIdle`,
    `CharactersOpen`, `CompanionsOpen`, plus a slow continuous idle drift
    (small LeanTween-driven position/rotation oscillation) layered under
    whichever pose is current, so the background is never perfectly static
    even at rest.
  - A public method (e.g. `MoveTo(Pose)`) that cancels any in-flight pose
    tween and starts a new eased LeanTween move/rotate/FOV tween to the
    target pose — same cancel-and-restart discipline as the existing juice
    controller pattern, so rapid open/close/open doesn't stack tweens.
- Transition timing matches the panel/fade timing in section 4 so the
  background shift and the UI transition read as one motion, not two.

## 4. Shared selection shell

- **`SelectionLayout` flip**: grid/info panel moves to the **landscape
  left** / **portrait top** position (the "card"), live preview moves to
  **landscape right** / **portrait bottom** (the "hero") — mirrored from
  today. This is a change to the anchor-fraction logic only; the panels
  stay anchor-driven so the existing 8-size layout audit still applies,
  just against the flipped arrangement.
- **`SelectionTeaser` (new MonoBehaviour, one instance each for Characters
  and Companions on the Main Menu)** — Characters teaser top, Companions
  teaser bottom (on the right side of the Main Menu). Each shows a title
  banner and a static 3-thumbnail spoiler row (the catalog's first three
  items in that category, reusing `UnlockTile`'s thumbnail rendering) with
  the currently-equipped item checked/highlighted, refreshed on
  `Wallet.Changed`/`UnlockService` changes the same way `SelectionScreen`
  already refreshes today.
- **Open transition** (tapping a teaser): `MenuBackgroundRig.MoveTo` the
  matching pose; the Main Menu `CanvasGroup` fades out (the same
  `LeanAlpha` pattern `MainMenu.Play()` already uses); the tapped teaser
  panel tweens from its Main Menu rect to the full-screen shell's left
  position while the real `SelectionScreen` cross-fades in under it. A
  true shared-element "morph" (matching exact rect transforms across two
  differently-styled prefabs) is not worth the fragility here — a
  slide+crossfade sells the same motion with far less risk of visual
  glitches at odd aspect ratios.
- **Back button** (top-left on the full screen) reverses all three: shell
  crossfades out, background rig returns to `MainMenuIdle`, Main Menu
  `CanvasGroup` fades back in — matching `SelectionScreen.Back()`'s
  existing `onClose` callback, just adding the reverse animation around it.
- **Visual style**: import the Hyper Casual UI Pack; build the teaser
  panels and the redesigned shell (background frame, tile frames, top bar,
  action buttons) from it, following the project's existing "shared style,
  edit once" convention (`MenuButton`/`MenuLabel`). This pass **only**
  restyles the teasers and the selection shell — every other menu keeps its
  current look until the separate later rollout pass (section 7).

## 5. Character preview

- `CharacterDefinition.CreatePreview` stops creating a primitive cube and
  instead instantiates the real `Player` look: the player prefab (or a
  preview-safe variant with input/physics/scoring scripts stripped, the
  same way the companion preview already omits gameplay behavior) wearing
  the character's material via the existing `ApplyLook` material-swap path,
  so what you see in the picker is the same mesh/material the player
  actually wears in a run.
- **`PreviewRotator` (new small component on the preview stage)** — drag
  input (Input System pointer, consistent with the rest of the project's
  UI input) rotates the stage's yaw live while dragging; shared by both
  Character and Companion previews since they already sit on the same
  stage/camera mechanism in `SelectionScreen`. This replaces the
  no-rotation decision from 2026-09-26.
- Selecting an item already updates `Player.ApplyLook` at run start and
  fires `Wallet`/`UnlockService` change events today; the teaser and shell
  previews just need to subscribe the same way so the equipped look updates
  immediately everywhere, not only after reopening the screen.

## 6. Companion preview

- **Color/lighting fix**: determine whether the cat's whiter look was
  actually changed (URP material conversion during the 2026-09-25 migration
  rewriting `_Color`/`_BaseColor`) or is a lighting artifact of being lit by
  Core's warm sunset light/ambient in the preview stage (the existing
  preview-lighting caveat already noted for the character cube in the
  handoff doc) — fix at whichever source it actually is, not by re-tinting
  a shared light per companion.
- **Dynamic animation**: stop freezing the preview `Animator`
  (`speed = 0f`); instead loop each companion's own real clips — `Idle` for
  animals without Walk/Run (Pug, Piggy, Woolly, Llama, per the handoff
  doc), a short Idle/Walk cycle for the wanderers (cat, Daisy, Bolt,
  Stripes) — so every pet reads as alive in the preview regardless of which
  one is focused, combined with the same `PreviewRotator` drag-to-rotate
  from section 5.

## 7. Out of scope for this pass (explicitly deferred)

- Gameplay HUD redesign (powerup icons, shrinking survival-timer bar, gem
  counter restyle) — separate future sub-project, reusing this pass's
  imported UI pack once it exists.
- Powerup world-pickup visual redesign (smaller, collectible-styled
  meshes) — separate future sub-project, independent of this one.
- Rolling the Hyper Casual UI Pack out to Main Menu's remaining chrome,
  Pause, Game Over, Level Select, and Success screens — separate future
  consistency pass, once this screen proves the style out.
- Any change to `UnlockService`, `SaveService`, `Wallet`, or the catalog's
  public shape — this pass is visual/interaction only.
- Final diorama set-dressing for `MenuShowcaseStage` — this spec ships a
  minimal placeholder arrangement; final composition is a separate,
  user-driven decoration pass.

## 8. Edge cases & error handling

- **Rapid teaser taps / open-close-open**: `MenuBackgroundRig.MoveTo`
  cancels its own in-flight tween before starting a new one — no stacked
  or fighting camera tweens, same discipline as the existing juice
  controller pattern.
- **Locked items**: unaffected by this pass — Unlock/Buy/Try flow and
  affordability logic in `SelectionScreen.Refresh()` stay exactly as-is;
  only the preview and layout change.
- **Non-wandering companions**: looping only `Idle` must not throw when
  `Wanders == false` and no Walk/Run clip exists — same guard the
  `Wanders` flag already provides today.
- **Extreme aspect ratios**: the teaser's 3-thumbnail row and the flipped
  `SelectionLayout` panels must stay legible at the existing 8 audited
  screen sizes (both orientations) — this pass re-runs that audit against
  the new arrangement rather than assuming it still passes.
- **Screen destroyed mid-transition** (e.g. app backgrounded on mobile
  during the open/close animation): `SelectionScreen.OnDestroy` already
  tears down its stage/camera/texture unconditionally — the new teaser/rig
  tweens must be cancelled the same defensive way, not left running against
  destroyed objects.

## 9. Testing/verification plan

Extend `PlaySmokeTest` (per the project's convention) with:

- Teaser → full screen → Back round-trip for both categories: correct
  panel becomes the focused screen, background rig reaches the expected
  pose, Main Menu reappears correctly on Back.
- Selecting a character/companion updates the teaser's equipped thumbnail
  and the Main Menu preview without reopening the screen.
- Preview rotates on drag input for both Character and Companion screens.
- Companion preview animator is unpaused and playing (not stuck at
  `speed == 0`).
- Re-run the existing 8-screen-size layout audit against the flipped
  `SelectionLayout` and the new teaser widgets, portrait and landscape.
- Console stays clean (0 errors) through a full open/select/rotate/back
  cycle on both screens.

Not test-suite-covered, needs a human pass (per CLAUDE.md, one targeted
screenshot/eval rather than a broad capture):

- The background's out-of-focus read at actual screen resolution — confirm
  the low-res RenderTexture trick alone sells "blurred," or whether the
  optional box-blur pass is needed.
- The cat's corrected color, by eye, against the new lighting.
- Overall "does this feel like the reference" — a subjective call the
  automated checks can't make.
