using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.modular_avatar.core;
using UnityEngine;


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
            var mama = multiHead.gameObject.AddComponent<ModularAvatarMergeArmature>().NullCheck("multiHeadmama");
            mama.mergeTarget = _prj.HumanoidObjectReference(HumanBodyBones.Head).NullCheck("headReference"); 
        }

        private void CreateControl()
        {
            BlendTreeBuilder bb = new BlendTreeBuilder("MultiMesh");
            AnimationClipsBuilder ac = new AnimationClipsBuilder();
            bb.RootDBT(() =>
            {
                bb.Param("1").AddMotion(ac.OnAnim(__meshPath));
                //bb.Param("1").AddAAP(GetParamName("HoppeOff"), 0);//辞書を作るときに便利（ほっぺ強制ON）
                ShapeToggle(bb, _config.Func_Hoppe, "HoppeOff");
                ShapeToggle(bb, _config.Func_Pen, "PenOff");
                ShapeToggle(bb, _config.Func_Carry, "RingOff");
                ShapeToggle(bb, _config.Func_Move, "CloudOff");
                ShapeToggle(bb, _config.Func_Ico, "i1");
                ShapeToggle(bb, _config.Func_Ico, "i2");
                ShapeToggle(bb, _config.Func_Ico, "i3");
                ShapeToggle(bb, _config.Func_Ico, "i4");
                ShapeToggle(bb, _config.Func_Ico, "i5");
                ShapeToggle(bb, _config.Func_Ico, "i6");
                ShapeToggle(bb, _config.Func_Ico, "i7");
                ShapeToggle(bb, _config.Func_Ico, "i8");
                AntiCulling(bb);
                PenColorSetting(bb);
                RingColorSetting(bb);
            });
            bb.Attach(_tgt.gameObject);
        }


        private void ShapeToggle(BlendTreeBuilder bb, bool isActive, string shapeName)
        {
            bb.NName(shapeName).Param("1").Add1D(GetParamName(shapeName), () =>
            {
                bb.Param(0).AddMotion(ShapeAnim(shapeName, (isActive ? 0 : 100)));
                bb.Param(1).AddMotion(ShapeAnim(shapeName, 100));
            });
        }

        private void AntiCulling(BlendTreeBuilder bb)
        {
            _ac.Clip("AntiCulling")
                .Bind("MultiTool/Root/ForBounds", typeof(Transform), "m_LocalScale.x").Const2F(100)
                .Bind("MultiTool/Root/ForBounds", typeof(Transform), "m_LocalScale.y").Const2F(100)
                .Bind("MultiTool/Root/ForBounds", typeof(Transform), "m_LocalScale.z").Const2F(100);

            bb.NName("AntiCulling").Param("1").AddMotion(_ac.Outp("AntiCulling"));
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

        private void PenColorSetting(BlendTreeBuilder bb)
        {
            int COLORNUM = 8;
            var ac = new AnimationClipsBuilder();
            for (int i = 0; i < COLORNUM; i++)
            {
                Quaternion q = new Quaternion(1, 1, 0, -(float)i / COLORNUM);
                ac.Clip($@"Color{i}").IsQuaternion((x) =>
                {
                    x.Bind("MultiTool/MultiMesh", typeof(SkinnedMeshRenderer), "material._Main2ndTex_ST.@a").Const2F(q);
                });
            }

            int OFFSET = 3;
            bb.NName("PenColor").Param("1").Add1D("FlatsPlus/Pen/ComRx", () =>
            {
                for (int i = 0; i < COLORNUM; i++)
                {
                    bb.Param(i + OFFSET).AddMotion(ac.Outp($@"Color{i}"));
                }
            });
        }

        private void RingColorSetting(BlendTreeBuilder bb)
        {
            int COLORNUM = 3;
            var ac = new AnimationClipsBuilder();
            for (int i = 0; i < COLORNUM; i++)
            {
                Quaternion q = new Quaternion(1, 1, -(float)i / COLORNUM, 0);
                ac.Clip($@"RingColor{i}").IsQuaternion((x) =>
                {
                    x.Bind("MultiTool/MultiMesh", typeof(SkinnedMeshRenderer), "material._Main3rdTex_ST.@a").Const2F(q);
                });
            }
            bb.NName("RingColor").Param("1").Add1D("FlatsPlus/Carry/ModeActual", () =>
            {
                bb.Param(3).AddMotion(ac.Outp($@"RingColor{0}")); //0はないので無視、1,2はTakeMeで青
                bb.Param(4).Add1D("IsLocal", () => //4以降はローカルリモートで分岐
                {
                    bb.Param(0).AddMotion(ac.Outp($@"RingColor{1}")); //リモートは赤
                    bb.Param(1).AddMotion(ac.Outp($@"RingColor{2}")); //ローカルは灰
                });
            });
        }
    }
}