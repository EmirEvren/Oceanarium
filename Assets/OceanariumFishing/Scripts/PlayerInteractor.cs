using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    [RequireComponent(typeof(InteractionActor))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public LocalPlayerController controller;
        public float range=2.1f;
        public IInteractable Nearby {get;private set;}
        public InteractionActor Actor {get;private set;}
        private readonly Collider[] hits=new Collider[32];
        private void Awake()=>Actor=GetComponent<InteractionActor>();
        private void Update()
        {
            Nearby=null;if(!controller.InputEnabled)return;
            int count=Physics.OverlapSphereNonAlloc(transform.position+Vector3.up*.7f,range,hits,~0,QueryTriggerInteraction.Collide);
            float closest=float.MaxValue;
            for(int i=0;i<count;i++)
            {
                var candidate=hits[i].GetComponentInParent<IInteractable>();
                if(candidate==null || !candidate.CanInteract(Actor))continue;
                float distance=(hits[i].ClosestPoint(transform.position)-transform.position).sqrMagnitude;
                if(distance<closest){closest=distance;Nearby=candidate;}
            }
            if(Keyboard.current?.eKey.wasPressedThisFrame==true) Nearby?.Interact(Actor);
        }
    }
}
