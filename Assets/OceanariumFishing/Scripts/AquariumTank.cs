using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class AquariumTank:MonoBehaviour,IInteractable
    {
        public string id;public AquariumDefinition definition;public AquariumWorld world;public Transform swimVolume,viewPoint;public GameObject decoration,lockedSign;
        readonly Dictionary<string,AquariumFish> live=new();public IEnumerable<AquariumFish> Fish=>live.Values;
        public string PromptKey=>"aquarium.interact_tank";
        public bool CanInteract(InteractionActor actor)=>world&&world.Ready&&actor.GetComponent<FishingController>()==world.Player;
        public void Interact(InteractionActor actor){if(CanInteract(actor))world.Panel.Open(this);}
        public void Refresh()
        {
            var specimens=world.Display(this).ToArray();var ids=specimens.Select(f=>f.instanceId).ToArray();
            foreach(var id in live.Keys.Where(id=>!ids.Contains(id)).ToArray()){Destroy(live[id].gameObject);live.Remove(id);}
            foreach(var f in specimens)if(!live.ContainsKey(f.instanceId)){var s=world.Species(f.speciesId);if(!s)continue;var go=new GameObject("Specimen "+f.instanceId);go.transform.SetParent(swimVolume,false);var fish=go.AddComponent<AquariumFish>();fish.Initialize(this,f,s,world.fishMaterial);live.Add(f.instanceId,fish);}
            if(decoration)decoration.SetActive(world.Save.tanks.Any(t=>t.id==id&&t.decorated));if(lockedSign)lockedSign.SetActive(!world.Unlocked(this));
        }
    }
}
