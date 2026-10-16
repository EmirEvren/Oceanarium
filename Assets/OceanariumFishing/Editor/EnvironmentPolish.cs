using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OceanariumFishing.Editor
{
    public static class EnvironmentPolish
    {
        [MenuItem("Oceanarium Fishing/Diagnose Grass Rendering %&h")]
        public static void Diagnose()
        {
            var grass=GameObject.Find("Coastal grass field");
            if(grass!=null)grass.SetActive(!grass.activeSelf);
            Debug.Log("Grass renderer active: "+(grass!=null && grass.activeSelf));
        }
        [MenuItem("Oceanarium Fishing/Apply Ground Art Pass %&g")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string root=FoundationBuilder.Root;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"/Textures/OF_GroundGrain.asset");
            if(texture==null)
            {
                texture=new Texture2D(256,256,TextureFormat.RGB24,true){name="OF_GroundGrain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
                var random=new System.Random(81);
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    // Periodic low contrast grain, all generated inside Unity.
                    float broad=Mathf.Sin(x*Mathf.PI/32)*Mathf.Cos(y*Mathf.PI/64)*.035f;
                    float fine=(float)random.NextDouble()*.09f;float value=.82f+broad+fine;
                    texture.SetPixel(x,y,new Color(value,value,value));
                }
                texture.Apply();AssetDatabase.CreateAsset(texture,root+"/Textures/OF_GroundGrain.asset");
            }
            foreach(string key in new[]{"Grass","Sand","Path","Plank","Timber"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/OF_"+key+".mat");mat.SetTexture("_BaseMap",texture);mat.SetTextureScale("_BaseMap",key=="Grass"?Vector2.one*12:Vector2.one*4);EditorUtility.SetDirty(mat);
            }
            foreach(int i in new[]{1,3})
            {
                Replace(IslandGeometry.Terrain(),"Island_"+i);
                Replace(IslandGeometry.Path(new[]{new Vector3(1,0,-15),new Vector3(1,0,-10),new Vector3(-1,0,-6),new Vector3(-5,0,-3),new Vector3(-7,0,1)},1.7f),"CabinPath_"+i);
                Replace(IslandGeometry.Path(new[]{new Vector3(-1,0,-6),new Vector3(2,0,-4),new Vector3(5,0,0),new Vector3(9,0,3)},1.5f),"SanctuaryPath_"+i);
            }
            Replace(IslandGeometry.Leaf(2.9f,.48f,.65f),"PalmFrond");
            foreach(string key in new[]{"Leaf","LeafLight"}){var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/OF_"+key+".mat");mat.SetFloat("_Cull",0);EditorUtility.SetDirty(mat);}
            var mesh=GrassMesh();var existing=AssetDatabase.LoadAssetAtPath<Mesh>(root+"/Meshes/OF_GrassTufts.asset");
            var grassMaterial=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/OF_CoastalGrass.mat");
            if(grassMaterial==null){grassMaterial=new Material(Shader.Find("OceanariumFishing/Coastal Grass"));AssetDatabase.CreateAsset(grassMaterial,root+"/Materials/OF_CoastalGrass.mat");}
            if(existing==null){AssetDatabase.CreateAsset(mesh,root+"/Meshes/OF_GrassTufts.asset");existing=mesh;}else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);}
            foreach(string scene in new[]{"OF_MainMenu","OF_HomeIsland"})
            {
                EditorSceneManager.OpenScene(root+"/Scenes/"+scene+".unity");
                // Editor-only authored object lookup; no runtime name dependency.
                var field=GameObject.Find("Coastal grass field");
                if(field==null)field=new GameObject("Coastal grass field",typeof(MeshFilter),typeof(MeshRenderer));
                field.SetActive(true);
                field.GetComponent<MeshFilter>().sharedMesh=existing;field.GetComponent<MeshRenderer>().sharedMaterial=grassMaterial;
                field.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets();Debug.Log("Ground art pass applied: terrain skirt, trails, ground grain and batched coastal grass.");
        }
        private static void Replace(Mesh fresh,string name)
        {var old=AssetDatabase.LoadAssetAtPath<Mesh>(FoundationBuilder.Root+"/Meshes/OF_"+name+".asset");fresh.name="OF_"+name;EditorUtility.CopySerialized(fresh,old);EditorUtility.SetDirty(old);Object.DestroyImmediate(fresh);}
        private static Mesh GrassMesh()
        {
            var v=new List<Vector3>();var tri=new List<int>();var random=new System.Random(172);
            for(int n=0;n<3000;n++)
            {
                float x=(float)random.NextDouble()*40-20,z=(float)random.NextDouble()*32-16;
                float ratio=IslandGeometry.Radius(x,z)/IslandGeometry.Coast(Mathf.Atan2(z/.872f,x));
                if(ratio>.76f || ratio<.4f || (new Vector2(x+8,z-3)).sqrMagnitude<26 || (new Vector2(x-8,z-3)).sqrMagnitude<22 || Mathf.Abs(x)<3 && z<0)continue;
                for(int b=0;b<3;b++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2,h=.18f+(float)random.NextDouble()*.3f;var p=new Vector3(x,IslandGeometry.Height(x,z),z);var side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.045f;
                    int k=v.Count;v.Add(p-side);v.Add(p+side);v.Add(p+Vector3.up*h+side*2);tri.AddRange(new[]{k,k+2,k+1});
                }
            }
            var mesh=new Mesh{name="OF_GrassTufts"};mesh.SetVertices(v);mesh.SetTriangles(tri,0);
            var uv=new List<Vector2>();var normals=new List<Vector3>();var tangents=new List<Vector4>();
            for(int i=0;i<v.Count;i++){uv.Add(new Vector2(i%3==1?1:0,i%3==2?1:0));normals.Add(Vector3.up);tangents.Add(new Vector4(1,0,0,1));}
            mesh.SetUVs(0,uv);mesh.SetNormals(normals);mesh.SetTangents(tangents);mesh.RecalculateBounds();return mesh;
        }
    }
}
