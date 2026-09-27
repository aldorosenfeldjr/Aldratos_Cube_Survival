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
    public void TheEditorNeverGivesStartingGems()
    {
        Assert.IsFalse(BuildInfo.GivesStartingGems);
        Assert.AreEqual(0, Wallet.Balance);
    }

    [Test]
    public void AFreshSaveOnADeviceStartsWithTheTestGemsOnlyWhenTheBuildAllowsIt()
    {
        // In the Editor the rule is off, so a fresh save is empty: this is what keeps every other test deterministic.
        SaveService.Load();
        Assert.AreEqual(BuildInfo.GivesStartingGems ? SaveService.DevelopmentBuildStartingGems : 0, Wallet.Balance);
    }
}
