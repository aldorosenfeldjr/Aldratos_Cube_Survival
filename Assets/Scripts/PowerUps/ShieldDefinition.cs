using UnityEngine;

[CreateAssetMenu(fileName = "Shield", menuName = "PowerUps/Shield")]
public class ShieldDefinition : PowerUpDefinition
{
    public override IPowerUpEffect CreateEffect()
    {
        return new ShieldEffect();
    }
}
