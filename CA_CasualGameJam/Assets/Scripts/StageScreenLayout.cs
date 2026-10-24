using UnityEngine;

/// <summary>Keeps the Figma layout and the world-space board in the same safe-area coordinates.</summary>
[ExecuteAlways, DefaultExecutionOrder(-200)]
public class StageScreenLayout : MonoBehaviour
{
    [SerializeField] private RectTransform _safeArea;
    [SerializeField] private RectTransform _content;
    [SerializeField] private RectTransform _home;
    [SerializeField] private RectTransform _stageLabel;
    [SerializeField] private RectTransform _boardArea;
    [SerializeField] private RectTransform _mixLegend;
    [SerializeField] private RectTransform _reloadPanel;
    [SerializeField] private Transform _board;
    [SerializeField] private Camera _boardCamera;
    [SerializeField] private Vector2Int _boardSize = new Vector2Int(4, 4);
    [SerializeField] private RectTransform _clearContent;
    [SerializeField] private RectTransform _clearBoardArea;
    [SerializeField] private RectTransform _clearActions;
    [SerializeField] private RectTransform _clearBackdropSafeArea;
    [SerializeField] private RectTransform _clearFrame;
    [SerializeField] private RectTransform _clearHeading;
    private float _clearProgress;
    private float _boardPunch = 1;
    private CanvasGroup[] _playGroups;
    public float ClearProgress => _clearProgress;
    public Transform BoardRoot => _board;
    public Camera BoardCamera => _boardCamera;
    public float VisualScale { get; private set; } = 1;
    private Rect _lastSafeArea, _lastCameraRect;
    private Vector2 _lastCanvasSize;
    private float _lastCameraSize;
    private bool _dirty = true;
    private readonly Vector3[] _corners = new Vector3[4];

    private void OnEnable() { _dirty = true; Canvas.willRenderCanvases += UpdateLayout; }
    private void OnDisable() => Canvas.willRenderCanvases -= UpdateLayout;
    private void OnValidate() => _dirty = true;
    private void Start() { Canvas.ForceUpdateCanvases(); ApplyLayout(); }
    private void LateUpdate() => UpdateLayout();
    public void SetClearProgress(float progress)
    {
        SetBoardPresentation(progress, 1);
    }
    public void SetBoardPresentation(float progress, float punch)
    {
        progress = Mathf.Clamp01(progress);
        if (Mathf.Approximately(progress, _clearProgress) && Mathf.Approximately(punch, _boardPunch)) return;
        _clearProgress = progress; _boardPunch = punch; _dirty = true; ApplyLayout();
    }
    public void SetPlayControlsOpacity(float alpha)
    {
        if (_playGroups == null)
        {
            _playGroups = new CanvasGroup[4];
            var controls = new[] { _home, _stageLabel, _mixLegend, _reloadPanel };
            for (int i = 0; i < controls.Length; i++)
                if (controls[i] != null) _playGroups[i] = controls[i].GetComponent<CanvasGroup>();
        }
        foreach (CanvasGroup group in _playGroups) if (group != null) group.alpha = alpha;
    }
    public void SetPlayControlsVisible(bool visible)
    {
        ControlVisible(_home,visible); ControlVisible(_stageLabel,visible);
        ControlVisible(_mixLegend,visible); ControlVisible(_reloadPanel,visible);
    }
    private static void ControlVisible(RectTransform rect,bool visible)
    { if (rect != null && rect.gameObject.activeSelf != visible) rect.gameObject.SetActive(visible); }
    private void UpdateLayout()
    {
        if (_safeArea == null || _content == null || _boardCamera == null) return;
        Vector2 canvasSize = ((RectTransform)_safeArea.parent).rect.size;
        if (!_dirty && _lastSafeArea == Screen.safeArea && _lastCanvasSize == canvasSize &&
            _lastCameraRect == _boardCamera.pixelRect && Mathf.Approximately(_lastCameraSize, _boardCamera.orthographicSize)) return;
        ApplyLayout();
    }

