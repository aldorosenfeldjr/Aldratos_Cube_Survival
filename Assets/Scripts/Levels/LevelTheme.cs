// Assets/Scripts/Levels/LevelTheme.cs
using UnityEngine;

[CreateAssetMenu(fileName = "LevelTheme", menuName = "Levels/Level Theme")]
public class LevelTheme : ScriptableObject
{
    [SerializeField]
    private string displayName;
    [Tooltip("Seconds to survive to clear the level. Set in LevelBuilder.")]
    [SerializeField]
    private int targetSeconds;
    [Tooltip("Gems for the first clear. Set in LevelBuilder.")]
    [SerializeField]
    private int firstClearReward;
    [Tooltip("Gems for every repeat clear. Set in LevelBuilder.")]
    [SerializeField]
    private int repeatClearReward;
    [SerializeField]
    private GameObject hazardPrefab;
    [SerializeField]
    private GameObject speedBoostPrefab;
    [SerializeField]
    private GameObject invincibilityPrefab;
    [SerializeField]
    private GameObject shieldPrefab;
    [SerializeField]
    private GameObject gemMultiplierPrefab;

    public string DisplayName => displayName;
    public int TargetSeconds => targetSeconds;
    public int FirstClearReward => firstClearReward;
    public int RepeatClearReward => repeatClearReward;
    public GameObject HazardPrefab => hazardPrefab;
    public GameObject SpeedBoostPrefab => speedBoostPrefab;
    public GameObject InvincibilityPrefab => invincibilityPrefab;
    public GameObject ShieldPrefab => shieldPrefab;
    public GameObject GemMultiplierPrefab => gemMultiplierPrefab;
}
