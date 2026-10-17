using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(100)]
    public sealed class FishingPresentation:MonoBehaviour
    {
        public Material material,lineMaterial;public FishingController owner;
        public Transform Tip {get;private set;}public Transform Bobber {get;private set;}public LineRenderer Line {get;private set;}
        Transform rodRoot,fishVisual,shadow;LineRenderer rodCurve,aimRing;AudioSource audioSource;FishingWater water;CharacterAppearance avatar;LocalPlayerController player;readonly List<AudioClip> clips=new();readonly List<Mesh> fishMeshes=new();readonly List<GameObject> effects=new();
        void Start(){water=FindFirstObjectByType<FishingWater>();avatar=GetComponentInChildren<CharacterAppearance>();player=GetComponent<LocalPlayerController>();}
        public void Equip(Transform hand,GameObject prefab)
        {
            if(rodRoot)return;rodRoot=prefab?Instantiate(prefab,hand).transform:new GameObject("Equipped starter rod").transform;rodRoot.SetParent(hand,false);rodRoot.localPosition=new Vector3(0,-.015f,.015f);rodRoot.localRotation=Quaternion.identity;
            rodCurve=rodRoot.gameObject.AddComponent<LineRenderer>();rodCurve.sharedMaterial=lineMaterial;rodCurve.useWorldSpace=false;rodCurve.positionCount=14;rodCurve.startWidth=.035f;rodCurve.endWidth=.009f;rodCurve.startColor=new Color(.5f,.3f,.14f);rodCurve.endColor=new Color(.9f,.71f,.4f);
            Tip=new GameObject("Rod tip").transform;Tip.SetParent(rodRoot,false);Tip.localPosition=new Vector3(0,0,1.6f);
            Shape(rodRoot,"Cork grip",new Vector3(0,0,.12f),new Vector3(.055f,.055f,.3f),new Color(.63f,.42f,.23f));Shape(rodRoot,"Reel",new Vector3(0,-.07f,.24f),Vector3.one*.11f,new Color(.26f,.4f,.42f));
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=.35f;audioSource.volume=.22f;audioSource.playOnAwake=false;
            for(int i=0;i<5;i++){int count=7200;var samples=new float[count];float frequency=220+i*135;for(int j=0;j<count;j++){float t=j/24000f;float envelope=Mathf.Sin(Mathf.PI*j/count)*Mathf.Exp(-t*12);samples[j]=Mathf.Sin(t*frequency*Mathf.PI*2)*envelope*.3f;}var clip=AudioClip.Create("Synth fishing cue "+i,count,1,24000,false);clip.SetData(samples,0);clips.Add(clip);}
        }
        public void Cue(int index){if(audioSource && clips.Count>index)audioSource.PlayOneShot(clips[index]);}
        public void BeginCast(Vector3 p)
        {
            ClearCast();Bobber=new GameObject("Fishing bobber").transform;Bobber.position=p;
            Shape(Bobber,"Cream float",Vector3.zero,new Vector3(.18f,.16f,.18f),new Color(1,.91f,.7f));Shape(Bobber,"Coral crown",Vector3.up*.07f,new Vector3(.14f,.09f,.14f),new Color(.96f,.3f,.2f));
            Line=new GameObject("Fishing line").AddComponent<LineRenderer>();Line.sharedMaterial=lineMaterial;Line.positionCount=25;Line.startWidth=.009f;Line.endWidth=.006f;Line.startColor=Line.endColor=new Color(.94f,.95f,.8f,.9f);Cue(0);
            shadow=new GameObject("Hooked fish surface silhouette").transform;Shape(shadow,"Fish silhouette",Vector3.zero,new Vector3(.23f,.025f,.62f),new Color(.09f,.30f,.30f));shadow.gameObject.SetActive(false);
        }
        public void ShowFish(FishSpecies species)
        {
            if(fishVisual)Destroy(fishVisual.gameObject);fishVisual=new GameObject("Caught "+species.id).transform;
            Shape(fishVisual,"Body",Vector3.zero,Vector3.Scale(species.shape,new Vector3(.65f,.65f,.65f)),species.color);
            Fin("Forked tail",new[]{new Vector3(-.25f,0,0),new Vector3(-.49f,.19f,0),new Vector3(-.43f,0,0),new Vector3(-.49f,-.19f,0)},species.color*.85f);
            Fin("Dorsal fin",new[]{new Vector3(-.19f,.1f,0),new Vector3(-.12f,.29f,0),new Vector3(.13f,.16f,0)},species.color*.8f);
            for(int side=-1;side<=1;side+=2)Fin("Pectoral fin",new[]{new Vector3(.06f,.02f,side*.09f),new Vector3(-.13f,-.1f,side*.22f),new Vector3(-.03f,-.04f,side*.1f)},species.color*.85f);
            for(int s=-1;s<=1;s+=2){Shape(fishVisual,"Eye",new Vector3(.20f,.055f,s*.09f),Vector3.one*.065f,Color.white);Shape(fishVisual,"Pupil",new Vector3(.21f,.06f,s*.119f),Vector3.one*.032f,new Color(.06f,.12f,.17f));}
            if(species.id=="clownfish")for(int i=0;i<3;i++)Shape(fishVisual,"Stripe",new Vector3(-.16f+i*.14f,0,0),new Vector3(.035f,.29f,.19f),Color.white);
            Cue(4);
        }
        void Fin(string name,Vector3[] vertices,Color color)
        {
            // Separate front/back vertices keep opposite face normals from cancelling.
            int count=vertices.Length;var bothSides=new Vector3[count*2];
            System.Array.Copy(vertices,0,bothSides,0,count);System.Array.Copy(vertices,0,bothSides,count,count);
            var mesh=new Mesh{name=name};mesh.vertices=bothSides;var triangles=new List<int>();
            for(int i=1;i<count-1;i++)triangles.AddRange(new[]{0,i,i+1,count,count+i+1,count+i});
            mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();fishMeshes.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(fishVisual,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);renderer.SetPropertyBlock(block);
        }
        void Shape(Transform parent,string name,Vector3 p,Vector3 scale,Color color)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;Destroy(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;var r=go.GetComponent<Renderer>();r.sharedMaterial=material;var b=new MaterialPropertyBlock();b.SetColor("_BaseColor",color);r.SetPropertyBlock(b);}
        public void Ripple(Vector3 p,bool burst=false)
        {var go=new GameObject("Fishing ripple");go.transform.position=p+Vector3.up*.02f;var r=go.AddComponent<LineRenderer>();r.sharedMaterial=lineMaterial;r.useWorldSpace=false;r.loop=true;r.positionCount=33;r.startWidth=r.endWidth=burst?.025f:.012f;r.startColor=r.endColor=new Color(.8f,1,.95f);for(int i=0;i<33;i++){float a=i*Mathf.PI*2/32;r.SetPosition(i,new Vector3(Mathf.Cos(a)*.17f,0,Mathf.Sin(a)*.17f));}var fx=go.AddComponent<FishingRipple>();fx.duration=burst?1:.7f;effects.Add(go);effects.RemoveAll(x=>!x);
            if(burst){var splash=new GameObject("Fishing splash");splash.transform.position=p;var particles=splash.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=particles.main;main.loop=false;main.duration=.4f;main.startLifetime=.45f;main.startSpeed=.8f;main.startSize=.045f;main.gravityModifier=.6f;main.startColor=new Color(.7f,.94f,.92f);var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,12)});var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=35;shape.radius=.07f;splash.transform.rotation=Quaternion.Euler(-90,0,0);particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=lineMaterial;particles.Play();Destroy(splash,1.2f);effects.Add(splash);}
        }
        void LateUpdate()
        {
            if(!owner || !rodRoot)return;ApplyPose();float tension=owner.Fight?.Tension??0;float bend=Mathf.Lerp(.04f,.3f,tension);
            if(owner.State==FishingState.Aiming)
            {
                if(!aimRing){aimRing=new GameObject("Cast landing guide").AddComponent<LineRenderer>();aimRing.sharedMaterial=lineMaterial;aimRing.loop=true;aimRing.positionCount=33;aimRing.widthMultiplier=.025f;}
                aimRing.enabled=true;var point=owner.AimPoint();point.y=water?water.Height(point,Time.time)+.04f:.1f;aimRing.startColor=aimRing.endColor=owner.AimValid?new Color(.5f,1,.7f):new Color(1,.4f,.3f);for(int i=0;i<33;i++){float angle=i*Mathf.PI*2/32;aimRing.SetPosition(i,point+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.35f);}
            }else if(aimRing)aimRing.enabled=false;
            for(int i=0;i<14;i++){float t=i/13f;rodCurve.SetPosition(i,new Vector3(0,-bend*t*t,1.6f*t));}Tip.localPosition=rodCurve.GetPosition(13);
            if(Bobber && Line)
            {Vector3 start=Tip.position,end=Bobber.position;float slack=(1-tension)*Mathf.Min(.65f,Vector3.Distance(start,end)*.05f);for(int i=0;i<25;i++){float t=i/24f;Line.SetPosition(i,Vector3.Lerp(start,end,t)+Vector3.down*Mathf.Sin(t*Mathf.PI)*slack);}}
            if(shadow){bool fighting=owner.State==FishingState.Fighting || owner.State==FishingState.Reeling;shadow.gameObject.SetActive(fighting);if(fighting && Bobber){shadow.position=Bobber.position+new Vector3(.15f,.012f,.1f);shadow.rotation=Quaternion.Euler(0,Time.time*20+owner.Fight.Direction*55,0);}}
            if(fishVisual){var facing=player.visual;fishVisual.position=transform.position+facing.right*.6f+facing.forward*.3f+Vector3.up*1.6f;fishVisual.rotation=Quaternion.Euler(0,facing.eulerAngles.y+Mathf.Sin(Time.time*5)*8,Mathf.Sin(Time.time*6)*5);}

        }
        void ApplyPose()
        {
            float tension=owner.Fight?.Tension??0;
            if(avatar?.Bones==null)return;float charge=owner.State==FishingState.Aiming?owner.Charge:0;float reel=owner.State==FishingState.Reeling?Mathf.Sin(Time.time*12)*12:0;
            if(owner.Equipped){float cast=owner.State==FishingState.Casting?Mathf.Lerp(-115,-20,Mathf.Clamp01(owner.StateTime/.18f)):-45-charge*65;float twitch=owner.State==FishingState.FishBiting?Mathf.Sin(Time.time*18)*5:0;avatar.Bones[9].localRotation=Quaternion.Euler(cast-tension*15+twitch,owner.Steering*12,-10);avatar.Bones[10].localRotation=Quaternion.Euler(-20+reel,0,0);avatar.Bones[6].localRotation=Quaternion.Euler(-30,0,20);if(owner.State==FishingState.CatchSuccess)avatar.Bones[9].localRotation=Quaternion.Euler(-125,0,-15);}
        }
        public void ClearCast(){if(Bobber)Destroy(Bobber.gameObject);if(Line)Destroy(Line.gameObject);if(fishVisual)Destroy(fishVisual.gameObject);if(shadow)Destroy(shadow.gameObject);foreach(var mesh in fishMeshes)if(mesh)Destroy(mesh);fishMeshes.Clear();Bobber=null;Line=null;fishVisual=null;shadow=null;}
        void OnDisable(){ClearCast();if(aimRing)aimRing.enabled=false;foreach(var fx in effects)if(fx)Destroy(fx);effects.Clear();}
        void OnDestroy(){if(rodRoot)Destroy(rodRoot.gameObject);if(aimRing)Destroy(aimRing.gameObject);foreach(var clip in clips)if(clip)Destroy(clip);}
    }

}
