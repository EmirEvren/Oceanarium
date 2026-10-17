using System;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace OceanariumFishing.Editor
{
    public static class CreatorBuilder
    {
        const string R=FoundationBuilder.Root;
        [MenuItem("Oceanarium Fishing/Build ASTRA 0.2 Creator")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building creator.");
            Directory.CreateDirectory(R+"/Characters");var mat=AssetDatabase.LoadAssetAtPath<Material>(R+"/Characters/OF_AvatarSurface.mat");
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="OF Avatar Surface"};mat.SetFloat("_Smoothness",.27f);AssetDatabase.CreateAsset(mat,R+"/Characters/OF_AvatarSurface.mat");}
            var path=R+"/Prefabs/OF_Player.prefab";var player=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var c=player.GetComponent<LocalPlayerController>();var old=player.GetComponent<CharacterMotion>();if(old)UnityEngine.Object.DestroyImmediate(old);
                for(int i=c.visual.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(c.visual.GetChild(i).gameObject);
                var avatar=c.visual.GetComponent<CharacterAppearance>();if(!avatar)avatar=c.visual.gameObject.AddComponent<CharacterAppearance>();avatar.surface=mat;avatar.loadLocal=true;avatar.controller=c;c.visual.localPosition=Vector3.zero;c.visual.localScale=Vector3.one;
                PrefabUtility.SaveAsPrefabAsset(player,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(player);}
            EditorSceneManager.OpenScene(R+"/Scenes/OF_CharacterCreator.unity");var entry=UnityEngine.Object.FindFirstObjectByType<SceneEntry>();
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())if(root!=entry.gameObject)UnityEngine.Object.DestroyImmediate(root);
            entry.hud=null;entry.player=null;
            RenderSettings.fog=false;RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.78f,.83f,.78f);RenderSettings.ambientEquatorColor=new Color(.59f,.65f,.61f);RenderSettings.ambientGroundColor=new Color(.4f,.44f,.4f);
            var bg=new GameObject("Studio background").AddComponent<Camera>();bg.clearFlags=CameraClearFlags.SolidColor;bg.backgroundColor=new Color(.82f,.88f,.82f);bg.cullingMask=0;bg.depth=-10;
            var cam=new GameObject("Preview camera",typeof(AudioListener)).AddComponent<Camera>();cam.tag="MainCamera";cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=bg.backgroundColor;cam.orthographic=true;cam.orthographicSize=1.13f;cam.transform.position=new Vector3(0,1.14f,4);cam.transform.LookAt(new Vector3(0,1.02f,0));cam.rect=new Rect(.42f,.2f,.56f,.61f);cam.nearClipPlane=.1f;cam.farClipPlane=30;
            var avatarPreview=new GameObject("Creator avatar").AddComponent<CharacterAppearance>();avatarPreview.surface=mat;avatarPreview.loadLocal=false;
            var creator=new GameObject("Character Creator").AddComponent<CharacterCreator>();creator.preview=avatarPreview;creator.previewCamera=cam;
            Light("Warm key",new Vector3(35,-30,0),1.3f,new Color(1,.91f,.78f),true);Light("Soft fill",new Vector3(25,140,0),.7f,new Color(.8f,.93f,1),false);
            StudioShape("Stage",PrimitiveType.Cylinder,new Vector3(0,-.10f,0),new Vector3(2.45f,.1f,2.45f),new Color(.74f,.80f,.69f));
            StudioShape("Stage rim",PrimitiveType.Cylinder,new Vector3(0,-.19f,0),new Vector3(2.5f,.03f,2.5f),new Color(.90f,.82f,.64f));
            StudioShape("Backdrop arch",PrimitiveType.Capsule,new Vector3(0,1.5f,-1.2f),new Vector3(2.45f,1.9f,.16f),new Color(.67f,.77f,.67f));
            AddLocalization();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("ASTRA 0.2 creator built; environment preserved.");
        }
        static void Light(string name,Vector3 angles,float intensity,Color color,bool shadows){var light=new GameObject(name).AddComponent<Light>();light.type=LightType.Directional;light.transform.eulerAngles=angles;light.intensity=intensity;light.color=color;light.shadows=shadows?LightShadows.Soft:LightShadows.None;}
        static void StudioShape(string name,PrimitiveType type,Vector3 p,Vector3 size,Color color)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.position=p;go.transform.localScale=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            string path=R+"/Characters/Studio_"+name.Replace(' ','_')+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.15f);go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        public static void AddLocalization()
        {
            var en=AssetDatabase.LoadAssetAtPath<StringTable>(R+"/Localization/Tables/OF_UI_en.asset");var tr=AssetDatabase.LoadAssetAtPath<StringTable>(R+"/Localization/Tables/OF_UI_tr.asset");
            void Add(string key,string english,string turkish=null){en.AddEntry("creator."+key,english);if(turkish!=null)tr.AddEntry("creator."+key,turkish);}
            foreach(var pair in CosmeticCatalog.Options){Add(pair.Key,CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pair.Key));foreach(var id in pair.Value)Add(id,CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_',' ')));}
            string[] keys={"title","subtitle","brand","studio","presentation","hair","hairColor","face","eyes","brows","mouth","beard","skin","top","bottom","shoes","hat","accessory","eyeColor","browColor","beardColor","outfitColor","nose","detail","glasses","randomize","reset","confirm","back","camera_reset","preview_help","match_brows","save_error"};
            string[] english={"Meet your island self","A little character. A whole ocean of possibility.","OCEANARIUM","THE REEFKEEPER STUDIO","Appearance","Hairstyles","Hair color","Face","Eyes","Eyebrows","Mouth","Facial hair","Skin tone","Tops","Bottoms","Shoes","Hats","Accessories","Eye color","Eyebrow color","Facial hair color","Clothing color","Nose","Face details","Glasses","Randomize","Reset","Save & continue","Back","Reset view","Drag to turn · Scroll to zoom","Match hair color","Could not save. Please check available storage."};
            string[] turkish={"Adadaki kendinle tanış","Küçük bir karakter. Okyanuslar kadar olasılık.","OCEANARIUM","RESİF BEKÇİSİ STÜDYOSU","Görünüm","Saç modelleri","Saç rengi","Yüz","Gözler","Kaşlar","Ağız","Sakal ve bıyık","Ten rengi","Üst giyim","Alt giyim","Ayakkabı","Şapkalar","Aksesuarlar","Göz rengi","Kaş rengi","Sakal rengi","Kıyafet rengi","Burun","Yüz ayrıntıları","Gözlük","Rastgele","Sıfırla","Kaydet ve devam et","Geri","Görünümü sıfırla","Döndürmek için sürükle · Tekerlekle yakınlaş","Saç rengiyle eşleştir","Kaydedilemedi. Kullanılabilir depolamayı kontrol et."};
            for(int i=0;i<keys.Length;i++)Add(keys[i],english[i],turkish[i]);
            CreatorTranslations.Turkish(tr);
            // Unverified locale translations are intentionally absent: LanguageService resolves English.
            EditorUtility.SetDirty(en);EditorUtility.SetDirty(tr);EditorUtility.SetDirty(en.SharedData);AssetDatabase.SaveAssets();
        }
    }
}
