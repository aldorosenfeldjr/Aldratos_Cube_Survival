using UnityEngine;

public enum Sfx { Click, Gem, PowerUp, Land, GameOver, Success }

/// <summary>
/// Every sound in the game and its volume (Assets/Resources/AudioLibrary.asset), read via <see cref="Instance"/>. The one place to
/// swap a clip or change a level. The shipped clips are generated placeholders (Tools > Audio > Rebuild Placeholder Audio only
/// fills EMPTY slots and missing files, so real clips assigned here are never overwritten).
/// </summary>
[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Game/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    private static AudioLibrary instance;

    public static AudioLibrary Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<AudioLibrary>("AudioLibrary");
            }
            return instance;
        }
    }

    [Header("Music")]
    [SerializeField] private AudioClip music;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

    [Header("Sound effects")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip gem;
    [SerializeField] private AudioClip powerUp;
    [SerializeField] private AudioClip land;
    [SerializeField] private AudioClip gameOver;
    [SerializeField] private AudioClip success;

    public AudioClip Music => music;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    public AudioClip Clip(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Click: return click;
            case Sfx.Gem: return gem;
            case Sfx.PowerUp: return powerUp;
            case Sfx.Land: return land;
            case Sfx.GameOver: return gameOver;
            case Sfx.Success: return success;
            default: return null;
        }
    }
}
