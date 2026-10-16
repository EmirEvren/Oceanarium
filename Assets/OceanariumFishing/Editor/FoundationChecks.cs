using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace OceanariumFishing.Editor
{
    public static class FoundationChecks
    {
        private static IEnumerator routine;
        private static readonly List<string> results=new();
        private static double deadline;
        private static Keyboard testKeyboard;
        private static Mouse testMouse;
        [MenuItem("Oceanarium Fishing/Run Play Mode Checks %&t")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying){Debug.LogWarning("Enter Play Mode in OF_HomeIsland first.");return;}
            if(routine!=null)return;
            results.Clear();deadline=EditorApplication.timeSinceStartup+90;routine=Checks();EditorApplication.update+=Tick;
        }
        private static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying)throw new Exception("Play Mode stopped during checks.");
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Checks timed out.");
                if(routine.MoveNext())return;
                Complete("PASS");
            }
            catch(Exception exception){results.Add(exception.ToString());Complete("FAIL");Debug.LogException(exception);}
        }
        private static void Complete(string status)
        {
            if(testKeyboard!=null){InputSystem.RemoveDevice(testKeyboard);testKeyboard=null;}
            if(testMouse!=null){InputSystem.RemoveDevice(testMouse);testMouse=null;}
            (routine as IDisposable)?.Dispose();routine=null;EditorApplication.update-=Tick;
            Directory.CreateDirectory("Docs");File.WriteAllText("Docs/ASTRA_00_TEST_RESULTS.txt",status+" - "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",results));Debug.Log("ASTRA 0.0 checks: "+status);
        }
        private static void Check(bool value,string message){if(!value)throw new Exception(message);results.Add("PASS: "+message);}
        private static IEnumerator Checks()
        {
            while(GameSession.Current==null || !GameSession.Current.Languages.Ready)yield return null;
            var session=GameSession.Current;
            Check(LocalizationSettings.AvailableLocales.Locales.Count==17,"17 locale assets loaded");
            Check(EditorBuildSettings.scenes.Count(s=>s.path.StartsWith(FoundationBuilder.Root+"/Scenes/"))==8,"8 foundation scenes registered");
            var player=UnityEngine.Object.FindFirstObjectByType<LocalPlayerController>();Check(player!=null,"Home Island local controller exists");
            Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"One local camera");
            Check(UnityEngine.Object.FindObjectsByType<IslandInteractable>(FindObjectsSortMode.None).Count(x=>x.kind==InteractionKind.StarterRod)==4,"Exactly four starter rods");
            Check(Physics.Raycast(new Vector3(5,10,-5),Vector3.down,out var hit,15) && hit.point.y>0 && hit.point.y<2,"Terrain collision height is valid");
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                foreach(var material in renderer.sharedMaterials)if(material==null || !material.shader.isSupported)throw new Exception("Unsupported material: "+renderer.name);
            results.Add("PASS: all scene materials use supported shaders");
            foreach(var code in LanguageService.Codes)
            {
                session.Languages.Select(code);while(session.Languages.Busy)yield return null;
                Check(session.Languages.SelectedCode==code && !string.IsNullOrEmpty(session.Languages.Text("interact.chest")),"Locale switch and fallback: "+code);
            }
            session.Languages.Select("en");while(session.Languages.Busy)yield return null;
            Check(PlayerPrefs.GetString("OF.Locale")=="en","Language preference persisted");
            testKeyboard=InputSystem.AddDevice<Keyboard>();testMouse=InputSystem.AddDevice<Mouse>();
            var start=player.transform.position;
            double until=EditorApplication.timeSinceStartup+1;
            while(EditorApplication.timeSinceStartup<until){InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.W));yield return null;}
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
            Check(Vector3.Distance(start,player.transform.position)>1.5f,"W input moves player on terrain");
            until=EditorApplication.timeSinceStartup+.4;
            while(EditorApplication.timeSinceStartup<until)yield return null;
            Check(player.Speed<.15f,"Release decelerates player to rest");
            until=EditorApplication.timeSinceStartup+.8;
            while(EditorApplication.timeSinceStartup<until){InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.W,Key.LeftShift));yield return null;}
            Check(player.Speed>4.5f,"Shift sprint reaches running speed");InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
            var rotation=player.cameraTarget.rotation;
            until=EditorApplication.timeSinceStartup+.25;
            while(EditorApplication.timeSinceStartup<until){InputSystem.QueueStateEvent(testMouse,new MouseState{delta=new Vector2(3,0)}.WithButton(MouseButton.Right));yield return null;}
            InputSystem.QueueStateEvent(testMouse,new MouseState());
            Check(Quaternion.Angle(rotation,player.cameraTarget.rotation)>2,"Right mouse input rotates camera target");
            player.Teleport(new Vector3(1,-4,-9));until=EditorApplication.timeSinceStartup+.5;
            while(EditorApplication.timeSinceStartup<until)yield return null;
            Check(player.transform.position.y>0,"Below-terrain recovery returns to safe spawn");
            player.Teleport(new Vector3(1,.85f,-18));until=EditorApplication.timeSinceStartup+.3;
            while(EditorApplication.timeSinceStartup<until)yield return null;
            var interactor=player.GetComponent<PlayerInteractor>();
            Check(interactor.Nearby!=null && interactor.Nearby.PromptKey=="interact.starterrod","Pier proximity resolves rod prompt");
            var actor=player.GetComponent<InteractionActor>();
            var rods=UnityEngine.Object.FindObjectsByType<IslandInteractable>(FindObjectsSortMode.None).Where(x=>x.kind==InteractionKind.StarterRod).ToArray();
            var nearby=interactor.Nearby;InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.E));
            until=EditorApplication.timeSinceStartup+.2;while(EditorApplication.timeSinceStartup<until)yield return null;
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
            Check(actor.HasStarterRod && !nearby.CanInteract(actor),"E input claims one rod");
            var other=rods.First(r=>r.CanInteract(actor));other.Interact(actor);Check(other.CanInteract(actor),"Second rod preserved for another actor");
            InputSystem.RemoveDevice(testKeyboard);testKeyboard=null;InputSystem.RemoveDevice(testMouse);testMouse=null;
            session.Travel(Destination.MainMenu);while(session.Loading)yield return null;
            Check(SceneManager.GetActiveScene().name==session.catalog.Resolve(Destination.MainMenu),"Home -> Menu transition");
            Check(GameSession.Current==session,"Session survives transition");
            session.Travel(Destination.HomeIsland);while(session.Loading)yield return null;
            for(int j=0;j<10;j++)yield return null;
            Check(UnityEngine.Object.FindObjectsByType<LocalPlayerController>(FindObjectsSortMode.None).Length==1,"Menu -> Home: exactly one local player");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"No duplicate audio listener after travel");
            results.Add("Input tests use temporary Input System devices. Manual feel, visual quality, standalone build and multiplayer remain separate checks.");
        }
        [MenuItem("Oceanarium Fishing/Capture Game View")]
        public static void Capture(){Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_00_HomeIsland.png");}
        [MenuItem("Oceanarium Fishing/Open Boot Scene")]
        public static void Boot(){if(!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(FoundationBuilder.Root+"/Scenes/OF_Boot.unity");}
    }
}
