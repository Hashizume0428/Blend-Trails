using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Installs the failure SE and shared-endpoint chooser without regenerating puzzle conditions.</summary>
public static class PuzzleInteractionBuilder
{
    public const string IncorrectSoundPath = "Assets/Se/クイズ不正解1_最初のブ.mp3";
    private const string PrefabPath = "Assets/UI/Stage/LineResumeSelector.prefab";

    [MenuItem("Color Puzzle/Apply Puzzle Interaction Updates")]
    public static void ApplyToAllStages()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before editing stages.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes before updating stages.");
        ConfigureSoundImport();
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            for (int number = 1; number <= StageCatalog.Count; number++)
            {
                Scene scene = EditorSceneManager.OpenScene(StageCatalog.ScenePath(number));
                ClearScreenBuilder.Apply(UnityEngine.Object.FindFirstObjectByType<GameManager>(), UnityEngine.Object.FindFirstObjectByType<GameHud>());
                ConfigureScene();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + scene.path);
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Debug.Log("Color Puzzle: updated clear actions, failure audio and endpoint selection in all 30 stages.");
    }
    private static void ConfigureSoundImport()
    {
        AssetDatabase.ImportAsset(IncorrectSoundPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(IncorrectSoundPath) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("The incorrect-answer SE is missing.");
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = false;
        importer.SaveAndReimport();
    }
    public static void ConfigureScene()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
        var input = manager != null ? manager.GetComponent<InputTouchManager>() : null;
        if (manager == null || hud == null || input == null) throw new InvalidOperationException("The stage, HUD and input are required.");
        var sound = AssetDatabase.LoadAssetAtPath<AudioClip>(IncorrectSoundPath);
        if (sound == null) throw new InvalidOperationException("The incorrect-answer SE is missing.");
        Set(manager, "_incorrectSound", sound);
        Transform root = hud.transform.Find("LineResumeSelector");
        if (root == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(EnsurePrefab(), hud.transform);
            instance.name = "LineResumeSelector"; root = instance.transform;
        }
        var chooser = root.GetComponent<LineResumeSelector>();
        Set(chooser, "_game", manager); Set(chooser, "_boardCamera", Camera.main);
        Set(input, "_resumeSelector", chooser);
        Transform clearPanel = hud.transform.Find("ClearPanel");
        if (clearPanel != null) clearPanel.SetAsLastSibling();
    }
    private static GameObject EnsurePrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null) return prefab;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new InvalidOperationException("The UI font is missing.");
        RectTransform root = Rect(null, "LineResumeSelector", Vector2.zero, Vector2.zero); Stretch(root);
        try
        {
            var chooser = root.gameObject.AddComponent<LineResumeSelector>();
            RectTransform overlay = Rect(root, "ColorChoiceOverlay", Vector2.zero, Vector2.zero); Stretch(overlay);
            var dismiss = Button(overlay, new Color(0, 0, 0, 0.16f), false);
            dismiss.transition = UnityEngine.UI.Selectable.Transition.None;
            RectTransform card = Rect(overlay, "Card", Vector2.zero, new Vector2(600, 280));
            Image(card, new Color32(250, 250, 250, 255), true, true);
            var shadow = card.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.22f); shadow.effectDistance = new Vector2(0, -8);
            Label(card, "Title", "SELECT COLOR", font, 40, new Vector2(-35, 80), new Vector2(450, 70));
            var close = Button(Rect(card, "CloseColorChoice", new Vector2(250, 87), new Vector2(72, 72)), Color.clear, false);
            Label((RectTransform)close.transform, "Label", "X", font, 36, Vector2.zero, new Vector2(72, 72));
            var first = Button(Rect(card, "ResumeFirstColor", new Vector2(-145, -35), new Vector2(240, 112)), Color.white, true);
            var second = Button(Rect(card, "ResumeSecondColor", new Vector2(145, -35), new Vector2(240, 112)), Color.white, true);
            var firstLabel = Label((RectTransform)first.transform, "Label", "RED", font, 38, Vector2.zero, new Vector2(220, 90));
            var secondLabel = Label((RectTransform)second.transform, "Label", "BLUE", font, 38, Vector2.zero, new Vector2(220, 90));
            RectTransform marker = Rect(root, "SelectedEndpoint", Vector2.zero, new Vector2(100, 100));
            var markerImage = Image(marker, Color.white, false, false);
            Set(chooser, "_overlay", overlay.gameObject); Set(chooser, "_card", card);
            Set(chooser, "_dismiss", dismiss); Set(chooser, "_close", close);
            Set(chooser, "_first", first); Set(chooser, "_second", second);
            Set(chooser, "_firstLabel", firstLabel); Set(chooser, "_secondLabel", secondLabel);
            Set(chooser, "_selectionMarker", markerImage);
            overlay.gameObject.SetActive(false); marker.gameObject.SetActive(false);
            return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        if (parent != null) rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast, bool sliced)
    {
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        if (sliced)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }
        image.color = color; image.raycastTarget = raycast;
        return image;
    }
    private static UnityEngine.UI.Button Button(RectTransform rect, Color color, bool sliced)
    {
        var image = Image(rect, color, true, sliced);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        var navigation = button.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = navigation;
        return button;
    }
    private static TMP_Text Label(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Vector2 position, Vector2 bounds)
    {
        var label = Rect(parent, name, position, bounds).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = size;
        label.color = new Color32(35, 35, 35, 255); label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }
    private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(name).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
