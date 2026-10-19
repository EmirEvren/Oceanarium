using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Managing.Scened;
using FishNet.Managing.Server;
using FishNet.Managing.Timing;
using FishNet.Managing.Transporting;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using Steamworks;
using UnityEngine;
using UScenes=UnityEngine.SceneManagement.SceneManager;
namespace OceanariumFishing
{
    public static class GamePlayers
    {
        public static LocalPlayerController Local=>NetworkSession.Current?.Online==true?NetworkPlayer.Local?.Controller:UnityEngine.Object.FindFirstObjectByType<LocalPlayerController>();
        public static FishingController Inventory=>Local?Local.GetComponent<FishingController>():null;
    }
    public sealed class NetworkSession:MonoBehaviour
    {
        public static NetworkSession Current{get;private set;}
        public NetworkManager Manager{get;private set;}public SteamPlatform Steam{get;private set;}public OceanAuthenticator Auth{get;private set;}
        public bool Online{get;private set;}public bool Busy{get;private set;}public bool IsHost=>Manager&&Manager.IsServerStarted;
        public bool DevelopmentTransport{get;private set;}public string DevelopmentIdentity{get;private set;}
        public string Status{get;private set;}="idle";
        public string LastDisconnect{get;private set;}="";
        public string Island{get;private set;}="OF_HomeIsland";
        public int PlayerCount=>IsHost?Auth.Identities.Count:FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Length;
        public readonly Dictionary<int,NetworkPlayer> Players=new();
        NetworkObject playerPrefab;float connectionDeadline,saveAge;bool leaving,sceneChanging;string sceneDestination;
        public static void Ensure(){if(!Current)new GameObject("Oceanarium multiplayer session").AddComponent<NetworkSession>();}
        void Awake(){if(Current){Destroy(gameObject);return;}Current=this;DontDestroyOnLoad(gameObject);Steam=gameObject.AddComponent<SteamPlatform>();gameObject.AddComponent<MultiplayerHUD>();}
        IEnumerator Start()
        {
            yield return null;var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="+connect_lobby"&&ulong.TryParse(args[i+1],out var lobby)){while(GameSession.Current?.Languages.Ready!=true||GameSession.Current.Loading||UScenes.GetActiveScene().name!="OF_MainMenu")yield return null;Steam.Join(lobby);yield break;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentIdentity="dev-"+Guid.NewGuid().ToString("N");
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-of-profile"&&Guid.TryParseExact(args[i+1],"N",out _))DevelopmentIdentity="dev-"+args[i+1];
            if(args.Contains("-of-local-host")||args.Contains("-of-local-client"))
            {while(GameSession.Current?.Languages.Ready!=true||GameSession.Current.Loading||UScenes.GetActiveScene().name!="OF_MainMenu")yield return null;StartDevelopment(args.Contains("-of-local-host"));}
#endif
        }
        public void Error(string code){Status=code;Debug.Log("Oceanarium session: "+code);}
        public void BeginJoin(){Busy=true;Status="joining";connectionDeadline=Time.realtimeSinceStartup+45;}
        public bool OpenWorld(string id,string name,bool create,bool importLegacy,string identity,string root=null)
        {
            try
            {
                var world=create?WorldSlotStore.Create(name,identity,importLegacy,root):WorldSlotStore.Load(id,out _,root);
                if(world.ownerIdentity!=identity)
                {
                    // A host explicitly opening their own offline slot may bind it to their authenticated Steam account.
                    if(world.ownerIdentity!="local"){Error("world_owner");return false;}
                    var owner=world.Find("local");if(owner!=null)owner.identity=identity;world.ownerIdentity=identity;
                }
                WorldPersistence.Open(world,identity,root);return WorldPersistence.Save();
            }
            catch(Exception e)when(e is System.IO.IOException||e is ArgumentException||e is NotSupportedException||e is UnauthorizedAccessException){Error("save_invalid");return false;}
        }
        public void Solo(string id,string name,bool create,bool importLegacy)
        {
            if(Busy||Online)return;string identity="local";
            if(!create){try{identity=WorldSlotStore.Load(id,out _).ownerIdentity;}catch{Error("save_invalid");return;}}
            if(!OpenWorld(id,name,create,importLegacy,identity))return;Status="solo";GameSession.Current.Travel(create?Destination.CharacterCreator:ResumeDestination(WorldPersistence.Active));
        }
        public static Destination ResumeDestination(WorldSlot world)
        {
            var location=world?.Find(world.ownerIdentity)?.location;
            return location?.valid==true&&location.island=="OF_TownIsland"&&world.shared.travel.repaired&&world.shared.unlockedIslands.Contains(location.island)?Destination.TownIsland:Destination.HomeIsland;
        }
        string ResumeIsland()=>GameSession.Current.catalog.Resolve(ResumeDestination(WorldPersistence.Active));
        public void Host(string id,string name,bool create,bool importLegacy,bool friendsOnly)
        {
            if(Busy||Online){Error("already_connected");return;}if(!Steam.Initialize())return;
            if(!OpenWorld(id,name,create,importLegacy,Steam.Identity))return;Busy=true;Status="hosting";Steam.Host(friendsOnly);
        }
        bool CreateNetwork(bool development)
        {
            if(Manager){Error("already_connected");return false;}
            playerPrefab=Resources.Load<NetworkObject>("OF_NetworkPlayer");var prefabs=Resources.Load<SinglePrefabObjects>("OF_NetworkPrefabs");var settings=Resources.Load<OceanNetworkSettings>("OceanNetworkSettings");
            if(!playerPrefab||!prefabs||!settings){Error("prefab_missing");return false;}
            DevelopmentTransport=development;Online=true;Busy=true;connectionDeadline=Time.realtimeSinceStartup+45;
            var go=new GameObject("Oceanarium FishNet");go.SetActive(false);DontDestroyOnLoad(go);
            var transportManager=go.AddComponent<TransportManager>();
            if(development){var tug=go.AddComponent<Tugboat>();tug.SetPort(settings.localPort);tug.SetClientAddress("127.0.0.1");tug.SetServerBindAddress("127.0.0.1",IPAddressType.IPv4);transportManager.Transport=tug;}
            else{var fishy=go.AddComponent<FishySteamworks.FishySteamworks>();fishy.SetPeerToPeer(true);transportManager.Transport=fishy;}
            var time=go.AddComponent<TimeManager>();time.SetTickRate(settings.tickRate);var server=go.AddComponent<ServerManager>();Auth=go.AddComponent<OceanAuthenticator>();
            Manager=go.AddComponent<NetworkManager>();Manager.SpawnablePrefabs=prefabs;go.SetActive(true);server.SetStartOnHeadless(false);server.SetAuthenticator(Auth);transportManager.Transport.SetMaximumClients(8);
            Manager.ClientManager.OnClientConnectionState+=ClientState;Manager.ServerManager.OnRemoteConnectionState+=RemoteState;Manager.ServerManager.OnServerConnectionState+=ServerState;
            Manager.SceneManager.OnClientLoadedStartScenes+=Loaded;
            Manager.SceneManager.OnClientPresenceChangeEnd+=Presence;Manager.SceneManager.OnLoadEnd+=SceneLoaded;
            return true;
        }
        public void StartSteamHost(){if(!CreateNetwork(false)){Abort("prefab_missing");return;}if(!Manager.ServerManager.StartConnection()){Abort("connection_failed");return;}LoadIsland(ResumeIsland());if(!Manager.ClientManager.StartConnection(Steam.Identity))Abort("connection_failed");}
        public void StartSteamClient(string host){CaptureAll();WorldTimeService.Current?.Flush();WorldPersistence.Close();if(!CreateNetwork(false)){Abort("prefab_missing");return;}if(!Manager.ClientManager.StartConnection(host))Abort("connection_failed");}
        public void StartDevelopment(bool host)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Busy||Online)return;if(string.IsNullOrEmpty(DevelopmentIdentity))DevelopmentIdentity="dev-"+Guid.NewGuid().ToString("N");
            if(host)
            {
                // Test slots are isolated from the user's worlds and original saves.
                string root=System.IO.Path.Combine(Application.persistentDataPath,"DevelopmentWorlds");var worlds=WorldSlotStore.List(out _,root);var saved=worlds.Find(w=>w.ownerIdentity==DevelopmentIdentity);
                if(!OpenWorld(saved?.worldId,"Local network test",saved==null,false,DevelopmentIdentity,root))return;
            }
            else WorldPersistence.Close();
            if(!CreateNetwork(true)){Abort("prefab_missing");return;}
            if(host){if(!Manager.ServerManager.StartConnection()){Abort("connection_failed");return;}LoadIsland(ResumeIsland());}if(!Manager.ClientManager.StartConnection())Abort("connection_failed");
#endif
        }
        public void Travel(Destination destination)
        {
            if(destination==Destination.MainMenu){Leave();return;}
            if(!IsHost){Error("host_only");return;}if(sceneChanging)return;
            string scene=GameSession.Current.catalog.Resolve(destination);
            if(!WorldPersistence.Active.shared.unlockedIslands.Contains(scene)){Error("island_locked");return;}LoadIsland(scene);
        }
        void LoadIsland(string scene)
        {
            CaptureAll();WorldTimeService.Current?.Flush();sceneChanging=true;sceneDestination=scene;Island=scene;Status="loading";
            foreach(var p in Players.Values.ToArray())if(p)Manager.ServerManager.Despawn(p.NetworkObject);Players.Clear();
            Manager.SceneManager.LoadGlobalScenes(new SceneLoadData(scene){ReplaceScenes=ReplaceOption.All});
        }
        void SceneLoaded(SceneLoadEndEventArgs args){if(!IsHost)return;sceneChanging=false;foreach(var c in Manager.ServerManager.Clients.Values.Where(c=>c.IsAuthenticated))TrySpawn(c);}
        void Loaded(NetworkConnection conn,bool server){if(server)TrySpawn(conn);}
        void Presence(ClientPresenceChangeEventArgs args){if(args.Added&&args.Scene.name==Island)TrySpawn(args.Connection);}
        void TrySpawn(NetworkConnection connection)
        {
            if(!IsHost||sceneChanging||Players.ContainsKey(connection.ClientId)||!Auth.Identities.TryGetValue(connection.ClientId,out var identity))return;
            var scene=UScenes.GetSceneByName(Island);if(!scene.IsValid()||!scene.isLoaded)return;
            var name=DevelopmentTransport?"Local tester":NetworkNames.Clean(SteamFriends.GetFriendPersonaName(new CSteamID(ulong.Parse(identity))));
            var record=WorldPersistence.Active.GetOrCreate(identity,name);record.displayName=name;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(DevelopmentTransport)NetworkDevelopmentProbe.Seed(record,connection.ClientId);
#endif
            if(!WorldPersistence.Save()){connection.Disconnect(false);Error("save_failed");return;}
            Vector3 spawn=NetworkSpawn.Resolve(record.location,Island,WorldPersistence.Active.shared.unlockedIslands,connection.ClientId%4);
            var obj=Instantiate(playerPrefab,spawn,Quaternion.identity);UScenes.MoveGameObjectToScene(obj.gameObject,scene);var player=obj.GetComponent<NetworkPlayer>();player.Prepare(record);Players.Add(connection.ClientId,player);
            Manager.ServerManager.Spawn(obj,connection);Manager.SceneManager.AddOwnerToDefaultScene(obj);Status="connected";Busy=false;connectionDeadline=0;
        }
        public void OwnerReady(){Busy=false;connectionDeadline=0;Status="connected";Island=UScenes.GetActiveScene().name;}
        void ClientState(ClientConnectionStateArgs args){if(args.ConnectionState==LocalConnectionState.Stopped&&Online&&!leaving)Abort(Status=="full"||Status=="duplicate"||Status=="authentication"||Status=="host_left"?Status:"connection_lost");}
        void ServerState(ServerConnectionStateArgs args){if(args.ConnectionState==LocalConnectionState.Stopped&&Online&&!leaving)Abort("connection_lost");}
        void RemoteState(NetworkConnection conn,RemoteConnectionStateArgs args)
        {
            if(args.ConnectionState!=RemoteConnectionState.Stopped)return;
            if(Players.Remove(conn.ClientId,out var p)&&p){p.Capture();WorldPersistence.Save();var harbor=FindFirstObjectByType<BoatHarbor>();harbor?.Readiness.Disconnect(p.Record.collection.ownerId);}
        }
        void Update()
        {
            if(connectionDeadline>0&&Time.realtimeSinceStartup>connectionDeadline){Abort("timeout");return;}
            if(!IsHost)return;saveAge+=Time.unscaledDeltaTime;if(saveAge>15){saveAge=0;CaptureAll();WorldPersistence.Save();}
        }
        public void CaptureAll()
        {
            foreach(var player in Players.Values.ToArray())if(player)player.Capture();
            if(!Online&&WorldPersistence.Local!=null&&GamePlayers.Local)
            {var p=GamePlayers.Local;WorldPersistence.Local.location=new(){island=UScenes.GetActiveScene().name,position=p.transform.position,heading=p.visual.eulerAngles.y,valid=true,context="walk"};WorldPersistence.Local.starterRod=p.GetComponent<InteractionActor>().HasStarterRod;}
        }
        public void Abort(string reason){LastDisconnect=reason;Error(reason);if(!leaving)StartCoroutine(Stop(reason));}
        public void Leave(){if(!leaving)StartCoroutine(Stop("left"));}
        IEnumerator Stop(string reason)
        {
            leaving=true;Busy=true;CaptureAll();WorldTimeService.Current?.Flush();if(!WorldPersistence.ReadOnly&&WorldPersistence.Active!=null)WorldPersistence.Save();
            if(IsHost){Manager.ServerManager.Broadcast(new SessionNotice{code="host_left"});yield return new WaitForSecondsRealtime(.15f);}
            if(Manager){Manager.ClientManager.StopConnection();Manager.ServerManager.StopConnection(true);Auth.Clear();Destroy(Manager.gameObject);}Manager=null;Auth=null;
            Steam.Leave();Players.Clear();Online=false;DevelopmentTransport=false;sceneChanging=false;connectionDeadline=0;WorldPersistence.Close();
            if(WorldTimeService.Current)Destroy(WorldTimeService.Current.gameObject);Busy=false;leaving=false;Status=reason;GameSession.Current?.Travel(Destination.MainMenu);
        }
        void OnApplicationQuit(){CaptureAll();WorldTimeService.Current?.Flush();if(WorldPersistence.Active!=null&&!WorldPersistence.ReadOnly)WorldPersistence.Save();Steam.Leave();}
        void OnDestroy(){if(Current==this)Current=null;}
    }
    public static class NetworkSpawn
    {
        public static Vector3 Resolve(SavedLocation saved,string island,List<string> unlocked,int index)
        {
            if(saved!=null&&saved.valid&&saved.island==island&&unlocked.Contains(island)&&Valid(saved.position,out var position))return position;
            var entry=UnityEngine.Object.FindFirstObjectByType<SceneEntry>();var origin=entry&&entry.player?entry.player.transform.position:new Vector3(0,2,0);
            if(Valid(origin+Vector3.right*(index*.8f),out var safe))return safe;
            return origin; // Authored spawn is the final fallback; no client-supplied location is used.
        }
        public static bool Valid(Vector3 p,out Vector3 ground)
        {
            ground=p;if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)||Mathf.Abs(p.x)>160||Mathf.Abs(p.z)>160||p.y<-.5f||p.y>40)return false;
            if(!Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,5,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.65f)return false;
            ground=hit.point+Vector3.up*.05f;return !Physics.CheckCapsule(ground+Vector3.up*.35f,ground+Vector3.up*1.55f,.25f,~0,QueryTriggerInteraction.Ignore);
        }
    }
}
