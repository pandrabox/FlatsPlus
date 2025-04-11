#if UNITY_EDITOR

using com.github.pandrabox.pandravase.runtime;
using UnityEngine;


namespace com.github.pandrabox.flatsplus.runtime
{
    public class FlatsPlus : PandraComponent
    {
        public bool Func_Carry = true;
        public bool Func_DanceController = true;
        public bool Func_Emo = true;
        public bool Func_Explore = true;
        public bool Func_Hoppe = true;
        public bool Func_Ico = true;
        public bool Func_Light = true;
        public bool Func_MakeEmo = true;
        public bool Func_MeshSetting = true;
        public bool Func_Move = true;
        public bool Func_Onaka = true;
        public bool Func_Pen = true;
        public bool Func_Sleep = true;
        public bool Func_Tail = true;
        public bool Func_Link = true;
        public bool Func_Sync = true;
        public bool Func_WriteDefaultOn = true;
        public bool Func_Guide = true;
        public bool Func_MultiTool = true;
        public bool Func_ClippingCanceler = true;


        public bool D_Guide_DefaultActive = true;
        public float D_Guide_DefaultSize = 0.4f;

        public string Language = null;
        public bool D_Hoppe_AllowTouch = true;
        public bool D_Hoppe_AllowStretch = true;
        public float D_Hoppe_StretchLimit = 1f;//0～2
        public bool D_Hoppe_Blush = true;
        public float D_Hoppe_Blush_Sensitivity = 1f;//0～1
        public bool D_Hoppe_UseOriginalBlush = true;
        public Hoppe_BlushControlType D_Hoppe_BlushControlType = Hoppe_BlushControlType.WithoutDance;
        public enum Hoppe_BlushControlType { Auto, OtherOnly, WithoutDance, On, Off }
        public bool D_Hoppe_ShowExpressionMenu = false;

        public bool D_Carry_AllowBlueGateDefault = true;
        public float D_Carry_GateMaxRange = 1f;

        public float D_Onaka_Pull = 0.5f;
        public float D_Onaka_Spring = 0.8f;
        public float D_Onaka_Gravity = 0.2f;
        public float D_Onaka_GravityFallOff = 1f;
        public float D_Onaka_Immobile = 0.8f;
        public float D_Onaka_LimitAngle = 20f;
        public float D_Onaka_RadiusTuning = 1f;

        public float Emo_TransitionTime = 0.5f;
        public string D_Emo_Preset = "";
        public string D_Emo_ConfigFilePath = "";

        public PVDanceController.DaceControlType D_Dance_ControlType = PVDanceController.DaceControlType.Normal;
        public bool D_Dance_FxEnable = false;

        public Texture2D[] Ico_Textures = new Texture2D[8];
        public bool Ico_VerView = false;
        public bool Light_IntensityPerfectSync = false;
        public float MakeEmo_MenuSize = 0.35f;
        public float MakeEmo_LockSize = 0.08f;
        public float MakeEmo_MenuOpacity = 0.85f;
        public Color MakeEmo_SelectColor = new Color(0, 210, 255, 200);
        public Color MakeEmo_BackGroundColor = new Color(0, 0, 0, 150);
        public int MakeEmo_Margin = 13;
        public float MakeEmo_ScrollSpeed = 0.03f;
        public float MakeEmo_DeadZone = 0.3f;
        public float Onaka_Pull = 0.5f;
        public float Onaka_Spring = 0.8f;
        public float Onaka_Gravity = 0.2f;
        public float Onaka_GravityFallOff = 1f;
        public float Onaka_Immobile = 0.8f;
        public float Onaka_LimitAngle = 20f;
        public float Onaka_RadiusTuning = 1f;
        public float Tail_SwingPeriod = 1.5f;
        public float Tail_SwingAngle = 60;
        public float Tail_SizeMax = 1;
        public float Tail_SizeMin = 0.01f;
        public bool Tail_SizePerfectSync = false;
        public float Tail_DefaultSize = .5f; //0～1
        public float Tail_GravityRange = .3f; //0～1
        public bool Tail_GravityPerfectSync = false;
        public float Tail_DefaultGravity = .5f; //0～1
        public bool Func_PoseClipper = true;

        public bool D_Explore_Pin = true;
        public bool D_Explore_Line = true;
        public bool D_Explore_Light = true;
        public bool D_Explore_Light_DefaultGlobal = false;
    }
}
#endif