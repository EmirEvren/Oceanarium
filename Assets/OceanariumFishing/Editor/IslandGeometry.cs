using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static class IslandGeometry
    {
        public static float HillHeight=1.65f;
        public static int Seed=1847;
        public static float Coast(float angle)=>22+2.3f*Mathf.Sin(angle*3+.4f)+1.4f*Mathf.Sin(angle*5-1)+.7f*Mathf.Cos(angle*9);
        public static float Radius(float x,float z)=>Mathf.Sqrt(x*x+z*z/.76f);
        public static float Height(float x,float z)
        {
            float ratio=Radius(x,z)/Coast(Mathf.Atan2(z/.872f,x));
            float h=Mathf.Lerp(.20f,1.5f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,.64f,ratio)));
            float inland=Mathf.Clamp01((.9f-ratio)*5);
            float north=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,10,z));
            float hills=Mathf.Exp(-((x+5)*(x+5)+(z-10)*(z-10))*.013f)+.65f*Mathf.Exp(-((x-11)*(x-11)+(z-8)*(z-8))*.035f);
            float noise=(Mathf.PerlinNoise(x*.13f+Seed*.01f,z*.13f+17)-.5f)*.27f;
            return h+inland*(north*HillHeight*hills+noise+.12f*Mathf.Sin(x*.24f)*Mathf.Cos(z*.3f));
        }
        static readonly Vector3[] cabin={new Vector3(1,0,-17),new Vector3(1,0,-10),new Vector3(-1,0,-6),new Vector3(-5,0,-3),new Vector3(-7,0,1)};
        static readonly Vector3[] garden={new Vector3(-1,0,-6),new Vector3(2,0,-4),new Vector3(5,0,0),new Vector3(9,0,3)};
        static float Distance(Vector3 p,Vector3[] path)
        {
            float d=100;
            for(int i=1;i<path.Length;i++){Vector3 a=path[i-1],b=path[i],ab=b-a;d=Mathf.Min(d,Vector3.Distance(p,a+ab*Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude)));}return d;
        }
        public static float PathDistance(float x,float z)=>Mathf.Min(Distance(new Vector3(x,0,z),cabin),Distance(new Vector3(x,0,z),garden));
        public static bool ClearForPlants(float x,float z,float margin=1.5f)=>PathDistance(x,z)>margin && new Vector2(x+8,z-3).sqrMagnitude>34 && new Vector2(x-8,z-3).sqrMagnitude>30 && !(z< -12 && Mathf.Abs(x-1)<3);
        public static Mesh Terrain()
        {
            const int segments=192,rings=80;
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
            for(int r=0;r<=rings;r++)for(int s=0;s<=segments;s++)
            {
                float a=s*Mathf.PI*2/segments,ratio=r/(float)rings,rad=Coast(a)*ratio;
                float x=Mathf.Cos(a)*rad,z=Mathf.Sin(a)*rad*.872f,h=r==rings?-.65f:Height(x,z);
                v.Add(new Vector3(x,h,z));uv.Add(new Vector2(x,z)*.1f);
                float grain=(Mathf.PerlinNoise(x*.6f+13,z*.6f+4)-.5f)*.05f;
                float grass=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.73f+grain,.86f+grain,ratio));
                float trail=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,1.1f,PathDistance(x,z)));
                c.Add(new Color(grass,trail,Mathf.Clamp01((h-2.4f)*.3f),1));
            }
            for(int r=0;r<rings;r++)for(int s=0;s<segments;s++){int a=r*(segments+1)+s,b=a+segments+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var m=new Mesh{name="OF_OrganicIsland"};m.SetVertices(v);m.SetUVs(0,uv);m.SetColors(c);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static Mesh Ocean()
        {
            const int n=200;var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
            {
                // Dense near shore, progressively wider cells toward the horizon.
                float gx=(x-n/2)/(n*.5f),gz=(z-n/2)/(n*.5f);
                float px=Mathf.Sign(gx)*(Mathf.Abs(gx)*30+Mathf.Pow(Mathf.Abs(gx),4)*170),pz=Mathf.Sign(gz)*(Mathf.Abs(gz)*30+Mathf.Pow(Mathf.Abs(gz),4)*170);
                float d=Radius(px,pz)-Coast(Mathf.Atan2(pz/.872f,px));
                v.Add(new Vector3(px,.05f,pz));c.Add(new Color(1-Mathf.SmoothStep(0,1,Mathf.Max(0,d)/20),0,0));
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;t.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});}
            var m=new Mesh{name="OF_OceanGrid"};m.SetVertices(v);m.SetColors(c);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static Mesh Leaf(float length,float width,float droop)=>CozyGeometry.Frond(length,width,droop);
        public static Mesh Path(Vector3[] points,float width)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int s=0;s<points.Length-1;s++)for(int j=0;j<20;j++)
            {
                Vector3 p=Vector3.Lerp(points[s],points[s+1],j/19f),side=Vector3.Cross((points[s+1]-points[s]).normalized,Vector3.up)*width*.5f;
                foreach(float sign in new[]{-1f,1f}){Vector3 q=p+side*sign;q.y=Height(q.x,q.z)+.035f;v.Add(q);}
                int a=v.Count-4;if(a>=0)t.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});
            }
            var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();return m;
        }
    }
}
