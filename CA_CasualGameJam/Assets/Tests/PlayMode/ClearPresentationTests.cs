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
        Assert.That(_presentation.Confetti.IsPlaying, Is.False, "The badge leads the first paper burst.");
        Assert.That(_layout.ClearProgress, Is.Zero);
        Assert.That(Button("NextStageButton").interactable, Is.False);
        Assert.That(_presentation.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(button => button.name == "ClearHomeButton"), Is.False);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(_layout.ClearProgress, Is.Zero, "Confetti plays before the board moves.");
        Assert.That(Button("NextStageButton").interactable, Is.False);
        yield return WaitUntil(() => _presentation.Confetti.IsPlaying);
        Assert.That(_presentation.ElapsedSeconds, Is.InRange(1.05f,1.5f));
        Assert.That(_layout.ClearProgress, Is.Zero);
        yield return null;
        Canvas.ForceUpdateCanvases();
        var paperRenderer = _presentation.Confetti.GetComponent<CanvasRenderer>();
        Assert.That(paperRenderer != null, Is.True, "Paper pieces require a UI renderer.");
        var paperMesh = paperRenderer.GetMesh();
        Assert.That(paperMesh.vertexCount, Is.GreaterThan(0), "The playing effect must produce visible geometry.");
        yield return WaitUntil(() => _layout.ClearProgress > 0);
        Assert.That(_presentation.IsPlaying, Is.True);
        yield return clear.ToCoroutine();
        Assert.That(_presentation.ElapsedSeconds, Is.InRange(3.15f,3.7f));
        Assert.That(_game.State, Is.EqualTo(GameState.Cleared));
        Assert.That(_presentation.IsPresented, Is.True);
        Assert.That(_board.localScale.x / _initialScale, Is.EqualTo(0.6f).Within(0.01f));
        CollectionAssert.AreEqual(colors, _game.TilesList.Select(tile => tile.CurrentColor));
        CollectionAssert.AreEqual(lines, _game.TilesList.Where(tile => tile.IsStartTile).Select(tile => tile.Line.PointCount));
        Assert.That(_game.LineVisuals.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
        var next = Button("NextStageButton");
        Assert.That(next.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage4"));
        Assert.That(GameObject.Find("ToonClearStage").GetComponent<TMP_Text>().text, Is.EqualTo("STAGE 3"));
        Assert.That(GameObject.Find("ToonClearBackdrop").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        Vector2 nextCenter = Center((RectTransform)next.transform);
        Assert.That(next.transform.parent.childCount, Is.EqualTo(1));
        Assert.That(nextCenter.x, Is.EqualTo(Screen.safeArea.center.x).Within(1));
        float boardBottom = Camera.main.WorldToScreenPoint(_board.Find("BoardBackground").GetComponent<SpriteRenderer>().bounds.min).y;
        Assert.That(nextCenter.y, Is.LessThan(boardBottom), "The next-stage action belongs below the solved board.");
        Assert.That(Screen.safeArea.Contains(nextCenter), Is.True);
        var title = GameObject.Find("ToonClearTitle").transform;
        float titleScale = title.localScale.x;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(title.localScale.x, Is.InRange(.95f, 1.05f));
        Assert.That(Mathf.Abs(title.localScale.x - titleScale), Is.GreaterThan(.001f), "The clear heading keeps pulsing while the card is displayed.");
        Assert.That(_presentation.Confetti.IsPlaying, Is.True, "Idle effects do not delay navigation.");
        Tap(next);
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == StageCatalog.SceneName(4) && !SceneNavigator.IsLoading);
        yield return null;
        Assert.That(Object.FindFirstObjectByType<GameManager>().CorrectTileCount, Is.Zero);

        yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(10));
        yield return null;
        yield return StageTestReference.Solve(Object.FindFirstObjectByType<GameManager>());
        Assert.That(Button("NextStageButton").transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage11"));
        Assert.That(GameObject.Find("ToonClearStage").GetComponent<TMP_Text>().text, Is.EqualTo("STAGE 10"));
        Assert.That(Button("NextStageButton").interactable, Is.True);
        Tap(Button("NextStageButton"));
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == StageCatalog.SceneName(11) && !SceneNavigator.IsLoading);
    }

    [UnityTest]
    public IEnumerator RestartDuringShrinkingCancelsPresentationAndRestoresThePlayableBoard()
    {
        yield return PrepareSolution();
        UniTask clear = _game.CommitLineAsync();
        yield return WaitUntil(() => _layout.ClearProgress > 0);
        var title = GameObject.Find("ToonClearTitle").transform;
        _game.RestartStage();
        yield return clear.ToCoroutine();
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        Assert.That(_presentation.IsPlaying || _presentation.IsPresented || _presentation.Confetti.IsPlaying, Is.False);
        Assert.That(_layout.ClearProgress, Is.Zero);
        Assert.That(title.localScale, Is.EqualTo(Vector3.one), "Restart stops the heading pulse and restores its resting scale.");
        Assert.That(_board.localScale.x, Is.EqualTo(_initialScale).Within(0.001f));
        Assert.That(_game.TilesList.All(tile => tile.CurrentColor == TileColor.None), Is.True);
        Assert.That(GameObject.Find("ClearPanel"), Is.Null);
        Assert.That(GameObject.Find("ToonClearBackdrop"), Is.Null);
        Assert.That(Button("HomeButton").interactable, Is.True);
        Assert.That(Button("ReloadAll").interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator BadgeAndBoardFadeCancelWithoutChangingColorsOrLeavingGlyphsHidden()
    {
        foreach (float moment in new[] { .85f,1.85f,2.95f,3.4f })
        {
            yield return PrepareSolution();
            var colors = _game.TilesList.Select(tile=>tile.CurrentColor).ToArray();
            UniTask clear = _game.CommitLineAsync();
            yield return WaitUntil(()=>_presentation.IsPlaying);
            yield return WaitUntil(()=>_presentation.ElapsedSeconds>=moment);
            CollectionAssert.AreEqual(colors,_game.TilesList.Select(tile=>tile.CurrentColor));
            var boardVisuals = _presentation.GetComponent<ClearBoardVisuals>();
            if (moment>1.8f && moment<1.98f) Assert.That(boardVisuals.Opacity,Is.LessThan(.1f));
            _game.RestartStage();
            yield return clear.ToCoroutine();
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(_presentation.IsPlaying || _presentation.IsPresented,Is.False);
            Assert.That(boardVisuals.Opacity,Is.EqualTo(1));
            Assert.That(_game.State,Is.EqualTo(GameState.Idle));
            Assert.That(_board.localScale.x,Is.EqualTo(_initialScale).Within(.001f));
            foreach(var renderer in _board.GetComponentsInChildren<Renderer>(true))
            {
                var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                if(block.HasProperty(Shader.PropertyToID("_Color")))Assert.That(block.GetColor("_Color").a,Is.EqualTo(1).Within(.001f));
            }
        }
    }

    [UnityTest]
    public IEnumerator DisablingPresentationCancelsTheOldTaskAndRestoresItsVisualState()
    {
        yield return PrepareSolution();
        UniTask clear = _game.CommitLineAsync();
        yield return WaitUntil(()=>_presentation.IsPlaying);
        yield return new WaitForSecondsRealtime(.9f);
        _presentation.enabled=false;
        yield return clear.ToCoroutine();
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(_presentation.IsPlaying || _presentation.IsPresented,Is.False);
        Assert.That(_presentation.GetComponent<ClearBoardVisuals>().Opacity,Is.EqualTo(1));
        Assert.That(GameObject.Find("ClearPanel"),Is.Null);
        _presentation.enabled=true;
        _game.RestartStage();
        yield return StageTestReference.Solve(_game);
        Assert.That(_presentation.IsPresented,Is.True);
        Assert.That(Button("NextStageButton").interactable,Is.True);
    }

    [UnityTest]
    public IEnumerator AnimatedBadgeUsesTheActiveYellowPalettes()
    {
        foreach(int number in new[]{6,11})
        {
            yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(number));
            yield return null;
            var game=Object.FindFirstObjectByType<GameManager>();
            yield return StageTestReference.Solve(game);
            yield return null;
            var badge=Object.FindFirstObjectByType<ToonClearBadge>();
            var mesh=badge.GetComponent<CanvasRenderer>().GetMesh();
            foreach(TileColor color in new[]{PuzzleRules.ToTileColor(game.Palette.First),PuzzleRules.ToTileColor(game.Palette.Second),game.Palette.Mixed})
                Assert.That(mesh.colors32.Contains((Color32)StagePaletteVisuals.UiColor(color)),Is.True,"Badge palette: "+number+" "+color);
        }
    }
}
