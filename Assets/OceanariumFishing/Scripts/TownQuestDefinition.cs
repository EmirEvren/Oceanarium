using UnityEngine;
namespace OceanariumFishing
{
    public enum TownObjective {Talk,Catch,Deliver,Find,Purchase,Visit,Discover,PlaceDecoration,Photo}
    [CreateAssetMenu(menuName="Oceanarium/Town quest")]
    public sealed class TownQuestDefinition:ScriptableObject
    {
        public string id,giver,titleKey,descriptionKey,target,prerequisite;public TownObjective objective;public int amount=1,coins=4,friendship=30;public string rewardResource;public int rewardAmount=1;
    }
}
