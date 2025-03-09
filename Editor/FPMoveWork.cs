using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPMoveDebug
    {
        [MenuItem("PanDbg/FPMove")]
        public static void FPMove_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPMoveWork(fp);
            }
        }
    }
#endif

    public class FPMoveWork : FlatsWork<FPMove>
    {
        public FPMoveWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            CreateMenu();
        }

        private void CreateMenu()
        {
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Move"));
            mb.AddToggle("FlatsPlus/Move/Mode", 1, ParameterSyncType.Int, L("Menu/Move/FlyDash"));
            mb.AddRadial("FlatsPlus/Move/DashSpeed", L("Menu/Move/DashSpeed"));
            mb.AddToggle("FlatsPlus/Move/Mode", 2, ParameterSyncType.Int, L("Menu/Move/Continue"));
            mb.AddRadial("FlatsPlus/Move/ContinueDirection", L("Menu/Move/ContinueDirection"), .55f);
        }
    }
}
