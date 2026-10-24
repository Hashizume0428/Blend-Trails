using NUnit.Framework;
using UnityEngine;

public class PuzzleRulesTests
{
    [TestCase(TileColor.None, false)]
    [TestCase(TileColor.Red, false)]
    [TestCase(TileColor.Blue, true)]
    [TestCase(TileColor.Purple, true)]
    [TestCase(TileColor.Orange, true)]
    public void RedBanChecksFinishedColor(TileColor current, bool expected)
    {
        Assert.That(PuzzleRules.IsTileCorrect(current, true, current, TileColor.Red), Is.EqualTo(expected));
    }
    [TestCase(TileColor.None, false)]
    [TestCase(TileColor.Red, true)]
    [TestCase(TileColor.Blue, true)]
    [TestCase(TileColor.Purple, false)]
    [TestCase(TileColor.Yellow, false)]
    [TestCase(TileColor.Green, false)]
    [TestCase(TileColor.Orange, false)]
    [TestCase((TileColor)7, false)]
    [TestCase((TileColor)8, false)]
    public void NormalTilesRequireOnlyRedOrBlue(TileColor current, bool expected)
    {
        Assert.That(PuzzleRules.IsTileCorrect(current, false, TileColor.None, TileColor.None), Is.EqualTo(expected));
    }
    [Test]
    public void MixingANormalTileMakesItIncorrectUntilOneColorIsReloaded()
    {
        var go = new GameObject("Normal tile");
        try
        {
            var tile = go.AddComponent<TileController>();
            tile.Configure(0, Vector2Int.zero, true, BaseColor.Red, new BaseColor[0], TileColor.None);
            Assert.That(tile.AddColor(BaseColor.Red), Is.True);
            Assert.That(tile.IsTileCorrect(), Is.True);
            Assert.That(tile.AddColor(BaseColor.Blue), Is.True, "Drawing purple must remain allowed.");
            Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.Purple));
            Assert.That(tile.IsTileCorrect(), Is.False, "The normal-cell rule also applies to start tiles.");
            Assert.That(tile.RemoveColor(BaseColor.Red), Is.True);
            Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.Blue));
            Assert.That(tile.IsTileCorrect(), Is.True);
        }
        finally { Object.DestroyImmediate(go); }
    }
    [Test]
    public void RequiredColorMatchesExactlyAndIgnoresOrder()
    {
        TileColor mixed = PuzzleRules.Mix(new[] { BaseColor.Blue, BaseColor.Red, BaseColor.Red });
        Assert.That(mixed, Is.EqualTo(TileColor.Purple));
        Assert.That(PuzzleRules.IsTileCorrect(mixed, true, TileColor.Purple, TileColor.None), Is.True);
        Assert.That(PuzzleRules.IsTileCorrect(TileColor.Red, true, TileColor.Purple, TileColor.None), Is.False);
    }
    [Test]
    public void ThreeColorsAndUnknownColorsAreRejected()
    {
        Assert.That(PuzzleRules.CanAddColor(TileColor.Purple, BaseColor.Yellow), Is.False);
        Assert.That(PuzzleRules.CanAddColor(TileColor.Blue, BaseColor.Red), Is.True);
        Assert.That(PuzzleRules.CanAddColor(TileColor.None, (BaseColor)5), Is.False);
    }
    [Test]
    public void RemovingOneIngredientReevaluatesBansAndFaceColor()
    {
        var go = new GameObject("Test tile");
        try
        {
            var tile = go.AddComponent<TileController>();
            var face = go.AddComponent<SpriteRenderer>();
            tile.BindVisuals(face, null, null, null, null);
            tile.Configure(0, Vector2Int.zero, false, BaseColor.Red, new BaseColor[0], TileColor.Red);
            Assert.That(tile.AddColor(BaseColor.Red), Is.True);
            Assert.That(tile.IsTileCorrect(), Is.False);
            Assert.That(tile.DisplayColor, Is.EqualTo(GlobalScripts.RedFaceColor));
            Assert.That(face.color, Is.EqualTo(GlobalScripts.RedFaceColor));
            tile.AddColor(BaseColor.Blue);
            Assert.That(tile.DisplayColor, Is.EqualTo(GlobalScripts.PurpleFaceColor));
            Assert.That(face.color, Is.EqualTo(GlobalScripts.PurpleFaceColor));
            Assert.That(tile.IsTileCorrect(), Is.False, "Mixing cannot make an unmarked tile correct.");
            Assert.That(tile.AddColor(BaseColor.Blue), Is.False);
            tile.RemoveColor(BaseColor.Blue);
            Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.Red));
            Assert.That(tile.IsTileCorrect(), Is.False);
            Assert.That(tile.DisplayColor, Is.EqualTo(GlobalScripts.RedFaceColor));
            Assert.That(face.color, Is.EqualTo(GlobalScripts.RedFaceColor));
            tile.RemoveColor(BaseColor.Red);
            Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.None));
            Assert.That(face.color, Is.EqualTo(GlobalScripts.NormalFaceColor));
        }
        finally { Object.DestroyImmediate(go); }
    }
    [Test]
    public void ReloadingMixReturnsToRemainingSingleColor()
    {
        var go = new GameObject("Test tile");
        try
        {
            var tile = go.AddComponent<TileController>();
            tile.AddColor(BaseColor.Red); tile.AddColor(BaseColor.Blue);
            tile.RemoveColor(BaseColor.Red);
            Assert.That(tile.DisplayColor, Is.EqualTo(GlobalScripts.BlueFaceColor));
            tile.AddColor(BaseColor.Red); tile.RemoveColor(BaseColor.Blue);
            Assert.That(tile.DisplayColor, Is.EqualTo(GlobalScripts.RedFaceColor));
            tile.Configure(0, Vector2Int.zero, false, BaseColor.Red, new[] { BaseColor.Red, BaseColor.Blue }, TileColor.None);
            tile.AddColor(BaseColor.Red); tile.AddColor(BaseColor.Blue); tile.RemoveColor(BaseColor.Red);
            Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.Blue));
            Assert.That(tile.IsTileCorrect(), Is.False);
            Assert.That(tile.DisplayColor, Is.EqualTo(GlobalScripts.BlueFaceColor));
        }
        finally { Object.DestroyImmediate(go); }
    }
    [Test]
    public void IncorrectRequiredColorStillDisplaysItsActualMixedColor()
    {
        var go = new GameObject("Test tile");
        try
        {
            var tile = go.AddComponent<TileController>();
            var face = go.AddComponent<SpriteRenderer>();
            tile.BindVisuals(face, null, null, null, null);
            tile.Configure(0, Vector2Int.zero, false, BaseColor.Red, new[] { BaseColor.Blue }, TileColor.None);
            tile.AddColor(BaseColor.Red); tile.AddColor(BaseColor.Blue);
            Assert.That(tile.IsTileCorrect(), Is.False);
            Assert.That(tile.DisplayColor, Is.EqualTo(GlobalScripts.PurpleFaceColor));
            Assert.That(face.color, Is.EqualTo(GlobalScripts.PurpleFaceColor));
            tile.RemoveColor(BaseColor.Red);
            Assert.That(tile.IsTileCorrect(), Is.True);
            Assert.That(face.color, Is.EqualTo(GlobalScripts.BlueFaceColor));
        }
        finally { Object.DestroyImmediate(go); }
    }
    [Test]
    public void PathAllowsOnlyAdjacentUnusedTilesOrImmediateBacktracking()
    {
        int[] path = { 0, 1, 5, 4 };
        Assert.That(PuzzleRules.ClassifyStep(path, 5, new Vector2Int(0, 1), new Vector2Int(1, 1)), Is.EqualTo(PathStep.Backtrack));
        Assert.That(PuzzleRules.ClassifyStep(path, 0, new Vector2Int(0, 1), Vector2Int.zero), Is.EqualTo(PathStep.Reject));
        Assert.That(PuzzleRules.ClassifyStep(path, 8, new Vector2Int(0, 1), new Vector2Int(0, 2)), Is.EqualTo(PathStep.Append));
        Assert.That(PuzzleRules.TryGetAxisStep(Vector2Int.zero, new Vector2Int(2, 2), out _), Is.False);
        Assert.That(PuzzleRules.TryGetAxisStep(Vector2Int.zero, new Vector2Int(0, 3), out Vector2Int step), Is.True);
        Assert.That(step, Is.EqualTo(Vector2Int.up));
    }
    [Test]
    public void EmptyBoardIsNeverClear()
    {
        var go = new GameObject("Test manager");
        try { Assert.That(go.AddComponent<GameManager>().StageClearCheck(), Is.False); }
        finally { Object.DestroyImmediate(go); }
    }
}
