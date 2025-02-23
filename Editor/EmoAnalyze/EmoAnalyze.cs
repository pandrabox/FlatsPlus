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
using UnityEditorInternal;
using System.Security.Cryptography;

namespace com.github.pandrabox.flatsplus.editor
{
    public class EmoAnalyzeMenuDefinition
    {
        [MenuItem("PanDbg/**EmoAnalyzer")]
        private static void EmoAnalyzeM()
        {
            SetDebugMode(true);
            new EmoAnalyze(TopAvatar).Run();
        }
        [MenuItem("PanDbg/**EmoAnalyzer_ForSetting")]
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
                    //for (int i = 0; i < 2; i++)
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
            c.ManualRun(cTgt, size, head, offset);
            GameObject.DestroyImmediate(cTgt);
            Selection.activeGameObject = c.Camera.gameObject;
            c.Camera.cullingMask = 1;
        }
    }
}