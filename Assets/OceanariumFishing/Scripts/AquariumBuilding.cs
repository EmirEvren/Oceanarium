using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(115)]
    public sealed class AquariumBuilding:MonoBehaviour,IAquariumBuildAuthority
    {
        public AquariumBuildCatalog catalog;public GameObject hallPrefab;public Renderer[] floorSurfaces,wallSurfaces;public GameObject[] rearClosure;
        public AquariumWorld World {get;private set;}public AquariumLayout Layout=>World.Save.layout;public AquariumNavigation Navigation {get;private set;}
        public event Action Changed;public bool Ready {get;private set;}public AquariumBuildHUD UI {get;private set;}
        readonly Dictionary<string,GameObject> live=new();readonly Dictionary<string,GameObject> substrates=new();readonly List<GameObject> halls=new();
        readonly List<Bounds> fixedObstacles=new();public IReadOnlyList<Bounds> FixedObstacles=>fixedObstacles;
        public AquariumBuildItem Item(string id)=>catalog&&catalog.items!=null?catalog.items.FirstOrDefault(i=>i&&i.id==id):null;
        public AquariumLayoutObject Object(string id)=>Layout.objects.FirstOrDefault(o=>o.id==id);
        public static bool Aquatic(AquariumBuildItem i)=>i&&(i.category==BuildCategory.Plants||i.category==BuildCategory.Corals||i.category==BuildCategory.Decorations);
        public static bool Physical(AquariumBuildItem i)=>i&&(i.category==BuildCategory.Tanks||i.category==BuildCategory.Furniture);
        public void Initialize(AquariumWorld w)
        {
            World=w;UI=GetComponent<AquariumBuildHUD>();Navigation=new AquariumNavigation(this);
            var staged=w.Data.Copy();foreach(var t in GetComponentsInChildren<AquariumTank>(true))
            {live[t.id]=t.gameObject;t.world=w;if(!staged.aquarium.layout.objects.Exists(o=>o.id==t.id))staged.aquarium.layout.objects.Add(new(){id=t.id,typeId="legacy_"+t.id,position=t.transform.localPosition,rotation=t.transform.localEulerAngles.y,stored=false,legacy=true});}
            // Fixed reception and original benches remain obstacles; floor, walls and removable exhibits do not.
            foreach(var c in GetComponentsInChildren<Collider>())if(!c.GetComponentInParent<AquariumTank>()&&(c.name=="Reception donation counter"||c.name=="Seat slat"))
            {var bounds=c.bounds;fixedObstacles.Add(new Bounds(transform.InverseTransformPoint(bounds.center),bounds.size));}
            w.Player.CommitAquarium(staged);Ready=true;Apply();
        }
        public Vector2 Size(AquariumLayoutObject o)
        {var i=Item(o.typeId);if(i)return new(i.size.x,i.size.z);if(live.TryGetValue(o.id,out var g)&&g){var d=g.GetComponent<AquariumTank>().definition;return new(d.dimensions.x+.25f,d.dimensions.z+.2f);}return Vector2.one;}
        public Vector3 ViewPoint(AquariumLayoutObject o)=>o.position+Quaternion.Euler(0,o.rotation,0)*new Vector3(0,0,-Size(o).y/2-.75f);
        public bool Inside(Vector3 p,int level,float margin=.3f)=>p.x>=-5.7f+margin&&p.x<=5.7f-margin&&p.z>=-4.8f+margin&&p.z<=4.8f+(level-1)*8-margin;
        public bool IsTank(AquariumLayoutObject o)=>o.legacy||Item(o.typeId)?.category==BuildCategory.Tanks;
        public string Validate(AquariumLayout layout,AquariumLayoutObject candidate)
        {
            var item=Item(candidate.typeId);
            if(Aquatic(item))
            {
                var parent=layout.objects.FirstOrDefault(o=>o.id==candidate.tankId&&!o.stored&&IsTank(o));if(parent==null)return "choose_tank";
                var tank=World.Tanks.FirstOrDefault(t=>t.id==parent.id);if(!tank)return "choose_tank";
                if(item.habitats.Length>0&&!tank.definition.habitats.Any(h=>item.habitats.Contains(h)))return "habitat";
                var p=candidate.position;var dim=tank.definition.dimensions;float radius=new Vector2(item.size.x,item.size.z).magnitude*.5f;
                if(Mathf.Abs(p.x)+radius>dim.x/2-.12f||Mathf.Abs(p.z)+radius>dim.z/2-.12f||item.size.y>dim.y*.45f)return "bounds";
                var others=layout.objects.Where(o=>!o.stored&&o.id!=candidate.id&&o.tankId==parent.id&&Aquatic(Item(o.typeId))).ToArray();
                if(others.Sum(o=>Item(o.typeId).decorationCost)+item.decorationCost>tank.definition.decorationBudget)return "budget";
                foreach(var o in others)if(Vector2.Distance(new(p.x,p.z),new(o.position.x,o.position.z))<radius+new Vector2(Item(o.typeId).size.x,Item(o.typeId).size.z).magnitude*.5f+.05f)return "collision";
                return null;
            }
            var size=Size(candidate);if(layout.level>1&&Mathf.Abs(candidate.position.z-5)<size.magnitude*.5f+.2f&&Mathf.Abs(candidate.position.x)>1.1f){return "collision";}foreach(var corner in AquariumNavigation.Corners(candidate.position,candidate.rotation,size))if(!Inside(corner,layout.level,.08f))return "bounds";
            if(item&&item.wallMounted&&Mathf.Abs(candidate.position.x)<5.7f-size.x/2-.35f&&candidate.position.z<4.8f+(layout.level-1)*8-size.y/2-.35f)return "wall_required";
            if(AquariumNavigation.Overlap(candidate.position,candidate.rotation,size,new Vector3(0,0,-3.8f),0,new Vector2(2.2f,2.2f),.1f))return "entrance";
            foreach(var b in fixedObstacles)if(AquariumNavigation.Overlap(candidate.position,candidate.rotation,size,b.center,0,new(b.size.x,b.size.z),.15f))return "collision";
            foreach(var o in layout.objects.Where(o=>o.id!=candidate.id&&!o.stored&&(IsTank(o)||Physical(Item(o.typeId)))))if(AquariumNavigation.Overlap(candidate.position,candidate.rotation,size,o.position,o.rotation,Size(o),.16f))return "collision";
            var simulation=JsonUtility.FromJson<AquariumLayout>(JsonUtility.ToJson(layout));simulation.objects.RemoveAll(o=>o.id==candidate.id);simulation.objects.Add(candidate);
            return Navigation.Validate(simulation)?null:"path";
        }
        bool Authorize(string owner,string command,int revision,out string reason)
        {reason=null;if(owner!=World.Data.ownerId)reason="owner";else if(string.IsNullOrWhiteSpace(command)||Layout.commands.Contains(command))reason="replay";else if(revision!=Layout.revision)reason="stale";return reason==null;}
        bool Commit(FishCollection data,string command,out string reason)
        {data.aquarium.layout.revision++;data.aquarium.layout.commands.Add(command);if(!World.Player.CommitAquarium(data)){reason="save";return false;}Apply();World.Refresh();Changed?.Invoke();reason=null;return true;}
        public bool Buy(string owner,string command,int revision,string type,out string objectId,out string reason)
        {
            objectId=null;if(!Authorize(owner,command,revision,out reason))return false;var item=Item(type);if(!item){reason="missing";return false;}
            if(item.futureLocked||Layout.level<item.level||World.Score.Total<item.requiredPopularity){reason="locked";return false;}
            bool unique=!Physical(item)&&!Aquatic(item);if(unique&&Layout.purchases.Contains(type)){reason="owned";return false;}
            if(World.Save.coins<item.price){reason="coins_low";return false;}var data=World.Data.Copy();var layout=data.aquarium.layout;data.aquarium.coins-=item.price;
            data.aquarium.transactions.Add(new(){id="build:"+command,kind="build-purchase",amount=-item.price,utc=DateTime.UtcNow.ToString("O")});
            if(unique){layout.purchases.Add(type);if(item.category==BuildCategory.Expansions)layout.level=Math.Max(layout.level,item.level+1);}
            else{objectId=Guid.NewGuid().ToString("N");layout.objects.Add(new(){id=objectId,typeId=type});if(item.tank)data.aquarium.tanks.Add(new(){id=objectId,definitionId=item.tank.id});}
            return Commit(data,command,out reason);
        }
        public bool PlaceObject(string owner,string command,int revision,string id,Vector3 position,float rotation,string tank,out string reason)
        {
            if(!Authorize(owner,command,revision,out reason))return false;var data=World.Data.Copy();var o=data.aquarium.layout.objects.FirstOrDefault(o=>o.id==id);if(o==null){reason="missing";return false;}
            o.position=position;o.rotation=Mathf.Repeat(rotation,360);o.tankId=tank;o.stored=false;o.roomId=tank??(position.z>12.8f?"ocean_gallery":position.z>4.8f?"exhibit_hall":"starter");
            reason=Validate(data.aquarium.layout,o);if(reason!=null)return false;return Commit(data,command,out reason);
        }
        public bool StoreObject(string owner,string command,int revision,string id,out string reason)
        {
            if(!Authorize(owner,command,revision,out reason))return false;var data=World.Data.Copy();var o=data.aquarium.layout.objects.FirstOrDefault(o=>o.id==id);if(o==null){reason="missing";return false;}o.stored=true;
            if(IsTank(o))foreach(var fish in data.kept.Where(f=>f.tankId==id&&f.ownership==FishOwnership.Displayed).ToArray())data.TryMove(fish.instanceId,data.ownerId,FishOwnership.Stored);
            return Commit(data,command,out reason);
        }
        public bool Style(string owner,string command,int revision,string type,string tank,out string reason)
        {
            if(!Authorize(owner,command,revision,out reason))return false;var i=Item(type);if(!i||!(Layout.purchases.Contains(type)||type=="floor_oak"||type=="wall_cream"||type=="sub_white")){reason="owned_required";return false;}
            var data=World.Data.Copy();if(i.category==BuildCategory.Floors)data.aquarium.layout.floor=type;else if(i.category==BuildCategory.Walls)data.aquarium.layout.wall=type;else if(i.category==BuildCategory.Substrates){var t=data.aquarium.tanks.FirstOrDefault(t=>t.id==tank);if(t==null){reason="choose_tank";return false;}t.substrate=type;}else{reason="missing";return false;}return Commit(data,command,out reason);
        }
        public void PreviewStyle(AquariumBuildItem i){if(i.category==BuildCategory.Floors)Paint(floorSurfaces,i.material);if(i.category==BuildCategory.Walls)Paint(wallSurfaces,i.material);}
        static void Paint(Renderer[] renderers,Material material){if(!material||renderers==null)return;foreach(var r in renderers)if(r)r.sharedMaterial=material;}
        public void RestoreStyles(){Paint(floorSurfaces,Item(Layout.floor)?.material);Paint(wallSurfaces,Item(Layout.wall)?.material);}
        int clearedLevel=1; readonly List<Mesh> gardenMeshes=new();
        void ClearExpandedGarden(){if(Layout.level<=clearedLevel)return;clearedLevel=Layout.level;var area=new Bounds(transform.TransformPoint(new Vector3(0,2,5+(Layout.level-1)*4)),new Vector3(12,9,(Layout.level-1)*8));foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>!t.IsChildOf(transform)&&(t.name.StartsWith("OF_Palm")||t.name.StartsWith("OF_TropicalTree")||t.name=="OF_Bush"||t.name.StartsWith("OF_Rock"))).ToArray())if(t.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.Intersects(area)))t.gameObject.SetActive(false);foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>!f.transform.IsChildOf(transform))){string hierarchy=f.name;for(var p=f.transform.parent;p;p=p.parent)hierarchy+=p.name;if(!(hierarchy.Contains("Meadow grass")||hierarchy.Contains("Tropical flower beds")||hierarchy.Contains("Understory")))continue;var source=f.sharedMesh;if(!source||!source.isReadable)continue;var mesh=UnityEngine.Object.Instantiate(source);var vertices=source.vertices;for(int sub=0;sub<source.subMeshCount;sub++){var tri=source.GetTriangles(sub);var keep=new List<int>();for(int i=0;i<tri.Length;i+=3){var p=f.transform.TransformPoint((vertices[tri[i]]+vertices[tri[i+1]]+vertices[tri[i+2]])/3);if(!area.Contains(p))keep.AddRange(new[]{tri[i],tri[i+1],tri[i+2]});}mesh.SetTriangles(keep,sub);}f.sharedMesh=mesh;gardenMeshes.Add(mesh);}}
        void OnDestroy(){foreach(var m in gardenMeshes)if(m)Destroy(m);}
        public void Apply()
        {
            ClearExpandedGarden();foreach(var closure in rearClosure)if(closure)closure.SetActive(Layout.level==1);
            while(halls.Count<Layout.level-1){var h=Instantiate(hallPrefab,transform);h.name=halls.Count==0?"Expanded Exhibit Hall":"Ocean Gallery";h.transform.localPosition=new Vector3(0,0,9+halls.Count*8);halls.Add(h);}
            for(int n=0;n<halls.Count;n++){halls[n].SetActive(n<Layout.level-1);var end=halls[n].transform.Find("End wall");if(end)end.gameObject.SetActive(n==Layout.level-2);}
            foreach(var o in Layout.objects.Where(o=>!Aquatic(Item(o.typeId))))
            {
                var item=Item(o.typeId);if(!live.TryGetValue(o.id,out var go)||!go){if(!item?.prefab)continue;go=Instantiate(item.prefab,transform);go.AddComponent<AquariumBuildObject>().id=o.id;live[o.id]=go;if(item.tank){var t=go.GetComponent<AquariumTank>();t.id=o.id;t.world=World;}}
                go.SetActive(!o.stored);go.transform.localPosition=o.position;go.transform.localRotation=Quaternion.Euler(0,o.rotation,0);
            }
            World.ReindexTanks();
            foreach(var o in Layout.objects.Where(o=>Aquatic(Item(o.typeId))))
            {
                var parent=World.Tanks.FirstOrDefault(t=>t.id==o.tankId);if(!live.TryGetValue(o.id,out var go)||!go){if(o.stored||!parent)continue;go=Instantiate(Item(o.typeId).prefab);go.AddComponent<AquariumBuildObject>().id=o.id;live[o.id]=go;}
                go.SetActive(!o.stored&&parent);if(parent){go.transform.SetParent(parent.swimVolume,false);go.transform.localPosition=o.position+Vector3.down*(parent.definition.dimensions.y/2-.08f);go.transform.localRotation=Quaternion.Euler(0,o.rotation,0);}
            }
            foreach(var t in World.Tanks)
            {
                var selected=World.Save.tanks.FirstOrDefault(s=>s.id==t.id)?.substrate??"sub_white";if(!Item(selected))selected="sub_white";
                if(substrates.TryGetValue(t.id,out var old)&&old&&old.name==selected)continue;if(old)Destroy(old);
                var item=Item(selected);if(item?.prefab){var surface=Instantiate(item.prefab,t.swimVolume);surface.name=selected;surface.transform.localPosition=Vector3.down*(t.definition.dimensions.y/2-.04f);surface.transform.localScale=new Vector3(t.definition.dimensions.x-.06f,1,t.definition.dimensions.z-.06f);substrates[t.id]=surface;}
            }
            RestoreStyles();foreach(var h in halls){foreach(var r in h.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Floor"))r.sharedMaterial=Item(Layout.floor).material;else if(r.name.StartsWith("Interior"))r.sharedMaterial=Item(Layout.wall).material;}
            Navigation.Rebuild(Layout);
        }
        public float DecorationScore(AquariumLayout data)
        {float total=(data.level-1)*7;foreach(var group in data.objects.Where(o=>!o.stored&&Item(o.typeId)&&(string.IsNullOrEmpty(o.tankId)||data.objects.Any(t=>t.id==o.tankId&&!t.stored))).GroupBy(o=>o.typeId)){int n=0;foreach(var o in group)total+=Item(o.typeId).popularity/(1+n++*2);}int themes=data.objects.Where(o=>!o.stored&&Item(o.typeId)?.theme=="coastal").Select(o=>o.typeId).Distinct().Count();return total+Mathf.Min(5,themes)*.4f;}
        public float Comfort=>Mathf.Clamp01(.2f+Layout.objects.Where(o=>!o.stored).Sum(o=>Item(o.typeId)?.comfort??0)*.05f+World.Tanks.Select(t=>t.definition.id).Distinct().Count()*.035f-World.ActiveVisitors*.018f);
        public bool SafeFishPoint(AquariumTank tank,Vector3 p,float radius=.18f)
        {foreach(var o in Layout.objects.Where(o=>!o.stored&&o.tankId==tank.id&&Aquatic(Item(o.typeId)))){var item=Item(o.typeId);var center=o.position+Vector3.down*(tank.definition.dimensions.y/2-.08f)+Vector3.up*item.size.y*.5f;var local=Quaternion.Euler(0,-o.rotation,0)*(p-center);var half=item.size*.5f+Vector3.one*radius;if(Mathf.Abs(local.x)<half.x&&Mathf.Abs(local.y)<half.y&&Mathf.Abs(local.z)<half.z)return false;}return true;}
        public GameObject Visual(string id)=>live.TryGetValue(id,out var g)?g:null;
    }
}




