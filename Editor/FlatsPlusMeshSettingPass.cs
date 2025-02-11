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
//using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FlatsPlusMeshSettingDebug
    {
        [MenuItem("PanDbg/FlatsPlusMeshSetting")]
        public static void FlatsPlusMeshSetting_Debug()
        {
            SetDebugMode(true);
            foreach( var a in AllAvatar)
            {
                var ms = a.gameObject.GetComponent<ModularAvatarMeshSettings>();
                if (ms != null) GameObject.DestroyImmediate(ms);

                new FlatsPlusMeshSettingMain(a);
            }
        }
    }
#endif

    internal class FlatsPlusMeshSettingPass : Pass<FlatsPlusMeshSettingPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FlatsPlusMeshSettingMain(ctx.AvatarDescriptor);
        }
    }

    public class FlatsPlusMeshSettingMain
    {
        GameObject _root;
        FlatsProject _prj;
        public FlatsPlusMeshSettingMain(VRCAvatarDescriptor desc)
        {
            _prj = new FlatsProject(desc);
            var tgt = desc.transform.GetComponentsInChildren<FlatsPlusMeshSetting>();
            if (tgt.Length == 0) return;
            _root = desc.gameObject;
            ModularAvatarMeshSettings modularAvatarMeshSettings = _root.GetComponent<ModularAvatarMeshSettings>();
            if (modularAvatarMeshSettings != null) return;
            var HipsReference = _prj.HumanoidObjectReference(HumanBodyBones.Hips);
            modularAvatarMeshSettings = _root.AddComponent<ModularAvatarMeshSettings>();
            modularAvatarMeshSettings.InheritProbeAnchor = ModularAvatarMeshSettings.InheritMode.Set;
            modularAvatarMeshSettings.ProbeAnchor = HipsReference;
            modularAvatarMeshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.Set;
            modularAvatarMeshSettings.RootBone = HipsReference;

            var (totalBoundsCenterX, s1) = _prj.Get<float>("TotalBoundsCenterX");
            var (totalBoundsCenterY, s2) = _prj.Get<float>("TotalBoundsCenterY");
            var (totalBoundsCenterZ, s3) = _prj.Get<float>("TotalBoundsCenterZ");
            var (totalBoundsExtentX, s4) = _prj.Get<float>("TotalBoundsExtentX");
            var (totalBoundsExtentY, s5) = _prj.Get<float>("TotalBoundsExtentY");
            var (totalBoundsExtentZ, s6) = _prj.Get<float>("TotalBoundsExtentZ");
            if (!(s1 && s2 && s3 && s4 && s5 && s6)) return;

            modularAvatarMeshSettings.Bounds = new Bounds()
            {
                center = new Vector3(totalBoundsCenterX, totalBoundsCenterY, totalBoundsCenterZ),
                extents = new Vector3(totalBoundsExtentX, totalBoundsExtentY, totalBoundsExtentZ)
            };
        }
    }
}