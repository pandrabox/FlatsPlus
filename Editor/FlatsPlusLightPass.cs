﻿using UnityEditor;
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
            PandraProject prj = FlatsPlusProject(desc).SetSuffixMode(false);
            FlatsPlusLight[] lights = desc.GetComponentsInChildren<FlatsPlusLight>();
            string animFolder = $@"{prj.ProjectFolder}Assets/Light/Anim/";
            foreach (FlatsPlusLight light in lights)
            {
                GameObject tgt=light.gameObject;
                var bb = new BlendTreeBuilder(prj, false, "FlatsPlus/Light", targetObj: tgt);
                bb.RootDBT(() => {
                    bb.Param("1").Add1D("IsLocal", () =>
                    {
                        bb.Param(0).Add1D("FlatsPlus/Light/Rx", () =>
                        {
                            bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                            bb.Param(1).Add1D("FlatsPlus/Light/IntensityRx", () =>
                            {
                                bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                                bb.Param(15).AddMotion($@"{animFolder}/Spot.anim");
                            });
                            bb.Param(2).Add1D("FlatsPlus/Light/IntensityRx", () =>
                            {
                                bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                                bb.Param(15).AddMotion($@"{animFolder}/Area.anim");
                            });
                            bb.Param(3).AddMotion($@"{animFolder}/Off.anim");
                        });
                        bb.Param(1).AddD(() =>
                        {
                            bb.Param("FlatsPlus/Light/Global").AssignmentBy1D("FlatsPlus/Light/LightMode", 0, 2, "FlatsPlus/Light/Tx");
                            bb.Param("1").AssignmentBy1D("FlatsPlus/Light/Intensity", 0, 1, "FlatsPlus/Light/IntensityTx", 0, 15);
                            bb.Param("1").Add1D("FlatsPlus/Light/LightMode", () =>
                            {
                                bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                                bb.Param(1).Add1D("FlatsPlus/Light/Intensity", () =>
                                {
                                    bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                                    bb.Param(1).AddMotion($@"{animFolder}/Spot.anim");
                                });
                                bb.Param(2).Add1D("FlatsPlus/Light/Intensity", () =>
                                {
                                    bb.Param(0).AddMotion($@"{animFolder}/Off.anim");
                                    bb.Param(1).AddMotion($@"{animFolder}/Area.anim");
                                });                                
                            });
                        });
                    });
                });
            }
        }
    }
}