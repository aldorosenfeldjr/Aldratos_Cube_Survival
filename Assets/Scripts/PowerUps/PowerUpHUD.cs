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
    [SerializeField]
    private PowerUpCollectFX collectFX;

    private readonly Dictionary<PowerUpDefinition, PowerUpHUDIcon> activeIcons = new Dictionary<PowerUpDefinition, PowerUpHUDIcon>();

    // Tracks grants whose fly-in animation hasn't landed yet. While a definition has a
    // pending entry, a re-grant of the same definition should update the pending duration
    // rather than call Initialize on the not-yet-visible icon (see HandleGranted re-grant
    // branch and the onArrived callback below).
    private class PendingGrant
    {
        public PowerUpHUDIcon Icon;
        public float Duration;
    }

    private readonly Dictionary<PowerUpDefinition, PendingGrant> pendingGrants = new Dictionary<PowerUpDefinition, PendingGrant>();

    private void OnEnable()
    {
        powerUpManager.OnPowerUpGranted += HandleGranted;
        powerUpManager.OnPowerUpExpired += HandleExpired;
    }

    private void OnDisable()
    {
        powerUpManager.OnPowerUpGranted -= HandleGranted;
        powerUpManager.OnPowerUpExpired -= HandleExpired;

        foreach (var icon in activeIcons.Values)
        {
            Destroy(icon.gameObject);
        }
        activeIcons.Clear();
        pendingGrants.Clear();
    }

    private void HandleGranted(PowerUpDefinition definition, float duration)
    {
        if (activeIcons.TryGetValue(definition, out var existingIcon))
        {
            if (pendingGrants.TryGetValue(definition, out var pending) && pending.Icon == existingIcon)
            {
                // Icon is still mid fly-in; just update the duration the onArrived
                // callback will use once it lands. Calling Initialize now would be
                // pointless (icon isn't visible yet) and would be overwritten anyway.
                pending.Duration = duration;
                return;
            }

            existingIcon.Initialize(definition.DisplayName, definition.Icon, duration);
            collectFX.PlayRefreshPulse((RectTransform)existingIcon.transform);
            return;
        }

        var icon = Instantiate(iconPrefab, container);
        icon.gameObject.SetActive(false);
        var pendingGrant = new PendingGrant { Icon = icon, Duration = duration };
        pendingGrants[definition] = pendingGrant;

        collectFX.PlayNewGrant(definition, (RectTransform)container, () =>
        {
            // Icon may have been destroyed (expired mid-flight) before landing.
            if (icon == null)
            {
                return;
            }

            // Icon may have been removed from tracking (e.g. expired and re-granted
            // as a fresh instance) between kickoff and arrival.
            if (!pendingGrants.TryGetValue(definition, out var current) || current != pendingGrant)
            {
                return;
            }

            icon.gameObject.SetActive(true);
            icon.Initialize(definition.DisplayName, definition.Icon, pendingGrant.Duration);
            pendingGrants.Remove(definition);
        });
        activeIcons[definition] = icon;
    }

    private void HandleExpired(PowerUpDefinition definition)
    {
        if (activeIcons.TryGetValue(definition, out var icon))
        {
            Destroy(icon.gameObject);
            activeIcons.Remove(definition);
        }
        pendingGrants.Remove(definition);
    }
}
