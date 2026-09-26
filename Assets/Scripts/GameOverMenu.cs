using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI highScore;
    [SerializeField]
    private TMPro.TextMeshProUGUI finalScoreText;
    [SerializeField]
    private TMPro.TextMeshProUGUI gemBreakdownText;
    [SerializeField]
    private GameObject nextLevelButton;
    [SerializeField]
    private GameObject scoreHud;
    [SerializeField]
    private GameObject firstSelected;

    [Header("Trial over prompt")]
    [SerializeField]
    private GameObject trialPanel;
    [SerializeField]
    private TMPro.TextMeshProUGUI trialTitle;
    [SerializeField]
    private Button trialBuyButton;
    [SerializeField]
    private Button trialUnlockButton;
    [SerializeField]
    private Button trialAdButton;
    [SerializeField]
    private Button trialDismissButton;

    private UnlockableDefinition trialItem;

    private void OnEnable()
    {
        highScore.text = $"High Score: {GameManager.Instance.HighScore}";

        scoreHud.SetActive(false);

        var game = GameManager.Instance;
        nextLevelButton.SetActive(game.ClearedThisRun && game.HasNextLevel);
        var total = game.GemsCollected + game.MultiplierBonus + game.ClearReward;
        gemBreakdownText.text = $"Gems collected: {game.GemsCollected}\nMultiplier bonus: {game.MultiplierBonus}\nClear reward: {game.ClearReward}\nTotal: +{total}";

        finalScoreText.text = $"Score: {GameManager.Instance.Score}";
        finalScoreText.transform.localScale = Vector3.zero;
        LeanTween.scale(finalScoreText.gameObject, Vector3.one, 0.6f)
            .setEaseOutElastic()
            .setDelay(1.5f);

        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(0, rectTransform.rect.height);

        rectTransform.LeanMoveY(0, 1f).setEaseOutElastic().delay = 0.75f;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);

        trialItem = game.TrialEnded;
        if (trialItem != null)
        {
            OpenTrialPanel();
        }
        else
        {
            trialPanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (scoreHud != null)
        {
            scoreHud.SetActive(true);
        }
    }

    private void Update()
    {
        var current = EventSystem.current.currentSelectedGameObject;

        if (current == null || !current.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(trialPanel.activeSelf ? FirstTrialButton() : firstSelected);
        }
    }

    public void Restart()
    {
        InterstitialPacer.OnLeave(() =>
        {
            gameObject.SetActive(false);
            GameManager.Instance.Enable();
        });
    }

    public void NextLevel()
    {
        InterstitialPacer.OnLeave(() =>
        {
            gameObject.SetActive(false);
            GameManager.Instance.NextLevel();
        });
    }

    public void Quit()
    {
        InterstitialPacer.OnLeave(() => GameManager.Instance.ReturnToMainMenu());
    }

    // Trial-over prompt: keep the item (buy / gems) or watch an ad for more runs. Each option shows only when the platform offers it.
    private void OpenTrialPanel()
    {
        trialPanel.SetActive(true);
        SetMainButtonsInteractable(false);
        trialTitle.text = $"Trial over: keep {trialItem.DisplayName}?";

        var store = Services.Store;
        trialBuyButton.gameObject.SetActive(store.IsAvailable);
        trialBuyButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"Buy {store.LocalizedPrice(trialItem.ProductId)}";

        var price = UnlockService.Price(trialItem);
        trialUnlockButton.gameObject.SetActive(Wallet.Balance >= price);
        trialUnlockButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"Unlock ({price} gems)";

        var ads = Services.Ads;
        trialAdButton.gameObject.SetActive(ads.IsAvailable && ads.RewardedReady);
        trialAdButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"Watch ad: {EconomyConfig.Instance.TrialRuns} more runs";

        EventSystem.current.SetSelectedGameObject(FirstTrialButton());
    }

    private GameObject FirstTrialButton()
    {
        foreach (var button in new[] { trialBuyButton, trialUnlockButton, trialAdButton })
        {
            if (button.gameObject.activeSelf)
            {
                return button.gameObject;
            }
        }
        return trialDismissButton.gameObject;
    }

    private void CloseTrialPanel()
    {
        trialPanel.SetActive(false);
        SetMainButtonsInteractable(true);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    private void SetMainButtonsInteractable(bool interactable)
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (!button.transform.IsChildOf(trialPanel.transform))
            {
                button.interactable = interactable;
            }
        }
    }

    public void TrialBuy()
    {
        var item = trialItem;
        Services.Store.Buy(item.ProductId, success =>
        {
            if (success)
            {
                UnlockService.GrantPurchase(item);
                UnlockService.Select(item);
                CloseTrialPanel();
            }
        });
    }

    public void TrialUnlock()
    {
        if (UnlockService.TryBuyWithGems(trialItem))
        {
            UnlockService.Select(trialItem);
            CloseTrialPanel();
        }
    }

    public void TrialWatchAd()
    {
        var item = trialItem;
        Services.Ads.ShowRewarded(earned =>
        {
            if (earned)
            {
                InterstitialPacer.MarkRewardedWatched();
                UnlockService.StartTrial(item);
                CloseTrialPanel();
            }
        });
    }

    public void TrialDismiss()
    {
        CloseTrialPanel();
    }
}
