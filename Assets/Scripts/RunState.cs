using UnityEngine;

/// <summary>Score and high score for the current run. Plain C#, owned by <see cref="GameManager"/>.</summary>
public class RunState
{
    private const string HighScorePreferenceKey = "HighScore";

    private float timer;

    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public bool IsNewBest => Score > HighScore;

    public RunState()
    {
        HighScore = PlayerPrefs.GetInt(HighScorePreferenceKey);
    }

    public void Reset()
    {
        Score = 0;
        timer = 0;
    }

    /// <summary>Advances the run clock; returns true when the score went up.</summary>
    public bool Tick(float deltaTime)
    {
        timer += deltaTime;
        if (timer < 1f)
        {
            return false;
        }

        Score++;
        timer = 0;
        return true;
    }

    /// <summary>Stores the score as the new high score if it beats it; returns true if it did.</summary>
    public bool CommitHighScore()
    {
        if (!IsNewBest)
        {
            return false;
        }

        HighScore = Score;
        PlayerPrefs.SetInt(HighScorePreferenceKey, HighScore);
        return true;
    }

    public void ClearHighScore()
    {
        HighScore = 0;
    }

    public static void DeleteSavedHighScore()
    {
        PlayerPrefs.DeleteKey(HighScorePreferenceKey);
        PlayerPrefs.Save();
    }
}
