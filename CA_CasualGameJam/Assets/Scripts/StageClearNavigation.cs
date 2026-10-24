using UnityEngine;

/// <summary>Per-stage destinations. The clear screen waits for an explicit button choice.</summary>
public class StageClearNavigation : MonoBehaviour
{
    [SerializeField, Min(1)] private int _stageNumber = 1;
    [SerializeField] private string _nextStageScene = SceneNavigator.SecondStageScene;

    public int StageNumber => _stageNumber;
    public string NextStageScene => _nextStageScene;
    public bool HasNextStage => !string.IsNullOrEmpty(_nextStageScene);

    private void Awake() => StageProgress.EnterStage(_stageNumber);
}
