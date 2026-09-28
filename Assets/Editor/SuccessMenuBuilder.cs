using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

// Builds Assets/Prefabs/UI/SuccessMenu.prefab from the shared MenuLabel and MenuButton prefabs
// (Tools > UI > Rebuild Success Menu). Same look as PauseMenu: dim full-screen panel, label, buttons.
// Rebuilding replaces the prefab file in place, so scene instances keep their link.
public static class SuccessMenuBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/SuccessMenu.prefab";

    [MenuItem("Tools/UI/Rebuild Success Menu")]
    public static void Rebuild()
    {
        var label = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MenuLabel.prefab");
        var button = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MenuButton.prefab");

        var root = new GameObject("SuccessMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.39215687f);

        var menu = root.AddComponent<SuccessMenu>();
        var titleText = AddLabel(root, label, "Title", 190f);
        var rewardText = AddLabel(root, label, "Reward", 90f);
        rewardText.fontSize = 40f;
        var next = AddButton(root, button, "NextLevel", "Next level", -20f);
        var keep = AddButton(root, button, "KeepGoing", "Keep going", -100f);
        var quit = AddButton(root, button, "Quit", "Menu", -180f);

        UnityEventTools.AddPersistentListener(next.GetComponent<Button>().onClick, menu.NextLevel);
        UnityEventTools.AddPersistentListener(keep.GetComponent<Button>().onClick, menu.KeepGoing);
        UnityEventTools.AddPersistentListener(quit.GetComponent<Button>().onClick, menu.Quit);

        var fields = new SerializedObject(menu);
        fields.FindProperty("title").objectReferenceValue = titleText;
        fields.FindProperty("rewardText").objectReferenceValue = rewardText;
        fields.FindProperty("nextLevelButton").objectReferenceValue = next;
        fields.FindProperty("keepGoingButton").objectReferenceValue = keep;
        fields.FindProperty("quitButton").objectReferenceValue = quit;
        fields.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("Rebuilt " + PrefabPath);
    }

    private static TextMeshProUGUI AddLabel(GameObject parent, GameObject prefab, string name, float y)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        instance.name = name;
        ((RectTransform)instance.transform).anchoredPosition = new Vector2(0f, y);
        return instance.GetComponentInChildren<TextMeshProUGUI>();
    }

    private static GameObject AddButton(GameObject parent, GameObject prefab, string name, string text, float y)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        instance.name = name;
        var rect = (RectTransform)instance.transform;
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(360f, 60f);
        instance.GetComponentInChildren<TextMeshProUGUI>().text = text;
        return instance;
    }
}
