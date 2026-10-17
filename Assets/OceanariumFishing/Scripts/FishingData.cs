using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace OceanariumFishing
{
    public enum FishRarity {Common,Uncommon,Rare,Epic,Legendary}
    public enum FishBehavior {Calm,Darting,Heavy,Erratic,Stubborn}
    public enum FishHabitat {Beach,Rocky,Pier,Deep}



    [Serializable] public sealed class FishInstance
    {
        public string instanceId,speciesId,caughtUtc,location;public int seed;public float weight,length,quality,value;public FishRarity rarity;public bool aquariumEligible;
        public static FishInstance Roll(FishSpecies species,int seed,string location)
        {
            var random=new System.Random(seed);float size=Mathf.Pow((float)random.NextDouble(),1.8f);float length=Mathf.Lerp(species.minLength,species.maxLength,size);float ratio=length/species.maxLength;
            float weight=Mathf.Clamp(species.maxWeight*ratio*ratio*ratio*Mathf.Lerp(.94f,1.04f,(float)random.NextDouble()),species.minWeight,species.maxWeight);
            return new FishInstance{instanceId=Guid.NewGuid().ToString("N"),speciesId=species.id,seed=seed,weight=weight,length=length,quality=size,value=species.baseValue*(.75f+size*.75f),rarity=species.rarity,aquariumEligible=species.aquariumEligible,location=location,caughtUtc=DateTime.UtcNow.ToString("O")};
        }
    }
    [Serializable] public sealed class FishRecord {public string speciesId;public float weight,length;}
    [Serializable] public sealed class FishCollection
    {
        public int version=1;public List<FishInstance> kept=new();public List<FishRecord> records=new();public List<string> resolved=new();
        public bool IsRecord(FishInstance fish)=>!records.Exists(r=>r.speciesId==fish.speciesId && r.weight>=fish.weight);
        public bool Resolve(FishInstance fish,bool keep)
        {
            if(fish==null || string.IsNullOrEmpty(fish.instanceId) || resolved.Contains(fish.instanceId))return false;
            resolved.Add(fish.instanceId);if(keep)kept.Add(fish);var record=records.Find(r=>r.speciesId==fish.speciesId);if(record==null){record=new FishRecord{speciesId=fish.speciesId};records.Add(record);}record.weight=Mathf.Max(record.weight,fish.weight);record.length=Mathf.Max(record.length,fish.length);return true;
        }
    }
    public static class FishCollectionStore
    {
        public static string PathName=>Path.Combine(Application.persistentDataPath,"fish-collection-v1.json");
        public static FishCollection Load(string path=null)
        {
            try{if(!File.Exists(path??PathName))return new();var d=JsonUtility.FromJson<FishCollection>(File.ReadAllText(path??PathName));if(d==null || d.version!=1 || d.kept==null || d.records==null || d.resolved==null)return new();d.kept.RemoveAll(f=>f==null || string.IsNullOrEmpty(f.instanceId) || f.weight<=0 || f.length<=0 || float.IsNaN(f.weight));return d;}catch(Exception e)when(e is IOException || e is ArgumentException || e is UnauthorizedAccessException){return new();}
        }
        public static bool Save(FishCollection data,string path=null)
        {try{string p=path??PathName;Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllText(p+".tmp",JsonUtility.ToJson(data,true));if(File.Exists(p))File.Replace(p+".tmp",p,p+".bak");else File.Move(p+".tmp",p);return true;}catch(Exception e)when(e is IOException || e is UnauthorizedAccessException){Debug.LogWarning("Fish collection save failed: "+e.Message);return false;}}
    }
    [Serializable] public sealed class FishSpawnEntry {public FishSpecies species;public float weight=1;}

}
