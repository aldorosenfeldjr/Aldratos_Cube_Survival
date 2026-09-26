using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Thin run coordinator: starts/restarts/ends a run and drives the score UI, cameras and menus.
/// Score lives in <see cref="RunState"/>, hazards in <see cref="HazardSpawner"/>,
/// time scale in <see cref="TimeScaleController"/>.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI scoreText;
    [SerializeField]
    private TMPro.TextMeshProUGUI highScoreText;
    [SerializeField]
    private Color normalScoreColor = Color.white;
    [SerializeField]
    private Color newBestScoreColor = new Color(1f, 0.4f, 0.1f);
    [SerializeField]
    private Image backgroundMenu;
    [SerializeField]
    [Tooltip("On-screen pause button (the only pause entry point on mobile). Shown only during a run.")]
    private GameObject pauseButton;

    [SerializeField]
    private HazardSpawner hazardSpawner;
    [SerializeField]
    private PowerUpSpawner powerUpSpawner;
    [SerializeField]
    private PowerUpJuiceController powerUpJuiceController;

    [SerializeField]
    private GameObject mainVCam;
    [SerializeField]
    private GameObject zoomVCam;
    [SerializeField]
    private GameObject gameOverMenu;
    [SerializeField]
    private GameObject NewRecordScreen;
    [SerializeField]
    private GameObject player;
    [SerializeField]
    private LevelRegistry registry;
    [SerializeField]
    private LevelSelect levelSelect;
    [SerializeField]
    private GameObject successMenu;

    private readonly RunState run = new RunState();
    private bool gameOver;
    private bool celebratedNewBest;
    private bool successShown;
    private bool clearedThisRun;
    private LevelTheme currentTheme;
    private static GameManager instance;
    public static GameManager Instance => instance;
    public int HighScore => run.HighScore;
    public int Score => run.Score;
    public string LevelName => currentTheme != null ? currentTheme.DisplayName : string.Empty;
    public int TargetSeconds => TargetSecondsOverride > 0 ? TargetSecondsOverride : (currentTheme != null ? currentTheme.TargetSeconds : 0);
    public LevelProgression.ClearResult LastClear { get; private set; }
    public bool HasNextLevel => LevelProgression.NextLevelId(registry, run.LevelId) != null;

    /// <summary>Test hook: when above 0, replaces the level's clear target (seconds).</summary>
    public int TargetSecondsOverride { get; set; }

    public static void ClearHighScore()
    {
        SaveService.ClearBestScores();

        if (instance != null)
        {
            instance.run.RefreshHighScore();
        }
    }

    private void Awake()
    {
        instance = this;
        successMenu.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }

    private void OnEnable()
    {
        NewRecordScreen.SetActive(false);
        player.SetActive(true);

        mainVCam.SetActive(true);
        zoomVCam.SetActive(false);

        gameOver = false;
        BeginRun();
        highScoreText.gameObject.SetActive(true);
        pauseButton.SetActive(true);
    }

    private void OnDisable()
    {
        if (highScoreText != null)
        {
            highScoreText.gameObject.SetActive(false);
        }
        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !successShown)
        {
            if (TimeScaleController.Instance.IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        if (gameOver)
            return;

        // No menu is open during play: drop UI focus so Space jumps instead of pressing the last clicked button.
        if (!TimeScaleController.Instance.IsPaused && EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (run.Tick(Time.deltaTime))
        {
            scoreText.text = run.Score.ToString();

            if (!clearedThisRun && TargetSeconds > 0 && run.Score >= TargetSeconds)
            {
                ClearLevel();
            }

            if (run.IsNewBest)
            {
                highScoreText.text = $"Best: {run.Score}";

                if (!celebratedNewBest)
                {
                    celebratedNewBest = true;
                    scoreText.color = newBestScoreColor;
                    LeanTween.scale(scoreText.gameObject, Vector3.one * 1.4f, 0.15f)
                        .setLoopPingPong(1);
                }
            }
        }
    }

    private void ClearLevel()
    {
        clearedThisRun = true;
        run.CommitHighScore();
        LastClear = LevelProgression.Clear(registry, run.LevelId, currentTheme);

        successShown = true;
        TimeScaleController.Instance.Pause();
        pauseButton.SetActive(false);
        successMenu.SetActive(true);
    }

    public void KeepGoing()
    {
        CloseSuccess();
        TimeScaleController.Instance.Resume();
        pauseButton.SetActive(true);
    }

    public void NextLevel()
    {
        var nextLevelId = LevelProgression.NextLevelId(registry, run.LevelId);
        CloseSuccess();
        hazardSpawner.StopSpawning();
        powerUpSpawner.StopSpawning();
        ClearFallingObjects();
        scoreText.transform.localScale = Vector3.one;
        player.GetComponent<Player>().ResetState();
        ResetTimeAndCamera();

        gameObject.SetActive(false);
        levelSelect.PlayLevel(nextLevelId);
    }

    private void CloseSuccess()
    {
        successShown = false;
        successMenu.SetActive(false);
    }

    private void ClearFallingObjects()
    {
        hazardSpawner.ClearAll();

        foreach (var powerUp in GameObject.FindGameObjectsWithTag("PowerUp"))
        {
            Destroy(powerUp);
        }
    }

    public void Pause()
    {
        TimeScaleController.Instance.Pause();
        ShowPauseMenu(true);
    }

    public void Resume()
    {
        TimeScaleController.Instance.Resume();
        ShowPauseMenu(false);
    }

    private void ShowPauseMenu(bool show)
    {
        backgroundMenu.gameObject.SetActive(show);
        pauseButton.SetActive(!show);
    }

    public void RestartGame()
    {
        ClearFallingObjects();

        scoreText.transform.localScale = Vector3.one;
        player.GetComponent<Player>().ResetState();
        ShowPauseMenu(false);

        BeginRun();
    }

    private void BeginRun()
    {
        run.Reset();
        celebratedNewBest = false;
        clearedThisRun = false;

        scoreText.text = "0";
        scoreText.color = normalScoreColor;
        highScoreText.text = $"Best: {run.HighScore}";

        hazardSpawner.BeginSpawning();
        powerUpSpawner.BeginSpawning();
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResetAll();
        }
        ResetTimeAndCamera();
    }

    private void ResetTimeAndCamera()
    {
        TimeScaleController.Instance.ResetAll();
        if (powerUpJuiceController != null)
        {
            powerUpJuiceController.ResetCamera();
        }
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ReturnToMainMenu()
    {
        TimeScaleController.Instance.ResetAll();
        SceneManager.LoadScene("Core", LoadSceneMode.Single);
    }

    public void GameOver()
    {
        hazardSpawner.StopSpawning();
        powerUpSpawner.StopSpawning();
        gameOver = true;
        CloseSuccess();

        ResetTimeAndCamera();

        if (run.CommitHighScore())
        {
            NewRecordScreen.SetActive(true);
        }

        mainVCam.SetActive(false);
        zoomVCam.SetActive(true);

        gameObject.SetActive(false);
        gameOverMenu.SetActive(true);
    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void ApplyLevel(string levelId, LevelTheme theme)
    {
        currentTheme = theme;
        run.SetLevel(levelId);
        hazardSpawner.ApplyTheme(theme);
        powerUpSpawner.ApplyTheme(theme);
    }
}
