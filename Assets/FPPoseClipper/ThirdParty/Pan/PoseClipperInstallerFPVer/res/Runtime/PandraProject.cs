﻿#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static pan.assets.fpposeclipperinstaller.runtime.Util;

namespace pan.assets.fpposeclipperinstaller.runtime
{
    public enum ProjectTypes { VPM, Asset };
    public class PandraProject
    {
        private VRCAvatarDescriptor _descriptor;
        public string ProjectName;
        public ProjectTypes ProjectType;
        public string ProjectFolder;
        public VRCAvatarDescriptor Descriptor { get { if (_descriptor == null) DebugPrint("未定義のDescriptorが呼び出されました", false, LogType.Error); return _descriptor; } }
        public GameObject RootObject => Descriptor.gameObject;
        public Transform RootTransform => Descriptor.transform;
        public string ResFolder => $@"{ProjectFolder}Res/";
        public string ImgFolder => $@"{DataFolder}Img/";
        public string AssetsFolder => $@"{ProjectFolder}Assets/";
        public string DataFolder => ProjectType == ProjectTypes.VPM ? AssetsFolder : ResFolder;
        public string AnimFolder => $@"{AssetsFolder}Anim/";
        public string EditorFolder => $@"{ProjectFolder}Editor/";
        public string RuntimeFolder => $@"{ProjectFolder}Runtime/";
        public string DebugFolder => $@"{ProjectFolder}Debug/";
        public string VaseFolder => "Packages/com.github.pandrabox.pandravase/";
        public string VaseDebugFolder => $@"{VaseFolder}Debug/";
        public string TmpFolder => $@"Assets/Temp/";
        public string Suffix => $@"Pan/{ProjectName}/";
        public string PrjRootObjName => $@"{ProjectName}_PrjRootObj";
        public VRCAvatarDescriptor.CustomAnimLayer[] BaseAnimationLayers => Descriptor.baseAnimationLayers;
        public int PlayableIndex(VRCAvatarDescriptor.AnimLayerType type) => Array.IndexOf(BaseAnimationLayers, BaseAnimationLayers.FirstOrDefault(l => l.type == type));
        public GameObject PrjRootObj => runtime.Util.GetOrCreateObject(RootTransform, PrjRootObjName);

        /// <summary>
        /// 1つのAvatarを編集するためのProjectを統括するクラス。Project共通で使うsuffix,ProjectNameなどの管理を提供する
        /// </summary>
        /// <param name="descriptor">Descriptor</param>
        /// <param name="suffix">変数名・レイヤ名等の前置詞</param>
        /// <param name="workFolder">Anim等を読み込む際使用する基本フォルダ</param>
        public PandraProject() => Init(null, "Generic", ProjectTypes.Asset);
        public PandraProject(string projectName, ProjectTypes projectTypes = ProjectTypes.Asset) => Init(null, projectName, projectTypes);
        public PandraProject(Transform somethingAvatarParts, string projectName, ProjectTypes projectType) => Init(GetAvatarDescriptor(somethingAvatarParts), projectName, projectType);
        public PandraProject(GameObject somethingAvatarParts, string projectName, ProjectTypes projectType) => Init(GetAvatarDescriptor(somethingAvatarParts), projectName, projectType);
        public PandraProject(VRCAvatarDescriptor descriptor, string projectName, ProjectTypes projectType) => Init(descriptor, projectName, projectType);
        private void Init(VRCAvatarDescriptor descriptor, string projectName, ProjectTypes projectType)
        {
            _descriptor = descriptor;
            ProjectName = projectName;
            ProjectType = projectType;
            if (ProjectType == ProjectTypes.Asset)
            {
                ProjectFolder = $@"{RootDir_Asset}{projectName}/"; //memo:PanはRootDirに含まれています
            }
            else
            {
                ProjectFolder = $@"{RootDir_VPM}{VPMDomainNameSuffix}{ProjectName.ToLower()}/";
            }
        }

        /// <summary>
        /// デバッグを開始し、PrjRootObjを削除する
        /// </summary>
        /// <param name="mode"></param>
        public void SetDebugMode(bool mode)
        {
            Util.SetDebugMode(mode);
            DebugPrint("DebugModeが開始されました。PrjRootObjの削除・Debugフォルダ・Tmpフォルダの削除を行います。");
            GameObject.DestroyImmediate(PrjRootObj);
            DeleteFolder(DebugFolder);
            DeleteFolder(TmpFolder);
            DeleteFolder(VaseDebugFolder);
        }

