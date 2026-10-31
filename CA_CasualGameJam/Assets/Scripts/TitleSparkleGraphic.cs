using UnityEngine;

/// <summary>Sparse four-point accents around the logo; never participates in input.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TitleSparkleGraphic : UnityEngine.UI.MaskableGraphic
{
    private float _seconds=-1;
    private static readonly Vector4[] Stars={new Vector4(166,249,.55f,18),new Vector4(804,526,.77f,21),
        new Vector4(196,208,3.85f,14),new Vector4(792,565,4.22f,16)};
    public void SetTime(float seconds){if(_seconds==seconds)return;_seconds=seconds;SetVerticesDirty();}
    protected override void Awake(){base.Awake();raycastTarget=false;}
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();if(_seconds<0)return;
        foreach(Vector4 star in Stars)
        {
            float dt=star.z<1?_seconds-star.z:Mathf.Repeat(_seconds-star.z,6),duration=star.z<1?.65f:.75f;
            if(dt<=0||dt>=duration)continue;
            float a=Mathf.Sin(dt/duration*Mathf.PI);Color tint=ToonSkinAssets.Cream;tint.a=a*.9f;tint*=color;
            Vector2 at=new Vector2(star.x,star.y);int start=mesh.currentVertCount;
            mesh.AddVert(Point(at),tint,Vector2.zero);
            for(int i=0;i<=8;i++)
            {
                float angle=-Mathf.PI*.5f+i*Mathf.PI*.25f+dt*.45f;
                Vector2 p=at+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*star.w*a*(i%2==0?1:.2f);
                mesh.AddVert(Point(p),tint,Vector2.zero);if(i>0)mesh.AddTriangle(start,start+i,start+i+1);
            }
        }
    }
    private Vector2 Point(Vector2 p){Rect r=rectTransform.rect;return new Vector2(r.xMin+p.x/941*r.width,r.yMax-p.y/1672*r.height);}
}
