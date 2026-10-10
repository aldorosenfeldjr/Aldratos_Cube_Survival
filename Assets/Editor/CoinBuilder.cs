using UnityEditor;
using UnityEngine;

// Builds the Coin Multiplier power-up: its definition asset and its two themed pickup prefabs (copies of the Shield
// pickups, retargeted to the Coin Multiplier definition). Idempotent: existing assets are updated in place.
// Menu: Tools > Coins > Rebuild Coin Assets. The pickup look (model, colour, halo, collider) is then applied by
// PowerUpPickupBuilder, which also renders the HUD icon from the same model.
public static class CoinBuilder
{
    private const string DefinitionPath = "Assets/PowerUps/CoinMultiplier.asset";

    [MenuItem("Tools/Coins/Rebuild Coin Assets")]
    public static void RebuildAll()
    {
        BuildDefinition();

        foreach (var (source, copy) in new[]
        {
            ("Assets/Prefabs/PowerUp_Shield.prefab", "Assets/Prefabs/PowerUp_CoinMultiplier.prefab"),
            ("Assets/Prefabs/KayKit_Shield.prefab", "Assets/Prefabs/KayKit_CoinMultiplier.prefab"),
        })
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(copy) == null)
            {
                AssetDatabase.CopyAsset(source, copy);
            }
            ConfigureMultiplierPrefab(copy);
        }

        AssetDatabase.SaveAssets();
        PowerUpPickupBuilder.RebuildAll();
        Debug.Log("Rebuilt coin assets.");
    }

    private static void BuildDefinition()
    {
        var definition = AssetDatabase.LoadAssetAtPath<CoinMultiplierDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<CoinMultiplierDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }
        var fields = new SerializedObject(definition);
        fields.FindProperty("displayName").stringValue = "Coin Multiplier";
        fields.FindProperty("duration").floatValue = 10f;
        fields.FindProperty("multiplier").intValue = 2;
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
    }

    private static void ConfigureMultiplierPrefab(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        var pickup = new SerializedObject(root.GetComponent<PowerUpPickup>());
        pickup.FindProperty("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(DefinitionPath);
        pickup.ApplyModifiedPropertiesWithoutUndo();

        // The look (shape, halo, collider) is applied afterwards by PowerUpPickupBuilder.
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
