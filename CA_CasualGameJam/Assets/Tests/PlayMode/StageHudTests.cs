using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class StageHudTests
{
    private GameManager _game;
    private static UnityEngine.UI.Button Button(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
    private TileController Tile(int x, int y) => _game.GetTile(new Vector2Int(x, y));
    private static Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    private static List<RaycastResult> Hits(Vector2 position)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
        return hits;
    }
    private static void Tap(UnityEngine.UI.Button button)
    {
        Assert.That(button.interactable, Is.True, button.name);
        var pointer = new PointerEventData(EventSystem.current) { position = Center((RectTransform)button.transform), button = PointerEventData.InputButton.Left };
        Assert.That(Hits(pointer.position)[0].gameObject, Is.EqualTo(button.gameObject));
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    [UnitySetUp]
    public IEnumerator LoadStage()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.StageScene);
        yield return null;
        Canvas.ForceUpdateCanvases();
        _game = Object.FindFirstObjectByType<GameManager>();
        Assert.That(_game.IsInitialized, Is.True);
    }
    private IEnumerator PaintBothStarts()
    {
        Assert.That(_game.TryBeginLine(Tile(0, 0).TileIndex), Is.True);
        Assert.That(_game.TryExtendLine(Tile(0, 3).TileIndex), Is.True);
        yield return _game.CommitLineAsync().ToCoroutine();
        Assert.That(_game.TryBeginLine(Tile(0, 3).TileIndex), Is.True);
        yield return _game.CommitLineAsync().ToCoroutine();
    }

    [UnityTest]
    public IEnumerator StageSkinFitsAndDecorationsDoNotBlockTileInput()
    {
        Assert.That(GameObject.Find("Level").GetComponent<TMP_Text>().text, Is.EqualTo("-Stage1-"));
        Assert.That(GameObject.Find("MixPurple"), Is.Not.Null);
        Assert.That(GameObject.Find("ReloadYellow"), Is.Null);
        foreach (string old in new[] { "Title", "Progress", "Legend", "Status", "Instructions" })
            Assert.That(GameObject.Find(old), Is.Null);
        Assert.That(Button("ReloadRed").interactable, Is.False);
        Assert.That(Button("ReloadBlue").interactable, Is.False);
        foreach (string name in new[] { "HomeButton", "ReloadAll", "ReloadBlue", "ReloadRed" })
        {
            var button = Button(name);
            Vector2 center = Center((RectTransform)button.transform);
            Assert.That(Screen.safeArea.Contains(center), Is.True, name);
            Assert.That(Hits(center)[0].gameObject, Is.EqualTo(button.gameObject));
        }
        foreach (TileController tile in _game.TilesList)
        {
            Vector2 point = Camera.main.WorldToScreenPoint(tile.transform.position);
            Assert.That(Screen.safeArea.Contains(point), Is.True, tile.name);
            Assert.That(Hits(point), Is.Empty, "Decorations must not intercept board taps.");
            Assert.That(Physics2D.OverlapPoint(tile.transform.position).GetComponent<TileController>(), Is.EqualTo(tile));
        }
        var input = Object.FindFirstObjectByType<InputTouchManager>();
        Assert.That(input.BeginPointer(Center((RectTransform)Button("HomeButton").transform), -1), Is.False);
        Assert.That(input.BeginPointer(Camera.main.WorldToScreenPoint(Tile(0, 0).transform.position), -1), Is.True);
        input.MovePointer(Camera.main.WorldToScreenPoint(Tile(3, 0).transform.position));
        Assert.That(_game.PreviewPath.Count, Is.EqualTo(4));
        input.CancelPointer();
        yield return null;
    }

    [UnityTest]
    public IEnumerator AllReloadClearsLinesAndColorsAndPreservesPuzzleConditions()
    {
        Sprite redFrame = Tile(3, 0).transform.Find("TileFlame").GetComponent<SpriteRenderer>().sprite;
        yield return PaintBothStarts();
        Assert.That(Tile(0, 3).CurrentColor, Is.EqualTo(TileColor.Purple));
        Tap(Button("ReloadAll"));
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNavigator.StageScene));
        foreach (TileController tile in _game.TilesList)
        {
            Assert.That(tile.CurrentColor, Is.EqualTo(TileColor.None));
            Assert.That(tile.IsErrorBlinking, Is.False);
        }
        Assert.That(_game.LineVisuals.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.Zero);
        Assert.That(_game.HasCommittedLine(BaseColor.Red), Is.False);
        Assert.That(_game.HasCommittedLine(BaseColor.Blue), Is.False);
        Assert.That(Tile(3, 0).transform.Find("TileFlame").GetComponent<SpriteRenderer>().sprite, Is.EqualTo(redFrame));
        Tile(1, 3).AddColor(BaseColor.Red);
        Assert.That(Tile(1, 3).IsTileCorrect(), Is.False, "The red prohibition must remain after ALL.");
        Assert.That(_game.TryBeginLine(Tile(0, 0).TileIndex), Is.True);
        _game.CancelLine();
    }

    [UnityTest]
    public IEnumerator ReloadControlsAreLockedDuringDrawingAndSequentialPainting()
    {
        yield return PaintBothStarts();
        Tap(Button("ReloadBlue"));
        Assert.That(Tile(0, 3).CurrentColor, Is.EqualTo(TileColor.Red));
        Assert.That(_game.TryBeginLine(Tile(0, 3).TileIndex), Is.True);
        Assert.That(Button("ReloadAll").interactable, Is.False);
        Assert.That(Button("ReloadRed").interactable, Is.False);
        Assert.That(Button("HomeButton").interactable, Is.True);
        Button("ReloadAll").onClick.Invoke();
        Assert.That(_game.State, Is.EqualTo(GameState.Drawing));
        UniTask commit = _game.CommitLineAsync();
        Assert.That(_game.State, Is.EqualTo(GameState.Resolving));
        Button("ReloadAll").onClick.Invoke();
        Assert.That(_game.State, Is.EqualTo(GameState.Resolving));
        yield return commit.ToCoroutine();
        Assert.That(Button("ReloadAll").interactable, Is.True);
        Assert.That(Button("ReloadBlue").interactable, Is.True);
        Tap(Button("ReloadRed"));
        Assert.That(Tile(0, 3).CurrentColor, Is.EqualTo(TileColor.Blue));
    }

    private static IEnumerator WaitForTitle()
    {
        float deadline = Time.realtimeSinceStartup + 10;
        while (SceneManager.GetActiveScene().name != SceneNavigator.TitleScene || SceneNavigator.IsLoading)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        yield return null;
        Assert.That(Object.FindFirstObjectByType<GameManager>(), Is.Null);
    }
    [UnityTest]
    public IEnumerator HomeReturnsToTitleFromIdle()
    {
        Tap(Button("HomeButton"));
        yield return WaitForTitle();
    }
    [UnityTest]
    public IEnumerator HomeCancelsDrawingAndReturnsToTitle()
    {
        Assert.That(_game.TryBeginLine(Tile(0, 0).TileIndex), Is.True);
        Assert.That(_game.TryExtendLine(Tile(3, 0).TileIndex), Is.True);
        Tap(Button("HomeButton"));
        yield return WaitForTitle();
    }
    [UnityTest]
    public IEnumerator HomeDuringPaintingCancelsThePendingOperation()
    {
        Assert.That(_game.TryBeginLine(Tile(0, 0).TileIndex), Is.True);
        Assert.That(_game.TryExtendLine(Tile(3, 0).TileIndex), Is.True);
        UniTask commit = _game.CommitLineAsync();
        Assert.That(_game.State, Is.EqualTo(GameState.Resolving));
        Tap(Button("HomeButton"));
        yield return WaitForTitle();
        yield return commit.ToCoroutine();
        yield return new WaitForSecondsRealtime(0.2f);
    }
}
