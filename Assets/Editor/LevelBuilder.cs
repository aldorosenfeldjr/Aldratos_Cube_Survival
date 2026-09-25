using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Keeps a level's three bookkeeping assets in step from the single table below: its LevelTheme
// asset, its LevelRegistry entry and its Build Settings scene entry. New level: author the scene
// (with a LevelInfo pointing at the theme asset this creates), add a row, run
// Tools > Levels > Rebuild Level Assets. Only ever adds or updates; never removes or reorders,
// and Build Settings is only written when a scene is missing from it. Read-only check: warns if
// the level scene does not reference its theme.
public static class LevelBuilder
{
    public class Row
    {
        public string DisplayName;
        public string SceneName;
        public string Hazard;
        public string SpeedBoost;
        public string Invincibility;
        public string Shield;
    }

    private const string ThemeFolder = "Assets/Levels";
    private const string RegistryPath = "Assets/Levels/LevelRegistry.asset";
    private const string SceneFolder = "Assets/Scenes";

    private static readonly Row[] Rows =
    {
        new Row
        {
            DisplayName = "Meadow", SceneName = "Level_Meadow",
            Hazard = "Assets/Prefabs/Crate.prefab",
            SpeedBoost = "Assets/Prefabs/PowerUp_SpeedBoost.prefab",
            Invincibility = "Assets/Prefabs/PowerUp_Invincibility.prefab",
            Shield = "Assets/Prefabs/PowerUp_Shield.prefab",
        },
        new Row
        {
            DisplayName = "Playground", SceneName = "Level_Playground",
            Hazard = "Assets/Prefabs/KayKit_Hazard.prefab",
            SpeedBoost = "Assets/Prefabs/KayKit_SpeedBoost.prefab",
            Invincibility = "Assets/Prefabs/KayKit_Invincibility.prefab",
            Shield = "Assets/Prefabs/KayKit_Shield.prefab",
        },
    };

    [MenuItem("Tools/Levels/Rebuild Level Assets")]
    public static void RebuildAll()
    {
        var notes = new List<string>();
        foreach (var row in Rows)
        {
            Build(row, notes);
        }

        AssetDatabase.SaveAssets();
        var summary = $"Rebuilt {Rows.Length} level(s).";
        if (notes.Count == 0)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary + "\n  " + string.Join("\n  ", notes));
        }
    }

    public static void Build(Row row, List<string> notes)
    {
        var themePath = $"{ThemeFolder}/LevelTheme_{row.DisplayName}.asset";
        var theme = AssetDatabase.LoadAssetAtPath<LevelTheme>(themePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<LevelTheme>();
            AssetDatabase.CreateAsset(theme, themePath);
        }

        var themeFields = new SerializedObject(theme);
        themeFields.FindProperty("displayName").stringValue = row.DisplayName;
        foreach (var (field, path) in new[]
        {
            ("hazardPrefab", row.Hazard), ("speedBoostPrefab", row.SpeedBoost),
            ("invincibilityPrefab", row.Invincibility), ("shieldPrefab", row.Shield),
        })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                notes.Add($"{row.DisplayName}: prefab missing at {path}");
            }
            themeFields.FindProperty(field).objectReferenceValue = prefab;
        }
        if (themeFields.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(theme);
        }

        var scenePath = $"{SceneFolder}/{row.SceneName}.unity";
        if (!File.Exists(scenePath))
        {
            notes.Add($"{row.DisplayName}: scene {scenePath} does not exist yet; theme built, registry and Build Settings skipped");
            return;
        }

        EnsureRegistryEntry(row);
        EnsureInBuildSettings(scenePath);

        var themeGuid = AssetDatabase.AssetPathToGUID(themePath);
        if (!File.ReadAllText(scenePath).Contains(themeGuid))
        {
            notes.Add($"{row.DisplayName}: scene {row.SceneName} has no LevelInfo pointing at {themePath}");
        }
    }

    private static void EnsureRegistryEntry(Row row)
    {
        var registry = AssetDatabase.LoadAssetAtPath<LevelRegistry>(RegistryPath);
        var registryFields = new SerializedObject(registry);
        var entries = registryFields.FindProperty("levelEntries");

        SerializedProperty entry = null;
        for (var i = 0; i < entries.arraySize; i++)
        {
            if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("sceneName").stringValue == row.SceneName)
            {
                entry = entries.GetArrayElementAtIndex(i);
                break;
            }
        }

        if (entry == null)
        {
            entries.arraySize++;
            entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("sceneName").stringValue = row.SceneName;
        }

        entry.FindPropertyRelative("displayName").stringValue = row.DisplayName;
        if (registryFields.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(registry);
        }
    }

    private static void EnsureInBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == scenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
