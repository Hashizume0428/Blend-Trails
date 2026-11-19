using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authors the guide without rebuilding the existing puzzle or stage skin.</summary>
public static class StageOneTutorialBuilder
{
    private const string Folder = "Assets/UI/Tutorial";
    private const string PrefabPath = Folder + "/StageOneTutorial.prefab";
    private const string RootName = "StageOneTutorial";
    private static readonly Vector2Int[] Red = {
        new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0),
        new Vector2Int(4,1), new Vector2Int(3,1), new Vector2Int(2,1), new Vector2Int(1,1), new Vector2Int(0,1) };
    private static readonly Vector2Int[] Blue = {
        new Vector2Int(4,3), new Vector2Int(3,3), new Vector2Int(2,3), new Vector2Int(1,3), new Vector2Int(0,3),
        new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2), new Vector2Int(4,2) };

    [MenuItem("Color Puzzle/Apply Stage 1 Tutorial")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || original.isDirty)
            throw new InvalidOperationException("Stop Play mode and save the current scene first.");
        string originalPath = original.path;
        try
        {
            Scene scene = EditorSceneManager.OpenScene(StageCatalog.ScenePath(1));
            Configure(UnityEngine.Object.FindFirstObjectByType<GameManager>());
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Stage 1.");
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalPath) && originalPath != SceneManager.GetActiveScene().path)
                EditorSceneManager.OpenScene(originalPath);
        }
    }

    public static void Configure(GameManager game)
    {
        if (EditorApplication.isPlaying || game == null)
            throw new InvalidOperationException("An authored board in Edit mode is required.");
        var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
        Transform area = hud != null ? hud.transform.Find("SafeArea/StageContent/BoardArea") : null;
        if (area == null) throw new InvalidOperationException("The existing stage BoardArea is missing.");
        Transform existing = area.Find(RootName);
        var navigation = game.GetComponent<StageClearNavigation>();
        if (navigation == null || navigation.PuzzleId != 1)
        {
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            return;
        }
        ValidateRoute(game, Red, BaseColor.Red);
        ValidateRoute(game, Blue, BaseColor.Blue);
        GameObject prefab = EnsurePrefab();
        GameObject root = existing != null ? existing.gameObject :
            (GameObject)PrefabUtility.InstantiatePrefab(prefab, area);
        var guide = root.GetComponent<StageOneTutorialGuide>();
        var serialized = new SerializedObject(guide);
        serialized.FindProperty("_game").objectReferenceValue = game;
        serialized.FindProperty("_navigation").objectReferenceValue = navigation;
        serialized.FindProperty("_boardCamera").objectReferenceValue = Camera.main;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(guide);
        EditorUtility.SetDirty(guide);
    }

    private static void ValidateRoute(GameManager game, Vector2Int[] route, BaseColor color)
    {
        var visited = new System.Collections.Generic.HashSet<Vector2Int>();
        foreach (Vector2Int cell in route)
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= game.BoardSize.x || cell.y >= game.BoardSize.y || !visited.Add(cell))
                throw new InvalidOperationException("Invalid tutorial cell " + cell);
        }
        for (int i = 1; i < route.Length; i++)
            if (Mathf.Abs(route[i].x - route[i - 1].x) + Mathf.Abs(route[i].y - route[i - 1].y) != 1)
                throw new InvalidOperationException("Tutorial cells must be adjacent.");
        TileController start = game.TilesList.Find(tile => tile.Coordinate == route[0]);
        if (start == null || !start.IsStartTile || start.StartColor != color)
            throw new InvalidOperationException("Tutorial start does not match the puzzle.");
        foreach (TileController tile in game.TilesList)
        {
            TileColor expected = Array.IndexOf(Red, tile.Coordinate) >= 0 ? TileColor.Red : TileColor.Blue;
            if (!PuzzleRules.IsTileCorrect(expected, tile.RequiredColor != TileColor.None, tile.RequiredColor, tile.ForbiddenColor))
                throw new InvalidOperationException("Tutorial solution conflicts with " + tile.name);
        }
    }

    private static GameObject EnsurePrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/UI", "Tutorial");
        string handPath = Folder + "/HandPointer.png";
        if (!File.Exists(handPath)) throw new FileNotFoundException("The transparent tutorial hand is missing.", handPath);
        Import(handPath);
        Shape("TouchHalo", (x, y) => Mathf.Clamp01((0.5f - new Vector2(x - 0.5f, y - 0.5f).magnitude) * 40));
        Shape("DirectionArrow", (x, y) =>
            (x > 0.05f && x < 0.6f && Mathf.Abs(y - 0.5f) < 0.12f) ||
            (x >= 0.45f && x < 0.95f && Mathf.Abs(y - 0.5f) < (0.95f - x) * 0.8f) ? 1 : 0);
        RectTransform root = Rect(null, RootName);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.sizeDelta = Vector2.zero;
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
        RectTransform trail = Rect(root, "GuideTrail");
        trail.anchorMin = Vector2.zero; trail.anchorMax = Vector2.one; trail.sizeDelta = Vector2.zero;
        var dashes = new UnityEngine.UI.Image[36];
        for (int i = 0; i < dashes.Length; i++) dashes[i] = Image(trail, $"Dash{i:00}", null);
        var arrow = Image(root, "DirectionArrow", "DirectionArrow");
        var halo = Image(root, "TouchHalo", "TouchHalo");
        var hand = Image(root, "HandPointer", "HandPointer");
        hand.rectTransform.pivot = FingerTip(handPath);
        var guide = root.gameObject.AddComponent<StageOneTutorialGuide>();
        var serialized = new SerializedObject(guide);
        Set(serialized, "_group", group); Set(serialized, "_hand", hand.rectTransform);
        Set(serialized, "_halo", halo); Set(serialized, "_arrow", arrow);
        var array = serialized.FindProperty("_dashes"); array.arraySize = dashes.Length;
        for (int i = 0; i < dashes.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = dashes[i];
        Route(serialized, "_redRoute", Red); Route(serialized, "_blueRoute", Blue);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        try { return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath); }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    private static RectTransform Rect(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }
    private static UnityEngine.UI.Image Image(Transform parent, string name, string sprite)
    {
        var image = Rect(parent, name).gameObject.AddComponent<UnityEngine.UI.Image>();
        if (sprite != null) image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + sprite + ".png");
        image.raycastTarget = false; image.preserveAspect = sprite != null;
        return image;
    }
    private static void Route(SerializedObject serialized, string name, Vector2Int[] route)
    {
        var property = serialized.FindProperty(name); property.arraySize = route.Length;
        for (int i = 0; i < route.Length; i++) property.GetArrayElementAtIndex(i).vector2IntValue = route[i];
    }
    private static void Set(SerializedObject serialized, string name, UnityEngine.Object value) =>
        serialized.FindProperty(name).objectReferenceValue = value;

    private static void Import(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
    private static void Shape(string name, Func<float, float, float> alpha)
    {
        string path = Folder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                pixels[y * 128 + x] = new Color(1, 1, 1, alpha((x + 0.5f) / 128, (y + 0.5f) / 128));
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        Import(path);
    }
    private static Vector2 FingerTip(string path)
    {
        var texture = new Texture2D(2, 2);
        try
        {
            texture.LoadImage(File.ReadAllBytes(path)); Color32[] pixels = texture.GetPixels32();
            for (int y = texture.height - 1; y >= 0; y--)
            {
                int min = texture.width, max = -1;
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 160) { min = Mathf.Min(min, x); max = x; }
                if (max >= min) return new Vector2((min + max + 1f) / (2 * texture.width), (y + 0.5f) / texture.height);
            }
            throw new InvalidOperationException("Hand sprite is fully transparent.");
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }
}
