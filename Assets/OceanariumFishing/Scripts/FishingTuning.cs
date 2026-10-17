using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Fishing tuning")]
    public sealed class FishingTuning:ScriptableObject
    {
        public FishingDifficultyProfiles difficulties;
        public FishingDifficulty Difficulty(string id="cozy")=>difficulties?difficulties.Get(id):new FishingDifficulty();
        public float chargeDuration=1.6f,minCast=3,flightDuration=.9f,minWait=2.5f,maxWait=6,hookWindow=1.8f,perfectWindow=.48f,goodWindow=1.15f;
        public float tensionRise=.20f,tensionRecovery=.32f,breakGrace=1.2f,slackGrace=6,progressRate=.09f,staminaDrain=.12f,escapeRate=.025f,comboSeconds=2.1f;public int maxCombo=5;
    }
}
