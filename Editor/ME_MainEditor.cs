using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using System;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Localizer;

namespace com.github.pandrabox.flatsplus.editor
{
    [CustomEditor(typeof(FlatsPlus))]
    public class ME_MainEditor : PandraEditor
    {
        ME_MainEditor() : base(true, "FlatsPlus", ProjectTypes.VPM) { }

        protected override void DefineSerial()
        {
            // シングルトンマネージャーの初期化
            ME_FuncManager.I.Initialize(serializedObject, this);

            // 言語マネージャーの初期化
            ME_LanguageManager.Instance.Initialize(ME_FuncManager.I.GetLanguageProperty());

            // ログ解析マネージャーの初期化
            ME_LogAnalyzer.Instance.Initialize(this);
        }

        protected override void OnInnerEnable()
        {
            // モジュールの初期化
            ME_FuncManager.I.OnEnableAll();
        }

        public override void OnInnerInspectorGUI()
        {
            // 言語選択UI表示
            ME_LanguageManager.Instance.DrawLanguageSelector(serializedObject);

            // 更新情報表示
            ME_Updater.I.DrawUpdateInfo();

            // 機能モジュールのメニュー表示
            ME_FuncManager.I.DrawAllMenus();

            // 機能の一括ON/OFF
            DrawAllChangeField();

            // 詳細設定表示
            ME_FuncManager.I.DrawDetailIfNeeded();

            DrawFlatPlusCheck();

            // ログ解析結果表示
            ME_LogAnalyzer.Instance.DrawAnalysisResults();
        }

        // 全機能の有効/無効切り替え
        private void DrawAllChangeField()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Editor/AllOff")))
            {
                ME_FuncManager.I.SetAllFunctionsEnabled(false);
            }
            if (GUILayout.Button(L("Editor/AllOn")))
            {
                ME_FuncManager.I.SetAllFunctionsEnabled(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        // FlatsPlusの旧バージョンであるFlatPlusの存在をチェックし、あれば警告を表示
        private void DrawFlatPlusCheck()
        {
            //そもそも型があるかチェック
            var flatPlusType = Type.GetType("Pan.Lib.FlatPlusManager, Assembly-CSharp");
            if (flatPlusType == null) return;

            //Descriptorを取得            
            if (!(target is MonoBehaviour monoBehaviour)) return;
            var thisObj = monoBehaviour.gameObject;
            var avatarDescriptor = thisObj.GetComponentInParent<VRCAvatarDescriptor>();
            if (avatarDescriptor == null) return;

            //Descriptorの子供にFlatPlusがあるかチェック
            var flatPlus = avatarDescriptor.gameObject.GetComponentInChildren(flatPlusType);
            if (flatPlus == null) return;

            //あるならAutoFixボタンを表示
            ShowTitle("旧バージョンの警告");
            EditorGUILayout.HelpBox("FlatPlusが見つかりました。FlatsPlusとの併用は推奨されません。AutoFixを押すと削除します。", MessageType.Warning);
            if (GUILayout.Button("AutoFix"))
            {
                DestroyImmediate(flatPlus.gameObject);
            }
        }

        // タイトル表示（外部からアクセス用）
        public void ShowTitle(string text)
        {
            Title(text);
        }
    }
}