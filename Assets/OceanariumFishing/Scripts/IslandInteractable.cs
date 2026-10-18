using UnityEngine;
namespace OceanariumFishing
{
    public enum InteractionKind { Cabin, Chest, StarterRod, AquariumPlot }
    public sealed class IslandInteractable : MonoBehaviour,IInteractable
    {
        public InteractionKind kind;
        public string stableId;
        public GameObject pickupVisual;
        [SerializeField] private bool claimed;
        public string PromptKey=>"interact."+kind.ToString().ToLowerInvariant();
        public bool CanInteract(InteractionActor actor)=>!claimed && actor!=null;
        public void Interact(InteractionActor actor)
        {
            if(!CanInteract(actor))return;
            if(kind==InteractionKind.Cabin&&WorldCalendarHUD.Current){WorldCalendarHUD.Current.OpenSleep(this);return;}
            if(kind==InteractionKind.StarterRod)
            {
                if(!actor.TryGiveStarterRod()){actor.Notify("notice.owned");return;}
                claimed=true;if(pickupVisual!=null)pickupVisual.SetActive(false);
            }
            actor.Notify("notice."+kind.ToString().ToLowerInvariant());
        }
    }
}
