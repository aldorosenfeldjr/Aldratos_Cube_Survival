using UnityEngine;

/// <summary>
/// Single source of truth for gameplay tunables. One asset (Assets/Resources/GameConfig.asset),
/// read via <see cref="Instance"/>: scripts hold no copies, prefabs and levels do not override.
/// Per-level differences belong in <see cref="LevelTheme"/>; power-up camera juice in PowerUpJuiceSettings.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")]
public class GameConfig : ScriptableObject
{
    private static GameConfig instance;

    public static GameConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GameConfig>("GameConfig");
            }
            return instance;
        }
    }

    [Header("Player")]
    [SerializeField] private float moveForce = 1600f;
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [Tooltip("How fast keyboard steering ramps between -1 and 1, per second (the old Input Manager 'Horizontal' axis used 3).")]
    [SerializeField] private float keyboardSteerRamp = 3f;

    [Header("Spawn area (hazards and pickups)")]
    [SerializeField] private float spawnMinX = -7f;
    [SerializeField] private float spawnMaxX = 7f;
    [SerializeField] private float spawnHeight = 11f;

    [Header("Hazards")]
    [SerializeField] private int maxHazardsPerWave = 3;
    [SerializeField] private float hazardWaveInterval = 1f;
    [SerializeField] private float hazardMinDrag = 2f;
    [SerializeField] private float hazardMaxDrag = 0.5f;

    [Header("Power-up pickups")]
    [SerializeField] private float pickupMinInterval = 4f;
    [SerializeField] private float pickupMaxInterval = 6f;
    [SerializeField] private float pickupMinDrag = 2f;
    [SerializeField] private float pickupMaxDrag = 0.5f;

    [Header("Camera shake")]
    [Tooltip("Scales every impact force. 1 = raw force; ~0.06 gives a subtle crate-landing shake.")]
    [SerializeField] private float impactShakeScale = 0.06f;
    [Tooltip("Force of the shake when the player dies. Not scaled by impactShakeScale.")]
    [SerializeField] private float deathShakeForce = 0.6f;

    [Header("Time")]
    [SerializeField] private float pauseFadeDuration = 0.5f;

    public float MoveForce => moveForce;
    public float MaxSpeed => maxSpeed;
    public float JumpForce => jumpForce;
    public float FallGravityMultiplier => fallGravityMultiplier;
    public float KeyboardSteerRamp => keyboardSteerRamp;
    public float SpawnMinX => spawnMinX;
    public float SpawnMaxX => spawnMaxX;
    public float SpawnHeight => spawnHeight;
    public int MaxHazardsPerWave => maxHazardsPerWave;
    public float HazardWaveInterval => hazardWaveInterval;
    public float HazardMinDrag => hazardMinDrag;
    public float HazardMaxDrag => hazardMaxDrag;
    public float PickupMinInterval => pickupMinInterval;
    public float PickupMaxInterval => pickupMaxInterval;
    public float PickupMinDrag => pickupMinDrag;
    public float PickupMaxDrag => pickupMaxDrag;
    public float ImpactShakeScale => impactShakeScale;
    public float DeathShakeForce => deathShakeForce;
    public float PauseFadeDuration => pauseFadeDuration;
}
