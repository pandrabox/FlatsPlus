#region
using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
#endregion

namespace com.github.pandrabox.flatsplus.editor
{
    public class FPGuideWork : FlatsWork<FPGuide>
    {
        public FPGuideWork(FlatsProject prj, params object[] args) : base(prj, args) { }
        protected override void OnConstruct()
        {
            var gd = _prj.CreateComponentObject<PVMessageUIParentDefinition>("GuideDef");
            gd.ParentFolder = "FlatsPlus";
            _prj.OverrideFolderIco("Menu/MessageUI".LL(), "Packages/com.github.pandrabox.flatsplus/Assets/Icon/Guide.png");
            _prj.OverrideMenuIco("Vase/MessageUI/SW", null, 1, "Packages/com.github.pandrabox.flatsplus/Assets/Icon/GUIDE_SW.png");
            _prj.OverrideRadialIco("Vase/MessageUI/Size", "Packages/com.github.pandrabox.flatsplus/Assets/Icon/GUIDE_Size.png");
        }
    }
}