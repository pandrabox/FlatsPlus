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
using VRC.SDK3.Dynamics.PhysBone.Components;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPTailDebug
    {
        [MenuItem("PanDbg/FPTail")]
        public static void FPTail_Debug()
        {
            SetDebugMode(true);
            new FPTailMain(TopAvatar);
        }
    }
#endif

    internal class FPTailPass : Pass<FPTailPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FPTailMain(ctx.AvatarDescriptor);
        }
    }

    public class FPTailMain
    {
        /// <summary>
        /// targetをrotationAngleで回転させた角度(Quaternion)を返す
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rotationAngle"></param>
        /// <returns></returns>
        public static Quaternion CalQuaternionRotation(GameObject target, Vector3 rotationAngle)
        {
            Quaternion currentRotation = target.transform.localRotation;
            Quaternion deltaRotation = Quaternion.Euler(rotationAngle);
            Quaternion newRotation = currentRotation * deltaRotation;
            return newRotation;
        }
        public static Quaternion CalcQuaternionRotationY(GameObject target, float yAngle)=>CalQuaternionRotation(target, new Vector3(0, yAngle, 0));



        public FPTailMain(VRCAvatarDescriptor desc)
        {
            FPTail[] fpTails = desc.GetComponentsInChildren<FPTail>();
            if (fpTails.Length == 0) return;

            PandraProject prj = FlatsPlusProject(desc).SetSuffixMode(false);
            FlatsDB db = new FlatsDB(prj);

            GameObject tail = prj.ArmatureTransform.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == db.TailName)?.gameObject;
            if (tail == null) { LowLevelDebugPrint("Tailがみつかりませんでした"); return; }
            var tailPB = tail.GetComponent<VRCPhysBone>();
            if (tailPB == null) tailPB = tail.AddComponent<VRCPhysBone>();

            tailPB.isAnimated = true;
            
            float swingAngle = 60f;
            float swingTime = 2f;

            var ac = new AnimationClipsBuilder();
            Quaternion[] sPos = new Quaternion[3];
            sPos[0]= CalcQuaternionRotationY(tail, swingAngle);
            sPos[1] = CalcQuaternionRotationY(tail, -swingAngle);
            ac.Clip($@"Swing").SetLoop(true);
            for (int i = 0; i < 4; i++)
            {
                Axis axis = (Axis)i;
                string axisName = axis.ToString().ToLower();
                ac.CurrentClip
                    .Bind("", typeof(Transform), $"m_LocalRotation.{axisName}")
                    .Smooth(
                        swingTime * 0, sPos[0].GetAxis(axis)
                        , swingTime * 1, sPos[1].GetAxis(axis)
                        , swingTime * 2, sPos[0].GetAxis(axis)
                    )
                    .SetAllFlat();
            }

            var ab = new AnimatorBuilder("Tail").AddLayer();
            ab.SetMotion(ac.Outp("Swing"));
            ab.BuildAndAttach(tail, true);
        }
    }
}