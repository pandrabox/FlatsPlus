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
            new FlatsPlusIcoMain(TopAvatar);
        }
    }
#endif

    internal class FlatsPlusIcoPass : Pass<FlatsPlusIcoPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FlatsPlusIcoMain(ctx.AvatarDescriptor);
        }
    }

    public class FlatsPlusIcoMain
    {
        private const int ICONUM=7;
        private const int MENUMAX = 8; // iconum+1(VeryView)
        GameObject _callPlane;
        GameObject _verViewObj => _menuItems[MENUMAX-1].gameObject;
        VRCAvatarDescriptor _desc;
        ModularAvatarMenuItem[] _menuItems = new ModularAvatarMenuItem[MENUMAX];
        public FlatsPlusIcoMain(VRCAvatarDescriptor desc)
        {
            _desc = desc;
            var tgt = desc.transform.GetComponentsInChildren<FlatsPlusIco>();
            if (tgt.Length == 0) return;
            foreach (var t in tgt)
            {
                if (!GetStructure(t)) continue;
                for(int i = 0; i < ICONUM; i++)
                {
                    LowLevelDebugPrint($@"アイコンの置換を行います{i}");
                    _menuItems[i].Control.icon = ResizeTexture(t.textures[i], 256);
                }
                ReplacePackTexture(t);
                if (!t.VerView) GameObject.DestroyImmediate(_verViewObj);
            }
        }

        private void ReplacePackTexture(FlatsPlusIco t)
        {
            using (var capture = new PanCapture(BGColor: new Color(1, 1, 1, 1), margin: 10, padding: 10, width: 170))
            {
                FlatsProject prj = new FlatsProject(_desc);
                var strImg = capture.TextToImage($"{prj.ProjectName}\n\r{prj.VPMVersion}");
                List<Texture2D> textures = t.textures.ToList();
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
        /// <param name="root">FlatsPlusIcoプレハブ</param>
        /// <returns></returns>
        private bool GetStructure(FlatsPlusIco root)
        {
            Transform Offset = root.transform.Find("Obj/Head/Offset");
            if (Offset == null)
            {
                LowLevelDebugPrint("Structureの取得に失敗しました:Offset", false);
                return false;
            }
            _callPlane = Offset.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == "CallPlate")?.gameObject;
            if (_callPlane == null)
            {
                LowLevelDebugPrint("Structureの取得に失敗しました:CallPlate", false);
                return false;
            }

            Array.Clear(_menuItems, 0, _menuItems.Length);
            var menuItems = root.transform.GetComponentsInChildren<ModularAvatarMenuItem>();
            if(menuItems.Length < ICONUM)
            {
                LowLevelDebugPrint($@"Structureの取得に失敗しました:menuItems(Length={menuItems.Length})", false);
                return false;
            }
            var icoMenuItems = menuItems.Where(x => x.Control.parameter.name == "FlatsPlus/Ico/IcoType").ToList();
            for (int i = 0; i < MENUMAX; i++)
            {
                var item = icoMenuItems.FirstOrDefault(x => x.Control.value == i + 1);
                if (item == null)
                {
                    LowLevelDebugPrint($@"Structureの取得に失敗しました:MenuItem{i}(Param:{i+1})", false);
                    return false;
                }
                _menuItems[i] = item;
            }
            LowLevelDebugPrint("Structureの取得に成功しました");
            return true;
        }
    }
}