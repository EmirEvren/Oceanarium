using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace OceanariumFishing.Editor
{
    // Injects actual Input System device states; never advances simulation or grants success.
    public static class FishingInputDemo
    {
        static Mouse mouse;static Keyboard keyboard;static FishingController fishing;static double began;static bool recover,hookSent;static int stage;static string trace;
        public static void Run()
        {
            if(!EditorApplication.isPlaying || mouse!=null)return;fishing=UnityEngine.Object.FindFirstObjectByType<FishingController>();if(!fishing)return;
            var player=fishing.GetComponent<LocalPlayerController>();player.Teleport(new Vector3(1,.85f,-24));player.SetViewAngles(180,20);mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();began=EditorApplication.timeSinceStartup;stage=0;trace="Real Input System fishing demonstration\n";hookSent=false;recover=false;EditorApplication.update+=Tick;
        }
        static void Send(bool held,float delta=0){var state=new MouseState{delta=new Vector2(delta,0)};if(held)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,state);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying || !fishing || EditorApplication.timeSinceStartup-began>90){Finish("FAIL: interrupted or timed out");return;}
            if(stage==0)
            {InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));stage=1;return;}
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            if(!fishing.Equipped)return;
            if(stage==1 && fishing.State==FishingState.Idle){Send(true);stage=2;trace+="Rod equipped via E; LMB pressed\n";return;}
            if(stage==2)
            {if(fishing.State==FishingState.Aiming && fishing.Charge>=.76f){Send(false);stage=3;trace+="Charged release casts\n";}else Send(true);return;}
            if(fishing.State==FishingState.FishBiting && !hookSent){Send(true);hookSent=true;trace+="LMB hooks during bite window\n";return;}
            if(fishing.State==FishingState.Fighting || fishing.State==FishingState.Reeling)
            {
                if(fishing.Fight.Tension>.72f)recover=true;if(fishing.Fight.Tension<.3f)recover=false;
                float target=-fishing.Fight.Direction;Send(!recover,Mathf.Clamp((target-fishing.Steering)/.015f,-12,12));return;
            }
            if(fishing.State==FishingState.CatchSuccess){trace+="Genuine catch reached through live simulation: "+fishing.Catch.speciesId+"\n";Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_03_Catch.png");Finish("PASS");return;}
            if(fishing.State==FishingState.FishEscaped){Finish("FAIL: "+fishing.Feedback);return;}
            Send(false);
        }
        static void Finish(string result)
        {EditorApplication.update-=Tick;if(mouse!=null){InputSystem.RemoveDevice(mouse);mouse=null;}if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}File.WriteAllText("Docs/ASTRA_03_INPUT_RESULTS.txt",result+"\n"+trace);Debug.Log("Fishing input demo: "+result);}
    }
}
