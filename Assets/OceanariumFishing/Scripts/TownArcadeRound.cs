using UnityEngine;
namespace OceanariumFishing
{
    public sealed class TownArcadeRound
    {
        public int Attempts{get;private set;}public int Hits{get;private set;}public bool Finished=>Attempts>=5;public bool Won=>Finished&&Hits>=3;
        public bool Reel(float elapsed){if(Finished||elapsed<0||float.IsNaN(elapsed)||float.IsInfinity(elapsed))return false;float marker=Mathf.PingPong(elapsed*.7f,1);Attempts++;if(marker>=.4f&&marker<=.6f)Hits++;return true;}
        public bool Claim(FishCollection collection,int day){string flag="arcade:"+day;if(!Won||collection.town.flags.Contains(flag))return false;collection.town.flags.Add(flag);collection.aquarium.coins+=3;return true;}
    }
}
