using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Shown when a run reaches the level's clear target: reward summary plus Next level / Keep going / Menu.</summary>
public class SuccessMenu : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI title;
    [SerializeField]
    private TMPro.TextMeshProUGUI rewardText;
    [SerializeField]
    private GameObject nextLevelButton;
    [SerializeField]
    private GameObject keepGoingButton;

    private void OnEnable()
    {
        var game = GameManager.Instance;
        var clear = game.LastClear;

        title.text = $"{game.LevelName} cleared!";
        var reward = $"+{clear.Reward} gems{(clear.FirstClear ? " (first clear)" : string.Empty)}";
        rewardText.text = clear.UnlockedLevelId != null ? $"{reward}\nNew level unlocked!" : reward;

        nextLevelButton.SetActive(game.HasNextLevel);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(game.HasNextLevel ? nextLevelButton : keepGoingButton);
    }

    private void Update()
    {
        var current = EventSystem.current.currentSelectedGameObject;

        if (current == null || !current.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(nextLevelButton.activeSelf ? nextLevelButton : keepGoingButton);
        }
    }

    // Button targets live inside this menu so the prefab has no scene references.
    public void NextLevel()
    {
        GameManager.Instance.NextLevel();
    }

    public void KeepGoing()
    {
        GameManager.Instance.KeepGoing();
    }

    public void Quit()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
}
