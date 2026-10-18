using System.Linq;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    public static partial class AquariumBuilder
    {
        // Authored facade modules share the museum's walkable shell and interaction layout.
        static void DressShopfront(Transform root,Transform roof,float rampLow)
        {
            ClearGardenFootprint(root);
            var mint=Mat("Painted seafoam siding",new Color(.42f,.64f,.58f));
            var dark=Mat("Deep ocean roof",new Color(.10f,.26f,.29f));
            var tile=Mat("Weathered turquoise shingles",new Color(.19f,.40f,.41f));
            var tileLight=Mat("Sunlit turquoise shingles",new Color(.23f,.46f,.45f));
            var paleStone=Mat("Warm limestone blocks",new Color(.62f,.60f,.50f));
            var trim=Mat("Painted ivory trim",new Color(.89f,.84f,.68f));
            var ink=Mat("Sign navy enamel",new Color(.08f,.22f,.27f));
            var pink=Mat("Garden pink",new Color(.85f,.49f,.47f));
            // Replace roof lines with layered overlapping shingles and deep edge boards.
            foreach(var t in roof.Cast<Transform>().Where(t=>t.name=="Roof seam"||t.name=="Pitched teal roof").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            for(int side=-1;side<=1;side+=2)
            {
                Quad(roof,"Continuous roof underlay",new(0,6,-5.8f),new(side*6.7f,3.97f,-5.8f),new(side*6.7f,3.97f,5.8f),new(0,6,5.8f),dark,new Vector3(side*.3f,1,0).normalized);
                for(int row=0;row<9;row++)for(int col=0;col<18;col++)
                {
                    float xx=(row+.5f)*.74f,zz=-5.53f+col*.65f;
                    var shingle=Part(roof,"Overlapping roof shingle",new Vector3(side*xx,6-xx*.303f+.045f,zz),new Vector3(.82f,.085f,.63f),(row+col)%4==0?tileLight:tile);
                    shingle.transform.localRotation=Quaternion.Euler(0,0,-side*16.86f);
                }
                Beam(roof,"Deep eave fascia",new Vector3(side*6.74f,3.97f,-5.86f),new Vector3(side*6.74f,3.97f,5.86f),.22f,dark);
                for(int end=-1;end<=1;end+=2){Beam(roof,"Gable rake ivory",new Vector3(0,6.13f,end*5.87f),new Vector3(side*6.83f,4.05f,end*5.87f),.19f,trim);Beam(roof,"Gable rake shadow",new Vector3(0,5.97f,end*5.83f),new Vector3(side*6.8f,3.9f,end*5.83f),.25f,wood);}
            }
            Part(roof,"Ridge cap",new Vector3(0,6.17f,0),new Vector3(.28f,.18f,11.9f),dark);
            // Gable weatherboards, a porthole and crafted fish crest.
            for(int row=0;row<7;row++){float y=4.13f+row*.23f;float half=(6-y)*3;Part(roof,"Gable clapboard",new Vector3(0,y,-5.13f),new Vector3(half*2,.205f,.11f),row%2==0?mint:teal);}
            var crest=Node(roof,"Gable porthole crest",new Vector3(0,5.07f,-5.25f));
            var circle=Part(crest,"Porthole surround",Vector3.zero,new Vector3(.92f,.09f,.92f),trim,false,PrimitiveType.Cylinder);circle.transform.localRotation=Quaternion.Euler(90,0,0);
            circle=Part(crest,"Porthole enamel",new Vector3(0,0,-.055f),new Vector3(.75f,.035f,.75f),ink,false,PrimitiveType.Cylinder);circle.transform.localRotation=Quaternion.Euler(90,0,0);
            FishBadge(crest,new Vector3(0,0,-.095f),.6f,brass);
            // Continuous side siding, stone courses and consistent structural corners.
            for(int side=-1;side<=1;side+=2)
            {
                for(int j=0;j<15;j++)Part(root,"Side cedar weatherboard",new Vector3(side*6.14f,.18f+j*.25f,0),new Vector3(.12f,.225f,10),j%3==0?mint:teal);
                for(int end=-1;end<=1;end+=2)Part(root,"Ivory corner trim",new Vector3(side*6.21f,2,end*5.04f),new Vector3(.17f,4.15f,.27f),trim);
                for(int row=0;row<2;row++)for(int j=0;j<10;j++)Part(root,"Foundation stone course",new Vector3(side*6.09f,-.18f-row*.29f,-4.55f+j),new Vector3(.2f,.265f,.93f),j%3==0?stone:paleStone);
                for(int j=0;j<5;j++)Part(root,"Front painted panel",new Vector3(side*3.75f,.12f+j*.2f,-5.16f),new Vector3(4.35f,.175f,.12f),j%2==0?mint:teal);
                for(int j=0;j<4;j++)Part(root,"Front limestone base",new Vector3(side*(2.05f+j*1.05f),-.22f,-5.18f),new Vector3(1,.36f,.24f),paleStone);
                Part(root,"Deep window sill",new Vector3(side*3.75f,1.08f,-5.28f),new Vector3(4.7f,.15f,.4f),trim);
                Part(root,"Window header molding",new Vector3(side*3.75f,3.14f,-5.18f),new Vector3(4.7f,.17f,.21f),trim);
                for(int edge=-1;edge<=1;edge+=2)Part(root,"Window casing",new Vector3(side*3.75f+edge*2.2f,2.1f,-5.19f),new Vector3(.14f,2.1f,.21f),trim);
                // A shallow flower box leaves the glazed display fully visible.
                var box=Node(root,"Window flower box",new Vector3(side*3.75f,.85f,-5.62f));Part(box,"Cedar planter front",Vector3.zero,new Vector3(2.9f,.28f,.44f),wood);
                for(int j=0;j<9;j++)Part(box,"Planter board",new Vector3(-1.28f+j*.32f,0,-.24f),new Vector3(.29f,.27f,.04f),j%2==0?teal:mint);
                for(int j=0;j<10;j++){float xx=-1.25f+j*.28f;Part(box,"Window greenery",new Vector3(xx,.22f,0),new Vector3(.34f,.29f,.32f),leaf,false,PrimitiveType.Sphere);for(int petal=0;petal<5;petal++){float a=petal*1.256f;Part(box,"Five petal flower",new Vector3(xx+Mathf.Sin(a)*.07f,.36f+Mathf.Cos(a)*.07f,-.13f),new Vector3(.1f,.1f,.05f),j%3==0?pink:cream,false,PrimitiveType.Sphere);}}
                // Heavy door jambs frame the open, traversable passage.
                Part(root,"Door jamb",new Vector3(side*1.4f,1.6f,-5.2f),new Vector3(.18f,3.2f,.32f),trim,true);
                var door=Node(root,"Open entrance door",new Vector3(side*1.43f,0,-4.95f));door.localRotation=Quaternion.Euler(0,side*82,0);
                Part(door,"Door panel",new Vector3(-side*.48f,1.35f,0),new Vector3(.92f,2.7f,.12f),teal);
                Part(door,"Door inset",new Vector3(-side*.48f,1.65f,-.075f),new Vector3(.65f,1.4f,.03f),mint);
                Part(door,"Brass door pull",new Vector3(-side*.8f,1.25f,-.12f),new Vector3(.045f,.23f,.06f),brass);
                for(int height=0;height<2;height++)Part(root,"Post collar",new Vector3(side*1.6f,height==0?.18f:2.93f,-6.7f),new Vector3(.27f,.16f,.27f),trim);
                Beam(root,"Porch diagonal brace",new Vector3(side*1.6f,2.5f,-6.7f),new Vector3(side*.95f,3.25f,-6.7f),.14f,wood);
            }
            // Replace the flat canopy with an angled striped shop awning.
            var old=root.Find("Entrance canopy");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            for(int j=0;j<10;j++){var strip=Part(root,"Canvas awning stripe",new Vector3(-1.71f+j*.38f,3.35f,-6.03f),new Vector3(.38f,.09f,2.2f),j%2==0?cream:teal);strip.transform.localRotation=Quaternion.Euler(-9,0,0);Part(root,"Awning scallop",new Vector3(-1.71f+j*.38f,3.1f,-7.1f),new Vector3(.38f,.28f,.1f),j%2==0?cream:teal,false,PrimitiveType.Sphere);}
            // Rounded wooden shop sign with raised contrasting border and balanced lettering.
            old=root.Find("Museum sign board");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);old=root.Find("Oceanarium lettering");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var board=Node(root,"Carved Oceanarium shop sign",new Vector3(0,4.06f,-5.36f));
            Part(board,"Sign outer frame",Vector3.zero,new Vector3(4.1f,.84f,.17f),brass);Part(board,"Sign painted face",new Vector3(0,0,-.105f),new Vector3(3.94f,.68f,.06f),ink);
            for(int side=-1;side<=1;side+=2){var cap=Part(board,"Rounded sign end",new Vector3(side*2.02f,0,0),new Vector3(.82f,.09f,.82f),brass,false,PrimitiveType.Cylinder);cap.transform.localRotation=Quaternion.Euler(90,0,0);}
            var label=Node(board,"Raised shop lettering",new Vector3(0,.02f,-.15f));var text=label.gameObject.AddComponent<TextMesh>();text.text="OCEANARIUM";text.fontSize=72;text.characterSize=.073f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.99f,.93f,.76f);
            // Boardwalk slats and rails align exactly with the accessible ramp.
            float angle=Mathf.Atan2(rampLow,4)*Mathf.Rad2Deg;
            for(int j=0;j<20;j++){float f=(j+.5f)/20;var plank=Part(root,"Entrance boardwalk plank",new Vector3(0,Mathf.Lerp(rampLow,0,f)+.018f,Mathf.Lerp(-9,-5,f)),new Vector3(2.8f,.035f,.188f),j%3==0?wood:MatCachedHoney());plank.transform.localRotation=Quaternion.Euler(angle,0,0);}
            for(int side=-1;side<=1;side+=2){for(int j=0;j<4;j++){float f=j/3f;Part(root,"Ramp baluster",new Vector3(side*1.48f,Mathf.Lerp(rampLow,0,f)+.45f,Mathf.Lerp(-9,-5,f)),new Vector3(.1f,.9f,.1f),wood);}Beam(root,"Ramp handrail",new Vector3(side*1.48f,rampLow+.94f,-9),new Vector3(side*1.48f,.94f,-5),.12f,trim);}
        }
        static Material MatCachedHoney()=>UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Honey cedar.mat");
        static void ClearGardenFootprint(Transform museum)
        {
            // Clip only decorative vegetation in the new footprint; keep source mesh assets intact.
            
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>!f.transform.IsChildOf(museum)).OrderBy(f=>f.name).ThenBy(f=>f.transform.position.x))
            {
                string hierarchy=filter.name;for(var p=filter.transform.parent;p;p=p.parent)hierarchy+="/"+p.name;
                if(!(hierarchy.Contains("Meadow grass")||hierarchy.Contains("Tropical flower beds")||hierarchy.Contains("Understory")))continue;
                var source=filter.sharedMesh;if(!source||!source.isReadable)continue;var vertices=source.vertices;var mesh=UnityEngine.Object.Instantiate(source);bool changed=false;
                for(int sub=0;sub<source.subMeshCount;sub++)
                {
                    var triangles=source.GetTriangles(sub);var keep=new System.Collections.Generic.List<int>();
                    for(int i=0;i<triangles.Length;i+=3){var p=filter.transform.TransformPoint((vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3);bool inside=p.x>1.8f&&p.x<14.2f&&p.z> -2.3f&&p.z<8.3f;bool ramp=p.x>6.35f&&p.x<9.65f&&p.z> -6.6f&&p.z< -1.9f;if(inside||ramp)changed=true;else{keep.Add(triangles[i]);keep.Add(triangles[i+1]);keep.Add(triangles[i+2]);}}mesh.SetTriangles(keep,sub);
                }
                if(!changed){UnityEngine.Object.DestroyImmediate(mesh);continue;}
                string stableId=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(filter).ToString(); string path=Dir+"/GardenClearance_"+Hash128.Compute(stableId)+".asset";var existing=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){UnityEditor.EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else UnityEditor.AssetDatabase.CreateAsset(mesh,path);mesh.RecalculateBounds();filter.sharedMesh=mesh;UnityEditor.EditorUtility.SetDirty(mesh);
            }
        }
        static void FishBadge(Transform parent,Vector3 p,float scale,Material material)
        {Part(parent,"Fish crest body",p,new Vector3(scale*.7f,scale*.38f,.05f),material,false,PrimitiveType.Sphere);var tail=Part(parent,"Fish crest tail",p+Vector3.left*scale*.38f,new Vector3(scale*.24f,scale*.24f,.04f),material);tail.transform.localRotation=Quaternion.Euler(0,0,45);Part(parent,"Fish crest eye",p+new Vector3(scale*.21f,scale*.04f,-.04f),Vector3.one*.04f,teal,false,PrimitiveType.Sphere);}
    }
}

