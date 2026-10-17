using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace OceanariumFishing.Editor
{
    // Drives the existing Input System and controller. Seeds only select reproducible legal spawns.
    // It never sets fight progress, grants fish, changes species assets or advances simulation time.
    public static class CozyFishingDemo
    {
        static Mouse mouse;static Keyboard keyboard;static FishingController fishing;static CollectionHUD ui;
        static int stage,catchIndex;static double began,stageTime;static bool recover;static string trace;static string firstId;
        public static void Run()
        {
            if(!EditorApplication.isPlaying||mouse!=null)return;
            fishing=UnityEngine.Object.FindFirstObjectByType<FishingController>();ui=fishing.GetComponent<CollectionHUD>();
            if(fishing.Active){Debug.LogWarning("Finish the current cast before starting the demonstration.");return;}
            fishing.SetDifficulty("cozy");GameSession.Current.Languages.Select("en");
            var player=fishing.GetComponent<LocalPlayerController>();player.Teleport(new Vector3(1,.85f,-24));player.SetViewAngles(180,20);
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();began=stageTime=EditorApplication.timeSinceStartup;stage=0;catchIndex=0;recover=false;
            trace="ASTRA 0.4 live input demonstration\nControlled legal spawn seeds, regular hook, neutral directional input, actual wall-clock fight simulation.\n";EditorApplication.update+=Tick;
        }
        static void Send(bool held){var state=new MouseState();if(held)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,state);}
        static void Next(int next){stage=next;stageTime=EditorApplication.timeSinceStartup;}
        static void Capture(string name){Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_04_"+name+".png");}
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying||!fishing||EditorApplication.timeSinceStartup-began>180)throw new Exception("Demonstration interrupted or timed out");
                double age=EditorApplication.timeSinceStartup-stageTime;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                if(stage==0)
                {
                    if(age<1)return;ui.Open(true);var locked=ui.species.FirstOrDefault(s=>!fishing.Collection.Discovered(s.id));if(locked)ui.SelectSpecies(locked.id);trace+="Locked species available before catches: "+ui.species.Count(s=>!fishing.Collection.Discovered(s.id))+"\n";Next(1);return;
                }
                if(stage==1){if(age<.8)return;Capture("Journal_Before");Next(2);return;}
                if(stage==2){if(age<.4)return;ui.Close();Next(3);return;}
                if(stage==3)
                {
                    if(age<.4)return;if(!fishing.GetComponent<InteractionActor>().HasStarterRod)InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));Next(4);return;
                }
                if(stage==4)
                {
                    if(!fishing.Equipped||fishing.State!=FishingState.Idle)return;Send(true);Next(5);return;
                }
                if(stage==5)
                {
                    if(fishing.State==FishingState.Aiming&&fishing.Charge>=.76f)
                    {
                        var zone=FishingZone.At(fishing.AimPoint());if(!zone)throw new Exception("No fishing zone at aimed location");
                        FishRarity desired=catchIndex==0?FishRarity.Common:FishRarity.Rare;int chosen=-1;
                        for(int i=1;i<5000;i++){UnityEngine.Random.InitState(i);int seed=UnityEngine.Random.Range(1,int.MaxValue);var s=zone.Select(seed,12,"clear","none","OF_HomeIsland");if(s&&s.rarity==desired){chosen=i;break;}}
                        if(chosen<0)throw new Exception("No configured spawn for requested rarity");UnityEngine.Random.InitState(chosen);trace+="Legal spawn RNG seed "+chosen+", target rarity "+desired+"\n";Send(false);Next(6);
                    }else Send(true);return;
                }
                if(stage==6)
                {
                    Send(false);if(fishing.State==FishingState.FishBiting&&fishing.StateTime>.85f){Send(true);trace+="Regular hook at "+fishing.StateTime.ToString("0.00")+" seconds\n";recover=false;Next(7);}return;
                }
                if(stage==7)
                {
                    if(fishing.Fight!=null&&(fishing.State==FishingState.Fighting||fishing.State==FishingState.Reeling))
                    {
                        if(fishing.Fight.Tension>.78f)recover=true;if(fishing.Fight.Tension<.42f)recover=false;Send(!recover);
                        if(fishing.Fight.Elapsed>2&&fishing.Fight.Elapsed<2.15f)Capture("Fight_"+catchIndex);return;
                    }
                    if(fishing.State==FishingState.CatchSuccess)
                    {Send(false);trace+="Caught "+fishing.Catch.speciesId+" "+fishing.Catch.rarity+" in "+fishing.Fight.Elapsed.ToString("0.00")+" s; direction input 0; new discovery="+fishing.NewDiscovery+"; personal best="+fishing.NewRecord+"\n";Capture(catchIndex==0?"Common_Catch":"Rare_Catch");Next(8);return;}
                    if(fishing.State==FishingState.FishEscaped)throw new Exception("Fish escaped: "+fishing.Feedback);return;
                }
                if(stage==8)
                {
                    if(age<1)return;string id=fishing.Catch.instanceId;int count=fishing.Collection.kept.Count;
                    if(!fishing.ResolveCatch(true))throw new Exception("Could not persist catch");
                    if(fishing.ResolveCatch(true)||fishing.Collection.kept.Count!=count+1)throw new Exception("Duplicate catch resolution");
                    if(!FishCollectionStore.Load().kept.Any(f=>f.instanceId==id))throw new Exception("Saved fish missing after reload");trace+="Persisted one specimen "+id+"; duplicate claim rejected\n";if(catchIndex==0)firstId=id;ui.SelectFish(id);Next(9);return;
                }
                if(stage==9)
                {
                    if(fishing.State!=FishingState.Idle)return;if(catchIndex==0){catchIndex=1;Next(4);}else{InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.I));Next(10);}return;
                }
                if(stage==10){if(age<1)return;if(!ui.IsOpen||ui.Journal)throw new Exception("I did not open inventory");Capture("Inventory_Detail");trace+="I opens inventory; retained individual selected\n";Next(11);return;}
                if(stage==11){if(age<.5)return;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));Next(12);return;}
                if(stage==12)
                {
                    if(age<.8)return;if(!ui.IsOpen||!ui.Journal)throw new Exception("J did not open journal");
                    ui.SelectSpecies("golden_trevally");Capture("Journal_Discovered");trace+="J opens journal; rare species discovery and personal records visible\n";Finish("PASS");
                }
            }
            catch(Exception e){Finish("FAIL: "+e.Message);Debug.LogException(e);}
        }
        static void Finish(string result)
        {
            EditorApplication.update-=Tick;if(mouse!=null){InputSystem.RemoveDevice(mouse);mouse=null;}if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
            File.WriteAllText("Docs/ASTRA_04_LIVE_INPUT_RESULTS.txt",result+"\n"+trace);Debug.Log("ASTRA 0.4 live demonstration: "+result);
        }
    }
}
