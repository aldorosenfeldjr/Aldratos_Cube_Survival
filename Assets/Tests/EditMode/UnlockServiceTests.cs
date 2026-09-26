using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class UnlockServiceTests : SaveTestBase
{
    private static UnlockCategory Category => UnlockCategory.Character;

    [Test]
    public void TheDefaultIsOwnedAndSelectedAndEverythingElseIsLocked()
    {
        var free = Character(PriceTier.Free, defaultOnly: true);

        Assert.IsTrue(UnlockService.IsOwned(free));
        Assert.AreEqual(free, UnlockService.Selected(Category));
        Assert.IsFalse(UnlockService.IsOwned(Character(PriceTier.Common)));
    }

    [Test]
    public void BuyingSpendsTheExactPriceAndRecordsTheSource()
    {
        var common = Character(PriceTier.Common);
        var price = UnlockService.Price(common);
        Wallet.Add(price + 7);

        Assert.IsTrue(UnlockService.TryBuyWithGems(common));

        Assert.AreEqual(7, Wallet.Balance);
        Assert.IsTrue(UnlockService.IsOwned(common));
        Assert.AreEqual("gems", SaveService.Data.owned.Single().source);
    }

    [Test]
    public void BuyingWithTooFewGemsChangesNothing()
    {
        var common = Character(PriceTier.Common);
        Wallet.Add(UnlockService.Price(common) - 1);

        Assert.IsFalse(UnlockService.TryBuyWithGems(common));

        Assert.IsFalse(UnlockService.IsOwned(common));
        Assert.AreEqual(UnlockService.Price(common) - 1, Wallet.Balance);
        Assert.AreEqual(0, SaveService.Data.owned.Count);
    }

    [Test]
    public void BuyingSomethingOwnedIsRefusedAndFree()
    {
        var common = Character(PriceTier.Common);
        Wallet.Add(UnlockService.Price(common) * 2);
        UnlockService.TryBuyWithGems(common);
        var balance = Wallet.Balance;

        Assert.IsFalse(UnlockService.TryBuyWithGems(common));

        Assert.AreEqual(balance, Wallet.Balance);
        Assert.AreEqual(1, SaveService.Data.owned.Count);
    }

    [Test]
    public void OnlyUsableItemsCanBeSelected()
    {
        var common = Character(PriceTier.Common);
        Assert.IsFalse(UnlockService.Select(common));

        UnlockService.GrantPurchase(common);

        Assert.IsTrue(UnlockService.Select(common));
        Assert.AreEqual(common, UnlockService.Selected(Category));
    }

    [Test]
    public void GrantingAPurchaseTwiceGrantsOnce()
    {
        var rare = Character(PriceTier.Rare);

        UnlockService.GrantPurchase(rare);
        UnlockService.GrantPurchase(rare);

        Assert.AreEqual(1, SaveService.Data.owned.Count);
        Assert.AreEqual("purchase", SaveService.Data.owned[0].source);
    }

    [Test]
    public void ASelectedItemThatNoLongerExistsFallsBackToTheDefault()
    {
        SaveService.Data.selected.Add(new SelectedEntry { category = Category.ToString(), id = "char.was-deleted" });

        Assert.AreEqual(Character(PriceTier.Free, defaultOnly: true), UnlockService.Selected(Category));
    }

    [Test]
    public void CharactersAndCompanionsAreSelectedIndependently()
    {
        var common = Character(PriceTier.Common);
        UnlockService.GrantPurchase(common);
        UnlockService.Select(common);

        var companionDefault = UnlockCatalog.Instance.DefaultFor(UnlockCategory.Companion);
        Assert.AreEqual(companionDefault, UnlockService.Selected(UnlockCategory.Companion));
        Assert.AreEqual(common, UnlockService.Selected(UnlockCategory.Character));
    }

    // --- Trials ---

    [Test]
    public void ATrialMakesALockedItemUsableAndSelectedButNotOwned()
    {
        var rare = Character(PriceTier.Rare);

        Assert.IsTrue(UnlockService.StartTrial(rare));

        Assert.IsFalse(UnlockService.IsOwned(rare));
        Assert.IsTrue(UnlockService.IsUsable(rare));
        Assert.AreEqual(rare, UnlockService.Selected(Category));
        Assert.AreEqual(EconomyConfig.Instance.TrialRuns, UnlockService.TrialRunsLeft(rare));
    }

    [Test]
    public void ATrialCannotStartOnSomethingOwned()
    {
        Assert.IsFalse(UnlockService.StartTrial(Character(PriceTier.Free, defaultOnly: true)));
    }

    [Test]
    public void ATrialEndsAfterItsRunsAndSelectionGoesBackToTheLastOwnedItem()
    {
        var owned = Character(PriceTier.Common);
        UnlockService.GrantPurchase(owned);
        UnlockService.Select(owned);
        var rare = Character(PriceTier.Rare);
        UnlockService.StartTrial(rare);

        var expired = new List<UnlockableDefinition>();
        for (var run = 0; run < EconomyConfig.Instance.TrialRuns; run++)
        {
            expired.AddRange(UnlockService.EndRunForTrials());
        }

        CollectionAssert.AreEqual(new[] { rare }, expired);
        Assert.IsFalse(UnlockService.IsUsable(rare));
        Assert.AreEqual(owned, UnlockService.Selected(Category));
        Assert.AreEqual(0, SaveService.Data.trials.Count);
    }

    [Test]
    public void ARunWithAnotherItemSelectedDoesNotUseUpTheTrial()
    {
        var rare = Character(PriceTier.Rare);
        UnlockService.StartTrial(rare);
        UnlockService.Select(Character(PriceTier.Free, defaultOnly: true));

        UnlockService.EndRunForTrials();

        Assert.AreEqual(EconomyConfig.Instance.TrialRuns, UnlockService.TrialRunsLeft(rare));
    }

    [Test]
    public void OnlyOneTrialRunsPerCategoryAndANewOneReplacesTheOld()
    {
        var first = Character(PriceTier.Rare);
        var second = Character(PriceTier.Epic);
        UnlockService.StartTrial(first);
        UnlockService.StartTrial(second);

        Assert.AreEqual(1, SaveService.Data.trials.Count);
        Assert.IsFalse(UnlockService.IsUsable(first));
        Assert.IsTrue(UnlockService.IsUsable(second));
    }

    [Test]
    public void ReplacingATrialRemembersTheOriginalOwnedItemNotTheFirstTrial()
    {
        var owned = Character(PriceTier.Free, defaultOnly: true);
        UnlockService.StartTrial(Character(PriceTier.Rare));
        var second = Character(PriceTier.Epic);
        UnlockService.StartTrial(second);

        for (var run = 0; run < EconomyConfig.Instance.TrialRuns; run++)
        {
            UnlockService.EndRunForTrials();
        }

        Assert.AreEqual(owned, UnlockService.Selected(Category));
    }

    [Test]
    public void OwningAnItemEndsItsTrialAndKeepsItSelected()
    {
        var rare = Character(PriceTier.Rare);
        UnlockService.StartTrial(rare);
        Wallet.Add(UnlockService.Price(rare));

        UnlockService.TryBuyWithGems(rare);

        Assert.AreEqual(0, SaveService.Data.trials.Count);
        Assert.IsTrue(UnlockService.IsOwned(rare));
        Assert.AreEqual(rare, UnlockService.Selected(Category));
    }

    // --- Restore purchases ---

    [Test]
    public void RestoringGrantsOwnedProductsAndRemoveAdsAndIsIdempotent()
    {
        var common = Character(PriceTier.Common);
        var products = new[] { common.ProductId, StoreProducts.RemoveAds, "unknown_product" };

        UnlockService.RestorePurchases(products);
        UnlockService.RestorePurchases(products);

        Assert.IsTrue(UnlockService.IsOwned(common));
        Assert.IsTrue(UnlockService.RemoveAdsOwned);
        Assert.AreEqual(1, SaveService.Data.owned.Count);
    }
}
