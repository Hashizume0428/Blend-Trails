/// <summary>Playable scenes, in the order of the ten supplied stage references.</summary>
public static class StageCatalog
{
    public const int Count = 10;
    public static string SceneName(int number) => number < 1 || number > Count ? string.Empty :
        number == 1 ? SceneNavigator.StageScene : $"Stage{number:00}";
    public static string ScenePath(int number) => $"Assets/Scenes/{SceneName(number)}.unity";
}
