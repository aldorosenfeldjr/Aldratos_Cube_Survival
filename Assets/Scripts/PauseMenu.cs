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
}
