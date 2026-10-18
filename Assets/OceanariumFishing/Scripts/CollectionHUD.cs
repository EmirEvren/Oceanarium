using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(100)]
    public sealed class CollectionHUD:MonoBehaviour
    {
        public FishingController fishing;public FishSpecies[] species;
        public bool IsOpen {get;private set;}public bool Journal {get;private set;}
        public string SelectedInstanceId {get;private set;}public string SelectedSpeciesId {get;private set;}
        CozyUI ui;Vector2 scroll;string sort="date",island="";int rarity=-1,page;bool records;float opened;
        static readonly string[] sorts={"date","weight","rarity","species"};
        bool townPage;TownJournal townJournal=new();
        string T(string key)=>GameSession.Current?.Languages.Text(key)??"";
        FishCollection Data=>fishing.Collection;
        public void Open(bool journal)
        {
            if(FindFirstObjectByType<AquariumHUD>()?.IsOpen==true||FindFirstObjectByType<AquariumBuildHUD>()?.IsOpen==true)return;
            if(FindFirstObjectByType<TownHUD>()?.IsOpen==true)return;
            if(fishing.Active||FindFirstObjectByType<BoatHarbor>()?.InputLocked==true)return;Journal=journal;IsOpen=true;records=false;page=0;scroll=Vector2.zero;opened=Time.unscaledTime;
            if(SelectedInstanceId==null)SelectedInstanceId=Data.kept.FirstOrDefault()?.instanceId;
        }
        public void Close()=>IsOpen=false;
        public void SelectFish(string id){SelectedInstanceId=id;}
        public void SelectSpecies(string id){SelectedSpeciesId=id;}
        void Update()
        {
            var keys=Keyboard.current;if(keys==null)return;
            if(keys.escapeKey.wasPressedThisFrame&&IsOpen){Close();return;}
            if(keys.iKey.wasPressedThisFrame){if(IsOpen&&!Journal)Close();else Open(false);}
            if(keys.jKey.wasPressedThisFrame){if(IsOpen&&Journal)Close();else Open(true);}
        }
        void OnGUI()
        {
            if(!IsOpen||GameSession.Current?.Languages.Ready!=true)return;ui??=new CozyUI();var old=GUI.matrix;var oldColor=GUI.color;
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale;
            GUI.depth=-20;GUI.color=new Color(1,1,1,Mathf.Clamp01((Time.unscaledTime-opened)*8));float x=(w-1280)/2;
            ui.Book(new Rect(x,55,1280,790));GUI.Label(new Rect(x+32,73,1000,45),T(Journal?"collection.journal":"collection.inventory"),ui.title);
            if(GUI.Button(new Rect(x+1140,75,108,43),T("close"),ui.button))Close();
            if(GUI.Button(new Rect(x+30,130,180,42),T("collection.inventory"),ui.tab)){Journal=false;records=false;scroll=Vector2.zero;}
            if(GUI.Button(new Rect(x+220,130,210,42),T("collection.journal"),ui.tab)){Journal=true;records=false;scroll=Vector2.zero;}
            if(GUI.Button(new Rect(x+440,130,170,42),T("collection.records"),ui.tab)){records=true;scroll=Vector2.zero;}
            if(GUI.Button(new Rect(x+630,130,240,42),T("town.residents")+" / "+T("town.quests"),ui.tab)){townPage=!townPage;}
            if(!string.IsNullOrEmpty(Data.loadWarning))GUI.Label(new Rect(x+30,180,1200,60),T(Data.loadWarning),ui.text);
            else if(townPage)townJournal.Draw(x,fishing,ui);
            else if(records)Records(x);
            else {Filters(x);if(Journal)JournalView(x);else Inventory(x);}
            GUI.Label(new Rect(x+32,807,1200,28),Data.resources.Count>0?T("travel.resource_inventory")+": "+string.Join(" · ",Data.resources.Select(r=>T("travel."+r.id)+" "+r.count)):T("collection.shortcuts"),ui.small);GUI.color=oldColor;GUI.matrix=old;GUI.depth=0;
        }
        void Filters(float x)
        {
            if(!Journal)
            {
                GUI.Label(new Rect(x+30,188,75,35),T("collection.sort"),ui.text);
                for(int i=0;i<sorts.Length;i++)if(GUI.Button(new Rect(x+110+i*136,183,130,40),(sort==sorts[i]?"• ":"")+T("collection.sort."+sorts[i]),ui.tab)){sort=sorts[i];scroll=Vector2.zero;}
                if(GUI.Button(new Rect(x+675,183,250,40),T("collection.rarity")+": "+(rarity<0?T("collection.all"):T("fishing.rarity."+((FishRarity)rarity).ToString().ToLowerInvariant())),ui.tab)){rarity=rarity>=4?-1:rarity+1;scroll=Vector2.zero;}
            }
            else GUI.Label(new Rect(x+30,185,550,36),string.Format(T("collection.discovered_count"),species.Count(s=>Data.Discovered(s.id)),species.Length),ui.heading);
            // Only actual configured and unlocked islands are exposed; no future destinations leak.
            var islands=species.SelectMany(s=>s.islands??Array.Empty<string>()).Where(i=>Data.unlockedIslands.Contains(i)).Distinct().OrderBy(i=>i).ToArray();
            if(GUI.Button(new Rect(x+940,183,305,40),T("collection.island")+": "+(island==""?T("collection.all"):T("island."+island)),ui.tab))
            {int i=Array.IndexOf(islands,island);island=i+1<islands.Length?islands[i+1]:"";scroll=Vector2.zero;page=0;}
        }
        void Inventory(float x)
        {
            var list=Data.Query(sort,island,rarity).Where(f=>f.ownership==FishOwnership.Stored).ToArray();GUI.Label(new Rect(x+30,236,700,35),T("collection.fish")+" · "+list.Length,ui.heading);
            scroll=GUI.BeginScrollView(new Rect(x+30,280,685,500),scroll,new Rect(0,0,660,Mathf.Max(495,list.Length*86)));
            for(int i=0;i<list.Length;i++)
            {
                var f=list[i];var s=Find(f.speciesId);float y=i*86;
                if(f.instanceId==SelectedInstanceId)ui.Panel(new Rect(0,y,650,80),ui.highlight);
                if(GUI.Button(new Rect(0,y,650,80),GUIContent.none,GUIStyle.none))SelectedInstanceId=f.instanceId;
                if(s?.icon)GUI.DrawTexture(new Rect(8,y+6,110,66),s.icon,ScaleMode.ScaleToFit);
                GUI.Label(new Rect(130,y+8,500,27),T(s?s.nameKey:"collection.unknown"),ui.heading);
                GUI.Label(new Rect(130,y+42,500,30),string.Format(T("fishing.weight"),f.weight)+" · "+T("fishing.rarity."+f.rarity.ToString().ToLowerInvariant())+" · "+Data.kept.Count(k=>k.speciesId==f.speciesId)+" "+T("collection.specimens"),ui.small);
            }
            if(list.Length==0)GUI.Label(new Rect(20,40,600,100),T("collection.empty"),ui.text);GUI.EndScrollView();
            var selected=list.FirstOrDefault(f=>f.instanceId==SelectedInstanceId)??list.FirstOrDefault();if(selected!=null){SelectedInstanceId=selected.instanceId;Specimen(x+750,selected);}
        }
        void Specimen(float x,FishInstance f)
        {
            var s=Find(f.speciesId);if(s?.icon)GUI.DrawTexture(new Rect(x+45,245,365,180),s.icon,ScaleMode.ScaleToFit);
            GUI.Label(new Rect(x,430,470,40),T(s?s.nameKey:"collection.unknown"),ui.title);
            GUI.Label(new Rect(x,478,470,270),T("fishing.rarity."+f.rarity.ToString().ToLowerInvariant())+"\n"+string.Format(T("fishing.weight"),f.weight)+"\n"+string.Format(T("fishing.length"),f.length)+"\n"+T("collection.quality")+": "+Mathf.RoundToInt(f.quality*100)+"%\n"+T("collection.location")+": "+Location(f)+"\n"+T("collection.date")+": "+Date(f.caughtUtc)+"\n"+T("collection.value")+": "+(s?FishValuation.Estimate(s,f):f.value).ToString("0.00"),ui.text);
            var record=Data.Record(f.speciesId);if(record!=null&&(f.weight>=record.weight||f.length>=record.length))GUI.Label(new Rect(x,747,470,42),T("collection.personal_best"),ui.heading);
        }
        void JournalView(float x)
        {
            var list=species.Where(s=>(string.IsNullOrEmpty(island)||Array.IndexOf(s.islands,island)>=0)&&s.islands.Any(i=>Data.unlockedIslands.Contains(i))).ToArray();
            int pages=Mathf.Max(1,Mathf.CeilToInt(list.Length/6f));page=Mathf.Clamp(page,0,pages-1);
            for(int i=0;i<6&&page*6+i<list.Length;i++)
            {
                var s=list[page*6+i];bool known=Data.Discovered(s.id);float cx=x+30+(i%3)*222,cy=248+(i/3)*232;
                ui.Panel(new Rect(cx,cy,210,220),SelectedSpeciesId==s.id?ui.highlight:ui.paper);
                if(GUI.Button(new Rect(cx,cy,210,220),GUIContent.none,GUIStyle.none)){SelectedSpeciesId=s.id;}
                GUI.DrawTexture(new Rect(cx+8,cy+16,194,125),known?s.icon:s.silhouette,ScaleMode.ScaleToFit);
                GUI.Label(new Rect(cx+12,cy+145,185,65),known?T(s.nameKey):"???",ui.heading);
                if(!known)GUI.Label(new Rect(cx+82,cy+65,40,40),"?",ui.question);
            }
            if(GUI.Button(new Rect(x+30,735,145,42),T("collection.previous"),ui.tab))page=Mathf.Max(0,page-1);
            GUI.Label(new Rect(x+295,742,150,32),(page+1)+" / "+pages,ui.text);
            if(GUI.Button(new Rect(x+550,735,145,42),T("collection.next"),ui.tab))page=Mathf.Min(pages-1,page+1);
            var chosen=list.FirstOrDefault(s=>s.id==SelectedSpeciesId)??list.FirstOrDefault();if(chosen)SpeciesDetail(x+750,chosen);
        }
        void SpeciesDetail(float x,FishSpecies s)
        {
            bool known=Data.Discovered(s.id);GUI.DrawTexture(new Rect(x+45,240,360,175),known?s.icon:s.silhouette,ScaleMode.ScaleToFit);
            GUI.Label(new Rect(x,420,470,48),known?T(s.nameKey):"???",ui.title);
            if(!known){GUI.Label(new Rect(x,490,460,160),T("collection.locked"),ui.text);return;}
            var r=Data.Record(s.id);
            string body=T("fishing.rarity."+s.rarity.ToString().ToLowerInvariant())+" · "+T(s.encyclopediaKey)+"\n\n"+T("collection.habitat")+": "+string.Join(", ",s.habitats.Select(h=>T("habitat."+h.ToString().ToLowerInvariant())))+"\n"+T("collection.islands")+": "+string.Join(", ",r.islands.Select(i=>T("island."+i)))+"\n"+T("collection.weather")+": "+string.Join(", ",s.weather.Select(v=>T("weather."+v)))+"\n"+T("collection.time")+": "+s.startHour.ToString("00")+":00–"+s.endHour.ToString("00")+":00\n"+T("collection.caught")+": "+r.count+(r.incompleteLegacyHistory?"+":"")+"\n"+T("collection.heaviest")+": "+r.weight.ToString("0.00")+" kg · "+T("collection.longest")+": "+r.length.ToString("0.0")+" cm\n"+T("collection.first")+": "+Date(r.firstCaughtUtc);
            GUI.Label(new Rect(x,475,460,285),body,ui.small);if(r.incompleteLegacyHistory)GUI.Label(new Rect(x,765,460,36),T("collection.legacy"),ui.small);
        }
        void Records(float x)
        {
            var heaviest=Data.records.OrderByDescending(r=>r.weight).FirstOrDefault();var longest=Data.records.OrderByDescending(r=>r.length).FirstOrDefault();var common=Data.records.OrderByDescending(r=>r.count).FirstOrDefault();
            var rare=species.Where(s=>Data.Discovered(s.id)).OrderByDescending(s=>s.rarity).FirstOrDefault();
            string Name(string id){var s=Find(id);return s?T(s.nameKey):T("collection.unknown");}
            string body=T("collection.total")+": "+Data.TotalCaught+"\n\n"+string.Format(T("collection.discovered_count"),Data.DiscoveredCount,species.Length)+"\n\n"+T("collection.heaviest")+": "+(heaviest!=null?Name(heaviest.speciesId)+" · "+heaviest.weight.ToString("0.00")+" kg":"—")+"\n\n"+T("collection.longest")+": "+(longest!=null?Name(longest.speciesId)+" · "+longest.length.ToString("0.0")+" cm":"—")+"\n\n"+T("collection.most_common")+": "+(common!=null?Name(common.speciesId)+" · "+common.count:"—")+"\n\n"+T("collection.rarest")+": "+(rare?T(rare.nameKey):"—");
            GUI.Label(new Rect(x+40,205,780,560),body,ui.heading);
            var perIsland=Data.history.GroupBy(f=>f.islandId).OrderBy(g=>g.Key);float y=220;foreach(var group in perIsland){GUI.Label(new Rect(x+855,y,350,80),T("island."+group.Key)+"\n"+group.Count()+" "+T("collection.caught"),ui.text);y+=100;}
            if(Data.records.Any(r=>r.incompleteLegacyHistory))GUI.Label(new Rect(x+40,765,1150,35),T("collection.legacy"),ui.small);
        }
        FishSpecies Find(string id)=>Array.Find(species,s=>s.id==id);
        string Location(FishInstance f)=>T("island."+f.islandId)+" / "+T("zone."+f.zoneId);
        string Date(string utc)=>DateTime.TryParse(utc,null,System.Globalization.DateTimeStyles.RoundtripKind,out var d)?d.ToLocalTime().ToString("g"):T("collection.unknown");
        void OnDestroy()=>ui?.Dispose();
    }
}





