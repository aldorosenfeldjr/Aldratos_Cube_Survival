using System;
using UnityEngine;

/// <summary>
/// Ownership, gem purchases and selection for unlockables. Game code and the selection screen use only this.
/// Trials arrive with the ad flow (economy step 4); until then usable == owned.
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

    public static bool IsUsable(UnlockableDefinition definition)
    {
        return IsOwned(definition);
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
        SaveService.Save();
        Changed?.Invoke();
    }

    /// <summary>Makes a usable item the selection for its category.</summary>
    public static bool Select(UnlockableDefinition definition)
    {
        if (!IsUsable(definition))
        {
            return false;
        }

        var key = definition.Category.ToString();
        var selected = SaveService.Data.selected.Find(entry => entry.category == key);
        if (selected == null)
        {
            selected = new SelectedEntry { category = key };
            SaveService.Data.selected.Add(selected);
        }
        selected.id = definition.Id;
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
}
