using System.Collections.Generic;

public enum StageKind { Tutorial, Normal, Extra }

/// <summary>Authored puzzle identities. Displayed stage numbers continue beyond this catalog.</summary>
public static class StageCatalog
{
    public const int Count = 30;
    public const int TutorialCount = 4;
    public const int FirstNormalPuzzle = TutorialCount + 1;
    public const int RecentPuzzleLimit = 8;
    public static StageKind Kind(int stageNumber) => stageNumber <= TutorialCount ? StageKind.Tutorial :
        stageNumber <= Count ? StageKind.Normal : StageKind.Extra;
    public static bool IsNormalPuzzle(int puzzleId) => puzzleId >= FirstNormalPuzzle && puzzleId <= Count;
    public static List<int> ExtraCandidates(IEnumerable<int> recentPuzzleIds)
    {
        var excluded = new HashSet<int>(recentPuzzleIds);
        var candidates = new List<int>();
        for (int id = FirstNormalPuzzle; id <= Count; id++)
            if (!excluded.Contains(id)) candidates.Add(id);
        return candidates;
    }
    public static string SceneName(int number) => number < 1 || number > Count ? string.Empty :
        number == 1 ? SceneNavigator.StageScene : $"Stage{number:00}";
    public static string ScenePath(int number) => $"Assets/Scenes/{SceneName(number)}.unity";
}
