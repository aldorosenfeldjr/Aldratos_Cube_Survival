# Power-Ups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add three falling power-up pickups (Speed Boost, Invincibility, Shield) that behave physically like the existing hazard crates but grant the player a temporary or one-shot benefit, built as a data-driven system where adding a future power-up type means adding one ScriptableObject asset, not editing a switch statement.

**Architecture:** An abstract `PowerUpDefinition` ScriptableObject per type, each producing its own `IPowerUpEffect`. A `PowerUpManager` singleton (own always-active GameObject) tracks active effects/timers and exposes a read-only query surface (`SpeedMultiplier`, `IsInvincible`, `TryConsumeShield()`) plus grant/expire events. A generic `PowerUpPickup` component (mirrors `Hazard.cs`) grants on player contact. A standalone `PowerUpSpawner` component (lives on the `GameManager` GameObject, started/stopped by `GameManager` the same way it already manages `hazardsCoroutine`) spawns pickups on its own slower, independent cadence. A new `PowerUpHUD` renders one icon+countdown slot per active effect.

**Tech Stack:** Unity 6000.6.0f1, C# (project assembly `NewAssembly`, `Assets/Scripts/AssemblyDef.asmdef`), Built-in Render Pipeline, TextMeshPro, LeanTween (already used project-wide for UI animation).

**Spec:** `docs/superpowers/specs/2026-09-13-powerups-design.md`

## Global Constraints

- Roster is exactly 3 types for v1: Speed Boost (Coin.fbx, blue tint), Invincibility (Star.fbx, gold), Shield (Heart_Full.fbx, red). Score Bonus (Key.fbx) is explicitly out of scope.
- Different power-up types stack (can be active simultaneously). Re-collecting the *same* type refreshes its timer instead of stacking a duplicate effect.
- Collection is touch-to-collect: identical physical model to hazards (falling Rigidbody, collect on contact at any point, mid-air or after landing/rolling).
- On hazard contact while Invincible or Shielded, the hazard is destroyed (not merely survived).
- No automated test framework is wired into this project's gameplay code (no `Tests/` folder, no test assembly). Verification is done live against the running Unity Editor via the Unity CLI (`unity` on PATH — confirm with `unity --version`; if not found, load the `unity-cli` skill) and its `eval_file` command, which compiles and runs a `.cs` file's top-level statements directly inside the connected Editor. Confirm an Editor is connected before verifying: `unity status --format json` must show one instance with `"state": "ready"`. After any script edit, before further verification, force Unity to pick up the change and confirm it compiled clean:

  ```csharp
  // refresh_assets.cs
  UnityEditor.AssetDatabase.Refresh();
  return "refreshed";
  ```
  ```
  unity command --format json eval_file -- --file "refresh_assets.cs" --timeout 30000
  ```
  Wait ~3 seconds, then:
  ```
  unity command --format json console -- --level error
  ```
  Check the response's `groundTruth.compilationFailed` is `false` and `groundTruth.consoleErrors` is `0` before proceeding. If `compilationFailed` is `true`, read the error message in the same response and fix before continuing.
- Never leave the Unity Editor in Play mode at the end of a task. After live-verifying in Play mode, call `unity command --format json editor_stop`, wait ~2 seconds, then confirm with a small eval returning `UnityEditor.EditorApplication.isPlaying` that it is `false`. Play-mode state does not always revert cleanly on stop in this environment — after stopping, reload the scene from disk before saving anything: `UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path, UnityEditor.SceneManagement.OpenSceneMode.Single)`.

---

## Task 1: Power-up type contracts

**Files:**
- Create: `Assets/Scripts/PowerUps/IPowerUpEffect.cs`
- Create: `Assets/Scripts/PowerUps/PowerUpDefinition.cs`

**Interfaces:**
- Produces: `interface IPowerUpEffect { void Apply(PowerUpManager manager); void Remove(PowerUpManager manager); }`
- Produces: `abstract class PowerUpDefinition : ScriptableObject { string DisplayName; Sprite Icon; float Duration; abstract IPowerUpEffect CreateEffect(); }`

- [ ] **Step 1: Create the effect interface**

```csharp
// Assets/Scripts/PowerUps/IPowerUpEffect.cs
public interface IPowerUpEffect
{
    void Apply(PowerUpManager manager);
    void Remove(PowerUpManager manager);
}
```

- [ ] **Step 2: Create the abstract definition base**

```csharp
// Assets/Scripts/PowerUps/PowerUpDefinition.cs
using UnityEngine;

public abstract class PowerUpDefinition : ScriptableObject
{
    [SerializeField]
    private string displayName;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    [Tooltip("0 = no timer; the effect is removed by being consumed instead (e.g. Shield).")]
    private float duration;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public float Duration => duration;

    public abstract IPowerUpEffect CreateEffect();
}
```

- [ ] **Step 3: Refresh and verify clean compile**

Run the refresh/console-check sequence from Global Constraints. `PowerUpDefinition` is abstract with no concrete subclass yet, so there is nothing to smoke-test at runtime — a clean compile (0 console errors) is the full verification for this task.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/PowerUps/IPowerUpEffect.cs Assets/Scripts/PowerUps/IPowerUpEffect.cs.meta Assets/Scripts/PowerUps/PowerUpDefinition.cs Assets/Scripts/PowerUps/PowerUpDefinition.cs.meta
git commit -m "Add power-up effect/definition contracts"
```

(Unity generates a `.meta` file per script automatically on the next `AssetDatabase.Refresh()`/domain reload — the Step 3 refresh already created them; just include them in the commit.)

---

## Task 2: PowerUpManager core (grant, tick, expire, query surface)

**Files:**
- Create: `Assets/Scripts/PowerUps/PowerUpManager.cs`

**Interfaces:**
- Consumes: `IPowerUpEffect`, `PowerUpDefinition` (Task 1)
- Produces: `class PowerUpManager : MonoBehaviour` with:
  - `static PowerUpManager Instance`
  - `float SpeedMultiplier` (get), `bool IsInvincible` (get), `bool HasShield` (get)
  - `event Action<PowerUpDefinition, float> OnPowerUpGranted`
  - `event Action<PowerUpDefinition> OnPowerUpExpired`
  - `void Grant(PowerUpDefinition definition)`
  - `void ResetAll()`
  - `internal void SetSpeedMultiplier(float value)`, `internal void SetInvincible(bool value)`, `internal void SetShieldActive(bool value)`

Note: `TryConsumeShield()` is added in Task 5, once `ShieldEffect` exists — referencing it here would not compile standalone.

- [ ] **Step 1: Write PowerUpManager**

```csharp
// Assets/Scripts/PowerUps/PowerUpManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    private static PowerUpManager instance;
    public static PowerUpManager Instance => instance;

    public float SpeedMultiplier { get; private set; } = 1f;
    public bool IsInvincible { get; private set; }
    public bool HasShield { get; private set; }

    public event Action<PowerUpDefinition, float> OnPowerUpGranted;
    public event Action<PowerUpDefinition> OnPowerUpExpired;

    protected class ActivePowerUp
    {
        public PowerUpDefinition Definition;
        public IPowerUpEffect Effect;
        public float RemainingTime;
    }

    protected readonly List<ActivePowerUp> activePowerUps = new List<ActivePowerUp>();

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        for (int i = activePowerUps.Count - 1; i >= 0; i--)
        {
            var active = activePowerUps[i];
            if (active.Definition.Duration <= 0f)
            {
                continue; // consumed-on-use types (e.g. Shield) don't tick down
            }

            active.RemainingTime -= Time.deltaTime;
            if (active.RemainingTime <= 0f)
            {
                active.Effect.Remove(this);
                activePowerUps.RemoveAt(i);
                OnPowerUpExpired?.Invoke(active.Definition);
            }
        }
    }

    public void Grant(PowerUpDefinition definition)
    {
        var existing = activePowerUps.Find(p => p.Definition.GetType() == definition.GetType());
        if (existing != null)
        {
            existing.RemainingTime = definition.Duration;
            OnPowerUpGranted?.Invoke(definition, definition.Duration);
            return;
        }

        var effect = definition.CreateEffect();
        effect.Apply(this);
        activePowerUps.Add(new ActivePowerUp
        {
            Definition = definition,
            Effect = effect,
            RemainingTime = definition.Duration
        });
        OnPowerUpGranted?.Invoke(definition, definition.Duration);
    }

    public void ResetAll()
    {
        foreach (var active in activePowerUps)
        {
            active.Effect.Remove(this);
            OnPowerUpExpired?.Invoke(active.Definition);
        }
        activePowerUps.Clear();
    }

    internal void SetSpeedMultiplier(float value) => SpeedMultiplier = value;
    internal void SetInvincible(bool value) => IsInvincible = value;
    internal void SetShieldActive(bool value) => HasShield = value;
}
```

(`ActivePowerUp` and `activePowerUps` are `protected` rather than `private` so Task 5's `TryConsumeShield()` addition — written as a modification to this same class — can search the list without needing a new accessor.)

- [ ] **Step 2: Refresh and verify clean compile**

Run the refresh/console-check sequence. No scene object references this component yet, so this is a compile-only checkpoint.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpManager.cs Assets/Scripts/PowerUps/PowerUpManager.cs.meta
git commit -m "Add PowerUpManager: grant/tick/expire and query surface"
```

