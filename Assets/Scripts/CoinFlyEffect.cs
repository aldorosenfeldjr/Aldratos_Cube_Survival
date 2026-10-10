using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cosmetic one-shot: an icon flies from a broken hazard's position to the HUD coin counter, then removes itself.
/// Purely visual — <see cref="GameManager.CollectCoin"/> credits the wallet immediately and independently, so this
/// can never desync the score. No <see cref="TimeScaleController"/> involvement: it always runs at normal speed.
/// </summary>
public static class CoinFlyEffect
{
    public static void Spawn(Image target, Vector3 worldStart, float duration)
    {
        var mainCamera = Camera.main;
        if (target == null || mainCamera == null)
        {
            return;
        }

        var canvas = target.canvas;
        var canvasRect = (RectTransform)canvas.transform;
        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        var icon = new GameObject("CoinFly", typeof(RectTransform)).AddComponent<Image>();
        icon.sprite = target.sprite;
        icon.raycastTarget = false;
        var rect = icon.rectTransform;
        rect.SetParent(canvasRect, false);
        rect.sizeDelta = target.rectTransform.rect.size;
        rect.anchoredPosition = LocalPoint(canvasRect, uiCamera, mainCamera.WorldToScreenPoint(worldStart));

        var targetPoint = LocalPoint(canvasRect, uiCamera, RectTransformUtility.WorldToScreenPoint(uiCamera, target.rectTransform.position));
        LeanTween.move(rect, targetPoint, duration).setEase(LeanTweenType.easeInQuad);
        LeanTween.scale(rect, Vector3.one * 0.6f, duration).setEase(LeanTweenType.easeInQuad).setOnComplete(() => Object.Destroy(icon.gameObject));
    }

    private static Vector2 LocalPoint(RectTransform canvasRect, Camera uiCamera, Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out var local);
        return local;
    }
}
