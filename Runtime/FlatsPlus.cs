#if UNITY_EDITOR

using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;

/* GitHubCopilotへのための連絡 
「D_***を反映して」と依頼した場合、次の手順を厳密に守って下さい
- 次を実行してください。
- FlatsPlusより該当の変数を取得します
- FlatsPlusEditorで次を行い、全文のコードを表示します（途中経過の報告は不要です）
    - private SerializedProperty　へ変数を追加する
    - protected override void DefineSerialへ記述を追加する
　　- DrawDetailの適切な場所への追加（ここのshowDetailは全てOFF）
- 「DrawDetailに値を追加したことに伴い、PanLocalize.txtに追加すべきテキスト」をチャットに返します
    - サンプル
        Key,ja,en,ko,zh-CN,zh-TW
        D_Hoppe_AllowTouch/Name,接触許可,Allow Touch,접촉 허용,允许触摸,允许触摸
        D_Hoppe_AllowTouch/Detail,ほっぺたを触ることを許可する,Allow touching the cheeks,볼을 만질 수 있게 허용,允许触摸脸颊,允许触摸脸颊
*/

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
        public bool D_Hoppe_AllowTouch = true;
        public bool D_Hoppe_AllowStretch = true;
        public float D_Hoppe_StretchLimit = 1f;//0～2
        public bool D_Hoppe_Blush = true;
        public float D_Hoppe_Blush_Sensitivity = 1f;//0～1
        public bool D_Hoppe_UseOriginalBlush = true;
        public Hoppe_BlushControlType D_Hoppe_BlushControlType = Hoppe_BlushControlType.WithoutDance;
        public enum Hoppe_BlushControlType { Auto, OtherOnly, WithoutDance, On, Off }
        public bool D_Hoppe_ShowExpressionMenu = false;

        public bool D_Carry_AllowBlueGateDefault = true;
        public float D_Carry_GateMaxRange = 1f;

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
        public bool Func_PoseClipper = true;
    }
}

namespace com.github.pandrabox.flatsplus.editor
{

    [CustomEditor(typeof(FlatsPlus))]
    public class FlatsPlusEditor : PandraEditor
    {
        FlatsPlusEditor() : base(true, "FlatsPlus", ProjectTypes.VPM) { }

        private const int _titleSize = 110;

        private SerializedProperty
            funcCarry, funcDanceController, funcEmo, funcExplore, funcHoppe, funcIco, funcLight, funcMakeEmo, funcMeshSetting, funcMove, funcOnaka, funcPen, funcSleep, funcTail, funcLink, funcSync,
            language, writedefaulton, clippingCanceler, funcPoseClipper,
            dHoppeAllowTouch, dHoppeAllowStretch, dHoppeStretchLimit, dHoppeBlush, dHoppeBlushSensitivity, dUseOriginalBlush, dHoppeBlushControlType, dHoppeShowExpressionMenu,
            dCarryAllowBluGateDefault, dCarryGateMaxRange;

        protected override void DefineSerial()
        {
            funcCarry = serializedObject.FindProperty(nameof(FlatsPlus.Func_Carry));
            funcDanceController = serializedObject.FindProperty(nameof(FlatsPlus.Func_DanceController));
            funcEmo = serializedObject.FindProperty(nameof(FlatsPlus.Func_Emo));
            funcExplore = serializedObject.FindProperty(nameof(FlatsPlus.Func_Explore));
            funcHoppe = serializedObject.FindProperty(nameof(FlatsPlus.Func_Hoppe));
            funcIco = serializedObject.FindProperty(nameof(FlatsPlus.Func_Ico));
            funcLight = serializedObject.FindProperty(nameof(FlatsPlus.Func_Light));
            funcMakeEmo = serializedObject.FindProperty(nameof(FlatsPlus.Func_MakeEmo));
            funcMeshSetting = serializedObject.FindProperty(nameof(FlatsPlus.Func_MeshSetting));
            funcMove = serializedObject.FindProperty(nameof(FlatsPlus.Func_Move));
            funcOnaka = serializedObject.FindProperty(nameof(FlatsPlus.Func_Onaka));
            funcPen = serializedObject.FindProperty(nameof(FlatsPlus.Func_Pen));
            funcSleep = serializedObject.FindProperty(nameof(FlatsPlus.Func_Sleep));
            funcTail = serializedObject.FindProperty(nameof(FlatsPlus.Func_Tail));
            funcLink = serializedObject.FindProperty(nameof(FlatsPlus.Func_Link));
            funcSync = serializedObject.FindProperty(nameof(FlatsPlus.Func_Sync));
            language = serializedObject.FindProperty(nameof(FlatsPlus.Language));
            writedefaulton = serializedObject.FindProperty(nameof(FlatsPlus.Func_WriteDefaultOn));
            clippingCanceler = serializedObject.FindProperty(nameof(FlatsPlus.Func_ClippingCanceler));
            funcPoseClipper = serializedObject.FindProperty(nameof(FlatsPlus.Func_PoseClipper));
            dHoppeAllowTouch = serializedObject.FindProperty(nameof(FlatsPlus.D_Hoppe_AllowTouch));
            dHoppeAllowStretch = serializedObject.FindProperty(nameof(FlatsPlus.D_Hoppe_AllowStretch));
            dHoppeStretchLimit = serializedObject.FindProperty(nameof(FlatsPlus.D_Hoppe_StretchLimit));
            dHoppeBlush = serializedObject.FindProperty(nameof(FlatsPlus.D_Hoppe_Blush));
            dHoppeBlushSensitivity = serializedObject.FindProperty(nameof(FlatsPlus.D_Hoppe_Blush_Sensitivity));
            dUseOriginalBlush = serializedObject.FindProperty(nameof(FlatsPlus.D_Hoppe_UseOriginalBlush));
            dHoppeBlushControlType = serializedObject.FindProperty(nameof(FlatsPlus.D_Hoppe_BlushControlType));
            dHoppeShowExpressionMenu = serializedObject.FindProperty(nameof(FlatsPlus.D_Hoppe_ShowExpressionMenu));
            dCarryAllowBluGateDefault = serializedObject.FindProperty(nameof(FlatsPlus.D_Carry_AllowBlueGateDefault));
            dCarryGateMaxRange = serializedObject.FindProperty(nameof(FlatsPlus.D_Carry_GateMaxRange));

        }

