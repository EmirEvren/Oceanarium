#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    // Compiled out of release players. Exercises real FishNet packets over loopback, never Steam proof.
    public sealed class NetworkDevelopmentProbe:MonoBehaviour
    {
        [Serializable] sealed class Sample
        {
            public string status,role,identity,world,hat,weather,save;public int players,owners,cameras,listeners,fish;public long minute;public float elapsed;public Vector3 position;public string[] avatars;
        }
        float age,printAge,connectedAge,quitAt=150;bool exiting;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("-of-probe"))return;
            var go=new GameObject("Development network probe");DontDestroyOnLoad(go);var probe=go.AddComponent<NetworkDevelopmentProbe>();Application.targetFrameRate=30;
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-of-seconds"&&float.TryParse(args[i+1],out var seconds))probe.quitAt=seconds;
        }
        public static bool Enabled=>Environment.GetCommandLineArgs().Contains("-of-probe");
        public static void Seed(WorldPlayerRecord record,int index)
        {
            if(!Enabled||record.collection.town.flags.Contains("network-fixture"))return;
            record.collection.town.flags.Add("network-fixture");record.appearance.hat=index%2==0?"straw_hat":"beanie";record.appearance.hairColor=index%2==0?"ginger":"black";record.appearanceChosen=true;record.starterRod=true;
            record.collection.town.purchases.Add(new(){product="coastal_rod",quantity=1});
            var prefab=Resources.Load<GameObject>("OF_NetworkPlayer");var species=prefab.GetComponent<CollectionHUD>().species[0];var fish=FishInstance.Roll(species,index+741,"home.pier");fish.ownerId=record.collection.ownerId;record.collection.Resolve(fish,true);
        }
        void Update()
        {
            age+=Time.unscaledDeltaTime;printAge+=Time.unscaledDeltaTime;var hub=NetworkSession.Current;if(!hub)return;
            if(NetworkPlayer.Local){connectedAge+=Time.unscaledDeltaTime;NetworkPlayer.Local.TestDirection=connectedAge>3&&connectedAge<5?Vector2.up:Vector2.zero;}
            if(printAge>2)
            {
                printAge=0;var local=NetworkPlayer.Local;var simulation=WorldTimeService.Current?.Simulation;
                Debug.Log("OF_PROBE "+JsonUtility.ToJson(new Sample{elapsed=age,status=hub.Status,role=hub.IsHost?"host":"client",identity=hub.DevelopmentIdentity,world=WorldPersistence.Active?.worldId,players=hub.PlayerCount,owners=FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Count(p=>p.IsOwner),cameras=Camera.allCameras.Length,listeners=FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled),position=local?local.transform.position:Vector3.zero,hat=local?.GetComponentInChildren<CharacterAppearance>().Data?.hat,fish=GamePlayers.Inventory?.Collection.kept.Count??0,minute=simulation?.State.minute??0,weather=simulation?.Current?.id,save=WorldPersistence.LastSave,avatars=FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Select(p=>p.DisplayName+":"+p.GetComponentInChildren<CharacterAppearance>().Data?.hat+":"+p.transform.position.ToString("F2")).ToArray()}));
            }
            if(!exiting&&age>quitAt){exiting=true;hub.Leave();}if(exiting&&!hub.Online&&!hub.Busy)Application.Quit();
        }
    }
}
#endif
