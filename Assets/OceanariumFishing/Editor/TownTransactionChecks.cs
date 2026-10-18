using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
namespace OceanariumFishing.Editor
{
    public static class TownTransactionChecks
    {
        public static void Run()
        {
            var lines=new List<string>();var objects=new List<UnityEngine.Object>();bool pass=true;
            void Check(bool condition,string message){lines.Add((condition?"PASS: ":"FAIL: ")+message);if(!condition)throw new Exception(message);}
            try
            {
                // Isolated fixture: never sells player fish or adds test money to their save.
                var data=new FishCollection();data.Normalize();data.aquarium.coins=100;
                var npc=ScriptableObject.CreateInstance<TownResidentDefinition>();objects.Add(npc);npc.id="oliver";npc.loved=new[]{"tea"};npc.rewardHearts=1;npc.rewardResource="recipe";
                var product=ScriptableObject.CreateInstance<TownProductDefinition>();objects.Add(product);product.id="tea";product.resourceId="tea";product.price=4;product.stockLimit=2;npc.products=new[]{product};
                var clock=new TownClock(540);bool failSave=false;var service=new TownTransactions(()=>data,next=>{if(failSave)return false;data=next;return true;},clock,new[]{npc});
                string owner=data.ownerId,error;int revision=0;
                Check(!service.Buy("stranger","a",0,"oliver","tea",out error)&&data.aquarium.coins==100,"Reject foreign owner without spending");
                Check(service.Buy(owner,"a",revision,"oliver","tea",out error)&&data.aquarium.coins==96&&TownTransactions.Count(data,"tea")==1,"Purchase uses existing wallet and resources");
                Check(!service.Buy(owner,"a",data.town.revision,"oliver","tea",out error),"Reject repeated command");
                Check(!service.Buy(owner,"b",0,"oliver","tea",out error),"Reject stale revision");
                failSave=true;string before=JsonUtility.ToJson(data);Check(!service.Buy(owner,"b",data.town.revision,"oliver","tea",out error)&&JsonUtility.ToJson(data)==before,"Failed save leaves inventory and wallet untouched");failSave=false;
                Check(service.Talk(owner,"talk",data.town.revision,"oliver",out error),"Daily conversation grants friendship");
                Check(!service.Talk(owner,"talk2",data.town.revision,"oliver",out error),"Repeated daily conversation cannot farm friendship");
                Check(service.Gift(owner,"gift",data.town.revision,"oliver","tea",out error)&&TownTransactions.Count(data,"tea")==0&&data.town.Resident("oliver").friendship==50,"Loved gift consumes one owned item and grants points");
                Check(!service.Gift(owner,"gift2",data.town.revision,"oliver","tea",out error),"Daily gift limit enforced");
                Check(service.Buy(owner,"second",data.town.revision,"oliver","tea",out error),"Second stocked item purchasable");Check(!service.Buy(owner,"third",data.town.revision,"oliver","tea",out error),"Stock limit enforced");
                clock.Advance(1440,1);Check(service.Talk(owner,"nextday",data.town.revision,"oliver",out error),"Temporary clock opens next daily conversation");Check(service.Gift(owner,"nextgift",data.town.revision,"oliver","tea",out error),"Next day gift limit resets");
                Check(service.ClaimFriendship(owner,"reward",data.town.revision,"oliver",out error)&&TownTransactions.Count(data,"recipe")==1,"Friendship reward delivered");Check(!service.ClaimFriendship(owner,"reward2",data.town.revision,"oliver",out error),"One-time reward cannot repeat");
                data.kept.Add(new(){instanceId="fixture-fish",ownerId=owner,speciesId="test",weight=1,length=10,quality=.5f,value=7,ownership=FishOwnership.Displayed,tankId="reef"});
                Check(!service.SellFish(owner,"sale",data.town.revision,"oliver","fixture-fish",out error),"Displayed fish cannot be sold");data.kept[0].ownership=FishOwnership.Stored;data.kept[0].tankId="";int coins=data.aquarium.coins;
                Check(service.SellFish(owner,"sale",data.town.revision,"oliver","fixture-fish",out error)&&data.kept.Count==0&&data.aquarium.coins==coins+7,"Sale removes exact owned instance and pays exact rounded value");Check(!service.SellFish(owner,"sale2",data.town.revision,"oliver","fixture-fish",out error),"Fish cannot be sold twice");
                var restored=JsonUtility.FromJson<FishCollection>(JsonUtility.ToJson(data));restored.Normalize();Check(restored.town.revision==data.town.revision&&restored.town.Resident("oliver").friendship==100&&restored.travel.version==1&&restored.aquarium.version==1,"Town state roundtrips alongside existing travel and aquarium schemas");
                npc.nightMerchant=true;Check(!npc.Available(clock),"Night merchant unavailable during daytime");
                npc.nightMerchant=false;var quest=ScriptableObject.CreateInstance<TownQuestDefinition>();objects.Add(quest);quest.id="delivery";quest.objective=TownObjective.Deliver;quest.target="tea";quest.amount=1;quest.coins=3;npc.quests=new[]{quest};
                Check(service.Quest(owner,"accept",data.town.revision,"oliver","delivery",false,out error),"Quest accepts exactly once");Check(!service.Quest(owner,"early",data.town.revision,"oliver","delivery",true,out error),"Missing delivery cannot claim reward");data.resources.Add(new(){id="tea",count=1});coins=data.aquarium.coins;Check(service.Quest(owner,"claim",data.town.revision,"oliver","delivery",true,out error)&&TownTransactions.Count(data,"tea")==0&&data.aquarium.coins==coins+3,"Delivery consumes item and grants reward atomically");Check(!service.Quest(owner,"claim2",data.town.revision,"oliver","delivery",true,out error),"Quest cannot pay twice");
            }
            catch(Exception e){pass=false;lines.Add(e.ToString());Debug.LogException(e);}
            finally{foreach(var obj in objects)UnityEngine.Object.DestroyImmediate(obj);}
            File.WriteAllText("Docs/ASTRA_08_TRANSACTION_TESTS.txt",(pass?"PASS ":"FAIL ")+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",lines));
        }
    }
}
