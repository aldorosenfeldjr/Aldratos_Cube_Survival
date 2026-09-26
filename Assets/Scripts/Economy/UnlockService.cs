using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Ownership, gem purchases, ad trials and selection for unlockables. Game code and the selection screen use only this.
/// A trial (won by watching a rewarded ad) makes an item usable and selected for a few runs; see <see cref="StartTrial"/>.
/// </summary>
public static class UnlockService
{
    public static event Action Changed;

    /// <summary>Gem price for a tier: the PC price, times the mobile multiplier on mobile, rounded to the nearest 5.</summary>
    public static int PriceFor(PriceTier tier, bool mobile)
    {
        var config = EconomyConfig.Instance;
        var price = config.TierPrice(tier);
        if (mobile)
        {
            price = (int)(Math.Round(price * config.MobilePriceMultiplier / 5.0, MidpointRounding.AwayFromZero) * 5);
        }
        return price;
    }

    public static int Price(UnlockableDefinition definition)
    {
        return PriceFor(definition.Tier, Application.isMobilePlatform);
    }

    public static bool IsOwned(UnlockableDefinition definition)
    {
        return definition.IsDefault || SaveService.Data.owned.Exists(entry => entry.id == definition.Id);
    }

    /// <summary>Owned, or on a running trial.</summary>
    public static bool IsUsable(UnlockableDefinition definition)
    {
        return IsOwned(definition) || TrialRunsLeft(definition) > 0;
    }

    public static int TrialRunsLeft(UnlockableDefinition definition)
    {
        var trial = SaveService.Data.trials.Find(entry => entry.id == definition.Id);
        return trial != null ? trial.runsLeft : 0;
    }

    /// <summary>Spends gems to own the item. False (nothing changes) when already owned or the balance is short.</summary>
    public static bool TryBuyWithGems(UnlockableDefinition definition)
    {
        if (IsOwned(definition))
        {
            return false;
        }

        // Owned entry first, then the spend: the spend saves once, so the gems and the item land in one write.
        var entry = new OwnedEntry { id = definition.Id, source = "gems" };
        SaveService.Data.owned.Add(entry);
        if (!Wallet.TrySpend(Price(definition)))
        {
            SaveService.Data.owned.Remove(entry);
            return false;
        }

        // Owning it ends any trial of the same item; it stays selected if it was.
        SaveService.Data.trials.RemoveAll(trial => trial.id == definition.Id);
        SaveService.Save();
        Changed?.Invoke();
        return true;
    }

    /// <summary>Grants an item bought with real money (or restored). Idempotent.</summary>
    public static void GrantPurchase(UnlockableDefinition definition)
    {
        if (IsOwned(definition))
        {
            return;
        }
        SaveService.Data.owned.Add(new OwnedEntry { id = definition.Id, source = "purchase" });
        SaveService.Data.trials.RemoveAll(trial => trial.id == definition.Id);
        SaveService.Save();
        Changed?.Invoke();
    }

    /// <summary>Grants everything the store reports as owned (idempotent), including Remove Ads.</summary>
    public static void RestorePurchases(IEnumerable<string> productIds)
    {
        var catalog = UnlockCatalog.Instance;
        foreach (var productId in productIds)
        {
            if (productId == StoreProducts.RemoveAds)
            {
                GrantRemoveAds();
                continue;
            }

            var definition = catalog != null ? catalog.Items.FirstOrDefault(item => item != null && item.ProductId == productId) : null;
            if (definition != null)
            {
                GrantPurchase(definition);
            }
        }
    }

    public static bool RemoveAdsOwned => SaveService.Data.removeAds;

    public static void GrantRemoveAds()
    {
        if (SaveService.Data.removeAds)
        {
            return;
        }
        SaveService.Data.removeAds = true;
        SaveService.Save();
        Changed?.Invoke();
    }

    /// <summary>
    /// Starts (or extends) an ad trial: the item becomes usable and selected for <see cref="EconomyConfig.TrialRuns"/> runs.
    /// One trial per category: a new one replaces the old. The previously selected owned item is remembered and comes back when it ends.
    /// </summary>
    public static bool StartTrial(UnlockableDefinition definition)
    {
        if (IsOwned(definition))
        {
            return false;
        }

        var key = definition.Category.ToString();
        var existing = SaveService.Data.trials.Find(entry => entry.category == key);
        var returnTo = existing != null ? existing.returnToId : Selected(definition.Category)?.Id;
        SaveService.Data.trials.RemoveAll(entry => entry.category == key);
        SaveService.Data.trials.Add(new TrialEntry { category = key, id = definition.Id, runsLeft = EconomyConfig.Instance.TrialRuns, returnToId = returnTo });
        SetSelectedId(key, definition.Id);
        SaveService.Save();
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Call once when a run ends. A running trial loses a run only if its item was the selected one. Returns the items whose
    /// trial just ran out (selection has gone back to the last owned item).
    /// </summary>
    public static List<UnlockableDefinition> EndRunForTrials()
    {
        var expired = new List<UnlockableDefinition>();
        var catalog = UnlockCatalog.Instance;
        foreach (var trial in new List<TrialEntry>(SaveService.Data.trials))
        {
            var definition = catalog != null ? catalog.Find(trial.id) : null;
            if (definition == null || Selected(definition.Category) != definition)
            {
                continue;
            }

            trial.runsLeft--;
            if (trial.runsLeft > 0)
            {
                continue;
            }

            SaveService.Data.trials.Remove(trial);
            SetSelectedId(trial.category, trial.returnToId);
            expired.Add(definition);
        }

        SaveService.Save();
        Changed?.Invoke();
        return expired;
    }

    /// <summary>Makes a usable item the selection for its category.</summary>
    public static bool Select(UnlockableDefinition definition)
    {
        if (!IsUsable(definition))
        {
            return false;
        }

        SetSelectedId(definition.Category.ToString(), definition.Id);
        SaveService.Save();
        Changed?.Invoke();
        return true;
    }

    /// <summary>The selected item for a category, or the category's default when nothing valid is saved.</summary>
    public static UnlockableDefinition Selected(UnlockCategory category)
    {
        var catalog = UnlockCatalog.Instance;
        if (catalog == null)
        {
            return null;
        }

        var key = category.ToString();
        var saved = SaveService.Data.selected.Find(entry => entry.category == key);
        var definition = saved != null ? catalog.Find(saved.id) : null;
        return definition != null && IsUsable(definition) ? definition : catalog.DefaultFor(category);
    }

    private static void SetSelectedId(string category, string id)
    {
        var selected = SaveService.Data.selected.Find(entry => entry.category == category);
        if (selected == null)
        {
            selected = new SelectedEntry { category = category };
            SaveService.Data.selected.Add(selected);
        }
        selected.id = id;
    }
}
