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
    public class FlatsDB
    {

#if PANDRADBG
        public class FlatsPlusMeshSettingDebug
        {
            [MenuItem("PanDbg/FlatsDBDebug")]
            public static void FlatsDBDebug()
            {
                SetDebugMode(true);
                foreach (var a in AllAvatar)
                {
                    PandraProject p = FlatsPlusProject(a);
                    var fdb = new FlatsDB(p);
                    var body = p.RootTransform.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(x => x.name == "Body");
                    var AttributePoint = body.sharedMesh.vertices[26];
                    LowLevelDebugPrint($@"x::{AttributePoint.x},  name:{p.RootObject.name}");
                }
            }
        }
#endif

        private const int ATTRIBUTEINDEX = 26;
        private const int FLOORINDEX = 7;
        private const string CSVPATH = "Packages/com.github.pandrabox.flatsplus/Assets/FlatsDB/db.csv";
        private Dictionary<string, Dictionary<string, string>> data;
        private AvatarType _currentAvatar;
        private PandraProject _prj;

        private string GetAvatarName(AvatarType avatar) => Enum.GetName(typeof(AvatarType), avatar).ToLower();
        private string CurrentAvatarName => GetAvatarName(_currentAvatar);

        public FlatsDB(PandraProject prj, AvatarType currentAvatar=0)
        {
            _prj = prj;
            data = new Dictionary<string, Dictionary<string, string>>();            
            if (!File.Exists(CSVPATH))
            {
                LowLevelDebugPrint($@"CSV:{CSVPATH}が見つかりませんでした");
                return;
            }
            string tmpPath = Path.Combine(_prj.TmpFolder, $"flatDB{Guid.NewGuid()}.csv");
            CreateDir(_prj.TmpFolder);
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
            SetCurrentAvatar(currentAvatar);
        }

        private float AttributeFloor(float a)
        {
            return Mathf.Floor(a * Mathf.Pow(10, FLOORINDEX));
        }
        public void SetCurrentAvatar(AvatarType avatar)
        {
            if (avatar == AvatarType.Undef)
            {
                var body = _prj?.RootTransform?.GetComponentsInChildren<SkinnedMeshRenderer>()?.FirstOrDefault(x => x.name == "Body");
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
                            _currentAvatar = avatarType;
                            return;
                        }
                    }
                }
                LowLevelDebugPrint("AttributePointによる判定に失敗しました");

                var tmpAvatarName = _prj?.Animator?.avatar?.name;
                if (tmpAvatarName != null)
                {
                    foreach (AvatarType avatarType in Enum.GetValues(typeof(AvatarType)))
                    {
                        if (avatarType == AvatarType.Undef) continue;
                        var (ttAvatarName, stat) = GetDirect<string>(avatarType, "AnimatorAvatarName");
                        if (stat && ttAvatarName == tmpAvatarName)
                        {
                            _currentAvatar = avatarType;
                            return;
                        }
                    }
                }
                LowLevelDebugPrint("AnimatorAvatarNameによる判定に失敗しました");

                tmpAvatarName = _prj?.RootObject?.name;
                if (tmpAvatarName != null)
                {
                    foreach (AvatarType avatarType in Enum.GetValues(typeof(AvatarType)))
                    {
                        if (avatarType == AvatarType.Undef) continue;
                        var (ttAvatarName, stat) = GetDirect<string>(avatarType, "FormalName");
                        if (stat && ttAvatarName == tmpAvatarName)
                        {
                            _currentAvatar = avatarType;
                            return;
                        }
                    }
                }
                LowLevelDebugPrint("アバターの判定に失敗しました");
            }
        }

        public (T value, bool success) Get<T>(string key) => GetDirect<T>(_currentAvatar, key);
        public (T value, bool success) GetDirect<T>(AvatarType avatarType, string key)
        {
            if (data.TryGetValue(key, out var row) && row.TryGetValue(GetAvatarName(avatarType), out var value))
            {
                try
                {
                    T result = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                    return (result, true);
                }
                catch (Exception ex)
                {
                    LowLevelDebugPrint($@"DBの取得に失敗しました (型変換エラー): {avatarType},{key} - {ex.Message}");
                    return (default(T), false);
                }
            }

            LowLevelDebugPrint($@"DBの取得に失敗しました (データ見つからず): {avatarType},{key}");
            return (default(T), false);
        }
    }
}