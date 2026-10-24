using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class StageClearNavigation : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField, Min(0)] private float _clearDisplaySeconds = 1f;
    private CancellationTokenSource _pendingReturn;

    private void OnEnable()
    {
        if (_gameManager == null) return;
        _gameManager.Changed += Refresh;
        Refresh();
    }
    private void Refresh()
    {
        if (_gameManager.State != GameState.Cleared) { CancelReturn(); return; }
        if (_pendingReturn != null) return;
        _pendingReturn = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        ReturnAfterClearAsync(_pendingReturn.Token).Forget();
    }
    private async UniTask ReturnAfterClearAsync(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_clearDisplaySeconds), ignoreTimeScale: true, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            await SceneNavigator.LoadAsync(SceneNavigator.StageSelectScene);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
    private void CancelReturn()
    {
        _pendingReturn?.Cancel();
        _pendingReturn?.Dispose();
        _pendingReturn = null;
    }
    private void OnDisable()
    {
        if (_gameManager != null) _gameManager.Changed -= Refresh;
        CancelReturn();
    }
    private void OnDestroy() => CancelReturn();
}
