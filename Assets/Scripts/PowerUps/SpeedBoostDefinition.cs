using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBoost", menuName = "PowerUps/Speed Boost")]
public class SpeedBoostDefinition : PowerUpDefinition
{
    [SerializeField]
    private float multiplier = 1.6f;

    public override IPowerUpEffect CreateEffect()
    {
        return new SpeedBoostEffect(multiplier);
    }
}
