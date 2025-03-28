using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.modular_avatar.core;
using System.Runtime.InteropServices;
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
        private static string __hoppePath = "Head/hoppe";
        private static string __contactPath = $@"Head/BlushContact";
        private static string __hoppeContact = $@"FlatsPlus/Hoppe/Blush/Contact";
        private Transform _hoppe2;
        private Transform _blushArmature;
        private Transform _blushHead;
        AnimationClipsBuilder _ac;


        public FPHoppePoWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            if (!_config.D_Hoppe_Blush) return;
            GetStructure();
            CreateAnim();
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

        private void CreateAnim()
        {
            _ac = new AnimationClipsBuilder();
            //0:Auto→コンタクトON, AllowSelf,AllowOther,
            //1:OtherOnly→コンタクトON, AllowOther,
            //2:WithoutDance→コンタクトはダンスのみ, AllowSelf,AllowOther,
            //3:On→コンタクトなし
            //4:Off→コンタクトなし
            void createContaceMode(string name, bool allowSelf, bool allowOther)
            {
                _ac.Clip(name)
                    .Bind(__contactPath, typeof(VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver), "allowSelf").Const2F(allowSelf ? 1 : 0)
                    .Bind(__contactPath, typeof(VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver), "allowOthers").Const2F(allowOther ? 1 : 0);
            }
            createContaceMode("CM0_Auto",  true, true);
            createContaceMode("CM1_OtherOnly",  false, true);
            createContaceMode("CM2_WithoutDance",  true, true);
            createContaceMode("CM3_On", false, false);
            createContaceMode("CM4_Off", false, false);
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
            var bb = new BlendTreeBuilder(__blush);
            float threshold = 1f - _config.D_Hoppe_Blush_Sensitivity;
            bb.RootDBT(() =>
            {
                bb.NName("ContactSwitch").Param("IsLocal").Add1D(__blushControlType, () =>
                {
                    //__blushControlType
                    //0:Auto→コンタクトON, AllowSelf,AllowOther,
                    //1:OtherOnly→コンタクトON, AllowOther,
                    //2:WithoutDance→コンタクトはダンスのみ, AllowSelf,AllowOther,
                    //3:On→コンタクトなし
                    //4:Off→コンタクトなし
                    bb.Param(1).AddMotion(_ac.OnAnim(__contactPath));
                    bb.Param(2).Add1D(_prj.IsDance, () =>
                    {
                        bb.Param(0).AddMotion(_ac.OnAnim(__contactPath));
                        bb.Param(1).AddMotion(_ac.OffAnim(__contactPath));
                    });
                    bb.Param(3).AddMotion(_ac.OffAnim(__contactPath));
                });
                bb.NName("ContactModeSetting").Param("IsLocal").Add1D(__blushControlType, () =>
                {
                    bb.Param(0).AddMotion(_ac.Outp("CM0_Auto"));
                    bb.Param(1).AddMotion(_ac.Outp("CM1_OtherOnly"));
                    bb.Param(2).AddMotion(_ac.Outp("CM2_WithoutDance"));
                    bb.Param(3).AddMotion(_ac.Outp("CM3_On"));
                    bb.Param(4).AddMotion(_ac.Outp("CM4_Off"));
                });
                bb.NName("CalcBlushState").Param("IsLocal").Add1D(__blushControlType, () =>
                {
                    bb.NName("Auto,OtherOnly,WithoutDnace").Param(2).Add1D(__hoppeContact, () =>
                    {
                        bb.Param(threshold).AddAAP(__blushOn, 0);
                        bb.Param(threshold + DELTA).AddAAP(__blushOn, 1);
                    });
                    bb.NName("On").Param(3).AddAAP(__blushOn, 1);
                    bb.NName("Off").Param(4).AddAAP(__blushOn, 0);
                });
                bb.NName("BlushObj").Param("1").Add1D(__blushOnRx, () =>
                {
                    bb.Param(0).AddMotion(_ac.OffAnim(__hoppePath));
                    bb.Param(1).AddMotion(_ac.OnAnim(__hoppePath));
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
            unitDefine(FlatsPlus.Hoppe_BlushControlType.Auto, 0);
            unitDefine(FlatsPlus.Hoppe_BlushControlType.OtherOnly, 1);
            unitDefine(FlatsPlus.Hoppe_BlushControlType.WithoutDance, 2);
            unitDefine(FlatsPlus.Hoppe_BlushControlType.On, 3);
            unitDefine(FlatsPlus.Hoppe_BlushControlType.Off, 4);
        }
        private void CreateExpressionMenu()
        {
            if (_config.D_Hoppe_ShowExpressionMenu)
            {
                LowLevelDebugPrint("メニューの作成を開始します");
            }
            else
            {
                LowLevelDebugPrint("メニューを作成しません");
                return;
            }
            var mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Hoppe"))
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/Auto"), 0)
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/OtherOnly"), 1)
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/WithoutDance"), 2)
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/On"), 3)
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/Off"), 4);
        }
    }
}