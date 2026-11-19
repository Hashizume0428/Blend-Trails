using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LineRenderer))]
public class LineController : MonoBehaviour
{
    [FormerlySerializedAs("isLineRender")]
    [SerializeField] private bool _isLineRender = true;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private BaseColor _baseColor;
    private readonly List<int> _path = new List<int>();
    private bool _composited;
    public int PointCount => Renderer.positionCount;
    public Color LineColor => Renderer.startColor;
    public BaseColor BaseColor => _baseColor;
    public IReadOnlyList<int> Path => _path;
    public bool IsRenderActive => _isLineRender;
    private LineRenderer Renderer => _lineRenderer != null ? _lineRenderer : (_lineRenderer = GetComponent<LineRenderer>());

    public void SetColor(BaseColor color)
    {
        _baseColor = color;
        Renderer.startColor = Renderer.endColor = GlobalScripts.GetLineColor(color);
    }
    public void SetComposited(bool composited)
    {
        _composited = composited;
        Renderer.enabled = _isLineRender && !_composited;
    }
    public void ShowPath(IReadOnlyList<int> path, IReadOnlyList<TileController> tiles)
    {
        _path.Clear();
        for (int i = 0; i < path.Count; i++) _path.Add(path[i]);
        Renderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++) Renderer.SetPosition(i, tiles[path[i]].transform.position);
        Renderer.enabled = _isLineRender && !_composited;
    }
    public void ResetToStart(Vector3 position)
    {
        _path.Clear();
        Renderer.positionCount = 1; Renderer.SetPosition(0, position);
        Renderer.enabled = _isLineRender && !_composited;
    }
    public void SetLineRenderActive() { _isLineRender = true; Renderer.enabled = !_composited; }
    public void SetLineRenderInactive() { _isLineRender = false; Renderer.enabled = false; }
}
