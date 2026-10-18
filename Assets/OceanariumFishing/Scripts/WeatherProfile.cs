using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Weather profile")]
    public sealed class WeatherProfile:ScriptableObject
    {
        public string id,nameKey;public float clouds,sun=1,wind=.2f,waves=1,rain,snow,fog=.002f,visitors=1,bite=1;
        public float[] seasonalWeights={1,1,1,1};public AudioClip ambient,thunder;
    }
}
