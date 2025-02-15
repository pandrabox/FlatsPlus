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
using System.Globalization;


namespace com.github.pandrabox.flatsplus.editor
{
    /// <summary>
    /// PandraProjectにFlats用DB機能を追加したクラス
    /// </summary>
    public class FlatsProject : PandraProject
    {

#if PANDRADBG
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
        //            LowLevelDebugPrint($@"x::{AttributePoint.x},  name:{p.RootObject.name}");
        //        }
        //    }
        //}
#endif

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


        private bool initialized = false;
        private AvatarType _currentAvatarType;
        private AvatarType CurrentAvatarType
        {
            get
            {
                if (!initialized)
                {
                    initialized = true;
                    Initialization();
                }
                return _currentAvatarType;
            }
        }



        private const int ATTRIBUTEINDEX = 26;
        private const int FLOORINDEX = 7;
        private const string CSVPATH = "Packages/com.github.pandrabox.flatsplus/Assets/FlatsDB/db.csv";
        private Dictionary<string, Dictionary<string, string>> data;
        private Dictionary<string, Dictionary<string, string>> Data
        {
            get
            {
                if (!initialized)
                {
                    initialized = true;
                    Initialization();
                }
                return data;
            }
        }
        public FlatsProject(VRCAvatarDescriptor desc)
        {
            Init(desc, "FlatsPlus", ProjectTypes.VPM);
        }


        private void Initialization()
        {
            data = new Dictionary<string, Dictionary<string, string>>();
            if (!File.Exists(CSVPATH))
            {
                LowLevelDebugPrint($@"CSV:{CSVPATH}が見つかりませんでした");
                return;
            }
            string tmpPath = Path.Combine(TmpFolder, $"flatDB{Guid.NewGuid()}.csv");
            CreateDir(TmpFolder);
            File.Copy(CSVPATH, tmpPath, true);

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
            LowLevelDebugPrint("AttributePointによる判定に失敗しました");

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
            LowLevelDebugPrint("AnimatorAvatarNameによる判定に失敗しました");

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
            LowLevelDebugPrint("アバターの判定に失敗しました");
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
                    LowLevelDebugPrint($@"DBの取得に失敗しました (値がnull): {avatarType},{key}");
                    return (default(T), false);
                }

                try
                {
                    T result = (T)Convert.ChangeType(valueStr, typeof(T), CultureInfo.InvariantCulture);
                    return (result, true);
                }
                catch (Exception ex)
                {
                    LowLevelDebugPrint($@"DBの取得に失敗しました (型変換エラー): {avatarType},{key}, 値: {valueStr} - {ex.Message}");
                    return (default(T), false);
                }
            }

            LowLevelDebugPrint($@"DBの取得に失敗しました (データ見つからず): {avatarType},{key}");
            return (default(T), false);
        }


        //メソッドチェーンのオーバーライド
        public new FlatsProject SetSuffixMode(bool mode)
        {
            base.SetSuffixMode(mode);  // 基底クラスのSetSuffixModeを呼び出す
            return this;  // FlatsProject型を返す
        }
    }
}