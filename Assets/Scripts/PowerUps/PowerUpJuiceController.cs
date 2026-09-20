// Assets/Scripts/PowerUps/PowerUpJuiceController.cs
using Unity.Cinemachine;
using UnityEngine;

public class PowerUpJuiceController : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera mainVCam;
    [SerializeField]
    private CinemachineImpulseSource impulseSource;
    [SerializeField]
    private PowerUpJuiceSettings juiceSettings;

    private float baseFieldOfView;
    private bool hasCapturedBaseFov;

    private void OnEnable()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpGranted += HandleGranted;
        }
    }

    private void OnDisable()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpGranted -= HandleGranted;
        }
    }

    private void HandleGranted(PowerUpDefinition definition, float duration)
    {
        if (!hasCapturedBaseFov)
        {
            baseFieldOfView = mainVCam.Lens.FieldOfView;
            hasCapturedBaseFov = true;
        }

        var preset = juiceSettings.GetPreset(definition.Importance);

        LeanTween.cancel(gameObject);

        impulseSource.GenerateImpulseWithForce(preset.shakeAmplitude);

        var targetFov = baseFieldOfView - preset.zoomPunchFovDelta;
        LeanTween.value(gameObject, baseFieldOfView, targetFov, preset.zoomDuration)
            .setOnUpdate(SetFieldOfView)
            .setEase(LeanTweenType.easeOutQuad)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                LeanTween.value(gameObject, targetFov, baseFieldOfView, preset.zoomDuration)
                    .setOnUpdate(SetFieldOfView)
                    .setEase(LeanTweenType.easeInQuad)
                    .setIgnoreTimeScale(true);
            });

        LeanTween.value(gameObject, Time.timeScale, preset.slowdownTimeScale, 0.02f)
            .setOnUpdate(SetTimeScale)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                LeanTween.value(gameObject, preset.slowdownTimeScale, 1f, preset.slowdownEaseBackDuration)
                    .setDelay(preset.slowdownHoldDuration)
                    .setOnUpdate(SetTimeScale)
                    .setIgnoreTimeScale(true);
            });
    }

    private void SetFieldOfView(float value)
    {
        var lens = mainVCam.Lens;
        lens.FieldOfView = value;
        mainVCam.Lens = lens;
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
        Time.fixedDeltaTime = 0.02f * value;
    }

    public void ForceResetTimeScale()
    {
        LeanTween.cancel(gameObject);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (hasCapturedBaseFov)
        {
            SetFieldOfView(baseFieldOfView);
        }
    }
}
