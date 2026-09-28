using UnityEngine;

public enum UnlockCategory { Character, Companion }

public enum PriceTier { Free, Common, Rare, Epic, Legendary }

/// <summary>
/// One thing the player can own and select (a character colour, a companion). The <see cref="Id"/> is stored in
/// saves and must never be renamed. Subclasses add their own look fields and build their own preview.
/// Instances are created by the table-driven builders, one row per item, never by hand.
/// </summary>
public abstract class UnlockableDefinition : ScriptableObject
{
    [Tooltip("Stable save key, e.g. char.red or comp.cow. Never rename.")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private UnlockCategory category;
    [SerializeField] private PriceTier tier;
    [SerializeField] private Sprite thumbnail;
    [Tooltip("Owned from the start (the free box colour / cat).")]
    [SerializeField] private bool isDefault;

    public string Id => id;
    public string DisplayName => displayName;
    public UnlockCategory Category => category;
    public PriceTier Tier => tier;
    public Sprite Thumbnail => thumbnail;
    public bool IsDefault => isDefault;

    /// <summary>Store product id: the save id with dots replaced (char.red -> char_red).</summary>
    public string ProductId => id.Replace('.', '_');

    /// <summary>Height, relative to the preview stage, that a previewed item's feet stand on (the pedestal's top).</summary>
    public const float PreviewFloor = -0.8f;

    /// <summary>Colour of the tile swatch shown when there is no thumbnail.</summary>
    public virtual Color TileColor => Color.white;

    /// <summary>Builds the object the preview camera looks at, under <paramref name="parent"/>. The screen destroys it when focus moves.</summary>
    public abstract GameObject CreatePreview(Transform parent);
}
