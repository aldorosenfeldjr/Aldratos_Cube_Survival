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

        // A power-up grant takes ownership of Time.timeScale away from GameManager's
        // pause tween, if one happens to be mid-flight, so the two systems don't fight
        // over the same value.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CancelPauseTween();
        }

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
