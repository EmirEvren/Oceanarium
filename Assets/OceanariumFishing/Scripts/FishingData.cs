using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace OceanariumFishing
{
    public enum FishRarity {Common,Uncommon,Rare,Epic,Legendary}
    public enum FishBehavior {Calm,Darting,Heavy,Erratic,Stubborn}
    public enum FishHabitat {Beach,Rocky,Pier,Deep}
    public enum FishOwnership {Pending,Stored,Displayed,Sold,Transferred,Released}
    [Serializable] public sealed class FishInstance
    {
        // Legacy fields retain their names so v1 specimens deserialize without losing metadata.
        public string instanceId,speciesId,caughtUtc,location;public int seed;public float weight,length,quality,value;public FishRarity rarity;public bool aquariumEligible;
        public string ownerId,islandId,zoneId,captureMethod="rod",tankId="";public FishOwnership ownership;
        public FishInstance Copy()=>JsonUtility.FromJson<FishInstance>(JsonUtility.ToJson(this));
        public bool Valid=>!string.IsNullOrWhiteSpace(instanceId)&&!string.IsNullOrWhiteSpace(speciesId)&&Finite(weight)&&weight>0&&Finite(length)&&length>0&&Finite(quality);
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        public static FishInstance Roll(FishSpecies species,int seed,string location)
        {
            var random=new System.Random(seed);float size=Mathf.Pow((float)random.NextDouble(),1.8f);float length=Mathf.Lerp(species.minLength,species.maxLength,size);float ratio=length/species.maxLength;
            float weight=Mathf.Clamp(species.maxWeight*ratio*ratio*ratio*Mathf.Lerp(.94f,1.04f,(float)random.NextDouble()),species.minWeight,species.maxWeight);
            var f=new FishInstance{instanceId=Guid.NewGuid().ToString("N"),speciesId=species.id,seed=seed,weight=weight,length=length,quality=size,rarity=species.rarity,aquariumEligible=species.aquariumEligible,location=location,zoneId=location,caughtUtc=DateTime.UtcNow.ToString("O")};
            f.value=FishValuation.Estimate(species,f);return f;
        }
    }
    public static class FishValuation
    {
        // Species baseValue already reflects rarity; do not apply another rarity multiplier.
        public static float Estimate(FishSpecies species,FishInstance fish)
        {
            float weight=Mathf.InverseLerp(species.minWeight,species.maxWeight,fish.weight),length=Mathf.InverseLerp(species.minLength,species.maxLength,fish.length);
            return Mathf.Round(species.baseValue*(.75f+.35f*weight+.2f*length+.2f*Mathf.Clamp01(fish.quality))*100)/100;
        }
    }
    [Serializable] public sealed class FishRecord
    {
        public string speciesId,firstCaughtUtc;public float weight,length;public int count;public bool incompleteLegacyHistory;
        public List<string> islands=new();
    }
    [Serializable] public sealed class FishCollection
    {
        public const int CurrentVersion=3;
        public WorldState world;
        public AquariumSave aquarium=new();public TravelProgress travel=new();public List<ResourceStack> resources=new();public TownProgress town=new();
        public int version=CurrentVersion;public List<FishInstance> kept=new();public List<FishRecord> records=new();public List<string> resolved=new();
        public List<FishInstance> history=new();public string ownerId;public string difficulty="cozy";public bool tutorialComplete;public int legacyUnattributedCatches;
        public List<string> unlockedIslands=new(){"OF_HomeIsland"};
        [NonSerialized]public bool writeBlocked;
        [NonSerialized]public string loadWarning;
        public int TotalCaught=>history.Count+legacyUnattributedCatches;
        public int DiscoveredCount=>records.Count;
        public FishCollection Copy(){var d=JsonUtility.FromJson<FishCollection>(JsonUtility.ToJson(this));d.writeBlocked=writeBlocked;d.loadWarning=loadWarning;return d;}
        public bool Discovered(string species)=>records.Exists(r=>r.speciesId==species);
        public bool IsRecord(FishInstance fish)=>!records.Exists(r=>r.speciesId==fish.speciesId && r.weight>=fish.weight && r.length>=fish.length);
        public FishRecord Record(string species)=>records.Find(r=>r.speciesId==species);
        public bool RecordCatch(FishInstance fish)
        {
            if(fish==null || !fish.Valid || fish.ownerId!=ownerId || resolved.Contains(fish.instanceId) || history.Exists(f=>f.instanceId==fish.instanceId))return false;
            history.Add(fish.Copy());var record=Record(fish.speciesId);
            if(record==null){record=new FishRecord{speciesId=fish.speciesId,firstCaughtUtc=fish.caughtUtc};records.Add(record);}
            record.count++;record.weight=Mathf.Max(record.weight,fish.weight);record.length=Mathf.Max(record.length,fish.length);
            if(!string.IsNullOrEmpty(fish.islandId)&&!record.islands.Contains(fish.islandId))record.islands.Add(fish.islandId);
            return true;
        }
        public bool Resolve(FishInstance fish,bool keep)
        {
            if(fish==null || !fish.Valid || fish.ownerId!=ownerId || resolved.Contains(fish.instanceId))return false;
            RecordCatch(fish);resolved.Add(fish.instanceId);
            if(keep){var owned=fish.Copy();owned.ownership=FishOwnership.Stored;owned.tankId="";kept.Add(owned);}return true;
        }
        public bool TryMove(string instanceId,string requestingOwner,FishOwnership next,string tank="",string recipient="")
        {
            var fish=kept.Find(f=>f.instanceId==instanceId);
            if(fish==null || requestingOwner!=ownerId || fish.ownerId!=requestingOwner)return false;
            if(next==FishOwnership.Displayed && fish.ownership==FishOwnership.Stored && fish.aquariumEligible && !string.IsNullOrWhiteSpace(tank)){fish.ownership=next;fish.tankId=tank;return true;}
            if(next==FishOwnership.Stored && fish.ownership==FishOwnership.Displayed){fish.ownership=next;fish.tankId="";return true;}
            if((next==FishOwnership.Sold || next==FishOwnership.Released || next==FishOwnership.Transferred) && fish.ownership==FishOwnership.Stored)
            {
                if(next==FishOwnership.Transferred && (string.IsNullOrWhiteSpace(recipient)||recipient==ownerId))return false;
                fish.ownership=next;fish.tankId="";if(next==FishOwnership.Transferred)fish.ownerId=recipient;kept.Remove(fish);return true;
            }
            return false;
        }
        public IEnumerable<FishInstance> Query(string sort,string island=null,int rarity=-1)
        {
            IEnumerable<FishInstance> list=kept.Where(f=>(string.IsNullOrEmpty(island)||f.islandId==island)&&(rarity<0||(int)f.rarity==rarity));
            return sort switch {"weight"=>list.OrderByDescending(f=>f.weight).ThenBy(f=>f.instanceId),"rarity"=>list.OrderByDescending(f=>f.rarity).ThenByDescending(f=>f.weight),"species"=>list.OrderBy(f=>f.speciesId).ThenByDescending(f=>f.weight),_=>list.OrderByDescending(f=>f.caughtUtc).ThenBy(f=>f.instanceId)};
        }
        public void Normalize()
        {
            bool legacy=version<2;
            if(string.IsNullOrWhiteSpace(ownerId))ownerId=Guid.NewGuid().ToString("N");
            kept??=new();records??=new();resolved??=new();history??=new();unlockedIslands??=new(){"OF_HomeIsland"};
            aquarium??=new();aquarium.Normalize();travel??=new();travel.Normalize();resources??=new();town??=new();town.Normalize();world?.Normalize();
            if(!unlockedIslands.Contains("OF_HomeIsland"))unlockedIslands.Add("OF_HomeIsland");
            if(difficulty!="cozy"&&difficulty!="normal"&&difficulty!="experienced")difficulty="cozy";
            kept=kept.Where(f=>f!=null&&f.Valid).GroupBy(f=>f.instanceId).Select(g=>g.First()).ToList();
            history=history.Where(f=>f!=null&&f.Valid).GroupBy(f=>f.instanceId).Select(g=>g.First()).ToList();
            resolved=resolved.Where(s=>!string.IsNullOrEmpty(s)).Distinct().ToList();
            records=records.Where(r=>r!=null&&!string.IsNullOrEmpty(r.speciesId)).GroupBy(r=>r.speciesId).Select(g=>new FishRecord{speciesId=g.Key,weight=g.Max(r=>r.weight),length=g.Max(r=>r.length),count=g.Max(r=>r.count),firstCaughtUtc=g.Select(r=>r.firstCaughtUtc).Where(s=>!string.IsNullOrEmpty(s)).OrderBy(s=>s).FirstOrDefault(),incompleteLegacyHistory=g.Any(r=>r.incompleteLegacyHistory),islands=g.SelectMany(r=>r.islands??new List<string>()).Distinct().ToList()}).ToList();
            foreach(var fish in kept)
            {
                if(string.IsNullOrEmpty(fish.ownerId))fish.ownerId=ownerId;
                if(string.IsNullOrEmpty(fish.zoneId))fish.zoneId=fish.location;
                if(string.IsNullOrEmpty(fish.islandId)&&fish.location?.StartsWith("home.")==true)fish.islandId="OF_HomeIsland";
                if(legacy || fish.ownership==FishOwnership.Pending)fish.ownership=FishOwnership.Stored;
                if(fish.ownership==FishOwnership.Displayed && string.IsNullOrEmpty(fish.tankId))fish.ownership=FishOwnership.Stored;
                if(!history.Exists(h=>h.instanceId==fish.instanceId))history.Add(fish.Copy());
                if(!resolved.Contains(fish.instanceId))resolved.Add(fish.instanceId);
            }
            kept.RemoveAll(f=>f.ownerId!=ownerId || (f.ownership!=FishOwnership.Stored&&f.ownership!=FishOwnership.Displayed));
            foreach(var group in history.GroupBy(f=>f.speciesId))
            {
                var r=Record(group.Key);if(r==null){r=new FishRecord{speciesId=group.Key};records.Add(r);}
                r.count=Math.Max(r.count,group.Count());r.weight=Mathf.Max(r.weight,group.Max(f=>f.weight));r.length=Mathf.Max(r.length,group.Max(f=>f.length));
                r.islands??=new();foreach(var island in group.Select(f=>f.islandId).Where(s=>!string.IsNullOrEmpty(s)).Distinct())if(!r.islands.Contains(island))r.islands.Add(island);
                if(string.IsNullOrEmpty(r.firstCaughtUtc)&&!legacy)r.firstCaughtUtc=group.Select(f=>f.caughtUtc).Where(s=>!string.IsNullOrEmpty(s)).OrderBy(s=>s).FirstOrDefault();
            }
            if(legacy)
            {
                legacyUnattributedCatches=Math.Max(0,resolved.Count-history.Count);
                foreach(var r in records){r.incompleteLegacyHistory=true;r.count=Math.Max(1,r.count);}
            }
            version=CurrentVersion;
        }
    }
    public static class FishCollectionStore
    {
        // Keep the existing path. Versioning is in the document; character/rod saves stay untouched.
        public static string PathName=>Path.Combine(Application.persistentDataPath,"fish-collection-v1.json");
        static FishCollection Read(string path)
        {
            var json=File.ReadAllText(path);if(!json.TrimStart().StartsWith("{")||!json.Contains("\"version\""))throw new ArgumentException("Invalid collection document");
            var data=JsonUtility.FromJson<FishCollection>(json);
            if(data==null||data.version<1||data.version>FishCollection.CurrentVersion)throw new ArgumentException("Unsupported collection schema");
            if(data.travel!=null&&data.travel.version!=1)throw new ArgumentException("Unsupported travel schema");
            if(data.world!=null&&data.world.version!=1)throw new ArgumentException("Unsupported world schema");
            if(data.town!=null&&data.town.version!=1)throw new ArgumentException("Unsupported town schema");
            if(data.aquarium!=null&&data.aquarium.version!=1)throw new ArgumentException("Unsupported aquarium schema");
            if(data.aquarium?.layout!=null&&data.aquarium.layout.version!=1)throw new ArgumentException("Unsupported aquarium layout schema");
            data.Normalize();return data;
        }
        public static FishCollection Load(string path=null)
        {
            string p=path??PathName;
            try{if(!File.Exists(p)){var empty=new FishCollection();empty.Normalize();return empty;}return Read(p);}
            catch(Exception e)when(e is IOException||e is ArgumentException||e is UnauthorizedAccessException)
            {
                // Never silently overwrite an unreadable/future save with a fresh empty inventory.
                Debug.LogWarning("Collection could not be read; source retained: "+e.Message);
                return new FishCollection{writeBlocked=true,loadWarning="collection.load_error"};
            }
        }
        public static bool Save(FishCollection data,string path=null)
        {
            if(data==null||data.writeBlocked)return false;
            try
            {
                string p=path??PathName;data.Normalize();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(p)));
                File.WriteAllText(p+".tmp",JsonUtility.ToJson(data,true));if(File.Exists(p))File.Replace(p+".tmp",p,p+".bak");else File.Move(p+".tmp",p);return true;
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){Debug.LogWarning("Fish collection save failed: "+e.Message);return false;}
        }
    }
    [Serializable] public sealed class FishSpawnEntry {public FishSpecies species;public float weight=1;}
}

