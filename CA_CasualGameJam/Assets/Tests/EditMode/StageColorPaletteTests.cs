using NUnit.Framework;
using UnityEngine;

public class StageColorPaletteTests
{
    [TestCase(1, BaseColor.Red, BaseColor.Blue)]
    [TestCase(5, BaseColor.Red, BaseColor.Blue)]
    [TestCase(6, BaseColor.Red, BaseColor.Yellow)]
    [TestCase(10, BaseColor.Red, BaseColor.Yellow)]
    [TestCase(11, BaseColor.Blue, BaseColor.Yellow)]
    [TestCase(15, BaseColor.Blue, BaseColor.Yellow)]
    [TestCase(16, BaseColor.Red, BaseColor.Blue)]
    [TestCase(20, BaseColor.Red, BaseColor.Blue)]
    [TestCase(21, BaseColor.Red, BaseColor.Yellow)]
    [TestCase(25, BaseColor.Red, BaseColor.Yellow)]
    [TestCase(26, BaseColor.Blue, BaseColor.Yellow)]
    [TestCase(30, BaseColor.Blue, BaseColor.Yellow)]
    [TestCase(31, BaseColor.Red, BaseColor.Blue)]
    [TestCase(35, BaseColor.Red, BaseColor.Blue)]
    [TestCase(36, BaseColor.Red, BaseColor.Yellow)]
    [TestCase(40, BaseColor.Red, BaseColor.Yellow)]
    [TestCase(41, BaseColor.Blue, BaseColor.Yellow)]
    [TestCase(int.MaxValue, BaseColor.Red, BaseColor.Yellow)]
    public void DisplayNumbersRotateEveryFiveStages(int stage, BaseColor first, BaseColor second)
    {
        StageColorPalette palette = StageColorPalette.ForStage(stage);
        Assert.That(palette.First, Is.EqualTo(first));
        Assert.That(palette.Second, Is.EqualTo(second));
        Assert.That(palette.Map(BaseColor.Red), Is.EqualTo(first));
        Assert.That(palette.Map(BaseColor.Blue), Is.EqualTo(second));
        Assert.That(palette.Map(TileColor.Purple), Is.EqualTo(PuzzleRules.Mix(new[] { first, second })));
        Assert.That(palette.Map(TileColor.None), Is.EqualTo(TileColor.None));
    }

    [TestCase(1, TileColor.Red, TileColor.Blue, TileColor.Purple)]
    [TestCase(6, TileColor.Red, TileColor.Yellow, TileColor.Orange)]
    [TestCase(11, TileColor.Blue, TileColor.Yellow, TileColor.Green)]
    public void NormalCellsAcceptOnlyTheTwoActiveSingleColors(int stage, TileColor first, TileColor second, TileColor mixed)
    {
        foreach (TileColor color in new[] { TileColor.None, TileColor.Red, TileColor.Blue, TileColor.Yellow,
            TileColor.Purple, TileColor.Orange, TileColor.Green, (TileColor)7, (TileColor)8 })
        {
            bool expected = color == first || color == second;
            Assert.That(PuzzleRules.IsTileCorrect(color, false, TileColor.None, TileColor.None, mixed), Is.EqualTo(expected), color.ToString());
        }
        Assert.That(PuzzleRules.IsTileCorrect(mixed, true, mixed, first, mixed), Is.True,
            "Bans compare the finished color, not its ingredients.");
        Assert.That(PuzzleRules.IsTileCorrect(first, true, mixed, TileColor.None, mixed), Is.False);
        Assert.That(PuzzleRules.IsTileCorrect(mixed, true, mixed, mixed, mixed), Is.False);
        StageColorPalette palette = StageColorPalette.ForStage(stage);
        foreach (BaseColor color in new[] { BaseColor.Red, BaseColor.Blue, BaseColor.Yellow })
            Assert.That(PuzzleRules.CanAddColor(TileColor.None, color, mixed), Is.EqualTo(palette.Contains(color)));
    }

    [Test]
    public void ApplyingPalettesRepeatedlyAlwaysUsesTheOriginalPuzzleConditions()
    {
        var go = new GameObject("Palette test tile");
        try
        {
            var tile = go.AddComponent<TileController>();
            tile.Configure(0, Vector2Int.zero, true, BaseColor.Blue,
                new[] { BaseColor.Red, BaseColor.Blue }, TileColor.Red);
            foreach (int stage in new[] { 6, 11, 1, 11, 6 })
            {
                StageColorPalette palette = StageColorPalette.ForStage(stage);
                tile.ResetRuntimeState();
                tile.ApplyPalette(palette);
                tile.ApplyPalette(palette);
                Assert.That(tile.StartColor, Is.EqualTo(palette.Second));
                Assert.That(tile.RequiredColor, Is.EqualTo(palette.Mixed));
                Assert.That(tile.ForbiddenColor, Is.EqualTo(PuzzleRules.ToTileColor(palette.First)));
                Assert.That(tile.AddColor(palette.First), Is.True);
                Assert.That(tile.IsTileCorrect(), Is.False);
                Assert.That(tile.AddColor(palette.Second), Is.True);
                Assert.That(tile.CurrentColor, Is.EqualTo(palette.Mixed));
                Assert.That(tile.IsTileCorrect(), Is.True);
                tile.RemoveColor(palette.First);
                Assert.That(tile.CurrentColor, Is.EqualTo(PuzzleRules.ToTileColor(palette.Second)));
                Assert.That(tile.IsTileCorrect(), Is.False);
                Assert.That(tile.DisplayColor, Is.EqualTo(GlobalScripts.GetFaceColor(tile.CurrentColor)));
                BaseColor unused = palette.Map(BaseColor.Yellow);
                Assert.That(tile.AddColor(unused), Is.False);
            }
        }
        finally { Object.DestroyImmediate(go); }
    }
}
