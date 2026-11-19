using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds shared palette references without regenerating any puzzle or UI hierarchy.</summary>
public static class StagePaletteBuilder
{
    private const string VisualsPath = "Assets/UI/Stage/StagePaletteVisuals.asset";

    [MenuItem("Color Puzzle/Apply Stage Color Rotation")]
    public static void ApplyToAllStages()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before editing stages.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes before applying palettes.");
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            for (int number = 1; number <= StageCatalog.Count; number++)
            {
                Scene scene = EditorSceneManager.OpenScene(StageCatalog.ScenePath(number));
                ConfigureScene();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + scene.path);
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Debug.Log("Color Puzzle: configured color rotation in all 30 stages; puzzle conditions are unchanged.");
    }

    public static void ConfigureScene()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
        if (manager == null || hud == null) throw new InvalidOperationException("A stage manager and HUD are required.");
        Set(manager, "_paletteVisuals", GetOrCreateVisuals());
        var legend = hud.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(image => image.name == "MixPurple");
        Set(hud, "_mixLegend", legend);
    }

    private static StagePaletteVisuals GetOrCreateVisuals()
    {
        var visuals = AssetDatabase.LoadAssetAtPath<StagePaletteVisuals>(VisualsPath);
        if (visuals != null) return visuals;
        visuals = ScriptableObject.CreateInstance<StagePaletteVisuals>();
        var settings = new SerializedObject(visuals);
        string[] starts = { "開始点赤", "開始点青", "開始点黄" };
        string[] bans = { "太いバツのアイコン赤", "太いバツのアイコン 青", "太いバツのアイコン 黄" };
        var startArray = settings.FindProperty("_startMarkers");
        var banArray = settings.FindProperty("_forbiddenMarkers");
        startArray.arraySize = banArray.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            startArray.GetArrayElementAtIndex(i).objectReferenceValue = SpriteAt($"Assets/Image/startPoint/{starts[i]}.png");
            banArray.GetArrayElementAtIndex(i).objectReferenceValue = SpriteAt($"Assets/Image/バツ/{bans[i]}.png");
        }
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/StagePaletteUI.shader");
        if (shader == null) throw new InvalidOperationException("The stage palette UI shader is missing.");
        settings.FindProperty("_uiPaletteShader").objectReferenceValue = shader;
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(visuals, VisualsPath);
        return visuals;
    }

    private static Sprite SpriteAt(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()
        ?? throw new InvalidOperationException("Missing palette sprite: " + path);

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(field).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
