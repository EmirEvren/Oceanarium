using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oceanarium.Editor
{
    public static class PrototypeTools
    {
        public const string ScenePath = "Assets/_Oceanarium/Scenes/HomeReefPrototype.unity";

        [MenuItem("Oceanarium/Create or Open Prototype %&o")]
        public static void Open()
        {
            if(EditorApplication.isPlaying) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(File.Exists(ScenePath)) {EditorSceneManager.OpenScene(ScenePath);return;}
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Oceanarium - Prototype Bootstrap");
            root.AddComponent<IslandPrototype>().SetShader(Shader.Find("Universal Render Pipeline/Lit"));
            EditorSceneManager.SaveScene(scene,ScenePath);
            Selection.activeGameObject=root;
        }

        [MenuItem("Oceanarium/Validate Running Prototype")]
        public static void Validate()
        {
            try
            {
                if(!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
                var prototype=UnityEngine.Object.FindFirstObjectByType<IslandPrototype>();
                Require(prototype!=null,"Prototype bootstrap exists");
                Require(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"Exactly one camera");
                Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"Exactly one audio listener");
                Require(prototype.RodCount==(prototype.HasRod?3:4),"Rod pickup preserves expected count");
                Require(GameObject.Find("Supply Chest")!=null,"Supply chest exists");
                Require(GameObject.Find("Cabin")!=null,"Cabin exists");
                Require(GameObject.Find("Player").GetComponent<CharacterController>()!=null,"Character controller exists");
                Require(Physics.Raycast(new Vector3(2,10,-1),Vector3.down,out var ground,20),"Spawn has ground");
                Require(ground.point.y<.3f && ground.point.y>=0,"Spawn ground is flat, not a scaled capsule");
                foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    Require(renderer.sharedMaterial!=null && renderer.sharedMaterial.shader.isSupported,"Supported material: "+renderer.name);
                Directory.CreateDirectory("Docs");
                File.WriteAllText("Docs/SmokeTestResult.txt","PASS - "+DateTime.UtcNow.ToString("O")+"\nScene: "+SceneManager.GetActiveScene().path+"\nBootstrap, camera, audio, rod count, chest, cabin, controller and material checks passed.\nManual movement, interaction, build and performance require separate verification.\n");
                Debug.Log("Oceanarium smoke test PASS. See Docs/SmokeTestResult.txt");
            }
            catch(Exception exception) {Debug.LogException(exception);}
        }
        private static void Require(bool condition,string message) {if(!condition) throw new InvalidOperationException(message);}
    }
}
