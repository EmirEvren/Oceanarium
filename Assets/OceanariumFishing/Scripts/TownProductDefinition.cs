using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Town product")]
    public sealed class TownProductDefinition:ScriptableObject
    {
        public string id,nameKey,resourceId,currency="coins";public int price=4,quantity=1,stockLimit=10,requiredHearts;
        public bool futureLocked;
        public AquariumBuildItem buildItem;
        public string cosmeticSlot,cosmeticItem;
    }
}
