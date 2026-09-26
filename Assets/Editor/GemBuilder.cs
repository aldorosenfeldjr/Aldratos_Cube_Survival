using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the gem assets: the octahedron mesh, gem material, HUD icon, the falling Gem pickup prefab, the
// Gem Multiplier power-up definition and its two themed pickup prefabs (copies of the Shield pickups with
// the gem look). Idempotent: existing assets are updated in place. Menu: Tools > Gems > Rebuild Gem Assets.
// The pickup prefab layout is then normalised by PowerUpPickupBuilder (which has rows for the two new prefabs).
public static class GemBuilder
{
    private const string Folder = "Assets/Gems";
    private const string MeshPath = Folder + "/Gem.asset";
    private const string MaterialPath = Folder + "/Gem.mat";
    private const string PrefabPath = "Assets/Prefabs/Gem.prefab";
    private const string IconPath = "Assets/PowerUps/Icons/GemMultiplier_Icon.png";
    private const string DefinitionPath = "Assets/PowerUps/GemMultiplier.asset";
    private const string PurpleBadgePath = "Assets/Materials/PowerUp_Badge_Purple.mat";
    private static readonly Color GemColor = new Color(0.25f, 0.85f, 1f);

    [MenuItem("Tools/Gems/Rebuild Gem Assets")]
    public static void RebuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets", "Gems");
        }

        var mesh = BuildMesh();
        var material = BuildMaterial(MaterialPath, GemColor, true);
        BuildMaterial(PurpleBadgePath, new Color(0.65f, 0.3f, 1f), false);
        BuildPickupPrefab(mesh, material);
        var icon = BuildIcon();
        BuildDefinition(icon);

        foreach (var (source, copy) in new[]
        {
            ("Assets/Prefabs/PowerUp_Shield.prefab", "Assets/Prefabs/PowerUp_GemMultiplier.prefab"),
            ("Assets/Prefabs/KayKit_Shield.prefab", "Assets/Prefabs/KayKit_GemMultiplier.prefab"),
        })
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(copy) == null)
            {
                AssetDatabase.CopyAsset(source, copy);
            }
            ConfigureMultiplierPrefab(copy, mesh, material);
        }

        AssetDatabase.SaveAssets();
        PowerUpPickupBuilder.RebuildAll();
        Debug.Log("Rebuilt gem assets.");
    }

    private static Mesh BuildMesh()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = "Gem" };
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }

        // Octahedron with flat-shaded faces (3 own vertices per triangle).
        const float r = 0.35f, h = 0.5f;
        Vector3[] corners = { new Vector3(0, h, 0), new Vector3(0, -h, 0), new Vector3(r, 0, 0), new Vector3(-r, 0, 0), new Vector3(0, 0, r), new Vector3(0, 0, -r) };
        int[][] faces =
        {
            new[] { 0, 4, 2 }, new[] { 0, 3, 4 }, new[] { 0, 5, 3 }, new[] { 0, 2, 5 },
            new[] { 1, 2, 4 }, new[] { 1, 4, 3 }, new[] { 1, 3, 5 }, new[] { 1, 5, 2 },
        };
        var vertices = new Vector3[faces.Length * 3];
        var triangles = new int[faces.Length * 3];
        for (var f = 0; f < faces.Length; f++)
        {
            for (var v = 0; v < 3; v++)
            {
                vertices[f * 3 + v] = corners[faces[f][v]];
                triangles[f * 3 + v] = f * 3 + v;
            }
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static Material BuildMaterial(string path, Color color, bool emissive)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.85f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.6f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildPickupPrefab(Mesh mesh, Material material)
    {
        var root = new GameObject("Gem");
        root.tag = "PowerUp";
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterial = material;
        root.transform.localScale = Vector3.one * 1.2f;
        var body = root.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;
        root.AddComponent<SphereCollider>().radius = 0.4f;
        root.AddComponent<GemPickup>();
        var destroyer = new SerializedObject(root.AddComponent<AutoDestroyer>());
        destroyer.FindProperty("delay").floatValue = 12f;
        destroyer.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    private static Sprite BuildIcon()
    {
        if (!File.Exists(IconPath))
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Mathf.Abs(x - size / 2f) / (size * 0.32f) + Mathf.Abs(y - size / 2f) / (size * 0.46f);
                    var shade = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((size / 2f - y) / (size / 2f) + 0.3f));
                    texture.SetPixel(x, y, d <= 1f ? new Color(GemColor.r * shade, GemColor.g * shade, GemColor.b * shade, 1f) : Color.clear);
                }
            }
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(IconPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
    }

    private static void BuildDefinition(Sprite icon)
    {
        var definition = AssetDatabase.LoadAssetAtPath<GemMultiplierDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<GemMultiplierDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }
        var fields = new SerializedObject(definition);
        fields.FindProperty("displayName").stringValue = "Gem Multiplier";
        fields.FindProperty("icon").objectReferenceValue = icon;
        fields.FindProperty("duration").floatValue = 10f;
        fields.FindProperty("multiplier").intValue = 2;
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
    }

    private static void ConfigureMultiplierPrefab(string path, Mesh mesh, Material material)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        var pickup = new SerializedObject(root.GetComponent<PowerUpPickup>());
        pickup.FindProperty("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(DefinitionPath);
        pickup.ApplyModifiedPropertiesWithoutUndo();

        var item = root.transform.Find("VisualPivot/ItemMesh");
        item.GetComponent<MeshFilter>().sharedMesh = mesh;
        item.GetComponent<MeshRenderer>().sharedMaterials = new[] { material };
        foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true))
        {
            collider.sharedMesh = mesh;
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
