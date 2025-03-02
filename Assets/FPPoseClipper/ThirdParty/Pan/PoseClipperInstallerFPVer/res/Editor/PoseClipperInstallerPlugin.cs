using nadena.dev.ndmf;
using pan.assets.fpposeclipperinstaller.editor;
using pan.assets.fpposeclipperinstaller.runtime;
using static pan.assets.fpposeclipperinstaller.runtime.Util;
using static pan.assets.fpposeclipperinstaller.runtime.Global;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using UnityEditor;
using System.Linq;
using UnityEngine.Animations;
using System.Collections.Generic;
using System;

[assembly: ExportsPlugin(typeof(FPPoseClipperInstallerPlugin))]

namespace pan.assets.fpposeclipperinstaller.editor
{
#if PANDRADBG
    public class PanDebug
    {
        [MenuItem("PanDbg/FPPoseClipperInstallerPlugin")]
        public static void DbgFPPoseClipperInstallerPlugin() {
            SetDebugMode(true);
            new FPPoseClipperInstallerMain(TopAvatar);
        }
    }
#endif
    public class FPPoseClipperInstallerPlugin : Plugin<FPPoseClipperInstallerPlugin>
    {
        public override string QualifiedName => "pan.assets.fpposeclipperinstaller";
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run(PROJECTNAME, ctx =>
                {
                    FPPoseClipperInstaller tgt = ctx.AvatarRootTransform.GetComponentInChildren<FPPoseClipperInstaller>(false);
                    if (tgt == null) return;
                    new FPPoseClipperInstallerMain(ctx.AvatarDescriptor);
                });
        }
    }
    public class FPPoseClipperInstallerMain
    {
        string _currentTargetForErrorReport;
        PoseClipMap _map;

        public FPPoseClipperInstallerMain(VRCAvatarDescriptor desc)
        {
            _map = new PoseClipMap(desc);
            if (_map == null)
            {
                ExceptionCall("mapの取得に失敗しました。予期しないエラーです。");
                return;
            }
            if (_map.AnyError)
            {
                ExceptionCall(_map.ErrMsg);
                return;
            }

            // ParentConstraint設定
            ParentConstraintSetting();

            // ScaleConstraint設定
            SetupScaleConstraint();
        }


        private void ParentConstraintSetting()
        {
            foreach (PoseClipUnitMap map in _map.PoseClipUnitMaps)
            {
                Transform poseClipperTransform = map.PoseClipperTransform;
                if (poseClipperTransform == null) ExceptionCall($@"poseClipperTransform. {_currentTargetForErrorReport}");
                Transform targetTransform = map.TargetTransform;

                if (poseClipperTransform == null) ExceptionCall($@"poseClipperTransform is null. {_currentTargetForErrorReport}");
                if (targetTransform == null) ExceptionCall($@"targetTransform is null. {_currentTargetForErrorReport}");

                var targetGameObject = targetTransform.gameObject;
                if (targetGameObject == null) ExceptionCall($@"targetGameObject is null. {_currentTargetForErrorReport}");
                var parentConstraint = targetGameObject.AddComponent<ParentConstraint>();
                if (targetTransform == null) ExceptionCall($@"parentConstraint is null. {_currentTargetForErrorReport}");

                parentConstraint.enabled = false;
                ConstraintSource constraintSource = new ConstraintSource();
                constraintSource.sourceTransform = poseClipperTransform;
                constraintSource.weight = 1;
                parentConstraint.AddSource(constraintSource);

                parentConstraint.constraintActive = true;
                parentConstraint.SetTranslationOffset(0, Vector3.zero);
                parentConstraint.SetRotationOffset(0, Vector3.zero);
            }
        }

        private void SetupScaleConstraint()
        {
            var headBone = _map.AvatarHeadTransform;
            if (headBone == null) ExceptionCall("headBone is null");

            var scaleConstraint = headBone.gameObject.AddComponent<ScaleConstraint>();
            if (scaleConstraint == null) ExceptionCall("scaleConstraint is null");

            ConstraintSource constraintSource = new ConstraintSource();
            constraintSource.sourceTransform = _map.PoseClipperScaleTransform;
            constraintSource.weight = 1;
            scaleConstraint.AddSource(constraintSource);

            scaleConstraint.constraintActive = true;

            var measure = new GameObject("measure").transform;
            measure.SetParent(headBone.transform);
            measure.localScale = new Vector3(1, 1, 1);
            measure.SetParent(_map.PoseClipperScaleTransform);
            scaleConstraint.locked = false;
            scaleConstraint.scaleOffset = measure.localScale;
            scaleConstraint.enabled = false;
            GameObject.DestroyImmediate(measure.gameObject);
        }

        private void ExceptionCall(string message)
        {
            _map.Prj.DebugPrint(message, false, LogType.Exception);
        }

    }
}