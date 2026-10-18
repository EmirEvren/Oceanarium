using System;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed partial class TownTransactions
    {
        public bool DrinkTea(string owner,string command,int revision,string npc,out string error)
        {if(!Begin(owner,command,revision,npc,out var data,out _,out error))return false;if(Count(data,"island_tea")<1){error="inventory";return false;}Add(data,"island_tea",-1);data.town.buff="tea";data.town.buffSeconds=300;return Commit(data,command,out error);}
        public static int Progress(FishCollection data,TownQuestDefinition quest)
        {
            return quest.objective switch
            {
                TownObjective.Talk=>data.town.residents.Any(r=>r.id==quest.target&&r.talkedDay>=0)?1:0,
                TownObjective.Catch=>data.history.Count(f=>quest.target=="any"||f.speciesId==quest.target),
                TownObjective.Discover=>quest.target=="any"?data.records.Count:data.Discovered(quest.target)?1:0,
                TownObjective.Deliver or TownObjective.Find=>Count(data,quest.target),
                TownObjective.Purchase=>data.town.purchases.Where(p=>p.product==quest.target).Sum(p=>p.quantity),
                TownObjective.Visit=>data.town.flags.Contains("visit:"+quest.target)?1:0,
                TownObjective.Photo=>data.town.flags.Contains("photo:"+quest.target)?1:0,
                TownObjective.PlaceDecoration=>data.aquarium.layout.objects.Count(o=>!o.stored&&!o.legacy&&(quest.target=="any"||o.typeId==quest.target)),
                _=>0
            };
        }
        public bool Quest(string owner,string command,int revision,string npc,string questId,bool claim,out string error)
        {
            if(!Begin(owner,command,revision,npc,out var data,out var def,out error))return false;var quest=def.quests.FirstOrDefault(q=>q&&q.id==questId);if(!quest){error="unavailable";return false;}
            var state=data.town.quests.Find(q=>q.id==questId);
            if(!claim)
            {
                if(state!=null){error="owned";return false;}if(!string.IsNullOrEmpty(quest.prerequisite)&&!data.town.quests.Any(q=>q.id==quest.prerequisite&&q.completed)){error="locked";return false;}data.town.quests.Add(new(){id=questId});data.town.trackedQuest=questId;
            }
            else
            {
                if(state==null||state.completed||Progress(data,quest)<quest.amount){error="objective";return false;}
                if(quest.objective==TownObjective.Deliver)Add(data,quest.target,-quest.amount);state.completed=true;data.aquarium.coins+=quest.coins;var resident=data.town.Resident(npc);resident.friendship=Mathf.Clamp(resident.friendship+quest.friendship,0,1000);if(!string.IsNullOrEmpty(quest.rewardResource))Add(data,quest.rewardResource,quest.rewardAmount);if(data.town.trackedQuest==questId)data.town.trackedQuest=null;
            }
            return Commit(data,command,out error);
        }
    }
}
