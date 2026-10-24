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
    [SerializeField] private ClearConfetti _confetti;
    [SerializeField, Min(0)] private float _celebrationDelay = 0.5f;
    [SerializeField, Min(0.05f)] private float _shrinkDuration = 0.4f;
    [SerializeField, Min(0.05f)] private float _buttonFadeDuration = 0.2f;
    private Tween _boardTween, _buttonTween;
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
        IsPlaying = IsPresented = false;
        if (_confetti != null) _confetti.Stop();
        if (_layout != null) { _layout.SetClearProgress(0); _layout.SetPlayControlsVisible(true); }
        if (_actions != null) { _actions.alpha = 0; _actions.interactable = _actions.blocksRaycasts = false; }
        if (_panel != null) _panel.gameObject.SetActive(false);
        if (_cardBackdrop != null) { _cardBackdrop.alpha = 0; _cardBackdrop.gameObject.SetActive(false); }
        if (_cardHeading != null) _cardHeading.alpha = 0;
    }
    private void OnDestroy() { _version++; _boardTween?.Kill(); _buttonTween?.Kill(); }
}
