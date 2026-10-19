using System.Collections;
using UnityEngine;
namespace OceanariumFishing
{
    [DefaultExecutionOrder(-500)]
    public sealed class SceneEntry : MonoBehaviour
    {
        public GameSession servicesPrefab;
        public Destination destination;
        public LocalPlayerController player;
        public FoundationHUD hud;
        void Awake()
        {
            if(NetworkSession.Current?.Online!=true||!player)return;player.gameObject.SetActive(false);
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(camera.gameObject.scene==gameObject.scene)camera.enabled=false;
            foreach(var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))if(listener.gameObject.scene==gameObject.scene)listener.enabled=false;
            foreach(var camera in FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None))if(camera.gameObject.scene==gameObject.scene)camera.enabled=false;
        }
        private IEnumerator Start()
        {
            if(GameSession.Current==null) Instantiate(servicesPrefab);
            yield return GameSession.Current.Languages.Initialize();
            if(NetworkSession.Current?.Online==true&&destination!=Destination.MainMenu)
            {while(!GamePlayers.Local)yield return null;player=GamePlayers.Local;}
            else if(player&&WorldPersistence.Local!=null)
            {var record=WorldPersistence.Local;player.GetComponent<InteractionActor>().RestoreRod(record.starterRod);if(record.location.valid&&record.location.island==UnityEngine.SceneManagement.SceneManager.GetActiveScene().name&&NetworkSpawn.Valid(record.location.position,out var position))player.Teleport(position);}
            if(destination==Destination.Boot) GameSession.Current.Travel(Destination.MainMenu);
            else if(hud!=null) hud.Bind(GameSession.Current,player,destination);
        }
    }
}
