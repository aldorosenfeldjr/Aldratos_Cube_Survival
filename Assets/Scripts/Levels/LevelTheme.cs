// Assets/Scripts/Levels/LevelTheme.cs
using UnityEngine;

[CreateAssetMenu(fileName = "LevelTheme", menuName = "Levels/Level Theme")]
public class LevelTheme : ScriptableObject
{
    [SerializeField]
    private string displayName;
    [SerializeField]
    private GameObject hazardPrefab;
    [SerializeField]
    private GameObject speedBoostPrefab;
    [SerializeField]
    private GameObject invincibilityPrefab;
    [SerializeField]
    private GameObject shieldPrefab;

    public string DisplayName => displayName;
    public GameObject HazardPrefab => hazardPrefab;
    public GameObject SpeedBoostPrefab => speedBoostPrefab;
    public GameObject InvincibilityPrefab => invincibilityPrefab;
    public GameObject ShieldPrefab => shieldPrefab;
}
