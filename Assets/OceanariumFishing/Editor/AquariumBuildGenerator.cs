using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Localization.Tables;
namespace OceanariumFishing.Editor
{
    public static partial class AquariumBuildGenerator
    {
        const string Dir="Assets/OceanariumFishing/Building";static Material wood,metal,leaf,cream,glass,water,coral;static string assetId;static int meshIndex;
        static T Asset<T>(string id)where T:ScriptableObject{var p=Dir+"/"+id+".asset";var a=AssetDatabase.LoadAssetAtPath<T>(p);if(!a){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,p);}return a;}
        static Material Material(string id,Color color,int pattern=0)
        {
            var p=Dir+"/"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.2f);
            if(pattern>0){var t=new Texture2D(128,128);var colors=new Color[16384];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float n=Mathf.PerlinNoise(x*.12f,y*.12f)*.16f;float v=pattern==1?Mathf.Sin(x*.3f+Mathf.Sin(y*.1f))*.07f:pattern==2?(x%32<2||y%32<2?-.2f:0):pattern==3?Mathf.Sin(x*.15f)*Mathf.Sin(y*.15f)*.1f:0;colors[y*128+x]=Color.white*(.85f+n+v);}t.SetPixels(colors);t.Apply();var path=Dir+"/"+id+"_pattern.png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);m.mainTextureScale=new Vector2(2,2);}EditorUtility.SetDirty(m);return m;
        }
        static void Name(string id,string en,string tr=null){var root="Assets/OceanariumFishing/Localization/Tables/OF_UI_";foreach(var locale in new[]{"en","tr"}){var table=AssetDatabase.LoadAssetAtPath<StringTable>(root+locale+".asset");table.AddEntry("build."+id,locale=="tr"&&tr!=null?tr:en);EditorUtility.SetDirty(table);EditorUtility.SetDirty(table.SharedData);}}
        static AquariumBuildItem Entry(string id,string label,BuildCategory category,int price,Vector3 size)
        {var i=Asset<AquariumBuildItem>(id);i.id=id;i.category=category;i.price=price;i.size=size;i.level=1;i.futureLocked=false;i.requiredPopularity=0;i.popularity=category==BuildCategory.Tanks?3:1;i.theme="coastal";Name(id,label);return i;}
        static GameObject Root(string id){assetId=id;meshIndex=0;return new GameObject(id);}
        static void Finish(AquariumBuildItem item,GameObject root)
        {item.prefab=PrefabUtility.SaveAsPrefabAsset(root,Dir+"/"+item.id+".prefab");Thumbnail(item,root);UnityEngine.Object.DestroyImmediate(root);EditorUtility.SetDirty(item);}
        [MenuItem("Oceanarium Fishing/Build ASTRA 0.6 Catalog")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode");Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
            wood=Material("Oiled walnut",new(.43f,.26f,.16f),1);metal=Material("Brushed brass",new(.65f,.48f,.23f));leaf=Material("Aquatic emerald",new(.18f,.48f,.31f));cream=Material("Porcelain",new(.92f,.85f,.66f));coral=Material("Living coral",new(.88f,.39f,.32f));
            glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/OceanariumFishing/Aquarium/Clear aquarium glass.mat");water=AssetDatabase.LoadAssetAtPath<Material>("Assets/OceanariumFishing/Aquarium/Aquarium water.mat");
            var all=new List<AquariumBuildItem>();
            string[] tankNames={"Small Rectangular Tank","Medium Rectangular Tank","Large Showcase Tank","Round Glass Tank","Corner Tank","Tall Column Tank","Wall-Mounted Display Tank","Premium Panoramic Tank"};
            Vector3[] dims={new(1.8f,1.4f,1.1f),new(2.8f,1.7f,1.4f),new(4.1f,2.1f,2),new(1.8f,1.6f,1.8f),new(1.8f,1.5f,1.8f),new(1.25f,2.5f,1.25f),new(2.1f,1.1f,.65f),new(4,1.8f,2.2f)};
            int[] prices={12,28,65,22,20,30,24,100};int[] capacities={3,6,12,4,3,4,3,14};
            for(int n=0;n<8;n++){var item=Entry("tank_"+n,tankNames[n],BuildCategory.Tanks,prices[n],new(dims[n].x+.15f,dims[n].y+1,dims[n].z+.15f));item.level=n==2?2:n==7?3:1;item.requiredPopularity=n==7?45:0;item.wallMounted=n==6;var d=Asset<AquariumDefinition>(item.id+"_definition");d.id=item.id;d.buildType=item.id;d.capacity=capacities[n];d.size=n==2||n==7?AquariumTankSize.Large:n==1?AquariumTankSize.Medium:AquariumTankSize.Small;d.dimensions=dims[n];if(n==3||n==5)d.dimensions=new Vector3(dims[n].x*.66f,dims[n].y,dims[n].z*.66f);if(n==4)d.dimensions=new Vector3(.75f,dims[n].y,.75f);d.maxLength=n==2||n==7?130:n==1?75:40;d.footprint=new(item.size.x,item.size.z);d.waterVolume=dims[n].x*dims[n].y*dims[n].z*1000;d.decorationBudget=capacities[n]*3;d.habitats=new[]{AquariumHabitat.TropicalSaltwater,AquariumHabitat.TemperateSaltwater,AquariumHabitat.Freshwater};d.glass=glass;d.water=water;item.tank=d;EditorUtility.SetDirty(d);var root=Root(item.id);Tank(root.transform,n,dims[n],d);Finish(item,root);all.Add(item);}
            string[] plantNames={"Anubias","Java Moss","Amazon Sword","Vallisneria","Java Fern","Hornwort","Cryptocoryne","Red Aquatic Plant","Sea Grass","Decorative Kelp"};
            for(int n=0;n<10;n++){var i=Entry("plant_"+n,plantNames[n],BuildCategory.Plants,2+n%3,new(.22f+(n%3)*.04f,.3f+(n%4)*.055f,.22f+(n%3)*.04f));i.habitats=n<8?new[]{AquariumHabitat.Freshwater}:new[]{AquariumHabitat.TropicalSaltwater,AquariumHabitat.TemperateSaltwater};var root=Root(i.id);Plant(root.transform,n,i.size);root.AddComponent<AquariumSway>();Finish(i,root);all.Add(i);}
            string[] decorNames={"Branching Coral","Brain Coral","Mushroom Coral","Sea Anemone","Small Reef Rock","Large Reef Rock","Driftwood","Mini Shipwreck","Treasure Chest Decoration","Ancient Anchor","Seashell Cluster","Bubble Ornament"};
            for(int n=0;n<12;n++){var i=Entry("decor_"+n,decorNames[n],n<4?BuildCategory.Corals:BuildCategory.Decorations,3+n%4,new(n==5?.55f:.3f,n==5?.45f:.27f,n==5?.4f:.28f));i.decorationCost=n==5?3:1;i.habitats=n<4?new[]{AquariumHabitat.TropicalSaltwater,AquariumHabitat.TemperateSaltwater}:Array.Empty<AquariumHabitat>();var root=Root(i.id);Decoration(root.transform,n);root.transform.localScale=i.size;BakeRootScale(root);Finish(i,root);all.Add(i);}
            string[] substrates={"Fine White Sand","Natural River Sand","Black Volcanic Sand","Golden Beach Sand","Coral Gravel","Dark Pebbles","Nutrient-Rich Soil"};Color[] sands={new(.92f,.88f,.72f),new(.58f,.48f,.31f),new(.12f,.15f,.17f),new(.87f,.65f,.28f),new(.77f,.57f,.48f),new(.26f,.29f,.3f),new(.25f,.16f,.1f)};
            for(int n=0;n<7;n++){var i=Entry(n==0?"sub_white":"sub_"+n,substrates[n],BuildCategory.Substrates,n==0?0:3,Vector3.one);i.material=Material(i.id,sands[n],3);var root=Root(i.id);Substrate(root.transform,n,i.material);Finish(i,root);all.Add(i);}
            string[] furnishings={"Wooden Bench","Cozy Sofa","Visitor Chair","Small Coffee Table","Potted Palm","Hanging Plant","Wall Shelf","Display Pedestal","Wooden Cabinet","Floor Lantern","Wall Lantern","Aquarium Information Sign","Fish Painting Frame","Small Rug","Decorative Seashell Stand"};
            for(int n=0;n<15;n++){var i=Entry("furniture_"+n,furnishings[n],BuildCategory.Furniture,4+n%5,new(n==0||n==1?1.5f:n==13?1.6f:.65f,n==4?2:1,n==0||n==1?.65f:n==13?1:.6f));i.wallMounted=n==5||n==6||n==10||n==12;i.comfort=n<3?3:n==9||n==10?2:1;var root=Root(i.id);Furniture(root.transform,n);Finish(i,root);all.Add(i);}
            string[] floors={"Warm Oak","Rustic Wood","Dark Walnut","Light Stone","Coastal Tiles","Decorative Pebble Pattern"};Color[] floorColors={new(.48f,.29f,.17f),new(.53f,.4f,.27f),new(.23f,.14f,.11f),new(.72f,.72f,.64f),new(.37f,.64f,.64f),new(.55f,.59f,.52f)};
            string[] walls={"Cream Plaster","Warm Wood Panels","Ocean Blue","Pale Mint","Natural Stone","Coastal Wallpaper"};Color[] wallColors={new(.92f,.86f,.71f),new(.53f,.35f,.21f),new(.25f,.47f,.62f),new(.62f,.76f,.65f),new(.59f,.6f,.54f),new(.74f,.82f,.72f)};
            for(int n=0;n<6;n++)foreach(bool wall in new[]{false,true}){string id=wall?(n==0?"wall_cream":"wall_"+n):(n==0?"floor_oak":"floor_"+n);var i=Entry(id,wall?walls[n]:floors[n],wall?BuildCategory.Walls:BuildCategory.Floors,n==0?0:8,Vector3.one);i.material=Material(id,wall?wallColors[n]:floorColors[n],n<3?1:n==5?3:2);var root=Root(id);Part(root.transform,"Material sample",Vector3.zero,new Vector3(1,.08f,1),i.material);Finish(i,root);all.Add(i);}
            for(int n=1;n<=3;n++){var i=Entry("expansion_"+n,new[]{"Expanded Exhibit Hall","Ocean Gallery","Grand Aquarium (Future)"}[n-1],BuildCategory.Expansions,new[]{30,80,200}[n-1],Vector3.one);i.level=n;i.requiredPopularity=n==1?15:n==2?40:100;i.futureLocked=n==3;all.Add(i);EditorUtility.SetDirty(i);}
            var catalog=Asset<AquariumBuildCatalog>("BuildCatalog");catalog.items=all.ToArray();EditorUtility.SetDirty(catalog);
            var hall=Root("ModularHall");Hall(hall.transform);var hallPrefab=PrefabUtility.SaveAsPrefabAsset(hall,Dir+"/ModularHall.prefab");UnityEngine.Object.DestroyImmediate(hall);
            EditorSceneManager.OpenScene("Assets/OceanariumFishing/Scenes/OF_HomeIsland.unity");var world=UnityEngine.Object.FindFirstObjectByType<AquariumWorld>();var build=world.GetComponent<AquariumBuilding>()??world.gameObject.AddComponent<AquariumBuilding>();if(!world.GetComponent<AquariumBuildHUD>())world.gameObject.AddComponent<AquariumBuildHUD>();build.catalog=catalog;build.hallPrefab=hallPrefab;
            build.floorSurfaces=world.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Cedar floorboard").ToArray();
            // Interior liners isolate customizable wall materials from exterior siding.
            var liners=world.transform.Find("Customizable interior liners");if(liners)UnityEngine.Object.DestroyImmediate(liners.gameObject);var lining=new GameObject("Customizable interior liners").transform;lining.SetParent(world.transform,false);
            for(int side=-1;side<=1;side+=2)Part(lining,"Interior side finish",new Vector3(side*5.82f,2,0),new Vector3(.03f,3.8f,9.6f),cream);
            for(int side=-1;side<=1;side+=2)Part(lining,"Interior rear finish",new Vector3(side*3.65f,2,4.86f),new Vector3(4.5f,3.8f,.03f),cream);
            build.wallSurfaces=lining.GetComponentsInChildren<Renderer>();build.rearClosure=world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Rear wall"||(t.name=="Oak frame post"&&Mathf.Abs(t.localPosition.x)<.1f&&t.localPosition.z>4)||(t.name=="Marine wall illustration"&&Mathf.Abs(t.localPosition.x)<.1f)).Select(t=>t.gameObject).ToArray();
            var frame=world.transform.Find("Expansion doorway frame");if(frame)UnityEngine.Object.DestroyImmediate(frame.gameObject);var doorway=new GameObject("Expansion doorway frame").transform;doorway.SetParent(world.transform,false);for(int side=-1;side<=1;side+=2)Part(doorway,"Rear wing wall",new Vector3(side*3.65f,2,5),new Vector3(4.5f,4,.2f),cream,true);Part(doorway,"Door lintel",new Vector3(0,3.65f,5),new Vector3(2.8f,.7f,.25f),wood,true);
            Localize();EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();Debug.Log("ASTRA 0.6 catalog generated: "+all.Count+" entries");
        }
        static void BakeRootScale(GameObject root){var scale=root.transform.localScale;foreach(Transform child in root.transform){child.localPosition=Vector3.Scale(child.localPosition,scale);child.localScale=Vector3.Scale(child.localScale,scale);}root.transform.localScale=Vector3.one;}
    }
}
