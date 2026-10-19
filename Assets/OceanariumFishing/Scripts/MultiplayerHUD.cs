using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace OceanariumFishing
{
    public sealed class MultiplayerHUD:MonoBehaviour
    {
        CozyUI ui;List<WorldSlot> worlds=new();Vector2 scroll;string panel="",selected,worldNameInput="";bool friendsOnly=true,importLegacy,unreadable,diagnostics;
        NetworkSession Hub=>NetworkSession.Current;string T(string key)=>GameSession.Current.Languages.Text(key);
        public void RefreshWorlds(){worlds=WorldSlotStore.List(out unreadable);selected=worlds.Count>0?worlds[0].worldId:null;importLegacy=worlds.Count==0&&System.IO.File.Exists(FishCollectionStore.LegacyPath);}
        void Update(){if(Keyboard.current?.f8Key.wasPressedThisFrame==true)diagnostics=!diagnostics;}
        void OnGUI()
        {
            if(GameSession.Current?.Languages.Ready!=true)return;ui??=new();var before=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale;
            bool menu=SceneManager.GetActiveScene().name=="OF_MainMenu";
            if(menu)
            {
                ui.Book(new Rect(60,80,440,640));GUILayout.BeginArea(new Rect(90,110,380,580));GUILayout.Label(T("brand"),ui.title);GUILayout.Label(T("tagline"),ui.text);GUILayout.Space(28);
                GUI.enabled=!Hub.Busy&&!Hub.Online;
                if(Button("net.solo")){panel="solo";RefreshWorlds();}if(Button("net.host")){panel="host";RefreshWorlds();}if(Button("net.join")){panel="join";Hub.Steam.Browse();}
                GUI.enabled=Hub.Steam.Lobby.IsValid();if(Button("net.invite"))Hub.Steam.Invite();GUI.enabled=true;
                if(Button("settings"))FindFirstObjectByType<FoundationHUD>()?.ToggleSettings();if(Button("net.quit"))Application.Quit();GUILayout.Space(16);GUILayout.Label(T("net.status."+Hub.Status),ui.small);GUILayout.EndArea();
                if(panel!=""&&FindFirstObjectByType<FoundationHUD>()?.SettingsOpen!=true)DrawSelection();
            }
            if(Hub.Online||Hub.Busy)
            {
                ui.Panel(new Rect(w-338,22,310,330));GUILayout.BeginArea(new Rect(w-318,40,270,295));GUILayout.Label(T("net.crew")+" · "+Hub.PlayerCount+" / 4",ui.heading);GUILayout.Label(T("net.status."+Hub.Status),ui.small);
                foreach(var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))GUILayout.Label(player.DisplayName+(player.HostPlayer?" · "+T("net.host_badge"):""),ui.text);
                GUI.enabled=Hub.Steam.Lobby.IsValid();if(Button("net.invite"))Hub.Steam.Invite();GUI.enabled=true;if(Button("net.leave"))Hub.Leave();
                if(WorldPersistence.ReadOnly)GUILayout.Label(T("net.foundation_note"),ui.small);GUILayout.EndArea();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(diagnostics)
            {
                ui.Panel(new Rect(25,720,1000,155));GUILayout.BeginArea(new Rect(40,730,970,140));
                GUILayout.Label("DEVELOPMENT · "+(Hub.DevelopmentTransport?"LOOPBACK, NOT STEAM AUTH":"STEAM")+" · "+(Hub.IsHost?"HOST":"CLIENT")+" · "+Hub.Status,ui.small);
                GUILayout.Label("World: "+WorldPersistence.Active?.worldId+" | Island: "+Hub.Island+" | Players: "+Hub.PlayerCount+"/4 | Local owner: "+(NetworkPlayer.Local?.OwnerId.ToString()??"none"),ui.small);
                GUILayout.Label("RTT: "+(Hub.Manager?Hub.Manager.TimeManager.RoundTripTime:0)+" ms | Tick: "+(Hub.Manager?Hub.Manager.TimeManager.Tick:0)+" | Save: "+WorldPersistence.LastSave+" | Auth: "+Hub.Auth?.LastResult+" | Disconnect: "+Hub.LastDisconnect,ui.small);
                if(!Hub.Online&&!Hub.Busy){GUILayout.BeginHorizontal();if(GUILayout.Button("Local test host",ui.tab))Hub.StartDevelopment(true);if(GUILayout.Button("Local test client",ui.tab))Hub.StartDevelopment(false);GUILayout.EndHorizontal();}GUILayout.EndArea();
            }
#endif
            GUI.matrix=before;
        }
        void DrawSelection()
        {
            ui.Book(new Rect(550,80,730,640));GUILayout.BeginArea(new Rect(580,110,670,580));GUILayout.Label(T("net."+(panel=="join"?"join":"worlds")),ui.title);
            if(panel=="join")
            {
                if(Button("net.refresh"))Hub.Steam.Browse();scroll=GUILayout.BeginScrollView(scroll);foreach(var lobby in Hub.Steam.Lobbies)if(GUILayout.Button(lobby.name+" · "+lobby.count+" / 4",ui.button))Hub.Steam.Join(lobby.id);GUILayout.EndScrollView();GUILayout.Label(T("net.invite_note"),ui.text);
            }
            else
            {
                if(unreadable)GUILayout.Label(T("net.status.save_invalid"),ui.small);
                scroll=GUILayout.BeginScrollView(scroll,GUILayout.Height(190));foreach(var world in worlds)if(GUILayout.Button((selected==world.worldId?"• ":"")+world.name,ui.button))selected=world.worldId;GUILayout.EndScrollView();
                if(Button("net.new_world"))selected=null;
                if(selected==null){GUILayout.Label(T("net.world_name"),ui.text);worldNameInput=GUILayout.TextField(worldNameInput,32);if(worlds.Count==0)importLegacy=GUILayout.Toggle(importLegacy,T("net.import"));}
                if(panel=="host"){GUILayout.Label(T("net.privacy"),ui.text);friendsOnly=GUILayout.Toggle(friendsOnly,T("net.friends_only"));GUILayout.Label(T(friendsOnly?"net.friends_note":"net.public_note"),ui.small);}
                GUI.enabled=!Hub.Busy&&!Hub.Online;if(Button("net.enter")){string worldName=string.IsNullOrWhiteSpace(worldNameInput)?T("net.default_world"):worldNameInput;if(panel=="solo")Hub.Solo(selected,worldName,selected==null,importLegacy);else Hub.Host(selected,worldName,selected==null,importLegacy,friendsOnly);}GUI.enabled=true;
            }
            if(Button("close"))panel="";GUILayout.EndArea();
        }
        bool Button(string key)=>GUILayout.Button(T(key),ui.button,GUILayout.Height(45));
        void OnDestroy(){ui?.Dispose();}
    }
}

