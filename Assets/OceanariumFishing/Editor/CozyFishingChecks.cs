using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static class CozyFishingChecks
    {
        static readonly List<string> results=new();
        static void Check(bool value,string name){if(!value)throw new Exception(name);results.Add("PASS: "+name);}
        [MenuItem("Oceanarium Fishing/Run ASTRA 0.4 Data and Balance Checks")]
        public static void Run()
        {
            results.Clear();string status="PASS";
            try{Balance();Data();}catch(Exception e){status="FAIL";results.Add(e.ToString());Debug.LogException(e);}
            Directory.CreateDirectory("Docs");File.WriteAllText("Docs/ASTRA_04_DATA_BALANCE_RESULTS.txt",status+" "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",results));Debug.Log("ASTRA 0.4 data/balance checks: "+status);
        }
        static FishSpecies[] Species()=>AssetDatabase.FindAssets("t:FishSpecies",new[]{FoundationBuilder.Root+"/Fishing"}).Select(g=>AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(s=>s.id).ToArray();
        static void Balance()
        {
            var species=Species();var rod=AssetDatabase.LoadAssetAtPath<FishingRodDefinition>(FoundationBuilder.Root+"/Fishing/StarterRod.asset");var tuning=AssetDatabase.LoadAssetAtPath<FishingTuning>(FoundationBuilder.Root+"/Fishing/CozyFishingTuning.asset");
            Check(species.Length==8&&species.Select(s=>s.id).Distinct().Count()==8,"Exactly eight existing species, stable unique IDs");
            Check(tuning.difficulties!=null&&tuning.difficulties.profiles.Length==3,"Three configured difficulty profiles");
            Check(tuning.Difficulty().id=="cozy"&&new FishCollection().difficulty=="cozy","Cozy is the default for new players");
            Check(tuning.Difficulty("cozy").hookWindow==3&&tuning.Difficulty("normal").hookWindow==2&&tuning.Difficulty("experienced").hookWindow==1.25f,"3.0 / 2.0 / 1.25 second hook windows");
            foreach(var profile in tuning.difficulties.profiles)
            {
                foreach(var s in species)
                {
                    int wins=0;var durations=new List<float>();float total=0;
                    for(int seed=1;seed<=100;seed++)
                    {
                        var specimen=FishInstance.Roll(s,seed*7919,"home.pier");var fight=new FishingFight(s,rod,tuning,specimen,false,profile);bool recover=false;
                        for(int step=0;step<2400&&!fight.Won&&!fight.Lost;step++)
                        {if(fight.Tension>.78f)recover=true;if(fight.Tension<.42f)recover=false;fight.Step(.05f,!recover,profile.id=="cozy"?0:-fight.Direction);}
                        total+=fight.Elapsed;if(fight.Won){wins++;durations.Add(fight.Elapsed);}
                    }
                    results.Add($"MEASURED {profile.id} {s.id} {s.rarity}: {wins}/100 caught; successful duration mean {(durations.Count>0?durations.Average():0):0.00}s, min {(durations.Count>0?durations.Min():0):0.00}s, max {(durations.Count>0?durations.Max():0):0.00}s. Regular hook; starter rod; Cozy uses zero directional input.");
                    if(profile.id=="cozy")Check(wins>=(s.rarity==FishRarity.Common?95:s.rarity==FishRarity.Uncommon?90:80),"Cozy controlled success target: "+s.id);
                    else Check(wins>90,"Competent player can catch with "+profile.id+": "+s.id);
                    Check(s.icon&&s.silhouette&&s.icon.width==384,"Generated icon and silhouette: "+s.id);
                    var colors=s.silhouette.GetPixels();Check(colors.Where(c=>c.a>0).All(c=>c.r<.1f&&c.g<.1f&&c.b<.1f),"Locked illustration reveals no color: "+s.id);
                }
            }
            var normalFish=species.First(s=>s.rarity==FishRarity.Common);var sample=FishInstance.Roll(normalFish,55,"home.pier");
            var cozy=new FishingFight(normalFish,rod,tuning,sample,false,tuning.Difficulty("cozy"));var normal=new FishingFight(normalFish,rod,tuning,sample,false,tuning.Difficulty("normal"));
            for(int i=0;i<20;i++){cozy.Step(.05f,true,0);normal.Step(.05f,true,0);}Check(cozy.Tension<normal.Tension&&cozy.Progress>normal.Progress,"Difficulty changes real tension and catch progress");
            float before=cozy.Tension;cozy.Step(.1f,false,0);Check(cozy.Tension<before,"Release lowers tension");
            var force=UnityEngine.Object.Instantiate(normalFish);force.fightingStrength=80;var overload=new FishingFight(force,rod,tuning,sample,false,tuning.Difficulty("cozy"));
            while(overload.Tension<.98f)overload.Step(.05f,true,0);for(int i=0;i<20;i++)overload.Step(.05f,true,0);Check(!overload.Lost,"A one-second tension spike cannot snap Cozy line");
            for(int i=0;i<20;i++)overload.Step(.05f,false,0);Check(!overload.Lost&&overload.Tension<.8f,"Release during grace rescues the line");UnityEngine.Object.DestroyImmediate(force);
        }
        static void Data()
        {
            var s=Species()[0];string dir="Temp/ASTRA04Checks";Directory.CreateDirectory(dir);string path=dir+"/collection.json";
            File.WriteAllText(path,"{\"version\":1,\"kept\":[{\"instanceId\":\"old-one\",\"speciesId\":\"clownfish\",\"weight\":0.2,\"length\":14,\"quality\":0.8,\"location\":\"home.beach\",\"caughtUtc\":\"2026-10-10T12:00:00Z\",\"aquariumEligible\":true}],\"records\":[{\"speciesId\":\"clownfish\",\"weight\":0.25,\"length\":15},{\"speciesId\":\"blue_tang\",\"weight\":0.4,\"length\":27}],\"resolved\":[\"old-one\",\"released-old\"]}");
            var data=FishCollectionStore.Load(path);Check(data.version==FishCollection.CurrentVersion&&data.kept.Single().instanceId=="old-one"&&data.kept[0].weight==.2f,"Version 1 migrates without replacing specimen IDs or measurements");
            Check(data.Discovered("blue_tang")&&data.Record("clownfish").weight==.25f,"Migration preserves released-species records and personal bests");
            Check(data.TotalCaught==2&&data.legacyUnattributedCatches==1&&data.Record("blue_tang").incompleteLegacyHistory,"Incomplete legacy catch history is explicit, not invented");
            var fish=FishInstance.Roll(s,567,"home.pier");fish.ownerId=data.ownerId;fish.islandId="OF_HomeIsland";fish.captureMethod="rod:starter";
            Check(data.RecordCatch(fish)&&!data.RecordCatch(fish),"Catch event counted exactly once before retention decision");int count=data.TotalCaught;int owned=data.kept.Count;
            Check(data.Resolve(fish,false)&&!data.Resolve(fish,false)&&data.kept.Count==owned&&data.TotalCaught==count,"Release neither owns nor duplicates fish");
            Check(data.Discovered(s.id)&&data.Record(s.id).count>0,"Released catch discovers its species and updates records");
            var kept=FishInstance.Roll(s,125,"home.pier");kept.ownerId=data.ownerId;kept.islandId="OF_HomeIsland";Check(data.Resolve(kept,true),"Keep creates one individually owned fish");
            Check(FishCollectionStore.Save(data,path),"Migrated collection saved atomically");var loaded=FishCollectionStore.Load(path);
            Check(loaded.kept.Any(f=>f.instanceId==kept.instanceId&&f.weight==kept.weight&&f.length==kept.length)&&loaded.TotalCaught==data.TotalCaught,"Inventory, dimensions, discovery and totals survive disk round trip");
            loaded.kept.Add(loaded.kept[0].Copy());loaded.Normalize();Check(loaded.kept.Select(f=>f.instanceId).Distinct().Count()==loaded.kept.Count,"Duplicate saved specimen IDs are removed");
            Check(loaded.Query("weight").First().weight>=loaded.Query("weight").Last().weight,"Weight sorting uses individual data");
            Check(loaded.Query("rarity").Select(f=>f.rarity).SequenceEqual(loaded.kept.OrderByDescending(f=>f.rarity).Select(f=>f.rarity)),"Rarity sorting");
            Check(loaded.Query("species").Select(f=>f.speciesId).SequenceEqual(loaded.kept.OrderBy(f=>f.speciesId).Select(f=>f.speciesId)),"Species sorting");
            Check(loaded.Query("date","OF_HomeIsland",(int)kept.rarity).All(f=>f.islandId=="OF_HomeIsland"&&f.rarity==kept.rarity),"Island and rarity filters use actual specimen metadata");
            Check(!loaded.TryMove(kept.instanceId,"other-player",FishOwnership.Displayed,"tank-a"),"Ownership validation rejects another player");
            Check(loaded.TryMove(kept.instanceId,loaded.ownerId,FishOwnership.Displayed,"tank-a"),"Stored eligible fish may be assigned to a tank");
            Check(!loaded.TryMove(kept.instanceId,loaded.ownerId,FishOwnership.Sold),"Displayed fish cannot simultaneously be sold");
            Check(loaded.TryMove(kept.instanceId,loaded.ownerId,FishOwnership.Stored)&&loaded.TryMove(kept.instanceId,loaded.ownerId,FishOwnership.Sold),"Removal clears placement before sale state");
            Check(!loaded.kept.Any(f=>f.instanceId==kept.instanceId)&&loaded.Discovered(s.id),"Sale-state removal keeps discovery records");
            Check(FishValuation.Estimate(s,fish)==FishValuation.Estimate(s,fish.Copy()),"Specimen valuation is deterministic");
            loaded.difficulty="experienced";loaded.tutorialComplete=true;FishCollectionStore.Save(loaded,path);var preferences=FishCollectionStore.Load(path);Check(preferences.tutorialComplete&&preferences.difficulty=="experienced","Difficulty and tutorial completion persist");
            File.WriteAllText(path,"{\"version\":99,\"important_future_progress\":42}");var future=FishCollectionStore.Load(path);Check(future.writeBlocked&&!FishCollectionStore.Save(future,path)&&File.ReadAllText(path).Contains("important_future_progress"),"Future-schema save cannot be silently overwritten");
            File.WriteAllText(path,"corrupt content preserved");var corrupt=FishCollectionStore.Load(path);Check(corrupt.writeBlocked&&!FishCollectionStore.Save(corrupt,path)&&File.ReadAllText(path)=="corrupt content preserved","Corrupt source is retained and writes blocked");
            File.WriteAllText(path,"{\"version\":2,\"ownerId\":\"existing-owner\",\"kept\":null,\"records\":null,\"resolved\":null,\"history\":null}");var empty=FishCollectionStore.Load(path);Check(empty.ownerId=="existing-owner"&&empty.kept.Count==0&&empty.difficulty=="cozy","Empty optional fields repair safely without replacing identity");
        }
    }
}
