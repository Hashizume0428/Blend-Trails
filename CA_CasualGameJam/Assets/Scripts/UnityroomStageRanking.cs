#if UNITY_WEBGL && !UNITY_EDITOR && !UNITY_INCLUDE_TESTS
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unityroom.Client;

/// <summary>Automatically synchronizes cleared stages with unityroom scoreboard No.1.</summary>
public sealed class UnityroomStageRanking : MonoBehaviour
{
    private const int ScoreboardId = 1;
    private const string HmacKey = "sWwZL9ZKGUiFIBNHdUbOPtjIHoFbL/orwBFLN9nWqkjIrXCPXT1qC28Tr8CZioLfJ27vTKVUDUo0lYfr0XDpBw==";
    private UnityroomClient _client;
    private StageRankingSync _sync;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var service = new GameObject(nameof(UnityroomStageRanking));
        DontDestroyOnLoad(service);
        service.AddComponent<UnityroomStageRanking>();
    }

    private void Awake()
    {
        _client = new UnityroomClient
        {
            HmacKey = HmacKey,
            Timeout = TimeSpan.FromSeconds(15),
            // The synchronization loop retries failures with a longer interval.
            MaxRetries = 0
        };
        _sync = new StageRankingSync(SendScoreAsync);
        _sync.Submit(StageProgress.ClearedStageCount);
        StageProgress.Cleared += OnStageCleared;
    }

    private void Start() => SynchronizeAsync(destroyCancellationToken).Forget();
    private void OnStageCleared(int clearedStageCount) => _sync.Submit(clearedStageCount);

    private async UniTask SendScoreAsync(int clearedStageCount, CancellationToken cancellationToken)
    {
        // Descending / high-score-only behavior is configured on the unityroom board.
        await _client.Scoreboards.SendAsync(new SendScoreRequest
        {
            ScoreboardId = ScoreboardId,
            Score = clearedStageCount
        }, cancellationToken);
    }

    private async UniTask SynchronizeAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TimeSpan interval = TimeSpan.FromSeconds(5);
            try
            {
                await _sync.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                // Keep local progress and try the newest pending score again, without blocking play.
                string reason = exception is UnityroomApiException apiException
                    ? $"{apiException.ErrorCode} ({apiException.ErrorType})"
                    : exception.GetType().Name;
                Debug.LogWarning($"unityroom ranking: score submission failed ({reason}); retrying in 30 seconds.");
                interval = TimeSpan.FromSeconds(30);
            }

            try { await UniTask.Delay(interval, ignoreTimeScale: true, cancellationToken: cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
        }
    }

    private void OnDestroy()
    {
        StageProgress.Cleared -= OnStageCleared;
        _client?.Dispose();
    }
}
#endif
