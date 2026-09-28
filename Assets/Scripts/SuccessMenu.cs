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
    [SerializeField]
    private GameObject quitButton;

    // The vertical slot each button sits in when all three are shown, read once from the positions the UI skin builder
    // set (single source of truth: the spacing is never duplicated here). When Next level is hidden, Keep going and
    // Quit slide up into its slot instead of leaving a gap at the top of the panel.
    private float[] slotY;

    private void Awake()
    {
        slotY = new[]
        {
            SlotYOf(nextLevelButton),
            SlotYOf(keepGoingButton),
            SlotYOf(quitButton),
        };
    }

    private void OnEnable()
    {
        var game = GameManager.Instance;
        var clear = game.LastClear;

        title.text = $"{game.LevelName} cleared!";
        var reward = $"+{clear.Reward} gems{(clear.FirstClear ? " (first clear)" : string.Empty)}";
        rewardText.text = clear.UnlockedLevelId != null ? $"{reward}\nNew level unlocked!" : reward;

        nextLevelButton.SetActive(game.HasNextLevel);
        LayoutButtons(game.HasNextLevel);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(game.HasNextLevel ? nextLevelButton : keepGoingButton);
    }

    private void LayoutButtons(bool hasNextLevel)
    {
        SetSlotY(keepGoingButton, hasNextLevel ? slotY[1] : slotY[0]);
        SetSlotY(quitButton, hasNextLevel ? slotY[2] : slotY[1]);
    }

    private static float SlotYOf(GameObject button) => ((RectTransform)button.transform).anchoredPosition.y;

    private static void SetSlotY(GameObject button, float y)
    {
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
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
