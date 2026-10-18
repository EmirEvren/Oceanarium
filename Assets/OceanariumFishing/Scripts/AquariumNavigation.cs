using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class AquariumNavigation
    {
        readonly AquariumBuilding building;const float Step=.4f;HashSet<Vector2Int> cells=new();public int Generation {get;private set;}
        static readonly Vector2Int[] directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        public AquariumNavigation(AquariumBuilding b){building=b;}
        public static Vector3[] Corners(Vector3 p,float angle,Vector2 size){var q=Quaternion.Euler(0,angle,0);return new[]{p+q*new Vector3(-size.x/2,0,-size.y/2),p+q*new Vector3(size.x/2,0,-size.y/2),p+q*new Vector3(size.x/2,0,size.y/2),p+q*new Vector3(-size.x/2,0,size.y/2)};}
        public static bool Overlap(Vector3 a,float ar,Vector2 az,Vector3 b,float br,Vector2 bz,float clearance)
        {var ac=Corners(a,ar,az+Vector2.one*clearance);var bc=Corners(b,br,bz+Vector2.one*clearance);foreach(var axis in new[]{Quaternion.Euler(0,ar,0)*Vector3.right,Quaternion.Euler(0,ar,0)*Vector3.forward,Quaternion.Euler(0,br,0)*Vector3.right,Quaternion.Euler(0,br,0)*Vector3.forward}){float amin=ac.Min(p=>Vector3.Dot(p,axis)),amax=ac.Max(p=>Vector3.Dot(p,axis)),bmin=bc.Min(p=>Vector3.Dot(p,axis)),bmax=bc.Max(p=>Vector3.Dot(p,axis));if(amax<=bmin||bmax<=amin)return false;}return true;}
        Vector3 Point(Vector2Int c)=>new(c.x*Step,0,c.y*Step);
        HashSet<Vector2Int> Build(AquariumLayout layout)
        {
            var result=new HashSet<Vector2Int>();var objects=layout.objects.Where(o=>!o.stored&&(building.IsTank(o)||AquariumBuilding.Physical(building.Item(o.typeId)))).ToArray();
            for(int x=-13;x<=13;x++)for(int z=-11;z<=11+(layout.level-1)*20;z++)
            {var c=new Vector2Int(x,z);var p=Point(c);if(!building.Inside(p,layout.level,.3f))continue;bool doorwayBarrier=layout.level>1&&Mathf.Abs(p.z-5)<.4f&&Mathf.Abs(p.x)>1.1f;bool blocked=doorwayBarrier||building.FixedObstacles.Any(b=>Overlap(p,0,Vector2.one*.5f,b.center,0,new(b.size.x,b.size.z),0));if(!blocked)blocked=objects.Any(o=>Overlap(p,0,Vector2.one*.5f,o.position,o.rotation,building.Size(o),0));if(!blocked)result.Add(c);}return result;
        }
        Vector2Int Nearest(HashSet<Vector2Int> graph,Vector3 p)=>graph.OrderBy(c=>(Point(c)-new Vector3(p.x,0,p.z)).sqrMagnitude).FirstOrDefault();
        public bool Validate(AquariumLayout layout)
        {
            var graph=Build(layout);if(graph.Count==0)return false;var start=Nearest(graph,new Vector3(0,0,-4.4f));var seen=new HashSet<Vector2Int>{start};var q=new Queue<Vector2Int>();q.Enqueue(start);
            while(q.Count>0){var c=q.Dequeue();foreach(var d in directions){var next=c+d;if(graph.Contains(next)&&seen.Add(next))q.Enqueue(next);}}
            var goals=layout.objects.Where(o=>!o.stored&&building.IsTank(o)).Select(building.ViewPoint).Append(building.transform.InverseTransformPoint(building.World.donation.position));
            foreach(var goal in goals){var c=Nearest(graph,goal);if(!seen.Contains(c)||Vector3.Distance(Point(c),new Vector3(goal.x,0,goal.z))>.65f)return false;}return true;
        }
        public void Rebuild(AquariumLayout layout){cells=Build(layout);Generation++;}
        public bool Walkable(Vector3 world){var p=building.transform.InverseTransformPoint(world);if(p.z< -4.5f)return Mathf.Abs(p.x)<1.1f;return cells.Contains(new Vector2Int(Mathf.RoundToInt(p.x/Step),Mathf.RoundToInt(p.z/Step)));}
        public List<Vector3> Route(Vector3 from,Vector3 to)
        {
            var result=new List<Vector3>();if(cells.Count==0)return result;var a=building.transform.InverseTransformPoint(from);var b=building.transform.InverseTransformPoint(to);
            var start=Nearest(cells,a);var end=Nearest(cells,b);var previous=new Dictionary<Vector2Int,Vector2Int>{{start,start}};var q=new Queue<Vector2Int>();q.Enqueue(start);
            while(q.Count>0&&!previous.ContainsKey(end)){var c=q.Dequeue();foreach(var d in directions){var n=c+d;if(cells.Contains(n)&&!previous.ContainsKey(n)){previous[n]=c;q.Enqueue(n);}}}
            if(!previous.ContainsKey(end))return result;var path=new List<Vector2Int>();for(var p=end;p!=start;p=previous[p])path.Add(p);path.Reverse();
            if(a.z< -4.5f)result.Add(building.World.entrance.position);foreach(var p in path)result.Add(building.transform.TransformPoint(Point(p)));result.Add(to);return result;
        }
    }
}

