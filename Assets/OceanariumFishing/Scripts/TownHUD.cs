using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    public sealed class TownHUD:MonoBehaviour
    {
        TownWorld world;CozyUI ui;Vector2 scroll;string page="shop",feedback,pending,kind;int revision;TownProductDefinition product;
        public TownResident Resident{get;private set;}public bool IsOpen=>Resident;int closedFrame=-1;public bool BlocksInput=>IsOpen||Time.frameCount==closedFrame;
        string T(string key)=>GameSession.Current?.Languages.Text(key)??key;
        void Awake(){world=GetComponent<TownWorld>();}
        public void Open(TownResident resident){if(!world.CanTrade(resident))return;Resident=resident;page="greeting";feedback=null;pending=null;scroll=Vector2.zero;}
        public void Close(){Resident=null;pending=null;closedFrame=Time.frameCount;}
        string FishLabel(string id){var fish=world.Inventory.Collection.kept.Find(f=>f.instanceId==id);if(fish==null)return T("collection.unknown");var species=world.Inventory.GetComponent<CollectionHUD>().species.FirstOrDefault(s=>s.id==fish.speciesId);return T(species?species.nameKey:"collection.unknown")+" · "+fish.weight.ToString("0.00")+" kg · "+fish.length.ToString("0.0")+" cm · "+Mathf.Max(1,Mathf.RoundToInt(fish.value))+" "+T("town.coins");}
        void Update(){if(!IsOpen)return;if(!world.CanTrade(Resident)||Keyboard.current?.escapeKey.wasPressedThisFrame==true)Close();}
        void Result(bool ok,string error){feedback=T(ok?"town.success":"town.error."+error);pending=null;}
        void Prepare(string action,string id){kind=action;pending=id;revision=world.Inventory.Collection.town.revision;}
        void Confirm()
        {
            if(!world.CanTrade(Resident)){Close();return;}var data=world.Inventory.Collection;string giftId=pending;string command=Guid.NewGuid().ToString("N"),error;bool ok;
            if(kind=="buy")ok=world.Transactions.Buy(data.ownerId,command,revision,Resident.definition.id,pending,out error);
            else if(kind=="sell")ok=world.Transactions.SellFish(data.ownerId,command,revision,Resident.definition.id,pending,out error);
            else ok=world.Transactions.Gift(data.ownerId,command,revision,Resident.definition.id,pending,out error);Result(ok,error);if(ok&&kind=="gift")feedback=T("town.reaction."+(Resident.definition.GiftPoints(giftId)>25?"loved":Resident.definition.GiftPoints(giftId)>5?"liked":Resident.definition.GiftPoints(giftId)<0?"disliked":"neutral"));
        }
        void OnGUI()
        {
            if(!IsOpen||GameSession.Current?.Languages.Ready!=true)return;ui??=new();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float x=(Screen.width/scale-1100)/2;GUI.depth=-30;
            ui.Book(new Rect(x,70,1100,760));var definition=Resident.definition;var data=world.Inventory.Collection;var state=data.town.residents.Find(r=>r.id==definition.id)??new TownResidentState{id=definition.id};
            GUI.Label(new Rect(x+30,90,870,45),T(definition.nameKey)+" · "+T(definition.shopKey),ui.title);
            GUI.Label(new Rect(x+30,145,900,35),T(definition.professionKey)+"  |  "+state.Hearts+" / 10 "+T("town.hearts")+"  |  "+data.aquarium.coins+" "+T("town.coins"),ui.text);
            if(GUI.Button(new Rect(x+965,92,105,40),T("close"),ui.button)){Close();GUI.matrix=old;GUI.depth=0;return;}
            string[] pages={"greeting","shop","gifts","sell","quests"};for(int n=0;n<pages.Length;n++)if(GUI.Button(new Rect(x+30+n*205,195,195,42),T("town."+pages[n]),ui.tab)){page=pages[n];feedback=page=="shop"?T(definition.shopIntroKey):null;scroll=Vector2.zero;pending=null;}
            if(pending!=null)
            {
                GUI.Label(new Rect(x+45,300,1000,100),T("town.confirm")+"\n"+(kind=="buy"?T(product.nameKey)+" · "+world.Transactions.Price(product,state)+" "+T("town."+product.currency):kind=="sell"?FishLabel(pending):T("town.item."+pending)),ui.heading);
                if(GUI.Button(new Rect(x+100,460,400,60),T("town.confirm_button"),ui.button))Confirm();if(GUI.Button(new Rect(x+570,460,400,60),T("town.cancel"),ui.button))pending=null;
            }
            else if(page=="arcade")world.GetComponent<TownActivities>().Draw(x,ui);
            else if(page=="quests")
            {
                float y=275;foreach(var quest in definition.quests){var questState=data.town.quests.Find(q=>q.id==quest.id);GUI.Label(new Rect(x+45,y,990,40),T(quest.titleKey),ui.heading);GUI.Label(new Rect(x+45,y+45,990,65),T(quest.descriptionKey),ui.text);GUI.Label(new Rect(x+45,y+115,600,40),Mathf.Min(quest.amount,TownTransactions.Progress(data,quest))+" / "+quest.amount+" · "+quest.coins+" "+T("town.coins"),ui.text);GUI.enabled=questState?.completed!=true;if(GUI.Button(new Rect(x+700,y+110,330,45),T(questState==null?"town.accept":"town.claim"),ui.button)){bool ok=world.Transactions.Quest(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,definition.id,quest.id,questState!=null,out var error);Result(ok,error);}GUI.enabled=true;y+=205;}
            }
            else if(page=="greeting")
            {
                GUI.Label(new Rect(x+45,275,990,100),T(state.talkedDay<0?definition.firstMeetingKey:state.Hearts>=2?definition.friendKey:definition.greetingKey),ui.heading);GUI.Label(new Rect(x+45,390,990,95),T(definition.biographyKey),ui.text);
                if(definition.id=="rosie"&&GUI.Button(new Rect(x+45,605,985,50),T("town.drink_tea"),ui.button)){bool ok=world.Transactions.DrinkTea(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,definition.id,out var error);Result(ok,error);}
                if(definition.id=="eddie"&&GUI.Button(new Rect(x+45,605,985,50),T("town.arcade_start"),ui.button)){page="arcade";world.GetComponent<TownActivities>().BeginArcade();}
                if(definition.id=="theo")GUI.Label(new Rect(x+45,600,985,110),WorldCalendarHUD.Current?WorldCalendarHUD.Current.ForecastText():T("town.bulletin"),ui.small);
                if(GUI.Button(new Rect(x+45,515,460,60),T("town.chat"),ui.button)){bool ok=world.Transactions.Talk(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,definition.id,out var error);Result(ok,error);}
                if(GUI.Button(new Rect(x+540,515,490,60),T("town.claim_friendship")+" · "+definition.rewardHearts+" "+T("town.hearts")+" · "+T("town.item."+definition.rewardResource),ui.button)){bool ok=world.Transactions.ClaimFriendship(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,definition.id,out var error);Result(ok,error);}
            }
            else
            {
                int count=page=="shop"?definition.products.Length:page=="gifts"?data.resources.Count:data.kept.Count;scroll=GUI.BeginScrollView(new Rect(x+30,260,1040,460),scroll,new Rect(0,0,1000,Mathf.Max(440,count*85)));float y=0;
                if(page=="shop")foreach(var p in definition.products){GUI.Label(new Rect(12,y,700,65),T(p.nameKey)+"\n"+world.Transactions.Price(p,state)+" "+T("town."+p.currency),ui.text);if(GUI.Button(new Rect(740,y+7,235,55),T("town.buy"),ui.button)){product=p;Prepare("buy",p.id);}y+=85;}
                else if(page=="gifts")foreach(var resource in data.resources.Where(r=>r.count>0)){GUI.Label(new Rect(12,y,700,65),T("town.item."+resource.id)+" × "+resource.count+(state.knownPreferences.Contains(resource.id)?" · "+T("town.preference."+(definition.GiftPoints(resource.id)>25?"loved":definition.GiftPoints(resource.id)>5?"liked":definition.GiftPoints(resource.id)<0?"disliked":"neutral")):""),ui.text);if(GUI.Button(new Rect(610,y+7,150,55),T("town.use"),ui.button)){bool ok=world.Transactions.UseItem(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,definition.id,resource.id,out var error);Result(ok,error);if(ok&&resource.id.EndsWith("clue"))feedback=T("town.item."+resource.id+".description");}if(GUI.Button(new Rect(775,y+7,200,55),T("town.give"),ui.button))Prepare("gift",resource.id);y+=85;}
                else if(definition.id=="oliver")foreach(var fish in data.kept.Where(f=>f.ownership==FishOwnership.Stored)){GUI.Label(new Rect(12,y,700,65),FishLabel(fish.instanceId),ui.text);if(GUI.Button(new Rect(740,y+7,235,55),T("town.sell"),ui.button))Prepare("sell",fish.instanceId);y+=85;}
                else GUI.Label(new Rect(20,20,900,100),T("town.fish_market"),ui.text);GUI.EndScrollView();
            }
            if(!string.IsNullOrEmpty(feedback))GUI.Label(new Rect(x+35,745,1020,65),feedback,ui.text);GUI.matrix=old;GUI.depth=0;
        }
        void OnDestroy(){ui?.Dispose();}
    }
}




