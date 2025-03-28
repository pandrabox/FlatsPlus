using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;



namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPHoppePoDebug
    {
        [MenuItem("PanDbg/FPHoppePo")]
        public static void FPHoppePo_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPHoppePoWork(fp);
            }
        }
    }
#endif

    public class FPHoppePoWork : FlatsWork<FPHoppe>
    {
        private static string __prefix = "FlatsPlus/Hoppe";
        private static string __blush = $@"{__prefix}/Blush";
        private static string __blushControlType = $@"{__blush}/ControlType";
        private static string __blushOn = $@"{__blush}/On";
        private static string __blushOnRx = $@"{__blush}/OnRx";
        private static string __hoppePath = "Hoppe2/hoppe";
        private Transform _hoppe2;
        private Transform _blushArmature;
        private Transform _blushHead;

        public FPHoppePoWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            if (!_config.D_Hoppe_Blush) return;
            GetStructure();
            CreateBlush();
            DefineParameters();
            CreateExpressionMenu();
        }
        private void GetStructure()
        {
            _hoppe2 = _tgt.transform.Find("Hoppe2").NullCheck("Hoppe2ObjRoot");
            _blushArmature = _hoppe2.transform.Find("Armature").NullCheck("BlushArmature");
            _blushHead = _blushArmature.transform.Find("Head").NullCheck("BlushHead");
        }

        /// <summary>
        /// 頬染めオブジェクトの導入
        //済 public bool D_Hoppe_Blush = true;
        //済 public float D_Hoppe_Blush_Sensitivity = 1f;//0～1
        //public Hoppe_BlushType D_Hoppe_BlushType;
        //public Hoppe_BlushControlType D_Hoppe_BlushControlType;
        //public enum Hoppe_BlushType { Original, FlatsPlus, Both };
        //public enum Hoppe_BlushControlType { Auto, OtherOnly, WithoutDance, On, Off }
        //__blushControlType0:Auto,1:OtherOnly,2:WithoutDance,3:On,4:Off
        //public bool D_Hoppe_Blush_DisableByGesture = true;
        /// </summary>
        private void CreateBlush()
        {
            //モデルのサイズを適切にしてMerge
            _hoppe2.transform.localScale = new Vector3(_prj.Hoppe2X, _prj.Hoppe2Y, _prj.Hoppe2Z);
            var mama = _blushHead.gameObject.AddComponent<ModularAvatarMergeArmature>();
            mama.mergeTarget = _prj.HumanoidObjectReference(HumanBodyBones.Head);

            //Controlの生成
            var ac = new AnimationClipsBuilder();
            var bb = new BlendTreeBuilder(__blush);
            float threshold = 1f - _config.D_Hoppe_Blush_Sensitivity;
            bb.RootDBT(() =>
            {
                bb.NName("BlshControl").Param("IsLocal").Add1D(__blushControlType, () =>
                {
                    bb.NName("Off").Param(0).AddAAP(__blushOn, 0);
                    bb.NName("Auto").Param(1).Add1D(_prj.HeadSensor, () =>
                    {
                        bb.Param(threshold).AddAAP(__blushOn, 0);
                        bb.Param(threshold + DELTA).AddAAP(__blushOn, 1);
                    });
                    bb.NName("OtherOnly").Param(2).Add1D(_prj.HeadSensor, () => //Autoと同じ。Contactの設定が違うだけ
                    {
                        bb.Param(threshold).AddAAP(__blushOn, 0);
                        bb.Param(threshold + DELTA).AddAAP(__blushOn, 1);
                    });
                    bb.NName("WithoutDance").Param(3).Add1D(_prj.IsDance, () =>
                    {
                        bb.Param(0).Add1D(_prj.HeadSensor, () =>
                        {
                            bb.Param(threshold).AddAAP(__blushOn, 0);
                            bb.Param(threshold + DELTA).AddAAP(__blushOn, 1);
                        });
                        bb.Param(1).AddAAP(__blushOn, 0);
                    });
                    bb.NName("On").Param(4).AddAAP(__blushOn, 1);
                    bb.NName("Off").Param(5).AddAAP(__blushOn, 0);
                });
                bb.NName("ContactSetting").Param("IsLocal").Add1D(__blushControlType, () =>
                {
                    //__blushControlType
                    //0:Auto→コンタクトON, AllowSelf,AllowOther,
                    //1:OtherOnly→コンタクトON, AllowOther,
                    //2:WithoutDance→コンタクトはダンスのみ, AllowSelf,AllowOther,
                    //3:On→コンタクトなし
                    //4:Off→コンタクトなし
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
    }
}