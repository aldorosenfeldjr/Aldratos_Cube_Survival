# Power-Ups — Design Spec

Status: approved for implementation planning
Date: 2026-09-13

## 1. Goal

Add falling power-up pickups that behave like the existing hazard crates
(fall from the sky, physically land/roll, collide with the player) but grant
the player a temporary or one-shot benefit instead of ending the run. Ship
as a genuinely modular system: adding a new power-up type later should mean
"add a ScriptableObject asset (+ a small effect class if the behavior is
new)", never "edit a switch statement buried in GameManager."

## 2. v1 Roster

All three use existing, already-imported assets from the Platformer Pack
(same pack the game's trees/rocks/platforms already come from — matching
art style, zero import work).

| Power-up | Asset | Effect | Duration |
|---|---|---|---|
| **Speed Boost** | `Coin.fbx`, tinted electric-blue | Player move-force and max-speed multiplied | 6s, refreshes on re-collect |
| **Invincibility** | `Star.fbx`, gold glow + sparkle | Hazard collisions destroy the hazard instead of ending the run | 6s, refreshes on re-collect |
| **Shield** | `Heart_Full.fbx`, red pulse | Absorbs exactly one hazard hit, then breaks | No timer — consumed on hit |

`Score Bonus` (Key.fbx) was considered and deliberately deferred — the
architecture below adds it later as a single new file, no other changes.

**Stacking rule:** different power-up types run simultaneously (e.g. Speed +
Invincibility + Shield all active at once is fine). Re-collecting the *same*
type while it's active refreshes/extends its timer rather than stacking a
second copy of the effect.

**Collection:** touch-to-collect, identical physical model to hazards — the
pickup is a normal falling Rigidbody that can land and roll; contact with
the player at any point (mid-air or after landing) collects it.

**Invincibility interaction with hazards:** on contact, the hazard is
destroyed (a satisfying "smash through" beat, matching genre convention)
rather than merely being survived.

## 3. Architecture

### Approaches considered

- **A — Data-driven (chosen):** an abstract `PowerUpDefinition : ScriptableObject`
  per type, each providing its own `IPowerUpEffect`. A single generic
  `PowerUpPickup` component (mirrors `Hazard.cs`) references a definition. A
  `PowerUpManager` singleton tracks active effects/timers and fires events for
  the HUD.
- **B — Enum + switch:** one enum, one big switch statement applying effects
  inline. Rejected — every new type means editing a shared switch, exactly
  what "modular, dynamic code" was asked to avoid.
- **C — Per-type MonoBehaviour subclasses on the pickup itself:** e.g.
  `SpeedBoostPickup : PowerUpPickupBase`. Rejected over A — tuning duration/
  visuals needs a code edit + reserialize instead of just editing a data
  asset, and it doesn't give the HUD a clean single place to query "what's
  active."

Approach A is the closest fit to the brief and reuses the same shape the
project already knows (`GameManager`/`Hazard` singleton + tag-based
component pattern), so there's no new mental model to learn, just a data
layer on top.

### New files (`Assets/Scripts/PowerUps/`)

```
PowerUpDefinition.cs        - abstract ScriptableObject base
  SpeedBoostDefinition.cs   - + multiplier field
  InvincibilityDefinition.cs
  ShieldDefinition.cs
IPowerUpEffect.cs           - Apply(Player) / Remove(Player)
  SpeedBoostEffect.cs
  InvincibilityEffect.cs
  ShieldEffect.cs
PowerUpPickup.cs            - falling pickup component (parallels Hazard.cs)
PowerUpManager.cs           - singleton: tracks active effects, timers, events
PowerUpSpawner.cs           - own coroutine, own cadence, independent of hazard spawner
PowerUpHUD.cs                - subscribes to PowerUpManager, drives icon+timer strip
```

`PowerUpDefinition` fields: `displayName`, `icon (Sprite)`, `duration`
(0 = no timer / consumed-on-use, like Shield), abstract
`CreateEffect() : IPowerUpEffect`. Each concrete subclass adds only the
field(s) its effect needs (e.g. `SpeedBoostDefinition.multiplier`) and
implements `CreateEffect()`. No enum, no factory switch — the ScriptableObject
*is* the factory.

### PowerUpManager

