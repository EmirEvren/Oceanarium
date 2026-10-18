using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(125)]
    public sealed class AquariumBuildHUD:MonoBehaviour
    {
        public bool IsOpen {get;private set;}public string SelectedObject=>selected;AquariumBuilding build;CozyUI ui;BuildCategory category;Vector2 scroll,ownedScroll;string search="",selected,type,tank,message,invalid;bool grid=true,previewStyle;GameObject ghost;Material ghostMaterial;Camera cameraView;Vector3 focus,position;float yaw,pitch=58,distance=17,rotation;AquariumLayoutObject undo;int undoRevision;bool placing;
        string T(string key)=>GameSession.Current?.Languages.Text("build."+key)??key;
        void Awake(){build=GetComponent<AquariumBuilding>();}
        string Command()=>Guid.NewGuid().ToString("N");
        public void Open(){if(!build.Ready||build.World.Player.Active||FindFirstObjectByType<BoatHarbor>()?.InputLocked==true)return;build.World.Panel.Close();build.World.Player.GetComponent<CollectionHUD>().Close();IsOpen=true;distance=17;pitch=58;yaw=0;focus=transform.position+Vector3.up;var go=new GameObject("Museum editing camera",typeof(Camera));cameraView=go.GetComponent<Camera>();cameraView.fieldOfView=48;cameraView.nearClipPlane=.1f;cameraView.farClipPlane=180;cameraView.clearFlags=CameraClearFlags.Skybox;build.World.Player.GetComponent<LocalPlayerController>().view.enabled=false;build.World.Player.GetComponent<LocalPlayerController>().InputEnabled=false;tank=build.World.Tanks.FirstOrDefault()?.id;}
        public void Close(){Cancel();IsOpen=false;if(cameraView)Destroy(cameraView.gameObject);if(build.Ready)build.World.Player.GetComponent<LocalPlayerController>().view.enabled=true;}
        void OnDestroy(){if(cameraView)Destroy(cameraView.gameObject);if(ghostMaterial)Destroy(ghostMaterial);}
        void Cancel(){placing=false;selected=null;if(ghost)Destroy(ghost);if(previewStyle){build.RestoreStyles();previewStyle=false;}}
        public void Select(string id)
        {Cancel();selected=id;var o=build.Object(id);if(o==null)return;type=o.typeId;position=o.position;rotation=o.rotation;if(!string.IsNullOrEmpty(o.tankId))tank=o.tankId;placing=true;var item=build.Item(type);var original=item?.prefab??build.Visual(id);if(original){ghost=Instantiate(original);ghost.name="Placement preview";foreach(var c in ghost.GetComponentsInChildren<Collider>())c.enabled=false;foreach(var c in ghost.GetComponentsInChildren<MonoBehaviour>())c.enabled=false;foreach(var l in ghost.GetComponentsInChildren<Light>())l.enabled=false;ghostMaterial??=new Material(Shader.Find("Universal Render Pipeline/Unlit"));foreach(var r in ghost.GetComponentsInChildren<Renderer>())r.sharedMaterial=ghostMaterial;}}
        public bool Confirm()
        {if(!placing||invalid!=null)return false;var o=build.Object(selected);undo=JsonUtility.FromJson<AquariumLayoutObject>(JsonUtility.ToJson(o));bool ok=build.PlaceObject(build.World.Data.ownerId,Command(),build.Layout.revision,selected,position,rotation,AquariumBuilding.Aquatic(build.Item(type))?tank:null,out message);if(ok){undoRevision=build.Layout.revision;Cancel();message="saved";}return ok;}
        public void FocusTank(string id){tank=id;var t=build.World.Tanks.FirstOrDefault(t=>t.id==id);if(t){focus=t.swimVolume.position;distance=5;pitch=48;}}
        void Update()
        {
            if(!build.Ready)return;var k=Keyboard.current;var m=Mouse.current;
            if(k?.bKey.wasPressedThisFrame==true){if(IsOpen)Close();else if(Vector3.Distance(build.World.Player.transform.position,transform.position)<18)Open();}
            if(!IsOpen)return;if(k?.escapeKey.wasPressedThisFrame==true){if(placing||previewStyle)Cancel();else Close();return;}
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);bool pointerOverUI=m!=null&&(m.position.ReadValue().x<390*scale||m.position.ReadValue().y<140*scale||m.position.ReadValue().y>Screen.height-65*scale);
            if(m!=null&&m.rightButton.isPressed){var delta=m.delta.ReadValue();yaw+=delta.x*.2f;pitch=Mathf.Clamp(pitch-delta.y*.15f,25,80);}
            if(m!=null&&!pointerOverUI)distance=Mathf.Clamp(distance-m.scroll.ReadValue().y*.008f,3,30);
            if(k!=null&&GUIUtility.keyboardControl==0){var move=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));focus+=Quaternion.Euler(0,yaw,0)*move*Time.unscaledDeltaTime*5;if(k.rKey.wasPressedThisFrame)rotation=Mathf.Repeat(rotation+45,360);if(k.gKey.wasPressedThisFrame)grid=!grid;}
            if(placing&&m!=null){var item=build.Item(type);var aquarium=build.World.Tanks.FirstOrDefault(t=>t.id==tank);bool aquatic=AquariumBuilding.Aquatic(item);float height=aquatic&&aquarium?aquarium.swimVolume.position.y-aquarium.definition.dimensions.y/2+.08f:transform.position.y;var ray=cameraView.ScreenPointToRay(m.position.ReadValue());var plane=new Plane(Vector3.up,new Vector3(0,height,0));if(!pointerOverUI&&!m.rightButton.isPressed&&plane.Raycast(ray,out float hit)){var p=ray.GetPoint(hit);position=aquatic&&aquarium?aquarium.swimVolume.InverseTransformPoint(p):transform.InverseTransformPoint(p);position.y=0;float step=aquatic?.1f:.25f;if(grid){position.x=Mathf.Round(position.x/step)*step;position.z=Mathf.Round(position.z/step)*step;}}
                var candidate=new AquariumLayoutObject{id=selected,typeId=type,position=position,rotation=rotation,tankId=aquatic?tank:null,legacy=build.Object(selected).legacy,stored=false};invalid=build.Validate(build.Layout,candidate);
                if(ghost){ghost.transform.position=aquatic&&aquarium?aquarium.swimVolume.TransformPoint(position+Vector3.down*(aquarium.definition.dimensions.y/2-.08f)):transform.TransformPoint(position);ghost.transform.rotation=(aquatic&&aquarium?aquarium.transform.rotation:transform.rotation)*Quaternion.Euler(0,rotation,0);ghostMaterial.SetColor("_BaseColor",invalid==null?new Color(.25f,.8f,.5f):new Color(.9f,.3f,.25f));}
                if(!pointerOverUI&&m.leftButton.wasPressedThisFrame)Confirm();if(k?.enterKey.wasPressedThisFrame==true)Confirm();
            }
        }
        void LateUpdate(){if(cameraView){cameraView.transform.position=focus+Quaternion.Euler(pitch,yaw,0)*Vector3.back*distance;cameraView.transform.LookAt(focus);}}
        void OnGUI()
        {
            if(!build.Ready||GameSession.Current?.Languages.Ready!=true)return;ui??=new CozyUI();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float width=Screen.width/scale,height=Screen.height/scale;GUI.depth=-40;
            if(!IsOpen){if(Vector3.Distance(build.World.Player.transform.position,transform.position)<18&&!build.World.Panel.IsOpen&&GUI.Button(new Rect(width-270,80,240,45),T("open"),ui.button))Open();GUI.matrix=old;GUI.depth=0;return;}
            ui.Book(new Rect(12,12,365,height-24));GUI.Label(new Rect(30,27,330,40),T("title"),ui.title);GUI.Label(new Rect(30,72,330,30),T("coins")+": "+build.World.Save.coins+" · "+T("comfort")+": "+(build.Comfort*100).ToString("0")+"%",ui.small);
            for(int n=0;n<9;n++){int row=n/3,col=n%3;if(GUI.Button(new Rect(27+col*110,110+row*34,106,31),T(((BuildCategory)n).ToString()),ui.small)){category=(BuildCategory)n;scroll=Vector2.zero;}}
            search=GUI.TextField(new Rect(28,218,330,30),search);if(string.IsNullOrEmpty(search))GUI.Label(new Rect(36,222,300,24),T("search"),ui.small);
            var items=build.catalog.items.Where(i=>i.category==category&&(string.IsNullOrEmpty(search)||T(i.id).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();float listHeight=Mathf.Max(130,height-530);scroll=GUI.BeginScrollView(new Rect(26,260,338,listHeight),scroll,new Rect(0,0,315,items.Length*112));
            for(int n=0;n<items.Length;n++){var item=items[n];float y=n*112;bool locked=item.futureLocked||build.Layout.level<item.level||build.World.Score.Total<item.requiredPopularity;bool owned=build.Layout.purchases.Contains(item.id)||item.id=="floor_oak"||item.id=="wall_cream"||item.id=="sub_white";if(item.thumbnail)GUI.DrawTexture(new Rect(0,y,95,74),item.thumbnail,ScaleMode.ScaleToFit);GUI.Label(new Rect(102,y,207,36),T(item.id),ui.small);GUI.Label(new Rect(102,y+37,207,28),item.price+" "+T("coins")+" · "+(locked?T("locked"):owned?T("owned"):T("buy")),ui.small);
                if(GUI.Button(new Rect(2,y+77,307,30),T("select"),ui.button)){Cancel();type=item.id;if(item.category==BuildCategory.Floors||item.category==BuildCategory.Walls){build.PreviewStyle(item);previewStyle=true;}message=locked?"locked":null;}}
            GUI.EndScrollView();float oy=270+listHeight;GUI.Label(new Rect(28,oy,320,30),T("owned_objects"),ui.heading);var objects=build.Layout.objects.Where(o=>{var i=build.Item(o.typeId);return i?i.category==category:category==BuildCategory.Tanks;}).ToArray();ownedScroll=GUI.BeginScrollView(new Rect(26,oy+34,338,150),ownedScroll,new Rect(0,0,315,objects.Length*35));
            for(int n=0;n<objects.Length;n++){var o=objects[n];if(GUI.Button(new Rect(0,n*35,310,32),(o.stored?T("storage"):T("placed"))+" · "+(o.legacy?GameSession.Current.Languages.Text("aquarium."+o.id):T(o.typeId)),ui.small))Select(o.id);}GUI.EndScrollView();
            if(GUI.Button(new Rect(width-155,15,135,42),T("close"),ui.button))Close();GUI.Label(new Rect(405,20,width-580,45),T("camera_hints"),ui.small);
            var current=build.Item(type);ui.Book(new Rect(400,height-135,width-420,120));float x=420;
            if(placing){GUI.Label(new Rect(x,height-125,width-450,30),(invalid==null?"✓ "+T("valid"):"! "+T(invalid))+" · "+rotation.ToString("0")+"°",ui.heading);if(GUI.Button(new Rect(x,height-83,125,38),T("rotate"),ui.button))rotation=Mathf.Repeat(rotation+45,360);if(GUI.Button(new Rect(x+135,height-83,125,38),T("grid")+": "+T(grid?"on":"off"),ui.button))grid=!grid;GUI.enabled=invalid==null;if(GUI.Button(new Rect(x+270,height-83,135,38),T("confirm"),ui.button))Confirm();GUI.enabled=true;if(GUI.Button(new Rect(x+415,height-83,120,38),T("cancel"),ui.button))Cancel();if(GUI.Button(new Rect(x+545,height-83,120,38),T("store"),ui.button)){build.StoreObject(build.World.Data.ownerId,Command(),build.Layout.revision,selected,out message);Cancel();}}
            else if(current){GUI.Label(new Rect(x,height-125,width-450,32),T(current.id)+" · "+T(current.wallMounted?"wall_required":AquariumBuilding.Aquatic(current)?"aquatic_hint":"layout_hint"),ui.small);bool purchased=build.Layout.purchases.Contains(current.id)||current.price==0;
                if(GUI.Button(new Rect(x,height-83,180,38),T(purchased?"apply":"buy"),ui.button)){
                    string id=null;bool ok=purchased||build.Buy(build.World.Data.ownerId,Command(),build.Layout.revision,type,out id,out message);
                    if(ok){if(AquariumBuilding.Physical(current)||AquariumBuilding.Aquatic(current)){if(id!=null)Select(id);}else if(current.category!=BuildCategory.Expansions){build.Style(build.World.Data.ownerId,Command(),build.Layout.revision,type,tank,out message);previewStyle=false;}else message="expanded";}}
                if((AquariumBuilding.Aquatic(current)||current.category==BuildCategory.Substrates)&&GUI.Button(new Rect(x+190,height-83,260,38),T("choose_tank")+": "+(build.World.Tanks.FirstOrDefault(t=>t.id==tank)?.name??"—"),ui.button)){var ts=build.World.Tanks;int idx=Array.FindIndex(ts,t=>t.id==tank);if(ts.Length>0)FocusTank(ts[(idx+1)%ts.Length].id);}
                if(GUI.Button(new Rect(x+460,height-83,120,38),T("cancel"),ui.button))Cancel();
            }
            if(undo!=null&&GUI.Button(new Rect(width-165,70,145,36),T("undo"),ui.button)){if(undoRevision!=build.Layout.revision)message="stale";else if(undo.stored)build.StoreObject(build.World.Data.ownerId,Command(),build.Layout.revision,undo.id,out message);else build.PlaceObject(build.World.Data.ownerId,Command(),build.Layout.revision,undo.id,undo.position,undo.rotation,undo.tankId,out message);undo=null;}
            if(!string.IsNullOrEmpty(message))GUI.Label(new Rect(410,height-175,width-440,37),T(message),ui.heading);GUI.matrix=old;GUI.depth=0;
        }
    }
}


