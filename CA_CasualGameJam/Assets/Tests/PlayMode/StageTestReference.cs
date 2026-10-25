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
        new Reference("35 15 14 24 21 41 45", "11 14 34 31", "25 42 43", "12 32", "14 24 31"),
        new Reference("11 14 24 21 31 33 43 41", "15 14 24 25 35 34 44 45", "13 43", "44", "14 24"),
        new Reference("33 32 22 24 14", "13 11 41 45 15 14 34", "23", "41", "14 24"),
        new Reference("12 32 34 14 15 45 43", "11 41 45 15 13 23", "", "23", "14 15 25 35 43 44 45"),
        new Reference("33 31 41 44 14 11", "23 21 41 45 15 14", "24", "15", "14 31 41 42 43 44"),
        new Reference("35 45 44 24 23 13 12 32 31", "11 41 43 13 15 25 24", "22", "", "13 23 24 31"),
        new Reference("13 12 11 21 22 23 24 34 33 32 31 41", "42 43 44 45 35 25 15 14 24 23 22", "31 34", "14 45", "22 23 24"),
        new Reference("32 31 21 11 12 13 14 15 25 35 45 44 34 24 23", "41 42 43 44 34 33 23 22 12", "24 45", "22 33", "12 23 34 44"),
        new Reference("15 25 24 23 22 32 33 34 35 45 44 43", "14 13 12 11 21 31 41 42 32 22 23 33", "43 45", "11 42", "22 23 32 33"),
        new Reference("23 24 14 15 25 35 45 44 43 42 32 22", "12 13 14 24 34 33 32 42 41 31 21 11", "22 45", "11 41", "14 24 32 42"),
        new Reference("33 23 24 34 35 25 15 14 13 12 22 32", "11 21 31 41 42 43 44 45 35 34 24 23 13", "12", "45", "13 23 24 34 35"),
        new Reference("41 42 43 33 34 35 25 15 14 13 23 22", "24 23 13 12 11 21 31 32 33 43 44 45", "15 35", "31 45", "13 23 33 43"),
        new Reference("21 22 32 31 41 42 43 33 34 35 45", "11 12 22 23 13 14 15 25 24 34 44", "33 35", "15 24", "22 34"),
        new Reference("13 12 22 23 33 34 35 45 44 43 42 32", "41 31 21 11 12 22 32 33 34 24 25 15 14", "42", "15", "12 22 32 33 34"),
        new Reference("41 42 43 33 34 44 45 35 25 24 23 13 12", "15 14 13 23 22 12 11 21 31 32 33 43 44", "25", "31", "12 13 23 33 43 44"),
        new Reference("15 14 24 34 33 43 42 32 22 21 11 12 13", "41 31 32 22 23 33 43 44 45 35 25 24 14", "11", "25", "14 22 24 32 33 43"),
        new Reference("15 14 13 12 22 23 33 43 44 45 35 25", "11 21 22 23 24 34 33 32 31 41 42", "43 45", "34 41", "22 23 33"),
        new Reference("45 35 34 33 32 42 41 31 21 11 12 13", "43 44 34 24 25 15 14 13 23 22 21", "11 41", "15 23", "13 21 34"),
        new Reference("11 21 22 23 33 32 31 41 42 43 44 34 24", "45 35 25 15 14 24 34 33 32 22 12 13 23", "44", "13", "22 23 24 32 33 34"),
        new Reference("32 33 34 44 45 35 25 24 23 13 12 11", "15 14 13 12 22 21 31 41 42 43 33 34", "23 25", "41 43", "12 13 33 34"),
        new Reference("15 14 13 23 22 21 31 41 42 43 44 45 35 25 24", "11 12 13 23 22 32 33 34 24", "25 45", "32 34", "13 22 23 24"),
        new Reference("41 31 21 22 23 33 34 35 25 24 14 15", "13 12 11 21 22 32 42 43 44 34 35 45", "14 25", "42 44", "21 22 34 35"),
        new Reference("21 31 41 42 43 44 34 33 23 22 12 13 14", "11 12 22 32 33 23 24 34 44 45 35 25 15", "14", "45", "12 22 23 33 34 44"),
        new Reference("21 22 12 13 14 15 25 24 34 35 45 44 43", "11 12 13 23 22 32 31 41 42 43 44 34 33", "45", "41", "12 13 22 34 43 44"),
        new Reference("14 24 34 33 43 42 41 31 32 22 12 13 23", "15 25 35 45 44 43 33 34 24 23 22 12 11 21", "13", "11", "12 22 23 24 33 34 43"),
        new Reference("41 42 43 33 34 44 45 35 25 24 23 13 12 22", "15 14 13 23 22 12 11 21 31 32 33 43 44 34", "25", "31", "12 13 22 23 33 34 43 44"),
        new Reference("41 31 32 42 43 44 45 35 25 24 23", "15 14 13 12 11 21 22 23 33 34 44", "25 45", "21 34", "23 44"),
        new Reference("33 23 22 21 31 41 42 43 44 45 35 34", "32 31 21 11 12 13 14 15 25 24 34", "35 41", "15 24", "21 31 34"),
        new Reference("32 33 34 24 23 13 12 11 21 31 41 42", "14 15 25 35 45 44 43 33 23 22 21 11", "13 41", "43 45", "11 21 23 33"),
        new Reference("34 33 43 44 45 35 25 24 14 13 12", "11 21 31 41 42 32 22 23 24 14 15 25", "12 45", "15 22", "14 24 25"),
        new Reference("25 35 45 44 43 33 23 13 12 22 21 31 41", "11 12 22 32 42 43 33 34 24 14 15", "13 21", "14 34", "12 22 33 43"),
        new Reference("22 23 33 32 31 41 42 43 44 34 35", "21 11 12 13 23 24 14 15 25 35 45 44", "34 41", "15 45", "23 35 44"),
        new Reference("41 31 32 42 43 33 23 24 34 44", "25 15 14 13 12 11 21 22 23 24 34 44 45 35", "32 43", "21 45", "23 24 34 44"),
        new Reference("35 25 15 14 24 34 33 32 22 21 31", "45 44 43 42 41 31 21 11 12 22 23 13 14", "32 34", "11 13", "14 21 22 31"),
        new Reference("11 21 31 32 22 23 24 34 35 45 44 43", "12 13 14 15 25 24 34 33 43 42 41 31", "22 45", "33 41", "24 31 34 43"),
        new Reference("44 45 35 34 24 25 15 14 13 12 22", "11 21 31 41 42 43 33 32 22 23 24 25 35 34", "12 15", "32 43", "22 24 25 34 35"),
        new Reference("44 43 42 41 31 21 22 12 13 23 33 34 24", "45 35 25 15 14 24 23 33 32 22 12 11", "13 34", "11 32", "12 22 23 24 33"),
        new Reference("23 13 14 24 25 35 34 33 32 31 41 42", "45 44 43 33 32 22 21 11 12 13 14 15 25", "35 41", "11 15", "13 14 25 32 33"),
        new Reference("23 24 25 35 45 44 34 33 32 22 21 31 41", "11 12 13 14 15 25 24 34 33 43 42 41 31", "21 32", "15 43", "24 25 31 33 34 41"),
        new Reference("44 45 35 25 15 14 13 23 33 32 22 21 11", "12 22 21 31 41 42 43 33 34 24 25", "13 32", "24 43", "21 22 25 33"),
        new Reference("24 14 13 23 22 12 11 21 31 41 42 32 33", "15 25 35 45 44 34 33 43 42 32 22 23 13", "11 41", "34 43", "13 22 23 32 33 42"),
        new Reference("31 41 42 32 33 23 13 14 24 25 35", "15 14 13 12 11 21 22 32 42 43 33 34 35 45 44", "25 41", "43 45", "13 14 32 33 35 42"),
        new Reference("15 14 24 25 35 45 44 43 33 23 22 12 13", "11 21 22 32 31 41 42 43 44 34 35 25", "12 23", "34 41", "22 25 35 43 44"),
        new Reference("43 42 41 31 21 11 12 22 23 13 14 24 34 33", "15 25 35 45 44 34 33 23 22 32 42 41 31", "14", "44", "22 23 31 33 34 41 42"),
        new Reference("42 41 31 21 11 12 22 32 33 23 24 14 15 25", "13 14 24 34 35 45 44 43 33 32 22 21", "15 23", "43 45", "14 21 22 24 32 33"),
        new Reference("31 32 22 23 33 43 44 34 35 25 15 14", "41 42 32 22 21 11 12 13 14 15 25 24 34 44 45", "35", "24", "14 15 22 25 32 34 44"),
        new Reference("42 43 33 23 24 14 13 12 11 21 22 32 31", "41 31 21 11 12 22 23 13 14 15 25 35 45 44 34", "32", "44", "11 12 13 14 21 22 23 31"),
        new Reference("15 25 24 23 33 43 42 32 31 21 11 12 22", "41 42 32 33 43 44 45 35 34 24 14 13 12", "11 31", "14 34", "12 24 32 33 42 43"),
        new Reference("22 23 13 14 15 25 24 34 33 32 42 41 31", "35 45 44 43 42 32 31 21 11 12 13 23 24 14 15", "41", "11", "13 14 15 23 24 31 32 42"),
        new Reference("34 44 43 33 32 22 23 13 14 24 25", "12 11 21 22 32 31 41 42 43 44 45 35 25 15 14 24", "13", "15", "14 22 24 25 32 43 44"),
        new Reference("15 25 24 34 33 43 42 32 31 21 11 12 13", "14 13 23 22 12 11 21 31 41 42 43 44 34 35 45", "32", "35", "11 12 13 21 31 34 42 43"),
        new Reference("42 32 33 23 13 14 15 25 24 34 35 45 44 43", "41 31 32 22 21 11 12 13 14 24 25 35 34", "45", "11", "13 14 24 25 32 34 35"),
        new Reference("21 31 41 42 43 33 34 44 45 35 25 15 14 24 23", "11 12 22 23 13 14 24 25 35 34 33 32 42", "15", "32", "14 23 24 25 33 34 35 42"),
        new Reference("13 14 24 25 35 45 44 43 33 23 22 32 31 41", "15 25 35 34 44 43 42 32 31 21 11 12 22 23", "41", "12", "22 23 25 31 32 35 43 44"),
        new Reference("12 11 21 31 32 22 23 33 34 24 25 15 14 13", "41 42 43 33 34 44 45 35 25 24 14 13 23 22", "15", "45", "13 14 22 23 24 25 33 34"),
        new Reference("23 13 12 22 21 31 41 42 43 33 34 24 25 35 45", "11 21 22 32 42 43 44 34 35 25 24 14 15", "33", "14", "21 22 24 25 34 35 42 43")
    };
    private static Vector2Int[] Cells(string values) => values.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(value => new Vector2Int(value[1] - '1', value[0] - '1')).ToArray();

    public static void AssertLayout(GameManager game, int number)
    {
        Reference reference = Stages[number - 1];
        StageColorPalette palette = game.Palette;
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
            if (tile.IsStartTile) Assert.That(tile.StartColor, Is.EqualTo(tile.Coordinate == redStart ? palette.First : palette.Second));
            TileColor expected = purple.Contains(tile.Coordinate) ? TileColor.Purple : red.Contains(tile.Coordinate) ? TileColor.Red :
                blue.Contains(tile.Coordinate) ? TileColor.Blue : TileColor.None;
            expected = palette.Map(expected);
            Assert.That(tile.RequiredColor, Is.EqualTo(expected), $"Stage{number} {tile.name} frame");
            Assert.That(tile.ForbiddenColor, Is.EqualTo(TileColor.None));
            Assert.That(tile.transform.Find("TileFlame").GetComponent<SpriteRenderer>().sprite, Is.EqualTo(GlobalScripts.GetFrameImage(expected)));
            Assert.That(tile.transform.Find("TileFlame").GetComponent<SpriteRenderer>().enabled, Is.True);
        }
        Assert.That(GameObject.Find("SharedPurpleFrame"), Is.Null);
    }

    public static IEnumerator Solve(GameManager game)
    {
        int number = game.GetComponent<StageClearNavigation>().PuzzleId;
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
        int number = game.GetComponent<StageClearNavigation>().PuzzleId;
        Reference reference = Stages[number - 1];
        Vector2Int[] cells = Cells(color == BaseColor.Red ? reference.Red : reference.Blue);
        Assert.That(game.TryBeginLine(game.GetTile(cells[0]).TileIndex), Is.True, $"Stage{number} begin");
        for (int i = 1; i < cells.Length; i++)
            Assert.That(game.TryExtendLine(game.GetTile(cells[i]).TileIndex), Is.True, $"Stage{number} extend to {cells[i]}");
    }
}
