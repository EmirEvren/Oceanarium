using UnityEngine;
namespace OceanariumFishing
{
    public sealed class BoatInteraction:MonoBehaviour,IInteractable
    {
        public BoatHarbor harbor;public bool captain;
        public string PromptKey=>captain?"travel.talk":harbor&&harbor.Ready&&harbor.Progress.repaired?"travel.board":"travel.inspect";
        public bool CanInteract(InteractionActor actor)=>harbor&&harbor.Ready&&!harbor.Busy;
        public void Interact(InteractionActor actor){if(!CanInteract(actor))return;if(captain)harbor.Talk();else if(harbor.Progress.repaired)harbor.Board();else harbor.Inspect();}
    }
}
