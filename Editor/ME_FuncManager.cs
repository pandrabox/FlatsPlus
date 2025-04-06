using com.github.pandrabox.flatsplus.runtime;
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
        public static ME_FuncManager I => _instance ?? (_instance = new ME_FuncManager());

        private SerializedObject _serializedObject;
        private Dictionary<string, SerializedProperty> _serializedProperties = new Dictionary<string, SerializedProperty>();
        private ME_MainEditor _editor;
        private List<ME_FuncBase> _functionModules = new List<ME_FuncBase>();
        private bool _showDetail = false;
        private string _detailKey = "";
        private ME_FuncBase _currentDetailModule = null;
        public GameObject EditorObj => ((FlatsPlus)_editor.target).gameObject;

        // 詳細表示の状態維持フラグ
        private bool _keepDetailOpen = false;
        // オブジェクトフィールド操作検出用
        private bool _objectPickerWasOpen = false;

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

            // EditorApplicationイベントを登録
            EditorApplication.update += OnEditorUpdate;
            // イベント検出のためにSceneViewのDelegateを設定
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            // オブジェクトピッカー操作時の状態維持
            Event e = Event.current;
            if (e != null && e.commandName == "ObjectSelectorClosed" && _objectPickerWasOpen)
            {
                _objectPickerWasOpen = false;
                _keepDetailOpen = true;
                EditorApplication.delayCall += () =>
                {
                    // 詳細ビューを開き続ける
                    if (_currentDetailModule != null)
                    {
                        _showDetail = true;
                    }
                    EditorUtility.SetDirty(_editor.target);
                };
            }
        }

        // エディタの更新時に呼ばれるメソッド
        private void OnEditorUpdate()
        {
            // オブジェクトピッカーが表示されている場合はフラグを設定
            int controlID = EditorGUIUtility.GetObjectPickerControlID();
            if (controlID != 0)
            {
                _objectPickerWasOpen = true;
                _keepDetailOpen = true;
            }
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
            // 詳細表示が不要な場合は早期リターン
            if (!_showDetail && !_keepDetailOpen)
            {
                _currentDetailModule = null;
                return;
            }

            // 詳細表示を維持する場合、強制的に表示フラグをON
            if (_keepDetailOpen && _currentDetailModule != null)
            {
                _showDetail = true;
                _keepDetailOpen = false; // 一度だけ使用
            }

            try
            {
                EditorGUI.BeginChangeCheck();

                // タイトルとモジュールの詳細を描画
                ShowDetailTitle();

                if (_currentDetailModule != null)
                {
                    try
                    {
                        // モジュールの詳細表示を行う
                        _currentDetailModule.DrawDetail();
                    }
                    catch (ExitGUIException)
                    {
                        // 通常のGUIの中断例外は無視（Unity EditorのGUIシステムの一部）
                        // この例外によって上位のGUIレイアウトが壊れないように、ここでキャッチする
                        _keepDetailOpen = true;
                        EditorApplication.delayCall += () =>
                        {
                            // 次のフレームで強制的にインスペクタを再描画
                            EditorUtility.SetDirty(_editor.target);
                        };
                        return; // 即座にリターンして、残りのGUI描画をスキップ
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error drawing module detail: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                // 閉じるボタン
                if (GUILayout.Button("Editor/CloseDetail".LL()))
                {
                    _showDetail = false;
                    _keepDetailOpen = false;
                    _currentDetailModule = null;
                }

                // 変更を適用
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyModifiedProperties();
                }
            }
            catch (ExitGUIException)
            {
                // 最上位レベルでも例外を捕捉し、次のフレームで状態を回復
                _keepDetailOpen = true;
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.SetDirty(_editor.target);
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error drawing detail view: {ex.Message}\n{ex.StackTrace}");
                _showDetail = false;
                _keepDetailOpen = false;
                _currentDetailModule = null;
            }
        }

        // 機能の有効/無効を一括設定
        public void SetAllFunctionsEnabled(bool enabled)
        {
            foreach (var module in _functionModules)
            {
                // まとめてON/OFF機能の対象外に設定されているモジュールはスキップ
                if (module.ExcludeFromBulkToggle)
                    continue;

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
            _keepDetailOpen = false;
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

        // アンロード時のクリーンアップ
        public void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // モジュールをすべて取得
        public IReadOnlyList<ME_FuncBase> GetAllModules() => _functionModules;
    }
}