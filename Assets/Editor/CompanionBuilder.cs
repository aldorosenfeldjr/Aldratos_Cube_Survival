using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Table-driven builder for companion unlockables (Tools > Companions > Rebuild Companion Assets).
// One row per companion: loops its imported clips, builds an Animator controller (Idle_A, Idle_B and, when the model has them,
// Walk and Run: the names CatWanderer plays), a prefab (model + Animator), its CompanionDefinition, and lists it in the unlock
// catalog. Idempotent. A new companion is a table row; ids are stored in saves: never rename one that has shipped.
// The animal models are the Quaternius "Farm Animals Animated" pack (CC0), copied into Assets/Companions/Models.
public static class CompanionBuilder
{
    private const string Folder = "Assets/Companions";
    private const string Models = Folder + "/Models/";
    private const string CatModel = "Assets/Lowpoly Toon Cat Lite/Model/Cat Lite.fbx";
    private const string CatController = "Assets/Lowpoly Toon Cat Lite/Model/Animator/Cat_Lite_AC 1.controller";

    private struct Row
    {
        public string Id;
        public string Name;
        public PriceTier Tier;
        public string Model;
        public string Controller; // null: generated from the model's clips
        public float WorldSize;
        public bool Wanders;
        public Color Tint;
        public bool IsDefault;
    }

    // Only models with Walk and Run clips wander; the rest idle in place. The cat is the free default (the one already in the game).
    private static readonly Row[] Rows =
    {
        new Row { Id = "comp.cat", Name = "Kitty", Tier = PriceTier.Free, Model = CatModel, Controller = CatController, WorldSize = 0.52f, Wanders = true, Tint = new Color(0.8f, 0.6f, 0.4f), IsDefault = true },
        new Row { Id = "comp.pug", Name = "Pug", Tier = PriceTier.Rare, Model = Models + "Pug.fbx", WorldSize = 0.6f, Tint = new Color(0.85f, 0.75f, 0.55f) },
        new Row { Id = "comp.pig", Name = "Piggy", Tier = PriceTier.Rare, Model = Models + "Pig.fbx", WorldSize = 0.8f, Tint = new Color(0.95f, 0.65f, 0.7f) },
        new Row { Id = "comp.sheep", Name = "Woolly", Tier = PriceTier.Rare, Model = Models + "Sheep.fbx", WorldSize = 0.8f, Tint = new Color(0.92f, 0.92f, 0.9f) },
        new Row { Id = "comp.cow", Name = "Daisy", Tier = PriceTier.Epic, Model = Models + "Cow.fbx", WorldSize = 1.0f, Wanders = true, Tint = new Color(0.4f, 0.3f, 0.25f) },
        new Row { Id = "comp.llama", Name = "Llama", Tier = PriceTier.Epic, Model = Models + "Llama.fbx", WorldSize = 1.0f, Tint = new Color(0.85f, 0.8f, 0.65f) },
        new Row { Id = "comp.horse", Name = "Bolt", Tier = PriceTier.Legendary, Model = Models + "Horse.fbx", WorldSize = 1.2f, Wanders = true, Tint = new Color(0.55f, 0.35f, 0.2f) },
        new Row { Id = "comp.zebra", Name = "Stripes", Tier = PriceTier.Legendary, Model = Models + "Zebra.fbx", WorldSize = 1.2f, Wanders = true, Tint = new Color(0.2f, 0.2f, 0.22f) },
    };

    [MenuItem("Tools/Companions/Rebuild Companion Assets")]
    public static void RebuildAll()
    {
        var definitions = new List<UnlockableDefinition>();
        foreach (var row in Rows)
        {
            if (row.Controller == null)
            {
                EnsureLoopingClips(row.Model);
            }

            var controller = row.Controller != null
                ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(row.Controller)
                : BuildController(row);
            var prefab = BuildPrefab(row, controller);
            definitions.Add(BuildDefinition(row, prefab));
        }

        UnlockCatalogWriter.SetCategory(UnlockCategory.Companion, definitions);
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {Rows.Length} companion rows in the unlock catalog.");
    }

