using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class AquariumFish:MonoBehaviour
    {
        public FishInstance Specimen {get;private set;}public FishSpecies Species {get;private set;}
        AquariumTank tank;System.Random random;Vector3 target,bounds;float timer,phase,rest;Transform tail;readonly List<Mesh> meshes=new();
        float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        public void Initialize(AquariumTank home,FishInstance specimen,FishSpecies species,Material material)
        {
            tank=home;Specimen=specimen.Copy();Species=species;random=new System.Random(specimen.seed);phase=Rand(0,6.28f);
            float size=Mathf.Clamp(specimen.length*.018f,.24f,1.12f);bounds=home.definition.dimensions*.5f-new Vector3(size*.62f,.22f,size*.62f);bounds=Vector3.Max(bounds,Vector3.one*.12f);
            transform.localPosition=Point();target=Point();var model=new GameObject("Species geometry").transform;model.SetParent(transform,false);model.localScale=Vector3.one*size;
            // Model faces +Z; all eight species use their canonical saved shapes/colors.
            Vector3 shape=species.shape;Orb(model,"Body",Vector3.zero,new Vector3(shape.z,shape.y,shape.x),species.color,material);
            tail=new GameObject("Tail pivot").transform;tail.SetParent(model,false);tail.localPosition=new Vector3(0,0,-shape.x*.42f);
            Fin(tail,new[]{Vector3.zero,new Vector3(0,.25f,-.3f),new Vector3(0,0,-.23f),new Vector3(0,-.25f,-.3f)},species.color*.85f,material);
            Fin(model,new[]{new Vector3(0,shape.y*.38f,-.3f),new Vector3(0,shape.y*.75f,-.16f),new Vector3(0,shape.y*.4f,.2f)},species.color*.8f,material);
            for(int s=-1;s<=1;s+=2){Orb(model,"Eye",new Vector3(s*shape.z*.44f,shape.y*.16f,shape.x*.3f),Vector3.one*.105f,Color.white,material);Orb(model,"Pupil",new Vector3(s*(shape.z*.44f+.035f),shape.y*.16f,shape.x*.32f),Vector3.one*.055f,new Color(.05f,.1f,.15f),material);Fin(model,new[]{new Vector3(s*shape.z*.38f,0,.08f),new Vector3(s*shape.z*.9f,-.15f,-.15f),new Vector3(s*shape.z*.4f,-.1f,-.1f)},species.color*.8f,material);}
            if(species.id=="clownfish")for(int i=0;i<3;i++)Orb(model,"Ivory band",new Vector3(0,0,-.25f+i*.25f),new Vector3(shape.z*1.03f,shape.y*1.03f,.06f),new Color(1,.94f,.81f),material);
            if(species.id=="blue_tang")Orb(model,"Blue tang marking",new Vector3(0,.03f,-.04f),new Vector3(shape.z*1.04f,shape.y*.36f,shape.x*.68f),new Color(.06f,.15f,.3f),material);
            if(species.id=="pufferfish")for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Orb(model,"Puffer spot",new Vector3(Mathf.Sin(a)*shape.z*.46f,.14f,Mathf.Cos(a)*shape.x*.46f),Vector3.one*.075f,new Color(.25f,.31f,.19f),material);}
            if(species.id=="mackerel")for(int i=0;i<5;i++)Orb(model,"Back stripe",new Vector3(0,shape.y*.3f,-.32f+i*.14f),new Vector3(shape.z*.9f,.06f,.035f),new Color(.14f,.28f,.3f),material);
        }
        Vector3 Point(){for(int i=0;i<20;i++){var p=new Vector3(Rand(-bounds.x,bounds.x),Rand(-bounds.y*.6f,bounds.y),Rand(-bounds.z,bounds.z));if(!tank.world.Building||tank.world.Building.SafeFishPoint(tank,p,.22f))return p;}return new Vector3(0,bounds.y,0);}
        void Update()
        {
            if(tank==null)return;float dt=Time.deltaTime;timer-=dt;rest-=dt;Vector3 p=transform.localPosition;
            if(timer<=0||Vector3.Distance(p,target)<.12f){target=Point();timer=Rand(3,7);if(random.NextDouble()<.12)rest=Rand(.5f,1.5f);}
            var direction=target-p;
            foreach(var other in tank.Fish)if(other!=this){Vector3 away=p-other.transform.localPosition;float d=away.magnitude;if(d<.35f&&d>.001f)direction+=away/d*(.35f-d)*2;else if(Species.schooling&&other.Species==Species&&d<1.4f)direction+=other.transform.forward*.1f;}
            // Low central reef decoration occupies the bottom; steer above it.
            if(tank.decoration&&tank.decoration.activeSelf&&p.y<-.25f&&new Vector2(p.x,p.z).magnitude<.5f)direction+=Vector3.up;
            if(direction.sqrMagnitude>.001f)transform.localRotation=Quaternion.Slerp(transform.localRotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-2*dt));
            Vector3 next=p+transform.localRotation*Vector3.forward*(Species.swimSpeed*(rest>0?.16f:1)*dt);
            // Reflect the target before a boundary; final clamp only handles numerical overshoot.
            for(int a=0;a<3;a++){if(Mathf.Abs(next[a])>bounds[a]-.05f)target[a]=-Mathf.Sign(next[a])*bounds[a]*.7f;next[a]=Mathf.Clamp(next[a],-bounds[a],bounds[a]);}
            if(tank.world.Building&&!tank.world.Building.SafeFishPoint(tank,next,.22f)){target=Point();if(tank.world.Building.SafeFishPoint(tank,p,.22f))next=p;else next=new Vector3(p.x,bounds.y,p.z);}transform.localPosition=next;if(tail)tail.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*6+phase)*(rest>0?5:20),0);
        }
        static void Orb(Transform parent,string name,Vector3 position,Vector3 scale,Color color,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;Destroy(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;var r=go.GetComponent<Renderer>();r.sharedMaterial=material;var b=new MaterialPropertyBlock();b.SetColor("_BaseColor",color);r.SetPropertyBlock(b);}
        void Fin(Transform parent,Vector3[] v,Color color,Material material)
        {int n=v.Length;var all=new Vector3[n*2];System.Array.Copy(v,all,n);System.Array.Copy(v,0,all,n,n);var tri=new List<int>();for(int i=1;i<n-1;i++)tri.AddRange(new[]{0,i,i+1,n,n+i+1,n+i});var m=new Mesh();m.vertices=all;m.triangles=tri.ToArray();m.RecalculateNormals();meshes.Add(m);var go=new GameObject("Fin",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=m;var r=go.GetComponent<Renderer>();r.sharedMaterial=material;var b=new MaterialPropertyBlock();b.SetColor("_BaseColor",color);r.SetPropertyBlock(b);}
        void OnDestroy(){foreach(var m in meshes)if(m)Destroy(m);}
    }
}

