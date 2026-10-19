using System;
using System.IO;
using FishNet.Managing.Object;
using FishNet.Object;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering.Universal;
namespace OceanariumFishing.Editor
{
    [InitializeOnLoad] public static class NetworkBuilder
    {
        const string R="Assets/OceanariumFishing",D=R+"/Resources";
        static NetworkBuilder(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            const string path="Temp/OF_NetworkCommand.txt";if(!File.Exists(path))return;
            string command;try{command=File.ReadAllText(path).Trim();File.Delete(path);}catch(IOException){return;}
            try
            {
                if(command=="setup")Build();else if(command=="checks")NetworkChecks.Run();else if(command=="build")BuildDevelopment();
                else if(command=="host")NetworkSession.Current.StartDevelopment(true);else if(command=="client")NetworkSession.Current.StartDevelopment(false);
                else if(command=="leave")NetworkSession.Current.Leave();else if(command=="main")UnityEditor.SceneManagement.EditorSceneManager.OpenScene(R+"/Scenes/OF_MainMenu.unity");
                else if(command=="world-checks")WorldChecks.Run();else if(command=="world-live")WorldChecks.Live();
                else if(command=="refresh")AssetDatabase.Refresh();else if(command=="capture")ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_10_Current.png");
                Debug.Log("ASTRA 1.0 command completed: "+command);
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/OF_NetworkFailure.txt",e.ToString());}
        }
        [MenuItem("Oceanarium Fishing/Build ASTRA 1.0 Network Foundation")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            var settings=AssetDatabase.LoadAssetAtPath<OceanNetworkSettings>(D+"/OceanNetworkSettings.asset");if(!settings){settings=ScriptableObject.CreateInstance<OceanNetworkSettings>();AssetDatabase.CreateAsset(settings,D+"/OceanNetworkSettings.asset");}
            var root=PrefabUtility.LoadPrefabContents(R+"/Prefabs/OF_Player.prefab");
            try
            {
                root.name="OF_NetworkPlayer";root.AddComponent<NetworkObject>();root.AddComponent<NetworkPlayer>();
                var player=root.GetComponent<LocalPlayerController>();var target=new GameObject("Owned camera target").transform;target.SetParent(root.transform,false);player.cameraTarget=target;
                var cam=new GameObject("Owned camera",typeof(Camera),typeof(AudioListener),typeof(CinemachineBrain));cam.transform.SetParent(root.transform,false);cam.tag="MainCamera";
                var camera=cam.GetComponent<Camera>();camera.farClipPlane=280;camera.fieldOfView=48;camera.nearClipPlane=.1f;cam.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;player.view=camera;
                var follow=new GameObject("Owned follow camera").AddComponent<CinemachineCamera>();follow.transform.SetParent(root.transform,false);follow.Follow=target;follow.Lens.FieldOfView=48;
                var arm=follow.gameObject.AddComponent<CinemachineThirdPersonFollow>();arm.CameraDistance=7;arm.ShoulderOffset=new(.45f,0,0);arm.VerticalArmLength=.5f;arm.Damping=new(.12f,.15f,.12f);
                arm.AvoidObstacles=new(){Enabled=true,CollisionFilter=1,IgnoreTag="Player",CameraRadius=.22f,DampingIntoCollision=.05f,DampingFromCollision=.3f};
                root.GetComponentInChildren<CharacterAppearance>().loadLocal=false;PrefabUtility.SaveAsPrefabAsset(root,D+"/OF_NetworkPlayer.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var prefabs=AssetDatabase.LoadAssetAtPath<SinglePrefabObjects>(D+"/OF_NetworkPrefabs.asset");if(!prefabs){prefabs=ScriptableObject.CreateInstance<SinglePrefabObjects>();AssetDatabase.CreateAsset(prefabs,D+"/OF_NetworkPrefabs.asset");}
            prefabs.Clear();prefabs.AddObject(AssetDatabase.LoadAssetAtPath<NetworkObject>(D+"/OF_NetworkPlayer.prefab"));EditorUtility.SetDirty(prefabs);
            NetworkLocalization.Apply();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            File.WriteAllText("Docs/ASTRA_10_SETUP.txt","Network prefab and owned camera generated. Steam App ID: "+settings.steamAppId+". Tick rate: "+settings.tickRate+". Session capacity: 4 including host.\n");
        }
        public static void BuildDevelopment()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var content);if(!string.IsNullOrEmpty(content.Error))throw new InvalidOperationException(content.Error);
            var scenes=Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled);var paths=Array.ConvertAll(scenes,s=>s.path);Directory.CreateDirectory("Builds/ASTRA10");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=paths,locationPathName="Builds/ASTRA10/Oceanarium.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Docs/ASTRA_10_BUILD.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings+"\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Development build failed");
        }
    }
    public sealed class NetworkBuildGuard:IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report)
        {
            var settings=Resources.Load<OceanNetworkSettings>("OceanNetworkSettings");if(!settings||settings.steamAppId==0)throw new BuildFailedException("Configure OceanNetworkSettings Steam App ID.");
            if(settings.steamAppId==480&&!settings.allowDevelopmentAppId)throw new BuildFailedException("Spacewar requires explicit development opt-in.");
            if(settings.steamAppId==480&&(report.summary.options&BuildOptions.Development)==0)throw new BuildFailedException("Development App ID is excluded from release builds.");
            if(!Resources.Load<NetworkObject>("OF_NetworkPlayer"))throw new BuildFailedException("Generate the network prefab first.");
        }
    }
}
