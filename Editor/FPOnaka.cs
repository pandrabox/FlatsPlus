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

        public FPOnakaMain(VRCAvatarDescriptor desc)
        {
            _FPOnaka = desc.GetComponentInChildren<FPOnaka>();
            if (_FPOnaka == null) return;
            _prj = new FlatsProject(desc);
            _hips = _prj.HumanoidTransform(HumanBodyBones.Hips);
            _hips001 = _hips?.Find("Hips.001");
            if (_hips001 == null) {Msgbox("Onaka: Hips.001が見つかりません"); return; }
            _onakaTransform = _FPOnaka.transform.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "OnakaBase");
            _touchTransform = _onakaTransform?.Find("TouchArea");
            if(_touchTransform== null) { Msgbox("Onaka: TouchAreaが見つかりません"); return; }
            _onakaPB = _onakaTransform.GetComponent<VRCPhysBone>();
            VRCPhysBone legacyPB = _hips001.GetComponent<VRCPhysBone>();
            if (legacyPB != null) { GameObject.DestroyImmediate(legacyPB); }

            _onakaTransform.SetParent(_hips);

            _onakaTransform.localPosition = new Vector3(0, _prj.OnakaY, _prj.OnakaZ);
            _onakaTransform.localRotation = Quaternion.identity;
            _onakaTransform.localScale = Vector3.one;
            
            _onakaPB.pull = _FPOnaka.Pull;
            _onakaPB.spring = _FPOnaka.Spring;
            _onakaPB.gravity = _FPOnaka.Gravity;
            _onakaPB.gravityFalloff = _FPOnaka.GravityFallOff;
            _onakaPB.immobile = _FPOnaka.Immobile;
            _onakaPB.limitRotation = new Vector3(_FPOnaka.LimigAngle,0,0);
            _onakaPB.radius = _prj.OnakaRadius * _FPOnaka.RadiusTuning; 

            _hips001.SetParent(_touchTransform);
        }
    }
}