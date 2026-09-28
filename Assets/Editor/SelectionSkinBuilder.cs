using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dresses the selection UI and the Main Menu in the Hyper Casual UI Pack (Tools > UI > Apply Selection Skin; Rebuild Selection Screen
// runs it too). Table-driven and idempotent: give the pack sprites their 9-slice borders, then put one sprite per role on the two
// selection screens (card panel, tiles, buttons, gem chip, back tile), the Main Menu teasers and the Main Menu buttons. Change a
// sprite or number in the tables and run it again; never hand-edit the images. The in-game HUD and menus are GameUIBuilder's.
public static class SelectionSkinBuilder
{
    private static readonly string Popup = UIPack.PanelSprites + "Level popup.png";
    private static readonly string HudPanel = UIPack.PanelSprites + "HUD Pannel (1).png";
    private static readonly string SlotTeal = UIPack.PanelSprites + "Rectangle 363.png";
    private static readonly string SlotDark = UIPack.PanelSprites + "Rectangle 359.png";
    private static readonly string BackIcon = UIPack.IconSprites + "Back (1).png";
    private static readonly string CoinIcon = UIPack.IconSprites + "coin.png";
    private const string TeaserPrefab = "Assets/Prefabs/UI/SelectionTeaser.prefab";
    private const string TilePrefab = "Assets/Prefabs/UI/UnlockTile.prefab";
    private const string GemCounterPrefab = "Assets/Prefabs/UI/GemCounter.prefab";
    private const string MainMenuPrefab = "Assets/Prefabs/UI/MainMenu.prefab";

    private static readonly string[] ScreenPrefabs =
    {
        "Assets/Prefabs/UI/SelectionScreen.prefab",
        "Assets/Prefabs/UI/CompanionSelection.prefab",
    };

    // Child path inside a screen prefab -> button sprite. Green selects, orange spends gems, blue buys or tries, grey is neutral.
    private static readonly (string Path, string Sprite)[] Buttons =
    {
        ("InfoPanel/Select", UIPack.ButtonSprites + "green.png"),
        ("InfoPanel/Unlock", UIPack.ButtonSprites + "orange.png"),
        ("InfoPanel/Buy", UIPack.ButtonSprites + "cyan.png"),
        ("InfoPanel/Try", UIPack.ButtonSprites + "light blue.png"),
        ("InfoPanel/Restore", UIPack.ButtonSprites + "GREY.png"),
    };

    // Info-panel texts: font size only (their colours follow the item's tier in code).
    private static readonly (string Path, float Size)[] InfoTexts =
    {
        ("InfoPanel/Name", 56f),
        ("InfoPanel/Tier", 34f),
        ("InfoPanel/Status", 34f),
    };

    // The Main Menu's own buttons (size and position too: this table is the one place they are set).
    private static readonly (string Child, string Sprite, Vector2 Size, Vector2 Position, float FontSize)[] MenuButtons =
    {
        ("Play", UIPack.ButtonSprites + "green.png", new Vector2(420f, 100f), new Vector2(0f, 10f), 64f),
        ("Exit", UIPack.ButtonSprites + "GREY.png", new Vector2(300f, 64f), new Vector2(0f, -100f), 44f),
        ("ClearHighScore", UIPack.ButtonSprites + "GREY.png", new Vector2(320f, 50f), new Vector2(0f, -170f), 28f),
        ("RemoveAds", UIPack.ButtonSprites + "orange.png", new Vector2(420f, 64f), new Vector2(0f, -245f), 40f),
        ("SoundToggle", UIPack.ButtonSprites + "PURPLE.png", new Vector2(260f, 60f), new Vector2(40f, -30f), 34f),
    };

    private static readonly Vector4 PanelBorder = new Vector4(56f, 56f, 56f, 56f);
    private static readonly Vector4 BarBorder = new Vector4(40f, 40f, 40f, 40f);
    private static readonly Vector4 SlotBorder = new Vector4(20f, 20f, 20f, 20f);

    [MenuItem("Tools/UI/Apply Selection Skin")]
    public static void ApplyAll()
    {
        UIPack.EnsureFont();
        foreach (var (_, sprite) in Buttons)
        {
            UIPack.SetBorder(sprite, UIPack.ButtonBorder);
        }
        foreach (var (_, sprite, _, _, _) in MenuButtons)
        {
            UIPack.SetBorder(sprite, UIPack.ButtonBorder);
        }
        UIPack.SetBorder(Popup, PanelBorder);
        UIPack.SetBorder(HudPanel, BarBorder);
        UIPack.SetBorder(SlotTeal, SlotBorder);
        UIPack.SetBorder(SlotDark, SlotBorder);

        foreach (var path in ScreenPrefabs)
        {
            Edit(path, root =>
            {
                // Back is the pack's orange arrow tile, without a label.
                var back = (RectTransform)root.transform.Find("TopBar/Back");
                UIPack.Skin(back.GetComponent<Image>(), BackIcon, Color.white, Image.Type.Simple);
                back.GetComponent<Image>().preserveAspect = true;
                UIPack.PlaceMiddleLeft(back, 24f, 76f, 76f);
                back.GetComponentInChildren<TMP_Text>(true).text = string.Empty;

                UIPack.StyleFont(root.transform.Find("TopBar/Title").GetComponent<TMP_Text>());

                foreach (var (child, sprite) in Buttons)
                {
                    var button = root.transform.Find(child);
                    UIPack.Skin(button.GetComponent<Image>(), sprite, Color.white);
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    UIPack.StyleFont(label);
                    label.fontSize = child.EndsWith("Restore") ? 30f : 34f;
                    label.color = Color.white;
                }
                foreach (var (child, size) in InfoTexts)
                {
                    var text = root.transform.Find(child).GetComponent<TMP_Text>();
                    UIPack.StyleFont(text);
                    text.fontSize = size;
                }

                // The card: a teal panel with the pack's gold frame; the grid sits inside the frame.
                UIPack.Skin(root.transform.Find("GridPanel").GetComponent<Image>(), Popup, Color.white);
                var viewport = (RectTransform)root.transform.Find("GridPanel/Viewport");
                viewport.offsetMin = new Vector2(20f, 20f);
                viewport.offsetMax = new Vector2(-20f, -20f);
            });
        }

        Edit(TilePrefab, root =>
        {
            UIPack.Skin(root.GetComponent<Image>(), SlotDark, Color.white);
            // The selected item shows only the pack's gold frame (its border), not the fill.
            var frame = root.transform.Find("SelectedFrame").GetComponent<Image>();
            UIPack.Skin(frame, HudPanel, Color.white);
            frame.fillCenter = false;
            UIPack.StyleFont(root.GetComponentInChildren<TMP_Text>(true));
            UIPack.StyleFont(root.transform.Find("Badge").GetComponent<TMP_Text>());
        });

        Edit(GemCounterPrefab, root =>
        {
            UIPack.Skin(root.transform.Find("Slot").GetComponent<Image>(), SlotTeal, Color.white);
            UIPack.Skin(root.transform.Find("Icon").GetComponent<Image>(), CoinIcon, Color.white, Image.Type.Simple);
            root.transform.Find("Icon").GetComponent<Image>().preserveAspect = true;
            UIPack.Style(root.transform.Find("Label").GetComponent<TMP_Text>(), 36f, Color.white);
        });

        Edit(TeaserPrefab, root =>
        {
            UIPack.Skin(root.GetComponent<Image>(), HudPanel, Color.white);
            UIPack.Style(root.transform.Find("Title").GetComponent<TMP_Text>(), 32f, UIPack.Gold);
        });

        Edit(MainMenuPrefab, root =>
        {
            foreach (var (child, sprite, size, position, fontSize) in MenuButtons)
            {
                var button = (RectTransform)root.transform.Find(child);
                UIPack.Skin(button.GetComponent<Image>(), sprite, Color.white);
                button.sizeDelta = size;
                button.anchoredPosition = position;
                UIPack.Style(button.GetComponentInChildren<TMP_Text>(true), fontSize, Color.white);
            }
        });

        AssetDatabase.SaveAssets();
        Debug.Log("Applied the Hyper Casual UI Pack skin to the selection screens, tiles, gem chip, teasers and Main Menu buttons.");
    }

    private static void Edit(string prefabPath, System.Action<GameObject> apply)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
