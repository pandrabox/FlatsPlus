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
using System.Linq;
using VRC.SDK3.Avatars.Components;
using com.github.pandrabox.flatsplus.runtime;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using System.Text.RegularExpressions;
using com.github.pandrabox.pandravase.editor;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPMeshSettingDebug
    {
        [MenuItem("PanDbg/FPMeshSetting")]
        public static void FPMeshSetting_Debug()
        {
            SetDebugMode(true);
            foreach( var a in AllAvatar)
            {
                var ms = a.gameObject.GetComponent<ModularAvatarMeshSettings>();
                if (ms != null) GameObject.DestroyImmediate(ms);

                new FPMeshSettingWork(a.FP());
            }
        }
    }
#endif

    public class FPMeshSettingWork: FlatsWork<FPMeshSetting>
    {
        GameObject _root;
        public FPMeshSettingWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            _root = _desc.gameObject;
            ModularAvatarMeshSettings modularAvatarMeshSettings = _root.GetComponent<ModularAvatarMeshSettings>();
            if (modularAvatarMeshSettings != null) return;
            var HipsReference = _prj.HumanoidObjectReference(HumanBodyBones.Hips);
            modularAvatarMeshSettings = _root.AddComponent<ModularAvatarMeshSettings>();
            modularAvatarMeshSettings.InheritProbeAnchor = ModularAvatarMeshSettings.InheritMode.Set;
            modularAvatarMeshSettings.ProbeAnchor = HipsReference;
            modularAvatarMeshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.Set;
            modularAvatarMeshSettings.RootBone = HipsReference;

            modularAvatarMeshSettings.Bounds = new Bounds()
            {
                center = _prj.TotalBoundsCenter,
                extents = _prj.TotalBoundsExtent
            };
        }
    }
}