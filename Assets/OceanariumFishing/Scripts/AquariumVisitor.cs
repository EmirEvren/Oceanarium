using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public enum VisitorState {Arriving,Entering,Approaching,Observing,Reacting,Reception,Donating,Leaving}
    [DefaultExecutionOrder(110)]
    public sealed class AquariumVisitor:MonoBehaviour
    {
        public VisitorState State {get;private set;}public string VisitId {get;private set;}public int Viewed {get;private set;}public float Interest {get;private set;}
        AquariumWorld world;CharacterAppearance avatar;AquariumTank exhibit;readonly Queue<Vector3> route=new();readonly HashSet<string> seen=new();
        float age,wait,phase,speed,stuck;Vector3 previous;int variant;bool routingOut;
        public void Initialize(AquariumWorld w,int style)
        {
            world=w;variant=style%6;VisitId=Guid.NewGuid().ToString("N");avatar=gameObject.AddComponent<CharacterAppearance>();avatar.loadLocal=false;avatar.surface=w.fishMaterial;
            var data=new AppearanceData{skin=new[]{"peach","caramel","umber","porcelain","tan","cocoa"}[variant],hair=new[]{"wavy_bob","short_crop","long_straight","curly","ponytail","short_crop"}[variant],hat=new[]{"straw_hat","none","bucket_hat","beanie","baseball_cap","none"}[variant],outfitColor=new[]{"coral","navy","ochre","lavender","cream","seafoam"}[variant],glasses=variant==2?"round_glasses":"none"};data.Validate();avatar.Apply(data);transform.localScale=Vector3.one*.9f;State=VisitorState.Arriving;route.Enqueue(world.entrance.position);previous=transform.position;
        }
        Vector3 Lane(float z)=>new(world.transform.position.x,world.transform.position.y,z);
        void Travel(Vector3 goal){route.Clear();if(world.Building){foreach(var p in world.Building.Navigation.Route(transform.position,goal))route.Enqueue(p);}else route.Enqueue(goal);}
        int navGeneration;Vector3 destination;
        void Set(VisitorState next){State=next;age=0;}
        void Choose()
        {
            exhibit=world.Tanks.Where(t=>world.Display(t).Any()&&!seen.Contains(t.id)).OrderByDescending(t=>world.Display(t).Sum(f=>world.Contribution(f))).FirstOrDefault();
            if(!exhibit||Viewed>=2){Travel(world.donation.position);Set(VisitorState.Reception);return;}
            Travel(exhibit.viewPoint.position);Set(VisitorState.Approaching);
        }
        void Leave(){Travel(world.entrance.position);route.Enqueue(world.arrival.position);routingOut=true;Set(VisitorState.Leaving);}
        void Update()
        {
            if(!world||!world.Ready){Destroy(gameObject);return;}if(!routingOut&&(world.Hour<world.economy.dayStart||world.Hour>=world.economy.dayEnd))Leave();float dt=Time.deltaTime;age+=dt;speed=0;
            if(route.Count>0)
            {
                if(world.Building&&navGeneration!=world.Building.Navigation.Generation){destination=route.Last();navGeneration=world.Building.Navigation.Generation;Travel(destination);if(route.Count==0){Leave();return;}}Vector3 goal=route.Peek(),delta=goal-transform.position;
                if(delta.magnitude<.12f){route.Dequeue();stuck=0;return;}
                Vector3 move=delta.normalized;float factor=1;
                foreach(var other in world.Visitors)if(other&&other!=this){Vector3 away=transform.position-other.transform.position;away.y=0;if(away.magnitude<.65f&&away.sqrMagnitude>.001f){move+=away.normalized*(.65f-away.magnitude)*2;factor=.65f;}}
                Vector3 fromPlayer=transform.position-world.Player.transform.position;fromPlayer.y=0;if(fromPlayer.magnitude<.65f){move+=fromPlayer.normalized*.8f;factor=.5f;}
                Vector3 step=move.normalized*(1.15f*factor*dt);if(step.magnitude>delta.magnitude)step=delta;
                if(world.Building&&!world.Building.Navigation.Walkable(transform.position+step)&&world.transform.InverseTransformPoint(transform.position).z> -4.5f)step=delta.normalized*Mathf.Min(delta.magnitude,1.15f*dt);transform.position+=step;Vector3 flat=Vector3.ProjectOnPlane(move,Vector3.up);if(flat.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(flat),dt*6);speed=factor;
                stuck=Vector3.Distance(previous,transform.position)<.002f?stuck+dt:0;previous=transform.position;
                if((stuck>6||age>35)&&!routingOut)Leave();if(routingOut&&age>60&&Vector3.Distance(transform.position,world.entrance.position)<1)route.Clear();return;
            }
            switch(State)
            {
                case VisitorState.Arriving:Set(VisitorState.Entering);route.Enqueue(Lane(world.transform.position.z-2));break;
                case VisitorState.Entering:Choose();break;
                case VisitorState.Approaching:if(!exhibit||!world.Display(exhibit).Any()){Choose();break;}Set(VisitorState.Observing);wait=4+variant*.35f+(world.Building?world.Building.Comfort*3:0);break;
                case VisitorState.Observing:
                    if(exhibit){Vector3 look=exhibit.swimVolume.position-transform.position;look.y=0;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look),dt*3);}
                    if(age>wait){if(exhibit&&world.Display(exhibit).Any()){Viewed++;seen.Add(exhibit.id);Interest+=1+world.Display(exhibit).Max(f=>(int)f.rarity)+world.Display(exhibit).Select(f=>f.speciesId).Distinct().Count()*.3f;}Set(VisitorState.Reacting);}break;
                case VisitorState.Reacting:if(age>1.5f)Choose();break;
                case VisitorState.Reception:Set(VisitorState.Donating);break;
                case VisitorState.Donating:if(world.Donate(this)||age>4)Leave();break;
                case VisitorState.Leaving:Destroy(gameObject);break;
            }
        }
        void LateUpdate()
        {if(avatar?.Bones==null)return;phase+=Time.deltaTime*7;float swing=Mathf.Sin(phase)*speed*22;avatar.Bones[12].localRotation=Quaternion.Euler(swing,0,0);avatar.Bones[15].localRotation=Quaternion.Euler(-swing,0,0);avatar.Bones[6].localRotation=Quaternion.Euler(-swing,0,5);avatar.Bones[9].localRotation=Quaternion.Euler(swing,0,-5);if(State==VisitorState.Reacting)avatar.Bones[9].localRotation=Quaternion.Euler(-100,0,Mathf.Sin(age*8)*12);}
        void OnGUI()
        {if(State!=VisitorState.Reacting||!world.Ready||world.Panel.IsOpen)return;var camera=world.Player.GetComponent<LocalPlayerController>().view;if(!camera)return;var p=camera.WorldToScreenPoint(transform.position+Vector3.up*2);if(p.z>0){var style=new GUIStyle(GUI.skin.box){fontSize=15};GUI.Box(new Rect(p.x-85,Screen.height-p.y-28,170,30),GameSession.Current.Languages.Text("aquarium.reaction"),style);}}
    }
}

