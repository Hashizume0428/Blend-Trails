using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

/// <summary>Independent transcription of the supplied reference diagrams, including their example solutions.</summary>
public static class StageTestReference
{
    private sealed class Reference
    {
        public readonly string Red, Blue, RedFrames, BlueFrames, PurpleFrames;
        public Reference(string red, string blue, string redFrames, string blueFrames, string purpleFrames)
        { Red = red; Blue = blue; RedFrames = redFrames; BlueFrames = blueFrames; PurpleFrames = purpleFrames; }
    }
    private static readonly Reference[] Stages =
    {
        new Reference("11 15 25 21", "45 41 31 35", "15 21", "41 35", ""),
        new Reference("11 15 45 44", "34 24 23 43 42 22 21 41", "15 45", "43", ""),
        new Reference("15 45 44 14 13 33", "41 11 12 42 43 23", "13", "43", "23 33"),
        new Reference("22 23 13 11 41 44", "32 33 13 15 45 44 24", "11", "15 33", "13 23 44"),
        new Reference("35 15 14 24 21 41 45", "11 14 34 31", "45", "32", "14 31"),
        new Reference("11 14 24 21 31 33 43 41", "15 14 24 25 35 34 44 45", "13 43", "44", "14"),
        new Reference("33 32 22 24 14", "13 11 41 45 15 14 34", "23", "41", "14 24"),
        new Reference("12 32 34 14 15 45 43", "11 41 45 15 13 33", "", "23", "14 15 25 35 43 44 45"),
        new Reference("33 31 41 44 14 11", "23 21 41 45 15 14", "24", "15", "14 31"),
        new Reference("35 45 44 24 23 13 12 32 31", "11 41 43 13 15 25 24", "22", "", "13 24 31")
    };
    private static Vector2Int[] Cells(string values) => values.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(value => new Vector2Int(value[1] - '1', value[0] - '1')).ToArray();

    public static void AssertLayout(GameManager game, int number)
    {
        Reference reference = Stages[number - 1];
        Assert.That(game.BoardSize, Is.EqualTo(new Vector2Int(5, 4)));
        Assert.That(game.TilesList.Count, Is.EqualTo(20));
        var red = Cells(reference.RedFrames); var blue = Cells(reference.BlueFrames); var purple = Cells(reference.PurpleFrames);
        Vector2Int redStart = Cells(reference.Red)[0], blueStart = Cells(reference.Blue)[0];
        Assert.That(game.TilesList.Count(tile => tile.IsStartTile), Is.EqualTo(2));
        foreach (TileController tile in game.TilesList)
        {
            Vector2 point = Camera.main.WorldToScreenPoint(tile.transform.position);
            Assert.That(Screen.safeArea.Contains(point), Is.True, $"Stage{number} {tile.name} must fit the screen");
            Assert.That(Physics2D.OverlapPoint(tile.transform.position)?.GetComponent<TileController>(), Is.EqualTo(tile));
            Assert.That(tile.IsStartTile, Is.EqualTo(tile.Coordinate == redStart || tile.Coordinate == blueStart), tile.name);
            if (tile.IsStartTile) Assert.That(tile.StartColor, Is.EqualTo(tile.Coordinate == redStart ? BaseColor.Red : BaseColor.Blue));
            TileColor expected = purple.Contains(tile.Coordinate) ? TileColor.Purple : red.Contains(tile.Coordinate) ? TileColor.Red :
                blue.Contains(tile.Coordinate) ? TileColor.Blue : TileColor.None;
            Assert.That(tile.RequiredColor, Is.EqualTo(expected), $"Stage{number} {tile.name} frame");
            Assert.That(tile.ForbiddenColor, Is.EqualTo(TileColor.None));
            Assert.That(tile.transform.Find("TileFlame").GetComponent<SpriteRenderer>().sprite, Is.EqualTo(GlobalScripts.GetFrameImage(expected)));
            Assert.That(tile.transform.Find("TileFlame").GetComponent<SpriteRenderer>().enabled, Is.True);
        }
        Assert.That(GameObject.Find("SharedPurpleFrame"), Is.Null);
    }

    public static IEnumerator Solve(GameManager game)
    {
        int number = game.GetComponent<StageClearNavigation>().StageNumber;
        foreach (BaseColor color in new[] { BaseColor.Red, BaseColor.Blue })
        {
            DrawLine(game, color);
            Assert.That(GameObject.Find("ClearPanel"), Is.Null, "Clear must wait for release.");
            yield return game.CommitLineAsync().ToCoroutine();
        }
        Assert.That(game.State, Is.EqualTo(GameState.Cleared), $"Stage{number} must be solvable using its reference path.");
        Assert.That(game.CorrectTileCount, Is.EqualTo(20));
        Assert.That(game.StageClearCheck(), Is.True);
    }

    public static void DrawLine(GameManager game, BaseColor color)
    {
        int number = game.GetComponent<StageClearNavigation>().StageNumber;
        Reference reference = Stages[number - 1];
        Vector2Int[] cells = Cells(color == BaseColor.Red ? reference.Red : reference.Blue);
        Assert.That(game.TryBeginLine(game.GetTile(cells[0]).TileIndex), Is.True, $"Stage{number} begin");
        for (int i = 1; i < cells.Length; i++)
            Assert.That(game.TryExtendLine(game.GetTile(cells[i]).TileIndex), Is.True, $"Stage{number} extend to {cells[i]}");
    }
}
