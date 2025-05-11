#region
using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.Util;
#endregion

namespace com.github.pandrabox.flatsplus.editor
{
    /// <summary>
    /// Flatsに特化したPandraProject
    /// </summary>
    public class FlatsProject : PandraProject
    {

#if PANDRADBG
        #region
        //public static class FlatsPlusFlatsProjectDebug
        //{
        //    [MenuItem("PanDbg/FlatsProjectDebug")]
        //    public static void FlatsProjectDebug()
        //    {
        //        foreach (var a in AllAvatar)
        //        {
        //            PandraProject p = FlatsPlusProject(a);
        //            p.SetDebugMode(true); // 修正: インスタンスメソッドとして呼び出し
        //            var fdb = new FlatsProject(p);
        //            var body = p.RootTransform.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(x => x.name == "Body");
        //            var AttributePoint = body.sharedMesh.vertices[26];
        //            Log.I.Info($@"x::{AttributePoint.x},  name:{p.RootObject.name}");
        //        }
        //    }
        //}
        #endregion
#endif
        public FlatsProject(VRCAvatarDescriptor desc, bool ImmediateInitialize = false)
        {
            Init(desc, "FlatsPlus", ProjectTypes.VPM);
            Config = desc.GetComponentInChildren<FlatsPlus>();
            if (Config == null)
            {
                if (PDEBUGMODE)
                {
                    Config = new FlatsPlus();
                }
                else
                {
                    Log.I.Info("FlatsPlusプレハブが見つかりませんでした。アバター内にFlatsPlusプレハブがない場合、FlatsPlusは動作しません。");
                }
            }
            if (ImmediateInitialize)
            {
                Initialization();
            }
        }
        public FlatsPlus Config = null;
        public string CurrentAvatarName => GetAvatarName(CurrentAvatarType);
        public string TailName => GetDBString("TailName");
        public float TailScaleLimit0 => GetDBFloat("TailScaleLimit0");
        public float TailScaleXLimit0 => GetDBFloat("TailScaleXLimit0");
        public float TailScaleLimit1 => GetDBFloat("TailScaleLimit1");
        public string GetDBString(string key) { var (a, b) = Get<string>(key); return b ? a : null; }
        public float GetDBFloat(string key) { var (a, b) = Get<float>(key); return b ? a : -1; }
        public (T value, bool success) Get<T>(string key) => GetDirect<T>(CurrentAvatarType, key);
        public float TailColliderSize => GetDBFloat("TailColliderSize");
        public string TailColliderCurve => GetDBString("TailColliderCurve");
        public float OnakaRadius => GetDBFloat("OnakaRadius");
        public float OnakaY1 => GetDBFloat("OnakaY1");
        public float OnakaZ1 => GetDBFloat("OnakaZ1");
        public float OnakaY2 => GetDBFloat("OnakaY2");
        public float OnakaZ2 => GetDBFloat("OnakaZ2");
        public float OnakaCurveTop => GetDBFloat("OnakaCurveTop");
        public float PinY => GetDBFloat("PinY");
        public float FaceCapSize => GetDBFloat("FaceCapSize");
        public float FaceCapX => GetDBFloat("FaceCapX");
        public float FaceCapY => GetDBFloat("FaceCapY");
        public float FaceCapZ => GetDBFloat("FaceCapZ");
        public float Hoppe2X => GetDBFloat("Hoppe2X");
        public float Hoppe2Y => GetDBFloat("Hoppe2Y");
        public float Hoppe2Z => GetDBFloat("Hoppe2Z");
        public string[] DisHoppeShapes => GetDBString("DisHoppeShapes")?.Split('-');
        public float TotalBoundsCenterX => GetDBFloat("TotalBoundsCenterX");
        public float TotalBoundsCenterY => GetDBFloat("TotalBoundsCenterY");
        public float TotalBoundsCenterZ => GetDBFloat("TotalBoundsCenterZ");
        public float TotalBoundsExtentX => GetDBFloat("TotalBoundsExtentX");
        public float TotalBoundsExtentY => GetDBFloat("TotalBoundsExtentY");
        public float TotalBoundsExtentZ => GetDBFloat("TotalBoundsExtentZ");
        public string OriginalBlushName => GetDBString("OriginalBlushName");
        public Vector3 MultiHeadPos => GetAtVector3("MultiHeadPos");
        public Vector3 MultiHeadScale => Vector3.one * GetDBFloat("MultiHeadScale");

