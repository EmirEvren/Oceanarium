using System;
using UnityEngine;
namespace OceanariumFishing
{
    public enum AquariumHabitat { TropicalSaltwater,TemperateSaltwater,Freshwater }
    public enum AquariumTankSize {Small,Medium,Large}
    [CreateAssetMenu(menuName="Oceanarium/Aquarium tank")]
    public sealed class AquariumDefinition:ScriptableObject
    {
        public string id;public AquariumTankSize size;public int capacity=3;public float maxLength=35;
        public Vector3 dimensions=new(2.8f,1.7f,1.5f);public AquariumHabitat[] habitats;
        public bool futureLocked;public Material glass,water;public Vector2 footprint;public int decorationBudget=12;public float waterVolume;public string buildType;
    }
    [Serializable] public sealed class AquariumUpgrade
    {public string id;public int price;public float requiredPopularity;}
}
