using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigator
{
    public const string TitleScene = "Title";
    public const string StageSelectScene = "StageSelect";
    public const string StageScene = "SampleScene";
    public static bool IsLoading { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLoadingState() => IsLoading = false;

    public static async UniTask LoadAsync(string sceneName)
    {
        if (IsLoading) return;
        IsLoading = true;
        try { await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
        finally { IsLoading = false; }
    }
}
