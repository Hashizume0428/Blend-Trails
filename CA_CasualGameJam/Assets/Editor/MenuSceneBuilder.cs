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
    private static readonly Color Ink = new Color32(48, 43, 57, 255);
    private static TMP_FontAsset _font;
    private static Sprite _tileSprite;

    [MenuItem("Color Puzzle/Build Scene Flow")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building scenes.");
        Scene active = SceneManager.GetActiveScene();
        if (active.isDirty && (string.IsNullOrEmpty(active.path) || !EditorSceneManager.SaveScene(active)))
            throw new InvalidOperationException("Save the current scene before building the scene flow.");
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        _tileSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Image/tile/タイル.png").OfType<Sprite>().FirstOrDefault();
        if (_font == null || _tileSprite == null) throw new InvalidOperationException("Prototype UI assets are missing.");
        UpdateStage();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath) == null) BuildTitle();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StageSelectPath) == null) BuildStageSelect();
        string[] paths = { TitlePath, StageSelectPath, PrototypeSceneBuilder.ScenePath };
        EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true))
            .Concat(EditorBuildSettings.scenes.Where(scene => !paths.Contains(scene.path))).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(TitlePath);
        Debug.Log("Color Puzzle: Title -> StageSelect -> SampleScene -> StageSelect is ready.");
    }

    private static void UpdateStage()
    {
        Scene stage = EditorSceneManager.OpenScene(PrototypeSceneBuilder.ScenePath);
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
        if (manager == null || hud == null) throw new InvalidOperationException("The prototype stage is missing.");
        var serializedHud = new SerializedObject(hud);
        var button = serializedHud.FindProperty("_stageSelect").objectReferenceValue as UnityEngine.UI.Button;
        if (button == null) throw new InvalidOperationException("The clear panel button is missing.");
        button.name = "StageSelectButton";
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) throw new InvalidOperationException("The clear button label is missing.");
        label.text = "STAGE SELECT";
        var navigation = manager.GetComponent<StageClearNavigation>();
        if (navigation == null) navigation = manager.gameObject.AddComponent<StageClearNavigation>();
        SetReference(navigation, "_gameManager", manager);
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(stage);
        EditorSceneManager.SaveScene(stage);
    }

    private static Transform NewMenu(string name)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = GlobalScripts.BackgroundColor;
        var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(MenuNavigation));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return canvasObject.transform;
    }

    private static void BuildTitle()
    {
        Transform root = NewMenu("TitleCanvas");
        RectTransform tap = Rect(root, "TapToStart", Vector2.zero, Vector2.zero);
        tap.anchorMin = Vector2.zero; tap.anchorMax = Vector2.one;
        tap.offsetMin = tap.offsetMax = Vector2.zero;
        var background = tap.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = GlobalScripts.BackgroundColor;
        var button = tap.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = background;
        Label(tap, "Title", "COLOR\nTRACE", 112, new Vector2(0, 250), new Vector2(940, 310), FontStyles.Bold);
        Label(tap, "Subtitle", "RED + BLUE = PURPLE", 32, new Vector2(0, 15), new Vector2(900, 70));
        ColorChips(tap, new Vector2(0, -125), 100);
        Label(tap, "Prompt", "TAP TO START", 40, new Vector2(0, -400), new Vector2(900, 90), FontStyles.Bold);
        Label(tap, "Prototype", "PROTOTYPE", 24, new Vector2(0, -700), new Vector2(900, 60));
        BindNavigation(root, button, SceneNavigator.StageSelectScene);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), TitlePath);
    }

    private static void BuildStageSelect()
    {
        Transform root = NewMenu("StageSelectCanvas");
        Label(root, "Heading", "STAGE SELECT", 66, new Vector2(0, 650), new Vector2(960, 110), FontStyles.Bold);
        RectTransform card = Rect(root, "Stage01Button", new Vector2(0, 80), new Vector2(840, 440));
        var image = card.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = _tileSprite;
        image.color = GlobalScripts.NormalFaceColor;
        var button = card.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        Label(card, "StageName", "STAGE 01", 58, new Vector2(0, 105), new Vector2(760, 100), FontStyles.Bold);
        Label(card, "Description", "4 x 4  /  RED + BLUE", 30, new Vector2(0, 5), new Vector2(760, 80));
        ColorChips(card, new Vector2(0, -120), 80);
        Label(root, "Prompt", "Choose a stage", 32, new Vector2(0, -500), new Vector2(900, 80));
        BindNavigation(root, button, SceneNavigator.StageScene);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), StageSelectPath);
    }

    private static void BindNavigation(Transform root, UnityEngine.UI.Button button, string destination)
    {
        var navigation = root.GetComponent<MenuNavigation>();
        SetReference(navigation, "_continueButton", button);
        var serialized = new SerializedObject(navigation);
        serialized.FindProperty("_nextScene").stringValue = destination;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ColorChips(Transform parent, Vector2 position, float size)
    {
        RectTransform row = Rect(parent, "Colors", position, new Vector2(size * 3 + 48, size));
        var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        foreach (TileColor color in new[] { TileColor.Red, TileColor.Blue, TileColor.Purple })
        {
            RectTransform chip = Rect(row, color.ToString(), Vector2.zero, Vector2.one * size);
            var image = chip.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = _tileSprite;
            image.color = GlobalScripts.GetFaceColor(color);
            image.raycastTarget = false;
        }
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static void Label(Transform parent, string name, string value, float size, Vector2 position,
        Vector2 dimensions, FontStyles style = FontStyles.Normal)
    {
        var text = Rect(parent, name, position, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font; text.text = value; text.fontSize = size; text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center; text.color = Ink; text.raycastTarget = false;
    }
    private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
