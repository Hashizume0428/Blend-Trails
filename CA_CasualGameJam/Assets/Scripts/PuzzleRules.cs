using System;
using System.Collections.Generic;
using UnityEngine;

public enum BaseColor { Red = 0, Blue = 1, Yellow = 2 }
[Flags]
public enum TileColor
{
    None = 0, Red = 1, Blue = 2, Yellow = 4,
    Purple = Red | Blue, Green = Blue | Yellow, Orange = Red | Yellow
}
public enum PathStep { Reject, Append, Backtrack }

public static class PuzzleRules
{
    public static TileColor ToTileColor(BaseColor color) => (TileColor)(1 << (int)color);
    public static TileColor Mix(IEnumerable<BaseColor> colors)
    {
        TileColor result = TileColor.None;
        foreach (BaseColor color in colors) result |= ToTileColor(color);
        return result;
    }
    public static bool IsSupportedColor(TileColor color) => (int)color > 0 && (int)color < 7;
    public static bool CanAddColor(TileColor current, BaseColor added) =>
        Enum.IsDefined(typeof(BaseColor), added) && IsSupportedColor(current | ToTileColor(added));
    public static bool IsTileCorrect(TileColor current, bool hasRequiredColor, TileColor required, TileColor forbidden)
    {
        // Compare the finished color with the ban, not its ingredients.
        return IsSupportedColor(current) && current != forbidden && (!hasRequiredColor || current == required);
    }
    public static bool TryGetAxisStep(Vector2Int from, Vector2Int to, out Vector2Int step)
    {
        Vector2Int delta = to - from;
        step = Vector2Int.zero;
        if (delta == Vector2Int.zero || (delta.x != 0 && delta.y != 0)) return false;
        step = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
        return true;
    }
    public static PathStep ClassifyStep(IReadOnlyList<int> path, int candidate, Vector2Int from, Vector2Int to)
    {
        if (path.Count == 0 || Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y) != 1) return PathStep.Reject;
        if (path.Count >= 2 && path[path.Count - 2] == candidate) return PathStep.Backtrack;
        for (int i = 0; i < path.Count; i++) if (path[i] == candidate) return PathStep.Reject;
        return PathStep.Append;
    }
}
