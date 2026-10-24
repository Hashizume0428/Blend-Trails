using UnityEngine;

/// <summary>Two finite bursts sampled from the presentation clock, with no per-frame allocations.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ClearConfetti : UnityEngine.UI.MaskableGraphic
{
    [SerializeField, Range(12, 100)] private int _centralCount = 92;
    [SerializeField, Range(12, 80)] private int _sideCount = 42;
    [SerializeField] private bool _foreground;
    private struct Piece
    {
        public Vector2 Position, Velocity;
        public float Start, Gravity, Size, Spin, Rotation, Life;
        public Color Color;
    }
    private Piece[] _pieces;
    private float _elapsed, _epoch;
    private bool _auto;
    public bool IsPlaying { get; private set; }
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    public void Play()
    {
        var game = Object.FindFirstObjectByType<GameManager>();
        Begin(game != null ? game.Palette : StageColorPalette.ForStage(1), Time.frameCount);
        _auto = true; _epoch = Time.unscaledTime;
    }
    public void Begin(StageColorPalette palette, int seed)
    {
        var random = new System.Random(seed);
        float Next(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        Color[] colors = { StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(palette.First)),
            StagePaletteVisuals.UiColor(PuzzleRules.ToTileColor(palette.Second)),
            StagePaletteVisuals.UiColor(palette.Mixed), ToonSkinAssets.Cream, new Color32(255,217,40,255) };
        _pieces = new Piece[_centralCount + _sideCount];
        for (int i = 0; i < _pieces.Length; i++)
        {
            bool central = i < _centralCount; float side = i % 2 == 0 ? -1 : 1;
            _pieces[i] = new Piece {
                Start = central ? 1.10f : 2.45f,
                Position = new Vector2(central ? 470.5f : side < 0 ? 40 : 901, 700),
                Velocity = new Vector2(central ? Next(-520,520) : -side * Next(150,550), Next(-920,-290)),
                Gravity = Next(600,770), Size = Next(7,16), Spin = Next(-5.5f,5.5f),
                Rotation = Next(0,Mathf.PI * 2), Life = Next(2.8f,3.7f), Color = colors[i % colors.Length]
            };
        }
        _auto = false; _elapsed = 0; IsPlaying = false; SetVerticesDirty();
    }
    public void SetTime(float seconds)
    {
        bool playing = _pieces != null && seconds >= 1.10f && seconds < 6.15f;
        if (!playing && !IsPlaying) return;
        _elapsed = seconds; IsPlaying = playing; SetVerticesDirty();
    }
    public void Stop() { _auto = false; IsPlaying = false; _pieces = null; SetVerticesDirty(); }
    private void Update() { if (_auto) SetTime(Time.unscaledTime - _epoch); }
    protected override void OnDisable() { Stop(); base.OnDisable(); }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear(); if (!IsPlaying || _pieces == null) return;
        Rect area = rectTransform.rect;
        Vector2 scale = new Vector2(area.width / 941, area.height / 1672);
        for (int i = 0; i < _pieces.Length; i++)
        {
            if ((i % 3 == 0) != _foreground) continue;
            Piece piece = _pieces[i]; float dt = _elapsed - piece.Start;
            if (dt < 0 || dt > piece.Life) continue;
            Vector2 p = piece.Position + Vector2.Scale(piece.Velocity, new Vector2(.66f,1)) * dt + Vector2.up * (piece.Gravity * dt * dt * .5f);
            if (_elapsed > 2.1f && p.x > 75 && p.x < 870 && p.y > 235 && p.y < 1470) continue;
            Vector2 center = new Vector2(area.xMin + p.x * scale.x, area.yMax - p.y * scale.y);
            float angle = piece.Rotation + piece.Spin * dt;
            Vector2 x = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (piece.Size * scale.x * .5f * Mathf.Max(.25f,Mathf.Abs(Mathf.Cos(dt*5+piece.Rotation))));
            Vector2 y = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * (piece.Size * scale.x);
            Color tint = piece.Color; tint.a *= Mathf.Clamp01((piece.Life - dt) / .6f);
            int start = mesh.currentVertCount;
            mesh.AddVert(center - x - y, tint * color, Vector2.zero); mesh.AddVert(center - x + y, tint * color, Vector2.up);
            mesh.AddVert(center + x + y, tint * color, Vector2.one); mesh.AddVert(center + x - y, tint * color, Vector2.right);
            mesh.AddTriangle(start, start+1, start+2); mesh.AddTriangle(start, start+2, start+3);
        }
    }
}
