using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using com.github.pandrabox.pandravase.runtime;
using com.github.pandrabox.pandravase.editor;
using UnityEditor;
using static com.github.pandrabox.pandravase.editor.Util;
using static com.github.pandrabox.pandravase.editor.Localizer;
using System.Collections.Generic;
using System.Linq;

namespace com.github.pandrabox.flatsplus.runtime
{
    public class FlatsPlus : PandraComponent
    {
        public bool Func_Carry = true;
        public bool Func_DanceController = true;
        public bool Func_Emo = true;
        public bool Func_Explore = true;
        public bool Func_Hoppe = true;
        public bool Func_Ico = true;
        public bool Func_Light = true;
        public bool Func_MakeEmo = true;
        public bool Func_MeshSetting = true;
        public bool Func_Move = true;
        public bool Func_Onaka = true;
        public bool Func_Pen = true;
        public bool Func_Sleep = true;
        public bool Func_Tail = true;
        public bool Func_Link = true;
        public bool Func_Sync = true;
        public bool Func_WriteDefaultOn = true;
        public bool Func_ClippingCanceler = true; 

        public string Language = null;
        public DateTime LastBuild = DateTime.MinValue;

        public float Emo_TransitionTime = 0.5f;
        public Texture2D[] Ico_Textures = new Texture2D[6];
        public bool Ico_VerView = false;
        public bool Light_IntensityPerfectSync = false;
        public float MakeEmo_MenuSize = 0.35f;
        public float MakeEmo_LockSize = 0.08f;
        public float MakeEmo_MenuOpacity = 0.85f;
        public Color MakeEmo_SelectColor = new Color(0, 210, 255, 200);
        public Color MakeEmo_BackGroundColor = new Color(0, 0, 0, 150);
        public int MakeEmo_Margin = 13;
        public float MakeEmo_ScrollSpeed = 0.03f;
        public float MakeEmo_DeadZone = 0.3f;
        public float Onaka_Pull = 0.5f;
        public float Onaka_Spring = 0.8f;
        public float Onaka_Gravity = 0.2f;
        public float Onaka_GravityFallOff = 1f;
        public float Onaka_Immobile = 0.8f;
        public float Onaka_LimitAngle = 20f;
        public float Onaka_RadiusTuning = 1f;
        public float Tail_SwingPeriod = 1.5f;
        public float Tail_SwingAngle = 60;
        public float Tail_SizeMax = 1;
        public float Tail_SizeMin = 0.01f;
        public bool Tail_SizePerfectSync = false;
        public float Tail_DefaultSize = .5f; //0～1
        public float Tail_GravityRange = .3f; //0～1
        public bool Tail_GravityPerfectSync = false;
        public float Tail_DefaultGravity = .5f; //0～1
    }


    [CustomEditor(typeof(FlatsPlus))]
    public class FlatsPlusEditor : PandraEditor
    {
        FlatsPlusEditor() : base(true, "FlatsPlus", ProjectTypes.VPM) { }

        private const int _titleSize = 110;

        private SerializedProperty funcCarry, funcDanceController, funcEmo, funcExplore, funcHoppe, funcIco, funcLight, funcMakeEmo, funcMeshSetting, funcMove, funcOnaka, funcPen, funcSleep, funcTail, funcLink, funcSync, language, writedefaulton, clippingCanceler;

        public override void OnInnerInspectorGUI()
        {
            DrawLanguageSelect(language);
            DrawPropertyField(funcCarry, "Func/Carry");
            DrawPropertyField(funcDanceController, "Func/DanceController");
            DrawPropertyField(funcEmo, "Func/Emo");
            DrawPropertyField(funcExplore, "Func/Explore");
            DrawPropertyField(funcHoppe, "Func/Hoppe");
            DrawPropertyField(funcIco, "Func/Ico");
            DrawPropertyField(funcLight, "Func/Light");
            DrawPropertyField(funcMakeEmo, "Func/MakeEmo");
            DrawPropertyField(funcMeshSetting, "Func/MeshSetting");
            DrawPropertyField(funcMove, "Func/Move");
            DrawPropertyField(funcOnaka, "Func/Onaka");
            DrawPropertyField(funcPen, "Func/Pen");
            DrawPropertyField(funcSleep, "Func/Sleep");
            DrawPropertyField(funcTail, "Func/Tail");
            DrawPropertyField(funcLink, "Func/Link");
            DrawPropertyField(funcSync, "Func/Sync");
            DrawPropertyField(writedefaulton, "Func/WriteDefaultOn");
            DrawAllChangeField();
            DrawClippingCanceler();
        }

