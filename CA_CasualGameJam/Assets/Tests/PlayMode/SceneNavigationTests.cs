using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SceneNavigationTests
{
    [SetUp]
    public void ResetProgress() => StageProgress.ResetSession();

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
        Assert.That(button.interactable, Is.True, button.name);
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
    private static IEnumerator Solve(GameManager game) => StageTestReference.Solve(game);
    private static void AssertFreshStage(int number)
    {
        var game = Object.FindFirstObjectByType<GameManager>();
        Assert.That(game.IsInitialized, Is.True);
        StageTestReference.AssertLayout(game, number);
        Assert.That(game.CorrectTileCount, Is.Zero);
        Assert.That(game.GetComponent<StageClearNavigation>().StageNumber, Is.EqualTo(number));
        Assert.That(GameObject.Find("Level").GetComponent<TMP_Text>().text, Is.EqualTo($"-Stage{number}-"));
        foreach (TileController tile in game.TilesList) Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.None));
        Assert.That(Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(Button("HomeButton").interactable, Is.True, "Home must be ready as soon as the scene load finishes.");
        Assert.That(Button("ReloadAll").interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator TitleHasOneStageButtonAndOpensThePuzzleDirectly()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.TitleScene);
        yield return null;
        Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(GameObject.Find("TapToStart"), Is.Null);
        Canvas.ForceUpdateCanvases();
        var outside = new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width * 0.1f, Screen.height * 0.9f) };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(outside, hits);
        Assert.That(hits, Is.Empty, "Only the stage button should receive taps.");
        var stage = Button("Stage01Button");
        Assert.That(stage.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage1"));
        Tap(stage);
        stage.onClick.Invoke();
        yield return WaitForScene(SceneNavigator.StageScene);
        AssertFreshStage(1);
    }

    [UnityTest]
    public IEnumerator ClearingStageOneUpdatesTheTitleToStageTwoAndStartsThatStage()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        yield return Solve(Object.FindFirstObjectByType<GameManager>());
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(2));
        Assert.That(Button("ClearHomeButton").interactable, Is.True);
        Assert.That(Button("NextStageButton").interactable, Is.True);
        yield return new WaitForSecondsRealtime(1.3f);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNavigator.StageScene), "Clear must not navigate automatically.");
        Tap(Button("ClearHomeButton"));
        yield return WaitForScene(SceneNavigator.TitleScene);
        Assert.That(Button("Stage01Button").transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage2"));
        Tap(Button("Stage01Button"));
        yield return WaitForScene(SceneNavigator.SecondStageScene);
        AssertFreshStage(2);
    }

    [UnityTest]
    public IEnumerator AllTenReferenceStagesClearInOrderAndFinalClearCanReturnHome()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        yield return Solve(Object.FindFirstObjectByType<GameManager>());
        var next = Button("NextStageButton");
        Tap(next);
        next.onClick.Invoke();
        yield return WaitForScene(SceneNavigator.SecondStageScene);
        AssertFreshStage(2);
        var game = Object.FindFirstObjectByType<GameManager>();
        Assert.That(game.GetTile(new Vector2Int(3, 2)).IsStartTile, Is.True);
        Assert.That(game.GetTile(Vector2Int.zero).IsStartTile, Is.True);
        for (int number = 2; number <= StageCatalog.Count; number++)
        {
            AssertFreshStage(number);
            game = Object.FindFirstObjectByType<GameManager>();
            yield return Solve(game);
            Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(number + 1));
            Assert.That(Button("NextStageButton").interactable, Is.EqualTo(number < StageCatalog.Count));
            if (number < StageCatalog.Count)
            {
                Tap(Button("NextStageButton"));
                yield return WaitForScene(StageCatalog.SceneName(number + 1));
            }
        }
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(11));
        Assert.That(Button("NextStageButton").interactable, Is.False);
        Assert.That(Button("ClearHomeButton").interactable, Is.True);
        Assert.That(GameObject.Find("ClearMessage").GetComponent<TMP_Text>().text, Is.EqualTo("ALL STAGES CLEAR"));
        Button("NextStageButton").onClick.Invoke();
        yield return new WaitForSecondsRealtime(1.3f);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(StageCatalog.SceneName(10)));
        Tap(Button("ClearHomeButton"));
        yield return WaitForScene(SceneNavigator.TitleScene);
        var stage = Button("Stage01Button");
        Assert.That(stage.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage11"));
        Assert.That(stage.transform.Find("Description").GetComponent<TMP_Text>().text, Is.EqualTo("COMING SOON"));
        Assert.That(stage.interactable, Is.False);
        stage.onClick.Invoke();
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNavigator.TitleScene));
    }

    [UnityTest]
    public IEnumerator StageThreeUsesSeparatePurpleFramesAndHomeKeepsTheStageNumber()
    {
        yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(3));
        yield return null;
        var game = Object.FindFirstObjectByType<GameManager>();
        StageTestReference.AssertLayout(game, 3);
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(3));
        Assert.That(GameObject.Find("Level").GetComponent<TMP_Text>().text, Is.EqualTo("-Stage3-"));
        Tap(Button("HomeButton"));
        yield return WaitForScene(SceneNavigator.TitleScene);
        Assert.That(Button("Stage01Button").transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage3"));
        Tap(Button("Stage01Button"));
        yield return WaitForScene(StageCatalog.SceneName(3));
        AssertFreshStage(3);
    }

    [UnityTest]
    public IEnumerator ClearChoicesCannotNavigateBeforeTheStageIsCleared()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        var game = Object.FindFirstObjectByType<GameManager>();
        Assert.That(game.TryBeginLine(game.GetTile(Vector2Int.zero).TileIndex), Is.True);
        foreach (var button in Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<UnityEngine.UI.Button>(true))
            if (button.name == "NextStageButton" || button.name == "ClearHomeButton") button.onClick.Invoke();
        yield return null;
        Assert.That(game.State, Is.EqualTo(GameState.Drawing));
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(1));
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNavigator.StageScene));
        game.CancelLine();
    }

    [UnityTest]
    public IEnumerator IncompleteLinesAndReloadDoNotAdvanceTheTitleStage()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        var game = Object.FindFirstObjectByType<GameManager>();
        foreach (Vector2Int point in new[] { new Vector2Int(0, 0), new Vector2Int(4, 3) })
        {
            Assert.That(game.TryBeginLine(game.GetTile(point).TileIndex), Is.True);
            yield return game.CommitLineAsync().ToCoroutine();
        }
        Assert.That(game.State, Is.EqualTo(GameState.Idle));
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(1));
        Assert.That(game.ReloadAll(), Is.True);
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(1));
        Tap(Button("HomeButton"));
        yield return WaitForScene(SceneNavigator.TitleScene);
        Assert.That(Button("Stage01Button").transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage1"));
        Assert.That(Button("Stage01Button").interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator ReclearingTheSameStageDoesNotAdvanceTheNumberTwice()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        var game = Object.FindFirstObjectByType<GameManager>();
        yield return Solve(game);
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(2));
        game.RestartStage();
        yield return Solve(game);
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(2));
        Tap(Button("ClearHomeButton"));
        yield return WaitForScene(SceneNavigator.TitleScene);
        Assert.That(Button("Stage01Button").transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage2"));
    }
}
