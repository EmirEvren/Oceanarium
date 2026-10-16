using System.Collections;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class SceneEntry : MonoBehaviour
    {
        public GameSession servicesPrefab;
        public Destination destination;
        public LocalPlayerController player;
        public FoundationHUD hud;
        private IEnumerator Start()
        {
            if(GameSession.Current==null) Instantiate(servicesPrefab);
            yield return GameSession.Current.Languages.Initialize();
            if(destination==Destination.Boot) GameSession.Current.Travel(Destination.MainMenu);
            else if(hud!=null) hud.Bind(GameSession.Current,player,destination);
        }
    }
}
