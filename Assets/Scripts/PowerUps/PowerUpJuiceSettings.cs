// Assets/Scripts/PowerUps/PowerUpJuiceSettings.cs
using System;
using UnityEngine;

[Serializable]
public struct PowerUpJuicePreset
{
    public float shakeAmplitude;
    public float shakeDuration;
    public float zoomPunchFovDelta;
    public float zoomDuration;
    public float slowdownTimeScale;
    public float slowdownHoldDuration;
    public float slowdownEaseBackDuration;
}

[CreateAssetMenu(fileName = "PowerUpJuiceSettings", menuName = "PowerUps/Juice Settings")]
public class PowerUpJuiceSettings : ScriptableObject
{
    [SerializeField]
    private PowerUpJuicePreset minor = new PowerUpJuicePreset
    {
        shakeAmplitude = 0.15f,
        shakeDuration = 0.1f,
        zoomPunchFovDelta = 3f,
        zoomDuration = 0.08f,
        slowdownTimeScale = 0.6f,
        slowdownHoldDuration = 0.05f,
        slowdownEaseBackDuration = 0.1f
    };
    [SerializeField]
    private PowerUpJuicePreset major = new PowerUpJuicePreset
    {
        shakeAmplitude = 0.3f,
        shakeDuration = 0.15f,
        zoomPunchFovDelta = 6f,
        zoomDuration = 0.12f,
        slowdownTimeScale = 0.45f,
        slowdownHoldDuration = 0.1f,
        slowdownEaseBackDuration = 0.15f
    };
    [SerializeField]
    private PowerUpJuicePreset supreme = new PowerUpJuicePreset
    {
        shakeAmplitude = 0.5f,
        shakeDuration = 0.25f,
        zoomPunchFovDelta = 10f,
        zoomDuration = 0.2f,
        slowdownTimeScale = 0.3f,
        slowdownHoldDuration = 0.2f,
        slowdownEaseBackDuration = 0.25f
    };

    public PowerUpJuicePreset GetPreset(PowerUpImportance tier)
    {
        switch (tier)
        {
            case PowerUpImportance.Major:
                return major;
            case PowerUpImportance.Supreme:
                return supreme;
            default:
                return minor;
        }
    }
}
