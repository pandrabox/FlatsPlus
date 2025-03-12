// Header
using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using static com.github.pandrabox.pandravase.editor.Util;

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
                new FPMakeEmoWork(a.FP());
            }
        }
    }
#endif

    public class FPMakeEmoWork : FlatsWork<FPMakeEmo>
    {
        private const int TEXTUREXADD = 0;
        private const int TILESIZE = 256;
        private PVGridUI _ui;
        private Faces _faces;

        private const string pName = "FlatsPlus/MakeEmo";
        private static string enable = $@"{pName}/Enable";
        private static string eyeLevel = $@"{pName}/EyeLevel";
        private static string mouth = $@"{pName}/Mouth";
        private static string mouthRx = $@"{pName}/MouthRx";
        private static string other = $@"{pName}/Other";
        private static string eyeselecting = $@"{pName}/EyeSelecting";
        private static string isChanged = $@"{pName}/IsChanged";

        public FPMakeEmoWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            _faces = new FaceMaker(_prj, TILESIZE).Faces;
            GetStructure();
            SetConfig();
            CreateGridUI();
            Control();
            CreateExMenu();
        }

        private void GetStructure()
        {
            _ui = _tgt.GetComponentInChildren<PVGridUI>();
            if (_ui == null)
            {
                LowLevelExeption("GridUIが見つかりませんでした。");
            }
        }

        private void SetConfig()
        {
            _ui.MenuOpacity = _tgt.MenuOpacity;
            _ui.MenuSize = _tgt.MenuSize;
            _ui.SelectColor = _tgt.SelectColor;
            _ui.Speed = _tgt.ScrollSpeed;
            _ui.DeadZone = _tgt.DeadZone;
        }

        // GridUIの作成
        private void CreateGridUI()
        {
            int x, y;
            List<Texture2D> eyeTextures = _faces.EyeTextures;
            int eyeCount = eyeTextures.Count;
            x = Mathf.CeilToInt(Mathf.Sqrt(eyeCount)) + TEXTUREXADD;
            y = Mathf.CeilToInt((float)eyeCount / x);
            _ui.ParameterName = "FlatsPlus/MakeEmo/GUI";
            _ui.xMax = x;
            _ui.yMax = y;
            _ui.ItemCount = eyeCount;
            LowLevelDebugPrint($"EyeCount:{eyeCount}, x:{x}, y:{y}");
            _ui.MainTex = PackTexture(eyeTextures, x, x * TILESIZE, y * TILESIZE, _tgt.BackGroundColor, true, _tgt.Margin);
        }

        //制御部
        private void Control()
        {
            var bb = new BlendTreeBuilder("MakeEmoForBody");
            bb.RootDBT(() =>
            {
                bb.Param(enable).AddD(() =>
                {
                    List<Face> eyes = _faces.Eyes;
                    bb.Param("1").Add1D(eyeLevel, () =>
                    {
                        bb.Param(0).AddMotion(eyes[0].OffClip);
                        bb.Param(1).Add1D(_ui.nRx, () =>
                        {
                            bb.Param(0).AddMotion(eyes[0].OffClip);
                            for (int i = 0; i < eyes.Count; i++)
                            {
                                bb.Param(i + 1).AddMotion(eyes[i].OnClip);
                            }
                        });
                    });
                    List<Face> mouths = _faces.Mouths;
                    bb.Param("1").Add1D(mouthRx, () =>
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
            });
            bb.Attach(_prj.RootObject);
            var bb2 = new BlendTreeBuilder("MakeEmoForDisp");
            bb2.RootDBT(() =>
            {
                bb2.Param("IsLocal").AddD(() =>
                {
                    //GUIの表示制御(ONOFF,MODE)
                    bb2.Param("1").Add1D(eyeselecting, () =>
                    {
                        bb2.Param(0).Add1D(enable, () =>
                        {
                            bb2.Param(0).AddAAP(_ui.IsEnable, 0);
                            bb2.Param(1).AddAAP(_ui.IsEnable, 1, _ui.IsMode0, 0);
                        });
                        bb2.Param(1).AddAAP(_ui.IsEnable, 1, _ui.IsMode0, 1);
                    });
                    bb2.Param("1").AddD(() =>
                    {
                        bb2.Param("1").FDiffChecker(_ui.n, isChanged, max: _ui.xMax * _ui.yMax);
                        bb2.Param("1").FDiffChecker(mouth, isChanged, max: _faces.Mouths.Count + 1);
                        bb2.Param("1").FDiffChecker(eyeLevel, isChanged);
                        foreach (var o in _faces.Others)
                        {
                            bb2.Param("1").FDiffChecker($"{other}/{o.Name}", isChanged);
                        }
                    });
                });
            });
            bb2.Attach(_tgt.gameObject);
            var ab = new AnimatorBuilder(pName).AddLayer();
            ab.AddState("Enablation").SetParameterDriver(enable, 1)
                .TransToCurrent(ab.InitialState)
                    .AddCondition(AnimatorConditionMode.Greater, .5f, isChanged)
                    .AddCondition(AnimatorConditionMode.Greater, 50, _prj.FrameCount)
                .TransFromCurrent(ab.InitialState).MoveInstant();
            var enablateState = ab.CurrentState;
            ab.AddState("Clear")
                .SetParameterDriver(enable, 0)
                .SetParameterDriver(eyeLevel, 1)
                .SetParameterDriver(mouth, 0)
                .SetParameterDriver(_ui.Inputx, 0)
                .SetParameterDriver(_ui.Inputy, 0);
            foreach (var o in _faces.Others)
            {
                ab.SetParameterDriver($"{other}/{o.Name}", 0);
            }
            ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.If, 1, _ui.Reset)
                .TransFromCurrent(ab.InitialState).MoveInstant();
            ab.Attach(_prj.PrjRootObj);

            _prj.SetFrameCounter();
        }

        // メニューの作成
        private void CreateExMenu()
        {
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/MakeEmo"))
                .AddToggle(enable, 1, ParameterSyncType.Bool, L("Menu/MakeEmo/Enable"), 0, false)
                .Add2Axis(_ui.Inputx, _ui.Inputy, eyeselecting, L("Menu/MakeEmo/Eye"), 0, 0, true).SetMessage(L("Menu/MakeEmo/Eye/Message"))
                .AddRadial(eyeLevel, L("Menu/MakeEmo/EyeLevel"), 1, false);
            mb.AddFolder(L("Menu/MakeEmo/Mouth"));
            var mouths = _faces.Mouths;
            mb.AddToggle(mouth, 0, ParameterSyncType.Int, L("Menu/MakeEmo/Mouth/None")).SetIco(_faces.VoidIco);
            for (int i = 0; i < mouths.Count; i++)
            {
                var name = mouths[i].Name;
                mb.AddToggle(mouth, i + 1, ParameterSyncType.Int, name).SetIco(mouths[i].Tex);
            }
            _prj.VirtualSync(mouth, TransmissionBit(mouths.Count), PVnBitSync.nBitSyncMode.IntMode);
            mb.ExitFolder();
            mb.AddFolder(L("Menu/MakeEmo/Other")).SetMessage(L("Menu/MakeEmo/Other/Message"));
            foreach (var o in _faces.Others)
            {
                mb.AddToggle($"{other}/{o.Name}", localOnly: false).SetIco(o.Tex);
            }
            mb.ExitFolder();
            mb.AddButton(_ui.Reset, L("Menu/MakeEmo/Reset")).SetMessage(L("Menu/MakeEmo/Clear"));
        }
    }
}