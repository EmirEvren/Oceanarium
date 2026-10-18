using UnityEngine;
using UnityEditor;
namespace OceanariumFishing.Editor
{
    public static partial class TownBuilder
    {
        static void ShopArt(TownWorld world)
        {
            Material Mat(string id,Color color){var path=Dir+"/"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;}
            var wood=Mat("Shop timber",new(.38f,.22f,.13f));var cream=Mat("Shop linen",new(.94f,.84f,.63f));var brass=Mat("Shop brass",new(.65f,.48f,.19f));var green=Mat("Botanist foliage",new(.24f,.47f,.26f));
            GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material material,PrimitiveType shape=PrimitiveType.Cube,bool solid=false){var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
            for(int n=0;n<world.residents.Length;n++)
            {
                var d=world.residents[n];var shop=new GameObject("Crafted counter - "+d.id).transform;shop.SetParent(world.transform);shop.position=d.work+new Vector3(1.2f,0,.35f);var color=Mat("Shop color "+n,CosmeticCatalog.ColorOf(d.appearance.outfitColor));
                if(d.nightMerchant){var display=shop.gameObject.AddComponent<TownMerchantDisplay>();display.world=world;display.resident=d;}
                Part(shop,"Counter body",new(0,.4f,0),new(1.5f,.8f,.65f),color,solid:true);Part(shop,"Solid timber worktop",new(0,.85f,0),new(1.65f,.12f,.8f),wood);for(int s=-1;s<=1;s+=2){Part(shop,"Awning post",new(s*.78f,1.2f,.25f),new(.07f,2.4f,.07f),wood);Part(shop,"Lantern brass frame",new(s*.75f,2.05f,0),new(.16f,.24f,.16f),brass);Part(shop,"Lantern glass",new(s*.75f,2.05f,-.02f),new(.11f,.17f,.15f),cream);}
                for(int stripe=0;stripe<8;stripe++){var awning=Part(shop,"Linen awning stripe",new(-.77f+stripe*.22f,2.45f,0),new(.22f,.06f,1.15f),stripe%2==0?color:cream);awning.transform.localRotation=Quaternion.Euler(-9,0,0);}
                if(n==0||n==10)for(int i=0;i<3;i++){var rod=Part(shop,"Fishing rod display",new(-.45f+i*.4f,1.5f,.12f),new(.025f,1.25f,.025f),wood);rod.transform.localRotation=Quaternion.Euler(0,0,-14);Part(shop,"Reel",new(-.35f+i*.4f,1,.12f),Vector3.one*.12f,brass,PrimitiveType.Cylinder);}
                else if(n==1||n==8)for(int i=0;i<3;i++){Part(shop,"Plant pot",new(-.5f+i*.5f,1,0),new(.27f,.2f,.27f),wood,PrimitiveType.Cylinder);for(int j=0;j<4;j++){var leaf=Part(shop,"Plant leaf",new(-.5f+i*.5f,1.2f+j*.04f,0),new(.12f,.3f,.05f),green,PrimitiveType.Sphere);leaf.transform.localRotation=Quaternion.Euler(0,j*90,j*25);}}
                else if(n==2){for(int i=0;i<5;i++)Part(shop,"Golden pastry",new(-.5f+i*.22f,1,-.12f),new(.18f,.12f,.18f),cream,PrimitiveType.Sphere);Part(shop,"Tea kettle",new(.4f,1.15f,.16f),new(.3f,.35f,.3f),brass,PrimitiveType.Sphere);}
                else if(n==6){Part(shop,"Camera body",new(0,1.12f,0),new(.4f,.27f,.15f),wood);var lens=Part(shop,"Camera lens",new(0,1.12f,-.13f),new(.16f,.1f,.16f),brass,PrimitiveType.Cylinder);lens.transform.localRotation=Quaternion.Euler(90,0,0);}
                else if(n==7){Part(shop,"Tailor display torso",new(0,1.3f,0),new(.37f,.7f,.2f),color,PrimitiveType.Capsule);Part(shop,"Display hat brim",new(0,1.75f,0),new(.5f,.035f,.4f),cream,PrimitiveType.Cylinder);}
                else if(n==9){for(int i=0;i<4;i++)Part(shop,"Stack of printed bulletins",new(-.4f+i*.25f,.98f,-.03f),new(.22f,.1f,.35f),cream);}
                else for(int i=0;i<3;i++)Part(shop,n==11?"Carved timber samples":"Recovered coastal curios",new(-.45f+i*.45f,1.05f,0),new(.25f,.3f,.23f),i%2==0?wood:brass,n==11?PrimitiveType.Cube:PrimitiveType.Sphere);
            }
        }
    }
}
