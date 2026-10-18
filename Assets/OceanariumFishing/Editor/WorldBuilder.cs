using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;
namespace OceanariumFishing.Editor
{
    public static class WorldBuilder
    {
        const string Root="Assets/OceanariumFishing",Dir=Root+"/World";
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            T Asset<T>(string path)where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(path);if(!a){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
            var settings=Asset<WorldSettings>(Root+"/Resources/WorldSettings.asset");
            string[] ids={"clear","partly_cloudy","cloudy","light_rain","heavy_rain","windy","thunderstorm","snow","light_fog"};string[] names={"Clear","Partly cloudy","Cloudy","Light rain","Heavy rain","Windy","Thunderstorm","Snow","Light fog"};
            float[][] weights={new[]{35f,55,22,25},new[]{20f,20,20,20},new[]{12f,8,20,20},new[]{18f,6,15,12},new[]{5f,2,6,6},new[]{4f,5,8,6},new[]{1f,2,2,1},new[]{0f,0,0,3},new[]{5f,2,7,7}};
            var en=AssetDatabase.LoadAssetAtPath<StringTable>(Root+"/Localization/Tables/OF_UI_en.asset");var tr=AssetDatabase.LoadAssetAtPath<StringTable>(Root+"/Localization/Tables/OF_UI_tr.asset");
            void Text(string key,string value){en.AddEntry("world."+key,value);tr.AddEntry("world."+key,value);}
            settings.weather=new WeatherProfile[9];
            for(int i=0;i<9;i++){var p=Asset<WeatherProfile>(Dir+"/"+ids[i]+".asset");p.id=ids[i];p.nameKey="world.weather."+ids[i];p.seasonalWeights=weights[i];p.clouds=new[]{0,.3f,.7f,.75f,.9f,.4f,1,.8f,.6f}[i];p.wind=new[]{.15f,.25f,.3f,.35f,.55f,.9f,1,.2f,.1f}[i];p.waves=1+p.wind;p.rain=i==3?.35f:i==4||i==6?1:0;p.snow=i==7?1:0;p.fog=i==8?.012f:p.rain>0?.005f:.0015f;p.visitors=i==0?1.1f:i==4||i==6?.75f:.95f;p.bite=i==3?1.12f:1;p.sun=1-p.clouds*.4f;settings.weather[i]=p;Text("weather."+ids[i],names[i]);EditorUtility.SetDirty(p);}
            string[] seasons={"Spring","Summer","Autumn","Winter"},months={"Blossom","Suncrest","Harvest","Frostfall"};settings.seasons=new SeasonProfile[4];
            for(int i=0;i<4;i++){var s=Asset<SeasonProfile>(Dir+"/"+seasons[i]+".asset");s.id=seasons[i].ToLowerInvariant();s.nameKey="world.season."+i;s.monthKey="world.month."+i;s.foliage=new[]{new Color(.94f,1.08f,.94f),Color.white,new Color(1.12f,.89f,.7f),new Color(.85f,.95f,1.05f)}[i];s.flowers=new[]{1,.9f,.65f,.35f}[i];s.leaves=i==2?1:0;s.snow=i==3?.1f:0;settings.seasons[i]=s;EditorUtility.SetDirty(s);Text("season."+i,seasons[i]);Text("month."+i,months[i]);}
            string[] weekdays={"Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday"};for(int i=0;i<7;i++)Text("weekday."+i,weekdays[i]);
            foreach(var pair in new[]{("today","Today"),("tomorrow","Tomorrow"),("wind","Wind"),("fish","Active fish nearby"),("calendar","Island calendar"),("close","Close"),("year","Year"),("merchant","Gull · 20:00"),("events_note","Gull trades Monday and Friday nights, until 02:00. Season changes on day 1."),("sleep","Sleep until morning"),("sleep_body","Rest at home until 08:00 tomorrow? Your belongings and quests are kept. No income is awarded for skipped time."),("sleep_confirm","Sleep until tomorrow"),("sleep_invalid","Return to your cabin and finish your current activity first."),("save_error","The world could not be saved. Time was not advanced.")})Text(pair.Item1,pair.Item2);
            en.AddEntry("town.error.closed","The shop is closed. Visit during opening hours, outside the lunch break.");tr.AddEntry("town.error.closed","Dükkan kapalı. Açılış saatlerinde, öğle molası dışında gel.");
            en.AddEntry("town.bio.gull","A quiet trader who visits Monday and Friday nights, from 20:00 to 02:00.");tr.AddEntry("town.bio.gull","Pazartesi ve cuma geceleri 20:00–02:00 arasında gelen sessiz bir tüccar.");
            foreach(var guid in AssetDatabase.FindAssets("t:FishSpecies",new[]{Root})){var fish=AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(guid));fish.weather=ids;fish.seasons=new[]{0,1,2,3};fish.preferredWeather=fish.habitats.Contains(FishHabitat.Rocky)?new[]{"light_fog","cloudy"}:new[]{"clear","light_rain"};if(fish.rarity>=FishRarity.Rare){fish.startHour=18;fish.endHour=8;}EditorUtility.SetDirty(fish);}
            EditorUtility.SetDirty(settings);EditorUtility.SetDirty(en);EditorUtility.SetDirty(tr);EditorUtility.SetDirty(en.SharedData);AssetDatabase.SaveAssets();Debug.Log("ASTRA 0.9 world definitions generated");
        }
    }
}
