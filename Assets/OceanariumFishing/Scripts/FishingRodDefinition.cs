using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Fishing rod")]
    public sealed class FishingRodDefinition:ScriptableObject
    {
        public string id="starter",nameKey="fishing.rod.starter";public float castDistance=14,accuracy=.92f,strength=1,lineStrength=1,reelSpeed=1,handling=1;public bool durabilityEnabled;public int maxDurability=100,upgradeLevel;public FishRarity rarity;public GameObject visualPrefab;
    }
}
