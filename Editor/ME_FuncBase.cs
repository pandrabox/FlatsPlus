using com.github.pandrabox.pandravase.editor;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // 依存する機能の型リスト
        protected virtual List<Type> Dependencies => new List<Type>();

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
            return ME_FuncManager.I.GetProperty(name);
        }

        // 詳細表示の要求
        protected void RequestDetailView()
        {
            ME_FuncManager.I.SetDetailModule(DisplayKeyPart, this);
        }

        //DrawBoolFieldのチェック変更時に呼ばれる
        public virtual void OnChange(bool state) {}

        private void CheckDependencies(bool state)
        {
            if (!state) return;
            EnableDependencies();
        }


        // 依存するモジュールを有効化するメソッド
        protected void EnableDependencies()
        {
            // 依存関係が空の場合は何もしない
            if (Dependencies == null || Dependencies.Count == 0)
                return;

            var allModules = ME_FuncManager.I.GetAllModules();
            bool anyChanges = false;

            foreach (var dependency in Dependencies)
            {
                // 依存するタイプのモジュールを検索
                var dependentModule = allModules.FirstOrDefault(m => m.GetType() == dependency);
                if (dependentModule != null)
                {
                    // 依存モジュールのプロパティを取得
                    var property = ME_FuncManager.I.GetProperty(dependentModule.ManagementFunc);
                    if (property != null && !property.boolValue)
                    {
                        // 依存モジュールを有効化
                        property.boolValue = true;
                        anyChanges = true;
                        Debug.Log($"{ManagementFunc}の依存モジュール {dependentModule.ManagementFunc} を自動的に有効化しました");
                    }
                }
            }

            // 変更があった場合のみ適用
            if (anyChanges)
            {
                ME_FuncManager.I.ApplyModifiedProperties();
            }
        }


        private string ConvertToProjectRelativePath(string systemPath)
        {
            if (string.IsNullOrEmpty(systemPath))
                return null;

            // データパスを取得（/Assets）
            string dataPath = Application.dataPath;
            string projectRoot = dataPath.Substring(0, dataPath.Length - 6); // /Assetsを削除

            // Assetsフォルダ内のパスを変換
            if (systemPath.StartsWith(dataPath))
            {
                return "Assets" + systemPath.Substring(dataPath.Length).Replace('\\', '/');
            }

            // Packagesフォルダ内のパスを変換
            string packagesPath = System.IO.Path.Combine(projectRoot, "Packages").Replace('\\', '/');
            if (systemPath.StartsWith(packagesPath))
            {
                return "Packages" + systemPath.Substring(packagesPath.Length).Replace('\\', '/');
            }

            // プロジェクト内のパスでない場合
            return null;
        }

        private string GetFullPathFromProjectRelative(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
                return "";

            // データパスを取得（/Assets）
            string dataPath = Application.dataPath;
            string projectRoot = dataPath.Substring(0, dataPath.Length - 6); // /Assetsを削除

            if (projectPath.StartsWith("Assets/"))
            {
                return System.IO.Path.Combine(projectRoot, projectPath).Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
            else if (projectPath.StartsWith("Packages/"))
            {
                return System.IO.Path.Combine(projectRoot, projectPath).Replace('/', System.IO.Path.DirectorySeparatorChar);
            }

            // すでに絶対パスの場合はそのまま返す
            return projectPath;
        }

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
                ME_FuncManager.I.ApplyModifiedProperties();

                // 現在の機能のON/OFF状態が変わっていた場合、OnChangeを呼び出す
                if (propName == ManagementFunc && previousState != property.boolValue)
                {
                    OnChange(property.boolValue);
                    CheckDependencies(property.boolValue);
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
        protected void DrawFileField(string propName, string extension, string nullDirPath = null)
        {
            SerializedProperty property = SP(propName);
            string currentPath = property.stringValue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L($"{propName}/Name"), GUILayout.Width(110 + 20));

            // プロジェクト内のファイルを表示するObjectField（操作不可）
            EditorGUI.BeginDisabledGroup(true);
            TextAsset currentAsset = null;
            if (!string.IsNullOrEmpty(currentPath))
            {
                currentAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(currentPath);
            }
            EditorGUILayout.ObjectField(currentAsset, typeof(TextAsset), false, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();

            // Selectボタン
            bool openFileDialog = GUILayout.Button("Select", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            // ファイル選択ダイアログを遅延実行
            if (openFileDialog)
            {
                EditorApplication.delayCall += () =>
                {
                    // 初期ディレクトリを決定
                    string initialDir = "";
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        // プロジェクト相対パスをシステムパスに変換
                        string fullPath = GetFullPathFromProjectRelative(currentPath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            initialDir = System.IO.Path.GetDirectoryName(fullPath);
                        }
                    }
                    else if (nullDirPath != null)
                    {
                        initialDir = nullDirPath;
                    }

                    // ファイル選択ダイアログを表示
                    string selectedPath = EditorUtility.OpenFilePanel($"Select {extension.ToUpper()} File", initialDir, extension);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        // システムパスをプロジェクト相対パス（AssetsまたはPackages）に変換
                        string projectPath = ConvertToProjectRelativePath(selectedPath);
                        if (projectPath != null)
                        {
                            // パスを更新
                            property.stringValue = projectPath;
                            ME_FuncManager.I.ApplyModifiedProperties();
                            EditorApplication.RepaintHierarchyWindow(); // UIを更新
                        }
                        else
                        {
                            // プロジェクト外のファイルが選択された場合は警告
                            EditorUtility.DisplayDialog("Invalid Path",
                                "Selected file must be inside the Unity project (Assets or Packages folder).", "OK");
                        }
                    }
                };
            }
        }
        protected void DrawButton(string title, Action action)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L($"{title}/Name"), GUILayout.Width(110 + 20));

            if (GUILayout.Button(L($"{title}/ButtonName")))
            {
                action?.Invoke();
            }
            EditorGUILayout.EndHorizontal();
        }
        /// <summary>
        /// キーを選択すると値をpropに保存
        /// </summary>
        protected void DrawDictionarySelect(string propName, Dictionary<string, string> KeyValPair)
        {
            SerializedProperty property = SP(propName);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L($"{propName}/Name"), GUILayout.Width(110 + 20));

            // Dictionaryのキーを配列に変換
            string[] keys = new string[KeyValPair.Count];
            KeyValPair.Keys.CopyTo(keys, 0);

            // 現在保存されている値に対応するキーを探す
            string currentValue = property.stringValue;
            int selectedIndex = 0;

            // 現在の値に一致するキーを検索
            for (int i = 0; i < keys.Length; i++)
            {
                if (KeyValPair[keys[i]] == currentValue)
                {
                    selectedIndex = i;
                    break;
                }
            }

            // ポップアップで選択肢を表示し、新しいインデックスを取得
            int newIndex = EditorGUILayout.Popup(selectedIndex, keys);

            // 選択されたキーに対応する値を保存
            if (newIndex >= 0 && newIndex < keys.Length)
            {
                property.stringValue = KeyValPair[keys[newIndex]];
            }

            EditorGUILayout.EndHorizontal();
        }

        protected void DrawTextureField(string propName, int sizex, int? sizey)
        {

            sizey = sizey ?? sizex;
            SerializedProperty property = SP(propName);

            // 画像フィールドを表示
            property.objectReferenceValue = EditorGUILayout.ObjectField(
                property.objectReferenceValue,
                typeof(Texture2D),
                false,
                GUILayout.Width(sizex),
                GUILayout.Height((int)sizey)
            );
        }

    }
}