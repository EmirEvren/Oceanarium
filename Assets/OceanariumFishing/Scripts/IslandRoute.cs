using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Island route")]
    public sealed class IslandRoute:ScriptableObject
    {public string id,nameKey,requirementKey;public Destination destination;public Texture2D icon;public bool requiresRepair,futureLocked,hidden;public int requiredTier;public string[] requiredFlags;public Vector3 spawn;public Vector2 mapPosition;}
}
