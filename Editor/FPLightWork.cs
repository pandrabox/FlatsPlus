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
using static com.github.pandrabox.flatsplus.editor.Localizer;



namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPLightDebug
    {
        [MenuItem("PanDbg/FlatsPlusLight")]
        public static void FlatsPlusLight_Debug()
        {
            SetDebugMode(true);
            new FPLightWork(TopAvatar.FP());
        }
    }
#endif


    public class FPLightWork : FlatsWork<FPLight>
    {
        private string _animFolder;
        private static string __prefix = "FlatsPlus/Light";
        private static string __LightModeRx = $@"{__prefix}/LightModeRx";
        private static string __IntensityRx = $@"{__prefix}/IntensityRx";

        public FPLightWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            _animFolder = $@"{_prj.ProjectFolder}Assets/Light/Anim/";
            CreateControl();
            CreateMenu();
        }
        private void CreateControl()
        {
            var bb = new BlendTreeBuilder(__prefix);
            bb.RootDBT(() => {
                bb.Param("1").Add1D(__LightModeRx, () =>
                {
                    bb.Param(0).AddMotion($@"{_animFolder}/Off.anim");
                    bb.Param(1).Add1D(__IntensityRx, () =>
                    {
                        bb.Param(0).AddMotion($@"{_animFolder}/Off.anim");
                        bb.Param(1).AddMotion($@"{_animFolder}/Spot.anim");
                    });
                    bb.Param(2).Add1D(__IntensityRx, () =>
                    {
                        bb.Param(0).AddMotion($@"{_animFolder}/Off.anim");
                        bb.Param(1).AddMotion($@"{_animFolder}/Area.anim");
                    });
                });
            });
            bb.Attach(_tgt.gameObject);
        }
        private void CreateMenu()
        {
            var mSync = _prj.VirtualSync("FlatsPlus/Light/LightMode", 2, PVnBitSync.nBitSyncMode.IntMode, toggleSync: true);
            _prj.VirtualSync("FlatsPlus/Light/Intensity", 4, PVnBitSync.nBitSyncMode.FloatMode, _tgt.IntensityPerfectSync);
            new MenuBuilder(_prj).AddFolder("FlatsPlus", true).AddFolder(L("Menu/Light"))
                .AddToggle("FlatsPlus/Light/LightMode", 1, ParameterSyncType.Int, L("Menu/Light/Spot")).SetMessage(L("Menu/Light/Spot/Detail"))
                .AddToggle("FlatsPlus/Light/LightMode", 2, ParameterSyncType.Int, L("Menu/Light/Area")).SetMessage(L("Menu/Light/Area/Detail"))
                .AddRadial("FlatsPlus/Light/Intensity", L("Menu/Light/Intensity"), .5f)
                .AddToggle(mSync.SyncParameter, 1, ParameterSyncType.Bool, L("Menu/Light/Global")).SetMessage("Menu/Light/Global/Detail");
        }
    }
}