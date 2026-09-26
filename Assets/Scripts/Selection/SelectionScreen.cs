using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The shared selection screen for one <see cref="UnlockCategory"/>: a grid of the catalog's items, a static preview of
/// the focused one, and Select / Unlock (gems) actions. Focus follows the EventSystem selection, so mouse, touch,
/// arrow keys and gamepad all behave the same. The preview is a small camera rendering into a RenderTexture; the
/// camera and its stage are created while the screen is open and live far from the play area.
/// </summary>
public class SelectionScreen : MonoBehaviour
{
    private const int PreviewSize = 512;
    private static readonly Vector3 StagePosition = new Vector3(0f, -1000f, 0f);

    [SerializeField] private UnlockCategory category;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI selectLabel;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI unlockLabel;
    [SerializeField] private UnlockTile tilePrefab;
    [SerializeField] private RectTransform tileContainer;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private Color previewBackground = new Color(0.12f, 0.14f, 0.18f, 1f);

    private readonly List<UnlockTile> tiles = new List<UnlockTile>();
    private UnlockTile focused;
    private Action onClose;
    private RenderTexture previewTexture;
    private GameObject stage;
    private GameObject previewObject;

    public UnlockCategory Category => category;
    public IReadOnlyList<UnlockTile> Tiles => tiles;
    public UnlockTile Focused => focused;

    /// <summary>Opens the screen. <paramref name="onClose"/> runs when the player goes back (the caller re-shows its own menu).</summary>
    public void Open(Action onClose)
    {
        this.onClose = onClose;
        gameObject.SetActive(true);
    }

    public void Back()
    {
        gameObject.SetActive(false);
        onClose?.Invoke();
    }

    /// <summary>Focuses a tile's item: the preview and info show it. Used by taps and by the test.</summary>
    public void Focus(UnlockTile tile)
    {
        focused = tile;
        ShowPreview(tile.Definition);
        Refresh();
        EnsureVisible(tile);
    }

    public void Select()
    {
        if (focused != null && UnlockService.Select(focused.Definition))
        {
            Refresh();
            EventSystem.current.SetSelectedGameObject(focused.gameObject);
        }
    }

    public void Unlock()
    {
        if (focused != null && UnlockService.TryBuyWithGems(focused.Definition))
        {
            Refresh();
            EventSystem.current.SetSelectedGameObject(focused.gameObject);
        }
    }

    private void OnEnable()
    {
        titleText.text = category == UnlockCategory.Character ? "Characters" : "Companions";
        BuildTiles();
        CreateStage();
        Wallet.Changed += OnWalletChanged;

        if (tiles.Count == 0)
        {
            return;
        }

        var selected = UnlockService.Selected(category);
        var start = tiles.Find(tile => tile.Definition == selected) ?? tiles[0];
        Focus(start);
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(start.gameObject);
    }

    private void OnDisable()
    {
        Wallet.Changed -= OnWalletChanged;
        DestroyStage();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Back();
            return;
        }

        if (focused == null)
        {
            return;
        }

        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null || !current.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(focused.gameObject);
            return;
        }

        var tile = current.GetComponent<UnlockTile>();
        if (tile != null && tile != focused)
        {
            Focus(tile);
        }
    }

    private void OnWalletChanged(int balance)
    {
        if (focused != null)
        {
            Refresh();
        }
    }

    private void BuildTiles()
    {
        if (tiles.Count > 0 || UnlockCatalog.Instance == null)
        {
            return;
        }

        foreach (var definition in UnlockCatalog.Instance.InCategory(category))
        {
            var tile = Instantiate(tilePrefab, tileContainer);
            tile.Bind(definition);
            tiles.Add(tile);
        }
    }

    private void Refresh()
    {
        foreach (var tile in tiles)
        {
            tile.Refresh();
        }

        var definition = focused.Definition;
        var owned = UnlockService.IsOwned(definition);
        var selected = UnlockService.Selected(category) == definition;
        var price = UnlockService.Price(definition);
        var affordable = Wallet.Balance >= price;

        nameText.text = definition.DisplayName;
        tierText.text = definition.Tier == PriceTier.Free ? "Free" : definition.Tier.ToString();
        statusText.text = selected ? "Selected" : owned ? "Owned" : $"{price} gems";

        selectButton.gameObject.SetActive(owned);
        selectButton.interactable = !selected;
        selectLabel.text = selected ? "Selected" : "Select";

        unlockButton.gameObject.SetActive(!owned);
        unlockButton.interactable = affordable;
        unlockLabel.text = affordable ? $"Unlock ({price} gems)" : $"Need {price - Wallet.Balance} more gems";
    }

    private void CreateStage()
    {
        previewTexture = new RenderTexture(PreviewSize, PreviewSize, 24) { name = "SelectionPreview" };
        previewImage.texture = previewTexture;

        stage = new GameObject("SelectionPreviewStage") { hideFlags = HideFlags.DontSave };
        stage.transform.position = StagePosition;

        // Static 3/4 view of the item: no rotation, no idle animation.
        var cameraObject = new GameObject("PreviewCamera");
        cameraObject.transform.SetParent(stage.transform, false);
        cameraObject.transform.localPosition = new Vector3(2.4f, 1.9f, -3.4f);
        cameraObject.transform.LookAt(stage.transform.position);
        var previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = previewBackground;
        previewCamera.fieldOfView = 30f;
        previewCamera.nearClipPlane = 0.3f;
        previewCamera.farClipPlane = 30f;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;
        previewCamera.targetTexture = previewTexture;
    }

    private void DestroyStage()
    {
        if (previewImage != null)
        {
            previewImage.texture = null;
        }
        if (stage != null)
        {
            Destroy(stage);
        }
        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }
        stage = null;
        previewObject = null;
        previewTexture = null;
    }

    private void ShowPreview(UnlockableDefinition definition)
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
        previewObject = definition.CreatePreview(stage.transform);
        previewObject.transform.localPosition = Vector3.zero;
    }

    // Scrolls the grid just enough to bring a keyboard/gamepad-focused tile into view.
    private void EnsureVisible(UnlockTile tile)
    {
        if (scroll == null || scroll.viewport == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var viewport = scroll.viewport;
        var tileRect = (RectTransform)tile.transform;
        var corners = new Vector3[4];
        tileRect.GetWorldCorners(corners);
        var bottom = viewport.InverseTransformPoint(corners[0]).y;
        var top = viewport.InverseTransformPoint(corners[1]).y;

        var content = scroll.content;
        if (bottom < viewport.rect.yMin)
        {
            content.anchoredPosition += new Vector2(0f, viewport.rect.yMin - bottom);
        }
        else if (top > viewport.rect.yMax)
        {
            content.anchoredPosition -= new Vector2(0f, top - viewport.rect.yMax);
        }
    }
}
