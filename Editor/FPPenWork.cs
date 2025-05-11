using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;


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
                var fp = new FlatsProject(a);
                new FPPenWork(fp);
            }
        }
    }
#endif


    public class FPPenWork : FlatsWork<FPPen>
    {
        const float epsilon = 1.401298e-45F;
        const int COLORNUM = 8;
        const int OFFSET = 3;
        const int EXPLORENO = 15;

        public FPPenWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            GetMultiTool();
            Activate();
        }

        private void GetMultiTool()
        {
            var multiTool = _prj.Descriptor.GetComponentInChildren<FPMultiTool>().NullCheck("MultiTool");
            var pos = _tgt.transform.Find("Obj/HandR/Offset/pen").NullCheck("GimmickPen");
            multiTool.SetBone("Pen", pos);
        }


        private void Activate()
        {

            float penAnimTime = .25f;

            var ac = new AnimationClipsBuilder();
            void Set(string name, bool erace, bool inkParticle, bool inkObj)
            {
                ac.Clip(name)
               .Bind("Obj/HandR/Offset/Eraser", typeof(SphereCollider), "m_Enabled").Const2F(erace ? 1 : 0)
               .Bind("Obj/HandR/Offset/InkPos/Ink", typeof(ParticleSystem), "EmissionModule.enabled").Const2F(inkParticle ? 1 : 0)
               .Bind("Obj/HandR/Offset/InkPos/Ink", typeof(GameObject), "m_IsActive").Const2F(inkObj ? 1 : 0);
            }
            Set("Clear", false, false, false);
            Set("OFF", false, false, true);
            Set("Erace", true, false, true);
            Set("Write", false, true, true);
            void Set2(string name, bool inverse, bool penVisible)
            {
                var rot = new Vector3(inverse ? 180 : 0, 0, 0);
                ac.Clip(name).IsVector3((x) =>
                {
                    x.Bind("Obj/HandR/Offset/pen", typeof(Transform), "localEulerAnglesRaw.@a").Const2F(rot);
                })
                .Bind("", typeof(Animator), FPMultiToolWork.GetParamName("PenOn")).Const2F(penVisible ? 1 : 0);
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
                //Quaternion q = new Quaternion(1, 1, (float)i / COLORNUM, 0);
                ac.Clip($@"Color{i}")
                //    .IsQuaternion((x) =>
                //{
                //    x.Bind("Obj/HandR/Offset/pen", typeof(MeshRenderer), "material._MainTex_ST.@a").Const2F(q);
                //    x.Bind("Obj/HandR/Offset/pen", typeof(MeshRenderer), "material._EmissionMap_ST.@a").Const2F(q);
                //})
                .Color("Obj/HandR/Offset/InkPos/Ink", typeof(ParticleSystem), "InitialModule.startColor.maxColor", penColors[i]);
            }

            ac.Clip("ModeWrite")
               .Bind("Obj/HandR/Offset/InkPos/Ink", typeof(ParticleSystem), "InitialModule.startSize.scalar").Const2F(0.01f)
               .Bind("Obj/HandR/Offset/InkPos/Ink", typeof(ParentConstraint), "m_Sources.Array.data[0].weight").Const2F(1)
               .Bind("Obj/HandR/Offset/InkPos/Ink", typeof(ParentConstraint), "m_Sources.Array.data[1].weight").Const2F(0);
            ac.Clip("ModeExplore")
               .Bind("Obj/HandR/Offset/InkPos/Ink", typeof(ParticleSystem), "InitialModule.startSize.scalar").Const2F(0.1f)
               .Bind("Obj/HandR/Offset/InkPos/Ink", typeof(ParentConstraint), "m_Sources.Array.data[0].weight").Const2F(0)
               .Bind("Obj/HandR/Offset/InkPos/Ink", typeof(ParentConstraint), "m_Sources.Array.data[1].weight").Const2F(1);



            var bb = new BlendTreeBuilder("FlatsPlus/Pen/DBT");
            bb.RootDBT(() =>
            {
                bb.NName("CalcMem").Param("1").Add1D("FlatsPlus/Pen/Color", () =>
                {
                    bb.Param(0.5f / COLORNUM).AddAAP("FlatsPlus/Pen/Mem", 0);
                    bb.Param(0.5f / COLORNUM + 1).AddAAP("FlatsPlus/Pen/Mem", epsilon * COLORNUM);
                });
                bb.NName("CalcCom").Param("1").Add1D("FlatsPlus/Pen/ExploreOverride", () =>
                {
                    bb.Param(0).Add1D("FlatsPlus/Pen/Clear", () =>
                    {
                        bb.Param(0).Add1D("FlatsPlus/Pen/Mode", () =>
                        {
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
                    bb.Param(1).AddAAP("FlatsPlus/Pen/Com", EXPLORENO);
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
                    bb.Param(EXPLORENO - .1f).AddMotion(ac.Outp("OFF"));
                    bb.Param(EXPLORENO).AddMotion(ac.Outp("Write"));
                });
                bb.NName("Color").Param("1").Add1D("FlatsPlus/Pen/ComRx", () =>
                {
                    for (int i = 0; i < COLORNUM; i++)
                    {
                        bb.Param(i + OFFSET).AddMotion(ac.Outp($@"Color{i}"));
                    }
                    bb.Param(EXPLORENO).Add1D("FlatsPlus/Explore/ColorRx", () =>
                    {
                        for (int i = 0; i < COLORNUM; i++)
                        {
                            bb.Param((float)i / COLORNUM).AddMotion(ac.Outp($@"Color{i}"));
                        }
                    });
                });
                bb.NName("Mode2").Param("1").Add1D("FlatsPlus/Pen/ComRx", () =>
                {
                    bb.Param(EXPLORENO - .5f).AddMotion(ac.Outp("ModeWrite"));
                    bb.Param(EXPLORENO).AddMotion(ac.Outp("ModeExplore"));
                });
            });


            var ab = new AnimatorBuilder("FlatsPlus/Pen/Pose").AddLayer();
            ab.SetMotion(ac.Outp("PenOff"));
            ab.AddState("PenOn", ac.Outp("PenOn"))
                .TransToCurrent(ab.InitialState)
                    .AddCondition(AnimatorConditionMode.Greater, OFFSET - .5f, "FlatsPlus/Pen/ComRx")
                    .AddCondition(AnimatorConditionMode.Less, EXPLORENO - .5f, "FlatsPlus/Pen/ComRx")
                .TransFromCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Less, .5f, "FlatsPlus/Pen/ComRx")
                .TransFromCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, EXPLORENO - .5f, "FlatsPlus/Pen/ComRx");
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

            bb.Attach(_tgt.gameObject);
            ab.Attach(_tgt.gameObject);

            _prj.VirtualSync("FlatsPlus/Pen/Com", 4, PVnBitSync.nBitSyncMode.IntMode, true);

            new MenuBuilder(_prj).AddFolder("FlatsPlus", true)
                .AddFolder(L("Menu/Pen")).SetMessage(L("Menu/Pen/Message"), duration: 1)
                .AddToggle("FlatsPlus/Pen/Mode", L("Menu/Pen/Write"), 3, ParameterSyncType.Int).SetMessage(L("Menu/Pen/Write/Message"), duration: 1)
                .AddToggle("FlatsPlus/Pen/Mode", L("Menu/Pen/Erace"), 1, ParameterSyncType.Int).SetMessage(L("Menu/Pen/Erace/Message"), duration: 1)
                .AddButton("FlatsPlus/Pen/Clear", 1, ParameterSyncType.Bool, L("Menu/Pen/Clear")).SetMessage(L("Menu/Pen/Clear/Message"))
                .AddRadial("FlatsPlus/Pen/Color", L("Menu/Pen/Color")).SetMessage(L("Menu/Pen/Color/Message"), duration: 1);

        }
    }
}