using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace OceanariumFishing
{
    public sealed class WorldCalendarHUD:MonoBehaviour
    {
        public static WorldCalendarHUD Current{get;private set;}public bool IsOpen{get;private set;}public bool Busy{get;private set;}
        public bool BlocksInput=>IsOpen||Busy||closedFrame==Time.frameCount;
        CozyUI ui;int monthPage,closedFrame=-1;bool sleep;float fade;string message;IslandInteractable cabin;
        string T(string key)=>GameSession.Current?.Languages.Text("world."+key)??"";
        void Awake(){Current=this;}
        public void OpenSleep(IslandInteractable source){if(!ValidSleep(source))return;cabin=source;sleep=true;IsOpen=true;}
        bool ValidSleep(IslandInteractable source){var player=FindFirstObjectByType<FishingController>();return source&&source.kind==InteractionKind.Cabin&&player&&!player.Active&&Vector3.Distance(player.transform.position,source.transform.position)<5&&SceneManager.GetActiveScene().name=="OF_HomeIsland"&&WorldTimeService.Current.IsAuthority;}
        void Close(){IsOpen=false;sleep=false;closedFrame=Time.frameCount;}
        void Update()
        {
            if(Busy)return;var player=FindFirstObjectByType<LocalPlayerController>();
            if(Keyboard.current?.cKey.wasPressedThisFrame==true&&player&&(IsOpen||player.InputEnabled)){if(IsOpen)Close();else{IsOpen=true;monthPage=WorldTimeService.Current.Simulation.Day/28;}}
            if(IsOpen&&Keyboard.current?.escapeKey.wasPressedThisFrame==true)Close();
        }
        IEnumerator Rest()
        {
            if(!ValidSleep(cabin)){message=T("sleep_invalid");yield break;}Busy=true;IsOpen=false;
            while(fade<1){fade=Mathf.Min(1,fade+Time.unscaledDeltaTime*2);yield return null;}
            if(ValidSleep(cabin)&&WorldTimeService.Current.AdvanceTo(WorldTimeService.Current.Simulation.NextMorning))
            {foreach(var guest in FindObjectsByType<AquariumVisitor>(FindObjectsSortMode.None))Destroy(guest.gameObject);message=null;}else message=T("save_error");
            yield return new WaitForSecondsRealtime(.3f);while(fade>0){fade=Mathf.Max(0,fade-Time.unscaledDeltaTime*2);yield return null;}Busy=false;Close();
        }
        public string ForecastText()
        {
            var sim=WorldTimeService.Current.Simulation;string line(int day)=>string.Join(" / ",Enumerable.Range(0,4).Select(i=>(i*6).ToString("00")+":00 "+GameSession.Current.Languages.Text(sim.Forecast(day,i).nameKey)));
            var available=FindObjectsByType<FishingZone>(FindObjectsSortMode.None).SelectMany(z=>z.entries.Where(e=>e.species&&System.Array.IndexOf(e.species.seasons,sim.Month)>=0&&System.Array.IndexOf(e.species.weather,sim.Current.id)>=0&&(e.species.startHour<=e.species.endHour?sim.Hour>=e.species.startHour&&sim.Hour<e.species.endHour:sim.Hour>=e.species.startHour||sim.Hour<e.species.endHour)).Select(e=>e.species)).Distinct().Take(3);
            return T("today")+": "+line(sim.Day)+"\n"+T("tomorrow")+": "+line(sim.Day+1)+"\n"+T("wind")+": "+sim.Current.wind.ToString("0.0")+" · "+T("fish")+": "+string.Join(", ",available.Select(f=>GameSession.Current.Languages.Text(f.nameKey)));
        }
        void OnGUI()
        {
            var service=WorldTimeService.Current;if(service?.Simulation==null||GameSession.Current?.Languages.Ready!=true||!FindFirstObjectByType<FishingController>())return;
            var sim=service.Simulation;ui??=new();var matrix=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale;GUI.depth=-40;
            if(!IsOpen&&!Busy){GUI.Box(new Rect(w-335,24,310,100),"");GUI.Label(new Rect(w-320,29,285,90),((int)sim.Hour).ToString("00")+":"+(sim.State.minute%60).ToString("00")+" · "+sim.DayOfMonth+" "+T("month."+sim.Month)+"\n"+GameSession.Current.Languages.Text(sim.Current.nameKey)+"  [C]",ui.small);}
            if(IsOpen)
            {
                float x=(w-1100)/2;ui.Book(new Rect(x,70,1100,750));GUI.Label(new Rect(x+30,90,950,50),T(sleep?"sleep":"calendar"),ui.title);
                if(GUI.Button(new Rect(x+950,90,120,40),T("close"),ui.button))Close();
                if(sleep){GUI.Label(new Rect(x+50,220,1000,100),T("sleep_body"),ui.heading);if(GUI.Button(new Rect(x+250,420,600,60),T("sleep_confirm"),ui.button))StartCoroutine(Rest());}
                else
                {
                    GUI.Label(new Rect(x+300,160,600,45),T("month."+(monthPage%4))+" · "+T("year")+" "+(monthPage/4+1),ui.heading);
                    if(GUI.Button(new Rect(x+40,160,150,40),"<",ui.button))monthPage=Mathf.Max(0,monthPage-1);if(GUI.Button(new Rect(x+870,160,150,40),">",ui.button))monthPage++;
                    for(int col=0;col<7;col++)GUI.Label(new Rect(x+35+col*148,215,145,35),T("weekday."+col),ui.small);
                    for(int n=0;n<28;n++){int day=monthPage*28+n;var rect=new Rect(x+35+n%7*148,250+n/7*70,140,62);GUI.Box(rect,(sim.Day==day?"• ":"")+(n+1)+(sim.MerchantNight(day)?"\n"+T("merchant"):""),ui.small);}
                    GUI.Label(new Rect(x+40,550,1020,145),ForecastText(),ui.small);GUI.Label(new Rect(x+40,705,1020,60),T("season."+(monthPage%4))+" · "+T("events_note"),ui.small);
                }
                if(!string.IsNullOrEmpty(message))GUI.Label(new Rect(x+40,730,1000,50),message,ui.text);
            }
            if(fade>0){var color=GUI.color;GUI.color=new Color(.025f,.04f,.09f,fade);GUI.DrawTexture(new Rect(0,0,w,Screen.height/scale),Texture2D.whiteTexture);GUI.color=color;}
            GUI.matrix=matrix;GUI.depth=0;
        }
        void OnDestroy(){ui?.Dispose();if(Current==this)Current=null;}
    }
}
