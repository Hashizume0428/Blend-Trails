using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TitlePresentationTests : StageProgressTestFixture
{
    private static UnityEngine.UI.Button StartButton => GameObject.Find("Stage01Button").GetComponent<UnityEngine.UI.Button>();
    private static TitlePresentation Presentation => Object.FindFirstObjectByType<TitlePresentation>();
    private static IEnumerator OpenTitle()
    {
        yield return SceneManager.LoadSceneAsync(SceneNavigator.TitleScene);
        yield return null;
        Assert.That(Presentation, Is.Not.Null);
    }
    [UnityTest]
    public IEnumerator SceneLoadingDelayDoesNotConsumeTheLogoEntrance()
    {
        // Reproduce texture/shader initialization before the first displayed title frame.
        void DelayFirstTitleFrame(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneNavigator.TitleScene) System.Threading.Thread.Sleep(1100);
        }
        SceneManager.sceneLoaded += DelayFirstTitleFrame;
        try
        {
            yield return OpenTitle();
            var logo = GameObject.Find("ToonTitleLogo").GetComponent<RectTransform>();
            var opacity = logo.GetComponent<CanvasGroup>();
            Assert.That(Presentation.ElapsedSeconds, Is.LessThan(.08f), "Loading time must not skip the logo entrance.");
            Assert.That(opacity.alpha, Is.LessThan(.01f), "The first displayed frame starts with a hidden logo.");
            Assert.That(logo.localScale.x, Is.LessThan(.8f));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(opacity.alpha, Is.GreaterThan(.2f), "The logo must visibly fade and grow after the first frame.");
            Assert.That(logo.localScale.x, Is.GreaterThan(.8f));
            Assert.That(StartButton.interactable, Is.False);
            yield return WaitForTitleReady();
        }
        finally { SceneManager.sceneLoaded -= DelayFirstTitleFrame; }
    }
    [UnityTest]
    public IEnumerator EntranceBlocksEarlyClicksAndFinishesWithTimeScaleZero()
    {
        yield return OpenTitle();
        var start = StartButton;
        Assert.That(Presentation.IsReady, Is.False);
        Assert.That(start.interactable, Is.False);
        Assert.That(start.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
        Assert.That(start.GetComponent<CanvasGroup>().alpha, Is.LessThan(.01f));
        // The menu can refresh during the entrance; it must keep the readiness gate.
        StageProgress.ResetSession();
        Assert.That(start.interactable, Is.False);
        start.onClick.Invoke();
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNavigator.TitleScene));
        float previousScale = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            yield return WaitForTitleReady();
            Assert.That(Presentation.ElapsedSeconds, Is.GreaterThanOrEqualTo(TitlePresentation.ReadySeconds));
            Assert.That(Presentation.ElapsedSeconds, Is.LessThan(3.5f));
            Assert.That(start.interactable, Is.True);
            Assert.That(start.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            Assert.That(start.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1).Within(.001f));
        }
        finally { Time.timeScale = previousScale; }
    }
    [UnityTest]
    public IEnumerator ReadyButtonReceivesRealRaycastsAndOpensSavedStageOnlyOnce()
    {
        LoadLegacyProgress(3);
        yield return OpenTitle();yield return WaitForTitleReady();
        var start = StartButton;
        Assert.That(start.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage3"));
        Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Canvas.ForceUpdateCanvases();
        var rect = start.GetComponent<RectTransform>();
        var pointer = new PointerEventData(EventSystem.current) {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits, Is.Not.Empty);
        Assert.That(hits[0].gameObject, Is.EqualTo(start.gameObject), "Title effects must not intercept the start button.");
        ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        start.onClick.Invoke();
        float deadline = Time.realtimeSinceStartup + 10;
        while (SceneManager.GetActiveScene().name != StageCatalog.SceneName(3) || SceneNavigator.IsLoading)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));yield return null;
        }
        yield return null;
        Assert.That(StageProgress.CurrentStageNumber, Is.EqualTo(3));
        Assert.That(Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        GameObject.Find("HomeButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        while (SceneManager.GetActiveScene().name != SceneNavigator.TitleScene || SceneNavigator.IsLoading)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));yield return null;
        }
        yield return null;
        Assert.That(Presentation.IsReady, Is.False, "Re-entering the title must replay its entrance.");
        Assert.That(StartButton.interactable, Is.False);
        yield return WaitForTitleReady();
        Assert.That(StartButton.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("Stage3"));
    }
    [UnityTest]
    public IEnumerator DisablingRestoresTheLayoutAndReenablingDoesNotAccumulateOffsets()
    {
        yield return OpenTitle();
        var presentation = Presentation;
        var logo = GameObject.Find("ToonTitleLogo").GetComponent<RectTransform>();
        var mix = GameObject.Find("ToonTitleMix").GetComponent<RectTransform>();
        var button = StartButton.GetComponent<RectTransform>();
        presentation.enabled = false;
        Vector2 logoPosition=logo.anchoredPosition,mixPosition=mix.anchoredPosition,buttonPosition=button.anchoredPosition;
        Vector3 logoScale=logo.localScale,mixScale=mix.localScale,buttonScale=button.localScale;
        Assert.That(StartButton.interactable, Is.True);
        presentation.enabled = true;
        Assert.That(StartButton.interactable, Is.False);
        yield return WaitForTitleReady();
        presentation.enabled = false;
        Assert.That(Vector2.Distance(logo.anchoredPosition,logoPosition), Is.LessThan(.001f));Assert.That(logo.localScale, Is.EqualTo(logoScale));
        Assert.That(Vector2.Distance(mix.anchoredPosition,mixPosition), Is.LessThan(.001f));Assert.That(mix.localScale, Is.EqualTo(mixScale));
        Assert.That(Vector2.Distance(button.anchoredPosition,buttonPosition), Is.LessThan(.001f));Assert.That(button.localScale, Is.EqualTo(buttonScale));
        Assert.That(logo.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        Assert.That(StartButton.interactable, Is.True);
    }
    [UnityTest]
    public IEnumerator IdleContinuesAnimatingWhileTheStartButtonStaysAvailable()
    {
        yield return OpenTitle();yield return WaitForTitleReady();
        var logo = GameObject.Find("ToonTitleLogo").GetComponent<RectTransform>();
        Vector2 before = logo.anchoredPosition;int frame = Time.frameCount;
        yield return new WaitForSecondsRealtime(.45f);
        Assert.That(Time.frameCount, Is.GreaterThan(frame));
        Assert.That(Vector2.Distance(logo.anchoredPosition, before), Is.GreaterThan(.1f));
        Assert.That(StartButton.interactable, Is.True);
        foreach (var effect in Object.FindObjectsByType<TitleMixGraphic>(FindObjectsSortMode.None)) Assert.That(effect.raycastTarget, Is.False);
        foreach (var effect in Object.FindObjectsByType<TitleSparkleGraphic>(FindObjectsSortMode.None)) Assert.That(effect.raycastTarget, Is.False);
    }
}