        private void OverView()
        {
            DrawLanguageSelect(language);
            FlatsPlusUpdater.I.DrawUpdateInfo();
            DrawBoolField(funcCarry, "Func/Carry", true);
            DrawBoolField(funcDanceController, "Func/DanceController");
            DrawBoolField(funcEmo, "Func/Emo");
            DrawBoolField(funcExplore, "Func/Explore");
            DrawBoolField(funcHoppe, "Func/Hoppe", true);
            DrawBoolField(funcIco, "Func/Ico");
            DrawBoolField(funcLight, "Func/Light");
            DrawBoolField(funcMakeEmo, "Func/MakeEmo");
            DrawBoolField(funcMeshSetting, "Func/MeshSetting");
            DrawBoolField(funcMove, "Func/Move");
            DrawBoolField(funcOnaka, "Func/Onaka");
            DrawBoolField(funcPen, "Func/Pen");
            DrawBoolField(funcSleep, "Func/Sleep");
            DrawBoolField(funcTail, "Func/Tail");
            DrawBoolField(funcLink, "Func/Link");
            DrawBoolField(funcSync, "Func/Sync");
            DrawBoolField(writedefaulton, "Func/WriteDefaultOn");
            DrawBoolField(funcPoseClipper, "Func/PoseClipper");
            DrawClippingCanceler();
            DrawAllChangeField();
            DrawDetail();
            LogAnalyzeResult();
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
                funcPoseClipper.boolValue = false;
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
                funcPoseClipper.boolValue = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        #region OnOverride
        protected override void OnInnerEnable()
        {
            LogAnalyze();
        }
        public override void OnInnerInspectorGUI()
        {
            OverView();
        }
        #endregion

        #region DrawHogehoge
        private void DrawBoolField(SerializedProperty property, string key, bool showDetails = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Width(20));
            EditorGUILayout.LabelField(L($"{key}/Name"), GUILayout.Width(_titleSize));
            EditorGUILayout.LabelField(L($"{key}/Detail"));
            if (showDetails && GUILayout.Button("Editor/Detail".LL(), GUILayout.Width(50)))
            {
                _detailKey = key;
                _showDetail = true;
            }
            EditorGUILayout.EndHorizontal();
        }
        private void DrawFloatField(SerializedProperty property, string key, float? min = null, float? max = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L($"{key}/Name"), GUILayout.Width(_titleSize + 20));
            property.floatValue = EditorGUILayout.Slider(property.floatValue, min ?? 0, max ?? 1);
            EditorGUILayout.EndHorizontal();
        }
        private void DrawEnumField(SerializedProperty property, string key)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L($"{key}/Name"), GUILayout.Width(_titleSize + 20));
            property.enumValueIndex = EditorGUILayout.Popup(property.enumValueIndex, property.enumDisplayNames);
            EditorGUILayout.EndHorizontal();
        }
        private string _detailKey = "";
        private bool _showDetail = false;
        private void DrawDetail()
        {
            if (!_showDetail) return;
            bool isDrawn = false;
            try
            {
                ShowDetailTitle();
                if (_detailKey == "Func/Hoppe")
                {
                    isDrawn = true;
                    DrawBoolField(dHoppeAllowTouch, "D_Hoppe_AllowTouch");
                    DrawBoolField(dHoppeAllowStretch, "D_Hoppe_AllowStretch");
                    DrawFloatField(dHoppeStretchLimit, "D_Hoppe_StretchLimit", 0, 2);
                    DrawBoolField(dHoppeBlush, "D_Hoppe_Blush");
                    DrawFloatField(dHoppeBlushSensitivity, "D_Hoppe_Blush_Sensitivity", 0, 3);
                    DrawBoolField(dUseOriginalBlush, "D_Hoppe_UseOriginalBlush");
                    DrawEnumField(dHoppeBlushControlType, "D_Hoppe_BlushControlType");
                    DrawBoolField(dHoppeShowExpressionMenu, "D_Hoppe_ShowExpressionMenu");
                }
                else if (_detailKey == "Func/Carry")
                {
                    isDrawn = true;
                    DrawBoolField(dCarryAllowBluGateDefault, "D_Carry_AllowBluGateDefault");
                    DrawFloatField(dCarryGateMaxRange, "D_Carry_GateMaxRange", 0.1f, 5);
                }
                if (GUILayout.Button("Editor/CloseDetail".LL()))
                {
                    _showDetail = false;
                }
            }
            finally
            {
                if (!isDrawn) _showDetail = false;
            }
        }
        private void ShowDetailTitle()
        {
            var before = L("Editor/Detail2");
            var after = L($"{_detailKey}/Name");
            var title = $@"{before} : {after}";
            Title(title);
        }
        private void DrawClippingCanceler()
        {
            EditorGUI.BeginChangeCheck();
            DrawBoolField(clippingCanceler, "Func/ClippingCanceler");
            if (EditorGUI.EndChangeCheck())
            {
                new SetClippingCanceler(clippingCanceler.boolValue);
            }
        }

        #endregion

        #region LogAnalyze
        private DateTime _lastBuild;
        private List<string> _errorWorks;
        private List<string> _errorUnknowns;
        const string _logPath = "Packages/com.github.pandrabox.flatsplus/Log/log.txt";
        private string _logContent;
        private void LogAnalyze()
        {
            _lastBuild = DateTime.MinValue;
            _errorWorks = new List<string>();
            _errorUnknowns = new List<string>();
            if (!File.Exists(_logPath)) return;
            _logContent = File.ReadAllText(_logPath);
            if (_logContent.Length == 0) return;
            string[] lines = _logContent.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("@@FlatsPlusBuildStart@@"))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 1 && DateTime.TryParse(parts[0], out DateTime buildDateTime))
                    {
                        _lastBuild = buildDateTime;
                    }
                }
                if (line.Contains("@@ERROR@@"))
                {
                    var parts = line.Split(',');
                    bool workError = false;
                    if (parts.Length >= 2)
                    {
                        var workName = parts[1];
                        if (workName.Length > 0)
                        {
                            _errorWorks.Add(workName);
                            workError = true;
                        }
                    }
                    if (!workError)
                    {
                        _errorUnknowns.Add(line);
                    }
                }
            }
        }
        private void LogAnalyzeResult()
        {
            if (_lastBuild == DateTime.MinValue) return;
            Title(L("LogAnalyze/Title") + $@" ({_lastBuild.ToString()})");
            //EditorGUILayout.LabelField(L("LogAnalyze/ExecutionTime"), _lastBuild.ToString());
            Dictionary<string, (int Warnings, int Errors, int Exceptions)> res = Log.AnalyzeLog(_logPath);
            bool allFine = false;
            if (res != null && res.Count == 0)
            {
                allFine=true;
                EditorGUILayout.HelpBox(L("LogAnalyze/AllFine"), MessageType.Info);
            }
            foreach (var item in res)
            {
                if (item.Value.Warnings > 0 || item.Value.Errors > 0 || item.Value.Exceptions > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(L("LogAnalyze/PleaseCallMe"));
                    sb.AppendLine($@"{item.Key}");
                    if (item.Value.Warnings>0) sb.AppendLine($@" - {L("LogAnalyze/Warning")}:{item.Value.Warnings}");
                    if (item.Value.Errors > 0　|| item.Value.Exceptions > 0) sb.AppendLine($@" - {L("LogAnalyze/Error")}:{item.Value.Errors + item.Value.Exceptions}");
                    EditorGUILayout.HelpBox(sb.ToString(), MessageType.Error);
                }
            }
            const bool ISPREVIEWVERSION = true;
            if (!allFine || ISPREVIEWVERSION)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(L("LogAnalyze/CopyLog")))
                    {
                        EditorGUIUtility.systemCopyBuffer = _logContent;
                        EditorUtility.DisplayDialog(L("LogAnalyze/CopyCompleteTitle"), L("LogAnalyze/CopyCompleteMessage"), "OK");
                    }
                    if (GUILayout.Button(L("Editor/CloseProgressBar")))
                    {
                        PanProgressBar.Hide();
                    }
                }
            }
        }
        #endregion

        #region Language

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
        #endregion
    }
}
#endif