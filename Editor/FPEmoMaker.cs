using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace com.github.pandrabox.flatsplus.editor
{
    public class FPEmoMaker : EditorWindow
    {
        private LeftContent leftContent;
        private RightContent rightContent;
        private float lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.1f; // 0.1秒ごとに更新
        private const float RIGHT_PANEL_WIDTH = 400f; // 右パネルの固定幅

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
        public FPMKEmo[] Emos = new FPMKEmo[5];
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

        public void OnGUI()
        {
            // 表情リストのみを表示（プレビューは右側に移動）
            GUIStyle noMarginStyle = new GUIStyle();
            noMarginStyle.margin = new RectOffset(0, 0, 0, 0);
            noMarginStyle.padding = new RectOffset(0, 0, 0, 0);

            EditorGUILayout.BeginVertical(noMarginStyle);

            // スクロールビュー
            _thumbnailScroll = EditorGUILayout.BeginScrollView(_thumbnailScroll);
            GUILayout.BeginHorizontal(noMarginStyle);

            for (int i = 0; i < EmoMakerCommon.I.Emos.Length; i++)
            {
                var emo = EmoMakerCommon.I.Emos[i];
                if (emo == null) continue;

                GUILayout.BeginVertical(GUILayout.Width(EmoMakerCommon.I.TextureSize + 10));

                if (EmoMakerCommon.I.ActiveEmoIndex == i)
                {
                    GUILayout.Label($"表情 {i + 1}", EditorStyles.boldLabel);
                }
                else
                {
                    GUILayout.Label($"表情 {i + 1}");
                }

                Rect boxRect = GUILayoutUtility.GetRect(EmoMakerCommon.I.TextureSize, EmoMakerCommon.I.TextureSize);

                if (EmoMakerCommon.I.ActiveEmoIndex == i)
                {
                    EditorGUI.DrawRect(new Rect(boxRect.x - 3, boxRect.y - 3, boxRect.width + 6, boxRect.height + 6), new Color(1f, 0.8f, 0f));
                }

                if (emo.Texture != null)
                {
                    GUI.DrawTexture(boxRect, emo.Texture);
                }
                else
                {
                    GUI.Box(boxRect, "No Preview");
                }

                // クリック時の処理
                if (Event.current.type == EventType.MouseDown && boxRect.Contains(Event.current.mousePosition))
                {
                    EmoMakerCommon.I.ChangeActiveEmo(i);
                    Event.current.Use();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }

    public class RightContent
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private bool _showHiddenShapes = false;
        private float _largePreviewSize = 300f; // 大きなサムネイルのサイズ

        public void OnGUI()
        {
            float totalHeight = EditorWindow.GetWindow<FPEmoMaker>().position.height;
            // 上部70%（コントロール・スライダー）、下部30%（大きいプレビュー）に分割
            float upperHeight = totalHeight * 0.7f;
            float lowerHeight = totalHeight * 0.3f;

            // ===== 上部エリア：コントロールとスライダー =====
            EditorGUILayout.BeginVertical(GUILayout.Height(upperHeight));

            GUILayout.Label("Control", EditorStyles.boldLabel);

            if (GUILayout.Button("アクティブなアバターを取得"))
            {
                EmoMakerCommon.I.Initialize();
            }

            EmoMakerCommon.I.OriginalDesc = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                "Target", EmoMakerCommon.I.OriginalDesc, typeof(VRCAvatarDescriptor), true);

            EditorGUILayout.LabelField($"現在の表情: {EmoMakerCommon.I.ActiveEmoIndex + 1}", EditorStyles.boldLabel);

            if (EmoMakerCommon.I.WorkObj?.WorkObject == null || EmoMakerCommon.I.MShapes == null || EmoMakerCommon.I.MShapes.Count == 0)
            {
                GUILayout.Label("アバターが取得されていません。上のボタンをクリックして取得してください。", EditorStyles.helpBox);
                EditorGUILayout.EndVertical();
                DrawLargePreview(); // 下部プレビュー部分
                return;
            }

            var activeEmo = EmoMakerCommon.I.ActiveEmo;

            if (activeEmo?.Shapes == null || activeEmo.Shapes.Count == 0)
            {
                GUILayout.Label("表情データがありません。", EditorStyles.helpBox);
                EditorGUILayout.EndVertical();
                DrawLargePreview(); // 下部プレビュー部分
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
                    DrawShapeControl(shape);
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
                        DrawShapeControl(shape);
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
            DrawLargePreview();
        }

        // 大きなプレビュー部分を描画する新しいメソッド
        private void DrawLargePreview()
        {
            float lowerHeight = EditorWindow.GetWindow<FPEmoMaker>().position.height * 0.3f;

            // 余分なパディングを除去するスタイル
            GUIStyle noMarginStyle = new GUIStyle();
            noMarginStyle.margin = new RectOffset(0, 0, 0, 0);
            noMarginStyle.padding = new RectOffset(0, 0, 0, 0);

            EditorGUILayout.BeginVertical(noMarginStyle, GUILayout.Height(lowerHeight));

            var activeEmo = EmoMakerCommon.I.ActiveEmo;

            // プレビューエリアのサイズを計算（正方形かつ利用可能なスペース内で最大サイズ）
            float availableWidth = 400 - 10; // 右パネル幅から余白を引く
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

        private void DrawShapeControl(FPMKShape shape)
        {
            EditorGUILayout.BeginHorizontal();

            // ラベルの幅を調整（短いラベル名の場合でも一定の幅を確保）
            float labelWidth = 150;

            // ラベルを表示
            EditorGUILayout.LabelField(shape.ShortName, GUILayout.Width(labelWidth));

            // スライダーの前に小さなスペースを追加して見栄えを改善
            GUILayout.Space(5);

            // スライダーを表示（残りの利用可能な幅を使用）
            int prevVal = shape.Val;
            shape.Val = EditorGUILayout.IntSlider(shape.Val, 0, 100, GUILayout.ExpandWidth(true));

            // 値を数値で表示（オプション）
            //EditorGUILayout.LabelField(shape.Val.ToString(), GUILayout.Width(30));

            if (prevVal != shape.Val)
            {
                EmoMakerCommon.I.ActiveEmo.ReserveTmb();
            }

            EditorGUILayout.EndHorizontal();
        }
    }



}