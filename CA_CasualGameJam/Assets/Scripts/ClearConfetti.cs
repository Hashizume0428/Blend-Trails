using UnityEngine;

/// <summary>A single burst of lightweight UI paper pieces, above the world-space board.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ClearConfetti : UnityEngine.UI.MaskableGraphic
{
    [SerializeField, Range(24, 180)] private int _pieceCount = 100;
    [SerializeField, Min(0.5f)] private float _duration = 2.4f;
    private struct Piece
    {
        public Vector2 Position, Velocity, Size;
        public float Spin, Rotation;
        public Color Color;
    }
    private Piece[] _pieces;
    private float _elapsed;
    public bool IsPlaying { get; private set; }

    protected override void Awake() { base.Awake(); raycastTarget = false; }
    public void Play()
    {
        var random = new System.Random(Time.frameCount);
        float Next(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        Color[] palette = { GlobalScripts.GetLineColor(TileColor.Red), GlobalScripts.GetLineColor(TileColor.Blue),
            GlobalScripts.GetLineColor(TileColor.Purple), GlobalScripts.GetLineColor(TileColor.Yellow) };
        _pieces = new Piece[_pieceCount];
        for (int i = 0; i < _pieces.Length; i++)
            _pieces[i] = new Piece
            {
                Position = new Vector2(Next(0, 1), Next(1.01f, 1.3f)),
                Velocity = new Vector2(Next(-0.18f, 0.18f), Next(-0.48f, -0.24f)),
                Size = new Vector2(Next(7, 15), Next(16, 28)),
                Spin = Next(-7, 7), Rotation = Next(0, Mathf.PI * 2), Color = palette[i % palette.Length]
            };
        _elapsed = 0; IsPlaying = true; SetVerticesDirty();
    }
    public void Stop() { IsPlaying = false; _pieces = null; SetVerticesDirty(); }
    private void Update()
    {
        if (!IsPlaying) return;
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed >= _duration) Stop();
        else SetVerticesDirty();
    }
    protected override void OnDisable() { Stop(); base.OnDisable(); }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
    {
        helper.Clear();
        if (!IsPlaying || _pieces == null) return;
        Rect area = rectTransform.rect;
        float unit = area.width / 1324;
        foreach (Piece piece in _pieces)
        {
            float t = _elapsed;
            Vector2 p = piece.Position + piece.Velocity * t + Vector2.down * (0.18f * t * t);
            Vector2 center = area.min + Vector2.Scale(p, area.size);
            float angle = piece.Rotation + piece.Spin * t;
            Vector2 x = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (piece.Size.x * unit * 0.5f);
            Vector2 y = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) *
                (piece.Size.y * unit * 0.5f * Mathf.Max(0.2f, Mathf.Abs(Mathf.Cos(t * piece.Spin))));
            Color tint = piece.Color; tint.a *= Mathf.Clamp01((_duration - t) / 0.45f);
            int index = helper.currentVertCount;
            helper.AddVert(center - x - y, tint, Vector2.zero);
            helper.AddVert(center - x + y, tint, Vector2.up);
            helper.AddVert(center + x + y, tint, Vector2.one);
            helper.AddVert(center + x - y, tint, Vector2.right);
            helper.AddTriangle(index, index + 1, index + 2); helper.AddTriangle(index, index + 2, index + 3);
        }
    }
}
