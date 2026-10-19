using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class SteamPlatform:MonoBehaviour
    {
        public bool Available{get;private set;}public CSteamID Lobby{get;private set;}public ulong ExpectedHost{get;private set;}
        public string Identity=>Available?SteamUser.GetSteamID().m_SteamID.ToString():"local";
        public string DisplayName=>Available?NetworkNames.Clean(SteamFriends.GetPersonaName()):"Player";
        public readonly List<(ulong id,string name,int count)> Lobbies=new();
        public const string Protocol="oceanarium-1.0-v1";
        Callback<GameLobbyJoinRequested_t> invite;Callback<LobbyEnter_t> entered;Callback<LobbyChatUpdate_t> members;Callback<LobbyDataUpdate_t> lobbyData;
        Callback<GetAuthSessionTicketResponse_t> ticketReady;CallResult<LobbyCreated_t> created;CallResult<LobbyMatchList_t> listed;
        HAuthTicket ticket=HAuthTicket.Invalid;Action<byte[]> ticketCallback;byte[] ticketBytes;uint ticketLength;
        int generation;ulong requestedLobby;bool creating;float deadline;
        public bool Initialize()
        {
            if(Available)return true;var settings=Resources.Load<OceanNetworkSettings>("OceanNetworkSettings");
            if(!settings||settings.steamAppId==0||(settings.steamAppId==480&&(!Debug.isDebugBuild&&!Application.isEditor||!settings.allowDevelopmentAppId)))return Fail("steam_config");
            try
            {
                // Development launch only. Release builds are launched by Steam with their registered App ID.
                if(Application.isEditor||Debug.isDebugBuild)Environment.SetEnvironmentVariable("SteamAppId",settings.steamAppId.ToString());
                if(!Packsize.Test()||!DllCheck.Test()||!SteamAPI.Init())return Fail("steam_unavailable");
                if(SteamUtils.GetAppID().m_AppId!=settings.steamAppId||!SteamUser.BLoggedOn()||!SteamApps.BIsSubscribedApp(new AppId_t(settings.steamAppId))){SteamAPI.Shutdown();return Fail("steam_access");}
                Available=true;
                invite=Callback<GameLobbyJoinRequested_t>.Create(x=>Join(x.m_steamIDLobby.m_SteamID));
                entered=Callback<LobbyEnter_t>.Create(Entered);
                members=Callback<LobbyChatUpdate_t>.Create(x=>{if(x.m_ulSteamIDLobby==Lobby.m_SteamID)CheckHost();});
                lobbyData=Callback<LobbyDataUpdate_t>.Create(x=>{if(x.m_ulSteamIDLobby==Lobby.m_SteamID)CheckHost();});
                ticketReady=Callback<GetAuthSessionTicketResponse_t>.Create(x=>{if(x.m_hAuthTicket!=ticket||ticketCallback==null)return;var done=ticketCallback;ticketCallback=null;done(x.m_eResult==EResult.k_EResultOK?ticketBytes:null);});
                created=CallResult<LobbyCreated_t>.Create();listed=CallResult<LobbyMatchList_t>.Create();return true;
            }
            catch(Exception e)when(e is DllNotFoundException||e is EntryPointNotFoundException||e is InvalidOperationException){return Fail("steam_unavailable");}
        }
        bool Fail(string key){NetworkSession.Current?.Error(key);return false;}
        void Update(){if(!Available)return;SteamAPI.RunCallbacks();if(deadline>0&&Time.realtimeSinceStartup>deadline){deadline=0;Leave();NetworkSession.Current?.Abort("timeout");}}
        public void Host(bool friendsOnly)
        {
            if(!Initialize())return;Leave();creating=true;deadline=Time.realtimeSinceStartup+25;int request=++generation;
            created.Set(SteamMatchmaking.CreateLobby(friendsOnly?ELobbyType.k_ELobbyTypeFriendsOnly:ELobbyType.k_ELobbyTypePublic,4),(result,failed)=>
            {
                if(request!=generation){if(result.m_eResult==EResult.k_EResultOK)SteamMatchmaking.LeaveLobby(new CSteamID(result.m_ulSteamIDLobby));return;}
                creating=false;deadline=0;if(failed||result.m_eResult!=EResult.k_EResultOK){NetworkSession.Current.Abort("lobby_create");return;}
                Lobby=new(result.m_ulSteamIDLobby);ExpectedHost=SteamUser.GetSteamID().m_SteamID;
                SteamMatchmaking.SetLobbyData(Lobby,"protocol",Protocol);SteamMatchmaking.SetLobbyData(Lobby,"host",Identity);SteamMatchmaking.SetLobbyData(Lobby,"name",NetworkNames.Clean(WorldPersistence.Active.name));SteamMatchmaking.SetLobbyData(Lobby,"state","open");
                SteamMatchmaking.SetLobbyMemberLimit(Lobby,4);SteamMatchmaking.SetLobbyJoinable(Lobby,true);NetworkSession.Current.StartSteamHost();
            });
        }
        public void Browse()
        {
            if(!Initialize())return;Lobbies.Clear();SteamMatchmaking.AddRequestLobbyListStringFilter("protocol",Protocol,ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter("state","open",ELobbyComparison.k_ELobbyComparisonEqual);SteamMatchmaking.AddRequestLobbyListResultCountFilter(30);
            listed.Set(SteamMatchmaking.RequestLobbyList(),(result,failed)=>{if(failed){Fail("lobby_list");return;}for(int i=0;i<result.m_nLobbiesMatching;i++){var id=SteamMatchmaking.GetLobbyByIndex(i);Lobbies.Add((id.m_SteamID,NetworkNames.Clean(SteamMatchmaking.GetLobbyData(id,"name")),SteamMatchmaking.GetNumLobbyMembers(id)));}});
        }
        public void Join(ulong id)
        {
            if(!Initialize())return;if(NetworkSession.Current.Busy||NetworkSession.Current.Online){Fail("already_connected");return;}
            var lobby=new CSteamID(id);if(!lobby.IsValid()||!lobby.IsLobby()){Fail("lobby_invalid");return;}
            Leave();requestedLobby=id;deadline=Time.realtimeSinceStartup+25;NetworkSession.Current.BeginJoin();SteamMatchmaking.JoinLobby(lobby);
        }
        void Entered(LobbyEnter_t result)
        {
            if(creating)return;
            if(result.m_ulSteamIDLobby!=requestedLobby)
            {
                // Steam may finish a join after cancellation or timeout. Do not retain that stale membership.
                if(result.m_EChatRoomEnterResponse==(uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess&&result.m_ulSteamIDLobby!=Lobby.m_SteamID)SteamMatchmaking.LeaveLobby(new CSteamID(result.m_ulSteamIDLobby));
                return;
            }
            deadline=0;
            if(result.m_EChatRoomEnterResponse!=(uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess){requestedLobby=0;NetworkSession.Current.Abort(result.m_EChatRoomEnterResponse==(uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseFull?"full":"lobby_join");return;}
            Lobby=new(result.m_ulSteamIDLobby);requestedLobby=0;
            if(SteamMatchmaking.GetLobbyData(Lobby,"protocol")!=Protocol||SteamMatchmaking.GetLobbyData(Lobby,"state")!="open"||!ulong.TryParse(SteamMatchmaking.GetLobbyData(Lobby,"host"),out var host)||host!=SteamMatchmaking.GetLobbyOwner(Lobby).m_SteamID){NetworkSession.Current.Abort("lobby_invalid");return;}
            ExpectedHost=host;NetworkSession.Current.StartSteamClient(host.ToString());
        }
        void CheckHost(){if(!Lobby.IsValid()||NetworkSession.Current.IsHost)return;if(SteamMatchmaking.GetLobbyOwner(Lobby).m_SteamID!=ExpectedHost||SteamMatchmaking.GetLobbyData(Lobby,"state")=="closed")NetworkSession.Current.Abort("host_left");}
        public bool Member(ulong identity){if(!Available||!Lobby.IsValid())return false;for(int i=0;i<SteamMatchmaking.GetNumLobbyMembers(Lobby);i++)if(SteamMatchmaking.GetLobbyMemberByIndex(Lobby,i).m_SteamID==identity)return true;return false;}
        public void Invite(){if(Available&&Lobby.IsValid())SteamFriends.ActivateGameOverlayInviteDialog(Lobby);else Fail("not_hosting");}
        public void RequestTicket(ulong host,Action<byte[]> callback)
        {
            CancelTicket();ticketCallback=callback;ticketBytes=new byte[3072];var remote=new SteamNetworkingIdentity();remote.SetSteamID64(host);
            ticket=SteamUser.GetAuthSessionTicket(ticketBytes,ticketBytes.Length,out ticketLength,ref remote);
            if(ticket==HAuthTicket.Invalid||ticketLength==0){ticketCallback=null;callback(null);return;}Array.Resize(ref ticketBytes,(int)ticketLength);
        }
        public void CancelTicket(){if(Available&&ticket!=HAuthTicket.Invalid)SteamUser.CancelAuthTicket(ticket);ticket=HAuthTicket.Invalid;ticketCallback=null;ticketBytes=null;}
        public void Leave()
        {
            generation++;creating=false;deadline=0;requestedLobby=0;CancelTicket();
            if(Available&&Lobby.IsValid()){if(SteamMatchmaking.GetLobbyOwner(Lobby)==SteamUser.GetSteamID()){SteamMatchmaking.SetLobbyJoinable(Lobby,false);SteamMatchmaking.SetLobbyData(Lobby,"state","closed");}SteamMatchmaking.LeaveLobby(Lobby);}
            Lobby=CSteamID.Nil;ExpectedHost=0;
        }
        void OnDestroy(){Leave();invite?.Dispose();entered?.Dispose();members?.Dispose();lobbyData?.Dispose();ticketReady?.Dispose();created?.Dispose();listed?.Dispose();if(Available)SteamAPI.Shutdown();Available=false;}
    }
}
