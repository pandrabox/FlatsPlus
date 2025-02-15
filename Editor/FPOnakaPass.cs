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


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPOnakaDebug
    {
        [MenuItem("PanDbg/FPOnaka")]
        public static void FPOnaka_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new FPOnakaMain(a);
            }
        }
    }
#endif

    internal class FPOnakaPass : Pass<FPOnakaPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FPOnakaMain(ctx.AvatarDescriptor);
        }
    }

    public class FPOnakaMain
    {
        private FPOnaka _FPOnaka;
        private FlatsProject _prj;
        private Transform _hips001;
        private Transform _onakaTransform;
        private Transform _touchTransform;
        private VRCPhysBone _onakaPB;
        private Transform _hips;
        private Transform _pbRoot;

        public FPOnakaMain(VRCAvatarDescriptor desc)
        {
            _FPOnaka = desc.GetComponentInChildren<FPOnaka>();
            if (_FPOnaka == null) return;
            _prj = new FlatsProject(desc);
            _hips = _prj.HumanoidTransform(HumanBodyBones.Hips);
            _hips001 = _hips?.Find("Hips.001");
            if (_hips001 == null) { Msgbox("Onaka: Hips.001が見つかりません"); return; }
            VRCPhysBone legacyPB = _hips001.GetComponent<VRCPhysBone>();
            if (legacyPB != null) { GameObject.DestroyImmediate(legacyPB); }



            //OnakaBaseの作成
            var onakaBase = new GameObject("OnakaBase");
            onakaBase.transform.SetParent(_hips);
            onakaBase.transform.localPosition = new Vector3(0, _prj.OnakaY1, _prj.OnakaZ1);
            onakaBase.transform.localEulerAngles = Vector3.zero;
            onakaBase.transform.localScale = Vector3.one;

            //TouchAreaの作成
            var touchArea = new GameObject("TouchArea");
            touchArea.transform.SetParent(onakaBase.transform);
            touchArea.transform.localPosition = new Vector3(0, _prj.OnakaY2, _prj.OnakaZ2);
            touchArea.transform.localEulerAngles = Vector3.zero;
            touchArea.transform.localScale = Vector3.one;
            _hips001.transform.SetParent(touchArea.transform, true);

            ////途中のポイントの作成
            //var currentTransform = onakaBase.transform;
            //for (int i = 1; i <= 2; i++)
            //{
            //    Vector3 startPos = onakaBase.transform.position;
            //    Vector3 endPos = touchArea.transform.position;
            //    float t = i / 3f;
            //    Vector3 newPos = Vector3.Lerp(startPos, endPos, t);
            //    var midPoint = new GameObject($"MidPoint{i}");
            //    midPoint.transform.SetParent(currentTransform);
            //    midPoint.transform.position = newPos;
            //    midPoint.transform.localEulerAngles = Vector3.zero;
            //    midPoint.transform.localScale = Vector3.one;
            //    currentTransform= midPoint.transform;
            //}

            ////TouchAreaの組み換え
            //touchArea.transform.SetParent(currentTransform, true);



            var pb = onakaBase.AddComponent<VRCPhysBone>();
            pb.pull = _FPOnaka.Pull;
            pb.spring = _FPOnaka.Spring;
            pb.gravity = _FPOnaka.Gravity;
            pb.gravityFalloff = _FPOnaka.GravityFallOff;
            pb.immobile = _FPOnaka.Immobile;
            pb.limitType = VRCPhysBoneBase.LimitType.Angle;
            pb.maxAngleX = _FPOnaka.LimitAngle;
            pb.radius = _prj.OnakaRadius * _FPOnaka.RadiusTuning;
            //pb.ignoreTransforms = new List<Transform> { _hips001 };
            pb.radiusCurve = new AnimationCurve(new Keyframe(.23f, 0), new Keyframe(.33f, 1), new Keyframe(.43f, 0));
            pb.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            pb.allowPosing = VRCPhysBoneBase.AdvancedBool.False;
            pb.immobileType=VRCPhysBoneBase.ImmobileType.World;
        }
    }
}