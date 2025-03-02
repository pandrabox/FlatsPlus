#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.IO;
using VRC.SDK3.Avatars.Components;
using System.Linq;
using UnityEditor;
using nadena.dev.ndmf;

namespace pan.assets.fpposeclipperinstaller.runtime
{
    public static class Util
    {
        /////////////////////////Global/////////////////////////
        public static bool PDEBUGMODE = false;
        public const float FPS = 60;
        public const string ONEPARAM = "__ModularAvatarInternal/One";
        public static string RootDir_VPM = "Packages/";
        public static string RootDir_Asset = "Assets/Pan/";
        public static string VPMDomainNameSuffix = "com.github.pandrabox.";
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        public const string DIRSEPARATOR = "\\";
#else
        public const string DIRSEPARATOR = "/";
#endif

        /////////////////////////DEBUG/////////////////////////
        /// <summary>
        /// DebugMode��ݒ肷��
        /// �� using static com.github.pandrabox.pandravase.runtime.Global ���K�v�ł�
        /// </summary>
        /// <param name="mode">�ݒ肷��Mode</param>
        public static void SetDebugMode(bool mode)
        {
            PDEBUGMODE = mode;
        }

        public static void LowLevelDebugPrint(string message, bool debugOnly = true, LogType level = LogType.Warning, string projectName = "Vase", [CallerMemberName] string callerMethodName = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            if (debugOnly && !PDEBUGMODE) return;
            var msg = $@"[PandraBox.{projectName}.{callerMethodName}:{callerLineNumber}]:{message}";

            if (level == LogType.Log) Debug.Log(msg);
            else if (level == LogType.Error) Debug.LogError(msg);
            else if (level == LogType.Exception) throw new Exception(msg);
            else Debug.LogWarning(msg);
            
        }

        /// <summary>
        /// �V�[���̍ŏ��ɑ��݂���A�o�^�[��Descriptor
        /// </summary>
        public static VRCAvatarDescriptor TopAvatar => GameObject.FindObjectOfType<VRCAvatarDescriptor>();


        /////////////////////////����� Component�T��/////////////////////////
        /// <summary>
        /// �������Component��T�����A�ŏ��Ɍ����������̂�Ԃ��܂��B
        /// ��������Unity�W���@�\���g���Ă��������@��F
        ///     currentComponent.GetComponentsInChildren<T>(true);
        ///     ParentTransform.GetComponentsInChildren<Transform>(true)?.Where(t => t.name == TargetName)?.ToArray();
        ///     GameObject.FindObjectsOfType<Transform>()?.Where(t => t.name == TargetName)?.ToArray();
        /// </summary>
        /// <typeparam name="T">�T��Component</typeparam>
        /// <param name="current">�T����R���|�[�l���g</param>
        /// <returns>��������Component�Ȃ���null</returns>
        public static T FindComponentInParent<T>(GameObject current) where T : Component => FindComponentInParent<T>(current?.transform);
        public static T FindComponentInParent<T>(Transform current) where T : Component
        {
            Transform parent = current?.transform?.parent;
            while (parent != null)
            {
                T component = parent.GetComponent<T>();
                if (component != null) return component;
                parent = parent.parent;
            }
            LowLevelDebugPrint($@"Component�̒T���Ɏ��s���܂���");
            return null;
        }
        public static GameObject GetAvatarRootGameObject(GameObject tgt) => GetAvatarDescriptor(tgt).gameObject;
        public static GameObject GetAvatarRootGameObject(Transform tgt) => GetAvatarDescriptor(tgt).gameObject;
        public static Transform GetAvatarRootTransform(GameObject tgt) => GetAvatarDescriptor(tgt).transform;
        public static Transform GetAvatarRootTransform(Transform tgt) => GetAvatarDescriptor(tgt).transform;
        public static VRCAvatarDescriptor GetAvatarDescriptor(GameObject current) => GetAvatarDescriptor(current?.transform);
        public static VRCAvatarDescriptor GetAvatarDescriptor(Transform current)
        {
            return FindComponentInParent<VRCAvatarDescriptor>(current);
        }
        public static bool IsInAvatar(GameObject current) => IsInAvatar(current?.transform);
        public static bool IsInAvatar(Transform current)
        {
            return GetAvatarDescriptor(current) != null;
        }


