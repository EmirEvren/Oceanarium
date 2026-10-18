using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace OceanariumFishing.Editor
{
    public static partial class TownBuilder
    {
        static void ExpandProducts(TownWorld world)
        {
            void Add(int npc,string id,string label,int price,string build=null,string currency="coins",string slot=null,string cosmetic=null,int hearts=0)
            {var p=Asset<TownProductDefinition>(id);p.id=p.resourceId=id;p.nameKey="town.item."+id;p.price=price;p.quantity=1;p.stockLimit=slot==null?20:1;p.currency=currency;p.requiredHearts=hearts;p.cosmeticSlot=slot;p.cosmeticItem=cosmetic;p.buildItem=build==null?null:AssetDatabase.LoadAssetAtPath<AquariumBuildItem>(Root+"/Building/"+build+".asset");var list=new List<TownProductDefinition>(world.residents[npc].products);list.Add(p);world.residents[npc].products=list.ToArray();Text("item."+id,label);EditorUtility.SetDirty(p);}
            Add(2,"island_cookie","Island cookie · five-minute reel assistance",3);Add(2,"bait_dough","Bait dough · five-minute larger-catch luck",4);
            Add(1,"river_sand","Natural river sand",3,"sub_1");Add(1,"botanist_kelp","Decorative kelp",4,"plant_9");Add(1,"anemone","Decorative sea anemone",5,"decor_3");
            Add(3,"antique_anchor","Antique anchor",5,"decor_9");Add(3,"treasure_chest","Recovered treasure chest",3,"decor_8","rare_shell");Add(3,"treasure_clue","Coastal treasure notes · read from items",4);
            Add(4,"pearl_frame","Pearl trader's frame",1,"furniture_12","sea_pearl");Add(4,"gull_hat","Night merchant's beanie",3,null,"rare_shell","hat","beanie");
            Add(5,"salvage_cabinet","Reclaimed cabinet",3,"furniture_8","driftwood");Add(5,"hull_component","Hull upgrade component · reserved for future boat tier",4,null,"scrap");
            Add(6,"camera_filter","Warm camera filter · activate from items",5);Add(6,"photo_stand","Photo display pedestal",6,"furniture_7");
            Add(7,"tailored_boots","Fisherman boots",8,null,"coins","shoes","fisherman_boots");Add(7,"tailored_skirt","Long skirt",9,null,"coins","bottom","long_skirt");Add(7,"tailored_glasses","Round glasses",6,null,"coins","glasses","round_glasses");Add(7,"tailored_bag","Crossbody bag",9,null,"coins","accessory","crossbody");
            Add(8,"reef_stone","Reef stone",4,"decor_4");Add(8,"aquarium_lantern","Warm aquarium floor lantern",6,"furniture_9");Add(11,"oak_cabinet","Oak aquarium cabinet",7,"furniture_8");Add(11,"oak_floor","Warm oak flooring",5,"floor_oak");Add(11,"wall_shelf","Wall shelf",5,"furniture_6");
            Text("use","Use / read","Kullan / oku");Text("error.passive_item","This item is equipped automatically or used through its matching activity.");Text("error.future_component","Keep this component for a future boat upgrade. No island has been unlocked.");
            foreach(var pair in new[]{("rare_plant","Rare aquatic plant"),("sea_pearl","Sea pearl"),("exclusive_outfit","Exclusive cozy sweater"),("luminous_decor","Luminous aquarium decoration"),("fishing_clue","Rocky-shore fishing notes"),("cove_clue","Hidden cove notes"),("premium_furniture","Premium aquarium cabinet")})Text("item."+pair.Item1,pair.Item2);
            Text("item.treasure_clue.description","Coastal finds wash up along the town harbor trail. Search both sides of the approach for shells and recovered materials.");Text("item.fishing_clue.description","Try the rocky shore on Home Island: habitat changes the species available to your rod.");Text("item.cove_clue.description","Coral Cove appears on Finn's chart, but requires future navigation upgrades. This note does not unlock travel.");
        }
    }
}
