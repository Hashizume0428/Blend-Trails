using UnityEngine;

/// <summary>Changes only the playable colors; sky, panels and navigation keep the common B skin.</summary>
public sealed class ToonPaletteHud : MonoBehaviour
{
    public UnityEngine.UI.Image FirstButton, SecondButton, FirstIcon, SecondIcon;
    public UnityEngine.UI.Image FirstSwatch, SecondSwatch, MixedSwatch;
    private static readonly int WhiteHaze = Shader.PropertyToID("_WhiteHaze");
    private const float DisabledWhiteHaze = .4f;
    private UnityEngine.UI.Button _yellowButton;
    private UnityEngine.UI.Image _yellowFace, _yellowIcon;
    private Material _yellowMaterial;
    private Color _originalDisabledColor;
    private bool _wasInteractable;

    public void ApplyPalette(StageColorPalette palette)
    {
        ToonSkinAssets skin = ToonSkinAssets.Current;
        if (skin == null) return;
        ReleaseDisabledVisuals();
        FirstButton.sprite = skin.ColorButtons[(int)palette.First];
        SecondButton.sprite = skin.ColorButtons[(int)palette.Second];
        FirstButton.material = SecondButton.material = null;
        FirstButton.color = SecondButton.color = Color.white;
        FirstIcon.color = palette.First == BaseColor.Yellow ? ToonSkinAssets.Ink : Color.white;
        SecondIcon.color = palette.Second == BaseColor.Yellow ? ToonSkinAssets.Ink : Color.white;
        FirstSwatch.color = StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(palette.First));
        SecondSwatch.color = StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(palette.Second));
        MixedSwatch.color = StagePaletteVisuals.UiColor(palette.Mixed);
        if (!Application.isPlaying || skin.DisabledReloadShader == null) return;
        if (palette.First != BaseColor.Yellow && palette.Second != BaseColor.Yellow) return;
        _yellowFace = palette.First == BaseColor.Yellow ? FirstButton : SecondButton;
        _yellowIcon = palette.First == BaseColor.Yellow ? FirstIcon : SecondIcon;
        _yellowButton = _yellowFace.GetComponent<UnityEngine.UI.Button>();
        // One white wash covers both sprites, including the face's border and shadow.
        // Suppress the default disabled tint so it does not compound the wash.
        var colors = _yellowButton.colors;
        _originalDisabledColor = colors.disabledColor;
        colors.disabledColor = Color.white;
        _yellowButton.colors = colors;
        _yellowMaterial = new Material(skin.DisabledReloadShader) { name = "Yellow Reload Disabled" };
        _yellowFace.material = _yellowIcon.material = _yellowMaterial;
        _wasInteractable = !_yellowButton.IsInteractable();
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (_yellowMaterial == null || _yellowButton == null) return;
        bool interactable = _yellowButton.IsInteractable();
        if (_wasInteractable == interactable) return;
        _wasInteractable = interactable;
        _yellowMaterial.SetFloat(WhiteHaze, interactable ? 0 : DisabledWhiteHaze);
    }

    private void ReleaseDisabledVisuals()
    {
        if (_yellowMaterial == null) return;
        if (_yellowButton != null)
        {
            var colors = _yellowButton.colors;
            colors.disabledColor = _originalDisabledColor;
            _yellowButton.colors = colors;
        }
        if (_yellowFace != null) _yellowFace.material = null;
        if (_yellowIcon != null) _yellowIcon.material = null;
        Destroy(_yellowMaterial);
        _yellowMaterial = null;
    }

    private void OnDestroy() => ReleaseDisabledVisuals();
}
