using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Shared by the character and companion builders: replaces one category's entries in the unlock catalog and
// keeps every other category untouched, so each builder can be run on its own.
public static class UnlockCatalogWriter
{
    private const string CatalogPath = "Assets/Resources/UnlockCatalog.asset";

    public static void SetCategory(UnlockCategory category, IReadOnlyList<UnlockableDefinition> definitions)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<UnlockCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<UnlockCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var kept = new List<UnlockableDefinition>();
        foreach (var item in catalog.Items)
        {
            if (item != null && item.Category != category)
            {
                kept.Add(item);
            }
        }

        var fields = new SerializedObject(catalog);
        var items = fields.FindProperty("items");
        items.ClearArray();
        foreach (var item in kept)
        {
            items.InsertArrayElementAtIndex(items.arraySize);
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = item;
        }
        foreach (var definition in definitions)
        {
            items.InsertArrayElementAtIndex(items.arraySize);
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = definition;
        }
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }
}
