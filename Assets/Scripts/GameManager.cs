using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject hazardPrefab;
    [SerializeField]
    private int maxHazardsToSpawn = 3;
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
    private float PauseDuration;
    [SerializeField]
    private float minHazardDrag;
    [SerializeField]
    private float maxHazardDrag;

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
//    [SerializeField] 
//    private GameObject NewRecord;
    [SerializeField]
    private GameObject player;
    private int highScore;
    private int score;
    private float timer;
    private Coroutine hazardsCoroutine;
    private bool gameOver;
    private bool celebratedNewBest;
    private int pauseTweenId = -1;
    private static GameManager instance;
    public static GameManager Instance => instance;
    private const string HighScorePreferenceKey = "HighScore";
    public int HighScore => highScore;
    public int Score => score;

    public static void ClearHighScore()
    {
        PlayerPrefs.DeleteKey(HighScorePreferenceKey);
        PlayerPrefs.Save();

        if (instance != null)
        {
            instance.highScore = 0;
        }
    }

    private void Awake()
    {
        instance = this;

        highScore = PlayerPrefs.GetInt(HighScorePreferenceKey);
        //highScore = 0;
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
        score = 0;
        timer = 0;
        celebratedNewBest = false;

        scoreText.text = "0";
        scoreText.color = normalScoreColor;
        highScoreText.text = $"Best: {highScore}";
        highScoreText.gameObject.SetActive(true);

        hazardsCoroutine = StartCoroutine(SpawnHazards());

        powerUpSpawner.BeginSpawning();
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResetAll();
        }
        if (powerUpJuiceController != null)
        {
            powerUpJuiceController.ForceResetTimeScale();
        }
    }

    private void OnDisable()
    {
        if (highScoreText != null)
        {
            highScoreText.gameObject.SetActive(false);
        }
    }

    private void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.timeScale == 0)
            {
                Resume();
            }
            if (Time.timeScale == 1)
            {
                Pause();
            }
        }

        if (gameOver)
            return;
        
        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            score++;
            scoreText.text = score.ToString();

            if (score > highScore)
            {
                highScoreText.text = $"Best: {score}";

                if (!celebratedNewBest)
                {
                    celebratedNewBest = true;
                    scoreText.color = newBestScoreColor;
                    LeanTween.scale(scoreText.gameObject, Vector3.one * 1.4f, 0.15f)
                        .setLoopPingPong(1);
                }
            }

            timer = 0;
        }
    }

    private void Pause()
    {
        LeanTween.cancel(pauseTweenId);

        // A power-up slowdown tween mid-flight would otherwise keep fighting this
        // pause tween for ownership of Time.timeScale; reset it to the known "1"
        // starting point this tween assumes before taking over.
        if (powerUpJuiceController != null)
        {
            powerUpJuiceController.ForceResetTimeScale();
        }

        pauseTweenId = LeanTween.value(1, 0, PauseDuration)
            .setOnUpdate(SetTimeScale)
            .setIgnoreTimeScale(true)
            .id;
        backgroundMenu.gameObject.SetActive(true);
    }

    public void Resume()
    {
        LeanTween.cancel(pauseTweenId);
        pauseTweenId = LeanTween.value(0, 1, PauseDuration)
            .setOnUpdate(SetTimeScale)
            .setIgnoreTimeScale(true)
            .id;
        backgroundMenu.gameObject.SetActive(false);
    }

    /// <summary>
    /// Cancels this manager's in-flight pause/resume timescale tween without touching
    /// anything else, so another system (e.g. <see cref="PowerUpJuiceController"/>)
    /// can take ownership of <see cref="Time.timeScale"/> without the two fighting.
    /// </summary>
    public void CancelPauseTween()
    {
        LeanTween.cancel(pauseTweenId);
        pauseTweenId = -1;
    }

    public void RestartGame()
    {
        foreach (var hazard in GameObject.FindGameObjectsWithTag("Hazard"))
        {
            Destroy(hazard);
        }

        foreach (var powerUp in GameObject.FindGameObjectsWithTag("PowerUp"))
        {
            Destroy(powerUp);
        }

        if (hazardsCoroutine != null)
        {
            StopCoroutine(hazardsCoroutine);
        }

        score = 0;
        timer = 0;
        celebratedNewBest = false;

        scoreText.text = "0";
        scoreText.color = normalScoreColor;
        scoreText.transform.localScale = Vector3.one;
        highScoreText.text = $"Best: {highScore}";

        player.GetComponent<Player>().ResetState();

        hazardsCoroutine = StartCoroutine(SpawnHazards());

        powerUpSpawner.BeginSpawning();
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResetAll();
        }

        // Unconditionally clear any in-flight/stuck pause tween and hide the pause
        // overlay BEFORE forcing the timescale reset below. This must run before
        // ForceResetTimeScale(): doing it after would leave the old "if (Time.timeScale
        // < 1) Resume();" guard unreachable (ForceResetTimeScale already forces
        // Time.timeScale to 1) while Pause()'s tween keeps writing toward 0 afterward
        // with nothing left to counter it and the overlay still visible.
        //
        // Deliberately NOT calling Resume() here: Resume() starts its own new 0->1
        // tween, which would keep animating (and briefly dragging Time.timeScale back
        // down) for up to PauseDuration seconds after this method returns, fighting
        // the instant reset ForceResetTimeScale() is about to perform. A direct
        // cancel + hide gives an immediate, non-animated recovery instead.
        CancelPauseTween();
        backgroundMenu.gameObject.SetActive(false);

        if (powerUpJuiceController != null)
        {
            powerUpJuiceController.ForceResetTimeScale();
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
        Time.timeScale = 1f;
        SceneManager.LoadScene("Core", LoadSceneMode.Single);
    }

    private IEnumerator SpawnHazards()
    {
        var hazardsToSpawn = Random.Range(1, maxHazardsToSpawn);

        for (int i = 0; i < hazardsToSpawn; i++)
        {
            var x = Random.Range(-7, 7);
            var drag = Random.Range(maxHazardDrag, minHazardDrag);

            var hazard = Instantiate(hazardPrefab, new Vector3(x, 11, 0), Quaternion.identity);
            hazard.GetComponent<Rigidbody>().linearDamping = drag;
        }
        

        yield return new WaitForSeconds(1f);

        yield return SpawnHazards();
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
        Time.fixedDeltaTime = 0.02f * value;
    }

    public void GameOver()
    {
        StopCoroutine(hazardsCoroutine);
        powerUpSpawner.StopSpawning();
        gameOver = true;

        if (Time.timeScale < 1)
        {
            Resume();
        }

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScorePreferenceKey, highScore);
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
        hazardPrefab = theme.HazardPrefab;
        powerUpSpawner.ApplyTheme(theme);
    }
}
