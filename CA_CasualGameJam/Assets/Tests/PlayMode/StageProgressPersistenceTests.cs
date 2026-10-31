using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[Category("StageProgression")]
public class StageProgressPersistenceTests : StageProgressTestFixture
{
    [Test]
    public void StartupMigratesTheOldSaveIncludingStageElevenAndExtraProgress()
    {
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(1));
        foreach (int number in new[] { 4, 11, StageCatalog.Count + 1, 0 })
        {
            LoadLegacyProgress(number);
            Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(Mathf.Max(1, number)));
            Assert.That(StageProgress.CanPlayCurrentStage, Is.True);
        }
    }

    [Test]
    public void ExtraDrawsExcludeThePreviousEightAndSurviveRestartWithoutRerolling()
    {
        LoadLegacyProgress(60);
        StageProgress.RecordClear(60);
        CollectionAssert.AreEqual(Enumerable.Range(53, 8), StageProgress.RecentPuzzleIds);
        Assert.That(StageProgress.CurrentPuzzleId, Is.InRange(5, 52));
        for (int i = 0; i < 40; i++)
        {
            int number = StageProgress.CurrentStageNumber, puzzle = StageProgress.CurrentPuzzleId;
            int[] history = StageProgress.RecentPuzzleIds.ToArray();
            Assert.That(number, Is.EqualTo(61 + i));
            Assert.That(StageProgress.CurrentKind, Is.EqualTo(StageKind.Extra));
            Assert.That(puzzle, Is.InRange(5, 60));
            CollectionAssert.DoesNotContain(history, puzzle);
            StageProgress.ResetSession();
            StageProgress.EnterStage(puzzle);
            Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(number));
            Assert.That(StageProgress.CurrentPuzzleId, Is.EqualTo(puzzle));
            CollectionAssert.AreEqual(history, StageProgress.RecentPuzzleIds);
            StageProgress.RecordClear(number);
            StageProgress.RecordClear(number);
            Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(number + 1));
            Assert.That(StageProgress.RecentPuzzleIds.Last(), Is.EqualTo(puzzle));
            Assert.That(StageProgress.RecentPuzzleIds.Count, Is.EqualTo(8));
        }
    }

    [TestCase(31)]
    [TestCase(45)]
    [TestCase(60)]
    public void FormerExtraStageNowOpensItsNewAuthoredPuzzleWithoutLosingTheDisplayNumber(int number)
    {
        PlayerPrefs.SetInt(ProgressKey, 1); // Stale compatibility save must not override v2.
        PlayerPrefs.SetString(StateKey, $"{{\"stageNumber\":{number},\"puzzleId\":11,\"recentPuzzleIds\":[23,24,25,26,27,28,29,30]}}");
        StageProgress.ResetSession();
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(number));
        Assert.That(StageProgress.CurrentPuzzleId, Is.EqualTo(number));
        Assert.That(StageProgress.CurrentKind, Is.EqualTo(StageKind.Normal));
        Assert.That(StageProgress.CurrentScene, Is.EqualTo(StageCatalog.SceneName(number)));
        Assert.That(PlayerPrefs.GetInt(ProgressKey), Is.EqualTo(number));
        CollectionAssert.AreEqual(Enumerable.Range(number - 8, 8), StageProgress.RecentPuzzleIds);
        StageProgress.ResetSession();
        Assert.That(StageProgress.CurrentPuzzleId, Is.EqualTo(number), "Persist the migration.");
    }

    [Test]
    public void FormerExtraAboveSixtyKeepsItsSelectedPuzzleAndHistory()
    {
        PlayerPrefs.SetString(StateKey, "{\"stageNumber\":73,\"puzzleId\":11,\"recentPuzzleIds\":[23,24,25,26,27,28,29,30]}");
        StageProgress.ResetSession();
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(73));
        Assert.That(StageProgress.CurrentPuzzleId, Is.EqualTo(11));
        Assert.That(StageProgress.CurrentKind, Is.EqualTo(StageKind.Extra));
        CollectionAssert.AreEqual(Enumerable.Range(23, 8), StageProgress.RecentPuzzleIds);
    }

    [Test]
    public void ReclearingAndInvalidClearsDoNotAdvanceSavedProgress()
    {
        StageProgress.RecordClear(1);
        Assert.That(PlayerPrefs.GetInt(ProgressKey), Is.EqualTo(2));
        StageProgress.RecordClear(1);
        StageProgress.RecordClear(0);
        StageProgress.RecordClear(StageCatalog.Count + 1);
        StageProgress.ResetSession();
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator ClearSavesBeforePresentationAndTheTitleResumesTheSavedStage()
    {
        yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(3));
        yield return null;
        Assert.That(PlayerPrefs.GetInt(ProgressKey), Is.EqualTo(3));
        var game = Object.FindFirstObjectByType<GameManager>();
        StageTestReference.DrawLine(game, BaseColor.Red);
        yield return game.CommitLineAsync().ToCoroutine();
        StageTestReference.DrawLine(game, BaseColor.Blue);
        UniTask clear = game.CommitLineAsync();
        var presentation = Object.FindFirstObjectByType<GameHud>().ClearPresentation;
        float deadline = Time.realtimeSinceStartup + 5;
        while (!presentation.IsPlaying)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        Assert.That(game.State, Is.EqualTo(GameState.Resolving));
        Assert.That(PlayerPrefs.GetInt(ProgressKey), Is.EqualTo(4), "Save before confetti/shrinking can be interrupted.");
        yield return clear.ToCoroutine();

        // Run the same initialization that happens on a fresh application launch.
        StageProgress.ResetSession();
        yield return SceneManager.LoadSceneAsync(SceneNavigator.TitleScene);
        yield return null;
        yield return WaitForTitleReady();
        var stage = GameObject.Find("Stage01Button").GetComponent<UnityEngine.UI.Button>();
        Assert.That(stage.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage4"));
        Assert.That(stage.interactable, Is.True);
        stage.onClick.Invoke();
        deadline = Time.realtimeSinceStartup + 5;
        while (SceneManager.GetActiveScene().name != StageCatalog.SceneName(4) || SceneNavigator.IsLoading)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        yield return null;
        Assert.That(GameObject.Find("Level").GetComponent<TMP_Text>().text, Is.EqualTo("-Stage4-"));
        Assert.That(Object.FindFirstObjectByType<GameManager>().CorrectTileCount, Is.Zero);
    }
}
