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
        private bool _disposed = false;
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
            WorkObject.transform.position = new Vector3(10, 0);

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
            if (_disposed) return;

            Reset();
            WorkObject = null;
            _bodyRenderer = null;

            _disposed = true;
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
        private bool _disposed = false;

        public FPCapture()
        {
        }

        public void Initialize(GameObject parent, int textureSize)
        {
            if (parent == null) return;

            Dispose();
            _disposed = false;

            // カメラの作成
            _camera = new GameObject("Camera").AddComponent<Camera>();
            _camera.transform.parent = parent.transform;
            _camera.orthographic = true;
            _camera.orthographicSize = .3f;
            _camera.transform.localPosition = new Vector3(0, 1.28f, 100f);
            _camera.transform.eulerAngles = new Vector3(0, 180, 0);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1);
            _camera.aspect = 1.0f;

            CreateRenderTexture(textureSize);
        }

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
            if (_disposed) return;

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

            _disposed = true;
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

        public int TextureSize = 180;
        private bool _disposed = false;

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
            Capture.Initialize(WorkObj.WorkObject, TextureSize);
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
            if (_disposed) return;

            Capture?.Dispose();
            Capture = null;

            WorkObj?.Dispose();
            WorkObj = null;

            _disposed = true;
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
            Hide = ShortName.StartsWith("vrc.v_");
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
            EditorApplication.delayCall += () => Capture();
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
            }
        }

        public void OnGUI()
        {
            // スクロールビューを開始
            _thumbnailScroll = EditorGUILayout.BeginScrollView(_thumbnailScroll);

            // グリッド全体の余白を設定
            float cellSize = (EditorWindow.GetWindow<FPEmoMaker>().position.width - FPEmoMaker.RIGHT_PANEL_WIDTH - 40) / (GRID_SIZE + 1);

            // テーブルレイアウト
            GUILayout.BeginVertical();

            // 列選択ボタンの行
            GUILayout.BeginHorizontal();
            // 左上の空白セル
            GUILayout.Box("", GUILayout.Width(cellSize), GUILayout.Height(cellSize));

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

                    // 選択が変更されたことをEmoMakerに通知
                    UpdateSelectedEmos();

                    // イベントを処理済みとしてマーク
                    Event.current.Use();
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

                    // 選択が変更されたことをEmoMakerに通知
                    UpdateSelectedEmos();

                    // イベントを処理済みとしてマーク
                    Event.current.Use();
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

                    // 表情テクスチャを表示
                    if (emo != null && emo.Texture != null)
                    {
                        if (_selectedCells[row, col])
                        {
                            // 選択されたセルは通常の明るさで表示（明るさを100%に設定）
                            Color oldColor = GUI.color;
                            GUI.color = new Color(1.0f, 1.0f, 1.0f, 1.0f); // 完全な明るさ
                            GUI.DrawTexture(cellRect, emo.Texture, ScaleMode.ScaleToFit);
                            GUI.color = oldColor;
                        }
                        else
                        {
                            // 選択されていないセルは暗く表示（明るさを20%に設定）
                            Color oldColor = GUI.color;
                            float intensity = 0.2f;
                            GUI.color = new Color(intensity, intensity, intensity, 1.0f);
                            GUI.DrawTexture(cellRect, emo.Texture, ScaleMode.ScaleToFit);
                            GUI.color = oldColor;
                        }
                    }
                    else
                    {
                        // テクスチャがない場合は空欄 (暗い表示/明るい表示を区別)
                        if (_selectedCells[row, col])
                        {
                            GUIStyle brightStyle = new GUIStyle(GUI.skin.box);
                            brightStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f); // 明るいテキスト色
                            GUI.Box(cellRect, $"{row},{col}", brightStyle);
                        }
                        else
                        {
                            GUIStyle dimStyle = new GUIStyle(GUI.skin.box);
                            dimStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                            GUI.Box(cellRect, $"{row},{col}", dimStyle);
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

                EditorUtility.DisplayDialog("完了", $"{configCount}個の表情設定を適用しました。", "OK");
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
            float availableHeight = lowerHeight - 5;
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

            EditorGUILayout.EndVertical();
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