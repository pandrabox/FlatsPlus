﻿using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPHoppeDebug
    {
        [MenuItem("PanDbg/FPHoppe")]
        public static void FPHoppe_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPHoppeWork(fp);
            }
        }
    }
#endif

    public class FPHoppeWork : FlatsWork<FPHoppe>
    {
        private CheekInfo _cheekInfo;
        private static string __prefix = "FlatsPlus/Hoppe";
        private static string __blush = $@"{__prefix}/Blush";
        private static string __hoppePath = "Hoppe2/hoppe";

        public FPHoppeWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            CreateBlush();
            _cheekInfo = new CheekInfo(_tgt, _prj);
            CreateCheekSensor();
            CreateCheekControl();
        }
        private void CreateBlush()
        {
            Transform hoppe2 = _tgt.transform.Find("Hoppe2").NullCheck("Hoppe2ObjRoot");
            Transform blushArmature = hoppe2.transform.Find("Armature").NullCheck("BlushArmature");
            Transform blushHead = blushArmature.transform.Find("Head").NullCheck("BlushHead");

            hoppe2.transform.localScale = new Vector3(_prj.Hoppe2X, _prj.Hoppe2Y, _prj.Hoppe2Z);
            var mama = blushHead.gameObject.AddComponent<ModularAvatarMergeArmature>();
            mama.mergeTarget = _prj.HumanoidObjectReference(HumanBodyBones.Head);
            var ac = new AnimationClipsBuilder();
            var bb = new BlendTreeBuilder(__blush);
            bb.RootDBT(() =>
            {
                bb.Param("1").Add1D(_prj.HeadSensor, () =>
                {
                    bb.Param(0).AddMotion(ac.OffAnim(__hoppePath));
                    bb.Param(0.0001f).AddMotion(ac.OnAnim(__hoppePath));
                });
            });
            bb.Attach(_tgt.gameObject);
        }
        private class CheekInfo
        {
            public Transform Head;
            public Transform AvatarHead;
            public Transform[] AvatarCheeks;
            public bool Enable;
            public AnimationClipsBuilder AC;
            public CheekInfo(FPHoppe hphoppe, FlatsProject prj)
            {
                Head = hphoppe.transform.Find("Head");
                if (Head == null)
                {
                    Debug.LogError("Head not found");
                    return;
                }

                AvatarHead = prj.HumanoidTransform(HumanBodyBones.Head);
                if (AvatarHead == null)
                {
                    LowLevelExeption("Avatar head not found");
                    return;
                }

                AvatarCheeks = new[] { AvatarHead.Find("cheek_L"), AvatarHead.Find("cheek_R") };
                if (AvatarCheeks[0] == null || AvatarCheeks[1] == null)
                {
                    LowLevelExeption("Avatar cheeks not found");
                    return;
                }

                AC = new AnimationClipsBuilder();
                for (int n = 0; n < 2; n++)
                {
                    string LR = n == 0 ? "L" : "R";
                    AC.Clip($"{LR}0").IsVector3((x) => x.Bind($"cheek_{LR}", typeof(Transform), "m_LocalScale.@a").Const2F(AvatarCheeks[n].localScale));
                    AC.Clip($"{LR}1").IsVector3((x) => x.Bind($"cheek_{LR}", typeof(Transform), "m_LocalScale.@a").Const2F(new Vector3(4.062213f, 4.869611f, 2.235f)));


                    AC.Clip($"{LR}R0").Bind($"cheek_{LR}", typeof(RotationConstraint), "m_Weight").Const2F(0);
                    AC.Clip($"{LR}R1").Bind($"cheek_{LR}", typeof(RotationConstraint), "m_Weight").Const2F(1);
                }

                Enable = true;
            }
        }
        private void CreateCheekSensor()
        {
            if (!_cheekInfo.Enable) return;
            GameObject cheekSensor = new GameObject($"CheekSensor");
            cheekSensor.AddComponent<ModularAvatarVisibleHeadAccessory>();
            cheekSensor.transform.SetParent(_cheekInfo.Head);
            cheekSensor.transform.localPosition = new Vector3(0, 0, 0);
            cheekSensor.transform.localEulerAngles = new Vector3(0, 0, 0);
            cheekSensor.transform.localScale = Vector3.one;
            for (int n = 0; n < 2; n++)
            {
                string LR = n == 0 ? "L" : "R";
                int vector = n == 0 ? -1 : 1;
                GameObject boneRoot = new GameObject($"CheekSensor_{LR}");
                boneRoot.transform.SetParent(_cheekInfo.AvatarCheeks[n]);
                boneRoot.transform.localEulerAngles = Vector3.zero;
                boneRoot.transform.localPosition = Vector3.zero;
                LowLevelDebugPrint($"CheekSensor_{LR} {boneRoot.transform.position}");
                GameObject boneEnd = new GameObject($"CheekSensorEnd_{LR}");
                boneRoot.transform.SetParent(cheekSensor.transform, true);
                boneRoot.transform.localScale = Vector3.one;
                boneEnd.transform.SetParent(boneRoot.transform);
                boneEnd.transform.localPosition = new Vector3(0, 0.06f, 0);
                var pb = boneRoot.AddComponent<VRCPhysBone>();
                pb.immobile = 1;
                pb.radius = 0.06f;
                pb.maxStretch = 1f;
                pb.parameter = $@"{_prj.CheekSensor}{LR}";
                pb.limitType = VRCPhysBoneBase.LimitType.Angle;
                pb.maxAngleX = 40;

                // Add RotationConstraint to AvatarCheeks[n] and set it to follow boneRoot
                var rotationConstraint = _cheekInfo.AvatarCheeks[n].gameObject.AddComponent<RotationConstraint>();
                ConstraintSource source = new ConstraintSource();
                source.sourceTransform = boneRoot.transform;
                source.weight = 1;
                rotationConstraint.AddSource(source);
                rotationConstraint.constraintActive = true;
            }
        }
        private void CreateCheekControl()
        {
            if (!_cheekInfo.Enable) return;
            var ac = new AnimationClipsBuilder();
            var bb = new BlendTreeBuilder("CheekControl");
            bb.RootDBT(() =>
            {
                for (int n = 0; n < 2; n++)
                {
                    string LR = n == 0 ? "L" : "R";
                    bb.Param("1").Add1D($@"{_prj.CheekSensor}{LR}_Stretch", () =>
                    {
                        bb.Param(0).AddMotion(_cheekInfo.AC.Outp($"{LR}0"));
                        bb.Param(1).AddMotion(_cheekInfo.AC.Outp($"{LR}1"));
                    });
                    bb.Param("1").Add1D("FlatsPlus/Emo/IsDisHoppe", () =>
                    {
                        bb.Param(0).AddMotion(_cheekInfo.AC.Outp($"{LR}R1"));
                        bb.Param(1).AddMotion(_cheekInfo.AC.Outp($"{LR}R0"));
                    });
                }
            });
            bb.Attach(_cheekInfo.AvatarHead.gameObject);

        }
    }
}