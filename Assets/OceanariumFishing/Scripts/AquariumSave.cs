using System;
using System.Collections.Generic;
using System.Linq;
namespace OceanariumFishing
{
    [Serializable] public sealed class AquariumTankSave
    {public string id,definitionId;public string substrate="sub_white";public bool decorated;public int upgradeLevel;}
    [Serializable] public sealed class CoinTransaction
    {public string id,kind,utc;public int amount;}
    [Serializable] public sealed class AquariumSave
    {
        public int version=1,coins,pending,totalCollected,visitors;public float popularity;public AquariumLayout layout=new();
        public List<AquariumTankSave> tanks=new();public List<string> upgrades=new();public List<CoinTransaction> transactions=new();
        public void Normalize(){layout??=new();layout.Normalize();tanks??=new();upgrades??=new();transactions??=new();coins=Math.Max(0,coins);pending=Math.Max(0,pending);upgrades=upgrades.Distinct().ToList();}
    }
    // Local implementation below; a future host transport must authenticate owner/session commands.
    public interface IAquariumAuthority
    {bool Place(string owner,string specimen,string tank,out string reason);bool Remove(string owner,string specimen,out string reason);bool Collect(out string reason);bool Purchase(string id,out string reason);}
    public sealed class AquariumScore
    {public float diversity,rarity,quality,variety,habitat,decor,upgrades;public float Total=>diversity+rarity+quality+variety+habitat+decor+upgrades;}
}

