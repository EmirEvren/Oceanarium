using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    public sealed class CharacterCreator : MonoBehaviour
    {
        public CharacterAppearance preview;public Camera previewCamera;
        public AppearanceData Draft {get;private set;}
        public string Category {get;private set;}="hair";
        public static readonly string[] Categories={"presentation","hair","hairColor","face","eyes","brows","mouth","beard","skin","top","bottom","shoes","hat","accessory"};
        float rotation,zoom,framing=1.15f;Vector2 scroll;bool dragging;string errorKey;
        GUIStyle title,subtitle,label,card,nav,small,panel;Texture2D rounded;Color ink=new(.12f,.26f,.28f),cream=new(.98f,.96f,.9f),teal=new(.18f,.43f,.42f);
        void Start(){Draft=AppearanceStore.Load();preview.Apply(Draft);}
        string T(string key)=>GameSession.Current?.Languages.Text("creator."+key)??string.Empty;
        public void Select(string slot,string id){Draft.Set(slot,id);Draft.Validate();preview.Apply(Draft);}
        public void SetCategory(string id){Category=id;scroll=Vector2.zero;zoom=0;}
        public void Randomize(){Draft=AppearanceData.Randomized();preview.Apply(Draft);}
        public void ResetAppearance(){Draft=new AppearanceData();preview.Apply(Draft);rotation=0;zoom=0;}
        public bool Confirm()
        {if(!AppearanceStore.Save(Draft,out var error)){Debug.LogWarning(error);errorKey="save_error";return false;}GameSession.Current.Travel(Destination.HomeIsland);return true;}
        void Update()
        {
            if(Draft==null)return;var mouse=Mouse.current;
            if(mouse!=null)
            {
                var p=mouse.position.ReadValue();bool over=p.x>Screen.width*.43f && p.y>Screen.height*.19f && p.y<Screen.height*.86f;
                if(mouse.leftButton.wasPressedThisFrame && over)dragging=true;
                if(!mouse.leftButton.isPressed)dragging=false;
                if(dragging)rotation-=mouse.delta.ReadValue().x*.45f;
                if(over)zoom=Mathf.Clamp(zoom-mouse.scroll.ReadValue().y*.0015f,-.25f,.4f);
            }
            bool face=Category=="face" || Category=="eyes" || Category=="brows" || Category=="mouth" || Category=="beard";
            float desired=(face?.66f:1.13f)+zoom;framing=Mathf.Lerp(framing,desired,1-Mathf.Exp(-5*Time.deltaTime));previewCamera.orthographicSize=framing;
            var target=new Vector3(0,face?1.52f:1.02f,0);previewCamera.transform.position=Vector3.Lerp(previewCamera.transform.position,target+new Vector3(0,.12f,4),1-Mathf.Exp(-5*Time.deltaTime));previewCamera.transform.LookAt(previewCamera.transform.position+Vector3.back*4+Vector3.down*.12f);
            preview.transform.rotation=Quaternion.Slerp(preview.transform.rotation,Quaternion.Euler(0,rotation,0),1-Mathf.Exp(-12*Time.deltaTime));
        }
        void Styles()
        {
            if(title!=null)return;rounded=new Texture2D(32,32){name="Creator rounded card",hideFlags=HideFlags.HideAndDontSave};var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=Mathf.Max(0,Mathf.Abs(x-15.5f)-8),dy=Mathf.Max(0,Mathf.Abs(y-15.5f)-8);pixels[y*32+x]=new Color(1,1,1,Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy)));}rounded.SetPixels(pixels);rounded.Apply();
            title=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold};subtitle=new GUIStyle(GUI.skin.label){fontSize=17};label=new GUIStyle(GUI.skin.label){fontSize=22,fontStyle=FontStyle.Bold};small=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            foreach(var s in new[]{title,subtitle,label,small})s.normal.textColor=ink;
            card=new GUIStyle(GUI.skin.button){fontSize=16,wordWrap=true,alignment=TextAnchor.MiddleCenter,border=new RectOffset(12,12,12,12),padding=new RectOffset(10,10,8,8)};
            card.normal.background=rounded;card.hover.background=rounded;card.active.background=rounded;card.normal.textColor=ink;card.hover.textColor=ink;card.active.textColor=ink;
            nav=new GUIStyle(card){alignment=TextAnchor.MiddleLeft,fontSize=16,padding=new RectOffset(18,8,5,5)};panel=new GUIStyle{border=new RectOffset(12,12,12,12)};panel.normal.background=rounded;
        }
        bool Button(Rect rect,string text,bool selected=false,GUIStyle style=null)
        {var old=GUI.backgroundColor;GUI.backgroundColor=selected?new Color(.88f,.66f,.44f):cream;bool hit=GUI.Button(rect,text,style??card);GUI.backgroundColor=old;return hit;}
        void Panel(Rect rect,Color color){if(Event.current.type!=EventType.Repaint)return;var old=GUI.color;GUI.color=color;panel.Draw(rect,GUIContent.none,false,false,false,false);GUI.color=old;}
        void OnGUI()
        {
            if(Draft==null || GameSession.Current==null || !GameSession.Current.Languages.Ready)return;Styles();float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1440*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            Panel(new Rect(28,25,1384,118),cream);GUI.Label(new Rect(52,40,550,55),T("title"),title);GUI.Label(new Rect(55,101,1000,28),T("subtitle"),subtitle);GUI.Label(new Rect(1180,60,200,35),T("brand"),label);
            Panel(new Rect(28,160,200,710),new Color(.86f,.91f,.84f));
            for(int i=0;i<Categories.Length;i++)if(Button(new Rect(38,171+i*48,180,42),T(Categories[i]),Categories[i]==Category,nav))SetCategory(Categories[i]);
            Panel(new Rect(244,160,340,710),cream);GUI.Label(new Rect(264,179,300,40),T(Category),label);
            scroll=GUI.BeginScrollView(new Rect(258,228,316,618),scroll,new Rect(0,0,292,ContentHeight()));
            float y=0;
            if(Category=="face"){Options("nose",ref y);Options("detail",ref y);}
            else{Options(Category,ref y);if(Category=="eyes")Options("eyeColor",ref y);if(Category=="brows"){if(Button(new Rect(0,y,290,48),T("match_brows"),Draft.matchBrows)){Draft.matchBrows=!Draft.matchBrows;preview.Apply(Draft);}y+=62;if(!Draft.matchBrows)Options("browColor",ref y);}if(Category=="beard")Options("beardColor",ref y);if(Category=="top" || Category=="bottom" || Category=="shoes")Options("outfitColor",ref y);if(Category=="accessory")Options("glasses",ref y);}
            GUI.EndScrollView();
            GUI.Label(new Rect(650,156,720,30),T("studio"),subtitle);GUI.Label(new Rect(720,681,620,34),T("preview_help"),small);
            if(Button(new Rect(684,731,64,43),"↶"))rotation+=45;if(Button(new Rect(760,731,64,43),"↷"))rotation-=45;
            if(Button(new Rect(840,731,230,43),T("camera_reset"))){rotation=0;zoom=0;}
            if(Button(new Rect(1086,731,60,43),"−"))zoom=Mathf.Clamp(zoom+.1f,-.25f,.4f);if(Button(new Rect(1158,731,60,43),"+"))zoom=Mathf.Clamp(zoom-.1f,-.25f,.4f);
            if(Button(new Rect(625,804,170,54),T("randomize")))Randomize();if(Button(new Rect(808,804,140,54),T("reset")))ResetAppearance();if(Button(new Rect(962,804,180,54),T("back")))GameSession.Current.Travel(Destination.MainMenu);if(Button(new Rect(1156,804,228,54),T("confirm"),true))Confirm();
            if(errorKey!=null)GUI.Label(new Rect(650,776,700,25),T(errorKey),small);GUI.matrix=old;
        }
        float ContentHeight(){if(Category=="hair")return 800;if(Category=="eyes" || Category=="beard" || Category=="brows" || Category=="accessory")return 1000;return 760;}
        void Options(string slot,ref float y)
        {
            GUI.Label(new Rect(0,y,290,32),T(slot),subtitle);y+=38;var ids=CosmeticCatalog.Options[slot];bool palette=slot.Contains("Color") || slot=="skin";int columns=palette?4:2;float width=palette?66:140,height=palette?66:74;
            for(int i=0;i<ids.Length;i++)
            {
                var r=new Rect((i%columns)*(width+6),y+(i/columns)*(height+8),width,height);bool selected=Draft.Get(slot)==ids[i];
                if(palette){Panel(r,selected?new Color(.75f,.48f,.24f):new Color(.87f,.88f,.82f));Panel(new Rect(r.x+5,r.y+5,r.width-10,r.height-10),CosmeticCatalog.ColorOf(ids[i]));if(GUI.Button(r,new GUIContent("",T(ids[i])),GUIStyle.none))Select(slot,ids[i]);}
                else if(Button(r,T(ids[i]),selected))Select(slot,ids[i]);
            }
            y+=Mathf.Ceil(ids.Length/(float)columns)*(height+8)+22;
            if(palette){GUI.Label(new Rect(0,y-18,290,28),T(Draft.Get(slot)),small);y+=16;}
        }
        void OnDestroy(){if(rounded!=null)Destroy(rounded);}
    }
}
