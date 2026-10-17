using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static class FishingChecks
    {
        static IEnumerator routine;static FishingController fishing;static readonly List<string> results=new();static double deadline;
        public static void Run(){if(!EditorApplication.isPlaying || routine!=null)return;results.Clear();deadline=EditorApplication.timeSinceStartup+180;routine=Checks();EditorApplication.update+=Tick;}
        static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Fishing checks timed out");if(routine.MoveNext())return;Finish("PASS");}catch(Exception e){results.Add(e.ToString());Finish("FAIL");Debug.LogException(e);}}
        static void Finish(string status){EditorApplication.update-=Tick;(routine as IDisposable)?.Dispose();routine=null;if(fishing)fishing.ExternalInput=false;File.WriteAllText("Docs/ASTRA_03_TEST_RESULTS.txt",status+" "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",results));Debug.Log("ASTRA 0.3 checks: "+status);}
        static void Check(bool ok,string name){if(!ok)throw new Exception(name);results.Add("PASS: "+name);}
        static IEnumerator Advance(FishingState state,int limit=800)
        {for(int i=0;i<limit && fishing.State!=state;i++){fishing.Tick(.05f,false,false,false,0);yield return null;}Check(fishing.State==state,"Reached "+state);}
        static IEnumerator CastAndHook()
        {
            Check(fishing.BeginAim(),"Begin charged cast");fishing.Tick(1.2f,false,false,false,0);Check(fishing.Charge>.7f && fishing.Charge<.9f,"Charge reaches optimal band");Check(fishing.Cast(),"Valid ocean cast accepted");
            Check(!fishing.BeginAim() && !fishing.Cast(),"Overlapping casts rejected");
            var initial=fishing.presentation.Bobber.position;yield return null;
            var travel=Advance(FishingState.WaitingForBite);while(travel.MoveNext())yield return null;
            Check(Vector3.Distance(initial,fishing.presentation.Bobber.position)>3,"Bobber travels to water");Check(fishing.presentation.Line.positionCount==25,"Fishing line has curved control points");
            var water=UnityEngine.Object.FindFirstObjectByType<FishingWater>();fishing.Tick(.01f,false,false,false,0);Check(Mathf.Abs(fishing.presentation.Bobber.position.y-water.Height(fishing.presentation.Bobber.position,Time.time))<.1f,"Bobber tracks shader water height");
            var bite=Advance(FishingState.FishBiting);while(bite.MoveNext())yield return null;Check(fishing.Hook(),"Reaction input hooks fish");Check(fishing.Feedback=="perfect","Perfect hook window rewards timing");
            var hooked=Advance(FishingState.Fighting);while(hooked.MoveNext())yield return null;
        }
        static IEnumerator Win()
        {
            float peak=0,min=1;bool recovering=false;
            for(int i=0;i<1800 && fishing.State!=FishingState.CatchSuccess && fishing.State!=FishingState.FishEscaped;i++)
            {if(fishing.Fight.Tension>.72f)recovering=true;if(fishing.Fight.Tension<.3f)recovering=false;fishing.Tick(.05f,!recovering,false,false,-fishing.Fight.Direction);peak=Mathf.Max(peak,fishing.Fight.Tension);min=Mathf.Min(min,fishing.Fight.Tension);yield return null;}
            Check(fishing.State==FishingState.CatchSuccess,"Skillful reeling produces genuine catch");Check(peak>min+.2f,"Tension rises and recovers");Check(fishing.Fight.Combo>1 && fishing.Fight.Combo<=5,"Earned combo bounded at five");Check(fishing.Catch.weight>0 && fishing.Catch.length>0,"Catch has generated dimensions");
        }
        static IEnumerator Checks()
        {
            while(GameSession.Current==null || !GameSession.Current.Languages.Ready)yield return null;
            fishing=UnityEngine.Object.FindFirstObjectByType<FishingController>();Check(fishing!=null,"Fishing controller exists on Home Island");Check(JsonUtility.ToJson(fishing.Collection)==JsonUtility.ToJson(FishCollectionStore.Load()),"Fresh player session restores saved collection");fishing.ExternalInput=true;var player=fishing.GetComponent<LocalPlayerController>();var actor=fishing.GetComponent<InteractionActor>();
            player.Teleport(new Vector3(1,.85f,-24));player.SetViewAngles(180,20);for(int i=0;i<20;i++)yield return null;
            var pickup=UnityEngine.Object.FindObjectsByType<IslandInteractable>(FindObjectsSortMode.None).First(r=>r.kind==InteractionKind.StarterRod && r.CanInteract(actor));pickup.Interact(actor);Check(actor.HasStarterRod,"Existing pickup grants rod");Check(fishing.Equip(),"Starter rod equipped on hand");Check(!fishing.Equip(),"Duplicate equip rejected");
            var idle=Advance(FishingState.Idle);while(idle.MoveNext())yield return null;
            Check(fishing.presentation.Tip!=null && fishing.presentation.Tip.IsChildOf(player.visual),"Rod tip follows avatar hierarchy");
            foreach(var zone in UnityEngine.Object.FindObjectsByType<FishingZone>(FindObjectsSortMode.None))Check(zone.Select(81,12,"clear","none","OF_HomeIsland")!=null,"Spawn distribution: "+zone.zoneId);
            var definitions=AssetDatabase.FindAssets("t:FishSpecies",new[]{"Assets/OceanariumFishing/Fishing"}).Select(g=>AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(g))).ToArray();Check(definitions.Length==8,"Eight species definitions");
            foreach(var species in definitions)
            {
                var specimen=FishInstance.Roll(species,185,"test");var again=FishInstance.Roll(species,185,"test");Check(specimen.weight==again.weight && specimen.length==again.length,"Seeded specimen: "+species.id);
                var fight=new FishingFight(species,fishing.rod,fishing.tuning,specimen,true);bool recover=false;int steps=0;for(;steps<2400&&!fight.Won&&!fight.Lost;steps++){if(fight.Tension>.72f)recover=true;if(fight.Tension<.3f)recover=false;fight.Step(.05f,!recover,-fight.Direction);}Check(fight.Won,"Starter rod balanced catch: "+species.id+" in "+(steps*.05f).ToString("0.0")+"s");
                var fail=new FishingFight(species,fishing.rod,fishing.tuning,specimen,false);for(int i=0;i<2000&&!fail.Lost;i++)fail.Step(.05f,true,fail.Direction);Check(fail.Lost,"Overload escape: "+species.id);
            }
            var first=CastAndHook();while(first.MoveNext())yield return null;var win=Win();while(win.MoveNext())yield return null;
            int before=fishing.Collection.kept.Count;var caught=fishing.Catch;Check(fishing.ResolveCatch(true),"KEEP persists catch");Check(!fishing.ResolveCatch(true),"Duplicate reward rejected");Check(fishing.Collection.kept.Count==before+1,"Exactly one owned instance added");Check(FishCollectionStore.Load().kept.Any(f=>f.instanceId==caught.instanceId),"Disk reload preserves owned fish");
            idle=Advance(FishingState.Idle);while(idle.MoveNext())yield return null;Check(!player.FishingLocked && !fishing.presentation.Bobber && !fishing.presentation.Line,"Catch cleanup restores movement and removes cast objects");
            var second=CastAndHook();while(second.MoveNext())yield return null;win=Win();while(win.MoveNext())yield return null;before=fishing.Collection.kept.Count;Check(fishing.ResolveCatch(false),"RELEASE resolves catch");Check(fishing.Collection.kept.Count==before,"Release adds no inventory");
            idle=Advance(FishingState.Idle);while(idle.MoveNext())yield return null;
            Check(fishing.BeginAim(),"Repeated cast available");fishing.Tick(1.2f,false,false,false,0);Check(fishing.Cast(),"Repeat cast succeeds");var missed=Advance(FishingState.FishEscaped);while(missed.MoveNext())yield return null;Check(fishing.Feedback=="missed","Missed hook produces escape");idle=Advance(FishingState.Idle);while(idle.MoveNext())yield return null;
            fishing.BeginAim();fishing.Cancel();idle=Advance(FishingState.Idle);while(idle.MoveNext())yield return null;Check(!player.FishingLocked,"Cancel restores input");
            player.SetViewAngles(0,20);for(int i=0;i<30;i++)yield return null;fishing.BeginAim();fishing.Tick(1.2f,false,false,false,0);Check(!fishing.Cast(),"Land cast rejected");idle=Advance(FishingState.Idle);while(idle.MoveNext())yield return null;player.SetViewAngles(180,20);
            foreach(var code in LanguageService.Codes){GameSession.Current.Languages.Select(code);while(GameSession.Current.Languages.Busy)yield return null;Check(!string.IsNullOrEmpty(GameSession.Current.Languages.Text("fishing.keep")),"Fishing localization fallback: "+code);}GameSession.Current.Languages.Select("tr");
            Check(fishing.GetComponentInChildren<CharacterAppearance>().Data!=null,"Customized avatar preserved");Check(UnityEngine.Object.FindObjectsByType<FishingController>(FindObjectsSortMode.None).Length==1,"One local fishing controller");
        }
    }
}
