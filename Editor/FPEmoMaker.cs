using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace com.github.pandrabox.flatsplus.editor
{
    public enum Gesture
    {
        Neutral,
        Fist,
        HandOpen,
        FingerPoint,
        Victory,
        RocknRoll,
        HandGun,
        Thumbsup
    }

    public class FPEmoMaker : EditorWindow
    {
        public LeftContent leftContent;
        private RightContent rightContent;
        private float lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.1f; // 0.1秒ごとに更新
        public const float RIGHT_PANEL_WIDTH = 400f; // 右パネルの固定幅

        [MenuItem("Pan/EmoMaker")]
        public static void ShowWindow()
        {
            var window = GetWindow<FPEmoMaker>("EmoMaker");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            leftContent = new LeftContent();
            rightContent = new RightContent();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            if (currentTime - lastUpdateTime > UPDATE_INTERVAL)
            {
                lastUpdateTime = currentTime;
                FPEMTmb.I.CheckAndProcess(); // サムネイルの更新をチェック
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // 左パネル - 残りのスペースを使用
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width - RIGHT_PANEL_WIDTH));
            leftContent.OnGUI();
            EditorGUILayout.EndVertical();

            // 右パネル - 固定幅400px
            EditorGUILayout.BeginVertical(GUILayout.Width(RIGHT_PANEL_WIDTH));
            rightContent.OnGUI();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // エディタウィンドウを常に更新
            Repaint();
        }

        private void OnDestroy()
        {
            EmoMakerCommon.I.Dispose();
        }
    }


    /// <summary>
    /// アバターの複製を作成・管理するクラス
    /// </summary>
    public class FPWorkObject : IDisposable
    {
        public GameObject WorkObject { get; private set; }
        public const string WorkObjectName = "FlatsPlusEmoMakerRoot";
        private SkinnedMeshRenderer _bodyRenderer;
        private VRCAvatarDescriptor _originalDesc;

        public FPWorkObject()
        {
        }

        public VRCAvatarDescriptor Initialize()
        {
            // 選択されているGameObjectからアバターを取得
            var triggerObject = Selection.activeGameObject;
            if (triggerObject == null)
            {
                Log.I.Error("アクティブなGameObjectが選択されていません。");
                return null;
            }

            // VRCAvatarDescriptorを検索
            _originalDesc = triggerObject.GetComponent<VRCAvatarDescriptor>();
            if (_originalDesc == null)
            {
                Transform parent = triggerObject.transform.parent;
                while (parent != null)
                {
                    _originalDesc = parent.GetComponent<VRCAvatarDescriptor>();
                    if (_originalDesc != null) break;
                    parent = parent.parent;
                }
            }

            if (_originalDesc == null)
            {
                Log.I.Error("VRCAvatarDescriptorが見つかりませんでした");
                return null;
            }

            CreateWorkObject();
            return _originalDesc;
        }

        private void CreateWorkObject()
        {
            Dispose();

            WorkObject = new GameObject(WorkObjectName);
            WorkObject.tag = "EditorOnly";
            WorkObject.transform.position = Vector3.one*-10;

            GameObject originalObj = _originalDesc.gameObject;
            GameObject duplicateObj = GameObject.Instantiate(originalObj, WorkObject.transform);
            duplicateObj.transform.parent = WorkObject.transform;
            duplicateObj.transform.localPosition = Vector3.zero;

            // BodyRendererの取得
            _bodyRenderer = WorkObject.GetComponentInChildren<SkinnedMeshRenderer>(true);
        }

        public List<FPMKShape> GetShapes()
        {
            var shapes = new List<FPMKShape>();

            if (_bodyRenderer == null || _bodyRenderer.sharedMesh == null)
            {
                Log.I.Error("Bodyメッシュが見つかりませんでした");
                return shapes;
            }

            for (int i = 0; i < _bodyRenderer.sharedMesh.blendShapeCount; i++)
            {
                string name = _bodyRenderer.sharedMesh.GetBlendShapeName(i);
                shapes.Add(new FPMKShape(name));
            }

            Log.I.EndMethod($@"{shapes.Count}件のシェイプを取得します");
            return shapes;
        }

        public SkinnedMeshRenderer BodyRenderer => _bodyRenderer;

        public static void Reset()
        {
            var wo = GameObject.Find(WorkObjectName);
            if (wo != null)
            {
                GameObject.DestroyImmediate(wo);
            }
        }

        public void Dispose()
        {
            Reset();
            WorkObject = null;
            _bodyRenderer = null;
        }

    }

    /// <summary>
    /// カメラとレンダリングを管理するクラス
    /// </summary>
    public class FPCapture : IDisposable
    {
        private Camera _camera;
        private RenderTexture _renderTexture;
        private bool _isCapturing = false;

        public FPCapture()
        {
        }

        public void Initialize(GameObject parent, int textureSize, float cameraHeight = 1f)
        {
            if (parent == null) return;

            Dispose();
            // カメラの作成
            _camera = new GameObject("Camera").AddComponent<Camera>();
            _camera.transform.parent = parent.transform;
            _camera.orthographic = true;
            _camera.orthographicSize = .3f;
            _camera.transform.localPosition = new Vector3(0, cameraHeight, 100f);
            _camera.transform.eulerAngles = new Vector3(0, 180, 0);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1);
            _camera.aspect = 1.0f;

            CreateRenderTexture(textureSize);
        }

        public void SetHeight(float height)
        {
            if (_camera != null)
            {
                _camera.transform.localPosition = new Vector3(0, height, 100f);
            }
        }

        public void SetOrthographicSize(float size)
        {
            if (_camera != null)
            {
                _camera.orthographicSize = size;
            }
        }

        public float CameraHeight => _camera.transform.localPosition.y;
        public float OrthographicSize => _camera.orthographicSize;

        private void CreateRenderTexture(int textureSize)
        {
            if (_renderTexture != null) RenderTexture.ReleaseTemporary(_renderTexture);
            _renderTexture = RenderTexture.GetTemporary(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
            _renderTexture.antiAliasing = 4;
        }

        // カメラでレンダリングを実行する
        public bool CaptureToTexture(Texture2D targetTexture, int textureSize)
        {
            if (_camera == null || _renderTexture == null || targetTexture == null) return false;
            if (_isCapturing) return false;

            _isCapturing = true;
            try
            {
                // 現在の設定を保存
                var prevRT = RenderTexture.active;

                // レンダリング
                _camera.targetTexture = _renderTexture;
                _camera.Render(); // 1回目のレンダリング（変更を確実に反映）
                _camera.Render(); // 2回目のレンダリング（最終結果）
                RenderTexture.active = _renderTexture;

                // RenderTextureからTexture2Dにコピー
                targetTexture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
                targetTexture.Apply(true);

                // 後始末
                RenderTexture.active = prevRT;
                _camera.targetTexture = null;

                return true;
            }
            finally
            {
                _isCapturing = false;
            }
        }

        public void Dispose()
        {
            if (_camera != null)
            {
                if (_camera.gameObject != null)
                {
                    GameObject.DestroyImmediate(_camera.gameObject);
                }
                _camera = null;
            }

            if (_renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                _renderTexture = null;
            }
        }

        public bool IsCapturing => _isCapturing;
    }

    public class EmoMakerCommon : IDisposable
    {
        private static EmoMakerCommon _instance;
        public static EmoMakerCommon I => _instance ?? (_instance = new EmoMakerCommon());

        public VRCAvatarDescriptor OriginalDesc;
        public List<FPMKShape> MShapes;
        public FPWorkObject WorkObj { get; private set; }
        public FPCapture Capture { get; private set; }
        public FPMKEmo[] Emos = new FPMKEmo[64];
        public int ActiveEmoIndex = 0; // アクティブな表情のインデックス

        public int TextureSize = 300;//180;

        private EmoMakerCommon()
        {
            WorkObj = new FPWorkObject();
            Capture = new FPCapture();
        }

        public void Initialize()
        {
            Reset();
            OriginalDesc = WorkObj.Initialize();
            if (OriginalDesc == null) return;
            //Vector3 viewWorldPosition = OriginalDesc.transform.TransformPoint(OriginalDesc.ViewPosition);
            Capture.Initialize(WorkObj.WorkObject, TextureSize, OriginalDesc.ViewPosition.y);
            MShapes = WorkObj.GetShapes();
            InitializeEmos();

        }

        // リセット機能の追加
        public void Reset()
        {
            // 画像の削除
            FPEMTmb.I.Clear();

            // ゲームオブジェクトの削除
            FPWorkObject.Reset();
            WorkObj?.Dispose();

            // カメラのリセット
            Capture?.Dispose();

            // 表情データのリセット
            for (int i = 0; i < Emos.Length; i++)
            {
                Emos[i] = null;
            }

            // 基本データのリセット
            OriginalDesc = null;
            MShapes = null;
            ActiveEmoIndex = 0;
        }

        private void InitializeEmos()
        {
            for (int i = 0; i < Emos.Length; i++)
            {
                Emos[i] = new FPMKEmo(MShapes, TextureSize);
            }
            //Emos[0].ReserveTmb();//対症療法で表情1を直す場合
        }


        // 現在の選択を変更して適用
        public void ChangeActiveEmo(int newIndex)
        {
            if (newIndex == ActiveEmoIndex) return;
            ActiveEmoIndex = newIndex;
        }

        public FPMKEmo ActiveEmo => Emos[ActiveEmoIndex];

        public void Dispose()
        {
            Capture?.Dispose();
            Capture = null;

            WorkObj?.Dispose();
            WorkObj = null;
        }
    }

    public class FPMKShape
    {
        public string FullName, ShortName;
        public bool Hide;
        public int Val;

        public FPMKShape(string fullName)
        {
            FullName = fullName;
            ShortName = FullName.Replace("blendShape.", "");
            var aiueo = new[] { "あ", "い", "う", "え", "お" };
            Hide = ShortName.StartsWith("vrc.v_") || aiueo.Contains(ShortName);
            Val = 0;
        }
    }

    public class FPMKEmo
    {
        public List<FPMKShape> Shapes = new List<FPMKShape>();
        public Texture2D Texture;
        public bool NeedUpdate;

        public FPMKEmo(List<FPMKShape> shapes, int textureSize)
        {
            if (shapes != null)
            {
                foreach (var shape in shapes)
                {
                    Shapes.Add(new FPMKShape(shape.FullName)
                    {
                        Hide = shape.Hide,
                        Val = 0
                    });
                }
            }
            ReserveTmb();
        }

        public void ReserveTmb()
        {
            NeedUpdate = true;
            FPEMTmb.I.Reserve(this);
        }

        public string Hash => string.Join(",", Shapes.Select(s => $"{s.FullName}:{s.Val}").ToArray());
    }


    /// <summary>
    /// 画像サムネの作成と配布
    /// </summary>
    public class FPEMTmb
    {
        private static FPEMTmb _instance = new FPEMTmb();
        public static FPEMTmb I => _instance ?? (_instance = new FPEMTmb());
        private FPEMTmb() { }
        public Dictionary<string, Texture2D> Tmb = new Dictionary<string, Texture2D>();
        private FPMKEmo _currentEmo;
        public bool Running = false;
        private bool _need => _currentEmo.NeedUpdate;
        private bool _exected => Tmb.ContainsKey(_currentEmo.Hash);
        private string _hash; //キャプチャ中に値が変わるとまずいのでキャプチャ中は保持しておく

        //削除
        public void Clear()
        {
            Tmb.Clear();
        }

        //サムネ予約の受付
        public void Reserve(FPMKEmo emo)
        {
            _currentEmo = emo;
            RunCurrent();
        }

        //カレントの実行
        private void RunCurrent()
        {
            //いらないならやめる
            if (!_need) return;
            //あるなら返す
            if (_exected)
            {
                //Log.I.Info($@"作成済みのサムネイルを返します{_currentEmo.Hash}");
                _currentEmo.Texture = Tmb[_currentEmo.Hash];
                _currentEmo.NeedUpdate = false;
                return;
            }
            //実行中ならやめる
            if (Running) return;

            //作る
            Running = true;
            _hash = _currentEmo.Hash;
            SetShape();
            EditorApplication.delayCall += () => {
                EditorApplication.delayCall += () => Capture();
            };
        }

        //フラグのあるものを探して画像生成
        public void CheckAndProcess()
        {
            if (EmoMakerCommon.I.Emos == null) return;
            if (Running) return;

            var tgt = EmoMakerCommon.I.Emos.FirstOrDefault(emo => emo != null && emo.NeedUpdate);
            if (tgt != null) Reserve(tgt);
            tgt = EmoMakerCommon.I.Emos.FirstOrDefault(emo => emo != null && emo.Texture == null);
            if (tgt != null) Reserve(tgt);
        }

        //シェイプをセット
        private void SetShape()
        {
            var shapes = _currentEmo.Shapes;
            if (shapes == null || shapes.Count == 0)
            {
                Log.I.Error("シェイプがありません。無限ループするかもしれない");
                return;
            }
            var bodyRenderer = EmoMakerCommon.I.WorkObj.BodyRenderer;
            if (bodyRenderer == null) return;

            foreach (var shape in shapes)
            {
                int shapeIndex = bodyRenderer.sharedMesh.GetBlendShapeIndex(shape.FullName);
                if (shapeIndex != -1)
                {
                    bodyRenderer.SetBlendShapeWeight(shapeIndex, shape.Val);
                }
            }
        }
        private void Capture()
        {
            try
            {
                var capture = EmoMakerCommon.I.Capture;
                var textureSize = EmoMakerCommon.I.TextureSize;
                Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
                EmoMakerCommon.I.Capture.CaptureToTexture(tex, textureSize);
                Tmb[_hash] = tex;
                _currentEmo.Texture = tex;
                //Log.I.Info($@"サムネイルを作成しました: {_hash}");
                EditorWindow.GetWindow<FPEmoMaker>().Repaint();
                if (_hash != _currentEmo.Hash)
                {
                    //Log.I.Error("Hashが変わったので、再作成をコールします");
                    _currentEmo.ReserveTmb();
                }
            }
            catch (Exception ex)
            {
                Log.I.Error($"キャプチャ中にエラーが発生しました: {ex.Message}");
            }
            finally
            {

                Running = false; // 必ず実行フラグをリセット
            }
        }
    }

    public class LeftContent
    {
        private Vector2 _thumbnailScroll = Vector2.zero;
        private const int GRID_SIZE = 8; // 8x8グリッド

        // 選択状態の管理
        private bool[] _selectedRows = new bool[GRID_SIZE];
        private bool[] _selectedColumns = new bool[GRID_SIZE];
        private bool[,] _selectedCells = new bool[GRID_SIZE, GRID_SIZE];
        private bool _allSelected = false; // 全選択状態の管理

        // コピー・貼り付け機能用
        private enum CopyMode { None, Cell, Row, Column }
        private CopyMode _currentCopyMode = CopyMode.None;
        private int _copiedEmoIndex = -1; // セルコピー用: -1 はコピーしていない状態
        private int _copiedRowIndex = -1; // 行コピー用: -1 はコピーしていない状態
        private int _copiedColIndex = -1; // 列コピー用: -1 はコピーしていない状態
        private Material _copyHighlightMaterial = null; // コピー元表示用マテリアル

        // 入れ替え機能用
        private enum SwapMode { None, Cell, Row, Column }
        private SwapMode _currentSwapMode = SwapMode.None;
        private int _swapSourceIndex = -1; // 入れ替え元のインデックス（セル/行/列のいずれか）
        private Material _swapHighlightMaterial = null; // 入れ替え元表示用マテリアル

        // 暗い表示用のマテリアル
        private Material _dimMaterial = null;

        // 初期化
        public LeftContent()
        {
            // 初期選択：左上のセル
            _selectedCells[0, 0] = true;

            // 暗い表示用のシェーダーを作成
            Shader dimShader = Shader.Find("Hidden/Internal-Colored");
            if (dimShader != null)
            {
                _dimMaterial = new Material(dimShader);
                _dimMaterial.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 1.0f));

                // コピー元表示用のマテリアル（青っぽい色に、濃度を上げる）
                _copyHighlightMaterial = new Material(dimShader);
                _copyHighlightMaterial.SetColor("_Color", new Color(0.3f, 0.5f, 0.8f, 0.7f));

                // 入れ替え元表示用のマテリアル（赤っぽい色に、濃度を上げる）
                _swapHighlightMaterial = new Material(dimShader);
                _swapHighlightMaterial.SetColor("_Color", new Color(0.8f, 0.3f, 0.3f, 0.7f));
            }
        }

        public void OnGUI()
        {
            // スクロールビューの前にコピー・貼り付けボタンとして表示
            DrawControlButtons();

            // スクロールビューを開始
            _thumbnailScroll = EditorGUILayout.BeginScrollView(_thumbnailScroll);

            // グリッド全体の余白を設定
            float cellSize = (EditorWindow.GetWindow<FPEmoMaker>().position.width - FPEmoMaker.RIGHT_PANEL_WIDTH - 40) / (GRID_SIZE + 1);

            // テーブルレイアウト
            GUILayout.BeginVertical();

            // 列選択ボタンの行
            GUILayout.BeginHorizontal();

            // 左上の全選択ボタン
            GUIStyle allSelectStyle = new GUIStyle(GUI.skin.button);
            if (_allSelected)
            {
                allSelectStyle.normal.textColor = Color.yellow;
                allSelectStyle.fontStyle = FontStyle.Bold;
            }

            if (GUILayout.Button("全選択", allSelectStyle, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
            {
                ToggleAllSelection();
                Event.current.Use();
            }

            // 列選択ボタン
            for (int col = 0; col < GRID_SIZE; col++)
            {
                string colLabel = $"Right\n{((Gesture)col)}";

                // スタイルを選択状態に応じて変更
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                if (_selectedColumns[col])
                {
                    buttonStyle.normal.textColor = Color.yellow;
                    buttonStyle.fontStyle = FontStyle.Bold;
                }

                // 入れ替えモードで列が入れ替え元として選択されている場合
                if (_currentSwapMode == SwapMode.Column && _swapSourceIndex == col)
                {
                    // 背景色を赤く設定
                    Rect buttonRect = GUILayoutUtility.GetRect(cellSize, cellSize);
                    EditorGUI.DrawRect(buttonRect, new Color(0.8f, 0.4f, 0.4f, 1.0f));

                    // ボタンを表示
                    if (GUI.Button(buttonRect, colLabel, buttonStyle))
                    {
                        // ボタンの処理
                        if (!Event.current.control)
                        {
                            ClearSelection();
                        }
                        _selectedColumns[col] = !_selectedColumns[col];
                        for (int row = 0; row < GRID_SIZE; row++)
                        {
                            _selectedCells[row, col] = _selectedColumns[col];
                        }
                        UpdateAllSelectedState();
                        UpdateSelectedEmos();
                        Event.current.Use();
                    }
                }
                else
                {
                    // 通常ボタン表示
                    if (GUILayout.Button(colLabel, buttonStyle, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        // Ctrlキーが押されていなければ選択をクリア
                        if (!Event.current.control)
                        {
                            ClearSelection();
                        }

                        // 列選択の切り替え
                        _selectedColumns[col] = !_selectedColumns[col];

                        // 列内の全セルの選択状態を設定
                        for (int row = 0; row < GRID_SIZE; row++)
                        {
                            _selectedCells[row, col] = _selectedColumns[col];
                        }

                        // 全選択状態を更新
                        UpdateAllSelectedState();

                        // 選択が変更されたことをEmoMakerに通知
                        UpdateSelectedEmos();

                        // イベントを処理済みとしてマーク
                        Event.current.Use();
                    }
                }
            }
            GUILayout.EndHorizontal();

            // 各行の表示
            for (int row = 0; row < GRID_SIZE; row++)
            {
                GUILayout.BeginHorizontal();

                // 行選択ボタン
                string rowLabel = $"Left\n{((Gesture)row)}";

                // スタイルを選択状態に応じて変更
                GUIStyle rowButtonStyle = new GUIStyle(GUI.skin.button);
                if (_selectedRows[row])
                {
                    rowButtonStyle.normal.textColor = Color.yellow;
                    rowButtonStyle.fontStyle = FontStyle.Bold;
                }

                // 入れ替えモードで行が入れ替え元として選択されている場合
                if (_currentSwapMode == SwapMode.Row && _swapSourceIndex == row)
                {
                    // 背景色を赤く設定
                    Rect buttonRect = GUILayoutUtility.GetRect(cellSize, cellSize);
                    EditorGUI.DrawRect(buttonRect, new Color(0.8f, 0.4f, 0.4f, 1.0f));

                    // ボタンを表示
                    if (GUI.Button(buttonRect, rowLabel, rowButtonStyle))
                    {
                        // ボタンの処理
                        if (!Event.current.control)
                        {
                            ClearSelection();
                        }
                        _selectedRows[row] = !_selectedRows[row];
                        for (int col = 0; col < GRID_SIZE; col++)
                        {
                            _selectedCells[row, col] = _selectedRows[row];
                        }
                        UpdateAllSelectedState();
                        UpdateSelectedEmos();
                        Event.current.Use();
                    }
                }
                else
                {
                    // 通常ボタン表示
                    if (GUILayout.Button(rowLabel, rowButtonStyle, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        // Ctrlキーが押されていなければ選択をクリア
                        if (!Event.current.control)
                        {
                            ClearSelection();
                        }

                        // 行選択の切り替え
                        _selectedRows[row] = !_selectedRows[row];

                        // 行内の全セルの選択状態を設定
                        for (int col = 0; col < GRID_SIZE; col++)
                        {
                            _selectedCells[row, col] = _selectedRows[row];
                        }

                        // 全選択状態を更新
                        UpdateAllSelectedState();

                        // 選択が変更されたことをEmoMakerに通知
                        UpdateSelectedEmos();

                        // イベントを処理済みとしてマーク
                        Event.current.Use();
                    }
                }

                // セルの表示
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    int emoIndex = row * GRID_SIZE + col;
                    var emo = emoIndex < EmoMakerCommon.I.Emos.Length ? EmoMakerCommon.I.Emos[emoIndex] : null;

                    // セルの矩形を定義
                    Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize);

                    // セルの背景を描画
                    EditorGUI.DrawRect(cellRect, new Color(0.2f, 0.2f, 0.2f, 1.0f));

                    // コピー元のセルかどうか確認（複数のコピーモードに対応）
                    bool isCopiedElement = false;
                    bool shouldDisplayCLabel = false;

                    // セル単位のコピー
                    if (_currentCopyMode == CopyMode.Cell && emoIndex == _copiedEmoIndex)
                    {
                        isCopiedElement = true;
                        shouldDisplayCLabel = true;
                    }
                    // 行単位のコピー
                    else if (_currentCopyMode == CopyMode.Row && row == _copiedRowIndex)
                    {
                        isCopiedElement = true;
                        shouldDisplayCLabel = (col == 0); // 行の先頭のみにラベル表示
                    }
                    // 列単位のコピー
                    else if (_currentCopyMode == CopyMode.Column && col == _copiedColIndex)
                    {
                        isCopiedElement = true;
                        shouldDisplayCLabel = (row == 0); // 列の先頭のみにラベル表示
                    }

                    // 入れ替え元のセルかどうか確認
                    bool isSwapSourceCell = (_currentSwapMode == SwapMode.Cell && _swapSourceIndex == emoIndex);

                    // 行と列の入れ替えモードの場合も考慮
                    bool isInSwapSourceRow = (_currentSwapMode == SwapMode.Row && row == _swapSourceIndex);
                    bool isInSwapSourceCol = (_currentSwapMode == SwapMode.Column && col == _swapSourceIndex);

                    // 表情テクスチャを表示
                    if (emo != null && emo.Texture != null)
                    {
                        // 表示状態を決定（コピー元・入れ替え元は常に明るく、選択されたセルも明るい）
                        bool displayBright = _selectedCells[row, col] || isCopiedElement || isSwapSourceCell || isInSwapSourceRow || isInSwapSourceCol;

                        // 明るさを設定
                        Color oldColor = GUI.color;
                        if (displayBright)
                        {
                            GUI.color = new Color(1.0f, 1.0f, 1.0f, 1.0f); // 完全な明るさ
                        }
                        else
                        {
                            float intensity = 0.2f;
                            GUI.color = new Color(intensity, intensity, intensity, 1.0f);
                        }

                        // テクスチャを描画
                        GUI.DrawTexture(cellRect, emo.Texture, ScaleMode.ScaleToFit);

                        // 特殊状態のオーバーレイ
                        if (isCopiedElement && _copyHighlightMaterial != null)
                        {
                            Graphics.DrawTexture(cellRect, Texture2D.whiteTexture, _copyHighlightMaterial);
                            if (shouldDisplayCLabel)
                            {
                                GUI.Label(new Rect(cellRect.x + 5, cellRect.y + 5, 20, 20), "C",
                                          new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.cyan } });
                            }
                        }
                        else if ((isSwapSourceCell || isInSwapSourceRow || isInSwapSourceCol) && _swapHighlightMaterial != null)
                        {
                            Graphics.DrawTexture(cellRect, Texture2D.whiteTexture, _swapHighlightMaterial);

                            // セル入替モードの場合、または行/列入替モードでセルにSマークを表示
                            if (isSwapSourceCell ||
                                (_currentSwapMode == SwapMode.Row && isInSwapSourceRow && col == 0) ||
                                (_currentSwapMode == SwapMode.Column && isInSwapSourceCol && row == 0))
                            {
                                GUI.Label(new Rect(cellRect.x + 5, cellRect.y + 5, 20, 20), "S",
                                          new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.red } });
                            }
                        }

                        GUI.color = oldColor;
                    }
                    else
                    {
                        // テクスチャがない場合も同様の処理
                        bool displayBright = _selectedCells[row, col] || isCopiedElement || isSwapSourceCell || isInSwapSourceRow || isInSwapSourceCol;

                        if (displayBright)
                        {
                            GUIStyle brightStyle = new GUIStyle(GUI.skin.box);
                            brightStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                            GUI.Box(cellRect, $"{row},{col}", brightStyle);
                        }
                        else
                        {
                            GUIStyle dimStyle = new GUIStyle(GUI.skin.box);
                            dimStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                            GUI.Box(cellRect, $"{row},{col}", dimStyle);
                        }

                        if (isCopiedElement && _copyHighlightMaterial != null)
                        {
                            Graphics.DrawTexture(cellRect, Texture2D.whiteTexture, _copyHighlightMaterial);
                            if (shouldDisplayCLabel)
                            {
                                GUI.Label(new Rect(cellRect.x + 5, cellRect.y + 5, 20, 20), "C",
                                          new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.cyan } });
                            }
                        }
                        else if ((isSwapSourceCell || isInSwapSourceRow || isInSwapSourceCol) && _swapHighlightMaterial != null)
                        {
                            Graphics.DrawTexture(cellRect, Texture2D.whiteTexture, _swapHighlightMaterial);

                            // セル入替モードの場合、または行/列入替モードでセルにSマークを表示
                            if (isSwapSourceCell ||
                                (_currentSwapMode == SwapMode.Row && isInSwapSourceRow && col == 0) ||
                                (_currentSwapMode == SwapMode.Column && isInSwapSourceCol && row == 0))
                            {
                                GUI.Label(new Rect(cellRect.x + 5, cellRect.y + 5, 20, 20), "S",
                                          new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.red } });
                            }
                        }
                    }

                    // クリック時の処理
                    if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.button == 0) // 左クリック
                        {
                            // Ctrlキーが押されていない場合は行と列も含めて選択をクリア
                            if (!Event.current.control)
                            {
                                ClearSelection();
                            }

                            // セルの選択状態を切り替え
                            _selectedCells[row, col] = !_selectedCells[row, col];

                            // 行と列の選択状態を更新
                            UpdateRowColumnSelection();

                            // 全選択状態を更新
                            UpdateAllSelectedState();

                            // emoIndexが有効な範囲内かつ対応するEmoが存在する場合
                            if (emoIndex < EmoMakerCommon.I.Emos.Length && EmoMakerCommon.I.Emos[emoIndex] != null)
                            {
                                // アクティブなインデックスを更新
                                EmoMakerCommon.I.ChangeActiveEmo(emoIndex);
                            }

                            Event.current.Use();
                        }
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        // コントロールボタンを描画（コピー・貼り付け・入れ替え機能を統合）
        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // 選択されたセル情報を取得
            List<int> selectedIndices = GetSelectedIndices();
            int selectedRowCount = _selectedRows.Count(r => r);
            int selectedColCount = _selectedColumns.Count(c => c);

            // コピーモード判定
            bool inCopyMode = _currentCopyMode != CopyMode.None;

            // コピーの可否判定
            bool canStartCopy = _currentSwapMode == SwapMode.None && !inCopyMode;
            bool canStartCellCopy = canStartCopy && selectedIndices.Count == 1;
            bool canStartRowCopy = canStartCopy && selectedRowCount == 1 && selectedColCount == 0;
            bool canStartColCopy = canStartCopy && selectedColCount == 1 && selectedRowCount == 0;

            // コピー開始/解除ボタン
            GUI.enabled = (canStartCellCopy || canStartRowCopy || canStartColCopy) || inCopyMode;
            string copyButtonText = inCopyMode ? "コピー解除" : "コピー";
            if (GUILayout.Button(copyButtonText, GUILayout.Height(24)))
            {
                if (inCopyMode)
                {
                    // コピー解除
                    ClearCopyMode();
                }
                else
                {
                    // コピーモード設定
                    if (canStartCellCopy)
                    {
                        _currentCopyMode = CopyMode.Cell;
                        _copiedEmoIndex = selectedIndices[0];
                    }
                    else if (canStartRowCopy)
                    {
                        _currentCopyMode = CopyMode.Row;
                        for (int row = 0; row < GRID_SIZE; row++)
                        {
                            if (_selectedRows[row])
                            {
                                _copiedRowIndex = row;
                                break;
                            }
                        }
                    }
                    else if (canStartColCopy)
                    {
                        _currentCopyMode = CopyMode.Column;
                        for (int col = 0; col < GRID_SIZE; col++)
                        {
                            if (_selectedColumns[col])
                            {
                                _copiedColIndex = col;
                                break;
                            }
                        }
                    }
                }
            }

            // 入れ替えボタンの状態設定
            bool inSwapMode = _currentSwapMode != SwapMode.None;
            bool canStartSwap = !inCopyMode && !inSwapMode;
            bool canStartCellSwap = canStartSwap && selectedIndices.Count == 1;
            bool canStartRowSwap = canStartSwap && selectedRowCount == 1 && selectedColCount == 0;
            bool canStartColSwap = canStartSwap && selectedColCount == 1 && selectedRowCount == 0;

            // 入れ替え開始/解除ボタン
            GUI.enabled = (canStartSwap && (canStartCellSwap || canStartRowSwap || canStartColSwap)) || inSwapMode;
            string swapButtonText = inSwapMode ? "入替解除" : "入替開始";
            if (GUILayout.Button(swapButtonText, GUILayout.Height(24)))
            {
                if (inSwapMode)
                {
                    // 入れ替えモード解除
                    _currentSwapMode = SwapMode.None;
                    _swapSourceIndex = -1;
                }
                else
                {
                    // 入替モード判定と開始
                    if (canStartCellSwap)
                    {
                        // セル入れ替えモード開始
                        _currentSwapMode = SwapMode.Cell;
                        _swapSourceIndex = selectedIndices[0];
                    }
                    else if (canStartRowSwap)
                    {
                        // 行入れ替えモード開始
                        _currentSwapMode = SwapMode.Row;
                        for (int row = 0; row < GRID_SIZE; row++)
                        {
                            if (_selectedRows[row])
                            {
                                _swapSourceIndex = row;
                                break;
                            }
                        }
                    }
                    else if (canStartColSwap)
                    {
                        // 列入れ替えモード開始
                        _currentSwapMode = SwapMode.Column;
                        for (int col = 0; col < GRID_SIZE; col++)
                        {
                            if (_selectedColumns[col])
                            {
                                _swapSourceIndex = col;
                                break;
                            }
                        }
                    }
                }
            }

            // 実行ボタン（貼り付けと入替実行を統合）
            bool canExecuteAction = false;
            string actionButtonText = "確定";

            // コピー貼り付け実行判定
            bool canPaste = !inSwapMode && inCopyMode &&
                (selectedIndices.Count > 0 || selectedRowCount > 0 || selectedColCount > 0);

            if (canPaste)
            {
                canExecuteAction = true;
                actionButtonText = "確定";
            }

            // 入れ替え実行判定
            bool canExecuteSwap = false;
            switch (_currentSwapMode)
            {
                case SwapMode.Cell when selectedIndices.Count == 1 && _swapSourceIndex >= 0 && _swapSourceIndex != selectedIndices[0]:
                    canExecuteSwap = true;
                    break;

                case SwapMode.Row when selectedRowCount == 1:
                    int targetRow = -1;
                    for (int row = 0; row < GRID_SIZE; row++)
                    {
                        if (_selectedRows[row])
                        {
                            targetRow = row;
                            break;
                        }
                    }
                    canExecuteSwap = (targetRow >= 0 && targetRow != _swapSourceIndex);
                    break;

                case SwapMode.Column when selectedColCount == 1:
                    int targetCol = -1;
                    for (int col = 0; col < GRID_SIZE; col++)
                    {
                        if (_selectedColumns[col])
                        {
                            targetCol = col;
                            break;
                        }
                    }
                    canExecuteSwap = (targetCol >= 0 && targetCol != _swapSourceIndex);
                    break;
            }

            if (canExecuteSwap)
            {
                canExecuteAction = true;
                actionButtonText = "確定";
            }

            GUI.enabled = canExecuteAction;
            if (GUILayout.Button(actionButtonText, GUILayout.Height(24)))
            {
                if (canPaste)
                {
                    // 貼り付け実行
                    ExecutePaste();
                }
                else if (canExecuteSwap)
                {
                    // 入れ替え実行
                    ExecuteSwap();
                }
            }
            GUI.enabled = true; // ボタン制御をリセット

            EditorGUILayout.EndHorizontal();
        }

        // コピーモードをクリア
        private void ClearCopyMode()
        {
            _currentCopyMode = CopyMode.None;
            _copiedEmoIndex = -1;
            _copiedRowIndex = -1;
            _copiedColIndex = -1;
        }

        // 貼り付け処理を実行
        private void ExecutePaste()
        {
            // 選択されたインデックス、行、列を取得
            List<int> selectedIndices = GetSelectedIndices();
            List<int> selectedRows = new List<int>();
            List<int> selectedColumns = new List<int>();

            for (int i = 0; i < GRID_SIZE; i++)
            {
                if (_selectedRows[i]) selectedRows.Add(i);
                if (_selectedColumns[i]) selectedColumns.Add(i);
            }

            switch (_currentCopyMode)
            {
                case CopyMode.Cell:
                    // セル単位のコピーの場合
                    if (_copiedEmoIndex >= 0 && selectedIndices.Count > 0)
                    {
                        PasteCellToTargets(_copiedEmoIndex, selectedIndices);
                    }
                    break;

                case CopyMode.Row:
                    // 行単位のコピーの場合
                    if (_copiedRowIndex >= 0)
                    {
                        if (selectedRows.Count > 0)
                        {
                            // 行単位で貼り付け
                            foreach (int targetRow in selectedRows)
                            {
                                if (targetRow != _copiedRowIndex) // 自分自身にはコピーしない
                                {
                                    PasteRowToRow(_copiedRowIndex, targetRow);
                                }
                            }
                        }
                        else if (selectedIndices.Count > 0)
                        {
                            // 選択されたセルに行の内容を貼り付け
                            PasteRowToCells(_copiedRowIndex, selectedIndices);
                        }
                    }
                    break;

                case CopyMode.Column:
                    // 列単位のコピーの場合
                    if (_copiedColIndex >= 0)
                    {
                        if (selectedColumns.Count > 0)
                        {
                            // 列単位で貼り付け
                            foreach (int targetCol in selectedColumns)
                            {
                                if (targetCol != _copiedColIndex) // 自分自身にはコピーしない
                                {
                                    PasteColumnToColumn(_copiedColIndex, targetCol);
                                }
                            }
                        }
                        else if (selectedIndices.Count > 0)
                        {
                            // 選択されたセルに列の内容を貼り付け
                            PasteColumnToCells(_copiedColIndex, selectedIndices);
                        }
                    }
                    break;
            }

            // コピーモードをクリア
            ClearCopyMode();
        }

        // セルの内容を複数のセルに貼り付ける
        private void PasteCellToTargets(int sourceIndex, List<int> targetIndices)
        {
            if (sourceIndex < 0 || sourceIndex >= EmoMakerCommon.I.Emos.Length)
                return;

            var sourceEmo = EmoMakerCommon.I.Emos[sourceIndex];
            if (sourceEmo == null || sourceEmo.Shapes == null)
                return;

            // コピー元のシェイプ値をすべての選択セルに貼り付け
            foreach (int targetIndex in targetIndices)
            {
                if (targetIndex >= 0 && targetIndex < EmoMakerCommon.I.Emos.Length &&
                    targetIndex != sourceIndex) // 自分自身には貼り付けない
                {
                    var targetEmo = EmoMakerCommon.I.Emos[targetIndex];
                    if (targetEmo != null && targetEmo.Shapes != null)
                    {
                        // 各シェイプ値をコピー
                        foreach (var sourceShape in sourceEmo.Shapes)
                        {
                            var targetShape = targetEmo.Shapes.FirstOrDefault(s => s.FullName == sourceShape.FullName);
                            if (targetShape != null)
                            {
                                targetShape.Val = sourceShape.Val;
                            }
                        }

                        // サムネイル更新を予約
                        targetEmo.ReserveTmb();
                    }
                }
            }
        }

        // 行の内容を別の行に貼り付ける
        private void PasteRowToRow(int sourceRow, int targetRow)
        {
            if (sourceRow < 0 || sourceRow >= GRID_SIZE || targetRow < 0 || targetRow >= GRID_SIZE || sourceRow == targetRow)
                return;

            for (int col = 0; col < GRID_SIZE; col++)
            {
                int sourceIndex = sourceRow * GRID_SIZE + col;
                int targetIndex = targetRow * GRID_SIZE + col;

                if (sourceIndex >= 0 && sourceIndex < EmoMakerCommon.I.Emos.Length &&
                    targetIndex >= 0 && targetIndex < EmoMakerCommon.I.Emos.Length)
                {
                    var sourceEmo = EmoMakerCommon.I.Emos[sourceIndex];
                    var targetEmo = EmoMakerCommon.I.Emos[targetIndex];

                    if (sourceEmo != null && targetEmo != null && sourceEmo.Shapes != null && targetEmo.Shapes != null)
                    {
                        // 各シェイプ値をコピー
                        foreach (var sourceShape in sourceEmo.Shapes)
                        {
                            var targetShape = targetEmo.Shapes.FirstOrDefault(s => s.FullName == sourceShape.FullName);
                            if (targetShape != null)
                            {
                                targetShape.Val = sourceShape.Val;
                            }
                        }

                        // サムネイル更新を予約
                        targetEmo.ReserveTmb();
                    }
                }
            }
        }

        // 行の内容を選択されたセルに貼り付ける
        private void PasteRowToCells(int sourceRow, List<int> targetIndices)
        {
            if (sourceRow < 0 || sourceRow >= GRID_SIZE)
                return;

            foreach (int targetIndex in targetIndices)
            {
                int targetRow = targetIndex / GRID_SIZE;
                int targetCol = targetIndex % GRID_SIZE;

                // 同じ行の場合はスキップ
                if (targetRow == sourceRow)
                    continue;

                int sourceIndex = sourceRow * GRID_SIZE + targetCol; // 同じ列のセルをソースとして使用

                if (sourceIndex >= 0 && sourceIndex < EmoMakerCommon.I.Emos.Length &&
                    targetIndex >= 0 && targetIndex < EmoMakerCommon.I.Emos.Length)
                {
                    var sourceEmo = EmoMakerCommon.I.Emos[sourceIndex];
                    var targetEmo = EmoMakerCommon.I.Emos[targetIndex];

                    if (sourceEmo != null && targetEmo != null && sourceEmo.Shapes != null && targetEmo.Shapes != null)
                    {
                        // 各シェイプ値をコピー
                        foreach (var sourceShape in sourceEmo.Shapes)
                        {
                            var targetShape = targetEmo.Shapes.FirstOrDefault(s => s.FullName == sourceShape.FullName);
                            if (targetShape != null)
                            {
                                targetShape.Val = sourceShape.Val;
                            }
                        }

                        // サムネイル更新を予約
                        targetEmo.ReserveTmb();
                    }
                }
            }
        }

        // 列の内容を別の列に貼り付ける
        private void PasteColumnToColumn(int sourceCol, int targetCol)
        {
            if (sourceCol < 0 || sourceCol >= GRID_SIZE || targetCol < 0 || targetCol >= GRID_SIZE || sourceCol == targetCol)
                return;

            for (int row = 0; row < GRID_SIZE; row++)
            {
                int sourceIndex = row * GRID_SIZE + sourceCol;
                int targetIndex = row * GRID_SIZE + targetCol;

                if (sourceIndex >= 0 && sourceIndex < EmoMakerCommon.I.Emos.Length &&
                    targetIndex >= 0 && targetIndex < EmoMakerCommon.I.Emos.Length)
                {
                    var sourceEmo = EmoMakerCommon.I.Emos[sourceIndex];
                    var targetEmo = EmoMakerCommon.I.Emos[targetIndex];

                    if (sourceEmo != null && targetEmo != null && sourceEmo.Shapes != null && targetEmo.Shapes != null)
                    {
                        // 各シェイプ値をコピー
                        foreach (var sourceShape in sourceEmo.Shapes)
                        {
                            var targetShape = targetEmo.Shapes.FirstOrDefault(s => s.FullName == sourceShape.FullName);
                            if (targetShape != null)
                            {
                                targetShape.Val = sourceShape.Val;
                            }
                        }

                        // サムネイル更新を予約
                        targetEmo.ReserveTmb();
                    }
                }
            }
        }

        // 列の内容を選択されたセルに貼り付ける
        private void PasteColumnToCells(int sourceCol, List<int> targetIndices)
        {
            if (sourceCol < 0 || sourceCol >= GRID_SIZE)
                return;

            foreach (int targetIndex in targetIndices)
            {
                int targetRow = targetIndex / GRID_SIZE;
                int targetCol = targetIndex % GRID_SIZE;

                // 同じ列の場合はスキップ
                if (targetCol == sourceCol)
                    continue;

                int sourceIndex = targetRow * GRID_SIZE + sourceCol; // 同じ行のセルをソースとして使用

                if (sourceIndex >= 0 && sourceIndex < EmoMakerCommon.I.Emos.Length &&
                    targetIndex >= 0 && targetIndex < EmoMakerCommon.I.Emos.Length)
                {
                    var sourceEmo = EmoMakerCommon.I.Emos[sourceIndex];
                    var targetEmo = EmoMakerCommon.I.Emos[targetIndex];

                    if (sourceEmo != null && targetEmo != null && sourceEmo.Shapes != null && targetEmo.Shapes != null)
                    {
                        // 各シェイプ値をコピー
                        foreach (var sourceShape in sourceEmo.Shapes)
                        {
                            var targetShape = targetEmo.Shapes.FirstOrDefault(s => s.FullName == sourceShape.FullName);
                            if (targetShape != null)
                            {
                                targetShape.Val = sourceShape.Val;
                            }
                        }

                        // サムネイル更新を予約
                        targetEmo.ReserveTmb();
                    }
                }
            }
        }

        // 入れ替え処理を実行
        private void ExecuteSwap()
        {
            if (_currentSwapMode == SwapMode.None || _swapSourceIndex < 0)
                return;

            List<int> selectedIndices = GetSelectedIndices();

            switch (_currentSwapMode)
            {
                case SwapMode.Cell when selectedIndices.Count == 1:
                    int targetCellIndex = selectedIndices[0];
                    if (_swapSourceIndex != targetCellIndex)
                    {
                        SwapCells(_swapSourceIndex, targetCellIndex);
                    }
                    break;

                case SwapMode.Row:
                    int targetRow = -1;
                    for (int row = 0; row < GRID_SIZE; row++)
                    {
                        if (_selectedRows[row])
                        {
                            targetRow = row;
                            break;
                        }
                    }

                    if (_swapSourceIndex != targetRow && targetRow >= 0)
                    {
                        SwapRows(_swapSourceIndex, targetRow);
                    }
                    break;

                case SwapMode.Column:
                    int targetCol = -1;
                    for (int col = 0; col < GRID_SIZE; col++)
                    {
                        if (_selectedColumns[col])
                        {
                            targetCol = col;
                            break;
                        }
                    }

                    if (_swapSourceIndex != targetCol && targetCol >= 0)
                    {
                        SwapColumns(_swapSourceIndex, targetCol);
                    }
                    break;
            }

            // 入れ替えモードを解除
            _currentSwapMode = SwapMode.None;
            _swapSourceIndex = -1;

            // 更新を通知
            EditorWindow.GetWindow<FPEmoMaker>().Repaint();
        }

        // セル同士を入れ替える
        private void SwapCells(int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= EmoMakerCommon.I.Emos.Length ||
                targetIndex < 0 || targetIndex >= EmoMakerCommon.I.Emos.Length)
                return;

            var sourceEmo = EmoMakerCommon.I.Emos[sourceIndex];
            var targetEmo = EmoMakerCommon.I.Emos[targetIndex];

            if (sourceEmo == null || targetEmo == null)
                return;

            // シェイプ値を交換
            List<FPMKShape> tempShapes = new List<FPMKShape>();

            // ソースのシェイプをコピー
            foreach (var shape in sourceEmo.Shapes)
            {
                tempShapes.Add(new FPMKShape(shape.FullName)
                {
                    Hide = shape.Hide,
                    Val = shape.Val
                });
            }

            // ターゲットから元ソースへコピー
            foreach (var targetShape in targetEmo.Shapes)
            {
                var sourceShape = sourceEmo.Shapes.FirstOrDefault(s => s.FullName == targetShape.FullName);
                if (sourceShape != null)
                {
                    sourceShape.Val = targetShape.Val;
                }
            }

            // 一時保存したソースからターゲットへコピー
            foreach (var tempShape in tempShapes)
            {
                var targetShape = targetEmo.Shapes.FirstOrDefault(s => s.FullName == tempShape.FullName);
                if (targetShape != null)
                {
                    targetShape.Val = tempShape.Val;
                }
            }

            // サムネイルの更新を予約
            sourceEmo.ReserveTmb();
            targetEmo.ReserveTmb();
        }

        // 行同士を入れ替える
        private void SwapRows(int sourceRow, int targetRow)
        {
            if (sourceRow < 0 || sourceRow >= GRID_SIZE ||
                targetRow < 0 || targetRow >= GRID_SIZE)
                return;

            for (int col = 0; col < GRID_SIZE; col++)
            {
                int sourceIndex = sourceRow * GRID_SIZE + col;
                int targetIndex = targetRow * GRID_SIZE + col;

                if (sourceIndex >= 0 && sourceIndex < EmoMakerCommon.I.Emos.Length &&
                    targetIndex >= 0 && targetIndex < EmoMakerCommon.I.Emos.Length)
                {
                    SwapCells(sourceIndex, targetIndex);
                }
            }
        }

        // 列同士を入れ替える
        private void SwapColumns(int sourceCol, int targetCol)
        {
            if (sourceCol < 0 || sourceCol >= GRID_SIZE ||
                targetCol < 0 || targetCol >= GRID_SIZE)
                return;

            for (int row = 0; row < GRID_SIZE; row++)
            {
                int sourceIndex = row * GRID_SIZE + sourceCol;
                int targetIndex = row * GRID_SIZE + targetCol;

                if (sourceIndex >= 0 && sourceIndex < EmoMakerCommon.I.Emos.Length &&
                    targetIndex >= 0 && targetIndex < EmoMakerCommon.I.Emos.Length)
                {
                    SwapCells(sourceIndex, targetIndex);
                }
            }
        }

        // 全選択の切り替え
        private void ToggleAllSelection()
        {
            if (_allSelected)
            {
                // すでに全選択されている場合は全解除
                ClearSelection();
                _allSelected = false;
            }
            else
            {
                // 全選択する
                for (int row = 0; row < GRID_SIZE; row++)
                {
                    for (int col = 0; col < GRID_SIZE; col++)
                    {
                        _selectedCells[row, col] = true;
                    }
                    _selectedRows[row] = true;
                }

                for (int col = 0; col < GRID_SIZE; col++)
                {
                    _selectedColumns[col] = true;
                }

                _allSelected = true;

                // 選択が変更されたことをEmoMakerに通知
                UpdateSelectedEmos();
            }
        }

        // 全選択状態を更新
        private void UpdateAllSelectedState()
        {
            _allSelected = true;
            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    if (!_selectedCells[row, col])
                    {
                        _allSelected = false;
                        return;
                    }
                }
            }
        }

        // 行と列の選択状態をセルの状態から更新
        private void UpdateRowColumnSelection()
        {
            // 行の選択状態を更新
            for (int row = 0; row < GRID_SIZE; row++)
            {
                bool allSelected = true;
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    if (!_selectedCells[row, col])
                    {
                        allSelected = false;
                        break;
                    }
                }
                _selectedRows[row] = allSelected;
            }

            // 列の選択状態を更新
            for (int col = 0; col < GRID_SIZE; col++)
            {
                bool allSelected = true;
                for (int row = 0; row < GRID_SIZE; row++)
                {
                    if (!_selectedCells[row, col])
                    {
                        allSelected = false;
                        break;
                    }
                }
                _selectedColumns[col] = allSelected;
            }
        }

        // 選択をすべてクリア（行・列も含む）
        private void ClearSelection()
        {
            // 行選択をクリア
            for (int i = 0; i < GRID_SIZE; i++)
            {
                _selectedRows[i] = false;
            }

            // 列選択をクリア
            for (int i = 0; i < GRID_SIZE; i++)
            {
                _selectedColumns[i] = false;
            }

            // セル選択をクリア
            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    _selectedCells[row, col] = false;
                }
            }

            // 全選択状態を更新
            _allSelected = false;
        }

        // 選択された表情の更新を適用
        private void UpdateSelectedEmos()
        {
            // 選択されたセルのインデックスを収集
            List<int> selectedIndices = new List<int>();

            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    if (_selectedCells[row, col])
                    {
                        int emoIndex = row * GRID_SIZE + col;
                        if (emoIndex < EmoMakerCommon.I.Emos.Length && EmoMakerCommon.I.Emos[emoIndex] != null)
                        {
                            selectedIndices.Add(emoIndex);
                        }
                    }
                }
            }

            // 最初に選択されたものをアクティブに
            if (selectedIndices.Count > 0)
            {
                EmoMakerCommon.I.ChangeActiveEmo(selectedIndices[0]);
            }
        }

        // 選択されたインデックスのリストを返す
        public List<int> GetSelectedIndices()
        {
            List<int> selectedIndices = new List<int>();

            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    if (_selectedCells[row, col])
                    {
                        int emoIndex = row * GRID_SIZE + col;
                        if (emoIndex < EmoMakerCommon.I.Emos.Length && EmoMakerCommon.I.Emos[emoIndex] != null)
                        {
                            selectedIndices.Add(emoIndex);
                        }
                    }
                }
            }

            return selectedIndices;
        }

        // リソースをクリーンアップ
        public void Dispose()
        {
            if (_dimMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_dimMaterial);
                _dimMaterial = null;
            }

            if (_copyHighlightMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_copyHighlightMaterial);
                _copyHighlightMaterial = null;
            }

            if (_swapHighlightMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_swapHighlightMaterial);
                _swapHighlightMaterial = null;
            }
        }
    }

    public class RightContent
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private bool _showHiddenShapes = false;
        private float _largePreviewSize = 300f; // 大きなサムネイルのサイズ

        public void OnGUI()
        {
            LeftContent leftContent = EditorWindow.GetWindow<FPEmoMaker>().leftContent;
            List<int> selectedIndices = leftContent.GetSelectedIndices();

            float totalHeight = EditorWindow.GetWindow<FPEmoMaker>().position.height;
            // 上部70%（コントロール・スライダー）、下部30%（大きいプレビュー）に分割
            float upperHeight = totalHeight * 0.7f;
            float lowerHeight = totalHeight * 0.3f;

            // ===== 上部エリア：コントロールとスライダー =====
            EditorGUILayout.BeginVertical(GUILayout.Height(upperHeight));

            GUILayout.Label("Control", EditorStyles.boldLabel);

            // アバターを取得ボタン
            if (GUILayout.Button("アクティブなアバターを取得"))
            {
                EmoMakerCommon.I.Initialize();
            }

            // Config操作用のボタンを横並びに配置
            EditorGUILayout.BeginHorizontal();

            // Configの読み込みボタン
            GUI.enabled = EmoMakerCommon.I.MShapes != null && EmoMakerCommon.I.MShapes.Count > 0; // アバター取得済みの場合のみ有効
            if (GUILayout.Button("Config読み込み"))
            {
                LoadConfigFile();
            }

            // Configの書き出しボタン
            GUI.enabled = EmoMakerCommon.I.MShapes != null && EmoMakerCommon.I.MShapes.Count > 0; // アバター取得済みの場合のみ有効
            if (GUILayout.Button("Config書き出し"))
            {
                ExportConfigFile();
            }

            // ボタンの有効状態をリセット
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EmoMakerCommon.I.OriginalDesc = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                "Target", EmoMakerCommon.I.OriginalDesc, typeof(VRCAvatarDescriptor), true);

            // 選択された表情の数を表示
            EditorGUILayout.LabelField($"選択中: {selectedIndices.Count} 個の表情", EditorStyles.boldLabel);

            if (EmoMakerCommon.I.WorkObj?.WorkObject == null || EmoMakerCommon.I.MShapes == null || EmoMakerCommon.I.MShapes.Count == 0)
            {
                GUILayout.Label("アバターが取得されていません。上のボタンをクリックして取得してください。", EditorStyles.helpBox);
                EditorGUILayout.EndVertical();
                DrawLargePreview(selectedIndices); // 下部プレビュー部分
                return;
            }

            var activeEmo = EmoMakerCommon.I.ActiveEmo;

            if (activeEmo?.Shapes == null || activeEmo.Shapes.Count == 0)
            {
                GUILayout.Label("表情データがありません。", EditorStyles.helpBox);
                EditorGUILayout.EndVertical();
                DrawLargePreview(selectedIndices); // 下部プレビュー部分
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("通常シェイプ", EditorStyles.boldLabel);
            var visibleShapes = activeEmo.Shapes.Where(s => !s.Hide).ToList();

            if (visibleShapes.Count == 0)
            {
                GUILayout.Label("表示するシェイプがありません", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var shape in visibleShapes)
                {
                    DrawShapeControl(shape, selectedIndices);
                }
            }

            var hiddenShapes = activeEmo.Shapes.Where(s => s.Hide).ToList();
            if (hiddenShapes.Count > 0)
            {
                EditorGUILayout.Space(10);
                _showHiddenShapes = EditorGUILayout.Foldout(_showHiddenShapes, $"その他 ({hiddenShapes.Count})", true);

                if (_showHiddenShapes)
                {
                    foreach (var shape in hiddenShapes)
                    {
                        DrawShapeControl(shape, selectedIndices);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // セパレーター
            EditorGUILayout.Space(1);
            Rect separatorRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(1);

            // 大きなプレビューを描画
            DrawLargePreview(selectedIndices);
        }

        // CSVファイル読み込み処理
        private void LoadConfigFile()
        {
            if (EmoMakerCommon.I.MShapes == null || EmoMakerCommon.I.MShapes.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "アバターが取得されていません。先にアバターを取得してください。", "OK");
                return;
            }

            // ファイル選択ダイアログを表示
            string path = EditorUtility.OpenFilePanel("設定ファイルを選択", "", "csv");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                // ファイルを読み込む
                string[] lines = System.IO.File.ReadAllLines(path);
                if (lines.Length < 2)
                {
                    EditorUtility.DisplayDialog("エラー", "ファイルが空か、ヘッダーのみです。", "OK");
                    return;
                }

                // ヘッダー行を解析
                string[] headers = lines[0].Split(',');
                if (headers.Length < 3 || headers[0] != "Left" || headers[1] != "Right")
                {
                    EditorUtility.DisplayDialog("エラー", "ファイルフォーマットが正しくありません。先頭2列はLeft,Rightである必要があります。", "OK");
                    return;
                }

                // シェイプ名のマッピング作成
                Dictionary<string, int> shapeIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 大文字小文字を区別しない
                for (int i = 2; i < headers.Length; i++)
                {
                    string shapeName = headers[i];
                    if (!string.IsNullOrEmpty(shapeName))
                    {
                        shapeIndices[shapeName] = i;
                    }
                }

                // データ行を解析して適用
                int configCount = 0;
                for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] values = line.Split(',');
                    if (values.Length < 3) continue; // 少なくともLeft, Right, 1つの値が必要

                    // Left,Right座標を解析
                    if (!int.TryParse(values[0], out int leftValue) || !int.TryParse(values[1], out int rightValue))
                        continue;

                    // 値が範囲内かチェック
                    if (leftValue < 0 || leftValue >= 8 || rightValue < 0 || rightValue >= 8)
                        continue;

                    // インデックスの計算
                    int emoIndex = leftValue * 8 + rightValue;
                    if (emoIndex < 0 || emoIndex >= EmoMakerCommon.I.Emos.Length || EmoMakerCommon.I.Emos[emoIndex] == null)
                        continue;

                    var emo = EmoMakerCommon.I.Emos[emoIndex];
                    bool updated = false;

                    // シェイプ値を適用
                    foreach (var shape in emo.Shapes)
                    {
                        string shapeName = shape.ShortName;
                        if (shapeIndices.TryGetValue(shapeName, out int shapeIndex) && shapeIndex < values.Length)
                        {
                            if (int.TryParse(values[shapeIndex], out int shapeValue))
                            {
                                shape.Val = Mathf.Clamp(shapeValue, 0, 100); // 0〜100の範囲に制限
                                updated = true;
                            }
                        }
                    }

                    if (updated)
                    {
                        emo.ReserveTmb();
                        configCount++;
                    }
                }

            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("エラー", $"ファイルの読み込み中にエラーが発生しました: {ex.Message}", "OK");
                Debug.LogError($"Config読み込みエラー: {ex}");
            }
        }

        // CSVファイル書き出し処理
        private void ExportConfigFile()
        {
            if (EmoMakerCommon.I.MShapes == null || EmoMakerCommon.I.MShapes.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "アバターが取得されていません。先にアバターを取得してください。", "OK");
                return;
            }

            // ファイル保存ダイアログを表示
            string path = EditorUtility.SaveFilePanel("設定ファイルを保存", "", "config.csv", "csv");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                using (var writer = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8))
                {
                    // すべてのシェイプキーの名前を収集
                    HashSet<string> allShapeNames = new HashSet<string>();
                    foreach (var emo in EmoMakerCommon.I.Emos)
                    {
                        if (emo != null && emo.Shapes != null)
                        {
                            foreach (var shape in emo.Shapes)
                            {
                                if (!string.IsNullOrEmpty(shape.ShortName) && shape.Val > 0) // 値が0より大きいもののみ
                                {
                                    allShapeNames.Add(shape.ShortName);
                                }
                            }
                        }
                    }

                    // ヘッダー行を書き出し
                    writer.Write("Left,Right");
                    foreach (var shapeName in allShapeNames)
                    {
                        writer.Write($",{shapeName}");
                    }
                    writer.WriteLine();

                    // 各表情の値を書き出し
                    int exportCount = 0;
                    for (int left = 0; left < 8; left++)
                    {
                        for (int right = 0; right < 8; right++)
                        {
                            int index = left * 8 + right;
                            if (index < EmoMakerCommon.I.Emos.Length && EmoMakerCommon.I.Emos[index] != null)
                            {
                                var emo = EmoMakerCommon.I.Emos[index];

                                // 表情のいずれかのシェイプが0より大きい値を持っているかチェック
                                bool hasNonZeroValue = false;
                                foreach (var shape in emo.Shapes)
                                {
                                    if (shape.Val > 0)
                                    {
                                        hasNonZeroValue = true;
                                        break;
                                    }
                                }

                                // 値がある表情のみ出力
                                if (hasNonZeroValue)
                                {
                                    writer.Write($"{left},{right}");

                                    // 各シェイプの値を出力
                                    foreach (var shapeName in allShapeNames)
                                    {
                                        var shape = emo.Shapes.FirstOrDefault(s => s.ShortName == shapeName);
                                        int value = shape != null ? shape.Val : 0;
                                        writer.Write($",{value}");
                                    }
                                    writer.WriteLine();
                                    exportCount++;
                                }
                            }
                        }
                    }

                    EditorUtility.DisplayDialog("完了", $"{exportCount}個の表情設定をエクスポートしました。", "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("エラー", $"ファイルの書き出し中にエラーが発生しました: {ex.Message}", "OK");
                Debug.LogError($"Config書き出しエラー: {ex}");
            }
        }

        // 大きなプレビュー部分を描画する新しいメソッド
        private void DrawLargePreview(List<int> selectedIndices)
        {
            float lowerHeight = EditorWindow.GetWindow<FPEmoMaker>().position.height * 0.3f;

            // 余分なパディングを除去するスタイル
            GUIStyle noMarginStyle = new GUIStyle();
            noMarginStyle.margin = new RectOffset(0, 0, 0, 0);
            noMarginStyle.padding = new RectOffset(0, 0, 0, 0);

            EditorGUILayout.BeginVertical(noMarginStyle, GUILayout.Height(lowerHeight));

            // アクティブな表情を取得
            var activeEmo = EmoMakerCommon.I.ActiveEmo;

            // プレビューエリアのサイズを計算（正方形かつ利用可能なスペース内で最大サイズ）
            float availableWidth = FPEmoMaker.RIGHT_PANEL_WIDTH - 10; // 右パネル幅から余白を引く
            float availableHeight = lowerHeight - 35; // カメラ設定スライダー用に高さを少し減らす
            _largePreviewSize = Mathf.Min(availableWidth, availableHeight);

            // 大きなサムネイル表示用のレイアウト
            EditorGUILayout.BeginHorizontal(noMarginStyle);
            GUILayout.FlexibleSpace();

            // 大きなサムネイルの表示用Rect
            Rect largePreviewRect = GUILayoutUtility.GetRect(_largePreviewSize, _largePreviewSize);

            // アクティブな表情のテクスチャを表示
            if (activeEmo != null && activeEmo.Texture != null)
            {
                GUI.DrawTexture(largePreviewRect, activeEmo.Texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Box(largePreviewRect, "");
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // カメラ設定スライダーを追加（カメラが初期化されている場合のみ）
            if (EmoMakerCommon.I.WorkObj?.WorkObject != null && EmoMakerCommon.I.Capture != null)
            {
                // 利用可能な幅を計算
                float availableControlWidth = FPEmoMaker.RIGHT_PANEL_WIDTH - 20; // パネル幅から余白を引いた値
                float sliderWidth = (availableControlWidth - 130) / 2; // ラベル幅とスペースを考慮して2分割

                EditorGUILayout.BeginHorizontal();

                try
                {
                    // カメラのOrthographicSize設定
                    EditorGUILayout.LabelField("サイズ:", GUILayout.Width(50));
                    float currentSize = EmoMakerCommon.I.Capture.OrthographicSize;
                    float newSize = EditorGUILayout.Slider(currentSize, 0.1f, 1.0f, GUILayout.Width(sliderWidth));
                    if (newSize != currentSize)
                    {
                        EmoMakerCommon.I.Capture.SetOrthographicSize(newSize);
                        RegenerateAllThumbnails();
                    }

                    GUILayout.Space(10);

                    // カメラの高さ設定
                    EditorGUILayout.LabelField("高さ:", GUILayout.Width(50));
                    float currentHeight = EmoMakerCommon.I.Capture.CameraHeight;
                    float newHeight = EditorGUILayout.Slider(currentHeight, 0.8f, 1.5f, GUILayout.Width(sliderWidth));
                    if (newHeight != currentHeight)
                    {
                        EmoMakerCommon.I.Capture.SetHeight(newHeight);
                        RegenerateAllThumbnails();
                    }
                }
                catch (Exception ex)
                {
                    // エラー発生時はユーザーに通知せず、デバッグログに書き込む
                    Debug.LogError($"カメラ設定でエラーが発生しました: {ex.Message}");
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // カメラが初期化されていない場合のメッセージ
                EditorGUILayout.HelpBox("カメラ設定はアバター取得後に利用できます。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
        private void RegenerateAllThumbnails()
        {
            // キャッシュをクリア
            FPEMTmb.I.Clear();

            // すべての表情の更新フラグを立てる
            if (EmoMakerCommon.I.Emos != null)
            {
                foreach (var emo in EmoMakerCommon.I.Emos)
                {
                    if (emo != null)
                    {
                        emo.NeedUpdate = true;
                    }
                }
            }

            // 更新メッセージを表示
            //EditorUtility.DisplayDialog("更新", "カメラ設定が変更されました。すべての表情のサムネイルを再生成します。", "OK");
        }

        private void DrawShapeControl(FPMKShape shape, List<int> selectedIndices)
        {
            EditorGUILayout.BeginHorizontal();

            // ラベルの幅を調整（短いラベル名の場合でも一定の幅を確保）
            float labelWidth = 150;

            // ラベルを表示
            EditorGUILayout.LabelField(shape.ShortName, GUILayout.Width(labelWidth));

            // スライダーの前に小さなスペースを追加して見栄えを改善
            GUILayout.Space(5);

            // 選択された全ての表情でこのシェイプの値を取得
            bool allSameValue = true;
            int commonValue = shape.Val;

            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < EmoMakerCommon.I.Emos.Length && EmoMakerCommon.I.Emos[index] != null)
                {
                    var emo = EmoMakerCommon.I.Emos[index];
                    var shapeInEmo = emo.Shapes.FirstOrDefault(s => s.FullName == shape.FullName);

                    if (shapeInEmo != null && shapeInEmo.Val != commonValue)
                    {
                        allSameValue = false;
                        break;
                    }
                }
            }

            if (allSameValue)
            {
                // 全ての値が同じ場合はスライダーを表示
                int prevVal = shape.Val;
                shape.Val = EditorGUILayout.IntSlider(shape.Val, 0, 100, GUILayout.ExpandWidth(true));

                if (prevVal != shape.Val)
                {
                    // 選択された全ての表情のシェイプ値を更新
                    UpdateAllSelectedShapes(shape, selectedIndices);
                }
            }
            else
            {
                // 値が異なる場合は「あわせる」ボタン
                if (GUILayout.Button("値をあわせる", GUILayout.ExpandWidth(true)))
                {
                    UpdateAllSelectedShapes(shape, selectedIndices);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // 選択された全ての表情のシェイプ値を更新
        private void UpdateAllSelectedShapes(FPMKShape referenceShape, List<int> selectedIndices)
        {
            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < EmoMakerCommon.I.Emos.Length && EmoMakerCommon.I.Emos[index] != null)
                {
                    var emo = EmoMakerCommon.I.Emos[index];
                    var shapeInEmo = emo.Shapes.FirstOrDefault(s => s.FullName == referenceShape.FullName);

                    if (shapeInEmo != null)
                    {
                        shapeInEmo.Val = referenceShape.Val;
                        emo.ReserveTmb(); // サムネイルの更新を予約
                    }
                }
            }
        }
    }



}