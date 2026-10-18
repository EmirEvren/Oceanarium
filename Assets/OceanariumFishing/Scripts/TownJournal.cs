using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class TownJournal
    {
        TownCatalog catalog;Vector2 scroll;bool residents;int filter;
        static string T(string key)=>GameSession.Current.Languages.Text(key);
        public void Draw(float x,FishingController inventory,CozyUI ui)
        {
            catalog??=Resources.Load<TownCatalog>("TownContent");if(!catalog)return;var data=inventory.Collection;
            if(GUI.Button(new Rect(x+30,185,240,40),T("town.quests"),ui.tab)){residents=false;scroll=Vector2.zero;}
            if(GUI.Button(new Rect(x+280,185,240,40),T("town.residents"),ui.tab)){residents=true;scroll=Vector2.zero;}
            if(!residents&&GUI.Button(new Rect(x+550,185,300,40),T("town.filter."+filter),ui.tab)){filter=(filter+1)%3;scroll=Vector2.zero;}
            int rows=residents?catalog.residents.Length:catalog.residents.SelectMany(n=>n.quests).Count(q=>{var s=data.town.quests.Find(p=>p.id==q.id);return filter==0?s!=null&&!s.completed:filter==1?s==null:s?.completed==true;});
            scroll=GUI.BeginScrollView(new Rect(x+30,240,1200,545),scroll,new Rect(0,0,1160,Mathf.Max(530,rows*(residents?220:180))));float y=0;
            foreach(var npc in catalog.residents)
            {
                if(residents)
                {
                    if(npc.portrait)GUI.DrawTexture(new Rect(1020,y+65,86,86),npc.portrait);
                    var state=data.town.residents.Find(r=>r.id==npc.id);GUI.Label(new Rect(10,y,1130,35),T(npc.nameKey)+" · "+T(npc.professionKey)+" · "+(state?.Hearts??0)+" / 10 "+T("town.hearts"),ui.heading);
                    GUI.Label(new Rect(10,y+42,1000,60),T(npc.biographyKey),ui.text);
                    GUI.Label(new Rect(10,y+105,1000,45),T("town.known_gifts")+": "+string.Join(", ",(state?.knownPreferences??new()).Select(id=>T("town.item."+id))),ui.small);
                    GUI.Label(new Rect(10,y+155,1100,50),npc.rewardHearts+" "+T("town.hearts")+": "+T("town.item."+npc.rewardResource)+(state?.rewards.Contains(npc.rewardResource)==true?" [✓]":""),ui.small);y+=220;
                }
                else foreach(var quest in npc.quests)
                {
                    var state=data.town.quests.Find(q=>q.id==quest.id);if(filter==0&&(state==null||state.completed)||filter==1&&state!=null||filter==2&&(state==null||!state.completed))continue;
                    GUI.Label(new Rect(10,y,950,35),T(quest.titleKey)+" · "+T(npc.nameKey),ui.heading);GUI.Label(new Rect(10,y+40,1110,65),T(quest.descriptionKey),ui.text);GUI.Label(new Rect(10,y+110,800,35),Mathf.Min(quest.amount,TownTransactions.Progress(data,quest))+" / "+quest.amount+" · "+quest.coins+" "+T("town.coins")+" · "+T("travel.island_town"),ui.small);
                    if(state!=null&&!state.completed&&GUI.Button(new Rect(880,y+105,240,40),T("town.track"),ui.button)){var copy=data.Copy();copy.town.trackedQuest=quest.id;inventory.CommitAquarium(copy);}y+=180;
                }
            }
            GUI.EndScrollView();
        }
    }
}
