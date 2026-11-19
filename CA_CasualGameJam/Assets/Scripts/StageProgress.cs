using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent display number, selected puzzle and the last eight completed normal puzzles.</summary>
public static class StageProgress
{
    private const string ProgressKey = "ColorPuzzle.CurrentStageNumber";
    private const string StateKey = "ColorPuzzle.StageProgress.v2";
    [Serializable]
    private sealed class SavedState
    {
        public int stageNumber;
        public int puzzleId;
        public int[] recentPuzzleIds;
    }
    private static readonly List<int> Recent = new List<int>();
    private static readonly IReadOnlyList<int> RecentView = Recent.AsReadOnly();
    public static int CurrentStageNumber { get; private set; } = 1;
    public static int ClearedStageCount => CurrentStageNumber - 1;
    public static int CurrentPuzzleId { get; private set; } = 1;
    public static IReadOnlyList<int> RecentPuzzleIds => RecentView;
    public static StageKind CurrentKind => StageCatalog.Kind(CurrentStageNumber);
    public static string CurrentScene => StageCatalog.SceneName(CurrentPuzzleId);
    public static bool CanPlayCurrentStage => !string.IsNullOrEmpty(CurrentScene);
    public static event Action Changed;
    public static event Action<int> Cleared;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetSession()
    {
        Changed = null;
        Cleared = null;
        Recent.Clear();
        SavedState saved = null;
        if (PlayerPrefs.HasKey(StateKey))
        {
            try { saved = JsonUtility.FromJson<SavedState>(PlayerPrefs.GetString(StateKey)); }
            catch (ArgumentException) { }
        }
        if (IsValid(saved))
        {
            CurrentStageNumber = saved.stageNumber;
            CurrentPuzzleId = saved.puzzleId;
            Recent.AddRange(saved.recentPuzzleIds);
            return;
        }
        // An old extra can now be an authored puzzle after the catalog expands.
        // Prefer the v2 display number even if its legacy integer key is absent or stale.
        bool expandedStage = HasValidHistory(saved) && saved.stageNumber <= StageCatalog.Count &&
            StageCatalog.IsNormalPuzzle(saved.puzzleId) && saved.puzzleId < saved.stageNumber &&
            Array.IndexOf(saved.recentPuzzleIds, saved.puzzleId) < 0;
        CurrentStageNumber = expandedStage ? saved.stageNumber : Mathf.Max(1, PlayerPrefs.GetInt(ProgressKey, 1));
        SeedHistory(CurrentStageNumber);
        SelectPuzzle();
        Save();
    }

    private static bool HasValidHistory(SavedState saved)
    {
        if (saved == null || saved.stageNumber < 1 || saved.recentPuzzleIds == null ||
            saved.recentPuzzleIds.Length > StageCatalog.RecentPuzzleLimit) return false;
        var unique = new HashSet<int>();
        foreach (int id in saved.recentPuzzleIds)
            if (!StageCatalog.IsNormalPuzzle(id) || !unique.Add(id)) return false;
        return true;
    }

    private static bool IsValid(SavedState saved)
    {
        if (!HasValidHistory(saved)) return false;
        return saved.stageNumber <= StageCatalog.Count ? saved.puzzleId == saved.stageNumber :
            StageCatalog.IsNormalPuzzle(saved.puzzleId) && Array.IndexOf(saved.recentPuzzleIds, saved.puzzleId) < 0;
    }

    public static void RecordClear(int stageNumber)
    {
        if (stageNumber != CurrentStageNumber || stageNumber == int.MaxValue) return;
        if (StageCatalog.IsNormalPuzzle(CurrentPuzzleId))
        {
            Recent.Add(CurrentPuzzleId);
            if (Recent.Count > StageCatalog.RecentPuzzleLimit) Recent.RemoveAt(0);
        }
        CurrentStageNumber++;
        SelectPuzzle();
        Save();
        Cleared?.Invoke(ClearedStageCount);
        Changed?.Invoke();
    }

    // Direct Editor scene launches adopt that puzzle's number. Loading CurrentScene for Stage61+
    // keeps the increasing display number even though an authored scene is being reused.
    public static void EnterStage(int puzzleId)
    {
        if (string.IsNullOrEmpty(StageCatalog.SceneName(puzzleId)) || puzzleId == CurrentPuzzleId) return;
        CurrentStageNumber = CurrentPuzzleId = puzzleId;
        SeedHistory(CurrentStageNumber);
        Save();
        Changed?.Invoke();
    }

    private static void SeedHistory(int stageNumber)
    {
        Recent.Clear();
        int last = Math.Min(stageNumber - 1, StageCatalog.Count);
        for (int id = Math.Max(StageCatalog.FirstNormalPuzzle, last - StageCatalog.RecentPuzzleLimit + 1); id <= last; id++)
            Recent.Add(id);
    }

    private static void SelectPuzzle()
    {
        if (CurrentStageNumber <= StageCatalog.Count) CurrentPuzzleId = CurrentStageNumber;
        else
        {
            List<int> candidates = StageCatalog.ExtraCandidates(Recent);
            CurrentPuzzleId = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
    }

    private static void Save()
    {
        var state = new SavedState { stageNumber = CurrentStageNumber, puzzleId = CurrentPuzzleId, recentPuzzleIds = Recent.ToArray() };
        PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(state));
        PlayerPrefs.SetInt(ProgressKey, CurrentStageNumber);
        // Flush before an application can be task-killed during the clear presentation.
        PlayerPrefs.Save();
    }
}
