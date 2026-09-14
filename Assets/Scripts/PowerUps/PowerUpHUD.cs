// Assets/Scripts/PowerUps/PowerUpHUD.cs
using System.Collections.Generic;
using UnityEngine;

public class PowerUpHUD : MonoBehaviour
{
    [SerializeField]
    private PowerUpManager powerUpManager;
    [SerializeField]
    private PowerUpHUDIcon iconPrefab;
    [SerializeField]
    private Transform container;

    private readonly Dictionary<PowerUpDefinition, PowerUpHUDIcon> activeIcons = new Dictionary<PowerUpDefinition, PowerUpHUDIcon>();

    private void OnEnable()
    {
        powerUpManager.OnPowerUpGranted += HandleGranted;
        powerUpManager.OnPowerUpExpired += HandleExpired;
    }

    private void OnDisable()
    {
        powerUpManager.OnPowerUpGranted -= HandleGranted;
        powerUpManager.OnPowerUpExpired -= HandleExpired;
    }

    private void HandleGranted(PowerUpDefinition definition, float duration)
    {
        if (activeIcons.TryGetValue(definition, out var existingIcon))
        {
            existingIcon.Initialize(definition.Icon, duration);
            return;
        }

        var icon = Instantiate(iconPrefab, container);
        icon.Initialize(definition.Icon, duration);
        activeIcons[definition] = icon;
    }

    private void HandleExpired(PowerUpDefinition definition)
    {
        if (activeIcons.TryGetValue(definition, out var icon))
        {
            Destroy(icon.gameObject);
            activeIcons.Remove(definition);
        }
    }
}
