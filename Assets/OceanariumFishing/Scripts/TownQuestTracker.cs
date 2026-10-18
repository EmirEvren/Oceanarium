using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class TownQuestTracker:MonoBehaviour
    {
        FishingController inventory;TownCatalog catalog;CozyUI ui;
        void Start(){inventory=GetComponent<FishingController>();catalog=Resources.Load<TownCatalog>("TownContent");}
        void OnGUI(){if(!inventory||!catalog||string.IsNullOrEmpty(inventory.Collection.town.trackedQuest)||!GetComponent<LocalPlayerController>().InputEnabled||GameSession.Current?.Languages.Ready!=true)return;var quest=catalog.residents.SelectMany(n=>n.quests).FirstOrDefault(q=>q.id==inventory.Collection.town.trackedQuest);if(!quest)return;ui??=new();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float x=Screen.width/scale-350;ui.Panel(new Rect(x,145,325,95));GUI.Label(new Rect(x+12,153,300,44),GameSession.Current.Languages.Text(quest.titleKey),ui.small);GUI.Label(new Rect(x+12,202,300,27),Mathf.Min(quest.amount,TownTransactions.Progress(inventory.Collection,quest))+" / "+quest.amount,ui.small);GUI.matrix=old;}
        void OnDestroy(){ui?.Dispose();}
    }
}
