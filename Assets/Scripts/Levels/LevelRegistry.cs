// Assets/Scripts/Levels/LevelRegistry.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelRegistry", menuName = "Levels/Level Registry")]
public class LevelRegistry : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [SerializeField]
        private string sceneName;
        [SerializeField]
        private string displayName;
        [SerializeField]
        private LevelTheme theme;

        public string SceneName => sceneName;
        public string DisplayName => displayName;
        public LevelTheme Theme => theme;
    }

    [SerializeField]
    private List<Entry> levelEntries = new List<Entry>();

    public IReadOnlyList<Entry> LevelEntries => levelEntries;
}
