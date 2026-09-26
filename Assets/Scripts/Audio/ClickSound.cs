using UnityEngine;
using UnityEngine.UI;

/// <summary>On the shared MenuButton prefab, so every menu button (and every variant of it) clicks. One place, no per-menu wiring.</summary>
[RequireComponent(typeof(Button))]
public class ClickSound : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => AudioManager.Play(Sfx.Click));
    }
}
