using System;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class InteractionActor : MonoBehaviour
    {
        [SerializeField] private string actorId="local-player-1";
        public string ActorId=>actorId;
        public bool HasStarterRod {get;private set;}
        public event Action<string> Feedback;
        public bool TryGiveStarterRod() {if(HasStarterRod)return false;HasStarterRod=true;return true;}
        public void Notify(string key)=>Feedback?.Invoke(key);
    }
    public interface IInteractable
    {
        string PromptKey {get;}
        bool CanInteract(InteractionActor actor);
        void Interact(InteractionActor actor);
    }
}
