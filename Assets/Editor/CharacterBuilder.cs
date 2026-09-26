using UnityEditor;
using UnityEngine;

// Table-driven builder for character unlockables (Tools > Characters > Rebuild Character Assets).
// One row per character: creates its material (a copy of the Player material, recoloured), its
// CharacterDefinition asset, and lists it in Assets/Resources/UnlockCatalog.asset. Idempotent: existing
// assets are updated in place. The ids are stored in saves: never rename one once it has shipped.
// Current rows are placeholders for testing the selection screen; the real 12-colour set replaces them
// (roadmap item 2) by editing this table.
public static class CharacterBuilder
{
    private const string Folder = "Assets/Characters";
    private const string CatalogPath = "Assets/Resources/UnlockCatalog.asset";
    private const string PlayerMaterialPath = "Assets/Materials/Player.mat";

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
        new Row { Id = "char.yellow", Name = "Sunny", Tier = PriceTier.Rare, Color = new Color(0.98f, 0.78f, 0.1f) },
    };

    [MenuItem("Tools/Characters/Rebuild Character Assets")]
    public static void RebuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets", "Characters");
        }

        var catalog = LoadOrCreateCatalog();
        var catalogFields = new SerializedObject(catalog);
        var items = catalogFields.FindProperty("items");
        items.ClearArray();

        foreach (var row in Rows)
        {
            var definition = BuildDefinition(row);
            items.InsertArrayElementAtIndex(items.arraySize);
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = definition;
        }

        catalogFields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {Rows.Length} character rows and the unlock catalog.");
    }

    private static UnlockCatalog LoadOrCreateCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<UnlockCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<UnlockCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        return catalog;
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
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }
}
