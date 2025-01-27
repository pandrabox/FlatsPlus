using UnityEditor;
using UnityEngine;
using System.IO;
using com.github.pandrabox.pandravase.editor;
using static com.github.pandrabox.pandravase.runtime.Util;
using System;


public class TTIEditorWindow : EditorWindow
{
    [MenuItem("Tools/TTI")]
    public static void ShowWindow()
    {
        // ウィンドウを表示
        EditorWindow.GetWindow<TTIEditorWindow>("TTI");
    }

    private void OnGUI()
    {
        GUILayout.Label("TTI Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("Run"))
        {
            CaptureTextToImage();
        }
    }
    private void CaptureTextToImage()
    {
        Texture2D strImg;
        using (var imageGenerator = new ImageGenerator(512))
        {
            strImg = imageGenerator.DrawText("pandra").Capture();
        }
        //assets直下のico.pngを読み込み
        Texture2D BG = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ico.png");
        //strImgをBGに合わせてリサイズ
        strImg = ResizeTexture(strImg, BG.width, BG.height);
        //BGにstrImgを合成
        BG = MergeTexture(BG, strImg);
        //BGを保存
        SaveTexture(BG, "Assets/ico2.png");
    }

    private Texture2D ResizeTexture(Texture2D texture, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(texture, rt);
        Texture2D resizedTexture = new Texture2D(width, height);
        resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resizedTexture.Apply();
        RenderTexture.ReleaseTemporary(rt);
        return resizedTexture;
    }
    private Texture2D MergeTexture(Texture2D target, Texture2D source)
    {
        if (source.width != target.width || source.height != target.height)
        {
            Debug.LogError("Source and target textures must have the same dimensions.");
            return null;
        }

        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
        RenderTexture.active = rt;

        // Clear the RenderTexture
        GL.Clear(true, true, Color.clear);

        // Create a material with a shader that blends based on alpha
        Material blendMaterial = new Material(Shader.Find("Hidden/AlphaBlend"));
        blendMaterial.SetTexture("_SourceTex", source);
        blendMaterial.SetTexture("_TargetTex", target);

        // Draw the textures with blending
        Graphics.Blit(null, rt, blendMaterial);

        // Create a new Texture2D to store the result
        Texture2D mergedTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        mergedTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        mergedTexture.Apply();

        RenderTexture.ReleaseTemporary(rt);

        return mergedTexture;
    }


    private void SaveTexture(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
    }
}


public class ImageGenerator : IDisposable
{
    private int _size = 512;
    private GameObject _tti;
    private Color _backgroundColor;
    RenderTexture renderTexture;
    Camera camera;

    public ImageGenerator(int size, Color? backgroundColor = null)
    {
        if (backgroundColor == null)
        {
            _backgroundColor = new Color(1, 1, 1, 0);
        }
        else
        {
            _backgroundColor = (Color)backgroundColor;
        }
        _size = size;
        ReCreateTTI();
        CreateCapture();
    }

    public void ReCreateTTI()
    {
        ClearTTI();
        _tti = new GameObject("TTI");
    }
    public void ClearTTI()
    {
        _tti = GameObject.Find("TTI");
        if (_tti != null)
        {
            GameObject.DestroyImmediate(_tti);
        }
    }
    public void CaptureTextToImage()
    {
        ClearTTI();
    }

    public ImageGenerator DrawText(string text, float size = 3f, Color? fontColor = null)
    {
        if (fontColor == null) fontColor = Color.black;

        GameObject textGO = new GameObject("Text");
        textGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f) * size;
        textGO.transform.position = new Vector3(0, 0, 10); // 画面に見えるように位置を設定
        textGO.transform.SetParent(_tti.transform);

        TextMesh textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 500;
        textMesh.color = (Color)fontColor; // 白色でアルファ値を1に設定
        textMesh.anchor = TextAnchor.MiddleCenter;


        Renderer textRenderer = textGO.GetComponent<Renderer>();
        Bounds textBounds = textRenderer.bounds;

        AdjustCameraToFitBounds(textBounds);

        return this;
    }

    void AdjustCameraToFitBounds(Bounds bounds)
    {
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, camera.transform.position.z);

        float verticalSize = bounds.size.y / 2f;
        float horizontalSize = bounds.size.x / camera.aspect / 2f;

        camera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    public Texture2D Capture(string savePath = null)
    {
        // カメラの設定
        camera.transform.position = new Vector3(0, 0, -10); // カメラをText方向に配置
        camera.orthographic = true;
        //camera.orthographicSize = 5;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = _backgroundColor; // 背景を完全な透明に設定

        // カメラでレンダリング
        camera.Render();

        // RenderTextureをTexture2Dに変換（アルファチャンネルを考慮）
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        if (savePath != null) 
        {
            SaveImage(texture, savePath);
        }
        RenderTexture.active = null;
        renderTexture.Release();

        return texture;
    }

    public void CreateCapture()
    {
        // RenderTexture
        renderTexture = new RenderTexture(_size, _size, 24, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        // カメラ
        camera = new GameObject("CaptureCamera").AddComponent<Camera>();
        camera.targetTexture = renderTexture;
        camera.transform.SetParent(_tti.transform);
    }

    private void SaveImage(Texture2D image, string name = "debug_texture.png")
    {
        string debugPath = Path.Combine(Application.dataPath, name);
        byte[] debugData = image.EncodeToPNG();
        File.WriteAllBytes(debugPath, debugData);
        Debug.Log("Debug texture saved at: " + debugPath);
    }

    public void Dispose()
    {
        AssetDatabase.Refresh();
        ClearTTI();
    }

    public float TextWidth(TextMesh textMesh)
    {
        string text = textMesh.text;
        Font font = textMesh.font;
        float totalWidth = 0;

        // テキストメッシュのフォントサイズとスケールを考慮
        float fontSize = textMesh.fontSize;
        float characterSize = textMesh.characterSize;

        // フォントをアクティブにしてメトリクスを読み取れるようにする
        font.RequestCharactersInTexture(text, (int)fontSize, textMesh.fontStyle);

        foreach (char c in text)
        {
            if (font.GetCharacterInfo(c, out CharacterInfo charInfo, (int)fontSize, textMesh.fontStyle))
            {
                totalWidth += charInfo.advance * characterSize;
            }
        }
        return totalWidth * textMesh.transform.localScale.x;
    }
}

