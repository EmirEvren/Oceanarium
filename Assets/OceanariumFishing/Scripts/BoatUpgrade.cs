using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Boat upgrade")]
    public sealed class BoatUpgrade:ScriptableObject
    {public string id,nameKey;public int tier,price;public bool futureLocked=true;public string requiredFlag;public string[] categories={"engine","hull","navigation","storage","cosmetics"};}
}
