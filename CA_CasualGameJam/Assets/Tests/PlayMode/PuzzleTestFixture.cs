using System;
using System.Linq;
using UnityEngine;

/// <summary>Synthetic conditions for rule regressions, independent of the production stage layouts.</summary>
public static class PuzzleTestFixture
{
    public static void ApplyRegressionConditions(GameManager game)
    {
        Sprite blueMarker = game.TilesList.First(tile => tile.IsStartTile && tile.StartColor == BaseColor.Blue)
            .transform.Find("StartMarker").GetComponent<SpriteRenderer>().sprite;
        foreach (TileController tile in game.TilesList)
        {
            int x = tile.Coordinate.x, y = tile.Coordinate.y;
            BaseColor[] required = Array.Empty<BaseColor>();
            if ((x == 3 && y == 0) || (x == 2 && y == 2)) required = new[] { BaseColor.Red };
            if (x == 0 && y == 2) required = new[] { BaseColor.Blue };
            if (x == 3 && (y == 1 || y == 2)) required = new[] { BaseColor.Red, BaseColor.Blue };
            TileColor ban = x == 1 && y == 0 ? TileColor.Blue : x == 1 && y == 3 ? TileColor.Red : TileColor.None;
            tile.Configure(tile.TileIndex, tile.Coordinate, x == 0 && (y == 0 || y == 3), y == 3 ? BaseColor.Blue : BaseColor.Red, required, ban);
            if (x == 0 && y == 3) tile.transform.Find("StartMarker").GetComponent<SpriteRenderer>().sprite = blueMarker;
        }
        game.Initialize();
    }
}
