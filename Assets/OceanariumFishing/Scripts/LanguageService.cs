using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
namespace OceanariumFishing
{
    public sealed class LanguageService : MonoBehaviour
    {
        public static readonly string[] Codes={"en","tr","fr","it","de","es-ES","zh-Hans","pt-BR","nl","zh-Hant","ja","ko","es-419","pl","pt-PT","ru","uk"};
        private Dictionary<string,string> cache=new();
        private string requestedCode;
        private bool assigningLocale, initialized;
        public event Action Changed;
        public string SelectedCode {get; private set;}="en";
        public bool Ready {get; private set;}
        public bool Busy {get; private set;}
        private void OnEnable() => LocalizationSettings.SelectedLocaleChanged+=LocaleChanged;
        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged-=LocaleChanged;
            StopAllCoroutines();Busy=false;
        }
        private void LocaleChanged(Locale locale)
        {
            // The editor locale selector and LocalizedString users share this source of truth.
            if(initialized && !assigningLocale && locale!=null)Select(locale.Identifier.Code);
        }
        public IEnumerator Initialize()
        {
            if(!Ready && !Busy)Select(PlayerPrefs.GetString("OF.Locale","en"));
            while(Busy)yield return null;
        }
        public string Text(string key) => cache.TryGetValue(key,out var text)?text:string.Empty;
        public void Select(string code)
        {
            // Preserve the latest request while asynchronous table loads are in progress.
            requestedCode=string.IsNullOrWhiteSpace(code)?"en":code;
            if(!Busy && isActiveAndEnabled){Busy=true;StartCoroutine(LoadSelections());}
        }
        private IEnumerator LoadSelections()
        {
            try
            {
                yield return LocalizationSettings.InitializationOperation;
                initialized=true;
                while(requestedCode!=null)
                {
                    var code=requestedCode;requestedCode=null;
                    var english=LocalizationSettings.AvailableLocales.GetLocale("en");
                    var locale=LocalizationSettings.AvailableLocales.GetLocale(code) ?? english;
                    assigningLocale=true;
                    try{LocalizationSettings.SelectedLocale=locale;}
                    finally{assigningLocale=false;}
                    var source=LocalizationSettings.StringDatabase.GetTableAsync("OF_UI",english);
                    yield return source;
                    var selected=LocalizationSettings.StringDatabase.GetTableAsync("OF_UI",locale);
                    yield return selected;
                    if(requestedCode!=null && requestedCode!=locale.Identifier.Code)continue;
                    if(source.Result==null){Debug.LogError("OF_UI English fallback table could not be loaded.");yield break;}
                    var next=new Dictionary<string,string>();
                    foreach(var entry in source.Result.Values)
                    {
                        var translated=selected.Result?.GetEntry(entry.KeyId);
                        next[entry.Key]=string.IsNullOrEmpty(translated?.LocalizedValue)?entry.LocalizedValue:translated.LocalizedValue;
                    }
                    // Publish the complete cache together; the HUD reads it every repaint.
                    cache=next;SelectedCode=locale.Identifier.Code;Ready=true;
                    PlayerPrefs.SetString("OF.Locale",SelectedCode);PlayerPrefs.Save();Changed?.Invoke();
                }
            }
            finally{Busy=false;}
        }
    }
}
