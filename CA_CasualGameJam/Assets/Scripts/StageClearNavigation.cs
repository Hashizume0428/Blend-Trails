using UnityEngine;

/// <summary>A scene identifies a puzzle; a play session supplies its displayed Stage number.</summary>
[DefaultExecutionOrder(-100)]
public class StageClearNavigation : MonoBehaviour
{
    [SerializeField, Min(1)] private int _stageNumber = 1;
    [SerializeField] private string _nextStageScene = SceneNavigator.SecondStageScene;

    private int _displayStageNumber;
    public int PuzzleId => _stageNumber;
    public int StageNumber => Application.isPlaying ? _displayStageNumber : _stageNumber;
    public string NextStageScene => Application.isPlaying ? StageProgress.CurrentScene : _nextStageScene;
    public bool HasNextStage => !string.IsNullOrEmpty(NextStageScene);

    private void Awake()
    {
        StageProgress.EnterStage(_stageNumber);
        _displayStageNumber = StageProgress.CurrentStageNumber;
    }
}
