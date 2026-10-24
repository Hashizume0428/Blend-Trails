using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Updates just the clear presentation, retaining every authored puzzle condition.</summary>
public static class ClearScreenBuilder
{
    [MenuItem("Color Puzzle/Update Clear Presentation")]
    public static void ApplyAll()
    {
        Scene current = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || current.isDirty)
            throw new InvalidOperationException("Stop Play mode and save the current scene first.");
        string original = current.path;
        for (int number = 1; number <= StageCatalog.Count; number++)
        {
            Scene scene = EditorSceneManager.OpenScene(StageCatalog.ScenePath(number));
            Apply(UnityEngine.Object.FindFirstObjectByType<GameManager>(), UnityEngine.Object.FindFirstObjectByType<GameHud>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        var native = AssetImporter.GetAtPath("Assets/Plugins/iOS/ColorPuzzleHaptics.mm") as PluginImporter;
        if (native != null)
        {
            native.SetCompatibleWithAnyPlatform(false); native.SetCompatibleWithEditor(false);
            native.SetCompatibleWithPlatform(BuildTarget.iOS, true); native.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
    }

    public static void Apply(GameManager manager, GameHud hud)
    {
        if (manager == null || hud == null) throw new InvalidOperationException("The stage and HUD are required.");
        var layout = hud.GetComponent<StageScreenLayout>();
        if (layout == null) throw new InvalidOperationException("The stage layout is required.");
        var settings = new SerializedObject(hud);
        var panel = settings.FindProperty("_clearPanel").objectReferenceValue as CanvasGroup;
        if (panel == null) throw new InvalidOperationException("The clear panel is required.");
        var card = panel.transform.Find("Card") as RectTransform;
        if (card == null) throw new InvalidOperationException("The clear content is required.");
        Transparent(panel.gameObject, true); Transparent(card.gameObject, false);
        card.anchorMin = Vector2.zero; card.anchorMax = Vector2.one;
        card.offsetMin = card.offsetMax = Vector2.zero; card.localScale = Vector3.one;
        foreach (string name in new[] { "ClearTitle", "ClearMessage" })
        {
            Transform child = card.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }
        RectTransform area = Rect(card, "SolvedBoardArea");
        RectTransform actions = Rect(card, "ClearActions");
        var actionGroup = GetOrAdd<CanvasGroup>(actions.gameObject);
        actionGroup.alpha = 0; actionGroup.interactable = actionGroup.blocksRaycasts = false;
        var next = settings.FindProperty("_nextStage").objectReferenceValue as UnityEngine.UI.Button;
        var home = settings.FindProperty("_clearHome").objectReferenceValue as UnityEngine.UI.Button;
        if (next == null || home == null) throw new InvalidOperationException("Both clear buttons are required.");
        Style(home, actions, -270, "HOME");
        int number = manager.GetComponent<StageClearNavigation>().StageNumber;
        Style(next, actions, 270, $"Stage{number + 1}");
        RectTransform confettiRect = Rect(panel.transform, "ClearConfetti");
        confettiRect.anchorMin = Vector2.zero; confettiRect.anchorMax = Vector2.one;
        confettiRect.offsetMin = confettiRect.offsetMax = Vector2.zero;
        confettiRect.SetAsFirstSibling();
        GetOrAdd<CanvasRenderer>(confettiRect.gameObject);
        var confetti = GetOrAdd<ClearConfetti>(confettiRect.gameObject);
        confetti.raycastTarget = false;
        var presentation = GetOrAdd<StageClearPresentation>(hud.gameObject);
        Set(presentation, "_layout", layout); Set(presentation, "_panel", panel);
        Set(presentation, "_actions", actionGroup); Set(presentation, "_confetti", confetti);
        Set(layout, "_clearContent", card); Set(layout, "_clearBoardArea", area); Set(layout, "_clearActions", actions);
        Set(hud, "_clearPresentation", presentation); Set(hud, "_nextStageLabel", next.transform.Find("Label").GetComponent<TMPro.TMP_Text>());
        panel.alpha = 1; panel.gameObject.SetActive(false);
        layout.SetClearProgress(0); layout.SetPlayControlsVisible(true);
        EditorUtility.SetDirty(layout); EditorUtility.SetDirty(hud); EditorUtility.SetDirty(presentation);
    }
    private static void Style(UnityEngine.UI.Button button, Transform parent, float x, string label)
    {
        var rect = (RectTransform)button.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0); rect.sizeDelta = new Vector2(460, 166); rect.localScale = Vector3.one;
        var text = button.transform.Find("Label").GetComponent<TMPro.TMP_Text>();
        text.text = label; text.fontSize = 60; text.raycastTarget = false;
        var textRect = (RectTransform)text.transform;
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
    }
    private static void Transparent(GameObject go, bool raycast)
    {
        var image = go.GetComponent<UnityEngine.UI.Image>();
        if (image != null) { image.color = Color.clear; image.raycastTarget = raycast; }
    }
    private static RectTransform Rect(Transform parent, string name)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect != null) return rect;
        rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
    private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
