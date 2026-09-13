public class ShieldEffect : IPowerUpEffect
{
    public void Apply(PowerUpManager manager)
    {
        manager.SetShieldActive(true);
    }

    public void Remove(PowerUpManager manager)
    {
        manager.SetShieldActive(false);
    }
}
