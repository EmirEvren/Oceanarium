using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Network settings")]
    public sealed class OceanNetworkSettings:ScriptableObject
    {
        public uint steamAppId=4241310;
        public bool allowDevelopmentAppId;
        public ushort tickRate=30,localPort=7778;
    }
}
