using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// One-click end-to-end check of the main game flow: Tools > Smoke Test > Run Play Smoke Test.
// Enters play mode, drives the real UI (menu -> level -> run), exercises score, every power-up,
// hazards, pause during a slowdown, restart and game over, then exits play mode and prints a short
// report to the console (filter on "SMOKE TEST"). Runs against a temp save file (never the real save) and restores the legacy PlayerPrefs high score afterwards.
// Needs the Core scene open. Adding a check = one block in Run().
[InitializeOnLoad]
public static partial class PlaySmokeTest
{
    private const string PendingKey = "PlaySmokeTest.Pending";
    private const string HadHighScoreKey = "PlaySmokeTest.HadHighScore";
    private const string SavedHighScoreKey = "PlaySmokeTest.SavedHighScore";
    private const double WaitTimeout = 10.0;
    private const string MeadowId = "Level_Meadow";
    private const string PlaygroundId = "Level_Playground";

    private static string TempSavePath => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cube_survival_smoketest_save.json");

    private sealed class Until
    {
        public Func<bool> Condition;
        public double Deadline;
    }

    private static IEnumerator steps;
    private static object current;
    private static double resumeAt;
    private static double startedAt;
    private static bool finished;
    private static bool timedOut;
    private static readonly List<string> Passed = new List<string>();
    private static readonly List<string> Failed = new List<string>();
    private static readonly List<string> ConsoleErrors = new List<string>();

