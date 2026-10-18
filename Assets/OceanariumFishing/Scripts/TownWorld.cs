using UnityEngine;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(130)]
    public sealed class TownWorld:MonoBehaviour
    {
        public TownResidentDefinition[] residents;public float minutesPerSecond=2;
        public FishingController Inventory{get;private set;}public LocalPlayerController Player{get;private set;}
        public TownClock Clock{get;private set;}public TownTransactions Transactions{get;private set;}public TownHUD UI{get;private set;}
        public bool Ready=>Transactions!=null;
        public TownResident[] Actors{get;private set;}
        void Start(){Inventory=FindFirstObjectByType<FishingController>();Player=Inventory.GetComponent<LocalPlayerController>();Clock=WorldTimeService.Current?new(WorldTimeService.Current.Simulation):new(Inventory.Collection.town.elapsedMinutes);Transactions=new(()=>Inventory.Collection,Inventory.CommitAquarium,Clock,residents);UI=GetComponent<TownHUD>();Actors=GetComponentsInChildren<TownResident>();GetComponent<TownNavigation>().Build();}
        float saveAge;
        void Update(){if(!Ready)return;saveAge+=Time.deltaTime;if(saveAge>30){SaveClock();saveAge=0;}string visit=Player.transform.position.z< -28?"harbor":Vector2.Distance(new(Player.transform.position.x,Player.transform.position.z),new(0,1))<6?"plaza":null;if(visit!=null&&!Inventory.Collection.town.flags.Contains("visit:"+visit)){var copy=Inventory.Collection.Copy();copy.town.flags.Add("visit:"+visit);Inventory.CommitAquarium(copy);}}
        void SaveClock(){if(!Inventory)return;var copy=Inventory.Collection.Copy();copy.town.elapsedMinutes=Clock.Minutes;Inventory.CommitAquarium(copy);}
        void OnDisable(){if(Ready)SaveClock();}
        public bool CanTrade(TownResident npc)=>Ready&&npc&&npc.world==this&&npc.definition.Available(Clock)&&!Inventory.Active&&Vector3.Distance(Player.transform.position,npc.transform.position)<3.5f&&FindFirstObjectByType<BoatHarbor>()?.InputLocked!=true;
    }
}
