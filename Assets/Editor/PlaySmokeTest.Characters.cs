using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Character-set checks for the play smoke test (roadmap item 2): the catalog matches the plan, and the player
// really wears the selected (or on-trial) character.
public static partial class PlaySmokeTest
{
    private static void CharacterChecks()
    {
        var catalog = UnlockCatalog.Instance;
        var characters = catalog.InCategory(UnlockCategory.Character);
        var tiers = characters.GroupBy(item => item.Tier).ToDictionary(group => group.Key, group => group.Count());
        int Count(PriceTier tier) => tiers.TryGetValue(tier, out var count) ? count : 0;

        Check("12 characters: 1 free default, 4 Common, 4 Rare, 3 Epic",
            characters.Count == 12 && characters.Count(item => item.IsDefault) == 1 && Count(PriceTier.Free) == 1
            && Count(PriceTier.Common) == 4 && Count(PriceTier.Rare) == 4 && Count(PriceTier.Epic) == 3,
            $"total={characters.Count} free={Count(PriceTier.Free)} common={Count(PriceTier.Common)} rare={Count(PriceTier.Rare)} epic={Count(PriceTier.Epic)}");

        var pcTotal = characters.Sum(item => UnlockService.PriceFor(item.Tier, false));
        var mobileTotal = characters.Sum(item => UnlockService.PriceFor(item.Tier, true));
        Check("everything unlockable costs 20,400 gems on PC and 51,000 on mobile", pcTotal == 20400 && mobileTotal == 51000, $"pc={pcTotal} mobile={mobileTotal}");

        var ids = characters.Select(item => item.Id).Distinct().Count();
        var products = characters.Select(item => item.ProductId).Distinct().Count();
        var names = characters.Select(item => item.DisplayName).Distinct().Count();
        var colours = characters.Select(item => item.TileColor).Distinct().Count();
        Check("ids, store product ids, names and colours are all unique and every character has a material",
            ids == 12 && products == 12 && names == 12 && colours == 12 && characters.All(item => ((CharacterDefinition)item).Material != null),
            $"ids={ids} products={products} names={names} colours={colours}");

        // The player wears what is selected: an owned colour, a trial colour, and the default again when the trial ends.
        UseFreshTempSave();
        var player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        var renderer = player.GetComponent<Renderer>();
        var wasActive = player.gameObject.activeSelf;
        player.gameObject.SetActive(true); // an inactive player has not run Awake yet; no frame passes, so nothing else touches it
        var free = (CharacterDefinition)catalog.DefaultFor(UnlockCategory.Character);
        var owned = (CharacterDefinition)characters.First(item => item.Tier == PriceTier.Common);
        var trial = (CharacterDefinition)characters.First(item => item.Tier == PriceTier.Rare);

        player.ResetState();
        var startsAsDefault = renderer.sharedMaterial == free.Material;

        Wallet.Add(UnlockService.Price(owned));
        UnlockService.TryBuyWithGems(owned);
        UnlockService.Select(owned);
        player.ResetState();
        var wearsOwned = renderer.sharedMaterial == owned.Material;

        UnlockService.StartTrial(trial);
        player.ResetState();
        var wearsTrial = renderer.sharedMaterial == trial.Material;

        for (var run = 0; run < EconomyConfig.Instance.TrialRuns; run++)
        {
            UnlockService.EndRunForTrials();
        }
        player.ResetState();
        var backToOwned = renderer.sharedMaterial == owned.Material;

        Check("the player wears the default, the selected colour, a trial colour, then the last owned colour when the trial ends",
            startsAsDefault && wearsOwned && wearsTrial && backToOwned,
            $"default={startsAsDefault} owned={wearsOwned} trial={wearsTrial} back={backToOwned}");

        UseFreshTempSave();
        player.ResetState();
        player.gameObject.SetActive(wasActive);
    }
}
