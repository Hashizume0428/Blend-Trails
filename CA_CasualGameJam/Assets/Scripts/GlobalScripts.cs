using UnityEngine;

public static class GlobalScripts 
{
    public const int Red = 0;
    public const int Blue = 1;
    public const int Yellow = 2;
    public static readonly Color BackgroundColor = new Color32(242, 238, 238, 255);
    
    public static readonly Color NormalFaceColor = new Color32(249, 249, 249, 255); // #F9F9F9
    public static readonly Color RedFaceColor = new Color32(254, 159, 159, 255); // #FE9F9F
    public static readonly Color BlueFaceColor = new Color32(156, 198, 254, 255); // #9CC6FE
    public static readonly Color YellowFaceColor = new Color32(251, 225, 138, 255); // #FBE18A
    public static readonly Color PurpleFaceColor = new Color32(229, 163, 251, 255); // #E5A3FB
    public static readonly Color GreenFaceColor = new Color32(141, 223, 130, 255); // #8DDF82
    public static readonly Color OrangeFaceColor = new Color32(253, 196, 146, 255); // #FDC492
    
    public static Sprite NormalFlameImage;
    public static Sprite RedFlameImage;
    public static Sprite BlueFlameImage;
    public static Sprite YellowFlameImage;
    public static Sprite PurpleFlameImage;
    public static Sprite GreenFlameImage;
    public static Sprite OrangeFlameImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadFlameImages()
    {
        NormalFlameImage = LoadFlameImage("枠ノーマル");
        RedFlameImage = LoadFlameImage("枠赤");
        BlueFlameImage = LoadFlameImage("枠青");
        YellowFlameImage = LoadFlameImage("枠黄");
        PurpleFlameImage = LoadFlameImage("枠紫");
        GreenFlameImage = LoadFlameImage("枠緑");
        OrangeFlameImage = LoadFlameImage("枠オレンジ");
    }

    public static Color GetFaceColor(TileColor color)
    {
        switch (color)
        {
            case TileColor.Red: return RedFaceColor;
            case TileColor.Blue: return BlueFaceColor;
            case TileColor.Yellow: return YellowFaceColor;
            case TileColor.Purple: return PurpleFaceColor;
            case TileColor.Green: return GreenFaceColor;
            case TileColor.Orange: return OrangeFaceColor;
            default: return NormalFaceColor;
        }
    }
    public static Color GetLineColor(BaseColor color)
    {
        switch (color)
        {
            case BaseColor.Red: return new Color32(235, 61, 79, 255);
            case BaseColor.Blue: return new Color32(49, 95, 235, 255);
            default: return new Color32(232, 174, 32, 255);
        }
    }
    public static Color GetLineColor(TileColor color)
    {
        switch (color)
        {
            case TileColor.Red: return GetLineColor(BaseColor.Red);
            case TileColor.Blue: return GetLineColor(BaseColor.Blue);
            case TileColor.Yellow: return GetLineColor(BaseColor.Yellow);
            case TileColor.Purple: return new Color32(164, 74, 218, 255);
            case TileColor.Green: return new Color32(45, 166, 91, 255);
            case TileColor.Orange: return new Color32(235, 139, 40, 255);
            default: return Color.clear;
        }
    }
    public static Sprite GetFrameImage(TileColor color)
    {
        if (NormalFlameImage == null) LoadFlameImages();
        switch (color)
        {
            case TileColor.Red: return RedFlameImage;
            case TileColor.Blue: return BlueFlameImage;
            case TileColor.Yellow: return YellowFlameImage;
            case TileColor.Purple: return PurpleFlameImage;
            case TileColor.Green: return GreenFlameImage;
            case TileColor.Orange: return OrangeFlameImage;
            default: return NormalFlameImage;
        }
    }

    private static Sprite LoadFlameImage(string imageName)
    {
        // Multipleでインポートされた画像から、切り出されたスプライトを取得する。
        Sprite[] sprites = Resources.LoadAll<Sprite>($"Image/flame/{imageName}");
        if (sprites.Length == 0)
        {
            Debug.LogError($"枠のスプライトが見つかりません: {imageName}");
            return null;
        }

        return sprites[0];
    }
}
