using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameHud : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private UnityEngine.UI.Button _redReload;
    [SerializeField] private UnityEngine.UI.Button _blueReload;
    [FormerlySerializedAs("_restart")]
    [SerializeField] private UnityEngine.UI.Button _stageSelect;
    [SerializeField] private TMP_Text _progress;
    [SerializeField] private TMP_Text _status;
    [SerializeField] private CanvasGroup _clearPanel;
    private Tween _clearTween;
    private void Awake()
    {
        _redReload.onClick.AddListener(ReloadRed);
        _blueReload.onClick.AddListener(ReloadBlue);
        _stageSelect.onClick.AddListener(ReturnToStageSelect);
        _gameManager.Changed += Refresh;
        Refresh();
    }
    private void ReloadRed() => _gameManager.ReloadColor(BaseColor.Red);
    private void ReloadBlue() => _gameManager.ReloadColor(BaseColor.Blue);
    private void ReturnToStageSelect()
    {
        if (_gameManager.State == GameState.Cleared)
            SceneNavigator.LoadAsync(SceneNavigator.StageSelectScene).Forget();
    }
    private void Refresh()
    {
        _redReload.interactable = _gameManager.CanReloadColor(BaseColor.Red);
        _blueReload.interactable = _gameManager.CanReloadColor(BaseColor.Blue);
        _stageSelect.interactable = _gameManager.State == GameState.Cleared && !SceneNavigator.IsLoading;
        _progress.text = $"{_gameManager.CorrectTileCount:00} / {_gameManager.TilesList.Count:00}";
        _status.text = _gameManager.State == GameState.Drawing ? "Release to paint" :
            _gameManager.State == GameState.Resolving ? "Checking colors..." : "Trace from a colored dot";
        if (_gameManager.State == GameState.Idle)
        {
            _clearTween?.Kill();
            _clearPanel.gameObject.SetActive(false);
        }
    }
    public async UniTask PlayClearAsync(CancellationToken token)
    {
        _clearPanel.gameObject.SetActive(true); _clearPanel.alpha = 0; _stageSelect.interactable = false;
        _clearTween = DOTween.To(() => _clearPanel.alpha, value => _clearPanel.alpha = value, 1f, 0.3f).SetLink(gameObject);
        try { await UniTask.Delay(300, cancellationToken: token); }
        finally { _clearTween?.Kill(); _clearTween = null; }
    }
    private void OnDestroy()
    {
        _clearTween?.Kill();
        if (_gameManager != null) _gameManager.Changed -= Refresh;
        if (_redReload != null) _redReload.onClick.RemoveListener(ReloadRed);
        if (_blueReload != null) _blueReload.onClick.RemoveListener(ReloadBlue);
        if (_stageSelect != null) _stageSelect.onClick.RemoveListener(ReturnToStageSelect);
    }
}
