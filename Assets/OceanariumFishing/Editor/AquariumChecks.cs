using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace OceanariumFishing.Editor
{
    public static class AquariumChecks
    {
        static bool running;static readonly List<string> report=new();static Keyboard keyboard;
        static void Check(bool condition,string label){report.Add((condition?"PASS: ":"FAIL: ")+label);if(!condition)throw new Exception(label);}
        static void Capture(string name){Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_05_"+name+".png");}
        public static void Run(){if(running||!EditorApplication.isPlaying)return;running=true;report.Clear();GameSession.Current.StartCoroutine(RunGuarded());}
        public static void Polish(){if(running||!EditorApplication.isPlaying)return;running=true;report.Clear();GameSession.Current.StartCoroutine(RunGuarded(true));}
        static IEnumerator RunGuarded(bool polish=false)
        {
            var iterator=polish?PolishChecks():Checks();bool pass=true;
            while(true){object value=null;bool next=false;try{next=iterator.MoveNext();if(next)value=iterator.Current;}catch(Exception e){report.Add(e.ToString());Debug.LogException(e);pass=false;break;}if(!next)break;yield return value;}
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}running=false;File.WriteAllText(polish?"Docs/ASTRA_05_VISUAL_RESULTS.txt":"Docs/ASTRA_05_TEST_RESULTS.txt",(pass?"PASS ":"FAIL ")+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",report));
        }
        static IEnumerator Walk(LocalPlayerController player,Vector3 target,float limit=16)
        {
            float until=Time.time+limit;
            while(Vector3.Distance(new Vector3(player.transform.position.x,0,player.transform.position.z),new Vector3(target.x,0,target.z))>.25f&&Time.time<until)
            {var delta=target-player.transform.position;player.SetViewAngles(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,18);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return null;}
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(new Vector2(player.transform.position.x-target.x,player.transform.position.z-target.z).magnitude<.5f,"Physical player walk to "+target);
        }
        static IEnumerator Checks()
        {
            var w=UnityEngine.Object.FindFirstObjectByType<AquariumWorld>();Check(w&&w.Ready,"Museum initialized in existing Home Island");
            var player=w.Player.GetComponent<LocalPlayerController>();var collection=w.Player.GetComponent<CollectionHUD>();w.Panel.Close();collection.Close();
            Check(w.Tanks.Count(t=>t.definition.size==AquariumTankSize.Small)==3&&w.Tanks.Count(t=>t.definition.size==AquariumTankSize.Medium)==1,"Three small exhibits and one medium exhibit");
            Check(w.species.Length==8&&w.species.All(s=>s.aquariumHabitat!=AquariumHabitat.Freshwater),"Eight canonical marine species assigned plausible aquarium habitats");
            DateTime before=DateTime.UtcNow;CozyFishingDemo.Run();float deadline=Time.time+180;
            while(Time.time<deadline&&(!File.Exists("Docs/ASTRA_04_LIVE_INPUT_RESULTS.txt")||File.GetLastWriteTimeUtc("Docs/ASTRA_04_LIVE_INPUT_RESULTS.txt")<before))yield return null;
            string demo=File.ReadAllText("Docs/ASTRA_04_LIVE_INPUT_RESULTS.txt");Check(File.GetLastWriteTimeUtc("Docs/ASTRA_04_LIVE_INPUT_RESULTS.txt")>=before&&demo.StartsWith("PASS"),"Actual-input Cozy common/rare catches, Keep, inventory and journal");File.WriteAllText("Docs/ASTRA_05_FISHING_RESULTS.txt",demo);
            collection.Close();yield return null;keyboard=InputSystem.AddDevice<Keyboard>();
            var specimen=w.Data.kept.Where(f=>f.ownership==FishOwnership.Stored&&w.Tanks.Any(t=>w.Validate(f,t)==null)).OrderByDescending(f=>f.caughtUtc).First();var tank=w.Tanks.First(t=>w.Validate(specimen,t)==null);
            string id=specimen.instanceId;float length=specimen.length,weight=specimen.weight;string owner=specimen.ownerId;
            player.Teleport(w.arrival.position+Vector3.up*.15f);player.SetViewAngles(0,15);yield return new WaitForSeconds(.7f);Capture("Exterior");
            var walk=Walk(player,w.entrance.position+Vector3.forward*1.5f);while(walk.MoveNext())yield return walk.Current;Check(player.transform.position.y>w.transform.position.y-.2f,"Walkable ramp and interior floor");
            walk=Walk(player,new Vector3(w.transform.position.x,w.transform.position.y,tank.viewPoint.position.z));while(walk.MoveNext())yield return walk.Current;
            walk=Walk(player,tank.viewPoint.position);while(walk.MoveNext())yield return walk.Current;
            yield return new WaitForSeconds(.25f);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.25f);
            Check(w.Panel.IsOpen&&w.Panel.SelectedTank==tank,"E opens correct tank management");
            Check(!w.Place("another-owner",id,tank.id,out _),"Wrong-owner placement rejected");
            Check(w.Place(owner,id,tank.id,out var error),"Place exact retained specimen: "+error);Check(!w.Place(owner,id,tank.id,out _),"Duplicate placement rejected");
            Check(!w.Data.Copy().TryMove(id,owner,FishOwnership.Sold),"Displayed fish cannot be sold");
            Check(w.Data.kept.Single(f=>f.instanceId==id).ownership==FishOwnership.Displayed,"Ownership retained with displayed status");
            Check(w.Score.Total>0,"Popularity grows from actual exhibits");Capture("TankManagement");w.Panel.Close();
            var live=tank.Fish.Single(f=>f.Specimen.instanceId==id);Vector3 initial=live.transform.localPosition;yield return new WaitForSeconds(2);
            Check(Vector3.Distance(initial,live.transform.localPosition)>.03f,"Exact specimen swims continuously");Check(live.Specimen.length==length&&live.Specimen.weight==weight,"Saved individual dimensions preserved in display");
            player.SetViewAngles(0,20);Capture("LiveSpecimen");
            Check(w.Remove(owner,id,out _),"Remove returns same specimen to storage");Check(w.Data.kept.Single(f=>f.instanceId==id).ownership==FishOwnership.Stored,"Removed fish available in storage");Check(w.Place(owner,id,tank.id,out _),"Same specimen can be placed again");
            var incompatible=FishInstance.Roll(w.species.First(s=>s.aquariumHabitat!=tank.definition.habitats[0]),22,"test");incompatible.ownerId=owner;incompatible.ownership=FishOwnership.Stored;Check(w.Validate(incompatible,tank)=="habitat","Incompatible habitat rejected");
            // Validate capacity against actual owned specimens; no fabricated live fish are granted.
            var extra=w.Data.kept.Where(f=>f.ownership==FishOwnership.Stored&&w.Validate(f,tank)==null).Take(tank.definition.capacity-1).ToArray();foreach(var f in extra)Check(w.Place(owner,f.instanceId,tank.id,out _),"Fill tank using an existing owned specimen");
            if(w.Display(tank).Count()==tank.definition.capacity){var probe=specimen.Copy();probe.instanceId="capacity-probe";probe.ownership=FishOwnership.Stored;Check(w.Validate(probe,tank)=="full","Full tank rejects extra fish");}else report.Add("NOTE: insufficient existing compatible specimens for live full-capacity test");
            foreach(var f in extra)w.Remove(owner,f.instanceId,out _);
            int startVisits=w.Save.visitors;int price=w.economy.upgrades.First(u=>u.id=="comfort").price;deadline=Time.time+210;
            player.Teleport(w.transform.position+new Vector3(-1,0,-3));player.SetViewAngles(25,22);
            while(Time.time<deadline&&(w.Save.visitors<=startVisits||w.Save.pending<price)){yield return null;}
            Check(w.Save.visitors>startVisits&&w.Save.pending>=price,"Real visitors enter, observe exhibits, reach reception and fund an upgrade");Capture("Visitors");
            Check(w.Save.transactions.Where(t=>t.kind=="donation").Select(t=>t.id).Distinct().Count()==w.Save.transactions.Count(t=>t.kind=="donation"),"Visitor donation IDs unique");
            int balance=w.Save.coins,pending=w.Save.pending;Check(w.Collect(out _),"Collect actual visitor donations");Check(w.Save.coins==balance+pending&&w.Save.pending==0,"Wallet receives exact pending amount atomically");Check(!w.Collect(out _),"Repeated empty collection rejected");
            string upgrade=w.economy.upgrades.FirstOrDefault(u=>!w.Save.upgrades.Contains(u.id)&&u.price<=w.Save.coins&&u.requiredPopularity<=w.Score.Total)?.id;
            Check(upgrade!=null,"An affordable starter improvement exists");Check(w.Purchase(upgrade,out _),"Purchase funded upgrade: "+upgrade);Check(!w.Purchase(upgrade,out _),"Duplicate upgrade purchase rejected");
            var loaded=FishCollectionStore.Load();Check(loaded.version==FishCollection.CurrentVersion&&!loaded.writeBlocked,"Versioned save reload succeeds");Check(loaded.kept.Single(f=>f.instanceId==id).tankId==tank.id&&loaded.kept.Single(f=>f.instanceId==id).length==length,"Placement and exact specimen persist");Check(loaded.aquarium.coins==w.Save.coins&&loaded.aquarium.upgrades.Contains(upgrade),"Wallet and upgrade persist together");
            File.WriteAllText("Temp/ASTRA05_expected.json",JsonUtility.ToJson(w.Data));w.Panel.Open(null);Capture("Economy");
            Check(Shader.Find("OceanariumFishing/Aquarium Glass and Water").isSupported,"Aquarium transparent URP shader supported");
            report.Add("Restart verification is separate: stop/play and aquarium-restart-checks. No standalone process restart is implied.");
        }
        public static void Restart()
        {
            var w=UnityEngine.Object.FindFirstObjectByType<AquariumWorld>();var expected=JsonUtility.FromJson<FishCollection>(File.ReadAllText("Temp/ASTRA05_expected.json"));
            bool pass=w&&w.Ready&&expected.kept.Where(f=>f.ownership==FishOwnership.Displayed).All(f=>w.Data.kept.Any(k=>k.instanceId==f.instanceId&&k.tankId==f.tankId&&k.length==f.length))&&w.Save.coins==expected.aquarium.coins&&expected.aquarium.upgrades.All(u=>w.Save.upgrades.Contains(u))&&w.Tanks.Sum(t=>t.Fish.Count())==w.Data.kept.Count(f=>f.ownership==FishOwnership.Displayed);
            pass&=JsonUtility.ToJson(w.Save)==JsonUtility.ToJson(FishCollectionStore.Load().aquarium);
            File.WriteAllText("Docs/ASTRA_05_RESTART_RESULTS.txt",(pass?"PASS":"FAIL")+" "+DateTime.UtcNow.ToString("O")+"\nSeparate Play Mode session restores exact displayed instances, visuals, wallet, pending donations, transactions and upgrades.\n");
        }
        static IEnumerator PolishChecks()
        {
            var w=UnityEngine.Object.FindFirstObjectByType<AquariumWorld>();var player=w.Player.GetComponent<LocalPlayerController>();var collection=w.Player.GetComponent<CollectionHUD>();collection.Close();w.Panel.Close();keyboard=InputSystem.AddDevice<Keyboard>();
            foreach(var tank in w.Tanks.Where(t=>w.Unlocked(t)&&!w.Display(t).Any())){var candidate=w.Data.kept.FirstOrDefault(f=>w.Validate(f,tank)==null);if(candidate!=null)Check(w.Place(w.Data.ownerId,candidate.instanceId,tank.id,out _),"Populate another exhibit with an existing owned specimen");}
            player.Teleport(w.arrival.position+Vector3.back*3+Vector3.up*.15f);player.SetViewAngles(0,16);yield return new WaitForSeconds(.7f);Capture("Exterior_Final");
            player.Teleport(w.arrival.position+Vector3.up*.15f);var walk=Walk(player,w.entrance.position+Vector3.forward*1.7f);while(walk.MoveNext())yield return walk.Current;
            Check(Mathf.Abs(player.transform.position.y-w.transform.position.y)<.2f,"Updated terrain and floor align after physical entry");
            var tankTarget=w.Tanks.First(t=>w.Display(t).Any());walk=Walk(player,new Vector3(w.transform.position.x,w.transform.position.y,tankTarget.viewPoint.position.z));while(walk.MoveNext())yield return walk.Current;walk=Walk(player,tankTarget.viewPoint.position);while(walk.MoveNext())yield return walk.Current;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.4f);Check(w.Panel.IsOpen,"Tank modal still opens after environment polish");Capture("Management_Final");yield return new WaitForSeconds(.2f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;Check(!w.Panel.IsOpen&&!UnityEngine.Object.FindFirstObjectByType<FoundationHUD>().SettingsOpen,"Escape closes aquarium without also opening pause");
            player.SetViewAngles(0,30);yield return new WaitForSeconds(.6f);Capture("Interior_Final");
            var f=w.Display(tankTarget).First();var owned=f.Copy();owned.ownership=FishOwnership.Stored;var habitats=tankTarget.definition.habitats;
            try{tankTarget.definition.habitats=new[]{AquariumHabitat.Freshwater};Check(w.Validate(owned,tankTarget)=="habitat","Freshwater tank rejects marine specimen");}finally{tankTarget.definition.habitats=habitats;}
            int before=w.Save.coins;var expensive=w.economy.upgrades.FirstOrDefault(u=>!w.Save.upgrades.Contains(u.id)&&u.price>before);if(expensive!=null){Check(!w.Purchase(expensive.id,out _)&&w.Save.coins==before,"Unaffordable upgrade cannot spend wallet funds");}
            var snapshot=w.Tanks.SelectMany(t=>t.Fish).ToDictionary(fish=>fish.Specimen.instanceId,fish=>fish.transform.localPosition);yield return new WaitForSeconds(5);
            foreach(var t in w.Tanks)foreach(var fish in t.Fish){Vector3 p=fish.transform.localPosition;Check(Mathf.Abs(p.x)<t.definition.dimensions.x/2&&Mathf.Abs(p.y)<t.definition.dimensions.y/2&&Mathf.Abs(p.z)<t.definition.dimensions.z/2,"Swimming remains in volume: "+fish.Specimen.speciesId);Check(Vector3.Distance(p,snapshot[fish.Specimen.instanceId])>.02f,"Independent movement: "+fish.Specimen.speciesId);}
            var old=w.Data.Copy();var duplicate=old.kept.Where(k=>k.ownership==FishOwnership.Displayed).First().Copy();duplicate.instanceId="duplicate-score-probe";old.kept.Add(duplicate);Check(w.Calculate(old).Total-w.Score.Total<w.economy.speciesPoints,"Duplicate specimens have diminishing popularity, not a new diversity bonus");
            w.Panel.Open(null);yield return new WaitForSeconds(.5f);Capture("Reception_Final");yield return new WaitForSeconds(.2f);w.Panel.Close();
        }
    }
}