---

## Task 3: Speed Boost

**Files:**
- Create: `Assets/Scripts/PowerUps/SpeedBoostEffect.cs`
- Create: `Assets/Scripts/PowerUps/SpeedBoostDefinition.cs`

**Interfaces:**
- Consumes: `IPowerUpEffect`, `PowerUpDefinition`, `PowerUpManager.SetSpeedMultiplier` (Tasks 1–2)
- Produces: `class SpeedBoostEffect : IPowerUpEffect`; `class SpeedBoostDefinition : PowerUpDefinition` with `float Multiplier` field (default `1.6`)

- [ ] **Step 1: Write the effect**

```csharp
// Assets/Scripts/PowerUps/SpeedBoostEffect.cs
public class SpeedBoostEffect : IPowerUpEffect
{
    private readonly float multiplier;

    public SpeedBoostEffect(float multiplier)
    {
        this.multiplier = multiplier;
    }

    public void Apply(PowerUpManager manager)
    {
        manager.SetSpeedMultiplier(multiplier);
    }

    public void Remove(PowerUpManager manager)
    {
        manager.SetSpeedMultiplier(1f);
    }
}
```

- [ ] **Step 2: Write the definition**

```csharp
// Assets/Scripts/PowerUps/SpeedBoostDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBoost", menuName = "PowerUps/Speed Boost")]
public class SpeedBoostDefinition : PowerUpDefinition
{
    [SerializeField]
    private float multiplier = 1.6f;

    public override IPowerUpEffect CreateEffect()
    {
        return new SpeedBoostEffect(multiplier);
    }
}
```

- [ ] **Step 3: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 4: Smoke-test Grant/expire against a live PowerUpManager**

This is the first concrete type, so verify the manager's grant/apply/expire loop actually works end to end, independent of the scene (creates its own temporary `PowerUpManager` and `SpeedBoostDefinition` instances, no scene wiring needed yet):

```csharp
// smoke_speed_boost.cs
var managerGo = new GameObject("TempPowerUpManager");
var manager = managerGo.AddComponent<PowerUpManager>();

var def = ScriptableObject.CreateInstance<SpeedBoostDefinition>();
var so = new UnityEditor.SerializedObject(def);
so.FindProperty("duration").floatValue = 0.2f; // short, so the test finishes fast
so.FindProperty("multiplier").floatValue = 2f;
so.ApplyModifiedProperties();

manager.Grant(def);
var afterGrant = "SpeedMultiplier after grant: " + manager.SpeedMultiplier;

// Manually drive Update() forward past the 0.2s duration (Update() runs on the
// real Editor frame loop in Play mode, but this script runs in Edit mode, so we
// call it directly here to avoid needing a live Play session for this check).
System.Threading.Thread.Sleep(250);
var updateMethod = typeof(PowerUpManager).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
updateMethod.Invoke(manager, null);
var afterExpire = "SpeedMultiplier after expiry: " + manager.SpeedMultiplier;

UnityEngine.Object.DestroyImmediate(managerGo);
UnityEngine.Object.DestroyImmediate(def);

return afterGrant + " | " + afterExpire;
```

Run it:
```
unity command --format json eval_file -- --file "smoke_speed_boost.cs" --timeout 20000
```
Expected result string: `SpeedMultiplier after grant: 2 | SpeedMultiplier after expiry: 1`. If it doesn't match, the bug is in `PowerUpManager.Grant`/`Update` (Task 2) or `SpeedBoostEffect` (this task) — re-read both before changing anything.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/PowerUps/SpeedBoostEffect.cs Assets/Scripts/PowerUps/SpeedBoostEffect.cs.meta Assets/Scripts/PowerUps/SpeedBoostDefinition.cs Assets/Scripts/PowerUps/SpeedBoostDefinition.cs.meta
git commit -m "Add Speed Boost power-up"
```

---

## Task 4: Invincibility

**Files:**
- Create: `Assets/Scripts/PowerUps/InvincibilityEffect.cs`
- Create: `Assets/Scripts/PowerUps/InvincibilityDefinition.cs`

**Interfaces:**
- Consumes: `IPowerUpEffect`, `PowerUpDefinition`, `PowerUpManager.SetInvincible` (Tasks 1–2)
- Produces: `class InvincibilityEffect : IPowerUpEffect`; `class InvincibilityDefinition : PowerUpDefinition`

- [ ] **Step 1: Write the effect**

```csharp
// Assets/Scripts/PowerUps/InvincibilityEffect.cs
public class InvincibilityEffect : IPowerUpEffect
{
    public void Apply(PowerUpManager manager)
    {
        manager.SetInvincible(true);
    }

    public void Remove(PowerUpManager manager)
    {
        manager.SetInvincible(false);
    }
}
```

- [ ] **Step 2: Write the definition**

```csharp
// Assets/Scripts/PowerUps/InvincibilityDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "Invincibility", menuName = "PowerUps/Invincibility")]
public class InvincibilityDefinition : PowerUpDefinition
{
    public override IPowerUpEffect CreateEffect()
    {
        return new InvincibilityEffect();
    }
}
```

- [ ] **Step 3: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 4: Smoke-test**

Same pattern as Task 3 Step 4, swapping in `InvincibilityDefinition` (no `multiplier` property to set) and checking `manager.IsInvincible` before/after instead of `SpeedMultiplier`:

```csharp
// smoke_invincibility.cs
var managerGo = new GameObject("TempPowerUpManager");
var manager = managerGo.AddComponent<PowerUpManager>();

var def = ScriptableObject.CreateInstance<InvincibilityDefinition>();
var so = new UnityEditor.SerializedObject(def);
so.FindProperty("duration").floatValue = 0.2f;
so.ApplyModifiedProperties();

manager.Grant(def);
var afterGrant = "IsInvincible after grant: " + manager.IsInvincible;

System.Threading.Thread.Sleep(250);
var updateMethod = typeof(PowerUpManager).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
updateMethod.Invoke(manager, null);
var afterExpire = "IsInvincible after expiry: " + manager.IsInvincible;

UnityEngine.Object.DestroyImmediate(managerGo);
UnityEngine.Object.DestroyImmediate(def);

return afterGrant + " | " + afterExpire;
```

Expected: `IsInvincible after grant: True | IsInvincible after expiry: False`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/PowerUps/InvincibilityEffect.cs Assets/Scripts/PowerUps/InvincibilityEffect.cs.meta Assets/Scripts/PowerUps/InvincibilityDefinition.cs Assets/Scripts/PowerUps/InvincibilityDefinition.cs.meta
git commit -m "Add Invincibility power-up"
```

