using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject hazardPrefab;
    [SerializeField]
    private int maxHazardsToSpawn = 3;
    [SerializeField]
    private TMPro.TextMeshProUGUI scoreText;
    [SerializeField]
    private Image backgroundMenu;
    [SerializeField]
    private float PauseDuration;
    [SerializeField]
    private float minHazardDrag;
    [SerializeField]
    private float maxHazardDrag;

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
    private static GameManager instance;
    public static GameManager Instance => instance;
    private const string HighScorePreferenceKey = "HighScore";
    public int HighScore => highScore;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        highScore = PlayerPrefs.GetInt(HighScorePreferenceKey);
        //highScore = 0;
    }

    private void OnEnable() 
    {
        NewRecordScreen.SetActive(false);
        player.SetActive(true);

        mainVCam.SetActive(true);
        zoomVCam.SetActive(false);

        gameOver = false;
        scoreText.text = "0";
        score = 0;
        timer = 0;

        hazardsCoroutine = StartCoroutine(SpawnHazards());
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

            timer = 0;
        }
    }

    private void Pause()
    {
        LeanTween.value(1, 0, PauseDuration)
            .setOnUpdate(SetTimeScale)
            .setIgnoreTimeScale(true);
        backgroundMenu.gameObject.SetActive(true);
    }

    private void Resume()
    {
        LeanTween.value(0, 1, PauseDuration)
            .setOnUpdate(SetTimeScale)
            .setIgnoreTimeScale(true);
        backgroundMenu.gameObject.SetActive(false);
    }

    private IEnumerator SpawnHazards()
    {
        var hazardsToSpawn = Random.Range(1, maxHazardsToSpawn);

        for (int i = 0; i < hazardsToSpawn; i++)
        {
            var x = Random.Range(-7, 7);
            var drag = Random.Range(maxHazardDrag, minHazardDrag);

            var hazard = Instantiate(hazardPrefab, new Vector3(x, 11, 0), Quaternion.identity);
            hazard.GetComponent<Rigidbody>().drag = drag;
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
}
