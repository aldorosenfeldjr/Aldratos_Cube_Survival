// Assets/Scripts/PowerUps/PowerUpManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    private static PowerUpManager instance;
    public static PowerUpManager Instance => instance;

    public float SpeedMultiplier { get; private set; } = 1f;
    public bool IsInvincible { get; private set; }
    public bool HasShield { get; private set; }

    public event Action<PowerUpDefinition, float> OnPowerUpGranted;
    public event Action<PowerUpDefinition> OnPowerUpExpired;

    protected class ActivePowerUp
    {
        public PowerUpDefinition Definition;
        public IPowerUpEffect Effect;
        public float RemainingTime;
    }

    protected readonly List<ActivePowerUp> activePowerUps = new List<ActivePowerUp>();

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        for (int i = activePowerUps.Count - 1; i >= 0; i--)
        {
            var active = activePowerUps[i];
            if (active.Definition.Duration <= 0f)
            {
                continue; // consumed-on-use types (e.g. Shield) don't tick down
            }

            active.RemainingTime -= Time.deltaTime;
            if (active.RemainingTime <= 0f)
            {
                active.Effect.Remove(this);
                activePowerUps.RemoveAt(i);
                OnPowerUpExpired?.Invoke(active.Definition);
            }
        }
    }

    public void Grant(PowerUpDefinition definition)
    {
        var existing = activePowerUps.Find(p => p.Definition.GetType() == definition.GetType());
        if (existing != null)
        {
            existing.RemainingTime = definition.Duration;
            OnPowerUpGranted?.Invoke(definition, definition.Duration);
            return;
        }

        var effect = definition.CreateEffect();
        effect.Apply(this);
        activePowerUps.Add(new ActivePowerUp
        {
            Definition = definition,
            Effect = effect,
            RemainingTime = definition.Duration
        });
        OnPowerUpGranted?.Invoke(definition, definition.Duration);
    }

    public bool TryConsumeShield()
    {
        if (!HasShield)
        {
            return false;
        }

        var active = activePowerUps.Find(p => p.Effect is ShieldEffect);
        if (active != null)
        {
            active.Effect.Remove(this);
            activePowerUps.Remove(active);
            OnPowerUpExpired?.Invoke(active.Definition);
        }

        return true;
    }

    public void ResetAll()
    {
        foreach (var active in activePowerUps)
        {
            active.Effect.Remove(this);
            OnPowerUpExpired?.Invoke(active.Definition);
        }
        activePowerUps.Clear();
    }

    internal void SetSpeedMultiplier(float value) => SpeedMultiplier = value;
    internal void SetInvincible(bool value) => IsInvincible = value;
    internal void SetShieldActive(bool value) => HasShield = value;
}
