using UnityEditor;
using UnityEngine;

// Builds every power-up pickup prefab from the single table below, so a visual change is one edit here plus
// Tools > PowerUps > Rebuild Pickup Prefabs. Prefabs are modified in place (GUIDs, LevelTheme references and the
// PowerUpPickup definition survive). The same models are rendered into the HUD icons, so pickup and HUD always match.
//
// A pickup is a small glossy collectable, not a crate: one KayKit shape per kind (bolt, star, heart, diamond), tinted
// with its own emissive material, a soft glow halo behind it and a few sparkles. It holds still and faces the camera
// (camera looks down +Z, so the camera-facing side is -Z).
//   root (Rigidbody, SphereCollider, PowerUpPickup, AutoDestroyer)
//    └ VisualPivot          identity - pickups do not rotate
//        ├ ItemMesh         the shape, fitted to ItemSize
//        ├ Halo             camera-facing glow sprite behind the item
//        └ Sparkles         small looping particles
public static class PowerUpPickupBuilder
{
    private const float ItemSize = 0.75f;        // longest edge of the shape, in world units
    private const float HaloSize = 1.35f;
    private const float ColliderRadius = 0.42f;
    private const float PickupLifetime = 12f;    // uncollected pickups despawn after this many seconds
    private const string MaterialFolder = "Assets/PowerUps/Materials";
    private const string IconFolder = "Assets/PowerUps/Icons";
    private const string KayKit = "Assets/KayKit_Platformer_Pack/fbx(unity)";

    private enum Kind { Shield, SpeedBoost, Invincibility, GemMultiplier }

    private struct Look
    {
        public string Model;
        public Color Colour;
        public Look(string model, Color colour)
        {
            Model = model;
            Colour = colour;
        }
    }

    private static Look LookOf(Kind kind)
    {
        switch (kind)
        {
            case Kind.SpeedBoost: return new Look($"{KayKit}/green/power_green.fbx", new Color(0.25f, 0.9f, 0.35f));
            case Kind.Invincibility: return new Look($"{KayKit}/yellow/star_yellow.fbx", new Color(1f, 0.8f, 0.15f));
            case Kind.Shield: return new Look($"{KayKit}/red/heart_red.fbx", new Color(1f, 0.3f, 0.4f));
            default: return new Look($"{KayKit}/blue/diamond_blue.fbx", new Color(0.75f, 0.35f, 1f));
        }
    }

    private static readonly (string PrefabPath, Kind Kind)[] Entries =
    {
        ("Assets/Prefabs/PowerUp_Shield.prefab", Kind.Shield),
        ("Assets/Prefabs/PowerUp_SpeedBoost.prefab", Kind.SpeedBoost),
        ("Assets/Prefabs/PowerUp_Invincibility.prefab", Kind.Invincibility),
        ("Assets/Prefabs/KayKit_Shield.prefab", Kind.Shield),
        ("Assets/Prefabs/KayKit_SpeedBoost.prefab", Kind.SpeedBoost),
        ("Assets/Prefabs/KayKit_Invincibility.prefab", Kind.Invincibility),
        ("Assets/Prefabs/PowerUp_GemMultiplier.prefab", Kind.GemMultiplier),
        ("Assets/Prefabs/KayKit_GemMultiplier.prefab", Kind.GemMultiplier),
    };

    private static string DefinitionPath(Kind kind) => kind == Kind.GemMultiplier ? "Assets/PowerUps/GemMultiplier.asset" : $"Assets/PowerUps/{kind}.asset";

