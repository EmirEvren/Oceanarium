using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(130)]
    public sealed class BoatHarbor:MonoBehaviour
    {
        public string islandId="home";public Transform boat,captain,dockExit;public Transform[] seats;public GameObject damage,repairedVisual;public ParticleSystem smoke,wake,sparks;public IslandRoute[] routes;public BoatUpgrade[] upgrades;
        public LocalPlayerController Player {get;private set;}public FishingController Inventory {get;private set;}public TravelProgress Progress=>Inventory.Collection.travel;
        public BoatTravelState State {get;private set;}=BoatTravelState.Docked;public BoatReadiness Readiness {get;}=new();public bool Busy {get;private set;}public bool MapOpen {get;private set;}public bool DialogueOpen {get;private set;}public bool InputLocked=>Busy||MapOpen||DialogueOpen||Readiness.Seats.Count>0;
        public string Message {get;private set;}public string CinematicKey {get;private set;}public bool Ready=>Inventory&&Player;public bool SkipRequested {get;set;}
        public bool HasPassenger=>Ready&&Readiness.Seats.ContainsKey(Inventory.Collection.ownerId);public event Action StateChanged;
        Vector3 dockPosition;Quaternion dockRotation;Camera cinematicCamera;float elapsed;bool introQueued;BoatTravelHUD hud;
        public string Owner=>Inventory.Collection.ownerId;public int Count(string id)=>Inventory.Collection.resources.FirstOrDefault(s=>s.id==id)?.count??0;
        IEnumerator Start()
        {
            while(!GamePlayers.Inventory)yield return null;
            Player=GamePlayers.Local;Inventory=GamePlayers.Inventory;hud=GetComponent<BoatTravelHUD>();dockPosition=boat.position;dockRotation=boat.rotation;
            while(!GameSession.Current||!GameSession.Current.Languages.Ready||!Inventory||Inventory.Collection==null)yield return null;
            Readiness.Connect(Owner);Refresh();GameSession.Current.TransitionFailed+=LoadFailed;
            if(Progress.pendingDestination==islandId){StartCoroutine(Arrival());}
        }
        void OnDestroy(){if(GameSession.Current)GameSession.Current.TransitionFailed-=LoadFailed;if(cinematicCamera)Destroy(cinematicCamera.gameObject);}
        void Update()
        {
            if(!Ready||WorldPersistence.ReadOnly)return;elapsed+=Time.deltaTime;
            if(Busy&&Keyboard.current?.escapeKey.wasPressedThisFrame==true)SkipRequested=true;
            if(!Busy){boat.position=dockPosition+Vector3.up*Mathf.Sin(Time.time*1.6f)*.035f;boat.rotation=dockRotation*Quaternion.Euler(0,0,Mathf.Sin(Time.time*1.2f)*1.2f);}
            if(islandId=="home"&&!Progress.introduced&&!introQueued&&elapsed>8&&Player.GetComponent<InteractionActor>().HasStarterRod&&!Inventory.Active&&FindFirstObjectByType<AquariumBuildHUD>()?.IsOpen!=true&&FindFirstObjectByType<AquariumHUD>()?.IsOpen!=true){introQueued=true;StartCoroutine(Introduction());}
            if(!Busy&&HasPassenger&&Keyboard.current?.eKey.wasPressedThisFrame==true)Leave();
            if(!Busy&&Keyboard.current?.escapeKey.wasPressedThisFrame==true){DialogueOpen=false;if(MapOpen)Leave();}
        }
        bool Commit(FishCollection staged){if(!Inventory.CommitAquarium(staged)){Message="save_failed";return false;}Refresh();return true;}
        public void Refresh(){if(!Ready)return;damage.SetActive(!Progress.repaired);repairedVisual.SetActive(Progress.repaired);if(smoke){if(Progress.repaired)smoke.Stop();else if(!smoke.isPlaying)smoke.Play();}foreach(var pickup in FindObjectsByType<BoatResource>(FindObjectsSortMode.None))pickup.Refresh();}
        public bool Pickup(string id,string resource,int amount)
        {
            if(!Ready||Busy||amount<=0||Progress.collected.Contains(id))return false;
            var source=FindObjectsByType<BoatResource>(FindObjectsSortMode.None).FirstOrDefault(p=>p.id==id&&p.resource==resource&&p.amount==amount);if(!source||Vector3.Distance(Player.transform.position,source.transform.position)>3)return false;
            var data=Inventory.Collection.Copy();var stack=data.resources.FirstOrDefault(s=>s.id==resource);if(stack==null){stack=new(){id=resource};data.resources.Add(stack);}stack.count+=amount;data.travel.collected.Add(id);if(!Commit(data))return false;Message="pickup_"+resource;return true;
        }
        public void Talk(){if(Busy)return;DialogueOpen=true;Message=Progress.repaired?"finn_repaired":!Progress.accepted?"finn_intro":!Progress.inspected?"finn_inspect":MaterialsReady?"finn_ready":"finn_materials";}
        public bool Accept(){if(Busy||Progress.accepted)return false;var data=Inventory.Collection.Copy();data.travel.accepted=true;data.travel.introduced=true;bool ok=Commit(data);if(ok)Message="finn_inspect";return ok;}
        public bool Inspect(){if(Busy||!Progress.accepted||Progress.inspected||Vector3.Distance(Player.transform.position,dockExit.position)>5)return false;var data=Inventory.Collection.Copy();data.travel.inspected=true;bool ok=Commit(data);Message="finn_materials";return ok;}
        public bool MaterialsReady=>Count("driftwood")>=5&&Count("scrap")>=3&&Count("rope")>=2;
        public bool Repair()
        {
            if(Busy||Progress.repaired||!Progress.accepted||!Progress.inspected||!MaterialsReady||Vector3.Distance(Player.transform.position,captain.position)>4)return false;
            // Repair intent and resource spending commit together. A skipped/interrupted reveal cannot charge twice.
            var data=Inventory.Collection.Copy();foreach(var need in new[]{("driftwood",5),("scrap",3),("rope",2)})data.resources.First(s=>s.id==need.Item1).count-=need.Item2;data.travel.repaired=true;data.travel.boatTier=1;data.travel.flags.Add("boat_repaired");if(!data.unlockedIslands.Contains("OF_TownIsland"))data.unlockedIslands.Add("OF_TownIsland");if(!Commit(data))return false;StartCoroutine(RepairReveal());return true;
        }
        public string Permission(IslandRoute route)
        {if(!route||route.id==islandId)return "current_island";if(route.futureLocked)return route.requirementKey;if(route.requiresRepair&&!Progress.repaired)return "repair_required";if(Progress.boatTier<route.requiredTier)return "upgrade_required";if(route.requiredFlags!=null&&route.requiredFlags.Any(f=>!Progress.flags.Contains(f)))return route.requirementKey;return null;}
        public bool PurchaseUpgrade(string id,out string reason)
        {var u=upgrades.FirstOrDefault(u=>u.id==id);reason=null;if(!u||u.futureLocked||!Progress.repaired||u.tier!=Progress.boatTier+1||(!string.IsNullOrEmpty(u.requiredFlag)&&!Progress.flags.Contains(u.requiredFlag))){reason="upgrade_locked";return false;}if(Inventory.Collection.aquarium.coins<u.price){reason="coins";return false;}var data=Inventory.Collection.Copy();data.aquarium.coins-=u.price;data.travel.boatTier=u.tier;data.aquarium.transactions.Add(new(){id="boat:"+id,kind="boat-upgrade",amount=-u.price,utc=DateTime.UtcNow.ToString("O")});return Commit(data);}
        public bool Board()
        {if(Busy||!Progress.repaired||HasPassenger||Vector3.Distance(Player.transform.position,dockExit.position)>4)return false;DialogueOpen=false;Player.GetComponent<CollectionHUD>().Close();if(!Readiness.Board(Owner,out int seat))return false;Set(BoatTravelState.Boarding);StartCoroutine(Boarding(seat));return true;}
        IEnumerator Boarding(int seat)
        {Busy=true;Player.enabled=false;var start=Player.transform.position;float time=0;while(time<.7f){time+=Time.deltaTime;Player.Teleport(Vector3.Lerp(start,seats[seat].position,Mathf.SmoothStep(0,1,time/.7f)));yield return null;}Player.transform.SetParent(boat,true);Player.transform.rotation=seats[seat].rotation;Readiness.SetReady(Owner,true);Busy=false;MapOpen=true;Set(BoatTravelState.DestinationSelection);}
        public void Leave(){if(Busy||!HasPassenger)return;Readiness.Leave(Owner);MapOpen=false;Player.transform.SetParent(null,true);Player.Teleport(dockExit.position);Player.enabled=true;Player.InputEnabled=true;Set(BoatTravelState.Docked);}
        public bool RequestTravel(string destination)
        {var route=routes.FirstOrDefault(r=>r.id==destination);Message=Permission(route);if(Busy||!HasPassenger||!Readiness.CanDepart||Message!=null)return false;MapOpen=false;DialogueOpen=false;StartCoroutine(Departure(route));return true;}
        void Set(BoatTravelState state){State=state;StateChanged?.Invoke();}
        void BeginCamera(string key)
        {Busy=true;SkipRequested=false;CinematicKey=key;DialogueOpen=false;MapOpen=false;FindFirstObjectByType<AquariumBuildHUD>()?.Close();FindFirstObjectByType<AquariumHUD>()?.Close();Player.GetComponent<CollectionHUD>().Close();Player.InputEnabled=false;Player.enabled=false;Player.view.enabled=false;cinematicCamera=new GameObject("Harbor cinematic camera",typeof(Camera)).GetComponent<Camera>();cinematicCamera.fieldOfView=48;cinematicCamera.nearClipPlane=.1f;cinematicCamera.farClipPlane=220;cinematicCamera.clearFlags=CameraClearFlags.Skybox;}
        void EndCamera(){if(cinematicCamera)Destroy(cinematicCamera.gameObject);Player.view.enabled=true;Player.enabled=true;Player.InputEnabled=true;Busy=false;CinematicKey=null;SkipRequested=false;}
        void Shot(Vector3 point,Vector3 offset){if(cinematicCamera){cinematicCamera.transform.position=point+offset;cinematicCamera.transform.LookAt(point+Vector3.up*.8f);}}
        public IEnumerator Introduction()
        {
            if(Busy||Progress.introduced)yield break;BeginCamera("arrival_story");captain.SetParent(boat,true);captain.localPosition=new Vector3(0,.95f,1.15f);var far=dockPosition+new Vector3(5,0,-25);float time=0;
            while(time<24&&!SkipRequested){time+=Time.deltaTime;float f=Mathf.SmoothStep(0,1,time/20);boat.position=Vector3.Lerp(far,dockPosition,f)+Vector3.up*Mathf.Sin(time*2)*.09f;boat.rotation=dockRotation*Quaternion.Euler(0,0,Mathf.Sin(time*2.2f)*3);Shot(boat.position,new Vector3(-7,4,-8));yield return null;}
            boat.position=dockPosition;boat.rotation=dockRotation;captain.SetParent(transform,true);captain.position=dockExit.position+Vector3.right*1.3f;var data=Inventory.Collection.Copy();data.travel.introduced=true;Commit(data);EndCamera();Message="finn_intro";
        }
        IEnumerator RepairReveal()
        {
            BeginCamera("repair_reveal");damage.SetActive(true);repairedVisual.SetActive(false);float time=0;sparks.Play();
            while(time<13&&!SkipRequested){time+=Time.deltaTime;Shot(boat.position,new Vector3(6,3,-6));if(time>7){damage.SetActive(false);repairedVisual.SetActive(true);if(smoke)smoke.Stop();}yield return null;}
            sparks.Stop();Refresh();EndCamera();Message="travel_unlocked";
        }
        IEnumerator Departure(IslandRoute route)
        {
            BeginCamera("departing");Set(BoatTravelState.Departure);if(wake)wake.Play();float time=0;
            while(time<10&&!SkipRequested){time+=Time.deltaTime;boat.position=dockPosition+new Vector3(0,0,-time*1.8f);boat.rotation=dockRotation*Quaternion.Euler(0,0,Mathf.Sin(time*2)*1.5f);Shot(boat.position,new Vector3(-8,5,9));yield return null;}
            var data=Inventory.Collection.Copy();data.travel.pendingDestination=route.id;data.travel.location=dockExit.position;if(!Commit(data)){Recover();yield break;}Set(BoatTravelState.SceneLoading);GameSession.Current.Travel(route.destination);
        }
        IEnumerator Arrival()
        {
            BeginCamera("arriving");Set(BoatTravelState.Arrival);Readiness.Board(Owner,out int seat);Player.Teleport(seats[seat].position);Player.transform.SetParent(boat,true);var far=dockPosition+Vector3.back*18;float time=0;if(wake)wake.Play();
            while(time<8&&!SkipRequested){time+=Time.deltaTime;boat.position=Vector3.Lerp(far,dockPosition,Mathf.SmoothStep(0,1,time/8));Shot(boat.position,new Vector3(8,5,-7));yield return null;}
            boat.position=dockPosition;boat.rotation=dockRotation;if(wake)wake.Stop();Player.transform.SetParent(null,true);Player.Teleport(dockExit.position);Readiness.Leave(Owner);var data=Inventory.Collection.Copy();data.travel.pendingDestination=null;data.travel.lastIsland=islandId;data.travel.location=dockExit.position;if(!data.travel.discovered.Contains(islandId))data.travel.discovered.Add(islandId);Commit(data);EndCamera();Set(BoatTravelState.TravelComplete);Message="island_"+islandId;
        }
        void LoadFailed(Destination destination,string reason){Message="load_failed";Recover();}
        void Recover(){boat.position=dockPosition;Player.transform.SetParent(null,true);Player.Teleport(dockExit.position);Readiness.Leave(Owner);var data=Inventory.Collection.Copy();data.travel.pendingDestination=null;Commit(data);EndCamera();Set(BoatTravelState.Docked);}
        public void CloseDialogue(){DialogueOpen=false;}
    }
}
