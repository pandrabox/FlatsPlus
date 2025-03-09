using com.github.pandrabox.flatsplus.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPMeshSettingDebug
    {
        [MenuItem("PanDbg/FPMeshSetting")]
        public static void FPMeshSetting_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var ms = a.gameObject.GetComponent<ModularAvatarMeshSettings>();
                if (ms != null) GameObject.DestroyImmediate(ms);

                new FPMeshSettingWork(a.FP());
            }
        }
    }
#endif

    public class FPMeshSettingWork : FlatsWork<FPMeshSetting>
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