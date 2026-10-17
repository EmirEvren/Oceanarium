using System;
using UnityEngine;
namespace OceanariumFishing
{
    public enum FishingState {Idle,Equipping,Aiming,Casting,BobberFlying,WaitingForBite,FishBiting,Hooked,Fighting,Reeling,CatchSuccess,FishEscaped,ReturningToIdle}
    // Pure per-player simulation. A future host can drive this from validated input commands.
    public sealed class FishingFight
    {
        public float Tension {get;private set;}=.25f;public float Progress {get;private set;}public float Stamina {get;private set;}=1;public float Direction {get;private set;}public int Combo {get;private set;}=1;
        public bool Won=>Progress>=1 && Stamina<=.08f;public bool Lost {get;private set;}
        public float DangerSeconds=>overload;public float Elapsed=>elapsed;public bool RecoveryReward {get;private set;}
        public FishingDifficulty Difficulty {get;}
        readonly FishSpecies fish;readonly FishingRodDefinition rod;readonly FishingTuning tuning;readonly float size,phase;
        float elapsed,overload,slack,clean,mistake;bool recovered;
        public FishingFight(FishSpecies fish,FishingRodDefinition rod,FishingTuning tuning,FishInstance specimen,bool perfect,FishingDifficulty difficulty=null)
        {this.fish=fish;this.rod=rod;this.tuning=tuning;Difficulty=difficulty??tuning.Difficulty();size=Mathf.Lerp(.8f,1.2f,specimen.quality);phase=(specimen.seed%1000)*.1f;if(perfect){Stamina=.9f;Combo=2;}}
        public void Step(float dt,bool reel,float direction)
        {
            if(Won || Lost)return;dt=Mathf.Clamp(dt,0,.1f);elapsed+=dt;RecoveryReward=false;
            float tempo=fish.behavior==FishBehavior.Darting?2.8f:fish.behavior==FishBehavior.Erratic?1.8f:.7f;
            Direction=Mathf.Sin(elapsed*tempo+phase);if(fish.behavior==FishBehavior.Erratic)Direction=Mathf.Clamp(Direction+Mathf.Sin(elapsed*4.1f)*.55f,-1,1);
            float alignment=1-Mathf.Clamp01(Mathf.Abs(direction+Direction)*.65f);
            float control=Mathf.Lerp(Difficulty.minimumControl,1,alignment);float surge=.65f+.35f*Mathf.Sin(elapsed*1.4f+phase);
            float force=fish.fightingStrength*size*(fish.behavior==FishBehavior.Heavy?1.2f:1)*(.4f+Stamina*.6f)*Difficulty.pullMultiplier/Mathf.Max(.2f,rod.strength);
            Tension=Mathf.Clamp01(Tension+dt*(reel?tuning.tensionRise*Difficulty.tensionMultiplier*force*(.7f+surge)*(1.35f-control*.5f)/Mathf.Max(.2f,rod.lineStrength):-tuning.tensionRecovery*Difficulty.recoveryMultiplier));
            if(Tension>.72f)recovered=true;
            if(recovered && Tension<.48f && Tension>.12f){Combo=Mathf.Min(tuning.maxCombo,Combo+1);recovered=false;RecoveryReward=true;}
            if(reel && Tension<.88f && control>.48f)
            {
                clean+=dt;mistake=0;float bonus=1+(Combo-1)*.04f;
                Stamina=Mathf.Max(0,Stamina-dt*tuning.staminaDrain*Difficulty.staminaMultiplier*control*bonus/Mathf.Max(.3f,fish.stamina));
                float rate=Difficulty.useRarityDurations?1/Mathf.Max(1,Difficulty.fightSeconds[Mathf.Clamp((int)fish.rarity,0,Difficulty.fightSeconds.Length-1)]):tuning.progressRate;
                Progress=Mathf.Clamp01(Progress+dt*rate*Difficulty.progressMultiplier*rod.reelSpeed*control*bonus*(1.25f-Stamina*.45f)/size);
                if(clean>=tuning.comboSeconds){clean=0;Combo=Mathf.Min(tuning.maxCombo,Combo+1);}
            }
            else
            {
                mistake+=dt;if(mistake>Difficulty.comboMistakeGrace)clean=Mathf.Max(0,clean-dt*.4f);
                if(!reel)Progress=Mathf.Max(0,Progress-dt*tuning.escapeRate*Difficulty.escapeMultiplier*Stamina);
            }
            overload=Tension>=.98f?overload+dt:Mathf.Max(0,overload-dt*2);slack=Tension<.06f?slack+dt:0;
            if(overload>=Difficulty.breakGrace || slack>=Difficulty.slackGrace)Lost=true;
        }
    }
}
