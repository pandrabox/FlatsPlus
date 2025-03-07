using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.flatsplus.editor
{
    public static class Localizer
    {
        public static string Language;
        private static Dictionary<string, string> localizationDictionary = new Dictionary<string, string>();
        private const string dataPath = "Packages/com.github.pandrabox.flatsplus/Editor/Localize/Localize.csv";

        private static Dictionary<SystemLanguage, string> languageMap = new Dictionary<SystemLanguage, string>()
            {
                { SystemLanguage.English, "en" },
                { SystemLanguage.Japanese, "ja" },
                { SystemLanguage.Korean, "ko" },
                { SystemLanguage.ChineseSimplified, "zh-CN" },
                { SystemLanguage.ChineseTraditional, "zh-TW" }
            };

        public static void SetLanguage()
        {
            SystemLanguage systemLanguage = Application.systemLanguage;
            if (languageMap.ContainsKey(systemLanguage))
            {
                Language = languageMap[systemLanguage];
            }
            else
            {
                Language = "en";
            }
            ClearCache();
        }

        private static void LoadText()
        {
            if (localizationDictionary.Count > 0) return;
            if (Language == null) SetLanguage();
            localizationDictionary = new Dictionary<string, string>();
            if (File.Exists(dataPath))
            {
                string[] lines = File.ReadAllLines(dataPath);
                string[] headers = lines[0].Split(',');
                int langIndex = Array.IndexOf(headers, Language);
                if (langIndex == -1) langIndex = Array.IndexOf(headers, "en");
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] columns = lines[i].Split(',');
                    if (columns.Length > langIndex)
                    {
                        string key = columns[0];
                        string value = columns[langIndex];
                        if (localizationDictionary.ContainsKey(key))
                        {
                            LowLevelExeption("Duplicate key found in localization file: " + key);
                        }
                        localizationDictionary[key] = value;
                    }
                }
            }
            else
            {
                LowLevelExeption("Localization file not found: " + dataPath);
            }
        }

        public static string L(string name)
        {
            LoadText();
            if (localizationDictionary.TryGetValue(name, out string res))
            {
                return res;
            }
            else
            {
                LowLevelExeption("Localization key not found: " + name);
                return name;
            }
        }

        private static void ClearCache()
        {
            localizationDictionary.Clear();
        }
    }
}