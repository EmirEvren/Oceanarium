using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Season profile")]
    public sealed class SeasonProfile:ScriptableObject
    {
        public string id,monthKey,nameKey;public Color foliage=Color.white;public float flowers=1,leaves,snow;
    }
}
