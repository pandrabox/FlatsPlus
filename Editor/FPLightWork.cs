using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;



namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPLightDebug
    {
        [MenuItem("PanDbg/FlatsPlusLight")]
        public static void FlatsPlusLight_Debug()
        {
            SetDebugMode(true);
            new FPLightWork(TopAvatar.FP());
        }
    }
#endif


    public class FPLightWork : FlatsWork<FPLight>
    {
        private string _animFolder;
        private static string __prefix = "FlatsPlus/Light";
        private static string __LightModeRx = $@"{__prefix}/LightModeRx";
        private static string __IntensityRx = $@"{__prefix}/IntensityRx";

        public FPLightWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            if (!_config.D_Explore_Light) return;
            _animFolder = $@"{_prj.ProjectFolder}Assets/Light/Anim/";
            CreateControl();
            CreateMenu();
        }
        private void CreateControl()
        {
            var bb = new BlendTreeBuilder(__prefix);
            bb.RootDBT(() =>
            {
                bb.Param("1").Add1D(__LightModeRx, () =>
                {
                    bb.Param(0).AddMotion($@"{_animFolder}/Off.anim");
                    bb.Param(1).Add1D(__IntensityRx, () =>
                    {
                        bb.Param(0).AddMotion($@"{_animFolder}/Off.anim");
                        bb.Param(1).AddMotion($@"{_animFolder}/Spot.anim");
                    });
                    bb.Param(2).Add1D(__IntensityRx, () =>
                    {
                        bb.Param(0).AddMotion($@"{_animFolder}/Off.anim");
                        bb.Param(1).AddMotion($@"{_animFolder}/Area.anim");
                    });
                });
            });
            bb.Attach(_tgt.gameObject);
        }
        private void CreateMenu()
        {
            var mSync = _prj.VirtualSync("FlatsPlus/Light/LightMode", 2, PVnBitSync.nBitSyncMode.IntMode, toggleSync: true);
            _prj.VirtualSync("FlatsPlus/Light/Intensity", 4, PVnBitSync.nBitSyncMode.FloatMode, _config.Light_IntensityPerfectSync);
            new MenuBuilder(_prj).AddFolder("FlatsPlus", true).Ico("FlatsPlus").AddFolder(L("Menu/Explore"), true).Ico("Explore_Pin")
                .AddToggle("FlatsPlus/Light/LightMode", L("Menu/Light/Spot"), 1, ParameterSyncType.Int).SetMessage(L("Menu/Light/Spot/Detail")).Ico("Explore_FlashLight")
                .AddToggle("FlatsPlus/Light/LightMode", L("Menu/Light/Area"), 2, ParameterSyncType.Int).SetMessage(L("Menu/Light/Area/Detail")).Ico("Explore_Light")
                .AddRadial("FlatsPlus/Light/Intensity", L("Menu/Light/Intensity"), .5f).Ico("Explore_LightStr")
                .AddToggle(mSync.SyncParameter, L("Menu/Light/Global"), 1, ParameterSyncType.Bool, (_config.D_Explore_Light_DefaultGlobal ? 1 : 0)).SetMessage(L("Menu/Light/Global/Detail")).Ico("Explore_Global");
        }
    }
}