    public void ApplyLayout()
    {
        if (_safeArea == null || _content == null || _board == null || _boardCamera == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        _safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        _safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        _safeArea.offsetMin = _safeArea.offsetMax = Vector2.zero;
        Vector2 size = _safeArea.rect.size;
        if (size.x <= 0 || size.y <= 0) return;

        // B's portrait composition; extra phone height expands the section gaps.
        const float designWidth = 941, designGaps = 190;
        float boardHeight = 885f * (_boardSize.y + .9f) / (_boardSize.x + .7f);
        float sectionHeights = 775 + boardHeight;
        float scale = Mathf.Min(size.x / designWidth, size.y / sectionHeights);
        VisualScale = scale;
        float height = size.y / scale;
        float gap = Mathf.Max(0, (height - sectionHeights) / designGaps);
        _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0.5f, 0.5f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(designWidth, height);
        _content.localScale = Vector3.one * scale;
        float homeY = 28 * gap;
        float stageY = 103 * gap;
        float boardY = 285 + 115 * gap;
        float legendY = boardY + boardHeight + 28 * gap;
        float trayY = legendY + 140 + 28 * gap;
        Place(_home, 778, homeY, 140, 150);
        Place(_stageLabel, 164, stageY, 613, 275);
        Place(_boardArea, 28, boardY, 885, boardHeight);
        Place(_mixLegend, 214, legendY, 513, 140);
        Place(_reloadPanel, 24, trayY, 893, 315);

        if (_clearContent != null)
        {
            _clearContent.anchorMin = _safeArea.anchorMin; _clearContent.anchorMax = _safeArea.anchorMax;
            _clearContent.offsetMin = _clearContent.offsetMax = Vector2.zero;
        }
        if (_clearBackdropSafeArea != null)
        {
            _clearBackdropSafeArea.anchorMin = _safeArea.anchorMin;
            _clearBackdropSafeArea.anchorMax = _safeArea.anchorMax;
            _clearBackdropSafeArea.offsetMin = _clearBackdropSafeArea.offsetMax = Vector2.zero;
        }
        if (_clearFrame != null) Center(_clearFrame, new Vector2(0, -35 * scale), new Vector2(815, 1160) * scale);
        if (_clearHeading != null)
        {
            Center(_clearHeading, Vector2.zero, new Vector2(941, 1672));
            _clearHeading.localScale = Vector3.one * scale;
        }
        float previewY = -80 * scale;
        if (_clearBoardArea != null)
        {
            _clearBoardArea.anchorMin = _clearBoardArea.anchorMax = _clearBoardArea.pivot = new Vector2(0.5f, 0.5f);
            _clearBoardArea.sizeDelta = new Vector2(885, boardHeight) * (0.6f * scale);
            _clearBoardArea.anchoredPosition = new Vector2(0, previewY);
        }
        if (_clearActions != null)
        {
            _clearActions.anchorMin = _clearActions.anchorMax = _clearActions.pivot = new Vector2(0.5f, 0.5f);
            _clearActions.sizeDelta = new Vector2(710, 184);
            _clearActions.localScale = Vector3.one * scale;
            _clearActions.anchoredPosition = new Vector2(0, -470 * scale);
        }

        _boardArea.GetWorldCorners(_corners);
        Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, _corners[1]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, _corners[2]);
        float depth = -_boardCamera.transform.position.z;
        Vector3 worldLeft = _boardCamera.ScreenToWorldPoint(new Vector3(topLeft.x, topLeft.y, depth));
        Vector3 worldRight = _boardCamera.ScreenToWorldPoint(new Vector3(topRight.x, topRight.y, depth));
        float boardScale = (worldRight.x - worldLeft.x) / (_boardSize.x + 0.7f);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, _corners[0]);
        Vector3 worldBottom = _boardCamera.ScreenToWorldPoint(new Vector3(bottomLeft.x, bottomLeft.y, depth));
        Vector3 boardPosition = new Vector3((worldLeft.x + worldRight.x) / 2, (worldLeft.y + worldBottom.y) / 2, 0);
        if (_clearBoardArea != null && _clearProgress > 0)
        {
            _clearBoardArea.GetWorldCorners(_corners);
            Vector2 lower = RectTransformUtility.WorldToScreenPoint(null, _corners[0]);
            Vector2 upper = RectTransformUtility.WorldToScreenPoint(null, _corners[2]);
            Vector3 targetLower = _boardCamera.ScreenToWorldPoint(new Vector3(lower.x, lower.y, depth));
            Vector3 targetUpper = _boardCamera.ScreenToWorldPoint(new Vector3(upper.x, upper.y, depth));
            Vector3 targetPosition = (targetLower + targetUpper) / 2; targetPosition.z = 0;
            boardPosition = Vector3.Lerp(boardPosition, targetPosition, _clearProgress);
            boardScale = Mathf.Lerp(boardScale, (targetUpper.x - targetLower.x) / (_boardSize.x + 0.7f), _clearProgress);
        }
        _board.localScale = Vector3.one * (boardScale * _boardPunch);
        _board.position = boardPosition;
        Physics2D.SyncTransforms();
        _lastSafeArea = safe;
        _lastCanvasSize = ((RectTransform)_safeArea.parent).rect.size;
        _lastCameraRect = _boardCamera.pixelRect;
        _lastCameraSize = _boardCamera.orthographicSize;
        _dirty = false;
    }

    private static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x + width / 2, -y - height / 2);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }
}
