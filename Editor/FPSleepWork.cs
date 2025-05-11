using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;



namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPSleepDebug
    {
        [MenuItem("PanDbg/FPSleep")]
        public static void FPSleep_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPSleepWork(fp);
            }
        }
    }
#endif


    public class FPSleepWork : FlatsWork<FPSleep>
    {
        private static string __prefix = "FlatsPlus/Sleep";
        private static string __trackingControl = $@"{__prefix}/TrackingControl";
        private static string __locomotionControl = $@"{__prefix}/LocomotionControl";
        private static string __sw = $@"{__prefix}/SW";
        private static string __height = $@"{__prefix}/Height";
        private static string __heightIsDiff = $@"{__prefix}/HeightIsDiff";
        private static string __moveLock = $@"{__prefix}/MoveLock";
        private static string __poseLockAnim = $@"{__prefix}/PoseLockAnim";
        private static string __callPoseClipper = $@"CW/PC/Set/All";

        public FPSleepWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            CreateControl();
            CreateMenu();
        }

        private void CreateControl()
        {
            var bb = new BlendTreeBuilder(__prefix);
            bb.RootDBT(() =>
            {
                bb.Param(__sw).AddD(() =>
                {
                    bb.Param("1").FDiffChecker(__height, __heightIsDiff);
                });
            });
            bb.Attach(_prj.PrjRootObj);

            var ab = new AnimatorBuilder(__prefix);
            {
                ab.AddLayer(__trackingControl);
                ab.SetTrackingControl(true);

                ab.AddState("Body").SetTrackingControl(true, hip: false, leftFoot: false, rightFoot: false);
                var offState = ab.CurrentState;
                ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, .5f, __sw, true);

                ab.AddState("ALLAnim").SetTrackingControl(true, false, false, false, false, false, false);
                ab.TransToCurrent(offState).AddCondition(AnimatorConditionMode.Greater, .5f, __poseLockAnim, true);
                ab.TransFromCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Less, .5f, __sw);
            }
            {
                ab.AddLayer(__locomotionControl);
                ab.SetLocomotionControl(true);
                ab.AddState("FALSE").SetLocomotionControl(false);
                ab.TransToCurrent(ab.InitialState).AddCondition(AnimatorConditionMode.Greater, .5f, __moveLock, true);
            }

            ab.Attach(_prj.PrjRootObj);
        }

        private void CreateMenu()
        {
            var mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).Ico("FlatsPlus").AddFolder(L("Menu/Sleep")).SetMessage(L("Menu/Sleep/Message"), duration: 1).Ico("Sleep");
            mb.AddToggle(__sw, menuName: L("Menu/Sleep/Enable"), localOnly: false).Ico("Sleep_SW");
            mb.AddToggle(__moveLock, L("Menu/Sleep/MoveLock")).SetMessage(L("Menu/Sleep/MoveLock/Message"), duration: 1).Ico("FootLock");
            mb.AddRadial(__height, menuName: L("Menu/Sleep/Height"), defaultVal: .5f, localOnly: false).Ico("Foot_Height");
            mb.AddToggle(__poseLockAnim, L("Menu/Sleep/LockAnim"), 1, ParameterSyncType.Bool).SetMessage(L("Menu/Sleep/LockAnim/Message"), duration: 1).Ico("Sleep_Lock1");
            mb.AddToggle(__callPoseClipper, L("Menu/Sleep/LockPose"), 1, ParameterSyncType.Bool).SetMessage(L("Menu/Sleep/LockPose/Message"), duration: 1).Ico("Sleep_Lock2");
            _prj.OverrideFolderIco("PoseClipper", "Packages/com.github.pandrabox.flatsplus/Assets/Icon/Clip.png");
        }
    }
}