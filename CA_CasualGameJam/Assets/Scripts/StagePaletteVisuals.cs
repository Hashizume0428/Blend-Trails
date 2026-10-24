using UnityEngine;

/// <summary>Shared asset references, included in players through each stage's GameManager.</summary>
[CreateAssetMenu(menuName = "Color Puzzle/Palette Visuals")]
public class StagePaletteVisuals : ScriptableObject
{
    [SerializeField] private Sprite[] _startMarkers = new Sprite[3];
    [SerializeField] private Sprite[] _forbiddenMarkers = new Sprite[3];
    [SerializeField] private Shader _uiPaletteShader;

    public Sprite StartMarker(BaseColor color) => _startMarkers[(int)color];
    public Sprite ForbiddenMarker(TileColor color)
    {
        switch (color)
        {
            case TileColor.Red: return _forbiddenMarkers[(int)BaseColor.Red];
            case TileColor.Blue: return _forbiddenMarkers[(int)BaseColor.Blue];
            case TileColor.Yellow: return _forbiddenMarkers[(int)BaseColor.Yellow];
            default: return null;
        }
    }

    public Material CreateUiMaterial(StageColorPalette palette)
    {
        if (_uiPaletteShader == null) return null;
        var material = new Material(_uiPaletteShader) { name = $"Stage UI {palette.Mixed}" };
        // Vectors keep the authored sRGB values unchanged in both project color spaces.
        material.SetVector("_FirstColor", UiColor(PuzzleRules.ToTileColor(palette.First)));
        material.SetVector("_SecondColor", UiColor(PuzzleRules.ToTileColor(palette.Second)));
        material.SetVector("_MixedColor", UiColor(palette.Mixed));
        return material;
    }

    public static Color UiColor(TileColor color)
    {
        switch (color)
        {
            case TileColor.Red: return new Color32(229, 75, 75, 255);
            case TileColor.Blue: return new Color32(75, 142, 229, 255);
            case TileColor.Purple: return new Color32(191, 75, 229, 255);
            case TileColor.Yellow: return new Color32(229, 190, 75, 255);
            case TileColor.Orange: return new Color32(229, 148, 75, 255);
            case TileColor.Green: return new Color32(75, 181, 101, 255);
            default: return Color.white;
        }
    }
}
