using Cysharp.Threading.Tasks;
using UnityEngine;

public class MenuNavigation : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button _continueButton;
    [SerializeField] private string _nextScene = SceneNavigator.StageScene;

    private void OnEnable()
    {
        if (_continueButton != null) _continueButton.onClick.AddListener(Continue);
    }
    private void OnDisable()
    {
        if (_continueButton != null) _continueButton.onClick.RemoveListener(Continue);
    }
    private void Continue() => ContinueAsync().Forget();
    private async UniTask ContinueAsync()
    {
        if (_continueButton == null || !_continueButton.interactable || SceneNavigator.IsLoading) return;
        _continueButton.interactable = false;
        try { await SceneNavigator.LoadAsync(_nextScene); }
        finally { if (_continueButton != null) _continueButton.interactable = true; }
    }
}
