using UnityEngine;
namespace OceanariumFishing
{
    // Apply purchased tackle to a private rod copy; never mutate the shared asset or fight state.
    public sealed class TownEquipment:MonoBehaviour
    {
        FishingController fishing;FishingRodDefinition original,personal;int applied=-1;
        void Start(){fishing=GetComponent<FishingController>();original=fishing.rod;personal=Instantiate(original);fishing.rod=personal;Apply();}
        void Update(){if(!fishing)return;if(fishing.Collection.town.buffSeconds>0)fishing.Collection.town.buffSeconds=Mathf.Max(0,fishing.Collection.town.buffSeconds-Time.deltaTime);if(!fishing.Active&&applied!=fishing.Collection.town.revision)Apply();}
        void Apply()
        {
            var data=fishing.Collection;applied=data.town.revision;bool rod=TownTransactions.Count(data,"coastal_rod")>0,line=TownTransactions.Count(data,"braided_line")>0,hook=TownTransactions.Count(data,"balanced_hook")>0;
            personal.id=rod?"coastal":"starter";personal.castDistance=original.castDistance+(rod?3:0);personal.strength=original.strength*(rod?1.15f:1);personal.reelSpeed=original.reelSpeed*(rod?1.1f:1);personal.lineStrength=original.lineStrength*(line?1.2f:1);personal.handling=original.handling*(hook?1.12f:1);
            if(!string.IsNullOrEmpty(data.town.cosmeticSlot))
            {var appearance=AppearanceStore.Load();if(CosmeticCatalog.Options.TryGetValue(data.town.cosmeticSlot,out var choices)&&System.Array.IndexOf(choices,data.town.cosmeticItem)>=0){appearance.Set(data.town.cosmeticSlot,data.town.cosmeticItem);if(AppearanceStore.Save(appearance,out _)){GetComponentInChildren<CharacterAppearance>().Apply(appearance);var copy=data.Copy();copy.town.cosmeticSlot=null;copy.town.cosmeticItem=null;fishing.CommitAquarium(copy);}}}
        }
        void OnDestroy(){if(personal)Destroy(personal);}
    }
}
