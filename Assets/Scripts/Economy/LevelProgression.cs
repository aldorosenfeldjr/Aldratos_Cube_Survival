/// <summary>Level unlock and clear rules. The first level is always open; each next level opens when the one before it is cleared.</summary>
public static class LevelProgression
{
    public struct ClearResult
    {
        public bool FirstClear;
        public int Reward;
        public string UnlockedLevelId;
    }

    public static bool IsUnlocked(LevelRegistry registry, int index)
    {
        return index <= 0 || SaveService.IsCleared(registry.LevelEntries[index - 1].SceneName);
    }

    /// <summary>Marks the level cleared, credits the reward (big the first time, small on repeats) and saves. Returns what happened.</summary>
    public static ClearResult Clear(LevelRegistry registry, string levelId, LevelTheme theme)
    {
        var level = SaveService.Data.GetOrAddLevel(levelId);
        var result = new ClearResult { FirstClear = !level.cleared };
        result.Reward = result.FirstClear ? theme.FirstClearReward : theme.RepeatClearReward;

        level.cleared = true;
        Wallet.Add(result.Reward);
        SaveService.Save();

        if (result.FirstClear)
        {
            result.UnlockedLevelId = NextLevelId(registry, levelId);
        }
        return result;
    }

    /// <summary>Scene name of the level after <paramref name="levelId"/>, or null when it is the last.</summary>
    public static string NextLevelId(LevelRegistry registry, string levelId)
    {
        for (var i = 0; i < registry.LevelEntries.Count - 1; i++)
        {
            if (registry.LevelEntries[i].SceneName == levelId)
            {
                return registry.LevelEntries[i + 1].SceneName;
            }
        }
        return null;
    }
}
