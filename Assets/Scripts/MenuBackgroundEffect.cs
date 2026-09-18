using UnityEngine;
using UnityEngine.UI;

public class MenuBackgroundEffect : MonoBehaviour
{
    [SerializeField]
    private RectTransform particleContainer;
    [SerializeField]
    private Sprite particleSprite;
    [SerializeField]
    private int particleCount = 10;
    [SerializeField]
    private float minDuration = 6f;
    [SerializeField]
    private float maxDuration = 12f;
    [SerializeField]
    private float minSize = 10f;
    [SerializeField]
    private float maxSize = 28f;

    private RectTransform[] particles;

    private void OnEnable()
    {
        if (particles == null)
        {
            BuildParticles();
        }

        foreach (var particle in particles)
        {
            ResetParticle(particle);
        }
    }

    private void OnDisable()
    {
        if (particles == null)
        {
            return;
        }

        foreach (var particle in particles)
        {
            if (particle != null)
            {
                LeanTween.cancel(particle.gameObject);
            }
        }
    }

    private void BuildParticles()
    {
        particles = new RectTransform[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject("Particle" + i, typeof(RectTransform));
            go.transform.SetParent(particleContainer, false);
            var rectTransform = (RectTransform)go.transform;
            var size = Random.Range(minSize, maxSize);
            rectTransform.sizeDelta = new Vector2(size, size);

            var image = go.AddComponent<Image>();
            image.sprite = particleSprite;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, Random.Range(0.15f, 0.35f));

            particles[i] = rectTransform;
        }
    }

    private void ResetParticle(RectTransform particle)
    {
        var width = particleContainer.rect.width;
        var height = particleContainer.rect.height;
        var startX = Random.Range(-width / 2f, width / 2f);
        var startY = Random.Range(-height / 2f, 0f);
        particle.anchoredPosition = new Vector2(startX, startY);

        var image = particle.GetComponent<Image>();
        var targetAlpha = image.color.a;
        var duration = Random.Range(minDuration, maxDuration);
        var riseDistance = height * Random.Range(0.6f, 1.1f);

        LeanTween.value(particle.gameObject, 0f, 1f, duration)
            .setOnUpdate((float t) =>
            {
                particle.anchoredPosition = new Vector2(startX, startY + riseDistance * t);
                float fade = t < 0.15f ? t / 0.15f : (t > 0.85f ? (1f - t) / 0.15f : 1f);
                image.color = new Color(1f, 1f, 1f, targetAlpha * fade);
            })
            .setOnComplete(() => ResetParticle(particle));
    }
}
