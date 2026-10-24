using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authors B's presentation over the existing scene flow, colliders and puzzle data.</summary>
public static class ToonSkinBuilder
{
    private const string Folder = "Assets/UI/ToonBold/";
    private const string AssetPath = "Assets/Resources/ToonSkin.asset";
    private static ToonSkinAssets _skin;
    private static Color Ink => ToonSkinAssets.Ink;
    private static Color Cream => ToonSkinAssets.Cream;

    [MenuItem("Color Puzzle/Apply B Toon UI To All Screens")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Stop Play mode and save the open scenes first.");
        string original = SceneManager.GetActiveScene().path;
        try
        {
            _skin = BuildAssets();
            var palettes = AssetDatabase.LoadAssetAtPath<StagePaletteVisuals>("Assets/UI/Stage/StagePaletteVisuals.asset");
            var paletteSettings = new SerializedObject(palettes);
            for (int i = 0; i < 3; i++) paletteSettings.FindProperty("_startMarkers").GetArrayElementAtIndex(i).objectReferenceValue = _skin.Starts[i];
            paletteSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(palettes);
            Scene title = EditorSceneManager.OpenScene(MenuSceneBuilder.TitlePath);
            ApplyTitle(); Save(title);
            for (int number = 1; number <= StageCatalog.Count; number++)
            {
                Scene scene = EditorSceneManager.OpenScene(StageCatalog.ScenePath(number));
                ApplyStage(number, palettes); Save(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("B Toon UI applied: title and all thirty authored stages; puzzle data and navigation retained.");
        }
        finally
        {
            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        }
    }

    private static void ApplyTitle()
    {
        var menu = UnityEngine.Object.FindFirstObjectByType<MenuNavigation>();
        if (menu == null) throw new InvalidOperationException("The existing title navigation is missing.");
        RectTransform canvas = (RectTransform)menu.transform;
        Scaler(canvas);
        Sky(canvas);
        foreach (string name in new[] { "Title", "Subtitle", "Colors", "Prototype" })
        {
            Transform old = canvas.Find(name); if (old != null) old.gameObject.SetActive(false);
        }
        RectTransform safe = Rect(canvas, "ToonSafeArea"); Stretch(safe);
        RectTransform content = Rect(safe, "ToonTitleContent");
        content.sizeDelta = new Vector2(941, 1672);
        foreach (string name in new[] { "ApprovedLogoAndMix", "ApprovedFooter" })
        {
            var old = content.Find(name);
            if (old == null) continue;
            var oldImage = old.GetComponent<UnityEngine.UI.RawImage>();
            if (oldImage != null) oldImage.texture = null;
            old.gameObject.SetActive(false);
        }
        Icon(content, "ToonTitleLogo", _skin.TitleLogo, 85, 140, 771, 560, Color.white).preserveAspect = true;
        Icon(content, "ToonTitleMix", _skin.TitleMix, 135, 710, 671, 534, Color.white).preserveAspect = true;
        var settings = new SerializedObject(menu);
        var button = settings.FindProperty("_continueButton").objectReferenceValue as UnityEngine.UI.Button;
        if (button == null) throw new InvalidOperationException("The current-stage title button is missing.");
        button.transform.SetParent(content, false);
        foreach (Transform child in button.transform) child.gameObject.SetActive(false);
        StyleButton(button, _skin.PrimaryButton);
        Place((RectTransform)button.transform, 28, 1252, 885, 301);
        Icon(button.transform, "ToonPlayIcon", _skin.PlayIcon, 92, 61, 140, 154, Color.white);
        TMP_Text label = Label(button.transform, "Label", "Stage1", 144, 255, 40, 575, 183, true);
        label.gameObject.SetActive(true); Get<ToonStageLabel>(label.gameObject).Configure();
        settings.FindProperty("_stageLabel").objectReferenceValue = label;
        settings.FindProperty("_stageDescription").objectReferenceValue = null;
        settings.ApplyModifiedPropertiesWithoutUndo();
        var layout = Get<ToonTitleLayout>(canvas.gameObject);
        Set(layout, "_safeArea", safe); Set(layout, "_content", content);
        Canvas.ForceUpdateCanvases(); layout.Apply();
        EditorUtility.SetDirty(menu);
    }

    private static void ApplyStage(int number, StagePaletteVisuals palettes)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
        if (manager == null || hud == null || manager.TilesList.Count != 20) throw new InvalidOperationException("Existing board/HUD missing.");
        RectTransform canvas = (RectTransform)hud.transform;
        Scaler(canvas); Sky(canvas);
        Transform content = canvas.Find("SafeArea/StageContent");
        if (content == null) throw new InvalidOperationException("The existing stage content is missing.");
        var hs = new SerializedObject(hud);
        var level = hs.FindProperty("_stageLabel").objectReferenceValue as TMP_Text;
        RectTransform header = Rect(content, "ToonHeader");
        Panel(Rect(header, "Pill"), _skin.HeaderPanel, 0, 0, 613, 195);
        level.transform.SetParent(header, false);
        StyleText(level, 129, true); Place((RectTransform)level.transform, 14, 13, 585, 163);
        Get<ToonStageLabel>(level.gameObject).Configure();
        var tagline = Label(header, "ToonTagline", "TRACE & MIX", 59, -15, 213, 643, 78, true);
        tagline.gameObject.SetActive(number != 1);

        var home = hs.FindProperty("_home").objectReferenceValue as UnityEngine.UI.Button;
        StyleButton(home, _skin.HomeButton);
        Icon(home.transform, "ToonHomeIcon", _skin.HomeIcon, 26, 25, 88, 94, Color.white);
        RectTransform tray = (RectTransform)content.Find("ReloadPanel");
        Panel(tray, _skin.CreamPanel);
        Label(tray, "ToonResetTitle", "RESET", 47, 250, 8, 393, 64, false);
        var all = hs.FindProperty("_allReload").objectReferenceValue as UnityEngine.UI.Button;
        var first = hs.FindProperty("_redReload").objectReferenceValue as UnityEngine.UI.Button;
        var second = hs.FindProperty("_blueReload").objectReferenceValue as UnityEngine.UI.Button;
        SetupReset(all, 60, true); SetupReset(second, 327, false); SetupReset(first, 594, false);

        RectTransform legend = (RectTransform)content.Find("MixPurple");
        Panel(legend, _skin.HeaderPanel);
        UnityEngine.UI.Image firstDot = Icon(legend, "ToonFirst", _skin.Dot, 48, 27, 84, 84, Color.white);
        Label(legend, "ToonPlus", "+", 82, 137, 18, 67, 93, false);
        UnityEngine.UI.Image secondDot = Icon(legend, "ToonSecond", _skin.Dot, 216, 27, 84, 84, Color.white);
        Label(legend, "ToonEquals", "=", 82, 304, 18, 67, 93, false);
        UnityEngine.UI.Image mixedDot = Icon(legend, "ToonMixed", _skin.Dot, 383, 27, 84, 84, Color.white);
        var paletteHud = Get<ToonPaletteHud>(hud.gameObject);
        paletteHud.FirstButton = first.GetComponent<UnityEngine.UI.Image>(); paletteHud.SecondButton = second.GetComponent<UnityEngine.UI.Image>();
        paletteHud.FirstIcon = first.transform.Find("ToonResetIcon").GetComponent<UnityEngine.UI.Image>();
        paletteHud.SecondIcon = second.transform.Find("ToonResetIcon").GetComponent<UnityEngine.UI.Image>();
        paletteHud.FirstSwatch = firstDot; paletteHud.SecondSwatch = secondDot; paletteHud.MixedSwatch = mixedDot;
        paletteHud.ApplyPalette(StageColorPalette.ForStage(number));
        EditorUtility.SetDirty(paletteHud);

        Transform board = manager.TilesList[0].transform.parent;
        var backing = board.Find("BoardBackground").GetComponent<SpriteRenderer>();
        backing.sprite = _skin.BoardPanel; backing.drawMode = SpriteDrawMode.Sliced;
        backing.size = new Vector2(manager.BoardSize.x + .7f, manager.BoardSize.y + .9f);
        backing.transform.localScale = Vector3.one; backing.transform.localPosition = new Vector3(0, -.06f, 0);
        foreach (TileController tile in manager.TilesList)
        {
            var face = tile.transform.Find("TileFace").GetComponent<SpriteRenderer>();
            face.sprite = _skin.TileFace; Fit(face, .92f);
            var frame = tile.transform.Find("TileFlame").GetComponent<SpriteRenderer>();
            frame.sprite = _skin.Frame(tile.RequiredColor); frame.color = Color.white; Fit(frame, .95f);
            var marker = tile.transform.Find("StartMarker").GetComponent<SpriteRenderer>();
            marker.sprite = palettes.StartMarker(tile.StartColor); marker.color = Color.white; Fit(marker, .57f);
            var line = tile.Line;
            if (line != null) { var lr = line.GetComponent<LineRenderer>(); lr.widthMultiplier = .24f; Record(lr); }
            foreach (Transform child in tile.transform) { Record(child); foreach (var component in child.GetComponents<Component>()) Record(component); }
            Record(tile);
        }
        var lines = manager.LineVisuals;
        if (lines != null) { var ls = new SerializedObject(lines); ls.FindProperty("_width").floatValue = .24f; ls.ApplyModifiedPropertiesWithoutUndo(); }
        var layout = hud.GetComponent<StageScreenLayout>();
        Set(layout, "_stageLabel", header);
        StyleChooser(hud.GetComponentInChildren<LineResumeSelector>(true));

        StyleClear(hud, number);
        hs.ApplyModifiedPropertiesWithoutUndo();
        hud.transform.Find("ClearPanel").SetAsLastSibling();
        Canvas.ForceUpdateCanvases(); layout.ApplyLayout();
        EditorUtility.SetDirty(layout); EditorUtility.SetDirty(hud);
    }

    private static void StyleClear(GameHud hud, int number)
    {
        var hs = new SerializedObject(hud);
        var layout = hud.GetComponent<StageScreenLayout>();
        var presentation = hud.ClearPresentation;
        Transform card = hud.transform.Find("ClearPanel/Card");
        Transform actions = card.Find("ClearActions");
        // A camera-space card sits behind the live solved board. Text and actions stay in the overlay canvas.
        GameObject backdrop = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go => go.name == "ToonClearBackdrop");
        if (backdrop == null) backdrop = new GameObject("ToonClearBackdrop", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(CanvasGroup));
        var canvas = backdrop.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main;
        canvas.planeDistance = 19; canvas.sortingOrder = -500; Scaler(backdrop.transform);
        var backdropGroup = backdrop.GetComponent<CanvasGroup>();
        backdropGroup.alpha = 0; backdropGroup.blocksRaycasts = backdropGroup.interactable = false;
        var scrim = Get<UnityEngine.UI.Image>(Rect(backdrop.transform, "Scrim").gameObject);
        Stretch((RectTransform)scrim.transform); scrim.color = new Color(0, .04f, .14f, .52f); scrim.raycastTarget = false;
        var safe = Rect(backdrop.transform, "SafeArea"); Stretch(safe);
        var frame = Rect(safe, "CreamCard"); Panel(frame, _skin.CreamPanel);
        var heading = Rect(card, "ToonClearHeading");
        var headingGroup = Get<CanvasGroup>(heading.gameObject); headingGroup.alpha = 0;
        headingGroup.interactable = headingGroup.blocksRaycasts = false;
        var badge = Rect(heading, "SuccessBadge"); Place(badge, 340.5f, 149, 260, 272);
        Get<ToonClearBadge>(badge.gameObject).Configure(StageColorPalette.ForStage(number));
        Get<ToonClearBadge>(badge.gameObject).raycastTarget = false;
        var clearTitle = Label(heading, "ToonClearTitle", "STAGE CLEAR!", 112, 85, 376, 771, 132, true);
        Set(presentation, "_clearTitle", clearTitle.rectTransform);
        var stage = Label(heading, "ToonClearStage", $"STAGE {number}", 68, 180, 507, 581, 89, true);
        var message = Label(heading, "ToonClearMessage", "Every color in its place.", 38, 100, 602, 741, 53, false);
        hs.FindProperty("_clearStageLabel").objectReferenceValue = stage;
        hs.FindProperty("_clearMessage").objectReferenceValue = message;
        foreach (string old in new[] { "ClearTitle", "ClearMessage" })
            if (card.Find(old) != null) card.Find(old).gameObject.SetActive(false);
        var next = hs.FindProperty("_nextStage").objectReferenceValue as UnityEngine.UI.Button;
        StyleButton(next, _skin.PrimaryButton);
        var nextRect = (RectTransform)next.transform;
        nextRect.anchorMin = nextRect.anchorMax = nextRect.pivot = new Vector2(.5f, .5f);
        nextRect.anchoredPosition = Vector2.zero; nextRect.sizeDelta = new Vector2(710, 184);
        TMP_Text nextLabel = next.transform.Find("Label").GetComponent<TMP_Text>();
        StyleText(nextLabel, 78, false); nextLabel.color = Color.white; nextLabel.text = $"Stage{number + 1}";
        Place((RectTransform)nextLabel.transform, 24, 19, 562, 134);
        Get<ToonStageLabel>(nextLabel.gameObject).Configure();
        Icon(next.transform, "ToonNextIcon", _skin.NextIcon, 594, 46, 82, 82, Color.white);
        Set(layout, "_clearBackdropSafeArea", safe); Set(layout, "_clearFrame", frame); Set(layout, "_clearHeading", heading);
        Set(presentation, "_cardBackdrop", backdropGroup); Set(presentation, "_cardHeading", headingGroup);
        backdrop.SetActive(false);
        hs.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(presentation);
    }

