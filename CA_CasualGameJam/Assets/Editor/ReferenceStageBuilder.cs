using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authors all thirty supplied boards, preserving the existing stage UI.</summary>
public static class ReferenceStageBuilder
{
    // Coordinates here match the reference notes: row then column, both starting at 1.
    private sealed class Plan
    {
        public readonly string RedStart, BlueStart, RedFrames, BlueFrames, PurpleFrames;
        public Plan(string redStart, string blueStart, string red, string blue, string purple)
        { RedStart = redStart; BlueStart = blueStart; RedFrames = red; BlueFrames = blue; PurpleFrames = purple; }
    }
    private static readonly Plan[] Plans =
    {
        new Plan("11", "45", "15 21", "41 35", ""),
        new Plan("11", "34", "15 45", "43", ""),
        new Plan("15", "41", "13", "43", "23 33"),
        new Plan("22", "32", "11", "15 33", "13 23 44"),
        new Plan("35", "11", "45", "32", "14 24 31"),
        new Plan("11", "15", "13 43", "44", "14 24"),
        new Plan("33", "13", "23", "41", "14 24"),
        new Plan("12", "11", "", "23", "14 15 25 35 43 44 45"),
        new Plan("33", "23", "24", "15", "14 31 41 42 43 44"),
        new Plan("35", "11", "22", "", "13 23 24 31"),
        new Plan("13", "42", "31 34", "14 45", "22 23 24"),
        new Plan("32", "41", "24 45", "22 33", "12 23 34 44"),
        new Plan("15", "14", "43 45", "11 42", "22 23 32 33"),
        new Plan("23", "12", "22 45", "11 41", "14 24 32 42"),
        new Plan("33", "11", "12", "45", "13 23 24 34 35"),
        new Plan("41", "24", "15 35", "31 45", "13 23 33 43"),
        new Plan("21", "11", "33 35", "15 24", "22 34"),
        new Plan("13", "41", "42", "15", "12 22 32 33 34"),
        new Plan("41", "15", "25", "31", "12 13 23 33 43 44"),
        new Plan("15", "41", "11", "25", "14 22 24 32 33 43"),
        new Plan("15", "11", "43 45", "34 41", "22 23 33"),
        new Plan("45", "43", "11 41", "15 23", "13 21 34"),
        new Plan("11", "45", "44", "13", "22 23 24 32 33 34"),
        new Plan("32", "15", "23 25", "41 43", "12 13 33 34"),
        new Plan("15", "11", "25 45", "32 34", "13 22 23 24"),
        new Plan("41", "13", "14 25", "42 44", "21 22 34 35"),
        new Plan("21", "11", "14", "45", "12 22 23 33 34 44"),
        new Plan("21", "11", "45", "41", "12 13 22 34 43 44"),
        new Plan("14", "15", "13", "11", "12 22 23 24 33 34 43"),
        new Plan("41", "15", "25", "31", "12 13 22 23 33 34 43 44")
    };

    [MenuItem("Color Puzzle/Build Reference Stages 1-30")]
    public static void Build() => MenuSceneBuilder.Build();

