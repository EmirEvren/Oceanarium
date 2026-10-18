using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace OceanariumFishing
{
    public sealed class FishingController:MonoBehaviour
    {
        public FishingRodDefinition rod;public FishingTuning tuning;public FishingPresentation presentation;
        public FishingState State {get;private set;}=FishingState.Idle;public FishingFight Fight {get;private set;}public FishInstance Catch {get;private set;}public FishSpecies Species {get;private set;}
        public float Charge {get;private set;}public float StateTime=>age;public bool Active=>State!=FishingState.Idle;public bool Equipped {get;private set;}public bool NewRecord {get;private set;}
        public string Feedback {get;private set;}="ready";public FishCollection Collection {get;private set;}
        public bool NewDiscovery {get;private set;} public FishingDifficulty Difficulty=>tuning.Difficulty(Collection.difficulty); public bool TutorialVisible=>!Collection.tutorialComplete;
        public float Steering=>steer;public bool OptimalCast {get;private set;}
        public bool AimValid=>water && water.Valid(AimPoint());
        LocalPlayerController controller;InteractionActor actor;FishingWater water;float age,wait,steer,rippleTimer,cueTimer;int seed,lastCombo;Vector3 start,target;FishingZone zone;
        public event Action<FishingState> StateChanged;
        public bool ExternalInput {get;set;}
        void Awake(){controller=GetComponent<LocalPlayerController>();actor=GetComponent<InteractionActor>();Collection=FishCollectionStore.Load();presentation.owner=this;}
        void Start(){water=FindFirstObjectByType<FishingWater>();}
        public bool Transition(FishingState next)
        {
            bool valid=next==FishingState.ReturningToIdle && State!=FishingState.Idle || next==FishingState.FishEscaped && State>=FishingState.Casting && State<=FishingState.Reeling;
            valid|=(State,next) switch
            {
                (FishingState.Idle,FishingState.Equipping)=>true,(FishingState.Equipping,FishingState.Idle)=>true,(FishingState.Idle,FishingState.Aiming)=>Equipped,
                (FishingState.Aiming,FishingState.Casting)=>true,(FishingState.Casting,FishingState.BobberFlying)=>true,(FishingState.BobberFlying,FishingState.WaitingForBite)=>true,
                (FishingState.WaitingForBite,FishingState.FishBiting)=>true,(FishingState.FishBiting,FishingState.Hooked)=>true,(FishingState.Hooked,FishingState.Fighting)=>true,
                (FishingState.Fighting,FishingState.Reeling)=>true,(FishingState.Reeling,FishingState.Fighting)=>true,(FishingState.Fighting,FishingState.CatchSuccess)=>Fight!=null&&Fight.Won,(FishingState.Reeling,FishingState.CatchSuccess)=>Fight!=null&&Fight.Won,
                (FishingState.ReturningToIdle,FishingState.Idle)=>true,_=>false
            };
            if(!valid)return false;State=next;age=0;controller.FishingLocked=Active;StateChanged?.Invoke(State);return true;
        }
        public bool Equip()
        {if(Active || Equipped || !actor.HasStarterRod)return false;var avatar=GetComponentInChildren<CharacterAppearance>();if(avatar?.Bones==null)return false;presentation.Equip(avatar.Bones[11],rod.visualPrefab);Equipped=true;return Transition(FishingState.Equipping);}
        public bool BeginAim()
        {if(!Equipped || Active || !controller.InputEnabled)return false;Charge=0;Feedback="charge";return Transition(FishingState.Aiming);}
        public Vector3 AimPoint()
        {var direction=Vector3.ProjectOnPlane(controller.view.transform.forward,Vector3.up).normalized;return transform.position+direction*Mathf.Lerp(tuning.minCast,rod.castDistance,Charge);}
        public bool Cast()
        {
            if(State!=FishingState.Aiming)return false;target=AimPoint();if(!water || !water.Valid(target)){Feedback="invalid";Cancel();return false;}
            OptimalCast=Charge>=.72f && Charge<=.86f;
            seed=UnityEngine.Random.Range(1,int.MaxValue);var random=new System.Random(seed);
            if(!OptimalCast){var offset=controller.view.transform.right*((float)random.NextDouble()-.5f)*(1-rod.accuracy)*3;var candidate=target+offset;if(water.Valid(candidate))target=candidate;}
            zone=FishingZone.At(target);if(!zone){Feedback="empty";Cancel();return false;}Species=zone.Select(seed,12,"clear","none",SceneManager.GetActiveScene().name);if(!Species){Feedback="empty";Cancel();return false;}
            Catch=FishInstance.Roll(Species,seed,zone.zoneId);Catch.ownerId=Collection.ownerId;Catch.islandId=SceneManager.GetActiveScene().name;Catch.captureMethod="rod:"+rod.id;target.y=water.Height(target,Time.time);start=presentation.Tip.position;presentation.BeginCast(start);transform.GetComponent<LocalPlayerController>().visual.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(target-transform.position,Vector3.up));Feedback="cast";return Transition(FishingState.Casting);
        }
        public bool Hook()
        {
            if(State!=FishingState.FishBiting)return false;bool perfect=age<=Difficulty.perfectWindow;Feedback=perfect?"perfect":age<=Difficulty.goodWindow?"good":"late";Fight=new FishingFight(Species,rod,tuning,Catch,perfect,Difficulty);steer=0;lastCombo=Fight.Combo;presentation.Cue(perfect?4:2);return Transition(FishingState.Hooked);
        }
        public void Cancel()
        {if(State==FishingState.CatchSuccess)return;if(Active){presentation.ClearCast();Transition(FishingState.ReturningToIdle);}}
        public bool ResolveCatch(bool keep)
        {
            if(State!=FishingState.CatchSuccess || Catch==null)return false;
            // Stage a complete copy so failed writes neither grant nor consume a reward.
            var next=Collection.Copy();if(!next.Resolve(Catch,keep))return false;next.tutorialComplete=true;if(!FishCollectionStore.Save(next)){Feedback="save_error";return false;}
            Collection=next;Feedback=keep?"kept":"released";presentation.Cue(keep?4:1);presentation.Ripple(target,true);presentation.ClearCast();Transition(FishingState.ReturningToIdle);return true;
        }
        public bool SetDifficulty(string id)
        {if(Active)return false;var next=Collection.Copy();next.difficulty=tuning.Difficulty(id).id;if(!FishCollectionStore.Save(next))return false;Collection=next;return true;}
        public bool CommitAquarium(FishCollection staged)
        {if(staged==null||staged.ownerId!=Collection.ownerId||!FishCollectionStore.Save(staged))return false;Collection=staged;return true;}
        void Update()
        {
            if(!rod || !tuning || !presentation || ExternalInput)return;if(actor.HasStarterRod && !Equipped)Equip();
            var mouse=Mouse.current;var keys=Keyboard.current;
            if(!controller.InputEnabled){if(Active && State!=FishingState.CatchSuccess)Cancel();Tick(Time.deltaTime,false,false,false,0);return;}
            if(keys?.qKey.wasPressedThisFrame==true){Cancel();return;}
            bool down=mouse?.leftButton.wasPressedThisFrame==true,held=mouse?.leftButton.isPressed==true,up=mouse?.leftButton.wasReleasedThisFrame==true;
            if(State==FishingState.Idle && down)BeginAim();else if(State==FishingState.Aiming && up)Cast();else if(State==FishingState.FishBiting && down)Hook();
            steer=Mathf.Clamp(steer+(mouse?.delta.ReadValue().x??0)*.015f,-1,1);Tick(Time.deltaTime,held,down,up,steer);
        }
        public void Tick(float dt,bool reel,bool down,bool up,float direction)
        {
            age+=dt;rippleTimer+=dt;cueTimer+=dt;
            switch(State)
            {
                case FishingState.Equipping:if(age>.35f)Transition(FishingState.Idle);break;
                case FishingState.Aiming:Charge=Mathf.PingPong(age/tuning.chargeDuration,1);break;
                case FishingState.Casting:if(presentation.Bobber)presentation.Bobber.position=presentation.Tip.position;if(age>.18f){start=presentation.Tip.position;Transition(FishingState.BobberFlying);}break;
                case FishingState.BobberFlying:
                    float t=Mathf.Clamp01(age/tuning.flightDuration);if(presentation.Bobber)presentation.Bobber.position=Vector3.Lerp(start,target,t)+Vector3.up*(4*t*(1-t)*2.2f);
                    if(t>=1){wait=(Mathf.Lerp(tuning.minWait,tuning.maxWait,(seed%997)/997f)+(int)Species.rarity*.3f)*(OptimalCast?.85f:1);presentation.Ripple(target,true);presentation.Cue(1);Feedback="waiting";Transition(FishingState.WaitingForBite);}break;
                case FishingState.WaitingForBite:FloatBobber(false);if(age>=wait){presentation.Cue(3);presentation.Ripple(target,true);Feedback="bite";Transition(FishingState.FishBiting);}break;
                case FishingState.FishBiting:FloatBobber(true);if(age>Difficulty.hookWindow)Escape("missed");break;
                case FishingState.Hooked:if(age>.25f)Transition(FishingState.Fighting);break;
                case FishingState.Fighting:case FishingState.Reeling:
                    if(reel && State==FishingState.Fighting)Transition(FishingState.Reeling);else if(!reel && State==FishingState.Reeling)Transition(FishingState.Fighting);
                    Fight.Step(dt,reel,direction);FloatBobber(true);
                    if(Fight.RecoveryReward){Feedback="recovered";presentation.Cue(4);}if(Fight.Combo>lastCombo){lastCombo=Fight.Combo;Feedback="great";presentation.Cue(4);}
                    if(cueTimer>(Fight.Tension>.85f?.8f:1.3f) && reel){cueTimer=0;presentation.Cue(Fight.Tension>.85f?2:0);}
                    if(Fight.Lost)Escape("escaped");else if(Fight.Won){NewRecord=Collection.IsRecord(Catch);NewDiscovery=!Collection.Discovered(Catch.speciesId);var updated=Collection.Copy();updated.RecordCatch(Catch);bool saved=FishCollectionStore.Save(updated);if(saved)Collection=updated;Transition(FishingState.CatchSuccess);Feedback=saved?"caught":"save_error";presentation.ShowFish(Species);presentation.Ripple(target,true);}break;
                case FishingState.FishEscaped:if(age>1.5f){presentation.ClearCast();Transition(FishingState.ReturningToIdle);}break;
                case FishingState.ReturningToIdle:if(age>.4f){Fight=null;Catch=null;Transition(FishingState.Idle);}break;
            }
        }
        void FloatBobber(bool pull)
        {if(!presentation.Bobber)return;var p=target;if(Fight!=null){p=Vector3.Lerp(target,transform.position+controller.visual.forward*1.4f,Fight.Progress*.85f);p+=controller.visual.right*Fight.Direction*.5f;}p.y=water.Height(p,Time.time)+(pull?-.045f+Mathf.Sin(Time.time*14)*.035f:.025f);presentation.Bobber.position=p;if(rippleTimer>.75f){rippleTimer=0;presentation.Ripple(p);}}
        void Escape(string key){Feedback=key;presentation.Cue(0);Transition(FishingState.FishEscaped);}
        void OnDisable(){if(controller)controller.FishingLocked=false;if(presentation)presentation.ClearCast();State=FishingState.Idle;Fight=null;Catch=null;}
    }
}
