public class SpeedBoostEffect : IPowerUpEffect
{
    private readonly float multiplier;

    public SpeedBoostEffect(float multiplier)
    {
        this.multiplier = multiplier;
    }

    public void Apply(PowerUpManager manager)
    {
        manager.SetSpeedMultiplier(multiplier);
    }

    public void Remove(PowerUpManager manager)
    {
        manager.SetSpeedMultiplier(1f);
    }
}
