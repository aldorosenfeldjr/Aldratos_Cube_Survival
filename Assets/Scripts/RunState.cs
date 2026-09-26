/// <summary>Score and the level's best score for the current run. Plain C#, owned by <see cref="GameManager"/>. Bests live in <see cref="SaveService"/>.</summary>
public class RunState
{
    private float timer;

    public string LevelId { get; private set; }
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public int GemsCollected { get; private set; }
    public int MultiplierBonus { get; private set; }
    public bool IsNewBest => Score > HighScore;

    public void SetLevel(string levelId)
    {
        LevelId = levelId;
        RefreshHighScore();
    }

    public void Reset()
    {
        Score = 0;
        timer = 0;
        GemsCollected = 0;
        MultiplierBonus = 0;
    }

    /// <summary>Records a collected gem worth <paramref name="baseValue"/> x <paramref name="multiplier"/>; returns the gems earned.</summary>
    public int AddGem(int baseValue, int multiplier)
    {
        var earned = baseValue * multiplier;
        GemsCollected += baseValue;
        MultiplierBonus += earned - baseValue;
        return earned;
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

    /// <summary>Stores the score as the level's new best if it beats it; returns true if it did.</summary>
    public bool CommitHighScore()
    {
        if (LevelId == null || !SaveService.SetBestScore(LevelId, Score))
        {
            return false;
        }

        HighScore = Score;
        SaveService.Save();
        return true;
    }

    public void RefreshHighScore()
    {
        HighScore = LevelId == null ? 0 : SaveService.BestScore(LevelId);
    }
}
