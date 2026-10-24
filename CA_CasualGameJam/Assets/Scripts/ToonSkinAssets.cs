using TMPro;
using UnityEngine;

/// <summary>The approved B skin. References keep every UI asset available in player builds.</summary>
public sealed class ToonSkinAssets : ScriptableObject
{
    public Texture2D TitleArtwork;
    public TMP_FontAsset Font;
    public Material OutlinedText;
    public Sprite CreamPanel, HeaderPanel, PrimaryButton, NeutralButton, HomeButton;
    public Sprite TileFace, BoardPanel, ResetIcon, HomeIcon, PlayIcon, NextIcon, CloseIcon, Dot;
    public Sprite[] ColorButtons = new Sprite[3];
    public Sprite[] Frames = new Sprite[8];
    public Sprite[] Starts = new Sprite[3];
    private static ToonSkinAssets _current;
    public static ToonSkinAssets Current => _current != null ? _current : (_current = Resources.Load<ToonSkinAssets>("ToonSkin"));
    public static readonly Color Ink = new Color32(7, 27, 82, 255);
    public static readonly Color Cream = new Color32(255, 246, 216, 255);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCache() => _current = null;
    public Sprite Frame(TileColor color) => Frames[(int)color & 7];
}
