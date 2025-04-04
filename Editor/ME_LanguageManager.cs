using com.github.pandrabox.pandravase.editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;

namespace com.github.pandrabox.flatsplus.editor
{
    // 言語設定を担当するクラス
    public class ME_LanguageManager
    {

        private static ME_LanguageManager _instance;
        public static ME_LanguageManager Instance => _instance ?? (_instance = new ME_LanguageManager());

        private SerializedProperty _languageProperty;
        private const int _titleSize = 110;

        // 言語表示名と言語コードのマッピング
        private readonly Dictionary<string, string> _languageDisplayNames = new Dictionary<string, string>
        {
            { "en", "English" },
            { "ja", "日本語" },
            { "ko", "???" },
            { "zh-CN", "?体中文" },
            { "zh-TW", "繁體中文" }
        };
        private readonly string[] _languageCodes = { "en", "ja", "ko", "zh-CN", "zh-TW" };

        private ME_LanguageManager() { }

        public void Initialize(SerializedProperty languageProperty)
        {
            _languageProperty = languageProperty;
            
            // 初期言語設定
            SetInitialLanguage();
        }

        // 初期言語設定
        private void SetInitialLanguage()
        {
            if (_languageProperty == null) return;

            // 既定の言語を設定
            if (string.IsNullOrEmpty(_languageProperty.stringValue) ||
                !_languageCodes.Contains(_languageProperty.stringValue))
            {
                _languageProperty.stringValue = GetDefaultLanguage();
            }

            // ローカライズシステムに言語を設定
            Localizer.SetLanguage(_languageProperty.stringValue);
        }

        // 言語選択UIの描画
        public void DrawLanguageSelector(SerializedObject serializedObject)
        {
            if (_languageProperty == null) return;

            int selectedIndex = Array.IndexOf(_languageCodes, _languageProperty.stringValue);
            if (selectedIndex == -1) selectedIndex = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Language", GUILayout.Width(20 + _titleSize));

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup(selectedIndex, _languageCodes.Select(code => _languageDisplayNames[code]).ToArray());
            _languageProperty.stringValue = _languageCodes[selectedIndex];
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Localizer.SetLanguage(_languageProperty.stringValue);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
