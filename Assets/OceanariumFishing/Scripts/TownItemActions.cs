using System;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed partial class TownTransactions
    {
        public bool UseItem(string owner,string command,int revision,string npc,string item,out string error)
        {
            if(!Begin(owner,command,revision,npc,out var data,out _,out error))return false;
            if(Count(data,item)<1){error="inventory";return false;}
            switch(item)
            {
                case "island_tea":case "island_cookie":case "bait_dough":
                    Add(data,item,-1);data.town.buff=item;data.town.buffSeconds=300;break;
                case "exclusive_outfit":data.town.cosmeticSlot="top";data.town.cosmeticItem="sweater";break;
                case "camera_filter":if(!data.town.flags.Contains("photo-filter:warm"))data.town.flags.Add("photo-filter:warm");break;
                case "treasure_clue":case "fishing_clue":case "cove_clue":
                    if(!data.town.flags.Contains("read:"+item))data.town.flags.Add("read:"+item);break;
                case "hull_component":error="future_component";return false;
                default:error="passive_item";return false;
            }
            return Commit(data,command,out error);
        }
    }
}
