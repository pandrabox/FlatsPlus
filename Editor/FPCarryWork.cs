using UnityEditor;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.ndmf.util;
using nadena.dev.ndmf;
using com.github.pandrabox.pandravase.runtime;
using static com.github.pandrabox.pandravase.editor.Util;
using static com.github.pandrabox.pandravase.editor.Localizer;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using com.github.pandrabox.flatsplus.runtime;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using System.Text.RegularExpressions;
using com.github.pandrabox.pandravase.editor;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.Dynamics;
using System.Globalization;
using UnityEngine.Animations;


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
            LowLevelDebugPrint("CreateMenu");
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Carry"));
            mb.AddRadial("FlatsPlus/Carry/Distance", L("Menu/Carry/CallGate"), mainParameterName: "FlatsPlus/Carry/Adjusting", localOnly:false).SetMessage(L("Menu/Carry/CallGate/Message"));
            mb.AddToggle("FlatsPlus/Carry/GateActive", menuName:L("Menu/Carry/GateActive"), localOnly: false).SetMessage(L("Menu/Carry/GateActive/Message"));
            mb.AddButton("FlatsPlus/Carry/CallTakeMe", menuName:L("Menu/Carry/CallTakeMe"), localOnly: false).SetMessage(L("Menu/Carry/CallTakeMe/Message"));
            mb.AddToggle("FlatsPlus/Carry/AutoTakeMe", menuName: L("Menu/Carry/AutoTakeMe"), localOnly: false).SetMessage(L("Menu/Carry/AutoTakeMe/Message"));
            mb.AddToggle("FlatsPlus/Carry/HugOrCarry",1,ParameterSyncType.Int, L("Menu/Carry/Hug")).SetMessage(L("Menu/Carry/Hug/Message"));
            mb.AddToggle("FlatsPlus/Carry/HugOrCarry",2,ParameterSyncType.Int, L("Menu/Carry/Carry")).SetMessage(L("Menu/Carry/Carry/Message"));
            mb.AddRadial("FlatsPlus/Carry/Distance", L("Menu/Carry/Distance"), localOnly: false).SetMessage(L("Menu/Carry/Distance/Message"));
            mb.AddRadial("FlatsPlus/Carry/Rotation", L("Menu/Carry/Rotation"), localOnly: false).SetMessage(L("Menu/Carry/Rotation/Message"));
        }
    }
}
