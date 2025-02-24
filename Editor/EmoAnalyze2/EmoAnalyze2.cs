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
using System.Security.Cryptography;
using System.Text;

namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class EmoAnalyze2MenuDefinition
    {
        [MenuItem("PanDbg/**EmoAnalyze2r")]
        private static void EmoAnalyze2M()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                LowLevelDebugPrint($"EmoAnalyze2: {a.name}");
                new EmoAnalyze2(a);
            }
        }
        //[MenuItem("PanDbg/**EmoAnalyze2r_ForSetting")]
        //private static void EmoAnalyze2M_ForSetting()
        //{
        //    SetDebugMode(true);
        //    new EmoAnalyze2(TopAvatar).Setting();
        //}
    }
#endif

    public class EmoAnalyze2
    {
        FlatsProject _prj;
        EmoInfo _emoInfo;
        public EmoAnalyze2(VRCAvatarDescriptor desc)
        {
            _prj = new FlatsProject(desc);

            SkinnedMeshRenderer bodyMesh = _prj.RootTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(mesh => mesh.name == "Body");
            _emoInfo = new EmoInfo(bodyMesh);

            AnimatorController ac = (AnimatorController)_prj.FXLayer.animatorController;
            foreach (var layer in ac.layers)
            {
                HashSet<AnimatorStateTransition> uniqueTransitions = new HashSet<AnimatorStateTransition>();
                foreach (var state in layer.stateMachine.states)
                {
                    foreach (var transition in state.state.transitions)
                    {
                        uniqueTransitions.Add(transition);
                    }
                }
                foreach (var transition in layer.stateMachine.anyStateTransitions)
                {
                    uniqueTransitions.Add(transition);
                }
                var filteredTransitions = uniqueTransitions
                    .Where(t => t.destinationState != null)
                    .Where(t => t.conditions.Any(c => c.parameter == "GestureRight" || c.parameter == "GestureLeft"))
                    .ToList();
                foreach (var transition in filteredTransitions)
                {
                    (int? left, int? right) = GetThreshold(transition);
                    AnimationClip anim = transition.destinationState.motion as AnimationClip;
                    _emoInfo.AnimAnalyze(right, left, anim);
                }
            }
            save();
        }


        private void save()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Left,Right,");
            foreach (var key in _emoInfo.BlendShapeKeys)
            {
                sb.Append($"{key},");
            }
            sb.AppendLine("");
            for (int left = 0; left < EmoInfo.GestureCount; left++)
            {
                for (int right = 0; right < EmoInfo.GestureCount; right++)
                {
                    sb.Append($"{left},{right},");
                    int index = right + left * EmoInfo.GestureCount;
                    for (var i = 0; i < _emoInfo.ShapeVals[index].Length; i++)
                    {
                        int? val = _emoInfo.ShapeVals[index][i];
                        sb.Append($@"{val},");
                    }
                    sb.AppendLine("");
                }
            }
            string data = sb.ToString();
            string path = $"Packages/com.github.pandrabox.flatsplus/Editor/EmoAnalyze2/result/{_prj.CurrentAvatarName}.csv";
            File.WriteAllText(path, data);
        }

        private (int? Left, int? Right) GetThreshold(AnimatorStateTransition transition)
        {
            int?[] thresholds = new int?[2];
            for (int n = 0; n < 2; n++)
            {
                string parameter = n == 0 ? "GestureRight" : "GestureLeft";
                AnimatorCondition[] cn = transition.conditions.Where(c => c.parameter == parameter).ToArray();
                if (cn == null || cn.Length < 1)
                {
                    thresholds[n] = null;
                }
                else if (cn.Length == 1)
                {
                    thresholds[n] = (int)cn[0].threshold;
                }
                else if (cn.Length == 2)
                {
                    thresholds[n] = (int)((cn[0].threshold + cn[1].threshold) / 2);
                    LowLevelDebugPrint("実験的な処理です");
                }
                else
                {
                    LowLevelExeption($@"Error: {transition.destinationState.name}{parameter}は複雑すぎて解決できません");
                }
            }
            return (thresholds[1], thresholds[0]);
        }
    }

    public class EmoInfo
    {
        public string[] BlendShapeKeys;
        public int?[][] ShapeVals;
        public const int GestureCount = 8;
        public EmoInfo(SkinnedMeshRenderer bodymesh)
        {
            int blendShapeCount = bodymesh.sharedMesh.blendShapeCount;
            BlendShapeKeys = new string[blendShapeCount];
            for (int i = 0; i < blendShapeCount; i++)
            {
                BlendShapeKeys[i] = bodymesh.sharedMesh.GetBlendShapeName(i);
            }
            ShapeVals = new int?[GestureCount * GestureCount][];
            for (int i = 0; i < GestureCount * GestureCount; i++)
            {
                ShapeVals[i] = new int?[blendShapeCount];
            }
        }

        public void AnimAnalyze(int? gestureRight, int? gestureLeft, AnimationClip clip)
        {
            if (gestureRight == null && gestureLeft == null) return;
            EditorCurveBinding[] bodyBindings = AnimationUtility.GetCurveBindings(clip);
            //LowLevelDebugPrint($"************************Analyzing clip: {clip.name}");
            foreach (var binding in bodyBindings)
            {
                string propertyName = binding.propertyName.Replace("blendShape.", "");
                int shapeIndex = Array.IndexOf(BlendShapeKeys, propertyName);
                if (shapeIndex >= 0)
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve != null && curve.keys.Length > 0)
                    {
                        int shapeVal = (int)(curve.keys[0].value);
                        SetShapeVal(gestureRight, gestureLeft, shapeIndex, shapeVal);
                    }
                }
            }
        }

        public void SetShapeVal(int? gestureRight, int? gestureLeft, int shapeIndex, int shapeVal)
        {
            if (gestureLeft.HasValue && gestureRight.HasValue)
            {
                int index = gestureRight.Value + gestureLeft.Value * GestureCount;
                ShapeVals[index][shapeIndex] = shapeVal;
            }
            else
            {
                for (int i = 0; i < GestureCount; i++)
                {
                    int index = gestureLeft.HasValue ? i + gestureLeft.Value * GestureCount : gestureRight.Value + i * GestureCount;
                    ShapeVals[index][shapeIndex] = shapeVal;
                }
            }
            string shapeName = BlendShapeKeys[shapeIndex];
            //LowLevelDebugPrint($@"Right:{gestureRight} Left:{gestureLeft} Shape:{shapeIndex} ({shapeName}) Val:{shapeVal}");
        }
    }
}



