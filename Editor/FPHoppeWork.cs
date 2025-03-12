using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
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
        private static string __prefix = "FlatsPlus/Hoppe";
        private static string __blush = $@"{__prefix}/Blush";
        private static string __blushControlType = $@"{__blush}/ControlType";
        private static string __blushOn = $@"{__blush}/On";
        private static string __blushOnRx = $@"{__blush}/OnRx";
        private static string __hoppePath = "Hoppe2/hoppe";
        public Transform _head;
        public Transform _avatarHead;
        public Transform[] _avatarCheeks;
        public AnimationClipsBuilder _ac;

        public FPHoppeWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            GetStructure();
            CreateBlush();


            CreateCheekSensor();
            CreateCheekControl();
            DefineParameters();
            CreateExpressionMenu();
        }


        private void DefineParameters()
        {
            void unitDefine(FlatsPlus.Hoppe_BlushControlType type, float autoVal)
            {
                if (type == _config.D_Hoppe_BlushControlType)
                {
                    _prj.AddParameter(__blushControlType, ParameterSyncType.Float, true, autoVal);
                }
            }
            unitDefine(FlatsPlus.Hoppe_BlushControlType.Auto, 1);
            unitDefine(FlatsPlus.Hoppe_BlushControlType.OtherOnly, 2);
            unitDefine(FlatsPlus.Hoppe_BlushControlType.WithoutDance, 3);
            unitDefine(FlatsPlus.Hoppe_BlushControlType.On, 4);
            unitDefine(FlatsPlus.Hoppe_BlushControlType.Off, 5);
        }
        private void CreateExpressionMenu()
        {
            if (!_config.D_Hoppe_ShowExpressionMenu) return;
            var mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Hoppe"))
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/Auto"), 1)
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/OtherOnly"), 2)
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/WithoutDance"), 3)
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/On"), 4)
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/Off"), 5);
        }

        private void CreateBlush()
        {
            if (!_config.D_Hoppe_Blush) return;
            Transform hoppe2 = _tgt.transform.Find("Hoppe2").NullCheck("Hoppe2ObjRoot");
            Transform blushArmature = hoppe2.transform.Find("Armature").NullCheck("BlushArmature");
            Transform blushHead = blushArmature.transform.Find("Head").NullCheck("BlushHead");

            //モデルのサイズを適切にしてMerge
            hoppe2.transform.localScale = new Vector3(_prj.Hoppe2X, _prj.Hoppe2Y, _prj.Hoppe2Z);
            var mama = blushHead.gameObject.AddComponent<ModularAvatarMergeArmature>();
            mama.mergeTarget = _prj.HumanoidObjectReference(HumanBodyBones.Head);

            //Controlの生成
            var ac = new AnimationClipsBuilder();
            var bb = new BlendTreeBuilder(__blush);
            float th = 1f - _config.D_Hoppe_Blush_Sensitivity;
            bb.RootDBT(() =>
            {
                bb.NName("BlshControl").Param("IsLocal").Add1D(__blushControlType, () =>
                {
                    bb.NName("Off").Param(0).AddAAP(__blushOn, 0);
                    bb.NName("Auto").Param(1).Add1D(_prj.HeadSensor, () =>
                    {
                        bb.Param(th).AddAAP(__blushOn, 0);
                        bb.Param(th + DELTA).AddAAP(__blushOn, 1);
                    });
                    bb.NName("OtherOnly").Param(2).Add1D(_prj.HeadSensor, () => //Autoと同じ。Contactの設定が違うだけ
                    {
                        bb.Param(th).AddAAP(__blushOn, 0);
                        bb.Param(th + DELTA).AddAAP(__blushOn, 1);
                    });
                    bb.NName("WithoutDance").Param(3).Add1D(_prj.IsDance, () =>
                    {
                        bb.Param(0).Add1D(_prj.HeadSensor, () =>
                        {
                            bb.Param(th).AddAAP(__blushOn, 0);
                            bb.Param(th + DELTA).AddAAP(__blushOn, 1);
                        });
                        bb.Param(1).AddAAP(__blushOn, 0);
                    });
                    bb.NName("On").Param(4).AddAAP(__blushOn, 1);
                    bb.NName("Off").Param(5).AddAAP(__blushOn, 0);
                });
                bb.NName("ContactSetting").Param("IsLocal").Add1D(__blushControlType, () =>
                {
                    bb.Param(1).AddMotion(ac.OffAnim(__hoppePath)); //TODO: allowselfの設定アニメを追加
                    bb.Param(2).AddMotion(ac.OnAnim(__hoppePath));
                    bb.Param(3).AddMotion(ac.OnAnim(__hoppePath));
                });
                //実動作
                bb.NName("BlushObj").Param("1").Add1D(__blushOnRx, () =>
                {
                    bb.Param(0).AddMotion(ac.OffAnim(__hoppePath));
                    bb.Param(1).AddMotion(ac.OnAnim(__hoppePath));
                });
            });
            bb.Attach(_tgt.gameObject);
            _prj.VirtualSync(__blushOn, 1, pandravase.runtime.PVnBitSync.nBitSyncMode.IntMode);
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
                _ac.Clip($"{LR}1").IsVector3((x) => x.Bind($"cheek_{LR}", typeof(Transform), "m_LocalScale.@a").Const2F(new Vector3(4.062213f, 4.869611f, 2.235f)));

                _ac.Clip($"{LR}R0").Bind($"cheek_{LR}", typeof(RotationConstraint), "m_Weight").Const2F(0);
                _ac.Clip($"{LR}R1").Bind($"cheek_{LR}", typeof(RotationConstraint), "m_Weight").Const2F(1);
            }
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
                LowLevelDebugPrint($"CheekSensor_{LR} {boneRoot.transform.position}");
                GameObject boneEnd = new GameObject($"CheekSensorEnd_{LR}");
                boneRoot.transform.SetParent(cheekSensor.transform, true);
                boneRoot.transform.localScale = Vector3.one;
                boneEnd.transform.SetParent(boneRoot.transform);
                boneEnd.transform.localPosition = new Vector3(0, 0.06f, 0);
                var pb = boneRoot.AddComponent<VRCPhysBone>();
                pb.immobile = 1;
                pb.radius = 0.06f;
                pb.maxStretch = _config.D_Hoppe_AllowStretch ? _config.D_Hoppe_StretchLimit : 0;
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
            });
            bb.Attach(_avatarHead.gameObject);

        }
    }
}