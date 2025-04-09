using com.github.pandrabox.flatsplus.runtime;
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
        }
    }
}