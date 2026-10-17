using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
namespace OceanariumFishing.Editor
{
    public static class LocalizationChecks
    {
        private static bool running;
        private static readonly List<string> results=new();
        [MenuItem("Oceanarium Fishing/Run Live Language Checks")]
        public static void Run()
        {
            if(running || !EditorApplication.isPlaying || GameSession.Current==null)return;
            running=true;GameSession.Current.StartCoroutine(Checks());
        }
        private static void Check(bool condition,string message)
        {
            results.Add((condition?"PASS: ":"FAIL: ")+message);
            if(!condition)throw new InvalidOperationException(message);
        }
        private static IEnumerator Settled(LanguageService language)
        {
            double deadline=EditorApplication.timeSinceStartup+15;
            while(language.Busy && EditorApplication.timeSinceStartup<deadline)yield return null;
            Check(!language.Busy && language.Ready,"Language load completed");
        }
        private static IEnumerator Travel(GameSession session,Destination destination)
        {
            session.Travel(destination);double deadline=EditorApplication.timeSinceStartup+20;
            while(session.Loading && EditorApplication.timeSinceStartup<deadline)yield return null;
            yield return null;
            Check(!session.Loading && SceneManager.GetActiveScene().name=="OF_"+destination,"Travel to "+destination);
        }
        private static IEnumerator Checks()
        {
            results.Clear();bool complete=false;var session=GameSession.Current;var language=session.Languages;
            string original=PlayerPrefs.GetString("OF.Locale","en");GameObject freshObject=null;int notifications=0;
            Action changed=()=>{notifications++;Check(language.Text("settings")==LocalizationContent.Expected(language.SelectedCode,"settings"),"Changed event sees the completed translated cache");};
            language.Changed+=changed;
            try
            {
                yield return Settled(language);
                foreach(var code in LanguageService.Codes)
                {
                    language.Select(code);yield return Settled(language);
                    Check(language.SelectedCode==code && LocalizationSettings.SelectedLocale.Identifier.Code==code,"Service and Unity locale agree: "+code);
                    var table=AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.StringTable>(FoundationBuilder.Root+"/Localization/Tables/OF_UI_"+code+".asset");
                    var english=AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.StringTable>(FoundationBuilder.Root+"/Localization/Tables/OF_UI_en.asset");
                    foreach(var entry in english.Values)
                        Check(language.Text(entry.Key)==(table.GetEntry(entry.KeyId)?.LocalizedValue??entry.LocalizedValue),code+" translated or English fallback: "+entry.Key);
                    Check(PlayerPrefs.GetString("OF.Locale")==code,"Saved preference: "+code);
                }
                LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale("en");yield return Settled(language);
                LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale("tr");yield return Settled(language);
                Check(language.SelectedCode=="tr" && language.Text("settings")=="Ayarlar ve dil","External Unity selector refreshes Turkish HUD cache");
                language.Select("de");language.Select("fr");language.Select("tr");yield return Settled(language);
                Check(language.SelectedCode=="tr" && PlayerPrefs.GetString("OF.Locale")=="tr","Rapid changes keep the latest request");
                language.Select("de");LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale("tr");yield return Settled(language);
                Check(language.SelectedCode=="tr","External locale change wins during pending load");
                language.Select("missing-locale");yield return Settled(language);
                Check(language.SelectedCode=="en" && language.Text("close")=="Close","Unknown locale safely falls back to English");
                language.Select("tr");yield return Settled(language);
                language.enabled=false;
                freshObject=new GameObject("Temporary locale persistence check");var fresh=freshObject.AddComponent<LanguageService>();
                yield return fresh.Initialize();
                Check(fresh.Ready && fresh.SelectedCode=="tr" && fresh.Text("back")=="Ana menüye dön","Fresh service restores saved Turkish preference and text");
                UnityEngine.Object.DestroyImmediate(freshObject);freshObject=null;language.enabled=true;
                yield return Travel(session,Destination.MainMenu);
                Check(session.Languages.Text("play")=="Adayı keşfetmeye başla" && UnityEngine.Object.FindFirstObjectByType<FoundationHUD>()!=null,"Main menu retains Turkish with active HUD");
                yield return Travel(session,Destination.HomeIsland);
                Check(session.Languages.Text("home")=="YUVANIN RESİFİ" && UnityEngine.Object.FindFirstObjectByType<FoundationHUD>()!=null,"Home Island retains Turkish with active HUD");
                Check(notifications>=17,"Language updates notify listeners");complete=true;
            }
            finally
            {
                if(freshObject!=null)UnityEngine.Object.DestroyImmediate(freshObject);
                language.enabled=true;language.Changed-=changed;language.Select(original);
                Directory.CreateDirectory("Docs");File.WriteAllText("Docs/ASTRA_01_LOCALIZATION_TEST_RESULTS.txt",(complete?"PASS":"FAIL")+" - "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",results));
                running=false;Debug.Log("Live language checks: "+(complete?"PASS":"FAIL"));
            }
        }
    }
}