    [MenuItem("Tools/Companions/Place Companion Spawner In Core")]
    public static void PlaceSpawner()
    {
        var spawner = Object.FindAnyObjectByType<CompanionSpawner>(FindObjectsInactive.Include);
        var cat = Object.FindAnyObjectByType<CatWanderer>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            var position = cat != null ? cat.transform.position : new Vector3(-3.7f, 0.05f, -2.65f);
            var go = new GameObject("CompanionSpawner");
            go.transform.position = position;
            spawner = go.AddComponent<CompanionSpawner>();
            if (cat != null)
            {
                SceneManagerMoveToScene(go, cat.gameObject);
            }
        }

        if (cat != null)
        {
            // The hand-placed cat is now the default companion prefab: remove the scene copy.
            var scene = cat.gameObject.scene;
            Object.DestroyImmediate(cat.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            EditorSceneManager.SaveScene(spawner.gameObject.scene);
        }
        Debug.Log("Companion spawner placed; scene cat removed.");
    }

    private static void SceneManagerMoveToScene(GameObject go, GameObject sameSceneAs)
    {
        if (go.scene != sameSceneAs.scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, sameSceneAs.scene);
        }
    }

    // Imported clips do not loop by default; idle, walk and run must.
    private static void EnsureLoopingClips(string modelPath)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        var changed = importer.clipAnimations == null || importer.clipAnimations.Length == 0;
        foreach (var clip in clips)
        {
            var wantLoop = ShortName(clip.name) is "Idle" or "Walk" or "WalkSlow" or "Run";
            if (clip.loopTime != wantLoop)
            {
                clip.loopTime = wantLoop;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    private static string ShortName(string clipName)
    {
        var bar = clipName.LastIndexOf('|');
        return bar >= 0 ? clipName.Substring(bar + 1) : clipName;
    }

    private static AnimationClip FindClip(string modelPath, string shortName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__") && ShortName(clip.name) == shortName);
    }

    private static AnimatorController BuildController(Row row)
    {
        var path = $"{Folder}/{row.Id.Replace('.', '_')}.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;

        var idle = FindClip(row.Model, "Idle");
        machine.defaultState = machine.AddState("Idle_A");
        machine.defaultState.motion = idle;
        machine.AddState("Idle_B").motion = idle;
        if (row.Wanders)
        {
            machine.AddState("Walk").motion = FindClip(row.Model, "Walk");
            machine.AddState("Run").motion = FindClip(row.Model, "Run");
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject BuildPrefab(Row row, RuntimeAnimatorController controller)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(row.Model);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        var animator = instance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(row.Model).OfType<Avatar>().FirstOrDefault();
        animator.applyRootMotion = false;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var saved = PrefabUtility.SaveAsPrefabAsset(instance, $"{Folder}/{row.Id.Replace('.', '_')}.prefab");
        Object.DestroyImmediate(instance);
        return saved;
    }

    private static CompanionDefinition BuildDefinition(Row row, GameObject prefab)
    {
        var path = $"{Folder}/{row.Id.Replace('.', '_')}.asset";
        var definition = AssetDatabase.LoadAssetAtPath<CompanionDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<CompanionDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        var fields = new SerializedObject(definition);
        fields.FindProperty("id").stringValue = row.Id;
        fields.FindProperty("displayName").stringValue = row.Name;
        fields.FindProperty("category").enumValueIndex = (int)UnlockCategory.Companion;
        fields.FindProperty("tier").enumValueIndex = (int)row.Tier;
        fields.FindProperty("isDefault").boolValue = row.IsDefault;
        fields.FindProperty("prefab").objectReferenceValue = prefab;
        fields.FindProperty("worldSize").floatValue = row.WorldSize;
        fields.FindProperty("wanders").boolValue = row.Wanders;
        fields.FindProperty("tileColor").colorValue = row.Tint;
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }
}
