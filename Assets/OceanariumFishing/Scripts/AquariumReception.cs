using UnityEngine;
namespace OceanariumFishing
{
    public sealed class AquariumReception:MonoBehaviour,IInteractable
    {
        public AquariumWorld world;public string PromptKey=>"aquarium.interact_reception";
        public bool CanInteract(InteractionActor actor)=>world&&world.Ready&&actor.GetComponent<FishingController>()==world.Player;
        public void Interact(InteractionActor actor){if(CanInteract(actor))world.Panel.Open(null);}
    }
}
