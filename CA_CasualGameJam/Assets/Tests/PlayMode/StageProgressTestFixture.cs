using NUnit.Framework;
using UnityEngine;

/// <summary>Keep automated scene tests from changing the developer's saved progress.</summary>
public abstract class StageProgressTestFixture
{
    // This key is the persistent save format used by StageProgress.
    protected const string ProgressKey = "ColorPuzzle.CurrentStageNumber";
    private bool _hadSave;
    private int _savedStage;

    [SetUp]
    public void IsolateSavedProgress()
    {
        _hadSave = PlayerPrefs.HasKey(ProgressKey);
        _savedStage = PlayerPrefs.GetInt(ProgressKey);
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();
        StageProgress.ResetSession();
    }

    [TearDown]
    public void RestoreSavedProgress()
    {
        if (_hadSave) PlayerPrefs.SetInt(ProgressKey, _savedStage);
        else PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();
        StageProgress.ResetSession();
    }
}
