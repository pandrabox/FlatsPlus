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


namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class IcoDebug
    {
        [MenuItem("PanDbg/Ico")]
        public static void Ico_Debug()
        {
            SetDebugMode(true);
            new IcoMain(TopAvatar);
        }
    }
#endif

    internal class FlatsPlusIcoPass : Pass<FlatsPlusIcoPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new IcoMain(ctx.AvatarDescriptor);
        }
    }

    public class IcoMain
    {
        private const int iconum=7;
        GameObject _callPlane;
        VRCAvatarDescriptor _desc;
        ModularAvatarMenuItem[] _menuItems = new ModularAvatarMenuItem[iconum];
        public IcoMain(VRCAvatarDescriptor desc)
        {
            LowLevelDebugPrint("IcoMain");
            _desc = desc;
            var tgt = desc.transform.GetComponentsInChildren<FlatsPlusIco>();
            if (tgt.Length == 0) return;
            foreach (var t in tgt)
            {
                if (!GetStructure(t)) continue;
                for(int i = 0; i < iconum; i++)
                {
                    LowLevelDebugPrint($@"アイコンの置換を行います{i}");
                    _menuItems[i].Control.icon = ResizeTexture(t.textures[i], 256);
                }
                ReplacePackTexture(t);
            }
        }

        private void ReplacePackTexture(FlatsPlusIco t)
        {
            using (var capture = new PanCapture(BGColor: new Color(1, 1, 1, 1), margin: 10, padding: 10, width: 170))
            {
                PandraProject prj = FlatsPlusProject(_desc);
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
            if(menuItems.Length < iconum)
            {
                LowLevelDebugPrint($@"Structureの取得に失敗しました:menuItems(Length={menuItems.Length})", false);
                return false;
            }
            var icoMenuItems = menuItems.Where(x => x.Control.parameter.name == "FlatsPlus/Ico/IcoType").ToList();
            for (int i = 0; i < iconum; i++)
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