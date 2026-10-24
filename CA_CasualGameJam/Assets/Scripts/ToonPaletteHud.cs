using UnityEngine;

/// <summary>Changes only the playable colors; sky, panels and navigation keep the common B skin.</summary>
public sealed class ToonPaletteHud : MonoBehaviour
{
    public UnityEngine.UI.Image FirstButton, SecondButton, FirstIcon, SecondIcon;
    public UnityEngine.UI.Image FirstSwatch, SecondSwatch, MixedSwatch;
    public void ApplyPalette(StageColorPalette palette)
    {
        ToonSkinAssets skin = ToonSkinAssets.Current;
        if (skin == null) return;
        FirstButton.sprite = skin.ColorButtons[(int)palette.First];
        SecondButton.sprite = skin.ColorButtons[(int)palette.Second];
        FirstButton.material = SecondButton.material = null;
        FirstButton.color = SecondButton.color = Color.white;
        FirstIcon.color = palette.First == BaseColor.Yellow ? ToonSkinAssets.Ink : Color.white;
        SecondIcon.color = palette.Second == BaseColor.Yellow ? ToonSkinAssets.Ink : Color.white;
        FirstSwatch.color = StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(palette.First));
        SecondSwatch.color = StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(palette.Second));
        MixedSwatch.color = StagePaletteVisuals.UiColor(palette.Mixed);
    }
}