---

## Task 5: Shield (+ PowerUpManager.TryConsumeShield)

**Files:**
- Create: `Assets/Scripts/PowerUps/ShieldEffect.cs`
- Create: `Assets/Scripts/PowerUps/ShieldDefinition.cs`
- Modify: `Assets/Scripts/PowerUps/PowerUpManager.cs` (add `TryConsumeShield`)

**Interfaces:**
- Consumes: `IPowerUpEffect`, `PowerUpDefinition`, `PowerUpManager.SetShieldActive`, `PowerUpManager.activePowerUps`/`ActivePowerUp` (Tasks 1–2)
- Produces: `class ShieldEffect : IPowerUpEffect`; `class ShieldDefinition : PowerUpDefinition` (duration left at `0` — consumed-on-use, not timed); `PowerUpManager.bool TryConsumeShield()`

- [ ] **Step 1: Write the effect**

```csharp
// Assets/Scripts/PowerUps/ShieldEffect.cs
public class ShieldEffect : IPowerUpEffect
{
    public void Apply(PowerUpManager manager)
    {
        manager.SetShieldActive(true);
    }

    public void Remove(PowerUpManager manager)
    {
        manager.SetShieldActive(false);
    }
}
```

- [ ] **Step 2: Write the definition**

```csharp
// Assets/Scripts/PowerUps/ShieldDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "Shield", menuName = "PowerUps/Shield")]
public class ShieldDefinition : PowerUpDefinition
{
    public override IPowerUpEffect CreateEffect()
    {
        return new ShieldEffect();
    }
}
```

Leave the inherited `duration` field at its default `0` on the asset in Task 10 — `PowerUpManager.Update()` already treats `Duration <= 0f` as "don't tick this down," which is exactly what a consumed-on-use effect needs.

- [ ] **Step 3: Add TryConsumeShield to PowerUpManager**

Add this method to the existing `Assets/Scripts/PowerUps/PowerUpManager.cs`, anywhere inside the class body (e.g. directly below `Grant`):

```csharp
    public bool TryConsumeShield()
    {
        if (!HasShield)
        {
            return false;
        }

        var active = activePowerUps.Find(p => p.Effect is ShieldEffect);
        if (active != null)
        {
            active.Effect.Remove(this);
            activePowerUps.Remove(active);
            OnPowerUpExpired?.Invoke(active.Definition);
        }

        return true;
    }
```

- [ ] **Step 4: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 5: Smoke-test grant + consume (not timer expiry)**

```csharp
// smoke_shield.cs
var managerGo = new GameObject("TempPowerUpManager");
var manager = managerGo.AddComponent<PowerUpManager>();

var def = ScriptableObject.CreateInstance<ShieldDefinition>();
// duration left at its default 0 - this type is consumed-on-use, not timed.

manager.Grant(def);
var afterGrant = "HasShield after grant: " + manager.HasShield;

var consumedOnce = manager.TryConsumeShield();
var afterConsume = "HasShield after consume: " + manager.HasShield + ", consumed=" + consumedOnce;

var consumedTwice = manager.TryConsumeShield();
var secondConsumeAttempt = "second TryConsumeShield returns: " + consumedTwice;

UnityEngine.Object.DestroyImmediate(managerGo);
UnityEngine.Object.DestroyImmediate(def);

return afterGrant + " | " + afterConsume + " | " + secondConsumeAttempt;
```

Expected: `HasShield after grant: True | HasShield after consume: False, consumed=True | second TryConsumeShield returns: False`. The third value confirms a shield can't be consumed twice.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/PowerUps/ShieldEffect.cs Assets/Scripts/PowerUps/ShieldEffect.cs.meta Assets/Scripts/PowerUps/ShieldDefinition.cs Assets/Scripts/PowerUps/ShieldDefinition.cs.meta Assets/Scripts/PowerUps/PowerUpManager.cs
git commit -m "Add Shield power-up and PowerUpManager.TryConsumeShield"
```

---

## Task 6: PowerUpPickup (falling collectible component)

**Files:**
- Create: `Assets/Scripts/PowerUps/PowerUpPickup.cs`

**Interfaces:**
- Consumes: `PowerUpDefinition`, `PowerUpManager.Instance.Grant` (Tasks 1–2), `Player` (existing `Assets/Scripts/Player.cs`)
- Produces: `class PowerUpPickup : MonoBehaviour` with `[SerializeField] PowerUpDefinition definition`

- [ ] **Step 1: Write PowerUpPickup**

Detection uses `GetComponent<Player>()` rather than a tag check — more robust than depending on the player GameObject having a specific tag configured correctly, and this project's existing `Player`/`Hazard` scripts already mix both styles, so this isn't a departure from convention.

```csharp
// Assets/Scripts/PowerUps/PowerUpPickup.cs
using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [SerializeField]
    private PowerUpDefinition definition;

    private void OnCollisionEnter(Collision collision)
    {
        var player = collision.gameObject.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        PowerUpManager.Instance.Grant(definition);
        Destroy(gameObject);
    }
}
```

- [ ] **Step 2: Refresh and verify clean compile**

Run the refresh/console-check sequence. No prefab references this component yet (that's Task 10), so this is compile-only.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpPickup.cs Assets/Scripts/PowerUps/PowerUpPickup.cs.meta
git commit -m "Add PowerUpPickup component"
```

---

## Task 7: PowerUpSpawner

**Files:**
- Create: `Assets/Scripts/PowerUps/PowerUpSpawner.cs`

**Interfaces:**
- Produces: `class PowerUpSpawner : MonoBehaviour` with `void BeginSpawning()`, `void StopSpawning()`, `[SerializeField] GameObject[] pickupPrefabs`

- [ ] **Step 1: Write PowerUpSpawner**

Mirrors `GameManager.SpawnHazards()`'s spawn geometry (X range, height, drag) for visual/physical consistency with hazards, but on its own independent, slower timer.

```csharp
// Assets/Scripts/PowerUps/PowerUpSpawner.cs
using System.Collections;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject[] pickupPrefabs;
    [SerializeField]
    private float minSpawnInterval = 4f;
    [SerializeField]
    private float maxSpawnInterval = 6f;
    [SerializeField]
    private float spawnMinX = -7f;
    [SerializeField]
    private float spawnMaxX = 7f;
    [SerializeField]
    private float spawnHeight = 11f;
    [SerializeField]
    private float minDrag;
    [SerializeField]
    private float maxDrag;

    private Coroutine spawnCoroutine;

    public void BeginSpawning()
    {
        StopSpawning();
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

            if (pickupPrefabs.Length == 0)
            {
                continue;
            }

            var prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
            var x = Random.Range(spawnMinX, spawnMaxX);
            var pickup = Instantiate(prefab, new Vector3(x, spawnHeight, 0), Quaternion.identity);

            var rb = pickup.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearDamping = Random.Range(minDrag, maxDrag);
            }
        }
    }
}
```

- [ ] **Step 2: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/PowerUps/PowerUpSpawner.cs Assets/Scripts/PowerUps/PowerUpSpawner.cs.meta
git commit -m "Add PowerUpSpawner"
```

---

## Task 8: GameManager integration

**Files:**
- Modify: `Assets/Scripts/GameManager.cs`

**Interfaces:**
- Consumes: `PowerUpSpawner.BeginSpawning/StopSpawning` (Task 7), `PowerUpManager.Instance.ResetAll` (Task 2)

- [ ] **Step 1: Add the spawner field**

In `Assets/Scripts/GameManager.cs`, add a field next to the other `[SerializeField]` fields (e.g. right after `maxHazardDrag`, line 30):

```csharp
    [SerializeField]
    private PowerUpSpawner powerUpSpawner;
