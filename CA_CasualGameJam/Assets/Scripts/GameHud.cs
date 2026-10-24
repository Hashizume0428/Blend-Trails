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
    [SerializeField] private UnityEngine.UI.Button _allReload;
    [SerializeField] private UnityEngine.UI.Button _home;
    [FormerlySerializedAs("_restart")]
    [SerializeField] private UnityEngine.UI.Button _stageSelect;
    [SerializeField] private TMP_Text _progress;
    [SerializeField] private TMP_Text _status;
    [SerializeField] private CanvasGroup _clearPanel;
    private Tween _clearTween;
    private bool _leavingScene;
    private void Awake()
    {
        _redReload.onClick.AddListener(ReloadRed);
        _blueReload.onClick.AddListener(ReloadBlue);
        if (_allReload != null) _allReload.onClick.AddListener(ReloadAll);
        if (_home != null) _home.onClick.AddListener(ReturnToTitle);
        _stageSelect.onClick.AddListener(ReturnToStageSelect);
        _gameManager.Changed += Refresh;
        Refresh();
    }
    private bool CanOperate => !_leavingScene && !SceneNavigator.IsLoading;
    private void ReloadRed() { if (CanOperate) _gameManager.ReloadColor(BaseColor.Red); }
    private void ReloadBlue() { if (CanOperate) _gameManager.ReloadColor(BaseColor.Blue); }
    private void ReloadAll() { if (CanOperate) _gameManager.ReloadAll(); }
    private void ReturnToTitle()
    {
        if (CanOperate) ReturnToTitleAsync().Forget();
    }
    private async UniTask ReturnToTitleAsync()
    {
        _leavingScene = true;
        _gameManager.CancelLine();
        Refresh();
        try { await SceneNavigator.LoadAsync(SceneNavigator.TitleScene); }
        finally
        {
            if (this != null) { _leavingScene = false; Refresh(); }
        }
    }
    private void ReturnToStageSelect()
    {
        if (CanOperate && _gameManager.State == GameState.Cleared)
            SceneNavigator.LoadAsync(SceneNavigator.StageSelectScene).Forget();
    }
    private void Refresh()
    {
        _redReload.interactable = CanOperate && _gameManager.CanReloadColor(BaseColor.Red);
        _blueReload.interactable = CanOperate && _gameManager.CanReloadColor(BaseColor.Blue);
        if (_allReload != null) _allReload.interactable = CanOperate && _gameManager.CanReloadAll;
        if (_home != null) _home.interactable = CanOperate;
        _stageSelect.interactable = CanOperate && _gameManager.State == GameState.Cleared;
        if (_progress != null) _progress.text = $"{_gameManager.CorrectTileCount:00} / {_gameManager.TilesList.Count:00}";
        if (_status != null) _status.text = _gameManager.State == GameState.Drawing ? "Release to paint" :
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
        if (_allReload != null) _allReload.onClick.RemoveListener(ReloadAll);
        if (_home != null) _home.onClick.RemoveListener(ReturnToTitle);
        if (_stageSelect != null) _stageSelect.onClick.RemoveListener(ReturnToStageSelect);
    }
}
