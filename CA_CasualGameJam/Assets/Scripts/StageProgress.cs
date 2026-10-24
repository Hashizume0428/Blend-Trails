using System;
using UnityEngine;

/// <summary>Next stage to play, retained across scene changes for the current play session.</summary>
public static class StageProgress
{
    public static int CurrentStageNumber { get; private set; } = 1;
    public static string CurrentScene => GetSceneName(CurrentStageNumber);
    public static bool CanPlayCurrentStage => !string.IsNullOrEmpty(CurrentScene);
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetSession()
    {
        CurrentStageNumber = 1;
        Changed = null;
    }

    public static void RecordClear(int stageNumber)
    {
        if (stageNumber < CurrentStageNumber) return;
        CurrentStageNumber = stageNumber + 1;
        Changed?.Invoke();
    }

    public static void EnterStage(int stageNumber)
    {
        if (string.IsNullOrEmpty(GetSceneName(stageNumber)) || stageNumber == CurrentStageNumber) return;
        CurrentStageNumber = stageNumber;
        Changed?.Invoke();
    }

    public static string GetSceneName(int stageNumber)
    {
        return StageCatalog.SceneName(stageNumber);
    }
}
