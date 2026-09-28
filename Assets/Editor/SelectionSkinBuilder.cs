using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dresses the selection UI in the Hyper Casual UI Pack (Tools > UI > Apply Selection Skin; Rebuild Selection Screen runs it too).
// Table-driven and idempotent: give the pack sprites their 9-slice borders, then put one sprite per button role on the two
// selection screens and one panel sprite on their grid panels and on the Main Menu teasers. Change a sprite in the tables and run it
// again; never hand-edit the images. Only the selection UI is skinned: the shared MenuButton / MenuLabel prefabs are untouched.
public static class SelectionSkinBuilder
{
    private const string Pack = "Assets/Hyper_Casual_UI/Sprites/";
    private const string ButtonSprites = Pack + "Buttons/empty_buttons/";
    private const string PanelSprite = Pack + "Panel_Sprites/Rectangle 357.png";
    private const string TeaserPrefab = "Assets/Prefabs/UI/SelectionTeaser.prefab";

    private static readonly string[] ScreenPrefabs =
    {
        "Assets/Prefabs/UI/SelectionScreen.prefab",
        "Assets/Prefabs/UI/CompanionSelection.prefab",
    };

    // Child path inside a screen prefab -> button sprite. Green selects, orange spends gems, blue buys or tries, grey is neutral.
    private static readonly (string Path, string Sprite)[] Buttons =
    {
        ("TopBar/Back", ButtonSprites + "GREY.png"),
        ("InfoPanel/Select", ButtonSprites + "green.png"),
        ("InfoPanel/Unlock", ButtonSprites + "orange.png"),
        ("InfoPanel/Buy", ButtonSprites + "cyan.png"),
        ("InfoPanel/Try", ButtonSprites + "light blue.png"),
        ("InfoPanel/Restore", ButtonSprites + "GREY.png"),
    };

    // The Main Menu's own buttons (size and position too: this table is the one place they are set). FontSize < 0 keeps the prefab's.
    private const string MainMenuPrefab = "Assets/Prefabs/UI/MainMenu.prefab";
    private static readonly (string Child, string Sprite, Vector2 Size, Vector2 Position, float FontSize)[] MenuButtons =
    {
        ("Play", ButtonSprites + "green.png", new Vector2(420f, 100f), new Vector2(0f, 10f), 64f),
        ("Exit", ButtonSprites + "GREY.png", new Vector2(300f, 64f), new Vector2(0f, -100f), -1f),
        ("ClearHighScore", ButtonSprites + "GREY.png", new Vector2(320f, 50f), new Vector2(0f, -170f), -1f),
        ("RemoveAds", ButtonSprites + "orange.png", new Vector2(420f, 64f), new Vector2(0f, -245f), -1f),
    };

    // The button pills are 50 px tall with fully rounded ends: keep both ends intact when a button stretches sideways.
    private static readonly Vector4 ButtonBorder = new Vector4(25f, 0f, 25f, 0f);
    private static readonly Vector4 PanelBorder = new Vector4(24f, 24f, 24f, 24f);
    // The panel sprite is a light lilac; multiplied by this it becomes a deep purple that white tile names read clearly on.
    private static readonly Color PanelTint = new Color(0.5f, 0.42f, 0.72f, 0.94f);

    [MenuItem("Tools/UI/Apply Selection Skin")]
    public static void ApplyAll()
    {
        foreach (var (_, sprite) in Buttons)
        {
            SetBorder(sprite, ButtonBorder);
        }
        SetBorder(PanelSprite, PanelBorder);

        foreach (var path in ScreenPrefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var (child, sprite) in Buttons)
                {
                    Skin(root.transform.Find(child).GetComponent<Image>(), sprite, Color.white);
                }
                Skin(root.transform.Find("GridPanel").GetComponent<Image>(), PanelSprite, PanelTint);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        var teaser = PrefabUtility.LoadPrefabContents(TeaserPrefab);
        try
        {
            Skin(teaser.GetComponent<Image>(), PanelSprite, PanelTint);
            PrefabUtility.SaveAsPrefabAsset(teaser, TeaserPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(teaser);
        }

        foreach (var (_, sprite, _, _, _) in MenuButtons)
        {
            SetBorder(sprite, ButtonBorder);
        }
        var menu = PrefabUtility.LoadPrefabContents(MainMenuPrefab);
        try
        {
            foreach (var (child, sprite, size, position, fontSize) in MenuButtons)
            {
                var button = (RectTransform)menu.transform.Find(child);
                Skin(button.GetComponent<Image>(), sprite, Color.white);
                button.sizeDelta = size;
                button.anchoredPosition = position;
                if (fontSize > 0f)
                {
                    button.GetComponentInChildren<TMPro.TextMeshProUGUI>().fontSize = fontSize;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(menu, MainMenuPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(menu);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied the Hyper Casual UI Pack skin to the selection screens, the teasers and the Main Menu buttons.");
    }

    private static void SetBorder(string spritePath, Vector4 border)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
        if (importer.spriteBorder != border)
        {
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
    }

    private static void Skin(Image image, string spritePath, Color color)
    {
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        image.type = Image.Type.Sliced;
        image.color = color;
        image.enabled = true;
    }
}
