using UnityEngine;
namespace OceanariumFishing
{
    // Small reusable IMGUI palette; all backgrounds are generated in Unity.
    public sealed class CozyUI
    {
        public GUIStyle title,heading,text,small,button,tab,question;public Texture2D paper,wood,teal,highlight;public static readonly Color Ink=new(.12f,.26f,.28f);
        readonly System.Collections.Generic.Dictionary<Texture2D,GUIStyle> panels=new();
        public CozyUI()
        {
            paper=Rounded(new Color(.96f,.91f,.79f));wood=Rounded(new Color(.28f,.18f,.13f));teal=Rounded(new Color(.12f,.33f,.35f));highlight=Rounded(new Color(.76f,.87f,.77f));
            title=Label(31,true);heading=Label(23,true);text=Label(18);small=Label(15);
            question=Label(31,true);SetTextColor(question,new Color(.97f,.92f,.8f));
            foreach(var texture in new[]{paper,wood,teal,highlight}){var style=new GUIStyle{border=new RectOffset(8,8,8,8)};style.normal.background=texture;panels[texture]=style;}
            button=new GUIStyle(GUI.skin.button){fontSize=17,wordWrap=true,border=new RectOffset(12,12,12,12),padding=new RectOffset(14,14,8,8)};
            button.normal.background=teal;button.hover.background=highlight;button.active.background=highlight;button.normal.textColor=new Color(1,.96f,.84f);button.hover.textColor=button.active.textColor=Ink;
            tab=new GUIStyle(button){fontSize=15};
        }
        static void SetTextColor(GUIStyle style,Color color){foreach(var state in new[]{style.normal,style.hover,style.active,style.focused,style.onNormal,style.onHover,style.onActive,style.onFocused})state.textColor=color;}
        GUIStyle Label(int size,bool bold=false){var s=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,wordWrap=true};SetTextColor(s,Ink);return s;}
        static Texture2D Rounded(Color color)
        {
            var t=new Texture2D(32,32,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=Mathf.Max(7-x,x-24),dy=Mathf.Max(7-y,y-24);float distance=dx>0&&dy>0?Mathf.Sqrt(dx*dx+dy*dy):0;var c=color;c.a*=Mathf.Clamp01(8-distance);t.SetPixel(x,y,c);}t.Apply();return t;
        }
        public void Panel(Rect rect,Texture2D texture=null){if(Event.current.type==EventType.Repaint)panels[texture??paper].Draw(rect,GUIContent.none,false,false,false,false);}
        public void Book(Rect rect)
        {
            Panel(new Rect(rect.x-10,rect.y-10,rect.width+20,rect.height+20),wood);Panel(rect);
            var c=GUI.color;GUI.color=new Color(.72f,.57f,.35f,.55f);GUI.DrawTexture(new Rect(rect.x+16,rect.y+16,28,3),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rect.x+16,rect.y+16,3,28),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rect.xMax-44,rect.yMax-19,28,3),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rect.xMax-19,rect.yMax-44,3,28),Texture2D.whiteTexture);
            if(rect.width>1000){GUI.color=new Color(.58f,.4f,.23f,.18f);GUI.DrawTexture(new Rect(rect.x+720,rect.y+190,2,rect.height-240),Texture2D.whiteTexture);}GUI.color=c;
        }
        public void Dispose(){foreach(var t in new[]{paper,wood,teal,highlight})if(t)Object.Destroy(t);}
    }
}
