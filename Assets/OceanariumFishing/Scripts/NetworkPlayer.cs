using System;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(-400)]
    public sealed class NetworkPlayer:NetworkBehaviour
    {
        public static NetworkPlayer Local{get;private set;}
        public LocalPlayerController Controller{get;private set;}public WorldPlayerRecord Record{get;private set;}
        public string DisplayName{get;private set;}public bool HostPlayer{get;private set;}
        FishingController inventory;CharacterAppearance appearance;CharacterController motor;
        Vector2 input;float heading,lastInputAt;bool sprint,ready;uint sequence,receivedSequence,ticks;Vector3 target;float targetHeading,targetSpeed;
        float receiveWindow;int messages;string lastAppearance;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public Vector2 TestDirection;
#endif
        void Awake()
        {
            Controller=GetComponent<LocalPlayerController>();Controller.NetworkDriven=true;Controller.enabled=false;motor=GetComponent<CharacterController>();
            inventory=GetComponent<FishingController>();appearance=GetComponentInChildren<CharacterAppearance>();appearance.loadLocal=false;
            foreach(var behaviour in GetComponents<MonoBehaviour>())if(behaviour is LocalPlayerController||behaviour is FishingController||behaviour is FishingHUD||behaviour is CollectionHUD||behaviour is PlayerInteractor||behaviour is TownEquipment||behaviour is TownQuestTracker||behaviour is FishingPresentation)behaviour.enabled=false;
            // CharacterAppearance is on the visual child and remains active for every observer.
            SetCamera(false);target=transform.position;
        }
        void SetCamera(bool local)
        {
            foreach(var camera in GetComponentsInChildren<Camera>(true))camera.enabled=local;
            foreach(var listener in GetComponentsInChildren<AudioListener>(true))listener.enabled=local;
            foreach(var camera in GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>(true))camera.enabled=local;
            if(Controller.cameraTarget)Controller.cameraTarget.gameObject.SetActive(local);
        }
        public void Prepare(WorldPlayerRecord record){Record=record;DisplayName=record.displayName;appearance.Apply(record.appearance);if(record.location!=null&&float.IsFinite(record.location.heading))Controller.visual.rotation=Quaternion.Euler(0,record.location.heading,0);}
        public override void OnStartNetwork(){base.OnStartNetwork();TimeManager.OnTick+=Tick;}
        public override void OnStopNetwork(){TimeManager.OnTick-=Tick;if(Local==this)Local=null;base.OnStopNetwork();}
        public override void OnStartServer(){base.OnStartServer();motor.enabled=true;lastInputAt=Time.unscaledTime;target=transform.position;}
        public override void OnStartClient(){base.OnStartClient();motor.enabled=IsServerInitialized||IsOwner;SetCamera(false);}
        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);if(Record==null)return;
            TargetAppearance(connection,JsonUtility.ToJson(Record.appearance),Record.displayName,Record.identity==WorldPersistence.Active.ownerIdentity);
            if(connection==Owner)TargetInitialize(connection,JsonUtility.ToJson(Record),JsonUtility.ToJson(WorldPersistence.Active.shared),WorldPersistence.Active.worldId,WorldPersistence.Active.name);
        }
        [TargetRpc] void TargetAppearance(NetworkConnection connection,string json,string displayName,bool host)
        {var data=JsonUtility.FromJson<AppearanceData>(json);data.Validate();appearance.Apply(data);DisplayName=NetworkNames.Clean(displayName);HostPlayer=host;}
        [TargetRpc] void TargetInitialize(NetworkConnection connection,string json,string shared,string worldId,string worldName)
        {
            if(!IsOwner)return;var record=JsonUtility.FromJson<WorldPlayerRecord>(json);var world=JsonUtility.FromJson<SharedWorldData>(shared);
            if(!IsServerInitialized)WorldPersistence.Receive(record,world,worldId,worldName);
            inventory.RestoreCollection(IsServerInitialized?WorldPersistence.Active.CollectionFor(Record):WorldPersistence.LoadCollection());
            inventory.enabled=true;Controller.enabled=true;Controller.InputEnabled=true;GetComponent<InteractionActor>().enabled=true;GetComponent<InteractionActor>().RestoreRod(record.starterRod);
            foreach(var behaviour in GetComponents<MonoBehaviour>())
                if(behaviour is FishingHUD||behaviour is CollectionHUD||behaviour is FishingPresentation)behaviour.enabled=true;
            // Shared gameplay transactions remain host-only until their action RPC integrations in later milestones.
            GetComponent<PlayerInteractor>().enabled=IsServerInitialized;appearance.Apply(record.appearance);
            Local=this;ready=true;SetCamera(true);NetworkSession.Current.OwnerReady();
            if(record.location!=null&&float.IsFinite(record.location.heading))Controller.SetViewAngles(record.location.heading,18);
            if(!record.appearanceChosen)ChooseInitialAppearance(JsonUtility.ToJson(AppearanceStore.Load(AppearanceStore.LegacyPath)));
        }
        [ServerRpc] void ChooseInitialAppearance(string json)
        {
            if(Record==null||Record.appearanceChosen||json==null||json.Length>4096)return;
            try{var data=JsonUtility.FromJson<AppearanceData>(json);if(data==null)return;data.Validate();Record.appearance=data;Record.appearanceChosen=true;if(WorldPersistence.Save()){VisibleAppearance(JsonUtility.ToJson(data));TargetAppearance(Owner,JsonUtility.ToJson(data),Record.displayName,Record.identity==WorldPersistence.Active.ownerIdentity);}}
            catch(ArgumentException){NetworkSession.Current.Error("authentication");}
        }
        void Tick()
        {
            float dt=(float)TimeManager.TickDelta;ticks++;
            if(IsOwner&&ready)
            {
                var command=Controller.ReadNetworkInput();sequence++;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if(NetworkDevelopmentProbe.Enabled)command.direction=TestDirection;
#endif
                if(IsServerInitialized){input=command.direction;heading=command.heading;sprint=command.sprint;lastInputAt=Time.unscaledTime;receivedSequence=sequence;}
                else{SubmitInput(command.direction,command.heading,command.sprint,sequence);Controller.SimulateNetwork(command.direction,command.heading,command.sprint,dt);}
            }
            if(!IsServerInitialized)return;
            if(Time.unscaledTime-lastInputAt>.25f)input=Vector2.zero;
            Controller.SimulateNetwork(input,heading,sprint,dt);
            if(transform.position.y< -1||Mathf.Abs(transform.position.x)>160||Mathf.Abs(transform.position.z)>160)ServerTeleport(NetworkSpawn.Resolve(null,NetworkSession.Current.Island,WorldPersistence.Active.shared.unlockedIslands,OwnerId%4));
            if(ticks%2==0)Pose(transform.position,Controller.visual.eulerAngles.y,Controller.Speed,receivedSequence);
            if(ticks%30==0)
            {
                var clock=WorldTimeService.Current?.Simulation;if(clock!=null)Clock(JsonUtility.ToJson(clock.State));
                if(IsOwner&&ready)
                {
                    var current=appearance.Data;if(current!=null){var json=JsonUtility.ToJson(current);if(json!=lastAppearance){lastAppearance=json;Record.appearance=current.Copy();VisibleAppearance(json);}}
                }
            }
        }
        [ServerRpc] void SubmitInput(Vector2 direction,float yaw,bool run,uint seq,Channel channel=Channel.Unreliable)
        {
            if(Time.unscaledTime-receiveWindow>1){receiveWindow=Time.unscaledTime;messages=0;}
            if(++messages>90||seq<=receivedSequence||!float.IsFinite(direction.x)||!float.IsFinite(direction.y)||!float.IsFinite(yaw))return;
            input=Vector2.ClampMagnitude(direction,1);heading=Mathf.Repeat(yaw,360);sprint=run;receivedSequence=seq;lastInputAt=Time.unscaledTime;
        }
        [ObserversRpc] void Pose(Vector3 position,float yaw,float speed,uint acknowledged,Channel channel=Channel.Unreliable)
        {
            if(IsServerInitialized)return;target=position;targetHeading=yaw;targetSpeed=speed;
            if(IsOwner&&ready){float distance=Vector3.Distance(transform.position,position);if(distance>2)Controller.ApplyNetworkPosition(position);else if(distance>.15f)Controller.ApplyNetworkPosition(Vector3.Lerp(transform.position,position,.35f));}
        }
        [ObserversRpc(BufferLast=true)] void VisibleAppearance(string json){appearance.Apply(JsonUtility.FromJson<AppearanceData>(json));}
        [ObserversRpc] void Clock(string json)
        {if(IsServerInitialized||!IsOwner||!ready)return;var state=JsonUtility.FromJson<WorldState>(json);WorldPersistence.Active.shared.clock=state;WorldTimeService.Current?.ApplyWorld(state,false);}
        [ObserversRpc] void Teleported(Vector3 position){if(IsServerInitialized)return;target=position;Controller.ApplyNetworkPosition(position);}
        public void ServerTeleport(Vector3 position){if(!IsServerInitialized)return;Controller.ApplyNetworkPosition(position);input=Vector2.zero;Teleported(position);}
        void Update()
        {
            if(IsServerInitialized||IsOwner)return;transform.position=Vector3.Lerp(transform.position,target,1-Mathf.Exp(-14*Time.deltaTime));Controller.visual.rotation=Quaternion.Slerp(Controller.visual.rotation,Quaternion.Euler(0,targetHeading,0),1-Mathf.Exp(-14*Time.deltaTime));Controller.SetNetworkSpeed(targetSpeed);
        }
        public void Capture()
        {
            if(!IsServerInitialized||Record==null||WorldPersistence.Active==null)return;
            Record.location=new(){island=gameObject.scene.name,position=transform.position,heading=Controller.visual.eulerAngles.y,valid=true,context="walk"};
            if(IsOwner&&ready){Record.starterRod=GetComponent<InteractionActor>().HasStarterRod;Record.appearance=appearance.Data.Copy();WorldTimeService.Current?.Stamp(inventory.Collection);WorldPersistence.Active.StoreCollection(Record,inventory.Collection,true);}
        }
        public override void OnStopServer(){Capture();if(WorldPersistence.Active!=null&&!WorldPersistence.ReadOnly)WorldPersistence.Save();base.OnStopServer();}
        void OnDestroy(){if(Local==this)Local=null;}
    }
}