        /////////////////////////Path�̉���/////////////////////////
        /// <summary>
        /// �A�Z�b�g���쐬����
        /// </summary>
        /// <param name="asset">�쐬����A�Z�b�g</param>
        /// <param name="path">�p�X</param>
        /// <returns></returns>
        public static string OutpAsset(UnityEngine.Object asset, string path = "", bool debugOnly = false)
        {
            if (debugOnly && !PDEBUGMODE) return null;
            if (path == "") path = "Assets";
            var UnityDirPath = CreateDir(path);
            if (UnityDirPath == null)
            {
                LowLevelDebugPrint("�f�B���N�g��[{path}]�̐����Ɏ��s�������߃A�Z�b�g�̐����Ɏ��s���܂����B");
                return null;
            }

            var assetPath = AssetSavePath(asset, path);
            var absAssetPath = GetAbsolutePath(assetPath);

            AssetDatabase.CreateAsset(asset, assetPath);

            if (File.Exists(absAssetPath)) return assetPath;
            LowLevelDebugPrint($@"�A�Z�b�g{assetPath}�̐����Ɏ��s���܂���");
            return null;
        }

        /// <summary>
        /// �A�Z�b�g��ۑ�����K�؂ȃp�X��Ԃ��i�p�X�^�C�v�͕ۏ؂��Ȃ��j
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string AssetSavePath(UnityEngine.Object asset, string path)
        {
            if (path.HasExtension()) return path;
            string fileName = SanitizeStr(asset.name);
            var extensionMap = new Dictionary<Type, string>() {
                { typeof(AnimationClip), ".anim" },
                { typeof(Texture2D), ".png" },
                { typeof(Material), ".mat" },
            };
            string extension = extensionMap.TryGetValue(asset.GetType(), out string e) ? e : ".asset";
            return Path.Combine(path, fileName + extension);
        }

        /// <summary>
        /// �f�B���N�g�����쐬����
        /// </summary>
        /// <param name="path">�f�B���N�g���p�X(��΂܂���Assets/,Packages/����n�܂鑊��)</param>
        /// <returns>���������Unity�f�B���N�g���p�X�A���s������null</returns>
        public static string CreateDir(string path)
        {
            var absPath = Path.GetDirectoryName(GetAbsolutePath(path));
            Directory.CreateDirectory(absPath);
            if (Directory.Exists(absPath)) return GetUnityPath(absPath);
            LowLevelDebugPrint($@"�f�B���N�g��[{absPath}]�̍쐬�Ɏ��s���܂����B");
            return null;
        }

        /// <summary>
        /// PathTypes�̔���
        /// </summary>
        public enum PathTypes { Error, UnityAsset, AbsoluteAsset, UnityDir, AbsoluteDir };
        public static PathTypes PathType(this string path)
        {
            if (path.IsUnityPath()) return path.HasExtension() ? PathTypes.UnityAsset : PathTypes.UnityDir;
            if (path.IsAbsolutePath()) return path.HasExtension() ? PathTypes.AbsoluteAsset : PathTypes.AbsoluteDir;
            LowLevelDebugPrint($@"�����ȃp�X[{path}]�𔻒肵�܂����B");
            return PathTypes.Error;
        }

        /// <summary>
        /// UnityPath���ǂ������肷��
        /// </summary>
        public static bool IsUnityPath(this string path)
        {
            var tmp = DirSeparatorNormalize(path);
            return tmp.StartsWith("Assets/") || tmp.StartsWith("Packages/") || tmp == "Assets" || tmp == "Packages";
        }

        /// <summary>
        /// AbsolutePath���ǂ������肷��
        /// </summary>
        public static bool IsAbsolutePath(this string path)
        {
            var tmp = DirSeparatorLocalize(path);
            return tmp.StartsWith(AbsoluteAssetsPath) || tmp.StartsWith(AbsolutePackagesPath);
        }

        /// <summary>
        /// path���g���q�������ǂ����̔���
        /// </summary>
        /// <param name="path"></param>
        /// <returns>���Ȃ�true</returns>
        public static bool HasExtension(this string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var tmp = DirSeparatorNormalize(path);
            int lastSeparatorIndex = tmp.LastIndexOf('/');
            if (lastSeparatorIndex == -1 || lastSeparatorIndex == tmp.Length - 1) return false;
            return tmp.IndexOf('.', lastSeparatorIndex + 1) >= 0;
        }

