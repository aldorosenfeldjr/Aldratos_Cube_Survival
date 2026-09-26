using System.Linq;
using NUnit.Framework;

public class RunStateTests : SaveTestBase
{
    private const string LevelId = "Level_Meadow";

    [Test]
    public void ScoreGoesUpOncePerWholeSecond()
    {
        var run = new RunState();
        run.Reset();

        Assert.IsFalse(run.Tick(0.6f));
        Assert.AreEqual(0, run.Score);
        Assert.IsTrue(run.Tick(0.5f));
        Assert.AreEqual(1, run.Score);
    }

    [Test]
    public void GemsCountTheBaseValueAndTheMultiplierBonusSeparately()
    {
        var run = new RunState();
        run.Reset();

        var earned = run.AddGem(1, 2);

        Assert.AreEqual(2, earned);
        Assert.AreEqual(1, run.GemsCollected);
        Assert.AreEqual(1, run.MultiplierBonus);
    }

    [Test]
    public void ResetClearsTheRunButNotTheBest()
    {
        var run = new RunState();
        run.SetLevel(LevelId);
        run.Reset();
        run.Tick(1f);
        run.Tick(1f);
        run.AddGem(1, 3);
        run.CommitHighScore();

        run.Reset();

        Assert.AreEqual(0, run.Score);
        Assert.AreEqual(0, run.GemsCollected);
        Assert.AreEqual(0, run.MultiplierBonus);
        Assert.AreEqual(2, run.HighScore);
    }

    [Test]
    public void OnlyABetterScoreBecomesTheLevelsBestAndItIsSaved()
    {
        var run = new RunState();
        run.SetLevel(LevelId);
        run.Reset();
        run.Tick(1f);

        Assert.IsTrue(run.CommitHighScore());
        Assert.AreEqual(1, SaveService.BestScore(LevelId));

        run.Reset();
        Assert.IsFalse(run.CommitHighScore());
    }

    [Test]
    public void BestScoresAreKeptPerLevel()
    {
        var run = new RunState();
        run.SetLevel("Level_Meadow");
        run.Reset();
        run.Tick(1f);
        run.CommitHighScore();

        run.SetLevel("Level_Playground");

        Assert.AreEqual(0, run.HighScore);
    }
}

/// <summary>Data integrity of the shipped unlock catalog: the things a hand-edited table could silently break.</summary>
public class CatalogDataTests
{
    private static UnlockCatalog Catalog => UnlockCatalog.Instance;

    [Test]
    public void EveryIdAndProductIdIsUnique()
    {
        var items = Catalog.Items;

        Assert.AreEqual(items.Count, items.Select(item => item.Id).Distinct().Count());
        Assert.AreEqual(items.Count, items.Select(item => item.ProductId).Distinct().Count());
    }

    [Test]
    public void EveryCategoryHasExactlyOneFreeDefault()
    {
        foreach (UnlockCategory category in System.Enum.GetValues(typeof(UnlockCategory)))
        {
            var defaults = Catalog.InCategory(category).Where(item => item.IsDefault).ToList();

            Assert.AreEqual(1, defaults.Count, category.ToString());
            Assert.AreEqual(PriceTier.Free, defaults[0].Tier, category.ToString());
        }
    }

    [Test]
    public void OnlyDefaultsAreFree()
    {
        foreach (var item in Catalog.Items)
        {
            Assert.AreEqual(item.IsDefault, item.Tier == PriceTier.Free, item.Id);
        }
    }

    [Test]
    public void IdsCarryTheirCategoryPrefixAndNoSpaces()
    {
        foreach (var item in Catalog.Items)
        {
            var prefix = item.Category == UnlockCategory.Character ? "char." : "comp.";
            StringAssert.StartsWith(prefix, item.Id);
            Assert.IsFalse(item.Id.Contains(" "), item.Id);
        }
    }

    [Test]
    public void EveryCharacterHasAMaterialAndEveryCompanionAPrefabAndASize()
    {
        foreach (var item in Catalog.Items)
        {
            if (item is CharacterDefinition character)
            {
                Assert.IsNotNull(character.Material, item.Id);
            }
            else if (item is CompanionDefinition companion)
            {
                Assert.IsNotNull(companion.Prefab, item.Id);
                Assert.Greater(companion.WorldSize, 0.1f, item.Id);
            }
        }
    }

    [Test]
    public void TheTierPricesIncreaseWithTheTier()
    {
        var config = EconomyConfig.Instance;

        Assert.Less(config.TierPrice(PriceTier.Free), config.TierPrice(PriceTier.Common));
        Assert.Less(config.TierPrice(PriceTier.Common), config.TierPrice(PriceTier.Rare));
        Assert.Less(config.TierPrice(PriceTier.Rare), config.TierPrice(PriceTier.Epic));
        Assert.Less(config.TierPrice(PriceTier.Epic), config.TierPrice(PriceTier.Legendary));
    }
}
