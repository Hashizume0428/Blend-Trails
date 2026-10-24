using UnityEngine;

/// <summary>A diagonal highlight clipped geometrically to a rounded rectangle.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ClearSheenGraphic : UnityEngine.UI.MaskableGraphic
{
    [SerializeField] private float _inset = 30, _radius = 60;
    private readonly Vector2[] _outline = new Vector2[48], _a = new Vector2[56], _b = new Vector2[64];
    private float _progress = -1;
    public void SetProgress(float progress)
    {
        progress = progress > 0 && progress < 1 ? progress : -1;
        if (Mathf.Approximately(progress, _progress)) return;
        _progress = progress; SetVerticesDirty();
    }
    public void Configure(float inset, float radius) { _inset = inset; _radius = radius; }
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear(); if (_progress < 0) return;
        Rect r = rectTransform.rect;
        float scale = r.width / 710, inset = _inset * scale;
        r = Rect.MinMaxRect(r.xMin + inset, r.yMin + inset, r.xMax - inset, r.yMax - inset);
        float radius = Mathf.Min(_radius * scale, r.height * .5f);
        int count = 0;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center = new Vector2(corner == 0 || corner == 3 ? r.xMax - radius : r.xMin + radius,
                corner < 2 ? r.yMax - radius : r.yMin + radius);
            for (int i = 0; i <= 8; i++)
            {
                float angle = (corner * 90 + i * 90f / 8) * Mathf.Deg2Rad;
                _outline[count++] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
        }
        float beam = Mathf.Lerp(r.xMin - r.height, r.xMax + r.height, _progress), width = r.width * .13f;
        Half(mesh, count, beam - width, beam, beam, width);
        Half(mesh, count, beam, beam + width, beam, width);
    }
    private void Half(UnityEngine.UI.VertexHelper mesh, int count, float min, float max, float beam, float width)
    {
        count = Clip(_outline, count, _a, min, true);
        count = Clip(_a, count, _b, max, false);
        if (count < 3) return;
        int start = mesh.currentVertCount;
        for (int i = 0; i < count; i++)
        {
            Color tint = Color.white; tint.a = .4f * (1 - Mathf.Clamp01(Mathf.Abs(Project(_b[i]) - beam) / width));
            mesh.AddVert(_b[i], tint * color, Vector2.zero);
            if (i >= 2) mesh.AddTriangle(start, start + i - 1, start + i);
        }
    }
    private static float Project(Vector2 p) => p.x - p.y * .4f;
    private static int Clip(Vector2[] input, int count, Vector2[] output, float bound, bool greater)
    {
        if (count == 0) return 0;
        int n = 0; Vector2 previous = input[count - 1]; float previousD = (Project(previous) - bound) * (greater ? 1 : -1);
        for (int i = 0; i < count; i++)
        {
            Vector2 current = input[i]; float d = (Project(current) - bound) * (greater ? 1 : -1);
            if ((d >= 0) != (previousD >= 0)) output[n++] = Vector2.LerpUnclamped(previous, current, previousD / (previousD - d));
            if (d >= 0) output[n++] = current;
            previous = current; previousD = d;
        }
        return n;
    }
}
