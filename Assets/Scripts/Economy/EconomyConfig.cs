using UnityEngine;

/// <summary>
/// Single source of truth for economy tunables (Assets/Resources/EconomyConfig.asset), read via
/// <see cref="Instance"/>. Per-level clear targets and rewards live on <see cref="LevelTheme"/> instead.
/// </summary>
[CreateAssetMenu(fileName = "EconomyConfig", menuName = "Game/Economy Config")]
public class EconomyConfig : ScriptableObject
{
    private static EconomyConfig instance;

    public static EconomyConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<EconomyConfig>("EconomyConfig");
            }
            return instance;
        }
    }

    [Header("Gems")]
    [Tooltip("Gems one falling gem pickup is worth.")]
    [SerializeField] private int gemValue = 1;
    [Tooltip("Every gem price is multiplied by this on mobile, then rounded to the nearest 5.")]
    [SerializeField] private float mobilePriceMultiplier = 2.5f;

    [Header("Price tiers (PC gems)")]
    [SerializeField] private int commonPrice = 600;
    [SerializeField] private int rarePrice = 1800;
    [SerializeField] private int epicPrice = 3600;
    [SerializeField] private int legendaryPrice = 7200;

    [Header("Ads and trials")]
    [SerializeField] private int trialRuns = 3;
    [SerializeField] private int gameOversPerInterstitial = 10;

    public int GemValue => gemValue;
    public float MobilePriceMultiplier => mobilePriceMultiplier;
    public int CommonPrice => commonPrice;
    public int RarePrice => rarePrice;
    public int EpicPrice => epicPrice;
    public int LegendaryPrice => legendaryPrice;
    /// <summary>PC gem price of a tier. Use <see cref="UnlockService.PriceFor"/> for the platform price.</summary>
    public int TierPrice(PriceTier tier)
    {
        switch (tier)
        {
            case PriceTier.Common: return commonPrice;
            case PriceTier.Rare: return rarePrice;
            case PriceTier.Epic: return epicPrice;
            case PriceTier.Legendary: return legendaryPrice;
            default: return 0;
        }
    }

    public int TrialRuns => trialRuns;
    public int GameOversPerInterstitial => gameOversPerInterstitial;
}
