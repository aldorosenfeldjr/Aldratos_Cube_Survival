using UnityEditor;
using UnityEngine;

// Configures every power-up pickup prefab from the single table below, so a visual change
// (badge size, item scale, ...) is one edit here plus Tools > PowerUps > Rebuild Pickup Prefabs,
// not six prefab edits. Prefabs are modified in place, so GUIDs and LevelTheme references survive.
//
// Layout (camera looks down +Z, so the camera-facing side is -Z):
//   root (Rigidbody, MeshCollider, PowerUpPickup)
//    └ VisualPivot            identity - pickups do not rotate
//        ├ ItemMesh           oriented so its face points at the camera
//        └ BadgeFrame         flat, larger, sits behind the item
public static class PowerUpPickupBuilder
{
    private const float ItemScale = 0.8f;
    private static readonly Vector3 BadgeScale = new Vector3(1.5f, 1.5f, 0.35f);
    private static readonly Vector3 BadgePosition = new Vector3(0f, 0f, 0.45f);

    private const string GoldBadge = "Assets/Materials/PowerUp_Badge_Gold.mat";
    private const string BlueBadge = "Assets/Materials/PowerUp_Badge_Blue.mat";
    private const string GreenBadge = "Assets/Materials/PowerUp_Badge_Green.mat";

    // Meadow items are authored lying flat (face up +Y); Playground items are already upright.
    private static readonly Vector3 FlatItem = new Vector3(-90f, 0f, 0f);
    private static readonly Vector3 UprightItem = Vector3.zero;

    private struct Entry
    {
        public string PrefabPath;
        public Vector3 ItemEuler;
        public string BadgeMaterial;

        public Entry(string prefabPath, Vector3 itemEuler, string badgeMaterial)
        {
            PrefabPath = prefabPath;
            ItemEuler = itemEuler;
            BadgeMaterial = badgeMaterial;
        }
    }

    private static readonly Entry[] Entries =
    {
        new Entry("Assets/Prefabs/PowerUp_Shield.prefab", FlatItem, BlueBadge),
        new Entry("Assets/Prefabs/PowerUp_SpeedBoost.prefab", FlatItem, GreenBadge),
        new Entry("Assets/Prefabs/PowerUp_Invincibility.prefab", FlatItem, GoldBadge),
        new Entry("Assets/Prefabs/KayKit_Shield.prefab", UprightItem, BlueBadge),
        new Entry("Assets/Prefabs/KayKit_SpeedBoost.prefab", UprightItem, GreenBadge),
        new Entry("Assets/Prefabs/KayKit_Invincibility.prefab", UprightItem, GoldBadge),
    };

    [MenuItem("Tools/PowerUps/Rebuild Pickup Prefabs")]
    public static void RebuildAll()
    {
        foreach (var entry in Entries)
        {
            Rebuild(entry);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {Entries.Length} power-up pickup prefabs.");
    }

    private static void Rebuild(Entry entry)
    {
        var root = PrefabUtility.LoadPrefabContents(entry.PrefabPath);

        var pivot = root.transform.Find("VisualPivot");
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;

        // Idle spin/bob was removed: pickups must hold still and face the camera.
        foreach (var component in pivot.GetComponents<Component>())
        {
            if (component == null || component.GetType().Name == "PowerUpIdleMotion")
            {
                Object.DestroyImmediate(component, true);
            }
        }

        var item = pivot.Find("ItemMesh");
        item.localPosition = Vector3.zero;
        item.localEulerAngles = entry.ItemEuler;
        item.localScale = Vector3.one * ItemScale;

        var badge = pivot.Find("BadgeFrame");
        badge.localPosition = BadgePosition;
        badge.localRotation = Quaternion.identity;
        badge.localScale = BadgeScale;
        badge.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(entry.BadgeMaterial);

        // A tumbling pickup would turn its face away from the camera.
        root.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

        PrefabUtility.SaveAsPrefabAsset(root, entry.PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
