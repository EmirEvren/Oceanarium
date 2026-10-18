using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class BoatTravelHUD:MonoBehaviour
    {
        BoatHarbor harbor;CozyUI ui;string selected="town";float hover;Texture2D islandTexture;string T(string k)=>GameSession.Current?.Languages.Text("travel."+k)??k;
        void Awake(){harbor=GetComponent<BoatHarbor>();}
        void OnGUI()
        {
            if(!harbor.Ready||GameSession.Current?.Languages.Ready!=true)return;ui??=new CozyUI();var old=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;GUI.depth=-50;
            if(harbor.Busy){GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,w,70),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,h-85,w,85),Texture2D.whiteTexture);GUI.color=color;GUI.Label(new Rect(40,h-67,w-280,50),T(harbor.CinematicKey??"loading"),ui.question);if(GUI.Button(new Rect(w-225,h-65,190,42),T("skip"),ui.button))harbor.SkipRequested=true;}
            else if(harbor.MapOpen)Map(w,h);
            else if(harbor.DialogueOpen)
            {float x=(w-850)/2;ui.Book(new Rect(x,h-320,850,270));GUI.Label(new Rect(x+30,h-295,780,40),T("captain"),ui.title);GUI.Label(new Rect(x+30,h-240,780,100),T(harbor.Message),ui.text);if(GUI.Button(new Rect(x+30,h-120,510,45),T(!harbor.Progress.accepted?"accept":harbor.Progress.repaired?"ready_explore":"repair"),ui.button)){if(!harbor.Progress.accepted)harbor.Accept();else if(!harbor.Progress.repaired)harbor.Repair();else harbor.CloseDialogue();}if(GUI.Button(new Rect(x+580,h-120,230,45),T("close"),ui.button))harbor.CloseDialogue();}
            else if(harbor.islandId=="home"&&harbor.Progress.introduced&&!harbor.Progress.repaired&&FindFirstObjectByType<AquariumBuildHUD>()?.IsOpen!=true&&FindFirstObjectByType<AquariumHUD>()?.IsOpen!=true&&FindFirstObjectByType<CollectionHUD>()?.IsOpen!=true)
            {ui.Book(new Rect(w-370,170,340,235));GUI.Label(new Rect(w-350,185,300,38),T("quest"),ui.heading);string objective=!harbor.Progress.accepted?T("talk"):!harbor.Progress.inspected?T("inspect"):T("collect");GUI.Label(new Rect(w-350,228,300,40),objective,ui.small);GUI.Label(new Rect(w-350,272,300,115),T("driftwood")+" "+harbor.Count("driftwood")+" / 5\n"+T("scrap")+" "+harbor.Count("scrap")+" / 3\n"+T("rope")+" "+harbor.Count("rope")+" / 2",ui.text);}
            GUI.matrix=old;GUI.color=color;GUI.depth=0;
        }
        void Map(float w,float h)
        {
            float x=(w-1280)/2,y=(h-790)/2;ui.Book(new Rect(x,y,1280,790));GUI.Label(new Rect(x+35,y+22,900,48),T("map"),ui.title);if(GUI.Button(new Rect(x+1050,y+25,195,42),T("leave"),ui.button))harbor.Leave();
            var sea=new Rect(x+28,y+90,850,660);var c=GUI.color;GUI.color=new Color(.56f,.75f,.72f);GUI.DrawTexture(sea,Texture2D.whiteTexture);GUI.color=c;
            for(int row=0;row<12;row++)for(int col=0;col<15;col++){GUI.color=new Color(.26f,.48f,.51f,.25f);GUI.Label(new Rect(sea.x+col*57,sea.y+row*53,50,25),"~",ui.title);}GUI.color=c;
            foreach(var route in harbor.routes)
            {
                if(route.hidden&&!harbor.Progress.discovered.Contains(route.id))continue;Vector2 p=new(sea.x+route.mapPosition.x*sea.width,sea.y+route.mapPosition.y*sea.height);
                var home=harbor.routes.First(r=>r.id=="home");Vector2 start=new(sea.x+home.mapPosition.x*sea.width,sea.y+home.mapPosition.y*sea.height);for(int j=1;j<16;j++){var dot=Vector2.Lerp(start,p,j/16f);GUI.color=new Color(.4f,.49f,.42f,.45f);GUI.DrawTexture(new Rect(dot.x,dot.y,4,4),Texture2D.whiteTexture);}GUI.color=c;
                var rect=new Rect(p.x-65,p.y-45,130,90);bool over=rect.Contains(Event.current.mousePosition);float grow=over?4*Mathf.Sin(Time.unscaledTime*4)+5:0;var imageRect=new Rect(rect.x-grow,rect.y-grow,rect.width+grow*2,rect.height+grow*2);if(route.icon)GUI.DrawTexture(imageRect,route.icon,ScaleMode.ScaleToFit);if(GUI.Button(rect,GUIContent.none,GUIStyle.none))selected=route.id;
                GUI.Label(new Rect(p.x-100,p.y+48,200,35),T("island_"+route.id),ui.heading);if(harbor.Permission(route)!=null&&route.id!=harbor.islandId)GUI.Label(new Rect(p.x-90,p.y+80,180,25),T("locked"),ui.small);
            }
            GUI.Label(new Rect(sea.x+25,sea.y+25,100,100),"N\n✦",ui.title);
            var current=harbor.routes.FirstOrDefault(r=>r.id==selected)??harbor.routes[0];float rx=x+910;GUI.Label(new Rect(rx,y+115,330,45),T("island_"+current.id),ui.title);if(current.icon)GUI.DrawTexture(new Rect(rx,y+175,300,190),current.icon,ScaleMode.ScaleToFit);GUI.Label(new Rect(rx,y+385,320,100),T("description_"+current.id),ui.text);var reason=harbor.Permission(current);GUI.Label(new Rect(rx,y+505,320,90),reason==null?T("all_ready"):T(reason),ui.text);GUI.Label(new Rect(rx,y+610,320,35),T("passengers")+": "+harbor.Readiness.Seats.Count+" / 4",ui.heading);GUI.enabled=reason==null&&harbor.Readiness.CanDepart;if(GUI.Button(new Rect(rx,y+675,310,50),T("travel"),ui.button))harbor.RequestTravel(current.id);GUI.enabled=true;
        }
        void OnDestroy(){ui?.Dispose();if(islandTexture)Destroy(islandTexture);}
    }
}
