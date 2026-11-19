using System;
using UnityEngine;

/// <summary>The approved title entrance, followed by a six-second idle cycle.</summary>
[DisallowMultipleComponent]
public sealed class TitlePresentation : MonoBehaviour
{
    public const float ReadySeconds = 2.58f;
    [SerializeField] private RectTransform _logo, _mix, _button, _arrow;
    [SerializeField] private CanvasGroup _logoOpacity, _mixOpacity, _buttonOpacity;
    [SerializeField] private ToonSkyGraphic _sky;
    [SerializeField] private TitleMixGraphic _mixLight;
    [SerializeField] private TitleSparkleGraphic _sparkles;
    [SerializeField] private ClearSheenGraphic _logoSheen, _buttonSheen;
    private Vector2 _logoOrigin, _mixOrigin, _buttonOrigin, _arrowOrigin;
    private Vector3 _logoScale, _mixScale, _buttonScale;
    private Quaternion _logoRotation;
    private float _elapsed;
    private bool _cached, _running, _paused, _waitingForFirstUpdate;
    public bool IsReady { get; private set; }
    public float ElapsedSeconds => _running ? _elapsed : 0;
    public event Action ReadinessChanged;

    private void Awake() => CacheOrigins();
    private void OnEnable()
    {
        if (!CacheOrigins()) return;
        _running = true; _paused = false; _elapsed = 0; _waitingForFirstUpdate = true;
        SetReady(false); ApplyPose(0);
    }
    private bool CacheOrigins()
    {
        if (_cached) return true;
        if (_logo == null || _mix == null || _button == null || _arrow == null ||
            _logoOpacity == null || _mixOpacity == null || _buttonOpacity == null) return false;
        _logoOrigin = _logo.anchoredPosition; _mixOrigin = _mix.anchoredPosition;
        _buttonOrigin = _button.anchoredPosition; _arrowOrigin = _arrow.anchoredPosition;
        _logoScale = _logo.localScale; _mixScale = _mix.localScale; _buttonScale = _button.localScale;
        _logoRotation = _logo.localRotation; _cached = true;
        return true;
    }
    private void Update()
    {
        if (!_running || _paused) return;
        // OnEnable can precede slow scene/texture initialization. Keep the first
        // displayed frame at the hidden pose instead of consuming the entrance.
        if (_waitingForFirstUpdate) { _waitingForFirstUpdate = false; return; }
        // A startup hitch must not jump straight past the logo's visible growth.
        float delta = Time.unscaledDeltaTime;
        _elapsed += _elapsed < ReadySeconds ? Mathf.Min(delta, .05f) : delta;
        ApplyPose(_elapsed);
    }
    private void OnApplicationPause(bool pause)
    {
        if (!_running || pause == _paused) return;
        _paused = pause;
        if (!pause) _waitingForFirstUpdate = true;
    }
    private void ApplyPose(float t)
    {
        float logoIn = TitleMotionMath.Progress(t, .08f, .86f), logoBack = TitleMotionMath.Back(logoIn);
        float logoIdle = Mathf.SmoothStep(0, 1, TitleMotionMath.Progress(t, .9f, 2.6f));
        float phase = (t - 2.6f) * Mathf.PI * 2 / 6;
        _logo.anchoredPosition = _logoOrigin + Vector2.up * (67 * (1 - logoBack) - logoIdle * Mathf.Sin(phase) * 5);
        _logo.localScale = _logoScale * Mathf.LerpUnclamped(.7f, 1, logoBack);
        _logo.localRotation = _logoRotation * Quaternion.Euler(0, 0,
            Mathf.Lerp(3.724f, 0, TitleMotionMath.Out(logoIn)) - logoIdle * Mathf.Sin(phase) * .2005f);
        _logoOpacity.alpha = TitleMotionMath.Out(TitleMotionMath.Progress(t, .08f, .32f));

        float mixIn = TitleMotionMath.Progress(t, .48f, 1.22f);
        float mixBack = Mathf.LerpUnclamped(.83f, 1, TitleMotionMath.Back(mixIn));
        float cycle = Mathf.Repeat(t - 1.25f, 6);
        float hit = cycle >= .84f && cycle < 1.36f ? Mathf.Sin(TitleMotionMath.Progress(cycle, .84f, 1.36f) * Mathf.PI) : 0;
        float mixIdle = Mathf.SmoothStep(0, 1, TitleMotionMath.Progress(t, 1.6f, 2.6f));
        _mix.anchoredPosition = _mixOrigin + Vector2.down * (76 * (1 - TitleMotionMath.Out(mixIn)) + mixIdle * Mathf.Sin(phase * 2) * 4);
        _mix.localScale = Vector3.Scale(_mixScale, new Vector3(mixBack * (1 + hit * .014f), mixBack * (1 - hit * .012f), 1));
        _mixOpacity.alpha = TitleMotionMath.Out(TitleMotionMath.Progress(t, .48f, .80f));

        float buttonIn = TitleMotionMath.Progress(t, 1.98f, ReadySeconds);
        float idle = Mathf.SmoothStep(0, 1, TitleMotionMath.Progress(t, ReadySeconds, 3.1f));
        float breath = 1 + Mathf.Sin(phase * 2) * .004f * idle;
        _button.anchoredPosition = _buttonOrigin + Vector2.down * (87.5f * (1 - TitleMotionMath.Out(buttonIn)));
        _button.localScale = _buttonScale * (Mathf.LerpUnclamped(.94f, 1, TitleMotionMath.Back(buttonIn)) * breath);
        _arrow.anchoredPosition = _arrowOrigin + Vector2.right * (Mathf.Sin(phase * 2) * 4 * idle);
        _buttonOpacity.alpha = TitleMotionMath.Out(TitleMotionMath.Progress(t, 1.98f, 2.22f));
        // MenuNavigation owns Button.interactable; this group also removes the invisible button from raycasts.
        bool ready = t >= ReadySeconds;
        _buttonOpacity.interactable = _buttonOpacity.blocksRaycasts = ready;
        SetReady(ready);
        if (_sky != null) _sky.SetTime(t);
        if (_mixLight != null) _mixLight.SetTime(t);
        if (_sparkles != null) _sparkles.SetTime(t);
        if (_logoSheen != null) _logoSheen.SetProgress(t >= 3.8f ? Mathf.Repeat(t - 3.8f, 6) / .85f : -1);
        if (_buttonSheen != null) _buttonSheen.SetProgress(t >= 2.9f ? Mathf.Repeat(t - 2.9f, 6) / .8f : -1);
    }
    private void SetReady(bool ready)
    {
        if (IsReady == ready) return;
        IsReady = ready; ReadinessChanged?.Invoke();
    }
    private void OnDisable()
    {
        _running = false; _paused = false; _elapsed = 0; _waitingForFirstUpdate = false;
        if (_cached)
        {
            _logo.anchoredPosition = _logoOrigin; _logo.localScale = _logoScale; _logo.localRotation = _logoRotation;
            _mix.anchoredPosition = _mixOrigin; _mix.localScale = _mixScale;
            _button.anchoredPosition = _buttonOrigin; _button.localScale = _buttonScale; _arrow.anchoredPosition = _arrowOrigin;
            _logoOpacity.alpha = _mixOpacity.alpha = _buttonOpacity.alpha = 1;
            _buttonOpacity.interactable = _buttonOpacity.blocksRaycasts = true;
        }
        if (_sky != null) _sky.SetTime(-1);
        if (_mixLight != null) _mixLight.SetTime(-1);
        if (_sparkles != null) _sparkles.SetTime(-1);
        if (_logoSheen != null) _logoSheen.SetProgress(-1);
        if (_buttonSheen != null) _buttonSheen.SetProgress(-1);
        SetReady(true);
    }
}

internal static class TitleMotionMath
{
    public static float Progress(float t, float a, float b) => Mathf.Clamp01((t - a) / (b - a));
    public static float Out(float p) => 1 - Mathf.Pow(1 - Mathf.Clamp01(p), 3);
    public static float Back(float p) { p = Mathf.Clamp01(p) - 1; return 1 + 2.4f * p * p * p + 1.4f * p * p; }
}
