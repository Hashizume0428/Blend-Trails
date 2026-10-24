using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class TileController : MonoBehaviour
{
    private const float ErrorBlinkDuration = 2f;
    private const int ErrorBlinkLoops = 8;
    [SerializeField] private int _tileIndex;
    [SerializeField] private Vector2Int _coordinate;
    [SerializeField] private bool _isStartTile;
    [SerializeField] private BaseColor _startColor;
    [FormerlySerializedAs("_isCorlorDetected")]
    [SerializeField] private bool _hasRequiredColors;
    [SerializeField] private List<BaseColor> _requiredColors = new List<BaseColor>();
    [SerializeField] private TileColor _forbiddenColor;
    [SerializeField] private List<BaseColor> _currentColorsList = new List<BaseColor>();
    [SerializeField] private bool _isTileCorrect;
    [SerializeField] private SpriteRenderer _faceRenderer;
    [SerializeField] private SpriteRenderer _frameRenderer;
    [SerializeField] private SpriteRenderer _startMarker;
    [SerializeField] private SpriteRenderer _forbiddenMarker;
    [SerializeField] private LineController _line;
    private Tween _feedback;
    private Vector3 _faceScale;
    private Tween _invalidBlink;
    private Color _frameBaseColor = Color.white;
    private Color _forbiddenBaseColor = Color.white;
    private bool _indicatorColorsCached;

    public int TileIndex => _tileIndex;
    public Vector2Int Coordinate => _coordinate;
    public bool IsStartTile => _isStartTile;
    public BaseColor StartColor => _startColor;
    public LineController Line => _line;
    public TileColor CurrentColor => PuzzleRules.Mix(_currentColorsList);
    public IReadOnlyList<BaseColor> CurrentColors => _currentColorsList;
    public Color DisplayColor => GlobalScripts.GetFaceColor(CurrentColor);
    public bool IsErrorBlinking => _invalidBlink != null && _invalidBlink.IsActive();

    private void Awake()
    {
        if (_faceRenderer != null) _faceScale = _faceRenderer.transform.localScale;
        CacheIndicatorColors();
    }
    private void OnEnable() => RefreshVisuals();

    public void Configure(int index, Vector2Int coordinate, bool isStart, BaseColor startColor,
        IEnumerable<BaseColor> requiredColors, TileColor forbiddenColor)
    {
        _tileIndex = index;
        _coordinate = coordinate;
        _isStartTile = isStart;
        _startColor = startColor;
        _requiredColors = new List<BaseColor>(requiredColors);
        _hasRequiredColors = _requiredColors.Count > 0;
        _forbiddenColor = forbiddenColor;
        ResetRuntimeState();
    }
    public void BindVisuals(SpriteRenderer face, SpriteRenderer frame, SpriteRenderer start,
        SpriteRenderer forbidden, LineController line)
    {
        StopErrorBlink();
        _faceRenderer = face; _frameRenderer = frame; _startMarker = start;
        _forbiddenMarker = forbidden; _line = line;
        _indicatorColorsCached = false;
        CacheIndicatorColors();
        if (face != null) _faceScale = face.transform.localScale;
        RefreshVisuals();
    }
    public bool CanAddColor(BaseColor color) => PuzzleRules.CanAddColor(CurrentColor, color);
    public bool AddColor(BaseColor color)
    {
        if (_currentColorsList.Contains(color) || !CanAddColor(color)) return false;
        _currentColorsList.Add(color);
        RefreshVisuals();
        return true;
    }
    public bool RemoveColor(BaseColor color)
    {
        bool removed = _currentColorsList.Remove(color);
        RefreshVisuals();
        return removed;
    }
    public bool IsTileCorrect()
    {
        _isTileCorrect = PuzzleRules.IsTileCorrect(CurrentColor, _hasRequiredColors,
            PuzzleRules.Mix(_requiredColors), _forbiddenColor);
        return _isTileCorrect;
    }
    public void RefreshVisuals()
    {
        bool correct = IsTileCorrect();
        if (_faceRenderer != null)
            _faceRenderer.color = DisplayColor;
        if (_frameRenderer != null)
            _frameRenderer.sprite = GlobalScripts.GetFrameImage(_hasRequiredColors ? PuzzleRules.Mix(_requiredColors) : TileColor.None);
        if (_startMarker != null) _startMarker.gameObject.SetActive(_isStartTile);
        if (_forbiddenMarker != null)
        {
            _forbiddenMarker.transform.localPosition = Vector3.zero;
            _forbiddenMarker.gameObject.SetActive(_forbiddenColor != TileColor.None);
        }
        if (_line != null) _line.gameObject.SetActive(_isStartTile);
        if (correct) StopErrorBlink();
    }
    public void ResetRuntimeState()
    {
        StopErrorBlink();
        StopFeedback();
        _currentColorsList.Clear();
        RefreshVisuals();
    }
    public async UniTask PlayFeedbackAsync(CancellationToken token)
    {
        if (_faceRenderer == null) return;
        StopFeedback();
        Tween feedback = _faceRenderer.transform.DOPunchScale(_faceScale * 0.055f, 0.12f, 1, 0f).SetLink(gameObject);
        _feedback = feedback;
        try { await UniTask.Delay(120, cancellationToken: token); }
        finally
        {
            feedback.Kill();
            if (_feedback == feedback) StopFeedback();
        }
    }
    private void StopFeedback()
    {
        _feedback?.Kill(); _feedback = null;
        if (_faceRenderer != null && _faceScale != Vector3.zero) _faceRenderer.transform.localScale = _faceScale;
    }
    private void CacheIndicatorColors()
    {
        if (_indicatorColorsCached || (_frameRenderer == null && _forbiddenMarker == null)) return;
        if (_frameRenderer != null) _frameBaseColor = _frameRenderer.color;
        if (_forbiddenMarker != null) _forbiddenBaseColor = _forbiddenMarker.color;
        _indicatorColorsCached = true;
    }
    public void PlayErrorBlink()
    {
        StopErrorBlink();
        if (!Application.isPlaying || !isActiveAndEnabled || IsTileCorrect()) return;
        CacheIndicatorColors();
        SetIndicatorAlpha(1);
        _invalidBlink = DOTween.To(() => 1f, SetIndicatorAlpha, 0.2f, ErrorBlinkDuration / ErrorBlinkLoops)
            .SetEase(Ease.InOutSine).SetLoops(ErrorBlinkLoops, LoopType.Yoyo)
            .SetUpdate(true).SetLink(gameObject)
            .OnComplete(() =>
            {
                _invalidBlink = null;
                SetIndicatorAlpha(1);
            });
    }
    private void SetIndicatorAlpha(float alpha)
    {
        if (_frameRenderer != null)
        {
            Color color = _frameBaseColor; color.a *= alpha;
            _frameRenderer.color = color;
        }
        if (_forbiddenMarker != null)
        {
            Color color = _forbiddenBaseColor; color.a *= alpha;
            _forbiddenMarker.color = color;
        }
    }
    public void StopErrorBlink()
    {
        _invalidBlink?.Kill();
        _invalidBlink = null;
        if (_indicatorColorsCached) SetIndicatorAlpha(1);
    }
    private void OnDisable() { StopErrorBlink(); StopFeedback(); }
    private void OnDestroy() { StopErrorBlink(); StopFeedback(); }

    // Compatibility with the original tile API.
    public void AddColorList(int color) => AddColor((BaseColor)color);
    public void RemoveColorList(int color) => RemoveColor((BaseColor)color);
    public bool IsTileCurrent() => IsTileCorrect();
    public void SetTileIndex(int index) => _tileIndex = index;
    public bool GetIsTileCorrect() => IsTileCorrect();
    public bool GetIsStartTile() => _isStartTile;
    public void ChangeFaceColor(Color color) { if (_faceRenderer != null) _faceRenderer.color = color; }
    public void ChangeFlameImage(Sprite sprite) { if (_frameRenderer != null) _frameRenderer.sprite = sprite; }
}
