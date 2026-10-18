using System;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    // A synchronous authority boundary. Adapters must authenticate owner and proximity before calling.
    // Every successful mutation copies and commits the existing collection exactly once.
    public sealed partial class TownTransactions
    {
        readonly Func<FishCollection> read;readonly Func<FishCollection,bool> save;readonly ITownClock clock;
        readonly TownResidentDefinition[] residents;
        public TownTransactions(Func<FishCollection> read,Func<FishCollection,bool> save,ITownClock clock,TownResidentDefinition[] residents)
        {this.read=read;this.save=save;this.clock=clock;this.residents=residents;}
        public static int Count(FishCollection data,string id)=>data.resources.Where(r=>r.id==id).Sum(r=>r.count);
        static void Add(FishCollection data,string id,int delta){var s=data.resources.Find(r=>r.id==id);if(s==null){s=new(){id=id};data.resources.Add(s);}s.count+=delta;data.resources.RemoveAll(r=>r.count==0);}
        bool Begin(string owner,string command,int revision,string npc,out FishCollection next,out TownResidentDefinition definition,out string error)
        {
            var data=read();next=null;definition=residents.FirstOrDefault(n=>n&&n.id==npc);error=null;
            if(data.writeBlocked)error="save";else if(owner!=data.ownerId)error="owner";
            else if(string.IsNullOrWhiteSpace(command)||data.town.commands.Contains(command))error="replay";
            else if(revision!=data.town.revision)error="stale";else if(!definition||!definition.Available(clock))error="unavailable";
            if(error!=null)return false;next=data.Copy();return true;
        }
        bool Commit(FishCollection next,string command,out string error)
        {next.town.commands.Add(command);next.town.revision++;if(clock is TownClock c)next.town.elapsedMinutes=c.Minutes;if(!save(next)){error="save";return false;}error=null;return true;}
        public int Price(TownProductDefinition product,TownResidentState resident)=>Mathf.Max(1,Mathf.CeilToInt(product.price*(1-Mathf.Min(.15f,resident.Hearts*.015f))));
        public bool Buy(string owner,string command,int revision,string npc,string productId,out string error)
        {
            if(!Begin(owner,command,revision,npc,out var data,out var def,out error))return false;
            if(!def.ShopOpen(clock)){error="closed";return false;}var product=def.products.FirstOrDefault(p=>p&&p.id==productId);if(!product||product.futureLocked||product.quantity<1||product.price<0||string.IsNullOrWhiteSpace(product.resourceId)){error="locked";return false;}
            var resident=data.town.Resident(npc);if(resident.Hearts<product.requiredHearts){error="locked";return false;}
            var purchased=data.town.purchases.Find(p=>p.product==product.id);if(product.stockLimit>0&&(purchased?.quantity??0)>=product.stockLimit){error="stock";return false;}
            int price=Price(product,resident);if(product.currency=="coins"){if(data.aquarium.coins<price){error="funds";return false;}data.aquarium.coins-=price;}
            else{if(Count(data,product.currency)<price){error="funds";return false;}Add(data,product.currency,-price);}
            if(product.buildItem)
            {
                var item=product.buildItem;
                if(item.futureLocked||item.category==BuildCategory.Expansions){error="locked";return false;}
                bool physical=AquariumBuilding.Physical(item)||AquariumBuilding.Aquatic(item);
                if(physical){var id=Guid.NewGuid().ToString("N");data.aquarium.layout.objects.Add(new(){id=id,typeId=item.id,stored=true});if(item.tank)data.aquarium.tanks.Add(new(){id=id,definitionId=item.tank.id});}
                else{if(data.aquarium.layout.purchases.Contains(item.id)){error="owned";return false;}data.aquarium.layout.purchases.Add(item.id);}
                data.aquarium.layout.revision++;
            }
            else Add(data,product.resourceId,product.quantity);if(purchased==null){purchased=new(){product=product.id};data.town.purchases.Add(purchased);}purchased.quantity++;
            if(!string.IsNullOrEmpty(product.cosmeticSlot)){data.town.cosmeticSlot=product.cosmeticSlot;data.town.cosmeticItem=product.cosmeticItem;}
            return Commit(data,command,out error);
        }
        public bool Talk(string owner,string command,int revision,string npc,out string error)
        {
            if(!Begin(owner,command,revision,npc,out var data,out var def,out error))return false;var resident=data.town.Resident(npc);
            if(resident.talkedDay==clock.Day){error="daily";return false;}resident.talkedDay=clock.Day;resident.friendship=Mathf.Clamp(resident.friendship+def.conversationPoints,0,1000);return Commit(data,command,out error);
        }
        public bool Gift(string owner,string command,int revision,string npc,string resource,out string error)
        {
            if(!Begin(owner,command,revision,npc,out var data,out var def,out error))return false;var resident=data.town.Resident(npc);
            if(resident.giftDay!=clock.Day){resident.giftDay=clock.Day;resident.giftsToday=0;}if(resident.giftsToday>=def.giftLimit){error="daily";return false;}
            if(Count(data,resource)<1){error="inventory";return false;}Add(data,resource,-1);resident.giftsToday++;resident.friendship=Mathf.Clamp(resident.friendship+def.GiftPoints(resource),0,1000);
            if(!resident.knownPreferences.Contains(resource))resident.knownPreferences.Add(resource);return Commit(data,command,out error);
        }
        public bool ClaimFriendship(string owner,string command,int revision,string npc,out string error)
        {
            if(!Begin(owner,command,revision,npc,out var data,out var def,out error))return false;var resident=data.town.Resident(npc);
            if(resident.Hearts<def.rewardHearts||string.IsNullOrEmpty(def.rewardResource)||def.rewardQuantity<1){error="locked";return false;}
            if(resident.rewards.Contains(def.rewardResource)){error="replay";return false;}resident.rewards.Add(def.rewardResource);
            if(def.rewardBuildItem){data.aquarium.layout.objects.Add(new(){id=Guid.NewGuid().ToString("N"),typeId=def.rewardBuildItem.id,stored=true});data.aquarium.layout.revision++;}
            else Add(data,def.rewardResource,def.rewardQuantity);return Commit(data,command,out error);
        }
        public bool SellFish(string owner,string command,int revision,string npc,string instanceId,out string error)
        {
            if(!Begin(owner,command,revision,npc,out var data,out var def,out error))return false;if(!def.ShopOpen(clock)){error="closed";return false;}if(def.id!="oliver"){error="unavailable";return false;}
            var fish=data.kept.Find(f=>f.instanceId==instanceId);if(fish==null||fish.ownership!=FishOwnership.Stored||fish.ownerId!=owner||!float.IsFinite(fish.value)||fish.value<0||fish.value>1000000){error="inventory";return false;}
            int price=Mathf.Max(1,Mathf.RoundToInt(fish.value));if(data.aquarium.coins>int.MaxValue-price){error="funds";return false;}
            if(!data.TryMove(instanceId,owner,FishOwnership.Sold)){error="inventory";return false;}data.aquarium.coins+=price;return Commit(data,command,out error);
        }
    }
}
