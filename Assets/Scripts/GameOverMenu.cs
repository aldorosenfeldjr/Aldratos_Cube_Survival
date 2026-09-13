using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameOverMenu : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI highScore;
    [SerializeField]
    private GameObject firstSelected;
    [SerializeField]
    private GameObject quitButton;

    private void Awake()
    {
        if (Application.isMobilePlatform)
        {
            quitButton.SetActive(false);
        }
    }

    private void OnEnable()
    {
        highScore.text = $"High Score: {GameManager.Instance.HighScore}";

        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(0, rectTransform.rect.height);

        rectTransform.LeanMoveY(0, 1f).setEaseOutElastic().delay = 0.75f;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
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

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
