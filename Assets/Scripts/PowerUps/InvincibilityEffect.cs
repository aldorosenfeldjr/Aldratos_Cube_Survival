public class InvincibilityEffect : IPowerUpEffect
{
    public void Apply(PowerUpManager manager)
    {
        manager.SetInvincible(true);
    }

    public void Remove(PowerUpManager manager)
    {
        manager.SetInvincible(false);
    }
}
