using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static class NetworkChecks
    {
        public static void Run()
        {
                if(EditorApplication.isPlaying||WorldPersistence.Active!=null)throw new InvalidOperationException("Run isolated save checks outside an active world and Play Mode.");
                var log=new List<string>();bool passed=true;string root=Path.GetFullPath("Temp/ASTRA10-save-tests/"+Guid.NewGuid().ToString("N"));
            void Check(bool condition,string label){log.Add((condition?"PASS ":"FAIL ")+label);if(!condition)throw new Exception(label);}
            try
            {
                Check(Resources.Load<OceanNetworkSettings>("OceanNetworkSettings").steamAppId==4241310,"User-supplied Steam App ID 4241310");
                var a=WorldSlotStore.Create("Test island","local",false,root);var b=WorldSlotStore.Create("Test island","local",false,root);
                Check(a.worldId!=b.worldId,"Same world names produce independent IDs");
                var first=a.GetOrCreate("76561198000000001","<Guest>\n");first.appearance.hat="straw_hat";first.collection.resources.Add(new(){id="driftwood",count=7});first.collection.town.flags.Add("personal-test");first.collection.town.purchases.Add(new(){product="coastal_rod",quantity=1});first.starterRod=true;
                first.location=new(){valid=true,island="OF_TownIsland",position=new(3,1,5),heading=123};
                var species=AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:FishSpecies",new[]{"Assets/OceanariumFishing"})[0]));var fish=FishInstance.Roll(species,421,"home.pier");fish.ownerId=first.collection.ownerId;first.collection.Resolve(fish,true);
                var second=a.GetOrCreate("76561198000000002","Guest 2");second.appearance.hat="beanie";second.location=new(){valid=true,island="OF_HomeIsland",position=new(-2,1,4)};
                a.shared.clock.minute=129600;a.shared.clock.seed=976;a.shared.aquarium.coins=421;a.shared.aquarium.pending=31;a.shared.travel.repaired=true;a.shared.unlockedIslands.Add("OF_TownIsland");
                Check(WorldSlotStore.Save(a,out _,root),"Atomic complete-world save");var restored=WorldSlotStore.Load(a.worldId,out _,root);var returning=restored.GetOrCreate(first.identity,"Changed name");
                Check(restored.players.Count==3&&returning.collection.kept.Single().instanceId==fish.instanceId,"Returning player reuses profile and stable fish ID");
                Check(returning.appearance.hat=="straw_hat"&&returning.starterRod&&returning.collection.resources.Single().count==7&&returning.collection.town.purchases.Single().product=="coastal_rod","Appearance, inventory and equipment round trip");
                Check(returning.location.heading==123&&returning.location.island=="OF_TownIsland"&&restored.Find(second.identity).location.position.x==-2,"Independent player locations");
                Check(restored.Find(second.identity).collection.kept.Count==0&&!restored.Find(second.identity).collection.town.flags.Contains("personal-test"),"Independent fish and personal progression");
                var owner=restored.Find(restored.ownerIdentity);owner.location=new(){valid=true,island="OF_TownIsland"};
                Check(NetworkSession.ResumeDestination(restored)==Destination.TownIsland,"Returning host resumes the unlocked saved island");
                restored.shared.travel.repaired=false;Check(NetworkSession.ResumeDestination(restored)==Destination.HomeIsland,"Unavailable saved island falls back to Home Island");restored.shared.travel.repaired=true;
                Check(restored.shared.clock.minute==129600&&restored.shared.clock.seed==976&&restored.shared.aquarium.coins==421&&restored.shared.aquarium.pending==31&&restored.shared.travel.repaired,"Shared calendar, aquarium economy and boat survive reload");
                Check(WorldSlotStore.Load(b.worldId,out _,root).shared.aquarium.coins==0,"Separate world has separate progression");
                string path=WorldSlotStore.FileName(a.worldId,root);File.WriteAllText(path+".tmp","interrupted");Check(WorldSlotStore.Load(a.worldId,out _,root).shared.aquarium.coins==421,"Interrupted tmp does not replace valid committed world");
                Check(WorldSlotStore.Save(restored,out _,root),"Second save creates backup");File.WriteAllText(path,"corrupt");Check(WorldSlotStore.Load(a.worldId,out var recovery,root).worldId==a.worldId&&recovery=="recovered","Checksum failure recovers backup and retains corrupt evidence");
                var duplicate=WorldSlotStore.Load(a.worldId,out _,root);var clone=duplicate.Find(first.identity).collection.kept[0].Copy();clone.ownerId=duplicate.Find(second.identity).collection.ownerId;duplicate.Find(second.identity).collection.kept.Add(clone);Check(!WorldSlotStore.Save(duplicate,out _,root),"Duplicate fish ownership rejected before replacing committed save");
                Check(!WorldSlotStore.ValidIdentity("../elsewhere")&&!WorldSlotStore.ValidIdentity("client supplied arbitrary name"),"Persistent identity cannot be a path");
                bool rejected=false;try{WorldSlotStore.FileName("../../file",root);}catch(ArgumentException){rejected=true;}Check(rejected,"World path requires GUID");
                var oldCollection=File.Exists(FishCollectionStore.LegacyPath)?File.ReadAllBytes(FishCollectionStore.LegacyPath):null;var oldAppearance=File.Exists(AppearanceStore.LegacyPath)?File.ReadAllBytes(AppearanceStore.LegacyPath):null;
                if(oldCollection!=null)
                {
                    var legacy=FishCollectionStore.Load(FishCollectionStore.LegacyPath);if(!legacy.writeBlocked)
                    {var imported=WorldSlotStore.Create("Legacy import fixture","local",true,root);Check(imported.Find("local").collection.kept.Select(f=>f.instanceId).SequenceEqual(legacy.kept.Select(f=>f.instanceId))&&imported.shared.aquarium.coins==legacy.aquarium.coins,"Legacy inventory IDs and aquarium economy preserved by import");}
                    Check(oldCollection.SequenceEqual(File.ReadAllBytes(FishCollectionStore.LegacyPath)),"Original collection file byte-for-byte unchanged");
                }
                if(oldAppearance!=null)Check(oldAppearance.SequenceEqual(File.ReadAllBytes(AppearanceStore.LegacyPath)),"Original appearance file byte-for-byte unchanged");
                WorldPersistence.Receive(returning,restored.shared,restored.worldId,restored.name);Check(!FishCollectionStore.Save(WorldPersistence.LoadCollection())&&!AppearanceStore.Save(new AppearanceData(),out _),"Guest cannot persist final inventory or appearance through local stores");WorldPersistence.Close();
                foreach(var code in LanguageService.Codes){var table=AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.StringTable>("Assets/OceanariumFishing/Localization/Tables/OF_UI_"+code+".asset");Check(table&&table.GetEntry("net.host")!=null&&table.GetEntry("net.status.authentication")!=null,"Network UI keys available: "+code);}
                Check(!NetworkSpawn.Valid(new(float.NaN,0,0),out _)&&!NetworkSpawn.Valid(new(0,-50,0),out _),"Non-finite/below-world saved location rejected");
            }
            catch(Exception e){passed=false;log.Add(e.ToString());Debug.LogException(e);}
            finally{WorldPersistence.Close();if(WorldTimeService.Current&&!EditorApplication.isPlaying)UnityEngine.Object.DestroyImmediate(WorldTimeService.Current.gameObject);}
            File.WriteAllText("Docs/ASTRA_10_SAVE_TESTS.txt",(passed?"PASS":"FAIL")+" "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",log));
        }
    }
}
