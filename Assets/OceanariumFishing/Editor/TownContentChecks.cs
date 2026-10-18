using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace OceanariumFishing.Editor
{
    public static class TownContentChecks
    {
        public static void Run()
        {
            var log=new List<string>();bool pass=true;void Check(bool ok,string message){log.Add((ok?"PASS: ":"FAIL: ")+message);if(!ok)throw new Exception(message);}
            try
            {
                var catalog=Resources.Load<TownCatalog>("TownContent");Check(catalog&&catalog.residents.Length==12,"Twelve resident definitions load from persisted catalog");
                foreach(var npc in catalog.residents)
                {
                    Check(npc.quests.Length==2&&npc.portrait,"Two quests and portrait: "+npc.id);var data=new FishCollection();data.Normalize();data.aquarium.coins=10000;var clock=new TownClock(npc.nightMerchant?1200:600);var service=new TownTransactions(()=>data,next=>{data=next;return true;},clock,catalog.residents);
                    foreach(var quest in npc.quests)
                    {
                        Check(service.Quest(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,npc.id,quest.id,false,out _),"Accept "+quest.id);
                        // Isolated fixtures exercise every configured objective without altering the user's save.
                        switch(quest.objective)
                        {
                            case TownObjective.Talk:data.town.Resident(quest.target).talkedDay=clock.Day;break;
                            case TownObjective.Purchase:data.town.purchases.Add(new(){product=quest.target,quantity=quest.amount});break;
                            case TownObjective.Deliver:case TownObjective.Find:data.resources.Add(new(){id=quest.target,count=quest.amount});break;
                            case TownObjective.Visit:data.town.flags.Add("visit:"+quest.target);break;
                            case TownObjective.Photo:data.town.flags.Add("photo:"+quest.target);Check(catalog.residents.Any(r=>r.products.Any(p=>p.id=="camera")),"Photo objective has purchasable camera");break;
                            case TownObjective.Discover:for(int i=0;i<quest.amount;i++)data.records.Add(new(){speciesId="fixture"+i});break;
                            case TownObjective.PlaceDecoration:data.aquarium.layout.objects.Add(new(){id="fixture",typeId="plant_0",stored=false});break;
                            default:throw new Exception("Uncovered objective "+quest.objective);
                        }
                        Check(service.Quest(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,npc.id,quest.id,true,out _),"Complete configured objective "+quest.id);Check(!service.Quest(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,npc.id,quest.id,true,out _),"No duplicate quest reward "+quest.id);
                    }
                    foreach(var p in npc.products)Check(p&&p.price>=0&&(!p.buildItem||p.buildItem.prefab),"Valid product asset: "+p.id);
                }
                var gull=catalog.residents.First(n=>n.id=="gull");Check(gull.Available(new TownClock(23*60))&&gull.Available(new TownClock(25*60))&&!gull.Available(new TownClock(44*60)),"Merchant availability spans midnight on the same trading night");
                var round=new TownArcadeRound();for(int i=0;i<5;i++)round.Reel(.5f/.7f);var arcadeData=new FishCollection();arcadeData.Normalize();Check(round.Won&&round.Claim(arcadeData,1)&&arcadeData.aquarium.coins==3,"Arcade timing win grants three Coins");Check(!round.Reel(1)&&!round.Claim(arcadeData,1),"Finished round and daily reward cannot repeat");var miss=new TownArcadeRound();for(int i=0;i<5;i++)miss.Reel(0);Check(!miss.Won&&!miss.Claim(arcadeData,2),"Arcade loss cannot claim reward");
            }
            catch(Exception e){pass=false;log.Add(e.ToString());Debug.LogException(e);}
            File.WriteAllText("Docs/ASTRA_08_CONTENT_TESTS.txt",(pass?"PASS ":"FAIL ")+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",log));
        }
    }
}
