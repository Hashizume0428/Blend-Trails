using UnityEngine;

/// <summary>A crisp success badge using the current puzzle's two colors and their blend.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ToonClearBadge : UnityEngine.UI.MaskableGraphic
{
    [SerializeField] private Color _first = new Color32(255, 62, 67, 255);
    [SerializeField] private Color _second = new Color32(0, 104, 255, 255);
    [SerializeField] private Color _mixed = new Color32(138, 37, 245, 255);
    private float _merge = 1, _check = 1;
    public float RevealProgress => _check;

    public void SetTime(float seconds)
    {
        float merge = ClearMotionMath.Out(ClearMotionMath.Progress(seconds, .68f, 1.02f));
        float check = ClearMotionMath.Out(ClearMotionMath.Progress(seconds, .91f, 1.20f));
        if (Mathf.Approximately(merge, _merge) && Mathf.Approximately(check, _check)) return;
        _merge = merge; _check = check; SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        var game = FindFirstObjectByType<GameManager>();
        if (game != null && game.IsInitialized) Configure(game.Palette);
    }
    public void Configure(StageColorPalette palette)
    {
        _first = StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(palette.First));
        _second = StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(palette.Second));
        _mixed = StagePaletteVisuals.UiColor(palette.Mixed);
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Circle(mesh, new Vector2(128, 120), 125, new Color32(0, 137, 221, 255));
        Circle(mesh, new Vector2(128, 131), 123, ToonSkinAssets.Ink);
        Circle(mesh, new Vector2(128, 131), 113, ToonSkinAssets.Cream);
        Vector2 left = new Vector2(66, 137), joint = new Vector2(108, 93), right = new Vector2(187, 177);
        if (_merge < 1)
        {
            Color first = _first, second = _second;
            first.a = second.a = 1 - _check;
            Circle(mesh, Vector2.Lerp(new Vector2(55, 143), joint, _merge), 22, first);
            Circle(mesh, Vector2.Lerp(new Vector2(201, 143), joint, _merge), 22, second);
        }
        float shortStroke = Mathf.Clamp01(_check * 2), longStroke = Mathf.Clamp01(_check * 2 - 1);
        Stroke(mesh, left, joint, 33, ToonSkinAssets.Ink, shortStroke);
        Stroke(mesh, joint, right, 33, ToonSkinAssets.Ink, longStroke);
        Stroke(mesh, left, joint, 22, _first, shortStroke);
        Stroke(mesh, joint, right, 22, _second, longStroke);
        if (_check > .5f) Circle(mesh, joint, 22 * ClearMotionMath.Back(ClearMotionMath.Progress(_check, .5f, .76f)), _mixed);
    }
    private void Stroke(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, float radius, Color tint, float progress)
    {
        if (progress > 0) Capsule(mesh, a, Vector2.Lerp(a, b, progress), radius, tint);
    }
    private void Capsule(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, float radius, Color tint)
    {
        Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * radius;
        int start = mesh.currentVertCount;
        Vertex(mesh, a - n, tint); Vertex(mesh, a + n, tint);
        Vertex(mesh, b + n, tint); Vertex(mesh, b - n, tint);
        mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        Circle(mesh, a, radius, tint); Circle(mesh, b, radius, tint);
    }
    private void Circle(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius, Color tint)
    {
        int start = mesh.currentVertCount;
        Vertex(mesh, center, tint);
        const int segments = 72;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            Vertex(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint);
            if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
        }
    }
    private void Vertex(UnityEngine.UI.VertexHelper mesh, Vector2 point, Color tint)
    {
        Rect r = rectTransform.rect;
        mesh.AddVert(new Vector3(r.xMin + point.x / 256 * r.width, r.yMin + point.y / 268 * r.height), tint * color, Vector2.zero);
    }
}
