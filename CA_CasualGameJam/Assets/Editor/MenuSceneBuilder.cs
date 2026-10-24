using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class MenuSceneBuilder
{
    public const string TitlePath = "Assets/Scenes/Title.unity";
    public const string StageSelectPath = "Assets/Scenes/StageSelect.unity";
    public const string SecondStagePath = "Assets/Scenes/Stage02.unity";
    private static TMP_FontAsset _font;
    private static Sprite _tileSprite;

    [MenuItem("Color Puzzle/Build Scene Flow")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before editing scenes.");
        Scene active = SceneManager.GetActiveScene();
        if (active.isDirty && (string.IsNullOrEmpty(active.path) || !EditorSceneManager.SaveScene(active)))
            throw new InvalidOperationException("Save the current scene before editing the scene flow.");
        PrepareAssets();

        ReferenceStageBuilder.BuildStages();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath) == null) CreateTitle();
        Scene title = EditorSceneManager.OpenScene(TitlePath);
        UpdateTitle();
        Save(title);
        string[] paths = new[] { TitlePath }.Concat(Enumerable.Range(1, StageCatalog.Count).Select(StageCatalog.ScenePath)).ToArray();
        EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true))
            .Concat(EditorBuildSettings.scenes.Where(scene => !paths.Contains(scene.path) && scene.path != StageSelectPath)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Color Puzzle: sixty reference puzzles, tutorials and endless extra progression are ready.");
    }

    public static void UpdateClearScreen(int stageNumber, string nextScene)
    {
        PrepareAssets();
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
        if (manager == null || hud == null) throw new InvalidOperationException("The existing puzzle and HUD are required.");
        var navigation = GetOrAdd<StageClearNavigation>(manager.gameObject);
        var settings = new SerializedObject(navigation);
        settings.FindProperty("_stageNumber").intValue = stageNumber;
        settings.FindProperty("_nextStageScene").stringValue = nextScene;
        settings.ApplyModifiedPropertiesWithoutUndo();
        Transform panel = hud.transform.Find("ClearPanel");
        Transform card = panel != null ? panel.Find("Card") : null;
        if (card == null) throw new InvalidOperationException("The existing clear panel is missing.");
        var serializedHud = new SerializedObject(hud);
        var next = serializedHud.FindProperty("_nextStage").objectReferenceValue as UnityEngine.UI.Button;
        if (next == null) throw new InvalidOperationException("The existing clear button is missing.");
        next.name = "NextStageButton";
        StyleButton(next, new Vector2(0, -155), new Vector2(350, 120), new Color32(75, 142, 229, 255));
        Label(next.transform, "Label", "NEXT STAGE", 32, Vector2.zero, new Vector2(330, 100));
        TMP_Text stageLabel = hud.GetComponentsInChildren<TMP_Text>(true).First(text => text.name == "Level");
        stageLabel.text = $"-Stage{stageNumber}-";
        TMP_Text message = card.Find("ClearMessage").GetComponent<TMP_Text>();
        message.text = string.IsNullOrEmpty(nextScene) ? "ALL STAGES CLEAR" : "Every color in its place.";
        SetReference(hud, "_nextStage", next);
        SetReference(hud, "_stageLabel", stageLabel); SetReference(hud, "_clearMessage", message);
        panel.gameObject.SetActive(false);
        EditorUtility.SetDirty(hud); EditorUtility.SetDirty(navigation);
        ClearScreenBuilder.Apply(manager, hud);
        PuzzleInteractionBuilder.ConfigureScene();
    }

    private static void PrepareAssets()
    {
        if (_font == null) _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (_tileSprite == null) _tileSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Image/tile/タイル.png").OfType<Sprite>().FirstOrDefault();
        if (_font == null || _tileSprite == null) throw new InvalidOperationException("Prototype UI assets are missing.");
    }

    private static void UpdateTitle()
    {
        var navigation = UnityEngine.Object.FindFirstObjectByType<MenuNavigation>();
        if (navigation == null) throw new InvalidOperationException("The title menu is missing.");
        Transform root = navigation.transform;
        var serialized = new SerializedObject(navigation);
        var button = serialized.FindProperty("_continueButton").objectReferenceValue as UnityEngine.UI.Button;
        if (button == null) throw new InvalidOperationException("The title button is missing.");
        foreach (string name in new[] { "Title", "Subtitle", "Colors", "Prototype" })
        {
            Transform child = button.transform.Find(name);
            if (child != null) child.SetParent(root, false);
        }
        button.name = "Stage01Button";
        StyleButton(button, new Vector2(0, -430), new Vector2(840, 240), GlobalScripts.NormalFaceColor);
        Transform oldPrompt = button.transform.Find("Prompt");
        if (oldPrompt != null) oldPrompt.name = "Label";
        TMP_Text label = Label(button.transform, "Label", "Stage1", 56, new Vector2(0, 40), new Vector2(760, 95));
        label.color = new Color32(49, 49, 49, 255);
        TMP_Text description = Label(button.transform, "Description", "STAGE SELECT", 28, new Vector2(0, -55), new Vector2(760, 65));
        description.color = new Color32(104, 104, 104, 255);
        serialized.FindProperty("_nextScene").stringValue = SceneNavigator.StageScene;
        serialized.FindProperty("_useStageProgress").boolValue = true;
        serialized.FindProperty("_stageLabel").objectReferenceValue = label;
        serialized.FindProperty("_stageDescription").objectReferenceValue = description;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTitle()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = GlobalScripts.BackgroundColor;
        var canvasObject = new GameObject("TitleCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(MenuNavigation));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
        TMP_Text title = Label(canvasObject.transform, "Title", "COLOR\nTRACE", 112, new Vector2(0, 250), new Vector2(940, 310));
        title.color = new Color32(49, 49, 49, 255);
        var button = GetOrAdd<UnityEngine.UI.Button>(Rect(canvasObject.transform, "Stage01Button", Vector2.zero, new Vector2(840, 240)).gameObject);
        SetReference(canvasObject.GetComponent<MenuNavigation>(), "_continueButton", button);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene, TitlePath);
    }

    private static void StyleButton(UnityEngine.UI.Button button, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
        var image = GetOrAdd<UnityEngine.UI.Image>(button.gameObject);
        image.sprite = _tileSprite; image.color = color; image.raycastTarget = true;
        button.targetGraphic = image;
        var colors = button.colors; colors.normalColor = colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.55f); button.colors = colors;
        var navigation = button.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = navigation;
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        RectTransform rect = parent.Find(name) as RectTransform;
        if (rect == null) { rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); }
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static TMP_Text Label(Transform parent, string name, string value, float size, Vector2 position, Vector2 dimensions)
    {
        var text = GetOrAdd<TextMeshProUGUI>(Rect(parent, name, position, dimensions).gameObject);
        text.font = _font; text.text = value; text.fontSize = size; text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.raycastTarget = false;
        return text;
    }
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
    private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
