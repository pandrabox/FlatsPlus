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

        private const int ICONUM = 7;
        private const int MENUMAX = 8; // iconum+1(VerView)
        GameObject _callPlane;
        GameObject _verViewObj => _menuItems[MENUMAX-1].gameObject;
        ModularAvatarMenuItem[] _menuItems = new ModularAvatarMenuItem[MENUMAX];

        sealed protected override void OnConstruct()
        {
            GetStructure();
            for (int i = 0; i < ICONUM; i++)
            {
                LowLevelDebugPrint($@"アイコンの置換を行います{i}");
                _menuItems[i].Control.icon = ResizeTexture(_tgt.textures[i], 256);
            }
            ReplacePackTexture();
            if (!_tgt.VerView) GameObject.DestroyImmediate(_verViewObj);
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

        /// <summary>
        /// プレハブの構造確認・取得
        /// </summary>
        /// <returns></returns>
        private bool GetStructure()
        {
            Transform Offset = _tgt.transform.Find("Obj/Head/Offset").NullCheck("Offset");
            _callPlane = Offset.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == "CallPlate").gameObject.NullCheck("_callPlane");
            var menuItems = _tgt.transform.GetComponentsInChildren<ModularAvatarMenuItem>().NullCheck("menuItems");
            var icoMenuItems = menuItems.Where(x => x.Control.parameter.name == "FlatsPlus/Ico/IcoType").ToList();
            for (int i = 0; i < MENUMAX; i++)
            {
                var item = icoMenuItems.FirstOrDefault(x => x.Control.value == i + 1).NullCheck("menuIco" + i);
                _menuItems[i] = item;
            }
            LowLevelDebugPrint("Structureの取得に成功しました");
            return true;
        }
    }
}