using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;


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
                var fp = new FlatsProject(a);
                new FPTailWork(fp);
            }
        }
    }
#endif


    public class FPTailWork : FlatsWork<FPTail>
    {
        private BlendTreeBuilder _bb;
        public GameObject _tail;
        public VRCPhysBone _tailPB;
        //public List<VRCPhysBoneColliderBase> _tailColliders;
        //public VRCPhysBoneColliderBase _groundCollider;

        public FPTailWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            //throw new NotImplementedException();
            GetObjects();
            ObjectSetting();
            _bb = new BlendTreeBuilder("FlatsPlus/Tail");
            _bb.RootDBT(() =>
            {
                //Gravity();
                CreateSize();
            });
            CreateSwing();
            _bb.Attach(_tail);
            CreateMenu();
        }

        private void GetObjects()
        {
            _tail = _prj.HumanoidGameObject(HumanBodyBones.Hips)?.GetComponentsInChildren<Transform>()?.FirstOrDefault(x => x.name == _prj.TailName)?.gameObject.NullCheck();
            _tailPB = _tail.GetComponent<VRCPhysBone>();
            if (_tailPB == null) _tailPB = _tail.AddComponent<VRCPhysBone>();
            //_groundCollider = _tgt.GetComponentsInChildren<VRCPhysBoneCollider>().FirstOrDefault(x => x.name == "GroundCollider").NullCheck();
            //_tailColliders = _tailPB.colliders;
            //if (_tailColliders == null) _tailColliders = new List<VRCPhysBoneColliderBase>();
        }

        private void ObjectSetting()
        {
            _tailPB.isAnimated = true;
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
                    LowLevelDebugPrint($"ObjectSetting: 辞書情報のcurveInfosに問題があります - time: {curveInfos[i]}, value: {curveInfos[i + 1]}, mode: {curveInfos[i + 2]}", level: LogType.Exception);
                }
            }

            //_tailColliders.Add(_groundCollider);
        }

        private void Gravity()
        {
            var ac = new AnimationClipsBuilder();
            ac.Clip("Gravity-1").Bind("", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "gravity").Const2F(-_tgt.GravityRange);
            ac.Clip("Gravity1").Bind("", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "gravity").Const2F(_tgt.GravityRange);
            _bb.Param("1").Add1D("FlatsPlus/Tail/GravityRx", () =>
            {
                _bb.Param(0).AddMotion(ac.Outp("Gravity-1"));
                _bb.Param(1).AddMotion(ac.Outp("Gravity1"));
            });
            _bb.Param("1").FDiffChecker("FlatsPlus/Tail/GravityRx");
            _bb.Param("1").FDiffChecker("FlatsPlus/Tail/Gravity", "RxIsDiff");
            float unitTime = 10 / FPS;
            ac.Clip("PBReload")
                .Bind("", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "m_Enabled")
                .Smooth(0f, 0f, unitTime, 0f, unitTime, 1f, 3 * unitTime, 1f);
            AnimatorBuilder ab = new AnimatorBuilder("FlatsPlus/Tail/PBReload");
            ab.AddLayer().AddState("Reload", ac.Outp("PBReload"));
            ab.TransFromCurrent(ab.InitialState).MoveInstant();
            ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, .5f, "FlatsPlus/Tail/GravityRxIsDiff");
            //ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.IfNot, 1, "IsAnimatorEnabled");
            ab.Attach(_tail);

            _prj.VirtualSync("FlatsPlus/Tail/Gravity", 3, PVnBitSync.nBitSyncMode.FloatMode, _tgt.GravityPerfectSync);
        }

        private void CreateSize()
        {
            Vector3 tail0NormalSize = _tail.transform.localScale;
            Vector3 tail0BigParam = Vector3.one * _prj.TailScaleLimit0 * _tgt.SizeMax;
            if (_prj.TailScaleXLimit0 != -1) tail0BigParam.x = _prj.TailScaleXLimit0;
            Vector3 tail0Big = tail0NormalSize.HadamardProduct(tail0BigParam);

            Transform tail1 = GetDirectChildren(_tail).FirstOrDefault(child => child.name.ToLower().Contains("tail"));
            Vector3 tail1NormalSize = tail1.transform.localScale;
            Vector3 tail1BigParam = Vector3.one * _prj.TailScaleLimit1;
            Vector3 tail1Big = tail1NormalSize.HadamardProduct(tail1BigParam);

            AnimationClipsBuilder ac = new AnimationClipsBuilder();
            float smallVal = _tgt.SizeMin;
            ac.Clip("Small").IsVector3((x) =>
            {
                x.Bind("", typeof(Transform), $"m_LocalScale.@a").Const2F(smallVal);
                x.Bind(tail1?.name, typeof(Transform), $"m_LocalScale.@a").Const2F(tail1NormalSize);
            });
            ac.Clip("Normal").IsVector3((x) =>
            {
                x.Bind("", typeof(Transform), $"m_LocalScale.@a").Const2F(tail0NormalSize);
                x.Bind(tail1?.name, typeof(Transform), $"m_LocalScale.@a").Const2F(tail1NormalSize);
            });
            ac.Clip("Big").IsVector3((x) =>
            {
                x.Bind("", typeof(Transform), $"m_LocalScale.@a").Const2F(tail0Big);
                x.Bind(tail1?.name, typeof(Transform), $"m_LocalScale.@a").Const2F(tail1Big);
            });
            _bb.NName("TailSize").Param("1").Add1D("FlatsPlus/Tail/SizeRx", () =>
            {
                _bb.Param(0).AddMotion(ac.Outp("Small"));
                _bb.Param(0.5f).AddMotion(ac.Outp("Normal"));
                _bb.Param(1).AddMotion(ac.Outp("Big"));
            });

            _prj.VirtualSync("FlatsPlus/Tail/Size", 4, PVnBitSync.nBitSyncMode.FloatMode, _tgt.SizePerfectSync);
        }

        private void CreateSwing()
        {
            float swingAngle = _tgt.SwingAngle;
            float swingPeriod = _tgt.SwingPeriod;

            var ac = new AnimationClipsBuilder();
            Quaternion[] sPos = new Quaternion[3]; //Eularだとぺちゃっとした変な動きになったがQuaternionだと奇麗だった
            sPos[0] = CalcQuaternionRotationY(_tail, swingAngle);
            sPos[1] = CalcQuaternionRotationY(_tail, -swingAngle);
            sPos[2] = CalcQuaternionRotationY(_tail, 0);
            ac.Clip($@"Swing").SetLoop(true).IsQuaternion((x) =>
            {
                x.Bind("", typeof(Transform), $"m_LocalRotation.@a")
                .Smooth(
                    swingPeriod * 0, sPos[0]
                    , swingPeriod * 1, sPos[1]
                    , swingPeriod * 2, sPos[0]
                )
                .SetAllFlat();
            });
            ac.Clip($@"Stop").IsQuaternion((x) =>
            {
                x.Bind("", typeof(Transform), $"m_LocalRotation.@a").Const2F(sPos[2]);
            });

            var ab = new AnimatorBuilder("FlatsPlus/Tail/Swing").AddLayer();
            ab.SetMotion(ac.Outp("Stop"));
            ab.AddState("on", ac.Outp("Swing"))
                .TransToCurrent(ab.InitialState, transitionDuration: 1.5f)
                .AddCondition(AnimatorConditionMode.If, 1, "FlatsPlus/Tail/Swing", true);
            ab.Attach(_tail, true);

        }

        private void CreateMenu()
        {
            new MenuBuilder(_prj)
                .AddFolder(PRJNAME, true)
                .AddFolder(L("Menu/Tail"), true)
                //.AddRadial("FlatsPlus/Tail/Gravity", L("Menu/Tail/Gravity"), _tgt.DefaultGravity)
                .AddRadial("FlatsPlus/Tail/Size", L("Menu/Tail/Size"), _tgt.DefaultSize)
                .AddToggle("FlatsPlus/Tail/Swing", 1, ParameterSyncType.Bool, L("Menu/Tail/Swing"), 1, false);
        }

    }
}