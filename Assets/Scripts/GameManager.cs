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

    private RunState run;
    private bool gameOver;
    private bool celebratedNewBest;
    private static GameManager instance;
    public static GameManager Instance => instance;
    public int HighScore => run.HighScore;
    public int Score => run.Score;

    public static void ClearHighScore()
    {
        RunState.DeleteSavedHighScore();

        if (instance != null)
        {
            instance.run.ClearHighScore();
        }
    }

    private void Awake()
    {
        instance = this;
        run = new RunState();
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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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
        hazardSpawner.ClearAll();

        foreach (var powerUp in GameObject.FindGameObjectsWithTag("PowerUp"))
        {
            Destroy(powerUp);
        }

        scoreText.transform.localScale = Vector3.one;
        player.GetComponent<Player>().ResetState();
        ShowPauseMenu(false);

        BeginRun();
    }

    private void BeginRun()
    {
        run.Reset();
        celebratedNewBest = false;

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

    public void ApplyTheme(LevelTheme theme)
    {
        hazardSpawner.ApplyTheme(theme);
        powerUpSpawner.ApplyTheme(theme);
    }
}