        /// <summary>
        /// DirSeparator��"/"�ɂ���
        /// </summary>
        public static string DirSeparatorNormalize(string path) => (path ?? "").Replace(DIRSEPARATOR, "/");

        /// <summary>
        /// "/"��DirSeparator�ɂ���
        /// </summary>
        public static string DirSeparatorLocalize(string path) => (path ?? "").Replace("/", DIRSEPARATOR);

        /// <summary>
        /// �p�X��Absolute(��Fc:/test/Assets/aaa)��Unity(��FAssets/aaa)�̕ϊ�
        /// </summary>
        public static string AbsoluteAssetsPath => DirSeparatorLocalize(Application.dataPath);
        public static string AbsolutePackagesPath => DirSeparatorLocalize(Path.Combine(new DirectoryInfo(AbsoluteAssetsPath).Parent.FullName, "Packages"));
        public static string GetAbsolutePath(string path)
        {
            var tmp = DirSeparatorLocalize(path);
            if (IsAbsolutePath(tmp)) return tmp;
            if (ReplaceSubstring(ref tmp, "Assets", AbsoluteAssetsPath)) return tmp;
            if (ReplaceSubstring(ref tmp, "Packages", AbsolutePackagesPath)) return tmp;
            LowLevelDebugPrint($@"�����ȃp�X[{path}]�̕ϊ������݂܂���");
            return null;
        }
        public static string GetUnityPath(string path)
        {
            var tmp = DirSeparatorLocalize(path);
            if (IsUnityPath(tmp)) return DirSeparatorNormalize(tmp);
            if (ReplaceSubstring(ref tmp, AbsoluteAssetsPath, "Assets")) return DirSeparatorNormalize(tmp);
            if (ReplaceSubstring(ref tmp, AbsolutePackagesPath, "Packages")) return DirSeparatorNormalize(tmp);
            LowLevelDebugPrint($@"�����ȃp�X[{path}]�̕ϊ������݂܂���");
            return null;
        }

        /// <summary>
        /// A(�Q�Ɠn��)��B����n�܂��Ă����C�ɏ���������
        /// </summary>
        /// <param name="strA">������</param>
        /// <param name="strB">������</param>
        /// <param name="strC">������</param>
        /// <returns>�������������s���ꂽ���ǂ���</returns>
        public static bool ReplaceSubstring(ref string strA, string strB, string strC)
        {
            if (strA.StartsWith(strB))
            {
                strA = strC + strA.Substring(strB.Length);
                return true;
            }
            return false;
        }

        /// <summary>
        /// �t�H���_���폜
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFolder(string path)
        {
            if (!path.Contains("Packages") && path.Contains("Assets"))
            {
                var tgt = GetUnityPath(path);
                FileUtil.DeleteFileOrDirectory(tgt);
            }
            else
            {
                var tgt = GetAbsolutePath(path);
                if (Directory.Exists(tgt)) Directory.Delete(tgt, true);
            }
        }


        /////////////////////////CreateObject/////////////////////////
        /// <summary>
        /// �I�u�W�F�N�g�̐���
        /// </summary>
        /// <param name="parent">�e</param>
        /// <param name="name">�����I�u�W�F�N�g��</param>
        /// <param name="initialAction">����������</param>
        /// <returns>���������I�u�W�F�N�g</returns>
        private enum CreateType
        {
            Normal,
            ReCreate,
            GetOrCreate, //����Ύ擾�Ȃ���΍쐬�BComponent�ɂ��Ă����l�B
            AddOrCreate //����Ύ擾�Ȃ���΍쐬�BComponent�͊����������Ă��ǉ�����
        }
        public static GameObject GetOrCreateObject(GameObject parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.GetOrCreate, initialAction);
        public static GameObject GetOrCreateObject(Transform parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.GetOrCreate, initialAction);
        public static GameObject ReCreateObject(GameObject parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.ReCreate, initialAction);
        public static GameObject ReCreateObject(Transform parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.ReCreate, initialAction);
        public static GameObject CreateObject(GameObject parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.Normal, initialAction);
        public static GameObject CreateObject(Transform parent, string name, Action<GameObject> initialAction = null) => CreateObjectBase(parent, name, CreateType.Normal, initialAction);
        private static GameObject CreateObjectBase(GameObject parent, string name, CreateType createType, Action<GameObject> initialAction = null) => CreateObjectBase(parent?.transform, name, createType, initialAction);
        private static GameObject CreateObjectBase(Transform parent, string name, CreateType createType, Action<GameObject> initialAction = null)
        {
            if (createType == CreateType.ReCreate) RemoveChildObject(parent.transform, name);
            if (createType == CreateType.GetOrCreate || createType == CreateType.AddOrCreate)
            {
                GameObject tmp = parent.transform?.Find(name)?.gameObject;
                if (tmp != null) return tmp;
            }

            GameObject res = new GameObject(name);
            res.transform.SetParent(parent.transform);
            initialAction?.Invoke(res);
            return res;
        }

