using System.IO;
using UnityEditor;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static partial class TownBuilder
    {
        static void Portrait(TownResidentDefinition npc)
        {
            // Unity-generated small portrait badges, matching each resident's saved palette.
            var t=new Texture2D(96,96,TextureFormat.RGBA32,false);var skin=CosmeticCatalog.ColorOf(npc.appearance.skin);var hair=CosmeticCatalog.ColorOf(npc.appearance.hairColor);var cloth=CosmeticCatalog.ColorOf(npc.appearance.outfitColor);
            for(int y=0;y<96;y++)for(int x=0;x<96;x++){float dx=(x-48)/29f,dy=(y-55)/31f;Color c=new(.86f,.9f,.79f);if(y<33&&Mathf.Abs(x-48)<30)c=cloth;if(dx*dx+dy*dy<1)c=skin;if(dx*dx+dy*dy<1.1f&&y>72)c=hair;if((x-37)*(x-37)+(y-58)*(y-58)<8||(x-59)*(x-59)+(y-58)*(y-58)<8)c=new(.12f,.17f,.17f);if(y>=42&&y<=44&&x>41&&x<55)c=new(.5f,.24f,.2f);t.SetPixel(x,y,c);}t.Apply();string path=Dir+"/"+npc.id+"_portrait.png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);npc.portrait=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
