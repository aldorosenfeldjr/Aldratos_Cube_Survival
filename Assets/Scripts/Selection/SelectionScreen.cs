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
    private const float FadeInDuration = 0.25f;
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
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyLabel;
    [SerializeField] private Button tryButton;
    [SerializeField] private TextMeshProUGUI tryLabel;
    [SerializeField] private Button restoreButton;
    [SerializeField] private UnlockTile tilePrefab;
    [SerializeField] private RectTransform tileContainer;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private PreviewRotator previewRotator;
    [SerializeField] private Material pedestalMaterial;

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

        var group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
        group.alpha = 0f;
        group.LeanAlpha(1f, FadeInDuration);
    }

    // Idempotent: a second Back (a double tap, or Escape in the same frame) finds no callback left and does nothing.
    public void Back()
    {
        var close = onClose;
        onClose = null;
        gameObject.SetActive(false);
        close?.Invoke();
    }

    /// <summary>Focuses a tile's item: the preview and info show it. Used by taps and by the test.</summary>
    public void Focus(UnlockTile tile)
    {
        focused = tile;
        if (EventSystem.current.currentSelectedGameObject != tile.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(tile.gameObject);
        }
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

    /// <summary>Real-money purchase through the store service (only offered where a store exists).</summary>
    public void Buy()
    {
        if (focused == null)
        {
            return;
        }

        var item = focused.Definition;
        Services.Store.Buy(item.ProductId, success =>
        {
            if (success)
            {
                UnlockService.GrantPurchase(item);
                Refresh();
            }
        });
    }

    /// <summary>Watch a rewarded ad to try the item for a few runs. Granted only when the ad reports the reward was earned.</summary>
    public void Try()
    {
        if (focused == null)
        {
            return;
        }

        var item = focused.Definition;
        Services.Ads.ShowRewarded(earned =>
        {
            if (earned && UnlockService.StartTrial(item))
            {
                Refresh();
            }
        });
    }

    public void Restore()
    {
        Services.Store.Restore(productIds =>
        {
            UnlockService.RestorePurchases(productIds);
            if (focused != null)
            {
                Refresh();
            }
        });
    }

    private void OnEnable()
    {
        titleText.text = category == UnlockCategory.Character ? "Characters" : "Companions";
        BuildTiles();
        GetComponent<SelectionLayout>().Apply();
        CreateStage();
        Wallet.Changed += OnWalletChanged;

        if (tiles.Count == 0)
        {
            return;
        }

        var selected = UnlockService.Selected(category);
        var start = tiles.Find(tile => tile.Definition == selected) ?? tiles[0];
        EventSystem.current.SetSelectedGameObject(null);
        Focus(start);
    }

    private void OnDisable()
    {
        Wallet.Changed -= OnWalletChanged;
        LeanTween.cancel(gameObject);
        DestroyStage();
    }

    // A screen destroyed while open (rather than closed with Back) must still free its preview camera, stage and texture.
    private void OnDestroy()
    {
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
        var usable = UnlockService.IsUsable(definition);
        var trialRuns = UnlockService.TrialRunsLeft(definition);
        var selected = UnlockService.Selected(category) == definition;
        var price = UnlockService.Price(definition);
        var affordable = Wallet.Balance >= price;
        var store = Services.Store;
        var ads = Services.Ads;

        nameText.text = definition.DisplayName;
        tierText.text = definition.Tier == PriceTier.Free ? "Free" : definition.Tier.ToString();
        statusText.text = trialRuns > 0 ? $"Trial: {trialRuns} run{(trialRuns == 1 ? string.Empty : "s")} left"
            : selected ? "Selected" : owned ? "Owned" : $"{price} gems";

        selectButton.gameObject.SetActive(usable);
        selectButton.interactable = !selected;
        selectLabel.text = selected ? "Selected" : "Select";

        unlockButton.gameObject.SetActive(!owned);
        unlockButton.interactable = affordable;
        unlockLabel.text = affordable ? $"Unlock ({price} gems)" : $"Need {price - Wallet.Balance} more gems";

        buyButton.gameObject.SetActive(!owned && store.IsAvailable);
        buyLabel.text = $"Buy {store.LocalizedPrice(definition.ProductId)}";

        tryButton.gameObject.SetActive(!owned && ads.IsAvailable && trialRuns == 0);
        tryButton.interactable = ads.RewardedReady;
        tryLabel.text = ads.RewardedReady ? "Try: watch ad" : "Ad not ready";

        restoreButton.gameObject.SetActive(store.IsAvailable);
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
        cameraObject.transform.localPosition = new Vector3(2.0f, 1.3f, -4.0f);
        cameraObject.transform.LookAt(stage.transform.position + Vector3.down * 0.1f);
        var previewCamera = cameraObject.AddComponent<Camera>();
        // Transparent, so the hero stands over the shifting menu background instead of in a box.
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.fieldOfView = 38f;
        previewCamera.nearClipPlane = 0.3f;
        previewCamera.farClipPlane = 30f;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;
        previewCamera.targetTexture = previewTexture;

        // The stage is far from the play area but still lit by Core's warm sunset sun and ambient, which tints what it shows
        // (the white cat looked lavender). A range-limited neutral light evens the colours out and cannot reach gameplay.
        var lightObject = new GameObject("PreviewLight");
        lightObject.transform.SetParent(stage.transform, false);
        lightObject.transform.localPosition = new Vector3(1.5f, 2.5f, -2.5f);
        var previewLight = lightObject.AddComponent<Light>();
        previewLight.type = LightType.Point;
        previewLight.color = Color.white;
        previewLight.range = 12f;
        previewLight.intensity = 12f;

        // A pedestal for the hero to stand on: a wide base and a narrower top disc, feet at UnlockableDefinition.PreviewFloor.
        AddDisc("PedestalBase", 2.3f, 0.10f, UnlockableDefinition.PreviewFloor - 0.07f);
        AddDisc("Pedestal", 1.9f, 0.08f, UnlockableDefinition.PreviewFloor - 0.02f);
    }

    private void AddDisc(string discName, float diameter, float thickness, float centreY)
    {
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = discName;
        Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(stage.transform, false);
        disc.transform.localPosition = new Vector3(0f, centreY, 0f);
        disc.transform.localScale = new Vector3(diameter, thickness * 0.5f, diameter);
        disc.GetComponent<Renderer>().sharedMaterial = pedestalMaterial;
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
        previewRotator.Target = previewObject.transform;
    }

    // Scrolls the grid just enough to bring a keyboard/gamepad-focused tile into view.
    private void EnsureVisible(UnlockTile tile)
    {
        if (scroll == null || scroll.viewport == null)
        {
            return;
        }

        // A freshly opened screen has not been laid out yet; measure only real positions.
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
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
