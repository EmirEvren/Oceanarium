using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace OceanariumFishing
{
    [Serializable] public sealed class SavedLocation
    {
        public string island="OF_HomeIsland", context="arrival";
        public Vector3 position; public float heading; public bool valid;
    }
    [Serializable] public sealed class WorldPlayerRecord
    {
        public string identity, displayName; public AppearanceData appearance=new();
        // Reuses the existing inventory, equipment purchases, quests and encyclopedia.
        public FishCollection collection=new(); public SavedLocation location=new(); public bool starterRod,appearanceChosen;
    }
    [Serializable] public sealed class SharedWorldData
    {
        public AquariumSave aquarium=new(); public TravelProgress travel=new(); public WorldState clock=new();
        public List<string> unlockedIslands=new(){"OF_HomeIsland"};
        public List<FishInstance> displayedFish=new();
        public void Capture(FishCollection data)
        {
            var copy=data.Copy();aquarium=copy.aquarium;travel=copy.travel;clock=copy.world??new();unlockedIslands=copy.unlockedIslands;
            displayedFish.RemoveAll(f=>f.ownerId==data.ownerId);
            displayedFish.AddRange(copy.kept.Where(f=>f.ownership==FishOwnership.Displayed));
        }
        public void Apply(FishCollection data)
        {var copy=JsonUtility.FromJson<SharedWorldData>(JsonUtility.ToJson(this));data.aquarium=copy.aquarium;data.travel=copy.travel;data.world=copy.clock;data.unlockedIslands=copy.unlockedIslands;}
    }
    [Serializable] public sealed class WorldSlot
    {
        public const int Schema=1;
        public int version=Schema;public string worldId=Guid.NewGuid().ToString("N"),name,createdUtc=DateTime.UtcNow.ToString("O"),lastPlayedUtc,ownerIdentity;
        public SharedWorldData shared=new();public List<WorldPlayerRecord> players=new();
        public WorldPlayerRecord Find(string identity)=>players.Find(p=>p.identity==identity);
        public WorldPlayerRecord GetOrCreate(string identity,string displayName)
        {
            if(!WorldSlotStore.ValidIdentity(identity))throw new ArgumentException("Invalid player identity");
            var p=Find(identity);if(p!=null)return p;
            p=new(){identity=identity,displayName=NetworkNames.Clean(displayName)};p.collection.Normalize();players.Add(p);return p;
        }
        public FishCollection CollectionFor(WorldPlayerRecord player)
        {var data=player.collection.Copy();shared.Apply(data);data.Normalize();return data;}
        public void StoreCollection(WorldPlayerRecord player,FishCollection data,bool sharedAuthority)
        {
            if(sharedAuthority)shared.Capture(data);
            player.collection=data.Copy();player.collection.aquarium=null;player.collection.travel=null;player.collection.world=null;player.collection.unlockedIslands=null;
        }
    }
    public static class NetworkNames
    {
        public static string Clean(string value)
        {var chars=(value??"").Where(c=>!char.IsControl(c)&&c!='<'&&c!='>'&&c!='\u202e'&&c!='\u202d').Take(32).ToArray();var text=new string(chars).Trim();return text.Length==0?"Player":text;}
    }
    public static class WorldSlotStore
    {
        [Serializable] sealed class Envelope {public int format=1;public string payload,sha256;}
        public static string Root=>Path.Combine(Application.persistentDataPath,"Worlds");
        public static bool ValidIdentity(string id)=>id=="local"||(id!=null&&id.StartsWith("dev-")&&Guid.TryParseExact(id.Substring(4),"N",out _))||(ulong.TryParse(id,out var steam)&&steam>0);
        public static string FileName(string id,string root=null)
        {if(!Guid.TryParseExact(id,"N",out _))throw new ArgumentException("Invalid world ID");return Path.Combine(root??Root,id,"WorldSave.json");}
        static string Hash(string data){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-","");}
        static WorldSlot Decode(string json)
        {
            var e=JsonUtility.FromJson<Envelope>(json);if(e==null||e.format!=1||string.IsNullOrEmpty(e.payload)||Hash(e.payload)!=e.sha256)throw new InvalidDataException("save_invalid");
            var w=JsonUtility.FromJson<WorldSlot>(e.payload);if(w==null||w.version!=WorldSlot.Schema)throw new NotSupportedException("save_version");
            if(!Guid.TryParseExact(w.worldId,"N",out _)||w.shared==null||w.players==null||w.players.Any(p=>p==null||!ValidIdentity(p.identity)||p.collection==null||p.appearance==null)||w.players.Select(p=>p.identity).Distinct().Count()!=w.players.Count)throw new InvalidDataException("save_invalid");
            if(w.shared.aquarium==null||w.shared.travel==null||w.shared.clock==null||w.shared.unlockedIslands==null||w.shared.displayedFish==null)throw new InvalidDataException("save_invalid");
            if(!ValidIdentity(w.ownerIdentity)||w.Find(w.ownerIdentity)==null)throw new InvalidDataException("save_invalid");
            if(w.shared.aquarium.version!=1||w.shared.aquarium.layout?.version!=1||w.shared.travel.version!=1||w.shared.clock.version!=1)throw new NotSupportedException("save_version");
            var fishIds=new HashSet<string>();foreach(var p in w.players){if(p.collection.version<1||p.collection.version>FishCollection.CurrentVersion||p.collection.town?.version!=1)throw new NotSupportedException("save_version");foreach(var fish in p.collection.kept??new())if(fish==null||!fish.Valid||!fishIds.Add(fish.instanceId))throw new InvalidDataException("save_invalid");}
            return w;
        }
        public static WorldSlot Load(string id,out string status,string root=null)
        {
            string path=FileName(id,root);status="saved";
            try{var world=Decode(File.ReadAllText(path));if(world.worldId!=id)throw new InvalidDataException();return world;}
            catch(NotSupportedException){throw;}
            catch(Exception e)when(e is IOException||e is ArgumentException)
            {
                // A future schema is never replaced with an older backup. An interrupted first save may recover its valid tmp.
                string recovery=File.Exists(path+".bak")?path+".bak":!File.Exists(path)&&File.Exists(path+".tmp")?path+".tmp":null;
                if(recovery==null)throw new InvalidDataException("save_invalid",e);
                var world=Decode(File.ReadAllText(recovery));if(world.worldId!=id)throw new InvalidDataException("save_invalid");
                if(File.Exists(path))File.Copy(path,path+".corrupt",true);
                string restore=path+".restore";File.WriteAllText(restore,File.ReadAllText(recovery));if(File.Exists(path))File.Replace(restore,path,null);else File.Move(restore,path);
                status="recovered";return world;
            }
        }
        public static bool Save(WorldSlot world,out string status,string root=null)
        {
            try
            {
                if(world.version!=WorldSlot.Schema)throw new NotSupportedException();
                world.lastPlayedUtc=DateTime.UtcNow.ToString("O");var payload=JsonUtility.ToJson(world);string text=JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)},true);Decode(text);
                string p=FileName(world.worldId,root);Directory.CreateDirectory(Path.GetDirectoryName(p));
                using(var stream=new FileStream(p+".tmp",FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
                {var bytes=Encoding.UTF8.GetBytes(text);stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(p))File.Replace(p+".tmp",p,p+".bak");else File.Move(p+".tmp",p);status="saved";return true;
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException){status="save_failed";return false;}
        }
        public static List<WorldSlot> List(out bool unreadable,string root=null)
        {
            unreadable=false;var list=new List<WorldSlot>();if(!Directory.Exists(root??Root))return list;
            foreach(var dir in Directory.GetDirectories(root??Root)){var id=Path.GetFileName(dir);if(!Guid.TryParseExact(id,"N",out _))continue;try{list.Add(Load(id,out _,root));}catch{unreadable=true;}}
            return list.OrderByDescending(w=>w.lastPlayedUtc).ToList();
        }
        public static WorldSlot Create(string name,string identity,bool importLegacy=false,string root=null)
        {
            var world=new WorldSlot{name=NetworkNames.Clean(name),ownerIdentity=identity};var player=world.GetOrCreate(identity,"Player");
            if(importLegacy)
            {
                var data=FishCollectionStore.Load(FishCollectionStore.LegacyPath);if(data.writeBlocked)throw new InvalidDataException("save_invalid");
                player.appearance=AppearanceStore.Load(AppearanceStore.LegacyPath);player.appearanceChosen=true;player.starterRod=data.TotalCaught>0;world.StoreCollection(player,data,true);
            }
            if(!Save(world,out _,root))throw new IOException("save_failed");return world;
        }
    }
    // This adapter is the only default-save destination. Explicit paths remain available for legacy import and isolated tests.
    public static class WorldPersistence
    {
        public static WorldSlot Active{get;private set;}public static WorldPlayerRecord Local{get;private set;}
        public static bool ReadOnly{get;private set;}public static string LastSave="idle";
        static string rootOverride;
        public static void Open(WorldSlot world,string identity,string root=null)
        {
            Close();rootOverride=root;Active=world;Local=world.GetOrCreate(identity,"Player");ReadOnly=false;
            var service=WorldTimeService.Current;if(service)service.ApplyWorld(world.shared.clock,true);
        }
        public static void Receive(WorldPlayerRecord record,SharedWorldData shared,string worldId,string name)
        {
            ReadOnly=true;Local=record;Active=new(){worldId=worldId,name=name,shared=shared,players=new(){record}};
            if(Application.isPlaying)WorldTimeService.Ensure(LoadCollection())?.ApplyWorld(shared.clock,false);
        }
        public static FishCollection LoadCollection()=>Active.CollectionFor(Local);
        public static bool SaveCollection(FishCollection data)
        {
            if(ReadOnly||Active==null||Local==null||data.ownerId!=Local.collection.ownerId)return false;
            var previousCollection=Local.collection;var previousShared=JsonUtility.FromJson<SharedWorldData>(JsonUtility.ToJson(Active.shared));Active.StoreCollection(Local,data,true);
            if(Save())return true;Local.collection=previousCollection;Active.shared=previousShared;return false;
        }
        public static bool SaveAppearance(AppearanceData appearance)
        {if(ReadOnly||Local==null)return false;var before=Local.appearance;bool chosen=Local.appearanceChosen;Local.appearance=appearance.Copy();Local.appearance.Validate();Local.appearanceChosen=true;if(Save())return true;Local.appearance=before;Local.appearanceChosen=chosen;return false;}
        public static bool Save()
        {if(ReadOnly||Active==null)return false;return WorldSlotStore.Save(Active,out LastSave,rootOverride);}
        public static void Close(){Active=null;Local=null;ReadOnly=false;rootOverride=null;}
    }
}
