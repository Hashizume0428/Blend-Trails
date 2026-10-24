using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PuzzleVisualTests
{
    private GameManager _game;
    [UnitySetUp]
    public IEnumerator LoadStage()
    {
        yield return SceneManager.LoadSceneAsync("SampleScene");
        yield return null;
        _game = Object.FindFirstObjectByType<GameManager>();
        Assert.That(_game.IsInitialized, Is.True);
    }
    private TileController Tile(int x, int y) => _game.GetTile(new Vector2Int(x, y));
    private int Index(int x, int y) => Tile(x, y).TileIndex;
    private void Begin(int x, int y) => Assert.That(_game.TryBeginLine(Index(x, y)), Is.True);
    private void Extend(int x, int y) => Assert.That(_game.TryExtendLine(Index(x, y)), Is.True);
    private void AssertNoErrorBlinks()
    {
        foreach (TileController tile in _game.TilesList)
            Assert.That(tile.IsErrorBlinking, Is.False, $"Unexpected blink at {tile.Coordinate}");
    }

    [UnityTest]
    public IEnumerator SharedEdgesMixDuringPreviewAndRestoreOnCancelOrReload()
    {
        Begin(0, 0); Extend(3, 0); Extend(3, 2);
        yield return _game.CommitLineAsync().ToCoroutine();
        Begin(0, 3); Extend(3, 3); Extend(3, 1);
        BoardLineRenderer visuals = _game.LineVisuals;
        Assert.That(visuals.GetEdgeColor(Index(3, 1), Index(3, 2)), Is.EqualTo(TileColor.Purple));
        Assert.That(visuals.GetEdgeColor(Index(0, 0), Index(1, 0)), Is.EqualTo(TileColor.Red));
        Assert.That(visuals.GetEdgeColor(Index(0, 3), Index(1, 3)), Is.EqualTo(TileColor.Blue));
        Assert.That(Tile(3, 1).CurrentColor, Is.EqualTo(TileColor.Red), "Preview must not paint tiles.");
        Assert.That(Tile(0, 0).Line.GetComponent<LineRenderer>().enabled, Is.False);
        Assert.That(Tile(0, 3).Line.GetComponent<LineRenderer>().enabled, Is.False);
        Color ink = GlobalScripts.GetLineColor(TileColor.Purple);
        Color32 purple = QualitySettings.activeColorSpace == ColorSpace.Linear ? ink.linear : ink;
        CollectionAssert.Contains(visuals.GetComponent<MeshFilter>().sharedMesh.colors32, purple);
        _game.CancelLine();
        Assert.That(visuals.GetEdgeColor(Index(3, 1), Index(3, 2)), Is.EqualTo(TileColor.Red));
        Assert.That(visuals.GetEdgeColor(Index(0, 3), Index(1, 3)), Is.EqualTo(TileColor.None));
        Begin(0, 3); Extend(3, 3); Extend(3, 1);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(visuals.GetEdgeColor(Index(3, 1), Index(3, 2)), Is.EqualTo(TileColor.Purple));
        Assert.That(_game.ReloadColor(BaseColor.Red), Is.True);
        Assert.That(visuals.GetEdgeColor(Index(3, 1), Index(3, 2)), Is.EqualTo(TileColor.Blue));
        _game.RestartStage();
        Assert.That(visuals.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator CrossingMixesOnlyTheJunctionAndKeepsApproachingLinesTheirOwnColors()
    {
        Begin(0, 0); Extend(0, 1); Extend(3, 1);
        yield return _game.CommitLineAsync().ToCoroutine();
        Begin(0, 3); Extend(2, 3); Extend(2, 0);
        BoardLineRenderer visuals = _game.LineVisuals;
        Assert.That(visuals.GetNodeColor(Index(2, 1)), Is.EqualTo(TileColor.Purple));
        Assert.That(visuals.GetEdgeColor(Index(1, 1), Index(2, 1)), Is.EqualTo(TileColor.Red));
        Assert.That(visuals.GetEdgeColor(Index(2, 1), Index(3, 1)), Is.EqualTo(TileColor.Red));
        Assert.That(visuals.GetEdgeColor(Index(2, 0), Index(2, 1)), Is.EqualTo(TileColor.Blue));
        Assert.That(visuals.GetEdgeColor(Index(2, 1), Index(2, 2)), Is.EqualTo(TileColor.Blue));
        _game.CancelLine();
        Assert.That(visuals.GetNodeColor(Index(2, 1)), Is.EqualTo(TileColor.Red));
    }

    [UnityTest]
    public IEnumerator ErrorsBlinkOnlyAfterAllStartLinesAreCommittedIncludingEmptyTiles()
    {
        TileController forbidden = Tile(1, 3);
        var marker = forbidden.transform.Find("ForbiddenMarker").GetComponent<SpriteRenderer>();
        var frame = forbidden.transform.Find("TileFlame").GetComponent<SpriteRenderer>();
        Assert.That(marker.transform.localPosition, Is.EqualTo(Vector3.zero));
        AssertNoErrorBlinks();
        Begin(0, 0); Extend(0, 3); Extend(1, 3);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(forbidden.IsTileCorrect(), Is.False);
        Assert.That(forbidden.DisplayColor, Is.EqualTo(GlobalScripts.RedFaceColor));
        Assert.That(forbidden.transform.Find("TileFace").GetComponent<SpriteRenderer>().color,
            Is.EqualTo(GlobalScripts.RedFaceColor));
        AssertNoErrorBlinks();
        Begin(0, 3);
        AssertNoErrorBlinks();
        _game.CancelLine();
        AssertNoErrorBlinks();
        Begin(0, 3);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        Assert.That(Tile(2, 3).CurrentColor, Is.EqualTo(TileColor.None));
        foreach (TileController tile in _game.TilesList)
            Assert.That(tile.IsErrorBlinking, Is.EqualTo(!tile.IsTileCorrect()), $"Assessment at {tile.Coordinate}");
        float minimum = 1, maximum = 0;
        for (float end = Time.unscaledTime + 0.6f; Time.unscaledTime < end;)
        {
            minimum = Mathf.Min(minimum, frame.color.a);
            maximum = Mathf.Max(maximum, frame.color.a);
            Assert.That(marker.color.a, Is.EqualTo(frame.color.a).Within(0.001f));
            yield return null;
        }
        Assert.That(maximum - minimum, Is.GreaterThan(0.4f));
    }

    [UnityTest]
    public IEnumerator ErrorBlinksFinishAfterTwoSecondsAndDoNotResumeOnRefresh()
    {
        Begin(0, 3);
        yield return _game.CommitLineAsync().ToCoroutine();
        AssertNoErrorBlinks();
        Begin(0, 0);
        yield return _game.CommitLineAsync().ToCoroutine();
        TileController empty = Tile(1, 3);
        Assert.That(empty.CurrentColor, Is.EqualTo(TileColor.None));
        Assert.That(empty.IsErrorBlinking, Is.True);
        yield return new WaitForSecondsRealtime(1.8f);
        Assert.That(empty.IsErrorBlinking, Is.True, "The feedback must last for two seconds.");
        yield return new WaitForSecondsRealtime(0.35f);
        AssertNoErrorBlinks();
        foreach (TileController tile in _game.TilesList)
        {
            var frame = tile.transform.Find("TileFlame").GetComponent<SpriteRenderer>();
            var marker = tile.transform.Find("ForbiddenMarker").GetComponent<SpriteRenderer>();
            Assert.That(frame.color.a, Is.EqualTo(1));
            Assert.That(marker.color.a, Is.EqualTo(1));
            tile.RefreshVisuals();
        }
        AssertNoErrorBlinks();
    }

    [UnityTest]
    public IEnumerator ReloadWaitsForRecommitAndResetOrDisableStopsErrorBlinks()
    {
        TileController required = Tile(3, 1);
        var frame = required.transform.Find("TileFlame").GetComponent<SpriteRenderer>();
        Begin(0, 0); Extend(3, 0); Extend(3, 1);
        yield return _game.CommitLineAsync().ToCoroutine();
        AssertNoErrorBlinks();
        Begin(0, 3); Extend(3, 3); Extend(3, 1);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(required.CurrentColor, Is.EqualTo(TileColor.Purple));
        Assert.That(required.IsErrorBlinking, Is.False);
        Assert.That(frame.color.a, Is.EqualTo(1));
        Assert.That(Tile(2, 2).IsErrorBlinking, Is.True);
        Assert.That(_game.ReloadColor(BaseColor.Blue), Is.True);
        Assert.That(required.IsTileCorrect(), Is.False);
        AssertNoErrorBlinks();
        Begin(0, 3);
        AssertNoErrorBlinks();
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(required.IsErrorBlinking, Is.True);
        required.gameObject.SetActive(false);
        Assert.That(required.IsErrorBlinking, Is.False);
        Assert.That(frame.color.a, Is.EqualTo(1));
        required.gameObject.SetActive(true);
        Assert.That(required.IsErrorBlinking, Is.False);
        _game.RestartStage();
        AssertNoErrorBlinks();
        Assert.That(frame.color.a, Is.EqualTo(1));
    }
}
