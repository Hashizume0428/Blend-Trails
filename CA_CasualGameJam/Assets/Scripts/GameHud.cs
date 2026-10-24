using System.Threading;
using Cysharp.Threading.Tasks;
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
    [SerializeField] private TMP_Text _stageLabel;
    [SerializeField] private TMP_Text _clearMessage;
    [SerializeField] private TMP_Text _progress;
    [SerializeField] private TMP_Text _status;
    [SerializeField] private CanvasGroup _clearPanel;
    [SerializeField] private StageClearPresentation _clearPresentation;
    [SerializeField] private TMP_Text _nextStageLabel;
    [SerializeField] private UnityEngine.UI.Image _mixLegend;
    public StageClearPresentation ClearPresentation => _clearPresentation;
    private bool _leavingScene;
    private StageClearNavigation _stageNavigation;
    private Material _paletteMaterial;
    private bool _paletteApplied;
    private void Awake()
    {
        _redReload.onClick.AddListener(ReloadFirst);
        _blueReload.onClick.AddListener(ReloadSecond);
        if (_allReload != null) _allReload.onClick.AddListener(ReloadAll);
        if (_home != null) _home.onClick.AddListener(ReturnToTitle);
        _stageNavigation = _gameManager.GetComponent<StageClearNavigation>();
        if (_nextStage != null) _nextStage.onClick.AddListener(GoToNextStage);
        _gameManager.Changed += Refresh;
        SceneNavigator.LoadingChanged += Refresh;
        Refresh();
    }
    private bool CanOperate => !_leavingScene && !SceneNavigator.IsLoading &&
        (_clearPresentation == null || !_clearPresentation.IsPlaying);
    private void ReloadFirst() { if (CanOperate) _gameManager.ReloadColor(_gameManager.Palette.First); }
    private void ReloadSecond() { if (CanOperate) _gameManager.ReloadColor(_gameManager.Palette.Second); }
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
    private void GoToNextStage()
    {
        if (CanOperate && _gameManager.State == GameState.Cleared && _stageNavigation != null && _stageNavigation.HasNextStage)
            NavigateAsync(_stageNavigation.NextStageScene).Forget();
    }
    private void Refresh()
    {
        ApplyPaletteVisuals();
        if (_gameManager.State == GameState.Idle)
        {
            if (_clearPresentation != null) _clearPresentation.ResetPresentation();
            _clearPanel.gameObject.SetActive(false);
        }
        _redReload.interactable = CanOperate && _gameManager.CanReloadColor(_gameManager.Palette.First);
        _blueReload.interactable = CanOperate && _gameManager.CanReloadColor(_gameManager.Palette.Second);
        if (_allReload != null) _allReload.interactable = CanOperate && _gameManager.CanReloadAll;
        if (_home != null) _home.interactable = CanOperate;
        bool cleared = CanOperate && _gameManager.State == GameState.Cleared;
        if (_nextStage != null) _nextStage.interactable = cleared && _stageNavigation != null && _stageNavigation.HasNextStage;
        if (_nextStageLabel != null) _nextStageLabel.text = $"Stage{(_stageNavigation != null ? _stageNavigation.StageNumber + 1 : 2)}";
        if (_stageLabel != null) _stageLabel.text = $"-Stage{(_stageNavigation != null ? _stageNavigation.StageNumber : 1)}-";
        if (_clearMessage != null) _clearMessage.text = _stageNavigation != null && !_stageNavigation.HasNextStage
            ? "ALL STAGES CLEAR" : "Every color in its place.";
        if (_progress != null) _progress.text = $"{_gameManager.CorrectTileCount:00} / {_gameManager.TilesList.Count:00}";
        if (_status != null) _status.text = _gameManager.State == GameState.Drawing ? "Release to paint" :
            _gameManager.State == GameState.Resolving ? "Checking colors..." : "Trace from a colored dot";
    }
    private void ApplyPaletteVisuals()
    {
        if (_paletteApplied || !_gameManager.IsInitialized || _gameManager.PaletteVisuals == null) return;
        _paletteApplied = true;
        // The red/blue skin is already correct; keep its original UI material.
        if (_gameManager.Palette.Mixed == TileColor.Purple) return;
        _paletteMaterial = _gameManager.PaletteVisuals.CreateUiMaterial(_gameManager.Palette);
        if (_paletteMaterial == null) return;
        _redReload.GetComponent<UnityEngine.UI.Image>().material = _paletteMaterial;
        _blueReload.GetComponent<UnityEngine.UI.Image>().material = _paletteMaterial;
        if (_mixLegend != null) _mixLegend.material = _paletteMaterial;
    }
    public async UniTask PlayClearAsync(CancellationToken token)
    {
        if (_clearPresentation != null)
        {
            UniTask presentation = _clearPresentation.PlayAsync(token);
            Refresh();
            await presentation;
        }
        else { _clearPanel.gameObject.SetActive(true); _clearPanel.alpha = 1; }
    }
    private void OnDestroy()
    {
        SceneNavigator.LoadingChanged -= Refresh;
        if (_gameManager != null) _gameManager.Changed -= Refresh;
        if (_redReload != null) _redReload.onClick.RemoveListener(ReloadFirst);
        if (_blueReload != null) _blueReload.onClick.RemoveListener(ReloadSecond);
        if (_allReload != null) _allReload.onClick.RemoveListener(ReloadAll);
        if (_home != null) _home.onClick.RemoveListener(ReturnToTitle);
        if (_nextStage != null) _nextStage.onClick.RemoveListener(GoToNextStage);
        if (_paletteMaterial != null) Destroy(_paletteMaterial);
    }
}
