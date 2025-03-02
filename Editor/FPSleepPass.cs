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
    public class FPSleepDebug
    {
        [MenuItem("PanDbg/FPSleep")]
        public static void FPSleep_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new FPSleepMain(a);
            }
        }
    }
#endif

    internal class FPSleepPass : Pass<FPSleepPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FPSleepMain(ctx.AvatarDescriptor);
        }
    }

    public class FPSleepMain
    {
        private FPSleep _tgt;
        FlatsProject _prj;
        private static string __prefix = "FlatsPlus/Sleep";
        //private static string __tempolaryPose = $@"{__prefix}/TempolaryPose";
        private static string __trackingControl = $@"{__prefix}/TrackingControl";
        private static string __locomotionControl = $@"{__prefix}/LocomotionControl";
        private static string __sw = $@"{__prefix}/SW";
        private static string __height = $@"{__prefix}/Height";
        private static string __heightIsDiff = $@"{__prefix}/HeightIsDiff";
        //private static string __eyeMatch = $@"{__prefix}/EyeMatch";
        private static string __moveLock = $@"{__prefix}/MoveLock";
        private static string __poseLockAnim = $@"{__prefix}/PoseLockAnim";
        private static string __callPoseClipper = $@"CW/PC/Set/All";


        public FPSleepMain(VRCAvatarDescriptor desc)
        {
            _tgt = desc.GetComponentInChildren<FPSleep>();
            if (_tgt == null) return;
            _prj = new FlatsProject(desc);
            CreateControl();
            CreateMenu();
        }

        private void CreateControl()
        {
            var bb = new BlendTreeBuilder(__prefix);
            bb.RootDBT(() => {
                bb.Param(__sw).AddD(() =>
                {
                    bb.Param("1").FDiffChecker(__height, __heightIsDiff);
                });
            });
            bb.Attach(_prj.PrjRootObj);

            var ab = new AnimatorBuilder(__prefix);
            //{
            //    ab.AddLayer(__tempolaryPose);
            //    ab.AddState("OFF").SetTemporaryPoseSpace(false);
            //    var offState = ab.CurrentState;
            //    ab.TransToCurrent(ab.InitialState)
            //        .AddCondition(AnimatorConditionMode.Greater, .5f, __sw, true);
            //    ab.AddState("ON").SetTemporaryPoseSpace(true);
            //    ab.TransFromCurrent(offState).AddCondition(AnimatorConditionMode.Greater, .5f, __heightIsDiff);
            //    ab.TransToCurrent(offState).AddCondition(AnimatorConditionMode.Greater, .5f, __eyeMatch, true);
            //}
            {
                ab.AddLayer(__trackingControl);
                ab.SetTrackingControl(true);

                ab.AddState("Body").SetTrackingControl(false, true, true, true);
                var offState = ab.CurrentState;
                ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, .5f, __sw, true);

                ab.AddState("ALLAnim").SetTrackingControl(false);
                ab.TransToCurrent(offState).AddCondition(AnimatorConditionMode.Greater, .5f, __poseLockAnim, true);
                ab.TransFromCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Less, .5f, __sw);
            }
            {
                ab.AddLayer(__locomotionControl);
                ab.SetLocomotionControl(true);
                ab.AddState("FALSE").SetLocomotionControl(false);
                ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, .5f, __moveLock, true);
            }

            ab.Attach(_prj.PrjRootObj);
        }

        private void CreateMenu()
        {
            var mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder("Sleep").SetMessage("睡眠");
            mb.AddToggle(__sw, localOnly: false).SetMessage("睡眠モード ON");
            //mb.AddToggle(__eyeMatch);
            mb.AddToggle(__moveLock).SetMessage("移動ロック ON");
            mb.AddRadial(__height,defaultVal:.5f, localOnly: false).SetMessage("睡眠高さ");
            mb.AddToggle(__poseLockAnim, 1, ParameterSyncType.Bool, "Lock:Anim").SetMessage("アニメーションで全身をロック");
            mb.AddToggle(__callPoseClipper, 1, ParameterSyncType.Bool, "Lock:Current").SetMessage("現在の姿勢で全身をロック");
        }
    }
}