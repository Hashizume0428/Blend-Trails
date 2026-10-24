using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class StageOneTutorialTests : StageProgressTestFixture
{
    private GameManager _game;
    private StageOneTutorialGuide _guide;

    [UnitySetUp]
    public IEnumerator LoadStage()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        Canvas.ForceUpdateCanvases();
        _game = Object.FindFirstObjectByType<GameManager>();
        _guide = Object.FindFirstObjectByType<StageOneTutorialGuide>();
        Assert.That(_game.IsInitialized, Is.True);
        Assert.That(_guide, Is.Not.Null);
        // Model a focused foreground player, independently of the Editor test window's focus.
        _guide.SendMessage("OnApplicationFocus", true);
        _guide.SendMessage("OnApplicationPause", false);
    }

    private IEnumerator WaitForGuide()
    {
        float deadline = Time.realtimeSinceStartup + 4;
        while (!_guide.IsVisible)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Guide did not appear.");
            yield return null;
        }
    }

    private void Draw(string cells)
    {
        Vector2Int[] route = cells.Split(' ').Select(cell => new Vector2Int(cell[1] - '1', cell[0] - '1')).ToArray();
        Assert.That(_game.TryBeginLine(_game.GetTile(route[0]).TileIndex), Is.True);
        for (int i = 1; i < route.Length; i++)
            Assert.That(_game.TryExtendLine(_game.GetTile(route[i]).TileIndex), Is.True, route[i].ToString());
    }

    [UnityTest]
    public IEnumerator PreviewMovesWithoutPaintingAndAllowsInputThroughEveryGraphic()
    {
        yield return WaitForGuide();
        Vector2 initialTip = _guide.HandTipScreenPosition;
        yield return new WaitForSecondsRealtime(0.8f);
        Assert.That(Vector2.Distance(initialTip, _guide.HandTipScreenPosition), Is.GreaterThan(1));
        yield return new WaitForSecondsRealtime(4.2f);
        Assert.That(_game.State, Is.EqualTo(GameState.Idle));
        Assert.That(_game.TilesList.All(tile => tile.CurrentColor == TileColor.None), Is.True);
        Assert.That(_game.HasCommittedLine(BaseColor.Red), Is.False);
        Assert.That(_game.HasCommittedLine(BaseColor.Blue), Is.False);
        Assert.That(_game.StageClearCheck(), Is.False);
        Assert.That(_guide.GetComponentsInChildren<TMPro.TMP_Text>(true), Is.Empty);
        Assert.That(_guide.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g => !g.raycastTarget), Is.True);
        foreach (TileController tile in _game.TilesList)
        {
            var hits = new List<RaycastResult>();
            Vector2 position = Camera.main.WorldToScreenPoint(tile.transform.position);
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            Assert.That(hits, Is.Empty, tile.name);
        }
        var input = Object.FindFirstObjectByType<InputTouchManager>();
        Vector2 start = Camera.main.WorldToScreenPoint(_game.GetTile(Vector2Int.zero).transform.position);
        Assert.That(input.BeginPointer(start, -1), Is.True);
        Assert.That(_guide.IsVisible, Is.False);
        input.CancelPointer();
        yield return WaitForGuide();
        Assert.That(_guide.GuideColor, Is.EqualTo(BaseColor.Red));
    }

    [UnityTest]
    public IEnumerator DifferentStrokeShapeStillGuidesBlueAndResetsAndInterruptionsResume()
    {
        Draw("11 21 22 23 24 25 15 14 13 12");
        Assert.That(_guide.IsVisible, Is.False);
        float releasedAt = Time.unscaledTime;
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_guide.GuideColor, Is.EqualTo(BaseColor.Blue));
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(_guide.IsVisible, Is.False, "Do not start the blue guide before the release delay.");
        yield return WaitForGuide();
        Assert.That(Time.unscaledTime - releasedAt, Is.InRange(.45f, .8f), "Blue guidance starts about half a second after releasing red.");
        _guide.SendMessage("OnApplicationFocus", false);
        Assert.That(_guide.IsVisible, Is.False);
        _guide.SendMessage("OnApplicationFocus", true);
        yield return WaitForGuide();
        _guide.SendMessage("OnApplicationPause", true);
        Assert.That(_guide.IsVisible, Is.False);
        _guide.SendMessage("OnApplicationPause", false);
        yield return WaitForGuide();
        Assert.That(_game.ReloadColor(BaseColor.Red), Is.True);
        Assert.That(_guide.GuideColor, Is.EqualTo(BaseColor.Red));
        yield return WaitForGuide();
        Assert.That(_game.ReloadAll(), Is.True);
        Assert.That(_guide.IsVisible, Is.False);
        Assert.That(_guide.GuideColor, Is.EqualTo(BaseColor.Red));
        yield return WaitForGuide();
    }

    [UnityTest]
    public IEnumerator BlueFirstAndReferenceSolutionClearWithoutGuideDuringResolution()
    {
        StageTestReference.DrawLine(_game, BaseColor.Blue);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_guide.GuideColor, Is.EqualTo(BaseColor.Red));
        yield return WaitForGuide();
        StageTestReference.DrawLine(_game, BaseColor.Red);
        UniTask commit = _game.CommitLineAsync();
        Assert.That(_game.State, Is.EqualTo(GameState.Resolving));
        Assert.That(_guide.IsVisible, Is.False);
        yield return commit.ToCoroutine();
        Assert.That(_game.State, Is.EqualTo(GameState.Cleared));
        Assert.That(_game.CorrectTileCount, Is.EqualTo(20));
        Assert.That(_guide.IsVisible, Is.False);
    }

    [UnityTest]
    public IEnumerator DifferentFinalColorPartitionClearsWithoutConflictingGuidance()
    {
        Draw("11 21 22 12 13 23 24 14 15");
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_guide.GuideColor, Is.Null);
        Assert.That(_guide.IsVisible, Is.False);
        Assert.That(_game.TilesList.Count(tile => tile.CurrentColor == TileColor.Red), Is.EqualTo(9));
        Draw("45 44 43 42 41 31 32 33 34 35 25");
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.State, Is.EqualTo(GameState.Cleared));
        Assert.That(_game.CorrectTileCount, Is.EqualTo(20));
        Assert.That(_game.TilesList.Count(tile => tile.CurrentColor == TileColor.Blue), Is.EqualTo(11));
        Assert.That(_guide.IsVisible, Is.False);
    }

    [UnityTest]
    public IEnumerator OtherPuzzlesAndPersistentExtrasHaveNoTutorial()
    {
        foreach (int id in new[] { 2, 30 })
        {
            yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(id));
            yield return null;
            Assert.That(Object.FindFirstObjectByType<StageOneTutorialGuide>(), Is.Null);
        }
        LoadLegacyProgress(31);
        yield return SceneManager.LoadSceneAsync(StageProgress.CurrentScene);
        yield return null;
        var navigation = Object.FindFirstObjectByType<StageClearNavigation>();
        Assert.That(navigation.StageNumber, Is.EqualTo(31));
        Assert.That(StageCatalog.IsNormalPuzzle(navigation.PuzzleId), Is.True);
        Assert.That(Object.FindFirstObjectByType<StageOneTutorialGuide>(), Is.Null);
    }

    [UnityTest]
    public IEnumerator HomeHidesGuideBeforeLoadingAndLeavesNoTutorial()
    {
        yield return WaitForGuide();
        GameObject.Find("HomeButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Assert.That(_guide.IsVisible, Is.False);
        float deadline = Time.realtimeSinceStartup + 10;
        while (SceneNavigator.IsLoading || SceneManager.GetActiveScene().name != SceneNavigator.TitleScene)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        Assert.That(Object.FindFirstObjectByType<StageOneTutorialGuide>(), Is.Null);
    }
}
