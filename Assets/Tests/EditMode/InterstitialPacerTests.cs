using NUnit.Framework;

public class InterstitialPacerTests : SaveTestBase
{
    private FakeAdService ads;

    private static int Every => EconomyConfig.Instance.GameOversPerInterstitial;

    [SetUp]
    public void UseAFakeAdService()
    {
        ads = new FakeAdService();
        Services.Ads = ads;
    }

    private void GameOver(bool trialEnded = false, bool watchedRewarded = false)
    {
        InterstitialPacer.RegisterGameOver(trialEnded);
        if (watchedRewarded)
        {
            InterstitialPacer.MarkRewardedWatched();
        }
        InterstitialPacer.OnLeave(() => { });
    }

    private void GameOversUntilJustBeforeDue()
    {
        for (var i = 0; i < Every - 1; i++)
        {
            GameOver();
        }
    }

    [Test]
    public void NothingIsShownBeforeTheNthGameOver()
    {
        GameOversUntilJustBeforeDue();

        Assert.AreEqual(0, ads.InterstitialsShown);
    }

    [Test]
    public void TheNthGameOverShowsOneOnLeavingAndTheCountRestarts()
    {
        GameOversUntilJustBeforeDue();
        var proceeded = false;

        InterstitialPacer.RegisterGameOver(false);
        InterstitialPacer.OnLeave(() => proceeded = true);

        Assert.AreEqual(1, ads.InterstitialsShown);
        Assert.IsTrue(proceeded);
        Assert.AreEqual(0, SaveService.Data.gameOversSinceInterstitial);
    }

    [Test]
    public void ItNeverShowsTwoInARow()
    {
        GameOversUntilJustBeforeDue();
        GameOver();
        GameOver();

        Assert.AreEqual(1, ads.InterstitialsShown);
    }

    [Test]
    public void AWatchedRewardedAdSkipsItButItStaysDueForTheNextGameOver()
    {
        GameOversUntilJustBeforeDue();
        GameOver(watchedRewarded: true);
        Assert.AreEqual(0, ads.InterstitialsShown);

        GameOver();

        Assert.AreEqual(1, ads.InterstitialsShown);
    }

    [Test]
    public void AGameOverWhereATrialEndedNeverShowsOne()
    {
        GameOversUntilJustBeforeDue();

        GameOver(trialEnded: true);

        Assert.AreEqual(0, ads.InterstitialsShown);
    }

    [Test]
    public void RemoveAdsStopsThemForGood()
    {
        UnlockService.GrantRemoveAds();

        for (var i = 0; i < Every * 3; i++)
        {
            GameOver();
        }

        Assert.AreEqual(0, ads.InterstitialsShown);
    }

    [Test]
    public void WithNoAdServiceLeavingStillProceeds()
    {
        Services.Ads = new NullAdService();
        GameOversUntilJustBeforeDue();
        var proceeded = false;

        InterstitialPacer.RegisterGameOver(false);
        InterstitialPacer.OnLeave(() => proceeded = true);

        Assert.IsTrue(proceeded);
    }

    [Test]
    public void AnAdThatIsNotReadyDoesNotBlockLeavingAndStaysDue()
    {
        ads.InterstitialReady = false;
        GameOversUntilJustBeforeDue();
        var proceeded = false;

        InterstitialPacer.RegisterGameOver(false);
        InterstitialPacer.OnLeave(() => proceeded = true);
        Assert.IsTrue(proceeded);
        Assert.AreEqual(0, ads.InterstitialsShown);

        ads.InterstitialReady = true;
        GameOver();

        Assert.AreEqual(1, ads.InterstitialsShown);
    }

    [Test]
    public void TheCountSurvivesARestartOfTheApp()
    {
        GameOversUntilJustBeforeDue();

        SaveService.Load();

        Assert.AreEqual(Every - 1, SaveService.Data.gameOversSinceInterstitial);
    }
}
