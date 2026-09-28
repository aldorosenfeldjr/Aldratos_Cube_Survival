using System.IO;
using UnityEditor;
using UnityEngine;

// Renders a portrait of every catalog item (Tools > UI > Rebuild Portraits) into Assets/Portraits and assigns it as the item's
// thumbnail, so grid tiles and Main Menu teasers show the actual character or animal instead of a colour square. The portrait is
// the same preview object the selection screen builds, seen from a 3/4 angle on a transparent background, lit neutrally on a far-away
// stage. Idempotent: run it after any change to a character or companion row (their builders run it too).
public static class PortraitBuilder
{
    private const string Folder = "Assets/Portraits";
    private const int Size = 256;
    private const int RenderSize = 512; // rendered larger, then cropped and scaled down to Size
    private static readonly Vector3 StagePosition = new Vector3(0f, -3000f, 0f);

    [MenuItem("Tools/UI/Rebuild Portraits")]
    public static void RebuildAll()
    {
        var catalog = UnlockCatalog.Instance;
        if (catalog == null)
        {
            Debug.LogError("No UnlockCatalog: run the character and companion builders first.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets", "Portraits");
        }

        foreach (var item in catalog.Items)
        {
            var sprite = RenderToSprite($"{Folder}/{item.Id.Replace('.', '_')}.png", item.CreatePreview, item is CompanionDefinition ? 145f : -35f);
            var fields = new SerializedObject(item);
            fields.FindProperty("thumbnail").objectReferenceValue = sprite;
            fields.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Rendered {catalog.Items.Count} portraits into {Folder}.");
    }

    /// <summary>
    /// Renders whatever <paramref name="makeSubject"/> builds under the far-away stage (3/4 view, transparent, cropped to the subject,
    /// lit neutrally) into a PNG at <paramref name="assetPath"/>, imports it as a sprite and returns it. Also used for the power-up icons.
    /// </summary>
    public static Sprite RenderToSprite(string assetPath, System.Func<Transform, GameObject> makeSubject, float yaw)
    {
        var sun = RenderSettings.sun;
        var sunWasOn = sun != null && sun.enabled;
        var ambientMode = RenderSettings.ambientMode;
        var ambientLight = RenderSettings.ambientLight;
        try
        {
            // The scene's warm sunset light and ambient would tint the render: light it neutrally instead.
            if (sun != null)
            {
                sun.enabled = false;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            File.WriteAllBytes(assetPath, Render(makeSubject, yaw));
        }
        finally
        {
            if (sun != null)
            {
                sun.enabled = sunWasOn;
            }
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // Finds the opaque pixels and scales them to fill a square with a small margin, so a small animal and a big one both fill their tile.
    private static Texture2D CropToSubject(Texture2D source)
    {
        var pixels = source.GetPixels32();
        int minX = RenderSize, maxX = -1, minY = RenderSize, maxY = -1;
        for (var y = 0; y < RenderSize; y++)
        {
            for (var x = 0; x < RenderSize; x++)
            {
                if (pixels[y * RenderSize + x].a > 12)
                {
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }

        if (maxX < 0)
        {
            return source;
        }

        var side = Mathf.Max(maxX - minX + 1, maxY - minY + 1) * 1.1f;
        var centre = new Vector2((minX + maxX + 1) * 0.5f, (minY + maxY + 1) * 0.5f);
        source.wrapMode = TextureWrapMode.Clamp;
        var output = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var u = (centre.x + ((x + 0.5f) / Size - 0.5f) * side) / RenderSize;
                var v = (centre.y + ((y + 0.5f) / Size - 0.5f) * side) / RenderSize;
                output.SetPixel(x, y, source.GetPixelBilinear(u, v));
            }
        }
        output.Apply();
        return output;
    }

    private static byte[] Render(System.Func<Transform, GameObject> makeSubject, float yaw)
    {
        var stage = new GameObject("PortraitStage") { hideFlags = HideFlags.HideAndDontSave };
        var texture = new RenderTexture(RenderSize, RenderSize, 24, RenderTextureFormat.ARGB32);
        try
        {
            stage.transform.position = StagePosition;
            var subject = makeSubject(stage.transform);
            subject.transform.localPosition = Vector3.zero;
            subject.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var bounds = CompanionDefinition.WorldBounds(subject);
            var radius = Mathf.Max(0.01f, bounds.extents.magnitude);

            var lightObject = new GameObject("Key");
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            var key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.1f;

            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(stage.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.fieldOfView = 26f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.targetTexture = texture;
            var distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            var direction = new Vector3(0.55f, 0.4f, -1f).normalized;
            cameraObject.transform.position = bounds.center + direction * distance;
            cameraObject.transform.LookAt(bounds.center);
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(RenderSize, RenderSize, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            var framed = CropToSubject(image);
            var png = framed.EncodeToPNG();
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(framed);
            return png;
        }
        finally
        {
            Object.DestroyImmediate(stage); // first: the stage's camera still targets the texture
            texture.Release();
            Object.DestroyImmediate(texture);
        }
    }
}
