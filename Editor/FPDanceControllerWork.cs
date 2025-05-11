using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;


namespace com.github.pandrabox.flatsplus.editor
{
    public class FPDanceControllerWork : FlatsWork<FPDanceController>
    {
        public FPDanceControllerWork(FlatsProject fp) : base(fp) { }

        protected override void OnConstruct()
        {
            PVDanceController dc = _prj.CreateComponentObject<PVDanceController>("PVDanceControl");
            dc.ParrentFolder = "FlatsPlus";
            dc.ControlType = _config.D_Dance_ControlType;
            dc.FxEnable = _config.D_Dance_FxEnable;

            _prj.OverrideFolderIco("Menu/Dance".LL(), "Packages/com.github.pandrabox.flatsplus/Assets/Icon/DANCE.png");
            _prj.OverrideMenuIco(_prj.DanceDetectMode, null, 0, "Packages/com.github.pandrabox.flatsplus/Assets/Icon/DANCE_Mode0.png");
            _prj.OverrideMenuIco(_prj.DanceDetectMode, null, 1, "Packages/com.github.pandrabox.flatsplus/Assets/Icon/DANCE_Mode1.png");
            _prj.OverrideMenuIco(_prj.DanceDetectMode, null, 2, "Packages/com.github.pandrabox.flatsplus/Assets/Icon/DANCE_Mode2.png");
            _prj.OverrideMenuIco(_prj.OnDanceFxEnable, null, 1, "Packages/com.github.pandrabox.flatsplus/Assets/Icon/FX.png");
        }
    }
}