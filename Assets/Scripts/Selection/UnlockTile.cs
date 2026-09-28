using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One grid tile: swatch (or thumbnail), name, a price/lock badge for locked items and a frame on the selected one.</summary>
public class UnlockTile : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image swatch;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI badgeText;
    [SerializeField] private GameObject selectedFrame;

    public UnlockableDefinition Definition { get; private set; }
    public Button Button => button;

    public void Bind(UnlockableDefinition definition)
    {
        Definition = definition;
        nameText.text = definition.DisplayName;
        if (definition.Thumbnail != null)
        {
            swatch.sprite = definition.Thumbnail;
            swatch.color = Color.white;
            swatch.preserveAspect = true;
        }
        else
        {
            swatch.color = definition.TileColor;
        }
        Refresh();
    }

    public void Refresh()
    {
        var owned = UnlockService.IsOwned(Definition);
        badgeText.gameObject.SetActive(!owned);
        badgeText.text = owned ? string.Empty : $"Locked - {UnlockService.Price(Definition)}";
        selectedFrame.SetActive(UnlockService.Selected(Definition.Category) == Definition);
    }
}
