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
        readonly FishSpecies fish;readonly FishingRodDefinition rod;readonly FishingTuning tuning;readonly float size,phase;
        float elapsed,overload,slack,clean;bool recovered;
        public FishingFight(FishSpecies fish,FishingRodDefinition rod,FishingTuning tuning,FishInstance specimen,bool perfect)
        {this.fish=fish;this.rod=rod;this.tuning=tuning;size=Mathf.Lerp(.8f,1.2f,specimen.quality);phase=(specimen.seed%1000)*.1f;if(perfect){Stamina=.88f;Combo=2;}}
        public void Step(float dt,bool reel,float direction)
        {
            if(Won || Lost)return;dt=Mathf.Clamp(dt,0,.1f);elapsed+=dt;
            float tempo=fish.behavior==FishBehavior.Darting?2.8f:fish.behavior==FishBehavior.Erratic?1.8f:.7f;
            Direction=Mathf.Sin(elapsed*tempo+phase);if(fish.behavior==FishBehavior.Erratic)Direction=Mathf.Clamp(Direction+Mathf.Sin(elapsed*4.1f)*.55f,-1,1);
            float control=1-Mathf.Clamp01(Mathf.Abs(direction+Direction)*.65f);float surge=.65f+.35f*Mathf.Sin(elapsed*1.4f+phase);
            float force=fish.fightingStrength*size*(fish.behavior==FishBehavior.Heavy?1.2f:1)*(.4f+Stamina*.6f)/Mathf.Max(.2f,rod.strength);
            Tension=Mathf.Clamp01(Tension+dt*(reel?tuning.tensionRise*force*(.7f+surge)*(1.35f-control*.5f)/Mathf.Max(.2f,rod.lineStrength):-tuning.tensionRecovery));
            if(Tension>.72f)recovered=true;
            if(recovered && Tension<.48f && Tension>.12f){Combo=Mathf.Min(tuning.maxCombo,Combo+1);recovered=false;}
            if(reel && Tension>.13f && Tension<.83f && control>.48f)
            {
                clean+=dt;float bonus=1+(Combo-1)*.06f;Stamina=Mathf.Max(0,Stamina-dt*tuning.staminaDrain*control*bonus/Mathf.Max(.3f,fish.stamina));
                Progress=Mathf.Clamp01(Progress+dt*tuning.progressRate*rod.reelSpeed*control*bonus*(1.25f-Stamina*.45f)/size);
                if(clean>=tuning.comboSeconds){clean=0;Combo=Mathf.Min(tuning.maxCombo,Combo+1);}
            }
            else{clean=0;if(!reel)Progress=Mathf.Max(0,Progress-dt*tuning.escapeRate*Stamina);}
            overload=Tension>=.98f?overload+dt:Mathf.Max(0,overload-dt*2);slack=Tension<.06f?slack+dt:0;
            if(overload>tuning.breakGrace || slack>tuning.slackGrace)Lost=true;
        }
    }

}
