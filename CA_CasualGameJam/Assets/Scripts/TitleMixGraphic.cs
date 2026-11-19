using UnityEngine;

/// <summary>Light flowing along the approved red/yellow artwork, in its original image coordinates.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TitleMixGraphic : UnityEngine.UI.MaskableGraphic
{
    private float _seconds = -1;
    private static readonly Vector2[] Red = Route(new[] {
        new Vector2(311,297), new Vector2(360,297), new Vector2(425,297), new Vector2(470,297),
        new Vector2(470,297), new Vector2(520,297), new Vector2(537,320), new Vector2(537,370),
        new Vector2(537,370), new Vector2(537,415), new Vector2(537,486), new Vector2(537,527),
        new Vector2(537,527), new Vector2(537,588), new Vector2(586,603), new Vector2(690,603) });
    private static readonly Vector2[] Yellow = Route(new[] {
        new Vector2(1095,297), new Vector2(1040,297), new Vector2(991,297), new Vector2(947,297),
        new Vector2(947,297), new Vector2(884,297), new Vector2(854,320), new Vector2(854,376),
        new Vector2(854,376), new Vector2(854,430), new Vector2(854,483), new Vector2(854,526),
        new Vector2(854,526), new Vector2(854,585), new Vector2(806,603), new Vector2(711,603) });
    private static readonly Vector2[] Orange = { new Vector2(700,603), new Vector2(700,838) };
    private static readonly float[] RedLengths = Lengths(Red), YellowLengths = Lengths(Yellow), OrangeLengths = Lengths(Orange);
    public void SetTime(float seconds) { if (_seconds == seconds) return; _seconds = seconds; SetVerticesDirty(); }
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear(); if (_seconds < 1.25f) return;
        float cycle = Mathf.Repeat(_seconds - 1.25f, 6);
        if (cycle < .86f)
        {
            float p = Mathf.SmoothStep(0, 1, cycle / .86f);
            Light(mesh, Red, RedLengths, p, new Color(1, .714f, .69f));
            Light(mesh, Yellow, YellowLengths, p, new Color(1, .961f, .655f));
        }
        else if (cycle < 1.31f)
            Light(mesh, Orange, OrangeLengths, Mathf.SmoothStep(0,1,(cycle-.86f)/.45f), new Color(1,.824f,.584f));
        if (cycle >= 1.13f && cycle < 1.95f)
        {
            float p = (cycle - 1.13f) / .82f, alpha = Mathf.Sin(p * Mathf.PI);
            Radial(mesh, new Vector2(700,838), 190, new Color(1,.906f,.58f,alpha*.48f),true);
            Star(mesh,new Vector2(490,800),34*alpha,alpha,p*.45f);
            Star(mesh,new Vector2(910,800),34*alpha,alpha,p*.45f);
            Star(mesh,new Vector2(533,954),22*alpha,alpha,p*.45f);
            Star(mesh,new Vector2(867,954),22*alpha,alpha,p*.45f);
        }
    }
    private void Light(UnityEngine.UI.VertexHelper mesh, Vector2[] points, float[] lengths, float p, Color tint)
    {
        const int count = 12;
        for (int i=0;i<count;i++)
        {
            Vector2 a=At(points,lengths,Mathf.Clamp01(p-.14f+i*.14f/count));
            Vector2 b=At(points,lengths,Mathf.Clamp01(p-.14f+(i+1)*.14f/count));
            if ((a-b).sqrMagnitude<.001f) continue;
            Segment(mesh,a,b,42,new Color(tint.r,tint.g,tint.b,.68f));
            Segment(mesh,a,b,21,new Color(1,.976f,.851f,.65f));
        }
        Vector2 head=At(points,lengths,p);
        Radial(mesh,head,60,new Color(tint.r,tint.g,tint.b,.35f),true);
        Radial(mesh,head,18,new Color(1,.984f,.898f,.98f),false);
    }
    private Vector2 Point(Vector2 source)
    {
        Rect r=rectTransform.rect;
        float fit=Mathf.Min(r.width/1374,r.height/1145);
        return r.center+new Vector2(source.x-687,572.5f-source.y)*fit;
    }
    private void Segment(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
    {
        Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*(width*.5f);
        int start=mesh.currentVertCount; tint*=color;
        mesh.AddVert(Point(a-normal),tint,Vector2.zero);mesh.AddVert(Point(b-normal),tint,Vector2.zero);
        mesh.AddVert(Point(b+normal),tint,Vector2.zero);mesh.AddVert(Point(a+normal),tint,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
    private void Radial(UnityEngine.UI.VertexHelper mesh,Vector2 center,float radius,Color tint,bool fade)
    {
        int start=mesh.currentVertCount;tint*=color;
        mesh.AddVert(Point(center),tint,Vector2.zero);Color edge=tint;if(fade)edge.a=0;
        const int sides=32;
        for(int i=0;i<=sides;i++)
        {
            float a=i*Mathf.PI*2/sides;
            mesh.AddVert(Point(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius),edge,Vector2.zero);
            if(i>0)mesh.AddTriangle(start,start+i,start+i+1);
        }
    }
    private void Star(UnityEngine.UI.VertexHelper mesh,Vector2 at,float size,float alpha,float rotation)
    {
        Color tint=ToonSkinAssets.Cream;tint.a=alpha;tint*=color;int start=mesh.currentVertCount;
        mesh.AddVert(Point(at),tint,Vector2.zero);
        for(int i=0;i<=8;i++)
        {
            float a=-Mathf.PI*.5f+i*Mathf.PI*.25f+rotation;
            mesh.AddVert(Point(at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size*(i%2==0?1:.2f)),tint,Vector2.zero);
            if(i>0)mesh.AddTriangle(start,start+i,start+i+1);
        }
    }
    private static Vector2[] Route(Vector2[] controls)
    {
        var result=new Vector2[controls.Length/4*48];
        for(int segment=0;segment<controls.Length/4;segment++)for(int i=0;i<48;i++)
        {
            float p=i/47f,q=1-p;int k=segment*4;
            result[segment*48+i]=q*q*q*controls[k]+3*q*q*p*controls[k+1]+3*q*p*p*controls[k+2]+p*p*p*controls[k+3];
        }
        return result;
    }
    private static float[] Lengths(Vector2[] points)
    {
        var result=new float[points.Length];for(int i=1;i<points.Length;i++)result[i]=result[i-1]+Vector2.Distance(points[i-1],points[i]);return result;
    }
    private static Vector2 At(Vector2[] points,float[] lengths,float p)
    {
        float d=Mathf.Clamp01(p)*lengths[lengths.Length-1];int i=1;
        while(i<lengths.Length-1&&lengths[i]<d)i++;
        return Vector2.Lerp(points[i-1],points[i],Mathf.InverseLerp(lengths[i-1],lengths[i],d));
    }
}
