using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }

    public void Restart()
    {
        gameObject.SetActive(false);

        GameManager.Instance.Enable();
    }

    public void NextLevel()
    {
        gameObject.SetActive(false);
        GameManager.Instance.NextLevel();
    }

    public void Quit()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
}
