using UnityEditor;
using UnityEngine;
using System.IO;
using com.github.pandrabox.pandravase.editor;
using static com.github.pandrabox.pandravase.runtime.Util;
using System;
using com.github.pandrabox.pandravase.runtime;
using static com.github.pandrabox.pandravase.runtime.TextureUtil;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;


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
            PackTest();
        }
    }

    private void PackTest()
    {
        using (var capture = new PanCapture(BGColor: new Color(1, 1, 1, 1), margin:10, padding:10, width: 170))
        {
            var projectName = "FlatsPlus";
            PandraProject _prj = new PandraProject(projectName, ProjectTypes.VPM);
            var strImg = capture.TextToImage($"{_prj.ProjectName}\n\r{_prj.VPMVersion}");
            List<Texture2D> textures = new List<Texture2D>();
            for (int i = 1; i <= 6; i++)
            {
                textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>($@"Packages/com.github.pandrabox.flatsplus/Assets/Ico/Ico/i{i}.png"));
            }
            textures.Add(strImg);
            Texture2D BG = PackTexture(textures, 3, 170 * 3);
            SaveTexture(BG, "Assets/ico2.png");
        }
    }
}
