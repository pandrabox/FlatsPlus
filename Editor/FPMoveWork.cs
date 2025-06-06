﻿using com.github.pandrabox.flatsplus.runtime;
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
        private AnimationClipsBuilder _ac;
        sealed protected override void OnConstruct()
        {
            GetMultiTool();
            CloudControl();
            CreateMenu();
        }


        private void GetMultiTool()
        {
            var multiTool = _prj.Descriptor.GetComponentInChildren<FPMultiTool>().NullCheck("MultiTool");
            var cloudPos = _tgt.transform.Find("Obj/Cart/Sphere").NullCheck("CloudPos");
            multiTool.SetBone("Cloud", cloudPos);
        }

        private void CloudControl()
        {
            var bb = new BlendTreeBuilder("CloudControl");
            bb.RootDBT(() =>
            {
                bb.Param("1").Add1D("FlatsPlus/Moove/Cart", () =>
                {
                    bb.Param(0).AddAAP(FPMultiToolWork.GetParamName("CloudOn"), 0);
                    bb.Param(1).AddAAP(FPMultiToolWork.GetParamName("CloudOn"), 1);
                });
            });
            bb.Attach(_tgt.gameObject);
        }

        private void CreateMenu()
        {
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).Ico("FlatsPlus").AddFolder(L("Menu/Move")).Ico("Move");
            mb.AddToggle("FlatsPlus/Move/Mode", L("Menu/Move/FlyDash"), 1, ParameterSyncType.Int).Ico("MoveSw");
            mb.AddRadial("FlatsPlus/Move/DashSpeed", L("Menu/Move/DashSpeed")).Ico("MoveSpeed");
            mb.AddToggle("FlatsPlus/Move/Mode", L("Menu/Move/Continue"), 2, ParameterSyncType.Int).Ico("MoveHover");
            mb.AddRadial("FlatsPlus/Move/ContinueDirection", L("Menu/Move/ContinueDirection"), .55f).Ico("MoveHoverHeight");
            _prj.AddParameter("FlatsPlus/Moove/Cart", ParameterSyncType.Bool, false, 0, false);
        }
    }
}
