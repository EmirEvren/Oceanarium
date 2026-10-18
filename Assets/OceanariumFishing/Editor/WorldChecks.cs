using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace OceanariumFishing.Editor
{
    public static class WorldChecks
    {
        static List<string> log=new();static void Check(bool ok,string message){log.Add((ok?"PASS: ":"FAIL: ")+message);if(!ok)throw new Exception(message);}
        public static void Run()
        {
            log.Clear();bool pass=true;
            try
            {
                var settings=Resources.Load<WorldSettings>("WorldSettings");Check(settings&&settings.weather.Length==9&&settings.seasons.Length==4,"Nine weather and four seasonal assets");
                var sim=new WorldSimulation(new(){minute=1439},settings);sim.Tick(1);Check(sim.Day==1&&sim.Hour==0,"Midnight rollover");sim.SetMinute(28*1440-1);sim.Tick(1);Check(sim.Month==1&&sim.DayOfMonth==1,"Day 28 advances season");sim.SetMinute(112*1440-1);sim.Tick(1);Check(sim.Year==2&&sim.Month==0,"Winter advances year");
                sim=new(new(){minute=480},settings);for(int n=0;n<6000;n++)sim.Tick(.01);Check(Math.Abs(sim.State.minute+sim.State.fraction-540)<.00001,"Fractional accumulator preserves 60 simulated minutes");sim.Paused=true;var minute=sim.State.minute;sim.Tick(600);Check(sim.State.minute==minute,"Paused clock does not advance");sim.Paused=false;
                var clone=new WorldSimulation(JsonUtility.FromJson<WorldState>(JsonUtility.ToJson(sim.State)),settings);for(int day=0;day<112;day++)for(int block=0;block<4;block++)Check(sim.Forecast(day,block)==clone.Forecast(day,block),"Saved-seed forecast "+day+":"+block);
                for(int day=0;day<84;day++)for(int block=0;block<4;block++)Check(sim.Forecast(day,block).id!="snow","No snow outside winter "+day+":"+block);
                sim.SetMinute(23*60);Check(sim.MerchantAvailable,"Merchant on configured Monday night");sim.SetMinute(25*60);Check(sim.MerchantAvailable,"Merchant carries trading night across midnight");sim.SetMinute(26*60);Check(!sim.MerchantAvailable,"Merchant leaves at 02:00");Check(sim.NextMorning==2*1440+480,"Sleep always chooses next day morning");
                var npc=Resources.Load<TownCatalog>("TownContent").residents.First(r=>r.id=="oliver");Check(!npc.ShopOpen(new TownClock(19*60))&&!npc.ShopOpen(new TownClock(13*60))&&npc.ShopOpen(new TownClock(10*60)),"Shop closes at night and lunch");
                var data=new FishCollection();data.Normalize();data.world=sim.State.Copy();data.aquarium.coins=77;data.town.flags.Add("fixture");string path="Temp/ASTRA09_fixture.json";Check(FishCollectionStore.Save(data,path),"Atomic world save");var loaded=FishCollectionStore.Load(path);Check(loaded.world.minute==data.world.minute&&loaded.world.seed==data.world.seed&&loaded.aquarium.coins==77&&loaded.town.flags.Contains("fixture"),"World snapshot preserves wallet and town flags");
                var common=AssetDatabase.FindAssets("t:FishSpecies",new[]{"Assets/OceanariumFishing"}).Select(g=>AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(g))).Where(f=>f.rarity==FishRarity.Common).ToArray();Check(common.Length>0&&common.All(f=>f.startHour==0&&f.endHour==24&&f.weather.Length==9),"Starter common species remain available all day in every weather");
                Check(settings.tropicalSnowMultiplier<=.15f,"Tropical snowfall restrained");
            }
            catch(Exception e){pass=false;log.Add(e.ToString());Debug.LogException(e);}
            File.WriteAllText("Docs/ASTRA_09_SIMULATION_TESTS.txt",(pass?"PASS ":"FAIL ")+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",log));
        }
        public static void Live(){if(EditorApplication.isPlaying)GameSession.Current.StartCoroutine(LiveGuard());}
        static IEnumerator LiveGuard(){log.Clear();bool pass=true;var it=LiveSteps();while(true){bool next;object current=null;try{next=it.MoveNext();if(next)current=it.Current;}catch(Exception e){pass=false;log.Add(e.ToString());Debug.LogException(e);break;}if(!next)break;yield return current;}File.WriteAllText("Docs/ASTRA_09_RUNTIME_TESTS.txt",(pass?"PASS ":"FAIL ")+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",log));}
        static IEnumerator LiveSteps()
        {
            var time=WorldTimeService.Current;Check(time&&time.Simulation!=null,"World service initialized");var sim=time.Simulation;var player=UnityEngine.Object.FindFirstObjectByType<FishingController>();var before=sim.State.Copy();string fish=string.Join("|",player.Collection.kept.Select(f=>f.instanceId+":"+f.tankId));string layout=JsonUtility.ToJson(player.Collection.aquarium.layout);string appearance=JsonUtility.ToJson(AppearanceStore.Load());string difficulty=player.Collection.difficulty;
            var controller=player.GetComponent<LocalPlayerController>();controller.Teleport(new Vector3(0,3,-18));controller.SetViewAngles(180,10);sim.Paused=true;
            foreach(var item in new[]{("Sunrise",6f),("Midday",12f),("Sunset",18f),("Moonlight",23f)}){Check(time.AdvanceTo(sim.Day*1440+(long)(item.Item2*60)),"Set "+item.Item1+" through persisted authority");yield return new WaitForSeconds(4);ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_09_"+item.Item1+".png");yield return null;}
            foreach(var weather in new[]{"light_rain","windy","thunderstorm","snow"}){sim.Force(weather);time.Flush();yield return new WaitForSeconds(5);ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_09_"+weather+".png");yield return null;Check(sim.Current.id==weather,"Forced weather and persisted state: "+weather);}
            Check(time.Flush(),"Flush current world");var save=FishCollectionStore.Load();Check(save.world.minute==sim.State.minute&&save.world.forcedWeather==sim.State.forcedWeather,"World/weather save roundtrip");
            Check(string.Join("|",player.Collection.kept.Select(f=>f.instanceId+":"+f.tankId))==fish&&JsonUtility.ToJson(player.Collection.aquarium.layout)==layout&&JsonUtility.ToJson(AppearanceStore.Load())==appearance&&player.Collection.difficulty==difficulty,"Visual/time checks preserve specimens, museum, appearance and difficulty");
            // Restore original simulation snapshot after reversible visual QA; never reset player progress.
            sim.ApplySnapshot(before);sim.Paused=false;time.Flush();
        }
    }
}
