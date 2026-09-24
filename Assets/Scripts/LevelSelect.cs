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

    private void OnEnable()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        foreach (Transform child in tileContainer)
        {
            Destroy(child.gameObject);
        }

        GameObject firstTile = null;
        foreach (var entry in registry.LevelEntries)
        {
            var tile = Instantiate(levelTilePrefab, tileContainer);
            var label = tile.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            label.text = entry.DisplayName;

            var capturedSceneName = entry.SceneName;
            tile.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectLevel(capturedSceneName));

            if (firstTile == null)
            {
                firstTile = tile;
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstTile);
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

        gameManager.ApplyTheme(levelInfo.Theme);

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
