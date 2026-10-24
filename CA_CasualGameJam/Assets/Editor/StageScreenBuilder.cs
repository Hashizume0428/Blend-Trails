using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Applies the approved Figma stage skin without replacing the puzzle or its conditions.</summary>
public static class StageScreenBuilder
{
    private const string SkinPath = "Assets/UI/Stage/";
    private const string FontPath = "Assets/Fonts/Inter/Inter-Regular SDF.asset";

    [MenuItem("Color Puzzle/Apply Figma Stage UI")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before editing the stage.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != PrototypeSceneBuilder.ScenePath)
        {
            if (scene.isDirty) throw new InvalidOperationException("Save the current scene before switching to the stage.");
            scene = EditorSceneManager.OpenScene(PrototypeSceneBuilder.ScenePath);
        }
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
        if (manager == null || manager.TilesList.Count != 16 || hud == null)
            throw new InvalidOperationException("The existing 16-tile stage and HUD are required.");
        foreach (string name in new[] { "BoardPanel", "ReloadPanel", "Home", "ReloadAll", "ReloadRed", "ReloadBlue", "MixPurple" })
            ImportSprite(name);
        TMP_FontAsset font = GetFont();
        Transform canvas = hud.transform;
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1324, 2879);
        scaler.matchWidthOrHeight = 0;
        RectTransform safe = Rect(canvas, "SafeArea"); Stretch(safe);
        RectTransform content = Rect(safe, "StageContent");
        foreach (string old in new[] { "Title", "Progress", "Legend", "Status", "Instructions" })
        {
            Transform child = canvas.Find(old);
            if (child != null) child.gameObject.SetActive(false);
        }
        RectTransform stage = canvas.Find("Level") as RectTransform;
        if (stage == null) stage = content.Find("Level") as RectTransform;
        if (stage == null) throw new InvalidOperationException("The existing stage label is missing.");
        stage.SetParent(content, false);
        var label = stage.GetComponent<TMP_Text>();
        label.text = "-Stage1-"; label.font = font; label.fontSize = 128;
        label.fontStyle = FontStyles.Normal; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(49, 49, 49, 255); label.raycastTarget = false;
        label.enableAutoSizing = false; label.textWrappingMode = TextWrappingModes.NoWrap;
        label.margin = Vector4.zero;

        RectTransform boardArea = Rect(content, "BoardArea");
        RectTransform legend = Rect(content, "MixPurple");
        Image(legend, "MixPurple", false);
        RectTransform tray = Rect(content, "ReloadPanel");
        Image(tray, "ReloadPanel", false);
        ExistingButton(canvas, tray, "ReloadRed", 818);
        ExistingButton(canvas, tray, "ReloadBlue", 475);
        var all = NewButton(tray, "ReloadAll");
        Position((RectTransform)all.transform, 133, 73, 260, 260);
        var home = NewButton(content, "HomeButton", "Home");
        var board = manager.TilesList[0].transform.parent;
        Transform backing = board.Find("BoardBackground");
        if (backing == null) { backing = new GameObject("BoardBackground").transform; backing.SetParent(board, false); }
        var background = GetOrAdd<SpriteRenderer>(backing.gameObject);
        background.sprite = Sprite("BoardPanel"); background.color = Color.white; background.sortingOrder = -1;
        background.sharedMaterial = manager.TilesList[0].transform.Find("TileFace").GetComponent<SpriteRenderer>().sharedMaterial;
        backing.localPosition = new Vector3(-7.5f / 277, 0.3f + 5.5f / 277, 0);
        backing.localScale = Vector3.one * (1200f / 277 / background.sprite.bounds.size.x);
        foreach (TileController tile in manager.TilesList)
        {
            Fit(tile.transform.Find("TileFace").GetComponent<SpriteRenderer>(), 252f / 277);
            Fit(tile.transform.Find("TileFlame").GetComponent<SpriteRenderer>(), 260f / 277);
            Fit(tile.transform.Find("ForbiddenMarker").GetComponent<SpriteRenderer>(), 88f / 277);
            foreach (Transform child in tile.transform)
                if (PrefabUtility.IsPartOfPrefabInstance(child)) PrefabUtility.RecordPrefabInstancePropertyModifications(child);
        }
        var layout = GetOrAdd<StageScreenLayout>(canvas.gameObject);
        Set(layout, "_safeArea", safe); Set(layout, "_content", content); Set(layout, "_home", home.transform);
        Set(layout, "_stageLabel", stage); Set(layout, "_boardArea", boardArea); Set(layout, "_mixLegend", legend);
        Set(layout, "_reloadPanel", tray); Set(layout, "_board", board); Set(layout, "_boardCamera", Camera.main);
        Set(hud, "_allReload", all); Set(hud, "_home", home);
        Set(hud, "_progress", null); Set(hud, "_status", null);
        canvas.Find("ClearPanel").SetAsLastSibling();
        Canvas.ForceUpdateCanvases(); layout.ApplyLayout();
        EditorUtility.SetDirty(hud); EditorUtility.SetDirty(layout); EditorUtility.SetDirty(scaler);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Color Puzzle: applied Figma stage UI, retaining the existing red/blue puzzle.");
    }

    private static void ImportSprite(string name)
    {
        string path = SkinPath + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing Figma export: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
    private static TMP_FontAsset GetFont()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (existing != null) return existing;
        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Inter/Inter-Regular.ttf");
        if (source == null) throw new InvalidOperationException("The Inter Regular source font is missing.");
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
        font.name = "Inter-Regular SDF";
        font.TryAddCharacters("-Stage0123456789", out string missing);
        if (!string.IsNullOrEmpty(missing)) throw new InvalidOperationException("Missing stage glyphs: " + missing);
        AssetDatabase.CreateAsset(font, FontPath);
        font.material.name = font.name + " Material";
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (Texture2D texture in font.atlasTextures)
        {
            texture.name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(texture, font);
        }
        EditorUtility.SetDirty(font);
        return font;
    }
    private static UnityEngine.UI.Button ExistingButton(Transform canvas, Transform tray, string name, float x)
    {
        Transform button = canvas.Find(name) ?? tray.Find(name);
        if (button == null) throw new InvalidOperationException("Existing reload button is missing: " + name);
        button.SetParent(tray, false);
        foreach (Transform child in button) child.gameObject.SetActive(false);
        var result = button.GetComponent<UnityEngine.UI.Button>();
        StyleButton(result, name);
        Position((RectTransform)button, x, 73, 260, 260);
        return result;
    }
    private static UnityEngine.UI.Button NewButton(Transform parent, string name, string sprite = null)
    {
        RectTransform rect = Rect(parent, name);
        var button = GetOrAdd<UnityEngine.UI.Button>(rect.gameObject);
        StyleButton(button, sprite ?? name);
        return button;
    }
    private static void StyleButton(UnityEngine.UI.Button button, string sprite)
    {
        button.targetGraphic = Image((RectTransform)button.transform, sprite, true);
        var colors = button.colors;
        colors.normalColor = colors.disabledColor = colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f); button.colors = colors;
        var navigation = button.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = navigation;
    }
    private static UnityEngine.UI.Image Image(RectTransform rect, string sprite, bool raycast)
    {
        var image = GetOrAdd<UnityEngine.UI.Image>(rect.gameObject);
        image.sprite = Sprite(sprite); image.color = Color.white; image.raycastTarget = raycast;
        image.type = UnityEngine.UI.Image.Type.Simple; image.preserveAspect = true;
        return image;
    }
    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + name + ".png");
    private static void Fit(SpriteRenderer renderer, float width) => renderer.transform.localScale = Vector3.one * (width / renderer.sprite.bounds.size.x);
    private static RectTransform Rect(Transform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null) return existing;
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void Position(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x + width / 2, -y - height / 2); rect.sizeDelta = new Vector2(width, height);
    }
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
