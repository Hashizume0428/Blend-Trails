using System;
using UnityEngine;

/// <summary>A visual example only. Never submits input or changes the puzzle's colors.</summary>
[DefaultExecutionOrder(100)]
public class StageOneTutorialGuide : MonoBehaviour
{
    [SerializeField] private GameManager _game;
    [SerializeField] private StageClearNavigation _navigation;
    [SerializeField] private Camera _boardCamera;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private RectTransform _hand;
    [SerializeField] private UnityEngine.UI.Image _halo;
    [SerializeField] private UnityEngine.UI.Image _arrow;
    [SerializeField] private UnityEngine.UI.Image[] _dashes = Array.Empty<UnityEngine.UI.Image>();
    [SerializeField] private Vector2Int[] _redRoute = Array.Empty<Vector2Int>();
    [SerializeField] private Vector2Int[] _blueRoute = Array.Empty<Vector2Int>();
    [SerializeField, Min(0)] private float _entryDelay = 0.8f;
    [SerializeField, Min(0)] private float _resumeDelay = 1.5f;
    [SerializeField, Min(0)] private float _blueGuideDelay = 0.5f;
    [SerializeField, Min(0)] private float _startHold = 0.4f;
    [SerializeField, Min(0.05f)] private float _secondsPerCell = 0.28f;
    [SerializeField, Min(0)] private float _endHold = 0.4f;
    [SerializeField, Min(0)] private float _loopPause = 0.8f;
    [SerializeField, Range(0.3f, 2)] private float _handSize = 1.15f;
    private RectTransform _rect;
    private Vector2[] _points;
    private BaseColor? _guideColor;
    private float _elapsed;
    private float _lastReleaseAt = float.NegativeInfinity;
    private GameState _previousState;
    private bool _redWasCommitted;
    private bool _firstIdle = true, _focused = true, _paused;

    public bool IsVisible => _group != null && _group.alpha > 0;
    public BaseColor? GuideColor => _guideColor;
    public Vector2 HandTipScreenPosition => RectTransformUtility.WorldToScreenPoint(null, _hand.position);

    private void OnEnable()
    {
        _rect = (RectTransform)transform;
        _points = new Vector2[Mathf.Max(_redRoute.Length, _blueRoute.Length)];
        _firstIdle = true;
        _lastReleaseAt = float.NegativeInfinity;
        _previousState = _game != null ? _game.State : GameState.Idle;
        _redWasCommitted = _game != null && _game.HasCommittedLine(BaseColor.Red);
        if (_game != null) _game.Changed += RefreshState;
        SceneNavigator.LoadingChanged += RefreshState;
        Canvas.willRenderCanvases += UpdatePose;
        RefreshState();
    }

    private void OnDisable()
    {
        if (_game != null) _game.Changed -= RefreshState;
        SceneNavigator.LoadingChanged -= RefreshState;
        Canvas.willRenderCanvases -= UpdatePose;
        _guideColor = null;
        Hide();
    }

    private bool CanGuide => _game != null && _navigation != null && _navigation.PuzzleId == 1 &&
        _game.IsInitialized && _game.State == GameState.Idle && !SceneNavigator.IsLoading && _focused && !_paused;

    private void Hide() { if (_group != null) _group.alpha = 0; }

    private void RefreshState()
    {
        if (_game != null && _game.State == GameState.Resolving && _previousState == GameState.Drawing)
            _lastReleaseAt = Time.unscaledTime;
        if (_game != null) _previousState = _game.State;
        bool redCommitted = _game != null && _game.HasCommittedLine(BaseColor.Red);
        Hide();
        _guideColor = CanGuide ? SelectColor() : null;
        _elapsed = -(_firstIdle ? _entryDelay : _resumeDelay);
        if (_guideColor == BaseColor.Blue && redCommitted && !_redWasCommitted && !float.IsNegativeInfinity(_lastReleaseAt))
            _elapsed = -Mathf.Max(0, _blueGuideDelay - (Time.unscaledTime - _lastReleaseAt));
        _redWasCommitted = redCommitted;
        if (CanGuide) _firstIdle = false;
    }

