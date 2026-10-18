using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static class TownActivityChecks
    {
        static List<string> log=new();static void Check(bool ok,string message){log.Add((ok?"PASS: ":"FAIL: ")+message);if(!ok)throw new Exception(message);}
        public static void Run(){log.Clear();GameSession.Current.StartCoroutine(Guard());}
        static IEnumerator Guard(){var iterator=Steps();bool pass=true;while(true){object value=null;bool next;try{next=iterator.MoveNext();if(next)value=iterator.Current;}catch(Exception e){pass=false;log.Add(e.ToString());Debug.LogException(e);break;}if(!next)break;yield return value;}File.WriteAllText("Docs/ASTRA_08_ACTIVITY_TESTS.txt",(pass?"PASS ":"FAIL ")+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",log));}
        static IEnumerator Steps()
        {
            var world=UnityEngine.Object.FindFirstObjectByType<TownWorld>();Check(world&&world.Ready,"Town activity scene ready");
            void Buy(string npc,string product){var data=world.Inventory.Collection;if(data.town.purchases.Any(p=>p.product==product))return;Check(world.Transactions.Buy(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,npc,product,out var error),"Purchase "+product+": "+error);}
            Buy("lily","tailored_hat");yield return null;yield return null;Check(AppearanceStore.Load().hat=="fisherman_hat"&&world.Player.GetComponentInChildren<CharacterAppearance>().Data.hat=="fisherman_hat","Tailor purchase persists through existing appearance store and updates visible avatar");
            Buy("rosie","island_tea");var data=world.Inventory.Collection;if(TownTransactions.Count(data,"island_tea")>0){Check(world.Transactions.DrinkTea(data.ownerId,Guid.NewGuid().ToString("N"),data.town.revision,"rosie",out _),"Tea consumes owned inventory");Check(world.Inventory.Collection.town.buffSeconds>299,"Five-minute tea buff starts");}
            Buy("iris","camera");world.UI.Close();world.Player.Teleport(new Vector3(0,.85f,-32));world.Player.SetViewAngles(180,12);yield return new WaitForSeconds(.3f);yield return world.GetComponent<TownActivities>().Capture();Check(world.Inventory.Collection.town.flags.Contains("photo:harbor"),"Harbor photograph records subject");string photo=world.Inventory.Collection.town.flags.Last(f=>f.StartsWith("photo-file:")).Substring(11);Check(File.Exists(Path.Combine(Application.persistentDataPath,"Photos",photo+".png")),"Photo PNG exists in persistent photo library");
            var oliver=world.Actors.First(n=>n.definition.id=="oliver");var before=oliver.transform.position;float toBreak=Mathf.Repeat(13.1f-world.Clock.Hour,24)*60;world.Clock.Advance(toBreak,1);log.Add("Schedule test hour: "+world.Clock.Hour+", route nodes: "+world.GetComponent<TownNavigation>().Route(before,oliver.definition.social).Count);yield return new WaitForSeconds(8);Check(Vector3.Distance(before,oliver.transform.position)>1,"Resident physically walks toward scheduled break location");
            Check(world.residents.Sum(r=>r.quests.Length)==24,"Twenty-four functional quest definitions present");world.Inventory.GetComponent<CollectionHUD>().Open(true);yield return new WaitForSeconds(.2f);world.Inventory.GetComponent<CollectionHUD>().Close();Check(world.Player.enabled,"Journal closes without disabling player");
        }
    }
}

