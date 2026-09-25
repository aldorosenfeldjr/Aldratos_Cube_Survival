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
    private bool subscribed;
    private bool loggedMissingManagerWarning;

    private void OnEnable()
    {
        TrySubscribe(logIfMissing: true);
    }

    private void Update()
    {
        // PowerUpManager.Instance may not be set yet if Awake/OnEnable ordering
        // put this object first; keep retrying until we successfully subscribe.
        if (!subscribed)
        {
            TrySubscribe(logIfMissing: false);
        }
    }

    private void TrySubscribe(bool logIfMissing)
    {
        if (subscribed)
        {
            return;
        }

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpGranted += HandleGranted;
            subscribed = true;
            loggedMissingManagerWarning = false;
        }
        else if (logIfMissing && !loggedMissingManagerWarning)
        {
            Debug.LogWarning(
                "[PowerUpJuiceController] PowerUpManager.Instance was null on enable; will retry each frame until it becomes available.",
                this);
            loggedMissingManagerWarning = true;
        }
    }

    private void OnDisable()
    {
        if (subscribed && PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpGranted -= HandleGranted;
        }
        subscribed = false;
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

        var currentFov = mainVCam.Lens.FieldOfView;
        var targetFov = baseFieldOfView - preset.zoomPunchFovDelta;
        LeanTween.value(gameObject, currentFov, targetFov, preset.zoomDuration)
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

        TimeScaleController.Instance.PlaySlowdown(
            preset.slowdownTimeScale, 0.02f, preset.slowdownHoldDuration, preset.slowdownEaseBackDuration);
    }

    private void SetFieldOfView(float value)
    {
        var lens = mainVCam.Lens;
        lens.FieldOfView = value;
        mainVCam.Lens = lens;
    }

    public void ResetCamera()
    {
        LeanTween.cancel(gameObject);
        if (hasCapturedBaseFov)
        {
            SetFieldOfView(baseFieldOfView);
        }
    }
}
