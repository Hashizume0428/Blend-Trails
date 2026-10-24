using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class StagePaletteFlowTests : StageProgressTestFixture
{
    private static GameManager Game => Object.FindFirstObjectByType<GameManager>();
    private static UnityEngine.UI.Button Button(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();

    private static void SaveStage(int displayNumber, int puzzleId)
    {
        PlayerPrefs.SetString(StateKey, $"{{\"stageNumber\":{displayNumber},\"puzzleId\":{puzzleId},\"recentPuzzleIds\":[]}}");
        PlayerPrefs.Save();
        StageProgress.ResetSession();
    }

    private static void AssertPalette(BaseColor first, BaseColor second, TileColor mixed)
    {
        Assert.That(Game.IsInitialized, Is.True);
        Assert.That(Game.Palette.First, Is.EqualTo(first));
        Assert.That(Game.Palette.Second, Is.EqualTo(second));
        Assert.That(Game.Palette.Mixed, Is.EqualTo(mixed));
        Assert.That(Game.PaletteVisuals, Is.Not.Null);
        foreach (TileController tile in Game.TilesList.Where(tile => tile.IsStartTile))
            Assert.That(tile.transform.Find("StartMarker").GetComponent<SpriteRenderer>().sprite,
                Is.EqualTo(Game.PaletteVisuals.StartMarker(tile.StartColor)));
        var toon = Object.FindFirstObjectByType<ToonPaletteHud>();
        if (toon != null)
        {
            Assert.That(toon.FirstButton.sprite, Is.EqualTo(ToonSkinAssets.Current.ColorButtons[(int)first]));
            Assert.That(toon.SecondButton.sprite, Is.EqualTo(ToonSkinAssets.Current.ColorButtons[(int)second]));
            Assert.That(toon.FirstSwatch.color, Is.EqualTo(StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(first))));
            Assert.That(toon.SecondSwatch.color, Is.EqualTo(StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(second))));
            Assert.That(toon.MixedSwatch.color, Is.EqualTo(StagePaletteVisuals.UiColor(mixed)));
            Assert.That(toon.SecondIcon.color, Is.EqualTo(second == BaseColor.Yellow ? ToonSkinAssets.Ink : Color.white));
        }
        else if (mixed != TileColor.Purple)
        {
            var legend = GameObject.Find("MixPurple").GetComponent<UnityEngine.UI.Image>();
            Assert.That(legend.material.shader.name, Is.EqualTo("Color Puzzle/Stage Palette UI"));
            Assert.That(legend.material.GetVector("_FirstColor"), Is.EqualTo((Vector4)StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(first))));
            Assert.That(legend.material.GetVector("_SecondColor"), Is.EqualTo((Vector4)StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(second))));
            Assert.That(legend.material.GetVector("_MixedColor"), Is.EqualTo((Vector4)StagePaletteVisuals.UiColor(mixed)));
            Assert.That(Button("ReloadRed").GetComponent<UnityEngine.UI.Image>().material, Is.SameAs(legend.material));
            Assert.That(Button("ReloadBlue").GetComponent<UnityEngine.UI.Image>().material, Is.SameAs(legend.material));
        }
    }

    private static void Tap(UnityEngine.UI.Button button)
    {
        Assert.That(button.interactable, Is.True);
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject));
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    [UnityTest]
    public IEnumerator ActiveColorsDriveBothReloadButtonsAndMixRemoval()
    {
        foreach (int stage in new[] { 6, 11 })
        {
            yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(stage));
            yield return null;
            BaseColor first = stage == 6 ? BaseColor.Red : BaseColor.Blue;
            TileColor mixed = stage == 6 ? TileColor.Orange : TileColor.Green;
            AssertPalette(first, BaseColor.Yellow, mixed);
            TileController firstStart = Game.TilesList.Single(tile => tile.IsStartTile && tile.StartColor == first);
            TileController yellowStart = Game.TilesList.Single(tile => tile.IsStartTile && tile.StartColor == BaseColor.Yellow);
            Assert.That(Game.TryBeginLine(firstStart.TileIndex), Is.True);
            yield return Game.CommitLineAsync().ToCoroutine();
            Assert.That(yellowStart.AddColor(first), Is.True);
            Assert.That(Game.TryBeginLine(yellowStart.TileIndex), Is.True);
            yield return Game.CommitLineAsync().ToCoroutine();
            Assert.That(yellowStart.CurrentColor, Is.EqualTo(mixed));
            Tap(Button("ReloadBlue"));
            Assert.That(yellowStart.CurrentColor, Is.EqualTo(PuzzleRules.ToTileColor(first)));
            Assert.That(Game.HasCommittedLine(first), Is.True);
            Assert.That(Game.HasCommittedLine(BaseColor.Yellow), Is.False);
            Tap(Button("ReloadRed"));
            Assert.That(yellowStart.CurrentColor, Is.EqualTo(TileColor.None));
            Tap(Button("ReloadAll"));
            AssertPalette(first, BaseColor.Yellow, mixed);
        }
    }

    [UnityTest]
    public IEnumerator YellowPreviewMixesLinesAndCancelRestoresTheFirstLine()
    {
        foreach (int stage in new[] { 6, 11 })
        {
            yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(stage));
            yield return null;
            StageTestReference.DrawLine(Game, BaseColor.Red);
            yield return Game.CommitLineAsync().ToCoroutine();
            TileColor[] before = Game.TilesList.Select(tile => tile.CurrentColor).ToArray();
            StageTestReference.DrawLine(Game, BaseColor.Blue);
            TileController overlap = Game.TilesList.First(tile => tile.CurrentColor == Game.Palette.Mixed);
            Assert.That(Game.LineVisuals.GetNodeColor(overlap.TileIndex), Is.EqualTo(Game.Palette.Mixed));
            Game.CancelLine();
            CollectionAssert.AreEqual(before, Game.TilesList.Select(tile => tile.CurrentColor));
            Assert.That(Game.LineVisuals.GetNodeColor(overlap.TileIndex), Is.EqualTo(PuzzleRules.ToTileColor(Game.Palette.First)));
            StageTestReference.DrawLine(Game, BaseColor.Blue);
            yield return Game.CommitLineAsync().ToCoroutine();
            Assert.That(Game.State, Is.EqualTo(GameState.Cleared));
        }
    }

    [UnityTest]
    public IEnumerator ExtraPaletteUsesDisplayNumberAndSurvivesHomeAndSessionRestart()
    {
        foreach (int stage in new[] { 31, 36, 41 })
        {
            // Always reuse the same green-group authored puzzle to distinguish ID from display number.
            SaveStage(stage, 11);
            yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(11));
            yield return null;
            BaseColor first = stage == 41 ? BaseColor.Blue : BaseColor.Red;
            BaseColor second = stage == 31 ? BaseColor.Blue : BaseColor.Yellow;
            TileColor mixed = stage == 31 ? TileColor.Purple : stage == 36 ? TileColor.Orange : TileColor.Green;
            AssertPalette(first, second, mixed);
            StageTestReference.AssertLayout(Game, 11);
            Assert.That(Game.GetComponent<StageClearNavigation>().StageNumber, Is.EqualTo(stage));
            yield return SceneManager.LoadSceneAsync(SceneNavigator.TitleScene);
            yield return null;
            StageProgress.ResetSession();
            Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(stage));
            Assert.That(StageProgress.CurrentPuzzleId, Is.EqualTo(11));
            yield return SceneManager.LoadSceneAsync(StageProgress.CurrentScene);
            yield return null;
            AssertPalette(first, second, mixed);
            Assert.That(Game.CorrectTileCount, Is.Zero);
        }
    }

    [UnityTest]
    public IEnumerator BoundaryClearKeepsItsPaletteUntilTheNextSceneEnters()
    {
        foreach (int stage in new[] { 5, 10, 15, 30, 35, 40 })
        {
            int puzzle = stage <= 30 ? stage : 11;
            SaveStage(stage, puzzle);
            yield return SceneManager.LoadSceneAsync(StageCatalog.SceneName(puzzle));
            yield return null;
            GameManager game = Game;
            TileColor mixed = stage == 5 || stage == 35 ? TileColor.Purple :
                stage == 10 || stage == 40 ? TileColor.Orange : TileColor.Green;
            TileColor nextMixed = mixed == TileColor.Purple ? TileColor.Orange :
                mixed == TileColor.Orange ? TileColor.Green : TileColor.Purple;
            TileColor[] requirements = game.TilesList.Select(tile => tile.RequiredColor).ToArray();
            StageTestReference.DrawLine(game, BaseColor.Red);
            yield return game.CommitLineAsync().ToCoroutine();
            StageTestReference.DrawLine(game, BaseColor.Blue);
            UniTask clear = game.CommitLineAsync();
            float deadline = Time.realtimeSinceStartup + 5;
            while (StageProgress.CurrentStageNumber == stage)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                yield return null;
            }
            Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(stage + 1));
            Assert.That(game.Palette.Mixed, Is.EqualTo(mixed), "Saving the next stage must not recolor the clear presentation.");
            Assert.That(game.GetComponent<StageClearNavigation>().StageNumber, Is.EqualTo(stage));
            CollectionAssert.AreEqual(requirements, game.TilesList.Select(tile => tile.RequiredColor));
            yield return clear.ToCoroutine();
            Assert.That(game.Initialize(), Is.True);
            Assert.That(game.Palette.Mixed, Is.EqualTo(mixed), "Reinitialization must keep the entered stage's palette.");
            CollectionAssert.AreEqual(requirements, game.TilesList.Select(tile => tile.RequiredColor));
            yield return SceneManager.LoadSceneAsync(StageProgress.CurrentScene);
            yield return null;
            Assert.That(Game.Palette.Mixed, Is.EqualTo(nextMixed));
            Assert.That(GameObject.Find("Level").GetComponent<TMP_Text>().text, Is.EqualTo($"-Stage{stage + 1}-"));
        }
    }
}
