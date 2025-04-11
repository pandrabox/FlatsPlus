using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.flatsplus.editor
{


    public class FPMultiToolWork : FlatsWork<FPMultiTool>
    {
        private AnimationClipsBuilder _ac;
        //private Transform _multiHead;
        private string __objRootPath => "MultiTool";
        private string __meshPath => $@"{__objRootPath}/MultiMesh";


        public FPMultiToolWork(FlatsProject fp) : base(fp) { }

        sealed protected override void OnConstruct()
        {
            _ac = new AnimationClipsBuilder();
            SetTransform();
            CreateControl();
        }

        private void SetTransform()
        {
            var multiBluthArmature = _tgt.transform.Find("MultiTool/Root/Armature").NullCheck("multiBluthArmature");
            var multiHead = multiBluthArmature.Find("Head").NullCheck("multiHead");

            multiBluthArmature.transform.localScale = new Vector3(_prj.Hoppe2X, _prj.Hoppe2Y, _prj.Hoppe2Z);
            var mama = multiHead.gameObject.AddComponent<ModularAvatarMergeArmature>();
            mama.mergeTarget = _prj.HumanoidObjectReference(HumanBodyBones.Head);
        }

        private void CreateControl()
        {
            BlendTreeBuilder bb = new BlendTreeBuilder("MultiMesh");
            AnimationClipsBuilder ac = new AnimationClipsBuilder();
            bb.RootDBT(() => {
                bb.Param("1").AddMotion(ac.OnAnim(__meshPath));
                //bb.Param("1").AddAAP(GetParamName("HoppeOff"), 0);//辞書を作るときに便利（ほっぺ強制ON）
                ShapeToggle(bb, "HoppeOff");
            });
            bb.Attach(_tgt.gameObject);
        }
        

        private void ShapeToggle(BlendTreeBuilder bb, string shapeName)
        {
            bb.NName(shapeName).Param("1").Add1D(GetParamName(shapeName), () =>
            {
                bb.Param(0).AddMotion(ShapeAnim(shapeName, 0));
                bb.Param(1).AddMotion(ShapeAnim(shapeName, 100));
            });
        }

        private AnimationClip ShapeAnim(string shapeName, int val)
        {
            string nm = $@"{shapeName}_{val}";
            _ac.Clip(nm).Bind(__meshPath, typeof(SkinnedMeshRenderer), $@"blendShape.{shapeName}").Const2F(val);
            return _ac.Outp(nm);
        }

        public static string GetParamName(string shapeName)
        {
            return $@"FlatsPlus/MT/{shapeName}";
        }
    }
}