        public string LinkRx => "FlatsPlus/Link/Rx";
        public string LinkTx => "FlatsPlus/Link/Tx";

        public bool BuildTargetIsPC
        {
            get
            {
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                return target == BuildTarget.StandaloneWindows ||
                       target == BuildTarget.StandaloneWindows64 ||
                       target == BuildTarget.StandaloneOSX ||
                       target == BuildTarget.StandaloneLinux64;
            }
        }

        public Vector3 TotalBoundsCenter => new Vector3(TotalBoundsCenterX, TotalBoundsCenterY, TotalBoundsCenterZ);
        public Vector3 TotalBoundsExtent => new Vector3(TotalBoundsExtentX, TotalBoundsExtentY, TotalBoundsExtentZ);

        public string IsEmoBlush => "FlatsPlus/Emo/IsEmoBlush";

        public string CheekSensor => "FlatsPlus/CheekSensor";

        public string FPRoot => "Packages/com.github.pandrabox.flatsplus/";
        public string FPAseets => $@"{FPRoot}Assets/";
        public string FPFlatsCSV => $@"{FPAseets}FlatsDB/db.csv";

        public Dictionary<string, FaceType> GeneralShapes
        {
            get
            {
                Dictionary<string, FaceType> s = new Dictionary<string, FaceType>();
                foreach (FaceType type in Enum.GetValues(typeof(FaceType)))
                {
                    string shapes = GetDBString($@"{type}Shapes");
                    string[] splitShapes = shapes.Split('-');
                    foreach (var shape in splitShapes)
                    {
                        s.Add(shape, type);
                    }
                }
                return s;
            }
        }

        private bool initialized = false;
        private AvatarType _currentAvatarType;
        private AvatarType CurrentAvatarType
        {
            get
            {
                if (!initialized)
                {
                    Initialization();
                }
                return _currentAvatarType;
            }
        }

        private const int ATTRIBUTEINDEX = 26;
        private const int FLOORINDEX = 7;

        private Vector3 GetAtVector3(string key)
        {
            var v = GetDBString(key);
            var split = v.Split('@');
            return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
        }

        private Dictionary<string, Dictionary<string, string>> data;
        private Dictionary<string, Dictionary<string, string>> Data
        {
            get
            {
                if (!initialized)
                {
                    Initialization();
                }
                return data;
            }
        }


        private void Initialization()
        {
            try
            {
                initialized = true;
                data = new Dictionary<string, Dictionary<string, string>>();
                if (!File.Exists(FPFlatsCSV))
                {
                    Log.I.Error($@"CSV:{FPFlatsCSV}が見つかりませんでした");
                    return;
                }
                string tmpPath = Path.Combine(TmpFolder, $"flatDB{Guid.NewGuid()}.csv");
                CreateDir(TmpFolder);
                File.Copy(FPFlatsCSV, tmpPath, true);

                string[] lines = File.ReadAllLines(tmpPath);
                if (lines.Length < 2) return;

                string[] headers = lines[0].Split(',');

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = lines[i].Split(',');
                    if (values.Length < 2) continue;

                    string rowKey = values[0];

                    if (!data.ContainsKey(rowKey)) data[rowKey] = new Dictionary<string, string>();

                    for (int j = 1; j < values.Length; j++)
                    {
                        if (!string.IsNullOrWhiteSpace(values[j]))
                        {
                            data[rowKey][headers[j]] = values[j];
                        }
                    }
                }
                SetCurrentAvatar();
                Log.I.Info($@"FlatsProject Initialized By {CurrentAvatarName}");
            }
            catch
            {
                initialized = false;
            }
        }


