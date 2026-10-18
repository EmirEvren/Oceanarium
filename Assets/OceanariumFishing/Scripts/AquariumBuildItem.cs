using System;
using UnityEngine;
namespace OceanariumFishing {
    [CreateAssetMenu(menuName="Oceanarium/Build item")]
    public sealed class AquariumBuildItem:ScriptableObject
    {
        public string id; public BuildCategory category; public int price=5,level=1; public float popularity=1,requiredPopularity,comfort;
        public Vector3 size=Vector3.one; public AquariumHabitat[] habitats=Array.Empty<AquariumHabitat>();
        public GameObject prefab;public Texture2D thumbnail;public Material material;public AquariumDefinition tank;
        public bool futureLocked,wallMounted; public int decorationCost=1;public string theme="coastal";
    }
}
