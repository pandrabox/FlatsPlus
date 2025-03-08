using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.ndmf;
using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FlatsPlusDebug
    {
        [MenuItem("PanDbg/**FlatsPlus", priority = 0)]
        public static void FlatsPlus_Debug()
        {
            SetDebugMode(true);
            new FlatsPlusMain(TopAvatar);
        }
    }
#endif

    internal class FlatsPlusPass : Pass<FlatsPlusPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FlatsPlusMain(ctx.AvatarDescriptor);
        }
    }

    public class FlatsPlusMain
    {
        VRCAvatarDescriptor _desc;
        FlatsProject _p;
        private int totalSteps = 16; // 15から16に変更
        private int currentStep = 0;

        public FlatsPlusMain(VRCAvatarDescriptor desc)
        {
            try
            {
                PanLog.Clear(LogFilePath);
                PanLog.SetLogPath(LogFilePath);
                LowLevelDebugPrint("FlatPlus Start Works");
                LowLevelDebugPrint($@"@@BuildStartDateTime@@,{DateTime.Now}");
                AppearPackageInfo();
                _desc = desc.NullCheck();
                _p = new FlatsProject(_desc, true);
                FlatsPlus fp = _desc.GetComponentInChildren<FlatsPlus>().NullCheck();

                Localizer.SetLanguage(fp.Language);

                CreateWork<FPCarryWork>(fp.Func_Carry, "Carry");
                CreateInstantiate(fp.Func_DanceController, "DanceController");
                CreateWork<FPEmoWork>(fp.Func_Emo, "Emo");
                CreateWork<FPExploreWork>(fp.Func_Explore, "Explore");
                CreateWork<FPHoppeWork>(fp.Func_Hoppe, "Hoppe");
                CreateWork<FPIcoWork>(fp.Func_Ico, "Ico");
                CreateInstantiate(fp.Func_Sync, "Sync");
                CreateWork<FPLightWork>(fp.Func_Light, "Light");
                CreateInstantiate(fp.Func_Link, "Link");
                CreateWork<FPMakeEmoWork>(fp.Func_MakeEmo, "MakeEmo");
                CreateWork<FPMeshSettingWork>(fp.Func_MeshSetting, "MeshSetting");
                CreateInstantiate(true, "MessageUI");
                CreateWork<FPMoveWork>(fp.Func_Move, "Move");
                CreateWork<FPOnakaWork>(fp.Func_Onaka, "Onaka");
                CreateWork<FPPenWork>(fp.Func_Pen, "Pen");
                CreateInstantiate(fp.Func_PoseClipper, "FPPoseClipper");
                CreateWork<FPSleepWork>(fp.Func_Sleep, "Sleep");
                CreateInstantiate(fp.Func_Sync, "Sync");
                CreateWork<FPTailWork>(fp.Func_Tail, "Tail");
                CreateInstantiate(fp.Func_WriteDefaultOn, "WriteDefaultOn");
            }
            catch (Exception ex)
            {
                AppearError(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                LowLevelDebugPrint("FlatPlus Complete Works");
            }
        }

        private void CreateInstantiate(bool runCondition, string path) => CreateWork<FlatsWorkBase>(runCondition, path);

        private void CreateWork<T>(bool runCondition, string path) where T : FlatsWorkBase
        {
            try
            {
                if (!runCondition) return;

                currentStep++;
                EditorUtility.DisplayProgressBar(L("FlatsPlus_Progress"), $"{path}", (float)currentStep / totalSteps);

                if (!path.EndsWith(".prefab")) path = $"{path}/{path}.prefab";
                path = $@"{FlatsPlusAssetsPath}/{path}";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path).NullCheck(path);
                GameObject go = GameObject.Instantiate(prefab);
                go.transform.SetParent(_p.PrjRootObj.transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                if (go.name.EndsWith("(Clone)"))
                {
                    go.name = go.name.Replace("(Clone)", "").Trim();
                }

                if (typeof(T) == typeof(FlatsWorkBase))
                {
                    //何もしない
                }
                else if (typeof(T) == typeof(FPOnakaWork))
                {
                    Activator.CreateInstance(typeof(T), _p, false);
                }
                else
                {
                    Activator.CreateInstance(typeof(T), _p);
                }
            }
            catch (Exception ex)
            {
                AppearError(ex);
            }
        }
    }
}