        private string GetAvatarName(AvatarType avatar) => Enum.GetName(typeof(AvatarType), avatar).ToLower();

        private float AttributeFloor(float a)
        {
            return Mathf.Floor(a * Mathf.Pow(10, FLOORINDEX));
        }

        public void SetCurrentAvatar()
        {
            var body = RootTransform?.GetComponentsInChildren<SkinnedMeshRenderer>()?.FirstOrDefault(x => x.name == "Body");
            Vector3? attributePoint = body?.sharedMesh?.vertices[ATTRIBUTEINDEX];
            if (attributePoint != null)
            {
                float attributeVal = AttributeFloor(((Vector3)attributePoint).x);
                foreach (AvatarType avatarType in Enum.GetValues(typeof(AvatarType)))
                {
                    if (avatarType == AvatarType.Undef) continue;
                    var (attributePosX, stat) = GetDirect<float>(avatarType, "AttributePosX");
                    if (stat && attributeVal == AttributeFloor(attributePosX))
                    {
                        _currentAvatarType = avatarType;
                        return;
                    }
                }
            }
            Log.I.Info("AttributePointによる判定に失敗しました");

            var tmpAvatarName = Animator?.avatar?.name;
            if (tmpAvatarName != null)
            {
                foreach (AvatarType avatarType in Enum.GetValues(typeof(AvatarType)))
                {
                    if (avatarType == AvatarType.Undef) continue;
                    var (ttAvatarName, stat) = GetDirect<string>(avatarType, "AnimatorAvatarName");
                    if (stat && ttAvatarName == tmpAvatarName)
                    {
                        _currentAvatarType = avatarType;
                        return;
                    }
                }
            }
            Log.I.Info("AnimatorAvatarNameによる判定に失敗しました");

            tmpAvatarName = RootObject?.name;
            if (tmpAvatarName != null)
            {
                foreach (AvatarType avatarType in Enum.GetValues(typeof(AvatarType)))
                {
                    if (avatarType == AvatarType.Undef) continue;
                    var (ttAvatarName, stat) = GetDirect<string>(avatarType, "FormalName");
                    if (stat && ttAvatarName == tmpAvatarName)
                    {
                        _currentAvatarType = avatarType;
                        return;
                    }
                }
            }
            Log.I.Warning("アバターの判定に失敗しました");
        }

        /// <summary>
        /// 辞書参照の基本
        /// </summary>
        public (T value, bool success) GetDirect<T>(AvatarType avatarType, string key)
        {
            if (Data.TryGetValue(key, out var row) && row.TryGetValue(GetAvatarName(avatarType), out var rawValue))
            {
                string valueStr = rawValue?.ToString();
                if (valueStr == null)
                {
                    Log.I.Error($@"DBの取得に失敗しました (値がnull): {avatarType},{key}");
                    return (default(T), false);
                }

                try
                {
                    T result = (T)Convert.ChangeType(valueStr, typeof(T), CultureInfo.InvariantCulture);
                    return (result, true);
                }
                catch (Exception ex)
                {
                    Log.I.Exception(ex, $@"DBの取得に失敗しました (型変換エラー): {avatarType},{key}, 値: {valueStr}");
                    return (default(T), false);
                }
            }

            Log.I.Error($@"DBの取得に失敗しました (データ見つからず): {avatarType},{key}");
            return (default(T), false);
        }


        [Obsolete]
        public new FlatsProject SetSuffixMode(bool mode)
        {
            base.SetSuffixMode(mode);  // 基底クラスのSetSuffixModeを呼び出す
            return this;  // FlatsProject型を返す
        }
    }
}