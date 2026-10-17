using System;
using UnityEngine;
namespace OceanariumFishing
{
    [Serializable] public sealed class FishingDifficulty
    {
        public string id="cozy";
        public float hookWindow=3,perfectWindow=.6f,goodWindow=3,pullMultiplier=.6f,tensionMultiplier=.5f,recoveryMultiplier=1.5f,breakGrace=2,slackGrace=20;
        [Range(0,1)] public float minimumControl=.9f;
        public float staminaMultiplier=2,progressMultiplier=1,escapeMultiplier=.3f,comboMistakeGrace=1.5f;
        public bool useRarityDurations=true;
        public float[] fightSeconds={7.5f,11.5f,17,24,34};
    }
    [CreateAssetMenu(menuName="Oceanarium/Fishing difficulty profiles")]
    public sealed class FishingDifficultyProfiles:ScriptableObject
    {
        public FishingDifficulty[] profiles={new(),new(){id="normal",hookWindow=2,perfectWindow=.5f,goodWindow=2,pullMultiplier=1,tensionMultiplier=1,recoveryMultiplier=1,breakGrace=1.5f,slackGrace=8,minimumControl=.55f,staminaMultiplier=1.25f,progressMultiplier=1.1f,escapeMultiplier=.7f,comboMistakeGrace=.65f,useRarityDurations=false},new(){id="experienced",hookWindow=1.25f,perfectWindow=.35f,goodWindow=1.25f,pullMultiplier=1.15f,tensionMultiplier=1.1f,recoveryMultiplier=.9f,breakGrace=1,slackGrace=5,minimumControl=0,staminaMultiplier=1,progressMultiplier=1,escapeMultiplier=1,comboMistakeGrace=.2f,useRarityDurations=false}};
        public FishingDifficulty Get(string id)=>Array.Find(profiles,p=>p.id==id)??profiles[0];
    }
}
