using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Dresses the in-game UI in the Hyper Casual UI Pack (Tools > UI > Apply Game UI Skin): the HUD (score card, best / gem chips,
// power-up rows with shrinking bars, pause button) and the Pause, Game Over (+ trial), Success, New Record and Level Select
// screens. Every screen is one row of tables below: a backing panel or slot, then per button its pack sprite, rect and font size,
// then per label its rect, font size and colour. Idempotent: change a number or sprite here and run it again, never hand-edit the
// prefabs. The selection screens and Main Menu are dressed by SelectionSkinBuilder.
public static class GameUIBuilder
{
    private const string Prefabs = "Assets/Prefabs/";
    private const string UI = "Assets/Prefabs/UI/";

    // Pack sprites (see UIPack for the folders).
    private static readonly string Popup = UIPack.PanelSprites + "Level popup.png";
    private static readonly string PopupPink = UIPack.PanelSprites + "Game Over (1).png";
    private static readonly string Screen = UIPack.PanelSprites + "Level screen pannel.png";
    private static readonly string HudPanel = UIPack.PanelSprites + "HUD Pannel (1).png";
    private static readonly string SlotTeal = UIPack.PanelSprites + "Rectangle 363.png";
    private static readonly string SlotBrown = UIPack.PanelSprites + "Rectangle 363 (1).png";
    private static readonly string SlotDark = UIPack.PanelSprites + "Rectangle 359.png";
    private static readonly string BarTrack = UIPack.PanelSprites + "Rectangle 358.png";
    private static readonly string Green = UIPack.ButtonSprites + "green.png";
    private static readonly string DarkGreen = UIPack.ButtonSprites + "DARK Green empty.png";
    private static readonly string Orange = UIPack.ButtonSprites + "orange.png";
    private static readonly string Cyan = UIPack.ButtonSprites + "cyan.png";
    private static readonly string Purple = UIPack.ButtonSprites + "PURPLE.png";
    private static readonly string Grey = UIPack.ButtonSprites + "GREY.png";
    private static readonly string Golden = UIPack.ButtonSprites + "GOLDEN.png";
    private static readonly string CrownIcon = UIPack.IconSprites + "crown.png";
    private static readonly string CoinIcon = UIPack.IconSprites + "coin.png";
    private static readonly string PauseIcon = UIPack.IconSprites + "Pause (2).png";

    private struct Backing
    {
        public string Parent; // child path inside the prefab ("" = its root)
        public string Name;
        public string Sprite;
        public Vector4 Border;
        public float X, Y, W, H;
        public Color Tint;
        public bool Blocks;

        public Backing(string parent, string name, string sprite, float border, float x, float y, float w, float h, bool blocks = false)
            : this(parent, name, sprite, border, x, y, w, h, Color.white, blocks) { }

        public Backing(string parent, string name, string sprite, float border, float x, float y, float w, float h, Color tint, bool blocks)
        {
            Parent = parent; Name = name; Sprite = sprite; Border = new Vector4(border, border, border, border);
            X = x; Y = y; W = w; H = h; Tint = tint; Blocks = blocks;
        }
    }

    private struct Button
    {
        public string Path, Sprite;
        public float X, Y, W, H, Font;
        public Button(string path, string sprite, float x, float y, float w, float h, float font)
        {
            Path = path; Sprite = sprite; X = x; Y = y; W = w; H = h; Font = font;
        }
    }

    private struct Label
    {
        public string Path;
        public float X, Y, W, H, Font;
        public Color Colour;
        public Label(string path, float x, float y, float w, float h, float font, Color colour)
        {
            Path = path; X = x; Y = y; W = w; H = h; Font = font; Colour = colour;
        }
    }

    private struct Menu
    {
        public string Prefab;
        public Backing[] Backings;
        public Button[] Buttons;
        public Label[] Labels;
    }

    private static readonly Color White = UIPack.White;
    private static readonly Color Gold = UIPack.Gold;

    // Centre-anchored coordinates, y up, in canvas units (reference 1920 x 866).
    private static readonly Menu[] Menus =
    {
        new Menu
        {
            Prefab = UI + "PauseMenu.prefab",
            Backings = new[] { new Backing("", "Panel", Popup, 56f, 0f, 0f, 520f, 560f, true) },
            Buttons = new[]
            {
                new Button("Resume", Green, 0f, 95f, 320f, 68f, 44f),
                new Button("Restart", Orange, 0f, 10f, 320f, 68f, 44f),
                new Button("SoundToggle", Purple, 0f, -75f, 320f, 68f, 38f),
                new Button("Quit", Cyan, 0f, -160f, 320f, 68f, 44f),
            },
            Labels = new[] { new Label("Pause", 0f, 200f, 440f, 90f, 84f, White) },
        },
        new Menu
        {
            Prefab = UI + "GameOverMenu.prefab",
            Backings = new[]
            {
                new Backing("", "Panel", PopupPink, 56f, 0f, 0f, 620f, 740f, true),
                new Backing("", "GemSlot", SlotBrown, 20f, 0f, 15f, 480f, 200f),
            },
            Buttons = new[]
            {
                new Button("Restart", Orange, -135f, -150f, 250f, 68f, 40f),
                new Button("Quit", Cyan, 135f, -150f, 250f, 68f, 40f),
                new Button("NextLevel", Green, 0f, -235f, 400f, 72f, 44f),
            },
            Labels = new[]
            {
                new Label("Title", 0f, 300f, 560f, 90f, 88f, White),
                new Label("FinalScoreText", 0f, 215f, 560f, 80f, 64f, Gold),
                new Label("HighScore", 0f, 150f, 560f, 50f, 40f, UIPack.Soft),
                new Label("GemBreakdown", 0f, 15f, 440f, 190f, 27f, White),
            },
        },
        new Menu
        {
            // The trial-over offer sits inside the Game Over menu, on its own dimmed layer.
            Prefab = UI + "GameOverMenu.prefab",
            Backings = new[] { new Backing("TrialPanel", "Panel", Popup, 56f, 0f, 0f, 780f, 520f, true) },
            Buttons = new[]
            {
                new Button("TrialPanel/Buy", Cyan, 0f, 80f, 560f, 66f, 38f),
                new Button("TrialPanel/Unlock", Orange, 0f, 0f, 560f, 66f, 38f),
                new Button("TrialPanel/WatchAd", Green, 0f, -80f, 560f, 66f, 38f),
                new Button("TrialPanel/NoThanks", Grey, 0f, -160f, 560f, 66f, 38f),
            },
            Labels = new[] { new Label("TrialPanel/Title", 0f, 185f, 700f, 70f, 40f, White) },
        },
        new Menu
        {
            Prefab = UI + "SuccessMenu.prefab",
            Backings = new[]
            {
                new Backing("", "Panel", Popup, 56f, 0f, 0f, 600f, 600f, true),
                new Backing("", "RewardSlot", SlotDark, 20f, 0f, 105f, 500f, 140f),
            },
            Buttons = new[]
            {
                new Button("NextLevel", Green, 0f, -30f, 380f, 70f, 44f),
                new Button("KeepGoing", Orange, 0f, -115f, 380f, 70f, 44f),
                new Button("Quit", Cyan, 0f, -200f, 380f, 70f, 44f),
            },
            Labels = new[]
            {
                new Label("Title", 0f, 225f, 540f, 80f, 60f, Gold),
                new Label("Reward", 0f, 105f, 480f, 130f, 38f, White),
            },
        },
        new Menu
        {
            Prefab = UI + "LevelSelect.prefab",
            Backings = new[] { new Backing("", "Panel", Screen, 48f, 0f, 0f, 780f, 420f, true) },
            Buttons = new Button[0],
            Labels = new[] { new Label("Title", 0f, 145f, 700f, 90f, 76f, White) },
        },
    };

    [MenuItem("Tools/UI/Apply Game UI Skin")]
    public static void ApplyAll()
    {
        UIPack.EnsureFont();
        SetBorders();

        foreach (var menu in Menus)
        {
            ApplyMenu(menu);
        }

        ApplyNewRecord();
        ApplyLevelSelectGrid();
        ApplyLevelTile();
        ApplyScoreHud();
        ApplyPowerUpRow();
        ApplyPauseButton();

        AssetDatabase.SaveAssets();
        Debug.Log("Applied the Hyper Casual UI Pack skin to the HUD and the in-game menus.");
    }

    private static void SetBorders()
    {
        foreach (var menu in Menus)
        {
            foreach (var backing in menu.Backings)
            {
                UIPack.SetBorder(backing.Sprite, backing.Border);
            }
            foreach (var button in menu.Buttons)
            {
                UIPack.SetBorder(button.Sprite, UIPack.ButtonBorder);
            }
        }
        foreach (var pill in new[] { Green, DarkGreen, Orange, Cyan, Purple, Grey, Golden })
        {
            UIPack.SetBorder(pill, UIPack.ButtonBorder);
        }
        UIPack.SetBorder(HudPanel, new Vector4(40f, 40f, 40f, 40f));
        UIPack.SetBorder(SlotTeal, new Vector4(20f, 20f, 20f, 20f));
        UIPack.SetBorder(BarTrack, new Vector4(11f, 0f, 11f, 0f));
    }

    private static void ApplyMenu(Menu menu)
    {
        var root = PrefabUtility.LoadPrefabContents(menu.Prefab);
        try
        {
            for (var i = 0; i < menu.Backings.Length; i++)
            {
                var b = menu.Backings[i];
                var parent = string.IsNullOrEmpty(b.Parent) ? root.transform : root.transform.Find(b.Parent);
                var image = UIPack.ImageChild(parent, b.Name, b.Sprite, b.Tint, Image.Type.Sliced, b.Blocks);
                UIPack.PlaceCentre(image.rectTransform, b.X, b.Y, b.W, b.H);
                image.transform.SetSiblingIndex(i);
            }

            foreach (var b in menu.Buttons)
            {
                var rect = (RectTransform)root.transform.Find(b.Path);
                UIPack.Skin(rect.GetComponent<Image>(), b.Sprite, Color.white);
                UIPack.PlaceCentre(rect, b.X, b.Y, b.W, b.H);
                UIPack.Style(rect.GetComponentInChildren<TMP_Text>(true), b.Font, White);
            }

            foreach (var l in menu.Labels)
            {
                var rect = (RectTransform)root.transform.Find(l.Path);
                UIPack.PlaceCentre(rect, l.X, l.Y, l.W, l.H);
                UIPack.Style(rect.GetComponent<TMP_Text>(), l.Font, l.Colour);
            }

            PrefabUtility.SaveAsPrefabAsset(root, menu.Prefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyNewRecord()
    {
        var path = UI + "NewRecordScreen.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            UIPack.SetBorder(Golden, UIPack.ButtonBorder);
            UIPack.Skin(root.GetComponent<Image>(), Golden, Color.white);
            ((RectTransform)root.transform).sizeDelta = new Vector2(620f, 120f);
            var text = (RectTransform)root.transform.Find("NewRecordText");
            UIPack.PlaceCentre(text, 0f, 0f, 600f, 100f);
            UIPack.Style(text.GetComponent<TMP_Text>(), 56f, White);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyLevelSelectGrid()
    {
        var path = UI + "LevelSelect.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var scroll = (RectTransform)root.transform.Find("ScrollView");
            UIPack.PlaceCentre(scroll, 0f, -35f, 740f, 260f);
            var grid = root.GetComponentInChildren<GridLayoutGroup>(true);
            grid.cellSize = new Vector2(224f, 104f);
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(12, 12, 12, 12);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyLevelTile()
    {
        var path = Prefabs + "LevelTile.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            UIPack.Skin(root.GetComponent<Image>(), Green, Color.white);
            UIPack.Style(root.GetComponentInChildren<TMP_Text>(true), 34f, White);
            var button = root.GetComponent<UnityEngine.UI.Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = UIPack.Sprite(Green),
                selectedSprite = UIPack.Sprite(Green),
                pressedSprite = UIPack.Sprite(DarkGreen),
                disabledSprite = UIPack.Sprite(Grey),
            };
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Score card top-left with the best-score and gem chips under it, then the power-up rows.
    private static void ApplyScoreHud()
    {
        var path = UI + "Score.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(440f, 176f);

            var panel = UIPack.ImageChild(root.transform, "Panel", HudPanel, Color.white, Image.Type.Sliced, false);
            UIPack.PlaceTopLeft(panel.rectTransform, 0f, 0f, 440f, 110f);
            var bestSlot = UIPack.ImageChild(root.transform, "BestSlot", SlotTeal, Color.white, Image.Type.Sliced, false);
            UIPack.PlaceTopLeft(bestSlot.rectTransform, 0f, 122f, 210f, 52f);
            var bestIcon = UIPack.ImageChild(root.transform, "BestIcon", CrownIcon, Color.white, Image.Type.Simple, false);
            UIPack.PlaceTopLeft(bestIcon.rectTransform, 10f, 131f, 36f, 34f);
            var gemSlot = UIPack.ImageChild(root.transform, "GemSlot", SlotTeal, Color.white, Image.Type.Sliced, false);
            UIPack.PlaceTopLeft(gemSlot.rectTransform, 226f, 122f, 214f, 52f);
            var gemIcon = UIPack.ImageChild(root.transform, "GemIcon", CoinIcon, Color.white, Image.Type.Simple, false);
            UIPack.PlaceTopLeft(gemIcon.rectTransform, 236f, 130f, 34f, 34f);
            var index = 0;
            foreach (var backing in new[] { panel, bestSlot, bestIcon, gemSlot, gemIcon })
            {
                backing.transform.SetSiblingIndex(index++);
            }

            PlaceLabel(root, "ScoreLabel", 28f, 33f, 150f, 44f, 30f, UIPack.Soft, TextAlignmentOptions.MidlineLeft);
            PlaceLabel(root, "ScoreText", 150f, 4f, 270f, 100f, 76f, White, TextAlignmentOptions.MidlineRight);
            PlaceLabel(root, "HighScoreText", 54f, 122f, 150f, 52f, 30f, White, TextAlignmentOptions.MidlineLeft);
            PlaceLabel(root, "GemText", 278f, 122f, 156f, 52f, 34f, White, TextAlignmentOptions.MidlineLeft);

            var container = (RectTransform)root.transform.Find("PowerUpHUDContainer");
            UIPack.PlaceTopLeft(container, 0f, 190f, 300f, 64f);
            container.pivot = new Vector2(0f, 1f);
            container.anchoredPosition = new Vector2(0f, -190f);
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void PlaceLabel(GameObject root, string name, float x, float y, float w, float h, float font, Color colour, TextAlignmentOptions align)
    {
        var rect = (RectTransform)System.Array.Find(root.GetComponentsInChildren<Transform>(true), t => t.name == name);
        rect.SetParent(root.transform, false);
        UIPack.PlaceTopLeft(rect, x, y, w, h);
        UIPack.Style(rect.GetComponent<TMP_Text>(), font, colour, align);
    }

    // One power-up row: slot, icon, name, seconds left and the bar that shrinks as the time runs out.
    private static void ApplyPowerUpRow()
    {
        var path = Prefabs + "PowerUpHUDIcon.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(300f, 64f);
            var slot = root.GetComponent<Image>();
            if (slot == null)
            {
                slot = root.AddComponent<Image>();
            }
            UIPack.Skin(slot, SlotTeal, Color.white);
            slot.raycastTarget = false;

            var icon = (RectTransform)root.transform.Find("Icon");
            UIPack.PlaceTopLeft(icon, 8f, 6f, 52f, 52f);
            icon.GetComponent<Image>().preserveAspect = true;

            var track = UIPack.ImageChild(root.transform, "FillTrack", BarTrack, new Color(0.05f, 0.13f, 0.19f, 1f), Image.Type.Sliced, false);
            UIPack.PlaceTopLeft(track.rectTransform, 70f, 38f, 160f, 16f);
            var bar = (RectTransform)root.transform.Find("FillBar");
            UIPack.PlaceTopLeft(bar, 72f, 40f, 156f, 12f);
            var fill = bar.GetComponent<Image>();
            UIPack.Skin(fill, Green, Color.white, Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            track.transform.SetAsFirstSibling();

            PlaceLabel(root, "NameLabel", 70f, 4f, 170f, 30f, 24f, UIPack.Soft, TextAlignmentOptions.MidlineLeft);
            PlaceLabel(root, "Countdown", 236f, 8f, 56f, 48f, 40f, Gold, TextAlignmentOptions.MidlineRight);

            var settings = new SerializedObject(root.GetComponent<PowerUpHUDIcon>());
            settings.FindProperty("barTrack").objectReferenceValue = track.gameObject;
            settings.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Core holds instances of the HUD prefabs. The pause button is a MenuButton instance that gets the pack's orange pause tile;
    // the Score instance must follow the prefab's new layout, so its old position/size overrides are dropped.
    private static void ApplyPauseButton()
    {
        var core = SceneManager.GetSceneByName("Core");
        if (!core.isLoaded)
        {
            Debug.LogWarning("Scene Core is not open: the pause button and score card were not updated.");
            return;
        }

        foreach (var rootObject in core.GetRootGameObjects())
        {
            var button = rootObject.transform.Find("PauseButton");
            if (button == null)
            {
                continue;
            }

            var image = button.GetComponent<Image>();
            UIPack.Skin(image, PauseIcon, Color.white, Image.Type.Simple);
            image.preserveAspect = true;
            ((RectTransform)button).sizeDelta = new Vector2(96f, 96f);
            ((RectTransform)button).anchoredPosition = new Vector2(-72f, -72f);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = string.Empty;
            // Edits to a prefab instance only survive a save if they are recorded as overrides.
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);

            // Keep the instance's scene references; drop its layout and label-colour overrides so the prefab's card design shows.
            var score = rootObject.transform.Find("Score").gameObject;
            var kept = System.Array.FindAll(PrefabUtility.GetPropertyModifications(score),
                m => !((m.target is RectTransform && m.target.name == "Score") || (m.target is TMP_Text && m.target.name == "ScoreLabel")));
            PrefabUtility.SetPropertyModifications(score, kept);

            // The New Record badge keeps its scene position but takes the prefab's size.
            var record = rootObject.transform.Find("NewRecordScreen").gameObject;
            PrefabUtility.SetPropertyModifications(record, System.Array.FindAll(PrefabUtility.GetPropertyModifications(record),
                m => !(m.target is RectTransform && m.propertyPath.StartsWith("m_SizeDelta"))));

            EditorSceneManager.MarkSceneDirty(core);
            EditorSceneManager.SaveScene(core);
            return;
        }

        Debug.LogWarning("Core has no PauseButton under a root object.");
    }
}
