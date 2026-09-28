using UnityEditor;
using UnityEngine;

// Table-driven builder for character unlockables (Tools > Characters > Rebuild Character Assets).
// One row per character: creates its material (a copy of the Player material, recoloured), its
// CharacterDefinition asset, and lists it in Assets/Resources/UnlockCatalog.asset. Idempotent: existing
// assets are updated in place. The ids are stored in saves: never rename one once it has shipped.
// The set: 1 free default + 11 unlockable colours (4 Common, 4 Rare, 3 Epic = 20,400 gems on PC). Add or change a
// character by editing this table and running the menu item; never hand-edit the assets.
public static class CharacterBuilder
{
    private const string Folder = "Assets/Characters";
    private const string PlayerMaterialPath = "Assets/Materials/Player.mat";
    private const string PlayerMeshPath = "Assets/Platformer Pack/FBX/YellowBox.fbx";

    private struct Row
    {
        public string Id;
        public string Name;
        public PriceTier Tier;
        public Color Color;
        public bool IsDefault;
    }

    // The default row keeps the existing Player material (the current blue box).
    private static readonly Row[] Rows =
    {
        new Row { Id = "char.blue", Name = "Boxy", Tier = PriceTier.Free, IsDefault = true },
        new Row { Id = "char.red", Name = "Ruby", Tier = PriceTier.Common, Color = new Color(0.82f, 0.13f, 0.16f) },
        new Row { Id = "char.yellow", Name = "Sunny", Tier = PriceTier.Common, Color = new Color(0.98f, 0.78f, 0.1f) },
        new Row { Id = "char.green", Name = "Minty", Tier = PriceTier.Common, Color = new Color(0.25f, 0.75f, 0.42f) },
        new Row { Id = "char.orange", Name = "Peachy", Tier = PriceTier.Common, Color = new Color(0.98f, 0.52f, 0.2f) },
        new Row { Id = "char.purple", Name = "Grape", Tier = PriceTier.Rare, Color = new Color(0.5f, 0.27f, 0.75f) },
        new Row { Id = "char.pink", Name = "Bubblegum", Tier = PriceTier.Rare, Color = new Color(0.96f, 0.45f, 0.7f) },
        new Row { Id = "char.teal", Name = "Lagoon", Tier = PriceTier.Rare, Color = new Color(0.1f, 0.7f, 0.72f) },
        new Row { Id = "char.white", Name = "Snowy", Tier = PriceTier.Rare, Color = new Color(0.94f, 0.95f, 0.97f) },
        new Row { Id = "char.black", Name = "Midnight", Tier = PriceTier.Epic, Color = new Color(0.08f, 0.09f, 0.16f) },
        new Row { Id = "char.gold", Name = "Goldie", Tier = PriceTier.Epic, Color = new Color(0.85f, 0.65f, 0.13f) },
        new Row { Id = "char.lava", Name = "Lava", Tier = PriceTier.Epic, Color = new Color(0.9f, 0.22f, 0.05f) },
    };

    [MenuItem("Tools/Characters/Rebuild Character Assets")]
    public static void RebuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets", "Characters");
        }

        var definitions = new System.Collections.Generic.List<UnlockableDefinition>();
        foreach (var row in Rows)
        {
            definitions.Add(BuildDefinition(row));
        }

        UnlockCatalogWriter.SetCategory(UnlockCategory.Character, definitions);
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {Rows.Length} character rows in the unlock catalog.");
    }

    private static Material BuildMaterial(Row row)
    {
        if (row.IsDefault)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
        }

        var path = $"{Folder}/{row.Id.Replace('.', '_')}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            AssetDatabase.CopyAsset(PlayerMaterialPath, path);
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        material.SetColor("_BaseColor", row.Color);
        material.SetColor("_Color", row.Color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static CharacterDefinition BuildDefinition(Row row)
    {
        var path = $"{Folder}/{row.Id.Replace('.', '_')}.asset";
        var definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        var fields = new SerializedObject(definition);
        fields.FindProperty("id").stringValue = row.Id;
        fields.FindProperty("displayName").stringValue = row.Name;
        fields.FindProperty("category").enumValueIndex = (int)UnlockCategory.Character;
        fields.FindProperty("tier").enumValueIndex = (int)row.Tier;
        fields.FindProperty("isDefault").boolValue = row.IsDefault;
        fields.FindProperty("material").objectReferenceValue = BuildMaterial(row);
        fields.FindProperty("previewMesh").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Mesh>(PlayerMeshPath);
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }
}
