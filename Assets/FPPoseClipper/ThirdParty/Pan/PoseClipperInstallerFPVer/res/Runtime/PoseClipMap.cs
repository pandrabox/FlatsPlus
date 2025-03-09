#if UNITY_EDITOR
using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;
using static pan.assets.fpposeclipperinstaller.runtime.Global;

namespace pan.assets.fpposeclipperinstaller.runtime
{
    public class PoseClipUnitMap
    {
        public string PoseClipperObjName;
        public HumanBodyBones TargetHumanBone;
        public Transform PoseClipperTransform;
        public Transform TargetTransform;
        public PoseClipUnitMap(string poseClipperObjName, HumanBodyBones targetHumanBone, Transform poseClipperTransform, Transform targetTransform)
        {
            PoseClipperObjName = poseClipperObjName;
            TargetHumanBone = targetHumanBone;
            PoseClipperTransform = poseClipperTransform;
            TargetTransform = targetTransform;
        }
    }
    public class PoseClipMap
    {
        public string ErrMsg = "";
        public bool AnyError = false;
        public PandraProject Prj;
        public Animator Animator;
        public Transform PoseClipperTransform;
        public Transform PoseClipperArmatureTransform;
        public Transform AvatarHeadTransform;
        public Transform PoseClipperScaleTransform;
        public Dictionary<string, HumanBodyBones> ClipperToHumanoid = new Dictionary<string, HumanBodyBones>()
            {
                { "Hips", HumanBodyBones.Hips },
                { "Hips/Spine", HumanBodyBones.Spine },
                { "Hips/Spine/Chest", HumanBodyBones.Chest },
                { "Hips/Spine/Chest/Head", HumanBodyBones.Head },
                { "Hips/Spine/Chest/UpperArm_L", HumanBodyBones.LeftUpperArm },
                { "Hips/Spine/Chest/UpperArm_L/LowerArm_L", HumanBodyBones.LeftLowerArm },
                { "Hips/Spine/Chest/UpperArm_L/LowerArm_L/Hand_L", HumanBodyBones.LeftHand },
                { "Hips/Spine/Chest/UpperArm_R", HumanBodyBones.RightUpperArm },
                { "Hips/Spine/Chest/UpperArm_R/LowerArm_R", HumanBodyBones.RightLowerArm },
                { "Hips/Spine/Chest/UpperArm_R/LowerArm_R/Hand_R", HumanBodyBones.RightHand },
                { "Hips/UpperLeg_L", HumanBodyBones.LeftUpperLeg },
                { "Hips/UpperLeg_L/LowerLeg_L", HumanBodyBones.LeftLowerLeg },
                { "Hips/UpperLeg_L/LowerLeg_L/Foot_L", HumanBodyBones.LeftFoot },
                { "Hips/UpperLeg_R", HumanBodyBones.RightUpperLeg },
                { "Hips/UpperLeg_R/LowerLeg_R", HumanBodyBones.RightLowerLeg },
                { "Hips/UpperLeg_R/LowerLeg_R/Foot_R", HumanBodyBones.RightFoot }
            };
        public List<PoseClipUnitMap> PoseClipUnitMaps = new List<PoseClipUnitMap>();

        public PoseClipMap(VRCAvatarDescriptor desc)
        {
            if (desc == null) AddError("Desctiptorが指定されませんでした。");
            Prj = new PandraProject(desc, PROJECTNAME, ProjectTypes.Asset);
            if (Prj == null) AddError("Prjが取得できませんでした。未知の動作です。");

            Animator = Prj?.RootObject?.GetComponent<Animator>();
            if (Animator == null) AddError("Animatorの取得に失敗しました。");
            if (!Animator.isHuman) AddError("AnimatorがHumanoidではありません_Type1 本システムはHumanoidアバターのみに使用できます。");

            var PoseClipperTransform = Prj?.RootTransform?.GetComponentsInChildren<Transform>(true)?.FirstOrDefault(t => t.name == "PoseClipper" && t.GetComponent<ModularAvatarMergeAnimator>() != null);
            if (PoseClipperTransform == null) AddError("PoseClipperの取得に失敗しました。「PoseClipper」という名称のままアバターの下に入っていることを確認して下さい。");

            PoseClipperArmatureTransform = PoseClipperTransform?.Find("Armature");
            if (PoseClipperArmatureTransform == null) AddError("PoseClipperArmatureの取得に失敗しました。");

            PoseClipperScaleTransform = PoseClipperTransform?.Find("Scale");
            if (PoseClipperScaleTransform == null) AddError("PoseClipperScaleObjectの取得に失敗しました。");


            // Humanoidの取得
            foreach (var kvp in ClipperToHumanoid)
            {
                string poseClipperObjName = kvp.Key;
                HumanBodyBones target = kvp.Value;

                var poseClipperTransform = PoseClipperArmatureTransform?.Find(poseClipperObjName);
                if (poseClipperTransform == null) AddError($@"PoseClipperTransformの取得に失敗しました。：{poseClipperObjName}");

                var targetTransform = Animator.GetBoneTransform(target);
                if (targetTransform == null) AddError($@"targetTransformの取得に失敗しました。：{target}");
                PoseClipUnitMaps.Add(new PoseClipUnitMap(poseClipperObjName, target, poseClipperTransform, targetTransform));

            }

            // Armatureの取得
            {
                Transform poseClipperTransform = PoseClipperArmatureTransform;
                if (poseClipperTransform == null) AddError($@"PoseClipperTransformの取得に失敗しました。：Armature");
                Transform hipsTransform = Animator?.GetBoneTransform(HumanBodyBones.Hips);
                if (hipsTransform == null) AddError($@"HumanBodyBones.Hipsの取得に失敗しました。");
                Transform targetTransform = hipsTransform?.parent;
                if (targetTransform == null) AddError($@"AvatarArmatureの取得に失敗しました。");
                PoseClipUnitMaps.Add(new PoseClipUnitMap("Armature", HumanBodyBones.Hips, poseClipperTransform, targetTransform)); // HumanBodyBonesはnull非許容なのでHipsをメモ
            }

            // Constraintの確認
            foreach (PoseClipUnitMap map in PoseClipUnitMaps)
            {
                ParentConstraint existParentConstraint = map?.TargetTransform?.gameObject?.GetComponent<ParentConstraint>();
                if (existParentConstraint != null) AddError($@"設定済みのParentConstraintが見つかりました。本システムが使用するボーンにはParentConstraintが付いていない必要があります。 : {map.TargetHumanBone}");
            }

            // AvatarHeadTransformの取得とConstraintチェック
            {
                AvatarHeadTransform = Animator?.GetBoneTransform(HumanBodyBones.Head);
                if (AvatarHeadTransform == null) AddError($@"AvatarHeadTransformの取得に失敗しました");
                ScaleConstraint exitScaleConstraint = AvatarHeadTransform?.gameObject?.GetComponent<ScaleConstraint>();
                if (exitScaleConstraint != null) AddError($@"設定済みのScaleConstraintが見つかりました。HeadボーンにはScaleConstraintが付いていない必要があります。");
            }
        }

        private void AddError(string msg)
        {
            ErrMsg = $"{ErrMsg}\n{msg}";
            AnyError = true;
        }
    }
}
#endif