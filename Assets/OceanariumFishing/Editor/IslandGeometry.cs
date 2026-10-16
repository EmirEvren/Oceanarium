using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static class IslandGeometry
    {
        public static float Coast(float angle)=>22+2.3f*Mathf.Sin(angle*3+.4f)+1.4f*Mathf.Sin(angle*5-1)+.7f*Mathf.Cos(angle*9);
        public static float Radius(float x,float z)=>Mathf.Sqrt(x*x+z*z/0.76f);
        public static float Height(float x,float z)
        {
            float ratio=Radius(x,z)/Coast(Mathf.Atan2(z/.872f,x));
            float h=Mathf.Lerp(.32f,1.5f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,.64f,ratio)));
            return h+Mathf.Clamp01((.75f-ratio)*4)*(.16f*Mathf.Sin(x*.24f)*Mathf.Cos(z*.3f));
        }
        public static Mesh Terrain()
        {
            const int segments=160,rings=32;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int r=0;r<=rings;r++)for(int s=0;s<=segments;s++)
            {
                float a=s*Mathf.PI*2/segments,rad=Coast(a)*r/rings;
                float x=Mathf.Cos(a)*rad,z=Mathf.Sin(a)*rad*.872f;
                vertices.Add(new Vector3(x,r==rings?-.5f:Height(x,z),z));uv.Add(new Vector2(x,z)*.1f);
            }
            for(int r=0;r<rings;r++)for(int s=0;s<segments;s++)
            {int a=r*(segments+1)+s,b=a+segments+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var mesh=new Mesh{name="OF_OrganicIsland"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);
            // Separate sand and grassy interior, with coherent irregular coastline.
            var grass=new List<int>();var sand=new List<int>();
            for(int i=0;i<triangles.Count;i+=3)
            {
                Vector3 p=(vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3;
                float ratio=Radius(p.x,p.z)/Coast(Mathf.Atan2(p.z/.872f,p.x));
                var list=ratio<.77f?grass:sand;list.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
            }
            mesh.subMeshCount=2;mesh.SetTriangles(grass,0);mesh.SetTriangles(sand,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Ocean()
        {
            const int n=160;var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
            {
                float px=(x-n/2)*2.5f,pz=(z-n/2)*2.5f;
                float distance=Radius(px,pz)-Coast(Mathf.Atan2(pz/.872f,px));
                v.Add(new Vector3(px,.05f,pz));c.Add(new Color(1-Mathf.SmoothStep(0,1,Mathf.Max(0,distance)/16),0,0));
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;t.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});}
            var mesh=new Mesh{name="OF_OceanGrid"};mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Leaf(float length,float width,float droop)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<=12;i++)
            {
                float f=i/12f,w=Mathf.Sin(f*Mathf.PI)*width;
                v.Add(new Vector3(-w,-droop*f*f,length*f));v.Add(new Vector3(0,.08f-droop*f*f,length*f));v.Add(new Vector3(w,-droop*f*f,length*f));
            }
            for(int i=0;i<12;i++)for(int j=0;j<2;j++){int a=i*3+j;t.AddRange(new[]{a,a+3,a+1,a+1,a+3,a+4});}
            var mesh=new Mesh{name="OF_PalmFrond"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        public static Mesh Path(Vector3[] points,float width)
        {
            var sampled=new List<Vector3>();
            for(int segment=0;segment<points.Length-1;segment++)
            {
                int steps=Mathf.CeilToInt(Vector3.Distance(points[segment],points[segment+1])/.3f);
                for(int step=0;step<steps;step++)sampled.Add(Vector3.Lerp(points[segment],points[segment+1],step/(float)steps));
            }
            sampled.Add(points[points.Length-1]);points=sampled.ToArray();
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<points.Length;i++)
            {
                var forward=points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)];var side=Vector3.Cross(forward.normalized,Vector3.up)*width*.5f;
                foreach(float sign in new[]{-1f,1f}){var p=points[i]+side*sign;p.y=Height(p.x,p.z)+.06f;v.Add(p);}
                if(i>0){int a=(i-1)*2;t.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
            }
            var mesh=new Mesh{name="OF_WalkingPath"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
    }
}
