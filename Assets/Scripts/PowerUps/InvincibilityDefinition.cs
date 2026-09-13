using UnityEngine;

[CreateAssetMenu(fileName = "Invincibility", menuName = "PowerUps/Invincibility")]
public class InvincibilityDefinition : PowerUpDefinition
{
    public override IPowerUpEffect CreateEffect()
    {
        return new InvincibilityEffect();
    }
}
