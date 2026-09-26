using UnityEngine;

[CreateAssetMenu(fileName = "GemMultiplier", menuName = "PowerUps/Gem Multiplier")]
public class GemMultiplierDefinition : PowerUpDefinition
{
    [SerializeField]
    private int multiplier = 2;

    public override IPowerUpEffect CreateEffect()
    {
        return new GemMultiplierEffect(multiplier);
    }
}
