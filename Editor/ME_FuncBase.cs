﻿using com.github.pandrabox.pandravase.editor;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;

namespace com.github.pandrabox.flatsplus.editor
{
    // 機能モジュールの基底クラス
    public abstract class ME_FuncBase
    {
        // 管理対象の機能名（例: nameof(FP.Func_Hoppe)）
        public abstract string ManagementFunc { get; }

        // まとめてONOFF機能の対象外
        public virtual bool ExcludeFromBulkToggle => false;


        // 詳細設定があるかどうか（DrawDetailがオーバーライドされているかで自動判定）
        public virtual bool HasDetailSettings
        {
            get
            {
                // GetType()で現在のインスタンスの実際の型を取得
                var drawDetailMethod = GetType().GetMethod("DrawDetail",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                // このメソッドが見つかる = オーバーライドされている
                return drawDetailMethod != null;
            }
        }

        // 機能の表示名のキーの一部
        // 例えば "Hoppe" -> "Func/Hoppe/Name" と "Func/Hoppe/Detail" でローカライズされる
        protected string DisplayKeyPart => ManagementFunc.Replace("Func_", "");

        // 初期化処理
        public virtual void OnEnable() { }

        // メニューの描画
        public virtual void DrawMenu()
        {
            DrawBoolField(ManagementFunc, HasDetailSettings);
        }

        // 詳細設定の描画（オーバーライドして実装）- 引数なしに変更
        public virtual void DrawDetail() { }

        // SerializedPropertyの取得（シングルトン経由）
        protected SerializedProperty SP(string name)
        {
            return ME_FuncManager.Instance.GetProperty(name);
        }

        // 詳細表示の要求
        protected void RequestDetailView()
        {
            ME_FuncManager.Instance.SetDetailModule(DisplayKeyPart, this);
        }

        //DrawBoolFieldのチェック変更時に呼ばれる
        public virtual void OnChange(bool state) { }


        // UI描画用のヘルパーメソッド - 基本機能表示用
        protected void DrawBoolField(string propName, bool showDetails = false)
        {
            SerializedProperty property = SP(propName);
            string keyBase = $"Func/{propName.Replace("Func_", "")}";

            EditorGUILayout.BeginHorizontal();

            // 変更前の状態を記録
            bool previousState = property.boolValue;

            // プロパティフィールドを描画
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Width(20));

            // 値が変更された場合
            if (EditorGUI.EndChangeCheck())
            {
                // 変更を適用
                ME_FuncManager.Instance.ApplyModifiedProperties();

                // 現在の機能のON/OFF状態が変わっていた場合、OnChangeを呼び出す
                if (propName == ManagementFunc && previousState != property.boolValue)
                {
                    OnChange(property.boolValue);
                }
            }

            EditorGUILayout.LabelField(L($"{keyBase}/Name"), GUILayout.Width(110));
            EditorGUILayout.LabelField(L($"{keyBase}/Detail"));

            if (showDetails && GUILayout.Button("Editor/Detail".LL(), GUILayout.Width(50)))
            {
                RequestDetailView();
            }

            EditorGUILayout.EndHorizontal();
        }


        protected void DrawFloatField(string propName, float? min = null, float? max = null)
        {
            SerializedProperty property = SP(propName);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L($"{propName}/Name"), GUILayout.Width(110 + 20));
            property.floatValue = EditorGUILayout.Slider(property.floatValue, min ?? 0, max ?? 1);
            EditorGUILayout.EndHorizontal();
        }

        protected void DrawIntField(string propName, int? min = null, int? max = null)
        {
            SerializedProperty property = SP(propName);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L($"{propName}/Name"), GUILayout.Width(110 + 20));
            property.intValue = EditorGUILayout.IntSlider(property.intValue, min ?? 0, max ?? 100);
            EditorGUILayout.EndHorizontal();
        }

        protected void DrawEnumField(string propName)
        {
            SerializedProperty property = SP(propName);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L($"{propName}/Name"), GUILayout.Width(110 + 20));
            property.enumValueIndex = EditorGUILayout.Popup(property.enumValueIndex, property.enumDisplayNames);
            EditorGUILayout.EndHorizontal();
        }
    }
}