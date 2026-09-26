using NUnit.Framework;

public class WalletAndPricingTests : SaveTestBase
{
    [Test]
    public void AddingGemsRaisesTheBalanceAndIgnoresNonPositiveAmounts()
    {
        Wallet.Add(10);
        Wallet.Add(0);
        Wallet.Add(-5);

        Assert.AreEqual(10, Wallet.Balance);
    }

    [Test]
    public void SpendingMoreThanTheBalanceIsRefusedAndChangesNothing()
    {
        Wallet.Add(10);

        Assert.IsFalse(Wallet.TrySpend(11));
        Assert.IsFalse(Wallet.TrySpend(-1));
        Assert.AreEqual(10, Wallet.Balance);
        Assert.IsTrue(Wallet.TrySpend(10));
        Assert.AreEqual(0, Wallet.Balance);
    }

    [Test]
    public void TheChangedEventReportsTheNewBalance()
    {
        var seen = -1;
        void Handler(int balance) => seen = balance;
        Wallet.Changed += Handler;
        try
        {
            Wallet.Add(25);
            Assert.AreEqual(25, seen);
            Wallet.TrySpend(5);
            Assert.AreEqual(20, seen);
        }
        finally
        {
            Wallet.Changed -= Handler;
        }
    }

    [TestCase(PriceTier.Free, 0, 0)]
    [TestCase(PriceTier.Common, 600, 1500)]
    [TestCase(PriceTier.Rare, 1800, 4500)]
    [TestCase(PriceTier.Epic, 3600, 9000)]
    [TestCase(PriceTier.Legendary, 7200, 18000)]
    public void PricesFollowTheTierTableOnPcAndMobile(PriceTier tier, int pc, int mobile)
    {
        Assert.AreEqual(pc, UnlockService.PriceFor(tier, false));
        Assert.AreEqual(mobile, UnlockService.PriceFor(tier, true));
    }

    [Test]
    public void MobilePricesAreAlwaysMultiplesOfFive()
    {
        foreach (PriceTier tier in System.Enum.GetValues(typeof(PriceTier)))
        {
            Assert.AreEqual(0, UnlockService.PriceFor(tier, true) % 5, tier.ToString());
        }
    }
}
