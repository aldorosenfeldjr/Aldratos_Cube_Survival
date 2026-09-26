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
    private GameObject removeAdsButton;

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

        scoreRectTransform.anchoredPosition = new Vector2(scoreRectTransform.anchoredPosition.x, 20);
    }

    private void OnEnable()
    {
        RefreshRemoveAds();
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
            .LeanMoveY(-72f, 0.75f)
            .setEaseOutBounce();

        levelSelect.SetActive(true);
        Destroy(gameObject);
    }

    public void OpenCharacters()
    {
        gameObject.SetActive(false);
        selectionScreen.Open(() => gameObject.SetActive(true));
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
