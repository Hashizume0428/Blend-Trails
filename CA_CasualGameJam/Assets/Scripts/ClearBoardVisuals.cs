using System.Collections.Generic;
using UnityEngine;

/// <summary>Fades the real board without changing tile colors, line data or shared materials.</summary>
public sealed class ClearBoardVisuals : MonoBehaviour
{
    private static readonly int Tint = Shader.PropertyToID("_Color");
    private sealed class Entry
    {
        public Renderer Renderer;
        public MaterialPropertyBlock Original, Working;
        public Color Color;
    }
    private readonly List<Entry> _entries = new List<Entry>();
    private Renderer _background;
    private Camera _camera;
    private float _opacity = 1;
    public float Opacity => _opacity;

    public void Begin(Transform board, Camera camera)
    {
        Restore();
        _camera = camera;
        _background = board.Find("BoardBackground").GetComponent<Renderer>();
        foreach (Renderer renderer in board.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty(Tint)) continue;
            var original = new MaterialPropertyBlock();
            var working = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(original); renderer.GetPropertyBlock(working);
            Color tint = original.HasProperty(Tint) ? original.GetColor(Tint) : renderer.sharedMaterial.GetColor(Tint);
            _entries.Add(new Entry { Renderer = renderer, Original = original, Working = working, Color = tint });
        }
    }
    public void SetOpacity(float opacity)
    {
        opacity = Mathf.Clamp01(opacity);
        if (Mathf.Approximately(opacity, _opacity)) return;
        _opacity = opacity;
        foreach (Entry entry in _entries)
        {
            if (entry.Renderer == null) continue;
            Color tint = entry.Color; tint.a *= opacity;
            entry.Working.SetColor(Tint, tint);
            entry.Renderer.SetPropertyBlock(entry.Working);
        }
    }
    public void FitSheen(RectTransform target)
    {
        if (_background == null || _camera == null || target == null) return;
        Bounds bounds = _background.bounds;
        var parent = (RectTransform)target.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, _camera.WorldToScreenPoint(bounds.min), null, out Vector2 low);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, _camera.WorldToScreenPoint(bounds.max), null, out Vector2 high);
        target.anchorMin = target.anchorMax = target.pivot = Vector2.one * .5f;
        target.anchoredPosition = (low + high) * .5f;
        target.sizeDelta = high - low;
    }
    public void Restore()
    {
        foreach (Entry entry in _entries)
            if (entry.Renderer != null) entry.Renderer.SetPropertyBlock(entry.Original.isEmpty ? null : entry.Original);
        _entries.Clear(); _opacity = 1;
    }
    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();
}
