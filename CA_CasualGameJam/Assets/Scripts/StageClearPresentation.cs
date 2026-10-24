using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>Confetti, the solved board's movement, then the next-stage action.</summary>
public class StageClearPresentation : MonoBehaviour
{
    [SerializeField] private StageScreenLayout _layout;
    [SerializeField] private CanvasGroup _panel;
    [SerializeField] private CanvasGroup _actions;
    [SerializeField] private CanvasGroup _cardBackdrop;
    [SerializeField] private CanvasGroup _cardHeading;
    [SerializeField] private RectTransform _clearTitle;
    [SerializeField] private ClearConfetti _confetti;
    [SerializeField, Min(0)] private float _celebrationDelay = 0.5f;
    [SerializeField, Min(0.05f)] private float _shrinkDuration = 0.4f;
    [SerializeField, Min(0.05f)] private float _buttonFadeDuration = 0.2f;
    private Tween _boardTween, _buttonTween;
    private Tween _titlePulse;
    private int _version;
    public bool IsPlaying { get; private set; }
    public bool IsPresented { get; private set; }
    public ClearConfetti Confetti => _confetti;

    public async UniTask PlayAsync(CancellationToken token)
    {
        ResetPresentation();
        int version = _version;
        IsPlaying = true;
        _panel.gameObject.SetActive(true); _panel.alpha = 1; _panel.blocksRaycasts = true;
        if (_cardBackdrop != null) { _cardBackdrop.gameObject.SetActive(true); _cardBackdrop.alpha = 0; }
        if (_cardHeading != null) _cardHeading.alpha = 0;
        _layout.SetPlayControlsVisible(false);
        _confetti.Play();
        try
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(_celebrationDelay), ignoreTimeScale: true, cancellationToken: token);
            StartTitlePulse();
            _boardTween = DOTween.To(() => _layout.ClearProgress, SetProgress, 1, _shrinkDuration)
                .SetEase(Ease.InOutCubic).SetUpdate(true).SetLink(gameObject);
            await UniTask.Delay(System.TimeSpan.FromSeconds(_shrinkDuration), ignoreTimeScale: true, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            SetProgress(1);
            _buttonTween = DOTween.To(() => _actions.alpha, value => _actions.alpha = value, 1f, _buttonFadeDuration)
                .SetUpdate(true).SetLink(gameObject);
            await UniTask.Delay(System.TimeSpan.FromSeconds(_buttonFadeDuration), ignoreTimeScale: true, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            _actions.alpha = 1; _actions.interactable = _actions.blocksRaycasts = true;
            IsPresented = true;
        }
        finally
        {
            if (version == _version)
            {
                _boardTween?.Kill(); _buttonTween?.Kill(); IsPlaying = false;
                if (token.IsCancellationRequested) ResetPresentation();
            }
        }
    }
    private void StartTitlePulse()
    {
        StopTitlePulse();
        if (_clearTitle == null) return;
        _clearTitle.localScale = Vector3.one * .96f;
        _titlePulse = _clearTitle.DOScale(Vector3.one * 1.04f, .65f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
    }
    private void StopTitlePulse()
    {
        _titlePulse?.Kill(); _titlePulse = null;
        if (_clearTitle != null) _clearTitle.localScale = Vector3.one;
    }
    private void SetProgress(float progress)
    {
        _layout.SetClearProgress(progress);
        if (_cardBackdrop != null) _cardBackdrop.alpha = progress;
        if (_cardHeading != null) _cardHeading.alpha = progress;
    }
    public void ResetPresentation()
    {
        _version++;
        _boardTween?.Kill(); _buttonTween?.Kill(); _boardTween = _buttonTween = null;
        StopTitlePulse();
        IsPlaying = IsPresented = false;
        if (_confetti != null) _confetti.Stop();
        if (_layout != null) { _layout.SetClearProgress(0); _layout.SetPlayControlsVisible(true); }
        if (_actions != null) { _actions.alpha = 0; _actions.interactable = _actions.blocksRaycasts = false; }
        if (_panel != null) _panel.gameObject.SetActive(false);
        if (_cardBackdrop != null) { _cardBackdrop.alpha = 0; _cardBackdrop.gameObject.SetActive(false); }
        if (_cardHeading != null) _cardHeading.alpha = 0;
    }
    private void OnDisable() => StopTitlePulse();
    private void OnDestroy() { _version++; _boardTween?.Kill(); _buttonTween?.Kill(); StopTitlePulse(); }
}
