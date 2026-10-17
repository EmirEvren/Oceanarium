using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    // Surface-of-revolution and swept ellipsoid geometry; vertices are authored in rig bind space.
    public sealed class AvatarGeometry
    {
        readonly List<Vector3> vertices=new(),normals=new();readonly List<int> triangles=new();readonly List<BoneWeight> weights=new();
        public void Orb(Vector3 center,Vector3 radius,int bone,int other=-1,float joint=0,float blendWidth=.1f,int sides=24,int rings=16)
        {
            int start=vertices.Count;
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
            {
                float a=x*Mathf.PI*2/sides,b=y*Mathf.PI/rings;var unit=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));
                var p=center+Vector3.Scale(unit,radius);Add(p,new Vector3(unit.x/radius.x,unit.y/radius.y,unit.z/radius.z).normalized,bone,other,joint,blendWidth);
            }
            Grid(start,rings,sides);
        }
        public void Loft(float[] heights,float[] widths,float[] depths,Vector3 offset,int bone,int other=-1,float joint=0,float blendWidth=.1f,int sides=32,bool skirt=false)
        {
            int start=vertices.Count;
            for(int y=0;y<heights.Length;y++)for(int x=0;x<=sides;x++)
            {float a=x*Mathf.PI*2/sides;var p=offset+new Vector3(Mathf.Cos(a)*widths[y],heights[y],Mathf.Sin(a)*depths[y]);Add(p,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),bone,other,joint,blendWidth);if(skirt){float w=Mathf.InverseLerp(.8f,.3f,p.y)*.28f;float side=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.12f,p.x));weights[weights.Count-1]=new BoneWeight{boneIndex0=1,weight0=1-w,boneIndex1=12,weight1=w*(1-side),boneIndex2=15,weight2=w*side};}}
            Grid(start,heights.Length-1,sides);
        }
        public void Strand(Vector3[] points,float radius,int bone)
        {
            for(int i=0;i<points.Length-1;i++)
            {
                Vector3 delta=points[i+1]-points[i];int start=vertices.Count;var rotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
                for(int y=0;y<=8;y++)for(int x=0;x<=12;x++)
                {float a=x*Mathf.PI*2/12,b=y*Mathf.PI/8;var u=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));var p=(points[i]+points[i+1])*.5f+rotation*Vector3.Scale(u,new Vector3(radius,delta.magnitude*.5f+radius,radius));Add(p,rotation*u,bone,-1,0,1);}
                Grid(start,8,12);
            }
        }
        public void Sleeve(int side,float low,int bone)
        {
            const int sides=24;int start=vertices.Count;float[] ys={low,low+.02f,Mathf.Lerp(low,1.18f,.6f),1.18f,1.235f,1.26f};float[] radii={.099f,.108f,.112f,.118f,.092f,.015f};
            for(int y=0;y<ys.Length;y++)for(int x=0;x<=sides;x++)
            {float a=x*Mathf.PI*2/sides;float cx=side*Mathf.Lerp(.38f,.245f,Mathf.InverseLerp(.83f,1.26f,ys[y]));Add(new Vector3(cx+Mathf.Cos(a)*radii[y],ys[y],Mathf.Sin(a)*radii[y]),new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),bone,bone+1,.97f,.1f);}
            Grid(start,ys.Length-1,sides);
        }
        public void HairCurtain(float bottom,float wave,int bone)
        {
            const int rows=16,sides=32;int start=vertices.Count;
            // One flowing surface replaces disconnected tube-like locks across the back of the head.
            for(int y=0;y<=rows;y++)for(int x=0;x<=sides;x++)
            {
                float t=y/(float)rows,a=x*Mathf.PI/sides;
                float radius=.283f+Mathf.Sin(t*Mathf.PI)*.025f+wave*Mathf.Sin(t*Mathf.PI*3+a*3)*t;
                float height=Mathf.Lerp(1.79f,bottom,t)+Mathf.Pow(t,8)*Mathf.Cos(a*7)*.018f;
                var p=new Vector3(Mathf.Cos(a)*radius,height,-Mathf.Sin(a)*(radius*.86f+.018f));
                Add(p,new Vector3(Mathf.Cos(a),0,-Mathf.Sin(a)),bone,-1,0,1);
            }
            Grid(start,rows,sides);
            // Inner lining gives the open ends volume when viewed from below or from the side.
            int inner=vertices.Count;
            for(int i=0;i<(rows+1)*(sides+1);i++){var p=vertices[start+i];p.x*=.94f;p.z*=.94f;Add(p,-normals[start+i],bone,-1,0,1);}
            Grid(inner,rows,sides);
            for(int x=0;x<sides;x++){int a=start+rows*(sides+1)+x,b=inner+rows*(sides+1)+x;Tri(a,b,a+1);Tri(a+1,b,b+1);}
        }
        void Add(Vector3 p,Vector3 normal,int bone,int other,float joint,float width)
        {vertices.Add(p);normals.Add(normal);float w=other<0?0:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(joint-width,joint+width,p.y));weights.Add(new BoneWeight{boneIndex0=bone,weight0=1-w,boneIndex1=other<0?bone:other,weight1=w});}
        void Grid(int start,int rows,int sides)
        {for(int y=0;y<rows;y++)for(int x=0;x<sides;x++){int a=start+y*(sides+1)+x;Tri(a,a+1,a+sides+1);Tri(a+1,a+sides+2,a+sides+1);}}
        void Tri(int a,int b,int c){if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normals[a]+normals[b]+normals[c])<0)(b,c)=(c,b);triangles.Add(a);triangles.Add(b);triangles.Add(c);}
        public Mesh Build(Transform root,Transform[] bones,string name)
        {
            var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.boneWeights=weights.ToArray();var poses=new Matrix4x4[bones.Length];for(int i=0;i<bones.Length;i++)poses[i]=bones[i].worldToLocalMatrix*root.localToWorldMatrix;mesh.bindposes=poses;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
