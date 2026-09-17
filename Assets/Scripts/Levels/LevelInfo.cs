// Assets/Scripts/Levels/LevelInfo.cs
using UnityEngine;

public class LevelInfo : MonoBehaviour
{
    [SerializeField]
    private LevelTheme theme;

    public LevelTheme Theme => theme;
}
