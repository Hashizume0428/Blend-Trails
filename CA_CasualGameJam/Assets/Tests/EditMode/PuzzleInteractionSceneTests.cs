using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PuzzleInteractionSceneTests
{
    private const string SoundPath = "Assets/Se/クイズ不正解1_最初のブ.mp3";

    [Test]
    public void FailureClipIsShortAndPreloadedForImmediatePlayback()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundPath);
        Assert.That(clip, Is.Not.Null);
        Assert.That(clip.length, Is.InRange(0.09f, 0.16f));
        var settings = ((AudioImporter)AssetImporter.GetAtPath(SoundPath)).defaultSampleSettings;
        Assert.That(settings.preloadAudioData, Is.True);
        Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
        Assert.That(settings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
    }

    [Test]
    public void AllSixtyStagesHaveOneCenteredClearActionAndTheFailureClipAndEndpointChooser()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundPath);
        for (int number = 1; number <= StageCatalog.Count; number++)
        {
            var scene = EditorSceneManager.OpenPreviewScene(StageCatalog.ScenePath(number));
            try
            {
                var roots = scene.GetRootGameObjects();
                var game = roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).Single();
                var hud = roots.SelectMany(root => root.GetComponentsInChildren<GameHud>(true)).Single();
                var input = game.GetComponent<InputTouchManager>();
                Assert.That(input, Is.Not.Null, scene.path);
                var selector = hud.GetComponentInChildren<LineResumeSelector>(true);
                Assert.That(selector, Is.Not.Null, scene.path);
                Assert.That(new SerializedObject(game).FindProperty("_incorrectSound").objectReferenceValue, Is.EqualTo(clip), scene.path);
                Assert.That(new SerializedObject(input).FindProperty("_resumeSelector").objectReferenceValue, Is.EqualTo(selector), scene.path);
                var actions = hud.transform.Find("ClearPanel/Card/ClearActions");
                Assert.That(actions.childCount, Is.EqualTo(1), scene.path);
                Assert.That(actions.GetChild(0).name, Is.EqualTo("NextStageButton"));
                Assert.That(((RectTransform)actions.GetChild(0)).anchoredPosition.x, Is.Zero);
                Assert.That(hud.GetComponentsInChildren<Transform>(true).Any(item => item.name == "ClearHomeButton"), Is.False);
                var next = actions.GetChild(0).GetComponent<UnityEngine.UI.Button>();
                var motion = new SerializedObject(hud.ClearPresentation);
                foreach (string field in new[]{"_badge","_cardBody","_details","_introScrim","_backgroundScrim","_boardVisuals","_titleAnimator","_celebration","_boardSheen","_buttonSheen","_foregroundConfetti"})
                    Assert.That(motion.FindProperty(field).objectReferenceValue,Is.Not.Null,scene.path+" "+field);
                foreach(var effect in hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Where(item=>
                    item is ClearConfetti || item is ClearCelebrationGraphic || item is ClearSheenGraphic))
                    Assert.That(effect.raycastTarget,Is.False,scene.path+" "+effect.name);
                Assert.That(next.GetComponent<UnityEngine.UI.Image>().sprite, Is.EqualTo(ToonSkinAssets.Current.PrimaryButton), scene.path);
                Assert.That(next.transform.Find("Label").GetComponent<TMPro.TMP_Text>().font, Is.EqualTo(ToonSkinAssets.Current.Font), scene.path);
                var arrow = next.transform.Find("ToonNextIcon") as RectTransform;
                var nextRect = (RectTransform)next.transform;
                // Hidden clear actions can have zero world scale; inspect their authored anchors.
                Assert.That(arrow.anchorMin, Is.EqualTo(Vector2.up), scene.path);
                Assert.That(arrow.anchoredPosition.x, Is.InRange(arrow.pivot.x * arrow.sizeDelta.x,
                    nextRect.sizeDelta.x - (1 - arrow.pivot.x) * arrow.sizeDelta.x), scene.path);
                Assert.That(-arrow.anchoredPosition.y, Is.InRange((1 - arrow.pivot.y) * arrow.sizeDelta.y,
                    nextRect.sizeDelta.y - arrow.pivot.y * arrow.sizeDelta.y), scene.path);
                var backing = game.TilesList[0].transform.parent.Find("BoardBackground").GetComponent<SpriteRenderer>();
                float backingWidth = backing.bounds.size.x / backing.transform.parent.lossyScale.x;
                Assert.That(backingWidth, Is.InRange(game.BoardSize.x + .3f, game.BoardSize.x + 1.5f), scene.path);
                foreach (TileController tile in game.TilesList.Where(item => item.IsStartTile))
                {
                    var marker = tile.transform.Find("StartMarker").GetComponent<SpriteRenderer>();
                    var face = tile.transform.Find("TileFace").GetComponent<SpriteRenderer>();
                    float markerRatio = marker.sprite.bounds.size.x * marker.transform.localScale.x /
                        (face.sprite.bounds.size.x * face.transform.localScale.x);
                    Assert.That(markerRatio, Is.InRange(.5f, .8f), scene.path + " start dot must remain visible");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