    [MenuItem("Tools/PowerUps/Rebuild Pickup Prefabs")]
    public static void RebuildAll()
    {
        var halo = EnsureGlowSprite();
        foreach (Kind kind in System.Enum.GetValues(typeof(Kind)))
        {
            var material = BuildMaterial(kind);
            RenderIcon(kind, material);
        }

        foreach (var (path, kind) in Entries)
        {
            Rebuild(path, kind, halo);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {Entries.Length} power-up pickup prefabs and 4 HUD icons.");
    }

    // Soft white radial falloff (no ring), tinted per pickup by the SpriteRenderer colour.
    private static Sprite EnsureGlowSprite()
    {
        const string path = "Assets/UI/SoftGlow.png";
        if (!System.IO.File.Exists(path))
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    var a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Material BuildMaterial(Kind kind)
    {
        var path = $"{MaterialFolder}/Collectable_{kind}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        var colour = LookOf(kind).Colour;
        material.SetColor("_BaseColor", colour);
        material.SetFloat("_Smoothness", 0.85f);
        material.SetFloat("_Metallic", 0.1f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", colour * 0.18f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject BuildItem(Transform parent, Kind kind, Material material)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(LookOf(kind).Model);
        var item = (GameObject)Object.Instantiate(model, parent);
        item.name = "ItemMesh";
        foreach (var renderer in item.GetComponentsInChildren<Renderer>())
        {
            var materials = new Material[renderer.sharedMaterials.Length];
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }

        // Fit the longest edge to ItemSize and centre on the origin, whatever the model's own scale and pivot.
        item.transform.localScale = Vector3.one;
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        var bounds = CompanionDefinition.WorldBounds(item);
        var longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        item.transform.localScale = Vector3.one * (ItemSize / Mathf.Max(0.0001f, longest));
        bounds = CompanionDefinition.WorldBounds(item);
        item.transform.position += parent.position - bounds.center;
        return item;
    }

    private static void RenderIcon(Kind kind, Material material)
    {
        var sprite = PortraitBuilder.RenderToSprite($"{IconFolder}/{kind}_Icon.png", stage => BuildItem(stage, kind, material), 0f);
        var definition = AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(DefinitionPath(kind));
        var fields = new SerializedObject(definition);
        fields.FindProperty("icon").objectReferenceValue = sprite;
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
    }

    private static void Rebuild(string path, Kind kind, Sprite haloSprite)
    {
        var root = PrefabUtility.LoadPrefabContents(path);

        // Start from a clean root: the old badge-and-crate visual and any old colliders go.
        foreach (var child in new System.Collections.Generic.List<Transform>(GetChildren(root.transform)))
        {
            Object.DestroyImmediate(child.gameObject);
        }
        foreach (var collider in root.GetComponents<Collider>())
        {
            Object.DestroyImmediate(collider, true);
        }

        var pivot = new GameObject("VisualPivot").transform;
        pivot.SetParent(root.transform, false);

        var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Collectable_{kind}.mat");
        BuildItem(pivot, kind, material);
        BuildHalo(pivot, haloSprite, LookOf(kind).Colour);
        BuildSparkles(pivot, LookOf(kind).Colour);

        var sphere = root.AddComponent<SphereCollider>();
        sphere.radius = ColliderRadius;

        var autoDestroyer = root.GetComponent<AutoDestroyer>();
        if (autoDestroyer == null)
        {
            autoDestroyer = root.AddComponent<AutoDestroyer>();
        }
        var destroyerSettings = new SerializedObject(autoDestroyer);
        destroyerSettings.FindProperty("delay").floatValue = PickupLifetime;
        destroyerSettings.ApplyModifiedPropertiesWithoutUndo();

        // A tumbling pickup would turn its face away from the camera.
        root.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static System.Collections.Generic.IEnumerable<Transform> GetChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            yield return child;
        }
    }

    private static void BuildHalo(Transform pivot, Sprite sprite, Color colour)
    {
        var halo = new GameObject("Halo");
        halo.transform.SetParent(pivot, false);
        halo.transform.localPosition = new Vector3(0f, 0f, 0.12f); // just behind the item, away from the camera
        var renderer = halo.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(colour.r, colour.g, colour.b, 0.6f);
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MenuBokeh.mat") ?? renderer.sharedMaterial;
        halo.transform.localScale = Vector3.one * (HaloSize / Mathf.Max(0.01f, sprite.bounds.size.x));
    }

    private static void BuildSparkles(Transform pivot, Color colour)
    {
        var sparkles = new GameObject("Sparkles");
        sparkles.transform.SetParent(pivot, false);
        var system = sparkles.AddComponent<ParticleSystem>();
        var main = system.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 0.9f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main.startColor = Color.Lerp(colour, Color.white, 0.6f);
        main.maxParticles = 12;
        var emission = system.emission;
        emission.rateOverTime = 7f;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.32f;
        var over = system.sizeOverLifetime;
        over.enabled = true;
        over.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f)));
        var particles = sparkles.GetComponent<ParticleSystemRenderer>();
        particles.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MenuBokeh.mat");
    }
}
