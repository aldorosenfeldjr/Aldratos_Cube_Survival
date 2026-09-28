using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Shared helpers for the UI builders that dress the game in the Hyper Casual UI Pack: sprite paths, 9-slice borders, the
// Baloo2 text style (white with a dark outline, as in the pack's own screens) and rect placement. Builders stay idempotent:
// every helper finds an existing child by name before creating one.
public static class UIPack
{
    public const string Sprites = "Assets/Hyper_Casual_UI/Sprites/";
    public const string ButtonSprites = Sprites + "Buttons/empty_buttons/";
    public const string PanelSprites = Sprites + "Panel_Sprites/";
    public const string IconSprites = Sprites + "Icons/";

    // The button pills are 50 px tall with fully rounded ends: the fixed 25 px corners stay round at any size.
    public static readonly Vector4 ButtonBorder = new Vector4(25f, 20f, 25f, 20f);

    private const string FontFolder = "Assets/Fonts";
    private const string FontSource = "Assets/Hyper_Casual_UI/Fonts/Baloo2-ExtraBold.ttf";
    private const string FontAssetPath = FontFolder + "/Baloo2 ExtraBold SDF.asset";
    private const string OutlineMaterialPath = FontFolder + "/Baloo2 Outline.mat";
    private static readonly Color OutlineColour = new Color(0.08f, 0.17f, 0.24f, 1f);

    public static readonly Color White = Color.white;
    public static readonly Color Gold = new Color(1f, 0.86f, 0.3f, 1f);
    public static readonly Color Soft = new Color(0.85f, 0.95f, 1f, 1f);

    public static Sprite Sprite(string path)
    {
        EnsureSpriteImport(path);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new System.InvalidOperationException($"Missing pack sprite: {path}");
        }
        return sprite;
    }

    // Some pack images import as plain textures; make them sprites before use.
    private static TextureImporter EnsureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return importer;
    }

    public static void SetBorder(string spritePath, Vector4 border)
    {
        var importer = EnsureSpriteImport(spritePath);
        if (importer.spriteBorder != border)
        {
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
    }

    public static void Skin(Image image, string spritePath, Color colour, Image.Type type = Image.Type.Sliced)
    {
        image.sprite = Sprite(spritePath);
        image.type = type;
        image.color = colour;
        image.enabled = true;
    }

    // ---- text ----

    private static TMP_FontAsset font;
    private static Material outline;

    public static void EnsureFont()
    {
        if (font != null && outline != null)
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(FontFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Fonts");
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontSource);
            font = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = "Baloo2 ExtraBold SDF";
            AssetDatabase.CreateAsset(font, FontAssetPath);
            font.material.name = "Baloo2 ExtraBold Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures)
            {
                atlas.name = "Baloo2 ExtraBold Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            var ascii = new System.Text.StringBuilder();
            for (var c = 32; c < 127; c++)
            {
                ascii.Append((char)c);
            }
            font.TryAddCharacters(ascii.ToString(), out _);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }

        outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        if (outline == null)
        {
            outline = new Material(font.material) { name = "Baloo2 Outline" };
            AssetDatabase.CreateAsset(outline, OutlineMaterialPath);
        }
        outline.EnableKeyword("OUTLINE_ON");
        outline.SetFloat("_OutlineWidth", 0.22f);
        outline.SetColor("_OutlineColor", OutlineColour);
        EditorUtility.SetDirty(outline);
        AssetDatabase.SaveAssets();
    }

    public static void Style(TMP_Text text, float size, Color colour, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        EnsureFont();
        text.font = font;
        text.fontSharedMaterial = outline;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.color = colour;
        text.alignment = alignment;
    }

    /// <summary>Baloo2 with the dark outline, keeping the text's own size, colour and autosizing.</summary>
    public static void StyleFont(TMP_Text text)
    {
        EnsureFont();
        text.font = font;
        text.fontSharedMaterial = outline;
    }

    // ---- rects ----

    /// <summary>Centre-anchored rect: (x, y) is the rect's centre relative to the parent's centre, y up.</summary>
    public static void PlaceCentre(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>Top-left-anchored rect: (x, y) is the rect's top-left corner, measured from the parent's top-left, y down.</summary>
    public static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x + width * 0.5f, -(y + height * 0.5f));
    }

    /// <summary>Left-middle-anchored rect: (x) is its left edge's distance from the parent's left edge.</summary>
    public static void PlaceMiddleLeft(RectTransform rect, float x, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, 0f);
    }

    /// <summary>Finds the child called <paramref name="name"/> or creates an empty UI object with that name.</summary>
    public static RectTransform Child(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            return (RectTransform)existing;
        }
        var created = new GameObject(name, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return (RectTransform)created.transform;
    }

    /// <summary>A decorative or backing image child (created once, restyled every run).</summary>
    public static Image ImageChild(Transform parent, string name, string spritePath, Color colour, Image.Type type, bool blocksRaycasts)
    {
        var rect = Child(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }
        Skin(image, spritePath, colour, type);
        image.raycastTarget = blocksRaycasts;
        image.preserveAspect = type == Image.Type.Simple;
        return image;
    }
}