        /////////////////////////CreateComponentObject/////////////////////////
        /// <summary>
        /// �R���|�[�l���g�t���̃I�u�W�F�N�g�̐���
        /// </summary>
        /// <typeparam name="T">�A�^�b�`����R���|�[�l���g</typeparam>
        /// <param name="parent">�e</param>
        /// <param name="initialAction">����������</param>
        /// <returns>�A�^�b�`�����R���|�[�l���g</returns>
        public static T AddOrCreateComponentObject<T>(GameObject parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.AddOrCreate, initialAction);
        public static T AddOrCreateComponentObject<T>(Transform parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.AddOrCreate, initialAction);
        public static T GetOrCreateComponentObject<T>(GameObject parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.GetOrCreate, initialAction);
        public static T GetOrCreateComponentObject<T>(Transform parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.GetOrCreate, initialAction);
        public static T ReCreateComponentObject<T>(GameObject parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.ReCreate, initialAction);
        public static T ReCreateComponentObject<T>(Transform parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.ReCreate, initialAction);
        public static T CreateComponentObject<T>(GameObject parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.Normal, initialAction);
        public static T CreateComponentObject<T>(Transform parent, string name, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent, name, CreateType.Normal, initialAction);
        private static T CreateComponentObjectBase<T>(GameObject parent, string name, CreateType createType, Action<T> initialAction = null) where T : Component => CreateComponentObjectBase<T>(parent?.transform, name, createType, initialAction);
        private static T CreateComponentObjectBase<T>(Transform parent, string name, CreateType createType, Action<T> initialAction = null) where T : Component
        {
            GameObject obj = CreateObjectBase(parent, name, createType);
            if (createType == CreateType.GetOrCreate)
            {
                T cmp = obj.GetComponent<T>();
                if (cmp != null) return cmp;
            }
            T component = obj.AddComponent<T>();
            initialAction?.Invoke(component);
            return component;
        }


        /// <summary>
        /// �I�u�W�F�N�g�̍폜
        /// </summary>
        /// <param name="target"></param>
        static public void RemoveObject(Transform target) => RemoveObject(target?.gameObject);
        static public void RemoveObject(GameObject target)
        {
            if (target != null)
            {
                GameObject.DestroyImmediate(target);
            }
        }
        static public void RemoveChildObject(Transform parent, string name) => RemoveObject(GetChildObject(parent, name));
        static public void RemoveChildObject(GameObject parent, string name) => RemoveChildObject(parent?.transform, name);
        static public GameObject GetChildObject(Transform parent, string name) => parent?.Find(name)?.gameObject;
        static public GameObject GetChildObject(GameObject parent, string name) => GetChildObject(parent?.transform, name);

        /// <summary>
        /// ������𖳊Q��
        /// </summary>
        /// <param name="original">���̕�����</param>
        /// <returns>���Q�����ꂽ������</returns>
        public static string SanitizeStr(string original)
        {
            if (string.IsNullOrEmpty(original)) return "Untitled";
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                original = original.Replace(c.ToString(), string.Empty);
            }
            original = original.Trim();
            return string.IsNullOrEmpty(original) ? "Untitled" : original;
        }

        /// <summary>
        /// �Ώۂ��G�f�B�^�I�����[�E��\���ɐݒ�
        /// </summary>
        /// <param name="Target">�Ώ�</param>
        /// <param name="SW">�ݒ�l</param>
        public static void SetEditorOnly(Transform Target, bool SW) => SetEditorOnly(Target?.gameObject, SW);
        public static void SetEditorOnly(GameObject Target, bool SW)
        {
            if (SW)
            {
                Target.tag = "EditorOnly";
                Target.SetActive(false);
            }
            else
            {
                Target.tag = "Untagged";
                Target.SetActive(true);
            }
        }

