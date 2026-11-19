using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>A finite celebration followed by an interactive result with gentle idle motion.</summary>
public class StageClearPresentation : MonoBehaviour
{
    [SerializeField] private StageScreenLayout _layout;
    [SerializeField] private CanvasGroup _panel, _actions, _cardBackdrop, _cardHeading;
    [SerializeField] private RectTransform _clearTitle;
    [SerializeField] private ClearConfetti _confetti, _foregroundConfetti;
    [SerializeField] private ToonClearBadge _badge;
    [SerializeField] private CanvasGroup _cardBody, _details;
    [SerializeField] private RectTransform _card, _nextButton, _nextArrow;
    [SerializeField] private UnityEngine.UI.Image _introScrim, _backgroundScrim;
    [SerializeField] private ClearBoardVisuals _boardVisuals;
    [SerializeField] private ClearTitleAnimator _titleAnimator;
    [SerializeField] private ClearCelebrationGraphic _celebration;
    [SerializeField] private ClearSheenGraphic _boardSheen, _buttonSheen;
    [SerializeField, Min(.1f)] private float _mainDuration = 3.18f;
    private CancellationTokenSource _lifetime;
    private double _startedAt;
    private bool _running;
    private int _version;
    private Vector2 _arrowOrigin;
    private bool _cached;
    public bool IsPlaying { get; private set; }
    public bool IsPresented { get; private set; }
    public ClearConfetti Confetti => _confetti;
    public float ElapsedSeconds => _running ? (float)(Time.unscaledTimeAsDouble - _startedAt) * 3.18f / _mainDuration : 0;

