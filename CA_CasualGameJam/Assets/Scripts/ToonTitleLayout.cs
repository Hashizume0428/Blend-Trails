using UnityEngine;

[ExecuteAlways]
public sealed class ToonTitleLayout : MonoBehaviour
{
    [SerializeField] private RectTransform _safeArea, _content;
    private Rect _lastSafe;
    private Vector2 _lastSize;
    private void OnEnable() { _lastSize = Vector2.zero; Canvas.willRenderCanvases += Apply; }
    private void OnDisable() => Canvas.willRenderCanvases -= Apply;
    private void LateUpdate() => Apply();
    public void Apply()
    {
        if (_safeArea == null || _content == null || Screen.width <= 0 || Screen.height <= 0) return;
        Vector2 size = ((RectTransform)_safeArea.parent).rect.size;
        Rect safe = Screen.safeArea;
        if (_lastSafe == safe && _lastSize == size) return;
        _safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        _safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        _safeArea.offsetMin = _safeArea.offsetMax = Vector2.zero;
        _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(.5f, .5f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(941, 1672);
        _content.localScale = Vector3.one * Mathf.Min(_safeArea.rect.width / 941, _safeArea.rect.height / 1672);
        _lastSafe = safe; _lastSize = size;
    }
}
