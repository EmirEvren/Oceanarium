using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Authenticating;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using Steamworks;
using UnityEngine;
namespace OceanariumFishing
{
    public struct AuthChallenge:IBroadcast {public string nonce;public ulong host;}
    public struct AuthReply:IBroadcast {public string nonce,developmentId;public byte[] ticket;}
    public struct SessionNotice:IBroadcast {public string code;}
    public sealed class OceanAuthenticator:Authenticator
    {
        sealed class Pending {public NetworkConnection connection;public string nonce,identity;public float deadline;public bool ticketStarted;}
        readonly Dictionary<int,Pending> pending=new();
        public readonly Dictionary<int,string> Identities=new();
        public string LastResult{get;private set;}="idle";
        public override event Action<NetworkConnection,bool> OnAuthenticationResult;
        Callback<ValidateAuthTicketResponse_t> validated;
        public override void InitializeOnce(NetworkManager manager)
        {
            base.InitializeOnce(manager);manager.ServerManager.RegisterBroadcast<AuthReply>(Reply,false);manager.ClientManager.RegisterBroadcast<AuthChallenge>(Challenge);
            manager.ClientManager.RegisterBroadcast<SessionNotice>((message,channel)=>NetworkSession.Current.Error(message.code));
            manager.ServerManager.OnRemoteConnectionState+=Disconnected;
        }
        public override void OnRemoteConnection(NetworkConnection connection)
        {
            if(pending.ContainsKey(connection.ClientId)||Identities.ContainsKey(connection.ClientId)){Reject(connection,"duplicate");return;}
            if(Identities.Count>=4){Reject(connection,"full");return;}
            var hub=NetworkSession.Current;var p=new Pending{connection=connection,nonce=Guid.NewGuid().ToString("N"),deadline=Time.realtimeSinceStartup+20};pending.Add(connection.ClientId,p);
            if(!hub.DevelopmentTransport)
            {
                if(!hub.Steam.Available){Reject(connection,"steam_unavailable");return;}
                validated??=Callback<ValidateAuthTicketResponse_t>.Create(Validated);
                // Identity is obtained from SteamNetworkingSockets' remote identity by the transport, never from AuthReply.
                p.identity=managerAddress(connection);
                if(!ulong.TryParse(p.identity,out var steam)||!hub.Steam.Member(steam)){Reject(connection,"authentication");return;}
                if(Identities.Values.Contains(p.identity)||pending.Values.Any(other=>other!=p&&other.identity==p.identity)){Reject(connection,"duplicate");return;}
                if(steam==SteamUser.GetSteamID().m_SteamID){Accept(p);return;}
            }
            managerBroadcast(connection,new AuthChallenge{nonce=p.nonce,host=hub.Steam.Available?SteamUser.GetSteamID().m_SteamID:0});
        }
        string managerAddress(NetworkConnection connection)=>NetworkManager.TransportManager.Transport.GetConnectionAddress(connection.ClientId);
        void managerBroadcast(NetworkConnection connection,AuthChallenge challenge)=>NetworkManager.ServerManager.Broadcast(connection,challenge,false);
        void Challenge(AuthChallenge challenge,Channel channel)
        {
            var hub=NetworkSession.Current;
            if(hub.DevelopmentTransport)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NetworkManager.ClientManager.Broadcast(new AuthReply{nonce=challenge.nonce,developmentId=hub.DevelopmentIdentity});
#endif
                return;
            }
            if(!hub.Steam.Available||challenge.host!=hub.Steam.ExpectedHost){hub.Abort("authentication");return;}
            hub.Steam.RequestTicket(challenge.host,bytes=>{if(bytes==null){hub.Abort("authentication");return;}NetworkManager.ClientManager.Broadcast(new AuthReply{nonce=challenge.nonce,ticket=bytes});});
        }
        void Reply(NetworkConnection connection,AuthReply reply,Channel channel)
        {
            if(connection.IsAuthenticated||!pending.TryGetValue(connection.ClientId,out var p)||reply.nonce!=p.nonce||p.ticketStarted){Reject(connection,"authentication");return;}
            if(NetworkSession.Current.DevelopmentTransport)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Explicit loopback-only development transport. These IDs are NEVER Steam proof.
                if(reply.developmentId==null||!reply.developmentId.StartsWith("dev-")||!WorldSlotStore.ValidIdentity(reply.developmentId)){Reject(connection,"authentication");return;}
                p.identity=reply.developmentId;if(Identities.Values.Contains(p.identity)){Reject(connection,"duplicate");return;}Accept(p);
#else
                Reject(connection,"authentication");
#endif
                return;
            }
            if(reply.ticket==null||reply.ticket.Length<16||reply.ticket.Length>3072||!ulong.TryParse(p.identity,out var steam)){Reject(connection,"authentication");return;}
            var result=SteamUser.BeginAuthSession(reply.ticket,reply.ticket.Length,new CSteamID(steam));
            if(result!=EBeginAuthSessionResult.k_EBeginAuthSessionResultOK){Reject(connection,"authentication");return;}p.ticketStarted=true;
        }
        void Validated(ValidateAuthTicketResponse_t result)
        {
            string identity=result.m_SteamID.m_SteamID.ToString();var p=pending.Values.FirstOrDefault(x=>x.identity==identity&&x.ticketStarted);
            if(p!=null){if(result.m_eAuthSessionResponse==EAuthSessionResponse.k_EAuthSessionResponseOK)Accept(p);else Reject(p.connection,"authentication");return;}
            if(result.m_eAuthSessionResponse!=EAuthSessionResponse.k_EAuthSessionResponseOK)
                foreach(var entry in Identities.Where(x=>x.Value==identity).ToArray())if(NetworkManager.ServerManager.Clients.TryGetValue(entry.Key,out var conn))Reject(conn,"authentication");
        }
        void Accept(Pending p)
        {
            if(Identities.Count>=4){Reject(p.connection,"full");return;}
            if(Identities.Values.Contains(p.identity)){Reject(p.connection,"duplicate");return;}
            Identities.Add(p.connection.ClientId,p.identity);pending.Remove(p.connection.ClientId);LastResult="authenticated";
            NetworkManager.ServerManager.Broadcast(p.connection,new SessionNotice{code="connected"},false);OnAuthenticationResult?.Invoke(p.connection,true);
        }
        void Reject(NetworkConnection connection,string code)
        {LastResult=code;NetworkManager.ServerManager.Broadcast(connection,new SessionNotice{code=code},false);Cleanup(connection.ClientId);if(connection.IsAuthenticated)connection.Disconnect(false);else OnAuthenticationResult?.Invoke(connection,false);}
        void Disconnected(NetworkConnection connection,RemoteConnectionStateArgs args){if(args.ConnectionState==RemoteConnectionState.Stopped)Cleanup(connection.ClientId);}
        void Cleanup(int id)
        {
            string identity=Identities.GetValueOrDefault(id);if(pending.Remove(id,out var p)&&p.ticketStarted)identity=p.identity;
            Identities.Remove(id);if(NetworkSession.Current?.Steam.Available==true&&ulong.TryParse(identity,out var steam)&&steam!=SteamUser.GetSteamID().m_SteamID)SteamUser.EndAuthSession(new CSteamID(steam));
        }
        void Update(){foreach(var p in pending.Values.Where(x=>Time.realtimeSinceStartup>x.deadline).ToArray())Reject(p.connection,"timeout");}
        public void Clear(){foreach(var id in Identities.Keys.Concat(pending.Keys).Distinct().ToArray())Cleanup(id);validated?.Dispose();validated=null;}
        void OnDestroy(){Clear();}
    }
}
