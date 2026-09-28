using UnityEngine;

/// <summary>
/// Switches the selection screen between landscape (card/grid on the left, hero preview on the right) and
/// portrait (card/grid on top, hero preview below), matching the reference layout. The app auto-rotates, so this
/// re-runs whenever the screen's size changes. Panels are positioned by anchors only, so it works at any resolution.
/// </summary>
public class SelectionLayout : MonoBehaviour
{
    [Tooltip("The hero: live preview, name and action buttons.")]
    [SerializeField] private RectTransform infoPanel;
    [Tooltip("The card: the scrollable grid of items.")]
    [SerializeField] private RectTransform gridPanel;
    [Tooltip("Height of the top bar (Back, title, gem counter) that both panels sit below.")]
    [SerializeField] private float topBarHeight = 100f;
    [SerializeField] private float padding = 24f;
    [Tooltip("Share of the screen given to the hero preview: width in landscape, height in portrait.")]
    [SerializeField, Range(0.3f, 0.6f)] private float heroShare = 0.5f;

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
            SetAnchors(gridPanel, 0f, 0f, 1f - heroShare, 1f);
            SetAnchors(infoPanel, 1f - heroShare, 0f, 1f, 1f);
        }
        else
        {
            SetAnchors(gridPanel, 0f, 1f - heroShare, 1f, 1f);
            SetAnchors(infoPanel, 0f, 0f, 1f, 1f - heroShare);
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
