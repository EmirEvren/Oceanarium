using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/World settings")]
    public sealed class WorldSettings:ScriptableObject
    {
        public float minutesPerSecond=1,startHour=8,transitionMinutes=30;
        public WeatherProfile[] weather;public SeasonProfile[] seasons;
        public int[] merchantWeekdays={0,4};public float merchantArrival=20,merchantDeparture=2;
        public bool lightning=true;public float tropicalSnowMultiplier=.12f;
    }
}
