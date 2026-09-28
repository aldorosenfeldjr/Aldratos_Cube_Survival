// Assets/Scripts/PowerUps/PowerUpCollectFX.cs
using UnityEngine;
using UnityEngine.UI;

public class PowerUpCollectFX : MonoBehaviour
{
    [SerializeField]
    private RectTransform flyInLabelPrefab;
    [SerializeField]
    private RectTransform canvasRoot;

    public void PlayNewGrant(PowerUpDefinition definition, RectTransform targetSlot, System.Action onArrived)
    {
        var label = Instantiate(flyInLabelPrefab, canvasRoot);
        // Named lookup, not GetComponentInChildren<Image>(): the prefab also has a decorative glow Image sibling.
        var icon = label.Find("Icon").GetComponent<Image>();
        var name = label.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        icon.sprite = definition.Icon;
        name.text = definition.DisplayName;

        label.anchoredPosition = Vector2.zero;
        label.localScale = Vector3.zero;

        LeanTween.scale(label, Vector3.one * 1.2f, 0.15f)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                var targetAnchored = (Vector3)(Vector2)canvasRoot.InverseTransformPoint(targetSlot.position);
                LeanTween.move(label, targetAnchored, 0.25f)
                    .setEase(LeanTweenType.easeInQuad);
                LeanTween.scale(label, Vector3.one * 0.3f, 0.25f)
                    .setEase(LeanTweenType.easeInQuad)
                    .setOnComplete(() =>
                    {
                        Destroy(label.gameObject);
                        onArrived?.Invoke();
                    });
            });
    }

    public void PlayRefreshPulse(RectTransform existingIcon)
    {
        LeanTween.cancel(existingIcon.gameObject);
        LeanTween.scale(existingIcon, Vector3.one * 1.15f, 0.1f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(existingIcon, Vector3.one, 0.1f)
                    .setEase(LeanTweenType.easeInQuad);
            });
    }
}
