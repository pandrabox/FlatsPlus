using com.github.pandrabox.pandravase.editor;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.flatsplus.editor
{
    public class EmoAnalyzeMenuDefinition
    {
        [MenuItem("PanDbg/EmoAnalyzer")]
        private static void EmoAnalyzeM()
        {
            SetDebugMode(true);
            new EmoAnalyze(TopAvatar).Run();
        }
        [MenuItem("PanDbg/EmoAnalyzer_ForSetting")]
        private static void EmoAnalyzeM_ForSetting()
        {
            SetDebugMode(true);
            new EmoAnalyze(TopAvatar).Setting();
        }
    }

    public class EmoAnalyze
    {
        private const string RESDIR = "Packages/com.github.pandrabox.flatsplus/Editor/EmoAnalyze/result";
        private FlatsProject _prj;

        public EmoAnalyze(VRCAvatarDescriptor desc)
        {
            _prj = new FlatsProject(desc);
        }
        public void Run()
        {
            var tgt = _prj.RootObject;
            var head = _prj.HumanoidGameObject(HumanBodyBones.Head);
            var offset = new Vector3(_prj.FaceCapX, _prj.FaceCapY, _prj.FaceCapZ);
            var size = _prj.FaceCapSize;
            using (var c = new PanCapture())
            {
                //var c = new PanCapture();
                GameObject cTgt = c.CreateClone(tgt);
                var skinnedMeshRenderer = cTgt.transform.Find("Body").GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                    {
                        string blendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
                        skinnedMeshRenderer.SetBlendShapeWeight(i, 100);
                        Texture2D t = c.ManualRun(cTgt, size, head, offset);
                        var p = OutpAsset(t, $@"{RESDIR}/{_prj.CurrentAvatarName}{i}_{blendShapeName}.png");
                        skinnedMeshRenderer.SetBlendShapeWeight(i, 0);
                    }
                }
            }
        }
        public void Setting()
        {
            var tgt = _prj.RootObject;
            var head = _prj.HumanoidGameObject(HumanBodyBones.Head);
            var offset = new Vector3(_prj.FaceCapX, _prj.FaceCapY, _prj.FaceCapZ);
            var size = _prj.FaceCapSize;
            var c = new PanCapture();
            GameObject cTgt = c.CreateClone(tgt);
            Texture2D t = c.ManualRun(cTgt, size, head, offset);
            var p = OutpAsset(t, $@"{RESDIR}/{_prj.CurrentAvatarName}.png");
            GameObject.DestroyImmediate(cTgt);
            Selection.activeGameObject = c.Camera.gameObject;
            c.Camera.cullingMask = 1;
        }
    }
}