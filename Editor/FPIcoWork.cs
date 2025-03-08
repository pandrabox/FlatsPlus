using UnityEditor;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.ndmf.util;
using nadena.dev.ndmf;
using com.github.pandrabox.pandravase.runtime;
using static com.github.pandrabox.pandravase.editor.Util;
using static com.github.pandrabox.pandravase.editor.Localizer;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using com.github.pandrabox.flatsplus.runtime;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using System.Text.RegularExpressions;
using com.github.pandrabox.pandravase.editor;

namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FlatsPlusIcoDebug
    {
        [MenuItem("PanDbg/FlatsPlusIco")]
        public static void FlatsPlusIco_Debug()
        {
            SetDebugMode(true);
            var fp = new FlatsProject(TopAvatar);
            new FPIcoWork(fp);
        }
    }
#endif

    public class FPIcoWork : FlatsWork<FPIco>
    {
        public FPIcoWork(FlatsProject fp) : base(fp) { }

        GameObject _callPlane;

        sealed protected override void OnConstruct()
        {
            GetStructure();
            ReplacePackTexture();
            CreateMenu();
        }

        private void ReplacePackTexture()
        {
            using (var capture = new PanCapture(BGColor: new Color(1, 1, 1, 1), margin: 10, padding: 10, width: 170))
            {
                FlatsProject prj = new FlatsProject(_desc);
                var strImg = capture.TextToImage($"{prj.ProjectName}\n\r{prj.VPMVersion}");
                List<Texture2D> textures = _tgt.textures.ToList();
                textures.Add(strImg);
                Texture2D packTexture = PackTexture(textures, 3, 170 * 3);
                try
                {
                    _callPlane.GetComponent<Renderer>().material.mainTexture = packTexture;
                }
                catch
                {
                    LowLevelDebugPrint("Textureの置換に失敗しました(設定先mainTextureの取得に失敗しました)", false);
                }
            }
        }
        private void CreateMenu()
        {
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Ico"));
            string name;
            for (int i = 1; i < 9; i++)
            {
                if (i < 7)
                {
                    name = (i).ToString();
                    mb.AddToggle($"FlatsPlus/Ico/IcoType", i, ParameterSyncType.Int, name).SetIco(_tgt.textures[i-1]);
                }
                else if (i == 7)
                {
                    name = L("Menu/Ico/Resonance");
                    mb.AddToggle($"FlatsPlus/Ico/IcoType", i, ParameterSyncType.Int, name).SetMessage("Menu/Ico/Resonance/Detail").SetIco(_tgt.textures[i-1]);
                }
                else if (_tgt.VerView && i == 8)
                {
                    name = L("Menu/Ico/VerView");
                    Texture2D vvico= AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.github.pandrabox.flatsplus/Assets/Ico/Ico/i8.png");
                    mb.AddButton("FlatsPlus/Icon/VerView", i, ParameterSyncType.Int, name).SetMessage("Menu/Ico/VerView/Detail").SetIco(vvico);
                }
            }
        }

        /// <summary>
        /// プレハブの構造確認・取得
        /// </summary>
        /// <returns></returns>
        private bool GetStructure()
        {
            Transform Offset = _tgt.transform.Find("Obj/Head/Offset").NullCheck("Offset");
            _callPlane = Offset.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == "CallPlate").gameObject.NullCheck("_callPlane");
            return true;
        }
    }
}