    [MenuItem("Color Puzzle/Apply Revised Stages 5,6,9,10")]
    public static void ApplyRevisedStages()
    {
        Scene current = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || current.isDirty)
            throw new InvalidOperationException("Stop Play mode and save the current scene first.");
        string original = current.path;
        try
        {
            foreach (int number in new[] { 5, 6, 9, 10 })
            {
                Scene scene = EditorSceneManager.OpenScene(StageCatalog.ScenePath(number));
                ApplyBoard(number);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save " + scene.path);
            }
            AssetDatabase.SaveAssets();
        }
        finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original); }
    }

    [MenuItem("Color Puzzle/Add Reference Stages 11-30")]
    public static void BuildAdditionalStages()
    {
        Scene current = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || current.isDirty)
            throw new InvalidOperationException("Stop Play mode and save the current scene first.");
        string original = current.path;
        try
        {
            BuildStages(11);
            // Existing boards and UI remain authored as-is; only refresh their old static destinations.
            for (int number = 1; number <= 10; number++)
            {
                Scene scene = EditorSceneManager.OpenScene(StageCatalog.ScenePath(number));
                var navigation = UnityEngine.Object.FindFirstObjectByType<StageClearNavigation>();
                var settings = new SerializedObject(navigation);
                settings.FindProperty("_nextStageScene").stringValue = StageCatalog.SceneName(number + 1);
                settings.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene);
            }
            string[] paths = new[] { MenuSceneBuilder.TitlePath }.Concat(Enumerable.Range(1, StageCatalog.Count).Select(StageCatalog.ScenePath)).ToArray();
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(EditorBuildSettings.scenes.Where(scene => !paths.Contains(scene.path) && scene.path != MenuSceneBuilder.StageSelectPath)).ToArray();
            AssetDatabase.SaveAssets();
        }
        finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original); }
    }

    public static void BuildStages(int firstNumber = 1)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before editing stages.");
        for (int number = firstNumber; number <= StageCatalog.Count; number++)
        {
            string path = StageCatalog.ScenePath(number);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Scene template = EditorSceneManager.OpenScene(StageCatalog.ScenePath(1));
                if (!EditorSceneManager.SaveScene(template, path, true)) throw new InvalidOperationException("Could not create " + path);
            }
            Scene scene = EditorSceneManager.OpenScene(path);
            ApplyBoard(number);
            MenuSceneBuilder.UpdateClearScreen(number, StageCatalog.SceneName(number + 1));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + path);
        }
    }

    private static Vector2Int Cell(string value) => new Vector2Int(value[1] - '1', value[0] - '1');
    private static HashSet<Vector2Int> Cells(string values) => new HashSet<Vector2Int>(
        values.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(Cell));

    private static void ApplyBoard(int number)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var layout = UnityEngine.Object.FindFirstObjectByType<StageScreenLayout>();
        if (manager == null || layout == null || manager.TilesList.Count == 0)
            throw new InvalidOperationException("The existing stage UI and board are required.");
        Plan plan = Plans[number - 1];
        var red = Cells(plan.RedFrames); var blue = Cells(plan.BlueFrames); var purple = Cells(plan.PurpleFrames);
        Vector2Int redStart = Cell(plan.RedStart), blueStart = Cell(plan.BlueStart);
        Transform board = manager.TilesList[0].transform.parent;
        Transform oldShared = board.Find("SharedPurpleFrame");
        if (oldShared != null) UnityEngine.Object.DestroyImmediate(oldShared.gameObject);
        var existing = manager.TilesList.ToDictionary(tile => tile.Coordinate);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PuzzleTile.prefab");
        var tiles = new List<TileController>();
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 5; x++)
        {
            var cell = new Vector2Int(x, y);
            if (!existing.TryGetValue(cell, out TileController tile))
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
                tile = go.GetComponent<TileController>();
                tile.transform.SetParent(board, false);
            }
            tile.name = $"Tile_{x}_{y}";
            tile.transform.localPosition = new Vector3(x - 2, 1.5f - y, 0);
            tile.transform.localScale = Vector3.one;
            BaseColor[] required = purple.Contains(cell) ? new[] { BaseColor.Red, BaseColor.Blue } :
                red.Contains(cell) ? new[] { BaseColor.Red } : blue.Contains(cell) ? new[] { BaseColor.Blue } : Array.Empty<BaseColor>();
            BaseColor startColor = cell == blueStart ? BaseColor.Blue : BaseColor.Red;
            tile.Configure(y * 5 + x, cell, cell == redStart || cell == blueStart, startColor, required, TileColor.None);
            var marker = tile.transform.Find("StartMarker").GetComponent<SpriteRenderer>();
            marker.sprite = AssetDatabase.LoadAllAssetsAtPath(startColor == BaseColor.Red ?
                "Assets/Image/startPoint/開始点赤.png" : "Assets/Image/startPoint/開始点青.png").OfType<Sprite>().First();
            Fit(marker, 0.25f);
            Fit(tile.transform.Find("TileFace").GetComponent<SpriteRenderer>(), 252f / 277);
            var frame = tile.transform.Find("TileFlame").GetComponent<SpriteRenderer>();
            frame.enabled = true; Fit(frame, 260f / 277);
            if (tile.IsStartTile) { tile.Line.SetColor(startColor); tile.Line.ResetToStart(tile.transform.position); }
            tiles.Add(tile);
        }
        manager.TilesList = tiles;
        SetSize(manager, "_boardSize", new Vector2Int(5, 4));
        SetSize(layout, "_boardSize", new Vector2Int(5, 4));
        FitBoard(manager);
        foreach (TileController tile in tiles)
        {
            // Keep every configured condition, child state and fitted sprite as a prefab override.
            foreach (Transform child in tile.GetComponentsInChildren<Transform>(true))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                foreach (Component component in child.GetComponents<Component>())
                    if (component != null) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
        Canvas.ForceUpdateCanvases(); layout.ApplyLayout();
        EditorUtility.SetDirty(manager); EditorUtility.SetDirty(layout);
    }

    public static void FitBoard(GameManager manager)
    {
        Transform board = manager.TilesList[0].transform.parent;
        var background = board.Find("BoardBackground").GetComponent<SpriteRenderer>();
        float width = manager.BoardSize.x + 0.3f;
        float height = width * manager.BoardSize.y / manager.BoardSize.x;
        background.transform.localPosition = Vector3.zero;
        background.transform.localScale = new Vector3(width / background.sprite.bounds.size.x, height / background.sprite.bounds.size.y, 1);
        foreach (TileController tile in manager.TilesList)
            tile.transform.localPosition = new Vector3(tile.Coordinate.x - (manager.BoardSize.x - 1) / 2f,
                (manager.BoardSize.y - 1) / 2f - tile.Coordinate.y, 0);
    }
    private static void Fit(SpriteRenderer renderer, float width) => renderer.transform.localScale = Vector3.one * (width / renderer.sprite.bounds.size.x);
    private static void SetSize(UnityEngine.Object target, string field, Vector2Int size)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(field).vector2IntValue = size; settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
