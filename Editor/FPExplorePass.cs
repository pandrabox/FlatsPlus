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
using UnityEngine.Animations;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPExploreDebug
    {
        [MenuItem("PanDbg/FPExplore")]
        public static void FPExplore_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new FPExploreMain(a);
            }
        }
        [MenuItem("PanDbg/FPExplore_DBCreate")]
        public static void FPExplore_Debug_DBCreate()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new FPExploreMain(a, true);
            }
        }
    }
#endif

    internal class FPExplorePass : Pass<FPExplorePass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FPExploreMain(ctx.AvatarDescriptor);
        }
    }

    public class FPExploreMain
    {
        private FPExplore _FPExplore;
        private FlatsProject _prj;

        public FPExploreMain(VRCAvatarDescriptor desc)
        {
            _FPExplore = desc.GetComponentInChildren<FPExplore>();
            if (_FPExplore == null) return;
            _prj = new FlatsProject(desc);

            var ac = new AnimationClipsBuilder();
            ac.AddClip("Color0")
            var bb = new BlendTreeBuilder("Explore");
            bb.RootDBT(() => {
                bb.Param("1").Add1D("FlatsPlus/Explore/SW", () => {
                    bb.Param(0).AddMotion(ac.OffAnim("Pin"));
                    bb.Param(1).AddMotion(ac.OnAnim("Pin"));
                });
                bb.Param("1").Add1D("FlatsPlus/Explore/Color", () =>
                {
                    bb.Param(0).AddMotion(ac.OffAnim("Pin"));
                    bb.Param(1+1/9f).AddMotion(ac.OnAnim("Pin"));
                });
            });
        }
    }
}