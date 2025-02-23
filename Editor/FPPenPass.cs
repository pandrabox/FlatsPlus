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
    public class FPPenDebug
    {
        [MenuItem("PanDbg/FPPen")]
        public static void FPPen_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new FPPenMain(a);
            }
        }
    }
#endif

    internal class FPPenPass : Pass<FPPenPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FPPenMain(ctx.AvatarDescriptor);
        }
    }

    public class FPPenMain
    {
        private FPPen[] _FPPen;
        private FlatsProject _prj;

        const float epsilon = 1.401298e-45F;
        const int COLORNUM = 8;
        const int OFFSET = 3;

        public FPPenMain(VRCAvatarDescriptor desc)
        {
            _FPPen = desc.GetComponentsInChildren<FPPen>();
            if (_FPPen == null || _FPPen.Length < 1) return;
            _prj = new FlatsProject(desc);
            float penAnimTime = .25f;

            var ac = new AnimationClipsBuilder();
            void Set(string name, bool erace, bool inkParticle, bool inkObj)
            {
                ac.Clip(name)
               .Bind("Obj/HandR/Offset/Eraser", typeof(SphereCollider), "m_Enabled").Const2F(erace ? 1 : 0)
               .Bind("Obj/HandR/Offset/Ink", typeof(ParticleSystem), "EmissionModule.enabled").Const2F(inkParticle ? 1 : 0)
               .Bind("Obj/HandR/Offset/Ink", typeof(GameObject), "m_IsActive").Const2F(inkObj ? 1 : 0);
            }
            Set("Clear", false, false, false);
            Set("OFF", false, false, true);
            Set("Erace", true, false, true);
            Set("Write", false, true, true);
            void Set2(string name, bool inverse, bool penVisible)
            {
                var rot = new Vector3(inverse ? 180 : 0, 0, 0);
                ac.Clip(name).IsVector3((x) => {
                    x.Bind("Obj/HandR/Offset/pen", typeof(Transform), "localEulerAnglesRaw.@a").Const2F(rot);
                })
                .Bind("Obj/HandR/Offset/pen", typeof(GameObject), "m_IsActive").Const2F(penVisible ? 1 : 0);
            }
            Set2("PenErace", true, true);
            Set2("PenOff", false, false);
            Set2("PenOn", false, true);
            Color[] penColors = new Color[COLORNUM]
            {
                new Color(1,1,1),
                new Color(0,0,0),
                new Color(1,0,0),
                new Color(1,1,0),
                new Color(0,1,0),
                new Color(0,1,1),
                new Color(0,0,1),
                new Color(1,0,1),
            };
            for (int i = 0; i < COLORNUM; i++)
            {
                Quaternion q = new Quaternion(1, 1, (float)i / COLORNUM, 0);
                ac.Clip($@"Color{i}").IsQuaternion((x) => {
                    x.Bind("Obj/HandR/Offset/pen", typeof(MeshRenderer), "material._MainTex_ST.@a").Const2F(q);
                    x.Bind("Obj/HandR/Offset/pen", typeof(MeshRenderer), "material._EmissionMap_ST.@a").Const2F(q);
                })
                .Color("Obj/HandR/Offset/Ink", typeof(ParticleSystem), "InitialModule.startColor.maxColor", penColors[i]);
            }

            var bb = new BlendTreeBuilder("FlatsPlus/Pen/DBT");
            bb.RootDBT(() => {
                bb.NName("CalcMem").Param("1").Add1D("FlatsPlus/Pen/Color", () =>
                {
                    bb.Param(0.5f / COLORNUM).AddAAP("FlatsPlus/Pen/Mem", 0);
                    bb.Param(0.5f / COLORNUM + 1).AddAAP("FlatsPlus/Pen/Mem", epsilon * COLORNUM);
                });
                bb.NName("CalcCom").Param("1").Add1D("FlatsPlus/Pen/Clear", () => {
                    bb.Param(0).Add1D("FlatsPlus/Pen/Mode", () => {
                        bb.Param(0).AddAAP("FlatsPlus/Pen/Com", 0);
                        bb.Param(1).AddAAP("FlatsPlus/Pen/Com", 1);
                        bb.Param(2).AddD(() =>
                        {
                            bb.Param("1").AddAAP("FlatsPlus/Pen/Com", OFFSET);
                            bb.Param("1").Add1D("FlatsPlus/Pen/Mem", () =>
                            {
                                bb.Param(0).AddAAP("FlatsPlus/Pen/Com", 0);
                                bb.Param(epsilon * COLORNUM).AddAAP("FlatsPlus/Pen/Com", COLORNUM);
                            });
                        });
                    });
                    bb.Param(1).AddAAP("FlatsPlus/Pen/Com", 2);
                });
                bb.NName("Mode").Param("1").Add1D("FlatsPlus/Pen/ComRx", () =>
                {
                    bb.Param(0).AddMotion(ac.Outp("OFF"));
                    bb.Param(1).Add1D("GestureRight", () =>
                    {
                        bb.Param(0.4f).AddMotion(ac.Outp("OFF"));
                        bb.Param(0.5f).AddMotion(ac.Outp("Erace"));
                        bb.Param(1.4f).AddMotion(ac.Outp("Erace"));
                        bb.Param(1.5f).AddMotion(ac.Outp("OFF"));
                    });
                    bb.Param(2).AddMotion(ac.Outp("Clear"));
                    bb.Param(OFFSET).Add1D("GestureRight", () =>
                    {
                        bb.Param(0.4f).AddMotion(ac.Outp("OFF"));
                        bb.Param(0.5f).AddMotion(ac.Outp("Write"));
                        bb.Param(1.4f).AddMotion(ac.Outp("Write"));
                        bb.Param(1.5f).AddMotion(ac.Outp("OFF"));
                    });
                });
                bb.NName("Color").Param("1").Add1D("FlatsPlus/Pen/ComRx", () =>
                {
                    for (int i = 0; i < COLORNUM; i++)
                    {
                        bb.Param(i + OFFSET).AddMotion(ac.Outp($@"Color{i}"));
                    }
                });
            });


            var ab = new AnimatorBuilder("FlatsPlus/Pen/Pose").AddLayer();
            ab.SetMotion(ac.Outp("PenOff"));
            ab.AddState("PenOn", ac.Outp("PenOn"))
                .TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, OFFSET - .5f, "FlatsPlus/Pen/ComRx")
                .TransFromCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Less, .5f, "FlatsPlus/Pen/ComRx");
            var onState = ab.CurrentState;
            ab.AddState("PenErace", ac.Outp("PenErace"))
                .TransToCurrent(ab.InitialState)
                    .AddCondition(AnimatorConditionMode.Greater, 0.5f, "FlatsPlus/Pen/ComRx")
                    .AddCondition(AnimatorConditionMode.Less, OFFSET - .5f, "FlatsPlus/Pen/ComRx")
                .TransFromCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Less, .5f, "FlatsPlus/Pen/ComRx")
                .TransFromCurrent(onState, transitionDuration: penAnimTime).AddCondition(AnimatorConditionMode.Greater, OFFSET - .5f, "FlatsPlus/Pen/ComRx");
            var eraceState = ab.CurrentState;
            ab.SetTransition(onState, eraceState, transitionDuration: penAnimTime)
                .AddCondition(AnimatorConditionMode.Greater, 0.5f, "FlatsPlus/Pen/ComRx")
                .AddCondition(AnimatorConditionMode.Less, OFFSET - .5f, "FlatsPlus/Pen/ComRx");

            foreach (var pen in _FPPen)
            {
                bb.Attach(pen.gameObject);
                ab.Attach(pen.gameObject);
            }

            _prj.VirtualSync("FlatsPlus/Pen/Com", 4, PVnBitSync.nBitSyncMode.IntMode, true);

            new MenuBuilder(_prj).AddFolder("FlatsPlus", true)
                .AddFolder("Pen").SetMessage("ペンで描画します。右手Fistで描画・削除")
                .AddToggle("FlatsPlus/Pen/Mode", 3, ParameterSyncType.Int, "Write").SetMessage("右手Fistで描画")
                .AddToggle("FlatsPlus/Pen/Mode", 1, ParameterSyncType.Int, "Erace").SetMessage("右手Fistで描画を削除")
                .AddButton("FlatsPlus/Pen/Clear", 1, ParameterSyncType.Bool, "Clear").SetMessage("描画を削除しました")
                .AddRadial("FlatsPlus/Pen/Color", "Color").SetMessage("色を変更");
        }
    }
}