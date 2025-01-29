using UnityEditor;
using UnityEngine;
using System.IO;
using com.github.pandrabox.pandravase.editor;
using static com.github.pandrabox.pandravase.runtime.Util;
using System;
using Boo.Lang;
using com.github.pandrabox.pandravase.runtime;
using System.Text.RegularExpressions;
using System.Linq;


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
            CaptureTextToImage("FlatsPlus");
        }
    }



    private void CaptureTextToImage(string projectName)
    {
        PandraProject _prj = new PandraProject(projectName, ProjectTypes.VPM);
        Texture2D strImg;
        using (var imageGenerator = new ImageGenerator(170))
        {
            strImg = imageGenerator.DrawText($"{_prj.ProjectName}\n\r{_prj.VPMVersion}").Capture();
        }
        //Texture2D BG = AssetDatabase.LoadAssetAtPath<Texture2D>($@"Packages/com.github.pandrabox.flatsplus/Assets/Ico/Ico/i1.png");
        //strImg = ResizeTexture(strImg, BG.width, BG.height);
        //BG = MergeTexture(BG, strImg);

        //i1～6をリストに読み込み
        List<Texture2D> textures = new List<Texture2D>();
        for (int i = 1; i <= 6; i++)
        {
            textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>($@"Packages/com.github.pandrabox.flatsplus/Assets/Ico/Ico/i{i}.png"));
        }
        textures.Add(AddMergin(strImg,20));
        Texture2D BG = CreateTileTexture( textures, 3, 170*3);

        SaveTexture(BG, "Assets/ico2.png");
    }

    // マージンの作成(色を指定しない場合入力テクスチャの左上)
    public Texture2D AddMergin(Texture2D texture, int margin, Color? color = null)
    {
        if (color == null)
        {
            color = texture.GetPixel(0, texture.height - 1);
        }
        Texture2D mergedTexture = new Texture2D(texture.width + margin * 2, texture.height + margin * 2);
        mergedTexture.SetPixels(Enumerable.Repeat((Color)color, mergedTexture.width * mergedTexture.height).ToArray());
        mergedTexture.SetPixels(margin, margin, texture.width, texture.height, texture.GetPixels());
        mergedTexture.Apply();
        return mergedTexture;
    }

    // 指定サイズの空のTexture2Dを作成し、配列で指定した複数のTexture2Dを結合する。引数には折り返す列数を指定する。
    // 配列内のテクスチャは正方形
    private Texture2D CreateTileTexture(List<Texture2D> textures, int columns, int tileWidth, int tileHeight = -1) => CreateTileTexture(textures.ToArray(), columns, tileWidth, tileHeight);
    private Texture2D CreateTileTexture(Texture2D[] textures, int columns, int tileWidth, int tileHeight=-1)
    {
        int unitSize = tileWidth / columns;
        SetReadable(textures);
        ResizeTextures(textures, unitSize, unitSize);
        if (tileHeight == -1) tileHeight = tileWidth;
        Texture2D tileTexture = new Texture2D(tileWidth, tileHeight);
        Color[] colors = new Color[tileWidth * tileHeight];


        for (int i = 0; i < textures.Length; i++)
        {
            int x = i % columns * unitSize;
            int y = tileHeight - ((i / columns + 1) * unitSize); // Y座標を反転
            Color[] pixels = textures[i].GetPixels();
            tileTexture.SetPixels(x, y, unitSize, unitSize, pixels);
            Debug.LogWarning($@"{x},{y},{unitSize},{unitSize},{pixels},{textures.Length}");
        }
        tileTexture.Apply();
        return tileTexture;
    }

    //配列内の画像を全て指定サイズにリサイズする
    private Texture2D[] ResizeTextures(Texture2D[] textures, int width, int height)
    {
        for (int i = 0; i < textures.Length; i++)
        {
            textures[i] = ResizeTexture(textures[i], width, height);
        }
        return textures;
    }

    //指定サイズにリサイズする
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

    //配列内の画像を全てReadableに設定する
    public static Texture2D[] SetReadable(Texture2D[] textures)
    {
        for (int i = 0; i < textures.Length; i++)
        {
            textures[i] = SetReadable(textures[i]);
        }
        return textures;
    }

    /// <summary>
    /// 指定されたテクスチャをReadableに設定するメソッド。
    /// </summary>
    /// <param name="texture">対象のテクスチャ</param>
    /// <returns>修正後のテクスチャを返す</returns>
    public static Texture2D SetReadable(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogWarning("テクスチャがnullです。");
            return null;
        }
        // テクスチャのパスを取得
        string texturePath = AssetDatabase.GetAssetPath(texture);
        // テクスチャインポーターを取得
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"テクスチャインポーターが取得できません: {texturePath}");
            return texture;
        }
        // Readable設定を確認
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"テクスチャ '{texture.name}' のReadable設定を有効化しました。");
        }
        else
        {
            Debug.Log($"テクスチャ '{texture.name}' はすでにReadableです。");
        }
        return texture;
    }

    // テクスチャを保存する
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

