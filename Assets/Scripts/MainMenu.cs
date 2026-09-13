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

    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);

        scoreRectTransform.anchoredPosition = new Vector2(scoreRectTransform.anchoredPosition.x, 20);

        GetComponentInChildren<TMPro.TextMeshProUGUI>().gameObject
            .LeanScale(new Vector3(1.2f, 1.2f), 0.5f)
            .setLoopPingPong();
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
