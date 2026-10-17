using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;
namespace OceanariumFishing.Editor
{
    public static class CozyFishingBuilder
    {
        const string R=FoundationBuilder.Root,D=R+"/Fishing";
        [MenuItem("Oceanarium Fishing/Build ASTRA 0.4 Collection")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before updating assets.");
            var tuning=AssetDatabase.LoadAssetAtPath<FishingTuning>(D+"/CozyFishingTuning.asset");
            var profiles=AssetDatabase.LoadAssetAtPath<FishingDifficultyProfiles>(D+"/DifficultyProfiles.asset");
            if(!profiles){profiles=ScriptableObject.CreateInstance<FishingDifficultyProfiles>();AssetDatabase.CreateAsset(profiles,D+"/DifficultyProfiles.asset");}
            tuning.difficulties=profiles;EditorUtility.SetDirty(tuning);
            var species=AssetDatabase.FindAssets("t:FishSpecies",new[]{D}).Select(g=>AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(s=>s.id).ToArray();
            Directory.CreateDirectory(D+"/Icons");AssetDatabase.Refresh();foreach(var s in species){s.icon=Icon(s,false);s.silhouette=Icon(s,true);EditorUtility.SetDirty(s);}
            var root=PrefabUtility.LoadPrefabContents(R+"/Prefabs/OF_Player.prefab");
            try{var ui=root.GetComponent<CollectionHUD>()??root.AddComponent<CollectionHUD>();ui.fishing=root.GetComponent<FishingController>();ui.species=species;PrefabUtility.SaveAsPrefabAsset(root,R+"/Prefabs/OF_Player.prefab");}finally{PrefabUtility.UnloadPrefabContents(root);}
            Localize();AssetDatabase.SaveAssets();Debug.Log("ASTRA 0.4 profiles, specimen UI and species icons updated.");
        }
        static Texture2D Icon(FishSpecies s,bool silhouette)
        {
            const int w=384,h=224;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){name=s.id+(silhouette?" silhouette":" illustration"),filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[w*h];float ry=s.id=="pufferfish"?.60f:s.id=="blue_tang"||s.id=="sea_bream"?.48f:s.id=="sardine"||s.id=="mackerel"?.26f:.4f;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float nx=(x-w*.53f)/(w*.40f),ny=(y-h*.5f)/(h*.45f);
                float rx=s.id=="pufferfish"?.72f:s.id=="clownfish"?.86f:s.id=="blue_tang"?.88f:s.id=="sea_bream"?.9f:s.id=="red_snapper"?.96f:s.id=="sardine"?1.1f:1.04f;
                bool body=nx*nx/(rx*rx)+ny*ny/(ry*ry)<1;
                bool tail=nx<-.68f*rx&&nx>-1.27f&&Mathf.Abs(ny)<(.68f*rx+nx)*-.72f+.04f;
                if(s.id=="clownfish"||s.id=="pufferfish")tail=nx<-.65f*rx&&Mathf.Pow((nx+.94f)/.3f,2)+Mathf.Pow(ny/.27f,2)<1;
                else if(nx<-1.1f&&Mathf.Abs(ny)<(-nx-1.1f)*1.1f)tail=false;
                float fin=s.id=="sardine"?.10f:s.id=="red_snapper"?.30f:s.id=="golden_trevally"?.16f:.22f;
                bool dorsal=nx>-.5f&&nx<.35f&&ny>0&&ny<ry+fin*(1-Mathf.Abs(nx+.08f)/.45f);
                bool lower=nx>-.55f&&nx<.1f&&ny<0&&ny>-ry-.12f*(1-Mathf.Abs(nx+.2f)/.35f);
                if(!(body||tail||dorsal||lower))continue;
                Color c=s.color* Mathf.Lerp(.65f,1.15f,Mathf.InverseLerp(-ry,ry,ny));c.a=1;
                if(!body)c=Color.Lerp(s.color,new Color(.13f,.29f,.34f),.28f);
                if(s.id=="clownfish"&&body&&(Mathf.Abs(nx+.5f)<.085f||Mathf.Abs(nx)<.085f||Mathf.Abs(nx-.48f)<.085f))c=new Color(.99f,.95f,.79f);
                if(s.id=="blue_tang"&&(tail||lower))c=new Color(1,.81f,.18f);
                if(s.id=="blue_tang"&&body&&nx<.25f&&ny>.05f&&ny<.16f)c=new Color(.08f,.17f,.32f);
                if(s.id=="mackerel"&&body&&ny>.02f&&Mathf.Sin(nx*30+ny*12)>.62f)c=Color.Lerp(c,Color.black,.5f);
                if(s.id=="sardine"&&body)c=Color.Lerp(c,new Color(.78f,.88f,.89f),ny<.06f?.85f:.35f);
                if(s.id=="golden_trevally"&&body&&Mathf.Sin(nx*24)>.72f)c=Color.Lerp(c,new Color(.4f,.3f,.14f),.55f);
                if(s.id=="sea_bream"&&body&&Mathf.Abs(nx-.64f)<.065f)c=new Color(.91f,.76f,.34f);
                if(s.id=="pufferfish"&&body&&Mathf.Sin(nx*44)*Mathf.Cos(ny*35)>.65f)c=Color.Lerp(c,Color.black,.45f);
                if(body&&Mathf.Pow((nx-.66f)/.085f,2)+Mathf.Pow((ny-.12f)/.085f,2)<1)c=new Color(.99f,.98f,.9f);
                if(body&&Mathf.Pow((nx-.69f)/.043f,2)+Mathf.Pow((ny-.12f)/.057f,2)<1)c=new Color(.04f,.11f,.16f);
                pixels[y*w+x]=silhouette?new Color(.045f,.055f,.055f,1):c;
            }
            texture.SetPixels(pixels);texture.Apply();string path=D+"/Icons/"+s.id+(silhouette?"_locked":"_color")+".asset";
            var previous=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(previous){EditorUtility.CopySerialized(texture,previous);UnityEngine.Object.DestroyImmediate(texture);return previous;}AssetDatabase.CreateAsset(texture,path);return texture;
        }
        public static void Localize()
        {
            FishingBuilder.Localize();
            var en=AssetDatabase.LoadAssetAtPath<StringTable>(R+"/Localization/Tables/OF_UI_en.asset");var tr=AssetDatabase.LoadAssetAtPath<StringTable>(R+"/Localization/Tables/OF_UI_tr.asset");
            void Add(string key,string english,string turkish){en.AddEntry(key,english);tr.AddEntry(key,turkish);}
            Add("collection.inventory","Fish Inventory","Balık Envanteri");Add("collection.journal","Oceanarium Journal","Oceanarium Günlüğü");Add("collection.records","Fishing records","Balıkçılık rekorları");Add("collection.fish","Fish","Balıklar");Add("collection.specimens","specimens owned","balık saklanıyor");
            Add("collection.sort","Sort","Sırala");Add("collection.sort.date","Date","Tarih");Add("collection.sort.weight","Weight","Ağırlık");Add("collection.sort.rarity","Rarity","Nadirlik");Add("collection.sort.species","Species","Tür");Add("collection.all","All","Tümü");Add("collection.rarity","Rarity","Nadirlik");Add("collection.island","Island filter","Ada filtresi");
            Add("collection.quality","Specimen quality","Balığın kalitesi");Add("collection.location","Catch location","Yakalandığı yer");Add("collection.date","Catch date","Yakalanma tarihi");Add("collection.value","Estimated value","Tahmini değer");Add("collection.personal_best","Personal best!","Kişisel rekor!");Add("collection.empty","Your collection is waiting for its first fish. Keep a catch to see its individual details here.","Koleksiyonun ilk balığını bekliyor. Yakaladığın bir balığı saklayarak ayrıntılarını burada görebilirsin.");
            Add("collection.discovered_count","FISH DISCOVERED  {0} / {1}","KEŞFEDİLEN BALIKLAR  {0} / {1}");Add("collection.discovered","Discovered","Keşfedildi");Add("collection.undiscovered","Undiscovered","Keşfedilmedi");Add("collection.unknown","Unknown","Bilinmiyor");Add("collection.locked","An ocean secret. Catch this species to reveal its story. Released fish count too.","Okyanusun bir sırrı. Bu türü yakalayarak hikâyesini öğren. Serbest bıraktığın balıklar da sayılır.");
            Add("collection.habitat","Habitat","Yaşam alanı");Add("collection.islands","Discovered at","Keşfedildiği yer");Add("collection.weather","Preferred weather","Tercih ettiği hava");Add("collection.time","Active hours","Etkin saatler");Add("collection.caught","Caught","Yakalanan");Add("collection.heaviest","Heaviest","En ağır");Add("collection.longest","Longest","En uzun");Add("collection.first","First discovery","İlk keşif");Add("collection.total","Total fish caught","Toplam yakalanan balık");Add("collection.most_common","Most caught species","En çok yakalanan tür");Add("collection.rarest","Rarest species","En nadir tür");
            Add("collection.legacy","Some older catches have incomplete dates or counts. Existing records are preserved.","Bazı eski yakalamaların tarih veya adet bilgisi eksik. Mevcut rekorlar korundu.");Add("collection.load_error","The collection save could not be read. It has been preserved and will not be overwritten.","Koleksiyon kaydı okunamadı. Dosya korundu ve üzerine yazılmayacak.");Add("collection.shortcuts","I Inventory · J Journal · ESC Close","I Envanter · J Günlük · ESC Kapat");Add("collection.previous","Previous page","Önceki sayfa");Add("collection.next","Next page","Sonraki sayfa");Add("collection.difficulty","Fishing difficulty","Balıkçılık zorluğu");Add("difficulty.cozy","Cozy","Rahat");Add("difficulty.normal","Normal","Normal");Add("difficulty.experienced","Experienced","Deneyimli");
            Add("fishing.cozy_help","Hold LMB to reel. Release if tension is high.\nMouse direction is optional.\nQ: cancel","Sarmak için sol fareyi basılı tut. Gerilim yüksekse bırak.\nFare yönü isteğe bağlıdır.\nQ: iptal");Add("fishing.warning","! HIGH TENSION — Release to recover","! YÜKSEK GERİLİM — Toparlanmak için bırak");Add("fishing.calm","Steady reeling. You have time.","Sakince sar. Vaktin var.");Add("fishing.recovered","GREAT RECOVERY!","HARİKA TOPARLADIN!");
            Add("fishing.tutorial","YOUR FIRST CAST\n1. Hold LMB, then release to cast.\n2. Click when the fish bites.\n3. Hold LMB to reel.\n4. Release when tension is high.\n5. Keep or release your catch.","İLK ATIŞIN\n1. Sol fareyi basılı tut, atmak için bırak.\n2. Balık vurunca tıkla.\n3. Sarmak için sol fareyi basılı tut.\n4. Gerilim yükselince bırak.\n5. Balığını sakla veya serbest bırak.");
            Add("fishing.discovery","NEW SPECIES DISCOVERED!","YENİ TÜR KEŞFEDİLDİ!");Add("fishing.journal_added","Recorded in your Oceanarium Journal. Keeping or releasing preserves your discovery.","Oceanarium Günlüğüne işlendi. Saklasan da serbest bıraksan da keşfin korunur.");
            string[] islands={"OF_HomeIsland","OF_TownIsland","OF_ExplorationIsland01","OF_ExplorationIsland02","OF_ExplorationIsland03"},english={"Home Island","Town Island","Exploration Island 01","Exploration Island 02","Exploration Island 03"},turkish={"Ana Ada","Kasaba Adası","Keşif Adası 01","Keşif Adası 02","Keşif Adası 03"};for(int i=0;i<islands.Length;i++)Add("island."+islands[i],english[i],turkish[i]);
            string[] habitats={"beach","rocky","pier","deep"},eHabitats={"Shallow beach","Rocky reef","Pier waters","Deep water"},tHabitats={"Sığ kıyı","Kayalık resif","İskele suları","Derin su"};for(int i=0;i<4;i++){Add("habitat."+habitats[i],eHabitats[i],tHabitats[i]);Add("zone.home."+habitats[i],eHabitats[i],tHabitats[i]);}
            Add("weather.clear","Clear","Açık");Add("weather.cloudy","Cloudy","Bulutlu");
            string[] ids={"clownfish","blue_tang","sardine","mackerel","red_snapper","sea_bream","pufferfish","golden_trevally"};
            string[] descriptions={"A bright little reef companion with creamy white bands.","A vivid blue reef swimmer with a golden tail.","A slender silver fish that glimmers in coastal waters.","A swift swimmer with dark rippling stripes.","A warm red fish with a sturdy silhouette.","A silvery coastal fish with a golden accent.","A round, spotted reef visitor with tiny fins.","A golden ocean traveller with bold dark bars."};
            string[] trDescriptions={"Krem rengi bantlarıyla resifin parlak küçük sakini.","Altın renkli kuyruğuyla canlı mavi bir resif yüzücüsü.","Kıyı sularında parlayan ince, gümüş renkli bir balık.","Koyu dalgalı çizgileri olan hızlı bir yüzücü.","Güçlü siluetli, sıcak kırmızı renkli bir balık.","Altın renkli ayrıntısı olan gümüş bir kıyı balığı.","Minik yüzgeçleriyle yuvarlak ve benekli bir resif ziyaretçisi.","Belirgin koyu bantları olan altın renkli bir okyanus gezgini."};for(int i=0;i<ids.Length;i++)Add("fish."+ids[i],descriptions[i],trDescriptions[i]);
            Add("milestone","ASTRA 0.4 / Cozy fishing & collection\nCatches, journal and preferences are saved","ASTRA 0.4 / Rahat balıkçılık ve koleksiyon\nBalıklar, günlük ve tercihler kaydedilir");
            Add("objective","Explore your island. Collect a rod at the pier, then cast into the sea.","Adanı keşfet. İskelede bir olta al, sonra denize at.");Add("objective.done","Your rod is ready. I: Inventory · J: Journal","Oltan hazır. I: Envanter · J: Günlük");
            Add("controls","WASD Move · SHIFT Sprint · RMB Camera · E Interact · I Inventory · J Journal · ESC Settings","WASD Hareket · SHIFT Koş · Sağ fare Kamera · E Etkileşim · I Envanter · J Günlük · ESC Ayarlar");
            foreach(var code in LanguageService.Codes.Where(c=>c!="en"&&c!="tr"))
            {
                var table=AssetDatabase.LoadAssetAtPath<StringTable>(R+"/Localization/Tables/OF_UI_"+code+".asset");
                // Old milestone text promises no saving/no fishing: use the current English fallback.
                foreach(var key in new[]{"milestone","objective","objective.done","controls","notice.starterrod"})table.RemoveEntry(key);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(en);EditorUtility.SetDirty(tr);EditorUtility.SetDirty(en.SharedData);AssetDatabase.SaveAssets();
            var report=new System.Text.StringBuilder("ASTRA 0.4 localization coverage\nEnglish and Turkish: all collection/difficulty/tutorial keys translated.\nOther configured locales use existing Unity English fallback for missing keys.\n");
            foreach(var code in LanguageService.Codes){var table=AssetDatabase.LoadAssetAtPath<StringTable>(R+"/Localization/Tables/OF_UI_"+code+".asset");var missing=en.Values.Where(e=>string.IsNullOrEmpty(table.GetEntry(e.KeyId)?.LocalizedValue)).Select(e=>e.Key).ToArray();report.AppendLine(code+": "+missing.Length+" fallback keys: "+string.Join(", ",missing));}File.WriteAllText("Docs/ASTRA_04_LOCALIZATION_COVERAGE.txt",report.ToString());
        }
    }
}
