using System;
using System.Collections.Generic;

/// <summary>Everything persisted in save.json. Plain serialisable data; <see cref="SaveService"/> owns reading and writing.</summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public int gems;
    public List<LevelSave> levels = new List<LevelSave>();
    public List<OwnedEntry> owned = new List<OwnedEntry>();
    public List<SelectedEntry> selected = new List<SelectedEntry>();

    public LevelSave FindLevel(string id)
    {
        return levels.Find(level => level.id == id);
    }

    public LevelSave GetOrAddLevel(string id)
    {
        var level = FindLevel(id);
        if (level == null)
        {
            level = new LevelSave { id = id };
            levels.Add(level);
        }
        return level;
    }
}

/// <summary>Progress for one level. The id is the level's scene name, which never changes.</summary>
[Serializable]
public class LevelSave
{
    public string id;
    public bool cleared;
    public int bestScore;
}

/// <summary>An unlockable the player owns. <c>source</c> is "gems" or "purchase".</summary>
[Serializable]
public class OwnedEntry
{
    public string id;
    public string source;
}

/// <summary>The selected item for one <see cref="UnlockCategory"/> (stored by name).</summary>
[Serializable]
public class SelectedEntry
{
    public string category;
    public string id;
}
