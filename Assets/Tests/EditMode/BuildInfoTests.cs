using NUnit.Framework;

public class BuildInfoTests : SaveTestBase
{
    [Test]
    public void TheEditorUsesFakeServices()
    {
        Assert.IsTrue(BuildInfo.UsesTestServices);
        Assert.IsInstanceOf<FakeAdService>(new FakeAdService());
        Assert.IsTrue(Services.Ads.IsAvailable);
        Assert.IsTrue(Services.Store.IsAvailable);
    }

    [Test]
    public void TheEditorNeverGivesStartingCoins()
    {
        Assert.IsFalse(BuildInfo.GivesStartingCoins);
        Assert.AreEqual(0, Wallet.Balance);
    }

    [Test]
    public void AFreshSaveOnADeviceStartsWithTheTestCoinsOnlyWhenTheBuildAllowsIt()
    {
        // In the Editor the rule is off, so a fresh save is empty: this is what keeps every other test deterministic.
        SaveService.Load();
        Assert.AreEqual(BuildInfo.GivesStartingCoins ? SaveService.DevelopmentBuildStartingCoins : 0, Wallet.Balance);
    }
}
