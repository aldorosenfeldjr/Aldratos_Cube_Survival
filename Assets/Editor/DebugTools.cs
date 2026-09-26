using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only shortcuts for testing the game by hand (Tools > Debug). They change your REAL save file, so use them in Edit Mode
// (not while playing). Nothing here ships in a build: this file lives in an Editor folder.
public static class DebugTools
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    [MenuItem("Tools/Debug/Add 10000 Gems To Save")]
    public static void AddGems()
    {
        if (RefuseWhilePlaying())
        {
            return;
        }

        SaveService.UseFile(null);
        SaveService.Load();
        Wallet.Add(10000);
        Debug.Log($"Debug: added 10000 gems. Balance is now {Wallet.Balance}. Save: {SavePath}");
    }

    [MenuItem("Tools/Debug/Reset Save (gems, unlocks, levels, mute)")]
    public static void ResetSave()
    {
        if (RefuseWhilePlaying())
        {
            return;
        }

        if (File.Exists(SavePath) && EditorUtility.DisplayDialog("Reset save?", "This deletes your gems, unlocks, level progress and best scores.", "Delete", "Cancel"))
        {
            File.Delete(SavePath);
            SaveService.UseFile(null);
            SaveService.Load();
            Debug.Log("Debug: save deleted. Next play starts from scratch.");
        }
    }

    private static bool RefuseWhilePlaying()
    {
        if (!EditorApplication.isPlaying)
        {
            return false;
        }

        Debug.LogWarning("Debug tools change the save file: stop Play Mode first.");
        return true;
    }
}