    static PlaySmokeTest()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/Smoke Test/Run Play Smoke Test")]
    public static void Launch()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            Debug.LogWarning("SMOKE TEST: stop play mode / wait for compilation first.");
            return;
        }

        SessionState.SetBool(PendingKey, true);
        SessionState.SetBool(HadHighScoreKey, PlayerPrefs.HasKey(SaveService.LegacyHighScoreKey));
        SessionState.SetInt(SavedHighScoreKey, PlayerPrefs.GetInt(SaveService.LegacyHighScoreKey));
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            SessionState.SetBool(PendingKey, false);
            Begin();
        }
        else if (change == PlayModeStateChange.ExitingPlayMode)
        {
            Cleanup();
        }
    }

    private static void Begin()
    {
        Passed.Clear();
        Failed.Clear();
        ConsoleErrors.Clear();
        finished = false;
        startedAt = EditorApplication.timeSinceStartup;
        steps = Run();
        current = null;
        UseFreshTempSave();
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
    }

    private static void UseFreshTempSave()
    {
        System.IO.File.Delete(TempSavePath);
        SaveService.UseFile(TempSavePath);
    }

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            ConsoleErrors.Add(message);
        }
    }

    private static void Tick()
    {
        if (steps == null)
        {
            return;
        }

        var now = EditorApplication.timeSinceStartup;
        if (current is float && now < resumeAt)
        {
            return;
        }

        if (current is Until until && !until.Condition() && now < until.Deadline)
        {
            return;
        }

        timedOut = current is Until late && !late.Condition();

        try
        {
            if (!steps.MoveNext())
            {
                Finish();
                return;
            }
        }
        catch (Exception e)
        {
            Failed.Add($"exception: {e.GetType().Name}: {e.Message}");
            Finish();
            return;
        }

        current = steps.Current;
        if (current is float seconds)
        {
            resumeAt = now + seconds;
        }
        else if (current is Until next)
        {
            next.Deadline = now + WaitTimeout;
        }
    }

    private static Until WaitUntil(Func<bool> condition)
    {
        return new Until { Condition = condition };
    }

    private static void Check(string name, bool ok, string detail = null)
    {
        if (ok)
        {
            Passed.Add(name);
        }
        else
        {
            Failed.Add(detail == null ? name : $"{name} ({detail})");
        }
    }

    private static void Finish()
    {
        if (finished)
        {
            return;
        }

        finished = true;
        Check("no console errors", ConsoleErrors.Count == 0, ConsoleErrors.Count > 0 ? $"{ConsoleErrors.Count}, first: {ConsoleErrors[0]}" : null);

        var total = Passed.Count + Failed.Count;
        var report = new StringBuilder();
        report.Append(Failed.Count == 0 ? "SMOKE TEST PASS" : "SMOKE TEST FAIL");
        report.Append($" {Passed.Count}/{total} in {EditorApplication.timeSinceStartup - startedAt:0.0}s");
        if (Passed.Count > 0)
        {
            report.Append("\n  ok: ").Append(string.Join(", ", Passed));
        }
        foreach (var failure in Failed)
        {
            report.Append("\n  FAILED: ").Append(failure);
        }

        if (Failed.Count == 0)
        {
            Debug.Log(report.ToString());
        }
        else
        {
            Debug.LogError(report.ToString());
        }

        EditorApplication.isPlaying = false;
    }

    private static void Cleanup()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        steps = null;
        SaveService.UseFile(null);
        System.IO.File.Delete(TempSavePath);

        // Game over during the run may have saved a high score; put the player's own back.
        if (SessionState.GetBool(HadHighScoreKey, false))
        {
            PlayerPrefs.SetInt(SaveService.LegacyHighScoreKey, SessionState.GetInt(SavedHighScoreKey, 0));
        }
        else
        {
            PlayerPrefs.DeleteKey(SaveService.LegacyHighScoreKey);
        }
        PlayerPrefs.Save();
    }

    private static Transform CanvasChild(string path)
    {
        var canvas = GameObject.Find("Canvas");
        return canvas != null ? canvas.transform.Find(path) : null;
    }

    private static void Click(string canvasPath)
    {
        CanvasChild(canvasPath).GetComponent<Button>().onClick.Invoke();
    }

    private static IEnumerator Run()
    {
        var include = FindObjectsInactive.Include;
        var gameManager = UnityEngine.Object.FindAnyObjectByType<GameManager>(include);
        var hazardSpawner = UnityEngine.Object.FindAnyObjectByType<HazardSpawner>(include);
        var powerUpSpawner = UnityEngine.Object.FindAnyObjectByType<PowerUpSpawner>(include);
        var levelSelect = CanvasChild("LevelSelect");
        var hudContainer = CanvasChild("Score/PowerUpHUDContainer");
        if (gameManager == null || hazardSpawner == null || powerUpSpawner == null || levelSelect == null || hudContainer == null || PowerUpManager.Instance == null)
        {
            Failed.Add("Core scene is not open or is missing GameManager/spawners/HUD");
            yield break;
        }

        var powerUps = PowerUpManager.Instance;
        var time = TimeScaleController.Instance;

        // 0. Old PlayerPrefs high score migrates into Meadow's best (no clear granted), then the key is gone.
        PlayerPrefs.SetInt(SaveService.LegacyHighScoreKey, 42);
        SaveService.UseFile(TempSavePath);
        var migratedBest = SaveService.BestScore(MeadowId);
        Check("high score migration", migratedBest == 42 && !SaveService.IsCleared(MeadowId) && !PlayerPrefs.HasKey(SaveService.LegacyHighScoreKey),
            $"best={migratedBest} keyLeft={PlayerPrefs.HasKey(SaveService.LegacyHighScoreKey)}");
        UseFreshTempSave();

        // 0b. Selection screen (opened from the main menu, so it must run before Play consumes the menu).
        var selection = SelectionChecks();
        while (selection.MoveNext())
        {
            yield return selection.Current;
        }
        UseFreshTempSave();

        // 1. Menu flow, using the real buttons.
        Click("MainMenu/Play");
        yield return WaitUntil(() => levelSelect.gameObject.activeInHierarchy);
        Check("main menu -> level select", !timedOut);
        var tiles = levelSelect.GetComponentInChildren<GridLayoutGroup>(true).GetComponentsInChildren<Button>();
        Check("level select locks level 2", tiles.Length >= 2 && tiles[0].interactable && !tiles[1].interactable, $"tiles={tiles.Length}");
        tiles[0].onClick.Invoke();
        yield return WaitUntil(() => gameManager.isActiveAndEnabled);
        Check("level select -> run starts", !timedOut);
        if (timedOut)
        {
            yield break;
        }

        // Deterministic from here: nothing random can end the run mid-check.
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll();
        powerUpSpawner.StopSpawning();

        // 2. Score ticks.
        yield return 2.4f;
        Check("score ticks", gameManager.Score >= 2, $"score={gameManager.Score}");

        // 2b. The gem spawner drops a gem on its own (interval <= GemMaxInterval).
        var gemDeadline = Time.realtimeSinceStartup + GameConfig.Instance.GemMaxInterval + 2f;
        while (UnityEngine.Object.FindAnyObjectByType<GemPickup>() == null && Time.realtimeSinceStartup < gemDeadline)
        {
            yield return 0.25f;
        }
        Check("gem spawns on its own", UnityEngine.Object.FindAnyObjectByType<GemPickup>() != null);
        UnityEngine.Object.FindAnyObjectByType<GemSpawner>().StopSpawning(); // keep the wallet checks below deterministic
        foreach (var stray in UnityEngine.Object.FindObjectsByType<GemPickup>(FindObjectsSortMode.None))
        {
            UnityEngine.Object.Destroy(stray.gameObject);
        }

        // 3. Every power-up: effect applies and shows in the HUD.
        var badPowerUps = new List<string>();
        var definitions = AssetDatabase.FindAssets("t:PowerUpDefinition");
        foreach (var guid in definitions)
        {
            var definition = AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            powerUps.ResetAll();
            yield return 0.2f;
            var iconsBefore = hudContainer.childCount;
            powerUps.Grant(definition);
            yield return 1.2f;

            var effect = definition is ShieldDefinition ? powerUps.HasShield
                : definition is SpeedBoostDefinition ? powerUps.SpeedMultiplier > 1f
                : definition is InvincibilityDefinition ? powerUps.IsInvincible
                : definition is GemMultiplierDefinition ? powerUps.GemMultiplier > 1
                : true;
            if (!effect || hudContainer.childCount <= iconsBefore)
            {
                badPowerUps.Add($"{definition.name}: effect={effect} hudIcons {iconsBefore}->{hudContainer.childCount}");
            }
        }
        Check($"power-ups x{definitions.Length} (effect + HUD)", definitions.Length > 0 && badPowerUps.Count == 0, string.Join("; ", badPowerUps));
        powerUps.ResetAll();
        yield return 0.1f;

        // 3b. A real gem pickup lands on the player and credits the wallet; the multiplier doubles the value.
        var gemPlayer = UnityEngine.Object.FindAnyObjectByType<Player>();
        var gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gem.prefab");
        var gemStart = Wallet.Balance;
        UnityEngine.Object.Instantiate(gemPrefab, gemPlayer.transform.position + Vector3.up * 1.5f, Quaternion.identity);
        yield return 1.2f;
        var gemGain = Wallet.Balance - gemStart;
        Check("gem pickup credits wallet", gemGain == EconomyConfig.Instance.GemValue && gameManager.GemsCollected >= 1, $"gain={gemGain}");

        var multiplierDefinition = AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:GemMultiplierDefinition")[0]));
        powerUps.Grant(multiplierDefinition);
        var doubledStart = Wallet.Balance;
        gameManager.CollectGem();
        var doubledGain = Wallet.Balance - doubledStart;
        Check("gem multiplier doubles value", doubledGain == EconomyConfig.Instance.GemValue * 2 && gameManager.MultiplierBonus >= EconomyConfig.Instance.GemValue, $"gain={doubledGain} bonus={gameManager.MultiplierBonus}");
        powerUps.ResetAll();
        UseFreshTempSave(); // the level-clear checks below expect an empty wallet

        // 4. Hazards spawn and fall (invincible so a crate cannot end the run).
        var invincibility = AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:InvincibilityDefinition")[0]));
        powerUps.Grant(invincibility);
        hazardSpawner.BeginSpawning();
        yield return 2.5f;
        var hazards = GameObject.FindGameObjectsWithTag("Hazard").Length;
        Check("hazards spawn", hazards > 0, $"count={hazards}");
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll();
        powerUps.ResetAll();

        // 5. Pause in the middle of a power-up slowdown, then resume back to normal speed.
        powerUps.Grant(AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:ShieldDefinition")[0])));
        yield return 0.05f;
        gameManager.Pause();
        yield return 0.8f;
        var pausedScale = Time.timeScale;
        var pausedFlag = time.IsPaused;
        gameManager.Resume();
        yield return WaitUntil(() => Time.timeScale > 0.999f);
        Check("pause during slowdown + resume", pausedScale < 0.01f && pausedFlag && !timedOut, $"pausedScale={pausedScale:0.00} isPaused={pausedFlag} recovered={!timedOut}");
        powerUps.ResetAll();

        // 6. Restart resets the run.
        yield return 1.2f;
        gameManager.RestartGame();
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll();
        yield return 0.3f;
        var player = UnityEngine.Object.FindAnyObjectByType<Player>();
        var atSpawn = player != null && Vector3.Distance(player.transform.position, new Vector3(0f, 0.75f, 0f)) < 0.5f;
        Check("restart resets run", gameManager.Score == 0 && Mathf.Approximately(Time.timeScale, 1f) && atSpawn && gameManager.isActiveAndEnabled,
            $"score={gameManager.Score} timeScale={Time.timeScale:0.00} playerAtSpawn={atSpawn}");

        // 7. Game over shows the menu; its Restart button starts a new run.
        var gameOverMenu = CanvasChild("GameOverMenu");
        gameManager.GameOver();
        yield return WaitUntil(() => gameOverMenu.gameObject.activeInHierarchy);
        var shown = !timedOut && !gameManager.isActiveAndEnabled && Mathf.Approximately(Time.timeScale, 1f);
        yield return 2.5f;
        Click("GameOverMenu/Restart");
        yield return WaitUntil(() => gameManager.isActiveAndEnabled);
        hazardSpawner.StopSpawning();
        Check("game over -> restart", shown && !timedOut && !gameOverMenu.gameObject.activeSelf, $"menuShown={shown} runRestarted={!timedOut}");

        // 8. Level clear: reaching the (shortened) target credits the first-clear reward, unlocks level 2 and
        // pauses on the Success screen. Keep going resumes the same run without a second clear.
        var registry = AssetDatabase.LoadAssetAtPath<LevelRegistry>("Assets/Levels/LevelRegistry.asset");
        var successMenu = CanvasChild("SuccessMenu");
        gameManager.TargetSecondsOverride = 3;
        gameManager.RestartGame();
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll(); // BeginSpawning drops the first wave at once; it must not kill the test run
        powerUpSpawner.StopSpawning();
        yield return WaitUntil(() => successMenu.gameObject.activeInHierarchy);
        var cleared = !timedOut;
        yield return 1f;
        var firstReward = registry.LevelEntries[0].Theme.FirstClearReward;
        Check("clear at target: reward, unlock, pause", cleared && Wallet.Balance == firstReward && SaveService.IsCleared(MeadowId)
            && LevelProgression.IsUnlocked(registry, 1) && time.IsPaused && Time.timeScale < 0.01f && SaveService.BestScore(MeadowId) >= 3,
            $"shown={cleared} gems={Wallet.Balance}/{firstReward} cleared={SaveService.IsCleared(MeadowId)} paused={time.IsPaused} best={SaveService.BestScore(MeadowId)}");

        Click("SuccessMenu/KeepGoing");
        yield return WaitUntil(() => Time.timeScale > 0.999f);
        var resumed = !timedOut;
        var scoreAfterResume = gameManager.Score;
        yield return 1.5f;
        Check("keep going resumes run, no second clear", resumed && gameManager.Score > scoreAfterResume && !successMenu.gameObject.activeSelf && Wallet.Balance == firstReward,
            $"resumed={resumed} score {scoreAfterResume}->{gameManager.Score} gems={Wallet.Balance}");

        // 9. Save round trip: what is in memory equals what comes back from disk.
        var gemsBefore = Wallet.Balance;
        SaveService.Save();
        SaveService.Load();
        Check("save round trip", Wallet.Balance == gemsBefore && SaveService.IsCleared(MeadowId) && SaveService.BestScore(MeadowId) >= 3,
            $"gems={Wallet.Balance}/{gemsBefore} cleared={SaveService.IsCleared(MeadowId)}");

        // 10. Repeat clear pays the small reward; Next level loads the following level and starts a run.
        gameManager.RestartGame();
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll(); // BeginSpawning drops the first wave at once; it must not kill the test run
        powerUpSpawner.StopSpawning();
        yield return WaitUntil(() => successMenu.gameObject.activeInHierarchy);
        var repeatShown = !timedOut;
        var repeatReward = registry.LevelEntries[0].Theme.RepeatClearReward;
        var repeatPaid = Wallet.Balance == firstReward + repeatReward;
        Click("SuccessMenu/NextLevel");
        yield return WaitUntil(() => gameManager.isActiveAndEnabled && gameManager.LevelName == registry.LevelEntries[1].DisplayName);
        var loadedNext = !timedOut;
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll(); // BeginSpawning drops the first wave at once; it must not kill the test run
        yield return 0.3f;
        Check("repeat clear reward + next level", repeatShown && repeatPaid && loadedNext && Mathf.Approximately(Time.timeScale, 1f) && gameManager.Score <= 1,
            $"shown={repeatShown} repeatPaid={repeatPaid} nextLoaded={loadedNext} timeScale={Time.timeScale:0.00} score={gameManager.Score}");
        // 11. Game over after a clear (and Keep going) offers Next level; it loads the following level.
        gameManager.gameObject.SetActive(false);
        levelSelect.GetComponent<LevelSelect>().PlayLevel(MeadowId);
        yield return WaitUntil(() => gameManager.isActiveAndEnabled && gameManager.LevelName == registry.LevelEntries[0].DisplayName);
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll(); // BeginSpawning drops the first wave at once; it must not kill the test run
        powerUpSpawner.StopSpawning();
        yield return WaitUntil(() => successMenu.gameObject.activeInHierarchy);
        Click("SuccessMenu/KeepGoing");
        yield return 0.3f;
        gameManager.GameOver();
        yield return WaitUntil(() => gameOverMenu.gameObject.activeInHierarchy);
        var nextButton = gameOverMenu.transform.Find("NextLevel");
        var offered = nextButton != null && nextButton.gameObject.activeSelf;
        var offerDetail = $"clearedThisRun={gameManager.ClearedThisRun} hasNext={gameManager.HasNextLevel} button={(nextButton != null)} level={gameManager.LevelName}";
        Click("GameOverMenu/NextLevel");
        yield return WaitUntil(() => gameManager.isActiveAndEnabled && gameManager.LevelName == registry.LevelEntries[1].DisplayName);
        var reachedNext = !timedOut;
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll(); // BeginSpawning drops the first wave at once; it must not kill the test run
        Check("game over after clear: Next level", offered && reachedNext && !gameOverMenu.gameObject.activeSelf, $"offered={offered} reachedNext={reachedNext} {offerDetail}");
        gameManager.TargetSecondsOverride = 0;

        // 12. Trial-over prompt and interstitial pacing through the real game-over screen.
        var trialFlow = TrialFlowChecks(gameManager, hazardSpawner);
        while (trialFlow.MoveNext())
        {
            yield return trialFlow.Current;
        }
    }
}
