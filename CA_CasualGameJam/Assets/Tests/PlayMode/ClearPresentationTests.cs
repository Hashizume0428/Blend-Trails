using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ClearPresentationTests : StageProgressTestFixture
{
    private GameManager _game;
    private StageClearPresentation _presentation;
    private StageScreenLayout _layout;
    private Transform _board;
    private float _initialScale;
    private static UnityEngine.UI.Button Button(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
    private static Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    [UnitySetUp]
    public IEnumerator LoadStage()
    {
        yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(3));
        yield return null;
        _game = Object.FindFirstObjectByType<GameManager>();
        _presentation = Object.FindFirstObjectByType<GameHud>().ClearPresentation;
        _layout = Object.FindFirstObjectByType<StageScreenLayout>();
        _board = _game.TilesList[0].transform.parent; _initialScale = _board.localScale.x;
        Assert.That(_presentation, Is.Not.Null);
    }
    private static IEnumerator WaitUntil(System.Func<bool> condition,
        [System.Runtime.CompilerServices.CallerLineNumber] int callerLine = 0)
    {
        float deadline = Time.realtimeSinceStartup + 5;
        while (!condition())
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                $"Wait at line {callerLine}: scene={SceneManager.GetActiveScene().name}, loading={SceneNavigator.IsLoading}, stage={StageProgress.CurrentStageNumber}");
            yield return null;
        }
    }
    private static void Tap(UnityEngine.UI.Button button)
    {
        Assert.That(button.interactable, Is.True);
        var pointer = new PointerEventData(EventSystem.current)
        { position = Center((RectTransform)button.transform), button = PointerEventData.InputButton.Left };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject));
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    private IEnumerator PrepareSolution()
    {
        StageTestReference.DrawLine(_game, BaseColor.Red);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_presentation.IsPlaying, Is.False);
        Assert.That(_presentation.Confetti.IsPlaying, Is.False);
        StageTestReference.DrawLine(_game, BaseColor.Blue);
        Assert.That(_game.StageClearCheck(), Is.True);
        Assert.That(_presentation.IsPlaying, Is.False, "Wait for release before celebrating.");
    }

    [UnityTest]
    public IEnumerator ConfettiPrecedesShrinkingAndSolvedBoardKeepsColorsAndLinesWithStageActions()
    {
        yield return PrepareSolution();
        TileColor[] colors = _game.TilesList.Select(tile => tile.CurrentColor).ToArray();
        var lines = _game.TilesList.Where(tile => tile.IsStartTile).Select(tile => tile.Line.PointCount).ToArray();
        UniTask clear = _game.CommitLineAsync();
        yield return WaitUntil(() => _presentation.IsPlaying);
        Assert.That(_presentation.Confetti.IsPlaying, Is.True);
        Assert.That(_layout.ClearProgress, Is.Zero);
        Assert.That(Button("NextStageButton").interactable, Is.False);
        Assert.That(Button("ClearHomeButton").interactable, Is.False);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(_layout.ClearProgress, Is.Zero, "Confetti plays before the board moves.");
        Canvas.ForceUpdateCanvases();
        var paperRenderer = _presentation.Confetti.GetComponent<CanvasRenderer>();
        Assert.That(paperRenderer != null, Is.True, "Paper pieces require a UI renderer.");
        var paperMesh = paperRenderer.GetMesh();
        Assert.That(paperMesh.vertexCount, Is.GreaterThan(0), "The playing effect must produce visible geometry.");
        yield return WaitUntil(() => _layout.ClearProgress > 0);
        Assert.That(_presentation.IsPlaying, Is.True);
        yield return clear.ToCoroutine();
        Assert.That(_game.State, Is.EqualTo(GameState.Cleared));
        Assert.That(_presentation.IsPresented, Is.True);
        Assert.That(_board.localScale.x / _initialScale, Is.EqualTo(0.6f).Within(0.01f));
        CollectionAssert.AreEqual(colors, _game.TilesList.Select(tile => tile.CurrentColor));
        CollectionAssert.AreEqual(lines, _game.TilesList.Where(tile => tile.IsStartTile).Select(tile => tile.Line.PointCount));
        Assert.That(_game.LineVisuals.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
        var next = Button("NextStageButton"); var home = Button("ClearHomeButton");
        Assert.That(next.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage4"));
        Vector2 nextCenter = Center((RectTransform)next.transform), homeCenter = Center((RectTransform)home.transform);
        Assert.That(nextCenter.y, Is.EqualTo(homeCenter.y).Within(1));
        Assert.That(homeCenter.x, Is.LessThan(nextCenter.x));
        float boardBottom = Camera.main.WorldToScreenPoint(_board.Find("BoardBackground").GetComponent<SpriteRenderer>().bounds.min).y;
        Assert.That(nextCenter.y, Is.LessThan(boardBottom), "Both actions belong below the solved board.");
        Assert.That(Screen.safeArea.Contains(nextCenter) && Screen.safeArea.Contains(homeCenter), Is.True);
        yield return WaitUntil(() => !_presentation.Confetti.IsPlaying);
        Tap(next);
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == StageCatalog.SceneName(4) && !SceneNavigator.IsLoading);
        yield return null;
        Assert.That(Object.FindFirstObjectByType<GameManager>().CorrectTileCount, Is.Zero);

        yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(10));
        yield return null;
        yield return StageTestReference.Solve(Object.FindFirstObjectByType<GameManager>());
        Assert.That(Button("NextStageButton").transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage11"));
        Assert.That(Button("NextStageButton").interactable, Is.True);
        Tap(Button("ClearHomeButton"));
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == SceneNavigator.TitleScene && !SceneNavigator.IsLoading);
    }

    [UnityTest]
    public IEnumerator RestartDuringShrinkingCancelsPresentationAndRestoresThePlayableBoard()
    {
        yield return PrepareSolution();
        UniTask clear = _game.CommitLineAsync();
        yield return WaitUntil(() => _layout.ClearProgress > 0);
        _game.RestartStage();
        yield return clear.ToCoroutine();
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        Assert.That(_presentation.IsPlaying || _presentation.IsPresented || _presentation.Confetti.IsPlaying, Is.False);
        Assert.That(_layout.ClearProgress, Is.Zero);
        Assert.That(_board.localScale.x, Is.EqualTo(_initialScale).Within(0.001f));
        Assert.That(_game.TilesList.All(tile => tile.CurrentColor == TileColor.None), Is.True);
        Assert.That(GameObject.Find("ClearPanel"), Is.Null);
        Assert.That(Button("HomeButton").interactable, Is.True);
        Assert.That(Button("ReloadAll").interactable, Is.True);
    }
}
