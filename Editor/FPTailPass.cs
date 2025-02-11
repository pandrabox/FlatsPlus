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


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPTailDebug
    {
        [MenuItem("PanDbg/FPTail")]
        public static void FPTail_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                if (!a.name.Contains("flat"))
                {
                    LowLevelDebugPrint($@"skip {a.name}");
                    continue;
                }
                new FPTailMain(a);

            }
        }
    }
#endif

    internal class FPTailPass : Pass<FPTailPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FPTailMain(ctx.AvatarDescriptor);
        }
    }

    public class FPTailMain
    {
        private FPTail[] _FPTails;
        private GameObject _tail;
        private FlatsProject prj;
        private VRCPhysBone _tailPB;
        private BlendTreeBuilder _bb;
        public FPTail TailConfig => _FPTails[0];

        public FPTailMain(VRCAvatarDescriptor desc)
        {
            _FPTails = desc.GetComponentsInChildren<FPTail>();
            if (_FPTails.Length == 0) return;
            prj = new FlatsProject(desc).SetSuffixMode(false);
            _tail = prj.ArmatureTransform.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == prj.TailName)?.gameObject;
            if (_tail == null) { LowLevelDebugPrint("Tailがみつかりませんでした"); return; }

            _tailPB = _tail.GetComponent<VRCPhysBone>();
            if (_tailPB == null) _tailPB = _tail.AddComponent<VRCPhysBone>();
            _tailPB.isAnimated = true;

            _bb = new BlendTreeBuilder(prj, false, "Tail", targetObj: _tail);
            _bb.RootDBT(() => {
                CreateScale();
            });
            CreateSwing();
        }

        private void CreateScale()
        {
            Vector3 tail0NormalSize = _tail.transform.localScale;
            Vector3 tail0BigParam = Vector3.one * prj.TailScaleLimit0 * TailConfig.SizeMax;
            if (prj.TailScaleXLimit0 != -1) tail0BigParam.x = prj.TailScaleXLimit0;
            Vector3 tail0Big = tail0NormalSize.HadamardProduct(tail0BigParam);

            Transform tail1 = GetDirectChildren(_tail).FirstOrDefault(child => child.name.ToLower().Contains("tail"));
            Vector3 tail1NormalSize = tail1.transform.localScale;
            Vector3 tail1BigParam = Vector3.one * prj.TailScaleLimit1;
            Vector3 tail1Big = tail1NormalSize.HadamardProduct(tail1BigParam);

            AnimationClipsBuilder ac = new AnimationClipsBuilder();
            float smallVal = TailConfig.SizeMin;
            for (int i = 0; i < 3; i++)
            {
                Axis axis = (Axis)i;
                string axisName = axis.ToString().ToLower();
                ac.Clip("Small")
                    .Bind("", typeof(Transform), $"m_LocalScale.{axisName}")
                    .Const2F(smallVal)
                    .Bind(tail1?.name, typeof(Transform), $"m_LocalScale.{axisName}")
                    .Const2F(tail1NormalSize[i]);
                ac.Clip("Normal")
                    .Bind("", typeof(Transform), $"m_LocalScale.{axisName}")
                    .Const2F(tail0NormalSize[i])
                    .Bind(tail1?.name, typeof(Transform), $"m_LocalScale.{axisName}")
                    .Const2F(tail1NormalSize[i]);
                ac.Clip("Big")
                    .Bind("", typeof(Transform), $"m_LocalScale.{axisName}")
                    .Const2F(tail0Big[i])
                    .Bind(tail1?.name, typeof(Transform), $"m_LocalScale.{axisName}")
                    .Const2F(tail1Big[i]);
            }

            _bb.NName("TailSize").Param("1").Add1D("FlatsPlus/Tail/SizeRx", () => {
                _bb.Param(0).AddMotion(ac.Outp("Small"));
                _bb.Param(0.5f).AddMotion(ac.Outp("Normal"));
                _bb.Param(1).AddMotion(ac.Outp("Big"));
            });

            MenuBuilder mb = new MenuBuilder(prj);
            mb.AddFolder("FlatsPlus", true).AddFolder("Tail", true).AddRadial("FlatsPlus/Tail/Size", "Size", .5f);

            var sync = prj.CreateComponentObject<PVnBitSync>("sync");
            sync.Set("FlatsPlus/Tail/Size", 4, PVnBitSync.nBitSyncMode.FloatMode, TailConfig.SizePerfectSync);
        }

        private void CreateSwing()
        {
            float swingAngle = TailConfig.SwingAngle;
            float swingPeriod = TailConfig.SwingPeriod;

            var ac = new AnimationClipsBuilder();
            Quaternion[] sPos = new Quaternion[3]; //Eularだとぺちゃっとした変な動きになったがQuaternionだと奇麗だった
            sPos[0] = CalcQuaternionRotationY(_tail, swingAngle);
            sPos[1] = CalcQuaternionRotationY(_tail, -swingAngle);
            sPos[2] = CalcQuaternionRotationY(_tail, 0);
            ac.Clip($@"Swing").SetLoop(true);
            for (int i = 0; i < 4; i++)
            {
                Axis axis = (Axis)i;
                string axisName = axis.ToString().ToLower();
                ac.Clip($@"Swing")
                    .Bind("", typeof(Transform), $"m_LocalRotation.{axisName}")
                    .Smooth(
                        swingPeriod * 0, sPos[0].GetAxis(axis)
                        , swingPeriod * 1, sPos[1].GetAxis(axis)
                        , swingPeriod * 2, sPos[0].GetAxis(axis)
                    )
                    .SetAllFlat();
                ac.Clip($@"Stop").Bind("", typeof(Transform), $"m_LocalRotation.{axisName}").Const2F(sPos[2].GetAxis(axis));
            }

            var ab = new AnimatorBuilder("TailSwing").AddLayer();
            ab.SetMotion(ac.Outp("Stop"));
            ab.AddState("on", ac.Outp("Swing"))
                .TransToCurrent(ab.InitialState, new AnimatorBuilder.TransitionInfo(false, 0, true, 1.5f, 0))
                .AddCondition(AnimatorConditionMode.If, 1, "FlatsPlus/Tail/Swing", true);
            ab.BuildAndAttach(_tail, true);

            new MenuBuilder(prj).AddFolder(PRJNAME, true).AddFolder("Tail", true).AddToggle("FlatsPlus/Tail/Swing", 1, ParameterSyncType.Bool, "Swing", 1, false);
        }
    }
}