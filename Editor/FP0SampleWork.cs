#if FLATSPLUS_SampleOnly
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
    public class FPxxxxxDebug
    {
        [MenuItem("PanDbg/FPxxxxx")]
        public static void FPxxxxx_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPxxxxxWork(fp);
            }
        }
    }
#endif

    public class FPxxxxxWork : FlatsWork<FPxxxxx>
    {
        public FPxxxxxWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            CreateMenu();
        }

        private void CreateMenu()
        {
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/xxxxx"));
        }
    }
}
#endif