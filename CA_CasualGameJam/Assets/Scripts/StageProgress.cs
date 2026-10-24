using System;
using UnityEngine;

/// <summary>Next stage to play, saved locally so it survives an application restart.</summary>
public static class StageProgress
{
    private const string ProgressKey = "ColorPuzzle.CurrentStageNumber";
    public static int CurrentStageNumber { get; private set; } = 1;
    public static string CurrentScene => GetSceneName(CurrentStageNumber);
    public static bool CanPlayCurrentStage => !string.IsNullOrEmpty(CurrentScene);
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetSession()
    {
        CurrentStageNumber = Mathf.Clamp(PlayerPrefs.GetInt(ProgressKey, 1), 1, StageCatalog.Count + 1);
        Changed = null;
    }

    public static void RecordClear(int stageNumber)
    {
        if (stageNumber < CurrentStageNumber || string.IsNullOrEmpty(GetSceneName(stageNumber))) return;
        SetCurrentStage(stageNumber + 1);
    }

    public static void EnterStage(int stageNumber)
    {
        if (string.IsNullOrEmpty(GetSceneName(stageNumber)) || stageNumber == CurrentStageNumber) return;
        SetCurrentStage(stageNumber);
    }

    private static void SetCurrentStage(int stageNumber)
    {
        CurrentStageNumber = stageNumber;
        PlayerPrefs.SetInt(ProgressKey, stageNumber);
        // Mobile task termination need not call OnApplicationQuit, so flush at each change.
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static string GetSceneName(int stageNumber)
    {
        return StageCatalog.SceneName(stageNumber);
    }
}
