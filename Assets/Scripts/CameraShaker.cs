using Unity.Cinemachine;
using UnityEngine;

// Single owner of gameplay camera-shake strength, so every level shakes identically.
// Anything that lands (crates, future hazards) reports an impact force here instead of
// carrying its own CinemachineImpulseSource with its own hand-tuned values.
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    [SerializeField]
    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        Instance = this;
    }

    public void ShakeImpact(float force)
    {
        impulseSource.GenerateImpulseWithForce(force * GameConfig.Instance.ImpactShakeScale);
    }

    public void ShakeDeath()
    {
        impulseSource.GenerateImpulseWithForce(GameConfig.Instance.DeathShakeForce);
    }
}
