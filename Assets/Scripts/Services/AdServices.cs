using System;
using UnityEngine;

/// <summary>
/// Ads, as game code sees them. Real SDK adapters (AdMob) implement this on mobile builds; the Editor uses
/// <see cref="FakeAdService"/>; PC uses <see cref="NullAdService"/> (unavailable, so the UI hides every ad button).
/// </summary>
public interface IAdService
{
    /// <summary>False when this platform has no ads at all (PC).</summary>
    bool IsAvailable { get; }

    bool RewardedReady { get; }

    /// <summary>Shows a rewarded ad. <paramref name="onFinished"/> gets true only when the user earned the reward; closing early or a load failure gives false.</summary>
    void ShowRewarded(Action<bool> onFinished);

    bool InterstitialReady { get; }

    /// <summary>Shows an interstitial; <paramref name="onClosed"/> runs when it is gone (or straight away if it could not be shown).</summary>
    void ShowInterstitial(Action onClosed);
}

/// <summary>PC and any platform without an ad SDK: nothing is offered.</summary>
public class NullAdService : IAdService
{
    public bool IsAvailable => false;
    public bool RewardedReady => false;
    public bool InterstitialReady => false;

    public void ShowRewarded(Action<bool> onFinished)
    {
        onFinished?.Invoke(false);
    }

    public void ShowInterstitial(Action onClosed)
    {
        onClosed?.Invoke();
    }
}

/// <summary>Editor / test stand-in: instant "ad", configurable result, counts what was shown.</summary>
public class FakeAdService : IAdService
{
    public bool IsAvailable => true;
    public bool RewardedReady { get; set; } = true;
    public bool InterstitialReady { get; set; } = true;

    /// <summary>What the next rewarded ad reports: true = reward earned, false = closed early.</summary>
    public bool NextRewardEarned { get; set; } = true;

    public int RewardedShown { get; private set; }
    public int InterstitialsShown { get; private set; }

    public void ShowRewarded(Action<bool> onFinished)
    {
        RewardedShown++;
        Debug.Log("[FakeAd] rewarded ad shown, reward earned: " + NextRewardEarned);
        onFinished?.Invoke(NextRewardEarned);
    }

    public void ShowInterstitial(Action onClosed)
    {
        InterstitialsShown++;
        Debug.Log("[FakeAd] interstitial shown");
        onClosed?.Invoke();
    }
}
