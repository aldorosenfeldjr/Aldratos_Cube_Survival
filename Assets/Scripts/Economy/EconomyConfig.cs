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
    public int TrialRuns => trialRuns;
    public int GameOversPerInterstitial => gameOversPerInterstitial;
}
