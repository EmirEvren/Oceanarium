using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(200)]
    public sealed class TownRoutine:MonoBehaviour
    {
        TownResident npc;TownNavigation navigation;CharacterAppearance appearance;List<Vector3> route=new();int cursor,phase=-1;float nextPlan,stuck,walk;Vector3 previous;Renderer[] visuals;Collider body;bool visible=true;
        void Start(){npc=GetComponent<TownResident>();navigation=npc.world.GetComponent<TownNavigation>();appearance=GetComponent<CharacterAppearance>();body=GetComponent<Collider>();previous=transform.position;}
        void Update()
        {
            if(!npc.world.Ready||!navigation.Ready)return;var clock=npc.world.Clock;bool available=npc.definition.Available(clock);
            if(available!=visible){visuals??=GetComponentsInChildren<Renderer>();foreach(var r in visuals)r.enabled=available;body.enabled=available;visible=available;}if(!available||npc.world.UI.Resident==npc)return;
            var d=npc.definition;int state=clock.Hour<d.workStart||clock.Hour>=d.workEnd?0:clock.Hour>=d.breakStart&&clock.Hour<d.breakEnd?2:1;
            if(state==2&&WorldTimeService.Current?.Simulation.Current?.rain>.4f)state=0;
            if(Time.time>=nextPlan&&(phase!=state||stuck>3)){phase=state;route=navigation.Route(transform.position,state==0?d.home:state==2?d.social:d.work);cursor=0;nextPlan=Time.time+3;stuck=0;}
            if(cursor>=route.Count)return;var target=route[cursor];var delta=target-transform.position;if(delta.magnitude<.12f){cursor++;return;}
            bool obstructed=false;var player=npc.world.Player;if((player.transform.position-transform.position).sqrMagnitude<.6f)obstructed=true;
            foreach(var other in npc.world.Actors)if(other!=npc&&string.CompareOrdinal(npc.definition.id,other.definition.id)>0&&(other.transform.position-transform.position).sqrMagnitude<.65f&&Vector3.Dot(other.transform.position-transform.position,delta)>0){obstructed=true;break;}
            if(!obstructed){transform.position=Vector3.MoveTowards(transform.position,target,Time.deltaTime*1.1f);delta.y=0;if(delta.sqrMagnitude>.001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*6);stuck=0;}else stuck+=Time.deltaTime;
        }
        void LateUpdate(){if(appearance?.Bones==null)return;float speed=(transform.position-previous).magnitude/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;walk+=Time.deltaTime*7;float swing=Mathf.Sin(walk)*Mathf.Clamp01(speed)*22;appearance.Bones[12].localRotation=Quaternion.Euler(swing,0,0);appearance.Bones[15].localRotation=Quaternion.Euler(-swing,0,0);}
    }
}
