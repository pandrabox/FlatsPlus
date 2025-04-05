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
                EmoMakerCommon.I.CheckAndProcessUpdates();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2));
            leftContent.OnGUI();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2));
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
        public string WorkObjectName => "FlatsPlusEmoMakerRoot";
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

        public void Dispose()
        {
            if (_disposed) return;

            var wo = GameObject.Find(WorkObjectName);
            if (wo != null)
            {
                GameObject.DestroyImmediate(wo);
            }
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
        public FPMKEmo[,] Emos = new FPMKEmo[1, 2]; // 1行2列の表情配列
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
            // WorkObjにVRCAvatarDescriptorの検索処理を委譲
            OriginalDesc = WorkObj.Initialize();
            if (OriginalDesc == null) return;

            // WorkObjectが初期化されたら、Captureも初期化
            Capture.Initialize(WorkObj.WorkObject, TextureSize);

            // WorkObjからシェイプ情報を取得
            MShapes = WorkObj.GetShapes();

            // 表情を初期化
            InitializeEmos();
        }

        private void InitializeEmos()
        {
            for (int i = 0; i < Emos.GetLength(1); i++)
            {
                if (Emos[0, i] == null)
                {
                    // FPMKEmoのコンストラクタで全て初期化
                    Emos[0, i] = new FPMKEmo(MShapes, TextureSize);
                }
            }
        }

        // 更新が必要な表情のUpdateを実行
        public void CheckAndProcessUpdates()
        {
            for (int i = 0; i < Emos.GetLength(1); i++)
            {
                var emo = Emos[0, i];
                if (emo != null && emo.NeedUpdate && !Capture.IsCapturing)
                {
                    emo.ExecuteUpdate(this);
                    break; // 一度に1つだけ処理
                }
            }
        }

        // 現在の選択を変更して適用
        public void ChangeActiveEmo(int newIndex)
        {
            if (newIndex == ActiveEmoIndex) return;
            ActiveEmoIndex = newIndex;
            ActiveEmo?.MarkNeedUpdate();
        }

        public FPMKEmo ActiveEmo => (Emos != null && ActiveEmoIndex < Emos.GetLength(1)) ? Emos[0, ActiveEmoIndex] : null;

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
        public bool NeedUpdate { get; private set; } = true; // 初期状態では更新が必要

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

            CreateTexture(textureSize);
        }

        public void CreateTexture(int size)
        {
            if (Texture != null) return;
            Texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        }

        // Update要求フラグを建てる
        public void MarkNeedUpdate()
        {
            NeedUpdate = true;
        }

        // 実際の更新処理
        public void ExecuteUpdate(EmoMakerCommon common)
        {
            if (!NeedUpdate) return;

            // フラグをリセット
            NeedUpdate = false;

            // シェイプを設定
            SetShape(common.WorkObj?.BodyRenderer);

            // 遅延してテクスチャを更新
            EditorApplication.delayCall += () => {
                if (common?.Capture != null && !common.Capture.IsCapturing)
                {
                    UpdateTexture(common.Capture, common.TextureSize);
                }
            };
        }

        private void SetShape(SkinnedMeshRenderer bodyRenderer)
        {
            if (Shapes == null || Shapes.Count == 0) return;
            if (bodyRenderer == null) return;

            foreach (var shape in Shapes)
            {
                int shapeIndex = bodyRenderer.sharedMesh.GetBlendShapeIndex(shape.FullName);
                if (shapeIndex != -1)
                {
                    bodyRenderer.SetBlendShapeWeight(shapeIndex, shape.Val);
                }
            }
        }

        private void UpdateTexture(FPCapture capture, int textureSize)
        {
            CreateTexture(textureSize);
            capture?.CaptureToTexture(Texture, textureSize);
        }
    }

    public class LeftContent
    {
        public void OnGUI()
        {
            GUILayout.Label("表情リスト", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            for (int i = 0; i < EmoMakerCommon.I.Emos.GetLength(1); i++)
            {
                var emo = EmoMakerCommon.I.Emos[0, i];
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
        }
    }

    public class RightContent
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private bool _showHiddenShapes = false;

        public void OnGUI()
        {
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
                return;
            }

            var activeEmo = EmoMakerCommon.I.ActiveEmo;

            if (activeEmo?.Shapes == null || activeEmo.Shapes.Count == 0)
            {
                GUILayout.Label("表情データがありません。", EditorStyles.helpBox);
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
                _showHiddenShapes = EditorGUILayout.Foldout(_showHiddenShapes, $"隠しシェイプ ({hiddenShapes.Count})", true);

                if (_showHiddenShapes)
                {
                    foreach (var shape in hiddenShapes)
                    {
                        DrawShapeControl(shape);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawShapeControl(FPMKShape shape)
        {
            EditorGUILayout.BeginHorizontal();

            bool newHide = EditorGUILayout.Toggle(shape.Hide, GUILayout.Width(20));
            if (newHide != shape.Hide)
            {
                shape.Hide = newHide;
                EditorGUIUtility.ExitGUI();
            }

            EditorGUILayout.LabelField(shape.ShortName, GUILayout.Width(150));

            int prevVal = shape.Val;
            shape.Val = EditorGUILayout.IntSlider(shape.Val, 0, 100);

            if (prevVal != shape.Val)
            {
                EmoMakerCommon.I.ActiveEmo?.MarkNeedUpdate();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}