using UnityEngine;

/// <summary>Resolution-independent rays, a wave ring, and short-lived four-point sparkles.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ClearCelebrationGraphic : UnityEngine.UI.MaskableGraphic
{
    private float _seconds = -1;
    private static readonly Vector3[] Stars = {
        new Vector3(257,555,1.02f), new Vector3(695,640,1.12f), new Vector3(230,810,1.30f),
        new Vector3(208,745,2.72f), new Vector3(731,1042,2.87f),
        new Vector3(354,199,4.45f), new Vector3(614,301,4.82f), new Vector3(270,257,6.05f), new Vector3(659,191,6.35f)
    };
    public void SetTime(float seconds) { _seconds = seconds; SetVerticesDirty(); }
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear(); if (_seconds < 0) return;
        float move = ClearMotionMath.Cubic(ClearMotionMath.Progress(_seconds, 1.98f, 2.78f));
        float enter = ClearMotionMath.Out(ClearMotionMath.Progress(_seconds, .48f, .82f));
        Vector2 center = Point(new Vector2(470.5f, Mathf.Lerp(660, 285, move)));
        float unit = rectTransform.rect.width / 941;
        float alpha = enter * (1 - move);
        for (int i = 0; i < 12 && alpha > .001f; i++)
        {
            float angle = (i / 12f + _seconds * .012f) * Mathf.PI * 2;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Color tint = ToonSkinAssets.Cream; tint.a = alpha * .045f;
            Triangle(mesh, center, center + (direction * 420 - normal * 48) * unit,
                center + (direction * 420 + normal * 48) * unit, tint);
        }
        float wave = ClearMotionMath.Progress(_seconds, .60f, 1.10f);
        if (wave > 0 && wave < 1)
        {
            Color tint = ToonSkinAssets.Cream; tint.a = 1 - wave;
            float radius = Mathf.Lerp(170, 270, wave) * unit;
            for (int i = 0; i < 72; i++)
            {
                float a = i * Mathf.PI * 2 / 72, b = (i + 1) * Mathf.PI * 2 / 72;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Quad(mesh, center + p * radius, center + q * radius, center + q * (radius - 7 * unit), center + p * (radius - 7 * unit), tint);
            }
        }
        for (int i = 0; i < Stars.Length; i++)
        {
            Vector3 star = Stars[i]; float start = star.z;
            if (start > 4 && _seconds >= 8) start += Mathf.Floor((_seconds - start) / 4) * 4;
            float p = ClearMotionMath.Progress(_seconds, start, start + .8f);
            if (p <= 0 || p >= 1) continue;
            float size = (i % 2 == 0 ? 22 : 28) * Mathf.Sin(p * Mathf.PI) * unit;
            Color tint = ToonSkinAssets.Cream; tint.a = Mathf.Sin(p * Mathf.PI);
            Vector2 at = Point(star);
            for (int j = 0; j < 4; j++)
            {
                float a = j * Mathf.PI * .5f + p * .7f, b = a + Mathf.PI * .25f, c = a + Mathf.PI * .5f;
                Triangle(mesh, at, at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size,
                    at + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * (size * .25f), tint);
                Triangle(mesh, at, at + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * (size * .25f),
                    at + new Vector2(Mathf.Cos(c), Mathf.Sin(c)) * size, tint);
            }
        }
    }
    private Vector2 Point(Vector2 p) => new Vector2(rectTransform.rect.xMin + p.x / 941 * rectTransform.rect.width,
        rectTransform.rect.yMax - p.y / 1672 * rectTransform.rect.height);
    private void Triangle(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        int start = mesh.currentVertCount; tint *= color;
        mesh.AddVert(a, tint, Vector2.zero); mesh.AddVert(b, tint, Vector2.zero); mesh.AddVert(c, tint, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
    }
    private void Quad(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    { Triangle(mesh, a, b, c, tint); Triangle(mesh, a, c, d, tint); }
}
