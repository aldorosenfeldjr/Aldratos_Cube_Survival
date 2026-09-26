// Assets/Scripts/LevelSelect.cs
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;
    [SerializeField]
    private LevelRegistry registry;
    [SerializeField]
    private GameObject levelTilePrefab;
    [SerializeField]
    private Transform tileContainer;
    [SerializeField]
    private CanvasGroup canvasGroup;
    [SerializeField]
    private GameObject menuBackground;

    private string loadedLevelSceneName;
    private string pendingSceneName;

    /// <summary>Loads a level straight from a run (e.g. the Success screen's Next level), skipping the tile menu.</summary>
    public void PlayLevel(string sceneName)
    {
        pendingSceneName = sceneName;
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        if (pendingSceneName != null)
        {
            var sceneName = pendingSceneName;
            pendingSceneName = null;
            canvasGroup.alpha = 0f;
            SelectLevel(sceneName);
            return;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        foreach (Transform child in tileContainer)
        {
            Destroy(child.gameObject);
        }

        GameObject firstTile = null;
        for (var i = 0; i < registry.LevelEntries.Count; i++)
        {
            var entry = registry.LevelEntries[i];
            var unlocked = LevelProgression.IsUnlocked(registry, i);
            var tile = Instantiate(levelTilePrefab, tileContainer);
            var label = tile.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            label.text = $"{entry.DisplayName}\n<size=45%>{TileSubtitle(i, unlocked)}</size>";

            var button = tile.GetComponent<UnityEngine.UI.Button>();
            button.interactable = unlocked;
            var capturedSceneName = entry.SceneName;
            button.onClick.AddListener(() => SelectLevel(capturedSceneName));

            if (firstTile == null)
            {
                firstTile = tile;
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstTile);
    }

    private string TileSubtitle(int index, bool unlocked)
    {
        if (!unlocked)
        {
            var previous = registry.LevelEntries[index - 1];
            return $"Locked: survive {previous.Theme.TargetSeconds} s in {previous.DisplayName}";
        }

        var entry = registry.LevelEntries[index];
        var best = SaveService.BestScore(entry.SceneName);
        var goal = SaveService.IsCleared(entry.SceneName) ? "Cleared" : $"Goal {entry.Theme.TargetSeconds} s";
        return best > 0 ? $"{goal}, best {best}" : goal;
    }

    public void SelectLevel(string sceneName)
    {
        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (!string.IsNullOrEmpty(loadedLevelSceneName))
        {
            yield return SceneManager.UnloadSceneAsync(loadedLevelSceneName);
        }

        // A level scene left open in the Editor (or otherwise already loaded) would
        // otherwise stack on top of the chosen one, so only one level may be live.
        foreach (var entry in registry.LevelEntries)
        {
            if (entry.SceneName == sceneName)
            {
                continue;
            }

            var other = SceneManager.GetSceneByName(entry.SceneName);
            if (other.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(other);
            }
        }

        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        loadedLevelSceneName = sceneName;

        var levelScene = SceneManager.GetSceneByName(sceneName);
        LevelInfo levelInfo = null;
        foreach (var root in levelScene.GetRootGameObjects())
        {
            levelInfo = root.GetComponentInChildren<LevelInfo>();
            if (levelInfo != null)
            {
                break;
            }
        }

        gameManager.ApplyLevel(sceneName, levelInfo.Theme);

        foreach (var hazard in GameObject.FindGameObjectsWithTag("Hazard"))
        {
            Destroy(hazard);
        }

        foreach (var powerUp in GameObject.FindGameObjectsWithTag("PowerUp"))
        {
            Destroy(powerUp);
        }

        canvasGroup.alpha = 0f;
        menuBackground.SetActive(false);
        gameManager.Enable();
        gameObject.SetActive(false);
    }
}
