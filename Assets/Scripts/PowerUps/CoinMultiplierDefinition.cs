using UnityEngine;

[CreateAssetMenu(fileName = "CoinMultiplier", menuName = "PowerUps/Coin Multiplier")]
public class CoinMultiplierDefinition : PowerUpDefinition
{
    [SerializeField]
    private int multiplier = 2;

    public override IPowerUpEffect CreateEffect()
    {
        return new CoinMultiplierEffect(multiplier);
    }
}
