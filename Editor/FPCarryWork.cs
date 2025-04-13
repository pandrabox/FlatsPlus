using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;



namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPCarryDebug
    {
        [MenuItem("PanDbg/FPCarry")]
        public static void FPCarry_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPCarryWork(fp);
            }
        }
    }
#endif

    public class FPCarryWork : FlatsWork<FPCarry>
    {
        public FPCarryWork(FlatsProject fp) : base(fp) { }
        private AnimationClipsBuilder _ac;
        private string __suffix => "FlatsPlus/Carry";
        private string __Carry => $@"{__suffix}/Carry";
        private string __APD => $@"{__suffix}/APD";
        private string __Distance => $@"{__suffix}/Distance";
        private string __DistanceRx => $@"{__suffix}/DistanceRx";
        private string __Adjusting => $@"{__suffix}/Adjusting";
        private string __Rotation => $@"{__suffix}/Rotation";
        private string __RotationRx => $@"{__suffix}/RotationRx";
        private string __Mode => $@"{__suffix}/Mode";
        private string __ModeRx => $@"{__suffix}/ModeRx";
        private string __ModeActual => $@"{__suffix}/ModeActual";
        private string __isTakeMe => $@"{__suffix}/IsTakeMe";
        private string __isLocal => "IsLocal";
        private string __HugOrCarry => $@"{__suffix}/HugOrCarry";
        private string __Mode1Counter => $@"{__suffix}/Mode1Counter";
        private string __GateActive => $@"{__suffix}/GateActive";
        private string __CallTakeMe => $@"{__suffix}/CallTakeMe";
        private string __AllowBlueGate => $@"{__suffix}/AllowBlueGate";
        private string[] __Modes => new string[] { "MC_Hide", "MC_TakeMe", "MC_FixTakeMe", "MC_Hug", "MC_Carry", "MC_Adjust", "MC_Fix" };
        private float _takeMeComm => 2f;
        private string[] colors => new string[] { "Gray", "Red", "Blue" };

        sealed protected override void OnConstruct()
        {
            GetMultiTool();
            CreateClip();
            DefineParam();
            CreateDBT();
            CreateAPD();
            CreateMenu();
        }

        private void GetMultiTool()
        {
            var multiTool = _prj.Descriptor.GetComponentInChildren<FPMultiTool>().NullCheck("MultiTool");
            var posX = _tgt.transform.Find("Obj/StationX/Ring").NullCheck("posF");
            var posH = _tgt.transform.Find("Obj/Head/StationH/Ring").NullCheck("posH");
            multiTool.SetBone("HeadRing", posH);
            multiTool.SetBone("XRing", posX);
            //multiTool.SetBone("XRedRing", posX);
            //multiTool.SetBone("XBlueRing", posX);
        }

        private void CreateClip()
        {
            _ac = new AnimationClipsBuilder();
            _ac.Clip("Dist1")
                .Bind("Obj/LeftHand/HugPos0/HugPos", typeof(Transform), "m_LocalPosition.z").Const2F(1)
                .Bind("Obj/Neck/CarryPos0/CarryPos", typeof(Transform), "m_LocalPosition.z").Const2F(1)
                .Bind("Obj/Head/ViewPoint/GateAdjustor", typeof(Transform), "m_LocalPosition.z").Const2F(50);
            _ac.Clip("Rot1")
                .Bind("Obj/LeftHand/HugPos0/HugPos", typeof(Transform), "localEulerAnglesRaw.y").Const2F(180)
                .Bind("Obj/Neck/CarryPos0/CarryPos", typeof(Transform), "localEulerAnglesRaw.y").Const2F(180);
            _ac.Clip("StationActive")
                .Bind("Obj/StationX/Station", typeof(BoxCollider), "m_Enabled").Const2F(1)
                .Bind("Obj/Head/StationH/Station", typeof(BoxCollider), "m_Enabled").Const2F(1);
            _ac.Clip("PlzBlueGate")
                .Bind("", typeof(Animator), "FlatsPlus/Carry/RunTakeMe");
            //void DefineRingMode(string name, float r, float g, float b)
            //{
            //    _ac.Clip(name)
            //        //.Bind("Obj/Head/StationH/Ring", typeof(MeshRenderer), "material._Color.r").Const2F(r)
            //        //.Bind("Obj/Head/StationH/Ring", typeof(MeshRenderer), "material._Color.g").Const2F(g)
            //        //.Bind("Obj/Head/StationH/Ring", typeof(MeshRenderer), "material._Color.b").Const2F(b)
            //        //.Bind("Obj/Head/StationH/Ring", typeof(MeshRenderer), "material._Color.a").Const2F(1f)
            //        .Bind("Obj/StationX/Ring", typeof(MeshRenderer), "material._Color.r").Const2F(r)
            //        .Bind("Obj/StationX/Ring", typeof(MeshRenderer), "material._Color.g").Const2F(g)
            //        .Bind("Obj/StationX/Ring", typeof(MeshRenderer), "material._Color.b").Const2F(b)
            //        .Bind("Obj/StationX/Ring", typeof(MeshRenderer), "material._Color.a");
            //}
            //DefineRingMode("RM_Gray", .4f, .4f, .4f);
            //DefineRingMode("RM_Blue", .5f, .5f, 1f);
            //DefineRingMode("RM_Red", 1f, .5f, .5f);

            for (int m = 0; m < colors.Length + 1; m++)
            {
                var color1 = m < colors.Length ? colors[m] : "None";
                for (int i = 0; i < colors.Length; i++)
                {
                    var color2 = colors[i];
                    var val = color1 == color2 ? 0 : 1;
                    _ac.Clip($@"RingDrive_{color1}")
                        .Bind("", typeof(Animator), FPMultiToolWork.GetParamName($@"{color2}RingOff")).Const2F(val);
                }
            }

            /// <summary>
            /// Define the main control animation clip with various parameters.
            /// </summary>
            /// <param name="name">アニメ名</param>
            /// <param name="stHActive">頭のからSitするStationの有効</param>
            /// <param name="stHRingActive">[マルチツールにより無効になりました]頭の上にあるRingの有効</param>
            /// <param name="stHExitPos">頭から出るExitの位置 0:Hの位置 1:Xの位置</param>
            /// <param name="stXConstraintActive">StationXのConstraint有効</param>
            /// <param name="stXActive">StationX座り判定の有効</param>
            /// <param name="stXRingActive">StationXのRingの有効</param>
            /// <param name="stXRootPos">StationXの位置 0:GateAdjustor(通常。目線上のz) 1:Hの位置（TakeMe）</param>
            /// <param name="stXSheetPos">StationXの座り判定の位置 0:Xの位置 1:Hugの位置 2:Carryの位置</param>
            /// <param name="stXExitPos">StationXから出るExitの位置 0:Xの位置 1:Hの位置 2:Hugの位置 3:Carryの位置</param>
            void DefineMainControl(string name, bool stHActive, bool stHRingActive, int stHExitPos, bool stXConstraintActive, bool stXActive, bool stXRingActive, int stXRootPos, int stXSheetPos, int stXExitPos)
            {
                AnimationClipBuilder currentACB = _ac.Clip(name);
                currentACB
                    .Bind("Obj/Head/StationH/Station", typeof(GameObject), "m_IsActive").Const2F(stHActive ? 1 : 0)
                    //.Bind("Obj/Head/StationH/Ring", typeof(GameObject), "m_IsActive").Const2F(stHRingActive ? 1 : 0)
                    .Bind("Obj/StationX/Station", typeof(GameObject), "m_IsActive").Const2F(stXActive ? 1 : 0)
                    //.Bind("Obj/StationX/Ring", typeof(GameObject), "m_IsActive").Const2F(stXRingActive ? 1 : 0)
                    .Bind("Obj/StationX", typeof(ParentConstraint), "m_Enabled").Const2F(stXConstraintActive ? 1 : 0);
                DefineParentConstraint(currentACB, "Obj/Head/StationH/Exit", 2, stHExitPos);
                DefineParentConstraint(currentACB, "Obj/StationX", 2, stXRootPos);
                DefineParentConstraint(currentACB, "Obj/StationX/Station", 3, stXSheetPos);
                DefineParentConstraint(currentACB, "Obj/StationX/ExitPosX/Exit", 4, stXExitPos);
            }
            void DefineParentConstraint(AnimationClipBuilder acb, string path, int length, int activeIndex)
            {
                for (int i = 0; i < length; i++)
                    acb.Bind(path, typeof(ParentConstraint), $@"m_Sources.Array.data[{i}].weight").Const2F(i == activeIndex ? 1 : 0);
            }

            DefineMainControl("MC_Hide", false, false, 1, false, false, false, 0, 0, 1); //Objを非表示、Constraintはどこにしても同じなのだが、一応Fixの位置にしておく
            DefineMainControl("MC_TakeMe", true, true, 1, true, true, true, 1, 0, 1);
            DefineMainControl("MC_FixTakeMe", true, true, 1, false, true, true, 1, 0, 1); //最後にTakeMeした位置で固定
            DefineMainControl("MC_Hug", false, false, 1, false, true, false, 0, 2, 3);
            DefineMainControl("MC_Carry", false, false, 1, false, true, false, 0, 1, 2);
            DefineMainControl("MC_Adjust", true, true, 1, true, true, true, 0, 0, 1);
            DefineMainControl("MC_Fix", true, true, 1, false, true, true, 0, 0, 1); //最後にAdjustした位置で固定
        }
        private void DefineParam()
        {
            _prj.AddParameter(__Distance, ParameterSyncType.Float, true, 0);
            _prj.VirtualSync(__Distance, 6, PVnBitSync.nBitSyncMode.FloatMode);
            _prj.AddParameter(__Rotation, ParameterSyncType.Float, true, 0.5f);
            _prj.VirtualSync(__Rotation, 6, PVnBitSync.nBitSyncMode.FloatMode);
            _prj.AddParameter(__Adjusting, ParameterSyncType.Bool, false, 0);
            _prj.AddParameter(__Mode, ParameterSyncType.Int, true, 0); //0:OFF 1:TakeMe 2:FixTakeMe 3:Hug 4:Carry  5:Adjust 6:Fix
            _prj.VirtualSync(__Mode, 3, PVnBitSync.nBitSyncMode.IntMode);
        }
        private void CreateDBT()
        {
            var bb = new BlendTreeBuilder(__Carry);
            bb.RootDBT(() =>
            {
                bb.NName("距離調整").Param("1").FMultiplicationBy1D(_ac.Outp("Dist1"), __DistanceRx, 0, 1, .1f, _config.D_Carry_GateMaxRange);
                bb.NName("角度調整").Param("1").Add1D(__RotationRx, () =>
                {
                    bb.Param(0).AddMotion(_ac.Outp("Rot1").Multiplication(-1f));
                    bb.Param(0.5f).AddMotion(_ac.Outp("Rot1").Zero());
                    bb.Param(1).AddMotion(_ac.Outp("Rot1"));
                });
                bb.NName("椅子有効化").Param("1").FMultiplicationBy1D(_ac.Outp("StationActive"), __isLocal, 0, 1, 1, 0);
                bb.NName("TakeMe演算").Param(__AllowBlueGate).Add1D(_prj.LinkRx, () =>
                {
                    bb.Param(_takeMeComm - .5f).AddAAP(__isTakeMe, 0);
                    bb.Param(_takeMeComm - .4f).AddAAP(__isTakeMe, 1);
                    bb.Param(_takeMeComm + .4f).AddAAP(__isTakeMe, 1);
                    bb.Param(_takeMeComm + .5f).AddAAP(__isTakeMe, 0);
                });
                bb.NName("Mode1カウンタ").Param("1").AddD(() =>
                {
                    float resetVal = 10;
                    bb.NName("リセットホールド").Param("1").Add1D(__isTakeMe, () =>
                    {
                        bb.Param(0).AddAAP(__Mode1Counter, resetVal);//TakeMeでなければリセット
                        bb.Param(1).FAssignmentBy1D(__Mode1Counter, -1, resetVal, __Mode1Counter); //TakeMeならホールド
                    });
                    bb.NName("デクリメント").Param("1").AddAAP(__Mode1Counter, -1);
                });
                bb.NName("モード計算_ホスト").Param(__isLocal).Add1D(__HugOrCarry, () =>
                {
                    bb.Param(0).Add1D(__GateActive, () =>
                    {
                        bb.Param(0).AddAAP(__Mode, 0);//Off
                        bb.Param(1).Add1D(__Adjusting, () =>
                        {
                            bb.Param(0).AddAAP(__Mode, 6);//Fix
                            bb.Param(1).AddAAP(__Mode, 5);//Adjust
                        });
                    });
                    bb.Param(1).AddAAP(__Mode, 3);//Hug
                    bb.Param(2).AddAAP(__Mode, 4);//Carry
                });
                bb.NName("モード計算_ゲスト").Param("1").Add1D(__isTakeMe, () =>
                {
                    bb.Param(0).FAssignmentBy1D(__ModeRx, 0, 8, __ModeActual);
                    bb.Param(1).Add1D(__Mode1Counter, () =>
                    {
                        bb.Param(0).AddAAP(__ModeActual, 2);//カウントダウン後は2FixTakeMe
                        bb.Param(1).AddAAP(__ModeActual, 1);//カウント中は1TakeMe
                    });
                });

                bb.NName("実動作").Param("1").Add1D(__ModeActual, () =>
                {
                    for (int i = 0; i < __Modes.Length; i++)
                        bb.Param(i).AddMotion(_ac.Outp(__Modes[i]));
                });
                //bb.NName("Xリング色").Param("1").Add1D(__ModeActual, () =>
                //{
                //    bb.Param(3).AddMotion(_ac.Outp("RM_Blue")); //0はないので無視、1,2はTakeMeで青
                //    bb.Param(4).Add1D(__isLocal, () => //4以降はローカルリモートで分岐
                //    {
                //        bb.Param(0).AddMotion(_ac.Outp("RM_Red")); //リモートは赤
                //        bb.Param(1).AddMotion(_ac.Outp("RM_Gray")); //ローカルは灰
                //    });
                //});


                var swAAP = FPMultiToolWork.GetParamName("RingOff");
                bb.NName("RingSW").Param("1").Add1D(__ModeActual, () => { 
                    bb.Param(0).NName("MC_Hide").AddAAP(swAAP, 1);
                    bb.Param(1).NName("MC_TakeMe").AddAAP(swAAP, 0);
                    bb.Param(2).NName("MC_FixTakeMe").AddAAP(swAAP, 0);
                    bb.Param(3).NName("MC_Hug").AddAAP(swAAP, 1);
                    bb.Param(4).NName("MC_Carry").AddAAP(swAAP, 0);
                });

                //色制御はMultiTool側でやっている

                //bb.NName("リング色").Param("1").Add1D(__ModeActual, () =>
                //{
                //    bb.Param(3).AddMotion(_ac.Outp("RM_Blue")); //0はないので無視、1,2はTakeMeで青
                //    bb.Param(4).Add1D(__isLocal, () => //4以降はローカルリモートで分岐
                //    {
                //        bb.Param(0).AddMotion(_ac.Outp("RM_Red")); //リモートは赤
                //        bb.Param(1).AddMotion(_ac.Outp("RM_Gray")); //ローカルは灰
                //    });
                //});

                //bb.NName("RingDrive").Param("1").Add1D(__ModeActual, () =>
                //{
                //    bb.Param(0).NName("MC_Hide").AddMotion(_ac.Outp($"RingDrive_None"));
                //    bb.Param(1).NName("MC_TakeMe").AddMotion(_ac.Outp($"RingDrive_Blue"));
                //    bb.Param(2).NName("MC_FixTakeMe").AddMotion(_ac.Outp($"RingDrive_Blue"));
                //    bb.Param(3).NName("MC_Hug").AddMotion(_ac.Outp($"RingDrive_None"));
                //    bb.Param(4).Add1D(__isLocal, () => //4以降はローカルリモートで分岐
                //    {
                //        bb.Param(0).AddMotion(_ac.Outp($"RingDrive_Red"));
                //        bb.Param(1).AddMotion(_ac.Outp($"RingDrive_Gray"));
                //    });
                //});
                //bb.NName("アンチカリング").Param("1").AddAAP("FlatsPlus/Sync/Global", 1);
            });
            bb.Attach(_tgt.gameObject);
        }
        private void CreateAPD()
        {
            var ab = new AnimatorBuilder(__APD);
            ab.AddLayer();
            ab.AddState("GateOn")
                .SetParameterDriver(__GateActive, 1)
                .TransToCurrent(ab.InitialState)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 1, __Adjusting)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 1, __GateActive)
                .TransFromCurrent(ab.InitialState).MoveInstant();
            ab.AddState("GateOff")
                .SetParameterDriver(__GateActive, 0)
                .SetParameterDriver(__Adjusting, 0)
                .TransToCurrent(ab.InitialState)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, .5f, __HugOrCarry)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 1, __GateActive)
                .TransToCurrent(ab.InitialState)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, .5f, __HugOrCarry)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 1, __Adjusting)
                .TransFromCurrent(ab.InitialState).MoveInstant();
            ab.AddState("IsCallTakeMe")
                .SetParameterDriver(_prj.LinkTx, 2)
                .TransToCurrent(ab.InitialState)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 1, __CallTakeMe)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, _takeMeComm - .5f, _prj.LinkTx)
                .TransToCurrent(ab.InitialState)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 1, __CallTakeMe)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, _takeMeComm + .5f, _prj.LinkTx)
                .TransFromCurrent(ab.InitialState).MoveInstant();
            ab.AddState("IsNotTakeMe")
                .SetParameterDriver(_prj.LinkTx, 0)
                .TransToCurrent(ab.InitialState)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 1, __CallTakeMe)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, _takeMeComm - .5f, _prj.LinkTx)
                    .AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, _takeMeComm + .5f, _prj.LinkTx)
                .TransFromCurrent(ab.InitialState).MoveInstant();
            ab.Attach(_tgt.gameObject);
        }
        private void CreateMenu()
        {
            Log.I.StartMethod("メニューの作成を開始します");
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Carry"));
            mb.AddRadial(__Distance, L("Menu/Carry/CallGate"), mainParameterName: __Adjusting).SetMessage(L("Menu/Carry/CallGate/Message"));
            mb.AddToggle(__GateActive, menuName: L("Menu/Carry/GateActive")).SetMessage(L("Menu/Carry/GateActive/Message"));
            mb.AddToggle(__CallTakeMe, menuName: L("Menu/Carry/CallTakeMe")).SetMessage(L("Menu/Carry/CallTakeMe/Message"));
            mb.AddToggle(__AllowBlueGate, L("Menu/Carry/AllowBlueGate"), 1, ParameterSyncType.Bool, (_config.D_Carry_AllowBlueGateDefault ? 1 : 0), false);
            mb.AddToggle(__HugOrCarry, L("Menu/Carry/Hug"), 1, ParameterSyncType.Int).SetMessage(L("Menu/Carry/Hug/Message"));
            mb.AddToggle(__HugOrCarry, L("Menu/Carry/Carry"), 2, ParameterSyncType.Int).SetMessage(L("Menu/Carry/Carry/Message"));
            mb.AddRadial(__Distance, L("Menu/Carry/Distance")).SetMessage(L("Menu/Carry/Distance/Message"));
            mb.AddRadial(__Rotation, L("Menu/Carry/Rotation"), .5f).SetMessage(L("Menu/Carry/Rotation/Message"));
            Log.I.EndMethod("メニューの作成が完了しました");
        }
    }
}
