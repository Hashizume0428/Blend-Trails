using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PrototypeSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PrefabPath = "Assets/Prefabs/PuzzleTile.prefab";
    private static readonly Color Ink = new Color32(48, 43, 57, 255);
    private static TMP_FontAsset _font;

    [MenuItem("Color Puzzle/Import Text Resources")]
    public static void ImportTextResources() => TMP_PackageResourceImporter.ImportResources(true, false, false);

    [MenuItem("Color Puzzle/Build Prototype")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        GameObject existingBoard = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Board");
        if (existingBoard != null && existingBoard.GetComponentsInChildren<TileController>(true).Length > 0)
            throw new InvalidOperationException("The prototype already exists. Edit its Inspector settings in place.");
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (_font == null) throw new InvalidOperationException("Run Color Puzzle/Import Text Resources first.");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.Refresh();
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        camera.orthographic = true; camera.orthographicSize = 5.35f;
        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = GlobalScripts.BackgroundColor;
        var board = existingBoard != null ? existingBoard : new GameObject("Board");
        var original = UnityEngine.Object.FindFirstObjectByType<TileController>();
        if (original == null) original = new GameObject("Tile").AddComponent<TileController>();
        ConfigureTileVisuals(original);
        original.Configure(0, Vector2Int.zero, false, BaseColor.Red, Array.Empty<BaseColor>(), TileColor.None);
        original.transform.localScale = Vector3.one;
        PrefabUtility.SaveAsPrefabAssetAndConnect(original.gameObject, PrefabPath, InteractionMode.AutomatedAction);
        var tiles = new List<TileController>();
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            TileController tile = x == 0 && y == 0 ? original :
                ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene)).GetComponent<TileController>();
            tile.transform.SetParent(board.transform, false);
            tile.transform.position = new Vector3(x - 1.5f, 1.8f - y, 0);
            tile.name = $"Tile_{x}_{y}";
            BaseColor[] required = Array.Empty<BaseColor>();
            if ((x == 3 && y == 0) || (x == 2 && y == 2)) required = new[] { BaseColor.Red };
            if (x == 0 && y == 2) required = new[] { BaseColor.Blue };
            if (x == 3 && (y == 1 || y == 2)) required = new[] { BaseColor.Red, BaseColor.Blue };
            TileColor forbidden = x == 1 && y == 0 ? TileColor.Blue : x == 1 && y == 3 ? TileColor.Red : TileColor.None;
            bool start = x == 0 && (y == 0 || y == 3);
            BaseColor startColor = y == 3 ? BaseColor.Blue : BaseColor.Red;
            tile.Configure(y * 4 + x, new Vector2Int(x, y), start, startColor, required, forbidden);
            var startMarker = tile.transform.Find("StartMarker").GetComponent<SpriteRenderer>();
            startMarker.sprite = SpriteAt(startColor == BaseColor.Red ? "Assets/Image/startPoint/開始点赤.png" : "Assets/Image/startPoint/開始点青.png");
            FitSprite(startMarker, 0.25f);
            var banMarker = tile.transform.Find("ForbiddenMarker").GetComponent<SpriteRenderer>();
            banMarker.sprite = SpriteAt(forbidden == TileColor.Blue ? "Assets/Image/バツ/太いバツのアイコン 青.png" : "Assets/Image/バツ/太いバツのアイコン赤.png");
            FitSprite(banMarker, 0.25f);
            if (start)
            {
                tile.Line.SetColor(startColor);
                tile.Line.ResetToStart(tile.transform.position);
            }
            EditorUtility.SetDirty(tile);
            PrefabUtility.RecordPrefabInstancePropertyModifications(tile);
            PrefabUtility.RecordPrefabInstancePropertyModifications(tile.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(startMarker);
            PrefabUtility.RecordPrefabInstancePropertyModifications(startMarker.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(banMarker);
            PrefabUtility.RecordPrefabInstancePropertyModifications(banMarker.transform);
            // RefreshVisuals also changes the frame sprite and child active states.
            foreach (Transform child in tile.transform)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                var renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            if (start) PrefabUtility.RecordPrefabInstancePropertyModifications(tile.Line.GetComponent<LineRenderer>());
            tiles.Add(tile);
        }
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (manager == null) manager = new GameObject("GameManager").AddComponent<GameManager>();
        manager.TilesList = tiles;
        var audio = GetOrAdd<AudioSource>(manager.gameObject);
        audio.playOnAwake = false; audio.spatialBlend = 0;
        SetRef(manager, "_audioSource", audio);
        SetRef(manager, "_tileSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Se/タイル判定(決定ボタンを押す48.mp3"));
        SetRef(manager, "_clearSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Se/ステージクリア(決定ボタンを押す27.mp3"));
        SetRef(manager, "_buttonSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Se/ボタン選択(決定ボタンを押す44.mp3"));
        GameHud hud = BuildHud(manager);
        SetRef(manager, "_hud", hud);
        var input = GetOrAdd<InputTouchManager>(manager.gameObject);
        SetRef(input, "_camera", camera); SetRef(input, "_gameManager", manager);
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        ConfigureLineVisuals(manager, board.transform, tiles);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Color Puzzle: saved 16-tile prototype, prefab and UI.");
    }

    [MenuItem("Color Puzzle/Update Line and Error Visuals")]
    public static void UpdateLineAndErrorVisuals()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before updating assets.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open SampleScene before updating visuals.");
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (manager == null || manager.TilesList.Count == 0) throw new InvalidOperationException("The prototype board is missing.");
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            CenterForbiddenMarker(prefab.GetComponent<TileController>());
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        foreach (TileController tile in manager.TilesList) CenterForbiddenMarker(tile);
        ConfigureLineVisuals(manager, manager.TilesList[0].transform.parent, manager.TilesList);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Color Puzzle: updated mixed line rendering and centered forbidden markers.");
    }

    private static void CenterForbiddenMarker(TileController tile)
    {
        var serialized = new SerializedObject(tile);
        var marker = serialized.FindProperty("_forbiddenMarker").objectReferenceValue as SpriteRenderer;
        if (marker == null) return;
        marker.transform.localPosition = Vector3.zero;
        EditorUtility.SetDirty(marker.transform);
        if (PrefabUtility.IsPartOfPrefabInstance(marker.transform))
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker.transform);
    }

    private static void ConfigureLineVisuals(GameManager manager, Transform parent, List<TileController> tiles)
    {
        BoardLineRenderer visuals = parent.GetComponentInChildren<BoardLineRenderer>(true);
        if (visuals == null)
        {
            var visualObject = new GameObject("LineVisuals");
            visualObject.transform.SetParent(parent, false);
            visuals = visualObject.AddComponent<BoardLineRenderer>();
        }
        TileController start = tiles.First(tile => tile.IsStartTile);
        visuals.CopyStyleFrom(start.Line.GetComponent<LineRenderer>());
        SetRef(manager, "_lineVisuals", visuals);
        EditorUtility.SetDirty(visuals);
        EditorUtility.SetDirty(visuals.GetComponent<MeshRenderer>());
    }

    private static void ConfigureTileVisuals(TileController tile)
    {
        SpriteRenderer face = ChildSprite(tile.transform, "TileFace", SpriteAt("Assets/Image/tile/タイル.png"), 0, 0.92f);
        SpriteRenderer frame = ChildSprite(tile.transform, "TileFlame", GlobalScripts.GetFrameImage(TileColor.None), 1, 0.96f);
        SpriteRenderer start = ChildSprite(tile.transform, "StartMarker", SpriteAt("Assets/Image/startPoint/開始点赤.png"), 4, 0.25f);
        SpriteRenderer ban = ChildSprite(tile.transform, "ForbiddenMarker", SpriteAt("Assets/Image/バツ/太いバツのアイコン赤.png"), 4, 0.25f);
        ban.transform.localPosition = Vector3.zero;
        Transform lineTransform = tile.transform.Find("Line");
        if (lineTransform == null) { lineTransform = new GameObject("Line").transform; lineTransform.SetParent(tile.transform, false); }
        var line = GetOrAdd<LineController>(lineTransform.gameObject);
        var renderer = line.GetComponent<LineRenderer>();
        const string materialPath = "Assets/Materials/PuzzleLine.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP 2D unlit shader is missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        renderer.sharedMaterial = material; renderer.useWorldSpace = true;
        renderer.widthMultiplier = 0.085f; renderer.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
        renderer.numCapVertices = 8; renderer.numCornerVertices = 6; renderer.sortingOrder = 2;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.positionCount = 1; renderer.SetPosition(0, Vector3.zero);
        var collider = GetOrAdd<BoxCollider2D>(tile.gameObject);
        collider.size = Vector2.one * 0.96f;
        tile.BindVisuals(face, frame, start, ban, line);
    }

    private static GameHud BuildHud(GameManager manager)
    {
        var canvasObject = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(GameHud));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasObject.transform;
        Text(root, "Title", "COLOR TRACE", 68, new Vector2(0, -210), new Vector2(960, 100), true, FontStyles.Bold);
        Text(root, "Level", "LEVEL 01  /  MIX & MATCH", 26, new Vector2(0, -290), new Vector2(900, 55), true);
        TMP_Text progress = Text(root, "Progress", "00 / 16", 48, new Vector2(0, -390), new Vector2(800, 70), true);
        Text(root, "Legend", "RED + BLUE = PURPLE", 30, new Vector2(0, 475), new Vector2(960, 55), false);
        TMP_Text status = Text(root, "Status", "Trace from a colored dot", 27, new Vector2(0, 415), new Vector2(960, 55), false);
        var red = ReloadButton(root, "ReloadRed", BaseColor.Red, -160);
        var blue = ReloadButton(root, "ReloadBlue", BaseColor.Blue, 160);
        Text(root, "Instructions", "Fill every tile. Match the frames. Avoid the marked color.", 23,
            new Vector2(0, 85), new Vector2(960, 80), false);
        RectTransform overlay = Rect("ClearPanel", root, Vector2.zero, Vector2.zero, 0.5f);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        var shade = overlay.gameObject.AddComponent<UnityEngine.UI.Image>();
        shade.color = new Color(0.12f, 0.1f, 0.16f, 0.65f); shade.raycastTarget = true;
        var group = overlay.gameObject.AddComponent<CanvasGroup>();
        RectTransform card = Rect("Card", overlay, Vector2.zero, new Vector2(890, 610), 0.5f);
        var cardImage = card.gameObject.AddComponent<UnityEngine.UI.Image>();
        cardImage.sprite = SpriteAt("Assets/Image/tile/タイル.png"); cardImage.raycastTarget = false;
        Text(card, "ClearTitle", "STAGE CLEAR", 68, new Vector2(0, 130), new Vector2(820, 100), null, FontStyles.Bold);
        Text(card, "ClearMessage", "Every color in its place.", 30, new Vector2(0, 35), new Vector2(800, 70), null);
        RectTransform restartRect = Rect("StageSelectButton", card, new Vector2(0, -135), new Vector2(600, 120), 0.5f);
        var restartImage = restartRect.gameObject.AddComponent<UnityEngine.UI.Image>();
        restartImage.sprite = SpriteAt("Assets/Image/tile/タイル.png"); restartImage.color = GlobalScripts.GetLineColor(BaseColor.Blue);
        var restart = restartRect.gameObject.AddComponent<UnityEngine.UI.Button>();
        restart.targetGraphic = restartImage;
        var label = Text(restartRect, "Label", "STAGE SELECT", 32, Vector2.zero, new Vector2(560, 100), null, FontStyles.Bold);
        label.color = Color.white;
        overlay.gameObject.SetActive(false);
        var hud = canvasObject.GetComponent<GameHud>();
        SetRef(hud, "_gameManager", manager); SetRef(hud, "_redReload", red); SetRef(hud, "_blueReload", blue);
        SetRef(hud, "_stageSelect", restart); SetRef(hud, "_progress", progress);
        SetRef(hud, "_status", status); SetRef(hud, "_clearPanel", group);
        return hud;
    }

    private static UnityEngine.UI.Button ReloadButton(Transform parent, string name, BaseColor color, float x)
    {
        RectTransform rect = Rect(name, parent, new Vector2(x, 265), new Vector2(260, 180), 0);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = SpriteAt("Assets/Image/tile/タイル.png");
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors; colors.disabledColor = new Color(0.78f, 0.76f, 0.79f, 0.6f); button.colors = colors;
        var icon = Rect("Icon", rect, new Vector2(0, 20), new Vector2(90, 90), 0.5f).gameObject.AddComponent<UnityEngine.UI.Image>();
        icon.sprite = SpriteAt(color == BaseColor.Red ? "Assets/Image/reloadButton/リロード赤.png" : "Assets/Image/reloadButton/リロード青.png");
        icon.preserveAspect = true; icon.raycastTarget = false;
        Text(rect, "Label", color == BaseColor.Red ? "RESET RED" : "RESET BLUE", 25,
            new Vector2(0, -58), new Vector2(240, 50), null, FontStyles.Bold);
        return button;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size, float anchorY)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, anchorY);
        rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static TMP_Text Text(Transform parent, string name, string value, float size, Vector2 position,
        Vector2 dimensions, bool? top, FontStyles style = FontStyles.Normal)
    {
        var text = Rect(name, parent, position, dimensions, top == true ? 1 : top == false ? 0 : 0.5f)
            .gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font; text.text = value; text.fontSize = size; text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center; text.color = Ink; text.raycastTarget = false;
        return text;
    }
    private static SpriteRenderer ChildSprite(Transform parent, string name, Sprite sprite, int order, float width)
    {
        Transform child = parent.Find(name);
        if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
        child.localPosition = Vector3.zero;
        var renderer = GetOrAdd<SpriteRenderer>(child.gameObject);
        renderer.sprite = sprite; renderer.sortingOrder = order; renderer.color = Color.white;
        renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        FitSprite(renderer, width);
        return renderer;
    }
    private static void FitSprite(SpriteRenderer renderer, float width)
    {
        float scale = width / renderer.sprite.bounds.size.x;
        renderer.transform.localScale = Vector3.one * scale;
    }
    private static Sprite SpriteAt(string path)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) throw new InvalidOperationException("Missing sprite: " + path);
        return sprite;
    }
    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
    private static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
