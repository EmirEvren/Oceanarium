using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

namespace OceanariumFishing.Editor
{
    public static class FoundationBuilder
    {
        public const string Root="Assets/OceanariumFishing";
        private static readonly Dictionary<string,Material> mats=new();
        
        private static Mesh frond;
        private static string[] names={"OF_Boot","OF_MainMenu","OF_CharacterCreator","OF_HomeIsland","OF_TownIsland","OF_ExplorationIsland_01","OF_ExplorationIsland_02","OF_ExplorationIsland_03"};
        [MenuItem("Oceanarium Fishing/Build ASTRA 0.0 Foundation %&b")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            // Never silently overwrite an artist-edited scene.
            if(File.Exists(Root+"/Scenes/OF_HomeIsland.unity")) {EditorSceneManager.OpenScene(Root+"/Scenes/OF_HomeIsland.unity");Debug.Log("Foundation already exists. Opened existing scene without rebuilding.");return;}
            mats.Clear();
            foreach(var folder in new[]{"Materials","Meshes","Prefabs","Scenes","Settings","Localization/Locales","Localization/Tables"})Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            Configure();CreatePalette();CreateLocalization();
            frond=SaveMesh(IslandGeometry.Leaf(2.9f,.48f,.65f),"PalmFrond");
            var catalog=ScriptableObject.CreateInstance<SceneCatalog>();catalog.sceneNames=names;AssetDatabase.CreateAsset(catalog,Root+"/Settings/OF_SceneCatalog.asset");
            var services=new GameObject("OF_Services");services.AddComponent<LanguageService>();var session=services.AddComponent<GameSession>();session.catalog=catalog;
            var servicesPrefab=PrefabUtility.SaveAsPrefabAsset(services,Root+"/Prefabs/OF_Services.prefab").GetComponent<GameSession>();UnityEngine.Object.DestroyImmediate(services);
            var palm=SavePrefab(CreatePalm(),"OF_Palm");var bush=SavePrefab(CreateBush(),"OF_Bush");var flower=SavePrefab(CreateFlower(),"OF_Flower");var rock=SavePrefab(CreateRock(),"OF_Rock");
            var cabin=SavePrefab(CreateCabin(),"OF_Cabin");var chest=SavePrefab(CreateChest(),"OF_Chest");var rod=SavePrefab(CreateRod(),"OF_StarterRod");var pier=SavePrefab(CreatePier(),"OF_Pier");
            var avatar=SavePrefab(CreatePlayer(),"OF_Player");
            for(int i=0;i<names.Length;i++)
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var entry=new GameObject("Scene Composition").AddComponent<SceneEntry>();entry.servicesPrefab=servicesPrefab;entry.destination=(Destination)i;
                if(i!=0)
                {
                    entry.hud=new GameObject("Local UI").AddComponent<FoundationHUD>();
                    SetupLighting();
                    if(i==3 || i==1)
                    {
                        MeshObject("Organic island",SaveMesh(IslandGeometry.Terrain(),"Island_"+i),new[]{mats["Grass"],mats["Sand"]},true);
                        MeshObject("Lagoon",SaveMesh(IslandGeometry.Ocean(),"Ocean_"+i),new[]{mats["Ocean"]},false);
                        var path=IslandGeometry.Path(new[]{new Vector3(1,0,-15),new Vector3(1,0,-10),new Vector3(-1,0,-6),new Vector3(-5,0,-3),new Vector3(-7,0,1)},1.7f);
                        MeshObject("Cabin trail",SaveMesh(path,"CabinPath_"+i),new[]{mats["Path"]},false);
                        var path2=IslandGeometry.Path(new[]{new Vector3(-1,0,-6),new Vector3(2,0,-4),new Vector3(5,0,0),new Vector3(9,0,3)},1.5f);
                        MeshObject("Sanctuary trail",SaveMesh(path2,"SanctuaryPath_"+i),new[]{mats["Path"]},false);
                        Place(cabin,new Vector3(-8,0,3),12);Place(chest,new Vector3(-4.4f,0,1),-8);
                        var dock=Place(pier,new Vector3(1,0,-17),0,false);dock.transform.position=new Vector3(1,.55f,-17);
                        for(int j=0;j<4;j++){var item=Place(rod,new Vector3(2.35f,0,-17-j*.65f),0,false);item.transform.position=new Vector3(2.35f,.72f,-17-j*.65f);item.GetComponent<IslandInteractable>().stableId="home.rod."+j;}
                        var random=new System.Random(1847);
                        for(int j=0;j<32;j++)
                        {
                            float angle=(float)random.NextDouble()*Mathf.PI*2, radius=13+(float)random.NextDouble()*5;
                            var p=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius*.83f);
                            if(p.z < -10 && Mathf.Abs(p.x)<6 || (p-new Vector3(-8,0,3)).sqrMagnitude<30 || (p-new Vector3(8,0,3)).sqrMagnitude<30)continue;
                            var tree=Place(palm,p,(float)random.NextDouble()*360);tree.transform.localScale=Vector3.one*(.75f+(float)random.NextDouble()*.4f);
                        }
                        for(int j=0;j<200;j++)
                        {
                            var p=new Vector3((float)random.NextDouble()*38-19,0,(float)random.NextDouble()*30-15);
                            float ratio=IslandGeometry.Radius(p.x,p.z)/IslandGeometry.Coast(Mathf.Atan2(p.z/.872f,p.x));
                            if(ratio>.9f || ratio<.42f || (p-new Vector3(-8,0,3)).sqrMagnitude<30 || (p-new Vector3(8,0,3)).sqrMagnitude<24 || Mathf.Abs(p.x)<3 && p.z<0)continue;
                            var prop=Place(j%7==0?rock:j%3==0?flower:bush,p,(float)random.NextDouble()*360);prop.transform.localScale*=.6f+(float)random.NextDouble()*.6f;
                        }
                        CreatePlot();CreateClouds();
                    }
                    var cam=new GameObject("Local Camera",typeof(Camera),typeof(AudioListener));cam.tag="MainCamera";
                    cam.GetComponent<Camera>().farClipPlane=280;cam.GetComponent<Camera>().fieldOfView=48;
                    cam.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
                    if(i==3)
                    {
                        var p=(GameObject)PrefabUtility.InstantiatePrefab(avatar);p.transform.position=new Vector3(1,IslandGeometry.Height(1,-9)+.15f,-9);
                        entry.player=p.GetComponent<LocalPlayerController>();entry.player.view=cam.GetComponent<Camera>();
                        var target=new GameObject("Local Camera Target").transform;target.position=p.transform.position+Vector3.up*1.55f;target.rotation=Quaternion.Euler(24,0,0);entry.player.cameraTarget=target;
                        cam.AddComponent<CinemachineBrain>();var cm=new GameObject("Follow Camera").AddComponent<CinemachineCamera>();cm.Follow=target;cm.Lens.FieldOfView=48;
                        var follow=cm.gameObject.AddComponent<CinemachineThirdPersonFollow>();follow.CameraDistance=7;follow.ShoulderOffset=new Vector3(.45f,0,0);follow.VerticalArmLength=.5f;follow.Damping=new Vector3(.12f,.15f,.12f);
                        follow.AvoidObstacles=new CinemachineThirdPersonFollow.ObstacleSettings{Enabled=true,CollisionFilter=1,IgnoreTag="Player",CameraRadius=.22f,DampingIntoCollision=.05f,DampingFromCollision=.3f};
                    }
                    else {cam.transform.position=new Vector3(28,23,-35);cam.transform.LookAt(new Vector3(0,1,0));}
                }
                EditorSceneManager.SaveScene(scene,Root+"/Scenes/"+names[i]+".unity");
            }
            var builds=new List<EditorBuildSettingsScene>();foreach(var n in names)builds.Add(new EditorBuildSettingsScene(Root+"/Scenes/"+n+".unity",true));
            foreach(var existing in EditorBuildSettings.scenes)if(!existing.path.StartsWith(Root+"/Scenes/"))builds.Add(existing);
            EditorBuildSettings.scenes=builds.ToArray();AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Root+"/Scenes/OF_HomeIsland.unity");
            Debug.Log("ASTRA 0.0 foundation generated: eight scenes, reusable prefabs, localization tables and procedural environment.");
        }
        private static void Configure()
        {
            PlayerSettings.colorSpace=ColorSpace.Linear;
            var source=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),Root+"/Settings/OF_PC_RenderPipeline.asset");
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Settings/OF_PC_RenderPipeline.asset");
            pipeline.msaaSampleCount=4;pipeline.supportsCameraDepthTexture=true;pipeline.useSRPBatcher=true;pipeline.shadowDistance=65;
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowResolution=UnityEngine.ShadowResolution.High;
            EditorUtility.SetDirty(pipeline);
        }
        private static Material Mat(string name,Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="OF_"+name,color=color,enableInstancing=true};m.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(m,Root+"/Materials/OF_"+name+".mat");mats[name]=m;return m;
        }
        private static void CreatePalette()
        {
            Mat("Grass",new Color(.46f,.64f,.19f));Mat("Sand",new Color(.91f,.76f,.45f));Mat("Path",new Color(.69f,.43f,.23f));Mat("Timber",new Color(.38f,.21f,.12f));Mat("Plank",new Color(.64f,.4f,.22f));
            Mat("Cream",new Color(.94f,.83f,.59f));Mat("Roof",new Color(.23f,.47f,.42f));Mat("Leaf",new Color(.3f,.52f,.13f));Mat("LeafLight",new Color(.58f,.72f,.19f));Mat("Rock",new Color(.48f,.5f,.4f));
            Mat("Petal",new Color(.95f,.39f,.27f));Mat("Gold",new Color(.96f,.67f,.22f));Mat("Dark",new Color(.09f,.16f,.19f));Mat("Skin",new Color(.81f,.52f,.32f));Mat("Shirt",new Color(.19f,.5f,.57f));Mat("White",new Color(.94f,.94f,.83f));
            var ocean=new Material(Shader.Find("OceanariumFishing/Cozy Ocean")){name="OF_Ocean"};AssetDatabase.CreateAsset(ocean,Root+"/Materials/OF_Ocean.mat");mats["Ocean"]=ocean;
        }
        private static Mesh SaveMesh(Mesh mesh,string name){AssetDatabase.CreateAsset(mesh,Root+"/Meshes/OF_"+name+".asset");return mesh;}
        private static GameObject MeshObject(string name,Mesh mesh,Material[] materials,bool collision,Transform parent=null)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=materials;
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
        }
        private static GameObject Shape(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,string material,bool collider=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<MeshRenderer>().sharedMaterial=mats[material];
            if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,string material)
        {var go=Shape(parent,name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f,width),material);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
        private static GameObject SavePrefab(GameObject go,string name){var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;}
        private static GameObject Place(GameObject prefab,Vector3 p,float yaw,bool ground=true)
        {var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);if(ground)p.y=IslandGeometry.Height(p.x,p.z);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,yaw,0);return go;}
        private static GameObject CreatePalm()
        {
            var root=new GameObject("Palm");
            for(int j=0;j<9;j++){var a=new Vector3(j*j*.008f,j*.44f,0);var b=new Vector3((j+1)*(j+1)*.008f,(j+1)*.44f,0);Beam(root.transform,"Segmented trunk",a,b,.38f-j*.017f,j%2==0?"Timber":"Plank");}
            var hit=root.AddComponent<CapsuleCollider>();hit.center=new Vector3(.25f,1.9f,0);hit.radius=.25f;hit.height=3.8f;
            var crown=new GameObject("Swaying crown");crown.transform.SetParent(root.transform,false);crown.transform.localPosition=new Vector3(.65f,3.95f,0);crown.AddComponent<VegetationSway>();
            for(int j=0;j<9;j++){var leaf=MeshObject("Palm frond",frond,new[]{mats[j%2==0?"Leaf":"LeafLight"]},false,crown.transform);leaf.transform.localRotation=Quaternion.Euler(-18+(j%3)*12,j*40,0);}
            for(int j=0;j<3;j++)Shape(crown.transform,"Coconut",PrimitiveType.Sphere,new Vector3((j-1)*.18f,-.18f,.1f),Vector3.one*.32f,"Timber");return root;
        }
        private static GameObject CreateBush()
        {
            var root=new GameObject("Coastal fern");
            for(int j=0;j<9;j++){var leaf=MeshObject("Fern leaf",frond,new[]{mats[j%2==0?"Leaf":"LeafLight"]},false,root.transform);leaf.transform.localScale=new Vector3(.45f,.6f,.32f);leaf.transform.localRotation=Quaternion.Euler(-35,j*43,0);}
            return root;
        }
        private static GameObject CreateFlower()
        {
            var root=new GameObject("Hibiscus cluster");
            for(int j=0;j<3;j++)
            {
                var p=new Vector3((j-1)*.24f,.4f+j*.12f,j*.12f);Beam(root.transform,"Stem",new Vector3(p.x,0,p.z),p,.035f,"Leaf");
                for(int k=0;k<5;k++){float a=k*Mathf.PI*2/5;Shape(root.transform,"Petal",PrimitiveType.Sphere,p+new Vector3(Mathf.Cos(a)*.12f,0,Mathf.Sin(a)*.12f),new Vector3(.23f,.08f,.23f),"Petal");}
                Shape(root.transform,"Pollen",PrimitiveType.Sphere,p+Vector3.up*.05f,Vector3.one*.12f,"Gold");
            }
            return root;
        }
        private static GameObject CreateRock()
        {
            var root=new GameObject("Coastal stone");var rock=Shape(root.transform,"Rock",PrimitiveType.Sphere,new Vector3(0,.3f,0),new Vector3(1.15f,.75f,.85f),"Rock",true);rock.transform.localRotation=Quaternion.Euler(8,25,14);return root;
        }
        private static GameObject CreateCabin()
        {
            var root=new GameObject("Reefkeeper cabin");var t=root.transform;
            Shape(t,"Foundation",PrimitiveType.Cube,new Vector3(0,.18f,0),new Vector3(5,.36f,4.5f),"Timber",true);
            for(int j=0;j<12;j++)
            {
                float y=.5f+j*.23f;Shape(t,"Front siding",PrimitiveType.Cube,new Vector3(0,y,-1.8f),new Vector3(4.6f,.2f,.12f),j%2==0?"Plank":"Timber");
                Shape(t,"Back siding",PrimitiveType.Cube,new Vector3(0,y,1.8f),new Vector3(4.6f,.2f,.12f),"Plank");
                foreach(float x in new[]{-2.25f,2.25f})Shape(t,"Side siding",PrimitiveType.Cube,new Vector3(x,y,0),new Vector3(.12f,.2f,3.6f),"Plank");
            }
            var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,1.65f,0);box.size=new Vector3(4.6f,3,3.6f);
            foreach(float x in new[]{-2.3f,2.3f})foreach(float z in new[]{-1.85f,1.85f})Shape(t,"Corner post",PrimitiveType.Cube,new Vector3(x,1.7f,z),new Vector3(.18f,3.2f,.18f),"Cream");
            for(int j=0;j<15;j++)
            {
                float z=-2.25f+j*.32f;
                foreach(float side in new[]{-1f,1f}){var panel=Shape(t,"Roof tile",PrimitiveType.Cube,new Vector3(side*1.28f,3.7f,z),new Vector3(3.05f,.15f,.35f),j%3==0?"Leaf":"Roof");panel.transform.localRotation=Quaternion.Euler(0,0,side*-26);}
            }
            Shape(t,"Roof ridge",PrimitiveType.Cylinder,new Vector3(0,4.35f,0),new Vector3(.22f,2.4f,.22f),"Cream").transform.localRotation=Quaternion.Euler(90,0,0);
            Shape(t,"Door",PrimitiveType.Cube,new Vector3(.45f,1.45f,-1.91f),new Vector3(1.15f,2.25f,.15f),"Roof");
            Shape(t,"Door handle",PrimitiveType.Sphere,new Vector3(.8f,1.4f,-2.03f),Vector3.one*.12f,"Gold");
            var door=new GameObject("Cabin interaction");door.transform.SetParent(t,false);door.transform.localPosition=new Vector3(.45f,1,-2.1f);door.AddComponent<BoxCollider>().size=new Vector3(1.2f,2,.25f);door.GetComponent<BoxCollider>().isTrigger=true;
            var interact=door.AddComponent<IslandInteractable>();interact.kind=InteractionKind.Cabin;interact.stableId="home.cabin";
            foreach(float x in new[]{-1.35f,1.55f})
            {
                Shape(t,"Window",PrimitiveType.Cube,new Vector3(x,2,-1.94f),new Vector3(.8f,.9f,.12f),"Shirt");
                Shape(t,"Window cross",PrimitiveType.Cube,new Vector3(x,2,-2.02f),new Vector3(.07f,1,.08f),"Cream");
                Shape(t,"Window cross",PrimitiveType.Cube,new Vector3(x,2,-2.02f),new Vector3(.9f,.07f,.08f),"Cream");
            }
            for(int j=0;j<14;j++)Shape(t,"Porch board",PrimitiveType.Cube,new Vector3(-2.4f+j*.37f,.27f,-2.65f),new Vector3(.34f,.16f,1.5f),"Plank",true);
            for(int j=0;j<2;j++)Shape(t,"Entry step",PrimitiveType.Cube,new Vector3(.45f,.12f-j*.08f,-3.5f-j*.3f),new Vector3(1.8f,.22f,.5f),"Plank",true);
            return root;
        }
        private static GameObject CreateChest()
        {
            var root=new GameObject("Supply chest");var t=root.transform;
            Shape(t,"Body",PrimitiveType.Cube,new Vector3(0,.42f,0),new Vector3(1.3f,.75f,.8f),"Timber",true);
            for(int j=0;j<5;j++)Shape(t,"Lid board",PrimitiveType.Cube,new Vector3(-.52f+j*.26f,.85f,0),new Vector3(.24f,.18f,.88f),"Plank");
            foreach(float x in new[]{-.42f,.42f})Shape(t,"Brass binding",PrimitiveType.Cube,new Vector3(x,.85f,0),new Vector3(.08f,.2f,.91f),"Gold");
            Shape(t,"Latch",PrimitiveType.Cube,new Vector3(0,.62f,-.43f),new Vector3(.18f,.28f,.08f),"Gold");
            var interact=root.AddComponent<IslandInteractable>();interact.kind=InteractionKind.Chest;interact.stableId="home.chest";return root;
        }
        private static GameObject CreateRod()
        {
            var root=new GameObject("Starter fishing rod");var visual=new GameObject("Rod visual");visual.transform.SetParent(root.transform,false);
            Beam(visual.transform,"Bamboo shaft",Vector3.zero,new Vector3(.4f,2,0),.045f,"Plank");Beam(visual.transform,"Cork grip",Vector3.zero,new Vector3(.09f,.45f,0),.085f,"Timber");
            Shape(visual.transform,"Reel",PrimitiveType.Cylinder,new Vector3(.05f,.4f,.08f),new Vector3(.16f,.05f,.16f),"Gold").transform.localRotation=Quaternion.Euler(90,0,0);
            Beam(visual.transform,"Fishing line",new Vector3(.4f,2,0),new Vector3(.6f,.5f,0),.008f,"Cream");
            var hit=root.AddComponent<BoxCollider>();hit.center=Vector3.up;hit.size=new Vector3(.5f,2,.4f);hit.isTrigger=true;
            var interact=root.AddComponent<IslandInteractable>();interact.kind=InteractionKind.StarterRod;interact.pickupVisual=visual;return root;
        }
        private static GameObject CreatePier()
        {
            var root=new GameObject("Wooden pier");var t=root.transform;
            for(int i=0;i<24;i++)Shape(t,"Weathered board",PrimitiveType.Cube,new Vector3(0,0,-i*.38f),new Vector3(3,.22f,.35f),i%4==0?"Timber":"Plank",true);
            foreach(float x in new[]{-1.3f,1.3f})for(int j=0;j<5;j++)
            {
                Beam(t,"Pier support",new Vector3(x,-1.2f,-j*2),new Vector3(x,.6f,-j*2),.2f,"Timber");
                if(j<4)Beam(t,"Rope rail",new Vector3(x,.6f,-j*2),new Vector3(x,.6f,-(j+1)*2),.045f,"Cream");
            }
            return root;
        }
        private static GameObject CreatePlayer()
        {
            var root=new GameObject("Local explorer");root.tag="Player";
            var motor=root.AddComponent<CharacterController>();motor.height=1.9f;motor.radius=.27f;motor.center=Vector3.up*.95f;motor.stepOffset=.3f;motor.slopeLimit=48;
            var controller=root.AddComponent<LocalPlayerController>();var visual=new GameObject("Avatar slots").transform;visual.SetParent(root.transform,false);controller.visual=visual;
            Shape(visual,"Shirt torso",PrimitiveType.Capsule,new Vector3(0,.95f,0),new Vector3(.63f,.36f,.43f),"Shirt");
            Shape(visual,"Collar",PrimitiveType.Sphere,new Vector3(0,1.21f,.15f),new Vector3(.4f,.14f,.15f),"Cream");
            var head=new GameObject("Head slot").transform;head.SetParent(visual,false);head.localPosition=new Vector3(0,1.53f,0);
            Shape(head,"Face",PrimitiveType.Sphere,Vector3.zero,new Vector3(.72f,.73f,.64f),"Skin");
            Shape(head,"Hair cap",PrimitiveType.Sphere,new Vector3(0,.23f,-.045f),new Vector3(.76f,.4f,.67f),"Timber");
            for(int j=0;j<5;j++)Shape(head,"Hair fringe",PrimitiveType.Sphere,new Vector3(-.26f+j*.13f,.21f,.23f),new Vector3(.18f,.24f,.15f),"Timber");
            foreach(float x in new[]{-.16f,.16f})
            {
                Shape(head,"Eye white",PrimitiveType.Sphere,new Vector3(x,.03f,.29f),new Vector3(.14f,.17f,.075f),"White");
                Shape(head,"Pupil",PrimitiveType.Sphere,new Vector3(x,.025f,.328f),new Vector3(.065f,.095f,.035f),"Dark");
                Shape(head,"Ear",PrimitiveType.Sphere,new Vector3(Mathf.Sign(x)*.36f,0,0),new Vector3(.14f,.22f,.14f),"Skin");
            }
            Shape(head,"Nose",PrimitiveType.Sphere,new Vector3(0,-.065f,.33f),new Vector3(.11f,.1f,.11f),"Skin");
            Shape(head,"Smile",PrimitiveType.Sphere,new Vector3(0,-.19f,.29f),new Vector3(.13f,.035f,.025f),"Timber");
            var motion=root.AddComponent<CharacterMotion>();motion.controller=controller;motion.head=head;
            foreach(float side in new[]{-1f,1f})
            {
                var arm=new GameObject(side<0?"Left arm":"Right arm").transform;arm.SetParent(visual,false);arm.localPosition=new Vector3(side*.37f,1.18f,0);
                Shape(arm,"Sleeve",PrimitiveType.Capsule,new Vector3(0,-.13f,0),new Vector3(.22f,.2f,.24f),"Shirt");
                Shape(arm,"Hand",PrimitiveType.Sphere,new Vector3(0,-.43f,0),new Vector3(.19f,.24f,.21f),"Skin");
                var leg=new GameObject(side<0?"Left leg":"Right leg").transform;leg.SetParent(visual,false);leg.localPosition=new Vector3(side*.17f,.7f,0);
                Shape(leg,"Trousers",PrimitiveType.Capsule,new Vector3(0,-.23f,0),new Vector3(.25f,.24f,.27f),"Cream");Shape(leg,"Boot",PrimitiveType.Sphere,new Vector3(0,-.59f,.055f),new Vector3(.27f,.23f,.4f),"Timber");
                if(side<0){motion.leftArm=arm;motion.leftLeg=leg;}else{motion.rightArm=arm;motion.rightLeg=leg;}
            }
            root.AddComponent<InteractionActor>();root.AddComponent<PlayerInteractor>().controller=controller;return root;
        }
        private static void CreatePlot()
        {
            var root=new GameObject("Future aquarium garden");root.transform.position=new Vector3(8,IslandGeometry.Height(8,3),3);
            for(int j=0;j<4;j++)
            {
                float x=j<2?-3:3,z=j%2==0?-2.5f:2.5f;Beam(root.transform,"Survey stake",new Vector3(x,0,z),new Vector3(x,.65f,z),.12f,"Plank");
                Shape(root.transform,"Sanctuary ribbon",PrimitiveType.Cube,new Vector3(x,.53f,z),new Vector3(.3f,.17f,.07f),"Roof");
            }
            var sign=Shape(root.transform,"Garden sign",PrimitiveType.Cube,new Vector3(0,.9f,-2.5f),new Vector3(1.4f,.7f,.14f),"Roof",true);
            Beam(root.transform,"Signpost",new Vector3(0,0,-2.5f),new Vector3(0,1,-2.5f),.12f,"Timber");
            var interact=sign.AddComponent<IslandInteractable>();interact.kind=InteractionKind.AquariumPlot;interact.stableId="home.aquarium.plot";
            // Fish emblem without baked text: localized purpose appears through interaction.
            Shape(sign.transform,"Fish emblem",PrimitiveType.Sphere,new Vector3(0,0,-.6f),new Vector3(.55f,.4f,.12f),"Cream");
        }
        private static void CreateClouds()
        {
            var root=new GameObject("Cloud banks");for(int j=0;j<12;j++)for(int k=0;k<4;k++)Shape(root.transform,"Cloud puff",PrimitiveType.Sphere,new Vector3(-80+j*15+k*3,35+Mathf.Sin(j)*4,65+j%3*12),new Vector3(10,4,6),"White");
        }
        private static void SetupLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.63f,.77f,.88f);RenderSettings.ambientEquatorColor=new Color(.51f,.61f,.56f);RenderSettings.ambientGroundColor=new Color(.28f,.29f,.2f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.55f,.79f,.83f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=100;RenderSettings.fogEndDistance=240;
            var sun=new GameObject("Afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.89f,.7f);sun.intensity=1.4f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(42,-35,0);RenderSettings.sun=sun;
            var sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/OF_Sky.mat");
            if(sky==null){sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetFloat("_AtmosphereThickness",.65f);sky.SetColor("_SkyTint",new Color(.42f,.59f,.7f));sky.SetFloat("_Exposure",1.1f);AssetDatabase.CreateAsset(sky,Root+"/Materials/OF_Sky.mat");}RenderSettings.skybox=sky;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Settings/OF_AfternoonVolume.asset");
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/Settings/OF_AfternoonVolume.asset");
                var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.Neutral);AssetDatabase.AddObjectToAsset(tone,profile);
                var color=profile.Add<ColorAdjustments>();color.saturation.Override(5);color.contrast.Override(5);AssetDatabase.AddObjectToAsset(color,profile);
            }
            var volume=new GameObject("Afternoon grading").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            var probe=new GameObject("Island sky reflection").AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(0,8,0);probe.size=new Vector3(65,25,65);probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.resolution=128;probe.clearFlags=ReflectionProbeClearFlags.Skybox;
        }
        private static void CreateLocalization()
        {
            var settings=ScriptableObject.CreateInstance<LocalizationSettings>();AssetDatabase.CreateAsset(settings,Root+"/Localization/OF_LocalizationSettings.asset");LocalizationEditorSettings.ActiveLocalizationSettings=settings;
            foreach(string code in LanguageService.Codes){var locale=Locale.CreateLocale(code);AssetDatabase.CreateAsset(locale,Root+"/Localization/Locales/"+code+".asset");LocalizationEditorSettings.AddLocale(locale);}
            var collection=LocalizationEditorSettings.CreateStringTableCollection("OF_UI",Root+"/Localization/Tables");LocalizationEditorSettings.CreateAssetTableCollection("OF_Assets",Root+"/Localization/Tables");
            var text=new Dictionary<string,string>
            {
                ["brand"]="OCEANARIUM\nFISHING",["tagline"]="A small island. A sanctuary to grow.",["play"]="Begin island exploration",["settings"]="Settings & language",["milestone"]="ASTRA 0.0  /  Foundation preview\nSolo exploration • No progress saving",
                ["home"]="HOME REEF",["objective"]="Explore the cabin, collect a rod at the pier, and discover your future aquarium garden.",["objective.done"]="Starter rod collected. Explore your island sanctuary.",
                ["controls"]="WASD  Move     SHIFT  Sprint     Hold RMB  Camera     E  Interact     ESC  Settings",["close"]="Close",["back"]="Return to main menu",["sensitivity"]="Camera sensitivity",
                ["language.note"]="17 locales prepared. Untranslated content uses English. Language preference is saved.",["reserved"]="COMING LATER",["reserved.body"]="This scene is reserved for a future milestone. No gameplay is implemented here.",
                ["interact.cabin"]="[E] Inspect cabin",["interact.chest"]="[E] Inspect supply chest",["interact.starterrod"]="[E] Collect starter rod",["interact.aquariumplot"]="[E] Discover sanctuary garden",
                ["notice.cabin"]="Your reefkeeper cabin. Interiors will arrive in a later milestone.",["notice.chest"]="A shared supply chest. Inventory and persistent storage are planned for later.",["notice.starterrod"]="Your first rod! Casting and fishing will arrive in the fishing milestone.",["notice.owned"]="You already have a rod. These are reserved for the rest of your future crew.",["notice.aquariumplot"]="A place for your future aquarium sanctuary. Construction comes in a later milestone."
            };
            string[] languages={"English","Turkish","French","Italian","German","Spanish (Spain)","Chinese (Simplified)","Portuguese (Brazil)","Dutch","Chinese (Traditional)","Japanese","Korean","Spanish (Latin America)","Polish","Portuguese (Portugal)","Russian","Ukrainian"};
            for(int i=0;i<languages.Length;i++)text["locale."+LanguageService.Codes[i]]=languages[i];
            foreach(var table in collection.StringTables)
            {
                foreach(var pair in text)table.AddEntry(pair.Key,pair.Value);
                // One QA marker demonstrates locale refresh without claiming translated game content.
                if(table.LocaleIdentifier.Code=="tr")table.AddEntry("settings","Ayarlar / Settings");
                EditorUtility.SetDirty(table);EditorUtility.SetDirty(table.SharedData);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

