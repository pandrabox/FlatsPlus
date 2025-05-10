#region
using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Util;
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
                var fp = new FlatsProject(a);
                new FPEmoWork(fp);
            }
        }
    }
#endif


    public class FPEmoWork : FlatsWork<FPEmo>
    {
        private AnimationClipsBuilder clips;
        private Dictionary<string, bool> _disHoppeInfo;
        private string[] _disHoppeShapes;
        private Dictionary<string, bool> _blushInfo;
        private string _blushShape => _prj.OriginalBlushName;
        private string emoDataFolder = "Packages/com.github.pandrabox.flatsplus/Assets/Emo/res/";
        private string _defaultEmoDataPath => $@"{emoDataFolder}{_prj.CurrentAvatarName}.csv";
        private string _emoDataFile
        {
            get
            {
                string emoPath = "";
                if (_config?.D_Emo_Preset == "Custom")
                {
                    emoPath = _config?.D_Emo_ConfigFilePath;
                    if (IsEmoDataFile(emoPath)) return emoPath;
                }

                if (_config?.D_Emo_Preset != "Auto")
                {
                    emoPath = _config?.D_Emo_Preset;
                    if (IsEmoDataFile(emoPath)) return emoPath;
                }
                return _defaultEmoDataPath;
            }
        }
        private bool IsEmoDataFile(string path)
        {
            if (path == null) return false;

            // 存在しなければfalse
            if (!File.Exists(path))
            {
                Log.I.Info($"Emoファイルが存在しません: {path}");
                return false;
            }

            try
            {
                // 読み込む。失敗したらfalse
                string[] lines = File.ReadAllLines(path);

                // 66行未満ならばfalse (ヘッダー + 64 表情 + 余裕分)
                if (lines.Length < 66)
                {
                    Log.I.Info($"Emoファイルの行数が足りません: {lines.Length}行 (66行以上必要)");
                    return false;
                }

                // 1行目がLeft,Right,で開始していなければfalse
                string header = lines[0];
                if (!header.StartsWith("Left,Right,"))
                {
                    Log.I.Info($"Emoファイルのヘッダー形式が不正です: {header}");
                    return false;
                }

                // すべてのチェックをパスした場合はtrue
                return true;
            }
            catch
            {
                Log.I.Info($"Emoファイルの検証中にエラーが発生しました: {path}");
                return false;
            }
        }

        public FPEmoWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            _disHoppeShapes = _prj.DisHoppeShapes;
            _disHoppeInfo = new Dictionary<string, bool>();
            _blushInfo = new Dictionary<string, bool>();
            RemoveExistEmo();
            CreateEmoClips();
            CreateAndAttachEmoController();
            ApplyConstraints();
        }

        // 既存のレイヤにEmoへの遷移がある場合、遷移先をDummyClipに変更
        private void RemoveExistEmo()
        {
            Log.I.Info("RemoveExistEmo");
            var ac = new AnimationClipsBuilder();
            for (int m = 0; m < _desc.baseAnimationLayers.Length; m++)
            {
                var runtimePlayable = _desc.baseAnimationLayers[m].animatorController;
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
                _desc.baseAnimationLayers[m].animatorController = playable;
                _desc.baseAnimationLayers[m].isDefault = false;
            }
        }
        // 表情クリップ生成
        private void CreateEmoClips()
        {
            string dataText = File.ReadAllText(_emoDataFile);
            string[] lines = dataText.Split('\n');
            clips = new AnimationClipsBuilder();
            string[] shapeNames = lines[0].Split(',');
            for (int i = 0; i < 64; i++)
            {
                bool isBlank = true;
                bool isDisHoppe = false;
                bool isBlush = false;
                string clipName = $@"emo{i}";
                //Log.I.Info($@"{i + 1}:line:{lines[i + 1]}");
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
                    bool addThis = true;
                    if (!shapeVals[n].HasValue) continue;
                    int shapeVal = (int)shapeVals[n];
                    string shapeName = "blendShape." + shapeNames[n];
                    if (shapeVal > 5 && _disHoppeShapes.Contains(shapeNames[n]))
                    {
                        isDisHoppe = true;
                    }
                    if (shapeNames[n] == _blushShape)
                    {
                        isBlush = true;
                        if (_config.D_Hoppe_Blush) addThis = false; //頬を自動制御する場合はGestureによる制御を無効化
                    }
                    if (addThis)
                    {
                        clips.Clip(clipName).Bind("Body", typeof(SkinnedMeshRenderer), shapeName).Const2F(shapeVal);
                        isBlank = false;
                    }
                    //Log.I.Info($@"{i}:{shapeName}:{shapeVal}");
                }
                _disHoppeInfo.Add(clipName, isDisHoppe);
                _blushInfo.Add(clipName, isBlush);
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
            ab.AddAnimatorParameter(_prj.IsEmoBlush);
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
                    ab.SetParameterDriver(_prj.IsEmoBlush, _blushInfo[clipName] ? 1 : 0);
                }
            }
            for (int nTo = 0; nTo < GESTURENUM * GESTURENUM; nTo++)
            {
                int left = nTo / GESTURENUM;
                int right = nTo % GESTURENUM;
                ab.ChangeCurrentState(states[nTo]).TransFromAny(transitionDuration: _tgt.TransitionTime)
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
            bb.RootDBT(() =>
            {
                bb.Param("1").FAssignmentBy1D(_prj.IsDance, 0, 1, "FlatsPlus/Emo/Disable", 0, 1);
            });
            bb.Attach(_prj.RootObject);
        }
    }

}