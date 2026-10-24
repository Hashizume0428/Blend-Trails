using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SceneNavigationTests
{
    private static IEnumerator WaitForScene(string name)
    {
        float deadline = Time.realtimeSinceStartup + 10;
        while (SceneManager.GetActiveScene().name != name || SceneNavigator.IsLoading)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Scene transition did not finish: " + name);
            yield return null;
        }
        yield return null;
    }
    private static UnityEngine.UI.Button Button(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
    private static void Tap(UnityEngine.UI.Button button)
    {
        Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Canvas.ForceUpdateCanvases();
        var rect = button.GetComponent<RectTransform>();
        Assert.That(rect.rect.width, Is.GreaterThan(0));
        Assert.That(rect.rect.height, Is.GreaterThan(0));
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0));
        Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), "The visible button must receive taps.");
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    private static IEnumerator Solve(GameManager game)
    {
        int Index(int x, int y) => game.GetTile(new Vector2Int(x, y)).TileIndex;
        void Extend(int x, int y) => Assert.That(game.TryExtendLine(Index(x, y)), Is.True);
        Assert.That(game.TryBeginLine(Index(0, 0)), Is.True);
        Extend(0, 1); Extend(1, 1); Extend(1, 0); Extend(2, 0);
        Extend(2, 1); Extend(2, 2); Extend(3, 2); Extend(3, 1); Extend(3, 0);
        yield return game.CommitLineAsync().ToCoroutine();
        Assert.That(game.TryBeginLine(Index(0, 3)), Is.True);
        Extend(0, 2); Extend(1, 2); Extend(1, 3); Extend(2, 3);
        Extend(3, 3); Extend(3, 2); Extend(3, 1);
        yield return game.CommitLineAsync().ToCoroutine();
        Assert.That(game.State, Is.EqualTo(GameState.Cleared));
    }

    [UnityTest]
    public IEnumerator TitleTapStageSelectionClearAndReplayLoop()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.TitleScene);
        yield return null;
        var start = Button("TapToStart");
        Tap(start);
        start.onClick.Invoke(); // Repeated taps must not start another load.
        yield return WaitForScene(SceneNavigator.StageSelectScene);
        Tap(Button("Stage01Button"));
        yield return WaitForScene(SceneNavigator.StageScene);
        var game = Object.FindFirstObjectByType<GameManager>();
        Assert.That(game.IsInitialized, Is.True);
        Assert.That(game.CorrectTileCount, Is.Zero);
        yield return Solve(game);
        Assert.That(Button("StageSelectButton").interactable, Is.True);
        yield return WaitForScene(SceneNavigator.StageSelectScene);
        Tap(Button("Stage01Button"));
        yield return WaitForScene(SceneNavigator.StageScene);
        game = Object.FindFirstObjectByType<GameManager>();
        Assert.That(game.IsInitialized, Is.True);
        Assert.That(game.CorrectTileCount, Is.Zero);
        foreach (TileController tile in game.TilesList) Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.None));
        Assert.That(Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ClearButtonCanReturnToSelectionWithoutAutomaticReturn()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        var game = Object.FindFirstObjectByType<GameManager>();
        game.GetComponent<StageClearNavigation>().enabled = false;
        yield return Solve(game);
        Tap(Button("StageSelectButton"));
        yield return WaitForScene(SceneNavigator.StageSelectScene);
        Assert.That(Object.FindFirstObjectByType<GameManager>(), Is.Null);
    }
}
