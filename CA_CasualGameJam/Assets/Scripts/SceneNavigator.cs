using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigator
{
    public const string TitleScene = "Title";
    public const string StageScene = "SampleScene";
    public const string SecondStageScene = "Stage02";
    public static bool IsLoading { get; private set; }
    public static event Action LoadingChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLoadingState() { IsLoading = false; LoadingChanged = null; }

    public static async UniTask LoadAsync(string sceneName)
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            LoadingChanged?.Invoke();
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        finally { IsLoading = false; LoadingChanged?.Invoke(); }
    }
}
