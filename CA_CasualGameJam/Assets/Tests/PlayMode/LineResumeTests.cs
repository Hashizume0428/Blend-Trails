using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class LineResumeTests : StageProgressTestFixture
{
    private GameManager _game;
    private InputTouchManager InputManager => _game.GetComponent<InputTouchManager>();
    private LineResumeSelector Selector => Object.FindFirstObjectByType<LineResumeSelector>();
    private int Index(int x, int y) => _game.GetTile(new Vector2Int(x, y)).TileIndex;
    private TileController Tile(int x, int y) => _game.GetTile(new Vector2Int(x, y));
    private void Begin(int x, int y) => Assert.That(_game.TryBeginLine(Index(x, y)), Is.True);
    private void Extend(int x, int y) => Assert.That(_game.TryExtendLine(Index(x, y)), Is.True);
    private Vector2 ScreenPoint(int x, int y) => Camera.main.WorldToScreenPoint(Tile(x, y).transform.position);

    [UnitySetUp]
    public IEnumerator LoadStage()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        _game = Object.FindFirstObjectByType<GameManager>();
        PuzzleTestFixture.ApplyRegressionConditions(_game);
    }
    private static void Tap(string name)
    {
        var button = GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits, Is.Not.Empty, "The opened chooser must receive taps after its first rendered frame.");
        Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject));
        Assert.That(Screen.safeArea.Contains(pointer.position), Is.True);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    private IEnumerator DrawSharedEndpoint()
    {
        Begin(0, 0); Extend(2, 0);
        yield return _game.CommitLineAsync().ToCoroutine();
        Begin(0, 3); Extend(2, 3); Extend(2, 0);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(Tile(2, 0).CurrentColor, Is.EqualTo(TileColor.Purple));
    }

    [UnityTest]
    public IEnumerator CommittedEndpointCanExtendAndShrinkAllTheWayToItsStart()
    {
        Begin(0, 0); Extend(2, 0);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.TryBeginLine(Index(1, 0)), Is.False, "Only the endpoint can resume.");
        Begin(2, 0); Extend(3, 0);
        yield return _game.CommitLineAsync().ToCoroutine();
        CollectionAssert.AreEqual(new[] { Index(0, 0), Index(1, 0), Index(2, 0), Index(3, 0) }, Tile(0, 0).Line.Path);
        Begin(3, 0); Extend(0, 0);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(Tile(0, 0).Line.Path.Count, Is.EqualTo(1));
        Assert.That(_game.HasCommittedLine(BaseColor.Red), Is.True);
        for (int x = 1; x < 4; x++) Assert.That(Tile(x, 0).CurrentColor, Is.EqualTo(TileColor.None));
        Begin(0, 0); Extend(0, 1);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(Tile(0, 1).CurrentColor, Is.EqualTo(TileColor.Red));
    }

    [UnityTest]
    public IEnumerator CancelAfterRemovingOldSegmentsAndReroutingRestoresBothPathsAndMixedColors()
    {
        yield return DrawSharedEndpoint();
        int[] red = Tile(0, 0).Line.Path.ToArray(), blue = Tile(0, 3).Line.Path.ToArray();
        TileColor[] colors = _game.TilesList.Select(tile => tile.CurrentColor).ToArray();
        Assert.That(_game.TryBeginLine(Index(2, 0), BaseColor.Red), Is.True);
        Extend(1, 0); Extend(1, 1); Extend(1, 2);
        InputManager.CancelPointer();
        CollectionAssert.AreEqual(red, Tile(0, 0).Line.Path);
        CollectionAssert.AreEqual(blue, Tile(0, 3).Line.Path);
        CollectionAssert.AreEqual(colors, _game.TilesList.Select(tile => tile.CurrentColor));
        Assert.That(_game.LineVisuals.GetNodeColor(Index(2, 0)), Is.EqualTo(TileColor.Purple));
        Assert.That(_game.HasCommittedLine(BaseColor.Red) && _game.HasCommittedLine(BaseColor.Blue), Is.True);
    }

    [UnityTest]
    public IEnumerator SharedEndpointRequiresAColorChoiceAndAFreshDragThenPreservesTheOtherColor()
    {
        yield return DrawSharedEndpoint();
        int[] red = Tile(0, 0).Line.Path.ToArray();
        Assert.That(_game.TryBeginLine(Index(2, 0)), Is.False);
        Assert.That(InputManager.BeginPointer(ScreenPoint(2, 0), -1), Is.False);
        Assert.That(Selector.IsOpen, Is.True);
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        yield return null;
        Tap("ResumeSecondColor");
        Assert.That(Selector.SelectedColor, Is.EqualTo(BaseColor.Blue));
        InputManager.MovePointer(ScreenPoint(2, 1));
        Assert.That(Tile(2, 0).CurrentColor, Is.EqualTo(TileColor.Purple), "Choosing a color alone must not alter the board.");
        Assert.That(InputManager.BeginPointer(ScreenPoint(2, 0), -1), Is.True);
        InputManager.MovePointer(ScreenPoint(2, 1));
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(Tile(2, 0).CurrentColor, Is.EqualTo(TileColor.Red));
        CollectionAssert.AreEqual(red, Tile(0, 0).Line.Path);
        Assert.That(Tile(0, 3).Line.Path.Last(), Is.EqualTo(Index(2, 1)));
        Assert.That(Selector.SelectedColor, Is.Null);
    }

    [UnityTest]
    public IEnumerator NoChangeReleaseDoesNotRestartTheErrorAssessmentOrBlockTheNextInput()
    {
        Begin(0, 0); Extend(2, 0);
        yield return _game.CommitLineAsync().ToCoroutine();
        Begin(0, 3);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(Tile(4, 2).IsErrorBlinking, Is.True);
        yield return new WaitForSecondsRealtime(1.2f);
        Begin(2, 0);
        UniTask release = _game.CommitLineAsync();
        Assert.That(release.Status, Is.EqualTo(UniTaskStatus.Succeeded));
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        yield return new WaitForSecondsRealtime(1.0f);
        Assert.That(_game.TilesList.Any(tile => tile.IsErrorBlinking), Is.False, "An unchanged line must not trigger a new two-second assessment.");
    }

    [UnityTest]
    public IEnumerator UnusedStartWinsOverAnotherColorsEndpoint()
    {
        Begin(0, 0); Extend(0, 3);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(InputManager.BeginPointer(ScreenPoint(0, 3), -1), Is.True);
        Assert.That(Selector.IsOpen, Is.False);
        Assert.That(Tile(0, 3).CurrentColor, Is.EqualTo(TileColor.Purple));
        InputManager.CancelPointer();
        Assert.That(Tile(0, 3).CurrentColor, Is.EqualTo(TileColor.Red));
    }

    [UnityTest]
    public IEnumerator ChooserUsesCurrentStageColorsAndReloadAndCancelClearPendingSelection()
    {
        foreach (int stage in new[] { 6, 11 })
        {
            yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(stage));
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            foreach (BaseColor color in new[] { _game.Palette.First, _game.Palette.Second })
            {
                TileController start = _game.TilesList.Single(tile => tile.IsStartTile && tile.StartColor == color);
                Assert.That(_game.TryBeginLine(start.TileIndex), Is.True);
                if (start.Coordinate.x != 2) Extend(2, start.Coordinate.y);
                if (start.Coordinate.y != 2) Extend(2, 2);
                yield return _game.CommitLineAsync().ToCoroutine();
            }
            Assert.That(InputManager.BeginPointer(ScreenPoint(2, 2), -1), Is.False);
            Assert.That(Selector.IsOpen, Is.True);
            Assert.That(GameObject.Find("ResumeSecondColor").GetComponentInChildren<TMP_Text>().text, Is.EqualTo("YELLOW"));
            yield return null;
            Tap("CloseColorChoice");
            Assert.That(Selector.IsOpen, Is.False);
            InputManager.BeginPointer(ScreenPoint(2, 2), -1);
            yield return null;
            Tap("ResumeSecondColor");
            Assert.That(Selector.SelectedColor, Is.EqualTo(BaseColor.Yellow));
            Assert.That(_game.ReloadAll(), Is.True);
            Assert.That(Selector.SelectedColor, Is.Null);
            Assert.That(Selector.IsOpen, Is.False);
            Assert.That(InputManager.BeginPointer(ScreenPoint(2, 2), -1), Is.False);
        }
    }

    [UnityTest]
    public IEnumerator AReferenceSolutionCanBeCompletedAcrossSeveralStrokes()
    {
        yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(3));
        yield return null;
        _game = Object.FindFirstObjectByType<GameManager>();
        StageTestReference.DrawLine(_game, BaseColor.Red);
        int end = _game.PreviewPath.Last(), previous = _game.PreviewPath[_game.PreviewPath.Count - 2];
        Assert.That(_game.TryExtendLine(previous), Is.True);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.TryBeginLine(previous), Is.True);
        Assert.That(_game.TryExtendLine(end), Is.True);
        yield return _game.CommitLineAsync().ToCoroutine();
        StageTestReference.DrawLine(_game, BaseColor.Blue);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.State, Is.EqualTo(GameState.Cleared));
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(4));
    }
}
