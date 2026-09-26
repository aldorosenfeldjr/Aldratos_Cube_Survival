using System;

/// <summary>
/// Paces interstitials to roughly one per <see cref="EconomyConfig.GameOversPerInterstitial"/> game overs, shown when the
/// player leaves the game-over screen (between runs, never mid-run or at launch). Skipped when Remove Ads is owned, when a
/// rewarded ad was already watched on this game-over screen, or when a trial just ended. A skipped one stays due.
/// </summary>
public static class InterstitialPacer
{
    private static bool due;
    private static bool suppressed;

    /// <summary>Call once per game over.</summary>
    public static void RegisterGameOver(bool trialJustEnded)
    {
        var data = SaveService.Data;
        data.gameOversSinceInterstitial++;
        due = !data.removeAds && data.gameOversSinceInterstitial >= EconomyConfig.Instance.GameOversPerInterstitial;
        suppressed = trialJustEnded;
        SaveService.Save();
    }

    /// <summary>The player watched a rewarded ad on this game-over screen: no interstitial after it.</summary>
    public static void MarkRewardedWatched()
    {
        suppressed = true;
    }

    /// <summary>Call when the player leaves the game-over screen; <paramref name="proceed"/> runs after any interstitial.</summary>
    public static void OnLeave(Action proceed)
    {
        var ads = Services.Ads;
        if (!due || suppressed || !ads.IsAvailable || !ads.InterstitialReady)
        {
            proceed();
            return;
        }

        due = false;
        SaveService.Data.gameOversSinceInterstitial = 0;
        SaveService.Save();
        ads.ShowInterstitial(proceed);
    }
}
