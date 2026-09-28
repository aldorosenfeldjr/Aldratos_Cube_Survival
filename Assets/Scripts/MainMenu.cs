using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private GameObject levelSelect;

    [SerializeField]
    private RectTransform scoreRectTransform;

    [SerializeField]
    private GameObject firstSelected;

    [SerializeField]
    private GameObject exitButton;

    [SerializeField]
    private SelectionScreen selectionScreen;

    [SerializeField]
    private SelectionScreen companionScreen;

    [SerializeField]
    private GameObject removeAdsButton;

    [SerializeField]
    private SelectionTeaser characterTeaser;

    [SerializeField]
    private SelectionTeaser companionTeaser;

    // Opening a screen fades the menu out while the tapped teaser slides toward the side the screen's card sits on.
    private const float OpenDuration = 0.3f;
    private const float TeaserLandingLeft = 24f;

    // Wide canvases (landscape) keep the teasers stacked at the right edge. On narrow ones (portrait phones are about 850-880
    // units wide) that edge is only a few units clear of the centred buttons, so the teasers move under them instead.
    // The score card hangs from the top-left corner (pivot top): hidden above the screen on the menu, slides down for a run.
    private const float ScoreHiddenY = 200f;
    private const float ScoreShownY = -24f;
    public const float SideLayoutMinWidth = 1100f;
    public const float SideLayoutMargin = 16f;
    public const float SideCharacterY = 62f;
    public const float SideCompanionY = -125f;
    private const float StackedCharacterY = -390f;
    private const float StackedCompanionY = -580f;

    private bool transitioning;

    private void Awake()
    {
        ApplyTeaserLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (characterTeaser != null && companionTeaser != null && !transitioning)
        {
            ApplyTeaserLayout();
        }
    }

    /// <summary>Places both teasers for the canvas's current width.</summary>
    public void ApplyTeaserLayout()
    {
        ApplyTeaserLayout(((RectTransform)transform.parent).rect.width);
    }

    /// <summary>Places both teasers as they belong on a canvas of the given width (also used by the smoke test's layout audit).</summary>
    public void ApplyTeaserLayout(float canvasWidth)
    {
        var side = canvasWidth >= SideLayoutMinWidth;
        PlaceTeaser(characterTeaser, side, side ? SideCharacterY : StackedCharacterY);
        PlaceTeaser(companionTeaser, side, side ? SideCompanionY : StackedCompanionY);
    }

    private static void PlaceTeaser(SelectionTeaser teaser, bool side, float y)
    {
        var rect = (RectTransform)teaser.transform;
        var anchor = new Vector2(side ? 1f : 0.5f, 0.5f);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = new Vector2(side ? -SideLayoutMargin : 0f, y);
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        if (Application.isMobilePlatform)
        {
            exitButton.SetActive(false);
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);

        scoreRectTransform.anchoredPosition = new Vector2(scoreRectTransform.anchoredPosition.x, ScoreHiddenY);
    }

    // Whenever the menu (re)appears it is fully visible, usable and in its place, whatever a transition left behind.
    private void OnEnable()
    {
        RefreshRemoveAds();
        transitioning = false;
        var group = GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        ApplyTeaserLayout();
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
        if (characterTeaser != null)
        {
            LeanTween.cancel(characterTeaser.gameObject);
        }
        if (companionTeaser != null)
        {
            LeanTween.cancel(companionTeaser.gameObject);
        }
    }

    // Offered only where a store exists and the purchase is not owned yet.
    private void RefreshRemoveAds()
    {
        if (removeAdsButton == null)
        {
            return;
        }

        var store = Services.Store;
        removeAdsButton.SetActive(store.IsAvailable && !UnlockService.RemoveAdsOwned);
        removeAdsButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"Remove Ads {store.LocalizedPrice(StoreProducts.RemoveAds)}";
    }

    public void RemoveAds()
    {
        Services.Store.Buy(StoreProducts.RemoveAds, success =>
        {
            if (success)
            {
                UnlockService.GrantRemoveAds();
                RefreshRemoveAds();
            }
        });
    }

    private void Update()
    {
        var current = EventSystem.current.currentSelectedGameObject;

        if (current == null || !current.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
    public void Play()
    {
        GetComponent<CanvasGroup>()
            .LeanAlpha(0, 0.2f)
            .setOnComplete(OnComplete);
    }

    public void OnComplete()
    {
        scoreRectTransform
            .LeanMoveY(ScoreShownY, 0.75f)
            .setEaseOutBounce();

        levelSelect.SetActive(true);
        Destroy(gameObject);
    }

    public void OpenCharacters()
    {
        Open(selectionScreen, characterTeaser, MenuBackgroundRig.Instance != null ? MenuBackgroundRig.Instance.MoveToCharacters : (System.Action)null);
    }

    public void OpenCompanions()
    {
        Open(companionScreen, companionTeaser, MenuBackgroundRig.Instance != null ? MenuBackgroundRig.Instance.MoveToCompanions : (System.Action)null);
    }

    private void Open(SelectionScreen screen, SelectionTeaser teaser, System.Action moveBackground)
    {
        if (transitioning)
        {
            return;
        }

        transitioning = true;
        var group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        moveBackground?.Invoke();

        var teaserRect = (RectTransform)teaser.transform;
        var home = teaserRect.anchoredPosition;
        // Slide until the teaser's left edge sits TeaserLandingLeft from the canvas's left edge, whichever way it is anchored.
        var canvasWidth = ((RectTransform)transform.parent).rect.width;
        var landingX = TeaserLandingLeft - teaserRect.anchorMin.x * canvasWidth + teaserRect.pivot.x * teaserRect.rect.width;
        LeanTween.value(teaser.gameObject, home.x, landingX, OpenDuration)
            .setEaseInOutSine()
            .setOnUpdate((float x) => teaserRect.anchoredPosition = new Vector2(x, home.y));

        group.LeanAlpha(0f, OpenDuration).setOnComplete(() =>
        {
            gameObject.SetActive(false);
            screen.Open(OnScreenClosed);
        });
    }

    // The screen's Back: the background eases home and the menu fades back in (OnEnable has already made it usable).
    private void OnScreenClosed()
    {
        if (MenuBackgroundRig.Instance != null)
        {
            MenuBackgroundRig.Instance.MoveToMainMenu();
        }

        gameObject.SetActive(true);
        var group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.LeanAlpha(1f, OpenDuration);
    }

    // Button targets live inside this menu so the prefab has no scene references.
    // Uses the wired field: GameManager.Instance is not set until the first run starts.
    public void Exit()
    {
        gameManager.ExitGame();
    }

    public void ClearHighScore()
    {
        GameManager.ClearHighScore();
    }
}
