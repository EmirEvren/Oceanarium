using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Aquarium economy")]
    public sealed class AquariumEconomy:ScriptableObject
    {
        public float speciesPoints=12,rarityPoints=5,qualityPoints=4,tankVarietyPoints=4,habitatPoints=2,decorationPoints=6,upgradePoints=3,duplicateFactor=.25f;
        public float[] ranks={0,15,40,75,120};public float spawnSeconds=24,minSpawnSeconds=10;public int maxVisitors=5;
        public float dayStart=7,dayEnd=22;public int baseDonation=4;public float popularityDonation=.08f,comfortBonus=.2f,signageBonus=.2f;
        public AquariumUpgrade[] upgrades={new(){id="comfort",price=18},new(){id="signage",price=25},new(){id="decor",price=20},new(){id="medium",price=35,requiredPopularity=15}};
    }
}
