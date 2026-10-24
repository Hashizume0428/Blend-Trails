using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Draws each shared edge and junction once, using the union of its line colors.</summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BoardLineRenderer : MonoBehaviour
{
    private readonly Dictionary<Vector2Int, TileColor> _edges = new Dictionary<Vector2Int, TileColor>();
    private readonly Dictionary<int, TileColor> _nodes = new Dictionary<int, TileColor>();
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Color32> _colors = new List<Color32>();
    private readonly List<Vector2> _uvs = new List<Vector2>();
    private readonly List<int> _triangles = new List<int>();
    private Mesh _mesh;
    [SerializeField] private float _width = 0.24f;

    public TileColor GetEdgeColor(int from, int to) =>
        _edges.TryGetValue(EdgeKey(from, to), out TileColor color) ? color : TileColor.None;
    public TileColor GetNodeColor(int tileIndex) =>
        _nodes.TryGetValue(tileIndex, out TileColor color) ? color : TileColor.None;
    private static Vector2Int EdgeKey(int a, int b) => new Vector2Int(Mathf.Min(a, b), Mathf.Max(a, b));

    public void CopyStyleFrom(LineRenderer source)
    {
        var renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        _width = source.widthMultiplier;
    }

    public void Rebuild(IReadOnlyList<TileController> tiles, IEnumerable<TileController> starts)
    {
        _edges.Clear();
        _nodes.Clear();
        foreach (TileController start in starts)
        {
            LineController line = start.Line;
            IReadOnlyList<int> path = line.Path;
            if (!line.IsRenderActive || path.Count < 2) continue;
            TileColor color = PuzzleRules.ToTileColor(line.BaseColor);
            for (int i = 0; i < path.Count; i++)
            {
                int tile = path[i];
                _nodes[tile] = GetNodeColor(tile) | color;
                if (i == 0) continue;
                Vector2Int edge = EdgeKey(path[i - 1], tile);
                _edges[edge] = GetEdgeColor(edge.x, edge.y) | color;
            }
        }

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "Puzzle line geometry" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }
        _vertices.Clear(); _colors.Clear(); _uvs.Clear(); _triangles.Clear();
        float radius = _width * (tiles.Count > 0 ? tiles[0].transform.lossyScale.x : 1f) * 0.5f;
        foreach (var pair in _edges)
        {
            Vector3 from = tiles[pair.Key.x].transform.position;
            Vector3 to = tiles[pair.Key.y].transform.position;
            Vector3 direction = (to - from).normalized;
            Vector3 normal = new Vector3(-direction.y, direction.x, 0) * radius;
            AddQuad(from + normal, to + normal, to - normal, from - normal,
                GlobalScripts.GetLineColor(pair.Value));
        }
        // Junctions cover the strips' intersections, so no last-drawn primary color wins.
        foreach (var pair in _nodes)
        {
            Vector3 center = tiles[pair.Key].transform.position;
            Color color = GlobalScripts.GetLineColor(pair.Value);
            if (pair.Value == TileColor.Red || pair.Value == TileColor.Blue || pair.Value == TileColor.Yellow)
                AddDisc(center, radius, color);
            else
            {
                Vector3 x = Vector3.right * radius, y = Vector3.up * radius;
                AddQuad(center - x + y, center + x + y, center + x - y, center - x - y, color);
            }
        }
        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors);
        _mesh.SetUVs(0, _uvs);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
    }

    private void AddVertex(Vector3 position, Color color)
    {
        _vertices.Add(transform.InverseTransformPoint(position));
        // Mesh vertex colors are shader inputs; match SpriteRenderer/LineRenderer's sRGB palette.
        _colors.Add(QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);
        _uvs.Add(new Vector2(0.5f, 0.5f));
    }
    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
    {
        int first = _vertices.Count;
        AddVertex(a, color); AddVertex(b, color); AddVertex(c, color); AddVertex(d, color);
        _triangles.Add(first); _triangles.Add(first + 1); _triangles.Add(first + 2);
        _triangles.Add(first); _triangles.Add(first + 2); _triangles.Add(first + 3);
    }
    private void AddDisc(Vector3 center, float radius, Color color)
    {
        const int sides = 12;
        int first = _vertices.Count;
        AddVertex(center, color);
        for (int i = 0; i <= sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides;
            AddVertex(center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius, color);
            if (i == 0) continue;
            _triangles.Add(first); _triangles.Add(first + i); _triangles.Add(first + i + 1);
        }
    }
    private void OnDestroy()
    {
        if (_mesh == null) return;
        if (Application.isPlaying) Destroy(_mesh);
        else DestroyImmediate(_mesh);
    }
}
