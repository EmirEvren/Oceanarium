using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace OceanariumFishing.Editor
{
    public static partial class AquariumBuildGenerator
    {
        static GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool collider=false,PrimitiveType shape=PrimitiveType.Cube)
        {var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        static void Beam(Transform t,Vector3 a,Vector3 b,float width,Material m){var g=Part(t,"Crafted rail",(a+b)/2,new(width,Vector3.Distance(a,b),width),m);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static GameObject Mesh(Transform parent,string label,List<Vector3> vertices,List<int> triangles,Material material)
        {var mesh=new Mesh{name=assetId+"_mesh_"+meshIndex};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x,v.z)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();var path=Dir+"/"+assetId+"_mesh_"+(meshIndex++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);var g=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=material;return g;}
        static void Extrude(Transform p,string name,Vector2[] polygon,float bottom,float top,Material m,bool sides=true,bool cap=true)
        {var v=new List<Vector3>();var tri=new List<int>();if(sides)for(int n=0;n<polygon.Length;n++){var a=polygon[n];var b=polygon[(n+1)%polygon.Length];int s=v.Count;v.AddRange(new[]{new Vector3(a.x,bottom,a.y),new Vector3(b.x,bottom,b.y),new Vector3(b.x,top,b.y),new Vector3(a.x,top,a.y)});tri.AddRange(new[]{s,s+2,s+1,s,s+3,s+2});}if(cap){int s=v.Count;v.Add(new Vector3(polygon.Average(a=>a.x),top,polygon.Average(a=>a.y)));v.AddRange(polygon.Select(a=>new Vector3(a.x,top,a.y)));for(int n=0;n<polygon.Length;n++)tri.AddRange(new[]{s,s+1+(n+1)%polygon.Length,s+1+n});}Mesh(p,name,v,tri,m);}
        static Vector2[] Outline(int shape,Vector3 d)
        {
            float x=d.x/2,z=d.z/2;
            if(shape==3||shape==5)return Enumerable.Range(0,shape==3?32:8).Select(n=>new Vector2(Mathf.Sin(n*Mathf.PI*2/(shape==3?32:8))*x,Mathf.Cos(n*Mathf.PI*2/(shape==3?32:8))*z)).ToArray();
            if(shape==4)return new[]{new Vector2(-x,-z),new Vector2(x,-z),new Vector2(x,z)};
            if(shape==7){var p=new List<Vector2>{new(-x,z),new(x,z),new(x,-z*.55f)};for(int n=1;n<=16;n++){float a=n*Mathf.PI/16;p.Add(new Vector2(Mathf.Cos(a)*x,-z*.55f-Mathf.Sin(a)*z*.45f));}return p.ToArray();}
            if(shape==2)return new[]{new Vector2(-x+.25f,-z),new Vector2(x-.25f,-z),new Vector2(x,-z+.25f),new Vector2(x,z-.25f),new Vector2(x-.25f,z),new Vector2(-x+.25f,z),new Vector2(-x,z-.25f),new Vector2(-x,-z+.25f)};
            return new[]{new Vector2(-x,-z),new Vector2(x,-z),new Vector2(x,z),new Vector2(-x,z)};
        }
        static void Tank(Transform root,int shape,Vector3 dims,AquariumDefinition definition)
        {
            var t=root.gameObject.AddComponent<AquariumTank>();t.id="catalog";t.definition=definition;float baseY=shape==6?1.6f:.85f;var poly=Outline(shape,dims);
            Extrude(root,"Sculpted cabinet",poly,.08f,baseY-.08f,wood);Extrude(root,"Brass lower rim",poly,baseY-.08f,baseY,metal);
            Extrude(root,"Clear shaped glass",poly,baseY,baseY+dims.y,glass,true,false);Extrude(root,"Water surface",poly,baseY+dims.y-.06f,baseY+dims.y-.06f,water,false,true);
            for(int n=0;n<poly.Length;n++){var a=poly[n];var b=poly[(n+1)%poly.Length];Beam(root,new Vector3(a.x,baseY+dims.y,a.y),new Vector3(b.x,baseY+dims.y,b.y),.055f,metal);}
            if(shape==0||shape==1||shape==2||shape==7)for(int n=-1;n<=1;n+=2)Part(root,"Cabinet door pull",new Vector3(n*.25f,baseY*.5f,-dims.z/2-.025f),new(.13f,.035f,.035f),metal);
            if(shape==6){for(int s=-1;s<=1;s+=2)Beam(root,new Vector3(s*.7f,baseY-.3f,dims.z/2),new Vector3(s*.7f,baseY,-dims.z/2),.08f,metal);}
            if(shape==5)Extrude(root,"Crown",poly,baseY+dims.y,baseY+dims.y+.16f,wood);
            var volume=new GameObject("Bounded swimming volume").transform;volume.SetParent(root,false);volume.localPosition=new Vector3(shape==4?.36f:0,baseY+dims.y/2,shape==4?-.36f:0);t.swimVolume=volume;
            var vp=new GameObject("Visitor observation point").transform;vp.SetParent(root,false);vp.localPosition=new Vector3(0,0,-dims.z/2-.825f);t.viewPoint=vp;
            var box=root.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,(baseY+dims.y)/2,0);box.size=new Vector3(dims.x,baseY+dims.y,dims.z);
            var light=new GameObject("Soft underwater glow",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root,false);light.transform.localPosition=new Vector3(0,baseY+dims.y-.15f,0);light.type=LightType.Point;light.range=2;light.intensity=.45f;light.color=new Color(.45f,.8f,.85f);light.shadows=LightShadows.None;
        }
        static void Leaf(Transform p,Vector3 start,Vector3 end,float width,Material mat)
        {var dir=end-start;var side=Vector3.Cross(dir.normalized,Vector3.forward);if(side.sqrMagnitude<.1f)side=Vector3.right;side=side.normalized*width;var mid=(start+end)/2+Vector3.forward*.025f;var v=new List<Vector3>{start,mid-side,end,mid+side,mid+Vector3.up*.025f};var tri=new List<int>{0,1,4,1,2,4,2,3,4,3,0,4};int count=v.Count;v.AddRange(v.ToArray());tri.AddRange(new[]{5,9,6,6,9,7,7,9,8,8,9,5});Mesh(p,"Curved aquatic leaf",v,tri,mat);}
        static void Plant(Transform root,int kind,Vector3 size)
        {
            var green=kind==7?coral:leaf;int stems=kind==1?13:kind==5?8:kind==9?4:6;
            for(int n=0;n<stems;n++){float a=n*2.399f;float height=size.y*(.65f+.35f*Mathf.Sin(n*1.7f+1));Vector3 baseP=new(Mathf.Sin(a)*size.x*.2f,0,Mathf.Cos(a)*size.z*.2f);Vector3 tip=baseP+new Vector3(Mathf.Sin(a)*size.x*.22f,height,Mathf.Cos(a)*size.z*.22f);
                if(kind==1){Part(root,"Moss cushion",baseP+Vector3.up*height*.15f,new Vector3(.08f,height*.3f,.08f),green,false,PrimitiveType.Sphere);continue;}
                if(kind==3||kind==8){Leaf(root,baseP,tip,.022f,green);continue;}
                Beam(root,baseP,tip,.012f,green);int leaves=kind==5?7:kind==9?5:3;
                for(int j=1;j<=leaves;j++){float f=(float)j/(leaves+1);var b=Vector3.Lerp(baseP,tip,f);for(int s=-1;s<=1;s+=2){var end=b+new Vector3(Mathf.Cos(a)*s*size.x*.35f,height*.18f,Mathf.Sin(a)*s*size.z*.35f);Leaf(root,b,end,kind==5?.012f:kind==0?.047f:kind==9?.033f:.026f,green);}}
            }
        }
        static void Decoration(Transform t,int n)
        {
            if(n==0){for(int j=0;j<5;j++){float x=(j-2)*.15f;Beam(t,new(x,0,0),new(x*.8f,.8f,0),.09f,coral);for(int s=-1;s<=1;s+=2)Beam(t,new(x*.9f,.4f,0),new(x+s*.16f,.65f,.1f*s),.06f,coral);}}
            else if(n==1){Part(t,"Brain dome",new(0,.35f,0),new(1,.7f,.8f),coral,false,PrimitiveType.Sphere);for(int j=0;j<8;j++)for(int k=0;k<5;k++)Part(t,"Brain folds",new((j-3.5f)*.11f,.53f+Mathf.Sin(j)*.06f,(k-2)*.13f),new(.1f,.1f,.2f),cream,false,PrimitiveType.Sphere);}
            else if(n==2){for(int j=0;j<4;j++){var p=new Vector3(Mathf.Sin(j*2)*.3f,.15f+j*.14f,Mathf.Cos(j*2)*.2f);Beam(t,new(p.x,0,p.z),p,.09f,cream);Part(t,"Mushroom cap",p,new(.6f,.15f,.5f),coral,false,PrimitiveType.Sphere);}}
            else if(n==3){for(int j=0;j<14;j++){float a=j*2.4f;Beam(t,new(Mathf.Sin(a)*.18f,0,Mathf.Cos(a)*.18f),new(Mathf.Sin(a)*.4f,.5f+Mathf.Cos(j)*.15f,Mathf.Cos(a)*.4f),.075f,coral);}}
            else if(n==4||n==5){for(int j=0;j<4;j++){var p=new Vector3(Mathf.Sin(j*2)*.2f,.23f,Mathf.Cos(j*2)*.15f);var rock=Part(t,"Faceted reef stone",p,new(.65f,.55f,.55f),n==4?cream:wood,false,PrimitiveType.Sphere);rock.transform.localRotation=Quaternion.Euler(j*23,17*j,12);}}
            else if(n==6){Beam(t,new(-.5f,.12f,0),new(.5f,.45f,0),.18f,wood);Beam(t,new(0,.28f,0),new(.3f,.65f,.2f),.1f,wood);Beam(t,new(-.2f,.22f,0),new(-.35f,.35f,-.3f),.07f,wood);}
            else if(n==7){for(int j=0;j<7;j++){float x=(j-3)*.13f;Part(t,"Broken hull plank",new(x,.16f,0),new(.11f,.25f,.65f*(1-Mathf.Abs(x)*.7f)),wood); }Beam(t,Vector3.zero,new(.1f,.9f,0),.055f,wood);var sail=Part(t,"Tattered sail",new(.15f,.65f,0),new(.5f,.4f,.025f),cream);sail.transform.localRotation=Quaternion.Euler(0,0,-10);}
            else if(n==8){Part(t,"Chest box",new(0,.2f,0),new(.85f,.4f,.6f),wood);var lid=Part(t,"Open treasure lid",new(0,.5f,.15f),new(.85f,.13f,.6f),wood);lid.transform.localRotation=Quaternion.Euler(-35,0,0);for(int j=-1;j<=1;j++)Part(t,"Gold coin",new(j*.2f,.45f,-.1f),new(.2f,.06f,.2f),metal,false,PrimitiveType.Cylinder);}
            else if(n==9){Beam(t,new(0,.05f,0),new(0,.85f,0),.1f,metal);Beam(t,new(-.4f,.6f,0),new(.4f,.6f,0),.08f,metal);for(int s=-1;s<=1;s+=2){Beam(t,new(0,.05f,0),new(s*.4f,.2f,0),.1f,metal);Beam(t,new(s*.4f,.2f,0),new(s*.43f,.4f,0),.09f,metal);}}
            else if(n==10){for(int j=0;j<5;j++){float a=j*2.4f;var shell=Part(t,"Ridged scallop",new(Mathf.Sin(a)*.25f,.07f,Mathf.Cos(a)*.25f),new(.4f,.14f,.5f),j%2==0?cream:coral,false,PrimitiveType.Sphere);shell.transform.localRotation=Quaternion.Euler(0,j*35,15);}}
            else{Part(t,"Bubble diffuser",new(0,.08f,0),new(.4f,.16f,.4f),metal,false,PrimitiveType.Cylinder);for(int j=0;j<5;j++)Part(t,"Rising pearl bubble",new(Mathf.Sin(j)*.12f,.22f+j*.14f,0),Vector3.one*(.17f-j*.02f),glass,false,PrimitiveType.Sphere);t.gameObject.AddComponent<AquariumSway>();}
        }
        static void Substrate(Transform p,int kind,Material mat)
        {
            var v=new List<Vector3>();var tri=new List<int>();int count=16;for(int z=0;z<=count;z++)for(int x=0;x<=count;x++)v.Add(new Vector3(x/(float)count-.5f,Mathf.PerlinNoise(x*.7f+kind,z*.7f)*.035f,z/(float)count-.5f));for(int z=0;z<count;z++)for(int x=0;x<count;x++){int n=z*(count+1)+x;tri.AddRange(new[]{n,n+count+1,n+1,n+1,n+count+1,n+count+2});}Mesh(p,"Rippled substrate",v,tri,mat);
            for(int j=0;j<(kind==4||kind==5?65:30);j++){float x=Mathf.Repeat(j*.618f,1)-.5f,z=Mathf.Repeat(j*.317f,1)-.5f;float r=kind==5?.055f:kind==4?.035f:.013f;Part(p,kind==5?"Rounded dark pebble":"Individual substrate grain",new(x,.025f,z),new(r,r*.6f,r*.8f),mat,false,kind==4?PrimitiveType.Cube:PrimitiveType.Sphere);}
        }
        static void Furniture(Transform t,int n)
        {
            if(n<4){float width=n<2?1.4f:.6f;for(int s=-1;s<=1;s+=2)for(int z=-1;z<=1;z+=2)Part(t,"Tapered furniture leg",new(s*(width/2-.08f),.22f,z*.21f),new(.09f,.44f,.09f),wood);Part(t,"Seat or table top",new(0,.46f,0),new(width,.12f,.56f),wood);if(n<3){for(int j=0;j<3;j++)Part(t,"Backrest slat",new(0,.7f+j*.12f,.23f),new(width,.085f,.08f),wood);if(n==1){Part(t,"Soft seat cushion",new(0,.58f,0),new(1.35f,.18f,.52f),coral,false,PrimitiveType.Cube);for(int s=-1;s<=1;s+=2)Part(t,"Rolled sofa arm",new(s*.66f,.68f,0),new(.18f,.28f,.6f),cream,false,PrimitiveType.Capsule);}}}
            else if(n==4||n==5){float y=n==5?1.8f:0;Part(t,"Glazed planter",new(0,y+.2f,0),new(.45f,.4f,.45f),coral,false,PrimitiveType.Cylinder);if(n==4){Beam(t,new(0,.3f,0),new(0,1.5f,0),.065f,wood);for(int j=0;j<8;j++){float a=j*.785f;Leaf(t,new(0,1.5f,0),new(Mathf.Sin(a)*.3f,1.65f,Mathf.Cos(a)*.3f),.08f,leaf);}}else{for(int j=0;j<5;j++)Leaf(t,new(0,y+.35f,0),new(Mathf.Sin(j)*.25f,y-.4f,Mathf.Cos(j)*.2f),.07f,leaf);for(int s=-1;s<=1;s+=2)Beam(t,new(s*.2f,y+.25f,0),new(0,y+.9f,0),.015f,metal);}}
            else if(n==6){Part(t,"Wall shelf",new(0,1.6f,0),new(.6f,.09f,.3f),wood);for(int j=0;j<4;j++)Part(t,"Colorful book",new(-.2f+j*.1f,1.79f,0),new(.075f,.3f,.17f),j%2==0?coral:cream);}
            else if(n==7||n==14){Part(t,"Pedestal foot",new(0,.08f,0),new(.6f,.16f,.6f),metal);Part(t,"Fluted column",new(0,.5f,0),new(.4f,.85f,.4f),cream,false,PrimitiveType.Cylinder);Part(t,"Display top",new(0,.95f,0),new(.65f,.08f,.6f),wood);if(n==14)Part(t,"Pearl shell sculpture",new(0,1.12f,0),new(.5f,.25f,.35f),coral,false,PrimitiveType.Sphere);}
            else if(n==8){Part(t,"Cabinet carcass",new(0,.55f,0),new(.62f,1.1f,.55f),wood);for(int j=0;j<3;j++){Part(t,"Drawer face",new(0,.22f+j*.32f,-.29f),new(.56f,.28f,.04f),cream);Part(t,"Brass drawer knob",new(0,.22f+j*.32f,-.33f),Vector3.one*.06f,metal,false,PrimitiveType.Sphere);}}
            else if(n==9||n==10){float y=n==10?1.8f:.5f;Part(t,"Lantern base",new(0,y-.2f,0),new(.35f,.08f,.35f),metal);Part(t,"Lantern glass",new(0,y,0),new(.28f,.38f,.28f),cream);for(int s=-1;s<=1;s+=2)for(int z=-1;z<=1;z+=2)Beam(t,new(s*.16f,y-.22f,z*.16f),new(s*.16f,y+.22f,z*.16f),.025f,metal);Part(t,"Lantern roof",new(0,y+.25f,0),new(.4f,.12f,.4f),wood);var l=new GameObject("Amber illumination",typeof(Light)).GetComponent<Light>();l.transform.SetParent(t,false);l.transform.localPosition=new(0,y,0);l.type=LightType.Point;l.range=3;l.intensity=.65f;l.color=new(1,.78f,.47f);}
            else if(n==11||n==12){float y=n==12?1.8f:1; if(n==11)Beam(t,Vector3.zero,Vector3.up*y,.08f,wood);Part(t,"Carved picture frame",new(0,y,0),new(.62f,.44f,.07f),metal);Part(t,"Ocean illustration",new(0,y,-.045f),new(.54f,.36f,.025f),leaf);Part(t,"Fish silhouette",new(.03f,y,-.065f),new(.3f,.14f,.03f),cream,false,PrimitiveType.Sphere);var tail=Part(t,"Fish tail",new(-.17f,y,-.065f),new(.14f,.14f,.03f),coral);tail.transform.localRotation=Quaternion.Euler(0,0,45);}
            else if(n==13){Part(t,"Woven rug",new(0,.015f,0),new(1.6f,.025f,1),coral);for(int j=0;j<12;j++)for(int s=-1;s<=1;s+=2)Part(t,"Rug fringe",new(-.72f+j*.13f,.02f,s*.52f),new(.035f,.02f,.12f),cream);for(int s=-1;s<=1;s+=2)Part(t,"Woven border",new(s*.7f,.03f,0),new(.07f,.01f,.9f),cream);}
            if(n!=5&&n!=6&&n!=10&&n!=12&&n!=13){var c=t.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.4f,0);c.size=new Vector3(n<2?1.5f:.65f,.8f,.6f);}
        }
        static void Hall(Transform t)
        {
            Part(t,"Floor platform",new(0,-.18f,0),new(12,.35f,8),wood,true);for(int s=-1;s<=1;s+=2){Part(t,"Interior gallery wall",new(s*5.9f,2,0),new(.2f,4,8),cream,true);for(int z=-3;z<=3;z+=3){Part(t,"Gallery column",new(s*5.7f,2,z),new(.2f,4,.2f),wood);var l=new GameObject("Gallery soft light",typeof(Light)).GetComponent<Light>();l.transform.SetParent(t,false);l.transform.localPosition=new(s*4.8f,3,z);l.type=LightType.Point;l.range=6;l.intensity=1;l.color=new(1,.85f,.68f);}}
            Part(t,"End wall",new(0,2,4),new(12,4,.2f),cream,true);for(int s=-1;s<=1;s+=2)for(int z=-3;z<=3;z+=6)Part(t,"Foundation pier",new(s*5,-2,z),new(.5f,4,.5f),wood);
            // High rafters leave third-person editing and exhibits visible.
            for(int z=-3;z<=3;z+=3)Beam(t,new(-5.9f,4,z),new(5.9f,4,z),.16f,wood);
        }
        static void Thumbnail(AquariumBuildItem item,GameObject model)
        {
            var old=model.transform.position;model.transform.position=new Vector3(0,-1000,0);var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var cam=new GameObject("Catalog thumbnail camera",typeof(Camera)).GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new(.88f,.84f,.72f);cam.orthographic=true;cam.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)*.8f;cam.transform.position=bounds.center+new Vector3(3,2.3f,-4).normalized*10;cam.transform.LookAt(bounds.center);cam.nearClipPlane=.1f;cam.farClipPlane=30;
            var light=new GameObject("Thumbnail light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);
            var rt=RenderTexture.GetTemporary(160,120,24);cam.targetTexture=rt;cam.Render();var oldRT=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(160,120,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,160,120),0,0);image.Apply();var path=Dir+"/"+item.id+"_preview.png";File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=oldRT;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(cam.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);AssetDatabase.ImportAsset(path);item.thumbnail=AssetDatabase.LoadAssetAtPath<Texture2D>(path);model.transform.position=old;
        }
    }
}
