using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class CharacterAppearance : MonoBehaviour
    {
        public Material surface;public bool loadLocal=true;public LocalPlayerController controller;
        public AppearanceData Data {get;private set;} public Transform[] Bones {get;private set;}
        readonly List<Mesh> owned=new();readonly List<GameObject> rendered=new();
        readonly Dictionary<string,AvatarGeometry> groups=new();
        Vector3[] rest;float phase,blend,gesture,lastHeading,turn;
        InteractionActor actor;
        // The same bind-space skeleton is used by every cosmetic; no authority/network dependency.
        static readonly string[] BoneNames={"Root","Pelvis","Spine","Chest","Neck","Head","LeftUpperArm","LeftForearm","LeftHand","RightUpperArm","RightForearm","RightHand","LeftUpperLeg","LeftLowerLeg","LeftFoot","RightUpperLeg","RightLowerLeg","RightFoot"};
        static readonly int[] Parents={-1,0,1,2,3,4,3,6,7,3,9,10,1,12,13,1,15,16};
        static readonly Vector3[] Positions={new(0,0,0),new(0,.79f,0),new(0,.94f,0),new(0,1.13f,0),new(0,1.31f,0),new(0,1.57f,0),new(-.29f,1.17f,0),new(-.36f,.97f,0),new(-.39f,.76f,0),new(.29f,1.17f,0),new(.36f,.97f,0),new(.39f,.76f,0),new(-.145f,.76f,0),new(-.145f,.43f,0),new(-.145f,.13f,.025f),new(.145f,.76f,0),new(.145f,.43f,0),new(.145f,.13f,.025f)};
        void Start(){if(Data==null)Apply(loadLocal?AppearanceStore.Load():new AppearanceData());actor=GetComponentInParent<InteractionActor>();if(actor!=null)actor.Feedback+=Interact;lastHeading=transform.eulerAngles.y;}
        void Interact(string key){gesture=1;}
        void Rig()
        {
            if(Bones!=null)return;Bones=new Transform[BoneNames.Length];rest=new Vector3[Bones.Length];
            for(int i=0;i<Bones.Length;i++){var b=new GameObject(BoneNames[i]).transform;b.SetParent(Parents[i]<0?transform:Bones[Parents[i]],false);b.localPosition=Positions[i]-(Parents[i]<0?Vector3.zero:Positions[Parents[i]]);Bones[i]=b;rest[i]=b.localPosition;}
        }
        AvatarGeometry G(string color){if(!groups.TryGetValue(color,out var g)){g=new AvatarGeometry();groups.Add(color,g);}return g;}
        void O(string color,Vector3 p,Vector3 size,int bone,int lower=-1,float joint=0)=>G(color).Orb(p,size,bone,lower,joint);
        void Line(string color,int bone,float radius,params Vector3[] points)=>G(color).Strand(points,radius,bone);
        public void Apply(AppearanceData source)
        {
            Rig();Data=source.Copy();Data.Validate();for(int i=0;i<Bones.Length;i++){Bones[i].localPosition=rest[i];Bones[i].localRotation=Quaternion.identity;}
            foreach(var go in rendered){go.SetActive(false);Destroy(go);}rendered.Clear();foreach(var mesh in owned)Destroy(mesh);owned.Clear();groups.Clear();
            var d=Data;string skin=d.skin,hair=d.hairColor,cloth=d.outfitColor;
            O(skin,new(0,1.57f,0),new(.295f,.33f,.245f),5);O(skin,new(0,1.29f,0),new(.104f,.12f,.1f),4);
            O(skin,new(0,.96f,0),new(.225f,.3f,.14f),3,1,.95f);
            for(int s=-1;s<=1;s+=2)
            {
                int a=s<0?6:9,l=s<0?12:15;
                O(skin,new(s*.299f,1.55f,-.003f),new(.055f,.079f,.045f),5);
                // Covered arm vertices are omitted to keep every sleeve free of hidden-body poke-through.
                bool longSleeve=d.top=="sweater" || d.top=="hoodie" || d.top=="jacket";
                O(skin,new(s*.38f,longSleeve?.80f:.88f,0),new(.076f,longSleeve?.075f:.16f,.079f),a+1);
                O(skin,new(s*.39f,.755f,.01f),new(.077f,.09f,.062f),a+2);
                O(skin,new(s*.145f,.435f,0),new(.101f,.34f,.105f),l,l+1,.43f);
                O(skin,new(s*.145f,.105f,.055f),new(.102f,.085f,.166f),l+2);
            }
            Face();Hair();Clothes();Accessories();
            foreach(var pair in groups)
            {
                var go=new GameObject(pair.Key);go.transform.SetParent(transform,false);var renderer=go.AddComponent<SkinnedMeshRenderer>();var mesh=pair.Value.Build(transform,Bones,"OF_"+pair.Key);owned.Add(mesh);rendered.Add(go);renderer.sharedMesh=mesh;renderer.bones=Bones;renderer.rootBone=Bones[0];renderer.sharedMaterial=surface;renderer.localBounds=new Bounds(Vector3.up,Vector3.one*3);renderer.updateWhenOffscreen=true;
                var block=new MaterialPropertyBlock();var color=CosmeticCatalog.Hex.ContainsKey(pair.Key)?CosmeticCatalog.ColorOf(pair.Key):Parse(pair.Key);block.SetColor("_BaseColor",color);renderer.SetPropertyBlock(block);
            }
        }
        static Color Parse(string s){ColorUtility.TryParseHtmlString(s,out var c);return c;}
        void Face()
        {
            var d=Data;string brow=d.matchBrows?d.hairColor:d.browColor;float eyeH=d.eyes=="wide"?.063f:d.eyes=="sleepy"?.027f:d.eyes=="soft_oval"?.053f:.045f;
            for(int s=-1;s<=1;s+=2)
            {
                float x=s*.108f;O("#FFF4DF",new(x,1.59f,.226f),new(.061f,eyeH,.019f),5);
                O(d.eyeColor,new(x,1.59f,.242f),new(.033f,eyeH*.84f,.013f),5);O("#242630",new(x,1.59f,.252f),new(.019f,eyeH*.68f,.009f),5);O("#FFFFFF",new(x-.009f,1.605f,.26f),new(.009f,.012f,.005f),5);
                if(d.eyes=="happy")Line(d.skin,5,.015f,new(x-.045f,1.566f,.246f),new(x,1.575f,.257f),new(x+.045f,1.566f,.246f));
                if(d.eyes=="gentle")Line(brow,5,.007f,new(x-.044f,1.62f,.239f),new(x,1.634f,.249f),new(x+.044f,1.621f,.24f));
                float y=1.69f+(d.brows=="raised"?.025f:0),curve=d.brows=="straight"?0:d.brows=="rounded"?.018f:.009f;
                Line(brow,5,d.brows=="thin"?.007f:d.brows=="thick"?.017f:.012f,new(x-.047f,y,.225f),new(x,y+curve,.236f),new(x+.047f,y,.225f));
                if(d.detail=="blush" || d.detail=="cheek_detail")O("#CC887B",new(s*.171f,1.505f,.201f),new(.041f,.019f,.008f),5);
                if(d.detail=="freckles")for(int i=0;i<4;i++)O("#9B674C",new(s*(.13f+i*.017f),1.52f+(i%2)*.013f,.218f-i*.006f),new(.006f,.006f,.004f),5);
            }
            if(d.detail=="beauty_mark")O("#714C42",new(.16f,1.478f,.207f),new(.008f,.008f,.005f),5);
            O(d.skin,new(0,1.53f,.247f),new(d.nose=="soft"?.041f:.029f,.033f,d.nose=="pointed"?.049f:.034f),5);
            float width=d.mouth=="cheerful"?.065f:d.mouth=="soft_smile"?.055f:.04f;
            if(d.mouth=="tiny_open_smile" || d.mouth=="cheerful")O("#854B49",new(0,1.438f,.226f),new(width,.025f,.008f),5);
            else Line("#854B49",5,.007f,new(-width,1.452f,.222f),new(0,d.mouth=="neutral"?1.452f:d.mouth=="relaxed"?1.448f:1.433f,.234f),new(width,1.452f,.222f));
            if(d.beard!="none")
            {
                if(d.beard=="mustache" || d.beard=="full_beard")for(int s=-1;s<=1;s+=2)O(d.beardColor,new(s*.034f,1.476f,.235f),new(.043f,.015f,.017f),5);
                if(d.beard!="mustache")
                {float length=d.beard=="full_beard"?.115f:d.beard=="goatee"?.065f:.035f;O(d.beardColor,new(0,1.345f,.15f),new(d.beard=="goatee"?.07f:.18f,length,.09f),5);if(d.beard!="goatee")for(int s=-1;s<=1;s+=2)Line(d.beardColor,5,d.beard=="light_stubble"?.009f:.026f,new(s*.23f,1.53f,.13f),new(s*.19f,1.4f,.17f),new(s*.1f,1.355f,.2f));}
            }
        }
        void Hair()
        {
            var d=Data;string h=d.hairColor,id=d.hair;bool hat=d.hat!="none";
            // A fitted open crown leaves the forehead and ears clear, unlike a full sphere wig.
            G(h).Loft(new[]{1.70f,1.79f,1.88f,1.925f},new[]{.278f,.29f,.205f,.005f},new[]{.238f,.248f,.18f,.005f},Vector3.zero,5);
            for(int i=0;i<5;i++)O(h,new(-.215f+i*.105f,1.755f+(i%2)*.025f,.185f),new(.079f,.085f,.065f),5);
            if(hat)return; // Crown remains; protruding locks are tucked under hats.
            int index=System.Array.IndexOf(CosmeticCatalog.Options["hair"],id);
            if(index<5)
            {
                int count=id=="curly_short"?16:6;
                for(int i=0;i<count;i++){float a=i*2.39996f;float r=.23f*Mathf.Sqrt((i+.5f)/count);O(h,new(Mathf.Cos(a)*r,1.86f+((i%3)*.018f),Mathf.Sin(a)*r),new(id=="curly_short"?.07f:.11f,id=="messy_short"?.115f:.07f,.08f),5);}
                if(id=="side_swept")O(h,new(-.1f,1.77f,.21f),new(.16f,.09f,.075f),5);
                if(id=="medium_layered")for(int s=-1;s<=1;s+=2)O(h,new(s*.262f,1.63f,-.02f),new(.065f,.18f,.16f),5);
                return;
            }
            if(id=="double_buns")for(int s=-1;s<=1;s+=2)O(h,new(s*.27f,1.85f,-.035f),new(.13f,.13f,.13f),5);
            if(id=="high_ponytail" || id=="low_ponytail")
            {float y=id=="high_ponytail"?1.86f:1.61f;Line(d.outfitColor,5,.045f,new(-.045f,y,-.245f),new(.045f,y,-.245f));O(h,new(0,y-.15f,-.31f),new(.11f,.24f,.11f),5);return;}
            if(id=="twin_tails")for(int s=-1;s<=1;s+=2){O(h,new(s*.32f,1.55f,-.075f),new(.10f,.24f,.11f),5);O(d.outfitColor,new(s*.29f,1.75f,-.075f),new(.09f,.025f,.08f),5);}
            float bottom=id=="long_straight" || id=="long_wavy"?1.12f:id=="shoulder_layers"?1.3f:id=="braided"?1.28f:1.45f;
            if(id=="double_buns" || id=="twin_tails")bottom=1.57f;
            G(h).HairCurtain(bottom,id.Contains("wavy") || id=="shoulder_layers"?.035f:0,5);
            for(int s=-1;s<=1;s+=2)O(h,new(s*.274f,(1.76f+bottom)*.5f,-.005f),new(.045f,(1.76f-bottom)*.5f+.02f,.065f),5);
            if(id=="braided")for(int i=0;i<8;i++)O(h,new((i%2==0?-.035f:.035f),1.6f-i*.055f,-.285f),new(.07f,.05f,.055f),5);
        }
        void Clothes()
        {
            var d=Data;string c=d.outfitColor;bool sleeves=d.top!="basic_tshirt" && d.top!="striped_tshirt" && d.top!="fisherman_shirt";
            G(c).Loft(new[]{.76f,.79f,.9f,1.12f,1.22f,1.26f},new[]{.225f,.25f,.24f,.26f,.21f,.105f},new[]{.15f,.16f,.16f,.16f,.125f,.09f},Vector3.zero,3,1,.92f);
            for(int s=-1;s<=1;s+=2)
            {
                int a=s<0?6:9,l=s<0?12:15;float low=sleeves?.85f:1.025f;
                G(c).Sleeve(s,low,a);
                if(sleeves)O("cream",new(s*.38f,.84f,0),new(.086f,.024f,.09f),a+1);
                if(d.top=="fisherman_shirt" || d.top=="jacket")O("cream",new(s*.12f,1.1f,.155f),new(.055f,.048f,.012f),3);
                bool pants=d.bottom=="pants" || d.bottom=="cargo_pants";bool skirt=d.bottom.Contains("skirt");
                if(!skirt)G("navy").Loft(new[]{pants?.18f:.49f,pants?.22f:.52f,.7f,.81f},new[]{.111f,.116f,.125f,.12f},new[]{.116f,.12f,.126f,.12f},new(s*.14f,0,0),l,l+1,.43f);
                if(d.bottom=="cargo_pants")O(c,new(s*.245f,.57f,0),new(.03f,.07f,.085f),l);
                if(d.bottom=="denim_shorts")O("pastel_blue",new(s*.14f,.51f,0),new(.12f,.025f,.126f),l);
                bool boot=d.shoes.Contains("boots");string sc=d.shoes=="fisherman_boots"?"ochre":d.shoes=="cozy_shoes"?"coral":"cream";
                if(d.shoes!="sandals")O(sc,new(s*.145f,.12f,.075f),new(.115f,.10f,.18f),l+2);
                O("navy",new(s*.145f,.043f,.075f),new(.117f,.027f,.185f),l+2);
                if(boot)G(sc).Loft(new[]{.12f,d.shoes=="fisherman_boots"?.38f:.26f},new[]{.117f,.123f},new[]{.12f,.125f},new(s*.145f,0,.015f),l+1);
                if(d.shoes=="sneakers" || d.shoes=="sandals")for(int i=0;i<(d.shoes=="sandals"?1:3);i++)Line(c,l+2,.012f,new(s*.145f-.07f,.20f-i*.013f,.11f+i*.026f),new(s*.145f+.07f,.20f-i*.013f,.11f+i*.026f));
            }
            if(d.bottom.Contains("skirt"))
            {float low=d.bottom=="long_skirt"?.25f:.48f;G("navy").Loft(new[]{low,low+.04f,.67f,.81f},new[]{.34f,.34f,.27f,.24f},new[]{.23f,.23f,.18f,.16f},Vector3.zero,1,skirt:true);}
            if(d.top=="striped_tshirt")for(int i=0;i<5;i++)G("cream").Loft(new[]{.82f+i*.07f,.842f+i*.07f},new[]{.247f,.247f},new[]{.164f,.164f},Vector3.zero,3,1,.92f);
            if(d.top=="hoodie"){O(c,new(0,1.19f,-.15f),new(.17f,.12f,.08f),3);for(int s=-1;s<=1;s+=2)Line("cream",3,.009f,new(s*.055f,1.23f,.17f),new(s*.055f,1.06f,.185f));}
            if(d.top=="jacket" || d.top=="fisherman_shirt")Line("cream",3,.012f,new(0,.8f,.167f),new(0,1.2f,.166f));
            if(d.top=="sweater")G("cream").Loft(new[]{.77f,.81f},new[]{.25f,.25f},new[]{.165f,.165f},Vector3.zero,1);
        }
        void Accessories()
        {
            var d=Data;
            if(d.hat!="none")
            {
                string c=d.hat=="straw_hat"?"ochre":d.outfitColor;float brim=d.hat=="straw_hat"?.43f:d.hat=="baseball_cap"?.30f:.35f;
                if(d.hat!="beanie")O(c,new(0,1.81f,d.hat=="baseball_cap"?.12f:0),new(brim,.025f,d.hat=="baseball_cap"?.35f:brim*.85f),5);
                G(c).Loft(new[]{1.80f,1.88f,1.98f,2.015f},new[]{.30f,.285f,.23f,.005f},new[]{.255f,.25f,.20f,.005f},Vector3.zero,5);
                if(d.hat=="beanie")O(c,new(0,2.02f,0),new(.065f,.065f,.065f),5);
                if(d.hat=="fisherman_hat")O("cream",new(0,1.89f,.245f),new(.04f,.035f,.01f),5);
                if(d.hat=="bucket_hat")G(c).Loft(new[]{1.76f,1.83f},new[]{.36f,.29f},new[]{.31f,.255f},Vector3.zero,5);
            }
            if(d.glasses!="none")
            {
                for(int s=-1;s<=1;s+=2)
                {
                    float x=s*.11f;var points=new Vector3[17];for(int i=0;i<=16;i++){float a=i*Mathf.PI*2/16;float px=Mathf.Cos(a),py=Mathf.Sin(a);if(d.glasses=="square_glasses"){px=Mathf.Clamp(px*1.4f,-1,1);py=Mathf.Clamp(py*1.4f,-1,1);}points[i]=new(x+px*.078f,1.59f+py*.063f,.273f-Mathf.Abs(px)*.008f);}Line("#5B4941",5,.007f,points);
                    if(d.glasses=="sunglasses")O("navy",new(x,1.59f,.269f),new(.073f,.058f,.01f),5);
                    Line("#5B4941",5,.006f,new(s*.185f,1.6f,.263f),new(s*.295f,1.61f,.02f));
                }
                Line("#5B4941",5,.007f,new(-.035f,1.6f,.275f),new(.035f,1.6f,.275f));
            }
            if(d.accessory=="backpack")
            {O("ochre",new(0,1.025f,-.24f),new(.19f,.22f,.105f),3);O("cream",new(0,.95f,-.327f),new(.14f,.08f,.02f),3);for(int s=-1;s<=1;s+=2)Line("ochre",3,.018f,new(s*.16f,.88f,.17f),new(s*.18f,1.1f,.17f),new(s*.16f,1.24f,.13f),new(s*.16f,1.26f,-.08f),new(s*.16f,1.19f,-.22f));}
            if(d.accessory=="crossbody"){O("coral",new(.23f,.83f,.13f),new(.12f,.14f,.07f),1);Line("cream",3,.018f,new(-.18f,1.22f,.1f),new(0,1.02f,.18f),new(.23f,.83f,.18f));}
            if(d.accessory=="necklace"){Line("ochre",3,.008f,new(-.09f,1.27f,.10f),new(0,1.15f,.18f),new(.09f,1.27f,.1f));O("cream",new(0,1.14f,.185f),new(.02f,.027f,.012f),3);}
            if(d.accessory=="bracelet")O("ochre",new(-.389f,.82f,0),new(.088f,.019f,.092f),7);
        }
        void LateUpdate()
        {
            if(Bones==null)return;float speed=controller!=null?controller.Speed:0;blend=Mathf.Lerp(blend,Mathf.Clamp01(speed/3.4f),1-Mathf.Exp(-10*Time.deltaTime));float run=Mathf.InverseLerp(3.4f,5.8f,speed);phase+=Time.deltaTime*Mathf.Lerp(8,13,run);float swing=Mathf.Sin(phase)*blend*Mathf.Lerp(24,36,run);
            turn=Mathf.Lerp(turn,Mathf.Clamp(Mathf.DeltaAngle(lastHeading,transform.eulerAngles.y)*3,-12,12),Time.deltaTime*8);lastHeading=transform.eulerAngles.y;
            for(int i=0;i<Bones.Length;i++)Bones[i].localRotation=Quaternion.identity;
            Bones[0].localPosition=rest[0]+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.022f*blend+Mathf.Sin(Time.time*1.7f)*.006f*(1-blend));
            Bones[3].localRotation=Quaternion.Euler(run*4,turn,-turn*.2f);Bones[5].localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*.7f)*4*(1-blend),Mathf.Sin(Time.time*.9f)*1.5f);
            Bones[6].localRotation=Quaternion.Euler(swing,0,3);Bones[9].localRotation=Quaternion.Euler(-swing,0,-3);
            Bones[12].localRotation=Quaternion.Euler(-swing,0,0);Bones[15].localRotation=Quaternion.Euler(swing,0,0);
            Bones[13].localRotation=Quaternion.Euler(Mathf.Max(0,swing)*.7f,0,0);Bones[16].localRotation=Quaternion.Euler(Mathf.Max(0,-swing)*.7f,0,0);
            Bones[7].localRotation=Quaternion.Euler(-8-run*25,0,0);Bones[10].localRotation=Quaternion.Euler(-8-run*25-gesture*65,0,0);gesture=Mathf.MoveTowards(gesture,0,Time.deltaTime*1.5f);
        }
        void OnDestroy(){if(actor!=null)actor.Feedback-=Interact;foreach(var mesh in owned)if(mesh!=null)Destroy(mesh);}
    }
}
