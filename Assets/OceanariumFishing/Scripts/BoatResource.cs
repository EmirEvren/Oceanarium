using UnityEngine;
namespace OceanariumFishing
{
    public sealed class BoatResource:MonoBehaviour,IInteractable
    {
        public string id,resource;public int amount=1;BoatHarbor harbor;
        public string PromptKey=>"travel.pickup_"+resource;
        void Start(){harbor=FindFirstObjectByType<BoatHarbor>();Refresh();}
        public void Refresh(){if(!harbor)harbor=FindFirstObjectByType<BoatHarbor>();if(harbor&&harbor.Ready)gameObject.SetActive(!harbor.Progress.collected.Contains(id));}
        public bool CanInteract(InteractionActor actor)=>harbor&&harbor.Ready&&!harbor.Busy;
        public void Interact(InteractionActor actor){if(CanInteract(actor))harbor.Pickup(id,resource,amount);}
    }
}
