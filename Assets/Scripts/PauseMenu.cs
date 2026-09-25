using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject firstSelected;

    private void OnEnable()
    {
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

    // Button targets live inside this menu so the prefab has no scene references.
    public void Resume()
    {
        GameManager.Instance.Resume();
    }

    public void Restart()
    {
        GameManager.Instance.RestartGame();
    }

    public void Quit()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
}
