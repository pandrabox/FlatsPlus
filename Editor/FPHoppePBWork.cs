using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;



namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPHoppePBDebug
    {
        [MenuItem("PanDbg/FPHoppe")]
        public static void FPHoppe_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPHoppePBWork(fp);
            }
        }
    }
#endif

    /// <summary>
    /// 頬をつかむ判定
    /// </summary>
    public class FPHoppePBWork : FlatsWork<FPHoppe>
    {
        public Transform _head;
        public Transform _avatarHead;
        public Transform[] _avatarCheeks;
        public AnimationClipsBuilder _ac;
        private const string __touchAble = "FlatsPlus/Hoppe/Touchable";

        public FPHoppePBWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            GetStructure();
            CreateCheekSensor();
            CreateCheekControl();
            CreateExpressionMenu();
        }


        private void GetStructure()
        {
            _head = _tgt.transform.Find("Head").NullCheck();
            _avatarHead = _prj.HumanoidTransform(HumanBodyBones.Head).NullCheck();
            _avatarCheeks = new[] { _avatarHead.Find("cheek_L").NullCheck(), _avatarHead.Find("cheek_R").NullCheck() };
            _ac = new AnimationClipsBuilder();
            for (int n = 0; n < 2; n++)
            {
                string LR = n == 0 ? "L" : "R";
                _ac.Clip($"{LR}0").IsVector3((x) => x.Bind($"cheek_{LR}", typeof(Transform), "m_LocalScale.@a").Const2F(_avatarCheeks[n].localScale));
                _ac.Clip($"{LR}1").IsVector3((x) => x.Bind($"cheek_{LR}", typeof(Transform), "m_LocalScale.@a").Const2F(Vector3.one +  new Vector3(3.062213f, 3.869611f, 1.235f)* _config.D_Hoppe_StretchLimit));

                _ac.Clip($"{LR}R0").Bind($"cheek_{LR}", typeof(RotationConstraint), "m_Weight").Const2F(0);
                _ac.Clip($"{LR}R1").Bind($"cheek_{LR}", typeof(RotationConstraint), "m_Weight").Const2F(1);
            }
            _head.position = _avatarHead.position;
        }
        
        private void CreateCheekSensor()
        {
            if (!_config.D_Hoppe_AllowTouch) return;
            GameObject cheekSensor = new GameObject($"CheekSensor");
            cheekSensor.AddComponent<ModularAvatarVisibleHeadAccessory>();
            cheekSensor.transform.SetParent(_head);
            cheekSensor.transform.localPosition = new Vector3(0, 0, 0);
            cheekSensor.transform.localEulerAngles = new Vector3(0, 0, 0);
            cheekSensor.transform.localScale = Vector3.one;
            for (int n = 0; n < 2; n++)
            {
                string LR = n == 0 ? "L" : "R";
                int vector = n == 0 ? -1 : 1;
                GameObject boneRoot = new GameObject($"CheekSensor_{LR}");
                boneRoot.transform.SetParent(_avatarCheeks[n]);
                boneRoot.transform.localEulerAngles = Vector3.zero;
                boneRoot.transform.localPosition = Vector3.zero;
                Log.I.Info($"CheekSensor_{LR} {boneRoot.transform.position}");
                GameObject boneEnd = new GameObject($"CheekSensorEnd_{LR}");
                boneRoot.transform.SetParent(cheekSensor.transform, true);
                boneRoot.transform.localScale = Vector3.one;
                boneEnd.transform.SetParent(boneRoot.transform);
                boneEnd.transform.localPosition = new Vector3(0, 0.06f, 0);
                var pb = boneRoot.AddComponent<VRCPhysBone>();
                pb.immobile = 1;
                pb.radius = 0.06f;
                pb.maxStretch = _config.D_Hoppe_AllowStretch ? .1f : 0;
                pb.parameter = $@"{_prj.CheekSensor}{LR}";
                pb.limitType = VRCPhysBoneBase.LimitType.Angle;
                pb.maxAngleX = 40;

                var rotationConstraint = _avatarCheeks[n].gameObject.AddComponent<RotationConstraint>();
                ConstraintSource source = new ConstraintSource();
                source.sourceTransform = boneRoot.transform;
                source.weight = 1;
                rotationConstraint.AddSource(source);
                rotationConstraint.constraintActive = true;
            }
        }
        /// <summary>
        /// 頬をつかむ制御
        /// </summary>
        private void CreateCheekControl()
        {
            if (!_config.D_Hoppe_AllowTouch) return;
            var ac = new AnimationClipsBuilder();
            var bb = new BlendTreeBuilder("CheekControl");
            bb.RootDBT(() =>
            {
                for (int n = 0; n < 2; n++)
                {
                    string LR = n == 0 ? "L" : "R";
                    bb.Param("1").Add1D($@"{_prj.CheekSensor}{LR}_Stretch", () =>
                    {
                        bb.Param(0).AddMotion(_ac.Outp($"{LR}0"));
                        bb.Param(1).AddMotion(_ac.Outp($"{LR}1"));
                    });
                    bb.Param("1").Add1D("FlatsPlus/Emo/IsDisHoppe", () =>
                    {
                        bb.Param(0).AddMotion(_ac.Outp($"{LR}R1"));
                        bb.Param(1).AddMotion(_ac.Outp($"{LR}R0"));
                    });
                }
                bb.Param("1").Add1D(__touchAble, () =>
                {
                    bb.Param(0).AddMotion(ac.Outp("PBOff"));
                    bb.Param(1).AddMotion(ac.Outp("PBOn"));
                });
            });
            bb.Attach(_avatarHead.gameObject);

            ac.Clip("PBOn")
                .Bind("Head/CheekSensor/CheekSensor_R", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "m_Enabled").Const2F(1)
                .Bind("Head/CheekSensor/CheekSensor_L", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "m_Enabled").Const2F(1);
            ac.Clip("PBOff")
                .Bind("Head/CheekSensor/CheekSensor_R", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "m_Enabled").Const2F(0)
                .Bind("Head/CheekSensor/CheekSensor_L", typeof(VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone), "m_Enabled").Const2F(0);
            var bb2 = new BlendTreeBuilder("CheekControlPBSW");
            bb2.RootDBT(() =>
            {
                bb2.Param("1").Add1D(__touchAble, () =>
                {
                    bb2.Param(0).AddMotion(ac.Outp("PBOff"));
                    bb2.Param(1).AddMotion(ac.Outp("PBOn"));
                });
            });
            bb2.Attach(_tgt.gameObject);


        }
        private void CreateExpressionMenu()
        {
            Log.I.StartMethod("メニューの作成を開始します");
            if (!_config.D_Hoppe_ShowExpressionMenu)
            {
                Log.I.EndMethod("オプションにより不要が指定されたためメニューの作成をスキップします");
                return;
            }
            var mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Hoppe"), true)
                .AddToggle(__touchAble, L("Menu/Hoppe/Cheek/TouchAble"), 1, ParameterSyncType.Bool, 1, false);
            Log.I.EndMethod("メニューの作成が完了しました");
        }
    }

}