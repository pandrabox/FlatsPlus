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
    public class FlatsPlusLightDebug
    {
        [MenuItem("PanDbg/FlatsPlusLight")]
        public static void FlatsPlusLight_Debug()
        {
            SetDebugMode(true);
            new FlatsPlusLightMain(TopAvatar);
        }
    }
#endif

    internal class FlatsPlusLightPass : Pass<FlatsPlusLightPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FlatsPlusLightMain(ctx.AvatarDescriptor);
        }
    }

    public class FlatsPlusLightMain
    {
        public FlatsPlusLightMain(VRCAvatarDescriptor desc)
        {
            FlatsProject prj = new FlatsProject(desc).SetSuffixMode(false);
            FlatsPlusLight fpLight = desc.GetComponentInChildren<FlatsPlusLight>();
            if (fpLight == null) return;
            string animFolder = $@"{prj.ProjectFolder}Assets/Light/Anim/";
            var bb = new BlendTreeBuilder("FlatsPlus/Light");
            bb.RootDBT(() => {
                bb.Param("1").Add1D("FlatsPlus/Light/LightModeRx", () =>
                {
                    bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                    bb.Param(1).Add1D("FlatsPlus/Light/IntensityRx", () =>
                    {
                        bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                        bb.Param(1).AddMotion($@"{animFolder}/Spot.anim");
                    });
                    bb.Param(2).Add1D("FlatsPlus/Light/IntensityRx", () =>
                    {
                        bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                        bb.Param(1).AddMotion($@"{animFolder}/Area.anim");
                    });
                });
            });
            bb.Attach(fpLight.gameObject);
            var mSync = prj.VirtualSync("FlatsPlus/Light/LightMode", 2, PVnBitSync.nBitSyncMode.IntMode, toggleSync:true);
            prj.VirtualSync("FlatsPlus/Light/Intensity", 4, PVnBitSync.nBitSyncMode.FloatMode, fpLight.IntensityPerfectSync);

            new MenuBuilder(prj).AddFolder("FlatsPlus", true).AddFolder("Light")
                .AddToggle("FlatsPlus/Light/LightMode", 1, ParameterSyncType.Int, "Spot")
                .AddToggle("FlatsPlus/Light/LightMode", 2, ParameterSyncType.Int, "Area")
                .AddRadial("FlatsPlus/Light/Intensity", "Intensity", .5f)
                .AddToggle(mSync.SyncParameter, 1, ParameterSyncType.Bool, "Global");
        }
    }
}