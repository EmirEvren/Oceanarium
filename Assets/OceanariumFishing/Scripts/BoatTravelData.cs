using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    [Serializable] public sealed class ResourceStack {public string id;public int count;}
    [Serializable] public sealed class TravelProgress
    {
        public int version=1,boatTier;public bool introduced,accepted,inspected,repaired;public string lastIsland="home",pendingDestination,hullColor="seafoam";public Vector3 location;
        public List<string> collected=new(),discovered=new(){"home","town","coral"},flags=new();
        public void Normalize(){collected??=new();discovered??=new(){"home","town","coral"};flags??=new();if(string.IsNullOrEmpty(lastIsland))lastIsland="home";}
    }
    public enum BoatTravelState { Docked,Boarding,WaitingForPlayers,DestinationSelection,Departure,SceneLoading,Arrival,TravelComplete }
    // Pure readiness service. Network adapters supply authenticated connected-player IDs.
    public sealed class BoatReadiness
    {
        readonly Dictionary<string,int> seats=new();readonly HashSet<string> ready=new();readonly HashSet<string> connected=new();
        public IReadOnlyDictionary<string,int> Seats=>seats;public void Connect(string id){if(!string.IsNullOrWhiteSpace(id))connected.Add(id);}
        public void Disconnect(string id){connected.Remove(id);Leave(id);}public bool Board(string id,out int seat){seat=-1;if(!connected.Contains(id))return false;if(seats.TryGetValue(id,out seat))return true;if(seats.Count>=4)return false;seat=Enumerable.Range(0,4).First(s=>!seats.Values.Contains(s));seats.Add(id,seat);return true;}
        public void Leave(string id){seats.Remove(id);ready.Remove(id);}public void SetReady(string id,bool value){if(value&&seats.ContainsKey(id))ready.Add(id);else ready.Remove(id);}
        public bool CanDepart=>connected.Count>0&&connected.All(id=>seats.ContainsKey(id)&&ready.Contains(id));
    }
}
