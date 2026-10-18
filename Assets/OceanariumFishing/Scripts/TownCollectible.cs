using UnityEngine;
namespace OceanariumFishing
{
    public sealed class TownCollectible:MonoBehaviour,IInteractable
    {
        public string id,resource;public int amount=1;TownWorld world;
        public string PromptKey=>"town.collect";
        void Start(){world=FindFirstObjectByType<TownWorld>();}
        void Update(){if(world&&world.Ready&&world.Inventory.Collection.town.flags.Contains("pickup:"+id))gameObject.SetActive(false);}
        public bool CanInteract(InteractionActor actor)=>world&&world.Ready&&!world.Inventory.Collection.town.flags.Contains("pickup:"+id)&&Vector3.Distance(world.Player.transform.position,transform.position)<3;
        public void Interact(InteractionActor actor){if(!CanInteract(actor))return;var data=world.Inventory.Collection.Copy();var stack=data.resources.Find(r=>r.id==resource);if(stack==null){stack=new(){id=resource};data.resources.Add(stack);}stack.count+=amount;data.town.flags.Add("pickup:"+id);data.town.revision++;if(world.Inventory.CommitAquarium(data))gameObject.SetActive(false);}
    }
}
