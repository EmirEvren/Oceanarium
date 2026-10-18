using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OceanariumFishing
{
    // Session-scoped services; this is not a global player reference.
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Current { get; private set; }
        public SceneCatalog catalog;
        public LanguageService Languages { get; private set; }
        public bool Loading { get; private set; }
        public event Action<Destination> TransitionCompleted;public event Action<Destination,string> TransitionFailed;
        private void Awake()
        {
            if(Current!=null && Current!=this) {Destroy(gameObject);return;}
            Current=this; DontDestroyOnLoad(gameObject);
            Languages=GetComponent<LanguageService>();
        }
        public void Travel(Destination destination)
        {
            if(Loading)return; if(destination==Destination.TownIsland){var data=FindFirstObjectByType<FishingController>()?.Collection??FishCollectionStore.Load();if(!data.travel.repaired){TransitionFailed?.Invoke(destination,"repair_required");return;}} if(destination==Destination.ExplorationIsland01||destination==Destination.ExplorationIsland02||destination==Destination.ExplorationIsland03){TransitionFailed?.Invoke(destination,"locked");return;} StartCoroutine(Load(destination));
        }
        private IEnumerator Load(Destination destination)
        {
            WorldTimeService.Current?.Flush();Loading=true;
            AsyncOperation operation=null;string error=null;try{string path=catalog.Resolve(destination);if(!Application.CanStreamedLevelBeLoaded(path))error="unavailable";else operation=SceneManager.LoadSceneAsync(path);}catch(Exception e){error=e.Message;}if(operation==null){Loading=false;TransitionFailed?.Invoke(destination,error);yield break;}yield return operation;
            Loading=false;
            TransitionCompleted?.Invoke(destination);
        }
        private void OnDestroy() {if(Current==this) Current=null;}
    }
}

