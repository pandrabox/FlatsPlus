#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using static pan.assets.fpposeclipperinstaller.runtime.Util;
using static pan.assets.fpposeclipperinstaller.runtime.Global;
using System.Linq;

namespace pan.assets.fpposeclipperinstaller.runtime
{
    public class FPPoseClipperInstaller : MonoBehaviour, IEditorOnly
    {
    }


    [CustomEditor(typeof(FPPoseClipperInstaller))]
    public class FPPoseClipperInstallerEditor : PandraEditor
    {
        public FPPoseClipperInstallerEditor() : base(true, PROJECTNAME, ProjectTypes.Asset)
        {
        }


        protected override void DefineSerial()
        {
        }

        public override void OnInnerInspectorGUI()
        {
            Title("説明");
            EditorGUILayout.HelpBox("FPPoseClipperInstallerは「CHILD WITCH」様の「PoseClipper」のインストールをサポートします。\n「PoseClipper」と、このプレハブをアバター直下に入れればインストールが完了します \n 動画で説明されている手動設定は不要です（やった場合こちらは使わないで下さい）", MessageType.Info);



            Title("チェック");
            var desc = GetAvatarDescriptor(((FPPoseClipperInstaller)target).gameObject);
            var map = new PoseClipMap(desc);

            if (map.AnyError)
            {
                EditorGUILayout.HelpBox($"エラーがあります。解決しないとアバターがアップロードできません。次の処置をご検討下さい。\n・エラー内容を手動で修正する\n・エラーを報告する\n・本Installerを削除する", MessageType.Error);
                EditorGUILayout.HelpBox($"{map.ErrMsg}", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("FPPoseClipperInstallerは適切に準備できています！", MessageType.Info);
            }
        }
    }
}
#endif