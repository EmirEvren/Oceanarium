using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class AquariumWorld:MonoBehaviour,IAquariumAuthority
    {
        public AquariumEconomy economy;public FishSpecies[] species;public Material fishMaterial;public Transform entrance,arrival,donation;
        public GameObject comfortVisual,signageVisual,decorVisual;public Renderer[] roof;
        public AquariumTank[] Tanks {get;private set;}public FishingController Player {get;private set;}public AquariumHUD Panel {get;private set;}
        public FishCollection Data=>Player.Collection;public AquariumSave Save=>Data.aquarium;public AquariumScore Score {get;private set;}=new();
        public AquariumBuilding Building {get;private set;} public float Hour=12;public int ActiveVisitors=>visitors.Count;public int Rank=>Mathf.Max(0,Array.FindLastIndex(economy.ranks,r=>Score.Total>=r));
        readonly List<AquariumVisitor> visitors=new();float clock=5;public bool Ready=>Player!=null;
        void Start()
        {
            Player=FindFirstObjectByType<FishingController>();Tanks=GetComponentsInChildren<AquariumTank>();Panel=GetComponent<AquariumHUD>();
            if(!Player)return;Building=GetComponent<AquariumBuilding>();if(Building)Building.Initialize(this);ReindexTanks();var staged=Data.Copy();
            foreach(var tank in Tanks){tank.world=this;if(!staged.aquarium.tanks.Exists(t=>t.id==tank.id))staged.aquarium.tanks.Add(new(){id=tank.id,definitionId=tank.definition.id});}
            // Recover orphaned display assignments into the same owner's storage.
            foreach(var f in staged.kept.Where(f=>f.ownership==FishOwnership.Displayed&&!Tanks.Any(t=>t.id==f.tankId)))staged.TryMove(f.instanceId,staged.ownerId,FishOwnership.Stored);
            Player.CommitAquarium(staged);Refresh();
        }
        public void ReindexTanks(){Tanks=GetComponentsInChildren<AquariumTank>();foreach(var t in Tanks)t.world=this;}
        public FishSpecies Species(string id)=>species.FirstOrDefault(s=>s.id==id);
        public bool Unlocked(AquariumTank tank)=>!tank.definition.futureLocked&&(!string.IsNullOrEmpty(tank.definition.buildType)||tank.definition.size!=AquariumTankSize.Medium||Save.upgrades.Contains("medium"));
        public IEnumerable<FishInstance> Display(AquariumTank tank)=>Data.kept.Where(f=>f.ownership==FishOwnership.Displayed&&f.tankId==tank.id);
        public string Validate(FishInstance fish,AquariumTank tank)
        {
            if(fish==null||tank==null||fish.ownerId!=Data.ownerId)return "owner";
            if(!Unlocked(tank))return "locked";
            if(fish.ownership!=FishOwnership.Stored)return "displayed";
            var s=Species(fish.speciesId);if(!s||!fish.aquariumEligible||!tank.definition.habitats.Contains(s.aquariumHabitat))return "habitat";
            if(fish.length>tank.definition.maxLength)return "size";
            if(Display(tank).Count()>=tank.definition.capacity)return "full";return null;
        }
        public bool Place(string owner,string specimen,string tankId,out string reason)
        {
            var tank=Tanks.FirstOrDefault(t=>t.id==tankId);var fish=Data.kept.Find(f=>f.instanceId==specimen);
            reason=owner!=Data.ownerId?"owner":Validate(fish,tank);if(reason!=null)return false;
            var staged=Data.Copy();if(!staged.TryMove(specimen,owner,FishOwnership.Displayed,tankId)){reason="displayed";return false;}return Commit(staged,out reason);
        }
        public bool Remove(string owner,string specimen,out string reason)
        {var staged=Data.Copy();if(!staged.TryMove(specimen,owner,FishOwnership.Stored)){reason="owner";return false;}return Commit(staged,out reason);}
        bool Commit(FishCollection staged,out string reason)
        {staged.aquarium.popularity=Calculate(staged).Total;if(!Player.CommitAquarium(staged)){reason="save";return false;}reason=null;Refresh();return true;}
        public AquariumScore Calculate(FishCollection data)
        {
            var c=economy;var score=new AquariumScore();var displayed=data.kept.Where(f=>f.ownership==FishOwnership.Displayed&&Tanks.Any(t=>t.id==f.tankId)).ToArray();
            foreach(var group in displayed.GroupBy(f=>f.speciesId))
            {score.diversity+=c.speciesPoints;int i=0;foreach(var f in group.OrderByDescending(f=>f.quality)){float diminishing=i++==0?1:c.duplicateFactor/(i-1);score.rarity+=(int)f.rarity*c.rarityPoints*diminishing;score.quality+=Mathf.Clamp01(f.quality)*c.qualityPoints*diminishing;}}
            var occupied=Tanks.Where(t=>displayed.Any(f=>f.tankId==t.id)).ToArray();score.variety=occupied.Select(t=>t.definition.id).Distinct().Count()*c.tankVarietyPoints;
            score.habitat=occupied.Length*c.habitatPoints;score.decor=data.aquarium.tanks.Count(t=>t.decorated)*c.decorationPoints;score.upgrades=data.aquarium.upgrades.Count*c.upgradePoints;if(Building&&Building.Ready)score.decor+=Building.DecorationScore(data.aquarium.layout);return score;
        }
        public float Contribution(FishInstance f)=>(int)f.rarity*economy.rarityPoints+f.quality*economy.qualityPoints;
        public bool Collect(out string reason)
        {
            var staged=Data.Copy();int amount=staged.aquarium.pending;if(amount<=0){reason="empty_donations";return false;}
            staged.aquarium.pending=0;staged.aquarium.coins+=amount;staged.aquarium.totalCollected+=amount;
            staged.aquarium.transactions.Add(new(){id=Guid.NewGuid().ToString("N"),kind="collection",amount=amount,utc=DateTime.UtcNow.ToString("O")});return Commit(staged,out reason);
        }
        public bool Purchase(string id,out string reason)
        {
            var u=economy.upgrades.FirstOrDefault(u=>u.id==id);reason=null;
            if(u==null||Save.upgrades.Contains(id)){reason="owned";return false;}if(Score.Total<u.requiredPopularity){reason="rank_required";return false;}if(Save.coins<u.price){reason="coins_low";return false;}
            var staged=Data.Copy();staged.aquarium.coins-=u.price;staged.aquarium.upgrades.Add(id);
            if(id=="decor")foreach(var t in staged.aquarium.tanks)t.decorated=true;
            staged.aquarium.transactions.Add(new(){id="upgrade:"+id,kind="spend",amount=-u.price,utc=DateTime.UtcNow.ToString("O")});return Commit(staged,out reason);
        }
        internal bool Donate(AquariumVisitor visitor)
        {
            // Only a live visitor reaching reception after observing can originate this transaction.
            if(!visitors.Contains(visitor)||visitor.State!=VisitorState.Donating||visitor.Viewed==0||Vector3.Distance(visitor.transform.position,donation.position)>1)return false;
            string id="visit:"+visitor.VisitId;if(Save.transactions.Any(t=>t.id==id))return true;
            int amount=Mathf.Max(1,Mathf.RoundToInt((economy.baseDonation+Score.Total*economy.popularityDonation+visitor.Interest)*(1+(Building?Building.Comfort*.25f:0))*(Save.upgrades.Contains("comfort")?1+economy.comfortBonus:1)));
            var staged=Data.Copy();staged.aquarium.pending+=amount;staged.aquarium.visitors++;staged.aquarium.transactions.Add(new(){id=id,kind="donation",amount=amount,utc=DateTime.UtcNow.ToString("O")});return Commit(staged,out _);
        }
        public void Refresh()
        {Score=Calculate(Data);foreach(var tank in Tanks)tank.Refresh();if(comfortVisual)comfortVisual.SetActive(Save.upgrades.Contains("comfort"));if(signageVisual)signageVisual.SetActive(Save.upgrades.Contains("signage"));if(decorVisual)decorVisual.SetActive(Save.upgrades.Contains("decor"));}
        void Update()
        {
            if(!Ready)return;Hour=WorldTimeService.Current?.Simulation.Hour??Hour;visitors.RemoveAll(v=>!v);clock-=Time.deltaTime;
            bool inside=Mathf.Abs(Player.transform.position.x-transform.position.x)<6&&Player.transform.position.z-transform.position.z> -5.4f&&Player.transform.position.z-transform.position.z<5.4f+(Building?(Building.Layout.level-1)*8:0);
            foreach(var r in roof)if(r)r.enabled=!inside&&!(Building&&Building.UI&&Building.UI.IsOpen);
            if(clock<=0){clock=Mathf.Max(economy.minSpawnSeconds,economy.spawnSeconds/Mathf.Max(.5f,(WorldTimeService.Current?.Simulation.Current?.visitors??1)*(Hour<11?.75f:Hour<17?1.15f:.8f))/(1+Score.Total*.015f+(Save.upgrades.Contains("signage")?economy.signageBonus:0)));if(Hour>=economy.dayStart&&Hour<economy.dayEnd&&visitors.Count<economy.maxVisitors&&Tanks.Any(t=>Display(t).Any())){var go=new GameObject("Aquarium guest");go.transform.position=arrival.position;var visitor=go.AddComponent<AquariumVisitor>();visitors.Add(visitor);visitor.Initialize(this,Save.visitors+visitors.Count);}}
        }
        public IEnumerable<AquariumVisitor> Visitors=>visitors;
        void OnDestroy(){foreach(var v in visitors)if(v)Destroy(v.gameObject);}
    }
}