    public async UniTask PlayAsync(CancellationToken token)
    {
        ResetPresentation();
        if (_badge == null || _boardVisuals == null || _titleAnimator == null)
            throw new InvalidOperationException("Apply the approved clear motion to this stage before playing.");
        if (!_cached) { _arrowOrigin = _nextArrow.anchoredPosition; _cached = true; }
        int version = _version;
        var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, destroyCancellationToken);
        _lifetime = lifetime;
        CancellationToken localToken = lifetime.Token;
        IsPlaying = true; _running = true; _startedAt = Time.unscaledTimeAsDouble;
        _panel.gameObject.SetActive(true); _panel.alpha = 1; _panel.blocksRaycasts = true;
        _cardBackdrop.gameObject.SetActive(true); _cardBackdrop.alpha = 1;
        _cardHeading.alpha = 1;
        _layout.SetPlayControlsVisible(true); _layout.SetPlayControlsOpacity(1);
        _boardVisuals.Begin(_layout.BoardRoot, _layout.BoardCamera);
        GameManager game = FindFirstObjectByType<GameManager>();
        StageColorPalette palette = game != null ? game.Palette : StageColorPalette.ForStage(1);
        var navigation = game != null ? game.GetComponent<StageClearNavigation>() : null;
        int seed = navigation != null ? navigation.StageNumber * 4073 : 4073;
        _badge.Configure(palette);
        _confetti.Begin(palette, seed); _foregroundConfetti.Begin(palette, seed);
        _titleAnimator.Begin(); ApplyPose(0);
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_mainDuration), ignoreTimeScale: true, cancellationToken: localToken);
            localToken.ThrowIfCancellationRequested();
            if (version != _version) return;
            ApplyPose(3.18f);
            _actions.alpha = 1; _actions.interactable = _actions.blocksRaycasts = true;
            IsPlaying = false; IsPresented = true;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
        finally
        {
            if (version == _version && localToken.IsCancellationRequested) ResetPresentation();
        }
    }
    private void Update() { if (_running) ApplyPose(ElapsedSeconds); }
    private void ApplyPose(float t)
    {
        float enter = ClearMotionMath.Out(ClearMotionMath.Progress(t,.48f,.82f));
        float move = ClearMotionMath.Cubic(ClearMotionMath.Progress(t,1.98f,2.78f));
        float controls = 1 - ClearMotionMath.Out(ClearMotionMath.Progress(t,.40f,.70f));
        _layout.SetPlayControlsOpacity(controls); _layout.SetPlayControlsVisible(controls > 0);
        float punch = 1 + Mathf.Sin(ClearMotionMath.Progress(t,.05f,.52f)*Mathf.PI)*.018f;
        punch *= 1 + Mathf.Sin(ClearMotionMath.Progress(t,2.72f,3.12f)*Mathf.PI)*.024f;
        _layout.SetBoardPresentation(move,punch);
        float boardAlpha = t < 1.98f ? 1 - ClearMotionMath.Out(ClearMotionMath.Progress(t,1.65f,1.95f))
            : ClearMotionMath.Out(ClearMotionMath.Progress(t,2.30f,2.65f));
        _boardVisuals.SetOpacity(boardAlpha);
        // Linear blending needs a stronger scrim to match the approved sRGB movie.
        SetAlpha(_introScrim,enter * (1-move) * .94f); SetAlpha(_backgroundScrim,enter * move * .60f);
        float card = ClearMotionMath.Out(ClearMotionMath.Progress(t,2.0f,2.66f));
        _cardBody.alpha = card;
        _card.localScale = Vector3.one * Mathf.LerpUnclamped(.88f,1,ClearMotionMath.Back(card));
        _card.anchoredPosition = Vector2.down * (85 * (1-card) * _layout.VisualScale);
        float inBadge = ClearMotionMath.Progress(t,.60f,.98f);
        float badgeScale = Mathf.LerpUnclamped(0,1.46f,ClearMotionMath.Back(inBadge));
        float floating = t > 2.78f ? Mathf.Sin((t-2.78f)*Mathf.PI*2/2.6f)*3 : 0;
        RectTransform badge = _badge.rectTransform;
        badge.anchoredPosition = new Vector2(470.5f,-Mathf.Lerp(660,285,move)+floating);
        badge.localScale = Vector3.one * Mathf.Lerp(badgeScale,1,move);
        badge.localRotation = Quaternion.Euler(0,0,(1-move)*Mathf.Sin(inBadge*Mathf.PI*2)*4.3f);
        _badge.color = new Color(1,1,1,enter); _badge.SetTime(t);
        float pulse = t > 2.78f ? 1 + Mathf.Sin((t-2.78f)*Mathf.PI*2/1.3f)*.035f : 1;
        _clearTitle.anchoredPosition = new Vector2(470.5f,-Mathf.Lerp(940,442,move));
        _clearTitle.localScale = Vector3.one * (Mathf.Lerp(1.04f,1,move)*pulse); _titleAnimator.SetTime(t);
        float details = ClearMotionMath.Out(ClearMotionMath.Progress(t,2.48f,2.88f));
        _details.alpha = details; ((RectTransform)_details.transform).anchoredPosition = Vector2.down * (28*(1-details));
        float action = ClearMotionMath.Progress(t,2.82f,3.18f);
        _actions.alpha = ClearMotionMath.Out(action);
        float idle = t > 3.18f ? 1+Mathf.Sin((t-3.18f)*Mathf.PI*2/2.8f)*.008f : 1;
        _nextButton.localScale = Vector3.one * (Mathf.LerpUnclamped(.8f,1,ClearMotionMath.Back(action))*idle);
        _nextButton.anchoredPosition = Vector2.down * (59*(1-ClearMotionMath.Out(action)));
        _nextArrow.anchoredPosition = _arrowOrigin + Vector2.right * (t>3.2f ? Mathf.Sin((t-3.2f)*Mathf.PI*2/1.6f)*5 : 0);
        _celebration.SetTime(t); _confetti.SetTime(t); _foregroundConfetti.SetTime(t);
        _boardVisuals.FitSheen(_boardSheen.rectTransform); _boardSheen.SetProgress(ClearMotionMath.Progress(t,.18f,.68f));
        _buttonSheen.SetProgress(t>=3.42f ? (t-3.42f)%3.2f/.65f : -1);
    }
    private static void SetAlpha(UnityEngine.UI.Image image,float alpha)
    { Color tint=image.color; if (Mathf.Approximately(tint.a,alpha)) return; tint.a=alpha; image.color=tint; }
    public void ResetPresentation()
    {
        _version++; _running=false; IsPlaying=IsPresented=false;
        _lifetime?.Cancel(); _lifetime?.Dispose(); _lifetime=null;
        if (_titleAnimator != null) _titleAnimator.Restore();
        if (_boardVisuals != null) _boardVisuals.Restore();
        if (_confetti != null) _confetti.Stop();
        if (_foregroundConfetti != null) _foregroundConfetti.Stop();
        if (_celebration != null) _celebration.SetTime(-1);
        if (_boardSheen != null) _boardSheen.SetProgress(-1);
        if (_buttonSheen != null) _buttonSheen.SetProgress(-1);
        if (_layout != null) { _layout.SetBoardPresentation(0,1); _layout.SetPlayControlsOpacity(1); _layout.SetPlayControlsVisible(true); }
        if (_clearTitle != null) { _clearTitle.localScale=Vector3.one; _clearTitle.anchoredPosition=new Vector2(470.5f,-442); }
        if (_badge != null) { _badge.rectTransform.localScale=Vector3.one; _badge.rectTransform.localRotation=Quaternion.identity; _badge.rectTransform.anchoredPosition=new Vector2(470.5f,-285); _badge.color=Color.white; _badge.SetTime(2); }
        if (_card != null) { _card.localScale=Vector3.one; _card.anchoredPosition=Vector2.zero; }
        if (_details != null) { _details.alpha=0; ((RectTransform)_details.transform).anchoredPosition=Vector2.zero; }
        if (_nextButton != null) { _nextButton.localScale=Vector3.one; _nextButton.anchoredPosition=Vector2.zero; }
        if (_cached && _nextArrow != null) _nextArrow.anchoredPosition=_arrowOrigin;
        if (_introScrim != null) SetAlpha(_introScrim,0);
        if (_actions != null) { _actions.alpha=0; _actions.interactable=_actions.blocksRaycasts=false; }
        if (_cardHeading != null) _cardHeading.alpha=0;
        if (_cardBackdrop != null) { _cardBackdrop.alpha=0; _cardBackdrop.gameObject.SetActive(false); }
        if (_panel != null) _panel.gameObject.SetActive(false);
    }
    private void OnDisable() { if (Application.isPlaying) ResetPresentation(); }
    private void OnDestroy() { if (Application.isPlaying) ResetPresentation(); }
}

internal static class ClearMotionMath
{
    public static float Progress(float t,float start,float end) => Mathf.Clamp01((t-start)/(end-start));
    public static float Out(float p) => 1-Mathf.Pow(1-Mathf.Clamp01(p),3);
    public static float Cubic(float p) => p<.5f ? 4*p*p*p : 1-Mathf.Pow(-2*p+2,3)/2;
    public static float Back(float p) { p=Mathf.Clamp01(p);const float c=1.28f;return 1+(c+1)*Mathf.Pow(p-1,3)+c*Mathf.Pow(p-1,2); }
}
