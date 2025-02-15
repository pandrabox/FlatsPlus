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
    public class FPTailDebug
    {
        [MenuItem("PanDbg/FPTail")]
        public static void FPTail_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
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
        private FlatsProject _prj;
        private VRCPhysBone _tailPB;
        private BlendTreeBuilder _bb;
        public FPTail TailConfig => _FPTails[0];

        public FPTailMain(VRCAvatarDescriptor desc)
        {
            //しっぽの取得
            _FPTails = desc.GetComponentsInChildren<FPTail>();
            if (_FPTails.Length == 0) return;
            _prj = new FlatsProject(desc).SetSuffixMode(false);
            _tail = _prj.ArmatureTransform.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == _prj.TailName)?.gameObject;
            if (_tail == null) { LowLevelDebugPrint("Tailがみつかりませんでした"); return; }

            //しっぽPBの取得とアニメート設定
            _tailPB = _tail.GetComponent<VRCPhysBone>();
            if (_tailPB == null) _tailPB = _tail.AddComponent<VRCPhysBone>();
            _tailPB.isAnimated = true;
            ColliderSet();
            //追加コライダーの設定
            VRCPhysBoneColliderBase groundCollider = TailConfig.gameObject.GetComponentsInChildren<VRCPhysBoneCollider>().FirstOrDefault(x => x.name=="GroundCollider");
            if(groundCollider != null)
            {
                if (_tailPB.colliders == null)
                {
                    _tailPB.colliders = new List<VRCPhysBoneColliderBase>();
                }
                _tailPB.colliders.Add(groundCollider);
            }

            //ギミックの作成
            _bb = new BlendTreeBuilder("FlatsPlus/Tail");
            _bb.RootDBT(() => {
                Gravity();
                CreateSize();
            });
            CreateSwing();
            _bb.Attach(_tail);
        }

        private void ColliderSet()
        {
            _tailPB.radius = _prj.TailColliderSize;

            var curveString = _prj.TailColliderCurve;
            var curveInfos = curveString.Split('-');

            for (int i = 0; i < curveInfos.Length; i += 3)
            {
                if (float.TryParse(curveInfos[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float time) &&
                    float.TryParse(curveInfos[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) &&
                    int.TryParse(curveInfos[i + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int modeValue))
                {
                    var key = new Keyframe(time, value);

                    if (modeValue == 1)
                    {
                        key.inTangent = 0f;
                        key.outTangent = 0f;
                    }

                    _tailPB.radiusCurve.AddKey(key);

                }
                else
                {
                    LowLevelDebugPrint($"ColliderSet: 数値変換エラー - time: {curveInfos[i]}, value: {curveInfos[i + 1]}, mode: {curveInfos[i + 2]}", level: LogType.Exception);
                }
            }
        }

        private void Gravity()
        {
            var ac = new AnimationClipsBuilder();
            ac.Clip("Gravity-1").Bind("", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "gravity").Const2F(-TailConfig.GravityRange);
            ac.Clip("Gravity1").Bind("", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "gravity").Const2F(TailConfig.GravityRange);
            _bb.Param("1").Add1D("FlatsPlus/Tail/GravityRx", () => {
                _bb.Param(0).AddMotion(ac.Outp("Gravity-1"));
                _bb.Param(1).AddMotion(ac.Outp("Gravity1"));
            });
            _bb.Param("1").FDiffChecker("FlatsPlus/Tail/GravityRx");
            _bb.Param("1").FDiffChecker("FlatsPlus/Tail/Gravity", "RxIsDiff");
            float unitTime = 8 / FPS;
            ac.Clip("PBReload")
                .Bind("", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "m_Enabled")
                .Smooth(0f, 0f, unitTime, 0f, unitTime, 1f, 3 * unitTime, 1f);
            AnimatorBuilder ab = new AnimatorBuilder("FlatsPlus/Tail/PBReload");
            ab.AddLayer().AddState("Reload", ac.Outp("PBReload"));
            ab.TransFromCurrent(ab.InitialState, new AnimatorBuilder.TransitionInfo(true,0,false,0,0)).MoveInstant();
            ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater,.5f, "FlatsPlus/Tail/GravityRxIsDiff");
            ab.Attach(_tail);

            var mb = new MenuBuilder(_prj).AddFolder("FlatsPlus", true).AddFolder("Tail", true).AddRadial("FlatsPlus/Tail/Gravity", "Gravity", TailConfig.DefaultGravity);
            var sync = _prj.CreateComponentObject<PVnBitSync>("sync");
            sync.Set("FlatsPlus/Tail/Gravity", 3, PVnBitSync.nBitSyncMode.FloatMode, TailConfig.GravityPerfectSync);
        }

        private void CreateSize()
        {
            Vector3 tail0NormalSize = _tail.transform.localScale;
            Vector3 tail0BigParam = Vector3.one * _prj.TailScaleLimit0 * TailConfig.SizeMax;
            if (_prj.TailScaleXLimit0 != -1) tail0BigParam.x = _prj.TailScaleXLimit0;
            Vector3 tail0Big = tail0NormalSize.HadamardProduct(tail0BigParam);

            Transform tail1 = GetDirectChildren(_tail).FirstOrDefault(child => child.name.ToLower().Contains("tail"));
            Vector3 tail1NormalSize = tail1.transform.localScale;
            Vector3 tail1BigParam = Vector3.one * _prj.TailScaleLimit1;
            Vector3 tail1Big = tail1NormalSize.HadamardProduct(tail1BigParam);

            AnimationClipsBuilder ac = new AnimationClipsBuilder();
            float smallVal = TailConfig.SizeMin;
            ac.Clip("Small").IsVector3((x) => {
                x.Bind("", typeof(Transform), $"m_LocalScale.@a").Const2F(smallVal);
                x.Bind(tail1?.name, typeof(Transform), $"m_LocalScale.@a").Const2F(tail1NormalSize);
            });
            ac.Clip("Normal").IsVector3((x) => {
                x.Bind("", typeof(Transform), $"m_LocalScale.@a").Const2F(tail0NormalSize);
                x.Bind(tail1?.name, typeof(Transform), $"m_LocalScale.@a").Const2F(tail1NormalSize);
            });
            ac.Clip("Big").IsVector3((x) => {
                x.Bind("", typeof(Transform), $"m_LocalScale.@a").Const2F(tail0Big);
                x.Bind(tail1?.name, typeof(Transform), $"m_LocalScale.@a").Const2F(tail1Big);
            });
            _bb.NName("TailSize").Param("1").Add1D("FlatsPlus/Tail/SizeRx", () => {
                _bb.Param(0).AddMotion(ac.Outp("Small"));
                _bb.Param(0.5f).AddMotion(ac.Outp("Normal"));
                _bb.Param(1).AddMotion(ac.Outp("Big"));
            });

            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder("Tail", true).AddRadial("FlatsPlus/Tail/Size", "Size", TailConfig.DefaultSize);

            var sync = _prj.CreateComponentObject<PVnBitSync>("sync");
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
            ac.Clip($@"Swing").SetLoop(true).IsQuaternion((x) => { 
                x.Bind("", typeof(Transform), $"m_LocalRotation.@a")
                .Smooth(
                    swingPeriod * 0, sPos[0]
                    , swingPeriod * 1, sPos[1]
                    , swingPeriod * 2, sPos[0]
                )
                .SetAllFlat();
            });
            ac.Clip($@"Stop").IsQuaternion((x) => { 
                x.Bind("", typeof(Transform), $"m_LocalRotation.@a").Const2F(sPos[2]);
            });

            var ab = new AnimatorBuilder("FlatsPlus/Tail/Swing").AddLayer();
            ab.SetMotion(ac.Outp("Stop"));
            ab.AddState("on", ac.Outp("Swing"))
                .TransToCurrent(ab.InitialState, new AnimatorBuilder.TransitionInfo(false, 0, true, 1.5f, 0))
                .AddCondition(AnimatorConditionMode.If, 1, "FlatsPlus/Tail/Swing", true);
            ab.Attach(_tail, true);

            new MenuBuilder(_prj).AddFolder(PRJNAME, true).AddFolder("Tail", true).AddToggle("FlatsPlus/Tail/Swing", 1, ParameterSyncType.Bool, "Swing", 1, false);
        }
    }
}