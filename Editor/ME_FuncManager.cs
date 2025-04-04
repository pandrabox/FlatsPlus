using com.github.pandrabox.pandravase.editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;

namespace com.github.pandrabox.flatsplus.editor
{
    // モジュール管理用のシングルトンマネージャー
    public class ME_FuncManager
    {
        private static ME_FuncManager _instance;
        public static ME_FuncManager Instance => _instance ?? (_instance = new ME_FuncManager());

        private SerializedObject _serializedObject;
        private Dictionary<string, SerializedProperty> _serializedProperties = new Dictionary<string, SerializedProperty>();
        private ME_MainEditor _editor;
        private List<ME_FuncBase> _functionModules = new List<ME_FuncBase>();
        private bool _showDetail = false;
        private string _detailKey = "";
        private ME_FuncBase _currentDetailModule = null;

        // シングルトンなのでプライベートコンストラクタ
        private ME_FuncManager() { }

        // 初期化
        public void Initialize(SerializedObject serializedObject, ME_MainEditor editor)
        {
            _serializedObject = serializedObject;
            _editor = editor;
            _serializedProperties.Clear();
            _functionModules.Clear();

            // モジュールを初期化
            InitializeFunctionModules();
        }

        // SerializedPropertyの取得（キャッシュ使用）
        public SerializedProperty GetProperty(string name)
        {
            if (!_serializedProperties.ContainsKey(name) || _serializedProperties[name] == null)
            {
                var property = _serializedObject.FindProperty(name);
                if (property == null)
                {
                    Debug.LogError($"Property '{name}' not found in serialized object.");
                    return null;
                }
                _serializedProperties[name] = property;
            }
            return _serializedProperties[name];
        }

        // SerializedObjectへの変更を適用する
        public void ApplyModifiedProperties()
        {
            if (_serializedObject != null && _serializedObject.targetObject != null)
            {
                _serializedObject.ApplyModifiedProperties();
            }
        }

        // 言語設定用のSerializedPropertyを取得
        public SerializedProperty GetLanguageProperty()
        {
            return GetProperty("Language");
        }

        // 全モジュールのDrawMenuを実行
        public void DrawAllMenus()
        {
            foreach (var module in _functionModules)
            {
                module.DrawMenu();
            }

            // 全モジュールの描画後に変更を適用
            ApplyModifiedProperties();
        }

        // 詳細表示
        public void DrawDetailIfNeeded()
        {
            if (!_showDetail || _currentDetailModule == null) return;

            EditorGUI.BeginChangeCheck();

            try
            {
                ShowDetailTitle();
                _currentDetailModule.DrawDetail(); // 引数なしに変更

                if (GUILayout.Button("Editor/CloseDetail".LL()))
                {
                    _showDetail = false;
                    _currentDetailModule = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error drawing detail view: {ex.Message}");
                _showDetail = false;
                _currentDetailModule = null;
            }

            // 詳細表示での変更を適用
            if (EditorGUI.EndChangeCheck())
            {
                ApplyModifiedProperties();
            }
        }

        // 機能の有効/無効を一括設定
        public void SetAllFunctionsEnabled(bool enabled)
        {
            foreach (var module in _functionModules)
            {
                var property = GetProperty(module.ManagementFunc);
                if (property != null)
                {
                    property.boolValue = enabled;
                }
            }
            // 変更を適用
            ApplyModifiedProperties();
        }

        // 詳細表示を設定
        public void SetDetailModule(string key, ME_FuncBase module)
        {
            _detailKey = key;
            _currentDetailModule = module;
            _showDetail = true;
        }

        // キャッシュのクリア
        public void ClearCache()
        {
            _serializedProperties.Clear();
        }

        // モジュールの初期化
        private void InitializeFunctionModules()
        {
            // FPFuncBaseを継承するすべてのクラスを取得
            var fpFuncTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && typeof(ME_FuncBase).IsAssignableFrom(type))
                .ToList();

            foreach (var type in fpFuncTypes)
            {
                try
                {
                    // 引数なしのコンストラクタを取得してインスタンス化
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor != null)
                    {
                        var instance = (ME_FuncBase)constructor.Invoke(null);
                        _functionModules.Add(instance);
                        Debug.Log($"Added function module: {type.Name}");
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find appropriate constructor for {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error initializing module {type.Name}: {ex.Message}");
                }
            }
        }

        // 詳細表示のタイトル
        private void ShowDetailTitle()
        {
            var before = L("Editor/Detail2");
            var after = L($"Func/{_detailKey}/Name");
            var title = $@"{before} : {after}";
            _editor.ShowTitle(title);
        }

        // 全てのモジュールの初期化処理を呼び出す
        public void OnEnableAll()
        {
            foreach (var module in _functionModules)
            {
                module.OnEnable();
            }
        }

        // モジュールをすべて取得
        public IReadOnlyList<ME_FuncBase> GetAllModules() => _functionModules;
    }
}