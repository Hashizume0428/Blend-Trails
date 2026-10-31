using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Keep automated scene tests from changing the developer's saved progress.</summary>
public abstract class StageProgressTestFixture
{
    // This key is the persistent save format used by StageProgress.
    protected const string ProgressKey = "ColorPuzzle.CurrentStageNumber";
    protected const string StateKey = "ColorPuzzle.StageProgress.v2";
    private bool _hadSave;
    private int _savedStage;
    private bool _hadState;
    private string _savedState;

    // UnitySetUp runs before derived scene-loading UnitySetUps. NUnit SetUp
    // runs afterwards and would reset the progress established by scene Awake.
    [UnitySetUp]
    public IEnumerator IsolateSavedProgress()
    {
        _hadSave = PlayerPrefs.HasKey(ProgressKey);
        _savedStage = PlayerPrefs.GetInt(ProgressKey);
        _hadState = PlayerPrefs.HasKey(StateKey);
        _savedState = PlayerPrefs.GetString(StateKey);
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.DeleteKey(StateKey);
        PlayerPrefs.Save();
        StageProgress.ResetSession();
        yield break;
    }

    [TearDown]
    public void RestoreSavedProgress()
    {
        RestoreKeys();
        StageProgress.ResetSession();
        RestoreKeys();
    }

    private void RestoreKeys()
    {
        if (_hadSave) PlayerPrefs.SetInt(ProgressKey, _savedStage);
        else PlayerPrefs.DeleteKey(ProgressKey);
        if (_hadState) PlayerPrefs.SetString(StateKey, _savedState);
        else PlayerPrefs.DeleteKey(StateKey);
        PlayerPrefs.Save();
    }

    protected static void LoadLegacyProgress(int number)
    {
        PlayerPrefs.DeleteKey(StateKey);
        PlayerPrefs.SetInt(ProgressKey, number);
        PlayerPrefs.Save();
        StageProgress.ResetSession();
    }

    protected static IEnumerator WaitForTitleReady()
    {
        var presentation = Object.FindFirstObjectByType<TitlePresentation>();
        Assert.That(presentation, Is.Not.Null, "The title must include the approved presentation.");
        float deadline = Time.realtimeSinceStartup + 6;
        while (!presentation.IsReady)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Title entrance did not finish.");
            yield return null;
        }
        yield return null;
    }
}
