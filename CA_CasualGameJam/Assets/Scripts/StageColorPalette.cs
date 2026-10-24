using System;

/// <summary>Maps the two roles in an authored red/blue puzzle to this play session's colors.</summary>
public readonly struct StageColorPalette
{
    public const int StagesPerPalette = 5;
    public BaseColor First { get; }
    public BaseColor Second { get; }
    public TileColor PlayableColors => PuzzleRules.ToTileColor(First) | PuzzleRules.ToTileColor(Second);
    public TileColor Mixed => PlayableColors;

    private StageColorPalette(BaseColor first, BaseColor second) { First = first; Second = second; }

    public static StageColorPalette ForStage(int stageNumber)
    {
        int index = ((Math.Max(1, stageNumber) - 1) / StagesPerPalette) % 3;
        return index == 0 ? new StageColorPalette(BaseColor.Red, BaseColor.Blue) :
            index == 1 ? new StageColorPalette(BaseColor.Red, BaseColor.Yellow) :
            new StageColorPalette(BaseColor.Blue, BaseColor.Yellow);
    }

    public bool Contains(BaseColor color) => color == First || color == Second;
    public bool Contains(TileColor color) => PuzzleRules.IsSupportedColor(color) && (color & ~PlayableColors) == 0;

    public BaseColor Map(BaseColor authoredColor)
    {
        switch (authoredColor)
        {
            case BaseColor.Red: return First;
            case BaseColor.Blue: return Second;
            // Complete the permutation without merging two different authored ingredients.
            case BaseColor.Yellow: return (BaseColor)(3 - (int)First - (int)Second);
            default: throw new ArgumentOutOfRangeException(nameof(authoredColor));
        }
    }

    public TileColor Map(TileColor authoredColor)
    {
        TileColor result = authoredColor & ~(TileColor)7;
        if ((authoredColor & TileColor.Red) != 0) result |= PuzzleRules.ToTileColor(First);
        if ((authoredColor & TileColor.Blue) != 0) result |= PuzzleRules.ToTileColor(Second);
        if ((authoredColor & TileColor.Yellow) != 0) result |= PuzzleRules.ToTileColor(Map(BaseColor.Yellow));
        return result;
    }
}
