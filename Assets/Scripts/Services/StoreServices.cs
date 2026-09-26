using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Product ids that are not unlockables. Character/companion product ids come from <see cref="UnlockableDefinition.ProductId"/>.</summary>
public static class StoreProducts
{
    public const string RemoveAds = "remove_ads";
}

/// <summary>
/// Real-money purchases, as game code sees them. All products are non-consumable, so the store is the source of truth:
/// callers grant on success and again from <see cref="Restore"/>. Real adapters (Unity IAP) implement this on mobile.
/// </summary>
public interface IStoreService
{
    /// <summary>False when this platform has no store (PC): the UI hides every purchase button.</summary>
    bool IsAvailable { get; }

    /// <summary>The store's localised price string for a product (never hardcode one), or null while unknown.</summary>
    string LocalizedPrice(string productId);

    void Buy(string productId, Action<bool> onFinished);

    /// <summary>Asks the store for every product the user owns; <paramref name="onFinished"/> gets their product ids.</summary>
    void Restore(Action<IReadOnlyList<string>> onFinished);
}

public class NullStoreService : IStoreService
{
    public bool IsAvailable => false;

    public string LocalizedPrice(string productId)
    {
        return null;
    }

    public void Buy(string productId, Action<bool> onFinished)
    {
        onFinished?.Invoke(false);
    }

    public void Restore(Action<IReadOnlyList<string>> onFinished)
    {
        onFinished?.Invoke(new List<string>());
    }
}

/// <summary>Editor / test stand-in: instant purchase, configurable result, remembers what was "bought".</summary>
public class FakeStoreService : IStoreService
{
    public bool IsAvailable => true;
    public bool NextBuySucceeds { get; set; } = true;
    public List<string> Owned { get; } = new List<string>();

    public string LocalizedPrice(string productId)
    {
        return "$0.99";
    }

    public void Buy(string productId, Action<bool> onFinished)
    {
        Debug.Log("[FakeStore] buy " + productId + ", succeeds: " + NextBuySucceeds);
        if (NextBuySucceeds && !Owned.Contains(productId))
        {
            Owned.Add(productId);
        }
        onFinished?.Invoke(NextBuySucceeds);
    }

    public void Restore(Action<IReadOnlyList<string>> onFinished)
    {
        onFinished?.Invoke(new List<string>(Owned));
    }
}