        private static readonly Dictionary<string, string> languageDisplayNames = new Dictionary<string, string>
                    {
                        { "en", "English" },
                        { "ja", "日本語" },
                        { "ko", "한국어" },
                        { "zh-CN", "简体中文" },
                        { "zh-TW", "繁體中文" }
                    };
        private static readonly string[] languageCodes = { "en", "ja", "ko", "zh-CN", "zh-TW" };
        private void DrawLanguageSelect(SerializedProperty property)
        {
            int selectedIndex = Array.IndexOf(languageCodes, property.stringValue);
            if (selectedIndex == -1)
            {
                property.stringValue = GetDefaultLanguage();
                selectedIndex = Array.IndexOf(languageCodes, property.stringValue);
                if (selectedIndex == -1)
                {
                    selectedIndex = 0;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Language", GUILayout.Width(20 + _titleSize));

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup(selectedIndex, languageCodes.Select(code => languageDisplayNames[code]).ToArray());
            property.stringValue = languageCodes[selectedIndex];
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Localizer.SetLanguage(property.stringValue);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPropertyField(SerializedProperty property, string key)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Width(20));
            EditorGUILayout.LabelField(L($"{key}/Name"), GUILayout.Width(_titleSize));
            EditorGUILayout.LabelField(L($"{key}/Detail"));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAllChangeField()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Editor/AllOff")))
            {
                funcCarry.boolValue = false;
                funcDanceController.boolValue = false;
                funcEmo.boolValue = false;
                funcExplore.boolValue = false;
                funcHoppe.boolValue = false;
                funcIco.boolValue = false;
                funcLight.boolValue = false;
                funcMakeEmo.boolValue = false;
                funcMeshSetting.boolValue = false;
                funcMove.boolValue = false;
                funcOnaka.boolValue = false;
                funcPen.boolValue = false;
                funcSleep.boolValue = false;
                funcTail.boolValue = false;
                funcLink.boolValue = false;
                funcSync.boolValue = false;
                writedefaulton.boolValue = false;
            }
            if (GUILayout.Button(L("Editor/AllOn")))
            {
                funcCarry.boolValue = true;
                funcDanceController.boolValue = true;
                funcEmo.boolValue = true;
                funcExplore.boolValue = true;
                funcHoppe.boolValue = true;
                funcIco.boolValue = true;
                funcLight.boolValue = true;
                funcMakeEmo.boolValue = true;
                funcMeshSetting.boolValue = true;
                funcMove.boolValue = true;
                funcOnaka.boolValue = true;
                funcPen.boolValue = true;
                funcSleep.boolValue = true;
                funcTail.boolValue = true;
                funcLink.boolValue = true;
                funcSync.boolValue = true;
                writedefaulton.boolValue = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        protected override void DefineSerial()
        {
            funcCarry = serializedObject.FindProperty("Func_Carry");
            funcDanceController = serializedObject.FindProperty("Func_DanceController");
            funcEmo = serializedObject.FindProperty("Func_Emo");
            funcExplore = serializedObject.FindProperty("Func_Explore");
            funcHoppe = serializedObject.FindProperty("Func_Hoppe");
            funcIco = serializedObject.FindProperty("Func_Ico");
            funcLight = serializedObject.FindProperty("Func_Light");
            funcMakeEmo = serializedObject.FindProperty("Func_MakeEmo");
            funcMeshSetting = serializedObject.FindProperty("Func_MeshSetting");
            funcMove = serializedObject.FindProperty("Func_Move");
            funcOnaka = serializedObject.FindProperty("Func_Onaka");
            funcPen = serializedObject.FindProperty("Func_Pen");
            funcSleep = serializedObject.FindProperty("Func_Sleep");
            funcTail = serializedObject.FindProperty("Func_Tail");
            funcLink = serializedObject.FindProperty("Func_Link");
            funcSync = serializedObject.FindProperty("Func_Sync");
            language = serializedObject.FindProperty("Language");
            writedefaulton = serializedObject.FindProperty("Func_WriteDefaultOn");
            clippingCanceler = serializedObject.FindProperty("Func_ClippingCanceler");
        }

        protected override void OnInnerEnable()
        {
            LogAnalyze();
        }

        private void DrawClippingCanceler()
        {
            EditorGUI.BeginChangeCheck();
            DrawPropertyField(clippingCanceler, "Func/ClippingCanceler");
            if (EditorGUI.EndChangeCheck())
            {
                new SetClippingCanceler(clippingCanceler.boolValue);
            }
        }



        private void LogAnalyze()
        {

        }
    }
}