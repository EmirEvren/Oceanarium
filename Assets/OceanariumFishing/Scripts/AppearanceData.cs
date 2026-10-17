using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    [Serializable] public sealed class AppearanceData
    {
        public int version=1;
        public string presentation="feminine",hair="wavy_bob",hairColor="brown",skin="warm_beige",eyes="gentle",eyeColor="green",brows="soft_curved",browColor="brown",mouth="small_smile",nose="button",detail="blush",beard="none",beardColor="brown",top="fisherman_shirt",bottom="shorts",shoes="sneakers",hat="none",glasses="none",accessory="none",outfitColor="seafoam";
        public bool matchBrows=true;
        public AppearanceData Copy()=>JsonUtility.FromJson<AppearanceData>(JsonUtility.ToJson(this));
        public string Get(string slot)=>(string)typeof(AppearanceData).GetField(slot).GetValue(this);
        public void Set(string slot,string id)=>typeof(AppearanceData).GetField(slot).SetValue(this,id);
        public void Validate()
        {
            var defaults=new AppearanceData();
            foreach(var pair in CosmeticCatalog.Options)if(Array.IndexOf(pair.Value,Get(pair.Key))<0)Set(pair.Key,defaults.Get(pair.Key));
            version=1;
        }
        public static AppearanceData Randomized()
        {
            var data=new AppearanceData();foreach(var p in CosmeticCatalog.Options)data.Set(p.Key,p.Value[UnityEngine.Random.Range(0,p.Value.Length)]);
            if(UnityEngine.Random.value<.65f)data.beard="none";
            if(UnityEngine.Random.value<.65f)data.hat="none";
            data.beardColor=data.hairColor;data.browColor=data.hairColor;return data;
        }
    }
    public static class CosmeticCatalog
    {
        public static readonly Dictionary<string,string[]> Options=new()
        {
            ["presentation"]=Split("masculine feminine"),
            ["hair"]=Split("short_classic messy_short side_swept curly_short medium_layered long_straight long_wavy high_ponytail low_ponytail twin_tails short_bob wavy_bob braided double_buns shoulder_layers"),
            ["hairColor"]=Split("black dark_brown brown light_brown blonde platinum ginger auburn white silver pink pastel_pink blue pastel_blue purple mint"),
            ["skin"]=Split("porcelain ivory peach warm_beige golden tan caramel bronze umber cocoa deep_brown ebony"),
            ["eyes"]=Split("round soft_oval happy sleepy wide gentle"),["eyeColor"]=Split("brown dark_brown hazel green light_green blue light_blue gray amber violet"),
            ["brows"]=Split("straight soft_curved thick thin raised rounded"),["browColor"]=Split("black dark_brown brown light_brown blonde platinum ginger auburn white silver pink pastel_pink blue pastel_blue purple mint"),
            ["mouth"]=Split("small_smile neutral cheerful soft_smile tiny_open_smile relaxed"),["nose"]=Split("button soft pointed"),["detail"]=Split("none freckles blush beauty_mark cheek_detail"),
            ["beard"]=Split("none light_stubble short_beard full_beard mustache goatee"),["beardColor"]=Split("black dark_brown brown light_brown blonde platinum ginger auburn white silver pink pastel_pink blue pastel_blue purple mint"),
            ["top"]=Split("basic_tshirt striped_tshirt sweater fisherman_shirt hoodie jacket"),["bottom"]=Split("shorts denim_shorts pants cargo_pants short_skirt long_skirt"),
            ["shoes"]=Split("sneakers simple_boots fisherman_boots sandals cozy_shoes"),["hat"]=Split("none baseball_cap straw_hat fisherman_hat beanie bucket_hat"),["glasses"]=Split("none round_glasses square_glasses sunglasses"),
            ["accessory"]=Split("none backpack crossbody necklace bracelet"),["outfitColor"]=Split("seafoam coral cream navy lavender ochre")
        };
        static string[] Split(string s)=>s.Split(' ');
        public static readonly Dictionary<string,string> Hex=new()
        {
            ["black"]="25252D",["dark_brown"]="36251F",["brown"]="704737",["light_brown"]="A5734F",["blonde"]="E4BB66",["platinum"]="F6E2AA",["ginger"]="C76D36",["auburn"]="7B3930",["white"]="FAEEDB",["silver"]="B6C2CF",["pink"]="D96E9D",["pastel_pink"]="F4B5C4",["blue"]="406BAA",["pastel_blue"]="A8CEDE",["purple"]="8B6BA8",["mint"]="9FD8BD",
            ["porcelain"]="F9DFC9",["ivory"]="F1CEAD",["peach"]="EAB897",["warm_beige"]="D9A27C",["golden"]="C99058",["tan"]="B97B51",["caramel"]="A46B45",["bronze"]="925C3D",["umber"]="7B4C37",["cocoa"]="68412F",["deep_brown"]="503227",["ebony"]="38271F",
            ["hazel"]="928047",["green"]="437A65",["light_green"]="8CB87A",["light_blue"]="8ABDCF",["gray"]="87949F",["amber"]="C58C35",["violet"]="8978AF",
            ["seafoam"]="66B5A4",["coral"]="D98270",["cream"]="F4DBAF",["navy"]="384F6A",["lavender"]="A79BC1",["ochre"]="D3A34C"
        };
        public static Color ColorOf(string id){ColorUtility.TryParseHtmlString("#"+(Hex.TryGetValue(id,out var hex)?hex:"FFFFFF"),out var color);return color;}
    }
    public static class AppearanceStore
    {
        public static string PathName=>Path.Combine(Application.persistentDataPath,"appearance-v1.json");
        public static AppearanceData Load(string path=null)
        {
            try{var p=path??PathName;if(!File.Exists(p))return new();var text=File.ReadAllText(p);var d=JsonUtility.FromJson<AppearanceData>(text);if(d==null || d.version>1 || d.version<0)return new();d.Validate();return d;}
            catch(Exception e) when(e is IOException || e is ArgumentException || e is UnauthorizedAccessException){return new();}
        }
        public static bool Save(AppearanceData data,out string error,string path=null)
        {
            try{var p=path??PathName;var safe=data.Copy();safe.Validate();Directory.CreateDirectory(Path.GetDirectoryName(p));var tmp=p+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(safe,true));if(File.Exists(p))File.Replace(tmp,p,p+".bak");else File.Move(tmp,p);error=null;return true;}
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException){error=e.Message;return false;}
        }
    }
}
