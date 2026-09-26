using System.IO;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Reads and writes save.json (JSON in <see cref="Application.persistentDataPath"/>). The only place that
/// touches the file. Writes go to a temp file first and then replace the old one, so a crash mid-write
/// cannot corrupt the save. On the first load with no save file, the old PlayerPrefs high score is migrated.
/// </summary>
public static class SaveService
{
    public const string LegacyHighScoreKey = "HighScore";
    public const string MigratedLevelId = "Level_Meadow";

    /// <summary>Gems a fresh save starts with in a development build on a device (never in the Editor, so tests and real play are unaffected).</summary>
    public const int DevelopmentBuildStartingGems = 10000;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void FileSync_Flush();
#endif

    private static SaveData data;
    private static string pathOverride;

    private static string SavePath => pathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");

    public static SaveData Data
    {
        get
        {
            if (data == null)
            {
                Load();
            }
            return data;
        }
    }

    /// <summary>Points the service at another file and drops the loaded data (tests use a temp file). Null restores the real path.</summary>
    public static void UseFile(string path)
    {
        pathOverride = path;
        data = null;
    }

    public static void Load()
    {
        data = null;
        var path = SavePath;
        if (File.Exists(path))
        {
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Save file unreadable, starting fresh: {e.Message}");
            }
        }

        if (data != null)
        {
            return;
        }

        data = new SaveData();
        if (PlayerPrefs.HasKey(LegacyHighScoreKey))
        {
            MigrateLegacyHighScore();
        }

        if (Debug.isDebugBuild && !Application.isEditor)
        {
            data.gems = DevelopmentBuildStartingGems;
        }
    }

    public static void Save()
    {
        var path = SavePath;
        var temp = path + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(temp, JsonUtility.ToJson(Data));
        if (File.Exists(path))
        {
            ReplaceFile(temp, path);
        }
        else
        {
            File.Move(temp, path);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        FileSync_Flush();
#endif
    }

    // File.Replace is atomic where it exists; some platforms (WebGL) do not implement it, so fall back to copy + delete.
    private static void ReplaceFile(string temp, string path)
    {
        try
        {
            File.Replace(temp, path, null);
        }
        catch (System.Exception)
        {
            File.Copy(temp, path, true);
            File.Delete(temp);
        }
    }

    public static int BestScore(string levelId)
    {
        return Data.FindLevel(levelId)?.bestScore ?? 0;
    }

    public static bool IsCleared(string levelId)
    {
        return Data.FindLevel(levelId)?.cleared ?? false;
    }

    /// <summary>Sets a level's best score if it beats the stored one; returns true if it did. Caller saves.</summary>
    public static bool SetBestScore(string levelId, int score)
    {
        var level = Data.GetOrAddLevel(levelId);
        if (score <= level.bestScore)
        {
            return false;
        }
        level.bestScore = score;
        return true;
    }

    /// <summary>Clears every level's best score; gems and clears are kept.</summary>
    public static void ClearBestScores()
    {
        foreach (var level in Data.levels)
        {
            level.bestScore = 0;
        }
        Save();
    }

    // The old global high score becomes Meadow's best. It grants no clears.
    private static void MigrateLegacyHighScore()
    {
        data.GetOrAddLevel(MigratedLevelId).bestScore = PlayerPrefs.GetInt(LegacyHighScoreKey);
        Save();
        PlayerPrefs.DeleteKey(LegacyHighScoreKey);
        PlayerPrefs.Save();
    }
}
