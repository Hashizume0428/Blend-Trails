using UnityEngine;

/// <summary>A crisp success badge using the current puzzle's two colors and their blend.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ToonClearBadge : UnityEngine.UI.MaskableGraphic
{
    [SerializeField] private Color _first = new Color32(255, 62, 67, 255);
    [SerializeField] private Color _second = new Color32(0, 104, 255, 255);
    [SerializeField] private Color _mixed = new Color32(138, 37, 245, 255);

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
        Capsule(mesh, left, joint, 33, ToonSkinAssets.Ink);
        Capsule(mesh, joint, right, 33, ToonSkinAssets.Ink);
        Capsule(mesh, left, joint, 22, _first);
        Capsule(mesh, joint, right, 22, _second);
        Circle(mesh, joint, 22, _mixed);
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
