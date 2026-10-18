using System;
using UnityEngine;
namespace OceanariumFishing
{
    [Serializable] public sealed class WorldState
    {
        public int version=1,seed=731921;public long minute=480;public double fraction;public float speed=1;
        public string forcedWeather;public long forcedUntil=-1;
        public WorldState Copy()=>JsonUtility.FromJson<WorldState>(JsonUtility.ToJson(this));
        public void Normalize(){minute=Math.Max(0,minute);if(double.IsNaN(fraction)||double.IsInfinity(fraction)||fraction<0||fraction>=1)fraction=0;if(!float.IsFinite(speed)||speed<0||speed>120)speed=1;}
    }
    // Pure deterministic host simulation. Clients consume snapshots, never roll local schedules.
    public sealed class WorldSimulation:ITownClock
    {
        public WorldState State{get;private set;}public WorldSettings Settings{get;}
        public int Day=>(int)(State.minute/1440);public float Hour=>(float)((State.minute%1440+State.fraction)/60);
        public int Month=>Day/28%4;public int DayOfMonth=>Day%28+1;public int Year=>Day/112+1;public int Weekday=>Day%7;
        public bool Paused;public event Action<int,int> DaysChanged;public event Action Changed;
        public WorldSimulation(WorldState state,WorldSettings settings){State=state.Copy();State.Normalize();Settings=settings;}
        public void Tick(double seconds){if(Paused||seconds<=0||double.IsNaN(seconds)||double.IsInfinity(seconds))return;double delta=seconds*Settings.minutesPerSecond*State.speed+State.fraction;long whole=(long)Math.Floor(delta);State.fraction=delta-whole;if(whole>0)SetMinute(State.minute+whole);}
        public void SetMinute(long minute){int before=Day;State.minute=Math.Max(0,minute);if(Day!=before)DaysChanged?.Invoke(before,Day);Changed?.Invoke();}
        public long NextMorning=>(Day+1L)*1440+(long)(Settings.startHour*60);
        public WeatherProfile Forecast(int day,int block)
        {
            if(Settings.weather==null||Settings.weather.Length==0)return null;int season=Math.Max(0,day)/28%4;float total=0;
            foreach(var w in Settings.weather)if(w)total+=Mathf.Max(0,w.seasonalWeights[season]);
            uint hash=unchecked((uint)(State.seed+day*374761393+block*668265263));hash=(hash^(hash>>13))*1274126177u;hash^=hash>>16;
            float roll=(hash/(double)uint.MaxValue>1?1:(float)(hash/(double)uint.MaxValue))*total;
            foreach(var w in Settings.weather)if(w){roll-=Mathf.Max(0,w.seasonalWeights[season]);if(roll<=0)return w;}return Settings.weather[0];
        }
        public WeatherProfile Current
        {
            get{if(State.forcedUntil>State.minute&&!string.IsNullOrEmpty(State.forcedWeather)){foreach(var w in Settings.weather)if(w.id==State.forcedWeather)return w;}return Forecast(Day,(int)Hour/6);}
        }
        public WeatherProfile Previous=>Forecast((int)Math.Max(0,(State.minute/360-1)*360)/1440,(int)Math.Max(0,State.minute/360-1)%4);
        public float Blend=>Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(State.minute%360+State.fraction)/Mathf.Max(1,Settings.transitionMinutes)));
        public bool MerchantNight(int day)=>Array.IndexOf(Settings.merchantWeekdays,((day%7)+7)%7)>=0;
        public bool MerchantAvailable=>MerchantNight(Hour<Settings.merchantDeparture?Day-1:Day)&&(Hour>=Settings.merchantArrival||Hour<Settings.merchantDeparture);
        public void Force(string id){State.forcedWeather=id;State.forcedUntil=(State.minute/360+1)*360;Changed?.Invoke();}
        public void ApplySnapshot(WorldState state){int day=Day;State=state.Copy();State.Normalize();if(day!=Day)DaysChanged?.Invoke(day,Day);Changed?.Invoke();}
    }
}
