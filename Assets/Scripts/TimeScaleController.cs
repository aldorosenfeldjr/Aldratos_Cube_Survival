using System.Collections;
using UnityEngine;

/// <summary>
/// The only writer of <see cref="Time.timeScale"/> and <see cref="Time.fixedDeltaTime"/>.
/// The effective scale is pauseFactor * effectScale, so the pause fade and a power-up
/// slowdown can overlap without fighting over the same value.
/// Lives on its own always-active object: GameManager and the main vcam get disabled on game over.
/// </summary>
public class TimeScaleController : MonoBehaviour
{
    private const float BaseFixedDeltaTime = 0.02f;

    private float pauseFactor = 1f;
    private float effectScale = 1f;
    private Coroutine pauseRoutine;
    private Coroutine effectRoutine;

    public static TimeScaleController Instance { get; private set; }
    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Instance = this;
        ResetAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Pause()
    {
        IsPaused = true;
        RestartRoutine(ref pauseRoutine, Tween(pauseFactor, 0f, GameConfig.Instance.PauseFadeDuration, SetPauseFactor));
    }

    public void Resume()
    {
        IsPaused = false;
        RestartRoutine(ref pauseRoutine, Tween(pauseFactor, 1f, GameConfig.Instance.PauseFadeDuration, SetPauseFactor));
    }

    /// <summary>Dips the effect layer to <paramref name="scale"/>, holds, then eases back to 1. Real-time durations.</summary>
    public void PlaySlowdown(float scale, float inDuration, float holdDuration, float outDuration)
    {
        RestartRoutine(ref effectRoutine, SlowdownRoutine(scale, inDuration, holdDuration, outDuration));
    }

    /// <summary>Instantly returns everything to normal speed and clears pause.</summary>
    public void ResetAll()
    {
        StopRoutine(ref pauseRoutine);
        StopRoutine(ref effectRoutine);
        IsPaused = false;
        pauseFactor = 1f;
        effectScale = 1f;
        Apply();
    }

    private IEnumerator SlowdownRoutine(float scale, float inDuration, float holdDuration, float outDuration)
    {
        yield return Tween(effectScale, scale, inDuration, SetEffectScale);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Tween(scale, 1f, outDuration, SetEffectScale);
        effectRoutine = null;
    }

    private static IEnumerator Tween(float from, float to, float duration, System.Action<float> set)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            set(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        set(to);
    }

    private void SetPauseFactor(float value)
    {
        pauseFactor = value;
        Apply();
    }

    private void SetEffectScale(float value)
    {
        effectScale = value;
        Apply();
    }

    private void Apply()
    {
        var scale = pauseFactor * effectScale;
        Time.timeScale = scale;
        Time.fixedDeltaTime = BaseFixedDeltaTime * scale;
    }

    private void RestartRoutine(ref Coroutine routine, IEnumerator body)
    {
        StopRoutine(ref routine);
        routine = StartCoroutine(body);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
}
