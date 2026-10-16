using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Oceanarium
{
    public sealed class IslandPrototype : MonoBehaviour
    {
        [SerializeField] private Shader worldShader;
        [SerializeField, Min(1)] private float walkSpeed = 4;
        [SerializeField, Min(1)] private float runSpeed = 6.5f;
        private readonly List<Material> materials = new();
        private readonly List<GameObject> rods = new();
        private Transform world, avatar, visual, chest;
        private CharacterController motor;
        private Camera view;
        private bool started, menu = true, hasRod, inspectedChest, chestOpen;
        private float yaw = 25, pitch = 32, verticalSpeed, noticeUntil;
        private string notice;
        private Transform nearby;
        private GUIStyle titleStyle, bodyStyle, smallStyle, buttonStyle;
        public int RodCount => rods.Count;
        public bool HasRod => hasRod;
        public bool IsExploring => started && !menu;

        public void SetShader(Shader shader) => worldShader = shader;

        private void Awake()
        {
            world = new GameObject("Home Reef - Generated Environment").transform;
            world.SetParent(transform);
            BuildIsland();
            BuildPlayer();
            var cameraObject = new GameObject("Prototype Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform);
            view = cameraObject.GetComponent<Camera>();
            view.tag = "MainCamera";
            view.fieldOfView = 48;
            view.farClipPlane = 220;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.56f, .82f, .86f);
            RenderSettings.ambientLight = new Color(.65f, .72f, .72f);
            var sun = new GameObject("Warm afternoon sun", typeof(Light));
            sun.transform.SetParent(world);
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.8f;
            light.color = new Color(1, .9f, .72f);
            light.shadows = LightShadows.Soft;
            UpdateCamera(true);
        }

        private Material Palette(string name, Color color)
        {
            var material = new Material(worldShader != null ? worldShader : Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.color = color;
            material.SetFloat("_Smoothness", .12f);
            materials.Add(material);
            return material;
        }

        private GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid = true, Transform parent = null)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent != null ? parent : world, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) item.GetComponent<Collider>().enabled = false;
            else if (type == PrimitiveType.Cylinder)
            {
                // Unity's default capsule collider becomes rounded when a cylinder is flattened.
                item.GetComponent<Collider>().enabled = false;
                item.AddComponent<MeshCollider>().sharedMesh = item.GetComponent<MeshFilter>().sharedMesh;
            }
            return item;
        }

        private void BuildIsland()
        {
            var sea = Palette("Lagoon teal", new Color(.08f, .59f, .66f));
            var foam = Palette("Shallow turquoise", new Color(.35f, .8f, .78f));
            var sand = Palette("Warm sand", new Color(.91f, .79f, .52f));
            var grass = Palette("Sage grass", new Color(.43f, .67f, .35f));
            var wood = Palette("Honey timber", new Color(.48f, .28f, .15f));
            var plank = Palette("Sunlit boards", new Color(.72f, .48f, .27f));
            var wall = Palette("Cabin cream", new Color(.96f, .85f, .63f));
            var roof = Palette("Coral roof", new Color(.73f, .29f, .22f));
            var leaf = Palette("Canopy mint", new Color(.24f, .52f, .34f));
            var dark = Palette("Deep blue", new Color(.08f, .23f, .28f));
            var gold = Palette("Brass", new Color(.95f, .68f, .25f));
            Shape("Ocean", PrimitiveType.Cube, new Vector3(0,-.55f,0), new Vector3(180,.2f,180),sea,false);
            Shape("Shallows", PrimitiveType.Cylinder, new Vector3(0,-.4f,0),new Vector3(32,.08f,29),foam,false);
            Shape("Beach", PrimitiveType.Cylinder,new Vector3(0,-.4f,0),new Vector3(28,.4f,25),sand);
            Shape("Meadow", PrimitiveType.Cylinder,new Vector3(-1,-.04f,2),new Vector3(22,.08f,18),grass);
            Shape("Cabin",PrimitiveType.Cube,new Vector3(-5,1.6f,4),new Vector3(4,3,3.5f),wall);
            var roofA=Shape("Roof left",PrimitiveType.Cube,new Vector3(-6.1f,3.45f,4),new Vector3(2.8f,.25f,4.3f),roof);
            roofA.transform.localRotation=Quaternion.Euler(0,0,28);
            var roofB=Shape("Roof right",PrimitiveType.Cube,new Vector3(-3.9f,3.45f,4),new Vector3(2.8f,.25f,4.3f),roof);
            roofB.transform.localRotation=Quaternion.Euler(0,0,-28);
            Shape("Door",PrimitiveType.Cube,new Vector3(-5,1.1f,2.22f),new Vector3(.9f,2,.12f),wood);
            Shape("Round window",PrimitiveType.Sphere,new Vector3(-6.2f,2,2.2f),new Vector3(.7f,.7f,.15f),dark,false);
            chest=Shape("Supply Chest",PrimitiveType.Cube,new Vector3(-2,.55f,1.6f),new Vector3(1.3f,.9f,.8f),wood).transform;
            Shape("Chest lid",PrimitiveType.Cube,new Vector3(-2,1.04f,1.6f),new Vector3(1.4f,.18f,.9f),plank);
            Shape("Chest latch",PrimitiveType.Cube,new Vector3(-2,.75f,1.16f),new Vector3(.16f,.3f,.08f),gold,false);
            for(int i=0;i<15;i++)
                Shape("Pier board "+i,PrimitiveType.Cube,new Vector3(3,.22f,-7-i*.65f),new Vector3(3,.28f,.6f),plank);
            for(int i=0;i<4;i++)
            {
                float z=-8-i*2.3f;
                foreach(float x in new[]{1.7f,4.3f})
                    Shape("Pier post",PrimitiveType.Cylinder,new Vector3(x,-.1f,z),new Vector3(.22f,1,.22f),wood);
                var rod=Shape("Fishing Rod "+(i+1),PrimitiveType.Cylinder,new Vector3(4,.95f,-9-i*1.6f),new Vector3(.065f,.9f,.065f),wood,false);
                rod.transform.rotation=Quaternion.Euler(0,0,-18);
                rods.Add(rod);
                Shape("Rod reel",PrimitiveType.Sphere,new Vector3(0,-.45f,0),new Vector3(2.4f,.12f,2.4f),gold,false,rod.transform);
            }
            for(int i=0;i<9;i++)
            {
                float a=i*.67f;
                var p=new Vector3(Mathf.Cos(a)*10,0,Mathf.Sin(a)*7+2);
                Shape("Tree trunk "+i,PrimitiveType.Cylinder,p+Vector3.up*1.3f,new Vector3(.45f,1.3f,.45f),wood);
                Shape("Tree canopy "+i,PrimitiveType.Sphere,p+Vector3.up*3.2f,new Vector3(3.2f,3.4f,2.8f),leaf,false);
                Shape("Tree crown "+i,PrimitiveType.Sphere,p+new Vector3(.7f,4,0),new Vector3(2.3f,2.4f,2.3f),grass,false);
            }
            for(int i=0;i<7;i++)
                Shape("Stepping stone",PrimitiveType.Cylinder,new Vector3(-1+i*.6f,.16f,-i*.9f),new Vector3(.85f,.04f,.65f),sand,false);
            Shape("Boat hull",PrimitiveType.Sphere,new Vector3(6,-.05f,-13),new Vector3(2,.8f,4),wood,false);
            Shape("Boat inside",PrimitiveType.Cube,new Vector3(6,.3f,-13),new Vector3(1.35f,.12f,2.5f),plank,false);
            Shape("Broken engine",PrimitiveType.Cube,new Vector3(6,.65f,-14.4f),new Vector3(.6f,.7f,.6f),dark,false);
        }

        private void BuildPlayer()
        {
            avatar=new GameObject("Player").transform;
            avatar.SetParent(transform);
            avatar.position=new Vector3(1,.35f,-1);
            motor=avatar.gameObject.AddComponent<CharacterController>();
            motor.height=1.7f; motor.radius=.32f; motor.center=Vector3.up*.85f;
            motor.stepOffset=.35f;
            visual=new GameObject("Character visual").transform;
            visual.SetParent(avatar,false);
            var shirt=Palette("Ocean jacket",new Color(.15f,.43f,.57f));
            var skin=Palette("Peach",new Color(.88f,.62f,.42f));
            var hat=Palette("Straw",new Color(.97f,.77f,.36f));
            var boots=Palette("Boots",new Color(.19f,.24f,.27f));
            Shape("Jacket",PrimitiveType.Capsule,new Vector3(0,.7f,0),new Vector3(.65f,.45f,.5f),shirt,false,visual);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.35f,0),Vector3.one*.7f,skin,false,visual);
            Shape("Hat brim",PrimitiveType.Cylinder,new Vector3(0,1.65f,0),new Vector3(.95f,.035f,.95f),hat,false,visual);
            Shape("Hat crown",PrimitiveType.Cylinder,new Vector3(0,1.77f,0),new Vector3(.6f,.1f,.6f),hat,false,visual);
            foreach(float x in new[]{-.17f,.17f})
            {
                Shape("Boot",PrimitiveType.Capsule,new Vector3(x,.22f,0),new Vector3(.25f,.22f,.3f),boots,false,visual);
                Shape("Eye",PrimitiveType.Sphere,new Vector3(x*.7f,1.4f,.32f),Vector3.one*.065f,boots,false,visual);
            }
        }

        private void Update()
        {
            var keyboard=Keyboard.current;
            if(keyboard!=null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if(chestOpen) chestOpen=false;
                else if(started) menu=!menu;
            }
            nearby=null;
            if(!started || menu || chestOpen) return;
            var mouse=Mouse.current;
            if(mouse!=null && mouse.rightButton.isPressed)
            {
                var delta=mouse.delta.ReadValue();
                yaw+=delta.x*.15f; pitch=Mathf.Clamp(pitch-delta.y*.1f,15,65);
            }
            Vector2 input=Vector2.zero;
            if(keyboard!=null)
            {
                input.x=(keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0);
                input.y=(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0);
            }
            input=Vector2.ClampMagnitude(input,1);
            var direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
            verticalSpeed=motor.isGrounded?-2:Mathf.Max(-25,verticalSpeed-20*Time.deltaTime);
            motor.Move((direction*(keyboard!=null && keyboard.leftShiftKey.isPressed?runSpeed:walkSpeed)+Vector3.up*verticalSpeed)*Time.deltaTime);
            if(direction.sqrMagnitude>.01f) visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(direction),12*Time.deltaTime);
            if(avatar.position.y < -3)
            {
                motor.enabled=false; avatar.position=new Vector3(3,.6f,-6); motor.enabled=true; verticalSpeed=0;
            }
            float distance=2.3f;
            foreach(var rod in rods)
            {
                float d=Vector3.Distance(avatar.position,rod.transform.position);
                if(d<distance) {distance=d;nearby=rod.transform;}
            }
            if(Vector3.Distance(avatar.position,chest.position)<distance) nearby=chest;
            if(nearby!=null && keyboard!=null && keyboard.eKey.wasPressedThisFrame)
            {
                if(nearby==chest) {inspectedChest=true;chestOpen=true;}
                else if(hasRod) ShowNotice("rod.owned");
                else
                {
                    hasRod=true; rods.Remove(nearby.gameObject); Destroy(nearby.gameObject); nearby=null;
                    ShowNotice("rod.collected");
                }
            }
        }

        private void LateUpdate() => UpdateCamera(false);
        private void UpdateCamera(bool immediate)
        {
            if(view==null) return;
            if(!started)
            {
                view.transform.position=new Vector3(23,20,-30);
                view.transform.LookAt(new Vector3(0,0,-1));
                return;
            }
            var focus=avatar.position+Vector3.up*1.2f;
            var offset=Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-8);
            float distance=offset.magnitude;
            if(Physics.SphereCast(focus,.2f,offset.normalized,out var hit,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                distance=Mathf.Max(.5f,hit.distance-.15f);
            var target=focus+offset.normalized*distance;
            view.transform.position=immediate?target:Vector3.Lerp(view.transform.position,target,1-Mathf.Exp(-12*Time.deltaTime));
            view.transform.LookAt(focus);
        }

        private void ShowNotice(string key) {notice=PrototypeText.Get(key);noticeUntil=Time.unscaledTime+5;}
        private void OnGUI()
        {
            if(titleStyle==null)
            {
                titleStyle=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold,wordWrap=true};
                bodyStyle=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};
                smallStyle=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
                buttonStyle=new GUIStyle(GUI.skin.button){fontSize=19,fixedHeight=48};
            }
            var oldMatrix=GUI.matrix;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            float width=Screen.width/scale, height=Screen.height/scale;
            if(menu)
            {
                GUI.Box(new Rect(32,50,480,410),GUIContent.none);
                GUILayout.BeginArea(new Rect(58,72,430,370));
                GUILayout.Label(PrototypeText.Get("subtitle"),smallStyle);
                GUILayout.Space(14); GUILayout.Label(PrototypeText.Get("title"),titleStyle);
                GUILayout.Label(PrototypeText.Get("intro"),bodyStyle);
                GUILayout.Space(35);
                if(GUILayout.Button(PrototypeText.Get(started?"resume":"start"),buttonStyle)) {started=true;menu=false;UpdateCamera(true);}
                GUILayout.Space(24); GUILayout.Label(PrototypeText.Get("scope"),smallStyle);
                GUILayout.EndArea();
            }
            else
            {
                GUI.Box(new Rect(24,24,450,100),GUIContent.none);
                GUI.Label(new Rect(42,35,415,85),PrototypeText.Get(!hasRod?"goal.rod":!inspectedChest?"goal.chest":"goal.done"),bodyStyle);
                GUI.Label(new Rect(26,135,450,30),string.Format(PrototypeText.Get("rods"),rods.Count),smallStyle);
                GUI.Box(new Rect(20,height-48,width-40,34),GUIContent.none);
                GUI.Label(new Rect(34,height-43,width-68,32),PrototypeText.Get("controls"),smallStyle);
                if(nearby!=null && !chestOpen) GUI.Box(new Rect(width/2-150,height-110,300,40),PrototypeText.Get(nearby==chest?"chest":"rod"));
                if(Time.unscaledTime<noticeUntil) GUI.Label(new Rect(width/2-280,height-165,560,50),notice,bodyStyle);
                if(chestOpen)
                {
                    GUI.Box(new Rect(width/2-260,height/2-150,520,300),GUIContent.none);
                    GUILayout.BeginArea(new Rect(width/2-235,height/2-130,470,260));
                    GUILayout.Label(PrototypeText.Get("chest.title"),bodyStyle);
                    GUILayout.Space(14);GUILayout.Label(PrototypeText.Get("chest.body"),bodyStyle);
                    GUILayout.Space(18);
                    if(GUILayout.Button(PrototypeText.Get("close"),buttonStyle)) chestOpen=false;
                    GUILayout.EndArea();
                }
            }
            GUI.matrix=oldMatrix;
        }

        private void OnDestroy()
        {
            foreach(var material in materials) if(material!=null) Destroy(material);
        }
    }
}
