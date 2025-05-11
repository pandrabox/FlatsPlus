using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;


//0:Auto→コンタクトON, AllowSelf,AllowOther,
//1:OtherOnly→コンタクトON, AllowOther,
//2:WithoutDance→コンタクトはダンスのみ, AllowSelf,AllowOther,
//3:On→コンタクトなし
//4:Off→コンタクトなし

namespace com.github.pandrabox.flatsplus.editor
{

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
        //private Transform _hoppe2;
        private Transform _blushArmature;
        private Transform _blushHead;
        AnimationClipsBuilder _ac;
        private VRCContactReceiver _blushContact;
        private static float _defaultContactSize = 0.35f;
        private bool _useOriginalBlush => _prj.OriginalBlushName != "NULL" && _config.D_Hoppe_UseOriginalBlush;


        public FPHoppePoWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            if (!_config.D_Hoppe_Blush) return;
            GetStructure();
            SetContactSize();
            CreateAnim();
            CreateBlush();
            DefineParameters();
            CreateExpressionMenu();
        }
        private void GetStructure()
        {
            //_hoppe2 = _tgt.transform.Find("Hoppe2").NullCheck("Hoppe2ObjRoot");
            //_blushArmature = _hoppe2.transform.Find("Armature").NullCheck("BlushArmature");
            //_blushHead = _blushArmature.transform.Find("Head").NullCheck("BlushHead");

            Transform firstHead = _tgt.FindEx("Head").NullCheck("FirstHead");
            Transform blushContactTransform = firstHead.FindEx("BlushContact").NullCheck("BlushContactTransform");
            _blushContact = blushContactTransform.GetComponent<VRCContactReceiver>().NullCheck("BlushContact");
        }

        private void SetContactSize()
        {
            _blushContact.radius = _defaultContactSize * _config.D_Hoppe_Blush_Sensitivity;
        }

        private void CreateAnim()
        {
            _ac = new AnimationClipsBuilder();
            void createContactMode(string name, bool allowSelf, bool allowOther)
            {
                _ac.Clip(name)
                    .Bind(__contactPath, typeof(VRCContactReceiver), "allowSelf").Const2F(allowSelf ? 1 : 0)
                    .Bind(__contactPath, typeof(VRCContactReceiver), "allowOthers").Const2F(allowOther ? 1 : 0);
            }
            createContactMode("CM0_Auto", true, true);
            createContactMode("CM1_OtherOnly", false, true);
            createContactMode("CM2_WithoutDance", true, true);
            createContactMode("CM3_On", false, false);
            createContactMode("CM4_Off", false, false);

            if (_useOriginalBlush)
            {
                _ac.Clip("BlushOn").Bind("Body", typeof(SkinnedMeshRenderer), "blendShape.てれっ").Const2F(100);
                _ac.Clip("BlushOff").Bind("Body", typeof(SkinnedMeshRenderer), "blendShape.てれっ").Const2F(0);
            }
        }

        private void CreateBlush()
        {
            //モデルのサイズを適切にしてMerge
            //_hoppe2.transform.localScale = new Vector3(_prj.Hoppe2X, _prj.Hoppe2Y, _prj.Hoppe2Z);
            //var mama = _blushHead.gameObject.AddComponent<ModularAvatarMergeArmature>();
            //mama.mergeTarget = _prj.HumanoidObjectReference(HumanBodyBones.Head);

            //Controlの生成

            var bb = new BlendTreeBuilder(__blush);
            bb.RootDBT(() =>
            {
                bb.NName("ContactSwitch").Param("IsLocal").Add1D(__blushControlType, () =>
                {
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
                    //auto系なら通常出る場合+コンタクト反応時にON
                    bb.NName("Auto,OtherOnly,WithoutDnace").Param(2).Add1D(__hoppeContact, () =>
                    {
                        bb.Param(0).Add1D(_prj.IsEmoBlush, () =>
                        {
                            bb.Param(0).AddAAP(__blushOn, 0);
                            bb.Param(1).AddAAP(__blushOn, 1);
                        });
                        bb.Param(1).AddAAP(__blushOn, 1);
                    });
                    //ONならON
                    bb.NName("On").Param(3).AddAAP(__blushOn, 1);
                    //OFFらOFF
                    bb.NName("Off").Param(4).AddAAP(__blushOn, 0);
                });
                if (!_useOriginalBlush) //FP版Blushの制御
                {
                    bb.Param("1").FAssignmentBy1D(__blushOnRx, 0, 1, FPMultiToolWork.GetParamName("HoppeOn"), 0, 1);
                    //bb.NName("BlushObj").Param("1").Add1D(__blushOnRx, () =>
                    //{
                    //    bb.Param(0).AddMotion(_ac.OffAnim(__hoppePath));
                    //    bb.Param(1).AddMotion(_ac.OnAnim(__hoppePath));
                    //});
                }
                //else
                //{
                //    //useOriginalなら常にHoppeOffを再生
                //    bb.Param("1").AddAAP(FPMultiToolWork.GetParamName("HoppeOff"), 1);
                //}
            });
            bb.Attach(_tgt.gameObject);

            if (_useOriginalBlush)
            {
                var bb2 = new BlendTreeBuilder("BlushAbsolute");
                bb2.RootDBT(() =>
                {
                    bb2.Param("1").Add1D(__blushOnRx, () =>
                    {
                        bb2.Param(0).AddMotion(_ac.Outp("BlushOff"));
                        bb2.Param(1).AddMotion(_ac.Outp("BlushOn"));
                    });
                });
                bb2.Attach(_prj, true);
            }
            _prj.VirtualSync(__blushOn, 1, PVnBitSync.nBitSyncMode.IntMode);
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
            Log.I.StartMethod("メニューの作成を開始します");
            if (!_config.D_Hoppe_ShowExpressionMenu)
            {
                Log.I.EndMethod("オプションにより不要が指定されたためメニューの作成をスキップします");
                return;
            }
            var mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).Ico("FlatsPlus").AddFolder(L("Menu/Hoppe"), true).Ico("Hoppe")
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/Auto"), 0).Ico("HoppeAny")
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/OtherOnly"), 1).Ico("HoppeOTHERS")
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/WithoutDance"), 2).Ico("HoppeWithoutDance")
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/On"), 3).Ico("HoppeOn")
                .AddToggle(__blushControlType, L("Menu/Hoppe/Blush/Control/Off"), 4).Ico("HoppeOff");
            Log.I.EndMethod("メニューの作成が完了しました");
        }
    }
}