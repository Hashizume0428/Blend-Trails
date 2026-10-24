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
    public IEnumerator ForbiddenMarkerIsCenteredAndBlinksTogetherWithItsFrame()
    {
        TileController forbidden = Tile(1, 3);
        var marker = forbidden.transform.Find("ForbiddenMarker").GetComponent<SpriteRenderer>();
        var frame = forbidden.transform.Find("TileFlame").GetComponent<SpriteRenderer>();
        Assert.That(marker.transform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(forbidden.IsErrorBlinking, Is.False, "Empty tiles must not flash.");
        Begin(0, 0); Extend(0, 3); Extend(1, 3);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(forbidden.IsTileCorrect(), Is.False);
        Assert.That(forbidden.DisplayColor, Is.EqualTo(GlobalScripts.NormalFaceColor));
        Assert.That(forbidden.IsErrorBlinking, Is.True);
        float minimum = 1, maximum = 0;
        for (float end = Time.time + 0.6f; Time.time < end;)
        {
            minimum = Mathf.Min(minimum, frame.color.a);
            maximum = Mathf.Max(maximum, frame.color.a);
            Assert.That(marker.color.a, Is.EqualTo(frame.color.a).Within(0.001f));
            yield return null;
        }
        Assert.That(maximum - minimum, Is.GreaterThan(0.4f));
        Assert.That(_game.ReloadColor(BaseColor.Red), Is.True);
        Assert.That(forbidden.IsErrorBlinking, Is.False);
        Assert.That(frame.color.a, Is.EqualTo(1));
        Assert.That(marker.color.a, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RequiredFrameStopsBlinkingWhenCorrectAndRestoresAfterResetOrDisable()
    {
        TileController required = Tile(3, 1);
        var frame = required.transform.Find("TileFlame").GetComponent<SpriteRenderer>();
        Begin(0, 0); Extend(3, 0); Extend(3, 1);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(required.IsErrorBlinking, Is.True);
        Begin(0, 3); Extend(3, 3); Extend(3, 1);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(required.CurrentColor, Is.EqualTo(TileColor.Purple));
        Assert.That(required.IsErrorBlinking, Is.False);
        Assert.That(frame.color.a, Is.EqualTo(1));
        Assert.That(_game.ReloadColor(BaseColor.Blue), Is.True);
        Assert.That(required.IsErrorBlinking, Is.True);
        required.gameObject.SetActive(false);
        Assert.That(required.IsErrorBlinking, Is.False);
        Assert.That(frame.color.a, Is.EqualTo(1));
        required.gameObject.SetActive(true);
        Assert.That(required.IsErrorBlinking, Is.True);
        _game.RestartStage();
        Assert.That(required.IsErrorBlinking, Is.False);
        Assert.That(frame.color.a, Is.EqualTo(1));
    }
}
