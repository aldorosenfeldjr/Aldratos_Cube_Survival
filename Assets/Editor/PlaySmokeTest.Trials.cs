using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trial-over prompt and interstitial checks for the play smoke test (economy step 4). Runs inside a live run and
// drives the real game-over screen: the game ends, the prompt appears, its buttons are clicked, Restart is pressed.
public static partial class PlaySmokeTest
{
    private static IEnumerator TrialFlowChecks(GameManager gameManager, HazardSpawner hazardSpawner)
    {
        var catalog = UnlockCatalog.Instance;
        var free = catalog.DefaultFor(UnlockCategory.Character);
        var rare = catalog.InCategory(UnlockCategory.Character).Find(item => item.Tier == PriceTier.Rare);
        var gameOverMenu = CanvasChild("GameOverMenu");
        var panel = CanvasChild("GameOverMenu/TrialPanel");
        var restart = CanvasChild("GameOverMenu/Restart").GetComponent<Button>();
        Check("game-over screen has the trial-over prompt", panel != null && !panel.gameObject.activeSelf);
        if (panel == null)
        {
            yield break;
        }

        var ads = new FakeAdService();
        var store = new FakeStoreService();
        Services.Ads = ads;
        Services.Store = store;
        var every = EconomyConfig.Instance.GameOversPerInterstitial;

        UseFreshTempSave();
        Wallet.Add(UnlockService.Price(rare));
        UnlockService.StartTrial(rare);
        SaveService.Data.trials[0].runsLeft = 1;

        // The last trial run ends: the prompt offers Buy, Unlock (affordable), Watch ad; the main buttons are locked meanwhile.
        foreach (var step in Steps(EndRun(gameManager, gameOverMenu))) { yield return step; }
        var buy = panel.Find("Buy").GetComponent<Button>();
        var unlock = panel.Find("Unlock").GetComponent<Button>();
        var watch = panel.Find("WatchAd").GetComponent<Button>();
        var title = panel.Find("Title").GetComponent<TMP_Text>().text;
        Check("trial ends on game over: prompt names the item and offers Buy, Unlock, Watch ad",
            gameManager.TrialEnded == rare && panel.gameObject.activeSelf && buy.gameObject.activeSelf && unlock.gameObject.activeSelf && watch.gameObject.activeSelf
            && title == $"Trial over: keep {rare.DisplayName}?" && UnlockService.Selected(UnlockCategory.Character) == free && !restart.interactable,
            $"title='{title}' ended={gameManager.TrialEnded} trials={SaveService.Data.trials.Count} selected={UnlockService.Selected(UnlockCategory.Character)} panelActive={panel.gameObject.activeSelf}");

        ads.NextRewardEarned = false;
        watch.onClick.Invoke();
        Check("closing the ad early keeps the prompt open and grants nothing",
            panel.gameObject.activeSelf && !UnlockService.IsUsable(rare) && ads.RewardedShown == 1);

        SaveService.Data.gameOversSinceInterstitial = every - 1;
        ads.NextRewardEarned = true;
        watch.onClick.Invoke();
        Check("watching the ad from the prompt starts 3 more runs and closes it",
            !panel.gameObject.activeSelf && UnlockService.TrialRunsLeft(rare) == EconomyConfig.Instance.TrialRuns && UnlockService.Selected(UnlockCategory.Character) == rare && restart.interactable);

        // Leaving this game over must not also show an interstitial (a rewarded ad was just watched), even though one is due.
        foreach (var step in Steps(LeaveGameOver(gameManager, hazardSpawner, restart))) { yield return step; }
        Check("no interstitial after watching a rewarded ad on the same game over", ads.InterstitialsShown == 0);

        // A normal game over at the pacing threshold shows the interstitial when leaving.
        SaveService.Data.gameOversSinceInterstitial = every - 1;
        foreach (var step in Steps(EndRun(gameManager, gameOverMenu))) { yield return step; }
        var promptStayedAway = !panel.gameObject.activeSelf;
        foreach (var step in Steps(LeaveGameOver(gameManager, hazardSpawner, restart))) { yield return step; }
        Check($"game over {every}: interstitial shows on leaving, run restarts", promptStayedAway && ads.InterstitialsShown == 1 && gameManager.isActiveAndEnabled,
            $"shown={ads.InterstitialsShown}");

        // Dismissing the prompt: no interstitial on the game over where the trial ended, even when one is due.
        UnlockService.StartTrial(rare);
        SaveService.Data.trials[0].runsLeft = 1;
        SaveService.Data.gameOversSinceInterstitial = every - 1;
        foreach (var step in Steps(EndRun(gameManager, gameOverMenu))) { yield return step; }
        var promptOpen = panel.gameObject.activeSelf;
        panel.Find("NoThanks").GetComponent<Button>().onClick.Invoke();
        var closed = !panel.gameObject.activeSelf && restart.interactable && UnlockService.Selected(UnlockCategory.Character) == free;
        foreach (var step in Steps(LeaveGameOver(gameManager, hazardSpawner, restart))) { yield return step; }
        Check("No thanks closes the prompt; no interstitial on the game over where a trial ended", promptOpen && closed && ads.InterstitialsShown == 1);

        // Buying from the prompt keeps the item.
        UnlockService.StartTrial(rare);
        SaveService.Data.trials[0].runsLeft = 1;
        foreach (var step in Steps(EndRun(gameManager, gameOverMenu))) { yield return step; }
        buy.onClick.Invoke();
        Check("Buy from the prompt owns and selects the item",
            !panel.gameObject.activeSelf && UnlockService.IsOwned(rare) && UnlockService.Selected(UnlockCategory.Character) == rare && store.Owned.Contains(rare.ProductId));
        foreach (var step in Steps(LeaveGameOver(gameManager, hazardSpawner, restart))) { yield return step; }

        // PC: no ad and no store services, so a trial cannot be won; the prompt (if it ever appears) hides ad/store options.
        UseFreshTempSave();
        Wallet.Add(UnlockService.Price(rare));
        Services.Ads = new NullAdService();
        Services.Store = new NullStoreService();
        UnlockService.StartTrial(rare);
        SaveService.Data.trials[0].runsLeft = 1;
        foreach (var step in Steps(EndRun(gameManager, gameOverMenu))) { yield return step; }
        Check("no ads/store (PC): prompt shows no Buy or Watch ad",
            panel.gameObject.activeSelf && !buy.gameObject.activeSelf && !watch.gameObject.activeSelf && unlock.gameObject.activeSelf);
        unlock.onClick.Invoke();
        Check("Unlock with gems from the prompt owns and selects the item", !panel.gameObject.activeSelf && UnlockService.IsOwned(rare) && UnlockService.Selected(UnlockCategory.Character) == rare);
        foreach (var step in Steps(LeaveGameOver(gameManager, hazardSpawner, restart))) { yield return step; }

        Services.Ads = new FakeAdService();
        Services.Store = new FakeStoreService();
        UseFreshTempSave();
    }

    // The test driver only understands seconds and WaitUntil; a nested helper must be flattened into its steps.
    private static IEnumerable<object> Steps(IEnumerator nested)
    {
        while (nested.MoveNext())
        {
            yield return nested.Current;
        }
    }

    private static IEnumerator EndRun(GameManager gameManager, Transform gameOverMenu)
    {
        // The game-over screen only reads its state when it becomes active, so a run must be live before it ends.
        if (!gameManager.isActiveAndEnabled)
        {
            Failed.Add($"trial flow: no live run when the game should end (menuActive={gameOverMenu.gameObject.activeSelf})");
            yield break;
        }

        gameManager.GameOver();
        yield return WaitUntil(() => gameOverMenu.gameObject.activeInHierarchy);
        yield return 0.3f;
    }

    private static IEnumerator LeaveGameOver(GameManager gameManager, HazardSpawner hazardSpawner, Button restart)
    {
        if (!restart.interactable)
        {
            Failed.Add("Restart is locked: an overlay is still open");
            yield break;
        }

        restart.onClick.Invoke();
        yield return WaitUntil(() => gameManager.isActiveAndEnabled);
        hazardSpawner.StopSpawning();
        hazardSpawner.ClearAll();
        yield return 0.2f;
    }
}
