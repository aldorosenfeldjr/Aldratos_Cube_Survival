using System.Collections.Generic;
using UnityEngine;

/// <summary>Every unlockable definition (Assets/Resources/UnlockCatalog.asset). Filled by the character/companion builders.</summary>
[CreateAssetMenu(fileName = "UnlockCatalog", menuName = "Game/Unlock Catalog")]
public class UnlockCatalog : ScriptableObject
{
    private static UnlockCatalog instance;

    public static UnlockCatalog Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<UnlockCatalog>("UnlockCatalog");
            }
            return instance;
        }
    }

    [SerializeField] private List<UnlockableDefinition> items = new List<UnlockableDefinition>();

    public IReadOnlyList<UnlockableDefinition> Items => items;

    public UnlockableDefinition Find(string id)
    {
        return items.Find(item => item != null && item.Id == id);
    }

    public List<UnlockableDefinition> InCategory(UnlockCategory category)
    {
        return items.FindAll(item => item != null && item.Category == category);
    }

    public UnlockableDefinition DefaultFor(UnlockCategory category)
    {
        return items.Find(item => item != null && item.Category == category && item.IsDefault);
    }
}
