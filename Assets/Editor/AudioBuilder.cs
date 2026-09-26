using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Placeholder audio (Tools > Audio > Rebuild Placeholder Audio): synthesises simple tones as WAV files in Assets/Audio, imports them,
// fills the empty slots of Assets/Resources/AudioLibrary.asset, puts ClickSound on the shared MenuButton prefab and adds the Sound
// toggle to the main menu. Only MISSING files and EMPTY slots are filled, so real clips dropped in later are never overwritten.
// Tools > Audio > Place Audio Manager In Core adds the manager object to the open Core scene.
public static class AudioBuilder
{
    private const string Folder = "Assets/Audio";
    private const string LibraryPath = "Assets/Resources/AudioLibrary.asset";
    private const string ButtonPath = "Assets/Prefabs/UI/MenuButton.prefab";
    private const string MainMenuPath = "Assets/Prefabs/UI/MainMenu.prefab";
    private const int Rate = 44100;

    [MenuItem("Tools/Audio/Rebuild Placeholder Audio")]
    public static void RebuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets", "Audio");
        }

        var files = new Dictionary<string, float[]>
        {
            ["click"] = Click(),
            ["gem"] = Gem(),
            ["powerup"] = PowerUp(),
            ["land"] = Land(),
            ["gameover"] = GameOver(),
            ["success"] = Success(),
            ["music"] = Music(),
        };

        foreach (var pair in files)
        {
            var path = $"{Folder}/{pair.Key}.wav";
            if (!File.Exists(path))
            {
                WriteWav(path, pair.Value);
            }
        }
        AssetDatabase.Refresh();

        foreach (var name in files.Keys)
        {
            ConfigureImport($"{Folder}/{name}.wav", name == "music");
        }

        FillLibrary();
        AddClickSoundToButtonPrefab();
        AddSoundToggleToMainMenu();
        AssetDatabase.SaveAssets();
        Debug.Log("Placeholder audio ready.");
    }

    [MenuItem("Tools/Audio/Place Audio Manager In Core")]
    public static void PlaceManager()
    {
        var existing = UnityEngine.Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        var mainMenu = UnityEngine.Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (mainMenu == null)
        {
            Debug.LogError("Open the Core scene first.");
            return;
        }

        var scene = mainMenu.gameObject.scene;
        if (existing == null)
        {
            var go = new GameObject("AudioManager");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<AudioManager>();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Audio manager placed in " + scene.name);
    }

    // --- Synthesis ---

    private static float[] Tone(float frequency, float seconds, float volume, float decay, float endFrequency = -1f)
    {
        var samples = new float[(int)(seconds * Rate)];
        var phase = 0f;
        for (var i = 0; i < samples.Length; i++)
        {
            var t = i / (float)Rate;
            var f = endFrequency > 0f ? Mathf.Lerp(frequency, endFrequency, t / seconds) : frequency;
            phase += 2f * Mathf.PI * f / Rate;
            var attack = Mathf.Clamp01(t / 0.005f);
            var envelope = attack * Mathf.Exp(-decay * t);
            samples[i] = (Mathf.Sin(phase) + 0.25f * Mathf.Sin(2f * phase)) * envelope * volume;
        }
        return samples;
    }

    private static float[] Noise(float seconds, float volume, float decay, int seed)
    {
        var random = new System.Random(seed);
        var samples = new float[(int)(seconds * Rate)];
        for (var i = 0; i < samples.Length; i++)
        {
            var t = i / (float)Rate;
            samples[i] = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-decay * t) * volume;
        }
        return samples;
    }

    // Adds a clip into a buffer starting at a time offset (notes may overlap).
    private static void Mix(float[] buffer, float[] clip, float atSeconds)
    {
        var start = (int)(atSeconds * Rate);
        for (var i = 0; i < clip.Length && start + i < buffer.Length; i++)
        {
            buffer[start + i] += clip[i];
        }
    }

    private static float[] Sequence(float seconds, params (float at, float[] clip)[] notes)
    {
        var buffer = new float[(int)(seconds * Rate)];
        foreach (var (at, clip) in notes)
        {
            Mix(buffer, clip, at);
        }
        return buffer;
    }

    private static float[] Click() => Tone(1000f, 0.05f, 0.45f, 60f);

    private static float[] Gem() => Sequence(0.35f, (0f, Tone(1318.5f, 0.2f, 0.4f, 14f)), (0.07f, Tone(1760f, 0.28f, 0.4f, 12f)));

    private static float[] PowerUp() => Sequence(0.5f,
        (0f, Tone(523.25f, 0.18f, 0.35f, 9f)), (0.07f, Tone(659.25f, 0.18f, 0.35f, 9f)),
        (0.14f, Tone(783.99f, 0.18f, 0.35f, 9f)), (0.21f, Tone(1046.5f, 0.3f, 0.35f, 8f)));

    private static float[] Land() => Sequence(0.3f, (0f, Tone(120f, 0.25f, 0.6f, 14f, 45f)), (0f, Noise(0.12f, 0.22f, 30f, 7)));

    private static float[] GameOver() => Sequence(1.1f,
        (0f, Tone(392f, 0.35f, 0.4f, 6f)), (0.28f, Tone(311.1f, 0.35f, 0.4f, 6f)),
        (0.56f, Tone(261.6f, 0.35f, 0.4f, 6f)), (0.84f, Tone(196f, 0.6f, 0.4f, 4f)));

    private static float[] Success() => Sequence(1.2f,
        (0f, Tone(523.25f, 0.25f, 0.35f, 6f)), (0.14f, Tone(659.25f, 0.25f, 0.35f, 6f)),
        (0.28f, Tone(783.99f, 0.25f, 0.35f, 6f)), (0.42f, Tone(1046.5f, 0.7f, 0.4f, 3f)));

    // 8 bars of Am F C G x2 at 96 BPM (20 s): a soft arpeggio over a bass note. Every note decays to silence so the loop seam is clean.
    private static float[] Music()
    {
        var beat = 60f / 96f;
        var bar = beat * 4f;
        var eighth = beat / 2f;
        var chords = new[]
        {
            new[] { 220f, 261.63f, 329.63f }, // Am
            new[] { 174.61f, 220f, 261.63f }, // F
            new[] { 261.63f, 329.63f, 392f },  // C
            new[] { 196f, 246.94f, 293.66f }, // G
        };
        var buffer = new float[(int)(bar * 8f * Rate)];
        for (var b = 0; b < 8; b++)
        {
            var chord = chords[b % 4];
            var barStart = b * bar;
            Mix(buffer, Tone(chord[0] / 2f, bar * 0.95f, 0.22f, 1.2f), barStart);
            var pattern = new[] { 0, 1, 2, 1, 2, 1, 0, 1 };
            for (var n = 0; n < pattern.Length; n++)
            {
                Mix(buffer, Tone(chord[pattern[n]] * 2f, eighth * 1.6f, 0.13f, 5f), barStart + n * eighth);
            }
        }
        return buffer;
    }

    private static void WriteWav(string path, float[] samples)
    {
        using (var stream = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            var dataSize = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(Rate);
            writer.Write(Rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);
            foreach (var sample in samples)
            {
                writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }
    }

    // --- Assets ---

    private static void ConfigureImport(string path, bool music)
    {
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        // Both decompress on load: a 20 s mono track is ~1.7 MB in memory, and readable samples let the smoke test verify every clip.
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
        settings.quality = 0.6f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    private static void FillLibrary()
    {
        var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<AudioLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        var fields = new SerializedObject(library);
        foreach (var (property, file) in new[]
        {
            ("music", "music"), ("click", "click"), ("gem", "gem"), ("powerUp", "powerup"),
            ("land", "land"), ("gameOver", "gameover"), ("success", "success"),
        })
        {
            var slot = fields.FindProperty(property);
            if (slot.objectReferenceValue == null)
            {
                slot.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{file}.wav");
            }
        }
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(library);
    }

    private static void AddClickSoundToButtonPrefab()
    {
        var contents = PrefabUtility.LoadPrefabContents(ButtonPath);
        try
        {
            if (contents.GetComponent<ClickSound>() == null)
            {
                contents.AddComponent<ClickSound>();
                PrefabUtility.SaveAsPrefabAsset(contents, ButtonPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void AddSoundToggleToMainMenu()
    {
        var contents = PrefabUtility.LoadPrefabContents(MainMenuPath);
        try
        {
            var toggle = contents.transform.Find("SoundToggle");
            if (toggle == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), contents.transform);
                instance.name = "SoundToggle";
                var component = instance.AddComponent<SoundToggle>();
                var label = instance.GetComponentInChildren<TextMeshProUGUI>();
                var fields = new SerializedObject(component);
                fields.FindProperty("label").objectReferenceValue = label;
                fields.ApplyModifiedPropertiesWithoutUndo();
                UnityEventTools.AddPersistentListener(instance.GetComponent<Button>().onClick, component.Toggle);
                toggle = instance.transform;
            }

            // Top-left, mirroring the gem counter on the top-right.
            var rect = (RectTransform)toggle;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -30f);
            rect.sizeDelta = new Vector2(340f, 60f);

            PrefabUtility.SaveAsPrefabAsset(contents, MainMenuPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
