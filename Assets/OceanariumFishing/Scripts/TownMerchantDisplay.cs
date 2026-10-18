using UnityEngine;
namespace OceanariumFishing
{
    public sealed class TownMerchantDisplay:MonoBehaviour
    {
        public TownWorld world;public TownResidentDefinition resident;Renderer[] visuals;Collider[] colliders;bool visible=true;
        void Start(){visuals=GetComponentsInChildren<Renderer>();colliders=GetComponentsInChildren<Collider>();}
        void Update(){if(!world.Ready)return;bool value=resident.Available(world.Clock);if(value==visible)return;visible=value;foreach(var r in visuals)r.enabled=value;foreach(var c in colliders)c.enabled=value;}
    }
}