    private static void StyleChooser(LineResumeSelector chooser)
    {
        if (chooser == null) throw new InvalidOperationException("The existing endpoint chooser is missing.");
        var s = new SerializedObject(chooser);
        var card = s.FindProperty("_card").objectReferenceValue as RectTransform;
        Panel(card, _skin.CreamPanel); card.sizeDelta = new Vector2(600, 280);
        var shadow = card.GetComponent<UnityEngine.UI.Shadow>(); if (shadow != null) shadow.enabled = false;
        TMP_Text title = card.Find("Title").GetComponent<TMP_Text>(); StyleText(title, 44, false);
        Place((RectTransform)title.transform, 20, 17, 485, 80);
        var close = s.FindProperty("_close").objectReferenceValue as UnityEngine.UI.Button;
        StyleButton(close, _skin.HeaderPanel); Place((RectTransform)close.transform, 508, 20, 67, 67);
        Transform closeText = close.transform.Find("Label"); if (closeText != null) closeText.gameObject.SetActive(false);
        Icon(close.transform, "ToonCloseIcon", _skin.CloseIcon, 15, 15, 37, 37, Ink);
        foreach (string field in new[] { "_first", "_second" })
        {
            var button = s.FindProperty(field).objectReferenceValue as UnityEngine.UI.Button;
            StyleButton(button, _skin.ColorButtons[field == "_first" ? 0 : 1]);
            Place((RectTransform)button.transform, field == "_first" ? 30 : 310, 110, 260, 136);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true); StyleText(label, 52, false);
            label.color = Color.white; Place((RectTransform)label.transform, 8, 9, 244, 108);
            label.enableAutoSizing = true; label.fontSizeMin = 36; label.fontSizeMax = 52;
            Record(label); Record(button); Record(button.transform);
        }
        foreach (var component in chooser.GetComponentsInChildren<Component>(true)) Record(component);
    }

    private static void SetupReset(UnityEngine.UI.Button button, float x, bool all)
    {
        foreach (Transform child in button.transform) child.gameObject.SetActive(false);
        StyleButton(button, all ? _skin.NeutralButton : _skin.ColorButtons[0]);
        Place((RectTransform)button.transform, x, 76, 240, 212);
        Icon(button.transform, "ToonResetIcon", _skin.ResetIcon, 52, all ? 12 : 34, 136, 136, Color.white).preserveAspect = true;
        if (all) Label(button.transform, "ToonAllLabel", "ALL", 44, 30, 137, 180, 57, true);
    }

    public static ToonSkinAssets BuildAssets()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var skin = AssetDatabase.LoadAssetAtPath<ToonSkinAssets>(AssetPath);
        if (skin == null) { skin = ScriptableObject.CreateInstance<ToonSkinAssets>(); AssetDatabase.CreateAsset(skin, AssetPath); }
        skin.TitleLogo = ImportArtwork("BlendTrailsLogo");
        skin.TitleMix = ImportArtwork("BlendTrailsMix");
        skin.Font = Font();
        string materialPath = Folder + "OutlinedText.mat";
        skin.OutlinedText = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (skin.OutlinedText == null) { skin.OutlinedText = new Material(skin.Font.material); AssetDatabase.CreateAsset(skin.OutlinedText, materialPath); }
        skin.OutlinedText.SetColor(ShaderUtilities.ID_OutlineColor, Ink);
        skin.OutlinedText.SetFloat(ShaderUtilities.ID_OutlineWidth, .42f);
        skin.OutlinedText.SetFloat(ShaderUtilities.ID_FaceDilate, .12f);
        skin.OutlinedText.EnableKeyword("OUTLINE_ON");
        EditorUtility.SetDirty(skin.OutlinedText);
        skin.CreamPanel = PanelSprite("CreamPanel", Cream, false, 54);
        skin.HeaderPanel = PanelSprite("HeaderPanel", Cream, false, 112);
        skin.PrimaryButton = PanelSprite("PrimaryButton", new Color32(0, 139, 255, 255), true, 92);
        skin.NeutralButton = PanelSprite("NeutralButton", new Color32(68, 72, 81, 255), false, 46);
        skin.HomeButton = PanelSprite("HomeButton", new Color32(0, 150, 255, 255), false, 49);
        for (int i = 0; i < 3; i++)
        {
            Color tint = StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor((BaseColor)i));
            skin.ColorButtons[i] = PanelSprite("Reset" + (BaseColor)i, tint, false, 46);
            skin.Starts[i] = Sprite("Start" + (BaseColor)i, 128, 128, (x, y) => Alpha(tint, 60 - Vector2.Distance(new Vector2(x, y), new Vector2(64, 64))), Vector4.zero, 128);
        }
        skin.TileFace = Sprite("TileFace", 256, 256, (x, y) => Alpha(Color.white, -Distance(x, y, new Rect(0, 0, 256, 256), 27)), Vector4.zero, 256);
        for (int i = 0; i < 8; i++)
        {
            Color tint = i == 0 ? Ink : StagePaletteVisuals.UiColor((TileColor)i);
            int stroke = i == 0 ? 9 : 18;
            skin.Frames[i] = Sprite("Frame" + i, 256, 256, (x, y) =>
            {
                float outer = Distance(x, y, new Rect(0, 0, 256, 256), 29);
                float inner = Distance(x, y, new Rect(stroke, stroke, 256 - stroke * 2, 256 - stroke * 2), 29 - stroke);
                return Alpha(tint, Mathf.Min(-outer, inner));
            }, Vector4.zero, 256);
        }
        skin.BoardPanel = skin.CreamPanel;
        skin.Dot = Sprite("Dot", 128, 128, (x, y) => Alpha(Color.white, 62 - Vector2.Distance(new Vector2(x, y), new Vector2(64, 64))), Vector4.zero, 128);
        skin.ResetIcon = ImportArtwork("ResetArrowGenerated", 256);
        skin.NextIcon = Sprite("NextIcon", 128, 128, (x, y) =>
        {
            Vector2 p = new Vector2(x, y), tip = new Vector2(106, 64);
            float distance = Mathf.Min(Segment(p, new Vector2(20, 64), tip),
                Mathf.Min(Segment(p, new Vector2(72, 99), tip), Segment(p, tip, new Vector2(72, 29))));
            return Alpha(Color.white, 11 - distance);
        }, Vector4.zero, 128);
        skin.PlayIcon = Sprite("PlayIcon", 128, 144, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            if (!Triangle(p, new Vector2(5, 4), new Vector2(5, 140), new Vector2(122, 72))) return Color.clear;
            return Triangle(p, new Vector2(18, 27), new Vector2(18, 117), new Vector2(98, 72)) ? Color.white : Ink;
        }, Vector4.zero, 128);
        skin.HomeIcon = Sprite("HomeIcon", 128, 128, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            bool roof = Triangle(p, new Vector2(2, 63), new Vector2(64, 122), new Vector2(126, 63));
            bool baseHouse = new Rect(23, 10, 82, 63).Contains(p);
            if (!roof && !baseHouse) return Color.clear;
            bool inside = Triangle(p, new Vector2(20, 66), new Vector2(64, 108), new Vector2(108, 66)) || new Rect(34, 21, 60, 57).Contains(p);
            if (new Rect(54, 12, 23, 34).Contains(p)) inside = false;
            return inside ? Color.white : Ink;
        }, Vector4.zero, 128);
        skin.CloseIcon = Sprite("CloseIcon", 128, 128, (x, y) =>
            Alpha(Color.white, Mathf.Max(10 - Segment(new Vector2(x, y), new Vector2(24, 24), new Vector2(104, 104)),
                10 - Segment(new Vector2(x, y), new Vector2(24, 104), new Vector2(104, 24)))), Vector4.zero, 128);
        EditorUtility.SetDirty(skin); AssetDatabase.SaveAssets(); ToonSkinAssets.ResetCache();
        return skin;
    }

    private static TMP_FontAsset Font()
    {
        const string target = "Assets/Fonts/LilitaOne/LilitaOne SDF.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(target);
        if (font != null) return font;
        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/LilitaOne/LilitaOne-Regular.ttf");
        font = TMP_FontAsset.CreateFontAsset(source);
        font.name = "LilitaOne SDF";
        if (!font.TryAddCharacters(string.Concat(Enumerable.Range(32, 95).Select(i => (char)i)), out string missing))
            throw new InvalidOperationException("Missing UI glyphs: " + missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(font, target);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (Texture2D texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
        EditorUtility.SetDirty(font);
        return font;
    }
    private static Sprite ImportArtwork(string name, int maximumSize = 2048)
    {
        string file = Folder + name + ".png";
        if (!File.Exists(file)) throw new FileNotFoundException("Generated UI artwork is required.", file);
        var importer = (TextureImporter)AssetImporter.GetAtPath(file);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100; importer.spriteBorder = Vector4.zero;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.maxTextureSize = maximumSize; importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(file);
    }
    private static Sprite PanelSprite(string name, Color fill, bool creamRim, float radius)
    {
        return Sprite(name, 256, 268, (x, y) =>
        {
            Rect body = new Rect(0, 12, 256, 256);
            Color result = Alpha(new Color32(0, 137, 221, 255), -Distance(x, y, new Rect(0, 0, 256, 256), radius));
            result = Over(result, Alpha(Ink, -Distance(x, y, body, radius)));
            Rect inner = new Rect(12, 24, 232, 232);
            if (creamRim)
            {
                result = Over(result, Alpha(Cream, -Distance(x, y, inner, radius - 12)));
                inner = new Rect(25, 37, 206, 206);
            }
            float light = Mathf.Clamp01((y - 70) / 198f) * .06f;
            return Over(result, Alpha(Color.Lerp(fill, Color.white, light), -Distance(x, y, inner, radius - (creamRim ? 25 : 12))));
        }, new Vector4(96, 108, 96, 96), 100);
    }
    private static Sprite Sprite(string name, int width, int height, Func<float, float, Color> render, Vector4 border, float ppu)
    {
        string file = Folder + name + ".png";
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) pixels[y * width + x] = render(x + .5f, y + .5f);
        texture.SetPixels(pixels); texture.Apply();
        File.WriteAllBytes(file, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(file);
        var importer = (TextureImporter)AssetImporter.GetAtPath(file);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu; importer.spriteBorder = border;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(file);
    }
    private static float Distance(float x, float y, Rect r, float radius)
    {
        radius = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
        Vector2 q = new Vector2(Mathf.Abs(x - r.center.x) - r.width * .5f + radius, Mathf.Abs(y - r.center.y) - r.height * .5f + radius);
        return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
    }
    private static Color Alpha(Color color, float coverage) { color.a *= Mathf.Clamp01(coverage + .5f); return color; }
    private static Color Over(Color a, Color b)
    {
        float alpha = b.a + a.a * (1 - b.a);
        if (alpha <= 0) return Color.clear;
        Color c = (b * b.a + a * a.a * (1 - b.a)) / alpha; c.a = alpha; return c;
    }
    private static float Segment(Vector2 p, Vector2 a, Vector2 b) => Vector2.Distance(p, a + (b - a) * Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude));
    private static bool Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
        float x = Cross(b - a, p - a), y = Cross(c - b, p - b), z = Cross(a - c, p - c);
        return (x >= 0 && y >= 0 && z >= 0) || (x <= 0 && y <= 0 && z <= 0);
    }
    private static void Scaler(Transform canvas)
    {
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(941, 1672); scaler.matchWidthOrHeight = 0;
        EditorUtility.SetDirty(scaler);
    }
    private static void Sky(Transform canvas)
    {
        // Overlay graphics would obscure the world-space tiles. Render the sky behind their camera depth.
        GameObject background = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go => go.name == "ToonBackgroundCanvas");
        if (background == null) background = new GameObject("ToonBackgroundCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        var layer = background.GetComponent<Canvas>();
        layer.renderMode = RenderMode.ScreenSpaceCamera; layer.worldCamera = Camera.main; layer.planeDistance = 20; layer.sortingOrder = -1000;
        Scaler(background.transform);
        var old = canvas.Find("ToonSky") as RectTransform;
        var sky = old != null ? old : Rect(background.transform, "ToonSky");
        sky.SetParent(background.transform, false); Stretch(sky); Get<CanvasRenderer>(sky.gameObject);
        Get<ToonSkyGraphic>(sky.gameObject).raycastTarget = false;
        if (Camera.main != null) Camera.main.backgroundColor = GlobalScripts.BackgroundColor;
    }
    private static void StyleButton(UnityEngine.UI.Button button, Sprite sprite)
    {
        if (button == null) throw new InvalidOperationException("A wired button is missing.");
        var image = Panel((RectTransform)button.transform, sprite); image.raycastTarget = true;
        button.targetGraphic = image;
        var colors = button.colors; colors.normalColor = colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(.97f, .97f, 1); colors.pressedColor = new Color(.82f, .85f, .92f);
        colors.disabledColor = new Color(.82f, .82f, .86f, .7f); button.colors = colors;
        var navigation = button.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = navigation;
        Record(button); Record(image);
    }
    private static UnityEngine.UI.Image Panel(RectTransform rect, Sprite sprite)
    {
        var image = Get<UnityEngine.UI.Image>(rect.gameObject); image.sprite = sprite; image.material = null;
        image.type = UnityEngine.UI.Image.Type.Sliced; image.preserveAspect = false; image.color = Color.white; image.raycastTarget = false;
        return image;
    }
    private static void Panel(RectTransform rect, Sprite sprite, float x, float y, float w, float h) { Panel(rect, sprite); Place(rect, x, y, w, h); }
    private static UnityEngine.UI.Image Icon(Transform parent, string name, Sprite sprite, float x, float y, float w, float h, Color tint)
    {
        var rect = Rect(parent, name); Place(rect, x, y, w, h); rect.gameObject.SetActive(true);
        var image = Get<UnityEngine.UI.Image>(rect.gameObject); image.sprite = sprite; image.type = UnityEngine.UI.Image.Type.Simple;
        image.color = tint; image.raycastTarget = false; return image;
    }
    private static TMP_Text Label(Transform parent, string name, string value, float size, float x, float y, float w, float h, bool outlined)
    {
        var rect = Rect(parent, name); Place(rect, x, y, w, h); rect.gameObject.SetActive(true);
        var text = Get<TextMeshProUGUI>(rect.gameObject); StyleText(text, size, outlined); text.text = value; return text;
    }
    private static void StyleText(TMP_Text text, float size, bool outlined)
    {
        text.font = _skin.Font; text.fontSharedMaterial = outlined ? _skin.OutlinedText : _skin.Font.material;
        text.fontStyle = FontStyles.Normal; text.fontSize = size; text.color = outlined ? Color.white : Ink;
        text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = false; text.overflowMode = TextOverflowModes.Overflow; text.margin = Vector4.zero; text.raycastTarget = false;
    }
    private static RectTransform Rect(Transform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform; if (existing != null) return existing;
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
    }
    private static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x + w * .5f, -y - h * .5f); rect.sizeDelta = new Vector2(w, h); rect.localScale = Vector3.one;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void Fit(SpriteRenderer renderer, float width) => renderer.transform.localScale = Vector3.one * (width / renderer.sprite.bounds.size.x);
    private static T Get<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var settings = new SerializedObject(target); settings.FindProperty(field).objectReferenceValue = value; settings.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Record(UnityEngine.Object value)
    {
        EditorUtility.SetDirty(value);
        if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }
    private static void Save(Scene scene) { EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path); }
}
