#if UNITY_EDITOR

using FP = com.github.pandrabox.flatsplus.runtime.FlatsPlus;

namespace com.github.pandrabox.flatsplus.editor
{

    public class FPFuncCarry : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Carry);

        public override void DrawDetail()
        {
            DrawBoolField(nameof(FP.D_Carry_AllowBlueGateDefault));
            DrawFloatField(nameof(FP.D_Carry_GateMaxRange), 0.1f, 5);
        }
    }
    public class FPFuncDanceController : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_DanceController);
    }
    public class FPFuncEmo : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Emo);
    }
    public class FPFuncExplore : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Explore);
    }
    public class FPFuncHoppe : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Hoppe);

        public override void DrawDetail()
        {
            DrawBoolField(nameof(FP.D_Hoppe_AllowTouch));
            DrawBoolField(nameof(FP.D_Hoppe_AllowStretch));
            DrawFloatField(nameof(FP.D_Hoppe_StretchLimit), 0, 2);
            DrawBoolField(nameof(FP.D_Hoppe_Blush));
            DrawFloatField(nameof(FP.D_Hoppe_Blush_Sensitivity), 0, 3);
            DrawBoolField(nameof(FP.D_Hoppe_UseOriginalBlush));
            DrawEnumField(nameof(FP.D_Hoppe_BlushControlType));
            DrawBoolField(nameof(FP.D_Hoppe_ShowExpressionMenu));
        }
    }
    public class FPFuncIco : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Ico);

        //public override void DrawDetail()
        //{
        //    DrawBoolField(nameof(FP.Ico_VerView));
        //}
    }
    public class FPFuncLight : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Light);

        //public override void DrawDetail()
        //{
        //    DrawBoolField(nameof(FP.Light_IntensityPerfectSync));
        //}
    }
    public class FPFuncMakeEmo : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_MakeEmo);

        //public override void DrawDetail()
        //{
        //    DrawFloatField(nameof(FP.MakeEmo_MenuSize), 0.1f, 1f);
        //    DrawFloatField(nameof(FP.MakeEmo_LockSize), 0.01f, 0.2f);
        //    DrawFloatField(nameof(FP.MakeEmo_MenuOpacity), 0, 1);
        //    // 色の設定は専用UIが必要になるため省略
        //    DrawIntField(nameof(FP.MakeEmo_Margin), 0, 50);
        //    DrawFloatField(nameof(FP.MakeEmo_ScrollSpeed), 0.01f, 0.1f);
        //    DrawFloatField(nameof(FP.MakeEmo_DeadZone), 0, 1);
        //}
    }
    public class FPFuncMeshSetting : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_MeshSetting);
    }
    public class FPFuncMove : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Move);
    }
    public class FPFuncOnaka : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Onaka);

        public override void DrawDetail()
        {
            DrawFloatField(nameof(FP.D_Onaka_Pull), 0, 1);
            DrawFloatField(nameof(FP.D_Onaka_Spring), 0, 1);
            DrawFloatField(nameof(FP.D_Onaka_Gravity), 0, 1);
            DrawFloatField(nameof(FP.D_Onaka_GravityFallOff), 0, 2);
            DrawFloatField(nameof(FP.D_Onaka_Immobile), 0, 1);
            DrawFloatField(nameof(FP.D_Onaka_LimitAngle), 0, 90);
            DrawFloatField(nameof(FP.D_Onaka_RadiusTuning), 0.5f, 2);
        }
    }
    public class FPFuncPen : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Pen);
    }
    public class FPFuncSleep : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Sleep);
    }
    public class FPFuncTail : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Tail);

        //public override void DrawDetail()
        //{
        //    DrawFloatField(nameof(FP.Tail_SwingPeriod), 0.5f, 5f);
        //    DrawFloatField(nameof(FP.Tail_SwingAngle), 0, 180);
        //    DrawFloatField(nameof(FP.Tail_SizeMax), 0.1f, 2f);
        //    DrawFloatField(nameof(FP.Tail_SizeMin), 0.01f, 1f);
        //    DrawBoolField(nameof(FP.Tail_SizePerfectSync));
        //    DrawFloatField(nameof(FP.Tail_DefaultSize), 0, 1);
        //    DrawFloatField(nameof(FP.Tail_GravityRange), 0, 1);
        //    DrawBoolField(nameof(FP.Tail_GravityPerfectSync));
        //    DrawFloatField(nameof(FP.Tail_DefaultGravity), 0, 1);
        //}
    }
    public class FPFuncLink : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Link);
    }
    public class FPFuncSync : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Sync);
    }
    public class FPFuncWriteDefaultOn : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_WriteDefaultOn);
    }
    public class FPFuncPoseClipper : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_PoseClipper);
    }

    public class FPFuncClippingCanceler : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_ClippingCanceler);
    }


}

#endif