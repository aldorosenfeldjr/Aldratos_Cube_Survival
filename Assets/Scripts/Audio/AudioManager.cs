using UnityEngine;

/// <summary>
/// Plays music and sound effects. One instance lives in Core and survives scene reloads, so the music does not restart every time
/// the player returns to the menu. Game code calls the static <see cref="Play"/>, which is safe when there is no manager
/// (EditMode tests, a scene without audio). Muting is saved. Clips and volumes come from <see cref="AudioLibrary"/>.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private AudioSource musicSource;
    private AudioSource sfxSource;

    public static AudioManager Instance { get; private set; }

    /// <summary>How many effects have been played this session (state the smoke test can assert on; sound itself cannot be).</summary>
    public int PlayedCount { get; private set; }
    public Sfx LastPlayed { get; private set; }
    public bool MusicPlaying => musicSource != null && musicSource.isPlaying;
    public bool OutputMuted => musicSource.mute && sfxSource.mute;

    public static bool Muted
    {
        get => SaveService.Data.muted;
        set
        {
            SaveService.Data.muted = value;
            SaveService.Save();
            if (Instance != null)
            {
                Instance.ApplyMute();
            }
        }
    }

    /// <summary>Plays a sound effect. <paramref name="volumeScale"/> (0..1) scales it, e.g. a soft landing far from the player.</summary>
    public static void Play(Sfx sfx, float volumeScale = 1f)
    {
        if (Instance != null)
        {
            Instance.PlayEffect(sfx, volumeScale);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        var library = AudioLibrary.Instance;
        if (library != null && library.Music != null)
        {
            musicSource.clip = library.Music;
            musicSource.volume = library.MusicVolume;
            musicSource.Play();
        }
        ApplyMute();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ApplyMute()
    {
        var muted = SaveService.Data.muted;
        musicSource.mute = muted;
        sfxSource.mute = muted;
    }

    private void PlayEffect(Sfx sfx, float volumeScale)
    {
        var library = AudioLibrary.Instance;
        var clip = library != null ? library.Clip(sfx) : null;
        if (clip == null)
        {
            return;
        }

        PlayedCount++;
        LastPlayed = sfx;
        sfxSource.PlayOneShot(clip, library.SfxVolume * Mathf.Clamp01(volumeScale));
    }
}
