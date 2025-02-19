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

            Transform pinTransform = _FPExplore.FindEx("Pin");
            if (pinTransform == null) return;
            pinTransform.localPosition = new Vector3(0, _prj.PinY, 0);

            var ac = new AnimationClipsBuilder();
            ac.Clip("Color0").Bind("Pin", typeof(MeshRenderer), "material._Hue").Const2F(0);
            ac.Clip("Color1").Bind("Pin", typeof(MeshRenderer), "material._Hue").Const2F(1);
            ac.Clip("PinOff").IsVector3((x) => { x.Bind("Pin", typeof(Transform), "m_LocalScale.@a").Const2F(0); })
                .Bind("Pin", typeof(GameObject), "m_IsActive").Const2F(0);
            ac.Clip("PinOn").IsVector3((x) => { x.Bind("Pin", typeof(Transform), "m_LocalScale.@a").Const2F(999999); })
                .Bind("Pin", typeof(GameObject), "m_IsActive").Const2F(1);

            var bb = new BlendTreeBuilder("Explore");
            bb.RootDBT(() =>
            {
                bb.Param("1").Add1D("FlatsPlus/Explore/SW", () =>
                {
                    bb.Param(0).AddMotion(ac.Outp("PinOff"));
                    bb.Param(1).AddMotion(ac.Outp("PinOn"));
                });
                bb.Param("1").Add1D("FlatsPlus/Explore/ColorRx", () =>
                {
                    bb.Param(0).AddMotion(ac.Outp("Color0"));
                    bb.Param(1 + 1 / 9f).AddMotion(ac.Outp("Color1"));
                });
            });
            bb.Attach(_FPExplore.gameObject);

            new MenuBuilder(_prj).AddFolder("FlatsPlus", true).AddFolder("Explore")
                .AddToggle("FlatsPlus/Explore/SW", 1, ParameterSyncType.Bool, "Pin", 0, false)
                .AddRadial("FlatsPlus/Explore/Color", "Color");

            _prj.VirtualSync("FlatsPlus/Explore/Color", 3, PVnBitSync.nBitSyncMode.FloatMode, true);
        }
    }
}