using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class TownNavigation:MonoBehaviour
    {
        const int Size=65;const float Step=1;readonly Vector3[] points=new Vector3[Size*Size];readonly bool[] open=new bool[Size*Size];
        public bool Ready{get;private set;}
        public void Build()
        {
            var ground=GameObject.Find("Town organic island")?.GetComponent<Collider>();if(!ground)return;var hits=new Collider[24];
            for(int z=0;z<Size;z++)for(int x=0;x<Size;x++)
            {
                int index=z*Size+x;var p=new Vector3((x-32)*Step,30,(z-32)*Step);if(!ground.Raycast(new Ray(p,Vector3.down),out var hit,60))continue;p=hit.point+Vector3.up*.04f;points[index]=p;if(p.y<.65f||hit.normal.y<.8f)continue;
                bool blocked=false;int count=Physics.OverlapSphereNonAlloc(p+Vector3.up*.7f,.43f,hits,~0,QueryTriggerInteraction.Ignore);
                for(int n=0;n<count;n++)if(hits[n]!=ground&&!hits[n].GetComponentInParent<TownResident>()&&!hits[n].GetComponentInParent<LocalPlayerController>()){blocked=true;break;}open[index]=!blocked;
            }
            Ready=true;
        }
        int Nearest(Vector3 p){int best=-1;float distance=9;for(int i=0;i<points.Length;i++)if(open[i]){float d=(points[i]-p).sqrMagnitude;if(d<distance){best=i;distance=d;}}return best;}
        public List<Vector3> Route(Vector3 start,Vector3 goal)
        {
            var result=new List<Vector3>();int a=Nearest(start),b=Nearest(goal);if(a<0||b<0)return result;var previous=new int[points.Length];System.Array.Fill(previous,-1);var queue=new Queue<int>();previous[a]=a;queue.Enqueue(a);
            while(queue.Count>0&&previous[b]<0){int current=queue.Dequeue();int x=current%Size,z=current/Size;foreach(int next in new[]{x>0?current-1:-1,x<Size-1?current+1:-1,z>0?current-Size:-1,z<Size-1?current+Size:-1})if(next>=0&&open[next]&&previous[next]<0&&Mathf.Abs(points[current].y-points[next].y)<.6f){previous[next]=current;queue.Enqueue(next);}}
            if(previous[b]<0)return result;for(int at=b;at!=a;at=previous[at])result.Add(points[at]);result.Reverse();return result;
        }
    }
}
