using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A Main Menu spoiler for one <see cref="UnlockCategory"/>: a title and the catalog's first three items as small,
/// non-interactive <see cref="UnlockTile"/>s, so a locked item still shows its lock and the equipped one its frame. The
/// tiles are the same prefab the grid uses, scaled down inside fixed-size slots (so their text scales with them) and
/// they let taps through: the whole panel is one button, <see cref="OpenButton"/>, wired by the Main Menu.
/// </summary>
public class SelectionTeaser : MonoBehaviour
{
    private const int SpoilerCount = 3;
    private const float SlotSize = 88f;

    [SerializeField] private UnlockCategory category;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button openButton;
    [SerializeField] private UnlockTile tilePrefab;
    [SerializeField] private RectTransform tileContainer;

    private readonly System.Collections.Generic.List<UnlockTile> tiles = new System.Collections.Generic.List<UnlockTile>();

    public UnlockCategory Category => category;
    public Button OpenButton => openButton;

    private void OnEnable()
    {
        titleText.text = category == UnlockCategory.Character ? "Characters" : "Companions";
        BuildTiles();
        Refresh();
        Wallet.Changed += OnWalletChanged;
        UnlockService.Changed += Refresh;
    }

    private void OnDisable()
    {
        Wallet.Changed -= OnWalletChanged;
        UnlockService.Changed -= Refresh;
    }

    private void BuildTiles()
    {
        if (tiles.Count > 0 || UnlockCatalog.Instance == null)
        {
            return;
        }

        foreach (var definition in UnlockCatalog.Instance.InCategory(category).Take(SpoilerCount))
        {
            var slot = new GameObject("Slot", typeof(RectTransform));
            slot.transform.SetParent(tileContainer, false);
            ((RectTransform)slot.transform).sizeDelta = new Vector2(SlotSize, SlotSize);

            var tile = Instantiate(tilePrefab, slot.transform);
            var tileRect = (RectTransform)tile.transform;
            tileRect.anchorMin = tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.anchoredPosition = Vector2.zero;
            tileRect.localScale = Vector3.one * (SlotSize / tileRect.sizeDelta.x);

            tile.Bind(definition);
            tile.Button.interactable = false;
            foreach (var graphic in tile.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
            tiles.Add(tile);
        }
    }

    private void Refresh()
    {
        foreach (var tile in tiles)
        {
            tile.Refresh();
        }
    }

    private void OnWalletChanged(int balance)
    {
        Refresh();
    }
}