        /// <summary>
        /// �Ώۂ̃G�f�B�^�I�����[��Ԃ��`�F�b�N
        /// </summary>
        /// <param name="target">�Ώ�</param>
        /// <returns>���(true�Ȃ�΃G�f�B�^�I�����[)</returns>
        public static bool IsEditorOnly(Transform target) => IsEditorOnly(target?.gameObject);
        public static bool IsEditorOnly(GameObject target)
        {
            return target.tag == "EditorOnly" && target.activeSelf == false;
        }

        /// <summary>
        /// ���΃p�X�̎擾
        /// </summary>
        /// <param name="parent">�e</param>
        /// <param name="child">�q</param>
        /// <returns>���΃p�X</returns>
        public static string GetRelativePath(Transform parent, GameObject child) => GetRelativePath(parent, child?.transform);
        public static string GetRelativePath(GameObject parent, Transform child) => GetRelativePath(parent?.transform, child);
        public static string GetRelativePath(GameObject parent, GameObject child) => GetRelativePath(parent?.transform, child?.transform);
        public static string GetRelativePath(Transform parent, Transform child)
        {
            if (parent == null || child == null) return null;
            if (!child.IsChildOf(parent)) return null;
            string path = "";
            Transform current = child;
            while (current != parent)
            {
                path = current.name + (path == "" ? "" : "/") + path;
                current = current.parent;
            }
            return path;
        }

        /// <summary>
        /// Renderer��lilToon���g���Ă��邩�ǂ�������
        /// </summary>
        /// <param name="renderer"></param>
        /// <returns>�g���Ă����true</returns>
        public static bool IsLil(Renderer renderer)
        {
            if (renderer == null || renderer.sharedMaterials == null) return false;
            foreach (var material in renderer.sharedMaterials)
            {
                if (material != null && material.shader != null && material.shader.name.Contains("lilToon")) return true;
            }
            return false;
        }

        /// <summary>
        /// 1D BlendTree�Ȃǂł킸���ɈႤ�l���g���Ƃ��̒l
        /// </summary>
        public static float DELTA = 0.00001f;

        /// <summary>
        /// �W�F�X�`����
        /// </summary>
        public static string[] GestureNames = new string[] { "Neutral", "Fist", "HandOpen", "FingerPoint", "Victory", "RocknRoll", "HandGun", "Thumbsup" };

        /// <summary>
        /// �W�F�X�`���ԍ�
        /// </summary>
        public enum Gesture
        {
            Neutral,
            Fist,
            HandOpen,
            FingerPoint,
            Victory,
            RocknRoll,
            HandGun,
            Thumbsup
        }
        public const int GESTURENUM = 8;

        /// <summary>
        /// �������傫���ŏ���2�̗ݏ�����߂�
        /// </summary>
        /// <param name="paramNum">��</param>
        /// <returns>�ŏ���2�̗ݏ�</returns>
        public static int NextPowerOfTwoExponent(int paramNum)
        {
            if (paramNum <= 0) return 0;
            paramNum--;
            int exponent = 0;
            while (paramNum > 0)
            {
                paramNum >>= 1;
                exponent++;
            }
            return exponent;
        }

        /// <summary>
        /// 1�ڂ̕����񂪔z��̒��ɂ�����̂��ǂ������ׂ�
        /// </summary>
        /// <param name="firstString">1��</param>
        /// <param name="otherStrings">�z��</param>
        /// <returns>�����true</returns>
        public static bool ContainsFirstString(string firstString, params string[] otherStrings)
        {
            return otherStrings.Any(s => s.Contains(firstString));
        }

        /// <summary>
        /// �t�H���_���쐬����
        /// </summary>
        /// <param name="path">�쐬����p�X</param>
        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }


        public static PandraProject VaseProject(BuildContext ctx) => VaseProject(ctx.AvatarDescriptor);
        public static PandraProject VaseProject(GameObject child) => VaseProject(GetAvatarDescriptor(child));
        public static PandraProject VaseProject(Transform child) => VaseProject(GetAvatarDescriptor(child));
        public static PandraProject VaseProject(VRCAvatarDescriptor desc)
        {
            return new PandraProject(desc, "PandraVase", ProjectTypes.VPM);
        }
    }
}
#endif