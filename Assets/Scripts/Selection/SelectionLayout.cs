using UnityEngine;

/// <summary>
/// Switches the selection screen between landscape (info on the left, grid on the right) and portrait
/// (info on top, grid below). The app auto-rotates, so this re-runs whenever the screen's size changes.
/// Panels are positioned by anchors only, so it works at any resolution.
/// </summary>
public class SelectionLayout : MonoBehaviour
{
    [SerializeField] private RectTransform infoPanel;
    [SerializeField] private RectTransform gridPanel;
    [Tooltip("Height of the top bar (Back, title, gem counter) that both panels sit below.")]
    [SerializeField] private float topBarHeight = 100f;
    [SerializeField] private float padding = 24f;
    [Tooltip("Share of the width given to the info panel in landscape.")]
    [SerializeField, Range(0.3f, 0.6f)] private float landscapeInfoShare = 0.42f;
    [Tooltip("Share of the height given to the info panel in portrait (it also holds the preview, so it needs more).")]
    [SerializeField, Range(0.3f, 0.7f)] private float portraitInfoShare = 0.52f;

    public bool IsLandscape { get; private set; }

    private void OnEnable()
    {
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    public void Apply()
    {
        if (infoPanel == null || gridPanel == null)
        {
            return;
        }

        var size = ((RectTransform)transform).rect.size;
        IsLandscape = size.x >= size.y;

        if (IsLandscape)
        {
            SetAnchors(infoPanel, 0f, 0f, landscapeInfoShare, 1f);
            SetAnchors(gridPanel, landscapeInfoShare, 0f, 1f, 1f);
        }
        else
        {
            SetAnchors(infoPanel, 0f, 1f - portraitInfoShare, 1f, 1f);
            SetAnchors(gridPanel, 0f, 0f, 1f, 1f - portraitInfoShare);
        }
    }

    // Anchored fractions of the area below the top bar; padding keeps panels off the edges and off each other.
    private void SetAnchors(RectTransform panel, float minX, float minY, float maxX, float maxY)
    {
        panel.anchorMin = new Vector2(minX, minY);
        panel.anchorMax = new Vector2(maxX, maxY);
        var half = padding * 0.5f;
        panel.offsetMin = new Vector2(minX <= 0f ? padding : half, minY <= 0f ? padding : half);
        panel.offsetMax = new Vector2(maxX >= 1f ? -padding : -half, maxY >= 1f ? -(topBarHeight + half) : -half);
    }
}
