using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace OceanariumFishing.Editor
{
    public static class TownRuntimeChecks
    {
        static readonly List<string> log=new();
        static void Check(bool ok,string message){log.Add((ok?"PASS: ":"FAIL: ")+message);if(!ok)throw new Exception(message);}
        public static void Run(){if(!EditorApplication.isPlaying)return;log.Clear();GameSession.Current.StartCoroutine(Guard());}
        static IEnumerator Guard(){bool pass=true;var iter=Steps();while(true){bool next;object current=null;try{next=iter.MoveNext();if(next)current=iter.Current;}catch(Exception e){pass=false;log.Add(e.ToString());Debug.LogException(e);break;}if(!next)break;yield return current;}File.WriteAllText("Docs/ASTRA_08_RUNTIME_TESTS.txt",(pass?"PASS ":"FAIL ")+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",log));}
        static IEnumerator Steps()
        {
            var world=UnityEngine.Object.FindFirstObjectByType<TownWorld>();Check(world&&world.Ready,"Town world initialized against existing inventory");Check(world.Actors.Length==12,"Twelve unique town residents instantiated");Check(world.residents.Select(d=>d.id).Distinct().Count()==12,"Stable resident identifiers unique");
            Check(world.GetComponent<TownNavigation>().Ready,"Town navigation grid initialized");foreach(var npc in world.Actors.Where(n=>!n.definition.nightMerchant))Check(world.GetComponent<TownNavigation>().Route(npc.definition.work,npc.definition.social).Count>0,"Walkable route to social area: "+npc.definition.id);
            var inventory=world.Inventory;var originalFish=string.Join("|",inventory.Collection.kept.Select(f=>f.instanceId));string museum=JsonUtility.ToJson(inventory.Collection.aquarium.layout);string travel=JsonUtility.ToJson(inventory.Collection.travel);string appearance=JsonUtility.ToJson(AppearanceStore.Load());
            var oliver=world.Actors.First(n=>n.definition.id=="oliver");world.Player.Teleport(oliver.transform.position+Vector3.back*1.5f);world.Player.SetViewAngles(0,10);yield return new WaitForSeconds(.5f);world.UI.Open(oliver);yield return new WaitForSeconds(.3f);Check(world.UI.IsOpen&&!world.Player.InputEnabled,"Nearby NPC interaction opens dialogue and locks movement");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_08_Oliver.png");
            var data=inventory.Collection;if(data.town.residents.Find(r=>r.id=="oliver")?.talkedDay!=world.Clock.Day)Check(world.Transactions.Talk(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,"oliver",out _),"Daily conversation commits friendship");
            data=inventory.Collection;if(TownTransactions.Count(data,"braided_line")==0){int before=data.aquarium.coins;Check(world.Transactions.Buy(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,"oliver","braided_line",out _),"Buy functional line using earned Coins");Check(inventory.Collection.aquarium.coins==before-8,"Line costs exactly eight Coins");}
            yield return null;Check(inventory.rod.lineStrength>1,"Owned line affects active fishing equipment");
            world.UI.Close();yield return new WaitForSeconds(.2f);Check(world.Player.InputEnabled,"Closing NPC dialogue restores movement");
            data=inventory.Collection;Check(string.Join("|",data.kept.Select(f=>f.instanceId))==originalFish,"Player specimens preserved by shop validation");Check(JsonUtility.ToJson(data.aquarium.layout)==museum&&JsonUtility.ToJson(data.travel)==travel&&JsonUtility.ToJson(AppearanceStore.Load())==appearance,"Museum boat and appearance unchanged");
            var saved=FishCollectionStore.Load();Check(saved.town.revision==data.town.revision&&TownTransactions.Count(saved,"braided_line")>0,"Shop inventory and relationship save roundtrip");
            foreach(var quest in oliver.definition.quests)
            {
                data=inventory.Collection;var state=data.town.quests.Find(q=>q.id==quest.id);if(state?.completed==true)continue;
                if(state==null)Check(world.Transactions.Quest(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,"oliver",quest.id,false,out _),"Accept quest: "+quest.id);
                data=inventory.Collection;int before=data.aquarium.coins;Check(world.Transactions.Quest(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,"oliver",quest.id,true,out _),"Claim completed quest: "+quest.id);Check(inventory.Collection.aquarium.coins==before+quest.coins,"Exact quest reward");data=inventory.Collection;Check(!world.Transactions.Quest(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,"oliver",quest.id,true,out _),"Quest reward cannot repeat");
            }
            world.Player.Teleport(new Vector3(0,3,-10));world.Player.SetViewAngles(0,16);yield return new WaitForSeconds(.6f);ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_08_Town.png");
            var navigation=world.GetComponent<TownNavigation>();Check(navigation.Route(new Vector3(-10,2,-12),new Vector3(10,2,-11)).Count>0,"Cross-town route connects both market sides");
        }
    }
}
