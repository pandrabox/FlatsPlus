using com.github.pandrabox.pandravase.editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;

namespace com.github.pandrabox.flatsplus.editor
{
    // ����ݒ��S������N���X
    public class ME_LanguageManager
    {

        private static ME_LanguageManager _instance;
        public static ME_LanguageManager Instance => _instance ?? (_instance = new ME_LanguageManager());

        private SerializedProperty _languageProperty;
        private const int _titleSize = 110;

        // ����\�����ƌ���R�[�h�̃}�b�s���O
        private readonly Dictionary<string, string> _languageDisplayNames = new Dictionary<string, string>
        {
            { "en", "English" },
            { "ja", "���{��" },
            { "ko", "???" },
            { "zh-CN", "?�̒���" },
            { "zh-TW", "��铒���" }
        };
        private readonly string[] _languageCodes = { "en", "ja", "ko", "zh-CN", "zh-TW" };

        private ME_LanguageManager() { }

        public void Initialize(SerializedProperty languageProperty)
        {
            _languageProperty = languageProperty;
            
            // ��������ݒ�
            SetInitialLanguage();
        }

        // ��������ݒ�
        private void SetInitialLanguage()
        {
            if (_languageProperty == null) return;

            // ����̌����ݒ�
            if (string.IsNullOrEmpty(_languageProperty.stringValue) ||
                !_languageCodes.Contains(_languageProperty.stringValue))
            {
                _languageProperty.stringValue = GetDefaultLanguage();
            }

            // ���[�J���C�Y�V�X�e���Ɍ����ݒ�
            Localizer.SetLanguage(_languageProperty.stringValue);
        }

        // ����I��UI�̕`��
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
