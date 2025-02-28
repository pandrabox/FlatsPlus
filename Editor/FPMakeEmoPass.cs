// Header
#region
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
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.Dynamics;
using System.Globalization;
using System.ComponentModel;
using static com.github.pandrabox.pandravase.editor.AnimatorLayerAnalyzer;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
#endregion

namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPMakeEmoDebug
    {
        [MenuItem("PanDbg/FPMakeEmo")]
        public static void FPMakeEmo_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new FPMakeEmoMain(a);
            }
        }
    }
#endif

    internal class FPMakeEmoPass : Pass<FPMakeEmoPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FPMakeEmoMain(ctx.AvatarDescriptor);
        }
    }

    public class FPMakeEmoMain
    {
        private const int TEXTUREXADD = 1;
        private const int TILESIZE = 256;
        private FPMakeEmo _FPMakeEmo;
        private PVGridUI _gridUI;
        private FlatsProject _prj;
        private Faces _faces;

        private const string pName = "FlatsPlus/MakeEmo";
        private static string enable = $@"{pName}/Enable";
        private static string eyeLevel = $@"{pName}/EyeLevel";
        private static string mouth = $@"{pName}/Mouth";
        private static string other = $@"{pName}/Other";
        private static string reset = $@"{pName}/Reset";
        private static string eyeselecting = $@"{pName}/EyeSelecting";

        public FPMakeEmoMain(VRCAvatarDescriptor desc)
        {
            _FPMakeEmo = desc.GetComponentInChildren<FPMakeEmo>();
            if (_FPMakeEmo == null) return;
            _prj = new FlatsProject(desc);
            _faces = new FaceMaker(_prj, TILESIZE).Faces;
            GetStructure();
            CreateGridUI();
            Control();
            CreateExMenu();
        }

        private void GetStructure()
        {
            _gridUI = _FPMakeEmo.GetComponentInChildren<PVGridUI>();
            if(_gridUI == null)
            {
                LowLevelExeption("GridUIが見つかりませんでした。");
            }
        }

        // GridUIの作成
        private void CreateGridUI()
        {
            int x, y;
            List<Texture2D> eyeTextures = _faces.EyeTextures;
            int eyeCount = eyeTextures.Count;
            x = Mathf.CeilToInt(Mathf.Sqrt(eyeCount)) + TEXTUREXADD;
            y = Mathf.CeilToInt((float)eyeCount / x);
            _gridUI.ParameterName = "FlatsPlus/MakeEmo/GUI";
            _gridUI.xMax = x;
            _gridUI.yMax = y;
            LowLevelDebugPrint($"EyeCount:{eyeCount}, x:{x}, y:{y}");
            _gridUI.MainTex = PackTexture(eyeTextures, x, x * TILESIZE, y * TILESIZE);
        }

        //制御部
        private void Control()
        {
            var bb = new BlendTreeBuilder("MakeEmo");
            bb.RootDBT(() =>
            {
                bb.Param(enable).AddD(() =>
                {
                    List<Face> eyes = _faces.Eyes;
                    bb.Param("1").Add1D(eyeLevel, () =>
                    {
                        bb.Param(0).AddMotion(eyes[0].OffClip);
                        bb.Param(1).Add1D(_gridUI.n, () =>
                        {
                            bb.Param(0).AddMotion(eyes[0].OffClip);
                            for (int i = 0; i < eyes.Count; i++)
                            {
                                bb.Param(i + 1).AddMotion(eyes[i].OnClip);
                            }
                        });
                    });
                    List<Face> mouths = _faces.Mouths;
                    bb.Param("1").Add1D(mouth, () =>
                    {
                        bb.Param(0).AddMotion(mouths[0].OffClip);
                        for (int i = 0; i < mouths.Count; i++)
                        {
                            bb.Param(i + 1).AddMotion(mouths[i].OnClip);
                        }
                    });
                    foreach (var face in _faces.Others)
                    {
                        var name = face.Name;
                        bb.Param("1").Add1D($@"{other}/{name}", () =>
                        {
                            bb.Param(0).AddMotion(face.OffClip);
                            bb.Param(1).AddMotion(face.OnClip);
                        });
                    }
                    bb.Param("1").AddAAP("FlatsPlus/Emo/Disable", 1);
                });
                bb.Param("1").Add1D(eyeselecting, () =>
                {
                    bb.Param(0).Add1D(enable, () =>
                    {
                        bb.Param(0).AddAAP(_gridUI.IsEnable, 0);
                        bb.Param(1).AddAAP(_gridUI.IsEnable, 1, _gridUI.IsMode0, 0);
                    });
                    bb.Param(1).AddAAP(_gridUI.IsEnable, 1, _gridUI.IsMode0, 1);
                });
            });
            bb.Attach(_FPMakeEmo.gameObject);
            var ab = new AnimatorBuilder(pName);
            ab.AddLayer().AddState("Clear")
                .SetParameterDriver(enable, 0)
                .SetParameterDriver(eyeLevel, 1)
                .SetParameterDriver(mouth, 0)
                .SetParameterDriver(_gridUI.Inputx, 0)
                .SetParameterDriver(_gridUI.Inputy, 0)
                .SetParameterDriver(_gridUI.Currentx, 0)
                .SetParameterDriver(_gridUI.Currenty, 0);
            foreach (var o in _faces.Others)
            {
                ab.SetParameterDriver($"{other}/{o.Name}", 0);
            }
            ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.If, 1, reset, true);
            ab.Attach(_prj.PrjRootObj);
        }

        // メニューの作成
        private void CreateExMenu()
        {
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus").AddFolder("MakeEmo").SetMessage("表情の作成・固定");
            mb.AddToggle(enable, 1, ParameterSyncType.Bool, "SW", 0, false).SetMessage("表情を固定", "固定を解除");
            mb.Add2Axis(_gridUI.Inputx, _gridUI.Inputy, eyeselecting, "Eye", 0, 0, true).SetMessage("Stickで表情を選択");
            mb.AddRadial(eyeLevel, "EyeLevel", 1, false).SetMessage("目設定の強さ");
            mb.AddFolder("Mouth").SetMessage("口の設定");
            var mouths = _faces.Mouths;
            for (int i = 0; i < mouths.Count; i++)
            {
                var name = mouths[i].Name;
                mb.AddToggle(mouth, i, ParameterSyncType.Int, name).SetIco(mouths[i].Tex);
            }
            _prj.VirtualSync(mouth, TransmissionBit(mouths.Count), PVnBitSync.nBitSyncMode.IntMode);
            mb.ExitFolder();
            mb.AddFolder("Other").SetMessage("その他の設定");
            foreach (var o in _faces.Others)
            {
                mb.AddToggle($"{other}/{o.Name}").SetIco(o.Tex);
            }
            mb.ExitFolder();
            mb.AddButton(reset).SetMessage("表情設定をクリア");
        }
    }
}