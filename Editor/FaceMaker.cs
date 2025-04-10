﻿using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.flatsplus.editor
{
    public class Face
    {
        public string Name;
        public string PropertyName => "blendShape." + Name;
        public FaceType Type;
        public AnimationClip OnClip;
        public AnimationClip OffClip;
        public Texture2D Tex;
        public int BlendShapeCount;
    }
    public class Faces
    {
        public List<Face> All;
        public List<Face> Eyes => All.FindAll(face => face.Type == FaceType.Eye);
        public List<Face> Mouths => All.FindAll(face => face.Type == FaceType.Mouth);
        public List<Face> Others => All.FindAll(face => face.Type == FaceType.Other);
        public List<Face> Ignores => All.FindAll(face => face.Type == FaceType.Ignore);
        public int Count => All.Count;
        public Faces() => All = new List<Face>();
        public void Add(Face face) => All.Add(face);
        public Texture2D VoidIco;
        public List<Texture2D> EyeTextures => new List<Texture2D>() { VoidIco }.Concat(Eyes.Select(x => x.Tex)).ToList();
        public List<Texture2D> MouthTextures => new List<Texture2D>() { VoidIco }.Concat(Mouths.Select(x => x.Tex)).ToList();
    }

#if PANDRADBG
    public class FaceMakerDebug
    {
        [MenuItem("PanDbg/FaceMaker/all")]
        public static void FaceMaker_Debug()
        {
            SetDebugMode(true);
            var fp = new FlatsProject(TopAvatar);
            var FM = new FaceMaker(fp);
            foreach (var face in FM.Faces.All)
            {
                OutpAsset(face.Tex);
                OutpAsset(face.OnClip);
                OutpAsset(face.OffClip);
            }
        }
        [MenuItem("PanDbg/FaceMaker/unit")]
        public static void FaceMaker_DebugUnit()
        {
            SetDebugMode(true);
            var fp = new FlatsProject(TopAvatar);
            var FM = new FaceMaker(fp, unitMode: true);
            foreach (var face in FM.Faces.All)
            {
                if (face.Tex == null) continue;
                OutpAsset(face.Tex);
                OutpAsset(face.OnClip);
                OutpAsset(face.OffClip);
            }
        }
    }
#endif

    public class FaceMaker : FlatsWorkBase
    {
        public Faces Faces;
        public int TexSize;
        private bool _unitMode;
        public FaceMaker(FlatsProject prj, int texSize = 512, bool unitMode = false) : base(prj, texSize, unitMode) { }

        protected override void OnConstruct()
        {
            _unitMode = (bool)_args[1];
            TexSize = (int)_args[0];
            Faces = new Faces();
            GetNames();
            CreateClip();
            CreateTex();
        }

        private void GetNames()
        {
            Dictionary<string, FaceType> generalShapes = _prj.GeneralShapes;
            var bodyMesh = _prj.RootTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(mesh => mesh.name == "Body");
            if (bodyMesh == null)
            {
                Log.I.Error("BodyMeshが見つかりませんでした。");
            }
            for (int i = 0; i < bodyMesh.sharedMesh.blendShapeCount; i++)
            {
                string name = bodyMesh.sharedMesh.GetBlendShapeName(i);
                FaceType type;
                if (!generalShapes.ContainsKey(name)) type = FaceType.Other;
                else type = generalShapes[name];
                if (type == FaceType.Ignore) continue;
                Faces.Add(new Face { Name = name, Type = type, BlendShapeCount = i });
            }
            Log.I.Info($"FaceMaker Namesの解析完了: {Faces.Count}");
        }
        private void CreateClip()
        {
            var clips = new AnimationClipsBuilder(_prj);
            foreach (var face in Faces.Others)
            {
                clips.Clip($@"{face.Name}Off").Bind("Body", typeof(SkinnedMeshRenderer), face.PropertyName).Const2F(0);
                clips.Clip($@"{face.Name}On").Bind("Body", typeof(SkinnedMeshRenderer), face.PropertyName).Const2F(100);
                face.OffClip = clips.Outp($@"{face.Name}Off");
                face.OnClip = clips.Outp($@"{face.Name}On");
            }
            foreach (var faces in new[] { Faces.Eyes, Faces.Mouths })
            {
                foreach (var face in faces)
                {
                    foreach (var face2 in faces)
                    {
                        clips.Clip($@"{face.Name}Off").Bind("Body", typeof(SkinnedMeshRenderer), face2.PropertyName).Const2F(0);
                        clips.Clip($@"{face.Name}On").Bind("Body", typeof(SkinnedMeshRenderer), face2.PropertyName).Const2F(face2 == face ? 100 : 0);
                    }
                    face.OffClip = clips.Outp($@"{face.Name}Off");
                    face.OnClip = clips.Outp($@"{face.Name}On");
                }
            }
        }
        public void CreateTex()
        {
            var head = _prj.HumanoidGameObject(HumanBodyBones.Head);
            var offset = new Vector3(_prj.FaceCapX, _prj.FaceCapY, _prj.FaceCapZ);
            var size = _prj.FaceCapSize;
            using (var c = new PanCapture(width: TexSize))
            {
                GameObject cTgt = c.CreateClone(_prj.RootObject);
                var skinnedMeshRenderer = cTgt.transform.Find("Body").GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer == null)
                {
                    Log.I.Error("BodyMeshが見つかりませんでした。");
                    return;
                }
                Faces.VoidIco = c.ManualRun(cTgt, size, head, offset);
                foreach (var face in Faces.All)
                {
                    skinnedMeshRenderer.SetBlendShapeWeight(face.BlendShapeCount, 100);
                    var t = c.ManualRun(cTgt, size, head, offset);
                    t.name = face.Name;
                    t.wrapMode = TextureWrapMode.Clamp;
                    face.Tex = t;
                    OutpAsset(t);
                    skinnedMeshRenderer.SetBlendShapeWeight(face.BlendShapeCount, 0);
                    if (_unitMode) break;
                    //OutpAsset(t);
                    //Log.I.Info($"FaceMaker CreateTex: {face.BlendShapeCount}{face.Name}");
                }
            }
        }

        // CreateTex向けDBのCap座標・サイズ取得用の開発専用コマンド
        // 顔を撮影し、本来削除されるカメラ・Cloneを残します
        public void NonDisposeRun_ForDevelopOnly()
        {
            Log.I.Warning("開発専用コマンドです。");
            var tgt = _prj.RootObject;
            var head = _prj.HumanoidGameObject(HumanBodyBones.Head);
            var offset = new Vector3(_prj.FaceCapX, _prj.FaceCapY, _prj.FaceCapZ);
            var size = _prj.FaceCapSize;
            var c = new PanCapture();
            GameObject cTgt = c.CreateClone(tgt);
            Texture2D t = c.ManualRun(cTgt, size, head, offset);
            var p = OutpAsset(t);
            GameObject.DestroyImmediate(cTgt);
            Selection.activeGameObject = c.Camera.gameObject;
            c.Camera.cullingMask = 1;
        }
    }
}