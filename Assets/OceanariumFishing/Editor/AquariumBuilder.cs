using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
namespace OceanariumFishing.Editor
{
    public static partial class AquariumBuilder
    {
        const string Root="Assets/OceanariumFishing",Dir=Root+"/Aquarium";
        static Material wood,cream,teal,stone,brass,leaf,coral,glass,water;static int meshIndex;
        static Material Mat(string name,Color color,string shader="Universal Render Pipeline/Lit")
        {var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/"+name+".mat");if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat");}m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(m);return m;}
        static T Asset<T>(string name)where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(Dir+"/"+name+".asset");if(!a){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,Dir+"/"+name+".asset");}return a;}
        static GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material mat,bool collider=false,PrimitiveType type=PrimitiveType.Cube)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        static Transform Node(Transform parent,string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
        static GameObject Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material mat)
        {var go=Part(parent,name,(a+b)/2,new Vector3(width,Vector3.Distance(a,b),width),mat);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);return go;}
        static GameObject MeshPart(Transform parent,string name,Vector3[] vertices,int[] triangles,Material mat,Vector3 normal)
        {var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles;mesh.normals=Enumerable.Repeat(normal,vertices.Length).ToArray();mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up}.Take(vertices.Length).ToArray();mesh.RecalculateBounds();string path=Dir+"/Geometry_"+(meshIndex++)+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;}
        static void Quad(Transform p,string n,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material m,Vector3 normal)=>MeshPart(p,n,new[]{a,b,c,d},new[]{0,1,2,0,2,3},m,normal);
        static void Bench(Transform parent,Vector3 p)
        {var t=Node(parent,"Handcrafted bench",p);for(int i=0;i<3;i++)Part(t,"Seat slat",new Vector3(0,.5f,-.2f+i*.2f),new Vector3(1.8f,.09f,.17f),wood,true);for(int s=-1;s<=1;s+=2){Part(t,"Leg",new Vector3(s*.65f,.23f,0),new Vector3(.12f,.46f,.5f),teal);Part(t,"Back support",new Vector3(s*.65f,.8f,.3f),new Vector3(.09f,.85f,.09f),teal);}for(int i=0;i<2;i++)Part(t,"Back slat",new Vector3(0,.82f+i*.18f,.3f),new Vector3(1.8f,.13f,.09f),wood);}
        static void Plant(Transform parent,Vector3 p,bool flowers=false)
        {var t=Node(parent,"Planter",p);Part(t,"Terracotta pot",Vector3.up*.18f,new Vector3(.5f,.36f,.5f),coral,false,PrimitiveType.Cylinder);for(int i=0;i<5;i++){float a=i*6.28f/5;var tip=new Vector3(Mathf.Sin(a)*.25f,.7f,Mathf.Cos(a)*.25f);Beam(t,"Stem",new Vector3(0,.3f,0),tip,.035f,leaf);Part(t,flowers?"Blossom":"Leaf",tip,new Vector3(.28f,.12f,.18f),flowers?cream:leaf,false,PrimitiveType.Sphere);}}
        static void Lantern(Transform parent,Vector3 p)
        {var t=Node(parent,"Warm lantern",p);Part(t,"Lantern brass",Vector3.zero,new Vector3(.22f,.34f,.22f),brass);Part(t,"Warm glass",new Vector3(0,0,-.12f),new Vector3(.15f,.23f,.02f),cream);var light=t.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.79f,.51f);light.intensity=1.3f;light.range=4;light.shadows=LightShadows.None;}
        static AquariumTank Tank(Transform parent,string id,Vector3 p,AquariumDefinition d)
        {
            var root=Node(parent,id,p);var t=root.gameObject.AddComponent<AquariumTank>();t.id=id;t.definition=d;float x=d.dimensions.x/2,z=d.dimensions.z/2,y=d.dimensions.y;
            Part(root,"Stone plinth",new Vector3(0,.15f,0),new Vector3(x*2+.25f,.3f,z*2+.2f),stone,true);Part(root,"Timber cabinet",new Vector3(0,.6f,0),new Vector3(x*2,.6f,z*2),wood,true);for(int s=-1;s<=1;s+=2){Part(root,"Cabinet handle",new Vector3(s*.3f,.65f,-z-.02f),new Vector3(.13f,.04f,.04f),brass);}
            Part(root,"Sand substrate",new Vector3(0,.94f,0),new Vector3(x*2,.12f,z*2),cream);
            var walls=root.gameObject.AddComponent<BoxCollider>();walls.center=new Vector3(0,1+y/2,0);walls.size=new Vector3(x*2,y,z*2);
            Quad(root,"Front glass",new(-x,1,-z),new(x,1,-z),new(x,1+y,-z),new(-x,1+y,-z),glass,Vector3.back);
            Quad(root,"Back glass",new(x,1,z),new(-x,1,z),new(-x,1+y,z),new(x,1+y,z),glass,Vector3.forward);
            Quad(root,"Left glass",new(-x,1,z),new(-x,1,-z),new(-x,1+y,-z),new(-x,1+y,z),glass,Vector3.left);
            Quad(root,"Right glass",new(x,1,-z),new(x,1,z),new(x,1+y,z),new(x,1+y,-z),glass,Vector3.right);
            Quad(root,"Moving water surface",new(-x,1+y-.07f,-z),new(-x,1+y-.07f,z),new(x,1+y-.07f,z),new(x,1+y-.07f,-z),water,Vector3.up);
            for(int s=-1;s<=1;s+=2){Part(root,"Top rim",new Vector3(0,1+y,s*z),new Vector3(x*2+.1f,.08f,.08f),brass);Part(root,"Side rim",new Vector3(s*x,1+y,0),new Vector3(.08f,.08f,z*2),brass);for(int q=-1;q<=1;q+=2)Part(root,"Glass edge",new Vector3(s*x,1+y/2,q*z),new Vector3(.035f,y,.035f),teal);}
            t.swimVolume=Node(root,"Bounded swimming volume",new Vector3(0,1+y/2,0));t.viewPoint=Node(root,"Visitor observation point",new Vector3(0,0,-z-1));
            t.decoration=Node(root,"Coral decoration pack",new Vector3(0,1.03f,0)).gameObject;for(int i=0;i<4;i++){float a=i*1.6f;Beam(t.decoration.transform,"Coral branch",new Vector3(Mathf.Sin(a)*.2f,0,Mathf.Cos(a)*.2f),new Vector3(Mathf.Sin(a)*.3f,.3f+i*.05f,Mathf.Cos(a)*.3f),.08f,coral);}
            for(int i=0;i<3;i++){Part(root,"River stone",new Vector3(-x*.7f+i*.15f,1.07f,z*.5f),new Vector3(.22f,.16f,.19f),stone,false,PrimitiveType.Sphere);}
            t.lockedSign=Part(root,"Future tank plaque",new Vector3(0,1.5f,-z-.08f),new Vector3(.6f,.5f,.06f),wood);Part(t.lockedSign.transform,"Lock emblem",new Vector3(0,0,-.6f),new Vector3(.3f,.4f,.1f),brass);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Dir+"/Tank_"+id+".prefab");return t;
        }
        [MenuItem("Oceanarium Fishing/Build ASTRA 0.5 Aquarium")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");Directory.CreateDirectory(Dir);AssetDatabase.Refresh();meshIndex=0;
            wood=Mat("Warm cedar",new(.43f,.25f,.14f));cream=Mat("Warm ivory",new(.92f,.84f,.65f));teal=Mat("Lagoon trim",new(.16f,.4f,.39f));stone=Mat("River stone",new(.45f,.48f,.43f));brass=Mat("Antique brass",new(.65f,.48f,.22f));leaf=Mat("Garden green",new(.25f,.46f,.24f));coral=Mat("Coral accents",new(.77f,.4f,.29f));glass=Mat("Clear aquarium glass",new(.4f,.77f,.78f,.07f),"OceanariumFishing/Aquarium Glass and Water");water=Mat("Aquarium water",new(.13f,.6f,.67f,.19f),"OceanariumFishing/Aquarium Glass and Water");water.SetFloat("_Water",1);
            var tropical=Asset<AquariumDefinition>("Small tropical");tropical.id="small_tropical";tropical.habitats=new[]{AquariumHabitat.TropicalSaltwater};
            var temperate=Asset<AquariumDefinition>("Small temperate");temperate.id="small_temperate";temperate.habitats=new[]{AquariumHabitat.TemperateSaltwater};temperate.maxLength=45;
            var medium=Asset<AquariumDefinition>("Medium marine");medium.id="medium_marine";medium.size=AquariumTankSize.Medium;medium.capacity=6;medium.maxLength=75;medium.dimensions=new Vector3(3.6f,2,1.8f);medium.habitats=new[]{AquariumHabitat.TropicalSaltwater,AquariumHabitat.TemperateSaltwater};
            var large=Asset<AquariumDefinition>("Large future");large.id="large_future";large.size=AquariumTankSize.Large;large.capacity=12;large.maxLength=150;large.futureLocked=true;large.habitats=medium.habitats;
            foreach(var d in new[]{tropical,temperate,medium,large}){d.glass=glass;d.water=water;EditorUtility.SetDirty(d);}
            var species=AssetDatabase.FindAssets("t:FishSpecies",new[]{Root+"/Fishing"}).Select(g=>AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            foreach(var s in species){s.aquariumHabitat=s.id=="sardine"||s.id=="mackerel"||s.id=="sea_bream"?AquariumHabitat.TemperateSaltwater:AquariumHabitat.TropicalSaltwater;s.swimSpeed=s.id=="pufferfish"?.18f:s.id=="sardine"||s.id=="mackerel"?.4f:.28f;s.schooling=s.id=="sardine"||s.id=="mackerel";EditorUtility.SetDirty(s);}
            EditorSceneManager.OpenScene(Root+"/Scenes/OF_HomeIsland.unity");var previous=GameObject.Find("Oceanarium Museum");if(previous)UnityEngine.Object.DestroyImmediate(previous);var plot=GameObject.Find("Future aquarium garden");if(plot)plot.SetActive(false);
            var art=AssetDatabase.FindAssets("t:IslandArtSettings").Select(g=>AssetDatabase.LoadAssetAtPath<IslandArtSettings>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();if(art){IslandGeometry.HillHeight=art.hillHeight;IslandGeometry.Seed=art.seed;}
            var root=new GameObject("Oceanarium Museum");float floor=0;for(float x=2;x<=14;x+=1)for(float z=-2;z<=8;z+=1)floor=Mathf.Max(floor,IslandGeometry.Height(x,z));root.transform.position=new Vector3(8,floor+.16f,3);var r=root.transform;
            foreach(var existing in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {var p=existing.transform.position;if((existing.name.StartsWith("OF_Palm")||existing.name.StartsWith("OF_Rock"))&&((p.x>1.5f&&p.x<14.5f&&p.z> -2.5f&&p.z<8.5f)||(p.x>5.8f&&p.x<10.2f&&p.z> -11&&p.z< -2)))existing.SetActive(false);}
            var museumBounds=new Bounds(new Vector3(8,4,3),new Vector3(12.4f,10,10.4f));var pathBounds=new Bounds(new Vector3(8,3,-5),new Vector3(3.4f,8,8));
            foreach(var tree in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("OF_TropicalTree")||t.name=="OF_Palm"||t.name=="OF_Bush").ToArray())
            {var renderers=tree.GetComponentsInChildren<Renderer>();if(renderers.Any(renderer=>renderer.bounds.Intersects(museumBounds)||renderer.bounds.Intersects(pathBounds)))tree.gameObject.SetActive(false);}
            var border=GameObject.Find("Sanctuary garden border");if(border)border.SetActive(false);
            var world=root.AddComponent<AquariumWorld>();root.AddComponent<AquariumHUD>();world.economy=Asset<AquariumEconomy>("Museum economy");world.species=species;world.fishMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Characters/OF_AvatarSurface.mat");
            Part(r,"Natural stone foundation",new Vector3(0,-.45f,0),new Vector3(12,.85f,10),stone,true);
            var honey=Mat("Honey cedar",new(.48f,.29f,.17f));for(int i=0;i<30;i++)Part(r,"Cedar floorboard",new Vector3(-5.8f+i*.4f,-.035f,0),new Vector3(.38f,.08f,10),i%3==0?honey:wood,true);
            // Warm walls split around real doorway and broad front windows.
            Part(r,"Rear wall",new Vector3(0,2,5),new Vector3(12,4,.2f),cream,true);for(int s=-1;s<=1;s+=2){Part(r,"Side wall",new Vector3(s*6,2,0),new Vector3(.2f,4,10),cream,true);Part(r,"Window sill wall",new Vector3(s*3.75f,.55f,-5),new Vector3(4.5f,1.1f,.2f),teal,true);Part(r,"Window lintel",new Vector3(s*3.75f,3.55f,-5),new Vector3(4.5f,.9f,.2f),wood,true);Quad(r,"Welcoming window",new(s*3.75f-2.1f,1.1f,-5),new(s*3.75f+2.1f,1.1f,-5),new(s*3.75f+2.1f,3.1f,-5),new(s*3.75f-2.1f,3.1f,-5),glass,Vector3.back);for(int j=0;j<3;j++)Part(r,"Window mullion",new Vector3(s*3.75f-1.4f+j*1.4f,2.1f,-5.04f),new Vector3(.07f,2,.08f),wood);}
            var roofRoot=Node(r,"Cutaway roof and beams",Vector3.zero);for(int s=-1;s<=1;s+=2){Quad(roofRoot,"Pitched teal roof",new(0,6,-5.7f),new(s*6.6f,4,-5.7f),new(s*6.6f,4,5.7f),new(0,6,5.7f),teal,new Vector3(s*.3f,1,0).normalized);for(int i=0;i<20;i++)Beam(roofRoot,"Roof seam",new Vector3(0,6.02f,-5.7f+i*.6f),new Vector3(s*6.6f,4.02f,-5.7f+i*.6f),.045f,wood);}
            for(int z=-5;z<=5;z+=5){Beam(roofRoot,"Exposed oak tie",new Vector3(-6,3.9f,z),new Vector3(6,3.9f,z),.18f,wood);Beam(roofRoot,"Gable left",new Vector3(-6,4,z),new Vector3(0,6,z),.2f,wood);Beam(roofRoot,"Gable right",new Vector3(0,6,z),new Vector3(6,4,z),.2f,wood);}
            for(int side=-1;side<=1;side+=2)MeshPart(roofRoot,"Gable panel",new[]{new Vector3(-6,4,side*5),new Vector3(6,4,side*5),new Vector3(0,6,side*5)},new[]{0,1,2,0,2,1},cream,Vector3.forward*side);
            for(int side=-1;side<=1;side+=2){Part(r,"Interior wainscot",new Vector3(side*5.86f,.45f,0),new Vector3(.08f,.9f,9.8f),teal);Part(r,"Wall chair rail",new Vector3(side*5.8f,1,0),new Vector3(.1f,.1f,9.8f),wood);}
            for(int j=0;j<3;j++){var picture=Node(r,"Marine wall illustration",new Vector3(-3+j*3,2.8f,4.83f));Part(picture,"Cedar frame",Vector3.zero,new Vector3(1.1f,.7f,.1f),wood);Part(picture,"Sea canvas",new Vector3(0,0,-.06f),new Vector3(.95f,.55f,.02f),teal);Part(picture,"Fish emblem",new Vector3(.05f,0,-.09f),new Vector3(.48f,.22f,.04f),cream,false,PrimitiveType.Sphere);var fin=Part(picture,"Tail emblem",new Vector3(-.25f,0,-.09f),new Vector3(.18f,.18f,.04f),coral);fin.transform.localRotation=Quaternion.Euler(0,0,45);}
            for(int x=-6;x<=6;x+=3)for(int s=-1;s<=1;s+=2)if(x!=0||s>0)Part(r,"Oak frame post",new Vector3(x,2,s*5),new Vector3(.2f,4,.25f),wood,true);
            Part(r,"Entrance canopy",new Vector3(0,3.3f,-5.9f),new Vector3(3.4f,.18f,2),teal);for(int s=-1;s<=1;s+=2){Part(r,"Porch column",new Vector3(s*1.6f,1.65f,-6.7f),new Vector3(.16f,3.3f,.16f),wood,true);Lantern(r,new Vector3(s*1.9f,2.4f,-5.3f));}
            Part(r,"Museum sign board",new Vector3(0,4.15f,-5.3f),new Vector3(3.2f,.85f,.14f),wood);
            var sign=Node(r,"Oceanarium lettering",new Vector3(0,4.15f,-5.4f));var text=sign.gameObject.AddComponent<TextMesh>();text.text="OCEANARIUM";text.fontSize=60;text.characterSize=.075f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.91f,.68f);sign.localRotation=Quaternion.identity;
            // Sloping boardwalk connects existing terrain to raised museum floor.
            float outside=IslandGeometry.Height(8,-6)-root.transform.position.y;var ramp=Part(r,"Accessible wooden boardwalk",new Vector3(0,outside/2-.06f,-7),new Vector3(2.8f,.12f,Mathf.Sqrt(16+outside*outside)),wood,true);ramp.transform.localRotation=Quaternion.Euler(Mathf.Atan2(outside,4)*Mathf.Rad2Deg,0,0);
            world.entrance=Node(r,"Entrance waypoint",new Vector3(0,0,-5));world.arrival=Node(r,"Visitor arrival",new Vector3(0,outside,-9));world.donation=Node(r,"Reception approach",new Vector3(2,0,-3.4f));
            var reception=Part(r,"Reception donation counter",new Vector3(3.5f,.65f,-3.4f),new Vector3(1.4f,1.3f,1),wood,true);reception.AddComponent<AquariumReception>().world=world;Part(reception.transform,"Donation box",new Vector3(0,.68f,0),new Vector3(.28f,.22f,.4f),brass);Part(reception.transform,"Coin slot",new Vector3(0,.8f,0),new Vector3(.18f,.01f,.035f),teal);
            Tank(r,"reef",new Vector3(-3.7f,0,-1.5f),tropical);Tank(r,"coast",new Vector3(3.7f,0,-.1f),temperate);Tank(r,"lagoon",new Vector3(-3.7f,0,2.4f),tropical);Tank(r,"gallery",new Vector3(2.8f,0,3.4f),medium);
            Bench(r,new Vector3(-4.2f,0,-4.2f));for(int s=-1;s<=1;s+=2){Plant(r,new Vector3(s*5.3f,0,-5.8f),true);Plant(r,new Vector3(s*5.5f,0,4.4f));Lantern(r,new Vector3(s*5.5f,3.4f,1));}
            world.comfortVisual=Node(r,"Comfort upgrade",Vector3.zero).gameObject;Bench(world.comfortVisual.transform,new Vector3(0,0,3.8f));world.signageVisual=Node(r,"Signage upgrade",Vector3.zero).gameObject;for(int s=-1;s<=1;s+=2)Part(world.signageVisual.transform,"Coral banner",new Vector3(s*2,2.8f,-5.3f),new Vector3(.6f,1,.06f),coral);
            world.decorVisual=Node(r,"Garden upgrade",Vector3.zero).gameObject;for(int i=0;i<4;i++)Plant(world.decorVisual.transform,new Vector3(-4.8f+i*3.2f,0,-6.1f),true);
            for(int s=-1;s<=1;s+=2)for(int i=0;i<5;i++){Part(r,"Garden fence picket",new Vector3(s*(2.3f+i*.65f),.45f,-6.6f),new Vector3(.12f,.9f,.1f),wood);if(i<4)Part(r,"Garden fence rail",new Vector3(s*(2.65f+i*.65f),.6f,-6.6f),new Vector3(.65f,.09f,.09f),wood);}
            DressShopfront(r,roofRoot,outside);
            teal.SetInt("_Cull",0);world.roof=roofRoot.GetComponentsInChildren<Renderer>();world.comfortVisual.SetActive(false);world.signageVisual.SetActive(false);world.decorVisual.SetActive(false);
            Localize();EditorUtility.SetDirty(world.economy);PrefabUtility.SaveAsPrefabAsset(root,Dir+"/OceanariumMuseum.prefab");EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("ASTRA 0.5 aquarium built");
        }
        public static void Localize()=>AquariumLocalization.Apply();
    }
}
