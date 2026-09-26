using System;
using System.IO;
using NUnit.Framework;

/// <summary>
/// Gives every test its own empty save file and fresh fake ad/store services, and never touches the player's real save.
/// The game state under test is static (SaveService, Services), so isolation has to be explicit.
/// </summary>
public abstract class SaveTestBase
{
    protected string SavePath { get; private set; }

    [SetUp]
    public void UseTemporarySave()
    {
        SavePath = Path.Combine(Path.GetTempPath(), "cube_survival_unit_" + Guid.NewGuid().ToString("N") + ".json");
        SaveService.UseFile(SavePath);
        Services.Ads = new FakeAdService();
        Services.Store = new FakeStoreService();
    }

    [TearDown]
    public void DeleteTemporarySave()
    {
        foreach (var path in new[] { SavePath, SavePath + ".tmp" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        SaveService.UseFile(null);
    }

    protected static CharacterDefinition Character(PriceTier tier, bool defaultOnly = false)
    {
        foreach (var item in UnlockCatalog.Instance.InCategory(UnlockCategory.Character))
        {
            if (item.Tier == tier && item.IsDefault == defaultOnly)
            {
                return (CharacterDefinition)item;
            }
        }
        throw new InvalidOperationException("No character of tier " + tier);
    }

    protected static void Reload()
    {
        SaveService.Load();
    }
}
