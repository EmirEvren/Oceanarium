using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static class CreatorChecks
    {
        static IEnumerator routine;static readonly List<string> results=new();static double deadline;static string expected;
        [MenuItem("Oceanarium Fishing/Run ASTRA 0.2 Checks")]
        public static void Run(){if(!EditorApplication.isPlaying || routine!=null)return;results.Clear();deadline=EditorApplication.timeSinceStartup+180;routine=Checks();EditorApplication.update+=Tick;}
        static void Tick()
        {try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Creator checks timed out");if(routine.MoveNext())return;Finish("PASS");}catch(Exception e){results.Add(e.ToString());Finish("FAIL");Debug.LogException(e);}}
        static void Finish(string status){EditorApplication.update-=Tick;(routine as IDisposable)?.Dispose();routine=null;Directory.CreateDirectory("Docs");File.WriteAllText("Docs/ASTRA_02_TEST_RESULTS.txt",status+" "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",results));Debug.Log("ASTRA 0.2 checks: "+status);}
        static void Check(bool value,string message){if(!value)throw new Exception(message);results.Add("PASS: "+message);}
        static IEnumerator Checks()
        {
            while(GameSession.Current==null || !GameSession.Current.Languages.Ready)yield return null;
            var creator=UnityEngine.Object.FindFirstObjectByType<CharacterCreator>();Check(creator!=null,"Creator running in Play Mode");
            foreach(var slot in CosmeticCatalog.Options)
            {
                foreach(var id in slot.Value)
                {
                    creator.Select(slot.Key,id);yield return null;var avatar=creator.preview;
                    if(avatar.Data.Get(slot.Key)!=id)throw new Exception("Selection not applied: "+id);
                    foreach(var renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        if(!renderer.gameObject.activeSelf)continue;var mesh=renderer.sharedMesh;
                        if(mesh==null || renderer.sharedMaterial==null || !renderer.sharedMaterial.shader.isSupported)throw new Exception("Missing mesh/material: "+id);
                        if(mesh.bindposes.Length!=18 || renderer.bones.Any(b=>b==null))throw new Exception("Invalid skeleton: "+id);
                        foreach(var w in mesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f)throw new Exception("Invalid weight: "+id);
                    }
                }
                results.Add("PASS: all "+slot.Value.Length+" "+slot.Key+" selections applied with valid meshes, materials, bones and weights");
            }
            for(int i=0;i<30;i++){creator.Randomize();var d=creator.Draft;foreach(var p in CosmeticCatalog.Options)if(!p.Value.Contains(d.Get(p.Key)))throw new Exception("Invalid random item");yield return null;}
            results.Add("PASS: 30 random appearances");creator.ResetAppearance();Check(JsonUtility.ToJson(creator.Draft)==JsonUtility.ToJson(new AppearanceData()),"Reset restores defaults");
            var invalid=new AppearanceData{hair="removed-content",skin=null};invalid.Validate();Check(invalid.hair==new AppearanceData().hair && invalid.skin==new AppearanceData().skin,"Missing/invalid cosmetic IDs fall back");
            var path=Path.GetFullPath("Temp/appearance-test.json");Check(AppearanceStore.Save(creator.Draft,out _,path),"Atomic JSON save");Check(JsonUtility.ToJson(AppearanceStore.Load(path))==JsonUtility.ToJson(creator.Draft),"Disk round trip");
            File.WriteAllText(path,"not json");Check(AppearanceStore.Load(path).hair==new AppearanceData().hair,"Malformed JSON fallback");File.WriteAllText(path,"{\"version\":900,\"hair\":\"short_classic\"}");Check(AppearanceStore.Load(path).hair==new AppearanceData().hair,"Unknown future version is preserved on disk and safely defaults");
            foreach(var code in LanguageService.Codes){GameSession.Current.Languages.Select(code);while(GameSession.Current.Languages.Busy)yield return null;Check(!string.IsNullOrWhiteSpace(GameSession.Current.Languages.Text("creator.title")) && !string.IsNullOrWhiteSpace(GameSession.Current.Languages.Text("creator.long_wavy")),"Creator localization/fallback: "+code);}
            GameSession.Current.Languages.Select("tr");while(GameSession.Current.Languages.Busy)yield return null;
            creator.Select("hair","long_wavy");creator.Select("hairColor","ginger");creator.Select("top","hoodie");creator.Select("bottom","long_skirt");creator.Select("accessory","backpack");expected=JsonUtility.ToJson(creator.Draft);Check(creator.Confirm(),"Confirm saves and starts Home Island transition");
            while(GameSession.Current.Loading)yield return null;for(int i=0;i<10;i++)yield return null;
            var players=UnityEngine.Object.FindObjectsByType<LocalPlayerController>(FindObjectsSortMode.None);Check(players.Length==1,"Exactly one gameplay player");var applied=players[0].GetComponentInChildren<CharacterAppearance>();Check(applied!=null && JsonUtility.ToJson(applied.Data)==expected,"Gameplay reconstructs identical saved appearance");Check(players[0].GetComponent<CharacterMotion>()==null,"No competing placeholder animator");Check(players[0].GetComponent<PlayerInteractor>()!=null && players[0].view!=null,"Existing interactions and camera preserved");
            Check(JsonUtility.ToJson(AppearanceStore.Load())==expected,"Saved appearance remains available to a fresh session");
        }
    }
}
