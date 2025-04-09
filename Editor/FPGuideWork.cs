#region
using com.github.pandrabox.flatsplus.runtime;
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

        }
    }
}