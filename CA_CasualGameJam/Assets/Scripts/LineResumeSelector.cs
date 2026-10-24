using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Chooses the line at a shared endpoint, then waits for a fresh drag on that endpoint.</summary>
public class LineResumeSelector : MonoBehaviour
{
    [SerializeField] private GameManager _game;
    [SerializeField] private Camera _boardCamera;
    [SerializeField] private GameObject _overlay;
    [SerializeField] private RectTransform _card;
    [SerializeField] private UnityEngine.UI.Button _dismiss;
    [SerializeField] private UnityEngine.UI.Button _close;
    [SerializeField] private UnityEngine.UI.Button _first;
    [SerializeField] private UnityEngine.UI.Button _second;
    [SerializeField] private TMP_Text _firstLabel;
    [SerializeField] private TMP_Text _secondLabel;
    [SerializeField] private UnityEngine.UI.Image _selectionMarker;
    private int _pendingTile = -1, _selectedTile = -1;
    private BaseColor _firstColor, _secondColor;
    public bool IsOpen => _overlay != null && _overlay.activeSelf;
    public BaseColor? SelectedColor { get; private set; }

    private void Awake()
    {
        _first.onClick.AddListener(SelectFirst);
        _second.onClick.AddListener(SelectSecond);
        _dismiss.onClick.AddListener(Clear);
        _close.onClick.AddListener(Clear);
        Clear();
    }
    private void OnEnable()
    {
        if (_game != null) _game.Changed += Clear;
        SceneNavigator.LoadingChanged += Clear;
    }
    private void OnDisable()
    {
        if (_game != null) _game.Changed -= Clear;
        SceneNavigator.LoadingChanged -= Clear;
        Clear();
    }
    public void Show(int tileIndex, IReadOnlyList<BaseColor> colors)
    {
        Clear();
        if (_game == null || _game.State != GameState.Idle || SceneNavigator.IsLoading || colors.Count != 2) return;
        _pendingTile = tileIndex;
        _firstColor = colors[0]; _secondColor = colors[1];
        Style(_first, _firstLabel, _firstColor);
        Style(_second, _secondLabel, _secondColor);
        _overlay.SetActive(true);
        PositionCard();
    }
    private static void Style(UnityEngine.UI.Button button, TMP_Text label, BaseColor color)
    {
        button.GetComponent<UnityEngine.UI.Image>().color = StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(color));
        label.text = color.ToString().ToUpperInvariant();
    }
    private void SelectFirst() => Select(_firstColor);
    private void SelectSecond() => Select(_secondColor);
    private void Select(BaseColor color)
    {
        if (!IsOpen || (_game.GetLineStartColors(_pendingTile) & PuzzleRules.ToTileColor(color)) == 0) { Clear(); return; }
        _selectedTile = _pendingTile; _pendingTile = -1;
        SelectedColor = color;
        _overlay.SetActive(false);
        Sprite sprite = _game.PaletteVisuals != null ? _game.PaletteVisuals.StartMarker(color) : null;
        _selectionMarker.sprite = sprite;
        _selectionMarker.color = sprite != null ? Color.white : GlobalScripts.GetFaceColor(PuzzleRules.ToTileColor(color));
        _selectionMarker.gameObject.SetActive(true);
        PositionMarker();
    }
    public bool TryConsumeSelection(int tileIndex, out BaseColor color)
    {
        bool matches = SelectedColor.HasValue && _selectedTile == tileIndex;
        color = SelectedColor.GetValueOrDefault();
        Clear();
        return matches;
    }
    public void Clear()
    {
        _pendingTile = _selectedTile = -1; SelectedColor = null;
        if (_overlay != null) _overlay.SetActive(false);
        if (_selectionMarker != null) _selectionMarker.gameObject.SetActive(false);
    }
    private Vector2 TileScreenPosition(int index) => _boardCamera.WorldToScreenPoint(_game.TilesList[index].transform.position);
    private void LateUpdate()
    {
        if (IsOpen) PositionCard();
        if (SelectedColor.HasValue) PositionMarker();
    }
    private void PositionCard()
    {
        if (_boardCamera == null || _pendingTile < 0) return;
        var root = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, TileScreenPosition(_pendingTile), null, out Vector2 point);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Screen.safeArea.min, null, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Screen.safeArea.max, null, out Vector2 max);
        float scale = Mathf.Min(1, (max.x - min.x - 32) / _card.sizeDelta.x, (max.y - min.y - 32) / _card.sizeDelta.y);
        scale = Mathf.Max(0.1f, scale);
        _card.localScale = Vector3.one * scale;
        Vector2 half = _card.sizeDelta * (scale * 0.5f);
        point.y += half.y + 70;
        point.x = Mathf.Clamp(point.x, min.x + half.x + 16, max.x - half.x - 16);
        point.y = Mathf.Clamp(point.y, min.y + half.y + 16, max.y - half.y - 16);
        _card.anchoredPosition = point;
    }
    private void PositionMarker()
    {
        if (_boardCamera == null || _selectedTile < 0) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, TileScreenPosition(_selectedTile), null, out Vector2 point);
        _selectionMarker.rectTransform.anchoredPosition = point;
    }
    private void OnDestroy()
    {
        if (_first != null) _first.onClick.RemoveListener(SelectFirst);
        if (_second != null) _second.onClick.RemoveListener(SelectSecond);
        if (_dismiss != null) _dismiss.onClick.RemoveListener(Clear);
        if (_close != null) _close.onClick.RemoveListener(Clear);
    }
}
