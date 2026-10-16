using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
namespace OceanariumFishing
{
    public sealed class LanguageService : MonoBehaviour
    {
        public static readonly string[] Codes={"en","tr","fr","it","de","es-ES","zh-Hans","pt-BR","nl","zh-Hant","ja","ko","es-419","pl","pt-PT","ru","uk"};
        private readonly Dictionary<string,string> cache=new();
        public event Action Changed;
        public string SelectedCode {get; private set;}="en";
        public bool Ready {get; private set;}
        public bool Busy {get; private set;}
        public IEnumerator Initialize()
        {
            if(Ready) yield break;
            if(Busy) {while(Busy) yield return null;yield break;}
            Busy=true;
            yield return LocalizationSettings.InitializationOperation;
            yield return SelectRoutine(PlayerPrefs.GetString("OF.Locale","en"));
            Ready=true;Busy=false;
        }
        public string Text(string key) => cache.TryGetValue(key,out var text)?text:string.Empty;
        public void Select(string code) {if(!Busy) StartCoroutine(Change(code));}
        private IEnumerator Change(string code) {Busy=true;yield return SelectRoutine(code);Busy=false;}
        private IEnumerator SelectRoutine(string code)
        {
            var locale=LocalizationSettings.AvailableLocales.GetLocale(code) ?? LocalizationSettings.AvailableLocales.GetLocale("en");
            LocalizationSettings.SelectedLocale=locale;
            SelectedCode=locale.Identifier.Code;
            var source=LocalizationSettings.StringDatabase.GetTableAsync("OF_UI",LocalizationSettings.AvailableLocales.GetLocale("en"));
            yield return source;
            var selected=LocalizationSettings.StringDatabase.GetTableAsync("OF_UI",locale);
            yield return selected;
            cache.Clear();
            if(source.Result!=null)
                foreach(var entry in source.Result.Values)
                {
                    var translated=selected.Result?.GetEntry(entry.KeyId);
                    cache[entry.Key]=string.IsNullOrEmpty(translated?.LocalizedValue)?entry.LocalizedValue:translated.LocalizedValue;
                }
            PlayerPrefs.SetString("OF.Locale",SelectedCode);PlayerPrefs.Save();Changed?.Invoke();
        }
    }
}
