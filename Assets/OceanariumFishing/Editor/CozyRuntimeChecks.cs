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
    public static class CozyRuntimeChecks
    {
        static bool running;static readonly List<string> results=new();
        public static void Run(){if(running||!EditorApplication.isPlaying)return;running=true;GameSession.Current.StartCoroutine(Checks());}
        static void Check(bool ok,string message){results.Add((ok?"PASS: ":"FAIL: ")+message);if(!ok)throw new Exception(message);}
        static IEnumerator Travel(Destination scene){var session=GameSession.Current;session.Travel(scene);double end=EditorApplication.timeSinceStartup+20;while(session.Loading&&EditorApplication.timeSinceStartup<end)yield return null;Check(!session.Loading,"Scene transition: "+scene);for(int i=0;i<5;i++)yield return null;}
        static IEnumerator Checks()
        {
            bool complete=false;results.Clear();Keyboard keys=null;Mouse mouse=null;
            string appearance=File.Exists(AppearanceStore.PathName)?File.ReadAllText(AppearanceStore.PathName):null;
            string collection=File.Exists(FishCollectionStore.PathName)?File.ReadAllText(FishCollectionStore.PathName):null;
            try
            {
                var fishing=UnityEngine.Object.FindFirstObjectByType<FishingController>();var ui=fishing.GetComponent<CollectionHUD>();ui.Close();
                var saved=FishCollectionStore.Load();Check(JsonUtility.ToJson(saved)==JsonUtility.ToJson(fishing.Collection),"Fresh Play Mode session restores all saved inventory, history, records and preferences");
                Check(fishing.Collection.history.Select(f=>f.instanceId).Distinct().Count()==fishing.Collection.history.Count,"No duplicated catch events after restart");
                var player=fishing.GetComponent<LocalPlayerController>();player.Teleport(player.SafeSpawn);player.SetViewAngles(0,18);yield return null;
                Check(UnityEngine.Object.FindObjectsByType<LocalPlayerController>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"One local player and camera");
                Check(JsonUtility.ToJson(player.GetComponentInChildren<CharacterAppearance>().Data)==JsonUtility.ToJson(AppearanceStore.Load()),"Saved customized appearance is preserved");
                keys=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
                foreach(var key in new[]{Key.W,Key.A,Key.S,Key.D})
                {
                    var start=player.transform.position;float time=0;while(time<.45f){InputSystem.QueueStateEvent(keys,new KeyboardState(key));time+=Time.deltaTime;yield return null;}InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;Check(Vector3.Distance(start,player.transform.position)>.35f,key+" moves the character");
                }
                float elapsed=0;while(elapsed<.8f){InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W,Key.LeftShift));elapsed+=Time.deltaTime;yield return null;}Check(player.Speed>4.5f,"Sprint remains responsive");InputSystem.QueueStateEvent(keys,new KeyboardState());
                Quaternion rotation=player.cameraTarget.rotation;for(int i=0;i<8;i++){InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(12,0)}.WithButton(MouseButton.Right));yield return null;}InputSystem.QueueStateEvent(mouse,new MouseState());Check(Quaternion.Angle(rotation,player.cameraTarget.rotation)>1,"RMB camera orbit responds");
                InputSystem.QueueStateEvent(keys,new KeyboardState(Key.I));yield return null;yield return null;Check(ui.IsOpen&&!ui.Journal&&!player.InputEnabled,"Inventory shortcut opens modal and blocks movement");
                InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;InputSystem.QueueStateEvent(keys,new KeyboardState(Key.Escape));yield return null;yield return null;Check(!ui.IsOpen&&!UnityEngine.Object.FindFirstObjectByType<FoundationHUD>().SettingsOpen,"Escape closes inventory without also opening pause menu");InputSystem.QueueStateEvent(keys,new KeyboardState());
                ui.Open(true);yield return null;var heapBefore=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();float seconds=0;for(int i=0;i<120;i++){seconds+=Time.unscaledDeltaTime;yield return null;}long heapAfter=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();results.Add("MEASURED journal: 120 editor frames, mean "+(seconds/120*1000).ToString("0.00")+" ms; managed heap delta "+(heapAfter-heapBefore)+" bytes (includes editor/runtime activity; not per-frame allocation or standalone profiling).");ui.Close();
                InputSystem.RemoveDevice(keys);keys=null;InputSystem.RemoveDevice(mouse);mouse=null;
                yield return Travel(Destination.MainMenu);Check(UnityEngine.Object.FindFirstObjectByType<FoundationHUD>()!=null,"Main menu HUD available");
                yield return Travel(Destination.CharacterCreator);var creator=UnityEngine.Object.FindFirstObjectByType<CharacterCreator>();Check(creator!=null&&creator.preview.Bones.Length==18&&JsonUtility.ToJson(creator.Draft)==JsonUtility.ToJson(AppearanceStore.Load()),"Character Creator restores existing draft and skeleton");
                yield return Travel(Destination.HomeIsland);fishing=UnityEngine.Object.FindFirstObjectByType<FishingController>();Check(fishing!=null&&fishing.rod&&fishing.tuning&&fishing.presentation,"Fishing references survive scene transitions");
                Check((File.Exists(AppearanceStore.PathName)?File.ReadAllText(AppearanceStore.PathName):null)==appearance,"Character save bytes unchanged by collection workflows");
                Check((File.Exists(FishCollectionStore.PathName)?File.ReadAllText(FishCollectionStore.PathName):null)==collection,"Scene transitions do not rewrite or lose collection progress");
                foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))foreach(var material in renderer.sharedMaterials)Check(material&&material.shader.isSupported,"Supported material: "+renderer.name);
                complete=true;
            }
            finally
            {
                if(keys!=null)InputSystem.RemoveDevice(keys);if(mouse!=null)InputSystem.RemoveDevice(mouse);
                File.WriteAllText("Docs/ASTRA_04_REGRESSION_RESULTS.txt",(complete?"PASS":"FAIL")+" "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",results));running=false;
            }
        }
    }
}
