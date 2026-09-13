using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private RectTransform scoreRectTransform;

    [SerializeField]
    private GameObject firstSelected;

    [SerializeField]
    private GameObject exitButton;

    private void Start()
    {
        if (Application.isMobilePlatform)
        {
            exitButton.SetActive(false);
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);

        scoreRectTransform.anchoredPosition = new Vector2(scoreRectTransform.anchoredPosition.x, 20);
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

        gameManager.Enable();
        Destroy(gameObject);
    }
}
