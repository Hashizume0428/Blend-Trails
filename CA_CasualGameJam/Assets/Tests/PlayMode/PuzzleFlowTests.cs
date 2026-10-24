using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PuzzleFlowTests
{
    private GameManager _game;
    [UnitySetUp]
    public IEnumerator LoadStage()
    {
        yield return SceneManager.LoadSceneAsync("SampleScene");
        yield return null;
        _game = Object.FindFirstObjectByType<GameManager>();
        Assert.That(_game.IsInitialized, Is.True);
        Assert.That(_game.TilesList.Count, Is.EqualTo(16));
    }
    private int Index(int x, int y) => _game.GetTile(new Vector2Int(x, y)).TileIndex;
    private void Begin(int x, int y) => Assert.That(_game.TryBeginLine(Index(x, y)), Is.True);
    private void Extend(int x, int y) => Assert.That(_game.TryExtendLine(Index(x, y)), Is.True);

    [UnityTest]
    public IEnumerator FullSolutionClearsAndRestartResetsBoard()
    {
        int before = Time.frameCount;
        yield return null;
        Assert.That(Time.frameCount, Is.GreaterThan(before));
        Begin(0, 0);
        Extend(0, 1); Extend(1, 1); Extend(1, 0); Extend(2, 0);
        Extend(2, 1); Extend(2, 2); Extend(3, 2); Extend(3, 1); Extend(3, 0);
        Assert.That(_game.CorrectTileCount, Is.Zero, "Preview must not paint tiles.");
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        Assert.That(_game.TryBeginLine(Index(0, 0)), Is.False);
        Begin(0, 3);
        Extend(0, 2); Extend(1, 2); Extend(1, 3); Extend(2, 3);
        Extend(3, 3); Extend(3, 2); Extend(3, 1);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.State, Is.EqualTo(GameState.Cleared));
        Assert.That(_game.CorrectTileCount, Is.EqualTo(16));
        Assert.That(_game.StageClearCheck(), Is.True);
        var restart = GameObject.Find("Restart").GetComponent<UnityEngine.UI.Button>();
        Assert.That(restart.interactable, Is.True);
        restart.onClick.Invoke();
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        Assert.That(_game.CorrectTileCount, Is.Zero);
        foreach (TileController tile in _game.TilesList) Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.None));
    }
    [UnityTest]
    public IEnumerator OtherColorStartRemainsUsableAndReloadPreservesOtherLine()
    {
        Begin(0, 0); Extend(0, 3);
        yield return _game.CommitLineAsync().ToCoroutine();
        var blueStart = _game.GetTile(new Vector2Int(0, 3));
        Assert.That(blueStart.CurrentColor, Is.EqualTo(TileColor.Red));
        Begin(0, 3); Extend(1, 3);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(blueStart.CurrentColor, Is.EqualTo(TileColor.Purple));
        Assert.That(blueStart.Line.LineColor, Is.EqualTo(GlobalScripts.GetLineColor(BaseColor.Blue)));
        int bluePoints = blueStart.Line.PointCount;
        var resetRed = GameObject.Find("ReloadRed").GetComponent<UnityEngine.UI.Button>();
        Assert.That(resetRed.interactable, Is.True);
        resetRed.onClick.Invoke();
        Assert.That(blueStart.CurrentColor, Is.EqualTo(TileColor.Blue));
        Assert.That(blueStart.DisplayColor, Is.EqualTo(GlobalScripts.BlueFaceColor));
        Assert.That(blueStart.Line.PointCount, Is.EqualTo(bluePoints));
        Assert.That(_game.HasCommittedLine(BaseColor.Blue), Is.True);
        Begin(0, 0); _game.CancelLine();
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
    }
    [UnityTest]
    public IEnumerator FastSwipesBacktrackAndCancelWithoutPainting()
    {
        Begin(0, 0); Extend(3, 0);
        Assert.That(_game.PreviewPath.Count, Is.EqualTo(4));
        Extend(2, 0);
        Assert.That(_game.PreviewPath.Count, Is.EqualTo(3));
        Assert.That(_game.TryExtendLine(Index(3, 1)), Is.False);
        Assert.That(_game.ReloadColor(BaseColor.Red), Is.False);
        _game.CancelLine();
        Assert.That(_game.CorrectTileCount, Is.Zero);
        Assert.That(_game.GetTile(Vector2Int.zero).Line.PointCount, Is.EqualTo(1));
        yield return null;
    }
    [UnityTest]
    public IEnumerator PointerHitTestingCancelAndSingleTileCommit()
    {
        var input = Object.FindFirstObjectByType<InputTouchManager>();
        Camera camera = Camera.main;
        Physics2D.SyncTransforms();
        Assert.That(input.BeginPointer(new Vector2(-1000, -1000), -1), Is.False);
        Vector2 start = camera.WorldToScreenPoint(_game.GetTile(Vector2Int.zero).transform.position);
        Assert.That(input.BeginPointer(start, -1), Is.True);
        input.MovePointer(new Vector2(-1000, -1000));
        Assert.That(_game.PreviewPath.Count, Is.EqualTo(1));
        input.CancelPointer();
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        Begin(0, 0);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.GetTile(Vector2Int.zero).CurrentColor, Is.EqualTo(TileColor.Red));
        Assert.That(_game.TryBeginLine(Index(0, 0)), Is.False);
    }
    [UnityTest]
    public IEnumerator RestartDuringFeedbackCancelsOldOperation()
    {
        Begin(0, 0); Extend(1, 0);
        UniTask pending = _game.CommitLineAsync();
        Assert.That(_game.State, Is.EqualTo(GameState.Resolving));
        _game.RestartStage();
        yield return pending.ToCoroutine();
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        Assert.That(_game.CorrectTileCount, Is.Zero);
        yield return new WaitForSeconds(0.2f);
        Assert.That(_game.CorrectTileCount, Is.Zero);
    }
}
