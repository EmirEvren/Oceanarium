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
        public event Action<Destination> TransitionCompleted;
        private void Awake()
        {
            if(Current!=null && Current!=this) {Destroy(gameObject);return;}
            Current=this; DontDestroyOnLoad(gameObject);
            Languages=GetComponent<LanguageService>();
        }
        public void Travel(Destination destination)
        {
            if(!Loading) StartCoroutine(Load(destination));
        }
        private IEnumerator Load(Destination destination)
        {
            Loading=true;
            yield return SceneManager.LoadSceneAsync(catalog.Resolve(destination));
            Loading=false;
            TransitionCompleted?.Invoke(destination);
        }
        private void OnDestroy() {if(Current==this) Current=null;}
    }
}
