using UnityEngine;

/// <summary>Resolution-independent sky and quiet cloud silhouettes, behind the live board.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ToonSkyGraphic : UnityEngine.UI.MaskableGraphic
{
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        Color bottom = new Color32(42, 185, 240, 255), top = new Color32(45, 181, 243, 255);
        vh.AddVert(new Vector2(r.xMin, r.yMin), bottom, Vector2.zero);
        vh.AddVert(new Vector2(r.xMin, r.yMax), top, Vector2.up);
        vh.AddVert(new Vector2(r.xMax, r.yMax), top, Vector2.one);
        vh.AddVert(new Vector2(r.xMax, r.yMin), bottom, Vector2.right);
        vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
        Cloud(vh, r, -.025f, .91f, .20f);
        Cloud(vh, r, .99f, .81f, .18f);
        Cloud(vh, r, .02f, .79f, .12f);
        Cloud(vh, r, 1.02f, .29f, .20f);
        Cloud(vh, r, -.035f, .23f, .18f);
        Cloud(vh, r, .98f, .05f, .19f);
    }
    private static void Cloud(UnityEngine.UI.VertexHelper vh, Rect r, float x, float y, float width)
    {
        Vector2 p = r.min + Vector2.Scale(r.size, new Vector2(x, y));
        float w = r.width * width;
        Color tint = new Color32(174, 229, 250, 255);
        Disc(vh, p + new Vector2(-w * .29f, 0), new Vector2(w * .38f, w * .22f), tint);
        Disc(vh, p + new Vector2(-w * .13f, w * .24f), new Vector2(w * .33f, w * .37f), tint);
        Disc(vh, p + new Vector2(w * .20f, w * .10f), new Vector2(w * .32f, w * .28f), tint);
    }
    private static void Disc(UnityEngine.UI.VertexHelper vh, Vector2 center, Vector2 radius, Color tint)
    {
        int first = vh.currentVertCount;
        vh.AddVert(center, tint, Vector2.zero);
        const int segments = 32;
        for (int i = 0; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments;
            vh.AddVert(center + Vector2.Scale(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), radius), tint, Vector2.zero);
            if (i > 0) vh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
