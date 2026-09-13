using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameOverMenu : MonoBehaviour
{
    private LTDescr restartAnimation;
    [SerializeField]
    private TMPro.TextMeshProUGUI highScore;
    [SerializeField]
    private GameObject firstSelected;

    private void OnEnable()
    {
        highScore.text = $"High Score: {GameManager.Instance.HighScore}";

        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(0, rectTransform.rect.height);

        rectTransform.LeanMoveY(0, 1f).setEaseOutElastic().delay = 0.75f;

        if (restartAnimation is null)
        {
            restartAnimation = GetComponentInChildren<TMPro.TextMeshProUGUI>().gameObject
                .LeanScale(new Vector3(1.2f, 1.2f), 0.5f)
                .setLoopPingPong();
        }
        restartAnimation.resume();

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
        restartAnimation.pause();
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
