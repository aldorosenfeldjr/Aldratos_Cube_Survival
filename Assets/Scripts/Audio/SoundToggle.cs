using TMPro;
using UnityEngine;

/// <summary>A button that mutes and unmutes all sound. The label always shows the current state.</summary>
public class SoundToggle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable()
    {
        Refresh();
    }

    public void Toggle()
    {
        AudioManager.Muted = !AudioManager.Muted;
        Refresh();
    }

    private void Refresh()
    {
        label.text = AudioManager.Muted ? "Sound: Off" : "Sound: On";
    }
}
