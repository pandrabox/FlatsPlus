// Header
#region
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
using VRC.Dynamics;
using System.Globalization;
using System.ComponentModel;
#endregion

namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPEmoDebug
    {
        [MenuItem("PanDbg/FPEmo")]
        public static void FPEmo_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new FPEmoMain(a);
            }
        }
    }
#endif

    internal class FPEmoPass : Pass<FPEmoPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new FPEmoMain(ctx.AvatarDescriptor);
        }
    }

    public class FPEmoMain
    {
        private FPEmo _FPEmo;
        private FlatsProject _prj;
        private AnimationClipsBuilder clips;
        private Dictionary<string, bool> _disHoppeInfo;
        private string[] _disHoppeShapes;
        private string emoDataFolder = "Packages/com.github.pandrabox.flatsplus/Assets/Emo/res/";
        private string emoDataFile => $@"{emoDataFolder}{_prj.CurrentAvatarName}.csv";

        public FPEmoMain(VRCAvatarDescriptor desc)
        {
            _FPEmo = desc.GetComponentInChildren<FPEmo>();
            if (_FPEmo == null) return;
            _prj = new FlatsProject(desc);
            _disHoppeShapes = _prj.DisHoppeShapes;
            _disHoppeInfo = new Dictionary<string, bool>();
            RemoveExistEmo();
            CreateEmoClips();
            CreateAndAttachEmoController();
            ApplyConstraints();
        }

        // 既存のレイヤにEmoへの遷移がある場合、遷移先をDummyClipに変更
        private void RemoveExistEmo()
        {
            var ac = new AnimationClipsBuilder();
            for (int m = 0; m < _prj.Descriptor.baseAnimationLayers.Length; m++)
            {
                var runtimePlayable = _prj.Descriptor.baseAnimationLayers[m].animatorController;
                if (runtimePlayable == null) continue;
                var playable = runtimePlayable as AnimatorController;
                for (int i = playable.layers.Length - 1; i > 0; i--)
                {
                    var layer = playable.layers[i];
                    AnimatorLayerAnalyzer la = new AnimatorLayerAnalyzer(layer);
                    var emos = la.EmoTransitions(_prj);
                    foreach (var emo in emos)
                    {
                        emo.Transition.destinationState.motion = ac.DummyClip;
                    }
                }
                _prj.Descriptor.baseAnimationLayers[m].animatorController = playable;
                _prj.Descriptor.baseAnimationLayers[m].isDefault = false;
            }
        }
        // 表情クリップ生成
        private void CreateEmoClips()
        {
            string dataText = File.ReadAllText(emoDataFile);
            string[] lines = dataText.Split('\n');
            if (lines.Length < 66)
            {
                LowLevelExeption("表情データに異常があります");
                return;
            }
            clips = new AnimationClipsBuilder();
            string[] shapeNames = lines[0].Split(',');
            for (int i = 0; i < 64; i++)
            {
                bool isBlank = true;
                bool isDisHoppe = false;
                string clipName= $@"emo{i}";
                //LowLevelDebugPrint($@"{i + 1}:line:{lines[i + 1]}");
                int?[] shapeVals = lines[i + 1].Split(',').Select(x =>
                {
                    if (int.TryParse(x, out int result))
                    {
                        return (int?)result;
                    }
                    return null;
                }).ToArray();
                for (int n = 2; n < shapeVals.Length; n++) //0,1はGesture情報
                {
                    if (!shapeVals[n].HasValue) continue;
                    int shapeVal = (int)shapeVals[n];
                    string shapeName = "blendShape." + shapeNames[n];
                    clips.Clip(clipName).Bind("Body", typeof(SkinnedMeshRenderer), shapeName).Const2F(shapeVal);
                    if(shapeVal > 5 && _disHoppeShapes.Contains(shapeNames[n]))
                    {
                        isDisHoppe = true;
                    }
                    isBlank = false;
                    //LowLevelDebugPrint($@"{i}:{shapeName}:{shapeVal}");
                }
                _disHoppeInfo.Add(clipName, isDisHoppe);
                if (isBlank)
                {
                    clips.Clip(clipName).Dummy();
                }
                //OutpAsset(clips.Outp($@"emo{i}"));
            }
        }
        // 表情制御コントローラの生成とアタッチ
        private void CreateAndAttachEmoController()
        {
            var ab = new AnimatorBuilder("FlatsPlus/Emo");
            ab.AddAnimatorParameter("FlatsPlus/Emo/IsDisHoppe");
            List<AnimatorState> states = new List<AnimatorState>();
            ab.AddLayer();
            for (int left = 0; left < GESTURENUM; left++)
            {
                for (int right = 0; right < GESTURENUM; right++)
                {
                    int index = left * GESTURENUM + right;
                    string clipName = $@"emo{index}";
                    AnimationClip currentClip = clips.Outp(clipName);
                    int offset = 200;
                    Vector3 pos = new Vector3(left * 250, right * 100 + offset, 0);
                    ab.AddState(clipName, currentClip, position: pos);
                    states.Add(ab.CurrentState);
                    ab.SetParameterDriver("FlatsPlus/Emo/IsDisHoppe", _disHoppeInfo[clipName] ? 1 : 0);
                }
            }
            for (int nTo = 0; nTo < GESTURENUM * GESTURENUM; nTo++)
            {
                int left = nTo / GESTURENUM;
                int right = nTo % GESTURENUM;
                ab.ChangeCurrentState(states[nTo]).TransFromAny(transitionDuration: _FPEmo.TransitionTime)
                    .AddCondition(AnimatorConditionMode.Equals, left, "GestureLeft")
                    .AddCondition(AnimatorConditionMode.Equals, right, "GestureRight")
                    .AddCondition(AnimatorConditionMode.Less, 0.5f, "FlatsPlus/Emo/Disable");
            }
            ab.ChangeCurrentState(ab.InitialState).TransFromAny()
                .AddCondition(AnimatorConditionMode.Greater, 0.5f, "FlatsPlus/Emo/Disable");
            ab.Attach(_prj.RootObject, true);

        }

        private void ApplyConstraints()
        {
            var ac = new AnimationClipsBuilder();
            // Dance対応
            var bb = new BlendTreeBuilder("FlatsPlus/EmoConstraints");
            bb.RootDBT(() => {
                bb.Param("1").FAssignmentBy1D(_prj.IsDance, 0, 1, "FlatsPlus/Emo/Disable", 0, 1);
            });
            bb.Attach(_prj.RootObject);
        }
    }

}