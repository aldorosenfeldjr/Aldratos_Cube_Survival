using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Audio checks for the play smoke test (roadmap item 4). Sound itself cannot be heard here, so this proves everything around it:
// every clip exists and is neither silent nor clipping, music is playing, buttons trigger the click, mute works and is saved, and
// (in the checks next to each gameplay event) the right effect is requested. Which sound PLAYS is judged by ear, by you.
public static partial class PlaySmokeTest
{
    private static IEnumerator AudioChecks()
    {
        var audio = AudioManager.Instance;
        var library = AudioLibrary.Instance;
        Check("audio manager is in Core and the audio library is loaded", audio != null && library != null);
        if (audio == null || library == null)
        {
            yield break;
        }

        var problems = new List<string>();
        foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx)))
        {
            CheckClip(problems, sfx.ToString(), library.Clip(sfx));
        }
        CheckClip(problems, "Music", library.Music);
        Check("every sound and the music exist, are audible and do not clip", problems.Count == 0, string.Join("; ", problems));

        Check("volumes are sensible", library.SfxVolume > 0.2f && library.SfxVolume <= 1f && library.MusicVolume > 0.05f && library.MusicVolume < library.SfxVolume + 0.01f,
            $"sfx={library.SfxVolume} music={library.MusicVolume}");
        Check("music is playing on the main menu", audio.MusicPlaying);

        // Every menu button clicks: the component sits on the shared prefab, and a real button press asks for the click.
        UseFreshTempSave();
        var prefabHasClick = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MenuButton.prefab").GetComponent<ClickSound>() != null;
        var before = audio.PlayedCount;
        Click("MainMenu/Characters");
        yield return WaitUntil(() => CanvasChild("SelectionScreen").gameObject.activeInHierarchy);
        Check("pressing a menu button plays the click", prefabHasClick && audio.PlayedCount == before + 1 && audio.LastPlayed == Sfx.Click,
            $"prefab={prefabHasClick} played {before}->{audio.PlayedCount} last={audio.LastPlayed}");
        Click("SelectionScreen/TopBar/Back");
        yield return 0.2f;

        // Mute: the toggle flips it, the label shows it, the outputs go silent, and it is remembered.
        var toggle = CanvasChild("MainMenu/SoundToggle");
        Check("main menu has a Sound toggle showing Sound: On", toggle != null && toggle.GetComponentInChildren<TMP_Text>().text == "Sound: On" && !AudioManager.Muted && !audio.OutputMuted);
        if (toggle == null)
        {
            yield break;
        }

        toggle.GetComponent<Button>().onClick.Invoke();
        var label = toggle.GetComponentInChildren<TMP_Text>().text;
        var mutedNow = AudioManager.Muted && audio.OutputMuted;
        SaveService.Load();
        var mutedAfterReload = AudioManager.Muted;
        Check("Sound toggle mutes music and effects, says Sound: Off, and is saved", label == "Sound: Off" && mutedNow && mutedAfterReload, $"label={label} muted={mutedNow} reloaded={mutedAfterReload}");

        toggle.GetComponent<Button>().onClick.Invoke();
        Check("toggling again turns sound back on", toggle.GetComponentInChildren<TMP_Text>().text == "Sound: On" && !AudioManager.Muted && !audio.OutputMuted);
        UseFreshTempSave();
    }

    // Not silent (someone would wonder why), not clipping (harsh), and long enough to hear.
    private static void CheckClip(List<string> problems, string name, AudioClip clip)
    {
        if (clip == null)
        {
            problems.Add(name + ": missing");
            return;
        }

        var data = new float[clip.samples * clip.channels];
        if (!clip.GetData(data, 0))
        {
            problems.Add(name + ": cannot read samples");
            return;
        }

        float peak = 0f;
        double sumSquares = 0;
        foreach (var sample in data)
        {
            peak = Mathf.Max(peak, Mathf.Abs(sample));
            sumSquares += sample * sample;
        }
        var rms = Mathf.Sqrt((float)(sumSquares / data.Length));

        if (peak < 0.1f || rms < 0.005f)
        {
            problems.Add($"{name}: too quiet (peak {peak:0.00}, rms {rms:0.000})");
        }
        if (peak > 0.98f)
        {
            problems.Add($"{name}: clips (peak {peak:0.00})");
        }
        if (clip.length < 0.03f)
        {
            problems.Add($"{name}: too short");
        }
    }
}
