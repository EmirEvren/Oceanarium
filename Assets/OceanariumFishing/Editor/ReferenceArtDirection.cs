using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Object=UnityEngine.Object;
namespace OceanariumFishing.Editor
{
 public static partial class AstraArtPass
 {
  [MenuItem("Oceanarium Fishing/Apply Reference Tropical Direction")]
  public static void ReferenceDirection()
  {
   if(EditorApplication.isPlaying)return;
   var cfg=AssetDatabase.LoadAssetAtPath<IslandArtSettings>(R+"/Settings/OF_IslandArt.asset");
   if(cfg){cfg.hillHeight=2.7f;cfg.sunIntensity=1.3f;cfg.sunlight=new Color(1,.92f,.78f);cfg.skyAmbient=new Color(.62f,.76f,.84f);cfg.horizon=new Color(.56f,.78f,.87f);cfg.sunEuler=new Vector3(42,-35,0);EditorUtility.SetDirty(cfg);}
   Apply();
  }
  static void NewMat(string key,Color color)
  {
   var mat=M(key);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="OF_"+key};AssetDatabase.CreateAsset(mat,R+"/Materials/OF_"+key+".mat");materials[key]=mat;}
   mat.color=color;mat.enableInstancing=true;mat.SetFloat("_Smoothness",.23f);EditorUtility.SetDirty(mat);
  }
  static void ReferencePalette()
  {
   NewMat("Terracotta",new Color(.65f,.23f,.13f));NewMat("RoofLight",new Color(.77f,.32f,.18f));NewMat("LeafSun",new Color(.48f,.65f,.24f));NewMat("Cap",new Color(.83f,.79f,.64f));NewMat("Glass",new Color(.15f,.39f,.47f));NewMat("PetalWhite",new Color(.98f,.91f,.74f));
   M("Plank").color=new Color(.66f,.43f,.23f);M("Timber").color=new Color(.40f,.26f,.13f);
   M("Canopy").SetColor("_BaseColor",new Color(.42f,.60f,.23f));
   M("IslandBlend").SetColor("_Grass",new Color(.43f,.61f,.26f));M("IslandBlend").SetColor("_Sand",new Color(.9f,.79f,.57f));M("IslandBlend").SetColor("_Path",new Color(.74f,.61f,.4f));
   var ocean=M("Ocean");ocean.SetColor("_Shallow",new Color(.12f,.72f,.68f));ocean.SetColor("_Deep",new Color(.035f,.37f,.55f));ocean.SetFloat("_Reflection",.28f);ocean.SetFloat("_SunGlint",.7f);
   // Low contrast, seamless wood grain created in Unity and reused by all timber.
   string path=R+"/Textures/OF_WoodGrain.asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
   if(!tex){tex=new Texture2D(128,128,TextureFormat.RGB24,true){name="OF_WoodGrain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};AssetDatabase.CreateAsset(tex,path);}
   for(int y=0;y<128;y++)for(int x=0;x<128;x++)
   {float a=x*Mathf.PI*2/128,b=y*Mathf.PI*2/128;float grain=Mathf.Sin(b*14+Mathf.Sin(a*2)*.7f)+.4f*Mathf.Sin(b*31+Mathf.Sin(a*3));float f=.87f+grain*.055f;tex.SetPixel(x,y,new Color(f,f,f));}
   tex.Apply();EditorUtility.SetDirty(tex);foreach(string key in new[]{"Plank","Timber"})M(key).SetTexture("_BaseMap",tex);
   var sky=M("Sky");sky.shader=Shader.Find("OceanariumFishing/Tropical Sky");sky.SetColor("_Zenith",new Color(.13f,.42f,.8f));sky.SetColor("_Horizon",new Color(.42f,.7f,.86f));EditorUtility.SetDirty(sky);
  }
  static void Ring(Transform t,string name,Vector3 center,float radius,float thickness,string material,bool vertical=false)
  {
   var p=new Vector3[33];var r=new float[33];for(int j=0;j<33;j++){float a=j*Mathf.PI*2/32;p[j]=center+(vertical?new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0):new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));r[j]=thickness;}
   Part(t,name,Save(CozyGeometry.Tube(p,r,8),"ReferenceRing"+meshId++),Vector3.zero,Vector3.one,material);
  }
  static void ReferenceCabin(Transform t)
  {
   var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-2.22f,3,-1.81f),new Vector3(0,4.25f,-1.81f),new Vector3(2.22f,3,-1.81f)};mesh.triangles=new[]{0,1,2};mesh.RecalculateNormals();Part(t,"Timber gable",Save(mesh,"CabinGable"),Vector3.zero,Vector3.one,"Plank");
   Beam(t,"Left gable bargeboard",new Vector3(-2.7f,3.03f,-2.31f),new Vector3(0,4.46f,-2.31f),.11f,"Plank");Beam(t,"Right gable bargeboard",new Vector3(0,4.46f,-2.31f),new Vector3(2.7f,3.03f,-2.31f),.11f,"Plank");
   Orb(t,"Round attic window",new Vector3(0,3.57f,-1.86f),new Vector3(.69f,.69f,.08f),"Glass");Ring(t,"Round window frame",new Vector3(0,3.57f,-1.94f),.36f,.045f,"Cream",true);
   Box(t,"Attic window mullion",new Vector3(0,3.57f,-1.96f),new Vector3(.05f,.68f,.06f),"Plank");Box(t,"Attic window transom",new Vector3(0,3.57f,-1.96f),new Vector3(.68f,.05f,.06f),"Plank");
   foreach(float side in new[]{-1f,1f})
   {
    float x=side*1.65f;Box(t,"Porch top rail",new Vector3(x,1.05f,-3.3f),new Vector3(1.1f,.12f,.12f),"Plank");
    for(int j=0;j<4;j++)Box(t,"Porch spindle",new Vector3(x-.45f+j*.3f,.7f,-3.3f),new Vector3(.065f,.62f,.065f),"Cream");
   }
   Box(t,"Side window glass",new Vector3(2.32f,1.95f,.15f),new Vector3(.1f,1.05f,1),"Glass");
   foreach(float z in new[]{-.41f,.71f})Box(t,"Side window frame",new Vector3(2.4f,1.95f,z),new Vector3(.15f,1.2f,.12f),"Cream");
   foreach(float y in new[]{1.37f,1.95f,2.53f})Box(t,"Side window bar",new Vector3(2.4f,y,.15f),new Vector3(.15f,.08f,1.2f),"Cream");
   Box(t,"Side window mullion",new Vector3(2.4f,1.95f,.15f),new Vector3(.15f,1.1f,.07f),"Cream");
  }
  static void ReferencePier(Transform t)
  {
   // Wider end platform gives all four players room to stand at separate holders.
   for(int j=0;j<10;j++)Box(t,"End deck board",new Vector3(0,.015f,-6.1f-j*.34f),new Vector3(6.3f,.23f,.32f),j%4==0?"Timber":"Plank");
   var deck=new GameObject("End deck collision");deck.transform.SetParent(t,false);deck.transform.localPosition=new Vector3(0,-.02f,-7.63f);deck.AddComponent<BoxCollider>().size=new Vector3(6.3f,.2f,3.4f);
   foreach(float x in new[]{-2.95f,2.95f})foreach(float z in new[]{-6.2f,-8.8f})
   {
    Beam(t,"Thick mooring post",new Vector3(x,-1.8f,z),new Vector3(x,.83f,z),.19f,"Plank");Orb(t,"Mooring cap",new Vector3(x,.83f,z),new Vector3(.4f,.12f,.4f),"Plank");
    for(int k=0;k<4;k++)Ring(t,"Wrapped mooring rope",new Vector3(x,.28f+k*.065f,z),.22f,.033f,"Cream");
   }
   for(int j=0;j<4;j++)
   {
    float x=-2.25f+j*1.5f;Box(t,"Rod station base",new Vector3(x,.16f,-8.4f),new Vector3(.48f,.13f,.45f),"Plank");
    Box(t,"Rod station socket",new Vector3(x,.33f,-8.48f),new Vector3(.17f,.4f,.17f),"Timber");Ring(t,"Socket rim",new Vector3(x,.52f,-8.48f),.095f,.024f,"Gold");
   }
   Beam(t,"Harbour lantern post",new Vector3(2.95f,0,-6.2f),new Vector3(2.95f,2.3f,-6.2f),.14f,"Plank");Beam(t,"Harbour lantern arm",new Vector3(2.95f,2.25f,-6.2f),new Vector3(2.35f,2.25f,-6.2f),.1f,"Plank");Lantern(t,new Vector3(2.4f,1.76f,-6.2f));
  }
  static void ReferenceAvatar(Transform root,Transform t)
  {
   var head=root.Find("Avatar slots/Head slot");Clean(head,"Explorer cap");var cap=Group(head,"Explorer cap",Vector3.zero);
   var smooth=Save(CozyGeometry.Orb(0,24,16,0),"AvatarSoft");
   foreach(var mf in root.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh==orb)mf.sharedMesh=smooth;
   Part(cap,"Canvas crown",smooth,new Vector3(0,.25f,-.015f),new Vector3(.81f,.48f,.76f),"Cap");
   Part(cap,"Cap visor",smooth,new Vector3(0,.14f,.36f),new Vector3(.65f,.065f,.55f),"Cap");
   Part(cap,"Back cap opening",smooth,new Vector3(0,.12f,-.385f),new Vector3(.25f,.13f,.025f),"Timber");
   for(int j=0;j<8;j++){float a=j*Mathf.PI/7;Part(cap,"Hair lock",smooth,new Vector3(Mathf.Cos(a)*.3f,.025f,-Mathf.Sin(a)*.29f),new Vector3(.17f,.22f,.16f),"Timber");}
   Orb(cap,"Cap button",new Vector3(0,.492f,-.015f),new Vector3(.08f,.035f,.08f),"Cap");
   for(int j=0;j<5;j++){float a=j*Mathf.PI*2/5;Curve(cap,"Canvas seam",new Vector3(0,.48f,0),new Vector3(Mathf.Sin(a)*.38f,.18f,Mathf.Cos(a)*.35f),-.07f,.007f,"Cream");}
   Clean(t,"Satchel");Box(t,"Canvas backpack",new Vector3(0,1,-.28f),new Vector3(.46f,.52f,.2f),"Plank");Box(t,"Backpack flap",new Vector3(0,1.17f,-.405f),new Vector3(.49f,.19f,.06f),"Timber");
   Box(t,"Backpack pocket",new Vector3(0,.88f,-.4f),new Vector3(.3f,.18f,.07f),"Plank");
   foreach(float x in new[]{-.15f,.15f}){Curve(t,"Shoulder strap",new Vector3(x,1.25f,-.25f),new Vector3(x,.78f,.2f),-.14f,.035f,"Timber");Box(t,"Brass buckle",new Vector3(x,1.07f,-.44f),new Vector3(.065f,.08f,.025f),"Gold");}
   foreach(string side in new[]{"Left leg","Right leg"})
   {
    var leg=root.Find("Avatar slots/"+side);var trousers=leg.Find("Trousers");trousers.localPosition=new Vector3(0,-.13f,0);trousers.localScale=new Vector3(.27f,.15f,.29f);
    var renderer=trousers.GetComponent<MeshRenderer>();renderer.sharedMaterial=M("Roof");Clean(leg,"Socks");Part(leg,"Socks",smooth,new Vector3(0,-.4f,0),new Vector3(.21f,.28f,.22f),"Cream");
   }
  }
  static void ReferenceScene(Transform details)
  {
   var clouds=GameObject.Find("Cloud banks");if(clouds)clouds.SetActive(false);RenderSettings.skybox=M("Sky");
   // Stratified limestone and planted ledges frame the cabin without occupying the trails.
   var cliffs=At(details,"Limestone terraces",-10,9);
   for(int layer=0;layer<3;layer++)for(int j=0;j<5;j++)
   {
    float x=-6+j*2.5f+layer*.6f,z=layer*1.5f,y=layer*.85f;
    var rock=Part(cliffs,"Limestone shelf",stone,new Vector3(x,y,z),new Vector3(3.4f,2.1f,3.6f),"Rock");rock.transform.localRotation=Quaternion.Euler(0,j*21,0);
    Part(cliffs,"Grassy ledge",orb,new Vector3(x,y+.93f,z),new Vector3(3.5f,.35f,3.65f),"LeafSun");
   }
   Combine(cliffs,"ReferenceCliffs");foreach(var r in cliffs.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial==M("Rock"))r.gameObject.AddComponent<MeshCollider>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;
   // Paving stones make the entrance feel authored while keeping the blended walking trail.
   var paving=new GameObject("Cabin stepping stones").transform;paving.SetParent(details,false);
   for(int j=0;j<9;j++){float f=j/8f;float x=Mathf.Lerp(-1,-7,f),z=Mathf.Lerp(-6,-.5f,f);Part(paving,"Worn paving stone",stone,new Vector3(x,IslandGeometry.Height(x,z)+.045f,z),new Vector3(.85f,.13f,.66f),"Sand");}
   Combine(paving,"ReferencePaving");
   var flowers=new GameObject("Tropical flower beds").transform;flowers.SetParent(details,false);
   var random=new System.Random(818);
   foreach(var a in new[]{new Vector2(-11,-1),new Vector2(-5,2),new Vector2(-12,6),new Vector2(10,6),new Vector2(6,-1),new Vector2(-5,-9)})
   for(int j=0;j<12;j++)
   {
    float x=a.x+(float)random.NextDouble()*2-1,z=a.y+(float)random.NextDouble()*1.4f-.7f;if(IslandGeometry.PathDistance(x,z)<1.2f || Mathf.Abs(x+8)<2.8f && Mathf.Abs(z-3)<2.5f)continue;
    Vector3 p=new Vector3(x,IslandGeometry.Height(x,z),z);Part(flowers,"Flower cushion",orb,p+Vector3.up*.18f,new Vector3(.6f,.4f,.55f),"Leaf");
    for(int k=0;k<3;k++){Vector3 q=p+new Vector3((k-1)*.16f,.42f+(j%3)*.05f,k*.11f);for(int b=0;b<5;b++){float angle=b*Mathf.PI*.4f;Part(flowers,"Daisy petal",orb,q+new Vector3(Mathf.Cos(angle)*.09f,0,Mathf.Sin(angle)*.09f),new Vector3(.16f,.055f,.16f),j%4==0?"Petal":"PetalWhite");}Part(flowers,"Daisy center",orb,q+Vector3.up*.03f,Vector3.one*.08f,"Gold");}
   }
   Combine(flowers,"ReferenceFlowerBeds");
   var rods=Object.FindObjectsByType<IslandInteractable>(FindObjectsSortMode.None).Where(r=>r.kind==InteractionKind.StarterRod).OrderBy(r=>r.stableId).ToArray();
   for(int j=0;j<rods.Length;j++)rods[j].transform.position=new Vector3(1-2.25f+j*1.5f,.86f,-25.48f);
   var player=Object.FindFirstObjectByType<LocalPlayerController>();if(player){player.transform.position=new Vector3(1,IslandGeometry.Height(1,-13.5f)+.12f,-13.5f);player.cameraTarget.position=player.transform.position+Vector3.up*1.55f;}
   var cam=Object.FindFirstObjectByType<CinemachineThirdPersonFollow>();if(cam){cam.CameraDistance=7.6f;cam.GetComponent<CinemachineCamera>().Lens.FieldOfView=48;}
  }
 }
}




