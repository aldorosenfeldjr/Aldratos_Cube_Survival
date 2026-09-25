using UnityEditor;
using UnityEngine;

// Configures every hazard prefab from the single table below, so the shared hazard rules
// (tag, rigidbody, breaking effect, collider kind) live in one place. New hazard: make the
// prefab with its mesh + material, add a row, run Tools > Hazards > Rebuild Hazard Prefabs.
// Prefabs are modified in place, so GUIDs and LevelTheme references survive. Anything already
// correct is left untouched (an existing hand-fitted collider is kept).
public static class HazardBuilder
{
    public enum ColliderKind
    {
        Box,
        ConvexMesh,
    }

    private const string HazardTag = "Hazard";
    private const string BreakingEffect = "Assets/Prefabs/CrateBreakingEffect.prefab";

    private static readonly (string PrefabPath, ColliderKind Collider)[] Entries =
    {
        ("Assets/Prefabs/Crate.prefab", ColliderKind.Box),
        ("Assets/Prefabs/KayKit_Hazard.prefab", ColliderKind.ConvexMesh),
    };

    [MenuItem("Tools/Hazards/Rebuild Hazard Prefabs")]
    public static void RebuildAll()
    {
        foreach (var entry in Entries)
        {
            Configure(entry.PrefabPath, entry.Collider);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {Entries.Length} hazard prefabs.");
    }

    public static void Configure(string prefabPath, ColliderKind colliderKind)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);

        root.tag = HazardTag;

        var body = GetOrAdd<Rigidbody>(root);
        body.mass = 1f;
        body.linearDamping = 0f; // HazardSpawner sets the real drag per spawn
        body.angularDamping = 0.05f;
        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.constraints = RigidbodyConstraints.None;

        EnsureCollider(root, colliderKind);

        var hazard = GetOrAdd<Hazard>(root);
        var settings = new SerializedObject(hazard);
        settings.FindProperty("breakingEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ParticleSystem>(BreakingEffect);
        settings.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void EnsureCollider(GameObject root, ColliderKind kind)
    {
        var existing = root.GetComponent<Collider>();
        var correct = kind == ColliderKind.Box
            ? existing is BoxCollider
            : existing is MeshCollider meshCollider && meshCollider.convex && meshCollider.sharedMesh != null;
        if (correct)
        {
            return;
        }

        if (existing != null)
        {
            Object.DestroyImmediate(existing, true);
        }

        var mesh = root.GetComponent<MeshFilter>().sharedMesh;
        if (kind == ColliderKind.Box)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;
        }
        else
        {
            var convex = root.AddComponent<MeshCollider>();
            convex.sharedMesh = mesh;
            convex.convex = true;
        }
    }
}
