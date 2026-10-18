using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    public enum BuildCategory { Tanks, Plants, Corals, Substrates, Decorations, Furniture, Floors, Walls, Expansions }


    [Serializable] public sealed class AquariumLayoutObject
    {
        public string id,typeId,tankId,roomId="starter";public Vector3 position;public float rotation;public bool stored=true,legacy;
    }
    [Serializable] public sealed class AquariumLayout
    {
        public int version=1,revision,level=1;public string floor="floor_oak",wall="wall_cream";
        public List<AquariumLayoutObject> objects=new();public List<string> purchases=new();public List<string> commands=new();
        public void Normalize(){objects??=new();purchases??=new();commands??=new();level=Mathf.Clamp(level,1,3);}
    }
    // Transport adapters must authenticate owner and carry an expected revision and unique command ID.
    public interface IAquariumBuildAuthority
    {
        bool Buy(string owner,string command,int revision,string type,out string objectId,out string reason);
        bool PlaceObject(string owner,string command,int revision,string id,Vector3 position,float rotation,string tank,out string reason);
        bool StoreObject(string owner,string command,int revision,string id,out string reason);
    }


}

