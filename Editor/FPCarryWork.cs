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
    public class FPCarryDebug
    {
        [MenuItem("PanDbg/FPCarry")]
        public static void FPCarry_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPCarryWork(fp);
            }
        }
    }
#endif

    public class FPCarryWork : FlatsWork<FPCarry>
    {
        public FPCarryWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            CreateMenu();
        }

        private void CreateMenu()
        {
            Log.I.StartMethod("メニューの作成を開始します");
            _prj.AddParameter("FlatsPlus/Carry/GateActive", ParameterSyncType.Bool);
            _prj.AddParameter("FlatsPlus/Carry/CallTakeMe", ParameterSyncType.Bool);
            _prj.AddParameter("FlatsPlus/Carry/AutoTakeMe", ParameterSyncType.Bool);
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Carry"));
            mb.AddRadial("FlatsPlus/Carry/Distance", L("Menu/Carry/CallGate"), mainParameterName: "FlatsPlus/Carry/Adjusting", localOnly: false).SetMessage(L("Menu/Carry/CallGate/Message"));
            mb.AddToggle("FlatsPlus/Carry/GateActive", menuName: L("Menu/Carry/GateActive"), localOnly: false).SetMessage(L("Menu/Carry/GateActive/Message"));
            mb.AddButton("FlatsPlus/Carry/CallTakeMe", menuName: L("Menu/Carry/CallTakeMe"), localOnly: false).SetMessage(L("Menu/Carry/CallTakeMe/Message"));
            mb.AddToggle("FlatsPlus/Carry/AutoTakeMe", menuName: L("Menu/Carry/AutoTakeMe"), localOnly: false).SetMessage(L("Menu/Carry/AutoTakeMe/Message"));
            mb.AddToggle("FlatsPlus/Carry/HugOrCarry", L("Menu/Carry/Hug"), 1, ParameterSyncType.Int).SetMessage(L("Menu/Carry/Hug/Message"));
            mb.AddToggle("FlatsPlus/Carry/HugOrCarry", L("Menu/Carry/Carry"), 2, ParameterSyncType.Int).SetMessage(L("Menu/Carry/Carry/Message"));
            mb.AddRadial("FlatsPlus/Carry/Distance", L("Menu/Carry/Distance"), localOnly: false).SetMessage(L("Menu/Carry/Distance/Message"));
            mb.AddRadial("FlatsPlus/Carry/Rotation", L("Menu/Carry/Rotation"), localOnly: false).SetMessage(L("Menu/Carry/Rotation/Message"));
            Log.I.EndMethod("メニューの作成が完了しました");
        }
    }
}