Singleton (`PowerUpManager.Instance`), same lifecycle shape as
`GameManager.Instance`. Responsibilities:
- `Grant(PowerUpDefinition def)` — called by `PowerUpPickup` on collection.
  If an effect of the same *definition type* is already active, refreshes its
  timer instead of double-applying. Otherwise calls `def.CreateEffect()`,
  applies it, starts its timer (skipped for duration-0 types like Shield).
- Per-frame timer tick; on expiry calls `effect.Remove(player)` and fires
  `OnPowerUpExpired` for the HUD.
- Query surface used by `Player`: `float SpeedMultiplier`, `bool IsInvincible`,
  `bool TryConsumeShield()` (returns true once, removes the shield effect).
- Events: `OnPowerUpGranted(PowerUpDefinition, duration)`, `OnPowerUpExpired(PowerUpDefinition)`
  — HUD-only consumers, no gameplay code should need to subscribe.

### PowerUpSpawner

Deliberately **not** part of `GameManager` — its own component (sits next to
`GameManager` on the same GameObject, or as a child), with `BeginSpawning()`/
`StopSpawning()` methods that `GameManager` calls at the same points it
already starts/stops `hazardsCoroutine` (`OnEnable`, `RestartGame`,
`GameOver`). Keeps `GameManager` from growing a second, unrelated
responsibility — it just calls two methods, same as it does for the hazard
coroutine today.

Spawns one power-up at a time, at a random X (same range as hazards, matching
the platform's safe span), dropped from the same height with similar drag,
on a slower/rarer independent timer than hazards (randomized ~4–6s between
spawns) so it reads as a rarer treat, not a second hazard stream. Picks
uniformly among the configured `PowerUpDefinition[]` array (Inspector-
assigned), so adding Score Bonus later is "drop the new asset in this array,"
nothing else.

### Player integration

Two small, additive changes to `Player.cs` — reads from `PowerUpManager`,
doesn't own any power-up state itself:

```csharp
// movement — force/max-speed multiplied by PowerUpManager.Instance.SpeedMultiplier
// OnCollisionEnter(Hazard):
if (PowerUpManager.Instance.IsInvincible) { Destroy(collision.gameObject); return; }
if (PowerUpManager.Instance.TryConsumeShield()) { Destroy(collision.gameObject); return; }
GameOver(); // existing path, unchanged
```

### HUD

New small horizontal strip near the existing Score HUD: one icon+countdown
slot per active power-up, instantiated from a single small prefab when
`OnPowerUpGranted` fires, destroyed on `OnPowerUpExpired`/consume. Shield
(no timer) shows the icon with no countdown, removed when consumed.

## 4. Visual identity per type

- **Speed (Coin):** tint material toward electric blue; simple particle
  trail on the player while active (reuses the project's existing
  LeanTween/particle patterns, no new tech).
- **Invincibility (Star):** gold glow, gentle spin/pulse on the pickup;
  player gets a brief sparkle/pulse effect while active.
- **Shield (Heart):** red pulse on the pickup; a translucent bubble/aura
  around the player while shielded, with a small pop effect on consume.

Each pickup keeps a physically distinct silhouette (star/coin/heart) plus a
distinct color, so at-a-glance it reads differently from the hazard crates
even while falling fast.

## 5. Out of scope for v1 (explicitly deferred, not forgotten)

- Score Bonus (Key.fbx) — architecture supports it as a pure data addition.
- World-space floating icon above each pickup (relying on shape+color+trail
  instead, keeps it lean).
- Any cross-scene/theming work — that's the next phase, and this system's
  data-driven shape is exactly what makes it portable to a differently
  themed scene later without a rewrite.

## 6. Testing/verification plan

Live-Editor verification via the Unity CLI/MCP connection already in use
this session, same pattern as previous phases:
- Confirm each pickup falls, lands, and rolls like a hazard (visual + a
  `get_scene_hierarchy`/position poll over time).
- Force-grant each power-up type via `eval` and confirm: HUD icon appears
  with correct countdown, effect is measurably applied (speed via position-
  over-time deltas, invincibility/shield via forced hazard collision not
  ending the run), and expiry correctly removes the effect + HUD entry.
- Confirm stacking: grant two different types back-to-back, confirm both
  active simultaneously; grant the same type twice, confirm timer refresh
  instead of double-effect.
- Confirm console stays clean (0 errors) through a full grant → expire →
  restart cycle.
