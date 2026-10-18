using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    [Serializable] public sealed class TownResidentState
    {
        public string id; public int friendship, talkedDay=-1, giftDay=-1, giftsToday;
        public List<string> knownPreferences=new(), rewards=new();
        public int Hearts=>Mathf.Clamp(friendship/100,0,10);
    }
    [Serializable] public sealed class TownPurchase {public string product;public int quantity;}
    [Serializable] public sealed class TownQuestState {public string id;public bool completed;}
    [Serializable] public sealed class TownProgress
    {
        public int version=1,revision;public double elapsedMinutes=540;
        public List<TownResidentState> residents=new();public List<TownPurchase> purchases=new();
        public List<string> commands=new(),flags=new();
        public List<TownQuestState> quests=new();public string trackedQuest;public float buffSeconds;public string buff;
        public string cosmeticSlot,cosmeticItem;
        public void Normalize(){residents??=new();purchases??=new();commands??=new();flags??=new();quests??=new();if(double.IsNaN(elapsedMinutes)||double.IsInfinity(elapsedMinutes)||elapsedMinutes<0)elapsedMinutes=540;}
        public TownResidentState Resident(string id){var state=residents.Find(r=>r.id==id);if(state==null){state=new(){id=id};residents.Add(state);}return state;}
    }
    // Replaceable by the future host calendar. No operating-system wall clock.
    public interface ITownClock {int Day{get;}float Hour{get;}}
    public sealed class TownClock : ITownClock
    {
        readonly WorldSimulation simulation;double minutes;public WorldSimulation World=>simulation;
        public TownClock(WorldSimulation simulation){this.simulation=simulation;}
        public double Minutes{get=>simulation!=null?simulation.State.minute+simulation.State.fraction:minutes;private set=>minutes=value;}public int Day=>(int)(Minutes/1440);public float Hour=>(float)(Minutes%1440)/60;
        public TownClock(double minutes){Minutes=Math.Max(0,minutes);}
        public void Advance(float seconds,float minutesPerSecond){if(seconds>0&&minutesPerSecond>0){if(simulation!=null)simulation.SetMinute(simulation.State.minute+(long)(seconds*minutesPerSecond));else Minutes+=seconds*minutesPerSecond;}}
    }
}
