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
    [FormerlySerializedAs("_restart"), FormerlySerializedAs("_stageSelect")]
    [SerializeField] private UnityEngine.UI.Button _nextStage;
    [SerializeField] private UnityEngine.UI.Button _clearHome;
    [SerializeField] private TMP_Text _stageLabel;
    [SerializeField] private TMP_Text _clearMessage;
    [SerializeField] private TMP_Text _progress;
    [SerializeField] private TMP_Text _status;
    [SerializeField] private CanvasGroup _clearPanel;
    private Tween _clearTween;
    private bool _leavingScene;
    private StageClearNavigation _stageNavigation;
    private void Awake()
    {
        _redReload.onClick.AddListener(ReloadRed);
        _blueReload.onClick.AddListener(ReloadBlue);
        if (_allReload != null) _allReload.onClick.AddListener(ReloadAll);
        if (_home != null) _home.onClick.AddListener(ReturnToTitle);
        _stageNavigation = _gameManager.GetComponent<StageClearNavigation>();
        if (_nextStage != null) _nextStage.onClick.AddListener(GoToNextStage);
        if (_clearHome != null) _clearHome.onClick.AddListener(ReturnHomeAfterClear);
        _gameManager.Changed += Refresh;
        SceneNavigator.LoadingChanged += Refresh;
        Refresh();
    }
    private bool CanOperate => !_leavingScene && !SceneNavigator.IsLoading;
    private void ReloadRed() { if (CanOperate) _gameManager.ReloadColor(BaseColor.Red); }
    private void ReloadBlue() { if (CanOperate) _gameManager.ReloadColor(BaseColor.Blue); }
    private void ReloadAll() { if (CanOperate) _gameManager.ReloadAll(); }
    private void ReturnToTitle()
    {
        if (CanOperate) NavigateAsync(SceneNavigator.TitleScene).Forget();
    }
    private async UniTask NavigateAsync(string sceneName)
    {
        _leavingScene = true;
        _gameManager.CancelLine();
        Refresh();
        try { await SceneNavigator.LoadAsync(sceneName); }
        finally
        {
            if (this != null) { _leavingScene = false; Refresh(); }
        }
    }
    private void ReturnHomeAfterClear()
    {
        if (CanOperate && _gameManager.State == GameState.Cleared)
            NavigateAsync(SceneNavigator.TitleScene).Forget();
    }
    private void GoToNextStage()
    {
        if (CanOperate && _gameManager.State == GameState.Cleared && _stageNavigation != null && _stageNavigation.HasNextStage)
            NavigateAsync(_stageNavigation.NextStageScene).Forget();
    }
    private void Refresh()
    {
        _redReload.interactable = CanOperate && _gameManager.CanReloadColor(BaseColor.Red);
        _blueReload.interactable = CanOperate && _gameManager.CanReloadColor(BaseColor.Blue);
        if (_allReload != null) _allReload.interactable = CanOperate && _gameManager.CanReloadAll;
        if (_home != null) _home.interactable = CanOperate;
        bool cleared = CanOperate && _gameManager.State == GameState.Cleared;
        if (_clearHome != null) _clearHome.interactable = cleared;
        if (_nextStage != null) _nextStage.interactable = cleared && _stageNavigation != null && _stageNavigation.HasNextStage;
        if (_stageLabel != null) _stageLabel.text = $"-Stage{(_stageNavigation != null ? _stageNavigation.StageNumber : 1)}-";
        if (_clearMessage != null) _clearMessage.text = _stageNavigation != null && !_stageNavigation.HasNextStage
            ? "ALL STAGES CLEAR" : "Every color in its place.";
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
        _clearPanel.gameObject.SetActive(true); _clearPanel.alpha = 0;
        if (_nextStage != null) _nextStage.interactable = false;
        if (_clearHome != null) _clearHome.interactable = false;
        _clearTween = DOTween.To(() => _clearPanel.alpha, value => _clearPanel.alpha = value, 1f, 0.3f).SetLink(gameObject);
        try { await UniTask.Delay(300, cancellationToken: token); }
        finally { _clearTween?.Kill(); _clearTween = null; }
    }
    private void OnDestroy()
    {
        _clearTween?.Kill();
        SceneNavigator.LoadingChanged -= Refresh;
        if (_gameManager != null) _gameManager.Changed -= Refresh;
        if (_redReload != null) _redReload.onClick.RemoveListener(ReloadRed);
        if (_blueReload != null) _blueReload.onClick.RemoveListener(ReloadBlue);
        if (_allReload != null) _allReload.onClick.RemoveListener(ReloadAll);
        if (_home != null) _home.onClick.RemoveListener(ReturnToTitle);
        if (_nextStage != null) _nextStage.onClick.RemoveListener(GoToNextStage);
        if (_clearHome != null) _clearHome.onClick.RemoveListener(ReturnHomeAfterClear);
    }
}
