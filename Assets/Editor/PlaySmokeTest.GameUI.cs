using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Checks for the in-game UI skin and the power-up collectables: the HUD, the Pause / Game Over / Success / Level Select screens wear
// pack sprites with sensible sizes and fit every screen, the power-up rows have their shrinking bar, and every pickup is a small
// collectable (not a crate) that can still be caught. Structure only: how it LOOKS is judged on a screenshot against the pack.
public static partial class PlaySmokeTest
{
    private static readonly string[] MenuRoots = { "PauseMenu", "GameOverMenu", "SuccessMenu", "LevelSelect" };
    private static readonly string[] MenuPanels = { "PauseMenu/Panel", "GameOverMenu/Panel", "GameOverMenu/TrialPanel/Panel", "SuccessMenu/Panel", "LevelSelect/Panel" };

    private static bool IsPackSprite(Sprite sprite)
    {
        return sprite != null && AssetDatabase.GetAssetPath(sprite).StartsWith("Assets/Hyper_Casual_UI/");
    }

    private static void GameUIChecks()
    {
        var problems = new List<string>();

        foreach (var path in MenuPanels)
        {
            var image = CanvasChild(path)?.GetComponent<Image>();
            if (image == null || !IsPackSprite(image.sprite) || image.type != Image.Type.Sliced || image.sprite.border == Vector4.zero)
            {
                problems.Add(path);
            }
        }
        foreach (var root in MenuRoots)
        {
            var menu = CanvasChild(root);
            if (menu == null)
            {
                problems.Add(root + " missing");
                continue;
            }
            foreach (var button in menu.GetComponentsInChildren<Button>(true))
            {
                var rect = (RectTransform)button.transform;
                var image = button.GetComponent<Image>();
                if (!IsPackSprite(image.sprite) || rect.rect.width > 600f || rect.rect.height > 130f)
                {
                    problems.Add($"{root}/{button.name} {rect.rect.size}");
                }
            }
        }
        Check("in-game menus wear the pack's panels and buttons, none oversized", problems.Count == 0, string.Join(" | ", problems));

        problems.Clear();
        foreach (var path in new[] { "Score/Panel", "Score/BestSlot", "Score/GemSlot" })
        {
            var image = CanvasChild(path)?.GetComponent<Image>();
            if (image == null || !IsPackSprite(image.sprite))
            {
                problems.Add(path);
            }
        }
        var pauseImage = CanvasChild("PauseButton")?.GetComponent<Image>();
        if (pauseImage == null || !IsPackSprite(pauseImage.sprite) || pauseImage.color.a < 0.9f)
        {
            problems.Add("PauseButton");
        }
        var hud = UnityEngine.Object.FindAnyObjectByType<PowerUpHUD>(FindObjectsInactive.Include);
        var rowPrefab = hud != null ? new SerializedObject(hud).FindProperty("iconPrefab").objectReferenceValue as PowerUpHUDIcon : null;
        var rowImage = rowPrefab != null ? rowPrefab.GetComponent<Image>() : null;
        var track = rowPrefab != null ? new SerializedObject(rowPrefab).FindProperty("barTrack").objectReferenceValue : null;
        if (rowImage == null || !IsPackSprite(rowImage.sprite) || track == null)
        {
            problems.Add("power-up row (slot or bar track)");
        }
        Check("HUD: score card, best and gem chips, pause tile and power-up rows wear the pack", problems.Count == 0, string.Join(" | ", problems));

        problems.Clear();
        foreach (var size in new[] { new Vector2(1920f, 866f), new Vector2(1440f, 1000f), new Vector2(1000f, 1500f), new Vector2(760f, 1900f), new Vector2(720f, 1280f) })
        {
            // The canvas scaler's match-0.5 rule (reference 1920 x 866): screen size in canvas units.
            var scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(size.x / 1920f, 2f), Mathf.Log(size.y / 866f, 2f), 0.5f));
            var units = size / scale;
            foreach (var path in MenuPanels)
            {
                var rect = CanvasChild(path) as RectTransform;
                if (rect != null && (rect.rect.width > units.x - 16f || rect.rect.height > units.y - 16f))
                {
                    problems.Add($"{path} {rect.rect.size} on {size.x}x{size.y} ({units.x:0}x{units.y:0})");
                }
            }
        }
        Check("menu panels fit every screen size", problems.Count == 0, string.Join(" | ", problems));

        problems.Clear();
        var pickups = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (contents.GetComponent<PowerUpPickup>() == null)
                {
                    continue;
                }
                pickups++;
                var bounds = new Bounds();
                var hasMesh = false;
                foreach (var renderer in contents.GetComponentsInChildren<MeshRenderer>())
                {
                    bounds = hasMesh ? Encapsulate(bounds, renderer.bounds) : renderer.bounds;
                    hasMesh = true;
                }
                var largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                var sphere = contents.GetComponent<SphereCollider>();
                if (!hasMesh || largest > 0.95f || sphere == null || sphere.radius > 0.6f || contents.GetComponent<MeshCollider>() != null
                    || contents.GetComponentInChildren<SpriteRenderer>() == null || contents.GetComponent<Rigidbody>() == null)
                {
                    problems.Add($"{path} size={largest:0.00} sphere={(sphere != null ? sphere.radius.ToString("0.00") : "none")}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
        Check("every power-up pickup is a small collectable with a sphere collider and a glow", pickups >= 8 && problems.Count == 0, $"pickups={pickups} {string.Join(" | ", problems)}");

        problems.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:PowerUpDefinition"))
        {
            var definition = AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            var icon = definition.Icon;
            if (icon == null || icon.rect.width < 200f || !AssetDatabase.GetAssetPath(icon).StartsWith("Assets/PowerUps/Icons/"))
            {
                problems.Add(definition.name);
            }
        }
        Check("HUD power-up icons are the collectable renders", problems.Count == 0, string.Join(" | ", problems));
    }

    private static Bounds Encapsulate(Bounds bounds, Bounds other)
    {
        bounds.Encapsulate(other);
        return bounds;
    }
}
