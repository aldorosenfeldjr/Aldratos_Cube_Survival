public class CoinMultiplierEffect : IPowerUpEffect
{
    private readonly int multiplier;

    public CoinMultiplierEffect(int multiplier)
    {
        this.multiplier = multiplier;
    }

    public void Apply(PowerUpManager manager)
    {
        manager.SetCoinMultiplier(multiplier);
    }

    public void Remove(PowerUpManager manager)
    {
        manager.SetCoinMultiplier(1);
    }
}
