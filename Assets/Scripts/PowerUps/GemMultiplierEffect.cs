public class GemMultiplierEffect : IPowerUpEffect
{
    private readonly int multiplier;

    public GemMultiplierEffect(int multiplier)
    {
        this.multiplier = multiplier;
    }

    public void Apply(PowerUpManager manager)
    {
        manager.SetGemMultiplier(multiplier);
    }

    public void Remove(PowerUpManager manager)
    {
        manager.SetGemMultiplier(1);
    }
}
