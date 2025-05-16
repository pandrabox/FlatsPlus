using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.runtime;
using System.Collections.Generic;
using static com.github.pandrabox.pandravase.editor.Localizer;

namespace com.github.pandrabox.flatsplus.editor
{
    public class FPMenuOrderWork : FlatsWork<FPMenuOrder>
    {
        public FPMenuOrderWork(FlatsProject fp) : base(fp) { }

        protected override void OnConstruct()
        {
            PVMenuOrderOverride t = _prj.CreateComponentObject<PVMenuOrderOverride>("PVDanceControlOverride");
            t.FolderName = "FlatsPlus";
            t.MenuOrder = new List<string>()
            {
                L("Menu/Sleep"),
                L("Menu/Tail"),
                L("Menu/Pen"),
                L("Menu/Carry"),
                L("Menu/Move"),
                L("Menu/MakeEmo"),
                L("Menu/Dance"),
                L("Menu/MessageUI"),
                L("Menu/Ico"),
                L("Menu/Explore"),
                "PoseClipper"
            };
        }
    }
}