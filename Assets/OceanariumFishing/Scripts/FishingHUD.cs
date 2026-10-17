using UnityEngine;
namespace OceanariumFishing
{
    public sealed class FishingHUD:MonoBehaviour
    {
        public FishingController fishing;GUIStyle title,text,button;Texture2D round;
        string T(string key)=>GameSession.Current?.Languages.Text("fishing."+key)??"";
        void Init(){if(title!=null)return;title=new GUIStyle(GUI.skin.label){fontSize=26,fontStyle=FontStyle.Bold};text=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true};button=new GUIStyle(GUI.skin.button){fontSize=18};foreach(var s in new[]{title,text})s.normal.textColor=new Color(.96f,.94f,.82f);round=new Texture2D(1,1);round.SetPixel(0,0,new Color(.06f,.19f,.23f,.95f));round.Apply();button.normal.background=round;button.hover.background=round;button.active.background=round;button.normal.textColor=new Color(.96f,.9f,.7f);}
        void Panel(Rect r)=>GUI.DrawTexture(r,round);
        void Bar(float x,float y,float value,string key,Color color){GUI.Label(new Rect(x,y,350,26),T(key),text);GUI.color=new Color(.16f,.31f,.34f);GUI.DrawTexture(new Rect(x,y+30,350,15),Texture2D.whiteTexture);GUI.color=color;GUI.DrawTexture(new Rect(x,y+30,350*Mathf.Clamp01(value),15),Texture2D.whiteTexture);GUI.color=Color.white;}
        void OnGUI()
        {
            if(!fishing || !fishing.Equipped || GameSession.Current?.Languages.Ready!=true)return;Init();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            if(fishing.State==FishingState.CatchSuccess)
            {
                float x=w-450;Panel(new Rect(x,110,410,570));GUI.Label(new Rect(x+26,130,360,40),T("caught"),title);GUI.Label(new Rect(x+26,180,360,40),T("fish."+fishing.Catch.speciesId),title);GUI.Label(new Rect(x+26,230,360,30),T("rarity."+fishing.Catch.rarity.ToString().ToLowerInvariant()),text);
                GUI.Label(new Rect(x+26,285,360,30),string.Format(T("weight"),fishing.Catch.weight),text);GUI.Label(new Rect(x+26,325,360,30),string.Format(T("length"),fishing.Catch.length),text);GUI.Label(new Rect(x+26,370,360,50),T(fishing.Catch.quality>.8f?"quality.large":"quality.healthy"),text);if(fishing.NewRecord)GUI.Label(new Rect(x+26,425,360,55),T("record"),title);
                if(GUI.Button(new Rect(x+25,510,170,60),T("keep"),button))fishing.ResolveCatch(true);if(GUI.Button(new Rect(x+210,510,170,60),T("release"),button))fishing.ResolveCatch(false);if(fishing.Feedback=="save_error")GUI.Label(new Rect(x+25,585,360,70),T("save_error"),text);
            }
            else if(fishing.Fight!=null && (fishing.State==FishingState.Fighting || fishing.State==FishingState.Reeling))
            {
                float x=w-420;Panel(new Rect(x,130,390,475));GUI.Label(new Rect(x+20,145,350,36),T("fight"),title);Bar(x+20,195,fishing.Fight.Progress,"progress",new Color(.44f,.84f,.69f));Bar(x+20,255,fishing.Fight.Tension,"tension",fishing.Fight.Tension>.8f?new Color(1,.4f,.3f):new Color(.98f,.76f,.39f));Bar(x+20,315,fishing.Fight.Stamina,"stamina",new Color(.5f,.7f,.92f));GUI.Label(new Rect(x+20,380,350,32),T("pull")+"  "+(fishing.Fight.Direction>0?"→":"←"),title);
                GUI.color=new Color(.25f,.39f,.4f);GUI.DrawTexture(new Rect(x+20,421,350,8),Texture2D.whiteTexture);GUI.color=new Color(.44f,.84f,.69f);GUI.DrawTexture(new Rect(x+20+(-fishing.Fight.Direction+1)*165,415,20,20),Texture2D.whiteTexture);GUI.color=new Color(1,.82f,.4f);GUI.DrawTexture(new Rect(x+20+(fishing.Steering+1)*165,413,5,24),Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(new Rect(x+20,447,350,35),string.Format(T("combo"),fishing.Fight.Combo),title);GUI.Label(new Rect(x+20,490,350,90),T("fight_help"),text);
            }
            else
            {
                Panel(new Rect(w/2-260,h-230,520,145));GUI.Label(new Rect(w/2-235,h-215,470,40),T(fishing.State==FishingState.Idle?"ready":fishing.Feedback),title);
                if(fishing.State==FishingState.Aiming){Bar(w/2-175,h-172,fishing.Charge,"power",new Color(.5f,.85f,.71f));GUI.color=new Color(1,.8f,.4f);GUI.DrawTexture(new Rect(w/2-175+350*.72f,h-142,350*.14f,15),Texture2D.whiteTexture);GUI.color=Color.white;}
                else GUI.Label(new Rect(w/2-235,h-168,470,65),T(fishing.State==FishingState.FishBiting?"hook_help":fishing.State==FishingState.Idle?"cast_help":"cancel_help"),text);
            }
            GUI.matrix=old;
        }
        void OnDestroy(){if(round)Destroy(round);}
    }
}
