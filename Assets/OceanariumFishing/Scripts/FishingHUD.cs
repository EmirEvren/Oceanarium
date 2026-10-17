using UnityEngine;
namespace OceanariumFishing
{
    public sealed class FishingHUD:MonoBehaviour
    {
        public FishingController fishing;CozyUI ui;float progress,tension;FoundationHUD foundation;CollectionHUD collection;
        string T(string key)=>GameSession.Current?.Languages.Text("fishing."+key)??"";
        void Start(){foundation=FindFirstObjectByType<FoundationHUD>();collection=GetComponent<CollectionHUD>();}
        void Update(){progress=Mathf.MoveTowards(progress,fishing?.Fight?.Progress??0,Time.unscaledDeltaTime*1.7f);tension=Mathf.MoveTowards(tension,fishing?.Fight?.Tension??0,Time.unscaledDeltaTime*2);}
        void Bar(float x,float y,float value,string key,Color color)
        {
            GUI.Label(new Rect(x,y,365,30),T(key)+"  "+Mathf.RoundToInt(value*100)+"%",ui.text);var old=GUI.color;
            GUI.color=new Color(.25f,.39f,.4f);GUI.DrawTexture(new Rect(x,y+34,365,24),Texture2D.whiteTexture);GUI.color=color;GUI.DrawTexture(new Rect(x,y+34,365*Mathf.Clamp01(value),24),Texture2D.whiteTexture);GUI.color=old;
        }
        void OnGUI()
        {
            if(!fishing||!fishing.Equipped||GameSession.Current?.Languages.Ready!=true||collection?.IsOpen==true||foundation?.SettingsOpen==true)return;
            ui??=new CozyUI();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;float x=w-465;
            if(fishing.State==FishingState.CatchSuccess)
            {
                ui.Book(new Rect(x,105,420,625));GUI.Label(new Rect(x+25,125,370,45),T("caught"),ui.heading);GUI.Label(new Rect(x+25,175,370,42),T("fish."+fishing.Catch.speciesId),ui.title);
                GUI.Label(new Rect(x+25,225,370,30),T("rarity."+fishing.Catch.rarity.ToString().ToLowerInvariant()),ui.text);
                GUI.Label(new Rect(x+25,268,370,32),string.Format(T("weight"),fishing.Catch.weight),ui.text);GUI.Label(new Rect(x+25,306,370,32),string.Format(T("length"),fishing.Catch.length),ui.text);
                GUI.Label(new Rect(x+25,350,370,48),T(fishing.Catch.quality>.8f?"quality.large":"quality.healthy"),ui.text);
                if(fishing.NewDiscovery)GUI.Label(new Rect(x+25,405,370,86),T("discovery"),ui.heading);
                else if(fishing.NewRecord)GUI.Label(new Rect(x+25,405,370,72),T("record"),ui.heading);
                if(GUI.Button(new Rect(x+25,515,175,57),T("keep"),ui.button))fishing.ResolveCatch(true);
                if(GUI.Button(new Rect(x+215,515,175,57),T("release"),ui.button))fishing.ResolveCatch(false);
                if(fishing.Feedback=="save_error")GUI.Label(new Rect(x+25,595,370,95),T("save_error"),ui.text);
                else GUI.Label(new Rect(x+25,595,370,85),T("journal_added"),ui.text);
            }
            else if(fishing.Fight!=null&&(fishing.State==FishingState.Fighting||fishing.State==FishingState.Reeling))
            {
                ui.Book(new Rect(x,130,420,560));GUI.Label(new Rect(x+25,150,370,40),T("fight"),ui.title);
                Bar(x+25,210,progress,"progress",new Color(.36f,.69f,.58f));Bar(x+25,290,tension,"tension",tension>.8f?new Color(.85f,.4f,.28f):new Color(.84f,.64f,.29f));
                GUI.Label(new Rect(x+25,375,370,70),T(fishing.Fight.Tension>.8f?"warning":"calm"),ui.heading);
                GUI.Label(new Rect(x+25,450,370,38),string.Format(T("combo"),fishing.Fight.Combo),ui.heading);
                GUI.Label(new Rect(x+25,500,370,145),T(fishing.Difficulty.id=="cozy"?"cozy_help":"fight_help"),ui.text);
            }
            else
            {
                ui.Panel(new Rect(w/2-280,h-245,560,155));GUI.Label(new Rect(w/2-255,h-228,510,45),T(fishing.State==FishingState.Idle?"ready":fishing.Feedback),ui.heading);
                if(fishing.State==FishingState.Aiming)Bar(w/2-182,h-180,fishing.Charge,"power",new Color(.36f,.69f,.58f));
                else GUI.Label(new Rect(w/2-255,h-177,510,75),T(fishing.State==FishingState.FishBiting?"hook_help":fishing.State==FishingState.Idle?"cast_help":"cancel_help"),ui.text);
                if(fishing.State==FishingState.FishBiting)GUI.Label(new Rect(w/2-35,h-298,70,42),"!",ui.title);
            }
            if(fishing.TutorialVisible&&fishing.Active&&fishing.State!=FishingState.CatchSuccess)
            {ui.Panel(new Rect(28,180,340,220));GUI.Label(new Rect(48,198,300,190),T("tutorial"),ui.small);}
            GUI.matrix=old;
        }
        void OnDestroy()=>ui?.Dispose();
    }
}
