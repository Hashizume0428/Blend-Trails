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
    public void AllThirtyStagesHaveOneCenteredClearActionAndTheFailureClipAndEndpointChooser()
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
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
