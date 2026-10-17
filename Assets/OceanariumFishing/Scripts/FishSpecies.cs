using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Fish species")]
    public sealed class FishSpecies:ScriptableObject
    {
        public string id,nameKey;public FishRarity rarity;public float minWeight,maxWeight,minLength,maxLength,baseValue=20,biteProbability=1,fightingStrength=1,stamina=1;
        public FishHabitat[] habitats;public string[] islands={"OF_HomeIsland"},weather={"clear","cloudy"};public float startHour=0,endHour=24;public string preferredBait="none";public bool aquariumEligible=true;public string encyclopediaKey;public FishBehavior behavior;public Color color=Color.cyan;public Vector3 shape=new(1,.5f,.28f);
    }
}
