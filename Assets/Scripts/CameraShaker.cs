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
    [SerializeField]
    [Tooltip("Scales every impact force. 1 = raw force; ~0.06 gives a subtle crate-landing shake.")]
    private float impactShakeScale = 0.06f;

    private void Awake()
    {
        Instance = this;
    }

    public void ShakeImpact(float force)
    {
        impulseSource.GenerateImpulseWithForce(force * impactShakeScale);
    }
}
