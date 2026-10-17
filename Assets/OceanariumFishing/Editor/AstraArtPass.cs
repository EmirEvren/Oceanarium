using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace OceanariumFishing.Editor
{
 public static partial class AstraArtPass
 {
  const string R=FoundationBuilder.Root;
  static Mesh box,orb,stone,frond,pole;static int meshId;static IslandArtSettings settings;
  static readonly Dictionary<string,Material> materials=new();
  static Material M(string key)=>materials.TryGetValue(key,out var m)?m:(materials[key]=AssetDatabase.LoadAssetAtPath<Material>(R+"/Materials/OF_"+key+".mat"));
  static Mesh Save(Mesh m,string key){string p=R+"/Meshes/OF_"+key+".asset";m.name="OF_"+key;var old=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(m,p);return m;}
  static GameObject Part(Transform parent,string name,Mesh mesh,Vector3 p,Vector3 scale,string material)
  {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=M(material);return go;}
  static GameObject Box(Transform t,string n,Vector3 p,Vector3 s,string m)=>Part(t,n,box,p,s,m);
  static GameObject Orb(Transform t,string n,Vector3 p,Vector3 s,string m)=>Part(t,n,orb,p,s,m);
  static void Beam(Transform t,string n,Vector3 a,Vector3 b,float radius,string mat)
  {var go=Part(t,n,pole,(a+b)*.5f,new Vector3(radius,(b-a).magnitude,radius),mat);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
  static void Curve(Transform t,string n,Vector3 a,Vector3 b,float sag,float radius,string mat)
  {var p=new Vector3[13];var r=new float[13];for(int j=0;j<13;j++){float f=j/12f;p[j]=Vector3.Lerp(a,b,f)-Vector3.up*(Mathf.Sin(f*Mathf.PI)*sag);r[j]=radius;}Part(t,n,Save(CozyGeometry.Tube(p,r,6),"ArtCurve_"+meshId++),Vector3.zero,Vector3.one,mat);}
  static Transform Group(Transform parent,string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
  static void Clean(Transform root,string name){var child=root.Find(name);if(child)Object.DestroyImmediate(child.gameObject);}
  static void Combine(Transform root,string key,bool preserveSources=false)
  {
   if(preserveSources){foreach(var child in root.Cast<Transform>().Where(x=>x.name.StartsWith("Combined ")).ToArray())Object.DestroyImmediate(child.gameObject);foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())renderer.enabled=true;}
   var rs=root.GetComponentsInChildren<MeshRenderer>().Where(x=>x.enabled).ToArray();
   foreach(var group in rs.GroupBy(x=>x.sharedMaterial))
   {
    var list=new List<CombineInstance>();foreach(var r in group){list.Add(new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix});}
    var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(list.ToArray());
    Part(root,"Combined "+group.Key.name,Save(mesh,key+"_"+group.Key.name),Vector3.zero,Vector3.one,group.Key.name.Replace("OF_",""));
   }
   foreach(var r in rs){if(preserveSources)r.enabled=false;else{Object.DestroyImmediate(r.GetComponent<MeshFilter>());Object.DestroyImmediate(r);}}
  }
  [MenuItem("Oceanarium Fishing/Apply ASTRA 0.1 Art Pass")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before applying art.");
   materials.Clear();meshId=0;
   settings=AssetDatabase.LoadAssetAtPath<IslandArtSettings>(R+"/Settings/OF_IslandArt.asset");
   if(!settings){settings=ScriptableObject.CreateInstance<IslandArtSettings>();AssetDatabase.CreateAsset(settings,R+"/Settings/OF_IslandArt.asset");}
   IslandGeometry.HillHeight=settings.hillHeight;IslandGeometry.Seed=settings.seed;
   box=Save(CozyGeometry.RoundedBox(),"BeveledBox");orb=Save(CozyGeometry.Orb(),"SoftOrganic");stone=Save(CozyGeometry.Orb(32,11,7,.25f),"WeatheredRock");
   pole=Save(CozyGeometry.Tube(new[]{Vector3.down*.5f,Vector3.up*.5f},new[]{1f,1f},10),"ArtPole");frond=Save(CozyGeometry.Frond(3,.7f,.75f),"PalmFrond");
   Palette();
   foreach(string key in new[]{"Cabin","Pier","Chest","Player","Rock","Flower","Bush","Palm"})UpgradePrefab(key);
   for(int j=0;j<3;j++)CreateTree(j);
   foreach(string scene in new[]{"OF_MainMenu","OF_HomeIsland"})
   {
    EditorSceneManager.OpenScene(R+"/Scenes/"+scene+".unity");
    var terrain=GameObject.Find("Organic island");int index=scene=="OF_MainMenu"?1:3;
    Mesh mesh=Save(IslandGeometry.Terrain(),"Island_"+index);terrain.GetComponent<MeshFilter>().sharedMesh=mesh;terrain.GetComponent<MeshCollider>().sharedMesh=mesh;terrain.GetComponent<MeshRenderer>().sharedMaterials=new[]{M("IslandBlend")};
    GameObject.Find("Lagoon").GetComponent<MeshFilter>().sharedMesh=Save(IslandGeometry.Ocean(),"Ocean_"+index);
    foreach(string n in new[]{"Cabin trail","Sanctuary trail","Coastal grass field"}){var g=GameObject.Find(n);if(g)g.SetActive(false);}
    foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
    {
     if(root.name.StartsWith("OF_") && !root.name.Contains("Pier") && !root.name.Contains("Rod") && root.GetComponent<LocalPlayerController>()==null){var p=root.transform.position;p.y=IslandGeometry.Height(p.x,p.z);root.transform.position=p;}
     if(root.name=="Future aquarium garden"){var p=root.transform.position;p.y=IslandGeometry.Height(p.x,p.z);root.transform.position=p;}
    }
    var old=GameObject.Find("ASTRA 0.1 Details");if(old)Object.DestroyImmediate(old);
    var details=new GameObject("ASTRA 0.1 Details").transform;
    DressIsland(details);Grass(details);Lighting();ReferenceScene(details);
    var follow=Object.FindFirstObjectByType<CinemachineThirdPersonFollow>();
    if(follow){follow.CameraDistance=7.6f;follow.ShoulderOffset=new Vector3(.35f,0,0);follow.Damping=new Vector3(.12f,.22f,.12f);follow.AvoidObstacles=new CinemachineThirdPersonFollow.ObstacleSettings{Enabled=true,CollisionFilter=1,IgnoreTag="Player",CameraRadius=.3f,DampingIntoCollision=0,DampingFromCollision=.45f};follow.GetComponent<CinemachineCamera>().Lens.FieldOfView=48;}
    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
   }
   AssetDatabase.SaveAssets();Optimize();Debug.Log("ASTRA 0.1 art pass complete.");
  }
  [MenuItem("Oceanarium Fishing/Optimize ASTRA 0.1 Static Prefabs")]
  public static void Optimize()
  {
   if(EditorApplication.isPlaying)return;
   box=AssetDatabase.LoadAssetAtPath<Mesh>(R+"/Meshes/OF_BeveledBox.asset");
   foreach(string key in new[]{"Cabin","Pier","Chest","Flower","Bush","Rock"})
   {
    string path=R+"/Prefabs/OF_"+key+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
    Combine(go.transform,key+"Batched",true);PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);
   }
   AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(R+"/Scenes/OF_HomeIsland.unity");Debug.Log("Static prefab meshes combined; colliders and interaction components preserved.");
  }
  static void Palette()
  {
   var colors=new Dictionary<string,Color>{{"Grass",new Color(.38f,.52f,.24f)},{"Sand",new Color(.79f,.69f,.49f)},{"Leaf",new Color(.24f,.43f,.22f)},{"LeafLight",new Color(.43f,.59f,.28f)},{"Roof",new Color(.19f,.41f,.4f)},{"Plank",new Color(.56f,.36f,.22f)},{"Timber",new Color(.3f,.19f,.12f)},{"Rock",new Color(.48f,.49f,.43f)},{"Cream",new Color(.85f,.77f,.58f)}};
   foreach(var p in colors){var m=M(p.Key);m.color=p.Value;m.SetFloat("_Smoothness",.18f);m.SetTexture("_BaseMap",null);m.enableInstancing=true;EditorUtility.SetDirty(m);}
   foreach(var p in new[]{("IslandBlend","OceanariumFishing/Island Blend"),("Canopy","OceanariumFishing/Coastal Grass"),("Lantern","Universal Render Pipeline/Lit")})
   {var m=AssetDatabase.LoadAssetAtPath<Material>(R+"/Materials/OF_"+p.Item1+".mat");if(!m){m=new Material(Shader.Find(p.Item2));m.name="OF_"+p.Item1;AssetDatabase.CreateAsset(m,R+"/Materials/OF_"+p.Item1+".mat");}materials[p.Item1]=m;}
   M("Canopy").SetColor("_BaseColor",new Color(.35f,.52f,.26f));M("Canopy").SetFloat("_Wind",settings.windStrength);
   M("Lantern").color=new Color(1,.68f,.26f);M("Lantern").EnableKeyword("_EMISSION");M("Lantern").SetColor("_EmissionColor",new Color(1,.49f,.12f)*.65f);
   var ocean=M("Ocean");ocean.SetColor("_Shallow",new Color(.14f,.58f,.53f));ocean.SetColor("_Deep",new Color(.035f,.24f,.34f));ocean.SetFloat("_WaveHeight",.09f);ocean.SetFloat("_WaveScale",.55f);ocean.SetFloat("_FoamWidth",.65f);
   ReferencePalette();foreach(var m in materials.Values)if(m)EditorUtility.SetDirty(m);
  }
  static void UpgradePrefab(string key)
  {
   string path=R+"/Prefabs/OF_"+key+".prefab";var go=PrefabUtility.LoadPrefabContents(path);var root=go.transform;
   foreach(var child in root.Cast<Transform>().Where(x=>x.name.StartsWith("Combined ")).ToArray())Object.DestroyImmediate(child.gameObject);
   foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())renderer.enabled=true;
   Clean(root,"Art additions");
   if(key=="Player")Clean(root.Find("Avatar slots"),"Art additions");
   foreach(var f in go.GetComponentsInChildren<MeshFilter>())
   {
    if(!f.sharedMesh)continue;string n=f.sharedMesh.name;
    if(n=="Cube")f.sharedMesh=box;else if(n=="Sphere")f.sharedMesh=key=="Rock"?stone:orb;
   }
   var t=Group(root,"Art additions",Vector3.zero);
   if(key=="Cabin"){Cabin(t);ReferenceCabin(t);foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())if(renderer.name=="Roof tile" || renderer.name=="Roof ridge"){renderer.sharedMaterial=M("Terracotta");if(renderer.name=="Roof tile"){var p=renderer.transform.localPosition;p.y=3.58f;renderer.transform.localPosition=p;}}}
   if(key=="Pier"){Pier(t);ReferencePier(t);foreach(var child in root.GetComponentsInChildren<Transform>())if(child.name=="Pier support"){var size=child.localScale;size.x=.4f;size.z=.4f;child.localScale=size;}}
   if(key=="Player")
   {
    foreach(string side in new[]{"Left arm","Right arm"}){var arm=root.Find("Avatar slots/"+side);Clean(arm,"Forearm");Orb(arm,"Forearm",new Vector3(0,-.31f,0),new Vector3(.17f,.27f,.18f),"Skin");}
    var visual=root.Find("Avatar slots");Box(t,"Satchel",new Vector3(.29f,.9f,-.22f),new Vector3(.24f,.3f,.16f),"Plank");t.SetParent(visual,false);
    ReferenceAvatar(root,t);for(int j=0;j<3;j++)Orb(t,"Shirt button",new Vector3(0,.82f+j*.13f,.215f),Vector3.one*.035f,"Cream");
   }
   if(key=="Palm")
   {
    foreach(var child in root.Cast<Transform>().Where(x=>x!=t).ToArray())Object.DestroyImmediate(child.gameObject);
    Palm(t,0);Combine(t,"PalmArt");
   }
   if(key=="Rock"){var c=go.GetComponentInChildren<Collider>();if(c)c.gameObject.GetComponent<MeshFilter>().sharedMesh=stone;}
   if(key=="Cabin" || key=="Pier")Combine(t,key+"Details");
   PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);
  }
  static void Lantern(Transform t,Vector3 p)
  {
   Beam(t,"Lantern hanger",p+new Vector3(0,.45f,.05f),p+Vector3.up*.15f,.022f,"Dark");
   Box(t,"Amber glass",p,new Vector3(.22f,.32f,.2f),"Lantern");
   foreach(float x in new[]{-.13f,.13f})foreach(float z in new[]{-.11f,.11f})Beam(t,"Lantern frame",p+new Vector3(x,-.18f,z),p+new Vector3(x,.18f,z),.018f,"Dark");
   Box(t,"Lantern cap",p+Vector3.up*.19f,new Vector3(.32f,.06f,.28f),"Dark");
  }
  static void Pot(Transform t,Vector3 p,int variant)
  {
   Orb(t,"Terracotta planter",p+Vector3.up*.22f,new Vector3(.45f,.48f,.45f),"Petal");
   Orb(t,"Planter soil",p+Vector3.up*.43f,new Vector3(.33f,.06f,.33f),"Timber");
   for(int j=0;j<5;j++){float a=j*2.4f;Vector3 q=p+new Vector3(Mathf.Cos(a)*.14f,.64f+(j%2)*.12f,Mathf.Sin(a)*.14f);Beam(t,"Flower stem",p+Vector3.up*.4f,q,.015f,"Leaf");for(int k=0;k<5;k++){float b=k*Mathf.PI*.4f;Orb(t,"Flower petal",q+new Vector3(Mathf.Cos(b)*.07f,0,Mathf.Sin(b)*.07f),new Vector3(.12f,.04f,.12f),variant%2==0?"Cream":"Petal");}Orb(t,"Flower center",q+Vector3.up*.02f,Vector3.one*.06f,"Gold");}
  }
  static void Barrel(Transform t,Vector3 p)
  {
   for(int j=0;j<14;j++){float a=j*Mathf.PI*2/14;var b=Box(t,"Barrel stave",p+new Vector3(Mathf.Sin(a)*.29f,.4f,Mathf.Cos(a)*.29f),new Vector3(.14f,.76f,.13f),j%3==0?"Timber":"Plank");b.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);}
   foreach(float y in new[]{.15f,.64f}){var ps=new Vector3[25];var rs=new float[25];for(int j=0;j<25;j++){float a=j*Mathf.PI*2/24;ps[j]=p+new Vector3(Mathf.Cos(a)*.33f,y,Mathf.Sin(a)*.33f);rs[j]=.027f;}Part(t,"Barrel hoop",Save(CozyGeometry.Tube(ps,rs),"Hoop_"+meshId++),Vector3.zero,Vector3.one,"Dark");}
   Box(t,"Barrel lid",p+Vector3.up*.78f,new Vector3(.55f,.05f,.55f),"Plank");
  }
  static void Crate(Transform t,Vector3 p)
  {
   for(int j=0;j<4;j++)foreach(float z in new[]{-.32f,.32f})Box(t,"Crate slat",p+new Vector3(0,.1f+j*.14f,z),new Vector3(.75f,.11f,.07f),"Plank");
   foreach(float x in new[]{-.34f,.34f})foreach(float z in new[]{-.32f,.32f})Box(t,"Crate corner",p+new Vector3(x,.3f,z),new Vector3(.09f,.65f,.09f),"Timber");
   Beam(t,"Crate brace",p+new Vector3(-.3f,.06f,-.37f),p+new Vector3(.3f,.56f,-.37f),.045f,"Cream");
  }
  static void Net(Transform t,Vector3 p,float width,float height)
  {
   for(int j=0;j<=8;j++){float x=j*width/8;Curve(t,"Net cord",p+Vector3.right*x,p+new Vector3(x,-height,.08f),.06f,.009f,"Cream");}
   for(int j=0;j<=6;j++){float y=-j*height/6;Curve(t,"Net weave",p+Vector3.up*y,p+new Vector3(width,y,0),.14f,.009f,"Cream");}
  }
  static void Cabin(Transform t)
  {
   foreach(float x in new[]{-1.35f,1.55f})
   {
    foreach(float dx in new[]{-.47f,.47f})Box(t,"Window frame",new Vector3(x+dx,2,-2.04f),new Vector3(.1f,1.08f,.15f),"Cream");
    foreach(float y in new[]{1.48f,2.52f})Box(t,"Window sill",new Vector3(x,y,-2.06f),new Vector3(1.05f,.1f,.23f),"Cream");
    foreach(float side in new[]{-1f,1f})for(int j=0;j<5;j++)Box(t,"Shutter louver",new Vector3(x+side*.65f,1.63f+j*.18f,-2.02f),new Vector3(.26f,.14f,.13f),"Roof");
   }
   foreach(float x in new[]{-.2f,1.1f})Box(t,"Door jamb",new Vector3(x,1.47f,-2.04f),new Vector3(.12f,2.4f,.18f),"Cream");
   Box(t,"Door lintel",new Vector3(.45f,2.7f,-2.05f),new Vector3(1.45f,.15f,.2f),"Cream");
   for(int j=0;j<5;j++)Box(t,"Door bead",new Vector3(-.02f+j*.23f,1.38f,-2.01f),new Vector3(.025f,2.1f,.04f),"LeafLight");
   foreach(float x in new[]{-2.26f,2.26f}){Box(t,"Porch pillar",new Vector3(x,1.55f,-3.27f),new Vector3(.17f,2.6f,.17f),"Cream");Beam(t,"Porch diagonal",new Vector3(x,2.3f,-3.27f),new Vector3(x-Mathf.Sign(x)*.48f,2.8f,-3.27f),.07f,"Cream");}
   Box(t,"Veranda lintel",new Vector3(0,2.9f,-3.27f),new Vector3(4.9f,.2f,.2f),"Timber");
   for(int j=0;j<17;j++){var b=Box(t,"Veranda roof",new Vector3(-2.5f+j*.31f,3.06f,-2.65f),new Vector3(.3f,.12f,1.7f),j%4==0?"RoofLight":"Terracotta");b.transform.localRotation=Quaternion.Euler(-12,0,0);}
   foreach(float side in new[]{-1f,1f})for(int row=0;row<4;row++)for(int j=0;j<12;j++)
   {float x=side*(.3f+row*.64f);var b=Box(t,"Roof shingle",new Vector3(x,4.36f-Mathf.Abs(x)*.488f,-2.12f+j*.39f+(row%2)*.08f),new Vector3(.76f,.07f,.38f),j%5==0?"RoofLight":"Terracotta");b.transform.localRotation=Quaternion.Euler(0,0,-side*26);}
   Lantern(t,new Vector3(-.55f,2.38f,-3.25f));Pot(t,new Vector3(-1.75f,.36f,-2.8f),0);Pot(t,new Vector3(1.7f,.36f,-2.9f),1);
   Barrel(t,new Vector3(-2.95f,0,-1.8f));Crate(t,new Vector3(2.95f,0,-.8f));Net(t,new Vector3(2.34f,2.5f,-.2f),.1f,1.5f);
   var net=Group(t,"Drying net",new Vector3(-2.4f,2,-1.87f));Net(net,Vector3.zero,.8f,1);
   Box(t,"Outdoor stool seat",new Vector3(-3.25f,.58f,-3),new Vector3(.62f,.12f,.6f),"Plank");foreach(float x in new[]{-.22f,.22f})foreach(float z in new[]{-.22f,.22f})Beam(t,"Stool leg",new Vector3(-3.25f+x,0,-3+z),new Vector3(-3.25f+x,.56f,-3+z),.045f,"Timber");
  }
  static void Pier(Transform t)
  {
   foreach(float x in new[]{-1.3f,1.3f})for(int j=0;j<5;j++)
   {
    Orb(t,"Post cap",new Vector3(x,.62f,-j*2),new Vector3(.28f,.09f,.28f),"Plank");
    if(j<4)Curve(t,"Sagging rope",new Vector3(x,.5f,-j*2),new Vector3(x,.5f,-(j+1)*2),.24f,.025f,"Cream");
   }

   Crate(t,new Vector3(-.78f,.14f,-7.1f));Barrel(t,new Vector3(.8f,.14f,-8));Lantern(t,new Vector3(-1.3f,1.15f,-6));Beam(t,"Lantern mast",new Vector3(-1.3f,0,-6),new Vector3(-1.3f,1.65f,-6),.07f,"Timber");
   var net=Group(t,"Pier drying net",new Vector3(-1.34f,.48f,-3));net.localRotation=Quaternion.Euler(0,90,0);Net(net,Vector3.zero,1.5f,.85f);
   for(int j=0;j<3;j++){Orb(t,"Buoy",new Vector3(1.6f,-.2f,-5-j*.43f),new Vector3(.26f,.43f,.26f),j%2==0?"Petal":"Cream");}
   for(int j=0;j<4;j++){float r=.16f+j*.035f;var ps=new Vector3[25];var rs=new float[25];for(int k=0;k<25;k++){float a=k*Mathf.PI*2/24;ps[k]=new Vector3(-.8f+Mathf.Cos(a)*r,.15f,-5.1f+Mathf.Sin(a)*r);rs[k]=.018f;}Part(t,"Rope coil",Save(CozyGeometry.Tube(ps,rs,6),"RopeCoil"+j),Vector3.zero,Vector3.one,"Cream");}
  }
  static void Palm(Transform t,int variant)
  {
   int count=17;var p=new Vector3[count];var radii=new float[count];float height=4.4f+variant*.5f;
   for(int j=0;j<count;j++){float f=j/(float)(count-1);p[j]=new Vector3(.7f*f*f*(variant==1?-1:1),f*height,.25f*Mathf.Sin(f*2));radii[j]=Mathf.Lerp(.29f,.16f,f)*(1+(j%2)*.07f);}
   Part(t,"Curved palm trunk",Save(CozyGeometry.Tube(p,radii,10),"CurvedTrunk"+variant),Vector3.zero,Vector3.one,"Plank");
   for(int layer=0;layer<2;layer++)for(int j=0;j<8;j++){var leaf=Part(t,"Feathered frond",frond,p[count-1]+Vector3.up*layer*.18f,Vector3.one*(layer==0?.88f:.69f),"Canopy");leaf.transform.localRotation=Quaternion.Euler(layer==0?12:-30,j*45+layer*23+variant*17,0);}
   for(int j=0;j<3;j++)Orb(t,"Coconut",p[count-1]+new Vector3((j-1)*.17f,-.15f,.13f),new Vector3(.27f,.35f,.28f),"Timber");
  }
  static void CreateTree(int variant)
  {
   var root=new GameObject("OF_TropicalTree_"+variant);var t=root.transform;
   if(variant==0)Palm(t,1);
   else
   {
    float height=variant==1?3.2f:2.25f;Beam(t,"Trunk",Vector3.zero,new Vector3(.14f,height,0),.16f,"Plank");
    for(int j=0;j<6;j++){float a=j*2.4f;Vector3 end=new Vector3(Mathf.Cos(a)*.85f,height-.3f+(j%3)*.28f,Mathf.Sin(a)*.8f);Beam(t,"Branch",new Vector3(0,height*.6f,0),end,.065f,"Plank");Part(t,"Leaf cloud",Save(CozyGeometry.Orb(j+8,14,9,.16f),"CanopyCloud"+j),end,new Vector3(2.1f,1.55f,2),"Canopy");}
   }
   Combine(t,"Tree"+variant);var c=root.AddComponent<CapsuleCollider>();c.center=Vector3.up*1.5f;c.height=3;c.radius=.22f;
   PrefabUtility.SaveAsPrefabAsset(root,R+"/Prefabs/OF_TropicalTree_"+variant+".prefab");Object.DestroyImmediate(root);
  }
  static Transform At(Transform parent,string name,float x,float z)=>Group(parent,name,new Vector3(x,IslandGeometry.Height(x,z),z));
  static void DressIsland(Transform root)
  {
   var random=new System.Random(settings.seed);float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
   // Authored clusters anchor vistas; clear paths and activity circles are excluded.
   var anchors=new[]{new Vector2(-13,8),new Vector2(-15,-5),new Vector2(14,6),new Vector2(4,12),new Vector2(-4,13),new Vector2(12,-6)};
   for(int j=0;j<anchors.Length;j++)
   {
    Vector2 a=anchors[j];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Prefabs/OF_TropicalTree_"+(j%3)+".prefab");var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab);tree.transform.SetParent(root);tree.transform.position=new Vector3(a.x,IslandGeometry.Height(a.x,a.y),a.y);tree.transform.localScale=Vector3.one*Rand(.85f,1.12f);tree.transform.rotation=Quaternion.Euler(0,Rand(0,360),0);
    var patch=At(root,"Understory "+j,a.x,a.y);
    for(int k=0;k<5;k++){Vector3 p=new Vector3(Rand(-1.7f,1.7f),.2f,Rand(-1.5f,1.5f));Part(patch,"Coastal shrub",orb,p,new Vector3(Rand(.5f,1),Rand(.4f,.7f),Rand(.5f,1)),k%2==0?"Leaf":"LeafLight");}
    Combine(patch,"Understory"+j);
   }
   var rocks=At(root,"Northern rock shelves",-4,12);
   for(int j=0;j<9;j++){Vector3 p=new Vector3(Rand(-4,4),Rand(-.3f,.1f),Rand(-1.5f,2));var b=Part(rocks,"Weathered outcrop",stone,p,new Vector3(Rand(1.4f,3.5f),Rand(.9f,2),Rand(1,2.4f)),"Rock");b.transform.localRotation=Quaternion.Euler(Rand(-15,15),Rand(0,360),Rand(-12,12));b.AddComponent<SphereCollider>().radius=.45f;}
   Combine(rocks,"RockShelf");
   var spawn=At(root,"Welcome garden",-2.7f,-9);Pot(spawn,new Vector3(-.2f,0,0),1);Box(spawn,"Welcome sign",new Vector3(0,.9f,.35f),new Vector3(1,.45f,.12f),"Roof");Beam(spawn,"Sign post",new Vector3(0,0,.35f),new Vector3(0,1,.35f),.06f,"Plank");Orb(spawn,"Fish emblem",new Vector3(0,.9f,.27f),new Vector3(.5f,.19f,.05f),"Cream");Combine(spawn,"WelcomeGarden");
   var garden=At(root,"Sanctuary garden border",8,3);
   foreach(float x in new[]{-3.5f,3.5f})for(int j=0;j<6;j++)Part(garden,"Garden border stone",stone,new Vector3(x,.09f,-2+j*.8f),new Vector3(.45f,.25f,.5f),"Rock");
   Pot(garden,new Vector3(-2.5f,0,-2.7f),0);Pot(garden,new Vector3(2.5f,0,-2.7f),1);Crate(garden,new Vector3(3.6f,0,2));Combine(garden,"GardenBorder");
   var beach=At(root,"Beach shade",-10,-13);Beam(beach,"Parasol mast",Vector3.zero,Vector3.up*2.65f,.055f,"Timber");
   for(int j=0;j<10;j++){float a=j*Mathf.PI*.2f;Vector3 p=new Vector3(Mathf.Cos(a),2.3f,Mathf.Sin(a));var panel=Part(beach,"Shade petal",orb,p,new Vector3(1.7f,.18f,1.1f),j%2==0?"Cream":"Roof");panel.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,18);}
   Box(beach,"Beach mat",new Vector3(0,.03f,0),new Vector3(1.6f,.03f,2),"Cream");Crate(beach,new Vector3(1.8f,0,0));Combine(beach,"BeachShade");
   var shore=new GameObject("Shore keepsakes").transform;shore.SetParent(root,false);
   for(int j=0;j<55;j++)
   {
    float a=Rand(-3.1f,3.1f),r=IslandGeometry.Coast(a)*Rand(.86f,.94f);float x=Mathf.Cos(a)*r,z=Mathf.Sin(a)*r*.872f;if(!IslandGeometry.ClearForPlants(x,z,1.6f))continue;Vector3 p=new Vector3(x,IslandGeometry.Height(x,z)+.04f,z);
    if(j%7==0){for(int k=0;k<5;k++){float b=k*Mathf.PI*.4f;var arm=Part(shore,"Starfish arm",orb,p+new Vector3(Mathf.Sin(b)*.09f,0,Mathf.Cos(b)*.09f),new Vector3(.09f,.035f,.23f),"Petal");arm.transform.localRotation=Quaternion.Euler(0,b*Mathf.Rad2Deg,0);}}
    else if(j%5==0)Beam(shore,"Driftwood",p,p+new Vector3(.65f,.13f,.3f),.09f,"Plank");
    else Part(shore,j%2==0?"Seashell":"Pebble",j%2==0?orb:stone,p,new Vector3(.16f,.07f,.21f),j%2==0?"Cream":"Rock");
   }
   Combine(shore,"ShoreDetails");
  }
  static void Grass(Transform root)
  {
   var random=new System.Random(settings.seed+42);var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
   for(int j=0;j<4500*settings.vegetationDensity;j++)
   {
    float x=(float)random.NextDouble()*40-20,z=(float)random.NextDouble()*32-16;float ratio=IslandGeometry.Radius(x,z)/IslandGeometry.Coast(Mathf.Atan2(z/.872f,x));
    if(ratio>.85f || !IslandGeometry.ClearForPlants(x,z,1.3f) || Mathf.PerlinNoise(x*.28f+8,z*.28f)<.45f)continue;
    for(int k=0;k<3;k++){float a=(float)random.NextDouble()*Mathf.PI*2,h=.18f+(float)random.NextDouble()*(ratio>.75f?.5f:.25f);Vector3 p=new Vector3(x,IslandGeometry.Height(x,z),z),side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.04f;int n=v.Count;v.AddRange(new[]{p-side,p+side,p+Vector3.up*h+side*2});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one});tr.AddRange(new[]{n,n+2,n+1});}
   }
   var m=new Mesh();m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.SetNormals(Enumerable.Repeat(Vector3.up,v.Count).ToList());m.RecalculateBounds();var go=Part(root,"Meadow grass",Save(m,"MeadowGrass"),Vector3.zero,Vector3.one,"Canopy");go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
  }
  static void Lighting()
  {
   var sun=RenderSettings.sun;sun.color=settings.sunlight;sun.intensity=settings.sunIntensity;sun.transform.rotation=Quaternion.Euler(settings.sunEuler);sun.shadowStrength=.8f;sun.shadowBias=.035f;
   RenderSettings.ambientSkyColor=settings.skyAmbient;RenderSettings.ambientEquatorColor=new Color(.44f,.5f,.43f);RenderSettings.ambientGroundColor=new Color(.29f,.29f,.23f);RenderSettings.fogColor=settings.horizon;RenderSettings.fogStartDistance=75;RenderSettings.fogEndDistance=230;
   var sky=RenderSettings.skybox;if(sky.HasProperty("_Exposure"))sky.SetFloat("_Exposure",.95f);if(sky.HasProperty("_AtmosphereThickness"))sky.SetFloat("_AtmosphereThickness",.8f);EditorUtility.SetDirty(sky);
   var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(R+"/Settings/OF_AfternoonVolume.asset");if(profile.TryGet<ColorAdjustments>(out var color)){color.saturation.Override(-6);color.contrast.Override(4);EditorUtility.SetDirty(color);}EditorUtility.SetDirty(profile);
  }
 }
}







