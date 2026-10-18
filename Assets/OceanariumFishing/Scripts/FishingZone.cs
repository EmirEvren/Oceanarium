using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class FishingZone:MonoBehaviour
    {
        public string zoneId;public FishHabitat habitat;public float radius=14;public FishSpawnEntry[] entries;
        public FishSpecies Select(int seed,float hour,string weather,string bait,string island,int season=-1)
        {
            var choices=new List<FishSpawnEntry>();float total=0;
            foreach(var e in entries){var s=e.species;if(!s || Array.IndexOf(s.habitats,habitat)<0 || Array.IndexOf(s.islands,island)<0 || Array.IndexOf(s.weather,weather)<0 || season>=0&&Array.IndexOf(s.seasons,season)<0)continue;bool time=s.startHour<=s.endHour?hour>=s.startHour&&hour<s.endHour:hour>=s.startHour||hour<s.endHour;if(!time)continue;float w=e.weight*s.biteProbability/(1+(int)s.rarity*1.5f)*(s.preferredBait==bait?1.15f:1)*(Array.IndexOf(s.preferredWeather,weather)>=0?1.2f:1);choices.Add(new FishSpawnEntry{species=s,weight=w});total+=w;}
            if(total<=0)return null;float roll=(float)new System.Random(seed).NextDouble()*total;foreach(var e in choices){roll-=e.weight;if(roll<=0)return e.species;}return choices[choices.Count-1].species;
        }
        public static FishingZone At(Vector3 p)
        {FishingZone best=null;float distance=float.MaxValue;foreach(var z in FindObjectsByType<FishingZone>(FindObjectsSortMode.None)){float d=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(z.transform.position.x,z.transform.position.z));if(d<z.radius&&d<distance){distance=d;best=z;}}return best;}
    }
}
