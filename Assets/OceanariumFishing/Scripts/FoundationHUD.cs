using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    public sealed class FoundationHUD : MonoBehaviour
    {
        private GameSession session; private LocalPlayerController player; private PlayerInteractor interactor;
        private Destination destination; private bool settings; private string notice; private float noticeUntil;
        private GUIStyle heading,body,small,button; private Vector2 languageScroll;
        public bool SettingsOpen=>settings;
        private CollectionHUD collection;
        public void Bind(GameSession service,LocalPlayerController localPlayer,Destination scene)
        {
            session=service;player=localPlayer;destination=scene;collection=player?player.GetComponent<CollectionHUD>():null;
            if(player!=null){interactor=player.GetComponent<PlayerInteractor>();interactor.Actor.Feedback+=Feedback;}
        }
        private void Feedback(string key){notice=key;noticeUntil=Time.unscaledTime+5;}
        private void OnDestroy(){if(interactor!=null && interactor.Actor!=null)interactor.Actor.Feedback-=Feedback;}
        private void Update()
        {
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true && collection?.IsOpen!=true)settings=!settings;
            if(player!=null)player.InputEnabled=!settings && collection?.IsOpen!=true && session!=null && !session.Loading;
        }
        private string T(string key)=>session.Languages.Text(key);
        private void OnGUI()
        {
            if(session==null || !session.Languages.Ready || collection?.IsOpen==true)return;
            if(heading==null)
            {
                heading=new GUIStyle(GUI.skin.label){fontSize=44,fontStyle=FontStyle.Bold,wordWrap=true};
                body=new GUIStyle(GUI.skin.label){fontSize=20,wordWrap=true};
                small=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};
                button=new GUIStyle(GUI.skin.button){fontSize=18,fixedHeight=44};
            }
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);var matrix=GUI.matrix;
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            if(destination==Destination.MainMenu)
            {
                Panel(new Rect(65,90,500,420));GUILayout.BeginArea(new Rect(95,115,440,375));
                GUILayout.Label(T("brand"),heading);GUILayout.Label(T("tagline"),body);GUILayout.Space(35);
                if(GUILayout.Button(T("play"),button))session.Travel(Destination.CharacterCreator);
                if(GUILayout.Button(T("settings"),button))settings=!settings;
                GUILayout.Space(18);GUILayout.Label(T("milestone"),small);GUILayout.EndArea();
            }
            else if(destination==Destination.HomeIsland)
            {
                Panel(new Rect(28,25,340,115));GUI.Label(new Rect(48,40,300,30),T("home"),body);
                GUI.Label(new Rect(48,77,300,55),T(interactor.Actor.HasStarterRod?"objective.done":"objective"),small);
                Panel(new Rect(28,h-65,w-56,40));GUI.Label(new Rect(42,h-57,w-85,32),T("controls"),small);
                if(interactor.Nearby!=null && !settings){Panel(new Rect(w/2-200,h-130,400,45));GUI.Label(new Rect(w/2-180,h-122,360,35),T(interactor.Nearby.PromptKey),body);}
                if(Time.unscaledTime<noticeUntil){Panel(new Rect(w/2-310,h-240,620,80));GUI.Label(new Rect(w/2-290,h-230,580,65),T(notice),body);}
            }
            else if(destination!=Destination.CharacterCreator)
            {
                Panel(new Rect(50,70,600,260));GUILayout.BeginArea(new Rect(75,95,550,215));GUILayout.Label(T("reserved"),heading);GUILayout.Label(T("reserved.body"),body);
                if(GUILayout.Button(T("back"),button))session.Travel(Destination.MainMenu);GUILayout.EndArea();
            }
            if(settings)
            {
                Panel(new Rect(w-470,30,440,h-115));GUILayout.BeginArea(new Rect(w-445,50,390,h-155));
                GUILayout.Label(T("settings"),body);GUILayout.Label(T("language.note"),small);
                languageScroll=GUILayout.BeginScrollView(languageScroll,GUILayout.Height(player?230:370));
                foreach(var code in LanguageService.Codes)
                {if(GUILayout.Button((code==session.Languages.SelectedCode?"• ":"")+T("locale."+code),button))session.Languages.Select(code);}
                GUI.enabled=true;GUILayout.EndScrollView();
                if(player!=null){GUILayout.Label(T("sensitivity"),small);player.sensitivity=GUILayout.HorizontalSlider(player.sensitivity,.04f,.3f);}
                if(collection!=null)
                {
                    var fishing=player.GetComponent<FishingController>();GUILayout.Label(T("collection.difficulty"),small);
                    foreach(var id in new[]{"cozy","normal","experienced"})
                    {GUI.enabled=!fishing.Active;if(GUILayout.Button((fishing.Collection.difficulty==id?"• ":"")+T("difficulty."+id),button))fishing.SetDifficulty(id);}
                    if(GUILayout.Button(T("collection.journal"),button)){settings=false;collection.Open(true);}GUI.enabled=true;
                }
                if(GUILayout.Button(T("close"),button))settings=false;
                if(destination!=Destination.MainMenu && GUILayout.Button(T("back"),button))session.Travel(Destination.MainMenu);
                GUILayout.EndArea();
            }
            GUI.matrix=matrix;
        }
        private static void Panel(Rect rect){var old=GUI.color;GUI.color=new Color(.055f,.16f,.19f,.93f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    }
}