    private BaseColor? SelectColor()
    {
        if (_redRoute.Length < 2 || _blueRoute.Length < 2 ||
            _redRoute.Length + _blueRoute.Length != _game.TilesList.Count) return null;
        bool red = _game.HasCommittedLine(BaseColor.Red), blue = _game.HasCommittedLine(BaseColor.Blue);
        foreach (TileController tile in _game.TilesList)
        {
            bool redCell = Array.IndexOf(_redRoute, tile.Coordinate) >= 0;
            bool blueCell = Array.IndexOf(_blueRoute, tile.Coordinate) >= 0;
            if (redCell == blueCell) return null;
            TileColor solution = redCell ? TileColor.Red : TileColor.Blue;
            if (!PuzzleRules.IsTileCorrect(solution, tile.RequiredColor != TileColor.None,
                tile.RequiredColor, tile.ForbiddenColor)) return null;
            if (tile.IsStartTile && tile.Coordinate !=
                (tile.StartColor == BaseColor.Red ? _redRoute[0] : _blueRoute[0])) return null;
            TileColor expected = redCell && red ? TileColor.Red : blueCell && blue ? TileColor.Blue : TileColor.None;
            // A different partial solution is still playable; simply avoid a conflicting demonstration.
            if (tile.CurrentColor != expected) return null;
        }
        return !red ? BaseColor.Red : !blue ? BaseColor.Blue : (BaseColor?)null;
    }

    private void LateUpdate()
    {
        if (!CanGuide || !_guideColor.HasValue) { Hide(); return; }
        _elapsed += Time.unscaledDeltaTime;
        UpdatePose();
    }

    private void UpdatePose()
    {
        if (!CanGuide || !_guideColor.HasValue || _elapsed < 0 || _boardCamera == null ||
            _group == null || _hand == null || _halo == null || _arrow == null) { Hide(); return; }
        Vector2Int[] route = _guideColor == BaseColor.Red ? _redRoute : _blueRoute;
        float travel = (route.Length - 1) * _secondsPerCell;
        float active = _startHold + travel + _endHold;
        float phase = _elapsed % Mathf.Max(0.01f, active + _loopPause);
        if (phase >= active) { Hide(); return; }
        for (int i = 0; i < route.Length; i++)
        {
            TileController tile = _game.GetTile(route[i]);
            if (tile == null) { Hide(); return; }
            Vector2 screen = _boardCamera.WorldToScreenPoint(tile.transform.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out _points[i]))
            { Hide(); return; }
        }
        float cell = Vector2.Distance(_points[0], _points[1]);
        if (cell <= 0) { Hide(); return; }
        Color color = _guideColor == BaseColor.Red ? new Color32(255, 65, 82, 255) : new Color32(35, 122, 255, 255);
        int dash = 0;
        for (int edge = 0; edge < route.Length - 1; edge++)
        {
            Vector2 delta = _points[edge + 1] - _points[edge];
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            for (int step = 0; step < 4 && dash < _dashes.Length; step++, dash++)
            {
                var image = _dashes[dash];
                image.rectTransform.anchoredPosition = Vector2.Lerp(_points[edge], _points[edge + 1], (step + 0.5f) / 4);
                image.rectTransform.sizeDelta = new Vector2(cell * 0.15f, cell * 0.045f);
                image.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                image.color = new Color(color.r, color.g, color.b, 0.38f);
            }
        }
        float progress = Mathf.Clamp((phase - _startHold) / _secondsPerCell, 0, route.Length - 1);
        int segment = Mathf.Min(Mathf.FloorToInt(progress), route.Length - 2);
        Vector2 tip = Vector2.Lerp(_points[segment], _points[segment + 1], progress - segment);
        Vector2 direction = (_points[segment + 1] - _points[segment]).normalized;
        _hand.anchoredPosition = tip;
        _hand.sizeDelta = Vector2.one * (cell * _handSize);
        _halo.rectTransform.anchoredPosition = tip;
        _halo.rectTransform.sizeDelta = Vector2.one * (cell * (0.49f + 0.035f * Mathf.Sin(phase * 6)));
        _halo.color = new Color(color.r, color.g, color.b, 0.25f);
        _arrow.rectTransform.anchoredPosition = tip + direction * (cell * 0.48f);
        _arrow.rectTransform.sizeDelta = new Vector2(cell * 0.38f, cell * 0.26f);
        _arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        _arrow.color = color;
        _arrow.enabled = progress > 0 && progress < route.Length - 1;
        _group.alpha = 1;
    }

    private void OnApplicationFocus(bool focused) { _focused = focused; RefreshState(); }
    private void OnApplicationPause(bool paused) { _paused = paused; RefreshState(); }
}
