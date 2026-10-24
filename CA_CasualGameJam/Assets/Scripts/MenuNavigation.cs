using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class MenuNavigation : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button _continueButton;
    [SerializeField] private string _nextScene = SceneNavigator.StageScene;
    [SerializeField] private bool _useStageProgress;
    [SerializeField] private TMP_Text _stageLabel;
    [SerializeField] private TMP_Text _stageDescription;
    private bool _opening;

    private void OnEnable()
    {
        if (_continueButton != null) _continueButton.onClick.AddListener(Continue);
        StageProgress.Changed += Refresh;
        SceneNavigator.LoadingChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (_continueButton != null) _continueButton.onClick.RemoveListener(Continue);
        StageProgress.Changed -= Refresh;
        SceneNavigator.LoadingChanged -= Refresh;
    }
    private void Refresh()
    {
        if (_useStageProgress)
        {
            if (_stageLabel != null) _stageLabel.text = $"Stage{StageProgress.CurrentStageNumber}";
            if (_stageDescription != null) _stageDescription.text = StageProgress.CanPlayCurrentStage ? "STAGE SELECT" : "COMING SOON";
        }
        if (_continueButton != null)
            _continueButton.interactable = !_opening && !SceneNavigator.IsLoading && (!_useStageProgress || StageProgress.CanPlayCurrentStage);
    }
    private void Continue() => ContinueAsync().Forget();
    private async UniTask ContinueAsync()
    {
        if (_continueButton == null || !_continueButton.interactable || SceneNavigator.IsLoading) return;
        _opening = true;
        Refresh();
        try { await SceneNavigator.LoadAsync(_useStageProgress ? StageProgress.CurrentScene : _nextScene); }
        finally { if (this != null) { _opening = false; Refresh(); } }
    }
}
