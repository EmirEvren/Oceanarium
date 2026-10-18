using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(120)]
    public sealed class AquariumHUD:MonoBehaviour
    {
        public bool IsOpen {get;private set;}public AquariumTank SelectedTank {get;private set;}
        AquariumWorld world;CozyUI ui;Vector2 scroll;string selected,message;int tab;float opened;
        string T(string k)=>GameSession.Current?.Languages.Text("aquarium."+k)??k;
        string L(string k)=>GameSession.Current?.Languages.Text(k)??k;
        void Awake()=>world=GetComponent<AquariumWorld>();
        public void Open(AquariumTank tank){if(!world.Ready||world.Player.Active||world.Building?.UI?.IsOpen==true)return;world.Player.GetComponent<CollectionHUD>().Close();SelectedTank=tank;IsOpen=true;tab=tank?0:2;selected=null;message=null;scroll=Vector2.zero;opened=Time.unscaledTime;world.Player.GetComponent<LocalPlayerController>().InputEnabled=false;}
        public void Close()=>IsOpen=false;
        void Update(){if(IsOpen&&Keyboard.current?.escapeKey.wasPressedThisFrame==true)Close();}
        void OnGUI()
        {
            if(!IsOpen||!world.Ready||GameSession.Current?.Languages.Ready!=true)return;ui??=new CozyUI();var matrix=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-25;GUI.color=new Color(1,1,1,Mathf.Clamp01((Time.unscaledTime-opened)*8));float x=(Screen.width/scale-1240)/2;
            ui.Book(new Rect(x,60,1240,780));GUI.Label(new Rect(x+30,82,950,45),T("title")+(SelectedTank?" · "+T(SelectedTank.id):""),ui.title);
            if(GUI.Button(new Rect(x+1090,80,115,42),L("close"),ui.button))Close();
            for(int i=0;i<3;i++){GUI.enabled=i==2||SelectedTank;if(GUI.Button(new Rect(x+30+i*240,142,230,42),T(new[]{"fish","decorations","reception"}[i]),ui.tab)){tab=i;scroll=Vector2.zero;}}GUI.enabled=true;
            GUI.Label(new Rect(x+790,145,410,40),T("coins")+": "+world.Save.coins+"   ·   "+T("popularity")+": "+world.Score.Total.ToString("0"),ui.heading);
            if(tab==0&&SelectedTank)FishPanel(x);else if(tab==1&&SelectedTank)DecorPanel(x);else Reception(x);
            if(!string.IsNullOrEmpty(message))GUI.Label(new Rect(x+32,787,1160,40),T(message),ui.text);GUI.matrix=matrix;GUI.color=color;GUI.depth=0;
        }
        void FishPanel(float x)
        {
            var tank=SelectedTank;int count=world.Display(tank).Count();GUI.Label(new Rect(x+30,205,1150,42),T(tank.definition.size.ToString())+" · "+T("capacity")+" "+count+" / "+tank.definition.capacity+" · "+string.Join(" / ",tank.definition.habitats.Select(h=>T(h.ToString()))),ui.heading);
            var list=world.Data.kept.Where(f=>f.ownership==FishOwnership.Stored||f.tankId==tank.id).OrderByDescending(f=>f.tankId==tank.id).ThenBy(f=>f.speciesId).ToArray();
            scroll=GUI.BeginScrollView(new Rect(x+30,260,650,500),scroll,new Rect(0,0,625,Mathf.Max(490,list.Length*97)));
            for(int i=0;i<list.Length;i++)
            {var f=list[i];var s=world.Species(f.speciesId);float y=i*97;if(selected==f.instanceId)ui.Panel(new Rect(0,y,615,90),ui.highlight);if(GUI.Button(new Rect(0,y,615,90),GUIContent.none,GUIStyle.none))selected=f.instanceId;if(s?.icon)GUI.DrawTexture(new Rect(5,y+8,100,68),s.icon,ScaleMode.ScaleToFit);GUI.Label(new Rect(115,y+5,480,30),s?L(s.nameKey):f.speciesId,ui.heading);string reason=f.ownership==FishOwnership.Displayed?"in_tank":world.Validate(f,tank)??"compatible";GUI.Label(new Rect(115,y+42,480,42),f.length.ToString("0.0")+" cm · "+f.weight.ToString("0.00")+" kg · "+T(reason),ui.small);}
            if(list.Length==0)GUI.Label(new Rect(15,35,570,130),T("empty_storage"),ui.text);GUI.EndScrollView();
            var item=list.FirstOrDefault(f=>f.instanceId==selected)??list.FirstOrDefault();if(item==null)return;selected=item.instanceId;var species=world.Species(item.speciesId);
            if(species?.icon)GUI.DrawTexture(new Rect(x+760,260,320,150),species.icon,ScaleMode.ScaleToFit);
            GUI.Label(new Rect(x+725,420,470,38),species?L(species.nameKey):item.speciesId,ui.title);
            GUI.Label(new Rect(x+725,466,470,240),string.Format(L("fishing.weight"),item.weight)+"\n"+string.Format(L("fishing.length"),item.length)+"\n"+L("fishing.rarity."+item.rarity.ToString().ToLowerInvariant())+" · "+L("collection.quality")+": "+(item.quality*100).ToString("0")+"%\n"+L("collection.location")+": "+item.location+"\n"+T("tank")+": "+(item.ownership==FishOwnership.Displayed?T(item.tankId):T("storage"))+"\n"+T("contribution")+": "+world.Contribution(item).ToString("0.0"),ui.text);
            bool displayed=item.ownership==FishOwnership.Displayed;GUI.enabled=displayed||world.Validate(item,tank)==null;
            if(GUI.Button(new Rect(x+725,712,470,50),T(displayed?"remove":"add"),ui.button)){bool ok=displayed?world.Remove(world.Data.ownerId,item.instanceId,out message):world.Place(world.Data.ownerId,item.instanceId,tank.id,out message);if(ok)message=displayed?"removed":"placed";}GUI.enabled=true;
        }
        void DecorPanel(float x)
        {GUI.Label(new Rect(x+40,220,1150,60),T("decor_note"),ui.heading);GUI.Label(new Rect(x+40,310,1100,170),T("decor_detail")+"\n\n"+T("future_large"),ui.text);if(GUI.Button(new Rect(x+40,560,450,50),L("build.open"),ui.button)){Close();world.Building?.UI.Open();if(SelectedTank)world.Building?.UI.FocusTank(SelectedTank.id);}}
        void Reception(float x)
        {
            var save=world.Save;var score=world.Score;GUI.Label(new Rect(x+35,210,570,45),T("rank."+world.Rank),ui.title);
            float next=world.Rank+1<world.economy.ranks.Length?world.economy.ranks[world.Rank+1]:score.Total;GUI.Label(new Rect(x+35,260,560,38),T("popularity")+": "+score.Total.ToString("0.0")+" / "+next.ToString("0"),ui.heading);
            GUI.Label(new Rect(x+35,305,560,210),T("diversity")+": "+score.diversity.ToString("0")+"\n"+T("rarity_quality")+": "+(score.rarity+score.quality).ToString("0.0")+"\n"+T("tank_habitat")+": "+(score.variety+score.habitat).ToString("0")+"\n"+T("decor_upgrades")+": "+(score.decor+score.upgrades).ToString("0")+"\n"+T("visitors")+": "+save.visitors+" · "+T("present")+": "+world.ActiveVisitors,ui.text);
            GUI.Label(new Rect(x+35,530,560,100),T("donations")+": "+save.pending+" "+T("coins")+"\n"+T("collected")+": "+save.totalCollected,ui.heading);
            GUI.enabled=save.pending>0;if(GUI.Button(new Rect(x+35,640,540,48),T("collect"),ui.button)){if(world.Collect(out message))message="collected_ok";}GUI.enabled=true;
            GUI.Label(new Rect(x+35,704,555,64),T("recent")+": "+string.Join(" · ",save.transactions.Where(t=>t.kind=="donation").TakeLast(3).Select(t=>"+"+t.amount)),ui.small);
            GUI.Label(new Rect(x+650,210,530,45),T("upgrades"),ui.title);
            for(int i=0;i<world.economy.upgrades.Length;i++){var u=world.economy.upgrades[i];float y=270+i*119;GUI.Label(new Rect(x+650,y,510,30),T("upgrade."+u.id),ui.heading);GUI.Label(new Rect(x+650,y+32,510,38),T("effect."+u.id),ui.small);bool owned=save.upgrades.Contains(u.id);GUI.enabled=!owned;if(GUI.Button(new Rect(x+650,y+72,510,37),owned?T("owned"):u.price+" "+T("coins")+" · "+T("buy"),ui.button)){if(world.Purchase(u.id,out message))message="purchased";}GUI.enabled=true;}
        }
        void OnDestroy(){ui?.Dispose();}
    }
}

