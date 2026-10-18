using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Town resident")]
    public sealed class TownResidentDefinition:ScriptableObject
    {
        public string id,nameKey,professionKey,biographyKey,greetingKey,shopKey;
        public string firstMeetingKey,friendKey,shopIntroKey,farewellKey;
        public AppearanceData appearance=new();public GameObject visualPrefab;
        public Vector3 home,work,social;public float workStart=8,breakStart=13,breakEnd=14,workEnd=18;
        public bool nightMerchant;public int appearanceDayInterval=2;
        public string[] loved={},liked={},disliked={};public int conversationPoints=15,giftLimit=1;
        public int rewardHearts=2;public string rewardResource;public int rewardQuantity=1;
        public TownProductDefinition[] products={};
        public TownQuestDefinition[] quests={};
        public Texture2D portrait;public AquariumBuildItem rewardBuildItem;
        public bool ShopOpen(ITownClock clock)=>nightMerchant?Available(clock):clock.Hour>=workStart&&clock.Hour<workEnd&&!(clock.Hour>=breakStart&&clock.Hour<breakEnd);
        public bool Available(ITownClock clock)
        {
            if(!nightMerchant)return true;var settings=(clock as TownClock)?.World?.Settings??(clock as WorldSimulation)?.Settings;
            float end=settings?settings.merchantDeparture:2,start=settings?settings.merchantArrival:20;int day=clock.Hour<end?clock.Day-1:clock.Day;
            return System.Array.IndexOf(settings?settings.merchantWeekdays:new[]{0,4},((day%7)+7)%7)>=0&&(clock.Hour>=start||clock.Hour<end);
        }
        public int GiftPoints(string resource)=>System.Array.IndexOf(loved,resource)>=0?35:System.Array.IndexOf(liked,resource)>=0?20:System.Array.IndexOf(disliked,resource)>=0?-5:5;
    }
}
