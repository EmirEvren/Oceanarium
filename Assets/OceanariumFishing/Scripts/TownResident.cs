using UnityEngine;
namespace OceanariumFishing
{
    public sealed class TownResident:MonoBehaviour,IInteractable
    {
        public TownResidentDefinition definition;public TownWorld world;CharacterAppearance avatar;
        public string PromptKey=>"town.talk";
        public bool CanInteract(InteractionActor actor)=>world&&world.CanTrade(this)&&!world.UI.IsOpen;
        public void Interact(InteractionActor actor){if(CanInteract(actor))world.UI.Open(this);}
        void Start(){avatar=GetComponent<CharacterAppearance>();avatar.Apply(definition.appearance);}
        void LateUpdate(){if(avatar?.Bones==null)return;avatar.Bones[9].localRotation=Quaternion.Euler(world.UI?.Resident==this?-35:Mathf.Sin(Time.time+transform.position.x)*3,0,5);if(world.UI?.Resident==this){var d=world.Player.transform.position-transform.position;d.y=0;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*5);}}
    }
}
