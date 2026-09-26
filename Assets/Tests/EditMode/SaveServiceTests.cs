using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SaveServiceTests : SaveTestBase
{
    private const string MeadowId = "Level_Meadow";

    [Test]
    public void FreshSaveIsEmpty()
    {
        Assert.AreEqual(0, SaveService.Data.gems);
        Assert.AreEqual(0, SaveService.Data.owned.Count);
        Assert.AreEqual(0, SaveService.Data.trials.Count);
        Assert.IsFalse(SaveService.Data.removeAds);
        Assert.AreEqual(0, SaveService.BestScore(MeadowId));
        Assert.IsFalse(SaveService.IsCleared(MeadowId));
    }

    [Test]
    public void EverythingSurvivesASaveAndReload()
    {
        var common = Character(PriceTier.Common);
        Wallet.Add(650);
        UnlockService.TryBuyWithGems(common);
        UnlockService.Select(common);
        SaveService.Data.GetOrAddLevel(MeadowId).cleared = true;
        SaveService.SetBestScore(MeadowId, 77);
        SaveService.Data.removeAds = true;
        SaveService.Data.gameOversSinceInterstitial = 4;
        SaveService.Save();

        Reload();

        Assert.AreEqual(50, Wallet.Balance);
        Assert.IsTrue(UnlockService.IsOwned(common));
        Assert.AreEqual(common, UnlockService.Selected(UnlockCategory.Character));
        Assert.IsTrue(SaveService.IsCleared(MeadowId));
        Assert.AreEqual(77, SaveService.BestScore(MeadowId));
        Assert.IsTrue(SaveService.Data.removeAds);
        Assert.AreEqual(4, SaveService.Data.gameOversSinceInterstitial);
    }

    [Test]
    public void TrialsSurviveAReload()
    {
        var rare = Character(PriceTier.Rare);
        UnlockService.StartTrial(rare);

        Reload();

        Assert.AreEqual(EconomyConfig.Instance.TrialRuns, UnlockService.TrialRunsLeft(rare));
        Assert.AreEqual(rare, UnlockService.Selected(UnlockCategory.Character));
    }

    [Test]
    public void SavingLeavesNoTemporaryFileBehind()
    {
        Wallet.Add(5);
        Wallet.Add(5);

        Assert.IsTrue(File.Exists(SavePath));
        Assert.IsFalse(File.Exists(SavePath + ".tmp"));
    }

    [Test]
    public void ACorruptSaveStartsFreshInsteadOfCrashing()
    {
        File.WriteAllText(SavePath, "{ this is not json");
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Save file unreadable"));

        Reload();

        Assert.AreEqual(0, Wallet.Balance);
    }

    [Test]
    public void ASaveWrittenBeforeNewFieldsExistedStillLoads()
    {
        // Version 1 files from before owned/selected/trials were added: only gems and levels.
        File.WriteAllText(SavePath, "{\"version\":1,\"gems\":123,\"levels\":[{\"id\":\"Level_Meadow\",\"cleared\":true,\"bestScore\":9}]}");

        Reload();

        Assert.AreEqual(123, Wallet.Balance);
        Assert.IsTrue(SaveService.IsCleared(MeadowId));
        Assert.IsNotNull(SaveService.Data.owned);
        Assert.IsNotNull(SaveService.Data.trials);
        Assert.IsNotNull(SaveService.Data.selected);
    }

    [Test]
    public void BestScoreOnlyGoesUp()
    {
        Assert.IsTrue(SaveService.SetBestScore(MeadowId, 10));
        Assert.IsFalse(SaveService.SetBestScore(MeadowId, 10));
        Assert.IsFalse(SaveService.SetBestScore(MeadowId, 3));
        Assert.AreEqual(10, SaveService.BestScore(MeadowId));
        Assert.IsTrue(SaveService.SetBestScore(MeadowId, 11));
    }

    [Test]
    public void ClearingBestScoresKeepsGemsAndClears()
    {
        Wallet.Add(40);
        SaveService.Data.GetOrAddLevel(MeadowId).cleared = true;
        SaveService.SetBestScore(MeadowId, 50);

        SaveService.ClearBestScores();

        Assert.AreEqual(0, SaveService.BestScore(MeadowId));
        Assert.AreEqual(40, Wallet.Balance);
        Assert.IsTrue(SaveService.IsCleared(MeadowId));
    }

    [Test]
    public void TheOldGlobalHighScoreMigratesToMeadowWithoutGrantingAClear()
    {
        var hadKey = PlayerPrefs.HasKey(SaveService.LegacyHighScoreKey);
        var oldValue = PlayerPrefs.GetInt(SaveService.LegacyHighScoreKey);
        try
        {
            PlayerPrefs.SetInt(SaveService.LegacyHighScoreKey, 42);

            Reload();

            Assert.AreEqual(42, SaveService.BestScore(MeadowId));
            Assert.IsFalse(SaveService.IsCleared(MeadowId));
            Assert.IsFalse(PlayerPrefs.HasKey(SaveService.LegacyHighScoreKey));
        }
        finally
        {
            if (hadKey)
            {
                PlayerPrefs.SetInt(SaveService.LegacyHighScoreKey, oldValue);
            }
            else
            {
                PlayerPrefs.DeleteKey(SaveService.LegacyHighScoreKey);
            }
            PlayerPrefs.Save();
        }
    }
}