```

- [ ] **Step 2: Start spawning + reset power-up state in OnEnable**

In `OnEnable()`, the existing body is:

```csharp
    private void OnEnable() 
    {
        NewRecordScreen.SetActive(false);
        player.SetActive(true);

        mainVCam.SetActive(true);
        zoomVCam.SetActive(false);

        gameOver = false;
        score = 0;
        timer = 0;
        celebratedNewBest = false;

        scoreText.text = "0";
        scoreText.color = normalScoreColor;
        highScoreText.text = $"Best: {highScore}";
        highScoreText.gameObject.SetActive(true);

        hazardsCoroutine = StartCoroutine(SpawnHazards());
    }
```

Change the last line to:

```csharp
        hazardsCoroutine = StartCoroutine(SpawnHazards());

        powerUpSpawner.BeginSpawning();
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResetAll();
        }
    }
```

- [ ] **Step 3: Stop spawning on GameOver**

In `GameOver()`, the existing body starts:

```csharp
    public void GameOver()
    {
        StopCoroutine(hazardsCoroutine);
        gameOver = true;
```

Change to:

```csharp
    public void GameOver()
    {
        StopCoroutine(hazardsCoroutine);
        powerUpSpawner.StopSpawning();
        gameOver = true;
```

- [ ] **Step 4: Restart spawning + clear leftover pickups in RestartGame**

In `RestartGame()`, the existing body starts:

```csharp
    public void RestartGame()
    {
        foreach (var hazard in GameObject.FindGameObjectsWithTag("Hazard"))
        {
            Destroy(hazard);
        }

        if (hazardsCoroutine != null)
        {
            StopCoroutine(hazardsCoroutine);
        }
```

Change to:

```csharp
    public void RestartGame()
    {
        foreach (var hazard in GameObject.FindGameObjectsWithTag("Hazard"))
        {
            Destroy(hazard);
        }

        foreach (var powerUp in GameObject.FindGameObjectsWithTag("PowerUp"))
        {
            Destroy(powerUp);
        }

        if (hazardsCoroutine != null)
        {
            StopCoroutine(hazardsCoroutine);
        }
```

And where `RestartGame()` currently ends with:

```csharp
        hazardsCoroutine = StartCoroutine(SpawnHazards());

        if (Time.timeScale < 1)
        {
            Resume();
        }
    }
```

Change to:

```csharp
        hazardsCoroutine = StartCoroutine(SpawnHazards());

        powerUpSpawner.BeginSpawning();
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResetAll();
        }

        if (Time.timeScale < 1)
        {
            Resume();
        }
    }
```

The `"PowerUp"` tag referenced in Step 4 doesn't exist in the project yet — it's created in Task 10 alongside the prefabs that use it. Compiling this task doesn't require the tag to exist (`FindGameObjectsWithTag` is a runtime call, not a compile-time reference), but *running* this code with an undefined tag throws at runtime, so Step 5 below only checks compilation, not behavior — full behavior is covered by Task 12's end-to-end verification, after Task 10 creates the tag.

- [ ] **Step 5: Refresh and verify clean compile**

Run the refresh/console-check sequence. `powerUpSpawner` will be `null` in the Inspector until Task 10 wires it — that's fine for a compile check; don't enter Play mode in this task (a null `powerUpSpawner.BeginSpawning()` call would throw `NullReferenceException` at runtime until Task 10 wires the field).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/GameManager.cs
git commit -m "Wire PowerUpSpawner lifecycle into GameManager"
```

---

## Task 9: Player integration

**Files:**
- Modify: `Assets/Scripts/Player.cs`

**Interfaces:**
- Consumes: `PowerUpManager.Instance.SpeedMultiplier/IsInvincible/TryConsumeShield()` (Tasks 2, 5)

- [ ] **Step 1: Apply the speed multiplier to movement**

In `Assets/Scripts/Player.cs`, the existing movement block in `Update()` is:

```csharp
        if (rb.linearVelocity.magnitude <= maximumVelocity)
        {
            rb.AddForce(new Vector3(horizontalInput * forceMultiplier * Time.deltaTime, 0, 0));
        }
```

Change to:

```csharp
        var speedMultiplier = PowerUpManager.Instance != null ? PowerUpManager.Instance.SpeedMultiplier : 1f;

        if (rb.linearVelocity.magnitude <= maximumVelocity * speedMultiplier)
        {
            rb.AddForce(new Vector3(horizontalInput * forceMultiplier * speedMultiplier * Time.deltaTime, 0, 0));
        }
```

(The `null` guard matches the existing `if (GameManager.Instance == null) return;` pattern a few lines above in the same method — `PowerUpManager` may not have run `Awake()` yet the very first frame, or may not exist at all until Task 10 places it in the scene.)

- [ ] **Step 2: Check Invincibility/Shield before ending the run on hazard contact**

The existing `OnCollisionEnter` is:

```csharp
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            GameOver();
            Instantiate(deathParticles, transform.position, Quaternion.identity);
            cinemachineImpulseSource.GenerateImpulse();
        }
    }
```

Change to:

```csharp
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsInvincible)
            {
                Destroy(collision.gameObject);
                return;
            }

            if (PowerUpManager.Instance != null && PowerUpManager.Instance.TryConsumeShield())
            {
                Destroy(collision.gameObject);
                return;
            }

            GameOver();
            Instantiate(deathParticles, transform.position, Quaternion.identity);
            cinemachineImpulseSource.GenerateImpulse();
        }
    }
```

- [ ] **Step 3: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player.cs
git commit -m "Apply Speed/Invincibility/Shield power-up effects in Player"
```

---

## Task 10: Unity scene/asset setup (tag, ScriptableObject assets, prefabs, PowerUpManager GameObject, wiring)

**Files:**
- Modify (via Editor, not hand-edited): `Assets/Scenes/Game.unity`, `ProjectSettings/TagManager.asset`
- Create (via Editor): 3 `PowerUpDefinition` asset files under `Assets/PowerUps/`, 3 prefabs under `Assets/Prefabs/`

This task is pure Unity-Editor scene/asset work, done live through the Unity CLI's `eval_file` (the same technique used for every previous phase in this project this session — see `git log` for prior commits touching `Assets/Scenes/Game.unity` for reference if unfamiliar with the pattern). Do this in **Edit mode**, not Play mode (Play-mode edits to assets/prefabs don't reliably persist in this environment — see Global Constraints).

- [ ] **Step 1: Create the "PowerUp" tag**

```csharp
// add_powerup_tag.cs
var tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
var tagsProp = tagManager.FindProperty("tags");

for (int i = 0; i < tagsProp.arraySize; i++)
{
    if (tagsProp.GetArrayElementAtIndex(i).stringValue == "PowerUp")
    {
        return "Tag 'PowerUp' already exists.";
    }
}

tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "PowerUp";
tagManager.ApplyModifiedProperties();

return "Created tag 'PowerUp'.";
```

Run via `unity command --format json eval_file -- --file "add_powerup_tag.cs" --timeout 20000`.

- [ ] **Step 2: Create the three PowerUpDefinition assets**

```csharp
// create_powerup_definitions.cs
System.IO.Directory.CreateDirectory("Assets/PowerUps");

var speed = ScriptableObject.CreateInstance<SpeedBoostDefinition>();
{
    var so = new UnityEditor.SerializedObject(speed);
    so.FindProperty("displayName").stringValue = "Speed Boost";
    so.FindProperty("duration").floatValue = 6f;
    so.FindProperty("multiplier").floatValue = 1.6f;
    so.ApplyModifiedProperties();
}
UnityEditor.AssetDatabase.CreateAsset(speed, "Assets/PowerUps/SpeedBoost.asset");

var invincibility = ScriptableObject.CreateInstance<InvincibilityDefinition>();
{
    var so = new UnityEditor.SerializedObject(invincibility);
    so.FindProperty("displayName").stringValue = "Invincibility";
    so.FindProperty("duration").floatValue = 6f;
    so.ApplyModifiedProperties();
}
UnityEditor.AssetDatabase.CreateAsset(invincibility, "Assets/PowerUps/Invincibility.asset");

var shield = ScriptableObject.CreateInstance<ShieldDefinition>();
{
    var so = new UnityEditor.SerializedObject(shield);
    so.FindProperty("displayName").stringValue = "Shield";
    // duration left at 0 - consumed-on-use, not timed.
    so.ApplyModifiedProperties();
}
UnityEditor.AssetDatabase.CreateAsset(shield, "Assets/PowerUps/Shield.asset");

UnityEditor.AssetDatabase.SaveAssets();
return "Created SpeedBoost.asset, Invincibility.asset, Shield.asset under Assets/PowerUps/";
```

Note: the `icon` field on each definition is left unset here — Task 11 assigns icons once the HUD (and a decision on where icon sprites come from — see Task 11 Step 1) exists.

- [ ] **Step 3: Create the three pickup prefabs from the existing FBX meshes**

Each prefab: the FBX's root GameObject (so it carries its own embedded material — confirmed already present on `Coin.fbx`/`Star.fbx`/`Heart_Full.fbx`, same pattern as `Crate.fbx` in the existing `Crate.prefab`), a `Rigidbody`, a convex `MeshCollider` (matches how the existing `Crate.prefab` is set up — verify with `Find("Crate").GetComponent<MeshCollider>().convex` if unsure before copying the pattern), the `PowerUp` tag, and a `PowerUpPickup` component wired to the matching definition asset.

```csharp
// create_powerup_prefabs.cs
var sb = new System.Text.StringBuilder();

GameObject BuildPickupPrefab(string fbxPath, string prefabName, string tint, string definitionAssetPath)
{
    var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
    var instance = GameObject.Instantiate(fbx);
    instance.name = prefabName;
    instance.tag = "PowerUp";

    var rb = instance.AddComponent<Rigidbody>();
    rb.mass = 1f;

    // FBX imports commonly put the actual mesh on a child, not the root -
    // MeshCollider only auto-populates from a MeshFilter on the SAME
    // GameObject it's added to, so assign the mesh explicitly rather than
    // relying on that auto-population (which would silently leave the
    // collider empty if the mesh is on a child).
    var meshFilter = instance.GetComponentInChildren<MeshFilter>();
    var meshCollider = instance.AddComponent<MeshCollider>();
    meshCollider.sharedMesh = meshFilter.sharedMesh;
    meshCollider.convex = true;

    var pickup = instance.AddComponent<PowerUpPickup>();
    var pickupSo = new UnityEditor.SerializedObject(pickup);
    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(definitionAssetPath);
    pickupSo.FindProperty("definition").objectReferenceValue = definition;
    pickupSo.ApplyModifiedProperties();

    // Tint the embedded material's main color so the three pickups read as
    // visually distinct from each other and from the hazard crates at a
    // glance while falling.
    var renderer = instance.GetComponentInChildren<Renderer>();
    if (renderer != null && renderer.sharedMaterial != null)
    {
        var mat = new Material(renderer.sharedMaterial);
        ColorUtility.TryParseHtmlString(tint, out var color);
        mat.color = color;
        renderer.sharedMaterial = mat;
    }

    var prefabPath = "Assets/Prefabs/" + prefabName + ".prefab";
    var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
    GameObject.DestroyImmediate(instance);
    return prefab;
}

var coinPrefab = BuildPickupPrefab("Assets/Platformer Pack/FBX/Coin.fbx", "PowerUp_SpeedBoost", "#3B9CFF", "Assets/PowerUps/SpeedBoost.asset");
sb.AppendLine("Created " + coinPrefab.name + ".prefab");

var starPrefab = BuildPickupPrefab("Assets/Platformer Pack/FBX/Star.fbx", "PowerUp_Invincibility", "#FFD23B", "Assets/PowerUps/Invincibility.asset");
sb.AppendLine("Created " + starPrefab.name + ".prefab");

var heartPrefab = BuildPickupPrefab("Assets/Platformer Pack/FBX/Heart_Full.fbx", "PowerUp_Shield", "#FF4B4B", "Assets/PowerUps/Shield.asset");
sb.AppendLine("Created " + heartPrefab.name + ".prefab");

UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
```

Run via `eval_file`. If any `BuildPickupPrefab` call throws (e.g. `MeshCollider` convex fails because a mesh has too many vertices — Kenney-pack pickups are simple low-poly shapes so this is unlikely, but check the error message if it happens), fall back to a `BoxCollider` sized to the renderer's bounds instead of a convex `MeshCollider` for that one prefab, matching how simpler colliders are used elsewhere in this project.

- [ ] **Step 4: Verify the prefabs visually**

Enter Play mode, spawn one of each near the camera, screenshot, compare against the tints specified in Step 3, then exit Play mode per the Global Constraints Play-mode discipline:

```csharp
// spawn_powerup_preview.cs
var coin = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_SpeedBoost.prefab");
var star = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_Invincibility.prefab");
var heart = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_Shield.prefab");

GameObject.Instantiate(coin, new Vector3(-2f, 1f, 0f), Quaternion.identity);
GameObject.Instantiate(star, new Vector3(0f, 1f, 0f), Quaternion.identity);
GameObject.Instantiate(heart, new Vector3(2f, 1f, 0f), Quaternion.identity);

return "spawned preview pickups";
```

```
unity command --format json editor_play
```
(wait ~1s)
```
unity command --format json eval_file -- --file "spawn_powerup_preview.cs" --timeout 15000
```
(wait ~1s)
```
unity command --format json capture_game_view -- --source screen --width 1280 --height 720 --save_path "Screenshots/powerups_preview.png"
```
Then read `Assets/Screenshots/powerups_preview.png` and confirm three visually distinct, correctly tinted, correctly shaped objects are visible. Then stop Play mode and reload the scene from disk per Global Constraints, then delete the screenshot (`rm -rf Assets/Screenshots Assets/Screenshots.meta`) and refresh.

- [ ] **Step 5: Create the always-active PowerUpManager GameObject**

`PowerUpManager` must not be nested under the `GameManager` GameObject, which starts inactive and only activates when Play is clicked — nesting it there would reintroduce the same "component queried before its Awake() ran" bug already fixed once this session for `GameManager`'s own high-score loading (see `Assets/Scripts/GameManager.cs` git history, commit around the `Awake()`/`Start()` split). It needs to exist as its own always-active root object so `Awake()` runs at scene load.

```csharp
// create_powerup_manager_object.cs
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
foreach (var root in scene.GetRootGameObjects())
{
    if (root.name == "PowerUpManager")
    {
        return "PowerUpManager GameObject already exists.";
    }
}

var go = new GameObject("PowerUpManager");
go.AddComponent<PowerUpManager>();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "Created PowerUpManager GameObject.";
```

- [ ] **Step 6: Wire GameManager's PowerUpSpawner**

Add a `PowerUpSpawner` component to the `GameManager` GameObject (same object `GameManager.cs` lives on — matches the spec's stated placement), populate its `pickupPrefabs` array, and wire `GameManager.powerUpSpawner`.

```csharp
// wire_powerup_spawner.cs
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
GameObject gameManagerGo = null;
foreach (var root in scene.GetRootGameObjects())
{
    if (root.name == "GameManager") gameManagerGo = root;
}

var spawner = gameManagerGo.GetComponent<PowerUpSpawner>();
if (spawner == null)
{
    spawner = gameManagerGo.AddComponent<PowerUpSpawner>();
}

var coinPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_SpeedBoost.prefab");
var starPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_Invincibility.prefab");
var heartPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_Shield.prefab");

var spawnerSo = new UnityEditor.SerializedObject(spawner);
var prefabsProp = spawnerSo.FindProperty("pickupPrefabs");
prefabsProp.arraySize = 3;
prefabsProp.GetArrayElementAtIndex(0).objectReferenceValue = coinPrefab;
prefabsProp.GetArrayElementAtIndex(1).objectReferenceValue = starPrefab;
prefabsProp.GetArrayElementAtIndex(2).objectReferenceValue = heartPrefab;
// minDrag/maxDrag/spawnMinX/spawnMaxX/spawnHeight are left at their [SerializeField]
// defaults (already matching the hazard spawn geometry in GameManager.SpawnHazards).
spawnerSo.ApplyModifiedProperties();

var gameManager = gameManagerGo.GetComponent<GameManager>();
var gmSo = new UnityEditor.SerializedObject(gameManager);
gmSo.FindProperty("powerUpSpawner").objectReferenceValue = spawner;
gmSo.ApplyModifiedProperties();

UnityEditor.EditorUtility.SetDirty(gameManagerGo);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "Wired PowerUpSpawner onto GameManager with 3 prefabs.";
```

- [ ] **Step 7: Save the scene and verify clean console**

```
unity command --format json save_scene
```
Then run the refresh/console-check sequence from Global Constraints.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Create power-up definitions, prefabs, PowerUpManager object, and wire GameManager's spawner"
```

(Review `git status` before this commit to confirm only expected files changed: `Assets/Scenes/Game.unity`, `ProjectSettings/TagManager.asset`, the 3 new files under `Assets/PowerUps/`, the 3 new prefabs + `.meta` files under `Assets/Prefabs/`, and no stray `Assets/Screenshots/` leftovers.)

---

## Task 11: HUD (icon + countdown per active power-up)

**Files:**
- Create: `Assets/Scripts/PowerUps/PowerUpHUDIcon.cs`
- Create: `Assets/Scripts/PowerUps/PowerUpHUD.cs`
- Modify (via Editor): `Assets/Scenes/Game.unity` (new UI container + icon prefab)

**Interfaces:**
- Consumes: `PowerUpManager.OnPowerUpGranted/OnPowerUpExpired`, `PowerUpDefinition.Icon` (Tasks 1–2)
- Produces: `class PowerUpHUDIcon : MonoBehaviour` with `void Initialize(Sprite icon, float duration)`; `class PowerUpHUD : MonoBehaviour`

- [ ] **Step 1: Write PowerUpHUDIcon**

```csharp
// Assets/Scripts/PowerUps/PowerUpHUDIcon.cs
using UnityEngine;
using UnityEngine.UI;

public class PowerUpHUDIcon : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private TMPro.TextMeshProUGUI countdownText;

    private float remainingTime;
    private bool hasTimer;

    public void Initialize(Sprite icon, float duration)
    {
        iconImage.sprite = icon;
        hasTimer = duration > 0f;
        remainingTime = duration;
        countdownText.gameObject.SetActive(hasTimer);
        UpdateCountdownText();
    }

    private void Update()
    {
        if (!hasTimer)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime < 0f)
        {
            remainingTime = 0f;
        }
        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        if (hasTimer)
        {
            countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
        }
    }
}
```

- [ ] **Step 2: Write PowerUpHUD**

```csharp
// Assets/Scripts/PowerUps/PowerUpHUD.cs
using System.Collections.Generic;
using UnityEngine;

public class PowerUpHUD : MonoBehaviour
{
    [SerializeField]
    private PowerUpManager powerUpManager;
    [SerializeField]
    private PowerUpHUDIcon iconPrefab;
    [SerializeField]
    private Transform container;

    private readonly Dictionary<PowerUpDefinition, PowerUpHUDIcon> activeIcons = new Dictionary<PowerUpDefinition, PowerUpHUDIcon>();

    private void OnEnable()
    {
        powerUpManager.OnPowerUpGranted += HandleGranted;
        powerUpManager.OnPowerUpExpired += HandleExpired;
    }

    private void OnDisable()
    {
        powerUpManager.OnPowerUpGranted -= HandleGranted;
        powerUpManager.OnPowerUpExpired -= HandleExpired;
    }

    private void HandleGranted(PowerUpDefinition definition, float duration)
    {
        if (activeIcons.TryGetValue(definition, out var existingIcon))
        {
            existingIcon.Initialize(definition.Icon, duration);
            return;
        }

        var icon = Instantiate(iconPrefab, container);
        icon.Initialize(definition.Icon, duration);
        activeIcons[definition] = icon;
    }

    private void HandleExpired(PowerUpDefinition definition)
    {
        if (activeIcons.TryGetValue(definition, out var icon))
        {
            Destroy(icon.gameObject);
            activeIcons.Remove(definition);
        }
    }
}
```

- [ ] **Step 3: Refresh and verify clean compile**

Run the refresh/console-check sequence.

- [ ] **Step 4: Commit the scripts**

```bash
git add Assets/Scripts/PowerUps/PowerUpHUDIcon.cs Assets/Scripts/PowerUps/PowerUpHUDIcon.cs.meta Assets/Scripts/PowerUps/PowerUpHUD.cs Assets/Scripts/PowerUps/PowerUpHUD.cs.meta
git commit -m "Add PowerUpHUD and PowerUpHUDIcon scripts"
```

- [ ] **Step 5: Produce icon sprites**

`PowerUpDefinition.Icon` needs a `Sprite`. Render each pickup prefab to a small transparent-background PNG and import it as a UI sprite, rather than hand-authoring new art:

```csharp
// render_powerup_icons.cs
// Renders each pickup prefab in isolation on a temporary camera against a
// transparent background, at a fixed angle, and saves a small PNG. Run once
// per prefab (change prefabPath/outputPath each time), in Edit mode.
string prefabPath = "Assets/Prefabs/PowerUp_SpeedBoost.prefab"; // change per icon
string outputPath = "Assets/PowerUps/Icons/SpeedBoost_Icon.png"; // change per icon

System.IO.Directory.CreateDirectory("Assets/PowerUps/Icons");

var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
var previewInstance = GameObject.Instantiate(prefab, new Vector3(0, -1000, 0), Quaternion.Euler(20, 35, 0));

var cameraGo = new GameObject("IconCamera");
var cam = cameraGo.AddComponent<Camera>();
cam.transform.position = previewInstance.transform.position + new Vector3(0, 0.6f, -1.2f);
cam.transform.LookAt(previewInstance.transform.position);
cam.clearFlags = CameraClearFlags.SolidColor;
cam.backgroundColor = new Color(0, 0, 0, 0);
cam.orthographic = true;
cam.orthographicSize = 0.7f;

var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
cam.targetTexture = rt;
cam.Render();

RenderTexture.active = rt;
var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
tex.Apply();
RenderTexture.active = null;
cam.targetTexture = null;

System.IO.File.WriteAllBytes(outputPath, tex.EncodeToPNG());

GameObject.DestroyImmediate(previewInstance);
GameObject.DestroyImmediate(cameraGo);
GameObject.DestroyImmediate(rt);
GameObject.DestroyImmediate(tex);

UnityEditor.AssetDatabase.ImportAsset(outputPath);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(outputPath);
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.alphaIsTransparency = true;
importer.SaveAndReimport();

return "Rendered icon to " + outputPath;
```

Run this three times (once per prefab/output path: `PowerUp_SpeedBoost.prefab` → `SpeedBoost_Icon.png`, `PowerUp_Invincibility.prefab` → `Invincibility_Icon.png`, `PowerUp_Shield.prefab` → `Shield_Icon.png`), editing the two `string` lines each time. After each run, read the resulting PNG file to confirm it shows a clearly recognizable, correctly tinted silhouette on a transparent background before moving to the next one — if the framing is off (object cut off or too small), adjust `cam.orthographicSize` and re-run rather than proceeding with a bad icon.

- [ ] **Step 6: Assign icons to the definitions**

```csharp
// assign_powerup_icons.cs
void AssignIcon(string definitionPath, string iconPath)
{
    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(definitionPath);
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    var so = new UnityEditor.SerializedObject(definition);
    so.FindProperty("icon").objectReferenceValue = sprite;
    so.ApplyModifiedProperties();
}

AssignIcon("Assets/PowerUps/SpeedBoost.asset", "Assets/PowerUps/Icons/SpeedBoost_Icon.png");
AssignIcon("Assets/PowerUps/Invincibility.asset", "Assets/PowerUps/Icons/Invincibility_Icon.png");
AssignIcon("Assets/PowerUps/Shield.asset", "Assets/PowerUps/Icons/Shield_Icon.png");

UnityEditor.AssetDatabase.SaveAssets();
return "Assigned icons to all 3 definitions.";
```

- [ ] **Step 7: Build the HUD UI in-scene**

Places the strip directly below the existing `Canvas/Score` HUD (same left edge as `Score:`/`Best:`, matching the alignment work already done on that HUD — see the existing `HighScoreText` for the reference X position), builds one small icon+countdown prefab, and wires `PowerUpHUD`'s references.

```csharp
// build_powerup_hud.cs
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
GameObject Find(string path)
{
    var parts = path.Split('/');
    Transform current = null;
    foreach (var root in scene.GetRootGameObjects())
        if (root.name == parts[0]) current = root.transform;
    if (current == null) return null;
    for (int i = 1; i < parts.Length; i++)
    {
        current = current.Find(parts[i]);
        if (current == null) return null;
    }
    return current.gameObject;
}

var scoreRoot = Find("Canvas/Score");

// Container: a horizontal row below "Best:", left-aligned with it.
var containerGo = new GameObject("PowerUpHUDContainer", typeof(RectTransform));
containerGo.transform.SetParent(scoreRoot.transform, false);
var containerRt = containerGo.GetComponent<RectTransform>();
containerRt.anchoredPosition = new Vector2(-107.36f, -90f); // same X as Best: (see HighScoreText)
containerRt.sizeDelta = new Vector2(300f, 60f);
var layout = containerGo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
layout.spacing = 8f;
layout.childAlignment = TextAnchor.MiddleLeft;
layout.childControlWidth = false;
layout.childControlHeight = false;

// Icon prefab: a 48x48 Image with a small countdown TMP text overlaid at the bottom.
var iconGo = new GameObject("PowerUpIcon", typeof(RectTransform));
var iconRt = iconGo.GetComponent<RectTransform>();
iconRt.sizeDelta = new Vector2(48f, 48f);
var image = iconGo.AddComponent<UnityEngine.UI.Image>();
image.preserveAspect = true;

var countdownGo = new GameObject("Countdown", typeof(RectTransform));
countdownGo.transform.SetParent(iconGo.transform, false);
var countdownRt = countdownGo.GetComponent<RectTransform>();
countdownRt.anchorMin = new Vector2(0f, 0f);
countdownRt.anchorMax = new Vector2(1f, 0.4f);
countdownRt.offsetMin = Vector2.zero;
countdownRt.offsetMax = Vector2.zero;
var countdownText = countdownGo.AddComponent<TMPro.TextMeshProUGUI>();
countdownText.alignment = TMPro.TextAlignmentOptions.Center;
countdownText.fontSize = 20;
countdownText.color = Color.white;
countdownText.text = "0";

var iconScript = iconGo.AddComponent<PowerUpHUDIcon>();
var iconSo = new UnityEditor.SerializedObject(iconScript);
iconSo.FindProperty("iconImage").objectReferenceValue = image;
iconSo.FindProperty("countdownText").objectReferenceValue = countdownText;
iconSo.ApplyModifiedProperties();

var iconPrefabPath = "Assets/Prefabs/PowerUpHUDIcon.prefab";
var iconPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(iconGo, iconPrefabPath);
GameObject.DestroyImmediate(iconGo);

// PowerUpHUD component on the container, wired to the live PowerUpManager and the icon prefab.
var powerUpManagerGo = Find("PowerUpManager");
var hud = containerGo.AddComponent<PowerUpHUD>();
var hudSo = new UnityEditor.SerializedObject(hud);
hudSo.FindProperty("powerUpManager").objectReferenceValue = powerUpManagerGo.GetComponent<PowerUpManager>();
hudSo.FindProperty("iconPrefab").objectReferenceValue = iconPrefab.GetComponent<PowerUpHUDIcon>();
hudSo.FindProperty("container").objectReferenceValue = containerRt;
hudSo.ApplyModifiedProperties();

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "Built PowerUpHUD container + icon prefab, wired references.";
```

- [ ] **Step 8: Save, refresh, verify clean console**

```
unity command --format json save_scene
```
Then run the refresh/console-check sequence.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Add power-up HUD (icon + countdown strip) with rendered icons"
```

(Review `git status` first — expect `Assets/Scenes/Game.unity`, `Assets/Prefabs/PowerUpHUDIcon.prefab(+.meta)`, `Assets/PowerUps/Icons/*.png(+.meta)`, and the 3 modified `.asset` definition files under `Assets/PowerUps/`.)

---

## Task 12: End-to-end live verification

**Files:** none (verification only)

This exercises the spec's full testing plan (section 6) against the live Editor: fall/land physics, each effect's measurable behavior, stacking, same-type refresh, hazard-destruction on Invincible/Shield contact, HUD icon lifecycle, and a clean console through a full grant→expire→restart cycle.

- [ ] **Step 1: Enter Play mode and start a run**

```
unity command --format json editor_play
```
Wait ~0.5s, then click Play via the button's real `onClick` (matches how this project has verified Main Menu → gameplay transitions all session):
```csharp
// click_play.cs
var playBtn = GameObject.Find("Canvas/MainMenu/Play").GetComponent<UnityEngine.UI.Button>();
playBtn.onClick.Invoke();
return "Play clicked";
```
Wait ~1.5s for the transition, then move the player somewhere hazards won't reach so it survives long enough to verify (this project's established pattern from earlier phases — direct `transform.position` teleports can spuriously trigger `FallDownTrigger`'s `OnTriggerExit`, so keep the target position within the platform's existing X/Z footprint, just elevated):
```csharp
// safe_player.cs
GameObject player = GameObject.Find("NewPlayer");
var rb = player.GetComponent<Rigidbody>();
player.transform.position = new Vector3(0f, 5f, 0f);
rb.linearVelocity = Vector3.zero;
rb.useGravity = false;
return "player parked safely";
```

- [ ] **Step 2: Verify fall/land physics for each pickup type**

```csharp
// spawn_and_track_pickup.cs
var coinPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PowerUp_SpeedBoost.prefab");
var instance = GameObject.Instantiate(coinPrefab, new Vector3(3f, 8f, 0f), Quaternion.identity);
return "spawned at " + instance.transform.position;
```
Run, wait ~2s, then re-query the same object's Y position (find it via `GameObject.Find` won't work for an unnamed clone reliably — instead search by component: `GameObject.FindObjectsByType<PowerUpPickup>(FindObjectsInactive.Exclude)` and print each one's position) and confirm Y has decreased and eventually stabilizes near the platform surface height (~0.04–0.05, matching the ground height established earlier this project for this same X/Z region), not falling through or floating.

- [ ] **Step 3: Verify Speed Boost grants, measurably changes movement, and expires**

With the player still parked safely (Step 1), re-enable gravity and give it solid ground first, then grant directly and measure:
```csharp
// grant_and_check_speed.cs
var player = GameObject.Find("NewPlayer");
var rb = player.GetComponent<Rigidbody>();
rb.useGravity = true;
player.transform.position = new Vector3(0f, 3f, -2f);

var def = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerUpDefinition>("Assets/PowerUps/SpeedBoost.asset");
PowerUpManager.Instance.Grant(def);

return "Granted Speed Boost. SpeedMultiplier=" + PowerUpManager.Instance.SpeedMultiplier;
```
Expected: `SpeedMultiplier=1.6`. Confirm the HUD shows a speed-boost icon with a counting-down number (screenshot the game view, same `capture_game_view` pattern used throughout this project, and read the PNG). Wait 7 real seconds (longer than the 6s duration), then re-check `PowerUpManager.Instance.SpeedMultiplier` — expected back to `1`, and the HUD icon gone (screenshot again).

Both steps below need to check `Canvas/GameOverMenu`'s active state. It starts **inactive**, and `GameObject.Find` only finds active objects in the hierarchy — calling it naively would return `null` and throw on `.activeSelf` exactly when checking the "still inactive" case. Use a find-including-inactive helper (the same pattern used successfully throughout this project's live-Editor verification all session):

```csharp
// find_helper.cs — reference implementation, inline this function at the top
// of any eval script in this task that needs to look up a scene object.
GameObject Find(string path)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var parts = path.Split('/');
    Transform current = null;
    foreach (var root in scene.GetRootGameObjects())
        if (root.name == parts[0]) current = root.transform;
    if (current == null) return null;
    for (int i = 1; i < parts.Length; i++)
    {
        current = current.Find(parts[i]);
        if (current == null) return null;
    }
    return current.gameObject;
}
```

- [ ] **Step 4: Verify Invincibility destroys hazards on contact instead of ending the run**

```csharp
// grant_invincibility_and_collide.cs
GameObject Find(string path)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var parts = path.Split('/');
    Transform current = null;
    foreach (var root in scene.GetRootGameObjects())
        if (root.name == parts[0]) current = root.transform;
    if (current == null) return null;
    for (int i = 1; i < parts.Length; i++)
    {
        current = current.Find(parts[i]);
        if (current == null) return null;
    }
    return current.gameObject;
}

var def = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerUpDefinition>("Assets/PowerUps/Invincibility.asset");
PowerUpManager.Instance.Grant(def);

var player = GameObject.Find("NewPlayer");
var hazardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Crate.prefab");
var hazard = GameObject.Instantiate(hazardPrefab, player.transform.position + new Vector3(0f, 0.5f, 0f), Quaternion.identity);

return "IsInvincible=" + PowerUpManager.Instance.IsInvincible + ", spawned hazard instanceID=" + hazard.GetInstanceID();
```
Wait ~1s for physics to resolve the overlap into a real collision, then confirm via `unity command console --level error` that no exception occurred, and separately confirm the game did **not** end and the spawned hazard was destroyed:
```csharp
// check_invincibility_result.cs
GameObject Find(string path)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var parts = path.Split('/');
    Transform current = null;
    foreach (var root in scene.GetRootGameObjects())
        if (root.name == parts[0]) current = root.transform;
    if (current == null) return null;
    for (int i = 1; i < parts.Length; i++)
    {
        current = current.Find(parts[i]);
        if (current == null) return null;
    }
    return current.gameObject;
}

var gameOverMenu = Find("Canvas/GameOverMenu");
var gameEnded = gameOverMenu.activeSelf;
var remainingHazards = GameObject.FindGameObjectsWithTag("Hazard").Length;

return "gameEnded=" + gameEnded + " (expected False), remainingHazards=" + remainingHazards + " (expected 0, since only the one spawned in Step 4 existed and it should have been destroyed on contact)";
```

- [ ] **Step 5: Verify Shield absorbs exactly one hit then breaks**

```csharp
// grant_shield_and_collide_once.cs
var def = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerUpDefinition>("Assets/PowerUps/Shield.asset");
PowerUpManager.Instance.Grant(def);
var beforeFirstHit = "HasShield before any hit: " + PowerUpManager.Instance.HasShield;

var player = GameObject.Find("NewPlayer");
var hazardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Crate.prefab");
GameObject.Instantiate(hazardPrefab, player.transform.position + new Vector3(0f, 0.5f, 0f), Quaternion.identity);

return beforeFirstHit;
```
Wait ~1s, then check the result with the same `Find` helper as Step 4:
```csharp
// check_shield_after_first_hit.cs
GameObject Find(string path)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var parts = path.Split('/');
    Transform current = null;
    foreach (var root in scene.GetRootGameObjects())
        if (root.name == parts[0]) current = root.transform;
    if (current == null) return null;
    for (int i = 1; i < parts.Length; i++)
    {
        current = current.Find(parts[i]);
        if (current == null) return null;
    }
    return current.gameObject;
}

return "HasShield=" + PowerUpManager.Instance.HasShield + " (expected False), gameEnded=" + Find("Canvas/GameOverMenu").activeSelf + " (expected False)";
```
Then spawn a **second** hazard directly on the player the same way as Step 5's first script — this time, since the shield is already consumed, confirm the game **does** end:
```csharp
// check_shield_after_second_hit.cs
GameObject Find(string path)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var parts = path.Split('/');
    Transform current = null;
    foreach (var root in scene.GetRootGameObjects())
        if (root.name == parts[0]) current = root.transform;
    if (current == null) return null;
    for (int i = 1; i < parts.Length; i++)
    {
        current = current.Find(parts[i]);
        if (current == null) return null;
    }
    return current.gameObject;
}

return "gameEnded=" + Find("Canvas/GameOverMenu").activeSelf + " (expected True this time - shield was already spent)";
```

- [ ] **Step 6: Verify stacking (different types) and same-type refresh**

Restart the run first (click Restart on the Game Over screen from Step 5, or call `GameManager.Instance.Enable()` after re-parking the player safely), then:
```csharp
// verify_stacking_and_refresh.cs
var speedDef = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerUpDefinition>("Assets/PowerUps/SpeedBoost.asset");
var invincibilityDef = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerUpDefinition>("Assets/PowerUps/Invincibility.asset");

PowerUpManager.Instance.Grant(speedDef);
PowerUpManager.Instance.Grant(invincibilityDef);
var bothActive = "SpeedMultiplier=" + PowerUpManager.Instance.SpeedMultiplier + ", IsInvincible=" + PowerUpManager.Instance.IsInvincible;

System.Threading.Thread.Sleep(2000);
PowerUpManager.Instance.Grant(speedDef); // re-collect the same type before it expires
var stillActiveAfterRefresh = "After re-grant, SpeedMultiplier=" + PowerUpManager.Instance.SpeedMultiplier + " (should still be boosted, not doubled)";

return bothActive + " | " + stillActiveAfterRefresh;
```
Expected: `SpeedMultiplier=1.6, IsInvincible=True | After re-grant, SpeedMultiplier=1.6 (should still be boosted, not doubled)`. The key check is that re-granting Speed Boost while already active doesn't compound the multiplier (e.g. to 2.56) — confirms the "same type refreshes, doesn't stack" rule from the spec.

- [ ] **Step 7: Full restart cycle with a clean console**

```
unity command --format json console -- --level error
```
Confirm `groundTruth.consoleErrors` is `0` after everything above. If not, read the specific error entries (not just the count) and trace back to which step introduced it before treating this task as done.

- [ ] **Step 8: Clean up and exit Play mode**

```
unity command --format json editor_stop
```
Wait ~2s, confirm `EditorApplication.isPlaying` is `false` via a small eval, then reload the scene from disk (`EditorSceneManager.OpenScene`) per Global Constraints so none of this task's live test mutations (spawned hazards, parked player, granted effects) get accidentally saved. Delete any `Assets/Screenshots/` files created during this task and refresh.

- [ ] **Step 9: Commit (if anything changed)**

Run `git status`. If Task 12 was pure verification with no file changes (expected — this task doesn't touch source), there's nothing to commit. If the scene shows as dirty due to any residual state, investigate why before deciding whether to save/commit or discard — don't commit a scene change you can't explain.

---

## Summary of what ships

Speed Boost, Invincibility, and Shield fall like hazards, are collected on
contact, and grant their effect for the tuned duration (or until consumed,
for Shield). Active effects show as icons with countdowns near the score
HUD. Adding a 4th type later (Score Bonus, or anything else) means: one new
`PowerUpDefinition` subclass + `IPowerUpEffect` pair (Tasks 3–5 are the
templates), one new prefab (Task 10's `BuildPickupPrefab` pattern), and
adding it to `PowerUpSpawner.pickupPrefabs` — no changes to `PowerUpManager`,
`PowerUpPickup`, `PowerUpHUD`, `GameManager`, or `Player`.
