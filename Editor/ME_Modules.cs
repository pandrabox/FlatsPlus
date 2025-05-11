#if UNITY_EDITOR

using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;
using FP = com.github.pandrabox.flatsplus.runtime.FlatsPlus;

namespace com.github.pandrabox.flatsplus.editor
{

    public class FPFuncCarry : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Carry);
        public override bool PCOnly => true;
        protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncMultiTool), typeof(FPFuncLink) };
        public override void DrawDetail()
        {
            DrawBoolField(nameof(FP.D_Carry_AllowBlueGateDefault));
            DrawFloatField(nameof(FP.D_Carry_GateMaxRange), 0.1f, 5);
        }
    }
    public class FPFuncDanceController : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_DanceController);
        protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncEmo), typeof(FPFuncWriteDefaultOn) };
        public override void DrawDetail()
        {
            DrawEnumField(nameof(FP.D_Dance_ControlType));
            SerializedProperty danceProp = SP(nameof(FP.D_Dance_ControlType));
            Type enumType = typeof(PVDanceController.DaceControlType);
            string enumValueName = Enum.GetName(enumType, danceProp.enumValueIndex);
            string helpmsg = L($@"Dance/Help/{enumValueName}");
            EditorGUILayout.HelpBox(helpmsg, MessageType.Info);

            DrawBoolField(nameof(FP.D_Dance_FxEnable));
        }
    }
    public class FPFuncEmo : ME_FuncBase
    {
        private Dictionary<string, string> _presets;
        public override string ManagementFunc => nameof(FP.Func_Emo);
        public override void DrawDetail()
        {
            LoadPreset();
            DrawDictionarySelect(nameof(FP.D_Emo_Preset), _presets);

            string helpmsg;
            switch (SP(nameof(FP.D_Emo_Preset)).stringValue)
            {
                case "Auto":
                    helpmsg = L("Emo/Help/Auto");
                    break;
                case "Custom":
                    helpmsg = L("Emo/Help/Custom");
                    break;
                default:
                    helpmsg = L("Emo/Help/Avatar");
                    break;
            }
            EditorGUILayout.HelpBox(helpmsg, MessageType.Info);


            if (SP(nameof(FP.D_Emo_Preset)).stringValue == "Custom")
            {
                DrawFileField(nameof(FP.D_Emo_ConfigFilePath), "csv");
            }
            DrawButton("OpenEmoMaker", OpenEmoMaker);
            DrawFloatField(nameof(FP.Emo_TransitionTime), 0, 3);
        }
        private void OpenEmoMaker()
        {
            var targetPath = SP(nameof(FP.D_Emo_Preset)).stringValue;
            if (targetPath == "Custom")
            {
                targetPath = SP(nameof(FP.D_Emo_ConfigFilePath)).stringValue;
            }
            string configSavePath = "Assets/Pan/FlatsPlus/Save/Emo";
            Action<string> onSaveAction = (path) =>
            {
                SP(nameof(FP.D_Emo_Preset)).stringValue = "Custom";
                SP(nameof(FP.D_Emo_ConfigFilePath)).stringValue = path;
            };
            GameObject targetObj = ME_FuncManager.I.EditorObj;
            FPEmoMaker.OpenWindow(targetObj, targetPath, configSavePath, onSaveAction);
        }
        private void LoadPreset()
        {
            if (_presets != null) return;
            _presets = new Dictionary<string, string>();
            _presets.Add("Auto", "Auto");
            var path = "Packages/com.github.pandrabox.flatsplus/Assets/Emo/res";
            var files = System.IO.Directory.GetFiles(path, "*.csv");
            foreach (var file in files)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                _presets.Add(name, file);
            }
            _presets.Add("Custom", "Custom");
        }
    }
    public class FPFuncExplore : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Explore);
        public override bool PCOnly => true;
        //protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncMultiTool) };
        public override void DrawDetail()
        {
            DrawBoolField(nameof(FP.D_Explore_Pin));
            DrawBoolField(nameof(FP.D_Explore_Line));
            DrawBoolField(nameof(FP.D_Explore_Light));
            DrawBoolField(nameof(FP.D_Explore_Light_DefaultGlobal));
        }
    }
    public class FPFuncHoppe : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Hoppe);
        protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncEmo), typeof(FPFuncMultiTool) };

        public override void DrawDetail()
        {
            DrawBoolField(nameof(FP.D_Hoppe_AllowTouch));
            DrawBoolField(nameof(FP.D_Hoppe_AllowStretch));
            DrawFloatField(nameof(FP.D_Hoppe_StretchLimit), 0, 2);
            DrawBoolField(nameof(FP.D_Hoppe_Blush));
            DrawFloatField(nameof(FP.D_Hoppe_Blush_Sensitivity), 0, 3);
            DrawBoolField(nameof(FP.D_Hoppe_UseOriginalBlush));
            DrawEnumField(nameof(FP.D_Hoppe_BlushControlType));
            DrawBoolField(nameof(FP.D_Hoppe_ShowExpressionMenu));
        }
    }
    public class FPFuncIco : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Ico);
        public override bool PCOnly => true;
        protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncMultiTool), typeof(FPFuncLink) };

        public override void OnEnable()
        {
            // コンポーネント初期化時に配列の初期化を行う
            EnsureAllTexturesInitialized();
        }


        public override void DrawDetail()
        {
            DrawBoolField(nameof(FP.Ico_VerView));
            EnsureAllTexturesInitialized();
            EditorGUILayout.BeginHorizontal();
            //6個だけ編集を許可
            for (int i = 0; i < 6; i++)
            {
                DrawTextureField($"{nameof(FP.Ico_Textures)}.Array.data[{i}]", 50, 50);
            }
            EditorGUILayout.EndHorizontal();
        }
        // 表示用と非表示用を含む全テクスチャの初期化
        private void EnsureAllTexturesInitialized()
        {
            SerializedProperty texturesArray = SP(nameof(FP.Ico_Textures));

            // 配列のサイズが不足している場合、拡張する
            if (texturesArray.arraySize < 8)
            {
                texturesArray.arraySize = 8;
                ME_FuncManager.I.ApplyModifiedProperties();
            }

            // 全テクスチャをチェックして初期化
            for (int i = 0; i < 8; i++)
            {
                SerializedProperty texProp = texturesArray.GetArrayElementAtIndex(i);

                // テクスチャが未設定の場合は初期値を読み込む
                if (texProp.objectReferenceValue == null)
                {
                    // 対応するテクスチャを読み込む
                    string path = $"Packages/com.github.pandrabox.flatsplus/Assets/Ico/Ico/i{i + 1}.png";
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                    if (tex != null)
                    {
                        texProp.objectReferenceValue = tex;
                        ME_FuncManager.I.ApplyModifiedProperties();
                    }
                }
            }
        }
    }
    //public class FPFuncLight : ME_FuncBase
    //{
    //    public override string ManagementFunc => nameof(FP.Func_Light);

    //    public override void DrawDetail()
    //    {
    //        DrawBoolField(nameof(FP.Light_IntensityPerfectSync));
    //    }
    //}
    public class FPFuncMakeEmo : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_MakeEmo);
        public override bool PCOnly => true;
        protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncEmo) };

        public override void DrawDetail()
        {
            DrawFloatField(nameof(FP.MakeEmo_MenuSize), 0.1f, 1f);
            DrawFloatField(nameof(FP.MakeEmo_LockSize), 0.01f, 0.2f);
            DrawFloatField(nameof(FP.MakeEmo_MenuOpacity), 0, 1);
            DrawIntField(nameof(FP.MakeEmo_Margin), 0, 50);
            DrawFloatField(nameof(FP.MakeEmo_ScrollSpeed), 0.01f, 0.1f);
            DrawFloatField(nameof(FP.MakeEmo_DeadZone), 0, 1);
        }
    }
    public class FPFuncMeshSetting : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_MeshSetting);
    }
    public class FPFuncMove : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Move);
        public override bool PCOnly => true;
        protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncMultiTool) };
    }
    public class FPFuncOnaka : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Onaka);

        public override void DrawDetail()
        {
            DrawFloatField(nameof(FP.D_Onaka_Pull), 0, 1);
            DrawFloatField(nameof(FP.D_Onaka_Spring), 0, 1);
            DrawFloatField(nameof(FP.D_Onaka_Gravity), 0, 1);
            DrawFloatField(nameof(FP.D_Onaka_GravityFallOff), 0, 2);
            DrawFloatField(nameof(FP.D_Onaka_Immobile), 0, 1);
            DrawFloatField(nameof(FP.D_Onaka_LimitAngle), 0, 90);
            DrawFloatField(nameof(FP.D_Onaka_RadiusTuning), 0.5f, 2);
        }
    }
    public class FPFuncPen : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Pen);
        public override bool PCOnly => true;
        protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncMultiTool) };
    }
    public class FPFuncSleep : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Sleep);
        public override bool PCOnly => true;
        protected override List<Type> Dependencies => new List<Type> { typeof(FPFuncPoseClipper) };
    }
    public class FPFuncTail : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Tail);

        public override void DrawDetail()
        {
            DrawFloatField(nameof(FP.Tail_SwingPeriod), 0.1f, 5f);
            DrawFloatField(nameof(FP.Tail_SwingAngle), 0, 180);
            DrawFloatField(nameof(FP.Tail_SizeMax), 0.1f, 5f);
            DrawFloatField(nameof(FP.Tail_SizeMin), 0.01f, 1f);
            DrawBoolField(nameof(FP.Tail_SizePerfectSync));
            DrawFloatField(nameof(FP.Tail_DefaultSize), 0, 5);
        }
    }
    public class FPFuncLink : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Link);
        public override bool PCOnly => true;
    }
    //public class FPFuncSync : ME_FuncBase
    //{
    //    public override string ManagementFunc => nameof(FP.Func_Sync);
    //}
    public class FPFuncWriteDefaultOn : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_WriteDefaultOn);
    }
    public class FPFuncPoseClipper : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_PoseClipper);
        public override bool PCOnly => true;
    }

    public class FPFuncGuide : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_Guide);
        public override bool PCOnly => true;
        public override void DrawDetail()
        {
            DrawBoolField(nameof(FP.D_Guide_DefaultActive));
            DrawFloatField(nameof(FP.D_Guide_DefaultSize), 0.1f, 1f);
        }
    }

    public class FPFuncMultiTool : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_MultiTool);
    }

    public class FPFuncClippingCanceler : ME_FuncBase
    {
        public override string ManagementFunc => nameof(FP.Func_ClippingCanceler);
        public override bool PCOnly => true;
        public override bool ExcludeFromBulkToggle => true;
        public override void OnChange(bool state)
        {
            new SetClippingCanceler(state);
        }
    }
}

#endif