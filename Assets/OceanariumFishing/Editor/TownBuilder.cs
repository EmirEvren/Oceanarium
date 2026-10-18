using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
namespace OceanariumFishing.Editor
{
    public static partial class TownBuilder
    {
        const string Root="Assets/OceanariumFishing",Dir=Root+"/Town";
        static StringTable en,tr;
        static void Text(string key,string english,string turkish=null){en.AddEntry("town."+key,english);tr.AddEntry("town."+key,turkish??english);}
        static T Asset<T>(string id)where T:ScriptableObject{string path=Dir+"/"+id+".asset";var obj=AssetDatabase.LoadAssetAtPath<T>(path);if(!obj){obj=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(obj,path);}return obj;}
        static Vector3 Ground(float x,float z)=>new(x,IslandGeometry.Height(x/1.7f,z/1.7f)+.06f,z);
        [MenuItem("Oceanarium Fishing/Build ASTRA 0.8 Town Residents")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode");Directory.CreateDirectory(Dir);
            EditorSceneManager.OpenScene(Root+"/Scenes/OF_TownIsland.unity");en=AssetDatabase.LoadAssetAtPath<StringTable>(Root+"/Localization/Tables/OF_UI_en.asset");tr=AssetDatabase.LoadAssetAtPath<StringTable>(Root+"/Localization/Tables/OF_UI_tr.asset");
            // Close the open gables and give the roof independent underside vertices.
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name=="Pitched village roof"))
            {
                var mesh=filter.sharedMesh;if(mesh.vertexCount!=8)continue;var original=mesh.vertices;var vertices=original.Concat(original).ToArray();var front=new[]{0,1,2,0,2,3,4,5,6,4,6,7,0,3,7,1,6,2};var triangles=front.Concat(Enumerable.Range(0,front.Length/3).SelectMany(n=>new[]{front[n*3]+8,front[n*3+2]+8,front[n*3+1]+8})).ToArray();mesh.Clear();mesh.vertices=vertices;mesh.triangles=triangles;mesh.uv=vertices.Select(v=>new Vector2(v.x,v.z)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            }
            var old=GameObject.Find("Town residents and businesses");if(old)UnityEngine.Object.DestroyImmediate(old);var root=new GameObject("Town residents and businesses");var world=root.AddComponent<TownWorld>();root.AddComponent<TownHUD>();root.AddComponent<TownNavigation>();root.AddComponent<TownActivities>();
            string[] ids={"oliver","marina","rosie","morris","gull","bruno","iris","lily","luna","theo","eddie","henry"};
            string[] names={"Oliver Reed","Marina Bloom","Rosie Baker","Captain Morris","Mysterious Gull","Bruno Forge","Iris Shutter","Lily Stitch","Luna Glow","Theo Current","Eddie Dice","Henry Oak"};
            string[] shops={"Tidal Tackle","Shell & Seaweed","Saltwind Bakery & Tea House","The Sunken Chest","Gull Market","Old Mooring Workshop","Pixel & Lens Studio","Needle & Sail","Glowtide Alchemy","Tidal Press","Harbor Game Room","Master Carpenter"};
            string[] roles={"Fishing Equipment Merchant","Aquatic Botanist","Baker","Antique Dealer","Traveling Night Merchant","Recycler & Salvage Artist","Photographer","Tailor","Bioluminescence Researcher","Journalist","Tavern & Arcade Owner","Woodworker"};
            string[] greetings={"A steady line beats a hurried reel. What are we fishing for today?","A little green turns a glass box into a home. Mind the habitat on each plant!","The kettle is singing. Tell me about your morning by the sea.","Every anchor has a story. This one took me three tides to untangle.","You found me between tides. Pearls and rare shells speak louder than Coins.","Nothing is rubbish until I have tried making a fish out of it.","Hold that thought. The light on the harbor is lovely right now.","Salt in your hair, a good stitch in your sleeve. That's island fashion.","Some corals carry their own tiny stars. I study how they shine.","Today's bulletin: the harbor is open! Exploration routes still need proper preparation.","Welcome! A good fishing story needs one impossible detail.","Measure twice, sand once more. Your aquarium deserves a proper home."};
            string[] bios={"Oliver repairs tackle and teaches patient fishing.","Marina grows plants for healthy freshwater and marine exhibits.","Rosie makes the harbor feel like a kitchen shared by friends.","Morris collects coastal history, one recovered object at a time.","A quiet trader who visits on alternate nights, from 20:00 to 04:00.","Bruno gives washed-up materials another useful life.","Iris collects moments of island life through her lens.","Lily makes comfortable clothes for rainy walks and sunny docks.","Luna keeps careful notes on luminous marine life.","Theo publishes authored local news and practical fishing tips.","Eddie gathers fishermen around stories and friendly games.","Henry crafts sturdy display furniture from warm island timber."};
            Vector2[] locations={new(10,-11),new(-10,-12),new(7,-3),new(-7,-5),new(-8,-22),new(13,3),new(-13,4),new(-20,-2),new(18,7),new(-3,10),new(6,9),new(21,10)};
            string[] hairstyles={"messy_short","braided","double_buns","short_classic","long_straight","curly_short","short_bob","high_ponytail","long_wavy","side_swept","medium_layered","short_classic"};
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Characters/OF_AvatarSurface.mat");world.residents=new TownResidentDefinition[12];
            for(int i=0;i<12;i++)
            {
                var d=Asset<TownResidentDefinition>(ids[i]);d.id=ids[i];d.nameKey="town.npc."+ids[i];d.shopKey="town.shop."+ids[i];d.professionKey="town.profession."+ids[i];d.biographyKey="town.bio."+ids[i];d.greetingKey="town.greeting."+ids[i];
                Text("npc."+ids[i],names[i]);Text("shop."+ids[i],shops[i]);Text("profession."+ids[i],roles[i]);Text("bio."+ids[i],bios[i]);Text("greeting."+ids[i],greetings[i]);
                d.work=Ground(locations[i].x,locations[i].y);d.home=Ground(locations[i].x,locations[i].y+1);d.social=Ground(-4+i*.65f,-6);d.nightMerchant=i==4;
                d.appearance=new AppearanceData{presentation=i%2==0?"masculine":"feminine",hair=hairstyles[i],hairColor=CosmeticCatalog.Options["hairColor"][i],skin=CosmeticCatalog.Options["skin"][i],outfitColor=CosmeticCatalog.Options["outfitColor"][i%6],top=i%3==0?"fisherman_shirt":i%3==1?"jacket":"sweater",bottom=i%4==1?"long_skirt":"pants",hat=i==0?"fisherman_hat":i==3?"bucket_hat":i==11?"beanie":"none",glasses=i==6||i==9?"round_glasses":"none",accessory=i==6?"crossbody":i==7?"necklace":i==0?"backpack":"none",beard=i==3?"full_beard":i==11?"mustache":"none"};d.appearance.Validate();
                d.loved=new[]{i%2==0?"rare_shell":"island_tea"};d.liked=new[]{"driftwood"};d.disliked=new[]{"scrap"};d.rewardResource=new[]{"coastal_rod","rare_plant","island_tea","treasure_clue","sea_pearl","hull_component","camera_filter","exclusive_outfit","luminous_decor","fishing_clue","cove_clue","premium_furniture"}[i];d.rewardHearts=2;d.rewardQuantity=i==2?3:1;d.rewardBuildItem=i==1?AssetDatabase.LoadAssetAtPath<AquariumBuildItem>(Root+"/Building/plant_7.asset"):i==8?AssetDatabase.LoadAssetAtPath<AquariumBuildItem>(Root+"/Building/decor_1.asset"):i==11?AssetDatabase.LoadAssetAtPath<AquariumBuildItem>(Root+"/Building/furniture_8.asset"):null;
                var npc=new GameObject(names[i]);npc.transform.SetParent(root.transform);npc.transform.position=d.work;npc.transform.rotation=Quaternion.Euler(0,180,0);var avatar=npc.AddComponent<CharacterAppearance>();avatar.loadLocal=false;avatar.surface=material;var c=npc.AddComponent<CapsuleCollider>();c.radius=.32f;c.height=1.8f;c.center=Vector3.up*.9f;var resident=npc.AddComponent<TownResident>();resident.definition=d;resident.world=world;npc.AddComponent<TownRoutine>();
                Portrait(d);world.residents[i]=d;EditorUtility.SetDirty(d);
                var sign=new GameObject(shops[i]+" sign");sign.transform.SetParent(root.transform);sign.transform.position=d.work+new Vector3(0,2.4f,.4f);sign.transform.rotation=Quaternion.identity;var lettering=sign.AddComponent<TextMesh>();lettering.text=shops[i].Replace(" & "," &\n");lettering.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");lettering.fontSize=48;lettering.characterSize=.055f;lettering.anchor=TextAnchor.MiddleCenter;lettering.alignment=TextAlignment.Center;lettering.color=new Color(.97f,.9f,.7f);sign.GetComponent<MeshRenderer>().sharedMaterial=lettering.font.material;if(d.nightMerchant){var display=sign.AddComponent<TownMerchantDisplay>();display.world=world;display.resident=d;}
            }
            TownProductDefinition Product(string id,string label,int price,string build=null,string currency="coins")
            {var p=Asset<TownProductDefinition>(id);p.id=id;p.nameKey="town.item."+id;p.resourceId=id;p.price=price;p.currency=currency;p.stockLimit=10;p.quantity=1;p.buildItem=build==null?null:AssetDatabase.LoadAssetAtPath<AquariumBuildItem>(Root+"/Building/"+build+".asset");Text("item."+id,label);EditorUtility.SetDirty(p);return p;}
            world.residents[0].products=new[]{Product("coastal_rod","Coastal rod · +3 m cast, +15% strength",18),Product("braided_line","Braided line · +20% line strength",8),Product("balanced_hook","Balanced hook · +12% handling",6)};foreach(var p in world.residents[0].products)p.stockLimit=1;
            world.residents[1].products=new[]{Product("java_moss","Java moss · aquarium build storage",2,"plant_1"),Product("anubias","Anubias · aquarium build storage",3,"plant_0")};
            world.residents[2].products=new[]{Product("island_tea","Island tea · gift item",2)};
            world.residents[3].products=new[]{Product("antique_decor","Shipwreck decoration · aquarium storage",5,"decor_7")};
            world.residents[4].products=new[]{Product("gull_coral","Rare coral · aquarium storage",2,"decor_1","rare_shell")};
            world.residents[5].products=new[]{Product("recycled_bench","Recycled bench · aquarium storage",3,"furniture_0","scrap")};
            world.residents[6].products=new[]{Product("camera","Pocket camera · press P to save a photo",8),Product("picture_frame","Picture frame · aquarium storage",5,"furniture_12")};
            var hat=Product("tailored_hat","Fisherman hat · equips on purchase",10);hat.cosmeticSlot="hat";hat.cosmeticItem="fisherman_hat";hat.stockLimit=1;var coat=Product("tailored_coat","Cozy jacket · equips on purchase",12);coat.cosmeticSlot="top";coat.cosmeticItem="jacket";coat.stockLimit=1;world.residents[7].products=new[]{hat,coat};
            world.residents[8].products=new[]{Product("luna_coral","Marine coral · aquarium storage",4,"decor_0")};
            world.residents[9].products=Array.Empty<TownProductDefinition>();world.residents[10].products=Array.Empty<TownProductDefinition>();
            world.residents[11].products=new[]{Product("oak_bench","Oak bench · aquarium storage",4,"furniture_0"),Product("display_stand","Display stand · aquarium storage",6,"furniture_7")};
            ExpandProducts(world);AddQuests(world);ShopArt(world);FinishTown(world);foreach(var d in world.residents)EditorUtility.SetDirty(d);
            Text("talk","[E] Talk to resident","[E] Kasabalıyla konuş");Text("hearts","hearts","kalp");Text("coins","Coins","Sikke");Text("rare_shell","Rare Shells","Nadir kabuk");Text("scrap","Scrap Metal","Hurda metal");Text("greeting","Conversation","Sohbet");Text("shop","Shop","Dükkan");Text("gifts","Gifts","Hediyeler");Text("sell","Sell fish","Balık sat");Text("buy","Purchase","Satın al");Text("give","Give one item","Bir tane hediye et");Text("chat","How is your day?","Günün nasıl geçiyor?");Text("claim_friendship","Claim friendship reward","Arkadaşlık ödülünü al");Text("confirm","Confirm this transaction?","Bu işlemi onaylıyor musun?");Text("confirm_button","Confirm","Onayla");Text("cancel","Cancel","Vazgeç");Text("sell_selected","Sell the selected specimen for its displayed price.","Seçilen balığı gösterilen fiyattan sat.");Text("fish_market","Oliver at Tidal Tackle buys your stored fish.","Tidal Tackle'daki Oliver depodaki balıklarını satın alır.");Text("success","Done! Your progress is saved.","Tamam! İlerlemen kaydedildi.");
            foreach(var pair in new[]{("owner","This inventory belongs to another player."),("save","Could not save. Nothing was spent."),("replay","This transaction or reward was already claimed."),("stale","The inventory changed. Please select again."),("unavailable","This resident is unavailable."),("locked","This item is not unlocked."),("stock","This item is sold out."),("funds","Not enough currency."),("daily","Come back tomorrow for more friendship."),("inventory","That item is not available in your inventory."),("owned","You already own this style.")})Text("error."+pair.Item1,pair.Item2);
            foreach(var id in new[]{"loved","liked","neutral","disliked"})Text("preference."+id,id);
            Text("item.rare_shell","Rare shell");Text("item.driftwood","Driftwood");Text("item.scrap","Scrap metal");Text("item.rope","Rope bundle");
            Text("arcade_start","Play the fishing timing game");Text("arcade_rules","Free play. Click Reel while the marker is in green. Land 3 of 5 attempts to earn 3 Coins, once per town day. No entry fee.");Text("arcade_reel","Reel!");Text("arcade_win","Well fished! Daily reward saved if not already claimed.");Text("arcade_retry","Good practice. Start another round from the conversation.");
            Text("drink_tea","Drink one owned tea · 10% shorter bite waits for five minutes","Bir çay iç · Beş dakika boyunca balık bekleme süresi %10 azalır");
            Text("bulletin","HARBOR BULLETIN — Authored report\nHome and Town harbors are open. Current skies are clear; dynamic weather arrives in the next milestone. Visit rocky shores for different fish. Keep displayed specimens in their tanks until you choose to remove them.");
            Text("quests","Quests","Görevler");Text("residents","Town residents","Kasaba sakinleri");Text("known_gifts","Known gifts","Bilinen hediyeler");Text("accept","Accept quest","Görevi kabul et");Text("claim","Claim reward","Ödülü al");Text("track","Track quest","Görevi takip et");Text("filter.0","Active quests","Aktif görevler");Text("filter.1","Available quests","Mevcut görevler");Text("filter.2","Completed quests","Tamamlanan görevler");Text("error.objective","Finish the objective before claiming this reward.","Ödül almadan önce hedefi tamamla.");Text("collect","[E] Collect coastal materials","[E] Kıyı malzemelerini topla");
            Directory.CreateDirectory(Root+"/Resources");var content=AssetDatabase.LoadAssetAtPath<TownCatalog>(Root+"/Resources/TownContent.asset");if(!content){content=ScriptableObject.CreateInstance<TownCatalog>();AssetDatabase.CreateAsset(content,Root+"/Resources/TownContent.asset");}content.residents=world.residents;EditorUtility.SetDirty(content);
            EditorUtility.SetDirty(en);EditorUtility.SetDirty(tr);EditorUtility.SetDirty(en.SharedData);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Debug.Log("ASTRA 0.8: twelve residents and first shop slice generated.");
        }
    }
}





