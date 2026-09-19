using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SunGlowPulse : MonoBehaviour
{
    [SerializeField]
    private float minAlpha = 0.25f;
    [SerializeField]
    private float maxAlpha = 0.55f;
    [SerializeField]
    private float minScale = 0.92f;
    [SerializeField]
    private float maxScale = 1.08f;
    [SerializeField]
    private float pulseDuration = 3.5f;

    private Image glowImage;

    private void OnEnable()
    {
        glowImage = GetComponent<Image>();
        transform.localScale = Vector3.one * minScale;
        SetAlpha(minAlpha);
        Pulse(true);
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
    }

    private void Pulse(bool growing)
    {
        var targetAlpha = growing ? maxAlpha : minAlpha;
        var targetScale = growing ? maxScale : minScale;

        LeanTween.value(gameObject, glowImage.color.a, targetAlpha, pulseDuration)
            .setEaseInOutSine()
            .setOnUpdate(SetAlpha);

        LeanTween.scale(gameObject, Vector3.one * targetScale, pulseDuration)
            .setEaseInOutSine()
            .setOnComplete(() => Pulse(!growing));
    }

    private void SetAlpha(float alpha)
    {
        var color = glowImage.color;
        color.a = alpha;
        glowImage.color = color;
    }
}