        public string GetParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName)) return null;//            { DebugPrint("nullが入力されました"); return null; };
            string res;
            if (ContainsFirstString(parameterName, new string[] { "ONEf", "PBTB_CONST_1", ONEPARAM })) res = ONEPARAM;
            else if (ContainsFirstString(parameterName, new string[] { "GestureRight", "GestureLeft", "GestureRightWeight", "GestureLeftWeight", "IsLocal", "InStation", "Seated", "VRMode" })) res = parameterName;
            else if (parameterName.StartsWith("Env/")) res = parameterName;
            else if (ContainsFirstString(parameterName, new string[] { "Time", "ExLoaded", "IsMMD", "IsNotMMD", "IsLocal", "FrameTime" })) res = $@"Env/{parameterName}";
            else if (parameterName.Length > 0 && parameterName[0] == '$') res = parameterName.Substring(1);
            else res = $@"{Suffix}{parameterName}";
            return res;
        }

        private string NormalizedMotionPath(string motionPath)
        {
            if (File.Exists(motionPath)) return motionPath;
            motionPath = motionPath.Trim().Replace("\\", "/");
            if (File.Exists(motionPath)) return motionPath;
            if (!motionPath.Contains("/")) motionPath = $@"{AnimFolder}{motionPath}";
            if (File.Exists(motionPath)) return motionPath;
            if (!motionPath.Contains(".")) motionPath = $@"{motionPath}.anim";
            if (File.Exists(motionPath)) return motionPath;
            DebugPrint($@"Motion「{motionPath}」が見つかりませんでした");
            return null;
        }

        public Motion LoadMotion(string motionPath)
        {
            return AssetDatabase.LoadAssetAtPath<Motion>(NormalizedMotionPath(motionPath));
        }



        /////////////////////////DEBUG/////////////////////////
        /// <summary>
        /// DebugMessageを表示する
        /// </summary>
        /// <param name="message">表示するMessage</param>
        /// <param name="debugOnly">DebugModeでのみ表示</param>
        /// <param name="level">ログレベル</param>
        /// <param name="callerMethodName">システムが使用</param>
        public void DebugPrint(string message, bool debugOnly = true, LogType level = LogType.Warning, [CallerMemberName] string callerMethodName = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LowLevelDebugPrint(message, debugOnly, level, ProjectName, callerMethodName);
        }

        /// <summary>
        /// アセットをデバッグ出力
        /// </summary>
        /// <param name="asset">アセット</param>
        /// <param name="path">パス</param>
        /// <returns>成功したらそのパス、失敗したらnull</returns>
        public string DebugOutp(UnityEngine.Object asset, string path = "")
        {
            if (!PDEBUGMODE) return null;
            if (path == "") path = DebugFolder;
            return OutpAsset(asset, path, true);
        }

        /////////////////////////CreateObject/////////////////////////
        /// <summary>
        /// PrjRootObjの直下にオブジェクトを生成
        /// </summary>
        /// <param name="name">生成オブジェクト名</param>
        /// <param name="initialAction">生成時処理</param>
        /// <returns>生成したオブジェクト</returns>
        public GameObject GetOrCreateObject(string name, Action<GameObject> initialAction = null) => runtime.Util.GetOrCreateObject(PrjRootObj, name, initialAction);
        public GameObject ReCreateObject(string name, Action<GameObject> initialAction = null) => runtime.Util.ReCreateObject(PrjRootObj, name, initialAction);
        public GameObject CreateObject(string name, Action<GameObject> initialAction = null) => runtime.Util.CreateObject(PrjRootObj, name, initialAction);

        /////////////////////////CreateComponentObject/////////////////////////
        /// <summary>
        /// PrjRootObjの直下にComponentオブジェクトを生成
        /// </summary>
        /// <param name="name">生成オブジェクト名</param>
        /// <param name="initialAction">生成時処理</param>
        /// <returns>生成したオブジェクト</returns>
        public T AddOrCreateComponentObject<T>(string name, Action<T> initialAction = null) where T : Component => runtime.Util.AddOrCreateComponentObject<T>(PrjRootObj, name, initialAction);
        public T GetOrCreateComponentObject<T>(string name, Action<T> initialAction = null) where T : Component => runtime.Util.GetOrCreateComponentObject<T>(PrjRootObj, name, initialAction);
        public T ReCreateComponentObject<T>(string name, Action<T> initialAction = null) where T : Component => runtime.Util.ReCreateComponentObject<T>(PrjRootObj, name, initialAction);
        public T CreateComponentObject<T>(string name, Action<T> initialAction = null) where T : Component => runtime.Util.CreateComponentObject<T>(PrjRootObj, name, initialAction);
    }
}
#endif